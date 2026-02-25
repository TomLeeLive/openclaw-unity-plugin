# 🦞 OpenClaw Unity Plugin

> **TL;DR:** Vibe-code your game development remotely from anywhere! 🌍
> 
> **한줄요약:** 이제 집밖에서도 원격으로 바이브코딩으로 게임 개발 가능합니다! 🎮

Connect Unity to [OpenClaw](https://github.com/openclaw/openclaw) AI assistant via HTTP. Works in **Editor mode** without hitting Play!

[![Unity](https://img.shields.io/badge/Unity-2021.3+-black?logo=unity)](https://unity.com)
[![OpenClaw](https://img.shields.io/badge/OpenClaw-2026.2.3+-green)](https://github.com/openclaw/openclaw)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-support-yellow?logo=buy-me-a-coffee)](https://buymeacoffee.com/tomleelive)

## ⚠️ Disclaimer

This software is in **beta**. Use at your own risk.

- Always backup your project before using
- Test in a separate project first
- The authors are not responsible for any data loss or project corruption

See [LICENSE](LICENSE) for full terms.

## 🔀 Hybrid Architecture

This plugin supports **two connection modes** - use whichever fits your workflow:

### Mode A: OpenClaw Gateway (Remote Access)
```
Telegram/Discord/Web → OpenClaw Gateway → Unity Plugin
```
- ✅ Remote access from anywhere
- ✅ Chat integration (Telegram, Discord, etc.)
- ✅ Cron jobs, automation, multi-device
- ⚠️ Requires OpenClaw Gateway running

### Mode B: MCP Direct (Local Development)
```
Claude Code/Desktop → MCP Server → Unity Plugin
```
- ✅ Direct connection, no middleware
- ✅ Works with Claude Code, Cursor, etc.
- ✅ Lower latency for local development
- ⚠️ Local only (127.0.0.1)

### Quick Setup

| Mode | Setup |
|------|-------|
| **OpenClaw** | Just install plugin, Gateway handles connection |
| **MCP** | Enable MCP Bridge: `Window > OpenClaw > Start MCP Bridge` |

📖 **[Setup Guide](Documentation~/SETUP_GUIDE.md)** | **[셋업 가이드](Documentation~/SETUP_GUIDE_KO.md)**

## ✨ Key Features

- 🎮 **Works in Editor & Play Mode** - No need to hit Play to use AI tools
- 🔌 **Auto-Connect** - Connects when Unity starts, maintains connection across mode changes
- 📋 **Console Integration** - Capture and query Unity logs for debugging
- 🎬 **Scene Management** - List, load, and inspect scenes
- 🔧 **Component Editing** - Add, remove, and modify component properties
- 📸 **Debug Tools** - Screenshots, hierarchy view, and more
- 🎯 **Input Simulation** - Keyboard, mouse, and UI interaction for game testing
- 🔄 **Editor Control** - Trigger recompilation and asset refresh remotely
- 🔒 **Security Controls** - Configure what operations are allowed

## Requirements

| Component | Version |
|-----------|---------|
| **Unity** | 2021.3+ |
| **OpenClaw** | 2026.2.3+ |

> ⚠️ **Tested on Unity 6000.3 (Unity 6.3 LTS) only.**
> 
> While designed for Unity 2021.3+, this plugin has only been tested on Unity 6000.3.7f1 (Unity 6.3 LTS).
> If you encounter issues on other Unity versions, please:
> - 🐛 [Open an Issue](https://github.com/TomLeeLive/openclaw-unity-plugin/issues) with your Unity version and error details
> - 🔧 [Submit a Pull Request](https://github.com/TomLeeLive/openclaw-unity-plugin/pulls) if you have a fix
> 
> Your contributions help make this plugin work across all Unity versions!

## Installation

### Option 1: Git URL (Recommended)

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click `+` → `Add package from git URL...`
3. Enter:
   ```
   https://github.com/TomLeeLive/openclaw-unity-plugin.git
   ```

### Option 2: Local Package

1. Clone this repository
2. In Unity: `Window > Package Manager` → `+` → `Add package from disk...`
3. Select the `package.json` file

## Quick Start

### 1. Install OpenClaw Gateway Extension (Required)

Copy the gateway extension files to OpenClaw:

```bash
# Copy extension files
cp -r OpenClawPlugin~/* ~/.openclaw/extensions/unity/

# Restart gateway to load the extension
openclaw gateway restart

# Verify
openclaw unity status
```

> **Note:** `OpenClawPlugin~` contains the gateway extension that enables `unity_execute` and `unity_sessions` tools. This is required for OpenClaw to communicate with Unity.

### 2. Install Unity Package

See [Installation](#installation) above for Git URL or local package setup.

### 3. Configure in Unity

1. Open `Window > OpenClaw Plugin`
2. Set Gateway URL: `http://localhost:18789` (default)
3. Connection is automatic when Unity starts
4. Status shows green when connected

### 4. Chat with OpenClaw

Ask OpenClaw to inspect your scene, create objects, or debug issues - all without entering Play mode!

### 5. Install OpenClaw Skill (Optional)

The companion skill provides workflow patterns and tool references for the AI:

```bash
# Clone skill to OpenClaw workspace
git clone https://github.com/TomLeeLive/openclaw-unity-skill.git ~/.openclaw/workspace/skills/unity-plugin
```

The skill provides:
- Quick reference for all 55 tools
- Common workflow patterns (scene inspection, UI testing, etc.)
- Detailed parameter documentation
- Troubleshooting guides

> **Note:** The skill is separate from the gateway extension. The extension enables the tools; the skill teaches the AI how to use them effectively.

## 📚 Documentation

- **[Development Guide](Documentation~/DEVELOPMENT.md)** - Architecture, extending tools, and contribution guidelines
- **[Testing Guide](Documentation~/TESTING.md)** - Complete testing guide with examples

## Available Tools (~100 total)

### Console (3 tools)
| Tool | Description |
|------|-------------|
| `console.getLogs` | Get Unity console logs (with type filter) |
| `console.getErrors` | Get error/exception logs (with optional warnings) |
| `console.clear` | Clear captured logs |

### Scene (7 tools)
| Tool | Description |
|------|-------------|
| `scene.list` | List all scenes in build settings |
| `scene.getActive` | Get active scene info |
| `scene.getData` | Get scene hierarchy data |
| `scene.load` | Load a scene by name (Play mode) |
| `scene.open` | Open a scene in Editor mode |
| `scene.save` | Save active scene (Editor mode) |
| `scene.saveAll` | Save all open scenes (Editor mode) |

### GameObject (8 tools)
| Tool | Description |
|------|-------------|
| `gameobject.find` | Find by name, tag, or component type |
| `gameobject.getAll` | Get all GameObjects with filtering |
| `gameobject.create` | Create GameObject or primitive |
| `gameobject.destroy` | Destroy a GameObject |
| `gameobject.delete` | Delete a GameObject (alias for destroy) |
| `gameobject.getData` | Get detailed object data |
| `gameobject.setActive` | Enable/disable object |
| `gameobject.setParent` | Change parent |

### Transform (6 tools)
| Tool | Description |
|------|-------------|
| `transform.getPosition` | Get world position (x, y, z) |
| `transform.getRotation` | Get rotation in Euler angles |
| `transform.getScale` | Get local scale |
| `transform.setPosition` | Set world position |
| `transform.setRotation` | Set rotation (Euler) |
| `transform.setScale` | Set local scale |

### Component
| Tool | Description |
|------|-------------|
| `component.add` | Add component to object |
| `component.remove` | Remove component |
| `component.get` | Get component data |
| `component.set` | Set field/property value |
| `component.list` | List available types |

### Script (3 tools)
| Tool | Description |
|------|-------------|
| `script.execute` | Execute code/methods (Debug.Log, Time.timeScale, PlayerPrefs, reflection calls) |
| `script.read` | Read script file contents |
| `script.list` | List script files in project |

### Application
| Tool | Description |
|------|-------------|
| `app.getState` | Get play mode, FPS, etc. |
| `app.play` | Enter play mode (Editor) |
| `app.pause` | Toggle pause (Editor) |
| `app.stop` | Exit play mode (Editor) |

### Debug
| Tool | Description |
|------|-------------|
| `debug.log` | Write to console |
| `debug.screenshot` | Capture screenshot (with UI) |
| `debug.hierarchy` | Text hierarchy view |

### Editor (5 tools)
| Tool | Description |
|------|-------------|
| `editor.refresh` | Refresh AssetDatabase (triggers recompile) |
| `editor.recompile` | Request script recompilation |
| `editor.domainReload` | Force domain reload (reinitializes static fields) |
| `editor.focusWindow` | Focus Editor window (game/scene/console/hierarchy/project/inspector) |
| `editor.listWindows` | List all open Editor windows |

### Input Simulation (NEW in v1.2.0)
| Tool | Description |
|------|-------------|
| `input.keyPress` | Press and release a key |
| `input.keyDown` | Press and hold a key |
| `input.keyUp` | Release a key |
| `input.type` | Type text into input field |
| `input.mouseMove` | Move mouse cursor |
| `input.mouseClick` | Click at position |
| `input.mouseDrag` | Drag from A to B |
| `input.mouseScroll` | Scroll wheel |
| `input.getMousePosition` | Get current cursor position |
| `input.clickUI` | Click UI element by name |

> ⚠️ **Input Simulation Limitation**: Keyboard/mouse simulation works for **UI interactions** (Button clicks, InputField typing) but NOT for gameplay input using `Input.GetKey()` or legacy Input Manager. This is a Unity limitation - `Input.GetKey()` reads directly from the OS input buffer. For automated gameplay testing, use `transform.setPosition` to move objects directly, or migrate to Unity's **new Input System** which supports programmatic input injection.

### Material (5 tools) - NEW in v1.5.0
| Tool | Description |
|------|-------------|
| `material.create` | Create material with shader, color, metallic, smoothness |
| `material.assign` | Assign material to GameObject |
| `material.modify` | Modify material properties (color, metallic, emission, etc.) |
| `material.getInfo` | Get detailed material info with all shader properties |
| `material.list` | List materials in project with filtering |

### Prefab (5 tools) - NEW in v1.5.0
| Tool | Description |
|------|-------------|
| `prefab.create` | Create prefab from scene GameObject |
| `prefab.instantiate` | Instantiate prefab in scene with position |
| `prefab.open` | Open prefab for editing |
| `prefab.close` | Close prefab editing mode |
| `prefab.save` | Save currently edited prefab |

### Asset (7 tools) - NEW in v1.5.0
| Tool | Description |
|------|-------------|
| `asset.find` | Search assets by query, type, folder |
| `asset.copy` | Copy asset to new path |
| `asset.move` | Move/rename asset |
| `asset.delete` | Delete asset (with trash option) |
| `asset.refresh` | Refresh AssetDatabase |
| `asset.import` | Import/reimport specific asset |
| `asset.getPath` | Get asset path by name |

### Package Manager (4 tools) - NEW in v1.5.0
| Tool | Description |
|------|-------------|
| `package.add` | Install package by name or git URL |
| `package.remove` | Remove installed package |
| `package.list` | List installed packages |
| `package.search` | Search Unity package registry |

### Test Runner (3 tools) - NEW in v1.5.0
| Tool | Description |
|------|-------------|
| `test.run` | Run EditMode/PlayMode tests with filtering |
| `test.list` | List available tests |
| `test.getResults` | Get last test run results |

### Batch Execution (1 tool) - NEW in v1.6.0
| Tool | Description |
|------|-------------|
| `batch.execute` | Execute multiple tools in one call (10-100x performance) |

**Example:**
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

### Session Info (1 tool) - NEW in v1.6.0
| Tool | Description |
|------|-------------|
| `session.getInfo` | Get session info (project, processId, machineNa) for multi-instance support |

### ScriptableObject (6 tools) - NEW in v1.6.0
| Tool | Description |
|------|-------------|
| `scriptableobject.create` | Create new ScriptableObject asset |
| `scriptableobject.load` | Load and inspect ScriptableObject fields |
| `scriptableobject.save` | Save ScriptableObject changes |
| `scriptableobject.getField` | Get specific field value |
| `scriptableobject.setField` | Set field value with auto-save |
| `scriptableobject.list` | List ScriptableObjects in project |

### Shader (3 tools) - NEW in v1.6.0
| Tool | Description |
|------|-------------|
| `shader.list` | List shaders in project |
| `shader.getInfo` | Get shader properties and info |
| `shader.getKeywords` | Get shader keywords |

### Texture (5 tools) - NEW in v1.6.0
| Tool | Description |
|------|-------------|
| `texture.create` | Create new texture with color fill |
| `texture.getInfo` | Get texture info (size, format, import settings) |
| `texture.setPixels` | Fill region with color |
| `texture.resize` | Resize texture via import settings |
| `texture.list` | List textures in project |

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
│  │           OpenClawTools (~80 tools)                     │ │
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

## Configuration

Create via `Assets > Create > OpenClaw > Config` and place in `Resources` folder.

| Setting | Description | Default |
|---------|-------------|---------|
| `gatewayUrl` | OpenClaw gateway URL | `http://localhost:18789` |
| `apiToken` | Optional API token | (empty) |
| `autoConnect` | Connect on start | `true` |
| `showStatusOverlay` | Show status in Game view | `true` |
| `captureConsoleLogs` | Capture logs for AI | `true` |
| `allowCodeExecution` | Allow code execution | `true` |
| `allowFileAccess` | Allow file operations | `true` |
| `allowSceneModification` | Allow scene changes | `true` |

## Example Usage

### Scene Inspection
```
You: What GameObjects are in my scene?

OpenClaw: [Executes debug.hierarchy]

Your scene has:
▶ Main Camera [Camera, AudioListener]
▶ Directional Light [Light]
▶ Player [PlayerController, Rigidbody]
  ▶ Model [MeshRenderer]
▶ UI Canvas [Canvas, GraphicRaycaster]
  ▶ PlayButton [Button]
```

### Game Testing with Input Simulation
```
You: Test the login flow - enter username "TestPlayer" and click Play

OpenClaw: 
[Executes input.clickUI {name: "UsernameInput"}]
[Executes input.type {text: "TestPlayer"}]
[Executes input.clickUI {name: "PlayButton"}]
[Executes debug.screenshot]

Done! Clicked username input, typed "TestPlayer", and clicked Play button.
Screenshot attached showing the result.
```

### Remote Recompilation
```
You: I updated the PlayerController script, recompile Unity

OpenClaw: [Executes editor.recompile]

Script recompilation requested. Unity will reload shortly.
```

## Troubleshooting

### Bridge won't connect
1. Check Gateway status: `openclaw gateway status`
2. Verify URL: default is `http://localhost:18789`
3. Check `Window > OpenClaw Plugin` for errors

### Connection lost during Play mode transition
- Plugin uses `SessionState` to survive domain reloads
- Auto-reconnects after Play mode transition
- If stuck, use `editor.refresh` or click "Force Reconnect"

### Screenshot shows wrong content
- In Play mode: Uses `ScreenCapture` (includes UI)
- In Editor mode: Uses `Camera.main.Render()` (no overlay UI)
- Use Play mode for accurate game screenshots

### Screen Lock / Remote Access
When using `debug.screenshot` remotely (via SSH, screen sharing, etc.):

| Game View Mode | Screen Lock | Screenshot Works? |
|----------------|-------------|-------------------|
| **Play Focused** | ✅ OK | ✅ Yes |
| Normal | ❌ May fail | ⚠️ Depends |

**Best Practice:** Set Game View to **"Play Focused"** mode before locking the screen. This ensures Unity keeps rendering the game even when the window isn't visible.

To enable: Click the dropdown next to "Scale" in Game View → Select "Play Focused"

### Script changes not applied after Play mode restart
Unity's "Enter Play Mode Settings" can skip domain reload for faster iteration, but this prevents script recompilation.

**Symptoms:**
- Code changes don't take effect when re-entering Play mode
- Old behavior persists despite saving scripts
- `editor.refresh` or `editor.recompile` has no effect during Play mode

**Solution:**
1. Go to `Edit → Project Settings → Editor`
2. Find "Enter Play Mode Settings"
3. Check ✅ **"Reload Domain"**

**What this does:**
| Setting | Reload Domain ON | Reload Domain OFF |
|---------|------------------|-------------------|
| Script changes | ✅ Applied on Play | ❌ Ignored until manual refresh |
| Play mode entry | ~2-5 seconds | ~0.5 seconds |
| Static variables | Reset | Preserved |
| Best for | Development with active coding | Testing/playing without code changes |

**Tip:** Keep "Reload Domain" ON during development. Only disable it when you need fast iteration without code changes.

## 🔐 Security: Model Invocation Setting

When publishing to ClawHub or installing as a skill, you can configure `disableModelInvocation` in the skill metadata:

| Setting | AI Auto-Invoke | User Explicit Request |
|---------|---------------|----------------------|
| `false` (default) | ✅ Allowed | ✅ Allowed |
| `true` | ❌ Blocked | ✅ Allowed |

### Recommendation for Unity Plugin: **`true`**

**Reason:** During Unity development, it's useful for AI to autonomously perform supporting tasks like checking scene hierarchy, taking screenshots, and inspecting GameObjects.

**When to use `true`:** For sensitive tools (payments, deletions, message sending, etc.)

```yaml
# Example skill metadata
metadata:
  openclaw:
    disableModelInvocation: true  # Recommended for Unity plugin
```

## ⚠️ Important Notes

- **Development Only**: Disable `allowCodeExecution` in production builds
- **TextMeshPro**: Plugin works with or without TMPro (uses reflection)
- **Unity 6**: Deferred initialization prevents UPM EPIPE crashes

## 🎭 AI Personas for Game Development

Want your AI agent to have a consistent personality while developing your game? [ClawSouls](https://clawsouls.ai) provides open-source persona packages (Soul Spec) that work with OpenClaw and other AI frameworks. Give your coding assistant a specialized game dev personality, or create custom NPC behavior profiles.

- Browse community souls: [clawsouls.ai](https://clawsouls.ai)
- Soul Spec standard: [soulspec.org](https://soulspec.org)
- Quick start: `npx clawsouls init my-game-dev --spec 0.4`

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history.

## License

MIT License - See [LICENSE](LICENSE)

---

Made with 🦞 by the OpenClaw community

## License

This project has been licensed under [Apache-2.0](LICENSE) since its initial release.
Copyright 2026 Tom Lee (TomLeeLive)
