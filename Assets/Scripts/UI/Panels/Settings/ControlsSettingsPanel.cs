using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls settings sub-panel.
/// Displays keybinds for UI panel shortcuts.
/// </summary>
public class ControlsSettingsPanel : MonoBehaviour
{
    [Header("Keybind Display Texts")]
    [SerializeField] private TextMeshProUGUI characterPanelKeyText;
    [SerializeField] private TextMeshProUGUI inventoryPanelKeyText;
    [SerializeField] private TextMeshProUGUI talentPanelKeyText;
    [SerializeField] private TextMeshProUGUI skillPanelKeyText;
    [SerializeField] private TextMeshProUGUI professionsPanelKeyText;
    [SerializeField] private TextMeshProUGUI settingsMenuKeyText;
    
    [Header("Parent Reference")]
    [SerializeField] private SettingsPanel settingsPanel;
    
    private SettingsData settingsData;
    
    void OnEnable()
    {
        RefreshUI();
    }
    
    /// <summary>
    /// Refresh UI to display current keybinds
    /// </summary>
    public void RefreshUI()
    {
        if (settingsPanel != null)
            settingsData = settingsPanel.GetSettingsData();
        
        if (settingsData == null)
        {
            Debug.LogWarning("[ControlsSettingsPanel] No settings data available");
            SetDefaultDisplay();
            return;
        }
        
        // Update keybind displays
        UpdateKeybindText(characterPanelKeyText, "Character Panel", settingsData.GetCharacterPanelKey());
        UpdateKeybindText(inventoryPanelKeyText, "Inventory", settingsData.GetInventoryPanelKey());
        UpdateKeybindText(talentPanelKeyText, "Talents", settingsData.GetTalentPanelKey());
        UpdateKeybindText(skillPanelKeyText, "Skills", settingsData.GetSkillPanelKey());
        UpdateKeybindText(professionsPanelKeyText, "Professions", settingsData.GetProfessionsPanelKey());
        UpdateKeybindText(settingsMenuKeyText, "Settings Menu", settingsData.GetSettingsMenuKey());
    }
    
    /// <summary>
    /// Set default display when no settings available
    /// </summary>
    private void SetDefaultDisplay()
    {
        UpdateKeybindText(characterPanelKeyText, "Character Panel", KeyCode.C);
        UpdateKeybindText(inventoryPanelKeyText, "Inventory", KeyCode.I);
        UpdateKeybindText(talentPanelKeyText, "Talents", KeyCode.N);
        UpdateKeybindText(skillPanelKeyText, "Skills", KeyCode.P);
        UpdateKeybindText(professionsPanelKeyText, "Professions", KeyCode.K);
        UpdateKeybindText(settingsMenuKeyText, "Settings Menu", KeyCode.Escape);
    }
    
    /// <summary>
    /// Update a keybind text display
    /// </summary>
    private void UpdateKeybindText(TextMeshProUGUI textComponent, string actionName, KeyCode key)
    {
        if (textComponent != null)
        {
            textComponent.text = GetKeyDisplayName(key);
        }
    }
    
    /// <summary>
    /// Get a display-friendly name for a KeyCode
    /// </summary>
    private string GetKeyDisplayName(KeyCode key)
    {
        return key switch
        {
            KeyCode.Escape => "ESC",
            KeyCode.Return => "Enter",
            KeyCode.Space => "Space",
            KeyCode.Tab => "Tab",
            KeyCode.BackQuote => "`",
            KeyCode.LeftShift => "L-Shift",
            KeyCode.RightShift => "R-Shift",
            KeyCode.LeftControl => "L-Ctrl",
            KeyCode.RightControl => "R-Ctrl",
            KeyCode.LeftAlt => "L-Alt",
            KeyCode.RightAlt => "R-Alt",
            KeyCode.Alpha0 => "0",
            KeyCode.Alpha1 => "1",
            KeyCode.Alpha2 => "2",
            KeyCode.Alpha3 => "3",
            KeyCode.Alpha4 => "4",
            KeyCode.Alpha5 => "5",
            KeyCode.Alpha6 => "6",
            KeyCode.Alpha7 => "7",
            KeyCode.Alpha8 => "8",
            KeyCode.Alpha9 => "9",
            KeyCode.Keypad0 => "Num 0",
            KeyCode.Keypad1 => "Num 1",
            KeyCode.Keypad2 => "Num 2",
            KeyCode.Keypad3 => "Num 3",
            KeyCode.Keypad4 => "Num 4",
            KeyCode.Keypad5 => "Num 5",
            KeyCode.Keypad6 => "Num 6",
            KeyCode.Keypad7 => "Num 7",
            KeyCode.Keypad8 => "Num 8",
            KeyCode.Keypad9 => "Num 9",
            KeyCode.Mouse0 => "LMB",
            KeyCode.Mouse1 => "RMB",
            KeyCode.Mouse2 => "MMB",
            _ => key.ToString().ToUpper()
        };
    }
}

