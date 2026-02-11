/*
 * OpenClaw Unity Plugin - Unified Connection Manager
 * Works in both Editor and Play mode
 * https://github.com/TomLeeLive/openclaw-unity-plugin
 * MIT License
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenClaw.Unity
{
    /// <summary>
    /// Unified connection manager that works in both Editor and Play mode.
    /// Uses HTTP polling for communication with the OpenClaw gateway.
    /// </summary>
    public class OpenClawConnectionManager : IDisposable
    {
        private static OpenClawConnectionManager _instance;
        private static readonly object _lock = new object();
        
        public static OpenClawConnectionManager Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new OpenClawConnectionManager();
                    }
                    return _instance;
                }
            }
        }
        
        public enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected,
            Reconnecting,
            Error
        }
        
        // Connection state
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public string SessionId { get; private set; }
        public string LastError { get; private set; }
        public bool IsConnected => State == ConnectionState.Connected;
        
        // Events
        public event Action<ConnectionState> OnStateChanged;
        public event Action<string> OnCommandReceived;
        public event Action<string, object> OnToolExecuted;
        
        // Internal state
        private HttpClient _httpClient;
        private OpenClawConfig _config;
        private OpenClawTools _tools;
        private OpenClawLogger _logger;
        private CancellationTokenSource _cts;
        private bool _isPolling;
        private bool _disposed;
        
        // Main thread execution queue
        private readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();
        
        // Timing
        private DateTime _lastHeartbeat = DateTime.MinValue;
        private DateTime _lastPoll = DateTime.MinValue;
        private const float POLL_INTERVAL_SECONDS = 0.5f;
        private const float HEARTBEAT_INTERVAL_SECONDS = 30f;
        
        private OpenClawConnectionManager()
        {
            try
            {
                _httpClient = new HttpClient();
                _httpClient.Timeout = TimeSpan.FromSeconds(35); // Slightly longer than poll timeout
                _tools = new OpenClawTools(null);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[OpenClaw] Connection manager init error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Initialize the connection manager. Call from Editor startup or Runtime Start.
        /// </summary>
        public void Initialize(OpenClawConfig config, OpenClawLogger logger = null)
        {
            _config = config;
            _logger = logger;
            
            if (_config != null && _config.autoConnect)
            {
                ConnectAsync();
            }
        }
        
        /// <summary>
        /// Update loop - must be called from EditorApplication.update or MonoBehaviour.Update
        /// </summary>
        public void Update()
        {
            // Process main thread queue
            while (_mainThreadQueue.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[OpenClaw] Main thread action failed: {e.Message}");
                }
            }
            
            // Check if we need to reconnect
            if (State == ConnectionState.Disconnected && _config != null && _config.autoConnect)
            {
                var timeSinceLastAttempt = (DateTime.UtcNow - _lastPoll).TotalSeconds;
                if (timeSinceLastAttempt > 5) // Retry every 5 seconds
                {
                    ConnectAsync();
                }
            }
        }
        
        /// <summary>
        /// Queue an action to run on the main thread.
        /// </summary>
        public void RunOnMainThread(Action action)
        {
            if (action != null)
            {
                _mainThreadQueue.Enqueue(action);
            }
        }
        
        /// <summary>
        /// Connect to the OpenClaw gateway.
        /// </summary>
        public async void ConnectAsync()
        {
            if (State == ConnectionState.Connecting || State == ConnectionState.Connected)
                return;
            
            if (_config == null)
            {
                _config = OpenClawConfig.Instance;
            }
            
            if (_config == null)
            {
                Debug.LogWarning("[OpenClaw] No config found. Please create one via Window > OpenClaw Plugin.");
                return;
            }
            
            await Connect();
        }
        
        /// <summary>
        /// Quick reconnect - for Play mode transitions.
        /// Attempts to reconnect immediately without delay.
        /// </summary>
        public async void QuickReconnect()
        {
            if (State == ConnectionState.Connected) return;
            
            if (_config == null)
            {
                _config = OpenClawConfig.Instance;
            }
            
            if (_config == null) return;
            
            Debug.Log("[OpenClaw] Quick reconnecting...");
            await Connect();
        }
        
        private async Task Connect()
        {
            SetState(ConnectionState.Connecting);
            _cts = new CancellationTokenSource();
            
            try
            {
                Debug.Log($"[OpenClaw] Connecting to {_config.gatewayUrl}...");
                
                var registerData = new Dictionary<string, object>
                {
                    { "type", "unity" },
                    { "version", Application.unityVersion },
                    { "project", Application.productName },
                    { "platform", GetPlatformName() },
                    { "isEditor", Application.isEditor },
                    { "isPlaying", Application.isPlaying },
                    { "tools", _tools.GetToolList() }
                };
                
                var json = DictionaryToJson(registerData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(
                    GetFullUrl("unity/register"), 
                    content, 
                    _cts.Token
                );
                
                if (response.IsSuccessStatusCode)
                {
                    var responseText = await response.Content.ReadAsStringAsync();
                    var result = ParseJson(responseText);
                    
                    if (result.TryGetValue("sessionId", out var sessionId))
                    {
                        SessionId = sessionId?.ToString();
                        SetState(ConnectionState.Connected);
                        _lastHeartbeat = DateTime.UtcNow;
                        Debug.Log($"[OpenClaw] Connected! Session: {SessionId}");
                        
                        // Start polling in background
                        StartPolling();
                    }
                    else
                    {
                        LastError = "No session ID in response";
                        SetState(ConnectionState.Error);
                    }
                }
                else
                {
                    LastError = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                    SetState(ConnectionState.Error);
                    Debug.LogWarning($"[OpenClaw] Connection failed: {LastError}");
                }
            }
            catch (TaskCanceledException)
            {
                // Cancelled, ignore
                SetState(ConnectionState.Disconnected);
            }
            catch (Exception e)
            {
                LastError = e.Message;
                SetState(ConnectionState.Error);
                Debug.LogWarning($"[OpenClaw] Connection error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Disconnect from the gateway.
        /// </summary>
        public void Disconnect()
        {
            _cts?.Cancel();
            _isPolling = false;
            SessionId = null;
            SetState(ConnectionState.Disconnected);
            Debug.Log("[OpenClaw] Disconnected");
        }
        
        private async void StartPolling()
        {
            if (_isPolling) return;
            _isPolling = true;
            
            while (_isPolling && !_disposed && State == ConnectionState.Connected)
            {
                try
                {
                    await PollForCommands();
                    await SendHeartbeatIfNeeded();
                    
                    // Small delay between polls
                    await Task.Delay(TimeSpan.FromSeconds(POLL_INTERVAL_SECONDS), _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[OpenClaw] Polling error: {e.Message}");
                    
                    // On error, try to reconnect after a delay
                    if (State == ConnectionState.Connected)
                    {
                        SetState(ConnectionState.Reconnecting);
                        await Task.Delay(2000, _cts.Token);
                        if (_isPolling && !_disposed)
                        {
                            await Connect();
                        }
                    }
                }
            }
            
            _isPolling = false;
        }
        
        private static int _pollCount = 0;
        private static double _lastPollDiagLog = 0;
        
        private async Task PollForCommands()
        {
            if (string.IsNullOrEmpty(SessionId)) return;
            
            _pollCount++;
            _lastPoll = DateTime.UtcNow;
            
            // Log poll count every 20 polls (about 10 seconds)
            if (_pollCount % 20 == 0)
            {
                Debug.Log($"[OpenClaw Diag] Poll count: {_pollCount}, Session: {SessionId?.Substring(0, Math.Min(20, SessionId.Length))}...");
            }
            
            var response = await _httpClient.GetAsync(
                GetFullUrl($"unity/poll?sessionId={SessionId}"),
                _cts.Token
            );
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Session expired, reconnect
                Debug.LogWarning("[OpenClaw] Session expired, reconnecting...");
                SetState(ConnectionState.Reconnecting);
                await Task.Delay(1000);
                await Connect();
                return;
            }
            
            if (response.IsSuccessStatusCode)
            {
                var responseText = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseText) && responseText != "{}" && responseText != "null")
                {
                    Debug.Log($"[OpenClaw Diag] Received poll response: {responseText.Substring(0, Math.Min(100, responseText.Length))}...");
                    ProcessCommand(responseText);
                }
            }
            else
            {
                Debug.LogWarning($"[OpenClaw Diag] Poll failed with status: {response.StatusCode}");
            }
        }
        
        private async Task SendHeartbeatIfNeeded()
        {
            if (string.IsNullOrEmpty(SessionId)) return;
            
            var timeSinceHeartbeat = (DateTime.UtcNow - _lastHeartbeat).TotalSeconds;
            if (timeSinceHeartbeat < HEARTBEAT_INTERVAL_SECONDS) return;
            
            _lastHeartbeat = DateTime.UtcNow;
            
            try
            {
                var data = new Dictionary<string, object>
                {
                    { "sessionId", SessionId },
                    { "status", "alive" },
                    { "isPlaying", Application.isPlaying },
                    { "time", Time.realtimeSinceStartup }
                };
                
                var json = DictionaryToJson(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                await _httpClient.PostAsync(GetFullUrl("unity/heartbeat"), content, _cts.Token);
            }
            catch { /* Ignore heartbeat errors */ }
        }
        
        private void ProcessCommand(string json)
        {
            try
            {
                Debug.Log($"[OpenClaw Diag] ProcessCommand called, json length: {json?.Length ?? 0}");
                
                var command = ParseJson(json);
                var tool = command.TryGetValue("tool", out var t) ? t?.ToString() : null;
                
                // Support both "requestId" and "toolCallId" (gateway extension uses toolCallId)
                var requestId = command.TryGetValue("requestId", out var r) ? r?.ToString() : null;
                if (string.IsNullOrEmpty(requestId))
                {
                    requestId = command.TryGetValue("toolCallId", out var tc) ? tc?.ToString() : null;
                }
                
                // Handle parameters/arguments - support both field names (gateway uses "arguments")
                string parameters = "{}";
                object paramValue = null;
                if (command.TryGetValue("parameters", out var p) && p != null)
                {
                    paramValue = p;
                }
                else if (command.TryGetValue("arguments", out var a) && a != null)
                {
                    paramValue = a;
                }
                
                if (paramValue != null)
                {
                    if (paramValue is string ps)
                    {
                        parameters = ps;
                    }
                    else if (paramValue is Dictionary<string, object> pd)
                    {
                        parameters = DictionaryToJson(pd);
                    }
                    else
                    {
                        parameters = paramValue.ToString();
                    }
                }
                
                Debug.Log($"[OpenClaw] Received command: {tool}, requestId: {requestId}");
                OnCommandReceived?.Invoke(json);
                
                // Execute on main thread
                Debug.Log($"[OpenClaw Diag] Queueing tool for main thread: {tool}");
                RunOnMainThread(() => ExecuteCommand(tool, parameters, requestId));
            }
            catch (Exception e)
            {
                Debug.LogError($"[OpenClaw] Failed to process command: {e.Message}\n{e.StackTrace}");
            }
        }
        
        private async void ExecuteCommand(string tool, string parameters, string requestId)
        {
            Debug.Log($"[OpenClaw Diag] ExecuteCommand START: {tool}, requestId: {requestId}");
            
            object result = null;
            string error = null;
            
            try
            {
                Debug.Log($"[OpenClaw Diag] Calling _tools.Execute for: {tool}");
                result = _tools.Execute(tool, parameters);
                Debug.Log($"[OpenClaw Diag] Tool execution completed: {tool}, result type: {result?.GetType().Name ?? "null"}");
            }
            catch (Exception e)
            {
                error = e.Message;
                Debug.LogError($"[OpenClaw] Tool execution failed: {e}\n{e.StackTrace}");
            }
            
            OnToolExecuted?.Invoke(tool, result);
            
            // Send result back
            Debug.Log($"[OpenClaw Diag] Sending result for: {tool}, requestId: {requestId}");
            await SendResult(requestId, tool, result, error);
            Debug.Log($"[OpenClaw Diag] ExecuteCommand END: {tool}");
        }
        
        private async Task SendResult(string requestId, string tool, object result, string error)
        {
            try
            {
                var responseData = new Dictionary<string, object>
                {
                    { "sessionId", SessionId },
                    { "toolCallId", requestId }, // Use toolCallId for gateway extension compatibility
                    { "requestId", requestId },  // Also include requestId for backwards compatibility
                    { "tool", tool },
                    { "success", error == null },
                    { "result", result },
                    { "error", error }
                };
                
                var json = DictionaryToJson(responseData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                await _httpClient.PostAsync(GetFullUrl("unity/result"), content, _cts.Token);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OpenClaw] Failed to send result: {e.Message}");
            }
        }
        
        /// <summary>
        /// Send a message to the gateway.
        /// </summary>
        public async Task SendMessageAsync(string message)
        {
            if (!IsConnected) return;
            
            try
            {
                var data = new Dictionary<string, object>
                {
                    { "sessionId", SessionId },
                    { "message", message }
                };
                
                var json = DictionaryToJson(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                await _httpClient.PostAsync(GetFullUrl("unity/message"), content, _cts.Token);
            }
            catch (Exception e)
            {
                Debug.LogError($"[OpenClaw] Failed to send message: {e.Message}");
            }
        }
        
        private void SetState(ConnectionState newState)
        {
            if (State == newState) return;
            State = newState;
            
            // Notify on main thread
            RunOnMainThread(() => OnStateChanged?.Invoke(newState));
        }
        
        private string GetFullUrl(string endpoint)
        {
            if (_config == null) return endpoint;
            return _config.GetFullUrl(endpoint);
        }
        
        private string GetPlatformName()
        {
            #if UNITY_EDITOR
            return "Editor";
            #else
            return Application.platform.ToString();
            #endif
        }
        
        public OpenClawLogger Logger => _logger;
        public OpenClawTools Tools => _tools;
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            
            Disconnect();
            _httpClient?.Dispose();
            _cts?.Dispose();
            
            lock (_lock)
            {
                if (_instance == this)
                {
                    _instance = null;
                }
            }
        }
        
        #region JSON Helpers
        
        private string DictionaryToJson(Dictionary<string, object> dict)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (var kvp in dict)
            {
                if (!first) sb.Append(",");
                first = false;
                sb.Append($"\"{kvp.Key}\":");
                AppendValue(sb, kvp.Value);
            }
            sb.Append("}");
            return sb.ToString();
        }
        
        private void AppendValue(StringBuilder sb, object value)
        {
            if (value == null)
            {
                sb.Append("null");
            }
            else if (value is string s)
            {
                sb.Append($"\"{EscapeString(s)}\"");
            }
            else if (value is bool b)
            {
                sb.Append(b ? "true" : "false");
            }
            else if (value is int || value is float || value is double || value is long)
            {
                sb.Append(value);
            }
            else if (value is Dictionary<string, object> dict)
            {
                sb.Append(DictionaryToJson(dict));
            }
            else if (value is System.Collections.IList ilist)
            {
                // Handle any IList including List<Dictionary<...>>
                sb.Append("[");
                bool first = true;
                foreach (var item in ilist)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    AppendValue(sb, item);
                }
                sb.Append("]");
            }
            else if (value is System.Collections.IDictionary idict)
            {
                // Handle any IDictionary
                sb.Append("{");
                bool first = true;
                foreach (System.Collections.DictionaryEntry entry in idict)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    sb.Append($"\"{EscapeString(entry.Key.ToString())}\":");
                    AppendValue(sb, entry.Value);
                }
                sb.Append("}");
            }
            else
            {
                // Handle anonymous types and other objects via reflection
                var type = value.GetType();
                
                // Check if it's an anonymous type or has properties to serialize
                var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (props.Length > 0 && (type.Name.Contains("AnonymousType") || type.IsClass))
                {
                    sb.Append("{");
                    bool first = true;
                    foreach (var prop in props)
                    {
                        try
                        {
                            var propValue = prop.GetValue(value);
                            if (!first) sb.Append(",");
                            first = false;
                            sb.Append($"\"{prop.Name}\":");
                            AppendValue(sb, propValue);
                        }
                        catch
                        {
                            // Skip properties that can't be read
                        }
                    }
                    sb.Append("}");
                }
                else
                {
                    sb.Append($"\"{EscapeString(value.ToString())}\"");
                }
            }
        }
        
        private string EscapeString(string s)
        {
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }
        
        private string UnescapeString(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            
            return s.Replace("\\\"", "\"")
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t")
                    .Replace("\\\\", "\\");
        }
        
        private Dictionary<string, object> ParseJson(string json)
        {
            var result = new Dictionary<string, object>();
            if (string.IsNullOrEmpty(json)) return result;
            
            json = json.Trim();
            if (!json.StartsWith("{")) return result;
            
            json = json.Substring(1, json.Length - 2);
            
            var parts = SplitJsonParts(json);
            foreach (var part in parts)
            {
                var colonIndex = part.IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = part.Substring(0, colonIndex).Trim().Trim('"');
                    var value = part.Substring(colonIndex + 1).Trim();
                    
                    if (value.StartsWith("\"") && value.EndsWith("\""))
                        result[key] = UnescapeString(value.Substring(1, value.Length - 2));
                    else if (value == "true")
                        result[key] = true;
                    else if (value == "false")
                        result[key] = false;
                    else if (value == "null")
                        result[key] = null;
                    else if (value.StartsWith("{") && value.EndsWith("}"))
                        result[key] = ParseJson(value); // Recursively parse nested objects
                    else if (float.TryParse(value, out var f))
                        result[key] = f;
                    else
                        result[key] = value;
                }
            }
            
            return result;
        }
        
        private List<string> SplitJsonParts(string json)
        {
            var parts = new List<string>();
            int depth = 0;
            int start = 0;
            bool inString = false;
            
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                
                if (c == '"' && (i == 0 || json[i - 1] != '\\'))
                {
                    inString = !inString;
                }
                else if (!inString)
                {
                    if (c == '{' || c == '[') depth++;
                    else if (c == '}' || c == ']') depth--;
                    else if (c == ',' && depth == 0)
                    {
                        parts.Add(json.Substring(start, i - start));
                        start = i + 1;
                    }
                }
            }
            
            if (start < json.Length)
            {
                parts.Add(json.Substring(start));
            }
            
            return parts;
        }
        
        #endregion
    }
}
