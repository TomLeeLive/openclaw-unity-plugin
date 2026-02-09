/*
 * OpenClaw Unity Plugin
 * https://github.com/TomLeeLive/openclaw-unity-plugin
 * MIT License
 */

using UnityEngine;
using UnityEditor;

namespace OpenClaw.Unity.Editor
{
    /// <summary>
    /// Editor window for OpenClaw configuration and status.
    /// </summary>
    public class OpenClawWindow : EditorWindow
    {
        private OpenClawConfig _config;
        private Vector2 _scrollPos;
        private bool _showTools = true;
        private bool _showLogs = true;
        
        [MenuItem("Window/OpenClaw Plugin")]
        public static void ShowWindow()
        {
            var window = GetWindow<OpenClawWindow>("OpenClaw Plugin");
            window.minSize = new Vector2(350, 400);
        }
        
        private void OnEnable()
        {
            LoadOrCreateConfig();
        }
        
        private void LoadOrCreateConfig()
        {
            _config = Resources.Load<OpenClawConfig>("OpenClawConfig");
            
            if (_config == null)
            {
                // Create default config
                _config = CreateInstance<OpenClawConfig>();
                
                // Ensure Resources folder exists
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                
                AssetDatabase.CreateAsset(_config, "Assets/Resources/OpenClawConfig.asset");
                AssetDatabase.SaveAssets();
                
                Debug.Log("[OpenClaw] Created default config at Assets/Resources/OpenClawConfig.asset");
            }
        }
        
        private void OnGUI()
        {
            if (_config == null)
            {
                LoadOrCreateConfig();
                return;
            }
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            DrawHeader();
            EditorGUILayout.Space(10);
            DrawConnectionStatus();
            EditorGUILayout.Space(10);
            DrawConfiguration();
            EditorGUILayout.Space(10);
            DrawTools();
            EditorGUILayout.Space(10);
            DrawLogs();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.FlexibleSpace();
            
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("🦞 OpenClaw Unity Plugin", headerStyle, GUILayout.Height(30));
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField("Connect Unity to your AI assistant", EditorStyles.centeredGreyMiniLabel);
        }
        
        private void DrawConnectionStatus()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Use the unified connection manager via EditorBridge
            var connManager = OpenClawConnectionManager.Instance;
            var state = connManager?.State ?? OpenClawConnectionManager.ConnectionState.Disconnected;
            var isConnected = state == OpenClawConnectionManager.ConnectionState.Connected;
            var sessionId = connManager?.SessionId;
            
            // Status indicator
            EditorGUILayout.BeginHorizontal();
            
            var (statusColor, statusText) = state switch
            {
                OpenClawConnectionManager.ConnectionState.Connected => (Color.green, "Connected"),
                OpenClawConnectionManager.ConnectionState.Connecting => (Color.yellow, "Connecting..."),
                OpenClawConnectionManager.ConnectionState.Reconnecting => (Color.yellow, "Reconnecting..."),
                OpenClawConnectionManager.ConnectionState.Error => (Color.red, "Error"),
                _ => (Color.gray, "Disconnected")
            };
            
            var oldColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField("●", GUILayout.Width(20));
            GUI.color = oldColor;
            
            EditorGUILayout.LabelField($"Status: {statusText}");
            
            // Mode indicator
            var modeText = Application.isPlaying ? "(Play)" : "(Editor)";
            EditorGUILayout.LabelField(modeText, EditorStyles.miniLabel, GUILayout.Width(60));
            
            EditorGUILayout.EndHorizontal();
            
            // Session ID
            if (!string.IsNullOrEmpty(sessionId))
            {
                EditorGUILayout.LabelField($"Session: {sessionId}", EditorStyles.miniLabel);
            }
            
            // Error message
            if (!string.IsNullOrEmpty(connManager?.LastError) && state == OpenClawConnectionManager.ConnectionState.Error)
            {
                EditorGUILayout.HelpBox(connManager.LastError, MessageType.Error);
            }
            
            EditorGUILayout.Space(5);
            
            // Buttons
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = state != OpenClawConnectionManager.ConnectionState.Connecting;
            
            if (isConnected)
            {
                if (GUILayout.Button("Disconnect"))
                {
                    OpenClawEditorBridge.Disconnect();
                }
            }
            else
            {
                if (GUILayout.Button("Connect"))
                {
                    OpenClawEditorBridge.Connect();
                }
            }
            
            GUI.enabled = true;
            
            if (GUILayout.Button("Test Connection"))
            {
                TestConnection();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawConfiguration()
        {
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUI.BeginChangeCheck();
            
            _config.gatewayUrl = EditorGUILayout.TextField("Gateway URL", _config.gatewayUrl);
            _config.apiToken = EditorGUILayout.PasswordField("API Token", _config.apiToken);
            
            EditorGUILayout.Space(5);
            
            _config.autoConnect = EditorGUILayout.Toggle("Auto Connect", _config.autoConnect);
            _config.showStatusOverlay = EditorGUILayout.Toggle("Show Status Overlay", _config.showStatusOverlay);
            _config.heartbeatInterval = EditorGUILayout.FloatField("Heartbeat Interval (s)", _config.heartbeatInterval);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Logging", EditorStyles.miniBoldLabel);
            
            _config.captureConsoleLogs = EditorGUILayout.Toggle("Capture Console Logs", _config.captureConsoleLogs);
            _config.maxLogEntries = EditorGUILayout.IntField("Max Log Entries", _config.maxLogEntries);
            _config.minLogLevel = (OpenClawConfig.LogLevel)EditorGUILayout.EnumPopup("Min Log Level", _config.minLogLevel);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Security", EditorStyles.miniBoldLabel);
            
            _config.allowCodeExecution = EditorGUILayout.Toggle("Allow Code Execution", _config.allowCodeExecution);
            _config.allowFileAccess = EditorGUILayout.Toggle("Allow File Access", _config.allowFileAccess);
            _config.allowSceneModification = EditorGUILayout.Toggle("Allow Scene Modification", _config.allowSceneModification);
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_config);
            }
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Select Config Asset"))
            {
                Selection.activeObject = _config;
                EditorGUIUtility.PingObject(_config);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawTools()
        {
            _showTools = EditorGUILayout.Foldout(_showTools, "Available Tools", true);
            
            if (!_showTools) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var tools = new[]
            {
                ("console.*", "Log capture and management"),
                ("scene.*", "Scene listing and loading"),
                ("gameobject.*", "Find, create, modify GameObjects"),
                ("transform.*", "Position, rotation, scale"),
                ("component.*", "Add, remove, modify components"),
                ("script.*", "Read script files"),
                ("app.*", "Play mode control"),
                ("debug.*", "Logging, screenshots, hierarchy")
            };
            
            foreach (var (name, desc) in tools)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(name, EditorStyles.miniLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField(desc, EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawLogs()
        {
            _showLogs = EditorGUILayout.Foldout(_showLogs, "Recent Logs", true);
            
            if (!_showLogs) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var bridge = OpenClawBridge.Instance;
            if (bridge?.Logger != null)
            {
                var logs = bridge.Logger.GetLogs(10);
                
                if (logs.Count == 0)
                {
                    EditorGUILayout.LabelField("No logs captured", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    foreach (var log in logs)
                    {
                        var icon = log.type switch
                        {
                            LogType.Error or LogType.Exception => "❌",
                            LogType.Warning => "⚠️",
                            _ => "ℹ️"
                        };
                        
                        var msg = log.message.Length > 80 ? log.message.Substring(0, 77) + "..." : log.message;
                        EditorGUILayout.LabelField($"{icon} [{log.timestamp:HH:mm:ss}] {msg}", EditorStyles.miniLabel);
                    }
                }
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Total: {bridge.Logger.Count} logs", EditorStyles.miniLabel);
                if (GUILayout.Button("Clear", GUILayout.Width(50)))
                {
                    bridge.Logger.Clear();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("Logger not active", EditorStyles.centeredGreyMiniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void TestConnection()
        {
            var url = _config.gatewayUrl.TrimEnd('/') + "/api/health";
            
            Debug.Log($"[OpenClaw] Testing connection to {url}...");
            
            var request = UnityEngine.Networking.UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();
            
            operation.completed += _ =>
            {
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[OpenClaw] ✓ Connection successful! Response: {request.downloadHandler.text}");
                    EditorUtility.DisplayDialog("Connection Test", "Successfully connected to OpenClaw gateway!", "OK");
                }
                else
                {
                    Debug.LogError($"[OpenClaw] ✗ Connection failed: {request.error}");
                    EditorUtility.DisplayDialog("Connection Test", $"Failed to connect:\n{request.error}", "OK");
                }
                
                request.Dispose();
            };
        }
        
        private void OnInspectorUpdate()
        {
            // Refresh window periodically
            Repaint();
        }
    }
}
