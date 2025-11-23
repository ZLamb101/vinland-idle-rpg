using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Unified character panel showing Equipment and Stats side-by-side.
/// Left: All equipped items in slots
/// Right: Character attributes and total combat stats
/// </summary>
public class CharacterPanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject characterPanel;
    
    // ===== EQUIPMENT SECTION =====
    [Header("Equipment Slot UI Elements (11 Total)")]
    public EquipmentSlotUI headSlot;
    public EquipmentSlotUI neckSlot;
    public EquipmentSlotUI shouldersSlot;
    public EquipmentSlotUI chestSlot;
    public EquipmentSlotUI handsSlot;
    public EquipmentSlotUI legsSlot;
    public EquipmentSlotUI feetSlot;
    public EquipmentSlotUI ring1Slot;
    public EquipmentSlotUI ring2Slot;
    public EquipmentSlotUI mainHandSlot;
    public EquipmentSlotUI offHandSlot;
    
    // ===== STATS SECTION (Attributes + Combat Stats Combined) =====
    [Header("Stats Display")]
    public TextMeshProUGUI strengthText;
    public TextMeshProUGUI agilityText;
    public TextMeshProUGUI intellectText;
    public TextMeshProUGUI staminaText;
    public TextMeshProUGUI spiritText;
    public TextMeshProUGUI attackPowerText;
    public TextMeshProUGUI maxHealthText;
    public TextMeshProUGUI armorText;
    public TextMeshProUGUI critChanceText;
    public TextMeshProUGUI dodgeText;
    public TextMeshProUGUI healthRegenText;
    public TextMeshProUGUI attackSpeedText;
    
    // Services
    private ICharacterService characterService;
    private IEquipmentService equipmentService;
    
    void Start()
    {
        // Get services
        characterService = Services.Get<ICharacterService>();
        equipmentService = Services.Get<IEquipmentService>();
        
        // Subscribe to updates
        EventBus.Subscribe<CharacterLevelChangedEvent>(e => UpdateAllDisplays());
        EventBus.Subscribe<CharacterLevelUpEvent>(e => UpdateAllDisplays());
        EventBus.Subscribe<EquipmentChangedEvent>(OnEquipmentChanged);
        EventBus.Subscribe<StatsRecalculatedEvent>(e => UpdateAllDisplays());
        EventBus.Subscribe<TalentBonusesRecalculatedEvent>(e => UpdateAllDisplays());
        
        // Initialize equipment slots
        InitializeEquipmentSlots();
        
        // Hide panel initially
        if (characterPanel != null)
            characterPanel.SetActive(false);
    }
    
    void OnEnable()
    {
        // Update all displays when panel is opened
        UpdateAllDisplays();
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        EventBus.Unsubscribe<CharacterLevelChangedEvent>(e => UpdateAllDisplays());
        EventBus.Unsubscribe<CharacterLevelUpEvent>(e => UpdateAllDisplays());
        EventBus.Unsubscribe<EquipmentChangedEvent>(OnEquipmentChanged);
        EventBus.Unsubscribe<StatsRecalculatedEvent>(e => UpdateAllDisplays());
        EventBus.Unsubscribe<TalentBonusesRecalculatedEvent>(e => UpdateAllDisplays());
    }
    
    /// <summary>
    /// Update both Equipment and Stats displays
    /// </summary>
    void UpdateAllDisplays()
    {
        UpdateStatsDisplay();
        RefreshEquipmentSlots();
    }
    
    // ===== STATS SECTION (Combined Attributes + Combat Stats) =====
    
    void UpdateStatsDisplay()
    {
        if (characterService == null) return;
        
        CharacterData charData = characterService.GetCharacterData();
        if (charData == null) return;
        
        // Calculate total stats from all sources (attributes + equipment + talents)
        float totalAttackPower = charData.GetAttackPower();
        float totalMaxHealth = charData.GetMaxHealth();
        float totalArmor = charData.GetArmor();
        float totalCritChance = charData.GetCritChance();
        float totalDodge = charData.GetDodgeChance();
        float totalHealthRegen = charData.GetHealthRegen();
        float totalAttackSpeed = 0f;
        
        // Add equipment bonuses
        if (equipmentService != null)
        {
            EquipmentStats equipStats = equipmentService.GetTotalStats();
            totalAttackPower += equipStats.attackDamage;
            totalMaxHealth += equipStats.maxHealth;
            totalArmor += equipStats.armor;
            totalCritChance += equipStats.criticalChance;
            totalDodge += equipStats.dodge;
            totalHealthRegen += equipStats.healthRegen;
            totalAttackSpeed += equipStats.attackSpeed;
        }
        
        // Add talent bonuses
        var talentService = Services.Get<ITalentService>();
        if (talentService != null)
        {
            TalentBonuses talents = talentService.GetTotalBonuses();
            totalAttackPower += talents.attackDamage;
            totalAttackPower *= (1f + talents.damageMultiplier);
            totalMaxHealth += talents.maxHealth;
            totalMaxHealth *= (1f + talents.healthMultiplier);
            totalArmor += talents.armor;
            totalCritChance += talents.criticalChance;
            totalDodge += talents.dodge;
            totalAttackSpeed += talents.attackSpeed;
        }
        
        // === Display Attributes ===
        if (strengthText != null)
            strengthText.text = $"<b>Strength:</b> {charData.strength}";
        
        if (agilityText != null)
            agilityText.text = $"<b>Agility:</b> {charData.agility}";
        
        if (intellectText != null)
            intellectText.text = $"<b>Intellect:</b> {charData.intellect}";
        
        if (staminaText != null)
            staminaText.text = $"<b>Stamina:</b> {charData.stamina}";
        
        if (spiritText != null)
            spiritText.text = $"<b>Spirit:</b> {charData.spirit}";
        
        // === Display Total Combat Stats ===
        if (attackPowerText != null)
            attackPowerText.text = $"<b>Attack Power:</b> {totalAttackPower:F0}";
        
        if (maxHealthText != null)
            maxHealthText.text = $"<b>Max Health:</b> {totalMaxHealth:F0}";
        
        if (armorText != null)
            armorText.text = $"<b>Armor:</b> {totalArmor * 100:F0}%";
        
        if (critChanceText != null)
            critChanceText.text = $"<b>Crit Chance:</b> {totalCritChance * 100:F1}%";
        
        if (dodgeText != null)
            dodgeText.text = $"<b>Dodge:</b> {totalDodge * 100:F1}%";
        
        if (healthRegenText != null)
            healthRegenText.text = $"<b>Health Regen:</b> {totalHealthRegen:F0}/sec";
        
        if (attackSpeedText != null)
        {
            float baseSpeed = GameBalance.Combat.playerBaseAttackSpeed;
            float finalSpeed = baseSpeed + totalAttackSpeed;
            attackSpeedText.text = $"<b>Attack Speed:</b> {finalSpeed:F2}s";
        }
    }
    
    // ===== EQUIPMENT SECTION =====
    
    void InitializeEquipmentSlots()
    {
        // Set up each slot with its type and click handler (11 slots total)
        SetupSlot(headSlot, EquipmentSlot.Head);
        SetupSlot(neckSlot, EquipmentSlot.Neck);
        SetupSlot(shouldersSlot, EquipmentSlot.Shoulders);
        SetupSlot(chestSlot, EquipmentSlot.Chest);
        SetupSlot(handsSlot, EquipmentSlot.Hands);
        SetupSlot(legsSlot, EquipmentSlot.Legs);
        SetupSlot(feetSlot, EquipmentSlot.Feet);
        SetupSlot(ring1Slot, EquipmentSlot.Ring1);
        SetupSlot(ring2Slot, EquipmentSlot.Ring2);
        SetupSlot(mainHandSlot, EquipmentSlot.MainHand);
        SetupSlot(offHandSlot, EquipmentSlot.OffHand);
        
        RefreshEquipmentSlots();
    }
    
    void SetupSlot(EquipmentSlotUI slotUI, EquipmentSlot slotType)
    {
        if (slotUI == null)
        {
            Debug.LogWarning($"[CharacterPanel] EquipmentSlotUI is null for slot type: {slotType}");
            return;
        }
        
        slotUI.slotType = slotType;
        
        if (slotUI.slotButton != null)
        {
            slotUI.slotButton.onClick.RemoveAllListeners();
            slotUI.slotButton.onClick.AddListener(() => OnSlotClicked(slotType));
        }
        else
        {
            Debug.LogWarning($"[CharacterPanel] Button is null for {slotType} slot! Assign the Button component in the Inspector.");
        }
    }
    
    void OnSlotClicked(EquipmentSlot slot)
    {
        if (equipmentService == null) return;
        
        // Unequip item
        EquipmentData unequipped = equipmentService.UnequipItem(slot);
        
        if (unequipped != null)
        {
            // Try to add back to inventory
            var characterService = Services.Get<ICharacterService>();
            if (characterService != null)
            {
                InventoryItem invItem = unequipped.CreateInventoryItem();
                invItem.SetEquipmentData(unequipped);
                bool added = characterService.AddItemToInventory(invItem);
                
                if (!added)
                {
                    // Inventory full, re-equip the item
                    equipmentService.EquipItem(unequipped, out _);
                }
            }
        }
    }
    
    void OnEquipmentChanged(EquipmentChangedEvent e)
    {
        RefreshEquipmentSlots();
        UpdateStatsDisplay();
    }
    
    void RefreshEquipmentSlots()
    {
        if (equipmentService == null) return;
        
        RefreshSlot(headSlot);
        RefreshSlot(neckSlot);
        RefreshSlot(shouldersSlot);
        RefreshSlot(chestSlot);
        RefreshSlot(handsSlot);
        RefreshSlot(legsSlot);
        RefreshSlot(feetSlot);
        RefreshSlot(ring1Slot);
        RefreshSlot(ring2Slot);
        RefreshSlot(mainHandSlot);
        RefreshSlot(offHandSlot);
    }
    
    void RefreshSlot(EquipmentSlotUI slotUI)
    {
        if (slotUI == null || slotUI.itemIcon == null) return;
        
        EquipmentData equipment = equipmentService.GetEquipment(slotUI.slotType);
        
        if (equipment != null && equipment.icon != null)
        {
            slotUI.itemIcon.sprite = equipment.icon;
            slotUI.itemIcon.color = Color.white;
        }
        else
        {
            // Show empty slot icon or make transparent
            if (slotUI.emptySlotIcon != null)
            {
                slotUI.itemIcon.sprite = slotUI.emptySlotIcon;
                slotUI.itemIcon.color = new Color(1f, 1f, 1f, 0.3f);
            }
            else
            {
                slotUI.itemIcon.sprite = null;
                slotUI.itemIcon.color = new Color(1f, 1f, 1f, 0.1f);
            }
        }
    }
    
    public void TogglePanel()
    {
        if (characterPanel != null)
        {
            bool newState = !characterPanel.activeSelf;
            characterPanel.SetActive(newState);
            
            // Update displays when opening
            if (newState)
            {
                UpdateAllDisplays();
            }
        }
    }
}

