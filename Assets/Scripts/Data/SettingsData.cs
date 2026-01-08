using System;
using UnityEngine;

/// <summary>
/// Stores all game settings. Saved to AccountSaveData for account-wide persistence.
/// </summary>
[System.Serializable]
public class SettingsData
{
    // ═══════════════════════════════════════
    // Audio Settings (placeholder for future sound system)
    // ═══════════════════════════════════════
    
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    
    [Range(0f, 1f)]
    public float musicVolume = 1f;
    
    [Range(0f, 1f)]
    public float sfxVolume = 1f;
    
    public bool uiSoundsEnabled = true;
    
    // ═══════════════════════════════════════
    // Video Settings
    // ═══════════════════════════════════════
    
    /// <summary>
    /// Index into Screen.resolutions array
    /// </summary>
    public int resolutionIndex = -1; // -1 means use current/default
    
    /// <summary>
    /// 0 = Fullscreen, 1 = Windowed, 2 = Borderless Window
    /// </summary>
    public int displayMode = 0;
    
    public bool vSyncEnabled = true;
    
    /// <summary>
    /// Index into QualitySettings.names array
    /// </summary>
    public int qualityLevel = -1; // -1 means use current/default
    
    // ═══════════════════════════════════════
    // Keybind Settings
    // ═══════════════════════════════════════
    
    // Panel shortcuts (stored as KeyCode int values for serialization)
    public int characterPanelKey = (int)KeyCode.C;
    public int inventoryPanelKey = (int)KeyCode.I;
    public int talentPanelKey = (int)KeyCode.N;
    public int skillPanelKey = (int)KeyCode.P;
    public int professionsPanelKey = (int)KeyCode.K;
    public int settingsMenuKey = (int)KeyCode.Escape;
    
    // ═══════════════════════════════════════
    // UI Layout Settings
    // ═══════════════════════════════════════
    
    /// <summary>
    /// GameLog position as percentage of screen (0-1 range)
    /// X = -1 means use default position
    /// </summary>
    public Vector2 gameLogPosition = new Vector2(-1f, -1f);
    
    /// <summary>
    /// Create default settings
    /// </summary>
    public static SettingsData CreateDefault()
    {
        return new SettingsData();
    }
    
    /// <summary>
    /// Apply video settings to the game
    /// </summary>
    public void ApplyVideoSettings()
    {
        // Apply VSync
        QualitySettings.vSyncCount = vSyncEnabled ? 1 : 0;
        
        // Apply quality level
        if (qualityLevel >= 0 && qualityLevel < QualitySettings.names.Length)
        {
            QualitySettings.SetQualityLevel(qualityLevel, true);
        }
        
        // Apply resolution and display mode
        if (resolutionIndex >= 0 && resolutionIndex < Screen.resolutions.Length)
        {
            Resolution res = Screen.resolutions[resolutionIndex];
            FullScreenMode mode = GetFullScreenMode();
            Screen.SetResolution(res.width, res.height, mode, res.refreshRateRatio);
        }
        else if (displayMode != (int)Screen.fullScreenMode)
        {
            // Just apply display mode change with current resolution
            Screen.fullScreenMode = GetFullScreenMode();
        }
        
        Debug.Log($"[SettingsData] Applied video settings - VSync: {vSyncEnabled}, Quality: {qualityLevel}, Resolution: {resolutionIndex}, Mode: {displayMode}");
    }
    
    /// <summary>
    /// Get the FullScreenMode enum from our displayMode int
    /// </summary>
    public FullScreenMode GetFullScreenMode()
    {
        return displayMode switch
        {
            0 => FullScreenMode.ExclusiveFullScreen,
            1 => FullScreenMode.Windowed,
            2 => FullScreenMode.FullScreenWindow, // Borderless
            _ => FullScreenMode.ExclusiveFullScreen
        };
    }
    
    /// <summary>
    /// Get KeyCode for a panel shortcut
    /// </summary>
    public KeyCode GetCharacterPanelKey() => (KeyCode)characterPanelKey;
    public KeyCode GetInventoryPanelKey() => (KeyCode)inventoryPanelKey;
    public KeyCode GetTalentPanelKey() => (KeyCode)talentPanelKey;
    public KeyCode GetSkillPanelKey() => (KeyCode)skillPanelKey;
    public KeyCode GetProfessionsPanelKey() => (KeyCode)professionsPanelKey;
    public KeyCode GetSettingsMenuKey() => (KeyCode)settingsMenuKey;
}

