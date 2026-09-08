# Changelog

## [1.7.0] - 2026-09-09

- Fixed: declare `com.unity.ugui` as a package dependency — `Runtime/OpenClawTools.cs` uses `UnityEngine.UI`/`UnityEngine.EventSystems`, so a project without uGUI could not compile the package.

### Security

- **The MCP bridge requires a token.** `OpenClawMCPBridge` used to serve every
  request on 127.0.0.1:27182 with no authentication and with
  `Access-Control-Allow-Origin: *`, so any local process — and any web page open
  in the user's browser — could run Editor tools, `script.execute` included. It
  now generates a per-launch token at start, writes it to
  `~/.openclaw/unity-mcp-bridge.token` with mode `0600`, and requires it as
  `X-OpenClaw-Token` on every request (401 otherwise). CORS headers are gone,
  requests carrying `Origin` or `Referer` are refused with 403, non-loopback
  peers are refused with 403, and the listener no longer binds the `localhost`
  name — only `127.0.0.1`.
- **The gateway handshake is authenticated.** `OpenClawConnectionManager` now
  reads the gateway's per-launch bridge token (`OPENCLAW_BRIDGE_TOKEN`, else
  `OpenClawConfig.apiToken`, else `~/.openclaw/unity-bridge.token`) and sends it
  as `X-OpenClaw-Bridge-Token` on `/unity/register`, keeps the per-session token
  the gateway issues and sends it as `X-OpenClaw-Session` on every poll,
  heartbeat, result and message, and echoes each command's `nonce` with its
  result so the gateway can tell a real answer from a forged one. Requires
  openclaw-unity-skill 1.8.0 on the gateway side.
- **Tool results and responses are no longer dumped to the console.** The MCP
  bridge logged the full response JSON on every call; it now logs the status and
  byte count. Tokens are never logged at all — only the token file's path.

### Added

- `Runtime/OpenClawBridgeAuth.cs` — token generation, `0600` token files in the
  OpenClaw config dir (`$OPENCLAW_CONFIG_DIR`/`$OPENCLAW_HOME`/`~/.openclaw`),
  length-constant comparison.
- The OpenClaw panel's MCP section shows whether the bridge requires a token and
  where the token file is, with a copy button — and a warning box when legacy
  unauthenticated mode is on.
- `OPENCLAW_UNITY_ALLOW_LEGACY_UNAUTHENTICATED=1` restores the pre-1.7.0 open MCP
  bridge for an old client, with a warning in the Editor log and in the panel.

### Changed

- `MCP~/index.js` reads the token file (or `OPENCLAW_BRIDGE_TOKEN`) and sends it
  on every request, and explains what to do when the bridge answers 401/403.
- `OpenClawPlugin~/` (the bundled gateway extension) is now the same code as
  openclaw-unity-skill 1.8.0, instead of a diverged 1.3.6 copy that still served
  the unauthenticated `/unity/*` endpoints.
- `skill/SKILL.md` and `skill/references/tools.md` re-synced with the canonical
  skill package, which the stale copy had been missing every safety section of.

## [1.6.0] - 2026-02-11

### Added - Performance & Power Update 🚀

#### Batch Execution (1 tool)
- `batch.execute` - Execute multiple tools in one call (10-100x performance boost)
  - Supports `stopOnError` option
  - Returns detailed results for each command

#### Session Info (1 tool)
- `session.getInfo` - Get session info for multi-instance identification
  - Returns project name, process ID, machine name, session ID

#### ScriptableObject Tools (6 tools)
- `scriptableobject.create` - Create new ScriptableObject asset
- `scriptableobject.load` - Load and inspect ScriptableObject
- `scriptableobject.save` - Save ScriptableObject changes
- `scriptableobject.getField` - Get specific field value
- `scriptableobject.setField` - Set field value with auto-save option
- `scriptableobject.list` - List ScriptableObjects in project

#### Shader Tools (3 tools)
- `shader.list` - List shaders in project
- `shader.getInfo` - Get shader properties and info
- `shader.getKeywords` - Get shader keywords

#### Texture Tools (5 tools)
- `texture.create` - Create new texture with color fill
- `texture.getInfo` - Get texture info (size, format, import settings)
- `texture.setPixels` - Fill region with color
- `texture.resize` - Resize texture via import settings
- `texture.list` - List textures in project

#### MCP Resources Protocol
- 8 resource URIs for direct data access:
  - `unity://scene/hierarchy` - Scene hierarchy tree
  - `unity://scene/active` - Active scene info
  - `unity://project/scripts` - Project scripts list
  - `unity://project/scenes` - Build settings scenes
  - `unity://project/assets` - Asset search
  - `unity://editor/state` - Editor state
  - `unity://console/logs` - Console logs
  - `unity://session/info` - Session info

#### Custom Tools API
- `OpenClawCustomTools` class for extending with project-specific tools
- Simple registration: `OpenClawCustomTools.Register(name, description, handler)`
- Full CustomTool class support with categories
- Auto-integration with tool list and execution

### Fixed
- JSON parser now supports nested arrays/objects (batch.execute)
- ConnectionManager array parsing for command parameters

### Summary
- **16 new tools** added (total ~100 tools)
- MCP Resources for direct data access
- Custom Tools API for extensibility
- Batch execution for AI workflow optimization
- ScriptableObject support for data-driven games
- Shader/Texture tools for visual asset management

## [1.5.1] - 2026-02-11

### Changed
- Removed diagnostic logging added during debugging
- Cleaned up verbose debug output from connection manager

### Fixed
- Console output clutter from poll count and command execution logs

## [1.5.0] - 2026-02-11

### Added - Major Feature Update 🚀

#### Material Tools (5 tools)
- `material.create` - Create new material with shader, color, metallic, smoothness
- `material.assign` - Assign material to GameObject
- `material.modify` - Modify material properties (color, metallic, smoothness, emission, tiling)
- `material.getInfo` - Get detailed material info including all shader properties
- `material.list` - List materials in project with filtering

#### Prefab Tools (5 tools)
- `prefab.create` - Create prefab from scene GameObject
- `prefab.instantiate` - Instantiate prefab in scene with position
- `prefab.open` - Open prefab for editing
- `prefab.close` - Close prefab editing mode
- `prefab.save` - Save currently edited prefab

#### Asset Tools (7 tools)
- `asset.find` - Search assets by query, type, folder
- `asset.copy` - Copy asset to new path
- `asset.move` - Move/rename asset
- `asset.delete` - Delete asset (with trash option)
- `asset.refresh` - Refresh AssetDatabase
- `asset.import` - Import/reimport specific asset
- `asset.getPath` - Get asset path by name

#### Package Manager Tools (4 tools)
- `package.add` - Install package by name or git URL
- `package.remove` - Remove installed package
- `package.list` - List installed packages
- `package.search` - Search Unity package registry

#### Test Runner Tools (3 tools)
- `test.run` - Run EditMode/PlayMode tests with filtering
- `test.list` - List available tests
- `test.getResults` - Get last test run results

#### Script Execution Enhancements
- Enhanced `script.execute` with reflection-based method calls
- Support for Debug.Log/LogWarning/LogError
- Support for Time.timeScale modification
- Support for PlayerPrefs get/set operations
- Support for arbitrary method calls via reflection

### Changed
- Now **~80 tools** total (from 55)
- Industry-leading feature set for Unity AI integration

## [1.4.0] - 2026-02-11

### Added
- MCP Bridge for direct Claude Code integration
- Hybrid architecture: Gateway + MCP modes
- MCP section in OpenClaw Editor Window
- UI Menu consolidation (Window/OpenClaw Plugin/)

## [1.3.7] - 2026-02-11

### Added
- Security documentation: `disableModelInvocation` setting explanation
- Added recommendation for ClawHub publishing


All notable changes to this project will be documented in this file.

## [1.3.6] - 2026-02-10

### Added
- `editor.domainReload` - Force domain reload (reinitializes static fields)
- Now **55 tools** total

### Fixed
- Documentation updated to reflect all new tools

## [1.3.5] - 2026-02-10

### Added
- `scene.save` - Save the active scene (Editor mode only)
- `scene.saveAll` - Save all open scenes (Editor mode only)

## [1.3.4] - 2026-02-09

### Added
- Buy Me a Coffee support (`.github/FUNDING.yml`)
- Sponsor badge in README

### Changed
- Branding: Updated emoji from 🐾 to 🦞

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

- Fixed: declare `com.unity.ugui` as a package dependency — `Runtime/OpenClawTools.cs` uses `UnityEngine.UI`/`UnityEngine.EventSystems`, so a project without uGUI could not compile the package.
