/*
 * OpenClaw Unity Plugin
 * https://github.com/TomLeeLive/openclaw-unity-plugin
 * MIT License
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace OpenClaw.Unity
{
    /// <summary>
    /// Main bridge between Unity and OpenClaw AI assistant.
    /// Handles HTTP communication and tool execution.
    /// </summary>
    public class OpenClawBridge : MonoBehaviour
    {
        public static OpenClawBridge Instance { get; private set; }
        
        public enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected,
            Error
        }
        
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public string LastError { get; private set; }
        public bool IsConnected => State == ConnectionState.Connected;
        
        public event Action<ConnectionState> OnStateChanged;
        public event Action<string> OnMessageReceived;
        public event Action<string, object> OnToolExecuted;
        
        private OpenClawConfig _config;
        private OpenClawLogger _logger;
        private OpenClawTools _tools;
        private Coroutine _heartbeatCoroutine;
        private Coroutine _pollCoroutine;
        private string _sessionId;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            _config = OpenClawConfig.Instance;
            _tools = new OpenClawTools(this);
            
            if (_config.captureConsoleLogs)
            {
                _logger = new OpenClawLogger(_config.maxLogEntries, _config.minLogLevel);
                _logger.StartCapture();
            }
        }
        
        private void Start()
        {
            if (_config.autoConnect)
            {
                Connect();
            }
        }
        
        private void OnDestroy()
        {
            Disconnect();
            _logger?.Dispose();
            
            if (Instance == this)
                Instance = null;
        }
        
        public void Connect()
        {
            if (State == ConnectionState.Connecting || State == ConnectionState.Connected)
                return;
                
            StartCoroutine(ConnectCoroutine());
        }
        
        public void Disconnect()
        {
            if (_heartbeatCoroutine != null)
            {
                StopCoroutine(_heartbeatCoroutine);
                _heartbeatCoroutine = null;
            }
            
            if (_pollCoroutine != null)
            {
                StopCoroutine(_pollCoroutine);
                _pollCoroutine = null;
            }
            
            SetState(ConnectionState.Disconnected);
            _sessionId = null;
        }
        
        /// <summary>
        /// Schedule an action to run after a delay (for input simulation timing)
        /// </summary>
        public void ScheduleAction(float delay, Action action)
        {
            if (action == null) return;
            StartCoroutine(ScheduleActionCoroutine(delay, action));
        }
        
        private IEnumerator ScheduleActionCoroutine(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
        
        private IEnumerator ConnectCoroutine()
        {
            SetState(ConnectionState.Connecting);
            Debug.Log($"[OpenClaw] Connecting to {_config.gatewayUrl}...");
            
            // Register with OpenClaw gateway
            var registerData = new Dictionary<string, object>
            {
                { "type", "unity" },
                { "version", Application.unityVersion },
                { "project", Application.productName },
                { "platform", Application.platform.ToString() },
                { "tools", _tools.GetToolList() }
            };
            
            var request = CreatePostRequest("unity/register", registerData);
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                LastError = request.error;
                SetState(ConnectionState.Error);
                Debug.LogError($"[OpenClaw] Connection failed: {request.error}");
                yield break;
            }
            
            try
            {
                var response = JsonUtility.FromJson<RegisterResponse>(request.downloadHandler.text);
                _sessionId = response.sessionId;
                SetState(ConnectionState.Connected);
                Debug.Log($"[OpenClaw] Connected! Session: {_sessionId}");
                
                // Start heartbeat
                if (_config.heartbeatInterval > 0)
                {
                    _heartbeatCoroutine = StartCoroutine(HeartbeatCoroutine());
                }
                
                // Start polling for commands
                _pollCoroutine = StartCoroutine(PollCoroutine());
            }
            catch (Exception e)
            {
                LastError = e.Message;
                SetState(ConnectionState.Error);
                Debug.LogError($"[OpenClaw] Failed to parse response: {e.Message}");
            }
        }
        
        private IEnumerator HeartbeatCoroutine()
        {
            while (State == ConnectionState.Connected)
            {
                yield return new WaitForSeconds(_config.heartbeatInterval);
                
                var data = new Dictionary<string, object>
                {
                    { "sessionId", _sessionId },
                    { "status", "alive" },
                    { "time", Time.time },
                    { "fps", 1f / Time.deltaTime }
                };
                
                var request = CreatePostRequest("unity/heartbeat", data);
                yield return request.SendWebRequest();
                
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[OpenClaw] Heartbeat failed: {request.error}");
                }
            }
        }
        
        private IEnumerator PollCoroutine()
        {
            while (State == ConnectionState.Connected)
            {
                var request = CreateGetRequest($"unity/poll?sessionId={_sessionId}");
                request.timeout = 30; // Long polling
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = request.downloadHandler.text;
                    if (!string.IsNullOrEmpty(response) && response != "{}" && response != "null")
                    {
                        ProcessCommand(response);
                    }
                }
                else if (request.responseCode == 404)
                {
                    // Session expired or gateway restarted - reconnect
                    Debug.LogWarning("[OpenClaw] Session expired, reconnecting...");
                    Disconnect();
                    yield return new WaitForSeconds(1f);
                    Connect();
                    yield break;
                }
                else if (request.responseCode != 408) // Ignore timeout
                {
                    Debug.LogWarning($"[OpenClaw] Poll failed: {request.error}");
                    yield return new WaitForSeconds(5f);
                }
                
                yield return null; // Small delay between polls
            }
        }
        
        private void ProcessCommand(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<CommandMessage>(json);
                Debug.Log($"[OpenClaw] Received command: {command.tool}");
                
                OnMessageReceived?.Invoke(json);
                
                // Execute tool
                StartCoroutine(ExecuteToolCoroutine(command));
            }
            catch (Exception e)
            {
                Debug.LogError($"[OpenClaw] Failed to process command: {e.Message}");
            }
        }
        
        private IEnumerator ExecuteToolCoroutine(CommandMessage command)
        {
            object result = null;
            string error = null;
            
            try
            {
                result = _tools.Execute(command.tool, command.parameters);
            }
            catch (Exception e)
            {
                error = e.Message;
                Debug.LogError($"[OpenClaw] Tool execution failed: {e}");
            }
            
            // Send result back
            var responseData = new Dictionary<string, object>
            {
                { "sessionId", _sessionId },
                { "requestId", command.requestId },
                { "tool", command.tool },
                { "success", error == null },
                { "result", result },
                { "error", error }
            };
            
            var request = CreatePostRequest("unity/result", responseData);
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[OpenClaw] Failed to send result: {request.error}");
            }
            
            OnToolExecuted?.Invoke(command.tool, result);
        }
        
        public void SendMessage(string message)
        {
            StartCoroutine(SendMessageCoroutine(message));
        }
        
        private IEnumerator SendMessageCoroutine(string message)
        {
            var data = new Dictionary<string, object>
            {
                { "sessionId", _sessionId },
                { "message", message }
            };
            
            var request = CreatePostRequest("unity/message", data);
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[OpenClaw] Failed to send message: {request.error}");
            }
        }
        
        public OpenClawLogger Logger => _logger;
        public OpenClawTools Tools => _tools;
        
        private void SetState(ConnectionState newState)
        {
            if (State == newState) return;
            State = newState;
            OnStateChanged?.Invoke(newState);
        }
        
        private UnityWebRequest CreatePostRequest(string endpoint, Dictionary<string, object> data)
        {
            var url = _config.GetFullUrl(endpoint);
            var json = DictionaryToJson(data);
            
            var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            if (!string.IsNullOrEmpty(_config.apiToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_config.apiToken}");
            }
            
            request.timeout = (int)_config.requestTimeout;
            return request;
        }
        
        private UnityWebRequest CreateGetRequest(string endpoint)
        {
            var url = _config.GetFullUrl(endpoint);
            var request = UnityWebRequest.Get(url);
            
            if (!string.IsNullOrEmpty(_config.apiToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_config.apiToken}");
            }
            
            return request;
        }
        
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
            else if (value is int || value is float || value is double)
            {
                sb.Append(value);
            }
            else if (value is IEnumerable<object> list)
            {
                sb.Append("[");
                bool first = true;
                foreach (var item in list)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    AppendValue(sb, item);
                }
                sb.Append("]");
            }
            else if (value is Dictionary<string, object> dict)
            {
                sb.Append(DictionaryToJson(dict));
            }
            else
            {
                sb.Append($"\"{EscapeString(value.ToString())}\"");
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
        
        [Serializable]
        private class RegisterResponse
        {
            public string sessionId;
            public string status;
        }
        
        [Serializable]
        private class CommandMessage
        {
            public string requestId;
            public string tool;
            public string parameters;
        }
    }
}
