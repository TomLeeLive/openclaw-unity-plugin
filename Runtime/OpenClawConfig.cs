/*
 * OpenClaw Unity Plugin
 * https://github.com/TomLeeLive/openclaw-unity-plugin
 * MIT License
 */

using UnityEngine;

namespace OpenClaw.Unity
{
    /// <summary>
    /// Configuration for OpenClaw Unity Plugin.
    /// Create via Assets > Create > OpenClaw > Config
    /// </summary>
    [CreateAssetMenu(fileName = "OpenClawConfig", menuName = "OpenClaw/Config", order = 1)]
    public class OpenClawConfig : ScriptableObject
    {
        [Header("Connection")]
        [Tooltip("OpenClaw Gateway URL (e.g., http://localhost:18789)")]
        public string gatewayUrl = "http://localhost:18789";
        
        [Tooltip("API Token for authentication (optional, depends on gateway config)")]
        public string apiToken = "";
        
        [Header("Settings")]
        [Tooltip("Automatically connect when Unity starts")]
        public bool autoConnect = true;
        
        [Tooltip("Show connection status in Game view")]
        public bool showStatusOverlay = true;
        
        [Tooltip("Request timeout in seconds")]
        public float requestTimeout = 30f;
        
        [Tooltip("Heartbeat interval in seconds (0 to disable)")]
        public float heartbeatInterval = 30f;
        
        [Header("Logging")]
        [Tooltip("Capture Unity console logs for AI debugging")]
        public bool captureConsoleLogs = true;
        
        [Tooltip("Maximum log entries to keep in memory")]
        public int maxLogEntries = 1000;
        
        [Tooltip("Log level to capture")]
        public LogLevel minLogLevel = LogLevel.Warning;
        
        [Header("MCP Bridge (Direct Connection)")]
        [Tooltip("Enable MCP Bridge for direct Claude Code integration")]
        public bool enableMCPBridge = false;
        
        [Tooltip("MCP Bridge port (default: 27182)")]
        public int mcpBridgePort = 27182;
        
        [Header("Security")]
        [Tooltip("Allow code execution via AI (DANGEROUS - use only in development)")]
        public bool allowCodeExecution = true;
        
        [Tooltip("Allow file system access")]
        public bool allowFileAccess = true;
        
        [Tooltip("Allow scene modifications")]
        public bool allowSceneModification = true;
        
        public enum LogLevel
        {
            Log = 0,
            Warning = 1,
            Error = 2,
            Exception = 3
        }
        
        private static OpenClawConfig _instance;
        
        public static OpenClawConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }
        
        public static OpenClawConfig Load()
        {
            var config = Resources.Load<OpenClawConfig>("OpenClawConfig");
            if (config == null)
            {
                Debug.LogWarning("[OpenClaw] Config not found in Resources. Using defaults.");
                config = CreateInstance<OpenClawConfig>();
            }
            return config;
        }
        
        public string GetFullUrl(string endpoint)
        {
            var baseUrl = gatewayUrl.TrimEnd('/');
            return $"{baseUrl}/{endpoint.TrimStart('/')}";
        }
    }
}
