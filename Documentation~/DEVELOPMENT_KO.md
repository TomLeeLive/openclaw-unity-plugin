# 🛠️ OpenClaw Unity Plugin - Development Guide

이 문서는 OpenClaw Unity Plugin의 개발 가이드입니다. 아키텍처, 새로운 도구 추가 방법, 디버깅 팁 등을 다룹니다.

## 목차

1. [아키텍처 개요](#아키텍처-개요)
2. [프로젝트 구조](#프로젝트-구조)
3. [핵심 컴포넌트](#핵심-컴포넌트)
4. [새로운 도구 추가하기](#새로운-도구-추가하기)
5. [JSON 파싱](#json-파싱)
6. [Play Mode 전환 처리](#play-mode-전환-처리)
7. [디버깅](#디버깅)
8. [기여 가이드라인](#기여-가이드라인)

---

## 아키텍처 개요

### 통신 흐름

```
┌─────────────┐     HTTP      ┌─────────────────┐     Tool Call    ┌─────────────┐
│   OpenClaw  │ ────────────► │ Gateway Plugin  │ ───────────────► │    Unity    │
│   Agent     │               │ (index.ts)      │                  │   Plugin    │
└─────────────┘               └─────────────────┘                  └─────────────┘
                                      │                                   │
                                      │ POST /unity/poll                  │
                                      │◄──────────────────────────────────│
                                      │                                   │
                                      │ Command JSON                      │
                                      │──────────────────────────────────►│
                                      │                                   │
                                      │ POST /unity/result                │
                                      │◄──────────────────────────────────│
```

### 핵심 설계 원칙

1. **Edit Mode 지원**: Play 버튼 없이 Editor에서 AI 도구 사용 가능
2. **자동 재연결**: 연결 끊김 시 자동 복구
3. **Play Mode 전환 생존**: SessionState로 도메인 리로드 생존
4. **Main Thread 실행**: Unity API는 Main Thread에서만 호출

---

## 프로젝트 구조

```
openclaw-unity-plugin/
├── package.json              # UPM 패키지 정의
├── README.md                 # 사용자 문서
├── CHANGELOG.md              # 버전 히스토리
│
├── Runtime/                  # 런타임 코드 (Editor + Play)
│   ├── OpenClaw.Unity.asmdef
│   ├── OpenClawConnectionManager.cs   # HTTP 통신 담당
│   ├── OpenClawTools.cs               # 44개 도구 구현
│   ├── OpenClawBridge.cs              # MonoBehaviour (Play Mode)
│   ├── OpenClawConfig.cs              # 설정 ScriptableObject
│   ├── OpenClawLogger.cs              # 로그 캡처
│   └── OpenClawStatusOverlay.cs       # 상태 오버레이 UI
│
├── Editor/                   # Editor 전용 코드
│   ├── OpenClaw.Unity.Editor.asmdef
│   ├── OpenClawEditorBridge.cs        # [InitializeOnLoad] 진입점
│   └── OpenClawWindow.cs              # 설정 창
│
└── docs/                     # 문서
    ├── DEVELOPMENT.md        # 개발 가이드 (이 문서)
    └── TESTING.md            # 테스트 가이드
```

---

## 핵심 컴포넌트

### OpenClawEditorBridge.cs

Editor가 시작될 때 자동으로 초기화되는 진입점입니다.

```csharp
[InitializeOnLoad]
public static class OpenClawEditorBridge
{
    static OpenClawEditorBridge()
    {
        // Unity 6 UPM EPIPE 방지를 위해 지연 초기화
        EditorApplication.delayCall += () =>
        {
            _startTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += DeferredInitialize;
        };
    }
}
```

**주요 기능:**
- `[InitializeOnLoad]`로 Editor 시작 시 자동 실행
- 2초 지연 초기화 (Unity 6 안정성)
- `SessionState`로 Play Mode 전환 시 상태 저장/복원
- `EditorApplication.update`에서 Connection Manager 업데이트

### OpenClawConnectionManager.cs

HTTP 통신과 명령 실행을 담당하는 싱글톤입니다.

```csharp
public class OpenClawConnectionManager : IDisposable
{
    private static OpenClawConnectionManager _instance;
    
    public static OpenClawConnectionManager Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                    _instance = new OpenClawConnectionManager();
                return _instance;
            }
        }
    }
}
```

**주요 기능:**
- HTTP 폴링으로 Gateway에서 명령 수신
- Main Thread 큐로 Unity API 안전하게 호출
- 자동 재연결 로직
- JSON 파싱 (중첩 객체, 이스케이프 처리)

### OpenClawTools.cs

44개의 AI 도구 구현체입니다.

```csharp
public class OpenClawTools
{
    private readonly Dictionary<string, Func<Dictionary<string, object>, object>> _tools;
    
    public OpenClawTools(OpenClawBridge bridge)
    {
        _tools = new Dictionary<string, Func<Dictionary<string, object>, object>>
        {
            { "console.getLogs", ConsoleGetLogs },
            { "scene.list", SceneList },
            { "gameobject.find", GameObjectFind },
            // ... 44개 도구
        };
    }
}
```

---

## 새로운 도구 추가하기

### 1단계: 도구 메서드 정의

`OpenClawTools.cs`에 새 메서드를 추가합니다:

```csharp
private object MyNewTool(Dictionary<string, object> p)
{
    // 파라미터 추출
    var name = GetString(p, "name", "default");
    var count = GetInt(p, "count", 1);
    var enabled = GetBool(p, "enabled", true);
    
    try
    {
        // 도구 로직 구현
        var result = DoSomething(name, count, enabled);
        
        // 성공 응답
        return new { 
            success = true, 
            result = result,
            message = "Operation completed"
        };
    }
    catch (Exception e)
    {
        // 실패 응답
        return new { 
            success = false, 
            error = e.Message 
        };
    }
}
```

### 2단계: 도구 등록

생성자의 `_tools` Dictionary에 등록합니다:

```csharp
public OpenClawTools(OpenClawBridge bridge)
{
    _tools = new Dictionary<string, Func<Dictionary<string, object>, object>>
    {
        // 기존 도구들...
        
        // 새 도구 추가
        { "myCategory.myNewTool", MyNewTool },
    };
}
```

### 3단계: 설명 추가

`GetToolDescription` 메서드에 설명을 추가합니다:

```csharp
private string GetToolDescription(string name)
{
    return name switch
    {
        // 기존 설명들...
        
        "myCategory.myNewTool" => "Description of my new tool (params: name, count, enabled)",
        
        _ => name
    };
}
```

### 예제: 새로운 도구 추가

다음은 GameObject의 레이어를 변경하는 도구 예제입니다:

```csharp
// 1. 메서드 구현
private object GameObjectSetLayer(Dictionary<string, object> p)
{
    var name = GetString(p, "name", null);
    var layer = GetString(p, "layer", null);
    
    if (string.IsNullOrEmpty(name))
        return new { success = false, error = "name parameter required" };
    
    var go = GameObject.Find(name);
    if (go == null)
        return new { success = false, error = $"GameObject '{name}' not found" };
    
    int layerIndex = LayerMask.NameToLayer(layer);
    if (layerIndex < 0)
        return new { success = false, error = $"Layer '{layer}' not found" };
    
    go.layer = layerIndex;
    return new { 
        success = true, 
        gameObject = name, 
        layer = layer, 
        layerIndex = layerIndex 
    };
}

// 2. 등록 (생성자에서)
{ "gameobject.setLayer", GameObjectSetLayer },

// 3. 설명 추가
"gameobject.setLayer" => "Set GameObject layer (params: name, layer)",
```

---

## JSON 파싱

### 기본 구조

플러그인은 외부 라이브러리 없이 자체 JSON 파싱을 사용합니다:

```csharp
private Dictionary<string, object> ParseJson(string json)
{
    // 중첩 객체 지원
    if (value.StartsWith("{") && value.EndsWith("}"))
        result[key] = ParseJson(value);
    
    // 문자열 이스케이프 해제
    if (value.StartsWith("\"") && value.EndsWith("\""))
        result[key] = UnescapeString(value.Substring(1, value.Length - 2));
}
```

### 파라미터 헬퍼 메서드

```csharp
// 문자열 추출
var str = GetString(p, "key", "defaultValue");

// 정수 추출
var num = GetInt(p, "key", 0);

// 실수 추출
var flt = GetFloat(p, "key", 0.0f);

// 불리언 추출
var flag = GetBool(p, "key", false);
```

---

## Play Mode 전환 처리

### 문제점

Unity에서 Play Mode 진입/종료 시 도메인 리로드가 발생하면:
- 모든 static 변수 초기화
- HttpClient 연결 끊김
- 진행 중인 작업 손실

### 해결책: SessionState

```csharp
// Play Mode 전환 전 상태 저장
private static void OnPlayModeStateChanged(PlayModeStateChange state)
{
    switch (state)
    {
        case PlayModeStateChange.ExitingEditMode:
        case PlayModeStateChange.ExitingPlayMode:
            SessionState.SetBool(WAS_CONNECTED_KEY, manager.IsConnected);
            SessionState.SetString(SESSION_ID_KEY, manager.SessionId ?? "");
            SessionState.SetBool(PLAY_MODE_TRANSITION_KEY, true);
            break;
            
        case PlayModeStateChange.EnteredPlayMode:
        case PlayModeStateChange.EnteredEditMode:
            // 초기화 시 자동 재연결
            if (SessionState.GetBool(PLAY_MODE_TRANSITION_KEY, false))
            {
                manager.ConnectAsync();
            }
            break;
    }
}
```

---

## 디버깅

### Unity Console 로그

플러그인은 `[OpenClaw]` 접두사로 로그를 출력합니다:

```
[OpenClaw] Connecting to http://localhost:18789...
[OpenClaw] Connected! Session: unity_1234567890_abc123
[OpenClaw] Received command: debug.hierarchy
[OpenClaw] Tool result: debug.hierarchy - success
```

### Gateway 로그

```bash
openclaw gateway status
# 또는 Gateway 콘솔에서:
[Unity] Registered: MyProject (6000.3.7f1) - Session: unity_xxx
[Unity] Tool result: debug.hierarchy - success
```

### 연결 문제 디버깅

1. **Gateway 상태 확인**
   ```bash
   openclaw gateway status
   ```

2. **Unity 창에서 확인**
   - `Window > OpenClaw Plugin` 열기
   - Connection Status 확인
   - "Test Connection" 버튼 클릭

3. **HTTP 직접 테스트**
   ```bash
   curl http://localhost:18789/unity/status
   ```

### 자주 발생하는 실수

#### ❌ UnityEditor.Resources (존재하지 않음)

```csharp
// 잘못된 코드 - 컴파일 에러 발생
var windows = UnityEditor.Resources.FindObjectsOfTypeAll<EditorWindow>();
```

#### ✅ Resources.FindObjectsOfTypeAll (올바른 방법)

```csharp
// 올바른 코드 - UnityEngine.Resources 사용
var windows = Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>();
```

> **참고:** `Resources.FindObjectsOfTypeAll`은 `UnityEngine.Resources` 클래스의 메서드입니다.
> Editor 전용 타입(예: `EditorWindow`)을 찾을 때도 `UnityEngine.Resources`를 사용해야 합니다.
> `UnityEditor.Resources` 네임스페이스는 존재하지 않습니다. (v1.2.1에서 수정됨)

---

## 기여 가이드라인

### 코드 스타일

- C# 표준 명명 규칙 (PascalCase for public, _camelCase for private)
- 모든 public 메서드에 XML 문서 주석
- Unity API 호출은 Main Thread에서만

### 커밋 메시지

```
feat: Add new input simulation tools
fix: Properly handle nested JSON objects
docs: Update README with new tools
refactor: Simplify connection manager
```

### 테스트

새 기능 추가 시 반드시 테스트:
1. Editor Mode에서 동작 확인
2. Play Mode에서 동작 확인
3. Play Mode 전환 시 연결 유지 확인

### Pull Request

1. 새 브랜치 생성: `feature/your-feature-name`
2. 변경사항 커밋
3. CHANGELOG.md 업데이트
4. Pull Request 생성

---

## 연락처

- GitHub: https://github.com/TomLeeLive/openclaw-unity-plugin
- OpenClaw Discord: https://discord.com/invite/clawd
