using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI panel that displays auto-battle combat.
/// Shows player health, attack progress bars, combat log, and buttons.
/// Monster information is displayed via TargetFrame and per-monster containers above enemies.
/// </summary>
public class CombatPanel : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject combatPanel; // The main panel to show/hide
    public Image backgroundImage; // Background image to sync with zone
    
    [Header("Position")]
    public Vector2 startPosition = new Vector2(0f, 0f);

    [Header("Player Display")]
    public Image playerSprite; // Optional
    public Slider playerHealthBar;
    public TextMeshProUGUI playerHealthText;
    public Slider playerAttackProgressBar;
    
    [Header("Killstreak")]
    public TextMeshProUGUI killstreakText;
    
    [Header("Buttons")]
    public Button retreatButton;
    public Button continueButton; // Button to continue after defeat
    
    [Header("Mob Count Selector")]
    [Tooltip("Mob count selector. If not assigned, will try to find it in the scene.")]
    public MobCountSelector mobCountSelector; // Selector for number of mobs to fight
    
    // Note: Player damage display is now handled by CombatSceneController (floating text on hero visual)
    
    private ICombatService combatService; // Cached combat service reference
    private RectTransform rectTransform;
    private int currentKillstreak = 0;
    
    void Start()
    {
        // Move to specified position on game start
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = startPosition;
        }
        
        // Find mob count selector if not assigned
        if (mobCountSelector == null)
        {
            mobCountSelector = ComponentInjector.FindComponent<MobCountSelector>();
        }
        
        // Get combat service
        combatService = Services.Get<ICombatService>();
        
        // Subscribe to combat events via EventBus
        // Note: Player damage display is now handled by CombatSceneController (floating text on hero)
        EventBus.Subscribe<CombatStateChangedEvent>(OnCombatStateChanged);
        EventBus.Subscribe<PlayerHealthChangedEvent>(UpdatePlayerHealth);
        EventBus.Subscribe<PlayerAttackProgressEvent>(UpdatePlayerAttackProgress);
        EventBus.Subscribe<ZoneChangedEvent>(OnZoneChanged);
        EventBus.Subscribe<MonsterDiedEvent>(OnMonsterDied);
        
        // Setup buttons
        if (retreatButton != null)
            retreatButton.onClick.AddListener(OnRetreatClicked);
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
        
        // Hide buttons initially
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
        
        // Hide panel initially
        if (combatPanel != null)
            combatPanel.SetActive(false);

        // Initialize background
        if (Services.TryGet<IZoneService>(out var zoneService))
        {
            var currentZone = zoneService.GetCurrentZone();
            if (currentZone != null && backgroundImage != null)
            {
                backgroundImage.sprite = currentZone.backgroundImage;
            }
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from EventBus
        EventBus.Unsubscribe<CombatStateChangedEvent>(OnCombatStateChanged);
        EventBus.Unsubscribe<PlayerHealthChangedEvent>(UpdatePlayerHealth);
        EventBus.Unsubscribe<PlayerAttackProgressEvent>(UpdatePlayerAttackProgress);
        EventBus.Unsubscribe<ZoneChangedEvent>(OnZoneChanged);
        EventBus.Unsubscribe<MonsterDiedEvent>(OnMonsterDied);
    }
    
    void OnZoneChanged(ZoneChangedEvent e)
    {
        if (backgroundImage != null && e.newZone != null)
        {
            backgroundImage.sprite = e.newZone.backgroundImage;
        }
    }
    
    void OnCombatStateChanged(CombatStateChangedEvent e)
    {
        switch (e.newState)
        {
            case CombatManager.CombatState.Idle:
                HideCombatPanel();
                if (continueButton != null)
                    continueButton.gameObject.SetActive(false);
                break;
                
            case CombatManager.CombatState.Fighting:
                ShowCombatPanel();
                if (retreatButton != null)
                    retreatButton.gameObject.SetActive(true);
                if (continueButton != null)
                    continueButton.gameObject.SetActive(false);
                break;
                
            case CombatManager.CombatState.Defeat:
                // Hero died - reset killstreak
                ResetKillstreak();
                if (retreatButton != null)
                    retreatButton.gameObject.SetActive(false);
                if (continueButton != null)
                    continueButton.gameObject.SetActive(true);
                break;
        }
    }
    
    void UpdatePlayerHealth(PlayerHealthChangedEvent e)
    {
        // Clamp health to 0 minimum for display
        float displayCurrent = Mathf.Max(0f, e.currentHealth);
        
        if (playerHealthBar != null)
        {
            playerHealthBar.maxValue = e.maxHealth;
            playerHealthBar.value = displayCurrent;
        }
        
        if (playerHealthText != null)
            playerHealthText.text = $"{displayCurrent:F0} / {e.maxHealth:F0}";
    }
    
    void UpdatePlayerAttackProgress(PlayerAttackProgressEvent e)
    {
        if (playerAttackProgressBar != null)
            playerAttackProgressBar.value = e.progress;
    }
    
    void OnMonsterDied(MonsterDiedEvent e)
    {
        currentKillstreak++;
        UpdateKillstreakDisplay();
    }
    
    void UpdateKillstreakDisplay()
    {
        if (killstreakText != null)
        {
            killstreakText.text = $"Current Streak: {currentKillstreak}";
        }
    }
    
    void ResetKillstreak()
    {
        currentKillstreak = 0;
        UpdateKillstreakDisplay();
    }
    
    void ShowCombatPanel()
    {
        if (combatPanel != null)
            combatPanel.SetActive(true);
    }
    
    /// <summary>
    /// Reset mob count selector to default value of 1
    /// </summary>
    void ResetMobCountSelector()
    {
        // Try to find selector if not assigned
        if (mobCountSelector == null)
        {
            mobCountSelector = ComponentInjector.FindComponent<MobCountSelector>();
        }
        
        if (mobCountSelector != null)
        {
            mobCountSelector.SetMobCount(1);
        }
    }
    
    void HideCombatPanel()
    {
        if (combatPanel != null)
            combatPanel.SetActive(false);
    }
    
    void OnRetreatClicked()
    {
        // End combat and return to zone
        if (combatService != null)
        {
            combatService.EndCombat();
        }
        
        // Reset mob count selector to 1 when retreating
        ResetMobCountSelector();
        
        HideCombatPanel();
    }
    
    void OnContinueClicked()
    {
        // Resume combat after defeat
        if (combatService != null)
        {
            combatService.ResumeAfterDefeat();
        }
    }
}


