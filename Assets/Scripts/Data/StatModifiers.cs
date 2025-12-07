using UnityEngine;

/// <summary>
/// Stat modifiers applied by buffs/curses.
/// Used by both SkillData and AuraData.
/// </summary>
[System.Serializable]
public class StatModifiers
{
    [Header("Flat Bonuses")]
    [Tooltip("Flat attack damage bonus/penalty")]
    public float attackDamage = 0f;
    
    [Tooltip("Attack speed modifier (negative = faster)")]
    public float attackSpeed = 0f;
    
    [Tooltip("Flat max health bonus/penalty")]
    public float maxHealth = 0f;
    
    [Tooltip("Flat armor bonus/penalty (as damage reduction %)")]
    public float armor = 0f;
    
    [Header("Percentage Modifiers")]
    [Tooltip("Damage dealt multiplier (0.1 = +10% damage)")]
    public float damageMultiplier = 0f;
    
    [Tooltip("Attack speed multiplier (0.1 = +10% faster, -0.1 = 10% slower)")]
    public float attackSpeedMultiplier = 0f;
    
    [Tooltip("Damage taken multiplier (-0.1 = -10% damage taken)")]
    public float damageTakenMultiplier = 0f;
    
    [Tooltip("Movement/attack speed slow (0.3 = 30% slower)")]
    public float slowPercent = 0f;
    
    [Header("Special Effects")]
    [Tooltip("Makes target invulnerable while active")]
    public bool invulnerable = false;
    
    [Tooltip("Stuns target (cannot attack)")]
    public bool stunned = false;
    
    /// <summary>
    /// Check if these modifiers have any effect
    /// </summary>
    public bool HasAnyEffect()
    {
        return attackDamage != 0f || attackSpeed != 0f || attackSpeedMultiplier != 0f ||
               maxHealth != 0f || armor != 0f || damageMultiplier != 0f || 
               damageTakenMultiplier != 0f || slowPercent != 0f || invulnerable || stunned;
    }
    
    /// <summary>
    /// Get a formatted description of all active stat modifiers.
    /// Each modifier is on its own line with a leading indent.
    /// </summary>
    /// <param name="indent">Optional prefix for each line (e.g., "  " for indentation)</param>
    /// <returns>Formatted string with all non-zero modifiers</returns>
    public string GetFormattedDescription(string indent = "")
    {
        string desc = "";
        
        if (attackDamage != 0)
            desc += $"{indent}{(attackDamage > 0 ? "+" : "")}{attackDamage:F0} Attack Power\n";
        if (attackSpeed != 0)
            desc += $"{indent}{(attackSpeed > 0 ? "+" : "")}{attackSpeed:F2}s Attack Speed\n";
        if (attackSpeedMultiplier != 0)
            desc += $"{indent}{(attackSpeedMultiplier > 0 ? "+" : "")}{attackSpeedMultiplier * 100:F0}% Attack Speed\n";
        if (maxHealth != 0)
            desc += $"{indent}{(maxHealth > 0 ? "+" : "")}{maxHealth:F0} Max Health\n";
        if (armor != 0)
            desc += $"{indent}{(armor > 0 ? "+" : "")}{armor * 100:F0}% Armor\n";
        if (damageMultiplier != 0)
            desc += $"{indent}{(damageMultiplier > 0 ? "+" : "")}{damageMultiplier * 100:F0}% Damage Dealt\n";
        if (damageTakenMultiplier != 0)
            desc += $"{indent}{(damageTakenMultiplier > 0 ? "+" : "")}{damageTakenMultiplier * 100:F0}% Damage Taken\n";
        if (slowPercent != 0)
            desc += $"{indent}{slowPercent * 100:F0}% Slow\n";
        if (invulnerable)
            desc += $"{indent}Invulnerable\n";
        if (stunned)
            desc += $"{indent}Stunned\n";
        
        return desc;
    }
}

