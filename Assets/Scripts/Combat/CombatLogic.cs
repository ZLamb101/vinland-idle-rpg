using UnityEngine;

/// <summary>
/// Handles combat calculations and logic
/// Extracted from CombatManager to follow Single Responsibility Principle
/// </summary>
public static class CombatLogic
{
    /// <summary>
    /// Armor constant for WoW-style damage reduction formula.
    /// Higher K = more armor needed for same DR.
    /// </summary>
    public const float ARMOR_CONSTANT = 400f;
    
    /// <summary>
    /// Maximum damage reduction from armor (75% cap)
    /// </summary>
    public const float MAX_ARMOR_DR = 0.75f;
    
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
            
            // Apply percentage bonuses
            attackDamage *= (1f + equipStats.attackDamagePercent);
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
            hitRating = 0f,
            xpBonus = 0f,
            goldBonus = 0f,
            healthRegen = 0f,
            afkGainsPercent = GameBalance.Combat.baseAfkGainsPercent,
            
            // Spell stats
            spellDamage = 0f,
            spellDamagePercent = 0f,
            spellHealing = 0f,
            spellHealingPercent = 0f,
            
            // Elemental damage
            fireDamage = 0f,
            fireDamagePercent = 0f,
            frostDamage = 0f,
            frostDamagePercent = 0f,
            natureDamage = 0f,
            natureDamagePercent = 0f,
            shadowDamage = 0f,
            shadowDamagePercent = 0f,
            holyDamage = 0f,
            holyDamagePercent = 0f,
            
            // DoT damage
            bleedDamage = 0f,
            bleedDamagePercent = 0f,
            poisonDamage = 0f,
            poisonDamagePercent = 0f,
            
            // Profession stats
            professionXPPercent = 0f,
            professionPowerPercent = 0f
        };
        
        var characterService = Services.Get<ICharacterService>();
        CharacterData charData = characterService?.GetCharacterData();
        
        if (charData != null)
        {
            stats.critChance += charData.GetCritChance();
            stats.dodge += charData.GetDodgeChance();
            stats.armor += charData.GetArmor(); // Now raw armor value from attributes
            stats.healthRegen += charData.GetHealthRegen();
        }
        
        var equipmentService = Services.Get<IEquipmentService>();
        if (equipmentService != null)
        {
            EquipmentStats equipStats = equipmentService.GetTotalStats();
            
            // Core combat stats
            stats.critChance += equipStats.criticalChance;
            stats.lifesteal += equipStats.lifesteal;
            stats.dodge += equipStats.dodge;
            stats.armor += equipStats.armor; // Now raw armor value
            stats.hitRating += equipStats.hitRating;
            stats.healthRegen += equipStats.healthRegen;
            
            // Utility stats
            stats.xpBonus += equipStats.xpBonus;
            stats.goldBonus += equipStats.goldBonus;
            stats.afkGainsPercent += equipStats.afkGainsPercent;
            
            // Spell stats
            stats.spellDamage += equipStats.spellDamage;
            stats.spellDamagePercent += equipStats.spellDamagePercent;
            stats.spellHealing += equipStats.spellHealing;
            stats.spellHealingPercent += equipStats.spellHealingPercent;
            
            // Elemental damage
            stats.fireDamage += equipStats.fireDamage;
            stats.fireDamagePercent += equipStats.fireDamagePercent;
            stats.frostDamage += equipStats.frostDamage;
            stats.frostDamagePercent += equipStats.frostDamagePercent;
            stats.natureDamage += equipStats.natureDamage;
            stats.natureDamagePercent += equipStats.natureDamagePercent;
            stats.shadowDamage += equipStats.shadowDamage;
            stats.shadowDamagePercent += equipStats.shadowDamagePercent;
            stats.holyDamage += equipStats.holyDamage;
            stats.holyDamagePercent += equipStats.holyDamagePercent;
            
            // DoT damage
            stats.bleedDamage += equipStats.bleedDamage;
            stats.bleedDamagePercent += equipStats.bleedDamagePercent;
            stats.poisonDamage += equipStats.poisonDamage;
            stats.poisonDamagePercent += equipStats.poisonDamagePercent;
            
            // Profession stats
            stats.professionXPPercent += equipStats.professionXPPercent;
            stats.professionPowerPercent += equipStats.professionPowerPercent;
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
            stats.afkGainsPercent += talents.afkGainsPercent;
        }
        
        return stats;
    }
    
    /// <summary>
    /// Calculate damage reduction percentage from raw armor using WoW-style formula.
    /// Formula: DR = Armor / (Armor + K * AttackerLevel)
    /// </summary>
    /// <param name="armor">Raw armor value</param>
    /// <param name="attackerLevel">Level of the attacker (enemy)</param>
    /// <returns>Damage reduction as percentage (0.0 to MAX_ARMOR_DR)</returns>
    public static float CalculateArmorDamageReduction(float armor, int attackerLevel)
    {
        if (armor <= 0) return 0f;
        if (attackerLevel < 1) attackerLevel = 1;
        
        float damageReduction = armor / (armor + ARMOR_CONSTANT * attackerLevel);
        return Mathf.Min(damageReduction, MAX_ARMOR_DR);
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
    /// Calculate monster damage with dodge and armor applied.
    /// Now uses WoW-style armor formula with raw armor values.
    /// </summary>
    /// <param name="baseDamage">Raw damage from monster</param>
    /// <param name="stats">Player's combat stats</param>
    /// <param name="attackerLevel">Level of the attacking monster</param>
    /// <param name="wasDodged">Out parameter indicating if attack was dodged</param>
    /// <returns>Final damage after mitigation</returns>
    public static float CalculateMonsterDamage(float baseDamage, CombatStats stats, int attackerLevel, out bool wasDodged)
    {
        wasDodged = false;
        
        // Check dodge first
        if (stats.dodge > 0 && Random.value <= stats.dodge)
        {
            wasDodged = true;
            return 0f;
        }
        
        float damage = baseDamage;
        
        // Apply armor damage reduction using WoW-style formula
        if (stats.armor > 0)
        {
            float damageReduction = CalculateArmorDamageReduction(stats.armor, attackerLevel);
            damage *= (1f - damageReduction);
        }
        
        return damage;
    }
    
    /// <summary>
    /// Legacy overload for backwards compatibility - assumes attacker level 1
    /// </summary>
    public static float CalculateMonsterDamage(float baseDamage, CombatStats stats, out bool wasDodged)
    {
        return CalculateMonsterDamage(baseDamage, stats, 1, out wasDodged);
    }
    
    /// <summary>
    /// Calculate lifesteal healing
    /// </summary>
    public static float CalculateLifesteal(float damage, float lifestealPercent)
    {
        return damage * lifestealPercent;
    }
    
    /// <summary>
    /// Calculate miss chance based on level difference and hit rating.
    /// Hit rating reduces miss chance.
    /// </summary>
    /// <param name="playerLevel">Player's level</param>
    /// <param name="enemyLevel">Enemy's level</param>
    /// <param name="hitRating">Player's hit rating (reduces miss chance)</param>
    /// <returns>Final miss chance after hit rating reduction</returns>
    public static float CalculateMissChance(int playerLevel, int enemyLevel, float hitRating = 0f)
    {
        int levelDiff = enemyLevel - playerLevel;
        
        float baseMissChance;
        
        // If player is higher level than enemy, miss chance is reduced (can be negative)
        // If enemy is higher level, miss chance increases
        if (levelDiff <= 3)
        {
            // Formula: 5% + (levelDiff * 1%)
            baseMissChance = 0.05f + (levelDiff * 0.01f);
        }
        else
        {
            // Formula: 6% + ((levelDiff - 3) * 4%)
            baseMissChance = 0.06f + ((levelDiff - 3) * 0.04f);
        }
        
        // Hit rating reduces miss chance (1 hit rating = 0.1% miss reduction)
        float missReduction = hitRating * 0.001f;
        float finalMissChance = baseMissChance - missReduction;
        
        // Miss chance can't go below 1% (always a small chance to miss)
        return Mathf.Max(finalMissChance, 0.01f);
    }
    
    /// <summary>
    /// Calculate XP required to reach the next level from a given level
    /// Shared utility to avoid duplication of the XP formula
    /// </summary>
    public static int GetXPRequiredForLevel(int level)
    {
        return Mathf.FloorToInt(GameBalance.Progression.baseXPPerLevel * Mathf.Pow(level, GameBalance.Progression.xpScalingPerLevel));
    }
    
    /// <summary>
    /// Calculate total spell damage including base + bonuses
    /// </summary>
    public static float CalculateSpellDamage(float baseSpellDamage, CombatStats stats)
    {
        float totalDamage = baseSpellDamage + stats.spellDamage;
        totalDamage *= (1f + stats.spellDamagePercent);
        return totalDamage;
    }
    
    /// <summary>
    /// Calculate total healing including base + bonuses
    /// </summary>
    public static float CalculateHealing(float baseHealing, CombatStats stats)
    {
        float totalHealing = baseHealing + stats.spellHealing;
        totalHealing *= (1f + stats.spellHealingPercent);
        return totalHealing;
    }
    
    /// <summary>
    /// Calculate elemental damage with bonuses applied
    /// </summary>
    public static float CalculateElementalDamage(float baseDamage, ElementType element, CombatStats stats)
    {
        float bonusDamage = 0f;
        float bonusPercent = 0f;
        
        switch (element)
        {
            case ElementType.Fire:
                bonusDamage = stats.fireDamage;
                bonusPercent = stats.fireDamagePercent;
                break;
            case ElementType.Frost:
                bonusDamage = stats.frostDamage;
                bonusPercent = stats.frostDamagePercent;
                break;
            case ElementType.Nature:
                bonusDamage = stats.natureDamage;
                bonusPercent = stats.natureDamagePercent;
                break;
            case ElementType.Shadow:
                bonusDamage = stats.shadowDamage;
                bonusPercent = stats.shadowDamagePercent;
                break;
            case ElementType.Holy:
                bonusDamage = stats.holyDamage;
                bonusPercent = stats.holyDamagePercent;
                break;
        }
        
        float totalDamage = baseDamage + bonusDamage;
        totalDamage *= (1f + bonusPercent);
        return totalDamage;
    }
    
    /// <summary>
    /// Calculate DoT damage with bonuses applied
    /// </summary>
    public static float CalculateDotDamage(float baseDamage, DotType dotType, CombatStats stats)
    {
        float bonusDamage = 0f;
        float bonusPercent = 0f;
        
        switch (dotType)
        {
            case DotType.Bleed:
                bonusDamage = stats.bleedDamage;
                bonusPercent = stats.bleedDamagePercent;
                break;
            case DotType.Poison:
                bonusDamage = stats.poisonDamage;
                bonusPercent = stats.poisonDamagePercent;
                break;
        }
        
        float totalDamage = baseDamage + bonusDamage;
        totalDamage *= (1f + bonusPercent);
        return totalDamage;
    }
}

/// <summary>
/// Element types for elemental damage calculations
/// </summary>
public enum ElementType
{
    Fire,
    Frost,
    Nature,
    Shadow,
    Holy
}

/// <summary>
/// DoT (Damage over Time) types
/// </summary>
public enum DotType
{
    Bleed,
    Poison
}

/// <summary>
/// Helper struct for combat stats from equipment, talents, and attributes
/// Includes all stat types for comprehensive combat calculations
/// </summary>
public struct CombatStats
{
    // Core combat stats
    public float critChance;
    public float critDamage;
    public float lifesteal;
    public float dodge;
    public float armor;          // Raw armor value (use CalculateArmorDamageReduction for DR%)
    public float hitRating;      // Reduces miss chance
    
    // Utility stats
    public float xpBonus;
    public float goldBonus;
    public float healthRegen;
    public float afkGainsPercent;
    
    // Spell stats
    public float spellDamage;
    public float spellDamagePercent;
    public float spellHealing;
    public float spellHealingPercent;
    
    // Elemental damage - Fire
    public float fireDamage;
    public float fireDamagePercent;
    
    // Elemental damage - Frost
    public float frostDamage;
    public float frostDamagePercent;
    
    // Elemental damage - Nature
    public float natureDamage;
    public float natureDamagePercent;
    
    // Elemental damage - Shadow
    public float shadowDamage;
    public float shadowDamagePercent;
    
    // Elemental damage - Holy
    public float holyDamage;
    public float holyDamagePercent;
    
    // DoT damage - Bleed
    public float bleedDamage;
    public float bleedDamagePercent;
    
    // DoT damage - Poison
    public float poisonDamage;
    public float poisonDamagePercent;
    
    // Profession stats
    public float professionXPPercent;
    public float professionPowerPercent;
}
