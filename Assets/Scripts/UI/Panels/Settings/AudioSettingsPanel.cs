using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Audio settings sub-panel.
/// Placeholder implementation - sliders are wired but no audio system yet.
/// </summary>
public class AudioSettingsPanel : MonoBehaviour
{
    [Header("Volume Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    
    [Header("Volume Labels")]
    [SerializeField] private TextMeshProUGUI masterVolumeLabel;
    [SerializeField] private TextMeshProUGUI musicVolumeLabel;
    [SerializeField] private TextMeshProUGUI sfxVolumeLabel;
    
    [Header("Toggles")]
    [SerializeField] private Toggle uiSoundsToggle;
    
    [Header("Parent Reference")]
    [SerializeField] private SettingsPanel settingsPanel;
    
    private SettingsData settingsData;
    
    void Awake()
    {
        // Setup slider listeners
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
        
        if (uiSoundsToggle != null)
            uiSoundsToggle.onValueChanged.AddListener(OnUISoundsToggled);
    }
    
    void OnEnable()
    {
        RefreshUI();
    }
    
    /// <summary>
    /// Refresh UI to match current settings
    /// </summary>
    public void RefreshUI()
    {
        if (settingsPanel != null)
            settingsData = settingsPanel.GetSettingsData();
        
        if (settingsData == null)
        {
            Debug.LogWarning("[AudioSettingsPanel] No settings data available");
            return;
        }
        
        // Update sliders without triggering callbacks
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(settingsData.masterVolume);
            UpdateVolumeLabel(masterVolumeLabel, settingsData.masterVolume);
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(settingsData.musicVolume);
            UpdateVolumeLabel(musicVolumeLabel, settingsData.musicVolume);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(settingsData.sfxVolume);
            UpdateVolumeLabel(sfxVolumeLabel, settingsData.sfxVolume);
        }
        
        if (uiSoundsToggle != null)
            uiSoundsToggle.SetIsOnWithoutNotify(settingsData.uiSoundsEnabled);
    }
    
    private void OnMasterVolumeChanged(float value)
    {
        if (settingsData == null) return;
        
        settingsData.masterVolume = value;
        UpdateVolumeLabel(masterVolumeLabel, value);
        
        // TODO: Apply to AudioMixer when audio system is implemented
        // AudioManager.Instance?.SetMasterVolume(value);
    }
    
    private void OnMusicVolumeChanged(float value)
    {
        if (settingsData == null) return;
        
        settingsData.musicVolume = value;
        UpdateVolumeLabel(musicVolumeLabel, value);
        
        // TODO: Apply to AudioMixer when audio system is implemented
        // AudioManager.Instance?.SetMusicVolume(value);
    }
    
    private void OnSFXVolumeChanged(float value)
    {
        if (settingsData == null) return;
        
        settingsData.sfxVolume = value;
        UpdateVolumeLabel(sfxVolumeLabel, value);
        
        // TODO: Apply to AudioMixer when audio system is implemented
        // AudioManager.Instance?.SetSFXVolume(value);
    }
    
    private void OnUISoundsToggled(bool isOn)
    {
        if (settingsData == null) return;
        
        settingsData.uiSoundsEnabled = isOn;
        
        // TODO: Apply when audio system is implemented
        // AudioManager.Instance?.SetUISoundsEnabled(isOn);
    }
    
    private void UpdateVolumeLabel(TextMeshProUGUI label, float value)
    {
        if (label != null)
        {
            int percentage = Mathf.RoundToInt(value * 100f);
            label.text = $"{percentage}%";
        }
    }
}

