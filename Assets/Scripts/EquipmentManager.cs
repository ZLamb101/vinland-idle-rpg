using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager for character equipment.
/// Handles equipping/unequipping items and calculating total stats.
/// </summary>
public class EquipmentManager : MonoBehaviour, IEquipmentService
{
    private static EquipmentManager instance;
    
    [System.Obsolete("Use Services.Get<IEquipmentService>() instead. Direct Instance access is deprecated.", true)]
    public static EquipmentManager Instance => instance;
    
    [Header("Equipment Slots")]
    private Dictionary<EquipmentSlot, EquipmentData> equippedItems = new Dictionary<EquipmentSlot, EquipmentData>();
    
    // Events for UI updates
    public event Action<EquipmentSlot, EquipmentData> OnEquipmentChanged;
    public event Action OnStatsRecalculated;
    
    // Cached total stats
    private EquipmentStats totalStats = new EquipmentStats();

    private ICharacterService characterService;
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
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

    /// <summary>
    /// Equip an item to its designated slot
    /// </summary>
    public bool EquipItem(EquipmentData equipment)
    {
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
        EquipmentData previousEquipment = equippedItems[slot];
        
        // Unequip previous item if exists
        if (previousEquipment != null)
        {
            UnequipItem(slot);
        }
        
        // Equip new item
        equippedItems[slot] = equipment;
        
        // Recalculate stats
        RecalculateStats();
        
        // Notify listeners
        OnEquipmentChanged?.Invoke(slot, equipment);
        return true;
    }
    
    /// <summary>
    /// Unequip item from a specific slot
    /// </summary>
    public EquipmentData UnequipItem(EquipmentSlot slot)
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
        OnEquipmentChanged?.Invoke(slot, null);
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
            
            totalStats.attackDamage += equipment.attackDamage;
            totalStats.attackSpeed += equipment.attackSpeed;
            totalStats.maxHealth += equipment.maxHealth;
            totalStats.healthRegen += equipment.healthRegen;
            totalStats.armor += equipment.armor;
            totalStats.dodge += equipment.dodge;
            totalStats.criticalChance += equipment.criticalChance;
            totalStats.lifesteal += equipment.lifesteal;
            totalStats.xpBonus += equipment.xpBonus;
            totalStats.goldBonus += equipment.goldBonus;
        }
        
        OnStatsRecalculated?.Invoke();
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
    public float GetTotalAttackDamage() => totalStats.attackDamage;
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
    /// Load equipment data
    /// </summary>
    public void LoadEquipmentData(Dictionary<EquipmentSlot, string> saveData)
    {
        if (saveData == null) return;
        
        foreach (var kvp in saveData)
        {
            // Load equipment from Resources or AssetDatabase
            // This requires equipment to be in Resources folder
            EquipmentData equipment = Resources.Load<EquipmentData>(kvp.Value);
            if (equipment != null)
            {
                equippedItems[kvp.Key] = equipment;
            }
        }
        
        RecalculateStats();
    }
}

/// <summary>
/// Container for total equipment stats
/// </summary>
[System.Serializable]
public class EquipmentStats
{
    public float attackDamage = 0f;
    public float attackSpeed = 0f;
    public float maxHealth = 0f;
    public float healthRegen = 0f;
    public float armor = 0f;
    public float dodge = 0f;
    public float criticalChance = 0f;
    public float lifesteal = 0f;
    public float xpBonus = 0f;
    public float goldBonus = 0f;
}





