# 🦞 OpenClaw Unity Plugin

Unity를 [OpenClaw](https://github.com/openclaw/openclaw) AI 어시스턴트와 HTTP로 연결합니다. **Play 모드 없이 Editor 모드에서도 작동**합니다!

[![Unity](https://img.shields.io/badge/Unity-2021.3+-black?logo=unity)](https://unity.com)
[![OpenClaw](https://img.shields.io/badge/OpenClaw-2026.2.3+-green)](https://github.com/openclaw/openclaw)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## ⚠️ 면책 조항

이 소프트웨어는 **베타** 버전입니다. 사용에 따른 책임은 본인에게 있습니다.

- 사용 전 항상 프로젝트를 백업하세요
- 별도의 테스트 프로젝트에서 먼저 테스트하세요
- 저작자는 데이터 손실이나 프로젝트 손상에 대해 책임지지 않습니다

전체 조항은 [LICENSE](LICENSE)를 참조하세요.

## 🔀 하이브리드 아키텍처

이 플러그인은 **두 가지 연결 모드**를 지원합니다 - 워크플로우에 맞게 선택하세요:

### 모드 A: OpenClaw Gateway (원격 접속)
```
Telegram/Discord/Web → OpenClaw Gateway → Unity Plugin
```
- ✅ 어디서든 원격 접속
- ✅ 채팅 통합 (Telegram, Discord 등)
- ✅ 크론 작업, 자동화, 멀티 디바이스
- ⚠️ OpenClaw Gateway 실행 필요

### 모드 B: MCP 직접 연결 (로컬 개발)
```
Claude Code/Desktop → MCP Server → Unity Plugin
```
- ✅ 직접 연결, 미들웨어 불필요
- ✅ Claude Code, Cursor 등과 바로 연동
- ✅ 로컬 개발 시 낮은 지연시간
- ⚠️ 로컬 전용 (127.0.0.1)

### 빠른 설정

| 모드 | 설정 방법 |
|------|----------|
| **OpenClaw** | 플러그인 설치만 하면 Gateway가 연결 처리 |
| **MCP** | MCP Bridge 활성화: `Window > OpenClaw > Start MCP Bridge` |

📖 **[Setup Guide](Documentation~/SETUP_GUIDE.md)** | **[셋업 가이드](Documentation~/SETUP_GUIDE_KO.md)**

## ✨ 주요 기능

- 🎮 **Editor & Play 모드 모두 지원** - AI 도구 사용에 Play 버튼 불필요
- 🔌 **자동 연결** - Unity 시작 시 연결, 모드 전환 시에도 연결 유지
- 📋 **콘솔 통합** - 디버깅을 위한 Unity 로그 캡처 및 조회
- 🎬 **씬 관리** - 씬 목록, 로드, 검사
- 🔧 **컴포넌트 편집** - 컴포넌트 추가, 제거, 속성 수정
- 📸 **디버그 도구** - 스크린샷, 하이어라키 뷰 등
- 🎯 **입력 시뮬레이션** - 게임 테스트를 위한 키보드, 마우스, UI 상호작용
- 🔄 **에디터 제어** - 원격으로 리컴파일 및 에셋 새로고침 트리거
- 🔒 **보안 제어** - 허용 작업 설정 가능

## 요구사항

| 구성요소 | 버전 |
|---------|------|
| **Unity** | 2021.3+ |
| **OpenClaw** | 2026.2.3+ |

> ⚠️ **Unity 6000.3 (Unity 6.3 LTS)에서만 테스트되었습니다.**
> 
> Unity 2021.3+ 용으로 설계되었지만, Unity 6000.3.7f1 (Unity 6.3 LTS)에서만 테스트되었습니다.
> 다른 Unity 버전에서 문제가 발생하면:
> - 🐛 [Issue 등록](https://github.com/TomLeeLive/openclaw-unity-plugin/issues) - Unity 버전과 에러 내용 포함
> - 🔧 [Pull Request 제출](https://github.com/TomLeeLive/openclaw-unity-plugin/pulls) - 수정사항이 있다면
> 
> 여러분의 기여가 모든 Unity 버전 지원에 도움이 됩니다!

## 설치

### 옵션 1: Git URL (권장)

1. Unity Package Manager 열기 (`Window > Package Manager`)
2. `+` → `Add package from git URL...` 클릭
3. 입력:
   ```
   https://github.com/TomLeeLive/openclaw-unity-plugin.git
   ```

### 옵션 2: 로컬 패키지

1. 이 저장소 클론
2. Unity에서: `Window > Package Manager` → `+` → `Add package from disk...`
3. `package.json` 파일 선택

## 빠른 시작

### 1. OpenClaw Gateway Extension 설치 (필수)

Gateway 확장 파일을 OpenClaw에 복사:

```bash
# 확장 파일 복사
cp -r OpenClawPlugin~/* ~/.openclaw/extensions/unity/

# 확장 로드를 위해 Gateway 재시작
openclaw gateway restart

# 확인
openclaw unity status
```

> **참고:** `OpenClawPlugin~`에는 `unity_execute` 및 `unity_sessions` 도구를 활성화하는 Gateway 확장이 포함되어 있습니다. OpenClaw가 Unity와 통신하려면 필수입니다.

### 2. Unity 패키지 설치

위의 [설치](#설치) 섹션에서 Git URL 또는 로컬 패키지 설정을 참조하세요.

### 3. Unity에서 설정

1. `Window > OpenClaw Plugin` 열기
2. Gateway URL 설정: `http://localhost:18789` (기본값)
3. Unity 시작 시 자동 연결
4. 연결되면 상태가 녹색으로 표시

### 4. OpenClaw와 대화하기

OpenClaw에게 씬 검사, 오브젝트 생성, 이슈 디버그를 요청하세요 - Play 모드 진입 없이 모두 가능!

### 5. OpenClaw Skill 설치 (선택)

Companion skill은 AI를 위한 워크플로우 패턴과 도구 참조를 제공합니다:

```bash
# OpenClaw 워크스페이스에 skill 클론
git clone https://github.com/TomLeeLive/openclaw-unity-skill.git ~/.openclaw/workspace/skills/unity-plugin
```

Skill 제공 내용:
- 50개 도구 전체 빠른 참조
- 일반적인 워크플로우 패턴 (씬 검사, UI 테스트 등)
- 상세한 파라미터 문서
- 문제 해결 가이드

> **참고:** Skill은 Gateway 확장과 별개입니다. 확장은 도구를 활성화하고, skill은 AI가 효과적으로 사용하는 방법을 알려줍니다.

## 📚 문서

- **[개발 가이드](Documentation~/DEVELOPMENT.md)** - 아키텍처, 도구 확장, 기여 가이드라인
- **[테스트 가이드](Documentation~/TESTING.md)** - 예제가 포함된 완전한 테스트 가이드

## 사용 가능한 도구 (총 ~100개)

### Console (3개)
| 도구 | 설명 |
|------|------|
| `console.getLogs` | Unity 콘솔 로그 가져오기 (타입 필터 지원) |
| `console.getErrors` | 에러/예외 로그 가져오기 (경고 포함 옵션) |
| `console.clear` | 캡처된 로그 지우기 |

### Scene (5개)
| 도구 | 설명 |
|------|------|
| `scene.list` | 빌드 설정의 모든 씬 목록 |
| `scene.getActive` | 활성 씬 정보 가져오기 |
| `scene.getData` | 씬 하이어라키 데이터 가져오기 |
| `scene.load` | 이름으로 씬 로드 (Play 모드) |
| `scene.open` | Editor 모드에서 씬 열기 |

### GameObject (7개)
| 도구 | 설명 |
|------|------|
| `gameobject.find` | 이름, 태그, 컴포넌트 타입으로 찾기 |
| `gameobject.create` | GameObject 또는 프리미티브 생성 |
| `gameobject.destroy` | GameObject 파괴 |
| `gameobject.delete` | GameObject 삭제 (destroy 별칭) |
| `gameobject.getData` | 상세 오브젝트 데이터 가져오기 |
| `gameobject.setActive` | 오브젝트 활성화/비활성화 |
| `gameobject.setParent` | 부모 변경 |

### Transform (6개)
| 도구 | 설명 |
|------|------|
| `transform.getPosition` | 월드 위치 가져오기 (x, y, z) |
| `transform.getRotation` | 오일러 각도 회전값 가져오기 |
| `transform.getScale` | 로컬 스케일 가져오기 |
| `transform.setPosition` | 월드 위치 설정 |
| `transform.setRotation` | 회전 설정 (오일러) |
| `transform.setScale` | 로컬 스케일 설정 |

### Component (5개)
| 도구 | 설명 |
|------|------|
| `component.add` | 오브젝트에 컴포넌트 추가 |
| `component.remove` | 컴포넌트 제거 |
| `component.get` | 컴포넌트 데이터 가져오기 |
| `component.set` | 필드/속성 값 설정 |
| `component.list` | 사용 가능한 타입 목록 |

### Script (3개)
| 도구 | 설명 |
|------|------|
| `script.execute` | 코드/메서드 실행 (Debug.Log, Time.timeScale, PlayerPrefs, 리플렉션 호출) |
| `script.read` | 스크립트 파일 내용 읽기 |
| `script.list` | 프로젝트의 스크립트 파일 목록 |

### Application (4개)
| 도구 | 설명 |
|------|------|
| `app.getState` | 플레이 모드, FPS 등 가져오기 |
| `app.play` | 플레이 모드 진입 (Editor) |
| `app.pause` | 일시정지 토글 (Editor) |
| `app.stop` | 플레이 모드 종료 (Editor) |

### Debug (3개)
| 도구 | 설명 |
|------|------|
| `debug.log` | 콘솔에 쓰기 |
| `debug.screenshot` | 스크린샷 캡처 (UI 포함) |
| `debug.hierarchy` | 텍스트 하이어라키 뷰 |

### Editor (4개)
| 도구 | 설명 |
|------|------|
| `editor.refresh` | AssetDatabase 새로고침 (리컴파일 트리거) |
| `editor.recompile` | 스크립트 리컴파일 요청 |
| `editor.focusWindow` | Editor 창 포커스 (game/scene/console/hierarchy/project/inspector) |
| `editor.listWindows` | 열린 Editor 창 모두 목록 |

### Input Simulation (10개)
| 도구 | 설명 |
|------|------|
| `input.keyPress` | 키 누르고 떼기 |
| `input.keyDown` | 키 누르고 있기 |
| `input.keyUp` | 키 떼기 |
| `input.type` | 입력 필드에 텍스트 입력 |
| `input.mouseMove` | 마우스 커서 이동 |
| `input.mouseClick` | 위치에서 클릭 |
| `input.mouseDrag` | A에서 B로 드래그 |
| `input.mouseScroll` | 스크롤 휠 |
| `input.getMousePosition` | 현재 커서 위치 가져오기 |
| `input.clickUI` | 이름으로 UI 요소 클릭 |

### Material (5개) - v1.5.0 신규
| 도구 | 설명 |
|------|------|
| `material.create` | 셰이더, 색상, 메탈릭, 매끄러움으로 머티리얼 생성 |
| `material.assign` | GameObject에 머티리얼 할당 |
| `material.modify` | 머티리얼 속성 수정 (색상, 메탈릭, 이미션 등) |
| `material.getInfo` | 모든 셰이더 속성 포함 상세 머티리얼 정보 |
| `material.list` | 프로젝트 내 머티리얼 목록 (필터링) |

### Prefab (5개) - v1.5.0 신규
| 도구 | 설명 |
|------|------|
| `prefab.create` | 씬 GameObject에서 프리팹 생성 |
| `prefab.instantiate` | 씬에 프리팹 인스턴스화 (위치 지정) |
| `prefab.open` | 프리팹 편집 모드 열기 |
| `prefab.close` | 프리팹 편집 모드 닫기 |
| `prefab.save` | 현재 편집 중인 프리팹 저장 |

### Asset (7개) - v1.5.0 신규
| 도구 | 설명 |
|------|------|
| `asset.find` | 쿼리, 타입, 폴더로 에셋 검색 |
| `asset.copy` | 에셋을 새 경로로 복사 |
| `asset.move` | 에셋 이동/이름 변경 |
| `asset.delete` | 에셋 삭제 (휴지통 옵션) |
| `asset.refresh` | AssetDatabase 새로고침 |
| `asset.import` | 특정 에셋 임포트/재임포트 |
| `asset.getPath` | 이름으로 에셋 경로 가져오기 |

### Package Manager (4개) - v1.5.0 신규
| 도구 | 설명 |
|------|------|
| `package.add` | 패키지 설치 (이름 또는 git URL) |
| `package.remove` | 설치된 패키지 제거 |
| `package.list` | 설치된 패키지 목록 |
| `package.search` | Unity 패키지 레지스트리 검색 |

### Test Runner (3개) - v1.5.0 신규
| 도구 | 설명 |
|------|------|
| `test.run` | EditMode/PlayMode 테스트 실행 (필터링) |
| `test.list` | 사용 가능한 테스트 목록 |
| `test.getResults` | 마지막 테스트 실행 결과 가져오기 |

### Batch Execution (1개) - v1.6.0 신규
| 도구 | 설명 |
|------|------|
| `batch.execute` | 다중 도구 일괄 실행 (10-100x 성능 향상) |

**예제:**
```json
{
  "commands": [
    { "tool": "scene.getActive", "params": {} },
    { "tool": "gameobject.find", "params": { "name": "Player" } },
    { "tool": "debug.screenshot", "params": {} }
  ],
  "stopOnError": false
}
```

### Session Info (1개) - v1.6.0 신규
| 도구 | 설명 |
|------|------|
| `session.getInfo` | 세션 정보 (프로젝트, processId, machineName) - 멀티 인스턴스 지원 |

### ScriptableObject (6개) - v1.6.0 신규
| 도구 | 설명 |
|------|------|
| `scriptableobject.create` | 새 ScriptableObject 에셋 생성 |
| `scriptableobject.load` | ScriptableObject 필드 로드 및 검사 |
| `scriptableobject.save` | ScriptableObject 변경사항 저장 |
| `scriptableobject.getField` | 특정 필드 값 가져오기 |
| `scriptableobject.setField` | 필드 값 설정 (자동 저장) |
| `scriptableobject.list` | 프로젝트의 ScriptableObject 목록 |

### Shader (3개) - v1.6.0 신규
| 도구 | 설명 |
|------|------|
| `shader.list` | 프로젝트의 셰이더 목록 |
| `shader.getInfo` | 셰이더 속성 및 정보 |
| `shader.getKeywords` | 셰이더 키워드 가져오기 |

### Texture (5개) - v1.6.0 신규
| 도구 | 설명 |
|------|------|
| `texture.create` | 색상 채움으로 새 텍스처 생성 |
| `texture.getInfo` | 텍스처 정보 (크기, 포맷, 임포트 설정) |
| `texture.setPixels` | 영역 색상 채우기 |
| `texture.resize` | 임포트 설정으로 텍스처 크기 조정 |
| `texture.list` | 프로젝트의 텍스처 목록 |

## 🔧 커스텀 도구 API - v1.6.0 신규

프로젝트별 커스텀 도구를 등록하여 OpenClaw 기능을 확장할 수 있습니다:

```csharp
using OpenClaw.Unity;

// 게임 코드에서
OpenClawCustomTools.Register(new CustomTool
{
    Name = "mygame.spawnEnemy",
    Description = "지정 위치에 적 스폰",
    Execute = (args) => {
        var x = args.TryGetValue("x", out var xv) ? Convert.ToSingle(xv) : 0;
        var y = args.TryGetValue("y", out var yv) ? Convert.ToSingle(yv) : 0;
        var z = args.TryGetValue("z", out var zv) ? Convert.ToSingle(zv) : 0;
        
        // 스폰 로직
        var enemy = EnemyManager.Spawn(new Vector3(x, y, z));
        
        return new { success = true, spawned = enemy.name, position = new { x, y, z } };
    }
});

// 간단한 등록 방법
OpenClawCustomTools.Register(
    "mygame.getScore",
    "현재 점수 가져오기",
    (args) => new { success = true, score = GameManager.Score }
);
```

## 📦 MCP Resources - v1.6.0 신규

MCP 프로토콜의 Resources 기능을 지원합니다. 다음 리소스에 접근 가능:

| URI | 설명 |
|-----|------|
| `unity://scene/hierarchy` | 현재 씬 하이어라키 |
| `unity://scene/active` | 활성 씬 정보 |
| `unity://project/scripts` | 프로젝트 스크립트 목록 |
| `unity://project/scenes` | 빌드 설정 씬 목록 |
| `unity://project/assets?query=Player&type=Prefab` | 에셋 검색 |
| `unity://editor/state` | 에디터 상태 |
| `unity://console/logs` | 콘솔 로그 |
| `unity://session/info` | 세션 정보 |

## 아키텍처

```
┌─────────────────────────────────────────────────────────────┐
│                     Unity Editor                             │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │           OpenClawEditorBridge                          │ │
│  │           [InitializeOnLoad]                            │ │
│  │                                                          │ │
│  │  • EditorApplication.delayCall → 안전한 초기화          │ │
│  │  • EditorApplication.update → 연결 폴링                 │ │
│  │  • SessionState → Play 모드 전환 시 생존               │ │
│  └──────────────────────┬─────────────────────────────────┘ │
│                         │                                    │
│                         ▼                                    │
│  ┌────────────────────────────────────────────────────────┐ │
│  │         OpenClawConnectionManager                       │ │
│  │         (싱글톤 - 모드 간 공유)                          │ │
│  │                                                          │ │
│  │  • 명령을 위한 HTTP 폴링                                │ │
│  │  • 메인 스레드 실행 큐                                  │ │
│  │  • 자동 재연결                                          │ │
│  │  • 중첩 객체 지원 JSON 파싱                             │ │
│  └──────────────────────┬─────────────────────────────────┘ │
│                         │                                    │
│                         ▼                                    │
│  ┌────────────────────────────────────────────────────────┐ │
│  │           OpenClawTools (50개 도구)                     │ │
│  │                                                          │ │
│  │  • Scene/GameObject/Component 조작                      │ │
│  │  • 디버그 도구 (스크린샷, 하이어라키)                   │ │
│  │  • 입력 시뮬레이션 (키보드, 마우스, UI)                 │ │
│  │  • 에디터 제어 (리컴파일, 새로고침)                     │ │
│  └────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
                              │
                              │ HTTP
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                   OpenClaw Gateway                            │
│                   http://localhost:18789                      │
│                                                               │
│  엔드포인트:                                                  │
│  • POST /unity/register  - Unity 세션 등록                   │
│  • POST /unity/heartbeat - 세션 유지                         │
│  • GET  /unity/poll      - 명령 폴링                         │
│  • POST /unity/result    - 도구 실행 결과 전송               │
└──────────────────────────────────────────────────────────────┘
```

## 설정

`Assets > Create > OpenClaw > Config`로 생성하고 `Resources` 폴더에 배치합니다.

| 설정 | 설명 | 기본값 |
|------|------|--------|
| `gatewayUrl` | OpenClaw Gateway URL | `http://localhost:18789` |
| `apiToken` | 선택적 API 토큰 | (비어있음) |
| `autoConnect` | 시작 시 연결 | `true` |
| `showStatusOverlay` | Game 뷰에 상태 표시 | `true` |
| `captureConsoleLogs` | AI용 로그 캡처 | `true` |
| `allowCodeExecution` | 코드 실행 허용 | `true` |
| `allowFileAccess` | 파일 작업 허용 | `true` |
| `allowSceneModification` | 씬 변경 허용 | `true` |

## 사용 예시

### 씬 검사
```
You: 내 씬에 어떤 GameObjects가 있어?

OpenClaw: [debug.hierarchy 실행]

씬 구성:
▶ Main Camera [Camera, AudioListener]
▶ Directional Light [Light]
▶ Player [PlayerController, Rigidbody]
  ▶ Model [MeshRenderer]
▶ UI Canvas [Canvas, GraphicRaycaster]
  ▶ PlayButton [Button]
```

### 입력 시뮬레이션으로 게임 테스트
```
You: 로그인 플로우 테스트해줘 - 사용자명 "TestPlayer" 입력하고 Play 클릭

OpenClaw: 
[input.clickUI {name: "UsernameInput"} 실행]
[input.type {text: "TestPlayer"} 실행]
[input.clickUI {name: "PlayButton"} 실행]
[debug.screenshot 실행]

완료! 사용자명 입력 클릭, "TestPlayer" 입력, Play 버튼 클릭했습니다.
결과를 보여주는 스크린샷 첨부.
```

### 원격 리컴파일
```
You: PlayerController 스크립트 업데이트했어, Unity 리컴파일해줘

OpenClaw: [editor.recompile 실행]

스크립트 리컴파일 요청됨. Unity가 곧 리로드됩니다.
```

## 문제 해결

### Bridge가 연결되지 않음
1. Gateway 상태 확인: `openclaw gateway status`
2. URL 확인: 기본값 `http://localhost:18789`
3. `Window > OpenClaw Plugin`에서 오류 확인

### Play 모드 전환 중 연결 끊김
- 플러그인은 `SessionState`를 사용하여 도메인 리로드에서 생존
- Play 모드 전환 후 자동 재연결
- 멈춘 경우 `editor.refresh` 사용 또는 "Force Reconnect" 클릭

### 스크린샷이 잘못된 내용 표시
- Play 모드: `ScreenCapture` 사용 (UI 포함)
- Editor 모드: `Camera.main.Render()` 사용 (오버레이 UI 없음)
- 정확한 게임 스크린샷은 Play 모드 사용

### Play 모드 재시작 후 스크립트 변경 미적용
Unity의 "Enter Play Mode Settings"는 빠른 반복을 위해 도메인 리로드를 건너뛸 수 있지만, 이는 스크립트 리컴파일을 방지합니다.

**증상:**
- Play 모드 재진입 시 코드 변경 미적용
- 스크립트 저장에도 이전 동작 유지
- Play 모드 중 `editor.refresh` 또는 `editor.recompile` 효과 없음

**해결:**
1. `Edit → Project Settings → Editor` 이동
2. "Enter Play Mode Settings" 찾기
3. ✅ **"Reload Domain"** 체크

**설정 효과:**
| 설정 | Reload Domain ON | Reload Domain OFF |
|------|------------------|-------------------|
| 스크립트 변경 | ✅ Play 시 적용 | ❌ 수동 새로고침까지 무시 |
| Play 모드 진입 | ~2-5초 | ~0.5초 |
| 정적 변수 | 리셋 | 유지 |
| 적합한 상황 | 활발한 코딩 개발 | 코드 변경 없는 테스트/플레이 |

**팁:** 개발 중에는 "Reload Domain" ON 유지. 코드 변경 없이 빠른 반복이 필요할 때만 비활성화.

## 🔐 보안: 모델 호출 설정

ClawHub에 퍼블리시하거나 스킬로 설치할 때, 스킬 메타데이터에서 `disableModelInvocation`을 설정할 수 있습니다:

| 설정 | AI 자동 호출 | 사용자 명시적 요청 |
|------|-------------|------------------|
| `false` (기본값) | ✅ 허용 | ✅ 허용 |
| `true` | ❌ 차단 | ✅ 허용 |

### Unity 플러그인 권장: **`true`**

**이유:** Unity 작업 중 AI가 자율적으로 씬 확인, 스크린샷, GameObject 검사 등 보조 작업을 수행하는 것이 유용함.

**`true` 사용 시기:** 민감한 도구 (결제, 삭제, 메시지 전송 등)에 적합

```yaml
# 스킬 메타데이터 예시
metadata:
  openclaw:
    disableModelInvocation: true  # Unity 플러그인 권장값
```

## ⚠️ 중요 사항

- **개발 전용**: 프로덕션 빌드에서는 `allowCodeExecution` 비활성화
- **TextMeshPro**: TMPro 유무와 관계없이 플러그인 작동 (리플렉션 사용)
- **Unity 6**: 지연 초기화로 UPM EPIPE 크래시 방지

## 변경 로그

버전 히스토리는 [CHANGELOG.md](CHANGELOG.md) 참조.

## 라이선스

MIT 라이선스 - [LICENSE](LICENSE) 참조

---

Made with 🦞 by the OpenClaw community
