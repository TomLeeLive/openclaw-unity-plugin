# OpenClaw Unity Plugin Documentation

> **Version:** 1.2.3 | **Tools:** 50 | **Unity:** 2021.3+

## Overview

OpenClaw Unity Plugin connects your Unity project to the OpenClaw AI assistant via HTTP. Unlike traditional Unity tools, it works **in Editor mode without hitting Play** - enabling natural language control of your game development workflow.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Unity Editor                             │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │           OpenClawEditorBridge                          │ │
│  │           [InitializeOnLoad]                            │ │
│  │                                                          │ │
│  │  • EditorApplication.delayCall → safe init              │ │
│  │  • EditorApplication.update → connection polling        │ │
│  │  • SessionState → survives Play mode transitions        │ │
│  └──────────────────────┬─────────────────────────────────┘ │
│                         │                                    │
│                         ▼                                    │
│  ┌────────────────────────────────────────────────────────┐ │
│  │         OpenClawConnectionManager                       │ │
│  │         (Singleton - shared across modes)               │ │
│  │                                                          │ │
│  │  • HTTP polling for commands                            │ │
│  │  • Main thread execution queue                          │ │
│  │  • Automatic reconnection                               │ │
│  │  • JSON parsing with nested object support              │ │
│  └──────────────────────┬─────────────────────────────────┘ │
│                         │                                    │
│                         ▼                                    │
│  ┌────────────────────────────────────────────────────────┐ │
│  │           OpenClawTools (50 tools)                      │ │
│  │                                                          │ │
│  │  • Scene/GameObject/Component manipulation              │ │
│  │  • Debug tools (screenshot, hierarchy)                  │ │
│  │  • Input simulation (keyboard, mouse, UI)               │ │
│  │  • Editor control (recompile, refresh)                  │ │
│  └────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
                              │
                              │ HTTP
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                   OpenClaw Gateway                            │
│                   http://localhost:18789                      │
│                                                               │
│  Endpoints:                                                   │
│  • POST /unity/register  - Register Unity session             │
│  • POST /unity/heartbeat - Keep session alive                 │
│  • GET  /unity/poll      - Poll for commands                  │
│  • POST /unity/result    - Send tool execution results        │
└──────────────────────────────────────────────────────────────┘
```

## Core Components

### OpenClawEditorBridge
- **Location:** `Editor/OpenClawEditorBridge.cs`
- **Attribute:** `[InitializeOnLoad]`
- **Role:** Manages connection lifecycle in Editor mode
- **Features:**
  - 2-second delayed initialization (prevents UPM EPIPE crashes)
  - Survives Play mode transitions via `SessionState`
  - Registers `EditorApplication.update` for polling

### OpenClawConnectionManager
- **Location:** `Runtime/OpenClawConnectionManager.cs`
- **Role:** HTTP communication with OpenClaw Gateway
- **Features:**
  - Singleton pattern (shared across Editor/Play modes)
  - Command polling with automatic reconnection
  - Main thread execution queue for Unity API safety
  - Heartbeat (30-second interval)

### OpenClawTools
- **Location:** `Runtime/OpenClawTools.cs`
- **Role:** Tool implementations (50 tools)
- **Categories:**
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
- **Location:** `Runtime/OpenClawConfig.cs`
- **Role:** ScriptableObject for project settings
- **Create:** `Assets > Create > OpenClaw > Config`
- **Settings:**
  - `gatewayUrl` - Gateway URL (default: `http://localhost:18789`)
  - `autoConnect` - Connect on start (default: `true`)
  - `captureConsoleLogs` - Capture logs for AI (default: `true`)
  - Security toggles: `allowCodeExecution`, `allowFileAccess`, `allowSceneModification`

### OpenClawLogger
- **Location:** `Runtime/OpenClawLogger.cs`
- **Role:** Captures Unity console logs for AI debugging

## Key Patterns

### SafeDestroy()
Cross-mode object destruction:
```csharp
private static void SafeDestroy(UnityEngine.Object obj)
{
    if (Application.isPlaying)
        UnityEngine.Object.Destroy(obj);
    else
        UnityEngine.Object.DestroyImmediate(obj);
}
```

### Tool Aliases
Multiple names for the same operation:
- `gameobject.delete` → `gameobject.destroy`

### Reflection for Components
Dynamic component property access without compile-time dependencies (e.g., TextMeshPro support).

## Installation Summary

1. **Gateway Extension:** Copy `OpenClawPlugin~/` to `~/.openclaw/extensions/unity/`
2. **Unity Package:** Add via Git URL or local disk
3. **Skill (Optional):** Clone to `~/.openclaw/workspace/skills/unity-plugin/`

## Documentation Index

| Document | Description |
|----------|-------------|
| [README.md](../README.md) | Full feature list and usage examples |
| [DEVELOPMENT.md](DEVELOPMENT.md) | Architecture details, extending tools |
| [TESTING.md](TESTING.md) | Manual testing procedures |
| [CHANGELOG.md](../CHANGELOG.md) | Version history |

## Quick Reference

### Gateway Endpoints
| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/unity/register` | POST | Register Unity session |
| `/unity/heartbeat` | POST | Keep session alive |
| `/unity/poll` | GET | Poll for commands |
| `/unity/result` | POST | Send execution results |

### Tool Count by Category
| Category | Count |
|----------|-------|
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
| **Total** | **50** |

---

*Last updated: 2026-02-08 | OpenClaw Unity Plugin v1.2.3*
