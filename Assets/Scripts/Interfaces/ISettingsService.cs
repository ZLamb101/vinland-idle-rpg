/// <summary>
/// Service interface for the Settings panel.
/// Allows accessing settings from any scene.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Toggle the settings panel open/closed
    /// </summary>
    void TogglePanel();
    
    /// <summary>
    /// Open the settings panel
    /// </summary>
    void OpenSettings();
    
    /// <summary>
    /// Close the settings panel (saves settings)
    /// </summary>
    void CloseSettings();
    
    /// <summary>
    /// Check if the settings panel is currently open
    /// </summary>
    bool IsOpen { get; }
    
    /// <summary>
    /// Get the current settings data
    /// </summary>
    SettingsData GetSettingsData();
}

