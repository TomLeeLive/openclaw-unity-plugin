/*
 * OpenClaw Unity Plugin - Editor Connection
 * Works in Edit mode without Play button using EditorApplication.update
 * Survives Play mode transitions via SessionState persistence
 * https://github.com/TomLeeLive/openclaw-unity-plugin
 * MIT License
 */

#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace OpenClaw.Unity.Editor
{
    /// <summary>
    /// Editor-time connection handler that uses EditorApplication.update for polling.
    /// This ensures MCP stays connected even when not in Play mode.
    /// Uses SessionState to survive domain reloads during Play mode transitions.
    /// </summary>
    [InitializeOnLoad]
    public static class OpenClawEditorBridge
    {
        private static bool _initialized;
        private static bool _subscribed;
        private static OpenClawConfig _config;
        private static OpenClawLogger _logger;
        private static float _initDelay = 2f; // Wait 2 seconds after editor loads
        private static double _startTime;
        
        // SessionState keys for persisting across domain reloads
        private const string SESSION_ID_KEY = "OpenClaw_SessionId";
        private const string WAS_CONNECTED_KEY = "OpenClaw_WasConnected";
        private const string PLAY_MODE_TRANSITION_KEY = "OpenClaw_PlayModeTransition";
        
        /// <summary>
        /// Static constructor runs when Unity Editor loads.
        /// Keep this minimal to avoid UPM crashes.
        /// </summary>
        static OpenClawEditorBridge()
        {
            // Use delayCall instead of update to be even safer
            EditorApplication.delayCall += () =>
            {
                _startTime = EditorApplication.timeSinceStartup;
                EditorApplication.update += DeferredInitialize;
            };
        }
        
        /// <summary>
        /// Deferred initialization - waits for editor to stabilize.
        /// </summary>
        private static void DeferredInitialize()
        {
            // Wait for editor to stabilize
            if (EditorApplication.timeSinceStartup - _startTime < _initDelay)
                return;
            
            // Remove this callback
            EditorApplication.update -= DeferredInitialize;
            
            // Now do real initialization
            Initialize();
        }
        
        /// <summary>
        /// Initialize the editor bridge.
        /// </summary>
        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            
            // Subscribe to editor events
            if (!_subscribed)
            {
                _subscribed = true;
                EditorApplication.update += OnEditorUpdate;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                EditorApplication.quitting += OnQuitting;
                AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            }
            
            // Load config
            _config = OpenClawConfig.Instance;
            
            if (_config == null)
            {
                // No config, skip initialization
                return;
            }
            
            // Initialize logger if enabled
            if (_config.captureConsoleLogs)
            {
                try
                {
                    _logger = new OpenClawLogger(_config.maxLogEntries, _config.minLogLevel);
                    _logger.StartCapture();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[OpenClaw] Failed to start log capture: {e.Message}");
                }
            }
            
            // Check if we're recovering from a domain reload
            bool wasInTransition = SessionState.GetBool(PLAY_MODE_TRANSITION_KEY, false);
            bool wasConnected = SessionState.GetBool(WAS_CONNECTED_KEY, false);
            string savedSessionId = SessionState.GetString(SESSION_ID_KEY, null);
            
            // Clear the transition flag
            SessionState.SetBool(PLAY_MODE_TRANSITION_KEY, false);
            
            // Initialize the unified connection manager
            try
            {
                var manager = OpenClawConnectionManager.Instance;
                manager.Initialize(_config, _logger);
                
                // If we had a connection before domain reload, reconnect quickly
                if (wasInTransition && wasConnected)
                {
                    EditorApplication.delayCall += () =>
                    {
                        Debug.Log($"[OpenClaw Editor] Reconnecting after Play mode transition...");
                        manager.ConnectAsync();
                    };
                }
                else
                {
                    // Normal initialization
                    EditorApplication.delayCall += () => Debug.Log("[OpenClaw Editor] Initialized");
                }
            }
            catch (Exception e)
            {
                EditorApplication.delayCall += () => Debug.LogWarning($"[OpenClaw] Init failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Save state before assembly reload (domain reload).
        /// </summary>
        private static void OnBeforeAssemblyReload()
        {
            SaveConnectionState();
        }
        
        /// <summary>
        /// Save current connection state to SessionState.
        /// </summary>
        private static void SaveConnectionState()
        {
            var manager = OpenClawConnectionManager.Instance;
            if (manager != null)
            {
                SessionState.SetBool(WAS_CONNECTED_KEY, manager.IsConnected);
                SessionState.SetString(SESSION_ID_KEY, manager.SessionId ?? "");
            }
        }
        
        /// <summary>
        /// Get the connection manager instance.
        /// </summary>
        public static OpenClawConnectionManager ConnectionManager => OpenClawConnectionManager.Instance;
        
        /// <summary>
        /// Check if connected.
        /// </summary>
        public static bool IsConnected => _initialized && OpenClawConnectionManager.Instance.IsConnected;
        
        /// <summary>
        /// Get session ID.
        /// </summary>
        public static string SessionId => OpenClawConnectionManager.Instance?.SessionId;
        
        /// <summary>
        /// Get connection state.
        /// </summary>
        public static OpenClawConnectionManager.ConnectionState State => 
            _initialized ? OpenClawConnectionManager.Instance.State : OpenClawConnectionManager.ConnectionState.Disconnected;
        
        /// <summary>
        /// Connect to the gateway.
        /// </summary>
        public static void Connect()
        {
            if (!_initialized) Initialize();
            OpenClawConnectionManager.Instance.ConnectAsync();
        }
        
        /// <summary>
        /// Disconnect from the gateway.
        /// </summary>
        public static void Disconnect()
        {
            OpenClawConnectionManager.Instance?.Disconnect();
        }
        
        // Diagnostic counters
        private static int _updateCallCount = 0;
        private static double _lastDiagnosticLog = 0;
        private static bool _firstLogDone = false;
        
        /// <summary>
        /// EditorApplication.update callback - runs every editor frame.
        /// </summary>
        private static void OnEditorUpdate()
        {
            _updateCallCount++;
            
            // Log once on first update
            if (!_firstLogDone)
            {
                _firstLogDone = true;
                Debug.Log($"[OpenClaw] EditorBridge OnEditorUpdate started! Init={_initialized}");
            }
            
            // Log diagnostic every 30 seconds (reduced frequency)
            if (EditorApplication.timeSinceStartup - _lastDiagnosticLog > 30)
            {
                _lastDiagnosticLog = EditorApplication.timeSinceStartup;
                var mgr = OpenClawConnectionManager.Instance;
                var sessionId = mgr?.SessionId ?? "null";
                var shortSession = sessionId.Length > 20 ? sessionId.Substring(0, 20) + "..." : sessionId;
                Debug.Log($"[OpenClaw Diag] Updates: {_updateCallCount}, State: {mgr?.State}, Session: {shortSession}, Init: {_initialized}");
            }
            
            if (!_initialized) return;
            
            try
            {
                // Update the connection manager
                OpenClawConnectionManager.Instance?.Update();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OpenClaw] Update error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Handle play mode state changes.
        /// </summary>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    // About to enter Play mode - save state before domain reload
                    SessionState.SetBool(PLAY_MODE_TRANSITION_KEY, true);
                    SaveConnectionState();
                    Debug.Log("[OpenClaw] Saving connection state before Play mode...");
                    break;
                    
                case PlayModeStateChange.EnteredPlayMode:
                    // Just entered Play mode - reconnect if we were connected
                    if (!_initialized) Initialize();
                    if (!IsConnected && _config != null && _config.autoConnect)
                    {
                        EditorApplication.delayCall += Connect;
                    }
                    break;
                    
                case PlayModeStateChange.ExitingPlayMode:
                    // About to exit Play mode - save state
                    SessionState.SetBool(PLAY_MODE_TRANSITION_KEY, true);
                    SaveConnectionState();
                    Debug.Log("[OpenClaw] Saving connection state before exiting Play mode...");
                    break;
                    
                case PlayModeStateChange.EnteredEditMode:
                    // Back to Edit mode - reconnect if we were connected
                    if (!_initialized) Initialize();
                    if (!IsConnected && _config != null && _config.autoConnect)
                    {
                        EditorApplication.delayCall += Connect;
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Handle editor quitting.
        /// </summary>
        private static void OnQuitting()
        {
            _logger?.Dispose();
            OpenClawConnectionManager.Instance?.Dispose();
        }
        
        /// <summary>
        /// Manually trigger reconnection.
        /// </summary>
        public static void Reconnect()
        {
            Disconnect();
            EditorApplication.delayCall += () => Connect();
        }
        
        /// <summary>
        /// Test connection to the gateway.
        /// </summary>
        public static void TestConnection(Action<bool, string> callback)
        {
            if (_config == null)
            {
                callback?.Invoke(false, "No config found");
                return;
            }
            
            var url = _config.gatewayUrl.TrimEnd('/') + "/api/health";
            
            var request = UnityEngine.Networking.UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();
            
            operation.completed += _ =>
            {
                bool success = request.result == UnityEngine.Networking.UnityWebRequest.Result.Success;
                string message = success ? request.downloadHandler.text : request.error;
                callback?.Invoke(success, message);
                request.Dispose();
            };
        }
    }
}
#endif
