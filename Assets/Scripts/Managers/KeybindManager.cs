using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages keyboard shortcuts for opening UI panels.
/// Persists across scenes and reads keybinds from SettingsData.
/// Uses the new Input System.
/// </summary>
public class KeybindManager : MonoBehaviour, IKeybindService
{
    private SettingsData settingsData;
    
    // Cached panel references (found per-scene)
    private CharacterPanel characterPanel;
    private InventoryPanel inventoryPanel;
    private TalentPanel talentPanel;
    private SpellbookPanel spellbookPanel;
    private ProfessionPanel professionPanel;
    
    private bool panelsCached = false;
    private string lastSceneName = "";
    private bool keybindsEnabled = true;
    
    /// <summary>
    /// Check if keybinds are currently enabled
    /// </summary>
    public bool AreKeybindsEnabled => keybindsEnabled;
    
    void Awake()
    {
        // Prevent duplicates - use Services pattern like other managers
        if (Services.IsRegistered<IKeybindService>())
        {
            Debug.LogWarning("[KeybindManager] Service already registered. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        Services.Register<IKeybindService>(this);
    }
    
    void OnDestroy()
    {
        // Unregister from Services when destroyed
        if (Services.IsRegistered<IKeybindService>() && Services.Get<IKeybindService>() == (IKeybindService)this)
        {
            Services.Unregister<IKeybindService>();
        }
    }
    
    void Update()
    {
        // Skip if keybinds are disabled or no keyboard
        if (!keybindsEnabled) return;
        if (Keyboard.current == null) return;
        
        // Get settings from ISettingsService (single source of truth)
        RefreshSettingsFromService();
        
        // Refresh panel references if scene changed
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentScene != lastSceneName)
        {
            lastSceneName = currentScene;
            panelsCached = false;
        }
        
        // Cache panels if needed
        if (!panelsCached)
        {
            CachePanelReferences();
        }
        
        // Check for keybind presses
        HandleKeybinds();
    }
    
    /// <summary>
    /// Get settings from ISettingsService (avoids duplicate loading logic)
    /// </summary>
    private void RefreshSettingsFromService()
    {
        // Only refresh if service is available (SettingsPanel may not be loaded yet)
        if (!Services.IsRegistered<ISettingsService>())
        {
            // Fall back to loading directly if service not available yet
            if (settingsData == null)
            {
                LoadSettingsFallback();
            }
            return;
        }
        
        var settingsService = Services.Get<ISettingsService>();
        if (settingsService != null)
        {
            var newSettings = settingsService.GetSettingsData();
            if (newSettings != null)
            {
                settingsData = newSettings;
            }
        }
    }
    
    /// <summary>
    /// Fallback for loading settings when ISettingsService isn't available yet
    /// </summary>
    private void LoadSettingsFallback()
    {
        AccountSaveData accountData = SaveSystem.LoadAccount();
        if (accountData != null && accountData.settingsData != null)
        {
            settingsData = accountData.settingsData;
        }
        else
        {
            settingsData = SettingsData.CreateDefault();
        }
    }
    
    /// <summary>
    /// Cache references to panels in the current scene
    /// </summary>
    private void CachePanelReferences()
    {
        // Use FindObjectsByType with Include inactive to find disabled panels
        characterPanel = FindAnyObjectByType<CharacterPanel>(FindObjectsInactive.Include);
        inventoryPanel = FindAnyObjectByType<InventoryPanel>(FindObjectsInactive.Include);
        talentPanel = FindAnyObjectByType<TalentPanel>(FindObjectsInactive.Include);
        spellbookPanel = FindAnyObjectByType<SpellbookPanel>(FindObjectsInactive.Include);
        professionPanel = FindAnyObjectByType<ProfessionPanel>(FindObjectsInactive.Include);
        
        panelsCached = true;
        
        // Debug logging to verify panels were found
        Debug.Log($"[KeybindManager] Cached panels - Character: {characterPanel != null}, Inventory: {inventoryPanel != null}, Talent: {talentPanel != null}, Spellbook: {spellbookPanel != null}, Profession: {professionPanel != null}");
    }
    
    /// <summary>
    /// Handle keybind input using the new Input System
    /// </summary>
    private void HandleKeybinds()
    {
        if (settingsData == null) return;
        
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        
        // Don't process keybinds if settings panel is open
        var settingsService = Services.Get<ISettingsService>();
        if (settingsService != null && settingsService.IsOpen)
        {
            // Only allow Escape to close settings when it's open
            if (WasKeyPressedThisFrame(settingsData.GetSettingsMenuKey()))
            {
                settingsService.CloseSettings();
            }
            return;
        }
        
        // Don't process keybinds if typing in an input field
        if (IsInputFieldFocused())
            return;
        
        // Settings Menu (Escape)
        if (WasKeyPressedThisFrame(settingsData.GetSettingsMenuKey()))
        {
            settingsService?.TogglePanel();
        }
        
        // Character Panel
        if (WasKeyPressedThisFrame(settingsData.GetCharacterPanelKey()))
        {
            characterPanel?.TogglePanel();
        }
        
        // Inventory Panel
        if (WasKeyPressedThisFrame(settingsData.GetInventoryPanelKey()))
        {
            inventoryPanel?.TogglePanel();
        }
        
        // Talent Panel
        if (WasKeyPressedThisFrame(settingsData.GetTalentPanelKey()))
        {
            talentPanel?.TogglePanel();
        }
        
        // Skills Panel (Spellbook)
        if (WasKeyPressedThisFrame(settingsData.GetSkillPanelKey()))
        {
            spellbookPanel?.TogglePanel();
        }
        
        // Professions Panel
        if (WasKeyPressedThisFrame(settingsData.GetProfessionsPanelKey()))
        {
            professionPanel?.TogglePanel();
        }
    }
    
    /// <summary>
    /// Check if a key was pressed this frame using the new Input System
    /// </summary>
    private bool WasKeyPressedThisFrame(KeyCode keyCode)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return false;
        
        Key key = KeyCodeToKey(keyCode);
        if (key == Key.None) return false;
        
        return keyboard[key].wasPressedThisFrame;
    }
    
    /// <summary>
    /// Convert legacy KeyCode to new Input System Key
    /// </summary>
    private Key KeyCodeToKey(KeyCode keyCode)
    {
        return keyCode switch
        {
            // Letters
            KeyCode.A => Key.A,
            KeyCode.B => Key.B,
            KeyCode.C => Key.C,
            KeyCode.D => Key.D,
            KeyCode.E => Key.E,
            KeyCode.F => Key.F,
            KeyCode.G => Key.G,
            KeyCode.H => Key.H,
            KeyCode.I => Key.I,
            KeyCode.J => Key.J,
            KeyCode.K => Key.K,
            KeyCode.L => Key.L,
            KeyCode.M => Key.M,
            KeyCode.N => Key.N,
            KeyCode.O => Key.O,
            KeyCode.P => Key.P,
            KeyCode.Q => Key.Q,
            KeyCode.R => Key.R,
            KeyCode.S => Key.S,
            KeyCode.T => Key.T,
            KeyCode.U => Key.U,
            KeyCode.V => Key.V,
            KeyCode.W => Key.W,
            KeyCode.X => Key.X,
            KeyCode.Y => Key.Y,
            KeyCode.Z => Key.Z,
            
            // Numbers
            KeyCode.Alpha0 => Key.Digit0,
            KeyCode.Alpha1 => Key.Digit1,
            KeyCode.Alpha2 => Key.Digit2,
            KeyCode.Alpha3 => Key.Digit3,
            KeyCode.Alpha4 => Key.Digit4,
            KeyCode.Alpha5 => Key.Digit5,
            KeyCode.Alpha6 => Key.Digit6,
            KeyCode.Alpha7 => Key.Digit7,
            KeyCode.Alpha8 => Key.Digit8,
            KeyCode.Alpha9 => Key.Digit9,
            
            // Function keys
            KeyCode.F1 => Key.F1,
            KeyCode.F2 => Key.F2,
            KeyCode.F3 => Key.F3,
            KeyCode.F4 => Key.F4,
            KeyCode.F5 => Key.F5,
            KeyCode.F6 => Key.F6,
            KeyCode.F7 => Key.F7,
            KeyCode.F8 => Key.F8,
            KeyCode.F9 => Key.F9,
            KeyCode.F10 => Key.F10,
            KeyCode.F11 => Key.F11,
            KeyCode.F12 => Key.F12,
            
            // Special keys
            KeyCode.Escape => Key.Escape,
            KeyCode.Tab => Key.Tab,
            KeyCode.Space => Key.Space,
            KeyCode.Return => Key.Enter,
            KeyCode.Backspace => Key.Backspace,
            KeyCode.Delete => Key.Delete,
            KeyCode.Insert => Key.Insert,
            KeyCode.Home => Key.Home,
            KeyCode.End => Key.End,
            KeyCode.PageUp => Key.PageUp,
            KeyCode.PageDown => Key.PageDown,
            
            // Modifiers
            KeyCode.LeftShift => Key.LeftShift,
            KeyCode.RightShift => Key.RightShift,
            KeyCode.LeftControl => Key.LeftCtrl,
            KeyCode.RightControl => Key.RightCtrl,
            KeyCode.LeftAlt => Key.LeftAlt,
            KeyCode.RightAlt => Key.RightAlt,
            
            // Arrow keys
            KeyCode.UpArrow => Key.UpArrow,
            KeyCode.DownArrow => Key.DownArrow,
            KeyCode.LeftArrow => Key.LeftArrow,
            KeyCode.RightArrow => Key.RightArrow,
            
            // Numpad
            KeyCode.Keypad0 => Key.Numpad0,
            KeyCode.Keypad1 => Key.Numpad1,
            KeyCode.Keypad2 => Key.Numpad2,
            KeyCode.Keypad3 => Key.Numpad3,
            KeyCode.Keypad4 => Key.Numpad4,
            KeyCode.Keypad5 => Key.Numpad5,
            KeyCode.Keypad6 => Key.Numpad6,
            KeyCode.Keypad7 => Key.Numpad7,
            KeyCode.Keypad8 => Key.Numpad8,
            KeyCode.Keypad9 => Key.Numpad9,
            
            // Punctuation
            KeyCode.BackQuote => Key.Backquote,
            KeyCode.Minus => Key.Minus,
            KeyCode.Equals => Key.Equals,
            KeyCode.LeftBracket => Key.LeftBracket,
            KeyCode.RightBracket => Key.RightBracket,
            KeyCode.Backslash => Key.Backslash,
            KeyCode.Semicolon => Key.Semicolon,
            KeyCode.Quote => Key.Quote,
            KeyCode.Comma => Key.Comma,
            KeyCode.Period => Key.Period,
            KeyCode.Slash => Key.Slash,
            
            _ => Key.None
        };
    }
    
    /// <summary>
    /// Check if an input field is currently focused
    /// </summary>
    private bool IsInputFieldFocused()
    {
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem == null) return false;
        
        var selected = eventSystem.currentSelectedGameObject;
        if (selected == null) return false;
        
        // Check for TMP input field
        if (selected.GetComponent<TMPro.TMP_InputField>() != null)
            return true;
        
        // Check for legacy input field
        if (selected.GetComponent<UnityEngine.UI.InputField>() != null)
            return true;
        
        return false;
    }
    
    /// <summary>
    /// Refresh settings (call after settings are changed)
    /// </summary>
    public void RefreshSettings()
    {
        RefreshSettingsFromService();
        Debug.Log("[KeybindManager] Settings refreshed");
    }
    
    /// <summary>
    /// Temporarily disable all keybinds (for dialogues, cutscenes, etc.)
    /// </summary>
    public void DisableKeybinds()
    {
        keybindsEnabled = false;
        Debug.Log("[KeybindManager] Keybinds disabled");
    }
    
    /// <summary>
    /// Re-enable keybinds after being disabled
    /// </summary>
    public void EnableKeybinds()
    {
        keybindsEnabled = true;
        Debug.Log("[KeybindManager] Keybinds enabled");
    }
}
