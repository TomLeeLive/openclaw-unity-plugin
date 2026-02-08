# 🛠️ OpenClaw Unity Plugin - Development Guide

This document is the development guide for OpenClaw Unity Plugin. It covers architecture, how to add new tools, and debugging tips.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Project Structure](#project-structure)
3. [Core Components](#core-components)
4. [Adding New Tools](#adding-new-tools)
5. [JSON Parsing](#json-parsing)
6. [Play Mode Transition Handling](#play-mode-transition-handling)
7. [Debugging](#debugging)
8. [Contribution Guidelines](#contribution-guidelines)

---

## Architecture Overview

### Communication Flow

```
┌─────────────┐     HTTP      ┌─────────────────┐     Tool Call    ┌─────────────┐
│   OpenClaw  │ ────────────► │ Gateway Plugin  │ ───────────────► │    Unity    │
│   Agent     │               │ (index.ts)      │                  │   Plugin    │
└─────────────┘               └─────────────────┘                  └─────────────┘
                                      │                                   │
                                      │ POST /unity/poll                  │
                                      │◄──────────────────────────────────│
                                      │                                   │
                                      │ Command JSON                      │
                                      │──────────────────────────────────►│
                                      │                                   │
                                      │ POST /unity/result                │
                                      │◄──────────────────────────────────│
```

### Core Design Principles

1. **Edit Mode Support**: Use AI tools in Editor without pressing Play
2. **Auto Reconnection**: Automatic recovery on connection loss
3. **Play Mode Transition Survival**: Survives domain reload via SessionState
4. **Main Thread Execution**: Unity API calls only on Main Thread

---

## Project Structure

```
openclaw-unity-plugin/
├── package.json              # UPM package definition
├── README.md                 # User documentation
├── CHANGELOG.md              # Version history
│
├── Runtime/                  # Runtime code (Editor + Play)
│   ├── OpenClaw.Unity.asmdef
│   ├── OpenClawConnectionManager.cs   # HTTP communication
│   ├── OpenClawTools.cs               # 44 tool implementations
│   ├── OpenClawBridge.cs              # MonoBehaviour (Play Mode)
│   ├── OpenClawConfig.cs              # Settings ScriptableObject
│   ├── OpenClawLogger.cs              # Log capture
│   └── OpenClawStatusOverlay.cs       # Status overlay UI
│
├── Editor/                   # Editor-only code
│   ├── OpenClaw.Unity.Editor.asmdef
│   ├── OpenClawEditorBridge.cs        # [InitializeOnLoad] entry point
│   └── OpenClawWindow.cs              # Settings window
│
└── docs/                     # Documentation
    ├── DEVELOPMENT.md        # Development guide (Korean)
    ├── DEVELOPMENT_EN.md     # Development guide (this file)
    ├── TESTING.md            # Testing guide (Korean)
    └── TESTING_EN.md         # Testing guide (English)
```

---

## Core Components

### OpenClawEditorBridge.cs

Entry point that initializes automatically when Editor starts.

```csharp
[InitializeOnLoad]
public static class OpenClawEditorBridge
{
    static OpenClawEditorBridge()
    {
        // Delayed initialization to prevent Unity 6 UPM EPIPE
        EditorApplication.delayCall += () =>
        {
            _startTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += DeferredInitialize;
        };
    }
}
```

**Key Features:**
- Auto-runs on Editor start via `[InitializeOnLoad]`
- 2-second delayed initialization (Unity 6 stability)
- State save/restore on Play Mode transition via `SessionState`
- Updates Connection Manager in `EditorApplication.update`

### OpenClawConnectionManager.cs

Singleton handling HTTP communication and command execution.

```csharp
public class OpenClawConnectionManager : IDisposable
{
    private static OpenClawConnectionManager _instance;
    
    public static OpenClawConnectionManager Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                    _instance = new OpenClawConnectionManager();
                return _instance;
            }
        }
    }
}
```

**Key Features:**
- Receives commands from Gateway via HTTP polling
- Safely calls Unity API via Main Thread queue
- Auto-reconnection logic
- JSON parsing (nested objects, escape handling)

### OpenClawTools.cs

Implementation of 44 AI tools.

```csharp
public class OpenClawTools
{
    private readonly Dictionary<string, Func<Dictionary<string, object>, object>> _tools;
    
    public OpenClawTools(OpenClawBridge bridge)
    {
        _tools = new Dictionary<string, Func<Dictionary<string, object>, object>>
        {
            { "console.getLogs", ConsoleGetLogs },
            { "scene.list", SceneList },
            { "gameobject.find", GameObjectFind },
            // ... 44 tools
        };
    }
}
```

---

## Adding New Tools

### Step 1: Define Tool Method

Add a new method to `OpenClawTools.cs`:

```csharp
private object MyNewTool(Dictionary<string, object> p)
{
    // Extract parameters
    var name = GetString(p, "name", "default");
    var count = GetInt(p, "count", 1);
    var enabled = GetBool(p, "enabled", true);
    
    try
    {
        // Implement tool logic
        var result = DoSomething(name, count, enabled);
        
        // Success response
        return new { 
            success = true, 
            result = result,
            message = "Operation completed"
        };
    }
    catch (Exception e)
    {
        // Failure response
        return new { 
            success = false, 
            error = e.Message 
        };
    }
}
```

### Step 2: Register Tool

Add to the `_tools` Dictionary in the constructor:

```csharp
public OpenClawTools(OpenClawBridge bridge)
{
    _tools = new Dictionary<string, Func<Dictionary<string, object>, object>>
    {
        // Existing tools...
        
        // Add new tool
        { "myCategory.myNewTool", MyNewTool },
    };
}
```

### Step 3: Add Description

Add description in `GetToolDescription` method:

```csharp
private string GetToolDescription(string name)
{
    return name switch
    {
        // Existing descriptions...
        
        "myCategory.myNewTool" => "Description of my new tool (params: name, count, enabled)",
        
        _ => name
    };
}
```

### Example: Adding a New Tool

Here's an example of a tool that changes GameObject layer:

```csharp
// 1. Implement method
private object GameObjectSetLayer(Dictionary<string, object> p)
{
    var name = GetString(p, "name", null);
    var layer = GetString(p, "layer", null);
    
    if (string.IsNullOrEmpty(name))
        return new { success = false, error = "name parameter required" };
    
    var go = GameObject.Find(name);
    if (go == null)
        return new { success = false, error = $"GameObject '{name}' not found" };
    
    int layerIndex = LayerMask.NameToLayer(layer);
    if (layerIndex < 0)
        return new { success = false, error = $"Layer '{layer}' not found" };
    
    go.layer = layerIndex;
    return new { 
        success = true, 
        gameObject = name, 
        layer = layer, 
        layerIndex = layerIndex 
    };
}

// 2. Register (in constructor)
{ "gameobject.setLayer", GameObjectSetLayer },

// 3. Add description
"gameobject.setLayer" => "Set GameObject layer (params: name, layer)",
```

---

## JSON Parsing

### Basic Structure

The plugin uses custom JSON parsing without external libraries:

```csharp
private Dictionary<string, object> ParseJson(string json)
{
    // Nested object support
    if (value.StartsWith("{") && value.EndsWith("}"))
        result[key] = ParseJson(value);
    
    // String unescape
    if (value.StartsWith("\"") && value.EndsWith("\""))
        result[key] = UnescapeString(value.Substring(1, value.Length - 2));
}
```

### Parameter Helper Methods

```csharp
// Extract string
var str = GetString(p, "key", "defaultValue");

// Extract integer
var num = GetInt(p, "key", 0);

// Extract float
var flt = GetFloat(p, "key", 0.0f);

// Extract boolean
var flag = GetBool(p, "key", false);
```

---

## Play Mode Transition Handling

### The Problem

When entering/exiting Play Mode in Unity, domain reload causes:
- All static variables reset
- HttpClient connection lost
- In-progress work lost

### Solution: SessionState

```csharp
// Save state before Play Mode transition
private static void OnPlayModeStateChanged(PlayModeStateChange state)
{
    switch (state)
    {
        case PlayModeStateChange.ExitingEditMode:
        case PlayModeStateChange.ExitingPlayMode:
            SessionState.SetBool(WAS_CONNECTED_KEY, manager.IsConnected);
            SessionState.SetString(SESSION_ID_KEY, manager.SessionId ?? "");
            SessionState.SetBool(PLAY_MODE_TRANSITION_KEY, true);
            break;
            
        case PlayModeStateChange.EnteredPlayMode:
        case PlayModeStateChange.EnteredEditMode:
            // Auto-reconnect on initialization
            if (SessionState.GetBool(PLAY_MODE_TRANSITION_KEY, false))
            {
                manager.ConnectAsync();
            }
            break;
    }
}
```

---

## Debugging

### Unity Console Logs

The plugin outputs logs with `[OpenClaw]` prefix:

```
[OpenClaw] Connecting to http://localhost:18789...
[OpenClaw] Connected! Session: unity_1234567890_abc123
[OpenClaw] Received command: debug.hierarchy
[OpenClaw] Tool result: debug.hierarchy - success
```

### Gateway Logs

```bash
openclaw gateway status
# Or in Gateway console:
[Unity] Registered: MyProject (6000.3.7f1) - Session: unity_xxx
[Unity] Tool result: debug.hierarchy - success
```

### Connection Troubleshooting

1. **Check Gateway Status**
   ```bash
   openclaw gateway status
   ```

2. **Check in Unity Window**
   - Open `Window > OpenClaw Plugin`
   - Check Connection Status
   - Click "Test Connection" button

3. **Direct HTTP Test**
   ```bash
   curl http://localhost:18789/unity/status
   ```

### Common Mistakes

#### ❌ UnityEditor.Resources (doesn't exist)

```csharp
// Wrong code - causes compile error
var windows = UnityEditor.Resources.FindObjectsOfTypeAll<EditorWindow>();
```

#### ✅ Resources.FindObjectsOfTypeAll (correct way)

```csharp
// Correct code - use UnityEngine.Resources
var windows = Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>();
```

> **Note:** `Resources.FindObjectsOfTypeAll` is a method of the `UnityEngine.Resources` class.
> Even when finding Editor-only types (e.g., `EditorWindow`), you must use `UnityEngine.Resources`.
> The `UnityEditor.Resources` namespace does not exist. (Fixed in v1.2.1)

---

## Contribution Guidelines

### Code Style

- C# standard naming conventions (PascalCase for public, _camelCase for private)
- XML documentation comments on all public methods
- Unity API calls only on Main Thread

### Commit Messages

```
feat: Add new input simulation tools
fix: Properly handle nested JSON objects
docs: Update README with new tools
refactor: Simplify connection manager
```

### Testing

When adding new features, always test:
1. Works in Editor Mode
2. Works in Play Mode
3. Connection maintained during Play Mode transition

### Pull Request

1. Create new branch: `feature/your-feature-name`
2. Commit changes
3. Update CHANGELOG.md
4. Create Pull Request

---

## Contact

- GitHub: https://github.com/TomLeeLive/openclaw-unity-plugin
- OpenClaw Discord: https://discord.com/invite/clawd
