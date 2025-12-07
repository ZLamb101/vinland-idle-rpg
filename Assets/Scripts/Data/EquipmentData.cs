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
    
    [Header("Weapon Damage (MainHand/OffHand only)")]
    [Tooltip("Minimum weapon damage per swing")]
    public float weaponDamageMin = 0f;
    
    [Tooltip("Maximum weapon damage per swing")]
    public float weaponDamageMax = 0f;
    
    [Tooltip("Base weapon speed in seconds (e.g., 2.5 = 2.5s per swing)")]
    public float weaponSpeed = 0f;
    
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
            description = "",
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
        return description + "\n\n" + GetStatsDescription();
    }
    

    /// <summary>
    /// Get just the stats text formatted for tooltips
    /// </summary>
    public string GetStatsDescription()
    {
        string desc = "";
        
        // --- BASE STATS (White) ---
        // Weapon Damage & Speed
        if (weaponDamageMin > 0 || weaponDamageMax > 0)
        {
            float dps = (weaponDamageMin + weaponDamageMax) / 2f / (weaponSpeed > 0 ? weaponSpeed : 1f);
            desc += $"{weaponDamageMin:F0} - {weaponDamageMax:F0} Damage ({dps:F1} DPS)\n";
        }
        if (weaponSpeed > 0) desc += $"{weaponSpeed:F2} Speed\n";
        
        // Armor
        if (armor > 0) desc += $"{armor:F0} Armor\n";
        
        // --- BONUS STATS (Green #1EFF00) ---
        string greenHex = "<color=#1EFF00>";
        string endColor = "</color>";

        // Physical combat stats
        if (attackDamage > 0) desc += $"{greenHex}+{attackDamage:F0} Attack Power{endColor}\n";
        if (attackDamagePercent > 0) desc += $"{greenHex}+{attackDamagePercent * 100:F0}% Attack Power{endColor}\n";
        if (attackSpeed != 0) desc += $"{greenHex}{(attackSpeed < 0 ? "" : "+")}{attackSpeed:F2}s Attack Speed Bonus{endColor}\n";
        if (hitRating > 0) desc += $"{greenHex}+{hitRating:F0} Hit Rating{endColor}\n";
        
        // Spell stats
        if (spellDamage > 0) desc += $"{greenHex}+{spellDamage:F0} Spell Damage{endColor}\n";
        if (spellDamagePercent > 0) desc += $"{greenHex}+{spellDamagePercent * 100:F0}% Spell Damage{endColor}\n";
        if (spellHealing > 0) desc += $"{greenHex}+{spellHealing:F0} Healing Power{endColor}\n";
        if (spellHealingPercent > 0) desc += $"{greenHex}+{spellHealingPercent * 100:F0}% Healing Power{endColor}\n";
        
        // Elemental damage
        if (fireDamage > 0) desc += $"{greenHex}+{fireDamage:F0} Fire Damage{endColor}\n";
        if (fireDamagePercent > 0) desc += $"{greenHex}+{fireDamagePercent * 100:F0}% Fire Damage{endColor}\n";
        if (frostDamage > 0) desc += $"{greenHex}+{frostDamage:F0} Frost Damage{endColor}\n";
        if (frostDamagePercent > 0) desc += $"{greenHex}+{frostDamagePercent * 100:F0}% Frost Damage{endColor}\n";
        if (natureDamage > 0) desc += $"{greenHex}+{natureDamage:F0} Nature Damage{endColor}\n";
        if (natureDamagePercent > 0) desc += $"{greenHex}+{natureDamagePercent * 100:F0}% Nature Damage{endColor}\n";
        if (shadowDamage > 0) desc += $"{greenHex}+{shadowDamage:F0} Shadow Damage{endColor}\n";
        if (shadowDamagePercent > 0) desc += $"{greenHex}+{shadowDamagePercent * 100:F0}% Shadow Damage{endColor}\n";
        if (holyDamage > 0) desc += $"{greenHex}+{holyDamage:F0} Holy Damage{endColor}\n";
        if (holyDamagePercent > 0) desc += $"{greenHex}+{holyDamagePercent * 100:F0}% Holy Damage{endColor}\n";
        
        // DoT damage
        if (bleedDamage > 0) desc += $"{greenHex}+{bleedDamage:F0} Bleed Damage{endColor}\n";
        if (bleedDamagePercent > 0) desc += $"{greenHex}+{bleedDamagePercent * 100:F0}% Bleed Damage{endColor}\n";
        if (poisonDamage > 0) desc += $"{greenHex}+{poisonDamage:F0} Poison Damage{endColor}\n";
        if (poisonDamagePercent > 0) desc += $"{greenHex}+{poisonDamagePercent * 100:F0}% Poison Damage{endColor}\n";
        
        // Health stats
        if (maxHealth > 0) desc += $"{greenHex}+{maxHealth:F0} Max Health{endColor}\n";
        if (healthRegen > 0) desc += $"{greenHex}+{healthRegen:F1} Health Regen{endColor}\n";
        
        // Defensive stats (Dodge is bonus, Armor was base)
        if (dodge > 0) desc += $"{greenHex}+{dodge * 100:F0}% Dodge{endColor}\n";
        
        // Special combat stats
        if (criticalChance > 0) desc += $"{greenHex}+{criticalChance * 100:F0}% Critical Chance{endColor}\n";
        if (lifesteal > 0) desc += $"{greenHex}+{lifesteal * 100:F0}% Lifesteal{endColor}\n";
        
        // Utility stats
        if (xpBonus > 0) desc += $"{greenHex}+{xpBonus * 100:F0}% XP Gain{endColor}\n";
        if (goldBonus > 0) desc += $"{greenHex}+{goldBonus * 100:F0}% Gold Gain{endColor}\n";
        if (afkGainsPercent > 0) desc += $"{greenHex}+{afkGainsPercent * 100:F0}% AFK Gains{endColor}\n";
        
        // Profession stats
        if (professionXPPercent > 0) desc += $"{greenHex}+{professionXPPercent * 100:F0}% Profession XP{endColor}\n";
        if (professionPowerPercent > 0) desc += $"{greenHex}+{professionPowerPercent * 100:F0}% Profession Power{endColor}\n";
        
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
