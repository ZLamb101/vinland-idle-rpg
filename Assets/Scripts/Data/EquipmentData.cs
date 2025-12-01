using UnityEngine;

/// <summary>
/// Equipment slot types (simplified)
/// </summary>
public enum EquipmentSlot
{
    Head,
    Neck,
    Shoulders,
    Chest,
    Hands,          // Gloves
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
    
    [Header("Physical Combat Stats")]
    [Tooltip("Raw attack damage bonus")]
    public float attackDamage = 0f;
    
    [Tooltip("Attack damage percentage bonus (0.1 = +10% physical damage)")]
    public float attackDamagePercent = 0f;
    
    [Tooltip("Attack speed bonus (negative = faster, e.g., -0.1 = 0.1s faster)")]
    public float attackSpeed = 0f;
    
    [Tooltip("Hit rating - increases accuracy, reduces miss chance")]
    public float hitRating = 0f;
    
    [Header("Spell Stats")]
    [Tooltip("Raw spell damage bonus")]
    public float spellDamage = 0f;
    
    [Tooltip("Spell damage percentage bonus (0.1 = +10% spell damage)")]
    public float spellDamagePercent = 0f;
    
    [Tooltip("Raw spell healing bonus")]
    public float spellHealing = 0f;
    
    [Tooltip("Spell healing percentage bonus (0.1 = +10% healing)")]
    public float spellHealingPercent = 0f;
    
    [Header("Elemental Damage - Fire")]
    [Tooltip("Raw fire damage bonus")]
    public float fireDamage = 0f;
    
    [Tooltip("Fire damage percentage bonus (0.1 = +10% fire damage)")]
    public float fireDamagePercent = 0f;
    
    [Header("Elemental Damage - Frost")]
    [Tooltip("Raw frost damage bonus")]
    public float frostDamage = 0f;
    
    [Tooltip("Frost damage percentage bonus (0.1 = +10% frost damage)")]
    public float frostDamagePercent = 0f;
    
    [Header("Elemental Damage - Nature")]
    [Tooltip("Raw nature damage bonus")]
    public float natureDamage = 0f;
    
    [Tooltip("Nature damage percentage bonus (0.1 = +10% nature damage)")]
    public float natureDamagePercent = 0f;
    
    [Header("Elemental Damage - Shadow")]
    [Tooltip("Raw shadow damage bonus")]
    public float shadowDamage = 0f;
    
    [Tooltip("Shadow damage percentage bonus (0.1 = +10% shadow damage)")]
    public float shadowDamagePercent = 0f;
    
    [Header("Elemental Damage - Holy")]
    [Tooltip("Raw holy damage bonus")]
    public float holyDamage = 0f;
    
    [Tooltip("Holy damage percentage bonus (0.1 = +10% holy damage)")]
    public float holyDamagePercent = 0f;
    
    [Header("DoT Damage - Bleed")]
    [Tooltip("Raw bleed damage bonus")]
    public float bleedDamage = 0f;
    
    [Tooltip("Bleed damage percentage bonus (0.1 = +10% bleed damage)")]
    public float bleedDamagePercent = 0f;
    
    [Header("DoT Damage - Poison")]
    [Tooltip("Raw poison damage bonus")]
    public float poisonDamage = 0f;
    
    [Tooltip("Poison damage percentage bonus (0.1 = +10% poison damage)")]
    public float poisonDamagePercent = 0f;
    
    [Header("Health Stats")]
    [Tooltip("Max health bonus")]
    public float maxHealth = 0f;
    
    [Tooltip("Health regeneration per second")]
    public float healthRegen = 0f;
    
    [Header("Defensive Stats")]
    [Tooltip("Raw armor value - damage reduction calculated via formula: DR = Armor / (Armor + K * Level)")]
    public float armor = 0f;
    
    [Tooltip("Chance to dodge attacks (0.05 = 5% chance)")]
    public float dodge = 0f;
    
    [Header("Special Combat Stats")]
    [Tooltip("Critical hit chance (0.1 = 10% chance for 2x damage)")]
    public float criticalChance = 0f;
    
    [Tooltip("Lifesteal percentage (0.1 = heal 10% of damage dealt)")]
    public float lifesteal = 0f;
    
    [Header("Utility Stats")]
    [Tooltip("Extra XP gain percentage (0.1 = +10% XP)")]
    public float xpBonus = 0f;
    
    [Tooltip("Extra gold gain percentage (0.1 = +10% gold)")]
    public float goldBonus = 0f;
    
    [Tooltip("AFK gains percentage bonus (0.1 = +10% AFK gains, additive with base 25%)")]
    public float afkGainsPercent = 0f;
    
    [Header("Profession Stats")]
    [Tooltip("Profession XP gain percentage bonus (0.1 = +10% profession XP)")]
    public float professionXPPercent = 0f;
    
    [Tooltip("Profession power percentage bonus - increases crafting effectiveness (0.1 = +10%)")]
    public float professionPowerPercent = 0f;
    
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
        
        // Physical combat stats
        if (attackDamage > 0) desc += $"+{attackDamage:F0} Attack Damage\n";
        if (attackDamagePercent > 0) desc += $"+{attackDamagePercent * 100:F0}% Attack Damage\n";
        if (attackSpeed != 0) desc += $"{(attackSpeed < 0 ? "" : "+")}{attackSpeed:F2}s Attack Speed\n";
        if (hitRating > 0) desc += $"+{hitRating:F0} Hit Rating\n";
        
        // Spell stats
        if (spellDamage > 0) desc += $"+{spellDamage:F0} Spell Damage\n";
        if (spellDamagePercent > 0) desc += $"+{spellDamagePercent * 100:F0}% Spell Damage\n";
        if (spellHealing > 0) desc += $"+{spellHealing:F0} Spell Healing\n";
        if (spellHealingPercent > 0) desc += $"+{spellHealingPercent * 100:F0}% Spell Healing\n";
        
        // Elemental damage
        if (fireDamage > 0) desc += $"+{fireDamage:F0} Fire Damage\n";
        if (fireDamagePercent > 0) desc += $"+{fireDamagePercent * 100:F0}% Fire Damage\n";
        if (frostDamage > 0) desc += $"+{frostDamage:F0} Frost Damage\n";
        if (frostDamagePercent > 0) desc += $"+{frostDamagePercent * 100:F0}% Frost Damage\n";
        if (natureDamage > 0) desc += $"+{natureDamage:F0} Nature Damage\n";
        if (natureDamagePercent > 0) desc += $"+{natureDamagePercent * 100:F0}% Nature Damage\n";
        if (shadowDamage > 0) desc += $"+{shadowDamage:F0} Shadow Damage\n";
        if (shadowDamagePercent > 0) desc += $"+{shadowDamagePercent * 100:F0}% Shadow Damage\n";
        if (holyDamage > 0) desc += $"+{holyDamage:F0} Holy Damage\n";
        if (holyDamagePercent > 0) desc += $"+{holyDamagePercent * 100:F0}% Holy Damage\n";
        
        // DoT damage
        if (bleedDamage > 0) desc += $"+{bleedDamage:F0} Bleed Damage\n";
        if (bleedDamagePercent > 0) desc += $"+{bleedDamagePercent * 100:F0}% Bleed Damage\n";
        if (poisonDamage > 0) desc += $"+{poisonDamage:F0} Poison Damage\n";
        if (poisonDamagePercent > 0) desc += $"+{poisonDamagePercent * 100:F0}% Poison Damage\n";
        
        // Health stats
        if (maxHealth > 0) desc += $"+{maxHealth:F0} Max Health\n";
        if (healthRegen > 0) desc += $"+{healthRegen:F1} Health/sec\n";
        
        // Defensive stats
        if (armor > 0) desc += $"+{armor:F0} Armor\n";
        if (dodge > 0) desc += $"+{dodge * 100:F0}% Dodge Chance\n";
        
        // Special combat stats
        if (criticalChance > 0) desc += $"+{criticalChance * 100:F0}% Critical Chance\n";
        if (lifesteal > 0) desc += $"+{lifesteal * 100:F0}% Lifesteal\n";
        
        // Utility stats
        if (xpBonus > 0) desc += $"+{xpBonus * 100:F0}% XP Gain\n";
        if (goldBonus > 0) desc += $"+{goldBonus * 100:F0}% Gold Gain\n";
        if (afkGainsPercent > 0) desc += $"+{afkGainsPercent * 100:F0}% AFK Gains\n";
        
        // Profession stats
        if (professionXPPercent > 0) desc += $"+{professionXPPercent * 100:F0}% Profession XP\n";
        if (professionPowerPercent > 0) desc += $"+{professionPowerPercent * 100:F0}% Profession Power\n";
        
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
