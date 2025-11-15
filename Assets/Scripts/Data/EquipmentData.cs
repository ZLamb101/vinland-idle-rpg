using UnityEngine;

/// <summary>
/// Equipment slot types (simplified)
/// </summary>
public enum EquipmentSlot
{
    Head,
    Neck,
    Shoulders,
    Back,           // Cloak
    Chest,
    Hands,          // Gloves
    Waist,          // Belt
    Legs,
    Feet,           // Boots
    Ring1,
    Ring2,
    MainHand,       // Weapon
    OffHand         // Shield/Off-hand weapon
}

/// <summary>
/// ScriptableObject that defines equipment with stats and slot type.
/// Create instances via: Right-click in Project → Create → Vinland → Equipment
/// </summary>
[CreateAssetMenu(fileName = "New Equipment", menuName = "Vinland/Equipment", order = 4)]
public class EquipmentData : ScriptableObject
{
    [Header("Basic Info")]
    public string equipmentName = "Iron Sword";
    [TextArea(2, 4)]
    public string description = "A basic iron sword";
    public Sprite icon;
    
    [Header("Equipment Type")]
    public EquipmentSlot slot = EquipmentSlot.MainHand;
    public EquipmentTier tier = EquipmentTier.Common;
    
    [Header("Requirements")]
    public int levelRequired = 1;
    
    [Header("Combat Stats")]
    [Tooltip("Attack damage bonus")]
    public float attackDamage = 0f;
    
    [Tooltip("Attack speed bonus (negative = faster, e.g., -0.1 = 0.1s faster)")]
    public float attackSpeed = 0f;
    
    [Tooltip("Max health bonus")]
    public float maxHealth = 0f;
    
    [Tooltip("Health regeneration per second")]
    public float healthRegen = 0f;
    
    [Header("Defensive Stats")]
    [Tooltip("Damage reduction percentage (0.1 = 10% reduction)")]
    public float armor = 0f;
    
    [Tooltip("Chance to dodge attacks (0.05 = 5% chance)")]
    public float dodge = 0f;
    
    [Header("Special Stats")]
    [Tooltip("Critical hit chance (0.1 = 10% chance for 2x damage)")]
    public float criticalChance = 0f;
    
    [Tooltip("Lifesteal percentage (0.1 = heal 10% of damage dealt)")]
    public float lifesteal = 0f;
    
    [Tooltip("Extra XP gain percentage (0.1 = +10% XP)")]
    public float xpBonus = 0f;
    
    [Tooltip("Extra gold gain percentage (0.1 = +10% gold)")]
    public float goldBonus = 0f;
    
    [Header("Visual")]
    public Color rarityColor = Color.white;
    
    /// <summary>
    /// Create an InventoryItem from this equipment
    /// </summary>
    public InventoryItem CreateInventoryItem(int quantity = 1)
    {
        InventoryItem item = new InventoryItem
        {
            itemName = equipmentName,
            description = GetFullDescription(),
            icon = icon,
            quantity = quantity,
            maxStackSize = 1, // Equipment doesn't stack
            itemType = ItemType.Equipment
        };
        return item;
    }
    
    /// <summary>
    /// Get full description including stats
    /// </summary>
    public string GetFullDescription()
    {
        string desc = description + "\n\n";
        
        // Combat stats
        if (attackDamage > 0) desc += $"+{attackDamage:F0} Attack Damage\n";
        if (attackSpeed != 0) desc += $"{(attackSpeed < 0 ? "" : "+")}{attackSpeed:F2}s Attack Speed\n";
        if (maxHealth > 0) desc += $"+{maxHealth:F0} Max Health\n";
        if (healthRegen > 0) desc += $"+{healthRegen:F1} Health/sec\n";
        
        // Defensive stats
        if (armor > 0) desc += $"+{armor * 100:F0}% Damage Reduction\n";
        if (dodge > 0) desc += $"+{dodge * 100:F0}% Dodge Chance\n";
        
        // Special stats
        if (criticalChance > 0) desc += $"+{criticalChance * 100:F0}% Critical Chance\n";
        if (lifesteal > 0) desc += $"+{lifesteal * 100:F0}% Lifesteal\n";
        if (xpBonus > 0) desc += $"+{xpBonus * 100:F0}% XP Gain\n";
        if (goldBonus > 0) desc += $"+{goldBonus * 100:F0}% Gold Gain\n";
        
        return desc;
    }
}

/// <summary>
/// Equipment rarity/tier system
/// </summary>
public enum EquipmentTier
{
    Common,      // White/Gray
    Uncommon,    // Green
    Rare,        // Blue
    Epic,        // Purple
    Legendary    // Orange
}

