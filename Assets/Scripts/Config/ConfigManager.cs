using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Manages loading and saving of game balance configuration from JSON.
/// Config can be edited in any text editor without opening Unity.
/// </summary>
public class ConfigManager : MonoBehaviour, IConfigService
{
    private GameBalanceConfig config;
    
    /// <summary>
    /// Path to the config file in StreamingAssets (persists in builds)
    /// </summary>
    private string ConfigPath => Path.Combine(Application.streamingAssetsPath, "GameBalance.json");
    
    void Awake()
    {
        // Prevent duplicates - GameBootstrap creates all managers
        if (Services.IsRegistered<IConfigService>())
        {
            Debug.LogWarning("[ConfigManager] Service already registered. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        Services.Register<IConfigService>(this);
        LoadConfig();
    }
    
    /// <summary>
    /// Load configuration from JSON file. Creates default if missing.
    /// </summary>
    private void LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                config = JsonUtility.FromJson<GameBalanceConfig>(json);
                Debug.Log($"[ConfigManager] Loaded config from {ConfigPath}");
            }
            else
            {
                Debug.LogWarning($"[ConfigManager] Config file not found at {ConfigPath}. Creating default config.");
                config = new GameBalanceConfig();
                SaveConfig();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConfigManager] Failed to load config: {e.Message}");
            Debug.LogWarning("[ConfigManager] Using default config values as fallback.");
            config = new GameBalanceConfig(); // Fallback to defaults
        }
    }
    
    /// <summary>
    /// Save current config to JSON file.
    /// </summary>
    public void SaveConfig()
    {
        try
        {
            string json = JsonUtility.ToJson(config, true); // true = pretty print
            
            // Ensure directory exists
            string directory = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(ConfigPath, json);
            Debug.Log($"[ConfigManager] Saved config to {ConfigPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConfigManager] Failed to save config: {e.Message}");
        }
    }
    
    /// <summary>
    /// Reload config from disk. Useful for hot-reloading during development.
    /// Can be called from Unity menu or during play mode to see changes immediately.
    /// </summary>
    [ContextMenu("Reload Config")]
    public void ReloadConfig()
    {
        LoadConfig();
        Debug.Log("[ConfigManager] Config reloaded from disk!");
        
        // Optionally: Publish event to notify systems that config changed
        // EventBus.Publish(new ConfigReloadedEvent());
    }
    
    /// <summary>
    /// Reset config to default values and save.
    /// </summary>
    [ContextMenu("Reset to Defaults")]
    public void ResetToDefaults()
    {
        config = new GameBalanceConfig();
        SaveConfig();
        Debug.Log("[ConfigManager] Config reset to default values.");
    }
    
    void OnDestroy()
    {
        Services.Unregister<IConfigService>();
    }
    
    // IConfigService implementation
    public GameBalanceConfig GetConfig() => config ?? new GameBalanceConfig();
}

