using UnityEngine;
using TMPro;

/// <summary>
/// Action bar panel showing 6 skill slots.
/// Skills are auto-cast on cooldown during combat.
/// </summary>
public class ActionBarPanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject actionBarPanel;
    
    [Header("Skill Slots")]
    public SkillSlotUI[] skillSlots = new SkillSlotUI[6];
    
    [Header("Character Display")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI manaText;
    public TextMeshProUGUI xpText;
    
    // Services
    private ISkillService skillService;
    private ICharacterService characterService;
    
    void Start()
    {
        skillService = Services.Get<ISkillService>();
        characterService = Services.Get<ICharacterService>();
        
        // Subscribe to events
        EventBus.Subscribe<ActionBarChangedEvent>(OnActionBarChanged);
        EventBus.Subscribe<CharacterLevelChangedEvent>(OnLevelChanged);
        EventBus.Subscribe<CharacterLevelUpEvent>(e => UpdateLevelDisplay());
        EventBus.Subscribe<CharacterHealthChangedEvent>(OnHealthChanged);
        EventBus.Subscribe<CharacterManaChangedEvent>(OnManaChanged);
        EventBus.Subscribe<CharacterXPChangedEvent>(OnXPChanged);
        
        // Initialize slots
        InitializeSlots();
        
        // Initialize displays
        UpdateLevelDisplay();
        UpdateHealthDisplay();
        UpdateManaDisplay();
        UpdateXPDisplay();
    }
    
    void OnDestroy()
    {
        EventBus.Unsubscribe<ActionBarChangedEvent>(OnActionBarChanged);
        EventBus.Unsubscribe<CharacterLevelChangedEvent>(OnLevelChanged);
        EventBus.Unsubscribe<CharacterLevelUpEvent>(e => UpdateLevelDisplay());
        EventBus.Unsubscribe<CharacterHealthChangedEvent>(OnHealthChanged);
        EventBus.Unsubscribe<CharacterManaChangedEvent>(OnManaChanged);
        EventBus.Unsubscribe<CharacterXPChangedEvent>(OnXPChanged);
    }
    
    void OnLevelChanged(CharacterLevelChangedEvent e)
    {
        UpdateLevelDisplay();
    }
    
    void OnHealthChanged(CharacterHealthChangedEvent e)
    {
        UpdateHealthDisplay(e.currentHealth, e.maxHealth);
    }
    
    void OnManaChanged(CharacterManaChangedEvent e)
    {
        UpdateManaDisplay(e.currentMana, e.maxMana);
    }
    
    void OnXPChanged(CharacterXPChangedEvent e)
    {
        UpdateXPDisplay(e.newXP);
    }
    
    void UpdateLevelDisplay()
    {
        if (levelText == null) return;
        
        if (characterService == null)
            characterService = Services.Get<ICharacterService>();
        
        if (characterService != null)
        {
            int level = characterService.GetLevel();
            levelText.text = level.ToString();
        }
    }
    
    void UpdateHealthDisplay()
    {
        if (healthText == null) return;
        
        if (characterService == null)
            characterService = Services.Get<ICharacterService>();
        
        if (characterService != null)
        {
            float current = characterService.GetCurrentHealth();
            float max = characterService.GetMaxHealth();
            UpdateHealthDisplay(current, max);
        }
    }
    
    void UpdateHealthDisplay(float currentHealth, float maxHealth)
    {
        if (healthText != null)
        {
            float displayHealth = Mathf.Max(0f, currentHealth);
            healthText.text = $"{displayHealth:F0} / {maxHealth:F0}";
        }
    }
    
    void UpdateManaDisplay()
    {
        if (manaText == null) return;
        
        if (characterService == null)
            characterService = Services.Get<ICharacterService>();
        
        if (characterService != null)
        {
            CharacterData charData = characterService.GetCharacterData();
            if (charData != null)
            {
                UpdateManaDisplay(charData.currentMana, charData.GetMaxMana());
            }
        }
    }
    
    void UpdateManaDisplay(float currentMana, float maxMana)
    {
        if (manaText != null)
        {
            float displayMana = Mathf.Max(0f, currentMana);
            manaText.text = $"{displayMana:F0} / {maxMana:F0}";
        }
    }
    
    void UpdateXPDisplay()
    {
        if (xpText == null) return;
        
        if (characterService == null)
            characterService = Services.Get<ICharacterService>();
        
        if (characterService != null)
        {
            int currentXP = characterService.GetCurrentXP();
            int xpRequired = characterService.GetXPRequiredForNextLevel();
            UpdateXPDisplay(currentXP, xpRequired);
        }
    }
    
    void UpdateXPDisplay(int currentXP)
    {
        if (xpText == null) return;
        
        if (characterService == null)
            characterService = Services.Get<ICharacterService>();
        
        if (characterService != null)
        {
            int xpRequired = characterService.GetXPRequiredForNextLevel();
            UpdateXPDisplay(currentXP, xpRequired);
        }
    }
    
    void UpdateXPDisplay(int currentXP, int xpRequired)
    {
        if (xpText != null)
        {
            xpText.text = $"{currentXP} / {xpRequired}";
        }
    }
    
    void InitializeSlots()
    {
        // Set slot indices
        for (int i = 0; i < skillSlots.Length; i++)
        {
            if (skillSlots[i] != null)
            {
                skillSlots[i].slotIndex = i;
            }
        }
    }
    
    void OnActionBarChanged(ActionBarChangedEvent e)
    {
        // Slots handle their own updates via events
    }
    
    /// <summary>
    /// Toggle action bar visibility
    /// </summary>
    public void TogglePanel()
    {
        if (actionBarPanel != null)
        {
            actionBarPanel.SetActive(!actionBarPanel.activeSelf);
        }
    }
    
    /// <summary>
    /// Show the action bar
    /// </summary>
    public void ShowPanel()
    {
        if (actionBarPanel != null)
        {
            actionBarPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// Hide the action bar
    /// </summary>
    public void HidePanel()
    {
        if (actionBarPanel != null)
        {
            actionBarPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Set skill in a specific slot
    /// </summary>
    public void SetSkillInSlot(int slotIndex, SkillData skill)
    {
        if (slotIndex < 0 || slotIndex >= skillSlots.Length)
            return;
        
        if (skillSlots[slotIndex] != null)
        {
            skillSlots[slotIndex].SetSkill(skill);
        }
    }
    
    /// <summary>
    /// Clear a specific slot
    /// </summary>
    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= skillSlots.Length)
            return;
        
        if (skillSlots[slotIndex] != null)
        {
            skillSlots[slotIndex].ClearSlot();
        }
    }
    
    /// <summary>
    /// Clear all slots
    /// </summary>
    public void ClearAllSlots()
    {
        for (int i = 0; i < skillSlots.Length; i++)
        {
            ClearSlot(i);
        }
    }
}

