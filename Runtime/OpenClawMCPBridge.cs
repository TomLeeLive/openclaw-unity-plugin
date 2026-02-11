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
        
        private OpenClawMCPBridge()
        {
            _tools = new OpenClawTools(null);
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
                    default:
                        await SendJsonResponse(response, 404, new { error = "Not found" });
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[OpenClaw MCP] Request error: {e.Message}");
                try
                {
                    await SendJsonResponse(response, 500, new { error = e.Message });
                }
                catch { }
            }
        }
        
        private async Task HandleToolRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (request.HttpMethod != "POST")
            {
                await SendJsonResponse(response, 405, new { error = "Method not allowed" });
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
                await SendJsonResponse(response, 400, new { error = "Missing 'tool' field" });
                return;
            }
            
            var tool = toolObj?.ToString();
            var args = data.ContainsKey("arguments") ? data["arguments"] : new Dictionary<string, object>();
            var argsJson = DictionaryToJson(args as Dictionary<string, object> ?? new Dictionary<string, object>());
            
            // Execute on main thread and wait
            object result = null;
            string error = null;
            var done = new ManualResetEventSlim(false);
            
            #if UNITY_EDITOR
            EditorApplication.delayCall += () =>
            {
                try
                {
                    result = _tools.Execute(tool, argsJson);
                }
                catch (Exception e)
                {
                    error = e.Message;
                }
                done.Set();
            };
            #else
            // In play mode, queue for main thread
            OpenClawConnectionManager.Instance?.RunOnMainThread(() =>
            {
                try
                {
                    result = _tools.Execute(tool, argsJson);
                }
                catch (Exception e)
                {
                    error = e.Message;
                }
                done.Set();
            });
            #endif
            
            // Wait for execution (timeout 30s)
            if (!done.Wait(30000))
            {
                await SendJsonResponse(response, 504, new { error = "Execution timeout" });
                return;
            }
            
            if (error != null)
            {
                await SendJsonResponse(response, 200, new { success = false, error = error });
            }
            else
            {
                await SendJsonResponse(response, 200, new { success = true, result = result });
            }
        }
        
        private async Task HandleStatusRequest(HttpListenerResponse response)
        {
            var status = new Dictionary<string, object>
            {
                { "running", true },
                { "port", Port },
                { "unity_version", Application.unityVersion },
                { "project", Application.productName },
                #if UNITY_EDITOR
                { "mode", EditorApplication.isPlaying ? "play" : "edit" },
                #else
                { "mode", "runtime" },
                #endif
            };
            
            await SendJsonResponse(response, 200, status);
        }
        
        private async Task HandleToolsListRequest(HttpListenerResponse response)
        {
            var tools = _tools.GetToolList();
            await SendJsonResponse(response, 200, new { tools = tools, count = tools.Count });
        }
        
        private async Task SendJsonResponse(HttpListenerResponse response, int statusCode, object data)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            
            var json = DictionaryToJson(data);
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
            
            if (obj is Dictionary<string, object> dict)
            {
                var parts = new List<string>();
                foreach (var kv in dict)
                {
                    parts.Add($"\"{kv.Key}\":{DictionaryToJson(kv.Value)}");
                }
                return "{" + string.Join(",", parts) + "}";
            }
            
            if (obj is List<object> list)
            {
                var parts = new List<string>();
                foreach (var item in list)
                {
                    parts.Add(DictionaryToJson(item));
                }
                return "[" + string.Join(",", parts) + "]";
            }
            
            if (obj is List<string> strList)
            {
                var parts = new List<string>();
                foreach (var item in strList)
                {
                    parts.Add($"\"{EscapeJsonString(item)}\"");
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
        static OpenClawMCPBridgeEditor()
        {
            EditorApplication.delayCall += () =>
            {
                // Check if MCP bridge should auto-start
                var config = OpenClawConfig.Load();
                if (config != null && config.enableMCPBridge)
                {
                    OpenClawMCPBridge.Instance.Start(config.mcpBridgePort);
                }
            };
        }
        
        [MenuItem("Window/OpenClaw/Start MCP Bridge")]
        public static void StartMCPBridge()
        {
            var config = OpenClawConfig.Load();
            var port = config?.mcpBridgePort ?? 27182;
            OpenClawMCPBridge.Instance.Start(port);
        }
        
        [MenuItem("Window/OpenClaw/Stop MCP Bridge")]
        public static void StopMCPBridge()
        {
            OpenClawMCPBridge.Instance.Stop();
        }
        
        [MenuItem("Window/OpenClaw/MCP Bridge Status")]
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
