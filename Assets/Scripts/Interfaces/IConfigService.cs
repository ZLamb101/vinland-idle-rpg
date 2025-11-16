/// <summary>
/// Interface for game balance configuration service
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// Get the current game balance configuration
    /// </summary>
    GameBalanceConfig GetConfig();
    
    /// <summary>
    /// Reload configuration from disk (hot-reload during development)
    /// </summary>
    void ReloadConfig();
    
    /// <summary>
    /// Save current configuration to disk
    /// </summary>
    void SaveConfig();
    
    /// <summary>
    /// Reset configuration to default values
    /// </summary>
    void ResetToDefaults();
}

