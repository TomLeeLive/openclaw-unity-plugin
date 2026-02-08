# Unity Plugin Tools Reference

Complete parameter reference for all 44 tools.

## Console Tools

### console.getLogs
Get Unity console logs.
```json
{
  "count": 50,           // Max logs to return (default: 50)
  "type": "Error"        // Optional: "Log", "Warning", "Error", "Exception"
}
```

### console.clear
Clear captured logs. No parameters.

---

## Scene Tools

### scene.list
List all scenes in build settings. No parameters.

### scene.getActive
Get active scene info. No parameters.
Returns: name, path, buildIndex, isLoaded, rootCount

### scene.getData
Get full scene hierarchy.
```json
{
  "depth": 3             // How deep to traverse (default: unlimited)
}
```

### scene.load
Load a scene.
```json
{
  "sceneName": "GameScene",  // Scene name or path
  "additive": false          // Load additively (default: false)
}
```

---

## GameObject Tools

### gameobject.find
Find GameObjects.
```json
{
  "name": "Player",          // Find by exact name
  "tag": "Enemy",            // Find by tag
  "componentType": "Camera", // Find by component type
  "includeInactive": false,  // Include inactive objects
  "depth": 1                 // Child depth to return
}
```

### gameobject.create
Create GameObject.
```json
{
  "name": "MyObject",        // Object name
  "primitive": "Cube",       // Optional: Cube, Sphere, Cylinder, Capsule, Plane, Quad
  "parent": "ParentName",    // Optional parent
  "position": {"x": 0, "y": 0, "z": 0}
}
```

### gameobject.destroy
Destroy GameObject.
```json
{
  "name": "ObjectToDestroy"
}
```

### gameobject.getData
Get detailed object data.
```json
{
  "name": "Player",
  "includeComponents": true  // Include component list
}
```

### gameobject.setActive
Enable/disable object.
```json
{
  "name": "Player",
  "active": true
}
```

### gameobject.setParent
Change parent.
```json
{
  "name": "Child",
  "parent": "NewParent",     // null to unparent
  "worldPositionStays": true
}
```

---

## Transform Tools

### transform.setPosition
Set world position.
```json
{
  "objectName": "Player",
  "x": 10, "y": 0, "z": 5
}
```

### transform.setRotation
Set rotation (Euler angles).
```json
{
  "objectName": "Player",
  "x": 0, "y": 90, "z": 0
}
```

### transform.setScale
Set local scale.
```json
{
  "objectName": "Player",
  "x": 1, "y": 2, "z": 1
}
```

---

## Component Tools

### component.add
Add component to object.
```json
{
  "objectName": "Player",
  "componentType": "Rigidbody"
}
```

### component.remove
Remove component.
```json
{
  "objectName": "Player",
  "componentType": "Rigidbody"
}
```

### component.get
Get component data.
```json
{
  "objectName": "Player",
  "componentType": "Transform"
}
```

### component.set
Set component field/property.
```json
{
  "objectName": "Player",
  "componentType": "Rigidbody",
  "fieldName": "mass",
  "value": 10
}
```

### component.list
List available component types.
```json
{
  "filter": "Collider"       // Optional filter string
}
```

---

## Script Tools

### script.execute
Execute simple command.
```json
{
  "command": "Debug.Log(\"Hello\")"
}
```

### script.read
Read script file.
```json
{
  "path": "Assets/Scripts/Player.cs"
}
```

### script.list
List script files.
```json
{
  "folder": "Assets/Scripts",  // Optional folder
  "pattern": "*.cs"            // Optional pattern
}
```

---

## Application Tools

### app.getState
Get application state. No parameters.
Returns: isPlaying, isPaused, platform, unityVersion, productName, fps, time

### app.play
Control Play mode.
```json
{
  "state": true              // true = play, false = stop
}
```

### app.pause
Toggle pause. No parameters.

---

## Debug Tools

### debug.log
Write to console.
```json
{
  "message": "Test message",
  "type": "Log"              // "Log", "Warning", "Error"
}
```

### debug.screenshot
Capture screenshot.
```json
{
  "method": "screencapture", // "screencapture" or "camera"
  "superSize": 1             // Resolution multiplier
}
```

### debug.hierarchy
Get text hierarchy view.
```json
{
  "depth": 2                 // Max depth
}
```

---

## Editor Tools

### editor.refresh
Refresh AssetDatabase (triggers recompile). No parameters.

### editor.recompile
Request script recompilation. No parameters.

### editor.focusWindow
Focus Editor window.
```json
{
  "window": "game"           // game, scene, console, hierarchy, project, inspector
}
```

### editor.listWindows
List open Editor windows. No parameters.

---

## Input Tools

### input.keyPress
Press and release key.
```json
{
  "key": "Space"             // Key name (e.g., "A", "Space", "Return", "Escape")
}
```

### input.keyDown
Press and hold key.
```json
{
  "key": "W"
}
```

### input.keyUp
Release key.
```json
{
  "key": "W"
}
```

### input.type
Type text.
```json
{
  "text": "Hello World",
  "elementName": "InputField" // Optional: target input field
}
```

### input.mouseMove
Move mouse.
```json
{
  "x": 500, "y": 300
}
```

### input.mouseClick
Click at position.
```json
{
  "x": 500, "y": 300,
  "button": 0                // 0=left, 1=right, 2=middle
}
```

### input.mouseDrag
Drag operation.
```json
{
  "startX": 100, "startY": 100,
  "endX": 200, "endY": 200,
  "button": 0
}
```

### input.mouseScroll
Scroll wheel.
```json
{
  "delta": 120               // Positive = up, negative = down
}
```

### input.getMousePosition
Get cursor position. No parameters.

### input.clickUI
Click UI element by name.
```json
{
  "name": "PlayButton"
}
```
