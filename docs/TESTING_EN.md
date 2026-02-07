# 🧪 OpenClaw Unity Plugin - Testing Guide

This document is the testing guide for OpenClaw Unity Plugin. It includes usage and examples for all 44 tools.

## Table of Contents

1. [Test Environment Setup](#test-environment-setup)
2. [Basic Connection Test](#basic-connection-test)
3. [Console Tools](#console-tools)
4. [Scene Tools](#scene-tools)
5. [GameObject Tools](#gameobject-tools)
6. [Transform Tools](#transform-tools)
7. [Component Tools](#component-tools)
8. [Application Tools](#application-tools)
9. [Debug Tools](#debug-tools)
10. [Editor Tools](#editor-tools)
11. [Input Tools](#input-tools)
12. [Automation Test Scenarios](#automation-test-scenarios)

---

## Test Environment Setup

### 1. Start OpenClaw Gateway

```bash
# Check Gateway status
openclaw gateway status

# Start Gateway (if needed)
openclaw gateway start
```

### 2. Prepare Unity Project

1. Install OpenClaw Unity Plugin (see README.md)
2. Open `Window > OpenClaw Bridge`
3. Verify Gateway URL: `http://localhost:18789`
4. Confirm "OpenClaw Connected" status

### 3. Verify Test Session

```bash
# Check connected Unity sessions
openclaw unity status
```

Expected output:
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

## Basic Connection Test

### unity_sessions - List Sessions

**Description:** Returns a list of all connected Unity sessions.

**Parameters:** None

**Example:**
```
Ask OpenClaw: "Check connected Unity sessions"
```

**Response Example:**
```json
{
  "success": true,
  "sessions": [
    {
      "sessionId": "unity_1770465206658_j3x59z4",
      "project": "endless_survival",
      "version": "6000.3.7f1",
      "platform": "Editor",
      "tools": 44
    }
  ],
  "count": 1
}
```

---

## Console Tools

### console.getLogs - Get Logs

**Description:** Retrieves logs from Unity Console.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `count` | int | 100 | Number of logs to retrieve |
| `type` | string | null | Filter: "log", "warning", "error" |

**Example 1: Get all logs**
```
Ask OpenClaw: "Show Unity console logs"
```
```json
// Tool call
{ "tool": "console.getLogs", "parameters": {} }

// Response
[
  { "type": "Log", "message": "[OpenClaw] Connected!", "timestamp": "..." },
  { "type": "Warning", "message": "Shader not found", "timestamp": "..." }
]
```

**Example 2: Get errors only**
```
Ask OpenClaw: "Show only Unity error logs"
```
```json
// Tool call
{ "tool": "console.getLogs", "parameters": { "type": "error", "count": 10 } }
```

### console.clear - Clear Logs

**Description:** Clears captured logs.

**Parameters:** None

**Example:**
```
Ask OpenClaw: "Clear Unity console logs"
```
```json
// Tool call
{ "tool": "console.clear", "parameters": {} }

// Response
{ "success": true }
```

---

## Scene Tools

### scene.list - List Scenes

**Description:** Returns a list of all scenes registered in Build Settings.

**Parameters:** None

**Example:**
```
Ask OpenClaw: "Show scene list in project"
```
```json
// Response
[
  { "index": 0, "path": "Assets/Scenes/MainMenu.unity", "name": "MainMenu" },
  { "index": 1, "path": "Assets/Scenes/GameScene.unity", "name": "GameScene" }
]
```

### scene.getActive - Get Active Scene Info

**Description:** Returns information about the currently active scene.

**Parameters:** None

**Example:**
```
Ask OpenClaw: "Tell me about the current scene"
```
```json
// Response
{
  "name": "MainMenu",
  "path": "Assets/Scenes/MainMenu.unity",
  "buildIndex": 0,
  "isLoaded": true,
  "rootCount": 5
}
```

### scene.getData - Get Scene Data

**Description:** Returns hierarchy data of the scene.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | string | null | Scene name (null for current) |
| `depth` | int | 2 | Hierarchy depth |

**Example:**
```
Ask OpenClaw: "Show current scene structure up to 3 levels"
```
```json
// Tool call
{ "tool": "scene.getData", "parameters": { "depth": 3 } }
```

### scene.load - Load Scene

**Description:** Loads a scene.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | string | (required) | Scene name |
| `mode` | string | "Single" | "Single" or "Additive" |

**Example:**
```
Ask OpenClaw: "Switch to GameScene"
```
```json
// Tool call
{ "tool": "scene.load", "parameters": { "name": "GameScene" } }

// Response
{ "success": true, "scene": "GameScene" }
```

---

## GameObject Tools

### gameobject.find - Find Objects

**Description:** Searches for GameObjects by name, tag, or component type.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | string | null | Search by name |
| `tag` | string | null | Search by tag |
| `type` | string | null | Search by component type |
| `depth` | int | 1 | Result depth |

**Example 1: Search by name**
```
Ask OpenClaw: "Find object named Player"
```
```json
// Tool call
{ "tool": "gameobject.find", "parameters": { "name": "Player" } }
```

**Example 2: Search by tag**
```
Ask OpenClaw: "Find objects with Enemy tag"
```
```json
// Tool call
{ "tool": "gameobject.find", "parameters": { "tag": "Enemy" } }
```

**Example 3: Search by component**
```
Ask OpenClaw: "Find objects with Camera component"
```
```json
// Tool call
{ "tool": "gameobject.find", "parameters": { "type": "Camera" } }
```

### gameobject.create - Create Object

**Description:** Creates a new GameObject or Primitive.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | string | "New GameObject" | Object name |
| `primitive` | string | null | "Cube", "Sphere", "Cylinder", etc. |
| `position` | object | null | {x, y, z} position |

**Example 1: Create empty object**
```
Ask OpenClaw: "Create an empty object named Enemy"
```
```json
// Tool call
{ "tool": "gameobject.create", "parameters": { "name": "Enemy" } }
```

**Example 2: Create Primitive**
```
Ask OpenClaw: "Create a sphere at position (0, 1, 0)"
```
```json
// Tool call
{
  "tool": "gameobject.create",
  "parameters": {
    "name": "MySphere",
    "primitive": "Sphere",
    "position": { "x": 0, "y": 1, "z": 0 }
  }
}
```

### gameobject.destroy - Destroy Object

**Description:** Destroys a GameObject.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | string | (required) | Object name |

**Example:**
```
Ask OpenClaw: "Delete TempObject"
```
```json
// Tool call
{ "tool": "gameobject.destroy", "parameters": { "name": "TempObject" } }

// Response
{ "success": true, "destroyed": "TempObject" }
```

### gameobject.setActive - Activate/Deactivate

**Description:** Activates or deactivates a GameObject.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | string | (required) | Object name |
| `active` | bool | true | Active state |

**Example:**
```
Ask OpenClaw: "Deactivate Player object"
```
```json
// Tool call
{ "tool": "gameobject.setActive", "parameters": { "name": "Player", "active": false } }
```

---

## Transform Tools

### transform.setPosition - Set Position

**Description:** Sets world position of a GameObject.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | string | (required) | Object name |
| `x` | float | (current) | X coordinate |
| `y` | float | (current) | Y coordinate |
| `z` | float | (current) | Z coordinate |

**Example:**
```
Ask OpenClaw: "Move Player to origin"
```
```json
// Tool call
{
  "tool": "transform.setPosition",
  "parameters": { "name": "Player", "x": 0, "y": 0, "z": 0 }
}
```

### transform.setRotation - Set Rotation

**Description:** Sets rotation of a GameObject (Euler angles).

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | string | (required) | Object name |
| `x` | float | (current) | X rotation (degrees) |
| `y` | float | (current) | Y rotation (degrees) |
| `z` | float | (current) | Z rotation (degrees) |

**Example:**
```
Ask OpenClaw: "Rotate Camera 90 degrees on Y axis"
```
```json
// Tool call
{
  "tool": "transform.setRotation",
  "parameters": { "name": "Camera", "y": 90 }
}
```

### transform.setScale - Set Scale

**Description:** Sets local scale of a GameObject.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | string | (required) | Object name |
| `x` | float | (current) | X scale |
| `y` | float | (current) | Y scale |
| `z` | float | (current) | Z scale |

**Example:**
```
Ask OpenClaw: "Make Cube 2x size"
```
```json
// Tool call
{
  "tool": "transform.setScale",
  "parameters": { "name": "Cube", "x": 2, "y": 2, "z": 2 }
}
```

---

## Component Tools

### component.add - Add Component

**Description:** Adds a component to a GameObject.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `gameObject` | string | (required) | Object name |
| `type` | string | (required) | Component type |

**Example:**
```
Ask OpenClaw: "Add Rigidbody component to Player"
```
```json
// Tool call
{
  "tool": "component.add",
  "parameters": { "gameObject": "Player", "type": "Rigidbody" }
}
```

### component.get - Get Component

**Description:** Gets component data.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `gameObject` | string | (required) | Object name |
| `type` | string | (required) | Component type |

**Example:**
```
Ask OpenClaw: "Show Player's Transform info"
```
```json
// Tool call
{
  "tool": "component.get",
  "parameters": { "gameObject": "Player", "type": "Transform" }
}

// Response
{
  "type": "Transform",
  "fields": {
    "position": { "x": 0, "y": 1, "z": 0 },
    "rotation": { "x": 0, "y": 0, "z": 0, "w": 1 },
    "localScale": { "x": 1, "y": 1, "z": 1 }
  }
}
```

### component.set - Set Component Value

**Description:** Sets field/property value of a component.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `gameObject` | string | (required) | Object name |
| `type` | string | (required) | Component type |
| `field` | string | (required) | Field/property name |
| `value` | any | (required) | Value to set |

**Example:**
```
Ask OpenClaw: "Set Player's Rigidbody mass to 5"
```
```json
// Tool call
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

## Application Tools

### app.getState - Get App State

**Description:** Returns current application state.

**Parameters:** None

**Example:**
```
Ask OpenClaw: "Tell me current Unity state"
```
```json
// Response
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

### app.play - Start Play Mode

**Description:** Starts Play mode.

**Parameters:** None

**Example:**
```
Ask OpenClaw: "Start Unity Play mode"
```
```json
// Tool call
{ "tool": "app.play", "parameters": {} }

// Response
{ "success": true }
```

### app.stop - Stop Play Mode

**Description:** Stops Play mode.

**Parameters:** None

**Example:**
```
Ask OpenClaw: "Stop Unity Play mode"
```
```json
// Tool call
{ "tool": "app.stop", "parameters": {} }
```

### app.pause - Toggle Pause

**Description:** Toggles Play mode pause.

**Parameters:** None

**Example:**
```
Ask OpenClaw: "Pause the game"
```
```json
// Tool call
{ "tool": "app.pause", "parameters": {} }

// Response
{ "success": true, "isPaused": true }
```

---

## Debug Tools

### debug.log - Output Log

**Description:** Outputs a log to Unity Console.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `message` | string | "" | Log message |
| `level` | string | "log" | "log", "warning", "error" |

**Example:**
```
Ask OpenClaw: "Output 'Hello from AI!' to Unity console"
```
```json
// Tool call
{
  "tool": "debug.log",
  "parameters": { "message": "Hello from AI!", "level": "log" }
}
```

### debug.screenshot - Capture Screenshot

**Description:** Captures a screenshot of the game screen.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `filename` | string | (auto) | File name |
| `method` | string | "auto" | "auto", "camera", "screencapture" |
| `width` | int | (auto) | Width resolution |
| `height` | int | (auto) | Height resolution |

**Example:**
```
Ask OpenClaw: "Capture current game screen"
```
```json
// Tool call
{ "tool": "debug.screenshot", "parameters": {} }

// Response
{
  "success": true,
  "path": "/Users/.../screenshot_20260207_123456.png",
  "mode": "screencapture",
  "width": 1920,
  "height": 1080
}
```

### debug.hierarchy - Output Hierarchy

**Description:** Outputs scene hierarchy as text.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `depth` | int | 3 | Output depth |

**Example:**
```
Ask OpenClaw: "Show current scene structure"
```
```json
// Tool call
{ "tool": "debug.hierarchy", "parameters": { "depth": 3 } }

// Response
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

## Editor Tools

### editor.refresh - Refresh Assets

**Description:** Refreshes AssetDatabase (triggers recompile on script changes).

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `forceUpdate` | bool | false | Force update |

**Example:**
```
Ask OpenClaw: "Refresh Unity assets"
```
```json
// Tool call
{ "tool": "editor.refresh", "parameters": { "forceUpdate": true } }

// Response
{ "success": true, "action": "AssetDatabase.Refresh", "forceUpdate": true }
```

### editor.recompile - Recompile Scripts

**Description:** Requests script recompilation.

**Parameters:** None

**Example:**
```
Ask OpenClaw: "Recompile Unity scripts"
```
```json
// Tool call
{ "tool": "editor.recompile", "parameters": {} }

// Response
{ "success": true, "action": "RequestScriptCompilation" }
```

### editor.focusWindow - Focus Window

**Description:** Focuses a specific Editor window.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `window` | string | "game" | Window name |

**Supported windows:**
- `game` / `gameview` - Game View
- `scene` / `sceneview` - Scene View
- `console` - Console
- `hierarchy` - Hierarchy
- `project` - Project Browser
- `inspector` - Inspector
- `profiler` - Profiler
- `animation` - Animation
- `animator` - Animator

**Example:**
```
Ask OpenClaw: "Focus on Game window"
```
```json
// Tool call
{ "tool": "editor.focusWindow", "parameters": { "window": "game" } }

// Response
{ "success": true, "window": "game", "focused": true }
```

### editor.listWindows - List Open Windows

**Description:** Returns a list of all currently open Editor windows.

**Parameters:** None

**Example:**
```
Ask OpenClaw: "Show list of open Unity windows"
```
```json
// Tool call
{ "tool": "editor.listWindows", "parameters": {} }

// Response
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

## Input Tools

### input.keyPress - Key Input

**Description:** Presses and releases a key (tap).

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `key` | string | (required) | KeyCode name |
| `duration` | float | 0.1 | Press duration (seconds) |

**Supported keys:**
- Alphabet: `A`-`Z`
- Numbers: `Alpha0`-`Alpha9` or `0`-`9`
- Arrows: `LeftArrow`, `RightArrow`, `UpArrow`, `DownArrow` or `left`, `right`, `up`, `down`
- Special: `Space`, `Return`, `Escape`, `Tab`, `Backspace`
- Modifiers: `LeftShift`, `RightShift`, `LeftControl`, `LeftAlt`
- Mouse: `Mouse0` (left), `Mouse1` (right), `Mouse2` (wheel)

**Example:**
```
Ask OpenClaw: "Press W key"
```
```json
// Tool call
{ "tool": "input.keyPress", "parameters": { "key": "W" } }

// Response
{ "success": true, "key": "W", "keyCode": "W", "duration": 0.1 }
```

### input.keyDown / input.keyUp - Key Hold

**Description:** Presses or releases a key.

**Example:**
```
Ask OpenClaw: "Hold Shift key"
```
```json
// Press
{ "tool": "input.keyDown", "parameters": { "key": "LeftShift" } }

// Release later
{ "tool": "input.keyUp", "parameters": { "key": "LeftShift" } }
```

### input.type - Type Text

**Description:** Types text into currently focused input field.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `text` | string | (required) | Text to type |

**Example:**
```
Ask OpenClaw: "Type 'TestPlayer' in input field"
```
```json
// Tool call
{ "tool": "input.type", "parameters": { "text": "TestPlayer" } }

// Response
{ "success": true, "text": "TestPlayer", "target": "UsernameInput", "method": "TMP_InputField" }
```

### input.mouseMove - Move Mouse

**Description:** Moves mouse cursor.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `x` | float | (required) | X coordinate |
| `y` | float | (required) | Y coordinate |
| `normalized` | bool | false | Use 0-1 normalized coordinates |

**Example 1: Pixel coordinates**
```
Ask OpenClaw: "Move mouse to (500, 300)"
```
```json
// Tool call
{ "tool": "input.mouseMove", "parameters": { "x": 500, "y": 300 } }
```

**Example 2: Normalized coordinates (screen center)**
```
Ask OpenClaw: "Move mouse to screen center"
```
```json
// Tool call
{ "tool": "input.mouseMove", "parameters": { "x": 0.5, "y": 0.5, "normalized": true } }
```

### input.mouseClick - Mouse Click

**Description:** Clicks mouse at specific position.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `x` | float | (current) | X coordinate |
| `y` | float | (current) | Y coordinate |
| `button` | int | 0 | 0=left, 1=right, 2=wheel |
| `clicks` | int | 1 | Click count |
| `normalized` | bool | false | Use normalized coordinates |

**Example:**
```
Ask OpenClaw: "Double-click at (400, 500)"
```
```json
// Tool call
{
  "tool": "input.mouseClick",
  "parameters": { "x": 400, "y": 500, "clicks": 2 }
}
```

### input.mouseDrag - Mouse Drag

**Description:** Drags from start point to end point.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `startX` | float | (required) | Start X |
| `startY` | float | (required) | Start Y |
| `endX` | float | (required) | End X |
| `endY` | float | (required) | End Y |
| `button` | int | 0 | Mouse button |
| `steps` | int | 10 | Intermediate steps |

**Example:**
```
Ask OpenClaw: "Drag from (100, 100) to (500, 500)"
```
```json
// Tool call
{
  "tool": "input.mouseDrag",
  "parameters": {
    "startX": 100, "startY": 100,
    "endX": 500, "endY": 500,
    "steps": 20
  }
}
```

### input.mouseScroll - Mouse Scroll

**Description:** Scrolls mouse wheel.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `deltaX` | float | 0 | Horizontal scroll |
| `deltaY` | float | 0 | Vertical scroll |

**Example:**
```
Ask OpenClaw: "Scroll down"
```
```json
// Tool call
{ "tool": "input.mouseScroll", "parameters": { "deltaY": -120 } }
```

### input.getMousePosition - Get Mouse Position

**Description:** Returns current mouse cursor position.

**Parameters:** None

**Example:**
```
Ask OpenClaw: "Tell me current mouse position"
```
```json
// Response
{
  "x": 512,
  "y": 384,
  "normalizedX": 0.5,
  "normalizedY": 0.5,
  "screenWidth": 1024,
  "screenHeight": 768
}
```

### input.clickUI - Click UI Element

**Description:** Finds and clicks a UI element by name.

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | string | null | UI element name |
| `path` | string | null | Full path |
| `button` | int | 0 | Mouse button |

**Example 1: Click by name**
```
Ask OpenClaw: "Click PlayButton"
```
```json
// Tool call
{ "tool": "input.clickUI", "parameters": { "name": "PlayButton" } }

// Response
{ "success": true, "target": "PlayButton", "method": "Button.onClick" }
```

**Example 2: Click by path**
```
Ask OpenClaw: "Click Canvas/Menu/StartButton"
```
```json
// Tool call
{ "tool": "input.clickUI", "parameters": { "path": "Canvas/Menu/StartButton" } }
```

---

## Automation Test Scenarios

### Scenario 1: Login Flow Test

```
Ask OpenClaw: "Test login screen. Type 'TestPlayer' in UsernameInput and click PlayButton"
```

**Execution sequence:**
```json
// 1. Click input field to focus
{ "tool": "input.clickUI", "parameters": { "name": "UsernameInput" } }

// 2. Type text
{ "tool": "input.type", "parameters": { "text": "TestPlayer" } }

// 3. Click Play button
{ "tool": "input.clickUI", "parameters": { "name": "PlayButton" } }

// 4. Screenshot result
{ "tool": "debug.screenshot", "parameters": {} }
```

### Scenario 2: Gameplay Test

```
Ask OpenClaw: "Test character movement. Move forward with W key, jump with Space"
```

**Execution sequence:**
```json
// 1. Start Play mode
{ "tool": "app.play", "parameters": {} }

// 2. Move forward with W (1 second)
{ "tool": "input.keyDown", "parameters": { "key": "W" } }
// ... wait 1 second ...
{ "tool": "input.keyUp", "parameters": { "key": "W" } }

// 3. Jump with Space
{ "tool": "input.keyPress", "parameters": { "key": "Space" } }

// 4. Screenshot result
{ "tool": "debug.screenshot", "parameters": {} }
```

### Scenario 3: UI Navigation Test

```
Ask OpenClaw: "Test menu navigation. Settings > Audio > Volume control"
```

**Execution sequence:**
```json
// 1. Click Settings button
{ "tool": "input.clickUI", "parameters": { "name": "SettingsButton" } }

// 2. Click Audio tab
{ "tool": "input.clickUI", "parameters": { "name": "AudioTab" } }

// 3. Drag volume slider
{
  "tool": "input.mouseDrag",
  "parameters": {
    "startX": 200, "startY": 300,
    "endX": 400, "endY": 300
  }
}

// 4. Verify result
{ "tool": "debug.screenshot", "parameters": {} }
```

---

## Troubleshooting

### Tool returns "Unknown tool" error

- Check if Unity has recompiled
- Run `editor.recompile` tool
- Click Unity window to focus

### Parameters not being passed

- Check JSON format
- Check parameter name case sensitivity
- Check plugin version (v1.2.0+ required)

### Screenshot is black/empty

- Check Play mode
- Check Camera.main exists
- Check Game View window is open

### UI click not working

- Check EventSystem exists
- Check Canvas is active
- Check Raycast Target is enabled

---

## Test Checklist

- [ ] Gateway connection status
- [ ] Unity session registration
- [ ] Console tools (getLogs, clear)
- [ ] Scene tools (list, getActive, getData, load)
- [ ] GameObject tools (find, create, destroy, setActive)
- [ ] Transform tools (setPosition, setRotation, setScale)
- [ ] Component tools (add, remove, get, set)
- [ ] Application tools (getState, play, stop, pause)
- [ ] Debug tools (log, screenshot, hierarchy)
- [ ] Editor tools (refresh, recompile, focusWindow, listWindows)
- [ ] Input tools (keyPress, mouseClick, clickUI, type)
- [ ] Connection maintained during Play Mode transition
