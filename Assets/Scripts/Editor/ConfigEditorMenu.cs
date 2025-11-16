#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using UnityEngine;

/// <summary>
/// Editor menu shortcuts for config management.
/// Adds "Vinland" menu to Unity's top menu bar with quick access to config tools.
/// </summary>
public static class ConfigEditorMenu
{
    /// <summary>
    /// Open the config file location in Windows Explorer/Finder
    /// </summary>
    [MenuItem("Vinland/Open Config Folder")]
    private static void OpenConfigFolder()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "GameBalance.json");
        
        if (File.Exists(path))
        {
            EditorUtility.RevealInFinder(path);
        }
        else
        {
            Debug.LogWarning($"[ConfigEditor] Config file not found at: {path}");
            // Open the folder anyway
            EditorUtility.RevealInFinder(Application.streamingAssetsPath);
        }
    }
    
    /// <summary>
    /// Reload config from disk (useful during play mode for hot-reloading)
    /// </summary>
    [MenuItem("Vinland/Reload Config")]
    private static void ReloadConfig()
    {
        var configService = Services.Get<IConfigService>();
        if (configService != null)
        {
            configService.ReloadConfig();
            Debug.Log("[ConfigEditor] ✓ Config reloaded from disk!");
        }
        else
        {
            Debug.LogWarning("[ConfigEditor] ConfigService not found. Enter play mode first, or ensure ConfigManager exists.");
        }
    }
    
    /// <summary>
    /// Validate that reload is only available when config service exists
    /// </summary>
    [MenuItem("Vinland/Reload Config", true)]
    private static bool ValidateReloadConfig()
    {
        return Application.isPlaying && Services.Get<IConfigService>() != null;
    }
}
#endif

