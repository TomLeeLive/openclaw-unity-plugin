# 🧪 OpenClaw Unity Plugin - Testing Guide

이 문서는 OpenClaw Unity Plugin의 테스트 가이드입니다. 모든 44개 도구의 사용법과 예제를 포함합니다.

## 목차

1. [테스트 환경 설정](#테스트-환경-설정)
2. [기본 연결 테스트](#기본-연결-테스트)
3. [Console 도구](#console-도구)
4. [Scene 도구](#scene-도구)
5. [GameObject 도구](#gameobject-도구)
6. [Transform 도구](#transform-도구)
7. [Component 도구](#component-도구)
8. [Application 도구](#application-도구)
9. [Debug 도구](#debug-도구)
10. [Editor 도구](#editor-도구)
11. [Input 도구](#input-도구)
12. [자동화 테스트 시나리오](#자동화-테스트-시나리오)

---

## 테스트 환경 설정

### 1. OpenClaw Gateway 시작

```bash
# Gateway 상태 확인
openclaw gateway status

# Gateway 시작 (필요한 경우)
openclaw gateway start
```

### 2. Unity 프로젝트 준비

1. OpenClaw Unity Plugin 설치 (README.md 참조)
2. `Window > OpenClaw Plugin` 열기
3. Gateway URL 확인: `http://localhost:18789`
4. "OpenClaw Connected" 상태 확인

### 3. 테스트 세션 확인

```bash
# 연결된 Unity 세션 확인
openclaw unity status
```

예상 출력:
```
🎮 Unity Bridge Status

  ✅ MyProject
     Version: 6000.3.7f1
     Platform: Editor
     Session: unity_1234567890_abc123
     Connected: 30s ago
     Last seen: 1s ago
```

---

## 기본 연결 테스트

### unity_sessions - 세션 목록 조회

**설명:** 연결된 모든 Unity 세션 목록을 반환합니다.

**파라미터:** 없음

**예제:**
```
OpenClaw에게: "연결된 Unity 세션 확인해줘"
```

**응답 예시:**
```json
{
  "success": true,
  "sessions": [
    {
      "sessionId": "unity_1770465206658_j3x59z4",
      "project": "endless_survival",
      "version": "6000.3.7f1",
      "platform": "Editor",
      "tools": 42
    }
  ],
  "count": 1
}
```

---

## Console 도구

### console.getLogs - 로그 조회

**설명:** Unity Console의 로그를 가져옵니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `count` | int | 100 | 가져올 로그 수 |
| `type` | string | null | 필터: "log", "warning", "error" |

**예제 1: 모든 로그 조회**
```
OpenClaw에게: "Unity 콘솔 로그 보여줘"
```
```json
// 도구 호출
{ "tool": "console.getLogs", "parameters": {} }

// 응답
[
  { "type": "Log", "message": "[OpenClaw] Connected!", "timestamp": "..." },
  { "type": "Warning", "message": "Shader not found", "timestamp": "..." }
]
```

**예제 2: 에러만 조회**
```
OpenClaw에게: "Unity 에러 로그만 보여줘"
```
```json
// 도구 호출
{ "tool": "console.getLogs", "parameters": { "type": "error", "count": 10 } }
```

### console.clear - 로그 초기화

**설명:** 캡처된 로그를 초기화합니다.

**파라미터:** 없음

**예제:**
```
OpenClaw에게: "Unity 콘솔 로그 지워줘"
```
```json
// 도구 호출
{ "tool": "console.clear", "parameters": {} }

// 응답
{ "success": true }
```

---

## Scene 도구

### scene.list - 씬 목록 조회

**설명:** Build Settings에 등록된 모든 씬 목록을 반환합니다.

**파라미터:** 없음

**예제:**
```
OpenClaw에게: "프로젝트에 있는 씬 목록 보여줘"
```
```json
// 응답
[
  { "index": 0, "path": "Assets/Scenes/MainMenu.unity", "name": "MainMenu" },
  { "index": 1, "path": "Assets/Scenes/GameScene.unity", "name": "GameScene" }
]
```

### scene.getActive - 현재 씬 정보

**설명:** 현재 활성 씬의 정보를 반환합니다.

**파라미터:** 없음

**예제:**
```
OpenClaw에게: "현재 열린 씬 정보 알려줘"
```
```json
// 응답
{
  "name": "MainMenu",
  "path": "Assets/Scenes/MainMenu.unity",
  "buildIndex": 0,
  "isLoaded": true,
  "rootCount": 5
}
```

### scene.getData - 씬 데이터 조회

**설명:** 씬의 계층 구조 데이터를 반환합니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `name` | string | null | 씬 이름 (null이면 현재 씬) |
| `depth` | int | 2 | 계층 깊이 |

**예제:**
```
OpenClaw에게: "현재 씬 구조 3단계까지 보여줘"
```
```json
// 도구 호출
{ "tool": "scene.getData", "parameters": { "depth": 3 } }
```

### scene.load - 씬 로드

**설명:** 씬을 로드합니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `name` | string | (필수) | 씬 이름 |
| `mode` | string | "Single" | "Single" 또는 "Additive" |

**예제:**
```
OpenClaw에게: "GameScene 씬으로 전환해줘"
```
```json
// 도구 호출
{ "tool": "scene.load", "parameters": { "name": "GameScene" } }

// 응답
{ "success": true, "scene": "GameScene" }
```

---

## GameObject 도구

### gameobject.find - 오브젝트 검색

**설명:** 이름, 태그, 또는 컴포넌트 타입으로 GameObject를 검색합니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `name` | string | null | 이름으로 검색 |
| `tag` | string | null | 태그로 검색 |
| `type` | string | null | 컴포넌트 타입으로 검색 |
| `depth` | int | 1 | 결과 깊이 |

**예제 1: 이름으로 검색**
```
OpenClaw에게: "Player라는 이름의 오브젝트 찾아줘"
```
```json
// 도구 호출
{ "tool": "gameobject.find", "parameters": { "name": "Player" } }
```

**예제 2: 태그로 검색**
```
OpenClaw에게: "Enemy 태그가 붙은 오브젝트들 찾아줘"
```
```json
// 도구 호출
{ "tool": "gameobject.find", "parameters": { "tag": "Enemy" } }
```

**예제 3: 컴포넌트로 검색**
```
OpenClaw에게: "Camera 컴포넌트가 있는 오브젝트들 찾아줘"
```
```json
// 도구 호출
{ "tool": "gameobject.find", "parameters": { "type": "Camera" } }
```

### gameobject.create - 오브젝트 생성

**설명:** 새 GameObject 또는 Primitive를 생성합니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `name` | string | "New GameObject" | 오브젝트 이름 |
| `primitive` | string | null | "Cube", "Sphere", "Cylinder" 등 |
| `position` | object | null | {x, y, z} 위치 |

**예제 1: 빈 오브젝트 생성**
```
OpenClaw에게: "Enemy라는 빈 오브젝트 만들어줘"
```
```json
// 도구 호출
{ "tool": "gameobject.create", "parameters": { "name": "Enemy" } }
```

**예제 2: Primitive 생성**
```
OpenClaw에게: "위치 (0, 1, 0)에 구체 만들어줘"
```
```json
// 도구 호출
{
  "tool": "gameobject.create",
  "parameters": {
    "name": "MySphere",
    "primitive": "Sphere",
    "position": { "x": 0, "y": 1, "z": 0 }
  }
}
```

### gameobject.destroy - 오브젝트 삭제

**설명:** GameObject를 삭제합니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `name` | string | (필수) | 오브젝트 이름 |

**예제:**
```
OpenClaw에게: "TempObject 삭제해줘"
```
```json
// 도구 호출
{ "tool": "gameobject.destroy", "parameters": { "name": "TempObject" } }

// 응답
{ "success": true, "destroyed": "TempObject" }
```

### gameobject.setActive - 활성화/비활성화

**설명:** GameObject를 활성화하거나 비활성화합니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `name` | string | (필수) | 오브젝트 이름 |
| `active` | bool | true | 활성화 여부 |

**예제:**
```
OpenClaw에게: "Player 오브젝트 비활성화해줘"
```
```json
// 도구 호출
{ "tool": "gameobject.setActive", "parameters": { "name": "Player", "active": false } }
```

---

## Transform 도구

### transform.setPosition - 위치 설정

**설명:** GameObject의 월드 위치를 설정합니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `name` | string | (필수) | 오브젝트 이름 |
| `x` | float | (현재값) | X 좌표 |
| `y` | float | (현재값) | Y 좌표 |
| `z` | float | (현재값) | Z 좌표 |

**예제:**
```
OpenClaw에게: "Player를 원점으로 이동시켜줘"
```
```json
// 도구 호출
{
  "tool": "transform.setPosition",
  "parameters": { "name": "Player", "x": 0, "y": 0, "z": 0 }
}
```

### transform.setRotation - 회전 설정

**설명:** GameObject의 회전을 설정합니다 (Euler angles).

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `name` | string | (필수) | 오브젝트 이름 |
| `x` | float | (현재값) | X 회전 (도) |
| `y` | float | (현재값) | Y 회전 (도) |
| `z` | float | (현재값) | Z 회전 (도) |

**예제:**
```
OpenClaw에게: "Camera를 Y축으로 90도 회전시켜줘"
```
```json
// 도구 호출
{
  "tool": "transform.setRotation",
  "parameters": { "name": "Camera", "y": 90 }
}
```

### transform.setScale - 스케일 설정

**설명:** GameObject의 로컬 스케일을 설정합니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `name` | string | (필수) | 오브젝트 이름 |
| `x` | float | (현재값) | X 스케일 |
| `y` | float | (현재값) | Y 스케일 |
| `z` | float | (현재값) | Z 스케일 |

**예제:**
```
OpenClaw에게: "Cube를 2배 크기로 만들어줘"
```
```json
// 도구 호출
{
  "tool": "transform.setScale",
  "parameters": { "name": "Cube", "x": 2, "y": 2, "z": 2 }
}
```

---

## Component 도구

### component.add - 컴포넌트 추가

**설명:** GameObject에 컴포넌트를 추가합니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `gameObject` | string | (필수) | 오브젝트 이름 |
| `type` | string | (필수) | 컴포넌트 타입 |

**예제:**
```
OpenClaw에게: "Player에 Rigidbody 컴포넌트 추가해줘"
```
```json
// 도구 호출
{
  "tool": "component.add",
  "parameters": { "gameObject": "Player", "type": "Rigidbody" }
}
```

### component.get - 컴포넌트 조회

**설명:** 컴포넌트의 데이터를 가져옵니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `gameObject` | string | (필수) | 오브젝트 이름 |
| `type` | string | (필수) | 컴포넌트 타입 |

**예제:**
```
OpenClaw에게: "Player의 Transform 정보 보여줘"
```
```json
// 도구 호출
{
  "tool": "component.get",
  "parameters": { "gameObject": "Player", "type": "Transform" }
}

// 응답
{
  "type": "Transform",
  "fields": {
    "position": { "x": 0, "y": 1, "z": 0 },
    "rotation": { "x": 0, "y": 0, "z": 0, "w": 1 },
    "localScale": { "x": 1, "y": 1, "z": 1 }
  }
}
```

### component.set - 컴포넌트 값 설정

**설명:** 컴포넌트의 필드/프로퍼티 값을 설정합니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `gameObject` | string | (필수) | 오브젝트 이름 |
| `type` | string | (필수) | 컴포넌트 타입 |
| `field` | string | (필수) | 필드/프로퍼티 이름 |
| `value` | any | (필수) | 설정할 값 |

**예제:**
```
OpenClaw에게: "Player의 Rigidbody mass를 5로 설정해줘"
```
```json
// 도구 호출
{
  "tool": "component.set",
  "parameters": {
    "gameObject": "Player",
    "type": "Rigidbody",
    "field": "mass",
    "value": 5
  }
}
```

---

## Application 도구

### app.getState - 앱 상태 조회

**설명:** 현재 애플리케이션 상태를 반환합니다.

**파라미터:** 없음

**예제:**
```
OpenClaw에게: "현재 Unity 상태 알려줘"
```
```json
// 응답
{
  "isPlaying": true,
  "isPaused": false,
  "platform": "OSXEditor",
  "unityVersion": "6000.3.7f1",
  "productName": "endless_survival",
  "fps": 60,
  "time": 123.456
}
```

### app.play - Play 모드 시작

**설명:** Play 모드를 시작합니다.

**파라미터:** 없음

**예제:**
```
OpenClaw에게: "Unity Play 모드 시작해줘"
```
```json
// 도구 호출
{ "tool": "app.play", "parameters": {} }

// 응답
{ "success": true }
```

### app.stop - Play 모드 종료

**설명:** Play 모드를 종료합니다.

**파라미터:** 없음

**예제:**
```
OpenClaw에게: "Unity Play 모드 종료해줘"
```
```json
// 도구 호출
{ "tool": "app.stop", "parameters": {} }
```

### app.pause - 일시정지 토글

**설명:** Play 모드 일시정지를 토글합니다.

**파라미터:** 없음

**예제:**
```
OpenClaw에게: "게임 일시정지해줘"
```
```json
// 도구 호출
{ "tool": "app.pause", "parameters": {} }

// 응답
{ "success": true, "isPaused": true }
```

---

## Debug 도구

### debug.log - 로그 출력

**설명:** Unity Console에 로그를 출력합니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `message` | string | "" | 로그 메시지 |
| `level` | string | "log" | "log", "warning", "error" |

**예제:**
```
OpenClaw에게: "Unity 콘솔에 'Hello from AI!' 출력해줘"
```
```json
// 도구 호출
{
  "tool": "debug.log",
  "parameters": { "message": "Hello from AI!", "level": "log" }
}
```

### debug.screenshot - 스크린샷 캡처

**설명:** 게임 화면 스크린샷을 캡처합니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `filename` | string | (자동) | 파일 이름 |
| `method` | string | "auto" | "auto", "camera", "screencapture" |
| `width` | int | (자동) | 가로 해상도 |
| `height` | int | (자동) | 세로 해상도 |

**예제:**
```
OpenClaw에게: "현재 게임 화면 캡처해줘"
```
```json
// 도구 호출
{ "tool": "debug.screenshot", "parameters": {} }

// 응답
{
  "success": true,
  "path": "/Users/.../screenshot_20260207_123456.png",
  "mode": "screencapture",
  "width": 1920,
  "height": 1080
}
```

### debug.hierarchy - 계층 구조 출력

**설명:** 씬의 계층 구조를 텍스트로 출력합니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `depth` | int | 3 | 출력 깊이 |

**예제:**
```
OpenClaw에게: "현재 씬 구조 보여줘"
```
```json
// 도구 호출
{ "tool": "debug.hierarchy", "parameters": { "depth": 3 } }

// 응답
"▶ Main Camera [Camera, AudioListener]
▶ Directional Light [Light]
▶ Player [PlayerController]
  ▶ Model [MeshRenderer]
  ▶ Weapon [WeaponController]
▶ UI Canvas [Canvas]
  ▶ HealthBar [Image]
  ▶ ScoreText [TextMeshProUGUI]"
```

---

## Editor 도구

### editor.refresh - 에셋 새로고침

**설명:** AssetDatabase를 새로고침합니다 (스크립트 변경 시 재컴파일 트리거).

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `forceUpdate` | bool | false | 강제 업데이트 여부 |

**예제:**
```
OpenClaw에게: "Unity 에셋 새로고침해줘"
```
```json
// 도구 호출
{ "tool": "editor.refresh", "parameters": { "forceUpdate": true } }

// 응답
{ "success": true, "action": "AssetDatabase.Refresh", "forceUpdate": true }
```

### editor.recompile - 스크립트 재컴파일

**설명:** 스크립트 재컴파일을 요청합니다.

**파라미터:** 없음

**예제:**
```
OpenClaw에게: "Unity 스크립트 재컴파일해줘"
```
```json
// 도구 호출
{ "tool": "editor.recompile", "parameters": {} }

// 응답
{ "success": true, "action": "RequestScriptCompilation" }
```

### editor.focusWindow - 창 포커스

**설명:** 특정 Editor 창에 포커스를 맞춥니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `window` | string | "game" | 창 이름 |

**지원되는 창:**
- `game` / `gameview` - Game View
- `scene` / `sceneview` - Scene View
- `console` - Console
- `hierarchy` - Hierarchy
- `project` - Project Browser
- `inspector` - Inspector
- `profiler` - Profiler
- `animation` - Animation
- `animator` - Animator

**예제:**
```
OpenClaw에게: "Game 창으로 포커스 옮겨줘"
```
```json
// 도구 호출
{ "tool": "editor.focusWindow", "parameters": { "window": "game" } }

// 응답
{ "success": true, "window": "game", "focused": true }
```

### editor.listWindows - 열린 창 목록

**설명:** 현재 열려있는 모든 Editor 창 목록을 반환합니다.

**파라미터:** 없음

**예제:**
```
OpenClaw에게: "현재 열린 Unity 창 목록 보여줘"
```
```json
// 도구 호출
{ "tool": "editor.listWindows", "parameters": {} }

// 응답
{
  "success": true,
  "windows": [
    { "title": "Game", "type": "GameView", "focused": true, "position": "0,0,1920,1080" },
    { "title": "Scene", "type": "SceneView", "focused": false, "position": "0,0,1920,1080" },
    { "title": "Console", "type": "ConsoleWindow", "focused": false, "position": "0,600,1920,400" }
  ],
  "count": 3
}
```

---

## Input 도구

### input.keyPress - 키 입력

**설명:** 키를 눌렀다 뗍니다 (tap).

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `key` | string | (필수) | KeyCode 이름 |
| `duration` | float | 0.1 | 누르는 시간 (초) |

**지원되는 키:**
- 알파벳: `A`-`Z`
- 숫자: `Alpha0`-`Alpha9` 또는 `0`-`9`
- 방향키: `LeftArrow`, `RightArrow`, `UpArrow`, `DownArrow` 또는 `left`, `right`, `up`, `down`
- 특수키: `Space`, `Return`, `Escape`, `Tab`, `Backspace`
- 수정자: `LeftShift`, `RightShift`, `LeftControl`, `LeftAlt`
- 마우스: `Mouse0` (좌클릭), `Mouse1` (우클릭), `Mouse2` (휠클릭)

**예제:**
```
OpenClaw에게: "W키 눌러줘"
```
```json
// 도구 호출
{ "tool": "input.keyPress", "parameters": { "key": "W" } }

// 응답
{ "success": true, "key": "W", "keyCode": "W", "duration": 0.1 }
```

### input.keyDown / input.keyUp - 키 홀드

**설명:** 키를 누르거나 뗍니다.

**예제:**
```
OpenClaw에게: "Shift 키 누른 상태로 유지해줘"
```
```json
// 누르기
{ "tool": "input.keyDown", "parameters": { "key": "LeftShift" } }

// 나중에 떼기
{ "tool": "input.keyUp", "parameters": { "key": "LeftShift" } }
```

### input.type - 텍스트 입력

**설명:** 현재 포커스된 입력 필드에 텍스트를 입력합니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `text` | string | (필수) | 입력할 텍스트 |

**예제:**
```
OpenClaw에게: "입력 필드에 'TestPlayer' 입력해줘"
```
```json
// 도구 호출
{ "tool": "input.type", "parameters": { "text": "TestPlayer" } }

// 응답
{ "success": true, "text": "TestPlayer", "target": "UsernameInput", "method": "TMP_InputField" }
```

### input.mouseMove - 마우스 이동

**설명:** 마우스 커서를 이동합니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `x` | float | (필수) | X 좌표 |
| `y` | float | (필수) | Y 좌표 |
| `normalized` | bool | false | 0-1 정규화 좌표 사용 여부 |

**예제 1: 픽셀 좌표**
```
OpenClaw에게: "마우스를 (500, 300) 위치로 이동해줘"
```
```json
// 도구 호출
{ "tool": "input.mouseMove", "parameters": { "x": 500, "y": 300 } }
```

**예제 2: 정규화 좌표 (화면 중앙)**
```
OpenClaw에게: "마우스를 화면 중앙으로 이동해줘"
```
```json
// 도구 호출
{ "tool": "input.mouseMove", "parameters": { "x": 0.5, "y": 0.5, "normalized": true } }
```

### input.mouseClick - 마우스 클릭

**설명:** 특정 위치에서 마우스를 클릭합니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `x` | float | (현재위치) | X 좌표 |
| `y` | float | (현재위치) | Y 좌표 |
| `button` | int | 0 | 0=좌, 1=우, 2=휠 |
| `clicks` | int | 1 | 클릭 횟수 |
| `normalized` | bool | false | 정규화 좌표 사용 |

**예제:**
```
OpenClaw에게: "(400, 500) 위치에서 더블클릭해줘"
```
```json
// 도구 호출
{
  "tool": "input.mouseClick",
  "parameters": { "x": 400, "y": 500, "clicks": 2 }
}
```

### input.mouseDrag - 마우스 드래그

**설명:** 시작점에서 끝점까지 드래그합니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `startX` | float | (필수) | 시작 X |
| `startY` | float | (필수) | 시작 Y |
| `endX` | float | (필수) | 끝 X |
| `endY` | float | (필수) | 끝 Y |
| `button` | int | 0 | 마우스 버튼 |
| `steps` | int | 10 | 중간 단계 수 |

**예제:**
```
OpenClaw에게: "(100, 100)에서 (500, 500)까지 드래그해줘"
```
```json
// 도구 호출
{
  "tool": "input.mouseDrag",
  "parameters": {
    "startX": 100, "startY": 100,
    "endX": 500, "endY": 500,
    "steps": 20
  }
}
```

### input.mouseScroll - 마우스 스크롤

**설명:** 마우스 휠을 스크롤합니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `deltaX` | float | 0 | 수평 스크롤 |
| `deltaY` | float | 0 | 수직 스크롤 |

**예제:**
```
OpenClaw에게: "아래로 스크롤해줘"
```
```json
// 도구 호출
{ "tool": "input.mouseScroll", "parameters": { "deltaY": -120 } }
```

### input.getMousePosition - 마우스 위치 조회

**설명:** 현재 마우스 커서 위치를 반환합니다.

**파라미터:** 없음

**예제:**
```
OpenClaw에게: "현재 마우스 위치 알려줘"
```
```json
// 응답
{
  "x": 512,
  "y": 384,
  "normalizedX": 0.5,
  "normalizedY": 0.5,
  "screenWidth": 1024,
  "screenHeight": 768
}
```

### input.clickUI - UI 요소 클릭

**설명:** 이름으로 UI 요소를 찾아 클릭합니다.

**파라미터:**
| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `name` | string | null | UI 요소 이름 |
| `path` | string | null | 전체 경로 |
| `button` | int | 0 | 마우스 버튼 |

**예제 1: 이름으로 클릭**
```
OpenClaw에게: "PlayButton 클릭해줘"
```
```json
// 도구 호출
{ "tool": "input.clickUI", "parameters": { "name": "PlayButton" } }

// 응답
{ "success": true, "target": "PlayButton", "method": "Button.onClick" }
```

**예제 2: 경로로 클릭**
```
OpenClaw에게: "Canvas/Menu/StartButton 클릭해줘"
```
```json
// 도구 호출
{ "tool": "input.clickUI", "parameters": { "path": "Canvas/Menu/StartButton" } }
```

---

## 자동화 테스트 시나리오

### 시나리오 1: 로그인 플로우 테스트

```
OpenClaw에게: "로그인 화면 테스트해줘. UsernameInput에 'TestPlayer' 입력하고 PlayButton 클릭해"
```

**실행 순서:**
```json
// 1. 입력 필드 클릭하여 포커스
{ "tool": "input.clickUI", "parameters": { "name": "UsernameInput" } }

// 2. 텍스트 입력
{ "tool": "input.type", "parameters": { "text": "TestPlayer" } }

// 3. Play 버튼 클릭
{ "tool": "input.clickUI", "parameters": { "name": "PlayButton" } }

// 4. 결과 스크린샷
{ "tool": "debug.screenshot", "parameters": {} }
```

### 시나리오 2: 게임플레이 테스트

```
OpenClaw에게: "캐릭터 이동 테스트해줘. W키로 전진, Space로 점프"
```

**실행 순서:**
```json
// 1. Play 모드 시작
{ "tool": "app.play", "parameters": {} }

// 2. W키로 전진 (1초간)
{ "tool": "input.keyDown", "parameters": { "key": "W" } }
// ... 1초 대기 ...
{ "tool": "input.keyUp", "parameters": { "key": "W" } }

// 3. Space로 점프
{ "tool": "input.keyPress", "parameters": { "key": "Space" } }

// 4. 결과 스크린샷
{ "tool": "debug.screenshot", "parameters": {} }
```

### 시나리오 3: UI 네비게이션 테스트

```
OpenClaw에게: "메뉴 네비게이션 테스트해줘. 설정 > 오디오 > 볼륨 조절"
```

**실행 순서:**
```json
// 1. 설정 버튼 클릭
{ "tool": "input.clickUI", "parameters": { "name": "SettingsButton" } }

// 2. 오디오 탭 클릭
{ "tool": "input.clickUI", "parameters": { "name": "AudioTab" } }

// 3. 볼륨 슬라이더 드래그
{
  "tool": "input.mouseDrag",
  "parameters": {
    "startX": 200, "startY": 300,
    "endX": 400, "endY": 300
  }
}

// 4. 결과 확인
{ "tool": "debug.screenshot", "parameters": {} }
```

---

## 문제 해결

### 도구가 "Unknown tool" 에러 반환

- Unity가 재컴파일되었는지 확인
- `editor.recompile` 도구 실행
- Unity 창 클릭하여 포커스

### 파라미터가 전달되지 않음

- JSON 형식 확인
- 파라미터 이름 대소문자 확인
- 플러그인 버전 확인 (v1.2.0+ 필요)

### 스크린샷이 검은색/빈 화면

- Play 모드 확인
- Camera.main 존재 확인
- Game View 창이 열려있는지 확인

### UI 클릭이 동작하지 않음

- EventSystem 존재 확인
- Canvas가 활성화되어 있는지 확인
- Raycast Target이 켜져있는지 확인

---

## 테스트 체크리스트

- [ ] Gateway 연결 상태
- [ ] Unity 세션 등록
- [ ] Console 도구 (getLogs, clear)
- [ ] Scene 도구 (list, getActive, getData, load)
- [ ] GameObject 도구 (find, create, destroy, setActive)
- [ ] Transform 도구 (setPosition, setRotation, setScale)
- [ ] Component 도구 (add, remove, get, set)
- [ ] Application 도구 (getState, play, stop, pause)
- [ ] Debug 도구 (log, screenshot, hierarchy)
- [ ] Editor 도구 (refresh, recompile, focusWindow, listWindows)
- [ ] Input 도구 (keyPress, mouseClick, clickUI, type)
- [ ] Play Mode 전환 시 연결 유지
