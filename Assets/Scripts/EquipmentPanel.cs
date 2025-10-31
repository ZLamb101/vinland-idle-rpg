using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI panel that displays equipped items and allows equipping/unequipping.
/// Shows all WoW-style equipment slots.
/// </summary>
public class EquipmentPanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject equipmentPanel;
    
    [Header("Equipment Slot UI Elements")]
    public EquipmentSlotUI headSlot;
    public EquipmentSlotUI neckSlot;
    public EquipmentSlotUI shouldersSlot;
    public EquipmentSlotUI backSlot;
    public EquipmentSlotUI chestSlot;
    public EquipmentSlotUI handsSlot;
    public EquipmentSlotUI waistSlot;
    public EquipmentSlotUI legsSlot;
    public EquipmentSlotUI feetSlot;
    public EquipmentSlotUI ring1Slot;
    public EquipmentSlotUI ring2Slot;
    public EquipmentSlotUI mainHandSlot;
    public EquipmentSlotUI offHandSlot;
    
    [Header("Stats Display")]
    public TextMeshProUGUI attackDamageText;
    public TextMeshProUGUI attackSpeedText;
    public TextMeshProUGUI maxHealthText;
    public TextMeshProUGUI armorText;
    public TextMeshProUGUI critChanceText;
    public TextMeshProUGUI dodgeText;
    
    void Start()
    {
        // Subscribe to equipment changes
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged += OnEquipmentChanged;
            EquipmentManager.Instance.OnStatsRecalculated += UpdateStatsDisplay;
        }
        
        // Initialize all slots
        InitializeSlots();
        
        // Hide panel initially
        if (equipmentPanel != null)
            equipmentPanel.SetActive(false);
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged -= OnEquipmentChanged;
            EquipmentManager.Instance.OnStatsRecalculated -= UpdateStatsDisplay;
        }
    }
    
    void InitializeSlots()
    {
        // Set up each slot with its type and click handler
        SetupSlot(headSlot, EquipmentSlot.Head);
        SetupSlot(neckSlot, EquipmentSlot.Neck);
        SetupSlot(shouldersSlot, EquipmentSlot.Shoulders);
        SetupSlot(backSlot, EquipmentSlot.Back);
        SetupSlot(chestSlot, EquipmentSlot.Chest);
        SetupSlot(handsSlot, EquipmentSlot.Hands);
        SetupSlot(waistSlot, EquipmentSlot.Waist);
        SetupSlot(legsSlot, EquipmentSlot.Legs);
        SetupSlot(feetSlot, EquipmentSlot.Feet);
        SetupSlot(ring1Slot, EquipmentSlot.Ring1);
        SetupSlot(ring2Slot, EquipmentSlot.Ring2);
        SetupSlot(mainHandSlot, EquipmentSlot.MainHand);
        SetupSlot(offHandSlot, EquipmentSlot.OffHand);
        
        RefreshAllSlots();
        UpdateStatsDisplay();
    }
    
    void SetupSlot(EquipmentSlotUI slotUI, EquipmentSlot slotType)
    {
        if (slotUI == null) return;
        
        slotUI.slotType = slotType;
        
        if (slotUI.slotButton != null)
        {
            slotUI.slotButton.onClick.AddListener(() => OnSlotClicked(slotType));
        }
    }
    
    void OnEquipmentChanged(EquipmentSlot slot, EquipmentData equipment)
    {
        RefreshSlot(slot);
    }
    
    void RefreshSlot(EquipmentSlot slotType)
    {
        EquipmentSlotUI slotUI = GetSlotUI(slotType);
        if (slotUI == null) return;
        
        EquipmentData equipment = EquipmentManager.Instance?.GetEquipment(slotType);
        
        if (equipment != null)
        {
            // Show equipped item
            if (slotUI.itemIcon != null)
            {
                slotUI.itemIcon.sprite = equipment.icon;
                slotUI.itemIcon.gameObject.SetActive(true);
            }
        }
        else
        {
            // Empty slot - show default silhouette if available
            if (slotUI.itemIcon != null)
            {
                if (slotUI.emptySlotIcon != null)
                {
                    slotUI.itemIcon.sprite = slotUI.emptySlotIcon;
                    slotUI.itemIcon.gameObject.SetActive(true);
                }
                else
                {
                    slotUI.itemIcon.gameObject.SetActive(false);
                }
            }
        }
    }
    
    void RefreshAllSlots()
    {
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            RefreshSlot(slot);
        }
    }
    
    void OnSlotClicked(EquipmentSlot slotType)
    {
        // Unequip item from this slot
        if (EquipmentManager.Instance != null)
        {
            EquipmentData unequipped = EquipmentManager.Instance.UnequipItem(slotType);
            
            if (unequipped != null)
            {
                // Try to add back to inventory
                if (CharacterManager.Instance != null)
                {
                    InventoryItem item = new InventoryItem
                    {
                        itemName = unequipped.equipmentName,
                        description = unequipped.description,
                        icon = unequipped.icon,
                        quantity = 1,
                        maxStackSize = 1,
                        itemType = ItemType.Equipment,
                        equipmentData = unequipped
                    };
                    
                    bool added = CharacterManager.Instance.AddItemToInventory(item);
                    if (!added)
                    {
                        Debug.LogWarning("Inventory full! Cannot unequip item.");
                        // Re-equip the item
                        EquipmentManager.Instance.EquipItem(unequipped);
                    }
                }
            }
        }
    }
    
    void UpdateStatsDisplay()
    {
        if (EquipmentManager.Instance == null) return;
        
        EquipmentStats stats = EquipmentManager.Instance.GetTotalStats();
        
        // Update stat text displays
        if (attackDamageText != null)
            attackDamageText.text = $"Attack: {stats.attackDamage:F0}";
        
        if (attackSpeedText != null)
            attackSpeedText.text = $"Speed: {(stats.attackSpeed < 0 ? "+" : "")}{-stats.attackSpeed:F2}s";
        
        if (maxHealthText != null)
            maxHealthText.text = $"Health: +{stats.maxHealth:F0}";
        
        if (armorText != null)
            armorText.text = $"Armor: {stats.armor * 100:F0}%";
        
        if (critChanceText != null)
            critChanceText.text = $"Crit: {stats.criticalChance * 100:F0}%";
        
        if (dodgeText != null)
            dodgeText.text = $"Dodge: {stats.dodge * 100:F0}%";
    }
    
    EquipmentSlotUI GetSlotUI(EquipmentSlot slotType)
    {
        switch (slotType)
        {
            case EquipmentSlot.Head: return headSlot;
            case EquipmentSlot.Neck: return neckSlot;
            case EquipmentSlot.Shoulders: return shouldersSlot;
            case EquipmentSlot.Back: return backSlot;
            case EquipmentSlot.Chest: return chestSlot;
            case EquipmentSlot.Hands: return handsSlot;
            case EquipmentSlot.Waist: return waistSlot;
            case EquipmentSlot.Legs: return legsSlot;
            case EquipmentSlot.Feet: return feetSlot;
            case EquipmentSlot.Ring1: return ring1Slot;
            case EquipmentSlot.Ring2: return ring2Slot;
            case EquipmentSlot.MainHand: return mainHandSlot;
            case EquipmentSlot.OffHand: return offHandSlot;
            default: return null;
        }
    }
    
    public void TogglePanel()
    {
        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(!equipmentPanel.activeSelf);
        }
    }
}

/// <summary>
/// UI component for a single equipment slot
/// </summary>
[System.Serializable]
public class EquipmentSlotUI
{
    public EquipmentSlot slotType;
    public Button slotButton;
    public Image itemIcon;
    [Tooltip("Optional: Silhouette/background image shown when slot is empty")]
    public Sprite emptySlotIcon;
}

