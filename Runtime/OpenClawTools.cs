/*
 * OpenClaw Unity Plugin
 * https://github.com/TomLeeLive/openclaw-unity-plugin
 * MIT License
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenClaw.Unity
{
    /// <summary>
    /// Tool implementations for OpenClaw AI to interact with Unity.
    /// </summary>
    public class OpenClawTools
    {
        private readonly OpenClawBridge _bridge;
        private readonly Dictionary<string, Func<Dictionary<string, object>, object>> _tools;
        
        public OpenClawTools(OpenClawBridge bridge)
        {
            _bridge = bridge;
            _tools = new Dictionary<string, Func<Dictionary<string, object>, object>>
            {
                // Console
                { "console.getLogs", ConsoleGetLogs },
                { "console.clear", ConsoleClear },
                { "console.getErrors", ConsoleGetErrors },
                
                // Scene
                { "scene.list", SceneList },
                { "scene.getActive", SceneGetActive },
                { "scene.getData", SceneGetData },
                { "scene.load", SceneLoad },
                { "scene.open", SceneOpen }, // Editor mode scene open
                { "scene.save", SceneSave }, // Editor mode scene save
                { "scene.saveAll", SceneSaveAll }, // Editor mode save all scenes
                
                // GameObject
                { "gameobject.find", GameObjectFind },
                { "gameobject.getAll", GameObjectGetAll },
                { "gameobject.create", GameObjectCreate },
                { "gameobject.destroy", GameObjectDestroy },
                { "gameobject.delete", GameObjectDestroy }, // Alias for destroy
                { "gameobject.getData", GameObjectGetData },
                { "gameobject.setActive", GameObjectSetActive },
                { "gameobject.setParent", GameObjectSetParent },
                
                // Transform
                { "transform.getPosition", TransformGetPosition },
                { "transform.getRotation", TransformGetRotation },
                { "transform.getScale", TransformGetScale },
                { "transform.setPosition", TransformSetPosition },
                { "transform.setRotation", TransformSetRotation },
                { "transform.setScale", TransformSetScale },
                
                // Component
                { "component.add", ComponentAdd },
                { "component.remove", ComponentRemove },
                { "component.get", ComponentGet },
                { "component.set", ComponentSet },
                { "component.list", ComponentList },
                
                // Script
                { "script.execute", ScriptExecute },
                { "script.read", ScriptRead },
                { "script.list", ScriptList },
                
                // Application
                { "app.getState", AppGetState },
                { "app.play", AppPlay },
                { "app.pause", AppPause },
                { "app.stop", AppStop },
                
                // Debug
                { "debug.log", DebugLog },
                { "debug.screenshot", DebugScreenshot },
                { "debug.hierarchy", DebugHierarchy },
                
                // Editor (Unity Editor control)
                { "editor.refresh", EditorRefresh },
                { "editor.recompile", EditorRecompile },
                { "editor.domainReload", EditorDomainReload },
                { "editor.focusWindow", EditorFocusWindow },
                { "editor.listWindows", EditorListWindows },
                { "editor.getState", AppGetState }, // Alias for MCP compatibility
                { "editor.play", AppPlay },         // Alias for MCP compatibility
                { "editor.stop", AppStop },         // Alias for MCP compatibility
                { "editor.pause", AppPause },       // Alias for MCP compatibility
                { "editor.unpause", AppPlay },      // Alias for MCP compatibility (resume = play)
                
                // Material
                { "material.create", MaterialCreate },
                { "material.assign", MaterialAssign },
                { "material.modify", MaterialModify },
                { "material.getInfo", MaterialGetInfo },
                { "material.list", MaterialList },
                
                // Prefab
                { "prefab.create", PrefabCreate },
                { "prefab.instantiate", PrefabInstantiate },
                { "prefab.open", PrefabOpen },
                { "prefab.close", PrefabClose },
                { "prefab.save", PrefabSave },
                
                // Asset
                { "asset.find", AssetFind },
                { "asset.copy", AssetCopy },
                { "asset.move", AssetMove },
                { "asset.delete", AssetDelete },
                { "asset.refresh", AssetRefresh },
                { "asset.import", AssetImport },
                { "asset.getPath", AssetGetPath },
                
                // Package Manager
                { "package.add", PackageAdd },
                { "package.remove", PackageRemove },
                { "package.list", PackageList },
                { "package.search", PackageSearch },
                
                // Test Runner
                { "test.run", TestRun },
                { "test.list", TestList },
                { "test.getResults", TestGetResults },
                
                // Input (for game testing)
                { "input.keyPress", InputKeyPress },
                { "input.keyDown", InputKeyDown },
                { "input.keyUp", InputKeyUp },
                { "input.type", InputType },
                { "input.mouseMove", InputMouseMove },
                { "input.mouseClick", InputMouseClick },
                { "input.mouseDrag", InputMouseDrag },
                { "input.mouseScroll", InputMouseScroll },
                { "input.getMousePosition", InputGetMousePosition },
                { "input.clickUI", InputClickUI },
                { "input.click", InputClickUI }, // Alias for clickUI
            };
        }
        
        public List<Dictionary<string, object>> GetToolList()
        {
            return _tools.Keys.Select(name => new Dictionary<string, object>
            {
                { "name", name },
                { "description", GetToolDescription(name) }
            }).ToList();
        }
        
        public object Execute(string toolName, string parametersJson)
        {
            if (!_tools.TryGetValue(toolName, out var tool))
            {
                throw new ArgumentException($"Unknown tool: {toolName}");
            }
            
            var parameters = ParseJson(parametersJson);
            return tool(parameters);
        }
        
        #region Console Tools
        
        private object ConsoleGetLogs(Dictionary<string, object> p)
        {
            var count = GetInt(p, "count", 100);
            var type = GetString(p, "type", null);
            
            // Check if logger is available
            if (_bridge?.Logger == null)
            {
                return new { 
                    success = false, 
                    error = "Logger not initialized. Check captureConsoleLogs in OpenClaw Config.",
                    logs = new string[0]
                };
            }
            
            LogType? filterType = null;
            if (!string.IsNullOrEmpty(type))
            {
                filterType = type.ToLower() switch
                {
                    "error" => LogType.Error,
                    "warning" => LogType.Warning,
                    "log" => LogType.Log,
                    "exception" => LogType.Exception,
                    _ => null
                };
            }
            
            try
            {
                var logs = _bridge.Logger.GetLogsJson(count, filterType);
                return new { success = true, logs = logs ?? "[]" };
            }
            catch (System.Exception ex)
            {
                return new { success = false, error = ex.Message, logs = "[]" };
            }
        }
        
        private object ConsoleClear(Dictionary<string, object> p)
        {
            _bridge.Logger?.Clear();
            return new { success = true };
        }
        
        private object ConsoleGetErrors(Dictionary<string, object> p)
        {
            var count = GetInt(p, "count", 50);
            var includeWarnings = GetBool(p, "includeWarnings", false);
            
            var errors = new List<Dictionary<string, object>>();
            
            // Try to get from logger first
            if (_bridge?.Logger != null)
            {
                try
                {
                    var logs = _bridge.Logger.GetLogs(count * 2); // Get more to filter
                    foreach (var log in logs)
                    {
                        if (log.type == LogType.Error || log.type == LogType.Exception ||
                            (includeWarnings && log.type == LogType.Warning))
                        {
                            errors.Add(new Dictionary<string, object>
                            {
                                { "time", log.timestamp.ToString("HH:mm:ss.fff") },
                                { "type", log.type.ToString() },
                                { "message", log.message },
                                { "stackTrace", log.stackTrace }
                            });
                            if (errors.Count >= count) break;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    return new { success = false, error = ex.Message, errors = new object[0] };
                }
            }
            
            // If no errors from logger, try reading recent compile errors
            if (errors.Count == 0)
            {
                #if UNITY_EDITOR
                try
                {
                    // Check for compile errors
                    var hasCompileErrors = UnityEditor.EditorUtility.scriptCompilationFailed;
                    if (hasCompileErrors)
                    {
                        errors.Add(new Dictionary<string, object>
                        {
                            { "time", System.DateTime.Now.ToString("HH:mm:ss") },
                            { "type", "CompileError" },
                            { "message", "Script compilation failed. Check Unity Console for details." },
                            { "stackTrace", "" }
                        });
                    }
                }
                catch { }
                #endif
            }
            
            return new { 
                success = true, 
                count = errors.Count,
                errors = errors 
            };
        }
        
        #endregion
        
        #region Scene Tools
        
        private object SceneList(Dictionary<string, object> p)
        {
            var scenes = new List<Dictionary<string, object>>();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                scenes.Add(new Dictionary<string, object>
                {
                    { "index", i },
                    { "path", path },
                    { "name", System.IO.Path.GetFileNameWithoutExtension(path) }
                });
            }
            return scenes;
        }
        
        private object SceneGetActive(Dictionary<string, object> p)
        {
            var scene = SceneManager.GetActiveScene();
            return new Dictionary<string, object>
            {
                { "name", scene.name },
                { "path", scene.path },
                { "buildIndex", scene.buildIndex },
                { "isLoaded", scene.isLoaded },
                { "rootCount", scene.rootCount }
            };
        }
        
        private object SceneGetData(Dictionary<string, object> p)
        {
            var sceneName = GetString(p, "name", null);
            Scene scene;
            
            if (string.IsNullOrEmpty(sceneName))
            {
                scene = SceneManager.GetActiveScene();
            }
            else
            {
                scene = SceneManager.GetSceneByName(sceneName);
            }
            
            var rootObjects = scene.GetRootGameObjects();
            var objects = new List<Dictionary<string, object>>();
            
            foreach (var go in rootObjects)
            {
                objects.Add(GetGameObjectData(go, GetInt(p, "depth", 2)));
            }
            
            return new Dictionary<string, object>
            {
                { "name", scene.name },
                { "rootObjects", objects }
            };
        }
        
        private object SceneLoad(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", null);
            var mode = GetString(p, "mode", "Single");
            
            var loadMode = mode.ToLower() == "additive" 
                ? LoadSceneMode.Additive 
                : LoadSceneMode.Single;
            
            SceneManager.LoadScene(name, loadMode);
            return new { success = true, scene = name };
        }
        
        private object SceneOpen(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var name = GetString(p, "name", null);
            var path = GetString(p, "path", null);
            
            // Find scene path
            string scenePath = null;
            
            if (!string.IsNullOrEmpty(path))
            {
                scenePath = path;
            }
            else if (!string.IsNullOrEmpty(name))
            {
                // Search in build settings
                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    var sp = SceneUtility.GetScenePathByBuildIndex(i);
                    if (System.IO.Path.GetFileNameWithoutExtension(sp) == name)
                    {
                        scenePath = sp;
                        break;
                    }
                }
                
                // Search in Assets if not found
                if (string.IsNullOrEmpty(scenePath))
                {
                    var guids = UnityEditor.AssetDatabase.FindAssets($"t:Scene {name}");
                    if (guids.Length > 0)
                    {
                        scenePath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    }
                }
            }
            
            if (string.IsNullOrEmpty(scenePath))
            {
                return new { success = false, error = $"Scene not found: {name ?? path}" };
            }
            
            var mode = GetString(p, "mode", "Single");
            var openMode = mode.ToLower() == "additive" 
                ? UnityEditor.SceneManagement.OpenSceneMode.Additive 
                : UnityEditor.SceneManagement.OpenSceneMode.Single;
            
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, openMode);
            return new { success = true, scene = scenePath };
            #else
            return new { success = false, error = "scene.open is only available in Editor. Use scene.load in Play mode." };
            #endif
        }
        
        private object SceneSave(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return new { success = false, error = "No active scene" };
            
            if (string.IsNullOrEmpty(scene.path))
                return new { success = false, error = "Scene has no path. Use scene.saveAs with a path." };
            
            bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            return new { success = saved, scene = scene.name, path = scene.path };
            #else
            return new { success = false, error = "scene.save is only available in Editor mode." };
            #endif
        }
        
        private object SceneSaveAll(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            return new { success = saved, message = "All open scenes saved" };
            #else
            return new { success = false, error = "scene.saveAll is only available in Editor mode." };
            #endif
        }
        
        #endregion
        
        #region GameObject Tools
        
        private object GameObjectFind(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", null);
            var tag = GetString(p, "tag", null);
            var type = GetString(p, "type", null);
            
            GameObject[] results;
            
            if (!string.IsNullOrEmpty(name))
            {
                var go = GameObject.Find(name);
                results = go != null ? new[] { go } : new GameObject[0];
            }
            else if (!string.IsNullOrEmpty(tag))
            {
                results = GameObject.FindGameObjectsWithTag(tag);
            }
            else if (!string.IsNullOrEmpty(type))
            {
                var componentType = FindType(type);
                if (componentType != null)
                {
                    var components = UnityEngine.Object.FindObjectsByType(componentType, FindObjectsSortMode.None);
                    results = components.Select(c => (c as Component)?.gameObject).Where(g => g != null).ToArray();
                }
                else
                {
                    results = new GameObject[0];
                }
            }
            else
            {
                results = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            }
            
            var depth = GetInt(p, "depth", 1);
            return results.Take(100).Select(go => GetGameObjectData(go, depth)).ToList();
        }
        
        private object GameObjectGetAll(Dictionary<string, object> p)
        {
            var activeOnly = GetBool(p, "activeOnly", false);
            var includePosition = GetBool(p, "includePosition", true);
            var maxCount = GetInt(p, "maxCount", 500);
            var rootOnly = GetBool(p, "rootOnly", false);
            var nameFilter = GetString(p, "nameFilter", null);
            
            IEnumerable<GameObject> allObjects;
            
            if (rootOnly)
            {
                // Get only root level objects from active scene
                var scene = SceneManager.GetActiveScene();
                allObjects = scene.GetRootGameObjects();
            }
            else
            {
                // Get all objects including inactive
                allObjects = Resources.FindObjectsOfTypeAll<GameObject>()
                    .Where(go => go.scene.isLoaded); // Filter to scene objects only
            }
            
            // Apply filters
            if (activeOnly)
            {
                allObjects = allObjects.Where(go => go.activeInHierarchy);
            }
            
            if (!string.IsNullOrEmpty(nameFilter))
            {
                allObjects = allObjects.Where(go => go.name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));
            }
            
            var results = allObjects.Take(maxCount).Select(go => {
                var data = new Dictionary<string, object>
                {
                    { "name", go.name },
                    { "active", go.activeInHierarchy },
                    { "tag", go.tag },
                    { "layer", LayerMask.LayerToName(go.layer) }
                };
                
                if (includePosition)
                {
                    var t = go.transform;
                    data["x"] = t.position.x;
                    data["y"] = t.position.y;
                    data["z"] = t.position.z;
                }
                
                // Add parent info
                if (go.transform.parent != null)
                {
                    data["parent"] = go.transform.parent.name;
                }
                
                return data;
            }).ToList();
            
            return new Dictionary<string, object>
            {
                { "success", true },
                { "count", results.Count },
                { "objects", results }
            };
        }
        
        private object GameObjectCreate(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", "New GameObject");
            var primitive = GetString(p, "primitive", null);
            
            GameObject go;
            
            if (!string.IsNullOrEmpty(primitive))
            {
                var type = primitive.ToLower() switch
                {
                    "cube" => PrimitiveType.Cube,
                    "sphere" => PrimitiveType.Sphere,
                    "capsule" => PrimitiveType.Capsule,
                    "cylinder" => PrimitiveType.Cylinder,
                    "plane" => PrimitiveType.Plane,
                    "quad" => PrimitiveType.Quad,
                    _ => PrimitiveType.Cube
                };
                go = GameObject.CreatePrimitive(type);
                go.name = name;
            }
            else
            {
                go = new GameObject(name);
            }
            
            // Set position if provided
            if (p.ContainsKey("position"))
            {
                go.transform.position = ParseVector3(p["position"]);
            }
            
            return GetGameObjectData(go, 1);
        }
        
        private object GameObjectDestroy(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", null);
            var go = GameObject.Find(name);
            
            if (go != null)
            {
                // Use DestroyImmediate in Editor mode (not playing), Destroy in Play mode
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(go);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
                return new { success = true, destroyed = name };
            }
            
            return new { success = false, error = $"GameObject '{name}' not found" };
        }
        
        private object GameObjectGetData(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", null);
            var go = GameObject.Find(name);
            
            if (go == null)
            {
                return new { error = $"GameObject '{name}' not found" };
            }
            
            return GetGameObjectData(go, GetInt(p, "depth", 3));
        }
        
        private object GameObjectSetActive(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", null);
            var active = GetBool(p, "active", true);
            var go = GameObject.Find(name);
            
            if (go != null)
            {
                go.SetActive(active);
                return new { success = true, name = name, active = active };
            }
            
            return new { success = false, error = $"GameObject '{name}' not found" };
        }
        
        private object GameObjectSetParent(Dictionary<string, object> p)
        {
            var childName = GetString(p, "child", null);
            var parentName = GetString(p, "parent", null);
            
            var child = GameObject.Find(childName);
            var parent = string.IsNullOrEmpty(parentName) ? null : GameObject.Find(parentName);
            
            if (child != null)
            {
                child.transform.SetParent(parent?.transform);
                return new { success = true };
            }
            
            return new { success = false, error = $"Child '{childName}' not found" };
        }
        
        private Dictionary<string, object> GetGameObjectData(GameObject go, int depth)
        {
            var data = new Dictionary<string, object>
            {
                { "name", go.name },
                { "tag", go.tag },
                { "layer", LayerMask.LayerToName(go.layer) },
                { "active", go.activeSelf },
                { "position", Vec3ToDict(go.transform.position) },
                { "rotation", Vec3ToDict(go.transform.eulerAngles) },
                { "scale", Vec3ToDict(go.transform.localScale) },
                { "components", go.GetComponents<Component>().Select(c => c?.GetType().Name).Where(n => n != null).ToList() }
            };
            
            if (depth > 0 && go.transform.childCount > 0)
            {
                var children = new List<Dictionary<string, object>>();
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    children.Add(GetGameObjectData(go.transform.GetChild(i).gameObject, depth - 1));
                }
                data["children"] = children;
            }
            
            return data;
        }
        
        #endregion
        
        #region Transform Tools
        
        private object TransformGetPosition(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", null);
            var go = GameObject.Find(name);
            
            if (go != null)
            {
                var pos = go.transform.position;
                return new { success = true, x = pos.x, y = pos.y, z = pos.z };
            }
            
            return new { success = false, error = $"GameObject '{name}' not found" };
        }
        
        private object TransformGetRotation(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", null);
            var go = GameObject.Find(name);
            
            if (go != null)
            {
                var rot = go.transform.eulerAngles;
                return new { success = true, x = rot.x, y = rot.y, z = rot.z };
            }
            
            return new { success = false, error = $"GameObject '{name}' not found" };
        }
        
        private object TransformGetScale(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", null);
            var go = GameObject.Find(name);
            
            if (go != null)
            {
                var scale = go.transform.localScale;
                return new { success = true, x = scale.x, y = scale.y, z = scale.z };
            }
            
            return new { success = false, error = $"GameObject '{name}' not found" };
        }
        
        private object TransformSetPosition(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", null);
            var go = GameObject.Find(name);
            
            if (go != null)
            {
                go.transform.position = new Vector3(
                    GetFloat(p, "x", go.transform.position.x),
                    GetFloat(p, "y", go.transform.position.y),
                    GetFloat(p, "z", go.transform.position.z)
                );
                return new { success = true, position = Vec3ToDict(go.transform.position) };
            }
            
            return new { success = false, error = $"GameObject '{name}' not found" };
        }
        
        private object TransformSetRotation(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", null);
            var go = GameObject.Find(name);
            
            if (go != null)
            {
                go.transform.eulerAngles = new Vector3(
                    GetFloat(p, "x", go.transform.eulerAngles.x),
                    GetFloat(p, "y", go.transform.eulerAngles.y),
                    GetFloat(p, "z", go.transform.eulerAngles.z)
                );
                return new { success = true, rotation = Vec3ToDict(go.transform.eulerAngles) };
            }
            
            return new { success = false, error = $"GameObject '{name}' not found" };
        }
        
        private object TransformSetScale(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", null);
            var go = GameObject.Find(name);
            
            if (go != null)
            {
                go.transform.localScale = new Vector3(
                    GetFloat(p, "x", go.transform.localScale.x),
                    GetFloat(p, "y", go.transform.localScale.y),
                    GetFloat(p, "z", go.transform.localScale.z)
                );
                return new { success = true, scale = Vec3ToDict(go.transform.localScale) };
            }
            
            return new { success = false, error = $"GameObject '{name}' not found" };
        }
        
        #endregion
        
        #region Component Tools
        
        private object ComponentAdd(Dictionary<string, object> p)
        {
            var goName = GetString(p, "gameObject", null);
            var typeName = GetString(p, "type", null);
            
            var go = GameObject.Find(goName);
            if (go == null)
            {
                return new { success = false, error = $"GameObject '{goName}' not found" };
            }
            
            var type = FindType(typeName);
            if (type == null)
            {
                return new { success = false, error = $"Type '{typeName}' not found" };
            }
            
            var component = go.AddComponent(type);
            return new { success = true, component = type.Name };
        }
        
        private object ComponentRemove(Dictionary<string, object> p)
        {
            var goName = GetString(p, "gameObject", null);
            var typeName = GetString(p, "type", null);
            
            var go = GameObject.Find(goName);
            if (go == null)
            {
                return new { success = false, error = $"GameObject '{goName}' not found" };
            }
            
            var type = FindType(typeName);
            if (type == null)
            {
                return new { success = false, error = $"Type '{typeName}' not found" };
            }
            
            var component = go.GetComponent(type);
            if (component != null)
            {
                // Use DestroyImmediate in Editor mode (not playing), Destroy in Play mode
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(component);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(component);
                }
                return new { success = true };
            }
            
            return new { success = false, error = $"Component '{typeName}' not found on '{goName}'" };
        }
        
        private object ComponentGet(Dictionary<string, object> p)
        {
            var goName = GetString(p, "gameObject", null);
            var typeName = GetString(p, "type", null);
            
            var go = GameObject.Find(goName);
            if (go == null)
            {
                return new { error = $"GameObject '{goName}' not found" };
            }
            
            var type = FindType(typeName);
            if (type == null)
            {
                return new { error = $"Type '{typeName}' not found" };
            }
            
            var component = go.GetComponent(type);
            if (component == null)
            {
                return new { error = $"Component '{typeName}' not found on '{goName}'" };
            }
            
            return GetComponentData(component);
        }
        
        private object ComponentSet(Dictionary<string, object> p)
        {
            var goName = GetString(p, "gameObject", null);
            var typeName = GetString(p, "type", null);
            var field = GetString(p, "field", null);
            var value = p.ContainsKey("value") ? p["value"] : null;
            
            var go = GameObject.Find(goName);
            if (go == null)
            {
                return new { success = false, error = $"GameObject '{goName}' not found" };
            }
            
            var type = FindType(typeName);
            var component = go.GetComponent(type);
            if (component == null)
            {
                return new { success = false, error = $"Component '{typeName}' not found" };
            }
            
            try
            {
                var fieldInfo = type.GetField(field, BindingFlags.Public | BindingFlags.Instance);
                var propInfo = type.GetProperty(field, BindingFlags.Public | BindingFlags.Instance);
                
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(component, ConvertValue(value, fieldInfo.FieldType));
                    return new { success = true };
                }
                else if (propInfo != null && propInfo.CanWrite)
                {
                    propInfo.SetValue(component, ConvertValue(value, propInfo.PropertyType));
                    return new { success = true };
                }
                
                return new { success = false, error = $"Field/Property '{field}' not found or not writable" };
            }
            catch (Exception e)
            {
                return new { success = false, error = e.Message };
            }
        }
        
        private object ComponentList(Dictionary<string, object> p)
        {
            var prefix = GetString(p, "prefix", "").ToLower();
            var types = new List<string>();
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(Component).IsAssignableFrom(type) && !type.IsAbstract)
                        {
                            if (string.IsNullOrEmpty(prefix) || type.Name.ToLower().Contains(prefix))
                            {
                                types.Add(type.FullName);
                            }
                        }
                    }
                }
                catch { }
            }
            
            return types.OrderBy(t => t).Take(100).ToList();
        }
        
        private Dictionary<string, object> GetComponentData(Component component)
        {
            var type = component.GetType();
            var data = new Dictionary<string, object>
            {
                { "type", type.Name },
                { "fullType", type.FullName }
            };
            
            var fields = new Dictionary<string, object>();
            
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                try
                {
                    fields[field.Name] = SerializeValue(field.GetValue(component));
                }
                catch { }
            }
            
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.CanRead && prop.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        fields[prop.Name] = SerializeValue(prop.GetValue(component));
                    }
                    catch { }
                }
            }
            
            data["fields"] = fields;
            return data;
        }
        
        #endregion
        
        #region Script Tools
        
        private object ScriptExecute(Dictionary<string, object> p)
        {
            if (!OpenClawConfig.Instance.allowCodeExecution)
            {
                return new { success = false, error = "Code execution is disabled in config" };
            }
            
            var code = GetString(p, "code", null);
            var methodCall = GetString(p, "method", null);
            var targetName = GetString(p, "target", null);
            var typeName = GetString(p, "type", null);
            var argsJson = GetString(p, "args", null);
            
            try
            {
                // Method 1: Direct method call via reflection (more reliable)
                if (!string.IsNullOrEmpty(methodCall))
                {
                    return ExecuteMethodCall(targetName, typeName, methodCall, argsJson);
                }
                
                // Method 2: Simple code interpretation
                if (!string.IsNullOrEmpty(code))
                {
                    return ExecuteSimpleCode(code);
                }
                
                return new { success = false, error = "Provide 'code' or 'method' parameter" };
            }
            catch (Exception e)
            {
                return new { success = false, error = e.Message, stackTrace = e.StackTrace };
            }
        }
        
        private object ExecuteMethodCall(string targetName, string typeName, string methodName, string argsJson)
        {
            object target = null;
            Type type = null;
            
            // Find target object
            if (!string.IsNullOrEmpty(targetName))
            {
                var go = GameObject.Find(targetName);
                if (go != null)
                {
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        type = FindType(typeName);
                        target = go.GetComponent(type);
                    }
                    else
                    {
                        target = go;
                        type = typeof(GameObject);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(typeName))
            {
                // Static method call
                type = FindType(typeName);
            }
            
            if (type == null)
            {
                return new { success = false, error = $"Type not found: {typeName}" };
            }
            
            // Find method
            var method = type.GetMethod(methodName, 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            
            if (method == null)
            {
                // Try to find without case sensitivity
                method = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));
            }
            
            if (method == null)
            {
                return new { success = false, error = $"Method '{methodName}' not found on type '{type.Name}'" };
            }
            
            // Parse arguments
            object[] args = null;
            if (!string.IsNullOrEmpty(argsJson))
            {
                var argsList = ParseJsonArray(argsJson);
                var parameters = method.GetParameters();
                args = new object[parameters.Length];
                
                for (int i = 0; i < parameters.Length && i < argsList.Count; i++)
                {
                    args[i] = ConvertValue(argsList[i], parameters[i].ParameterType);
                }
            }
            
            // Invoke
            var result = method.Invoke(target, args);
            
            return new { 
                success = true, 
                method = methodName, 
                type = type.Name,
                result = SerializeValue(result)
            };
        }
        
        private object ExecuteSimpleCode(string code)
        {
            code = code.Trim();
            
            // Debug.Log
            if (code.StartsWith("Debug.Log(") && code.EndsWith(");"))
            {
                var msg = code.Substring(10, code.Length - 12).Trim().Trim('"');
                Debug.Log(msg);
                return new { success = true, output = msg, executed = "Debug.Log" };
            }
            
            // Debug.LogWarning
            if (code.StartsWith("Debug.LogWarning(") && code.EndsWith(");"))
            {
                var msg = code.Substring(17, code.Length - 19).Trim().Trim('"');
                Debug.LogWarning(msg);
                return new { success = true, output = msg, executed = "Debug.LogWarning" };
            }
            
            // Debug.LogError
            if (code.StartsWith("Debug.LogError(") && code.EndsWith(");"))
            {
                var msg = code.Substring(15, code.Length - 17).Trim().Trim('"');
                Debug.LogError(msg);
                return new { success = true, output = msg, executed = "Debug.LogError" };
            }
            
            // GameObject.Find().GetComponent<T>().method()
            var goFindMatch = System.Text.RegularExpressions.Regex.Match(code, 
                @"GameObject\.Find\(""([^""]+)""\)\.GetComponent<([^>]+)>\(\)\.(\w+)\((.*?)\);?");
            if (goFindMatch.Success)
            {
                var goName = goFindMatch.Groups[1].Value;
                var compType = goFindMatch.Groups[2].Value;
                var method = goFindMatch.Groups[3].Value;
                var args = goFindMatch.Groups[4].Value;
                
                return ExecuteMethodCall(goName, compType, method, $"[{args}]");
            }
            
            // Time.timeScale = value
            var timeScaleMatch = System.Text.RegularExpressions.Regex.Match(code, @"Time\.timeScale\s*=\s*([\d.]+);?");
            if (timeScaleMatch.Success)
            {
                var value = float.Parse(timeScaleMatch.Groups[1].Value);
                Time.timeScale = value;
                return new { success = true, executed = "Time.timeScale", value = value };
            }
            
            // Application.Quit()
            if (code.Contains("Application.Quit()"))
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                return new { success = true, executed = "EditorApplication.isPlaying = false" };
                #else
                Application.Quit();
                return new { success = true, executed = "Application.Quit" };
                #endif
            }
            
            // PlayerPrefs operations
            var ppSetMatch = System.Text.RegularExpressions.Regex.Match(code, @"PlayerPrefs\.Set(\w+)\(""([^""]+)"",\s*(.+)\);?");
            if (ppSetMatch.Success)
            {
                var ppType = ppSetMatch.Groups[1].Value;
                var key = ppSetMatch.Groups[2].Value;
                var value = ppSetMatch.Groups[3].Value.Trim().Trim('"');
                
                switch (ppType)
                {
                    case "Int":
                        PlayerPrefs.SetInt(key, int.Parse(value));
                        break;
                    case "Float":
                        PlayerPrefs.SetFloat(key, float.Parse(value));
                        break;
                    case "String":
                        PlayerPrefs.SetString(key, value);
                        break;
                }
                PlayerPrefs.Save();
                return new { success = true, executed = $"PlayerPrefs.Set{ppType}", key = key, value = value };
            }
            
            var ppGetMatch = System.Text.RegularExpressions.Regex.Match(code, @"PlayerPrefs\.Get(\w+)\(""([^""]+)""");
            if (ppGetMatch.Success)
            {
                var ppType = ppGetMatch.Groups[1].Value;
                var key = ppGetMatch.Groups[2].Value;
                
                object result = ppType switch
                {
                    "Int" => PlayerPrefs.GetInt(key),
                    "Float" => PlayerPrefs.GetFloat(key),
                    "String" => PlayerPrefs.GetString(key),
                    _ => null
                };
                
                return new { success = true, executed = $"PlayerPrefs.Get{ppType}", key = key, result = result };
            }
            
            return new { 
                success = false, 
                error = "Unsupported code pattern. Use 'method' parameter for reflection-based execution.",
                supported = new[] {
                    "Debug.Log/LogWarning/LogError",
                    "Time.timeScale = value",
                    "PlayerPrefs.SetInt/Float/String",
                    "PlayerPrefs.GetInt/Float/String",
                    "Application.Quit()"
                },
                suggestion = "Use script.execute with method, target, type, args for complex operations"
            };
        }
        
        private List<object> ParseJsonArray(string json)
        {
            var result = new List<object>();
            if (string.IsNullOrEmpty(json)) return result;
            
            json = json.Trim();
            if (!json.StartsWith("[")) return result;
            
            json = json.Substring(1, json.Length - 2);
            if (string.IsNullOrEmpty(json)) return result;
            
            // Simple split (doesn't handle nested objects)
            foreach (var item in json.Split(','))
            {
                var value = item.Trim();
                if (value.StartsWith("\"") && value.EndsWith("\""))
                    result.Add(value.Trim('"'));
                else if (value == "true")
                    result.Add(true);
                else if (value == "false")
                    result.Add(false);
                else if (value == "null")
                    result.Add(null);
                else if (float.TryParse(value, out var f))
                    result.Add(f);
                else
                    result.Add(value);
            }
            
            return result;
        }
        
        private object ScriptRead(Dictionary<string, object> p)
        {
            if (!OpenClawConfig.Instance.allowFileAccess)
            {
                return new { success = false, error = "File access is disabled in config" };
            }
            
            var path = GetString(p, "path", null);
            
            #if UNITY_EDITOR
            if (System.IO.File.Exists(path))
            {
                var content = System.IO.File.ReadAllText(path);
                return new { success = true, content = content };
            }
            #endif
            
            return new { success = false, error = $"File not found: {path}" };
        }
        
        private object ScriptList(Dictionary<string, object> p)
        {
            var scripts = new List<string>();
            
            #if UNITY_EDITOR
            var folder = GetString(p, "folder", "Assets/Scripts");
            if (System.IO.Directory.Exists(folder))
            {
                scripts = System.IO.Directory.GetFiles(folder, "*.cs", System.IO.SearchOption.AllDirectories)
                    .Select(f => f.Replace("\\", "/"))
                    .ToList();
            }
            #endif
            
            return scripts;
        }
        
        #endregion
        
        #region Application Tools
        
        private object AppGetState(Dictionary<string, object> p)
        {
            return new Dictionary<string, object>
            {
                { "isPlaying", Application.isPlaying },
                { "isPaused", false }, // EditorApplication.isPaused in Editor
                { "platform", Application.platform.ToString() },
                { "unityVersion", Application.unityVersion },
                { "productName", Application.productName },
                { "dataPath", Application.dataPath },
                { "fps", 1f / Time.deltaTime },
                { "time", Time.time }
            };
        }
        
        private object AppPlay(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = true;
            return new { success = true };
            #else
            return new { success = false, error = "Can only control play mode in Editor" };
            #endif
        }
        
        private object AppPause(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPaused = !UnityEditor.EditorApplication.isPaused;
            return new { success = true, isPaused = UnityEditor.EditorApplication.isPaused };
            #else
            return new { success = false, error = "Can only control pause in Editor" };
            #endif
        }
        
        private object AppStop(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            return new { success = true };
            #else
            return new { success = false, error = "Can only control play mode in Editor" };
            #endif
        }
        
        #endregion
        
        #region Debug Tools
        
        private object DebugLog(Dictionary<string, object> p)
        {
            var message = GetString(p, "message", "");
            var level = GetString(p, "level", "log");
            
            switch (level.ToLower())
            {
                case "error":
                    Debug.LogError($"[OpenClaw] {message}");
                    break;
                case "warning":
                    Debug.LogWarning($"[OpenClaw] {message}");
                    break;
                default:
                    Debug.Log($"[OpenClaw] {message}");
                    break;
            }
            
            return new { success = true };
        }
        
        private object DebugScreenshot(Dictionary<string, object> p)
        {
            var filename = GetString(p, "filename", $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            var path = System.IO.Path.Combine(Application.persistentDataPath, filename);
            var method = GetString(p, "method", "auto"); // auto, camera, screencapture
            
            try
            {
                // Method 1: ScreenCapture (captures UI overlays, but may fail in Editor)
                if ((method == "auto" || method == "screencapture") && Application.isPlaying)
                {
                    var texture = ScreenCapture.CaptureScreenshotAsTexture();
                    if (texture != null && texture.width > 100 && texture.height > 100)
                    {
                        var bytes = texture.EncodeToPNG();
                        System.IO.File.WriteAllBytes(path, bytes);
                        int w = texture.width, h = texture.height;
                        SafeDestroy(texture);
                        return new { success = true, path = path, mode = "screencapture", width = w, height = h };
                    }
                    if (texture != null) SafeDestroy(texture);
                    
                    // If auto mode and screencapture failed, try camera
                    if (method == "screencapture")
                        return new { success = false, error = "ScreenCapture failed or returned invalid size" };
                }
                
                // Method 2: Camera.main render (no UI overlay, but reliable)
                if (method == "auto" || method == "camera")
                {
                    var camera = Camera.main;
                    if (camera != null)
                    {
                        int width = GetInt(p, "width", 0);
                        int height = GetInt(p, "height", 0);
                        
                        if (width <= 0 || height <= 0)
                        {
                            width = camera.pixelWidth > 100 ? camera.pixelWidth : 1920;
                            height = camera.pixelHeight > 100 ? camera.pixelHeight : 1080;
                        }
                        
                        var rt = new RenderTexture(width, height, 24);
                        camera.targetTexture = rt;
                        camera.Render();
                        
                        var prevRT = RenderTexture.active;
                        RenderTexture.active = rt;
                        
                        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                        texture.Apply();
                        
                        camera.targetTexture = null;
                        RenderTexture.active = prevRT;
                        
                        var bytes = texture.EncodeToPNG();
                        System.IO.File.WriteAllBytes(path, bytes);
                        
                        SafeDestroy(rt);
                        SafeDestroy(texture);
                        
                        return new { success = true, path = path, mode = "camera", width = width, height = height };
                    }
                }
                
                return new { success = false, error = "No capture method succeeded" };
            }
            catch (System.Exception e)
            {
                return new { success = false, error = e.Message };
            }
        }
        
        /// <summary>
        /// Safely destroy an object - uses DestroyImmediate in Editor mode, Destroy in Play mode
        /// </summary>
        private void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(obj);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }
        
        private object DebugHierarchy(Dictionary<string, object> p)
        {
            var depth = GetInt(p, "depth", 3);
            var scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();
            
            var sb = new StringBuilder();
            foreach (var go in rootObjects)
            {
                AppendHierarchy(sb, go, 0, depth);
            }
            
            return sb.ToString();
        }
        
        private void AppendHierarchy(StringBuilder sb, GameObject go, int indent, int maxDepth)
        {
            sb.Append(new string(' ', indent * 2));
            sb.Append(go.activeSelf ? "▶ " : "▷ ");
            sb.Append(go.name);
            
            var components = go.GetComponents<Component>()
                .Where(c => c != null && c.GetType() != typeof(Transform))
                .Select(c => c.GetType().Name);
            
            if (components.Any())
            {
                sb.Append($" [{string.Join(", ", components)}]");
            }
            
            sb.AppendLine();
            
            if (indent < maxDepth)
            {
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    AppendHierarchy(sb, go.transform.GetChild(i).gameObject, indent + 1, maxDepth);
                }
            }
        }
        
        #endregion
        
        #region Editor Tools
        
        private object EditorRefresh(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var importOptions = GetBool(p, "forceUpdate", false) 
                ? UnityEditor.ImportAssetOptions.ForceUpdate 
                : UnityEditor.ImportAssetOptions.Default;
            
            UnityEditor.AssetDatabase.Refresh(importOptions);
            return new { success = true, action = "AssetDatabase.Refresh", forceUpdate = GetBool(p, "forceUpdate", false) };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object EditorRecompile(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            return new { success = true, action = "RequestScriptCompilation" };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object EditorDomainReload(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            // This forces a full domain reload, reinitializing all static fields
            UnityEditor.EditorUtility.RequestScriptReload();
            return new { success = true, action = "RequestScriptReload", note = "Domain reload requested. Connection will reconnect automatically." };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object EditorFocusWindow(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var windowName = GetString(p, "window", "Game");
            
            try
            {
                Type windowType = null;
                string actualName = windowName.ToLower();
                
                // Map common window names to types
                var windowTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "game", "UnityEditor.GameView" },
                    { "gameview", "UnityEditor.GameView" },
                    { "scene", "UnityEditor.SceneView" },
                    { "sceneview", "UnityEditor.SceneView" },
                    { "console", "UnityEditor.ConsoleWindow" },
                    { "hierarchy", "UnityEditor.SceneHierarchyWindow" },
                    { "project", "UnityEditor.ProjectBrowser" },
                    { "inspector", "UnityEditor.InspectorWindow" },
                    { "animation", "UnityEditor.AnimationWindow" },
                    { "animator", "UnityEditor.Graphs.AnimatorControllerTool" },
                    { "profiler", "UnityEditor.ProfilerWindow" },
                    { "audio", "UnityEditor.AudioMixerWindow" },
                    { "asset", "UnityEditor.AssetStoreWindow" },
                    { "package", "UnityEditor.PackageManager.UI.PackageManagerWindow" },
                };
                
                string typeName = null;
                if (windowTypes.TryGetValue(windowName, out typeName))
                {
                    // Find the type in UnityEditor assembly
                    var assembly = typeof(UnityEditor.EditorWindow).Assembly;
                    windowType = assembly.GetType(typeName);
                    
                    // Try alternative assemblies for some windows
                    if (windowType == null)
                    {
                        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            windowType = asm.GetType(typeName);
                            if (windowType != null) break;
                        }
                    }
                }
                
                if (windowType == null)
                {
                    return new { 
                        success = false, 
                        error = $"Window '{windowName}' not found",
                        availableWindows = new[] { "game", "scene", "console", "hierarchy", "project", "inspector", "profiler" }
                    };
                }
                
                // Focus the window
                var window = UnityEditor.EditorWindow.GetWindow(windowType, false, null, true);
                if (window != null)
                {
                    window.Focus();
                    return new { success = true, window = windowName, focused = true };
                }
                
                return new { success = false, error = $"Failed to focus window '{windowName}'" };
            }
            catch (System.Exception e)
            {
                return new { success = false, error = e.Message };
            }
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object EditorListWindows(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            try
            {
                var windows = new List<Dictionary<string, object>>();
                
                foreach (var window in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>())
                {
                    windows.Add(new Dictionary<string, object>
                    {
                        { "title", window.titleContent.text },
                        { "type", window.GetType().Name },
                        { "focused", window.hasFocus },
                        { "position", $"{window.position.x},{window.position.y},{window.position.width},{window.position.height}" }
                    });
                }
                
                return new { success = true, windows = windows, count = windows.Count };
            }
            catch (System.Exception e)
            {
                return new { success = false, error = e.Message };
            }
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        #endregion
        
        #region Input Tools
        
        // Static state for simulated input
        private static Dictionary<KeyCode, bool> _simulatedKeys = new Dictionary<KeyCode, bool>();
        private static Vector3 _simulatedMousePosition = Vector3.zero;
        private static Dictionary<int, bool> _simulatedMouseButtons = new Dictionary<int, bool>();
        
        private object InputKeyPress(Dictionary<string, object> p)
        {
            var keyName = GetString(p, "key", "");
            var duration = GetFloat(p, "duration", 0.1f);
            
            if (!TryParseKeyCode(keyName, out var keyCode))
            {
                return new { success = false, error = $"Unknown key: {keyName}. Use Unity KeyCode names (e.g., W, Space, LeftShift, Mouse0)" };
            }
            
            // Queue the key press using a coroutine-like approach
            SimulateKeyPress(keyCode, duration);
            
            return new { success = true, key = keyName, keyCode = keyCode.ToString(), duration = duration };
        }
        
        private object InputKeyDown(Dictionary<string, object> p)
        {
            var keyName = GetString(p, "key", "");
            
            if (!TryParseKeyCode(keyName, out var keyCode))
            {
                return new { success = false, error = $"Unknown key: {keyName}" };
            }
            
            _simulatedKeys[keyCode] = true;
            MarkKeyDown(keyCode);  // Track frame for GetKeyDown
            QueueInputEvent(new InputEvent { Type = InputEventType.KeyDown, KeyCode = keyCode });
            
            return new { success = true, key = keyName, keyCode = keyCode.ToString(), state = "down" };
        }
        
        private object InputKeyUp(Dictionary<string, object> p)
        {
            var keyName = GetString(p, "key", "");
            
            if (!TryParseKeyCode(keyName, out var keyCode))
            {
                return new { success = false, error = $"Unknown key: {keyName}" };
            }
            
            _simulatedKeys[keyCode] = false;
            MarkKeyUp(keyCode);  // Track frame for GetKeyUp
            QueueInputEvent(new InputEvent { Type = InputEventType.KeyUp, KeyCode = keyCode });
            
            return new { success = true, key = keyName, keyCode = keyCode.ToString(), state = "up" };
        }
        
        private object InputType(Dictionary<string, object> p)
        {
            var text = GetString(p, "text", "");
            var interval = GetFloat(p, "interval", 0.05f);
            
            if (string.IsNullOrEmpty(text))
            {
                return new { success = false, error = "No text provided" };
            }
            
            // Find the currently focused input field and type into it
            var eventSystem = EventSystem.current;
            if (eventSystem != null && eventSystem.currentSelectedGameObject != null)
            {
                var inputField = eventSystem.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>();
                if (inputField != null)
                {
                    inputField.text += text;
                    return new { success = true, text = text, target = inputField.gameObject.name, method = "InputField" };
                }
                
                // Try TMP_InputField via reflection (avoids hard dependency on TextMeshPro)
                var tmpInputField = GetTMPInputField(eventSystem.currentSelectedGameObject);
                if (tmpInputField != null)
                {
                    var textProp = tmpInputField.GetType().GetProperty("text");
                    if (textProp != null)
                    {
                        var currentText = textProp.GetValue(tmpInputField) as string ?? "";
                        textProp.SetValue(tmpInputField, currentText + text);
                        return new { success = true, text = text, target = (tmpInputField as Component)?.gameObject.name, method = "TMP_InputField" };
                    }
                }
            }
            
            // Fallback: queue character events
            foreach (var c in text)
            {
                QueueInputEvent(new InputEvent { Type = InputEventType.Character, Character = c });
            }
            
            return new { success = true, text = text, method = "character_events", length = text.Length };
        }
        
        private object InputMouseMove(Dictionary<string, object> p)
        {
            var x = GetFloat(p, "x", 0);
            var y = GetFloat(p, "y", 0);
            var normalized = GetBool(p, "normalized", false);
            
            Vector3 targetPos;
            if (normalized)
            {
                // Convert from 0-1 range to screen coordinates
                targetPos = new Vector3(x * Screen.width, y * Screen.height, 0);
            }
            else
            {
                targetPos = new Vector3(x, y, 0);
            }
            
            _simulatedMousePosition = targetPos;
            QueueInputEvent(new InputEvent { Type = InputEventType.MouseMove, Position = targetPos });
            
            return new { success = true, x = targetPos.x, y = targetPos.y, screenWidth = Screen.width, screenHeight = Screen.height };
        }
        
        private object InputMouseClick(Dictionary<string, object> p)
        {
            var x = GetFloat(p, "x", -1);
            var y = GetFloat(p, "y", -1);
            var button = GetInt(p, "button", 0); // 0 = left, 1 = right, 2 = middle
            var clicks = GetInt(p, "clicks", 1);
            var normalized = GetBool(p, "normalized", false);
            
            Vector3 targetPos;
            if (x < 0 || y < 0)
            {
                targetPos = _simulatedMousePosition;
            }
            else if (normalized)
            {
                targetPos = new Vector3(x * Screen.width, y * Screen.height, 0);
            }
            else
            {
                targetPos = new Vector3(x, y, 0);
            }
            
            _simulatedMousePosition = targetPos;
            
            // Try to click UI element at position
            var clickedUI = TryClickUIAtPosition(targetPos, button, clicks);
            if (clickedUI != null)
            {
                return new { success = true, x = targetPos.x, y = targetPos.y, button = button, clicks = clicks, 
                    target = clickedUI, method = "UI_EventSystem" };
            }
            
            // Queue raw mouse click events
            for (int i = 0; i < clicks; i++)
            {
                QueueInputEvent(new InputEvent { Type = InputEventType.MouseDown, Button = button, Position = targetPos });
                QueueInputEvent(new InputEvent { Type = InputEventType.MouseUp, Button = button, Position = targetPos });
            }
            
            // Also set simulated key state for Mouse buttons (so IsKeyPressed works)
            var mouseKeyCode = button switch
            {
                0 => KeyCode.Mouse0,
                1 => KeyCode.Mouse1,
                2 => KeyCode.Mouse2,
                _ => KeyCode.Mouse0
            };
            _simulatedKeys[mouseKeyCode] = true;
            _keyDownFrame[mouseKeyCode] = Time.frameCount;
            // Schedule release after a short time
            _bridge.ScheduleAction(0.1f, () => {
                _simulatedKeys[mouseKeyCode] = false;
                _keyUpFrame[mouseKeyCode] = Time.frameCount;
            });
            
            return new { success = true, x = targetPos.x, y = targetPos.y, button = button, clicks = clicks, method = "raw_events" };
        }
        
        private object InputMouseDrag(Dictionary<string, object> p)
        {
            var startX = GetFloat(p, "startX", 0);
            var startY = GetFloat(p, "startY", 0);
            var endX = GetFloat(p, "endX", 0);
            var endY = GetFloat(p, "endY", 0);
            var button = GetInt(p, "button", 0);
            var steps = GetInt(p, "steps", 10);
            var normalized = GetBool(p, "normalized", false);
            
            Vector3 start, end;
            if (normalized)
            {
                start = new Vector3(startX * Screen.width, startY * Screen.height, 0);
                end = new Vector3(endX * Screen.width, endY * Screen.height, 0);
            }
            else
            {
                start = new Vector3(startX, startY, 0);
                end = new Vector3(endX, endY, 0);
            }
            
            // Queue drag events
            QueueInputEvent(new InputEvent { Type = InputEventType.MouseMove, Position = start });
            QueueInputEvent(new InputEvent { Type = InputEventType.MouseDown, Button = button, Position = start });
            
            for (int i = 1; i <= steps; i++)
            {
                var t = (float)i / steps;
                var pos = Vector3.Lerp(start, end, t);
                QueueInputEvent(new InputEvent { Type = InputEventType.MouseMove, Position = pos });
            }
            
            QueueInputEvent(new InputEvent { Type = InputEventType.MouseUp, Button = button, Position = end });
            
            _simulatedMousePosition = end;
            
            return new { success = true, startX = start.x, startY = start.y, endX = end.x, endY = end.y, button = button, steps = steps };
        }
        
        private object InputMouseScroll(Dictionary<string, object> p)
        {
            var deltaX = GetFloat(p, "deltaX", 0);
            var deltaY = GetFloat(p, "deltaY", 0);
            
            QueueInputEvent(new InputEvent { Type = InputEventType.MouseScroll, ScrollDelta = new Vector2(deltaX, deltaY) });
            
            return new { success = true, deltaX = deltaX, deltaY = deltaY };
        }
        
        private object InputGetMousePosition(Dictionary<string, object> p)
        {
            var mousePos = Input.mousePosition;
            return new { 
                x = mousePos.x, 
                y = mousePos.y, 
                normalizedX = mousePos.x / Screen.width,
                normalizedY = mousePos.y / Screen.height,
                screenWidth = Screen.width,
                screenHeight = Screen.height
            };
        }
        
        private object InputClickUI(Dictionary<string, object> p)
        {
            var targetName = GetString(p, "name", null);
            var targetPath = GetString(p, "path", null);
            var button = GetInt(p, "button", 0);
            
            GameObject target = null;
            
            if (!string.IsNullOrEmpty(targetPath))
            {
                target = GameObject.Find(targetPath);
            }
            else if (!string.IsNullOrEmpty(targetName))
            {
                // Find by name, preferring UI elements
                var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                target = allObjects.FirstOrDefault(go => go.name == targetName && go.GetComponent<RectTransform>() != null)
                      ?? allObjects.FirstOrDefault(go => go.name == targetName);
            }
            
            if (target == null)
            {
                return new { success = false, error = $"UI element not found: {targetName ?? targetPath}" };
            }
            
            // Check for Button component and click it
            var btn = target.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.Invoke();
                return new { success = true, target = target.name, method = "Button.onClick" };
            }
            
            // Check for Toggle
            var toggle = target.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.isOn = !toggle.isOn;
                return new { success = true, target = target.name, method = "Toggle", isOn = toggle.isOn };
            }
            
            // Try pointer events
            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, target.transform.position),
                button = (PointerEventData.InputButton)button
            };
            
            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerClickHandler);
            
            return new { success = true, target = target.name, method = "PointerClick" };
        }
        
        // Input simulation helpers
        private enum InputEventType { KeyDown, KeyUp, Character, MouseMove, MouseDown, MouseUp, MouseScroll }
        
        private struct InputEvent
        {
            public InputEventType Type;
            public KeyCode KeyCode;
            public char Character;
            public int Button;
            public Vector3 Position;
            public Vector2 ScrollDelta;
        }
        
        private static Queue<InputEvent> _inputEventQueue = new Queue<InputEvent>();
        
        private void QueueInputEvent(InputEvent evt)
        {
            _inputEventQueue.Enqueue(evt);
        }
        
        private void SimulateKeyPress(KeyCode keyCode, float duration)
        {
            _simulatedKeys[keyCode] = true;
            MarkKeyDown(keyCode);
            QueueInputEvent(new InputEvent { Type = InputEventType.KeyDown, KeyCode = keyCode });
            
            // Schedule key release after duration
            if (_bridge != null)
            {
                _bridge.ScheduleAction(duration, () =>
                {
                    _simulatedKeys[keyCode] = false;
                    MarkKeyUp(keyCode);
                    QueueInputEvent(new InputEvent { Type = InputEventType.KeyUp, KeyCode = keyCode });
                });
            }
            else
            {
                // Fallback: immediate release
                _simulatedKeys[keyCode] = false;
                MarkKeyUp(keyCode);
                QueueInputEvent(new InputEvent { Type = InputEventType.KeyUp, KeyCode = keyCode });
            }
        }
        
        private bool TryParseKeyCode(string keyName, out KeyCode keyCode)
        {
            keyCode = KeyCode.None;
            if (string.IsNullOrEmpty(keyName)) return false;
            
            // Try direct parse
            if (Enum.TryParse<KeyCode>(keyName, true, out keyCode))
                return true;
            
            // Common aliases
            var aliases = new Dictionary<string, KeyCode>(StringComparer.OrdinalIgnoreCase)
            {
                { "left", KeyCode.LeftArrow },
                { "right", KeyCode.RightArrow },
                { "up", KeyCode.UpArrow },
                { "down", KeyCode.DownArrow },
                { "enter", KeyCode.Return },
                { "esc", KeyCode.Escape },
                { "ctrl", KeyCode.LeftControl },
                { "alt", KeyCode.LeftAlt },
                { "shift", KeyCode.LeftShift },
                { "lmb", KeyCode.Mouse0 },
                { "rmb", KeyCode.Mouse1 },
                { "mmb", KeyCode.Mouse2 },
            };
            
            if (aliases.TryGetValue(keyName, out keyCode))
                return true;
            
            // Single character
            if (keyName.Length == 1)
            {
                var c = char.ToUpper(keyName[0]);
                if (c >= 'A' && c <= 'Z')
                {
                    keyCode = (KeyCode)c;
                    return true;
                }
                if (c >= '0' && c <= '9')
                {
                    keyCode = KeyCode.Alpha0 + (c - '0');
                    return true;
                }
            }
            
            return false;
        }
        
        private string TryClickUIAtPosition(Vector3 screenPos, int button, int clicks)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return null;
            
            var pointerData = new PointerEventData(eventSystem)
            {
                position = screenPos,
                button = (PointerEventData.InputButton)button,
                clickCount = clicks
            };
            
            var results = new List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, results);
            
            if (results.Count > 0)
            {
                var target = results[0].gameObject;
                
                // Try button click
                var btn = target.GetComponentInParent<Button>();
                if (btn != null)
                {
                    btn.onClick.Invoke();
                    return btn.gameObject.name;
                }
                
                // Try input field focus
                var inputField = target.GetComponentInParent<UnityEngine.UI.InputField>();
                if (inputField != null)
                {
                    eventSystem.SetSelectedGameObject(inputField.gameObject);
                    return inputField.gameObject.name;
                }
                
                // Try TMP_InputField via reflection
                var tmpInput = GetTMPInputFieldInParent(target);
                if (tmpInput != null)
                {
                    eventSystem.SetSelectedGameObject((tmpInput as Component)?.gameObject);
                    return (tmpInput as Component)?.gameObject.name;
                }
                
                // Generic pointer click
                ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerClickHandler);
                return target.name;
            }
            
            return null;
        }
        
        // Helper to get TMP_InputField without hard dependency
        private static Type _tmpInputFieldType;
        private static bool _tmpTypeSearched;
        
        private object GetTMPInputField(GameObject go)
        {
            if (go == null) return null;
            EnsureTMPType();
            if (_tmpInputFieldType == null) return null;
            return go.GetComponent(_tmpInputFieldType);
        }
        
        private object GetTMPInputFieldInParent(GameObject go)
        {
            if (go == null) return null;
            EnsureTMPType();
            if (_tmpInputFieldType == null) return null;
            return go.GetComponentInParent(_tmpInputFieldType);
        }
        
        private static void EnsureTMPType()
        {
            if (_tmpTypeSearched) return;
            _tmpTypeSearched = true;
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                _tmpInputFieldType = assembly.GetType("TMPro.TMP_InputField");
                if (_tmpInputFieldType != null) break;
            }
        }
        
        /// <summary>
        /// Check if a simulated key is pressed (for custom input handling)
        /// </summary>
        public static bool IsKeyPressed(KeyCode keyCode)
        {
            return _simulatedKeys.TryGetValue(keyCode, out var pressed) && pressed;
        }
        
        // Track key state transitions per frame
        private static Dictionary<KeyCode, int> _keyDownFrame = new Dictionary<KeyCode, int>();
        private static Dictionary<KeyCode, int> _keyUpFrame = new Dictionary<KeyCode, int>();
        
        /// <summary>
        /// Check if a simulated key was just pressed this frame
        /// </summary>
        public static bool IsKeyDown(KeyCode keyCode)
        {
            return _keyDownFrame.TryGetValue(keyCode, out var frame) && frame == Time.frameCount;
        }
        
        /// <summary>
        /// Check if a simulated key was just released this frame
        /// </summary>
        public static bool IsKeyUp(KeyCode keyCode)
        {
            return _keyUpFrame.TryGetValue(keyCode, out var frame) && frame == Time.frameCount;
        }
        
        /// <summary>
        /// Internal: Mark key as just pressed
        /// </summary>
        internal static void MarkKeyDown(KeyCode keyCode)
        {
            _keyDownFrame[keyCode] = Time.frameCount;
        }
        
        /// <summary>
        /// Internal: Mark key as just released
        /// </summary>
        internal static void MarkKeyUp(KeyCode keyCode)
        {
            _keyUpFrame[keyCode] = Time.frameCount;
        }
        
        /// <summary>
        /// Get the simulated mouse position
        /// </summary>
        public static Vector3 GetSimulatedMousePosition()
        {
            return _simulatedMousePosition;
        }
        
        #endregion
        
        #region Material Tools
        
        private object MaterialCreate(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var name = GetString(p, "name", "New Material");
            var shaderName = GetString(p, "shader", "Standard");
            var path = GetString(p, "path", $"Assets/{name}.mat");
            var colorHex = GetString(p, "color", null);
            
            try
            {
                var shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    // Try common shader names
                    var shaderAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "standard", "Standard" },
                        { "unlit", "Unlit/Color" },
                        { "urp", "Universal Render Pipeline/Lit" },
                        { "urplit", "Universal Render Pipeline/Lit" },
                        { "urpunlit", "Universal Render Pipeline/Unlit" },
                        { "hdrp", "HDRP/Lit" },
                        { "transparent", "Standard" }, // Will set rendering mode
                    };
                    
                    if (shaderAliases.TryGetValue(shaderName, out var actualName))
                    {
                        shader = Shader.Find(actualName);
                    }
                }
                
                if (shader == null)
                {
                    return new { success = false, error = $"Shader '{shaderName}' not found" };
                }
                
                var material = new Material(shader);
                material.name = name;
                
                // Set color if provided
                if (!string.IsNullOrEmpty(colorHex))
                {
                    if (ColorUtility.TryParseHtmlString(colorHex, out var color))
                    {
                        material.color = color;
                    }
                }
                
                // Set metallic/smoothness if provided
                if (p.ContainsKey("metallic"))
                {
                    material.SetFloat("_Metallic", GetFloat(p, "metallic", 0f));
                }
                if (p.ContainsKey("smoothness"))
                {
                    material.SetFloat("_Glossiness", GetFloat(p, "smoothness", 0.5f));
                }
                
                // Save asset
                if (!path.EndsWith(".mat")) path += ".mat";
                UnityEditor.AssetDatabase.CreateAsset(material, path);
                UnityEditor.AssetDatabase.SaveAssets();
                
                return new { success = true, path = path, shader = shader.name, name = name };
            }
            catch (System.Exception e)
            {
                return new { success = false, error = e.Message };
            }
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object MaterialAssign(Dictionary<string, object> p)
        {
            var targetName = GetString(p, "target", null);
            var materialPath = GetString(p, "material", null);
            var materialName = GetString(p, "materialName", null);
            
            var go = GameObject.Find(targetName);
            if (go == null)
            {
                return new { success = false, error = $"GameObject '{targetName}' not found" };
            }
            
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
            {
                return new { success = false, error = $"No Renderer component on '{targetName}'" };
            }
            
            Material mat = null;
            
            #if UNITY_EDITOR
            if (!string.IsNullOrEmpty(materialPath))
            {
                mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            }
            else if (!string.IsNullOrEmpty(materialName))
            {
                var guids = UnityEditor.AssetDatabase.FindAssets($"t:Material {materialName}");
                if (guids.Length > 0)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                }
            }
            #endif
            
            if (mat == null)
            {
                return new { success = false, error = $"Material not found: {materialPath ?? materialName}" };
            }
            
            renderer.material = mat;
            return new { success = true, target = targetName, material = mat.name };
        }
        
        private object MaterialModify(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var path = GetString(p, "path", null);
            var name = GetString(p, "name", null);
            
            Material mat = null;
            
            if (!string.IsNullOrEmpty(path))
            {
                mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            }
            else if (!string.IsNullOrEmpty(name))
            {
                var guids = UnityEditor.AssetDatabase.FindAssets($"t:Material {name}");
                if (guids.Length > 0)
                {
                    path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                }
            }
            
            if (mat == null)
            {
                return new { success = false, error = $"Material not found: {path ?? name}" };
            }
            
            var modified = new List<string>();
            
            // Color
            if (p.ContainsKey("color"))
            {
                var colorStr = GetString(p, "color", "#FFFFFF");
                if (ColorUtility.TryParseHtmlString(colorStr, out var color))
                {
                    mat.color = color;
                    modified.Add("color");
                }
            }
            
            // Metallic
            if (p.ContainsKey("metallic"))
            {
                mat.SetFloat("_Metallic", GetFloat(p, "metallic", 0f));
                modified.Add("metallic");
            }
            
            // Smoothness/Glossiness
            if (p.ContainsKey("smoothness"))
            {
                mat.SetFloat("_Glossiness", GetFloat(p, "smoothness", 0.5f));
                modified.Add("smoothness");
            }
            
            // Emission
            if (p.ContainsKey("emission"))
            {
                var emissionStr = GetString(p, "emission", "#000000");
                if (ColorUtility.TryParseHtmlString(emissionStr, out var emission))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emission);
                    modified.Add("emission");
                }
            }
            
            // Main texture tiling
            if (p.ContainsKey("tilingX") || p.ContainsKey("tilingY"))
            {
                var tiling = mat.mainTextureScale;
                tiling.x = GetFloat(p, "tilingX", tiling.x);
                tiling.y = GetFloat(p, "tilingY", tiling.y);
                mat.mainTextureScale = tiling;
                modified.Add("tiling");
            }
            
            UnityEditor.EditorUtility.SetDirty(mat);
            UnityEditor.AssetDatabase.SaveAssets();
            
            return new { success = true, material = mat.name, modified = modified };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object MaterialGetInfo(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var path = GetString(p, "path", null);
            var name = GetString(p, "name", null);
            
            Material mat = null;
            
            if (!string.IsNullOrEmpty(path))
            {
                mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            }
            else if (!string.IsNullOrEmpty(name))
            {
                var guids = UnityEditor.AssetDatabase.FindAssets($"t:Material {name}");
                if (guids.Length > 0)
                {
                    path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                }
            }
            
            if (mat == null)
            {
                return new { success = false, error = $"Material not found: {path ?? name}" };
            }
            
            var properties = new Dictionary<string, object>();
            
            // Basic properties
            properties["color"] = ColorUtility.ToHtmlStringRGBA(mat.color);
            properties["renderQueue"] = mat.renderQueue;
            
            // Get shader properties
            var shader = mat.shader;
            var propertyCount = shader.GetPropertyCount();
            
            for (int i = 0; i < propertyCount; i++)
            {
                var propName = shader.GetPropertyName(i);
                var propType = shader.GetPropertyType(i);
                
                try
                {
                    switch (propType)
                    {
                        case UnityEngine.Rendering.ShaderPropertyType.Color:
                            properties[propName] = ColorUtility.ToHtmlStringRGBA(mat.GetColor(propName));
                            break;
                        case UnityEngine.Rendering.ShaderPropertyType.Float:
                        case UnityEngine.Rendering.ShaderPropertyType.Range:
                            properties[propName] = mat.GetFloat(propName);
                            break;
                        case UnityEngine.Rendering.ShaderPropertyType.Vector:
                            var v = mat.GetVector(propName);
                            properties[propName] = new { x = v.x, y = v.y, z = v.z, w = v.w };
                            break;
                        case UnityEngine.Rendering.ShaderPropertyType.Texture:
                            var tex = mat.GetTexture(propName);
                            properties[propName] = tex != null ? tex.name : null;
                            break;
                    }
                }
                catch { }
            }
            
            return new { 
                success = true, 
                name = mat.name, 
                path = path,
                shader = shader.name,
                properties = properties 
            };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object MaterialList(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var folder = GetString(p, "folder", "Assets");
            var filter = GetString(p, "filter", "");
            var maxCount = GetInt(p, "maxCount", 100);
            
            var searchFilter = string.IsNullOrEmpty(filter) ? "t:Material" : $"t:Material {filter}";
            var guids = UnityEditor.AssetDatabase.FindAssets(searchFilter, new[] { folder });
            
            var materials = new List<Dictionary<string, object>>();
            
            foreach (var guid in guids.Take(maxCount))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    materials.Add(new Dictionary<string, object>
                    {
                        { "name", mat.name },
                        { "path", path },
                        { "shader", mat.shader?.name ?? "Unknown" }
                    });
                }
            }
            
            return new { success = true, count = materials.Count, materials = materials };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        #endregion
        
        #region Prefab Tools
        
        private object PrefabCreate(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var sourceName = GetString(p, "source", null);
            var path = GetString(p, "path", null);
            
            var source = GameObject.Find(sourceName);
            if (source == null)
            {
                return new { success = false, error = $"Source GameObject '{sourceName}' not found" };
            }
            
            if (string.IsNullOrEmpty(path))
            {
                path = $"Assets/{source.name}.prefab";
            }
            if (!path.EndsWith(".prefab")) path += ".prefab";
            
            // Ensure directory exists
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            
            var prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(source, path);
            
            return new { 
                success = prefab != null, 
                path = path, 
                name = prefab?.name,
                error = prefab == null ? "Failed to create prefab" : null
            };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object PrefabInstantiate(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var path = GetString(p, "path", null);
            var name = GetString(p, "name", null);
            var newName = GetString(p, "newName", null);
            
            GameObject prefab = null;
            
            if (!string.IsNullOrEmpty(path))
            {
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            else if (!string.IsNullOrEmpty(name))
            {
                var guids = UnityEditor.AssetDatabase.FindAssets($"t:Prefab {name}");
                if (guids.Length > 0)
                {
                    path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }
            
            if (prefab == null)
            {
                return new { success = false, error = $"Prefab not found: {path ?? name}" };
            }
            
            var instance = UnityEditor.PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            
            if (!string.IsNullOrEmpty(newName))
            {
                instance.name = newName;
            }
            
            // Set position if provided
            if (p.ContainsKey("x") || p.ContainsKey("y") || p.ContainsKey("z"))
            {
                instance.transform.position = new Vector3(
                    GetFloat(p, "x", 0),
                    GetFloat(p, "y", 0),
                    GetFloat(p, "z", 0)
                );
            }
            
            return new { 
                success = true, 
                name = instance.name,
                prefab = prefab.name,
                position = Vec3ToDict(instance.transform.position)
            };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object PrefabOpen(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var path = GetString(p, "path", null);
            var name = GetString(p, "name", null);
            
            string prefabPath = null;
            
            if (!string.IsNullOrEmpty(path))
            {
                prefabPath = path;
            }
            else if (!string.IsNullOrEmpty(name))
            {
                var guids = UnityEditor.AssetDatabase.FindAssets($"t:Prefab {name}");
                if (guids.Length > 0)
                {
                    prefabPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                }
            }
            
            if (string.IsNullOrEmpty(prefabPath))
            {
                return new { success = false, error = $"Prefab not found: {path ?? name}" };
            }
            
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return new { success = false, error = $"Failed to load prefab at: {prefabPath}" };
            }
            
            UnityEditor.AssetDatabase.OpenAsset(prefab);
            
            return new { success = true, path = prefabPath, name = prefab.name };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object PrefabClose(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null)
            {
                return new { success = false, error = "No prefab is currently being edited" };
            }
            
            var prefabName = stage.prefabContentsRoot?.name;
            UnityEditor.SceneManagement.StageUtility.GoToMainStage();
            
            return new { success = true, closed = prefabName };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object PrefabSave(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null)
            {
                return new { success = false, error = "No prefab is currently being edited" };
            }
            
            var root = stage.prefabContentsRoot;
            var path = stage.assetPath;
            
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
            
            return new { success = true, path = path, name = root.name };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        #endregion
        
        #region Asset Tools
        
        private object AssetFind(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var query = GetString(p, "query", "");
            var type = GetString(p, "type", null);
            var folder = GetString(p, "folder", "Assets");
            var maxCount = GetInt(p, "maxCount", 100);
            
            var filter = string.IsNullOrEmpty(type) ? query : $"t:{type} {query}";
            var guids = UnityEditor.AssetDatabase.FindAssets(filter, new[] { folder });
            
            var assets = new List<Dictionary<string, object>>();
            
            foreach (var guid in guids.Take(maxCount))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var assetType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(path);
                
                assets.Add(new Dictionary<string, object>
                {
                    { "path", path },
                    { "name", System.IO.Path.GetFileNameWithoutExtension(path) },
                    { "type", assetType?.Name ?? "Unknown" },
                    { "guid", guid }
                });
            }
            
            return new { success = true, count = assets.Count, assets = assets };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object AssetCopy(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var sourcePath = GetString(p, "source", null);
            var destPath = GetString(p, "destination", null);
            
            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destPath))
            {
                return new { success = false, error = "Both source and destination paths required" };
            }
            
            var result = UnityEditor.AssetDatabase.CopyAsset(sourcePath, destPath);
            
            return new { success = result, source = sourcePath, destination = destPath };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object AssetMove(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var sourcePath = GetString(p, "source", null);
            var destPath = GetString(p, "destination", null);
            
            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destPath))
            {
                return new { success = false, error = "Both source and destination paths required" };
            }
            
            var error = UnityEditor.AssetDatabase.MoveAsset(sourcePath, destPath);
            
            return new { 
                success = string.IsNullOrEmpty(error), 
                source = sourcePath, 
                destination = destPath,
                error = string.IsNullOrEmpty(error) ? null : error
            };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object AssetDelete(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var path = GetString(p, "path", null);
            var moveToTrash = GetBool(p, "moveToTrash", true);
            
            if (string.IsNullOrEmpty(path))
            {
                return new { success = false, error = "Path required" };
            }
            
            bool result;
            if (moveToTrash)
            {
                result = UnityEditor.AssetDatabase.MoveAssetToTrash(path);
            }
            else
            {
                result = UnityEditor.AssetDatabase.DeleteAsset(path);
            }
            
            return new { success = result, path = path, movedToTrash = moveToTrash && result };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object AssetRefresh(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var importOptions = GetBool(p, "forceUpdate", false) 
                ? UnityEditor.ImportAssetOptions.ForceUpdate 
                : UnityEditor.ImportAssetOptions.Default;
            
            UnityEditor.AssetDatabase.Refresh(importOptions);
            
            return new { success = true, forceUpdate = GetBool(p, "forceUpdate", false) };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object AssetImport(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var path = GetString(p, "path", null);
            var forceUpdate = GetBool(p, "forceUpdate", false);
            
            if (string.IsNullOrEmpty(path))
            {
                return new { success = false, error = "Path required" };
            }
            
            var options = forceUpdate 
                ? UnityEditor.ImportAssetOptions.ForceUpdate 
                : UnityEditor.ImportAssetOptions.Default;
            
            UnityEditor.AssetDatabase.ImportAsset(path, options);
            
            return new { success = true, path = path };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object AssetGetPath(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var objectName = GetString(p, "name", null);
            var type = GetString(p, "type", null);
            
            var filter = string.IsNullOrEmpty(type) ? objectName : $"t:{type} {objectName}";
            var guids = UnityEditor.AssetDatabase.FindAssets(filter);
            
            if (guids.Length == 0)
            {
                return new { success = false, error = $"Asset not found: {objectName}" };
            }
            
            var paths = guids.Take(10).Select(g => UnityEditor.AssetDatabase.GUIDToAssetPath(g)).ToList();
            
            return new { success = true, path = paths[0], allPaths = paths };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        #endregion
        
        #region Package Manager Tools
        
        private object PackageAdd(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var packageId = GetString(p, "package", null);
            
            if (string.IsNullOrEmpty(packageId))
            {
                return new { success = false, error = "Package ID required (e.g., 'com.unity.textmeshpro' or git URL)" };
            }
            
            try
            {
                var request = UnityEditor.PackageManager.Client.Add(packageId);
                
                // Wait for completion (with timeout)
                var timeout = GetInt(p, "timeout", 30);
                var startTime = System.DateTime.Now;
                
                while (!request.IsCompleted)
                {
                    if ((System.DateTime.Now - startTime).TotalSeconds > timeout)
                    {
                        return new { success = false, error = "Package installation timed out", status = "pending" };
                    }
                    System.Threading.Thread.Sleep(100);
                }
                
                if (request.Status == UnityEditor.PackageManager.StatusCode.Success)
                {
                    var pkg = request.Result;
                    return new { 
                        success = true, 
                        name = pkg.name, 
                        version = pkg.version,
                        displayName = pkg.displayName
                    };
                }
                else
                {
                    return new { success = false, error = request.Error?.message ?? "Unknown error" };
                }
            }
            catch (System.Exception e)
            {
                return new { success = false, error = e.Message };
            }
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object PackageRemove(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var packageName = GetString(p, "package", null);
            
            if (string.IsNullOrEmpty(packageName))
            {
                return new { success = false, error = "Package name required" };
            }
            
            try
            {
                var request = UnityEditor.PackageManager.Client.Remove(packageName);
                
                var timeout = GetInt(p, "timeout", 30);
                var startTime = System.DateTime.Now;
                
                while (!request.IsCompleted)
                {
                    if ((System.DateTime.Now - startTime).TotalSeconds > timeout)
                    {
                        return new { success = false, error = "Package removal timed out", status = "pending" };
                    }
                    System.Threading.Thread.Sleep(100);
                }
                
                if (request.Status == UnityEditor.PackageManager.StatusCode.Success)
                {
                    return new { success = true, removed = packageName };
                }
                else
                {
                    return new { success = false, error = request.Error?.message ?? "Unknown error" };
                }
            }
            catch (System.Exception e)
            {
                return new { success = false, error = e.Message };
            }
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object PackageList(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            try
            {
                var includeBuiltIn = GetBool(p, "includeBuiltIn", false);
                var request = UnityEditor.PackageManager.Client.List(includeBuiltIn);
                
                var timeout = GetInt(p, "timeout", 30);
                var startTime = System.DateTime.Now;
                
                while (!request.IsCompleted)
                {
                    if ((System.DateTime.Now - startTime).TotalSeconds > timeout)
                    {
                        return new { success = false, error = "Package list timed out" };
                    }
                    System.Threading.Thread.Sleep(100);
                }
                
                if (request.Status == UnityEditor.PackageManager.StatusCode.Success)
                {
                    var packages = request.Result.Select(pkg => new Dictionary<string, object>
                    {
                        { "name", pkg.name },
                        { "version", pkg.version },
                        { "displayName", pkg.displayName },
                        { "source", pkg.source.ToString() }
                    }).ToList();
                    
                    return new { success = true, count = packages.Count, packages = packages };
                }
                else
                {
                    return new { success = false, error = request.Error?.message ?? "Unknown error" };
                }
            }
            catch (System.Exception e)
            {
                return new { success = false, error = e.Message };
            }
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object PackageSearch(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            var query = GetString(p, "query", "");
            
            try
            {
                var request = UnityEditor.PackageManager.Client.SearchAll();
                
                var timeout = GetInt(p, "timeout", 60);
                var startTime = System.DateTime.Now;
                
                while (!request.IsCompleted)
                {
                    if ((System.DateTime.Now - startTime).TotalSeconds > timeout)
                    {
                        return new { success = false, error = "Package search timed out" };
                    }
                    System.Threading.Thread.Sleep(100);
                }
                
                if (request.Status == UnityEditor.PackageManager.StatusCode.Success)
                {
                    var results = request.Result
                        .Where(pkg => string.IsNullOrEmpty(query) || 
                               pkg.name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                               (pkg.displayName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                        .Take(50)
                        .Select(pkg => new Dictionary<string, object>
                        {
                            { "name", pkg.name },
                            { "version", pkg.version },
                            { "displayName", pkg.displayName },
                            { "description", pkg.description?.Substring(0, Math.Min(100, pkg.description?.Length ?? 0)) }
                        }).ToList();
                    
                    return new { success = true, count = results.Count, results = results };
                }
                else
                {
                    return new { success = false, error = request.Error?.message ?? "Unknown error" };
                }
            }
            catch (System.Exception e)
            {
                return new { success = false, error = e.Message };
            }
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        #endregion
        
        #region Test Runner Tools
        
        // Test Runner tools use reflection to avoid compile-time dependency on Test Framework
        // Install com.unity.test-framework via Package Manager to use these tools
        
        private static Type _testRunnerApiType;
        private static Type _testModeType;
        private static Type _filterType;
        private static bool _testFrameworkChecked;
        private static bool _testFrameworkAvailable;
        
        private static List<Dictionary<string, object>> _testResults = new List<Dictionary<string, object>>();
        private static int _totalTests, _passedTests, _failedTests, _skippedTests;
        private static bool _testsRunning;
        
        private bool CheckTestFramework()
        {
            if (_testFrameworkChecked) return _testFrameworkAvailable;
            _testFrameworkChecked = true;
            
            try
            {
                _testRunnerApiType = Type.GetType("UnityEditor.TestTools.TestRunner.Api.TestRunnerApi, UnityEditor.TestRunner");
                _testModeType = Type.GetType("UnityEditor.TestTools.TestRunner.Api.TestMode, UnityEditor.TestRunner");
                _filterType = Type.GetType("UnityEditor.TestTools.TestRunner.Api.Filter, UnityEditor.TestRunner");
                
                _testFrameworkAvailable = _testRunnerApiType != null && _testModeType != null && _filterType != null;
            }
            catch
            {
                _testFrameworkAvailable = false;
            }
            
            return _testFrameworkAvailable;
        }
        
        private object TestRun(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            if (!CheckTestFramework())
            {
                return new { 
                    success = false, 
                    error = "Test Framework not installed. Add com.unity.test-framework via Package Manager.",
                    hint = "Window > Package Manager > + > Add package by name > com.unity.test-framework"
                };
            }
            
            var mode = GetString(p, "mode", "EditMode");
            var filter = GetString(p, "filter", null);
            
            try
            {
                // Create TestRunnerApi instance via reflection
                var apiInstance = ScriptableObject.CreateInstance(_testRunnerApiType);
                
                // Get TestMode enum value
                var testModeValue = Enum.Parse(_testModeType, mode.ToLower() == "playmode" ? "PlayMode" : "EditMode");
                
                // Create Filter
                var filterInstance = Activator.CreateInstance(_filterType);
                _filterType.GetField("testMode").SetValue(filterInstance, testModeValue);
                
                if (!string.IsNullOrEmpty(filter))
                {
                    _filterType.GetField("testNames").SetValue(filterInstance, new[] { filter });
                }
                
                // Create ExecutionSettings
                var execSettingsType = Type.GetType("UnityEditor.TestTools.TestRunner.Api.ExecutionSettings, UnityEditor.TestRunner");
                var execSettings = Activator.CreateInstance(execSettingsType, filterInstance);
                
                // Execute
                var executeMethod = _testRunnerApiType.GetMethod("Execute");
                executeMethod.Invoke(apiInstance, new[] { execSettings });
                
                _testsRunning = true;
                _testResults.Clear();
                
                return new { 
                    success = true, 
                    message = $"Test run started ({mode})",
                    note = "Check Unity Test Runner window for results"
                };
            }
            catch (Exception e)
            {
                return new { success = false, error = e.Message };
            }
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object TestList(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            if (!CheckTestFramework())
            {
                return new { 
                    success = false, 
                    error = "Test Framework not installed. Add com.unity.test-framework via Package Manager." 
                };
            }
            
            // For now, return a helpful message since async test list retrieval is complex
            return new { 
                success = true, 
                message = "Use Unity Test Runner window to view tests",
                hint = "Window > General > Test Runner",
                note = "test.run can execute tests by name filter"
            };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        private object TestGetResults(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            if (!CheckTestFramework())
            {
                return new { 
                    success = false, 
                    error = "Test Framework not installed. Add com.unity.test-framework via Package Manager." 
                };
            }
            
            return new { 
                success = true,
                message = "Check Unity Test Runner window for detailed results",
                hint = "Window > General > Test Runner",
                note = "Results are displayed in the Test Runner UI"
            };
            #else
            return new { success = false, error = "Only available in Editor" };
            #endif
        }
        
        #endregion
        
        #region Helpers
        
        private string GetToolDescription(string name)
        {
            return name switch
            {
                "console.getLogs" => "Get Unity console logs with optional type filter",
                "console.clear" => "Clear captured logs",
                "console.getErrors" => "Get error and exception logs (with optional warnings)",
                "scene.list" => "List all scenes in build settings",
                "scene.getActive" => "Get active scene info",
                "scene.getData" => "Get scene hierarchy data",
                "scene.load" => "Load a scene by name (Play mode)",
                "scene.open" => "Open a scene in Editor mode (EditorSceneManager)",
                "scene.save" => "Save the active scene (Editor mode only)",
                "scene.saveAll" => "Save all open scenes (Editor mode only)",
                "gameobject.find" => "Find GameObjects by name, tag, or component type",
                "gameobject.getAll" => "Get all GameObjects in scene (params: activeOnly, includePosition, maxCount, rootOnly, nameFilter)",
                "gameobject.create" => "Create a new GameObject or primitive",
                "gameobject.destroy" => "Destroy a GameObject",
                "gameobject.delete" => "Delete a GameObject (alias for destroy)",
                "gameobject.getData" => "Get detailed GameObject data",
                "gameobject.setActive" => "Enable/disable a GameObject",
                "gameobject.setParent" => "Change GameObject parent",
                "transform.getPosition" => "Get position (x, y, z)",
                "transform.getRotation" => "Get rotation (Euler angles x, y, z)",
                "transform.getScale" => "Get local scale (x, y, z)",
                "transform.setPosition" => "Set position",
                "transform.setRotation" => "Set rotation (Euler angles)",
                "transform.setScale" => "Set local scale",
                "component.add" => "Add component to GameObject",
                "component.remove" => "Remove component from GameObject",
                "component.get" => "Get component data/fields",
                "component.set" => "Set component field value",
                "component.list" => "List available component types",
                "script.execute" => "Execute C# code/method (params: code, method, target, type, args - supports Debug.Log, Time.timeScale, PlayerPrefs, reflection calls)",
                "script.read" => "Read script file contents",
                "script.list" => "List script files in project",
                "app.getState" => "Get application state (playing, fps, etc)",
                "app.play" => "Enter play mode (Editor only)",
                "app.pause" => "Toggle pause (Editor only)",
                "app.stop" => "Exit play mode (Editor only)",
                "debug.log" => "Write to Unity console",
                "debug.screenshot" => "Capture screenshot",
                "debug.hierarchy" => "Get text hierarchy view",
                "editor.refresh" => "Refresh AssetDatabase and recompile if needed (params: forceUpdate)",
                "editor.recompile" => "Request script recompilation",
                "editor.focusWindow" => "Focus an Editor window (params: window - game/scene/console/hierarchy/project/inspector)",
                "editor.listWindows" => "List all open Editor windows",
                "input.keyPress" => "Press and release a key (params: key, duration)",
                "input.keyDown" => "Press and hold a key (params: key)",
                "input.keyUp" => "Release a key (params: key)",
                "input.type" => "Type text into focused input field (params: text)",
                "input.mouseMove" => "Move mouse to position (params: x, y, normalized)",
                "input.mouseClick" => "Click at position (params: x, y, button, clicks, normalized)",
                "input.mouseDrag" => "Drag from start to end (params: startX, startY, endX, endY, button, steps)",
                "input.mouseScroll" => "Scroll mouse wheel (params: deltaX, deltaY)",
                "input.getMousePosition" => "Get current mouse position",
                "input.clickUI" => "Click a UI element by name (params: name or path)",
                // Material tools
                "material.create" => "Create a new material (params: name, shader, path, color, metallic, smoothness)",
                "material.assign" => "Assign material to GameObject (params: target, material or materialName)",
                "material.modify" => "Modify material properties (params: path or name, color, metallic, smoothness, emission)",
                "material.getInfo" => "Get material info and properties (params: path or name)",
                "material.list" => "List materials in project (params: folder, filter, maxCount)",
                // Prefab tools
                "prefab.create" => "Create prefab from GameObject (params: source, path)",
                "prefab.instantiate" => "Instantiate prefab in scene (params: path or name, newName, x, y, z)",
                "prefab.open" => "Open prefab for editing (params: path or name)",
                "prefab.close" => "Close prefab editing mode",
                "prefab.save" => "Save currently edited prefab",
                // Asset tools
                "asset.find" => "Find assets in project (params: query, type, folder, maxCount)",
                "asset.copy" => "Copy asset to new path (params: source, destination)",
                "asset.move" => "Move/rename asset (params: source, destination)",
                "asset.delete" => "Delete asset (params: path, moveToTrash)",
                "asset.refresh" => "Refresh AssetDatabase (params: forceUpdate)",
                "asset.import" => "Import/reimport asset (params: path, forceUpdate)",
                "asset.getPath" => "Get asset path by name (params: name, type)",
                // Package Manager tools
                "package.add" => "Install package (params: package - name or git URL, timeout)",
                "package.remove" => "Remove package (params: package, timeout)",
                "package.list" => "List installed packages (params: includeBuiltIn, timeout)",
                "package.search" => "Search Unity package registry (params: query, timeout)",
                // Test Runner tools
                "test.run" => "Run tests (params: mode - EditMode/PlayMode, filter, category)",
                "test.list" => "List available tests (params: mode)",
                "test.getResults" => "Get last test run results",
                _ => name
            };
        }
        
        private Dictionary<string, object> ParseJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return new Dictionary<string, object>();
            
            // Simple JSON parser for flat objects
            var result = new Dictionary<string, object>();
            json = json.Trim();
            
            if (!json.StartsWith("{")) return result;
            
            json = json.Substring(1, json.Length - 2); // Remove { }
            
            // Very basic parsing - real implementation should use JsonUtility or Newtonsoft
            var parts = json.Split(',');
            foreach (var part in parts)
            {
                var kv = part.Split(new[] { ':' }, 2);
                if (kv.Length == 2)
                {
                    var key = kv[0].Trim().Trim('"');
                    var value = kv[1].Trim();
                    
                    if (value.StartsWith("\""))
                        result[key] = value.Trim('"');
                    else if (value == "true")
                        result[key] = true;
                    else if (value == "false")
                        result[key] = false;
                    else if (value == "null")
                        result[key] = null;
                    else if (float.TryParse(value, out var f))
                        result[key] = f;
                    else
                        result[key] = value;
                }
            }
            
            return result;
        }
        
        private string GetString(Dictionary<string, object> p, string key, string defaultValue)
        {
            return p.TryGetValue(key, out var v) && v != null ? v.ToString() : defaultValue;
        }
        
        private int GetInt(Dictionary<string, object> p, string key, int defaultValue)
        {
            if (p.TryGetValue(key, out var v))
            {
                if (v is int i) return i;
                if (v is float f) return (int)f;
                if (int.TryParse(v?.ToString(), out var parsed)) return parsed;
            }
            return defaultValue;
        }
        
        private float GetFloat(Dictionary<string, object> p, string key, float defaultValue)
        {
            if (p.TryGetValue(key, out var v))
            {
                if (v is float f) return f;
                if (v is int i) return i;
                if (float.TryParse(v?.ToString(), out var parsed)) return parsed;
            }
            return defaultValue;
        }
        
        private bool GetBool(Dictionary<string, object> p, string key, bool defaultValue)
        {
            if (p.TryGetValue(key, out var v))
            {
                if (v is bool b) return b;
                if (bool.TryParse(v?.ToString(), out var parsed)) return parsed;
            }
            return defaultValue;
        }
        
        private Vector3 ParseVector3(object value)
        {
            if (value is Dictionary<string, object> dict)
            {
                return new Vector3(
                    GetFloat(dict, "x", 0),
                    GetFloat(dict, "y", 0),
                    GetFloat(dict, "z", 0)
                );
            }
            return Vector3.zero;
        }
        
        private Dictionary<string, float> Vec3ToDict(Vector3 v)
        {
            return new Dictionary<string, float> { { "x", v.x }, { "y", v.y }, { "z", v.z } };
        }
        
        private Type FindType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            
            // Try direct match first
            var type = Type.GetType(typeName);
            if (type != null) return type;
            
            // Search in all assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null) return type;
                
                // Try with UnityEngine prefix
                type = assembly.GetType($"UnityEngine.{typeName}");
                if (type != null) return type;
            }
            
            return null;
        }
        
        private object SerializeValue(object value)
        {
            if (value == null) return null;
            
            var type = value.GetType();
            
            if (type.IsPrimitive || type == typeof(string))
                return value;
            
            if (value is Vector3 v3)
                return Vec3ToDict(v3);
            
            if (value is Vector2 v2)
                return new Dictionary<string, float> { { "x", v2.x }, { "y", v2.y } };
            
            if (value is Quaternion q)
                return new Dictionary<string, float> { { "x", q.x }, { "y", q.y }, { "z", q.z }, { "w", q.w } };
            
            if (value is Color c)
                return new Dictionary<string, float> { { "r", c.r }, { "g", c.g }, { "b", c.b }, { "a", c.a } };
            
            if (value is UnityEngine.Object obj)
                return obj.name;
            
            return value.ToString();
        }
        
        private object ConvertValue(object value, Type targetType)
        {
            if (value == null) return null;
            
            if (targetType == typeof(string))
                return value.ToString();
            
            if (targetType == typeof(int))
                return Convert.ToInt32(value);
            
            if (targetType == typeof(float))
                return Convert.ToSingle(value);
            
            if (targetType == typeof(bool))
                return Convert.ToBoolean(value);
            
            if (targetType == typeof(Vector3) && value is Dictionary<string, object> dict)
                return ParseVector3(dict);
            
            return value;
        }
        
        #endregion
    }
}
