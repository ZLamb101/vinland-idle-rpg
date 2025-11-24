using UnityEngine;

/// <summary>
/// Calculated monster stats for a specific level
/// </summary>
public struct CalculatedMonsterStats
{
    public float health;
    public float attackDamage;
    public float attackSpeed;
    public float armor;
    public float attackRange;
    public int level;
}

/// <summary>
/// Utility class for calculating monster stats based on level and modifiers
/// </summary>
public static class MonsterStatCalculator
{
    /// <summary>
    /// Get a random level within the monster's level range (inclusive)
    /// </summary>
    public static int GetRandomLevel(MonsterData monsterData)
    {
        if (monsterData == null)
            return 1;
        
        // Clamp min/max level to valid range
        int minLevel = Mathf.Max(1, monsterData.minLevel);
        int maxLevel = Mathf.Max(minLevel, monsterData.maxLevel);
        
        return Random.Range(minLevel, maxLevel + 1);
    }
    
    /// <summary>
    /// Calculate monster stats for a specific level
    /// Uses base stats from GameBalance and applies level scaling and modifiers
    /// </summary>
    public static CalculatedMonsterStats CalculateMonsterStats(MonsterData monsterData, int level)
    {
        CalculatedMonsterStats stats = new CalculatedMonsterStats();
        
        if (monsterData == null)
        {
            // Fallback to default values
            stats.level = 1;
            stats.health = GameBalance.Combat.monsterBaseHealth;
            stats.attackDamage = GameBalance.Combat.monsterBaseAttackDamage;
            stats.attackSpeed = GameBalance.Combat.monsterBaseAttackSpeed;
            stats.armor = GameBalance.Combat.monsterBaseArmor;
            stats.attackRange = GameBalance.Combat.monsterMeleeRange;
            return stats;
        }
        
        // Clamp level to valid range
        int minLevel = Mathf.Max(1, monsterData.minLevel);
        int maxLevel = Mathf.Max(minLevel, monsterData.maxLevel);
        int clampedLevel = Mathf.Clamp(level, minLevel, maxLevel);
        stats.level = clampedLevel;
        
        // Calculate level scaling multiplier (15% per level above minimum)
        int levelDifference = clampedLevel - minLevel;
        float scalingMultiplier = 1f + (levelDifference * GameBalance.Combat.monsterLevelScalingMultiplier);
        
        // Get base stats from GameBalance
        float baseHealth = GameBalance.Combat.monsterBaseHealth;
        float baseDamage = GameBalance.Combat.monsterBaseAttackDamage;
        float baseSpeed = GameBalance.Combat.monsterBaseAttackSpeed;
        float baseArmor = GameBalance.Combat.monsterBaseArmor;
        
        // Calculate final stats: base * scaling * modifier
        stats.health = baseHealth * scalingMultiplier * monsterData.healthModifier;
        stats.attackDamage = baseDamage * scalingMultiplier * monsterData.damageModifier;
        stats.attackSpeed = baseSpeed * monsterData.speedModifier; // Speed modifier directly multiplies (lower = faster)
        stats.armor = baseArmor * monsterData.armorModifier; // Armor doesn't scale with level
        
        // Set attack range based on type
        stats.attackRange = monsterData.attackRangeType == MonsterAttackRangeType.Melee
            ? GameBalance.Combat.monsterMeleeRange
            : GameBalance.Combat.monsterRangedRange;
        
        return stats;
    }
    
    /// <summary>
    /// Calculate scaled rewards for a monster at a specific level
    /// </summary>
    public static void CalculateRewards(MonsterData monsterData, int level, out int xpReward, out int goldReward)
    {
        if (monsterData == null)
        {
            xpReward = 10;
            goldReward = 5;
            return;
        }
        
        // Clamp level to valid range
        int minLevel = Mathf.Max(1, monsterData.minLevel);
        int maxLevel = Mathf.Max(minLevel, monsterData.maxLevel);
        int clampedLevel = Mathf.Clamp(level, minLevel, maxLevel);
        
        // Calculate level scaling multiplier
        int levelDifference = clampedLevel - minLevel;
        float scalingMultiplier = 1f + (levelDifference * GameBalance.Combat.monsterLevelScalingMultiplier);
        
        // Scale rewards
        xpReward = Mathf.RoundToInt(monsterData.baseXPReward * scalingMultiplier);
        goldReward = Mathf.RoundToInt(monsterData.baseGoldReward * scalingMultiplier);
    }
}

