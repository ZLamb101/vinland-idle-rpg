using UnityEngine;

/// <summary>
/// Handles combat calculations and logic
/// Extracted from CombatManager to follow Single Responsibility Principle
/// </summary>
public static class CombatLogic
{
    /// <summary>
    /// Calculate player stats including equipment and talent bonuses
    /// </summary>
    public static void CalculatePlayerStats(out float maxHealth, out float currentHealth, out float attackDamage, out float attackSpeed)
    {
        // Base stats from character
        var characterService = Services.Get<ICharacterService>();
        if (characterService != null)
        {
            maxHealth = characterService.GetMaxHealthWithTalents();
            currentHealth = characterService.GetCurrentHealth();
        }
        else
        {
            maxHealth = 100f;
            currentHealth = 100f;
        }
        
        // Base attack values
        attackDamage = 10f;
        attackSpeed = 1.5f;
        
        // Add equipment bonuses
        var equipmentService = Services.Get<IEquipmentService>();
        if (equipmentService != null)
        {
            EquipmentStats equipStats = equipmentService.GetTotalStats();
            
            maxHealth += equipStats.maxHealth;
            attackDamage += equipStats.attackDamage;
            attackSpeed += equipStats.attackSpeed;
        }
        
        // Add talent bonuses
        var talentService = Services.Get<ITalentService>();
        if (talentService != null)
        {
            TalentBonuses talents = talentService.GetTotalBonuses();
            
            // Additive bonuses
            attackDamage += talents.attackDamage;
            attackSpeed += talents.attackSpeed;
            
            // Percentage multipliers
            attackDamage *= (1f + talents.damageMultiplier);
        }
        
        // Ensure health doesn't exceed max
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }
    
    /// <summary>
    /// Get combined combat stats from equipment and talents
    /// </summary>
    public static CombatStats GetCombatStats()
    {
        CombatStats stats = new CombatStats
        {
            critChance = 0f,
            critDamage = 2f, // Base crit is 2x
            lifesteal = 0f,
            dodge = 0f,
            armor = 0f,
            xpBonus = 0f,
            goldBonus = 0f
        };
        
        // Add equipment bonuses
        var equipmentService = Services.Get<IEquipmentService>();
        if (equipmentService != null)
        {
            EquipmentStats equipStats = equipmentService.GetTotalStats();
            stats.critChance += equipStats.criticalChance;
            stats.lifesteal += equipStats.lifesteal;
            stats.dodge += equipStats.dodge;
            stats.armor += equipStats.armor;
            stats.xpBonus += equipStats.xpBonus;
            stats.goldBonus += equipStats.goldBonus;
        }
        
        // Add talent bonuses
        var talentService = Services.Get<ITalentService>();
        if (talentService != null)
        {
            TalentBonuses talents = talentService.GetTotalBonuses();
            stats.critChance += talents.criticalChance;
            stats.critDamage += talents.criticalDamage;
            stats.lifesteal += talents.lifesteal;
            stats.dodge += talents.dodge;
            stats.armor += talents.armor;
            stats.xpBonus += talents.xpBonus;
            stats.goldBonus += talents.goldBonus;
        }
        
        return stats;
    }
    
    /// <summary>
    /// Calculate player attack damage with crit chance applied
    /// </summary>
    public static float CalculatePlayerDamage(float baseDamage, CombatStats stats, out bool wasCritical)
    {
        wasCritical = false;
        float damage = baseDamage;
        
        // Apply critical hit
        if (stats.critChance > 0 && Random.value <= stats.critChance)
        {
            damage *= stats.critDamage;
            wasCritical = true;
        }
        
        return damage;
    }
    
    /// <summary>
    /// Calculate monster damage with dodge and armor applied
    /// </summary>
    public static float CalculateMonsterDamage(float baseDamage, CombatStats stats, out bool wasDodged)
    {
        wasDodged = false;
        
        // Apply dodge
        if (stats.dodge > 0 && Random.value <= stats.dodge)
        {
            wasDodged = true;
            return 0f;
        }
        
        // Apply armor damage reduction
        float damage = baseDamage;
        if (stats.armor > 0)
        {
            damage *= (1f - stats.armor);
        }
        
        return damage;
    }
    
    /// <summary>
    /// Calculate XP and gold rewards with bonuses
    /// </summary>
    public static void CalculateRewards(int baseXP, int baseGold, CombatStats stats, out int finalXP, out int finalGold)
    {
        finalXP = Mathf.RoundToInt(baseXP * (1f + stats.xpBonus));
        finalGold = Mathf.RoundToInt(baseGold * (1f + stats.goldBonus));
    }
    
    /// <summary>
    /// Calculate lifesteal healing
    /// </summary>
    public static float CalculateLifesteal(float damage, float lifestealPercent)
    {
        return damage * lifestealPercent;
    }
}

/// <summary>
/// Helper struct for combat stats from equipment and talents
/// </summary>
public struct CombatStats
{
    public float critChance;
    public float critDamage;
    public float lifesteal;
    public float dodge;
    public float armor;
    public float xpBonus;
    public float goldBonus;
}

