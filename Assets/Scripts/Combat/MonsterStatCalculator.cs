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

public struct CalculatedMonsterRewards
{
    public int xpReward;
    public int goldReward;
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
        
        // Ensure level is within valid range
        int minLevel = Mathf.Max(1, monsterData.minLevel);
        int maxLevel = Mathf.Max(minLevel, monsterData.maxLevel);
        int finalLevel = Mathf.Clamp(level, minLevel, maxLevel);
        stats.level = finalLevel;
        
        // Calculate exponential scaling multiplier based on absolute level
        // Formula: (1 + scalingMultiplier) ^ level
        // Example: Level 8 with 15% scaling = (1.15)^8 ≈ 3.06x
        float scalingMultiplier = Mathf.Pow(1.0f + GameBalance.Combat.monsterLevelScalingMultiplier, finalLevel);
        
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
    public static CalculatedMonsterRewards CalculateRewards(MonsterData monsterData, int level)
    {
        CalculatedMonsterRewards rewards = new CalculatedMonsterRewards();

        if (monsterData == null)
        {
            rewards.xpReward = GameBalance.Combat.monsterBaseXPReward;
            rewards.goldReward = GameBalance.Combat.monsterBaseGoldReward;
            return rewards;
        }
        
        // Ensure level is within valid range
        int minLevel = Mathf.Max(1, monsterData.minLevel);
        int maxLevel = Mathf.Max(minLevel, monsterData.maxLevel);
        int finalLevel = Mathf.Clamp(level, minLevel, maxLevel);
        
        // Calculate exponential scaling multiplier based on absolute level
        // Formula: (1 + scalingMultiplier) ^ level
        // Example: Level 8 with 15% scaling = (1.15)^8 ≈ 3.06x
        float scalingMultiplier = Mathf.Pow(1.0f + GameBalance.Combat.monsterLevelScalingMultiplier, finalLevel);
        
        // Scale rewards
        rewards.xpReward = Mathf.RoundToInt(GameBalance.Combat.monsterBaseXPReward * scalingMultiplier * monsterData.xpRewardModifier);
        rewards.goldReward = Mathf.RoundToInt(GameBalance.Combat.monsterBaseGoldReward * scalingMultiplier * monsterData.goldRewardModifier);

        return rewards;
    }
}

