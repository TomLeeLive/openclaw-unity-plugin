---
name: unity-plugin
version: 1.7.0
description: Control Unity Editor via OpenClaw Unity Plugin. Use for Unity game development tasks including scene management, GameObject/Component manipulation, debugging, input simulation, and Play mode control — including arbitrary C# execution (script.execute) and reflection-based editor calls, which can modify scenes, assets, and settings. Project-changing and destructive operations (delete, save, package install, code execution, input simulation) are refused at runtime unless the operator starts the gateway with OPENCLAW_EDITOR_ALLOW_DESTRUCTIVE=1 and the call passes confirm: true; read-only inspection needs neither, and dryRun: true previews any call without sending it. Use only in trusted local projects. Triggers on explicit Unity Editor requests like inspecting scenes, creating objects, taking screenshots, testing gameplay, or controlling the Editor.
homepage: https://github.com/TomLeeLive/openclaw-unity-skill
author: Tom Jaejoon Lee
disableModelInvocation: true
---

# Unity Plugin Skill

Control Unity Editor through **~100 built-in tools**. Works in both Editor and Play mode.

## Safety and permissions

**What this skill can change:** anything the Unity Editor can — GameObjects and
components, scenes, prefabs, materials, shaders, textures and ScriptableObjects,
files under `Assets/`, installed packages, Play mode, and arbitrary C# run inside
the Editor process (`script.execute`, plus reflection-based calls).

**What runs without asking:** read-only tools only — `get*`, `list`, `find`,
`script.read`, `debug.hierarchy`, `debug.screenshot`, `console.getLogs`. Their
behaviour is unchanged.

**What is gated at runtime:** every project-changing tool — create, delete,
destroy, save, `set*`, `component.add`/`remove`, `asset.*` writes, `prefab.*`,
`material.*`, `texture.*`, `editor.refresh`/`recompile`, Play-mode control, input
simulation, `test.run`, `script.execute` — and any `batch.execute` that carries
one of them. The gateway extension refuses these by default. A tool name it does
not recognise (project-registered custom tools included) counts as
project-changing unless its verb is clearly read-only: the gate fails closed,
never open.

**How to enable them** — both steps are required:

1. The operator starts the gateway with the opt-in environment variable. It is
   read from the gateway process, so the model cannot set it:

   ```bash
   OPENCLAW_EDITOR_ALLOW_DESTRUCTIVE=1 openclaw gateway restart
   # or, scoped to this skill only:
   OPENCLAW_UNITY_ALLOW_DESTRUCTIVE=1 openclaw gateway restart
   ```

2. The caller passes `confirm: true` on each project-changing call, after the
   user has approved that specific change:

   ```
   unity_execute: asset.delete {path: "Assets/Old/Item.prefab"}, confirm: true
   ```

Missing either one is a refusal with an explanation of what was blocked and how
to allow it. Nothing reaches the Editor in the meantime.

**Preview first:** `dryRun: true` reports how a call is classified and what would
be sent, and sends nothing:

```
unity_execute: gameobject.destroy {name: "Player"}, dryRun: true
→ {risk: "project-changing", requiresConfirmation: true, wouldRun: false, executed: false}
```

**Check the current state:** `openclaw unity status` prints whether
project-changing tools are enabled, and `GET /unity/status` reports the same as
`destructiveOperations: "enabled" | "blocked"`.

**Scope of the gate:** it lives in the gateway extension that ships with this
skill (`extension/index.ts`), so it covers every call routed through the OpenClaw
gateway — Telegram, Discord and the other channels. The Unity Editor add-on's
local MCP bridge (port 27182, Mode 2 below) is a separate install from the plugin
repository and is not gated by this package: keep that port on the local machine
and review the plugin repository for its own controls.

## Connection Modes

### 1. OpenClaw Gateway (Remote)
For Telegram, Discord, and other OpenClaw channels:
- Auto-connects when Unity opens
- Configure in: Window → OpenClaw Plugin → Settings

### 2. MCP Bridge (Local)
For Claude Code, Cursor, and local AI tools:
- Start: Window → OpenClaw Plugin → MCP Bridge → Start
- Default port: 27182
- Add to Claude Code: `claude mcp add unity -- node <path>/MCP~/index.js`

## First-Time Setup

If `unity_execute` tool is not available, install the gateway extension:

```bash
# From skill directory
./scripts/install-extension.sh

# Restart gateway
openclaw gateway restart
```

The extension files are in `extension/` directory.

### What install-extension.sh Does

```bash
# 1. Copies extension files from skill to gateway
#    Source: <skill>/extension/
#    Destination: ~/.openclaw/extensions/unity/

# 2. Files installed:
#    - index.ts     # Extension entry point (HTTP handlers, tools)
#    - package.json # Extension metadata

# After installation, restart gateway to load the extension.
```

## 🔐 Security: Model Invocation Setting

When publishing to ClawHub, `disableModelInvocation` controls who may start the skill:

| Setting | AI Auto-Invoke | User Explicit Request |
|---------|---------------|----------------------|
| `false` (default) | ✅ Allowed | ✅ Allowed |
| `true` | ❌ Blocked | ✅ Allowed |

### Recommendation: **`true`**

**Reason:** this skill can execute arbitrary C# inside the Editor (`script.execute`)
and call Editor APIs by reflection. That is the whole point of it, and it is also
why it should never start on its own inference. As of v1.7.0 this skill ships with
`disableModelInvocation: true` — it runs only on explicit user request, and every
project-changing call is gated on top of that (see
[Safety and permissions](#safety-and-permissions)).

The full capability disclosure — arbitrary code execution, destructive operations,
network surface — is in [Security & Privacy Disclosure](#security--privacy-disclosure)
at the end of this document. **Read it before enabling the skill.**

## Quick Reference

### Core Tools

| Category | Key Tools |
|----------|-----------|
| **Scene** | `scene.getActive`, `scene.getData`, `scene.load`, `scene.open`, `scene.save` |
| **GameObject** | `gameobject.find`, `gameobject.getAll`, `gameobject.create`, `gameobject.destroy` |
| **Component** | `component.get`, `component.set`, `component.add`, `component.remove` |
| **Transform** | `transform.setPosition`, `transform.setRotation`, `transform.setScale` |
| **Debug** | `debug.hierarchy`, `debug.screenshot`, `console.getLogs` |
| **Input** | `input.clickUI`, `input.type`, `input.keyPress`, `input.mouseClick` |
| **Editor** | `editor.getState`, `editor.play`, `editor.stop`, `editor.refresh` |
| **Material** | `material.create`, `material.assign`, `material.modify`, `material.getInfo` |
| **Prefab** | `prefab.create`, `prefab.instantiate`, `prefab.open`, `prefab.save` |
| **Asset** | `asset.find`, `asset.copy`, `asset.move`, `asset.delete` |
| **Package** | `package.add`, `package.remove`, `package.list`, `package.search` |
| **Test** | `test.run`, `test.list`, `test.getResults` |

## Common Workflows

### 1. Scene Inspection

```
unity_execute: debug.hierarchy {depth: 2}
unity_execute: scene.getActive
```

### 2. Find & Modify Objects

```
unity_execute: gameobject.find {name: "Player"}
unity_execute: component.get {name: "Player", componentType: "Transform"}
unity_execute: transform.setPosition {name: "Player", x: 0, y: 5, z: 0}
```

### 3. UI Testing

```
unity_execute: input.clickUI {name: "PlayButton"}
unity_execute: input.type {text: "TestUser", elementName: "UsernameInput"}
unity_execute: debug.screenshot
```

### 4. Play Mode Control

```
unity_execute: editor.play              # Enter Play mode
unity_execute: editor.stop              # Exit Play mode
unity_execute: editor.getState          # Check current state
unity_execute: editor.pause             # Pause
unity_execute: editor.unpause           # Resume
```

### 5. Material Creation

```
unity_execute: material.create {name: "RedMetal", color: "#FF0000", metallic: 0.8}
unity_execute: material.assign {gameObjectName: "Player", materialPath: "Assets/Materials/RedMetal.mat"}
unity_execute: material.modify {path: "Assets/Materials/RedMetal.mat", metallic: 1.0, emission: "#FF4444"}
```

### 6. Prefab Workflow

```
unity_execute: prefab.create {gameObjectName: "Player", path: "Assets/Prefabs/Player.prefab"}
unity_execute: prefab.instantiate {prefabPath: "Assets/Prefabs/Player.prefab", x: 0, y: 1, z: 0}
unity_execute: prefab.open {path: "Assets/Prefabs/Player.prefab"}
unity_execute: prefab.save
unity_execute: prefab.close
```

### 7. Asset Management

```
unity_execute: asset.find {query: "Player", type: "Prefab"}
unity_execute: asset.copy {sourcePath: "Assets/Prefabs/Player.prefab", destPath: "Assets/Backup/Player.prefab"}
unity_execute: asset.move {sourcePath: "Assets/Old/Item.prefab", destPath: "Assets/New/Item.prefab"}
```

### 8. Package Management

```
unity_execute: package.list
unity_execute: package.search {query: "TextMeshPro"}
unity_execute: package.add {packageName: "com.unity.textmeshpro"}
unity_execute: package.add {gitUrl: "https://github.com/example/package.git"}
```

### 9. Test Running

```
unity_execute: test.list {testMode: "EditMode"}
unity_execute: test.run {testMode: "EditMode", filter: "PlayerTests"}
unity_execute: test.getResults
```

### 10. Script Execution (Enhanced)

`script.execute` is project-changing: it needs the operator opt-in and
`confirm: true` on every call (see [Safety and permissions](#safety-and-permissions)).

```
# Debug logging
unity_execute: script.execute {code: "Debug.Log('Hello')"}, confirm: true

# Time manipulation
unity_execute: script.execute {code: "Time.timeScale = 0.5"}

# PlayerPrefs
unity_execute: script.execute {code: "PlayerPrefs.SetInt('score', 100)"}

# Reflection-based method calls
unity_execute: script.execute {code: "MyClass.MyMethod()"}
unity_execute: script.execute {code: "MyClass.MyStaticMethod('param1', 123)"}
```

## Tool Categories (~100 tools)

### Console (3 tools)
- `console.getLogs` - Get logs with optional type filter (Log/Warning/Error)
- `console.getErrors` - Get error/exception logs (with optional warnings)
- `console.clear` - Clear captured logs

### Scene (7 tools)
- `scene.list` - List scenes in build settings
- `scene.getActive` - Get active scene info
- `scene.getData` - Get full hierarchy data
- `scene.load` - Load scene by name (Play mode)
- `scene.open` - Open scene in Editor mode
- `scene.save` - Save active scene (Editor mode)
- `scene.saveAll` - Save all open scenes (Editor mode)

### GameObject (8 tools)
- `gameobject.find` - Find by name, tag, or component
- `gameobject.getAll` - Get all GameObjects with filtering
- `gameobject.create` - Create object or primitive (Cube, Sphere, etc.)
- `gameobject.destroy` - Destroy object
- `gameobject.delete` - Delete object (alias for destroy)
- `gameobject.getData` - Get detailed data
- `gameobject.setActive` - Enable/disable
- `gameobject.setParent` - Change hierarchy

### Transform (6 tools)
- `transform.getPosition` - Get world position {x, y, z}
- `transform.getRotation` - Get Euler rotation {x, y, z}
- `transform.getScale` - Get local scale {x, y, z}
- `transform.setPosition` - Set world position {x, y, z}
- `transform.setRotation` - Set Euler rotation
- `transform.setScale` - Set local scale

### Component (5 tools)
- `component.add` - Add component by type name
- `component.remove` - Remove component
- `component.get` - Get component data/properties
- `component.set` - Set field/property value
- `component.list` - List available component types

### Script (3 tools)
- `script.execute` - Execute code: Debug.Log, Time, PlayerPrefs, **reflection calls**
- `script.read` - Read script file
- `script.list` - List project scripts

### Application (4 tools)
- `app.getState` - Get play mode, FPS, time
- `app.play` - Enter/exit Play mode
- `app.pause` - Toggle pause
- `app.stop` - Stop Play mode

### Debug (3 tools)
- `debug.log` - Write to console
- `debug.screenshot` - Capture screenshot
- `debug.hierarchy` - Text hierarchy view

### Editor (9 tools)
- `editor.refresh` - Refresh AssetDatabase (triggers recompile)
- `editor.recompile` - Request script recompilation
- `editor.domainReload` - Force domain reload
- `editor.focusWindow` - Focus window (game/scene/console/hierarchy/project/inspector)
- `editor.listWindows` - List open windows
- `editor.getState` - Get editor state
- `editor.play` - Enter Play mode
- `editor.stop` - Exit Play mode
- `editor.pause` / `editor.unpause` - Pause control

### Input Simulation (10 tools)
- `input.keyPress` - Press and release key
- `input.keyDown` / `input.keyUp` - Hold/release key
- `input.type` - Type text into field
- `input.mouseMove` - Move cursor
- `input.mouseClick` - Click at position
- `input.mouseDrag` - Drag operation
- `input.mouseScroll` - Scroll wheel
- `input.getMousePosition` - Get cursor position
- `input.clickUI` - Click UI element by name

### Material (5 tools) - NEW in v1.5.0
- `material.create` - Create material with shader, color, metallic, smoothness
- `material.assign` - Assign material to GameObject
- `material.modify` - Modify material properties (color, metallic, emission)
- `material.getInfo` - Get detailed material info with all shader properties
- `material.list` - List materials in project with filtering

### Prefab (5 tools) - NEW in v1.5.0
- `prefab.create` - Create prefab from scene GameObject
- `prefab.instantiate` - Instantiate prefab in scene with position
- `prefab.open` - Open prefab for editing
- `prefab.close` - Close prefab editing mode
- `prefab.save` - Save currently edited prefab

### Asset (7 tools) - NEW in v1.5.0
- `asset.find` - Search assets by query, type, folder
- `asset.copy` - Copy asset to new path
- `asset.move` - Move/rename asset
- `asset.delete` - Delete asset (with trash option)
- `asset.refresh` - Refresh AssetDatabase
- `asset.import` - Import/reimport specific asset
- `asset.getPath` - Get asset path by name

### Package Manager (4 tools) - NEW in v1.5.0
- `package.add` - Install package by name or git URL
- `package.remove` - Remove installed package
- `package.list` - List installed packages
- `package.search` - Search Unity package registry

### Test Runner (3 tools) - NEW in v1.5.0
- `test.run` - Run EditMode/PlayMode tests with filtering
- `test.list` - List available tests
- `test.getResults` - Get last test run results

### Batch Execution (1 tool) - NEW in v1.6.0
- `batch.execute` - Execute multiple tools in one call (10-100x performance)
  - `commands`: Array of {tool, params} objects
  - `stopOnError`: Stop on first error (default: false)

### Session (1 tool) - NEW in v1.6.0
- `session.getInfo` - Get session info (project, processId, machineName, sessionId)

### ScriptableObject (6 tools) - NEW in v1.6.0
- `scriptableobject.create` - Create new ScriptableObject asset
- `scriptableobject.load` - Load and inspect ScriptableObject fields
- `scriptableobject.save` - Save ScriptableObject changes
- `scriptableobject.getField` - Get specific field value
- `scriptableobject.setField` - Set field value with auto-save
- `scriptableobject.list` - List ScriptableObjects in project

### Shader (3 tools) - NEW in v1.6.0
- `shader.list` - List shaders in project
- `shader.getInfo` - Get shader properties and info
- `shader.getKeywords` - Get shader keywords

### Texture (5 tools) - NEW in v1.6.0
- `texture.create` - Create new texture with color fill
- `texture.getInfo` - Get texture info (size, format, import settings)
- `texture.setPixels` - Fill region with color
- `texture.resize` - Resize texture via import settings
- `texture.list` - List textures in project

## Custom Tools API - v1.6.0

Register project-specific tools:

```csharp
OpenClawCustomTools.Register(
    "mygame.getScore",
    "Get current score",
    (args) => new { success = true, score = GameManager.Score }
);
```

## MCP Resources - v1.6.0

Access Unity data via MCP resource URIs:

| URI | Description |
|-----|-------------|
| `unity://scene/hierarchy` | Scene hierarchy |
| `unity://scene/active` | Active scene info |
| `unity://project/scripts` | Script list |
| `unity://project/scenes` | Scene list |
| `unity://editor/state` | Editor state |
| `unity://console/logs` | Console logs |
| `unity://session/info` | Session info |

## Tips

### Screenshot Modes
- **Play mode**: `ScreenCapture` - includes all UI overlays
- **Editor mode**: `Camera.main.Render()` - no overlay UI
- Use `{method: "camera"}` for camera-only capture

### Finding Objects
```
gameobject.find {name: "Player"}           # By exact name
gameobject.find {tag: "Enemy"}             # By tag
gameobject.find {componentType: "Camera"}  # By component
gameobject.getAll {activeOnly: true}       # All active objects
```

### Script Recompilation
Unity may not auto-recompile after code changes. Use:
```
editor.refresh    # Full asset refresh + recompile
```

### Play Mode Transitions
- Plugin survives Play mode transitions via SessionState
- If connection lost, wait for auto-reconnect or use Window → OpenClaw Plugin → Settings → Connect

### MCP Bridge Usage
For Claude Code / Cursor integration:
1. Start: Window → OpenClaw Plugin → MCP Bridge → Start
2. Register: `claude mcp add unity -- node /path/to/MCP~/index.js`
3. Verify: `curl http://127.0.0.1:27182/status`

### Input Simulation Limitation
Keyboard/mouse simulation works for **UI interactions** but NOT for `Input.GetKey()`. For gameplay testing:
- Use `transform.setPosition` to move objects directly
- Or migrate to Unity's **new Input System**

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Tool timeout | Check Unity is responding, try `editor.getState` |
| Gateway no connection | Check Window → OpenClaw Plugin → Settings |
| MCP no connection | Start MCP Bridge, verify port 27182 |
| Scripts not updating | Use `editor.refresh` to force recompile |
| Wrong screenshot | Use Play mode for game view with UI |
| MCP 504 timeout | Unity busy or MCP Bridge not started |
| Test Runner not found | Install `com.unity.test-framework` package |

## Security & Privacy Disclosure

This skill drives a live Unity Editor — treat it like giving a collaborator editor access. Full disclosure of capabilities:

- **Arbitrary code execution (by design)**: `script.execute` compiles and runs C# inside the Unity process, and several tools use reflection to reach editor internals. This is the core of editor automation — it also means the skill can do anything the editor can. **Only use in trusted, version-controlled projects.** Ask the user to review C# snippets before running code they didn't write.
- **Destructive operations are gated at runtime (v1.7.0+)**: deleting GameObjects/assets, saving scenes/projects, installing packages, simulating keyboard/mouse input and running `script.execute` are refused by the gateway extension unless it was started with `OPENCLAW_EDITOR_ALLOW_DESTRUCTIVE=1` **and** the call carries `confirm: true`. Confirm the change with the user before setting it — see [Safety and permissions](#safety-and-permissions).
- **Package installation**: Git-based package installs import external, unvetted code into the project. Verify the source URL with the user first.
- **Metadata**: the connection handshake includes machine name and process ID (used to route messages to the right editor instance). No other host information is collected or transmitted.
- **Network surface**: MCP bridge listens on localhost port 27182. Keep it bound to localhost; do not expose the port beyond the local machine or a trusted network.
- **Trigger scope**: routine-sounding requests ("clean up the scene", "save everything", "just try it") map to state-changing editor operations — confirm once before the first state-changing call in a session.
- **Safety defaults**: `disableModelInvocation: true` is set — the model cannot auto-invoke this skill; it runs only on explicit user request. Project-changing tools default to refused, and `dryRun: true` previews any call without sending it. Keep project backups / source control current before automation sessions.

## Links

- **Skill Repository:** https://github.com/TomLeeLive/openclaw-unity-skill
- **Plugin Repository:** https://github.com/TomLeeLive/openclaw-unity-plugin
- **OpenClaw Docs:** https://docs.openclaw.ai
- **MCP Setup Guide:** See Plugin Repository → Documentation~/SETUP_GUIDE.md

## License

Apache-2.0 — See LICENSE.md
