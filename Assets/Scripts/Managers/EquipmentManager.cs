using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager for character equipment.
/// Handles equipping/unequipping items and calculating total stats.
/// </summary>
public class EquipmentManager : MonoBehaviour, IEquipmentService
{
    
    [Header("Equipment Slots")]
    private Dictionary<EquipmentSlot, EquipmentData> equippedItems = new Dictionary<EquipmentSlot, EquipmentData>();
    
    // Events migrated to EventBus - see GameEvent.cs for event types
    
    // Cached total stats
    private EquipmentStats totalStats = new EquipmentStats();

    private ICharacterService characterService;
    
    void Awake()
    {
        if (Services.IsRegistered<IEquipmentService>())
        {
            Debug.LogWarning("[EquipmentManager] Service already registered. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        
        // Register with service locator
        Services.Register<IEquipmentService>(this);
        
        // Initialize all equipment slots as empty
        foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
        {
            equippedItems[slot] = null;
        }
    }

    void Start()
    {
        characterService = Services.Get<ICharacterService>();
    }

    void OnDestroy()
    {
        Services.Unregister<IEquipmentService>();
    }

    /// <summary>
    /// Equip an item to its designated slot
    /// </summary>
    public bool EquipItem(EquipmentData equipment, out EquipmentData unequippedItem)
    {
        unequippedItem = null;
        
        if (equipment == null) return false;
        
        // Check level requirement
        if (characterService != null)
        {
            int playerLevel = characterService.GetLevel();
            if (playerLevel < equipment.levelRequired)
            {
                return false;
            }
        }
        
        // Check if slot already has equipment
        EquipmentSlot slot = equipment.slot;
        
        // Validate that the slot exists in our dictionary (handles old equipment with removed slots)
        if (!equippedItems.ContainsKey(slot))
        {
            Debug.LogWarning($"[EquipmentManager] Invalid equipment slot: {slot}. This item may be using an old slot type (Back/Waist). Please recreate the equipment item.");
            return false;
        }
        
        EquipmentData previousEquipment = equippedItems[slot];
        
        // Unequip previous item if exists (without saving, we'll save once after equipping)
        if (previousEquipment != null)
        {
            UnequipItemInternal(slot);
            unequippedItem = previousEquipment; // Return the unequipped item
        }
        
        // Equip new item
        equippedItems[slot] = equipment;
        
        // Recalculate stats
        RecalculateStats();
        
        // Notify listeners
        EventBus.Publish(new EquipmentChangedEvent { slot = slot, newEquipment = equipment, oldEquipment = previousEquipment });
        
        // Auto-save after equipping
        AutoSave("equipment equipped");
        
        return true;
    }
    
    /// <summary>
    /// Unequip item from a specific slot
    /// </summary>
    public EquipmentData UnequipItem(EquipmentSlot slot)
    {
        EquipmentData unequipped = UnequipItemInternal(slot);
        
        if (unequipped != null)
        {
            // Auto-save after unequipping
            AutoSave("equipment unequipped");
        }
        
        return unequipped;
    }
    
    /// <summary>
    /// Internal unequip without auto-save (used when replacing equipment)
    /// </summary>
    private EquipmentData UnequipItemInternal(EquipmentSlot slot)
    {
        EquipmentData unequipped = equippedItems[slot];
        
        if (unequipped == null)
        {
            return null;
        }
        
        // Remove item from slot
        equippedItems[slot] = null;
        
        // Recalculate stats
        RecalculateStats();
        
        // Notify listeners
        EventBus.Publish(new EquipmentChangedEvent { slot = slot, newEquipment = null, oldEquipment = unequipped });
        
        return unequipped;
    }
    
    /// <summary>
    /// Recalculate total stats from all equipped items
    /// </summary>
    void RecalculateStats()
    {
        // Reset stats
        totalStats = new EquipmentStats();
        
        // Sum up all equipment stats
        foreach (var kvp in equippedItems)
        {
            EquipmentData equipment = kvp.Value;
            if (equipment == null) continue;
            
            // Physical combat stats
            totalStats.attackDamage += equipment.attackDamage;
            totalStats.attackDamagePercent += equipment.attackDamagePercent;
            totalStats.attackSpeed += equipment.attackSpeed;
            totalStats.hitRating += equipment.hitRating;
            
            // Weapon stats (only from MainHand)
            if (kvp.Key == EquipmentSlot.MainHand)
            {
                totalStats.weaponDamageMin = equipment.weaponDamageMin;
                totalStats.weaponDamageMax = equipment.weaponDamageMax;
                totalStats.weaponSpeed = equipment.weaponSpeed;
            }
            
            // Spell stats
            totalStats.spellDamage += equipment.spellDamage;
            totalStats.spellDamagePercent += equipment.spellDamagePercent;
            totalStats.spellHealing += equipment.spellHealing;
            totalStats.spellHealingPercent += equipment.spellHealingPercent;
            
            // Elemental damage - Fire
            totalStats.fireDamage += equipment.fireDamage;
            totalStats.fireDamagePercent += equipment.fireDamagePercent;
            
            // Elemental damage - Frost
            totalStats.frostDamage += equipment.frostDamage;
            totalStats.frostDamagePercent += equipment.frostDamagePercent;
            
            // Elemental damage - Nature
            totalStats.natureDamage += equipment.natureDamage;
            totalStats.natureDamagePercent += equipment.natureDamagePercent;
            
            // Elemental damage - Shadow
            totalStats.shadowDamage += equipment.shadowDamage;
            totalStats.shadowDamagePercent += equipment.shadowDamagePercent;
            
            // Elemental damage - Holy
            totalStats.holyDamage += equipment.holyDamage;
            totalStats.holyDamagePercent += equipment.holyDamagePercent;
            
            // DoT damage - Bleed
            totalStats.bleedDamage += equipment.bleedDamage;
            totalStats.bleedDamagePercent += equipment.bleedDamagePercent;
            
            // DoT damage - Poison
            totalStats.poisonDamage += equipment.poisonDamage;
            totalStats.poisonDamagePercent += equipment.poisonDamagePercent;
            
            // Health stats
            totalStats.maxHealth += equipment.maxHealth;
            totalStats.healthRegen += equipment.healthRegen;
            
            // Defensive stats
            totalStats.armor += equipment.armor;
            totalStats.dodge += equipment.dodge;
            
            // Special combat stats
            totalStats.criticalChance += equipment.criticalChance;
            totalStats.lifesteal += equipment.lifesteal;
            
            // Utility stats
            totalStats.xpBonus += equipment.xpBonus;
            totalStats.goldBonus += equipment.goldBonus;
            totalStats.afkGainsPercent += equipment.afkGainsPercent;
            
            // Profession stats
            totalStats.professionXPPercent += equipment.professionXPPercent;
            totalStats.professionPowerPercent += equipment.professionPowerPercent;
        }
        
        EventBus.Publish(new StatsRecalculatedEvent { equipmentStats = totalStats });
    }
    
    /// <summary>
    /// Get equipment in a specific slot
    /// </summary>
    public EquipmentData GetEquipment(EquipmentSlot slot)
    {
        return equippedItems.ContainsKey(slot) ? equippedItems[slot] : null;
    }
    
    /// <summary>
    /// Check if a slot is empty
    /// </summary>
    public bool IsSlotEmpty(EquipmentSlot slot)
    {
        return equippedItems[slot] == null;
    }
    
    /// <summary>
    /// Get all equipped items
    /// </summary>
    public Dictionary<EquipmentSlot, EquipmentData> GetAllEquippedItems()
    {
        return new Dictionary<EquipmentSlot, EquipmentData>(equippedItems);
    }
    
    // Stat getters
    public float GetTotalAttackPower() => totalStats.attackDamage;
    public float GetTotalAttackSpeed() => totalStats.attackSpeed;
    public float GetTotalMaxHealth() => totalStats.maxHealth;
    public float GetTotalHealthRegen() => totalStats.healthRegen;
    public float GetTotalArmor() => totalStats.armor;
    public float GetTotalDodge() => totalStats.dodge;
    public float GetTotalCriticalChance() => totalStats.criticalChance;
    public float GetTotalLifesteal() => totalStats.lifesteal;
    public float GetTotalXPBonus() => totalStats.xpBonus;
    public float GetTotalGoldBonus() => totalStats.goldBonus;
    public EquipmentStats GetTotalStats() => totalStats;
    
    /// <summary>
    /// Save equipment data
    /// </summary>
    public Dictionary<EquipmentSlot, string> GetEquipmentSaveData()
    {
        Dictionary<EquipmentSlot, string> saveData = new Dictionary<EquipmentSlot, string>();
        foreach (var kvp in equippedItems)
        {
            if (kvp.Value != null)
            {
                saveData[kvp.Key] = kvp.Value.name; // ScriptableObject name
            }
        }
        return saveData;
    }
    
    /// <summary>
    /// Clear all equipment slots
    /// </summary>
    public void ClearAllEquipment()
    {
        // Store old equipment for events
        Dictionary<EquipmentSlot, EquipmentData> oldEquipment = new Dictionary<EquipmentSlot, EquipmentData>(equippedItems);
        
        // Clear all slots
        foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
        {
            equippedItems[slot] = null;
        }
        
        RecalculateStats();
        
        // Notify listeners for each slot that was cleared
        foreach (var kvp in oldEquipment)
        {
            if (kvp.Value != null)
            {
                EventBus.Publish(new EquipmentChangedEvent { slot = kvp.Key, newEquipment = null, oldEquipment = kvp.Value });
            }
        }
    }
    
    /// <summary>
    /// Load equipment data
    /// </summary>
    public void LoadEquipmentData(Dictionary<EquipmentSlot, string> saveData)
    {
        Debug.Log($"[EquipmentManager] LoadEquipmentData called. Save data is null: {saveData == null}");
        
        // Clear all existing equipment first to ensure clean state per character
        ClearAllEquipment();
        
        if (saveData == null)
        {
            Debug.Log("[EquipmentManager] No save data provided, equipment cleared");
            return;
        }
        
        Debug.Log($"[EquipmentManager] Loading {saveData.Count} equipment items");
        
        // Load the character's equipment
        foreach (var kvp in saveData)
        {
            Debug.Log($"[EquipmentManager] Attempting to load equipment for slot {kvp.Key} with asset name: {kvp.Value}");
            
            // Load equipment from Resources - try Equipment subfolder first, then root
            EquipmentData equipment = Resources.Load<EquipmentData>("Equipment/" + kvp.Value);
            
            // If not found in Equipment subfolder, try root Resources folder
            if (equipment == null)
            {
                equipment = Resources.Load<EquipmentData>(kvp.Value);
            }
            
            if (equipment != null)
            {
                Debug.Log($"[EquipmentManager] Successfully loaded equipment: {equipment.equipmentName} for slot {kvp.Key}");
                equippedItems[kvp.Key] = equipment;
                // Notify listeners that equipment was loaded
                EventBus.Publish(new EquipmentChangedEvent { slot = kvp.Key, newEquipment = equipment, oldEquipment = null });
            }
            else
            {
                Debug.LogWarning($"[EquipmentManager] Failed to load equipment asset: {kvp.Value} (make sure it's in Resources/Equipment/ folder)");
            }
        }
        
        RecalculateStats();
        Debug.Log("[EquipmentManager] Equipment loading complete");
    }
    
    /// <summary>
    /// Auto-save character data (called when equipment changes)
    /// </summary>
    private void AutoSave(string reason = "auto")
    {
        // Get active character slot
        int characterSlot = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        
        if (characterSlot < 0)
        {
            Debug.LogWarning($"[EquipmentManager] Cannot auto-save ({reason}): no active character slot");
            return;
        }
        
        bool success = SaveSystem.SaveCurrentCharacter(characterSlot);
        
        if (success)
        {
            Debug.Log($"[EquipmentManager] Auto-saved character ({reason})");
        }
        else
        {
            Debug.LogError($"[EquipmentManager] Failed to auto-save character ({reason})");
        }
    }
}

/// <summary>
/// Container for total equipment stats
/// Includes all stat types: physical, spell, elemental, DoT, defensive, utility, and profession
/// </summary>
[System.Serializable]
public class EquipmentStats
{
    // Physical combat stats
    public float attackDamage = 0f;
    public float attackDamagePercent = 0f;
    public float attackSpeed = 0f;
    public float hitRating = 0f;
    
    // Weapon stats (from MainHand only)
    public float weaponDamageMin = 0f;
    public float weaponDamageMax = 0f;
    public float weaponSpeed = 0f;
    
    // Spell stats
    public float spellDamage = 0f;
    public float spellDamagePercent = 0f;
    public float spellHealing = 0f;
    public float spellHealingPercent = 0f;
    
    // Elemental damage - Fire
    public float fireDamage = 0f;
    public float fireDamagePercent = 0f;
    
    // Elemental damage - Frost
    public float frostDamage = 0f;
    public float frostDamagePercent = 0f;
    
    // Elemental damage - Nature
    public float natureDamage = 0f;
    public float natureDamagePercent = 0f;
    
    // Elemental damage - Shadow
    public float shadowDamage = 0f;
    public float shadowDamagePercent = 0f;
    
    // Elemental damage - Holy
    public float holyDamage = 0f;
    public float holyDamagePercent = 0f;
    
    // DoT damage - Bleed
    public float bleedDamage = 0f;
    public float bleedDamagePercent = 0f;
    
    // DoT damage - Poison
    public float poisonDamage = 0f;
    public float poisonDamagePercent = 0f;
    
    // Health stats
    public float maxHealth = 0f;
    public float healthRegen = 0f;
    
    // Defensive stats
    public float armor = 0f;
    public float dodge = 0f;
    
    // Special combat stats
    public float criticalChance = 0f;
    public float lifesteal = 0f;
    
    // Utility stats
    public float xpBonus = 0f;
    public float goldBonus = 0f;
    public float afkGainsPercent = 0f;
    
    // Profession stats
    public float professionXPPercent = 0f;
    public float professionPowerPercent = 0f;
}
