/*
 * OpenClaw Unity Plugin
 * https://github.com/TomLeeLive/openclaw-unity-plugin
 * MIT License
 */

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace OpenClaw.Unity.Editor
{
    /// <summary>
    /// Setup utilities for OpenClaw Unity Plugin.
    /// </summary>
    public static class OpenClawSetup
    {
        [MenuItem("GameObject/OpenClaw/Add Bridge to Scene", false, 10)]
        public static void AddBridgeToScene()
        {
            // Check if bridge already exists
            var existing = Object.FindFirstObjectByType<OpenClawBridge>();
            if (existing != null)
            {
                Debug.Log("[OpenClaw] Bridge already exists in scene.");
                Selection.activeGameObject = existing.gameObject;
                return;
            }
            
            // Create bridge GameObject
            var bridgeGO = new GameObject("OpenClaw Plugin");
            bridgeGO.AddComponent<OpenClawBridge>();
            
            // Add status overlay component
            bridgeGO.AddComponent<OpenClawStatusOverlay>();
            
            Undo.RegisterCreatedObjectUndo(bridgeGO, "Add OpenClaw Plugin");
            Selection.activeGameObject = bridgeGO;
            
            Debug.Log("[OpenClaw] Bridge added to scene. Configure in Window > OpenClaw Plugin.");
            
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        
        [MenuItem("GameObject/OpenClaw/Add Bridge to Scene", true)]
        public static bool AddBridgeToSceneValidation()
        {
            return !EditorApplication.isPlaying;
        }
        
        [MenuItem("Assets/Create/OpenClaw/Config", false, 100)]
        public static void CreateConfig()
        {
            var config = ScriptableObject.CreateInstance<OpenClawConfig>();
            
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
            {
                path = "Assets";
            }
            else if (System.IO.Path.GetExtension(path) != "")
            {
                path = System.IO.Path.GetDirectoryName(path);
            }
            
            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/OpenClawConfig.asset");
            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = config;
            
            Debug.Log($"[OpenClaw] Config created at {assetPath}. Move to Resources folder for auto-loading.");
        }
        
        [MenuItem("Window/OpenClaw Plugin/Quick Setup", false, 1)]
        public static void QuickSetup()
        {
            // 1. Ensure config exists
            var config = Resources.Load<OpenClawConfig>("OpenClawConfig");
            if (config == null)
            {
                // Create Resources folder if needed
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                
                config = ScriptableObject.CreateInstance<OpenClawConfig>();
                AssetDatabase.CreateAsset(config, "Assets/Resources/OpenClawConfig.asset");
                AssetDatabase.SaveAssets();
                
                Debug.Log("[OpenClaw] Created config at Assets/Resources/OpenClawConfig.asset");
            }
            
            // 2. Add bridge to scene if not present
            var existing = Object.FindFirstObjectByType<OpenClawBridge>();
            if (existing == null)
            {
                AddBridgeToScene();
            }
            
            // 3. Open config window
            OpenClawWindow.ShowWindow();
            
            Debug.Log("[OpenClaw] Quick setup complete! Configure your gateway URL in the window.");
        }
        
        [MenuItem("Window/OpenClaw Plugin/Documentation", false, 100)]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/TomLeeLive/openclaw-unity-plugin");
        }
    }
}
