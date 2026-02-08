# OpenClaw Unity Plugin 문서

> **버전:** 1.3.0 | **도구:** 50개 | **Unity:** 2021.3+

## 개요

OpenClaw Unity Plugin은 Unity 프로젝트를 HTTP를 통해 OpenClaw AI 어시스턴트에 연결합니다. 기존 Unity 도구와 달리 **Play 모드 진입 없이 Editor 모드에서 작동**하여 자연어로 게임 개발 워크플로우를 제어할 수 있습니다.

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
│  │  • SessionState → Play 모드 전환 시 유지               │ │
│  └──────────────────────┬─────────────────────────────────┘ │
│                         │                                    │
│                         ▼                                    │
│  ┌────────────────────────────────────────────────────────┐ │
│  │         OpenClawConnectionManager                       │ │
│  │         (싱글톤 - 모드 간 공유)                        │ │
│  │                                                          │ │
│  │  • 명령 HTTP 폴링                                       │ │
│  │  • 메인 스레드 실행 큐                                  │ │
│  │  • 자동 재연결                                          │ │
│  │  • 중첩 객체 지원 JSON 파싱                            │ │
│  └──────────────────────┬─────────────────────────────────┘ │
│                         │                                    │
│                         ▼                                    │
│  ┌────────────────────────────────────────────────────────┐ │
│  │           OpenClawTools (50개 도구)                     │ │
│  │                                                          │ │
│  │  • Scene/GameObject/Component 조작                      │ │
│  │  • 디버그 도구 (스크린샷, 계층 구조)                   │ │
│  │  • 입력 시뮬레이션 (키보드, 마우스, UI)                │ │
│  │  • 에디터 제어 (리컴파일, 리프레시)                    │ │
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

## 핵심 컴포넌트

### OpenClawEditorBridge
- **위치:** `Editor/OpenClawEditorBridge.cs`
- **속성:** `[InitializeOnLoad]`
- **역할:** Editor 모드에서 연결 라이프사이클 관리
- **기능:**
  - 2초 지연 초기화 (UPM EPIPE 크래시 방지)
  - `SessionState`를 통한 Play 모드 전환 시 유지
  - 폴링을 위한 `EditorApplication.update` 등록

### OpenClawConnectionManager
- **위치:** `Runtime/OpenClawConnectionManager.cs`
- **역할:** OpenClaw Gateway와 HTTP 통신
- **기능:**
  - 싱글톤 패턴 (Editor/Play 모드 간 공유)
  - 자동 재연결 기능의 명령 폴링
  - Unity API 안전성을 위한 메인 스레드 실행 큐
  - 하트비트 (30초 간격)

### OpenClawTools
- **위치:** `Runtime/OpenClawTools.cs`
- **역할:** 도구 구현 (50개 도구)
- **카테고리:**
  - Console (3): getLogs, getErrors, clear
  - Scene (5): list, getActive, getData, load, open
  - GameObject (7): find, create, destroy, delete, getData, setActive, setParent
  - Transform (6): getPosition, getRotation, getScale, setPosition, setRotation, setScale
  - Component (5): add, remove, get, set, list
  - Script (3): execute, read, list
  - Application (4): getState, play, pause, stop
  - Debug (3): log, screenshot, hierarchy
  - Editor (4): refresh, recompile, focusWindow, listWindows
  - Input (10): keyPress, keyDown, keyUp, type, mouseMove, mouseClick, mouseDrag, mouseScroll, getMousePosition, clickUI

### OpenClawConfig
- **위치:** `Runtime/OpenClawConfig.cs`
- **역할:** 프로젝트 설정용 ScriptableObject
- **생성:** `Assets > Create > OpenClaw > Config`
- **설정:**
  - `gatewayUrl` - Gateway URL (기본값: `http://localhost:18789`)
  - `autoConnect` - 시작 시 연결 (기본값: `true`)
  - `captureConsoleLogs` - AI용 로그 캡처 (기본값: `true`)
  - 보안 토글: `allowCodeExecution`, `allowFileAccess`, `allowSceneModification`

### OpenClawLogger
- **위치:** `Runtime/OpenClawLogger.cs`
- **역할:** AI 디버깅을 위한 Unity 콘솔 로그 캡처

## 핵심 패턴

### SafeDestroy()
크로스 모드 오브젝트 삭제:
```csharp
private static void SafeDestroy(UnityEngine.Object obj)
{
    if (Application.isPlaying)
        UnityEngine.Object.Destroy(obj);
    else
        UnityEngine.Object.DestroyImmediate(obj);
}
```

### 도구 별칭
동일 작업에 대한 복수 이름:
- `gameobject.delete` → `gameobject.destroy`

### 컴포넌트 리플렉션
컴파일 타임 의존성 없이 동적 컴포넌트 속성 접근 (예: TextMeshPro 지원).

## 설치 요약

1. **Gateway 확장:** `OpenClawPlugin~/`을 `~/.openclaw/extensions/unity/`로 복사
2. **Unity 패키지:** Git URL 또는 로컬 디스크로 추가
3. **스킬 (선택):** `~/.openclaw/workspace/skills/unity-plugin/`에 클론

## 문서 목록

| 문서 | 설명 |
|------|------|
| [README.md](../README.md) | 전체 기능 목록 및 사용 예시 |
| [DEVELOPMENT.md](DEVELOPMENT.md) | 아키텍처 상세, 도구 확장 |
| [DEVELOPMENT_KO.md](DEVELOPMENT_KO.md) | 개발 가이드 (한국어) |
| [TESTING.md](TESTING.md) | 수동 테스트 절차 |
| [TESTING_KO.md](TESTING_KO.md) | 테스트 가이드 (한국어) |
| [CHANGELOG.md](../CHANGELOG.md) | 버전 히스토리 |

## 빠른 참조

### Gateway 엔드포인트
| 엔드포인트 | 메서드 | 용도 |
|------------|--------|------|
| `/unity/register` | POST | Unity 세션 등록 |
| `/unity/heartbeat` | POST | 세션 유지 |
| `/unity/poll` | GET | 명령 폴링 |
| `/unity/result` | POST | 실행 결과 전송 |

### 카테고리별 도구 수
| 카테고리 | 개수 |
|----------|------|
| Console | 3 |
| Scene | 5 |
| GameObject | 7 |
| Transform | 6 |
| Component | 5 |
| Script | 3 |
| Application | 4 |
| Debug | 3 |
| Editor | 4 |
| Input | 10 |
| **합계** | **50** |

---

*마지막 업데이트: 2026-02-08 | OpenClaw Unity Plugin v1.3.0*
