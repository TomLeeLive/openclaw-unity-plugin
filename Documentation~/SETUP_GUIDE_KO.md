# OpenClaw Unity Plugin - 셋업 가이드

다양한 사용 사례에 맞게 OpenClaw Unity Plugin을 설정하는 방법을 설명합니다.

## 🤔 어떤 모드가 필요한가요?

설정하기 전에 어떤 모드가 적합한지 확인하세요:

| AI 사용 방식 | 필요한 모드 | 이유 |
|-------------|------------|------|
| **채팅 앱** (Telegram, Discord) | 모드 A: Gateway | OpenClaw가 Unity로 명령 전달 |
| **Claude Code** 터미널에서 | 모드 B: MCP | 직접 연결 필요 |
| **둘 다** | 하이브리드 | 모든 상황 대응 |

### 이미 OpenClaw를 사용 중이라면...

OpenClaw를 통해 AI 어시스턴트와 채팅 중이라면 (Telegram, Discord 등), **MCP 설정이 필요 없습니다** - 어시스턴트가 이미 Gateway를 통해 Unity 도구에 접근 가능!

```
나 (Telegram) → OpenClaw Gateway → AI 어시스턴트 → unity_execute 도구 → Unity
                                   ↑
                            이미 접근 가능!
```

### MCP가 유용한 경우

MCP가 필요한 경우:
1. **Claude Code를 터미널에서 직접 사용** (OpenClaw 거치지 않고)
2. **Claude Desktop** 앱 사용
3. **Cursor** 또는 기타 MCP 호환 에디터 사용
4. **OpenClaw에서 Claude Code를 sub-agent로 spawn**하여 코딩 + 테스트 워크플로우

```
# MCP 없이:
$ claude
> Unity 제어해줘  →  ❌ 도구 없음

# MCP 설정 후:
$ claude  
> Unity 제어해줘  →  ✅ unity.* 도구 사용 가능
```

## 🅰️ 모드 A: OpenClaw Gateway (원격 접속)

**언제 사용?** Telegram, Discord, 웹에서 원격으로 게임 개발하고 싶을 때

### 설정 단계

```bash
# 1. OpenClaw 설치
npm install -g openclaw

# 2. Gateway 시작
openclaw gateway start

# 3. Unity 플러그인 설치
#    옵션 A: Package Manager > Add from git URL
#    https://github.com/TomLeeLive/openclaw-unity-plugin.git
#    
#    옵션 B: Clone 후 디스크에서 추가

# 4. Unity 프로젝트 열기
#    플러그인이 자동으로 Gateway에 연결됨

# 5. 채팅 연동 설정 (선택)
openclaw config
```

### 작동 방식

```
┌──────────────┐      ┌─────────────────┐      ┌──────────────┐
│  Telegram/   │ ───→ │    OpenClaw     │ ───→ │    Unity     │
│  Discord/Web │ ←─── │    Gateway      │ ←─── │    Editor    │
└──────────────┘      └─────────────────┘      └──────────────┘
      폰                  내 컴퓨터               내 컴퓨터
```

### 사용 예시

카페에서 폰으로:
```
나: "Player 오브젝트 위치 알려줘"
AI: Player 위치는 (10.5, 0, -3.2) 입니다

나: "원점으로 이동해"
AI: 완료. Player를 (0, 0, 0)으로 이동했습니다

나: "스크린샷 찍어"
AI: [스크린샷 첨부]
```

---

## 🅱️ 모드 B: MCP 직접 연결 (로컬 개발)

**언제 사용?** Claude Code, Claude Desktop, Cursor에서 Unity를 직접 제어하고 싶을 때

### 설정 단계

```bash
# 1. Unity 플러그인 설치 (위와 동일)

# 2. MCP 서버 의존성 설치
cd /path/to/openclaw-unity-plugin/MCP~
npm install

# 3. Claude Code에 MCP 서버 등록
claude mcp add unity -- node /full/path/to/openclaw-unity-plugin/MCP~/index.js

# 또는 Claude Desktop의 경우 claude_desktop_config.json 수정:
{
  "mcpServers": {
    "unity": {
      "command": "node",
      "args": ["/full/path/to/openclaw-unity-plugin/MCP~/index.js"]
    }
  }
}

# 4. Unity에서 MCP Bridge 시작
#    Window > OpenClaw > Start MCP Bridge

# 5. Claude Code 사용
claude
```

### 작동 방식

```
┌──────────────┐      ┌─────────────────┐      ┌──────────────┐
│ Claude Code  │ ───→ │   MCP Server    │ ───→ │    Unity     │
│ 또는 Desktop │ ←─── │   (Node.js)     │ ←─── │    Editor    │
└──────────────┘      └─────────────────┘      └──────────────┘
     터미널            localhost:27182         localhost:27182
```

### 사용 예시

터미널에서:
```
$ claude
> 현재 씬에 있는 GameObject 목록 알려줘
AI: 15개 GameObject 발견: Main Camera, Directional Light, Player, ...

> (5, 1, 0) 위치에 Cube 생성해
AI: (5, 1, 0)에 Cube 생성 완료

> Play 모드 진입하고 W키 눌러
AI: Play 모드 진입, W키 입력 시뮬레이션 중
```

---

## 🔀 하이브리드 모드 (둘 다 사용)

두 모드를 동시에 사용해도 충돌 없습니다.

```
집에서:   Claude Code → MCP → Unity (로컬, 빠름)
밖에서:   Telegram → OpenClaw Gateway → Unity (원격)
```

### 포트 설정

| 서비스 | 기본 포트 |
|--------|----------|
| MCP Bridge | 27182 |
| OpenClaw Gateway | 18789 |

### 설정 파일

Unity에서 `OpenClawConfig` 에셋 생성 (Assets > Create > OpenClaw > Config):

```
Gateway URL: http://localhost:18789
Enable MCP Bridge: ✓
MCP Bridge Port: 27182
```

---

## 문제 해결

### MCP Bridge 연결 안 됨

1. Unity Console에서 `[OpenClaw MCP]` 메시지 확인
2. 포트 27182 사용 중인지 확인: `lsof -i :27182`
3. 재시작: Window > OpenClaw > Stop MCP Bridge 후 Start

### Gateway 연결 안 됨

1. Gateway 상태 확인: `openclaw gateway status`
2. 포트 18789 접근 가능한지 확인
3. Unity Console에서 `[OpenClaw]` 메시지 확인

### Claude Code에서 도구 안 보임

1. MCP 서버 등록 확인: `claude mcp list`
2. MCP 서버 경로 정확한지 확인 (절대 경로 사용)
3. MCP 서버 추가 후 Claude Code 재시작

---

## 빠른 참조

| 작업 | 명령어 |
|------|--------|
| Gateway 시작 | `openclaw gateway start` |
| Gateway 중지 | `openclaw gateway stop` |
| Gateway 확인 | `openclaw gateway status` |
| MCP 추가 | `claude mcp add unity -- node /path/to/MCP~/index.js` |
| MCP Bridge 시작 | Window > OpenClaw > Start MCP Bridge |
| MCP 상태 확인 | Window > OpenClaw > MCP Bridge Status |
