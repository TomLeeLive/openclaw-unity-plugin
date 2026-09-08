# Unity Plugin Tools Reference

Complete parameter reference for all ~100 tools.

> **Permissions:** read-only tools (`get*`, `list`, `find`, `script.read`,
> `debug.hierarchy`, `debug.screenshot`, `console.getLogs`) run directly. Every
> other tool listed here changes the project and is refused unless the gateway
> was started with `OPENCLAW_EDITOR_ALLOW_DESTRUCTIVE=1` **and** the call passes
> `confirm: true`. `dryRun: true` previews any call without sending it. See
> [SKILL.md → Safety and permissions](../SKILL.md#safety-and-permissions).

## Console Tools (3)

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

### console.getErrors
Get error and exception logs.
```json
{
  "count": 50,              // Max errors to return (default: 50)
  "includeWarnings": false  // Include warnings (default: false)
}
```

---

## Scene Tools (7)

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
Load a scene (Play mode).
```json
{
  "sceneName": "GameScene",  // Scene name or path
  "additive": false          // Load additively (default: false)
}
```

### scene.open
Open a scene (Editor mode).
```json
{
  "scenePath": "Assets/Scenes/Game.unity"
}
```

### scene.save
Save active scene (Editor mode). No parameters.

### scene.saveAll
Save all open scenes (Editor mode). No parameters.

---

## GameObject Tools (8)

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

### gameobject.getAll
Get all GameObjects.
```json
{
  "activeOnly": true,        // Only active objects
  "rootOnly": false          // Only root objects
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

### gameobject.delete
Delete GameObject (alias for destroy).
```json
{
  "name": "ObjectToDelete"
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

## Transform Tools (6)

### transform.getPosition
Get world position.
```json
{
  "objectName": "Player"
}
```

### transform.getRotation
Get rotation (Euler angles).
```json
{
  "objectName": "Player"
}
```

### transform.getScale
Get local scale.
```json
{
  "objectName": "Player"
}
```

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

## Component Tools (5)

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

## Script Tools (3)

### script.execute
Execute command. Supports Debug.Log, Time, PlayerPrefs, and **reflection-based method calls**.
```json
{
  "code": "Debug.Log(\"Hello\")"
}
```

**Examples:**
- `Debug.Log('message')` - Log to console
- `Time.timeScale = 0.5` - Set time scale
- `PlayerPrefs.SetInt('key', 100)` - Set player prefs
- `MyClass.MyStaticMethod()` - Call static method via reflection
- `MyClass.Method('param', 123)` - Call with parameters

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

## Application Tools (4)

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

### app.stop
Stop Play mode. No parameters.

---

## Debug Tools (3)

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

## Editor Tools (9)

### editor.refresh
Refresh AssetDatabase (triggers recompile). No parameters.

### editor.recompile
Request script recompilation. No parameters.

### editor.domainReload
Force domain reload (reinitializes static fields). No parameters.

### editor.focusWindow
Focus Editor window.
```json
{
  "window": "game"           // game, scene, console, hierarchy, project, inspector
}
```

### editor.listWindows
List open Editor windows. No parameters.

### editor.getState
Get editor state. Same as app.getState.

### editor.play
Enter Play mode. No parameters.

### editor.stop
Exit Play mode. No parameters.

### editor.pause / editor.unpause
Pause/resume Play mode. No parameters.

---

## Input Tools (10)

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

---

## Material Tools (5) - NEW in v1.5.0

### material.create
Create a new material.
```json
{
  "name": "MyMaterial",           // Material name
  "shaderName": "Standard",       // Shader (default: "Standard")
  "color": "#FF0000",             // Optional: hex color
  "metallic": 0.5,                // Optional: 0-1
  "smoothness": 0.5,              // Optional: 0-1
  "path": "Assets/Materials"      // Optional: save folder
}
```

### material.assign
Assign material to GameObject.
```json
{
  "gameObjectName": "Cube",
  "materialPath": "Assets/Materials/MyMaterial.mat",
  "materialIndex": 0              // Optional: renderer material index
}
```

### material.modify
Modify material properties.
```json
{
  "path": "Assets/Materials/MyMaterial.mat",
  "color": "#00FF00",             // Optional
  "metallic": 1.0,                // Optional
  "smoothness": 0.8,              // Optional
  "emission": "#FF0000",          // Optional: emission color
  "emissionIntensity": 2.0        // Optional
}
```

### material.getInfo
Get material information.
```json
{
  "path": "Assets/Materials/MyMaterial.mat"
}
```
Returns: shader name, all properties with types and values

### material.list
List materials in project.
```json
{
  "folder": "Assets/Materials",   // Optional: search folder
  "filter": "Metal"               // Optional: name filter
}
```

---

## Prefab Tools (5) - NEW in v1.5.0

### prefab.create
Create prefab from scene object.
```json
{
  "gameObjectName": "Player",
  "path": "Assets/Prefabs/Player.prefab"
}
```

### prefab.instantiate
Instantiate prefab in scene.
```json
{
  "prefabPath": "Assets/Prefabs/Player.prefab",
  "x": 0, "y": 1, "z": 0,         // Optional: position
  "name": "Player_Instance"       // Optional: instance name
}
```

### prefab.open
Open prefab for editing.
```json
{
  "path": "Assets/Prefabs/Player.prefab"
}
```

### prefab.close
Close prefab editing mode.
```json
{
  "saveChanges": true             // Optional: save before closing
}
```

### prefab.save
Save currently edited prefab. No parameters.

---

## Asset Tools (7) - NEW in v1.5.0

### asset.find
Search for assets.
```json
{
  "query": "Player",              // Search query
  "type": "Prefab",               // Optional: asset type filter
  "folder": "Assets/Prefabs"      // Optional: folder filter
}
```

### asset.copy
Copy asset to new location.
```json
{
  "sourcePath": "Assets/Prefabs/Player.prefab",
  "destPath": "Assets/Backup/Player.prefab"
}
```

### asset.move
Move/rename asset.
```json
{
  "sourcePath": "Assets/Old/Item.prefab",
  "destPath": "Assets/New/Item.prefab"
}
```

### asset.delete
Delete asset.
```json
{
  "path": "Assets/Prefabs/OldPrefab.prefab",
  "useTrash": true                // Optional: move to trash instead of permanent delete
}
```

### asset.refresh
Refresh AssetDatabase. No parameters.

### asset.import
Import or reimport asset.
```json
{
  "path": "Assets/Textures/Texture.png"
}
```

### asset.getPath
Get asset path by name.
```json
{
  "name": "Player",
  "type": "Prefab"                // Optional: asset type
}
```

---

## Package Manager Tools (4) - NEW in v1.5.0

### package.add
Install a package.
```json
{
  "packageName": "com.unity.textmeshpro"  // Package name
}
```
Or install from Git:
```json
{
  "gitUrl": "https://github.com/example/package.git"
}
```

### package.remove
Remove installed package.
```json
{
  "packageName": "com.unity.textmeshpro"
}
```

### package.list
List installed packages. No parameters.

### package.search
Search Unity package registry.
```json
{
  "query": "TextMesh"
}
```

---

## Test Runner Tools (3) - NEW in v1.5.0

### test.run
Run tests.
```json
{
  "testMode": "EditMode",         // "EditMode" or "PlayMode"
  "filter": "PlayerTests",        // Optional: test name filter
  "category": "Unit"              // Optional: category filter
}
```

### test.list
List available tests.
```json
{
  "testMode": "EditMode"          // "EditMode" or "PlayMode"
}
```

### test.getResults
Get last test run results. No parameters.

> **Note:** Test Runner tools require `com.unity.test-framework` package installed.

---

## Batch Execution (1) - NEW in v1.6.0

### batch.execute
Execute multiple tools in one call.
```json
{
  "commands": [
    { "tool": "scene.getActive", "params": {} },
    { "tool": "gameobject.find", "params": { "name": "Player" } }
  ],
  "stopOnError": false
}
```

---

## Session (1) - NEW in v1.6.0

### session.getInfo
Get session info for multi-instance support. No parameters.
Returns: project, unityVersion, platform, dataPath, isPlaying, processId, machineName, sessionId

---

## ScriptableObject Tools (6) - NEW in v1.6.0

### scriptableobject.create
Create new ScriptableObject.
```json
{
  "type": "MyDataAsset",
  "path": "Assets/Data/NewAsset.asset",
  "name": "NewAsset"
}
```

### scriptableobject.load
Load and inspect ScriptableObject.
```json
{
  "path": "Assets/Data/MyAsset.asset"
}
```

### scriptableobject.save
Save ScriptableObject changes.
```json
{
  "path": "Assets/Data/MyAsset.asset"
}
```

### scriptableobject.getField
Get specific field value.
```json
{
  "path": "Assets/Data/MyAsset.asset",
  "field": "maxHealth"
}
```

### scriptableobject.setField
Set field value.
```json
{
  "path": "Assets/Data/MyAsset.asset",
  "field": "maxHealth",
  "value": 100,
  "save": true
}
```

### scriptableobject.list
List ScriptableObjects in project.
```json
{
  "folder": "Assets/Data",
  "type": "MyDataAsset",
  "maxCount": 50
}
```

---

## Shader Tools (3) - NEW in v1.6.0

### shader.list
List shaders in project.
```json
{
  "filter": "URP",
  "maxCount": 50,
  "includeBuiltIn": false
}
```

### shader.getInfo
Get shader properties.
```json
{
  "name": "Universal Render Pipeline/Lit"
}
```

### shader.getKeywords
Get shader keywords.
```json
{
  "name": "Universal Render Pipeline/Lit"
}
```

---

## Texture Tools (5) - NEW in v1.6.0

### texture.create
Create new texture.
```json
{
  "width": 256,
  "height": 256,
  "path": "Assets/Textures/NewTexture.png",
  "color": "#FF0000"
}
```

### texture.getInfo
Get texture info.
```json
{
  "path": "Assets/Textures/MyTexture.png"
}
```

### texture.setPixels
Fill region with color.
```json
{
  "path": "Assets/Textures/MyTexture.png",
  "x": 0,
  "y": 0,
  "width": 64,
  "height": 64,
  "color": "#00FF00"
}
```

### texture.resize
Resize texture.
```json
{
  "path": "Assets/Textures/MyTexture.png",
  "width": 512,
  "height": 512
}
```

### texture.list
List textures in project.
```json
{
  "folder": "Assets/Textures",
  "filter": "Player",
  "maxCount": 50
}
```
