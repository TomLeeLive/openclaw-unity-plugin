/*
 * OpenClaw Unity Plugin - Custom Tools API
 * Allows users to register their own tools
 * https://github.com/TomLeeLive/openclaw-unity-plugin
 * MIT License
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenClaw.Unity
{
    /// <summary>
    /// Custom tool definition for extending OpenClaw functionality.
    /// </summary>
    public class CustomTool
    {
        /// <summary>Tool name (e.g., "myproject.doSomething")</summary>
        public string Name { get; set; }
        
        /// <summary>Tool description for AI</summary>
        public string Description { get; set; }
        
        /// <summary>Execution handler</summary>
        public Func<Dictionary<string, object>, object> Execute { get; set; }
        
        /// <summary>Category for grouping (optional)</summary>
        public string Category { get; set; }
    }
    
    /// <summary>
    /// Registry for custom tools. Use this to extend OpenClaw with project-specific tools.
    /// 
    /// Example:
    /// <code>
    /// // In your game code
    /// OpenClawCustomTools.Register(new CustomTool
    /// {
    ///     Name = "mygame.spawnEnemy",
    ///     Description = "Spawn an enemy at position",
    ///     Execute = (args) => {
    ///         var x = args.TryGetValue("x", out var xv) ? Convert.ToSingle(xv) : 0;
    ///         var y = args.TryGetValue("y", out var yv) ? Convert.ToSingle(yv) : 0;
    ///         var z = args.TryGetValue("z", out var zv) ? Convert.ToSingle(zv) : 0;
    ///         // Your spawn logic here
    ///         return new { success = true, spawned = "Enemy", position = new { x, y, z } };
    ///     }
    /// });
    /// </code>
    /// </summary>
    public static class OpenClawCustomTools
    {
        private static readonly Dictionary<string, CustomTool> _customTools = new Dictionary<string, CustomTool>();
        
        /// <summary>Event fired when a custom tool is registered</summary>
        public static event Action<CustomTool> OnToolRegistered;
        
        /// <summary>Event fired when a custom tool is unregistered</summary>
        public static event Action<string> OnToolUnregistered;
        
        /// <summary>
        /// Register a custom tool.
        /// </summary>
        /// <param name="tool">Tool definition</param>
        public static void Register(CustomTool tool)
        {
            if (tool == null || string.IsNullOrEmpty(tool.Name))
            {
                Debug.LogError("[OpenClaw] Cannot register tool with null or empty name");
                return;
            }
            
            if (tool.Execute == null)
            {
                Debug.LogError($"[OpenClaw] Cannot register tool '{tool.Name}' without Execute handler");
                return;
            }
            
            _customTools[tool.Name] = tool;
            Debug.Log($"[OpenClaw] Custom tool registered: {tool.Name}");
            OnToolRegistered?.Invoke(tool);
        }
        
        /// <summary>
        /// Register a simple custom tool with just name, description, and handler.
        /// </summary>
        public static void Register(string name, string description, Func<Dictionary<string, object>, object> execute)
        {
            Register(new CustomTool
            {
                Name = name,
                Description = description,
                Execute = execute
            });
        }
        
        /// <summary>
        /// Unregister a custom tool.
        /// </summary>
        public static void Unregister(string name)
        {
            if (_customTools.Remove(name))
            {
                Debug.Log($"[OpenClaw] Custom tool unregistered: {name}");
                OnToolUnregistered?.Invoke(name);
            }
        }
        
        /// <summary>
        /// Check if a custom tool is registered.
        /// </summary>
        public static bool IsRegistered(string name)
        {
            return _customTools.ContainsKey(name);
        }
        
        /// <summary>
        /// Get a registered custom tool.
        /// </summary>
        public static CustomTool Get(string name)
        {
            return _customTools.TryGetValue(name, out var tool) ? tool : null;
        }
        
        /// <summary>
        /// Get all registered custom tools.
        /// </summary>
        public static IEnumerable<CustomTool> GetAll()
        {
            return _customTools.Values;
        }
        
        /// <summary>
        /// Get the count of registered custom tools.
        /// </summary>
        public static int Count => _customTools.Count;
        
        /// <summary>
        /// Execute a custom tool by name.
        /// </summary>
        public static object Execute(string name, Dictionary<string, object> parameters)
        {
            if (!_customTools.TryGetValue(name, out var tool))
            {
                return new { success = false, error = $"Custom tool '{name}' not found" };
            }
            
            try
            {
                return tool.Execute(parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OpenClaw] Custom tool '{name}' failed: {ex.Message}");
                return new { success = false, error = ex.Message };
            }
        }
        
        /// <summary>
        /// Clear all registered custom tools.
        /// </summary>
        public static void Clear()
        {
            _customTools.Clear();
            Debug.Log("[OpenClaw] All custom tools cleared");
        }
    }
}
