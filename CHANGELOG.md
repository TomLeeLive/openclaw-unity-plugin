# Changelog

All notable changes to this project will be documented in this file.

## [1.3.3] - 2026-02-09

### Added
- `gameobject.getAll` - Get all GameObjects in scene with filtering options
  - `activeOnly` - Filter to active objects only
  - `includePosition` - Include position data (default: true)
  - `maxCount` - Limit results (default: 500)
  - `rootOnly` - Get only root level objects
  - `nameFilter` - Filter by name (contains, case-insensitive)
- Now **52 tools** total

## [1.3.2] - 2026-02-08

### Added
- `IsKeyDown(KeyCode)` - Check if simulated key was just pressed this frame
- `IsKeyUp(KeyCode)` - Check if simulated key was just released this frame
- `ScheduleAction(float delay, Action action)` - Schedule delayed actions for input timing
- Frame-accurate key state tracking for GetKeyDown/GetKeyUp support

### Changed
- `SimulateKeyPress` now properly respects duration parameter using scheduled release
- Key press/release now marks frame for accurate GetKeyDown/GetKeyUp detection

### Documentation
- Updated README to clarify input simulation works with games that check OpenClaw simulated input
- Added example of how games can integrate with OpenClaw input simulation

## [1.3.1] - 2026-02-08

### Added
- `input.click` - Alias for `input.clickUI` for more intuitive API
- Now **51 tools** total

## [1.3.0] - 2026-02-08

### Changed
- Unified version across plugin (package.json) and extension (OpenClawPlugin~)
- Updated documentation to reflect v1.3.0

### Documentation
- Added Documentation~/README.md and README_KO.md (folder index)
- Added Documentation~/index.md and index_KO.md (main documentation)
- Updated all version references

## [1.2.3] - 2026-02-08

### Added
- `transform.getPosition` - Get world position (x, y, z)
- `transform.getRotation` - Get Euler rotation (x, y, z)
- `transform.getScale` - Get local scale (x, y, z)
- `gameobject.delete` - Alias for `gameobject.destroy`
- `scene.open` - Open scene in Editor mode using `EditorSceneManager`
- `SafeDestroy()` helper method for proper object cleanup
- Now **50 tools** total

### Fixed
- **Edit Mode Destroy Error**: "Destroy may not be called from edit mode"
  - `GameObjectDestroy` now uses `DestroyImmediate` in Editor mode
  - `ComponentRemove` now uses `DestroyImmediate` in Editor mode
  - `DebugScreenshot` now uses `SafeDestroy()` for texture cleanup
- All tools now work correctly in both Edit and Play modes

### Changed
- `scene.load` now clarifies it's for Play mode only
- Tool descriptions updated with clearer parameter info

## [1.2.2] - 2026-02-07

### Changed
- Renamed "OpenClaw Bridge" to "OpenClaw Plugin" throughout codebase
  - Menu: `Window > OpenClaw Plugin`
  - Window title: "OpenClaw Plugin"
  - GameObject name: "OpenClaw Plugin"
  - All documentation updated

## [1.2.1] - 2026-02-07

### Added
- `editor.focusWindow` - Focus specific Editor windows (Game, Scene, Console, etc.)
- `editor.listWindows` - List all open Editor windows with focus state
- Now **44 tools** total

### Fixed
- Compilation error: Changed `UnityEditor.Resources` to `Resources.FindObjectsOfTypeAll`

## [1.2.0] - 2026-02-07

### Improved
- **Play Mode Transition Handling**: Connection now properly survives Editor ↔ Play mode transitions
  - Uses `SessionState` to persist connection state across domain reloads
  - Listens to `AssemblyReloadEvents.beforeAssemblyReload` to save state
  - Auto-reconnects immediately after entering/exiting Play mode
  - Handles all four `PlayModeStateChange` events (ExitingEditMode, EnteredPlayMode, ExitingPlayMode, EnteredEditMode)

### Fixed
- **Screenshot Tool**: Now captures immediately instead of async
  - Play mode: Uses `CaptureScreenshotAsTexture()` for synchronous capture
  - Editor mode: Captures GameView via reflection (`m_RenderTexture`)
  - File exists when path is returned

### Added
- `QuickReconnect()` method for fast reconnection during transitions

## [1.1.0] - 2026-02-07

### Added
- **Editor Mode Support** - MCP connection now works without Play mode
- `OpenClawConnectionManager` - Unified connection manager for Editor & Play mode
- `OpenClawEditorBridge` - Editor-time initializer using `[InitializeOnLoad]`
- Automatic reconnection on connection loss
- Main thread execution queue for thread-safe tool calls

### Changed
- Renamed package from `openclaw-unity-bridge` to `openclaw-unity-plugin`
- Display name changed to "OpenClaw Unity Plugin"
- Connection maintained across Edit/Play mode transitions
- Deferred initialization to avoid Unity 6 UPM crashes

### Fixed
- UPM EPIPE crash on Unity 6 startup
- Connection status display in Editor window

## [1.0.0] - 2026-02-07

### Added
- Initial release
- 30+ Unity tools for AI interaction
- Console log capture
- Scene management
- GameObject manipulation
- Component editing
- Play mode control
- Debug tools (hierarchy, screenshot)
- Status overlay in Game view
- Configuration via ScriptableObject
