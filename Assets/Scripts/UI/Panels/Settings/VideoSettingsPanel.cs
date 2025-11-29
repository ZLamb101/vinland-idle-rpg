using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Video settings sub-panel.
/// Handles resolution, display mode, VSync, and graphics quality.
/// </summary>
public class VideoSettingsPanel : MonoBehaviour
{
    [Header("Dropdowns")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown displayModeDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    
    [Header("Toggles")]
    [SerializeField] private Toggle vSyncToggle;
    
    [Header("Apply Button")]
    [SerializeField] private Button applyButton;
    
    [Header("Parent Reference")]
    [SerializeField] private SettingsPanel settingsPanel;
    
    private SettingsData settingsData;
    private Resolution[] availableResolutions;
    private List<Resolution> filteredResolutions = new List<Resolution>();
    
    void Awake()
    {
        // Setup listeners
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (displayModeDropdown != null)
            displayModeDropdown.onValueChanged.AddListener(OnDisplayModeChanged);
        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        if (vSyncToggle != null)
            vSyncToggle.onValueChanged.AddListener(OnVSyncToggled);
        if (applyButton != null)
            applyButton.onClick.AddListener(ApplySettings);
    }
    
    void OnEnable()
    {
        RefreshUI();
    }
    
    /// <summary>
    /// Refresh UI to match current settings and populate dropdowns
    /// </summary>
    public void RefreshUI()
    {
        if (settingsPanel != null)
            settingsData = settingsPanel.GetSettingsData();
        
        if (settingsData == null)
        {
            Debug.LogWarning("[VideoSettingsPanel] No settings data available");
            return;
        }
        
        PopulateResolutionDropdown();
        PopulateDisplayModeDropdown();
        PopulateQualityDropdown();
        
        // Update VSync toggle
        if (vSyncToggle != null)
            vSyncToggle.SetIsOnWithoutNotify(settingsData.vSyncEnabled);
    }
    
    /// <summary>
    /// Populate the resolution dropdown with available resolutions
    /// </summary>
    private void PopulateResolutionDropdown()
    {
        if (resolutionDropdown == null) return;
        
        resolutionDropdown.ClearOptions();
        filteredResolutions.Clear();
        
        availableResolutions = Screen.resolutions;
        HashSet<string> addedResolutions = new HashSet<string>();
        List<string> options = new List<string>();
        
        int currentResolutionIndex = 0;
        Resolution currentRes = Screen.currentResolution;
        
        // Filter to unique resolutions (ignore refresh rate duplicates)
        for (int i = availableResolutions.Length - 1; i >= 0; i--)
        {
            Resolution res = availableResolutions[i];
            string key = $"{res.width}x{res.height}";
            
            if (!addedResolutions.Contains(key))
            {
                addedResolutions.Add(key);
                filteredResolutions.Add(res);
                options.Add($"{res.width} x {res.height}");
                
                // Check if this is the current resolution
                if (res.width == currentRes.width && res.height == currentRes.height)
                {
                    currentResolutionIndex = filteredResolutions.Count - 1;
                }
            }
        }
        
        // Reverse to have highest resolution first
        options.Reverse();
        filteredResolutions.Reverse();
        currentResolutionIndex = filteredResolutions.Count - 1 - currentResolutionIndex;
        
        resolutionDropdown.AddOptions(options);
        
        // Set current value
        if (settingsData.resolutionIndex >= 0 && settingsData.resolutionIndex < filteredResolutions.Count)
        {
            resolutionDropdown.SetValueWithoutNotify(settingsData.resolutionIndex);
        }
        else
        {
            resolutionDropdown.SetValueWithoutNotify(currentResolutionIndex);
            settingsData.resolutionIndex = currentResolutionIndex;
        }
    }
    
    /// <summary>
    /// Populate the display mode dropdown
    /// </summary>
    private void PopulateDisplayModeDropdown()
    {
        if (displayModeDropdown == null) return;
        
        displayModeDropdown.ClearOptions();
        displayModeDropdown.AddOptions(new List<string>
        {
            "Fullscreen",
            "Windowed",
            "Borderless Window"
        });
        
        // Determine current mode
        int currentMode = settingsData.displayMode;
        if (currentMode < 0 || currentMode > 2)
        {
            currentMode = Screen.fullScreenMode switch
            {
                FullScreenMode.ExclusiveFullScreen => 0,
                FullScreenMode.Windowed => 1,
                FullScreenMode.FullScreenWindow => 2,
                FullScreenMode.MaximizedWindow => 2,
                _ => 0
            };
            settingsData.displayMode = currentMode;
        }
        
        displayModeDropdown.SetValueWithoutNotify(currentMode);
    }
    
    /// <summary>
    /// Populate the quality dropdown with Unity quality levels
    /// </summary>
    private void PopulateQualityDropdown()
    {
        if (qualityDropdown == null) return;
        
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        
        // Set current value
        int currentQuality = settingsData.qualityLevel >= 0 
            ? settingsData.qualityLevel 
            : QualitySettings.GetQualityLevel();
        
        settingsData.qualityLevel = currentQuality;
        qualityDropdown.SetValueWithoutNotify(currentQuality);
    }
    
    private void OnResolutionChanged(int index)
    {
        if (settingsData == null) return;
        settingsData.resolutionIndex = index;
    }
    
    private void OnDisplayModeChanged(int index)
    {
        if (settingsData == null) return;
        settingsData.displayMode = index;
    }
    
    private void OnQualityChanged(int index)
    {
        if (settingsData == null) return;
        settingsData.qualityLevel = index;
    }
    
    private void OnVSyncToggled(bool isOn)
    {
        if (settingsData == null) return;
        settingsData.vSyncEnabled = isOn;
    }
    
    /// <summary>
    /// Apply all video settings immediately
    /// </summary>
    public void ApplySettings()
    {
        if (settingsData == null) return;
        
        // Use centralized apply method (single source of truth)
        settingsData.ApplyVideoSettings();
        
        // Save settings
        if (settingsPanel != null)
        {
            settingsPanel.SaveSettings();
        }
    }
}

