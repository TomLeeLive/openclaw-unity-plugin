/*
 * OpenClaw Unity Plugin - MCP Bridge
 * Local HTTP server for direct MCP integration
 * Enables Claude Code to connect directly without OpenClaw Gateway
 * https://github.com/TomLeeLive/openclaw-unity-plugin
 * MIT License
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OpenClaw.Unity
{
    /// <summary>
    /// Pending tool execution request for main thread processing.
    /// </summary>
    internal class PendingToolRequest
    {
        public string Tool;
        public string ArgsJson;
        public object Result;
        public string Error;
        public ManualResetEventSlim Done = new ManualResetEventSlim(false);
    }
    
    /// <summary>
    /// MCP Bridge - Local HTTP server for direct tool execution.
    /// Allows MCP clients (like Claude Code) to connect directly to Unity.
    /// </summary>
    public class OpenClawMCPBridge : IDisposable
    {
        private static OpenClawMCPBridge _instance;
        private static readonly object _lock = new object();
        
        public static OpenClawMCPBridge Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new OpenClawMCPBridge();
                    }
                    return _instance;
                }
            }
        }
        
        public bool IsRunning { get; private set; }
        public int Port { get; private set; } = 27182;
        public string LastError { get; private set; }
        
        private HttpListener _listener;
        private CancellationTokenSource _cts;
        private OpenClawTools _tools;
        private bool _disposed;
        
        // Queue for pending tool requests (processed on main thread)
        private readonly Queue<PendingToolRequest> _pendingRequests = new Queue<PendingToolRequest>();
        private readonly object _queueLock = new object();
        
        // Cached values (must be set from main thread)
        private string _unityVersion = "Unknown";
        private string _projectName = "Unknown";
        private string _editorMode = "unknown";
        
        private OpenClawMCPBridge()
        {
            _tools = new OpenClawTools(null);
        }
        
        /// <summary>
        /// Cache Unity values that can only be accessed from main thread.
        /// Call this from main thread before starting the bridge.
        /// </summary>
        public void CacheMainThreadValues()
        {
            _unityVersion = Application.unityVersion;
            _projectName = Application.productName;
            #if UNITY_EDITOR
            _editorMode = EditorApplication.isPlaying ? "play" : "edit";
            #else
            _editorMode = "runtime";
            #endif
        }
        
        /// <summary>
        /// Update editor mode cache. Call from main thread.
        /// </summary>
        public void UpdateEditorMode()
        {
            #if UNITY_EDITOR
            _editorMode = EditorApplication.isPlaying ? "play" : "edit";
            #endif
        }
        
        /// <summary>
        /// Process pending requests on main thread. Called by EditorApplication.update.
        /// </summary>
        public void ProcessPendingRequests()
        {
            PendingToolRequest request = null;
            
            lock (_queueLock)
            {
                if (_pendingRequests.Count > 0)
                {
                    request = _pendingRequests.Dequeue();
                }
            }
            
            if (request != null)
            {
                try
                {
                    request.Result = _tools.Execute(request.Tool, request.ArgsJson);
                }
                catch (Exception e)
                {
                    request.Error = e.Message;
                }
                request.Done.Set();
            }
        }
        
        /// <summary>
        /// Start the MCP bridge HTTP server.
        /// </summary>
        public void Start(int port = 27182)
        {
            if (IsRunning) return;
            
            Port = port;
            
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
                _listener.Prefixes.Add($"http://localhost:{Port}/");
                _listener.Start();
                
                _cts = new CancellationTokenSource();
                IsRunning = true;
                
                // Start listening loop
                Task.Run(() => ListenLoop(_cts.Token));
                
                Debug.Log($"[OpenClaw MCP] Bridge started on port {Port}");
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Debug.LogError($"[OpenClaw MCP] Failed to start bridge: {e.Message}");
                IsRunning = false;
            }
        }
        
        /// <summary>
        /// Stop the MCP bridge HTTP server.
        /// </summary>
        public void Stop()
        {
            if (!IsRunning) return;
            
            try
            {
                _cts?.Cancel();
                _listener?.Stop();
                _listener?.Close();
            }
            catch { }
            
            IsRunning = false;
            Debug.Log("[OpenClaw MCP] Bridge stopped");
        }
        
        private async Task ListenLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && IsRunning)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequest(context));
                }
                catch (HttpListenerException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception e)
                {
                    if (!ct.IsCancellationRequested)
                    {
                        Debug.LogWarning($"[OpenClaw MCP] Listener error: {e.Message}");
                    }
                }
            }
        }
        
        private async Task HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            
            try
            {
                // CORS headers for local development
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                
                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }
                
                var path = request.Url.AbsolutePath.ToLower();
                
                switch (path)
                {
                    case "/tool":
                        await HandleToolRequest(request, response);
                        break;
                    case "/status":
                        await HandleStatusRequest(response);
                        break;
                    case "/tools":
                        await HandleToolsListRequest(response);
                        break;
                    case "/poll":
                        // Gateway compatibility - MCP bridge has no pending requests to return
                        await SendJsonResponse(response, 200, new Dictionary<string, object>());
                        break;
                    default:
                        await SendJsonResponse(response, 404, MakeError("Not found"));
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[OpenClaw MCP] Request error: {e.Message}");
                try
                {
                    await SendJsonResponse(response, 500, MakeError(e.Message));
                }
                catch { }
            }
        }
        
        private static Dictionary<string, object> MakeError(string message)
        {
            return new Dictionary<string, object> { { "error", message } };
        }
        
        private async Task HandleToolRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (request.HttpMethod != "POST")
            {
                await SendJsonResponse(response, 405, MakeError("Method not allowed"));
                return;
            }
            
            // Read body
            string body;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                body = await reader.ReadToEndAsync();
            }
            
            // Parse JSON manually (Unity's JsonUtility has limitations)
            var data = ParseJson(body);
            
            if (!data.TryGetValue("tool", out var toolObj))
            {
                await SendJsonResponse(response, 400, MakeError("Missing 'tool' field"));
                return;
            }
            
            var tool = toolObj?.ToString();
            var args = data.ContainsKey("arguments") ? data["arguments"] : new Dictionary<string, object>();
            var argsJson = DictionaryToJson(args as Dictionary<string, object> ?? new Dictionary<string, object>());
            
            // Create pending request and queue it for main thread
            var pendingRequest = new PendingToolRequest
            {
                Tool = tool,
                ArgsJson = argsJson
            };
            
            lock (_queueLock)
            {
                _pendingRequests.Enqueue(pendingRequest);
            }
            
            // Wait for execution (timeout 30s)
            if (!pendingRequest.Done.Wait(30000))
            {
                await SendJsonResponse(response, 504, MakeError("Execution timeout - Unity may not be responding"));
                return;
            }
            
            if (pendingRequest.Error != null)
            {
                var errorResult = new Dictionary<string, object>
                {
                    { "success", false },
                    { "error", pendingRequest.Error }
                };
                await SendJsonResponse(response, 200, errorResult);
            }
            else
            {
                var successResult = new Dictionary<string, object>
                {
                    { "success", true },
                    { "result", pendingRequest.Result }
                };
                await SendJsonResponse(response, 200, successResult);
            }
        }
        
        private async Task HandleStatusRequest(HttpListenerResponse response)
        {
            var status = new Dictionary<string, object>
            {
                { "running", true },
                { "port", Port },
                { "unity_version", _unityVersion },
                { "project", _projectName },
                { "mode", _editorMode }
            };
            
            await SendJsonResponse(response, 200, status);
        }
        
        private async Task HandleToolsListRequest(HttpListenerResponse response)
        {
            var tools = _tools.GetToolList();
            var result = new Dictionary<string, object>
            {
                { "tools", tools },
                { "count", tools.Count }
            };
            await SendJsonResponse(response, 200, result);
        }
        
        private async Task SendJsonResponse(HttpListenerResponse response, int statusCode, object data)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            
            var json = DictionaryToJson(data);
            Debug.Log($"[OpenClaw MCP] Response ({statusCode}): {json}");
            var buffer = Encoding.UTF8.GetBytes(json);
            
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }
        
        private string DictionaryToJson(object obj)
        {
            if (obj == null) return "null";
            
            if (obj is string s)
                return $"\"{EscapeJsonString(s)}\"";
            
            if (obj is bool b)
                return b ? "true" : "false";
            
            if (obj is int || obj is long || obj is float || obj is double)
                return obj.ToString();
            
            // Check for Dictionary with string keys (any value type)
            var objType = obj.GetType();
            if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var keyType = objType.GetGenericArguments()[0];
                if (keyType == typeof(string))
                {
                    var parts = new List<string>();
                    foreach (System.Collections.DictionaryEntry entry in (System.Collections.IDictionary)obj)
                    {
                        parts.Add($"\"{entry.Key}\":{DictionaryToJson(entry.Value)}");
                    }
                    return "{" + string.Join(",", parts) + "}";
                }
            }
            
            if (obj is Dictionary<string, object> dict)
            {
                var parts = new List<string>();
                foreach (var kv in dict)
                {
                    parts.Add($"\"{kv.Key}\":{DictionaryToJson(kv.Value)}");
                }
                return "{" + string.Join(",", parts) + "}";
            }
            
            // Handle any IList (List<T>, arrays, etc.)
            if (obj is System.Collections.IList ilist)
            {
                var parts = new List<string>();
                foreach (var item in ilist)
                {
                    parts.Add(DictionaryToJson(item));
                }
                return "[" + string.Join(",", parts) + "]";
            }
            
            // Fallback: try JsonUtility
            try
            {
                return JsonUtility.ToJson(obj);
            }
            catch
            {
                return $"\"{EscapeJsonString(obj.ToString())}\"";
            }
        }
        
        private string EscapeJsonString(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }
        
        private Dictionary<string, object> ParseJson(string json)
        {
            var result = new Dictionary<string, object>();
            if (string.IsNullOrEmpty(json)) return result;
            
            json = json.Trim();
            if (!json.StartsWith("{")) return result;
            
            // Simple JSON parser for our needs
            try
            {
                var inner = json.Substring(1, json.Length - 2).Trim();
                var depth = 0;
                var inString = false;
                var start = 0;
                var parts = new List<string>();
                
                for (int i = 0; i < inner.Length; i++)
                {
                    var c = inner[i];
                    
                    if (c == '"' && (i == 0 || inner[i-1] != '\\'))
                        inString = !inString;
                    
                    if (!inString)
                    {
                        if (c == '{' || c == '[') depth++;
                        else if (c == '}' || c == ']') depth--;
                        else if (c == ',' && depth == 0)
                        {
                            parts.Add(inner.Substring(start, i - start).Trim());
                            start = i + 1;
                        }
                    }
                }
                
                if (start < inner.Length)
                    parts.Add(inner.Substring(start).Trim());
                
                foreach (var part in parts)
                {
                    var colonIdx = part.IndexOf(':');
                    if (colonIdx < 0) continue;
                    
                    var key = part.Substring(0, colonIdx).Trim().Trim('"');
                    var value = part.Substring(colonIdx + 1).Trim();
                    
                    result[key] = ParseJsonValue(value);
                }
            }
            catch { }
            
            return result;
        }
        
        private object ParseJsonValue(string value)
        {
            if (value == "null") return null;
            if (value == "true") return true;
            if (value == "false") return false;
            if (value.StartsWith("\"") && value.EndsWith("\""))
                return value.Substring(1, value.Length - 2);
            if (value.StartsWith("{"))
                return ParseJson(value);
            if (int.TryParse(value, out var i)) return i;
            if (double.TryParse(value, out var d)) return d;
            return value;
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Editor integration for MCP Bridge
    /// </summary>
    [InitializeOnLoad]
    public static class OpenClawMCPBridgeEditor
    {
        private static bool _updateHooked = false;
        
        static OpenClawMCPBridgeEditor()
        {
            EditorApplication.delayCall += () =>
            {
                // Check if MCP bridge should auto-start
                var config = OpenClawConfig.Load();
                if (config != null && config.enableMCPBridge)
                {
                    StartMCPBridge();
                }
            };
        }
        
        private static void OnEditorUpdate()
        {
            // Process pending MCP requests on main thread
            if (OpenClawMCPBridge.Instance.IsRunning)
            {
                OpenClawMCPBridge.Instance.ProcessPendingRequests();
                OpenClawMCPBridge.Instance.UpdateEditorMode();
            }
        }
        
        [MenuItem("Window/OpenClaw Plugin/MCP Bridge/Start", false, 20)]
        public static void StartMCPBridge()
        {
            var config = OpenClawConfig.Load();
            var port = config?.mcpBridgePort ?? 27182;
            
            // Cache main-thread-only values before starting
            OpenClawMCPBridge.Instance.CacheMainThreadValues();
            OpenClawMCPBridge.Instance.Start(port);
            
            // Hook into update loop to process requests on main thread
            if (!_updateHooked)
            {
                EditorApplication.update += OnEditorUpdate;
                _updateHooked = true;
            }
        }
        
        [MenuItem("Window/OpenClaw Plugin/MCP Bridge/Stop", false, 21)]
        public static void StopMCPBridge()
        {
            OpenClawMCPBridge.Instance.Stop();
            
            // Unhook update loop
            if (_updateHooked)
            {
                EditorApplication.update -= OnEditorUpdate;
                _updateHooked = false;
            }
        }
        
        [MenuItem("Window/OpenClaw Plugin/MCP Bridge/Status", false, 22)]
        public static void MCPBridgeStatus()
        {
            var bridge = OpenClawMCPBridge.Instance;
            if (bridge.IsRunning)
            {
                Debug.Log($"[OpenClaw MCP] Bridge running on port {bridge.Port}");
                Debug.Log($"[OpenClaw MCP] Connect MCP clients to: http://127.0.0.1:{bridge.Port}");
            }
            else
            {
                Debug.Log("[OpenClaw MCP] Bridge is not running");
            }
        }
    }
    #endif
}
