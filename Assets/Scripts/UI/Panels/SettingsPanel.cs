using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main settings menu panel controller.
/// Manages sub-panels for Controls, Audio, and Video settings.
/// Persists across scenes via DontDestroyOnLoad.
/// </summary>
public class SettingsPanel : MonoBehaviour, ISettingsService
{
    [Header("Sub-Panels")]
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject videoPanel;
    
    [Header("Category Buttons")]
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button audioButton;
    [SerializeField] private Button videoButton;
    
    [Header("Action Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button exitButton;
    
    [Header("Button Colors")]
    [SerializeField] private Color activeTabColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color inactiveTabColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    
    private SettingsData settingsData;
    private GameObject currentPanel;
    
    /// <summary>
    /// Check if the settings panel is currently open
    /// </summary>
    public bool IsOpen => gameObject.activeSelf;
    
    void Awake()
    {
        // Prevent duplicates - use Services pattern like other managers
        if (Services.IsRegistered<ISettingsService>())
        {
            Debug.LogWarning("[SettingsPanel] Service already registered. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        // DontDestroyOnLoad only works on root objects.
        // Move to root level before calling it, then set up our own Canvas.
        SetupPersistence();
        
        // Register with Services for access from any scene
        Services.Register<ISettingsService>(this);
        
        // Setup button listeners
        if (controlsButton != null)
            controlsButton.onClick.AddListener(ShowControlsPanel);
        if (audioButton != null)
            audioButton.onClick.AddListener(ShowAudioPanel);
        if (videoButton != null)
            videoButton.onClick.AddListener(ShowVideoPanel);
        if (resumeButton != null)
            resumeButton.onClick.AddListener(CloseSettings);
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitToDesktop);
    }
    
    /// <summary>
    /// Set up persistence across scenes.
    /// Moves to root and ensures we have our own Canvas.
    /// </summary>
    private void SetupPersistence()
    {
        // If we're already at root, just persist
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
            return;
        }
        
        // Get the source Canvas settings before unparenting
        Canvas sourceCanvas = GetComponentInParent<Canvas>();
        UnityEngine.UI.CanvasScaler sourceScaler = sourceCanvas?.GetComponent<UnityEngine.UI.CanvasScaler>();
        
        // Move to root
        transform.SetParent(null);
        
        // Add our own Canvas if we don't have one
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Render on top of other UI
        }
        
        // Add CanvasScaler if needed (copy settings from source)
        UnityEngine.UI.CanvasScaler scaler = GetComponent<UnityEngine.UI.CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            if (sourceScaler != null)
            {
                scaler.uiScaleMode = sourceScaler.uiScaleMode;
                scaler.referenceResolution = sourceScaler.referenceResolution;
                scaler.screenMatchMode = sourceScaler.screenMatchMode;
                scaler.matchWidthOrHeight = sourceScaler.matchWidthOrHeight;
            }
            else
            {
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }
        
        // Add GraphicRaycaster if needed (for button clicks)
        if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        // Now persist
        DontDestroyOnLoad(gameObject);
        
        Debug.Log("[SettingsPanel] Moved to root and set up persistence");
    }
    
    void OnDestroy()
    {
        // Unregister from Services when destroyed to prevent stale references
        if (Services.IsRegistered<ISettingsService>() && Services.Get<ISettingsService>() == (ISettingsService)this)
        {
            Services.Unregister<ISettingsService>();
        }
    }
    
    void Start()
    {
        // Reset position to full-screen overlay (Left/Right/Top/Bottom = 0)
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.offsetMin = Vector2.zero; // Left, Bottom
            rect.offsetMax = Vector2.zero; // Right, Top (negative values)
        }
        
        // Load settings from account data
        LoadSettings();
        
        // Apply saved video settings on startup
        if (settingsData != null)
        {
            settingsData.ApplyVideoSettings();
        }
        
        // Start with controls panel visible
        ShowControlsPanel();
        
        // Start hidden
        gameObject.SetActive(false);
    }
    
    void OnEnable()
    {
        // Refresh settings when panel opens
        LoadSettings();
    }
    
    /// <summary>
    /// Load settings from AccountSaveData
    /// </summary>
    private void LoadSettings()
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
    /// Save settings to AccountSaveData
    /// </summary>
    public void SaveSettings()
    {
        AccountSaveData accountData = SaveSystem.LoadAccount();
        if (accountData == null)
        {
            accountData = AccountSaveData.CreateDefault();
        }
        
        accountData.settingsData = settingsData;
        SaveSystem.SaveAccount(accountData);
        
        // Notify KeybindManager to refresh its settings
        Services.Get<IKeybindService>()?.RefreshSettings();
        
        Debug.Log("[SettingsPanel] Settings saved to account");
    }
    
    /// <summary>
    /// Get current settings data for sub-panels to access
    /// </summary>
    public SettingsData GetSettingsData()
    {
        return settingsData;
    }
    
    /// <summary>
    /// Show the Controls sub-panel
    /// </summary>
    public void ShowControlsPanel()
    {
        SwitchToPanel(controlsPanel);
        UpdateTabColors(controlsButton);
    }
    
    /// <summary>
    /// Show the Audio sub-panel
    /// </summary>
    public void ShowAudioPanel()
    {
        SwitchToPanel(audioPanel);
        UpdateTabColors(audioButton);
    }
    
    /// <summary>
    /// Show the Video sub-panel
    /// </summary>
    public void ShowVideoPanel()
    {
        SwitchToPanel(videoPanel);
        UpdateTabColors(videoButton);
    }
    
    /// <summary>
    /// Switch to a specific sub-panel
    /// </summary>
    private void SwitchToPanel(GameObject panel)
    {
        // Hide current panel
        if (currentPanel != null)
            currentPanel.SetActive(false);
        
        // Show new panel
        if (panel != null)
        {
            panel.SetActive(true);
            currentPanel = panel;
        }
    }
    
    /// <summary>
    /// Update tab button colors to show which is active
    /// </summary>
    private void UpdateTabColors(Button activeButton)
    {
        SetButtonColor(controlsButton, controlsButton == activeButton ? activeTabColor : inactiveTabColor);
        SetButtonColor(audioButton, audioButton == activeButton ? activeTabColor : inactiveTabColor);
        SetButtonColor(videoButton, videoButton == activeButton ? activeTabColor : inactiveTabColor);
    }
    
    private void SetButtonColor(Button button, Color color)
    {
        if (button == null) return;
        
        var colors = button.colors;
        colors.normalColor = color;
        button.colors = colors;
    }
    
    /// <summary>
    /// Open the settings panel
    /// </summary>
    public void OpenSettings()
    {
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// Close the settings panel (Resume)
    /// </summary>
    public void CloseSettings()
    {
        // Save settings before closing
        SaveSettings();
        
        // Apply video settings
        if (settingsData != null)
        {
            settingsData.ApplyVideoSettings();
        }
        
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Exit to desktop (quit the game)
    /// </summary>
    public void ExitToDesktop()
    {
        // Save settings before quitting
        SaveSettings();
        
        Debug.Log("[SettingsPanel] Exiting to desktop...");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    /// <summary>
    /// Toggle the settings panel on/off
    /// </summary>
    public void TogglePanel()
    {
        if (gameObject.activeSelf)
        {
            CloseSettings();
        }
        else
        {
            OpenSettings();
        }
    }
}
