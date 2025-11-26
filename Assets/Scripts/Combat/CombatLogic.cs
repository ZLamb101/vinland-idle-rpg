using UnityEngine;

/// <summary>
/// Handles combat calculations and logic
/// Extracted from CombatManager to follow Single Responsibility Principle
/// </summary>
public static class CombatLogic
{
    /// <summary>
    /// Calculate player stats including equipment and talent bonuses
    /// Now uses attribute-based stats (Strength, Agility, Stamina, Spirit)
    /// </summary>
    public static void CalculatePlayerStats(out float maxHealth, out float currentHealth, out float attackDamage, out float attackSpeed)
    {
        var characterService = Services.Get<ICharacterService>();
        CharacterData charData = characterService?.GetCharacterData();
        
        if (charData != null)
        {
            attackDamage = charData.GetAttackPower();
            maxHealth = charData.GetMaxHealth();
            currentHealth = characterService.GetCurrentHealth();
        }
        else
        {
            maxHealth = GameBalance.Combat.playerStartingHealth;
            currentHealth = GameBalance.Combat.playerStartingHealth;
            attackDamage = GameBalance.Combat.playerBaseAttackDamage;
        }
        
        attackSpeed = GameBalance.Combat.playerBaseAttackSpeed;
        
        var equipmentService = Services.Get<IEquipmentService>();
        if (equipmentService != null)
        {
            EquipmentStats equipStats = equipmentService.GetTotalStats();
            
            maxHealth += equipStats.maxHealth;
            attackDamage += equipStats.attackDamage;
            attackSpeed += equipStats.attackSpeed;
        }
        
        var talentService = Services.Get<ITalentService>();
        if (talentService != null)
        {
            TalentBonuses talents = talentService.GetTotalBonuses();
            
            maxHealth += talents.maxHealth;
            maxHealth *= (1f + talents.healthMultiplier);
            
            attackDamage += talents.attackDamage;
            attackSpeed += talents.attackSpeed;
            
            attackDamage *= (1f + talents.damageMultiplier);
        }
        
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }
    
    /// <summary>
    /// Get combined combat stats from equipment and talents
    /// Now includes attribute-based stats (Agility for crit/dodge, Strength for armor)
    /// </summary>
    public static CombatStats GetCombatStats()
    {
        CombatStats stats = new CombatStats
        {
            critChance = 0f,
            critDamage = GameBalance.Combat.playerBaseCritDamage,
            lifesteal = 0f,
            dodge = 0f,
            armor = 0f,
            xpBonus = 0f,
            goldBonus = 0f,
            healthRegen = 0f,
            afkGainsPercent = GameBalance.Combat.baseAfkGainsPercent // Base 25% AFK gains
        };
        
        var characterService = Services.Get<ICharacterService>();
        CharacterData charData = characterService?.GetCharacterData();
        
        if (charData != null)
        {
            stats.critChance += charData.GetCritChance();
            stats.dodge += charData.GetDodgeChance();
            stats.armor += charData.GetArmor(); // Armor is flat value, needs conversion to % for damage reduction
            stats.healthRegen += charData.GetHealthRegen();
        }
        
        var equipmentService = Services.Get<IEquipmentService>();
        if (equipmentService != null)
        {
            EquipmentStats equipStats = equipmentService.GetTotalStats();
            stats.critChance += equipStats.criticalChance;
            stats.lifesteal += equipStats.lifesteal;
            stats.dodge += equipStats.dodge;
            stats.armor += equipStats.armor; // Equipment armor is already in percentage format
            stats.xpBonus += equipStats.xpBonus;
            stats.goldBonus += equipStats.goldBonus;
            stats.healthRegen += equipStats.healthRegen;
            stats.afkGainsPercent += equipStats.afkGainsPercent; // Add equipment AFK gains
        }
        
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
            stats.afkGainsPercent += talents.afkGainsPercent; // Add talent AFK gains
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
        
        if (stats.dodge > 0 && Random.value <= stats.dodge)
        {
            wasDodged = true;
            return 0f;
        }
        
        float damage = baseDamage;
        if (stats.armor > 0)
        {
            damage *= (1f - stats.armor);
        }
        
        return damage;
    }
    
    /// <summary>
    /// Calculate lifesteal healing
    /// </summary>
    public static float CalculateLifesteal(float damage, float lifestealPercent)
    {
        return damage * lifestealPercent;
    }
    
    /// <summary>
    /// Calculate miss chance based on level difference
    /// Returns the additional miss chance from level difference (not including base 1%)
    /// </summary>
    public static float CalculateMissChance(int playerLevel, int enemyLevel)
    {
        int levelDiff = enemyLevel - playerLevel;
        
        // If player is higher level than enemy, miss chance is reduced (can be negative)
        // If enemy is higher level, miss chance increases
        if (levelDiff <= 3)
        {
            // Formula: 5% + (levelDiff * 1%)
            return 0.05f + (levelDiff * 0.01f);
        }
        else
        {
            // Formula: 6% + ((levelDiff - 3) * 4%)
            return 0.06f + ((levelDiff - 3) * 0.04f);
        }
    }
    
    /// <summary>
    /// Calculate XP required to reach the next level from a given level
    /// Shared utility to avoid duplication of the XP formula
    /// </summary>
    public static int GetXPRequiredForLevel(int level)
    {
        return Mathf.FloorToInt(GameBalance.Progression.baseXPPerLevel * Mathf.Pow(level, GameBalance.Progression.xpScalingPerLevel));
    }
}

/// <summary>
/// Helper struct for combat stats from equipment, talents, and attributes
/// Includes health regeneration from Spirit attribute
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
    public float healthRegen; // Health regeneration per second (from Spirit attribute)
    public float afkGainsPercent; // AFK gains multiplier (0.25 = 25% of normal gains) 
}

