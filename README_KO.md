# 🦞 OpenClaw Unity Plugin

Unity를 [OpenClaw](https://github.com/openclaw/openclaw) AI 어시스턴트와 HTTP로 연결합니다. **Play 모드 없이 Editor 모드에서도 작동**합니다!

[![Unity](https://img.shields.io/badge/Unity-2021.3+-black?logo=unity)](https://unity.com)
[![OpenClaw](https://img.shields.io/badge/OpenClaw-2026.2.3+-green)](https://github.com/openclaw/openclaw)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

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

## 사용 가능한 도구 (총 50개)

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
| `script.execute` | 간단한 명령 실행 |
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
