/// <summary>
/// Service interface for the Keybind Manager.
/// Handles keyboard shortcuts for UI panels.
/// </summary>
public interface IKeybindService
{
    /// <summary>
    /// Refresh keybind settings from AccountSaveData.
    /// Call this after settings are changed.
    /// </summary>
    void RefreshSettings();
    
    /// <summary>
    /// Temporarily disable all keybinds (for dialogues, cutscenes, etc.)
    /// </summary>
    void DisableKeybinds();
    
    /// <summary>
    /// Re-enable keybinds after being disabled
    /// </summary>
    void EnableKeybinds();
    
    /// <summary>
    /// Check if keybinds are currently enabled
    /// </summary>
    bool AreKeybindsEnabled { get; }
}

