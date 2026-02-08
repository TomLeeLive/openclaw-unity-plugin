---
name: unity-plugin
description: Control Unity Editor via OpenClaw Unity Plugin. Use for Unity game development tasks including scene management, GameObject/Component manipulation, debugging, input simulation, and Play mode control. Triggers on Unity-related requests like inspecting scenes, creating objects, taking screenshots, testing gameplay, or controlling the Editor.
---

# Unity Plugin Skill

Control Unity Editor through 44 built-in tools. Works in both Editor and Play mode.

## Quick Reference

### Core Tools

| Category | Key Tools |
|----------|-----------|
| **Scene** | `scene.getActive`, `scene.getData`, `scene.load` |
| **GameObject** | `gameobject.find`, `gameobject.create`, `gameobject.destroy` |
| **Component** | `component.get`, `component.set`, `component.add` |
| **Debug** | `debug.hierarchy`, `debug.screenshot`, `console.getLogs` |
| **Input** | `input.clickUI`, `input.type`, `input.keyPress` |
| **Editor** | `app.play`, `app.getState`, `editor.refresh` |

## Common Workflows

### 1. Scene Inspection

```
unity_execute: debug.hierarchy {depth: 2}
unity_execute: scene.getActive
```

### 2. Find & Modify Objects

```
unity_execute: gameobject.find {name: "Player"}
unity_execute: component.get {objectName: "Player", componentType: "Transform"}
unity_execute: transform.setPosition {objectName: "Player", x: 0, y: 5, z: 0}
```

### 3. UI Testing

```
unity_execute: input.clickUI {name: "PlayButton"}
unity_execute: input.type {text: "TestUser", elementName: "UsernameInput"}
unity_execute: debug.screenshot
```

### 4. Play Mode Control

```
unity_execute: app.play {state: true}   # Enter Play mode
unity_execute: app.play {state: false}  # Exit Play mode
unity_execute: app.getState             # Check current state
```

### 5. Force Recompile

```
unity_execute: editor.refresh           # Refresh assets & recompile
unity_execute: editor.recompile         # Request recompilation only
```

## Tool Categories

### Console (2 tools)
- `console.getLogs` - Get logs with optional type filter (Log/Warning/Error)
- `console.clear` - Clear captured logs

### Scene (4 tools)
- `scene.list` - List scenes in build settings
- `scene.getActive` - Get active scene info
- `scene.getData` - Get full hierarchy data
- `scene.load` - Load scene by name

### GameObject (6 tools)
- `gameobject.find` - Find by name, tag, or component
- `gameobject.create` - Create object or primitive (Cube, Sphere, etc.)
- `gameobject.destroy` - Destroy object
- `gameobject.getData` - Get detailed data
- `gameobject.setActive` - Enable/disable
- `gameobject.setParent` - Change hierarchy

### Transform (3 tools)
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
- `script.execute` - Execute simple command
- `script.read` - Read script file
- `script.list` - List project scripts

### Application (3 tools)
- `app.getState` - Get play mode, FPS, time
- `app.play` - Enter/exit Play mode
- `app.pause` - Toggle pause

### Debug (3 tools)
- `debug.log` - Write to console
- `debug.screenshot` - Capture screenshot
- `debug.hierarchy` - Text hierarchy view

### Editor (4 tools)
- `editor.refresh` - Refresh AssetDatabase (triggers recompile)
- `editor.recompile` - Request script recompilation
- `editor.focusWindow` - Focus window (game/scene/console/hierarchy/project/inspector)
- `editor.listWindows` - List open windows

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
```

### Script Recompilation
Unity may not auto-recompile after code changes. Use:
```
editor.refresh    # Full asset refresh + recompile
```

### Play Mode Transitions
- Plugin survives Play mode transitions via SessionState
- If connection lost, wait for auto-reconnect or use Window > OpenClaw Plugin > Force Reconnect

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Tool timeout | Check Unity is responding, try `app.getState` |
| No connection | Verify `openclaw unity status`, check gateway |
| Scripts not updating | Use `editor.refresh` to force recompile |
| Wrong screenshot | Use Play mode for game view with UI |
