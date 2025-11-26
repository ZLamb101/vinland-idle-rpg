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
            stats.level = 1;
            stats.health = GameBalance.Combat.monsterBaseHealth;
            stats.attackDamage = GameBalance.Combat.monsterBaseAttackDamage;
            stats.attackSpeed = GameBalance.Combat.monsterBaseAttackSpeed;
            stats.armor = GameBalance.Combat.monsterBaseArmor;
            stats.attackRange = GameBalance.Combat.monsterMeleeRange;
            return stats;
        }
        
        int minLevel = Mathf.Max(1, monsterData.minLevel);
        int maxLevel = Mathf.Max(minLevel, monsterData.maxLevel);
        int finalLevel = Mathf.Clamp(level, minLevel, maxLevel);
        stats.level = finalLevel;
        
        // Formula: (1 + scalingMultiplier) ^ level
        // Example: Level 8 with 15% scaling = (1.15)^8 ≈ 3.06x
        float scalingMultiplier = Mathf.Pow(1.0f + GameBalance.Combat.monsterLevelScalingMultiplier, finalLevel);
        
        float baseHealth = GameBalance.Combat.monsterBaseHealth;
        float baseDamage = GameBalance.Combat.monsterBaseAttackDamage;
        float baseSpeed = GameBalance.Combat.monsterBaseAttackSpeed;
        float baseArmor = GameBalance.Combat.monsterBaseArmor;
        
        // Calculate final stats: base * scaling * modifier
        stats.health = baseHealth * scalingMultiplier * monsterData.healthModifier;
        stats.attackDamage = baseDamage * scalingMultiplier * monsterData.damageModifier;
        stats.attackSpeed = baseSpeed * monsterData.speedModifier; 
        stats.armor = baseArmor * monsterData.armorModifier; 
        
        stats.attackRange = monsterData.attackRangeType == MonsterAttackRangeType.Melee
            ? GameBalance.Combat.monsterMeleeRange
            : GameBalance.Combat.monsterRangedRange;
        
        return stats;
    }
    
    /// <summary>
    /// Calculate scaled rewards for a monster at a specific level
    /// </summary>
    public static CalculatedMonsterRewards CalculateRewards(MonsterData monsterData, int monsterLevel, int playerLevel)
    {
        CalculatedMonsterRewards rewards = new CalculatedMonsterRewards();

        if (monsterData == null)
        {
            rewards.xpReward = GameBalance.Combat.monsterBaseXPReward;
            rewards.goldReward = GameBalance.Combat.monsterBaseGoldReward;
            return rewards;
        }

        // Formula: (1 + scalingMultiplier) ^ level
        // Example: Level 8 with 15% scaling = (1.15)^8 ≈ 3.06x
        float scalingMultiplier = Mathf.Pow(1.0f + GameBalance.Combat.monsterLevelScalingMultiplier, monsterLevel);
        
        if(playerLevel == monsterLevel){
            rewards.xpReward = ((monsterLevel * 5) + GameBalance.Combat.monsterBaseXPReward);
        }
        if(monsterLevel > playerLevel){
            rewards.xpReward = Mathf.RoundToInt(((monsterLevel * 5) + GameBalance.Combat.monsterBaseXPReward) * (1 + (monsterLevel - playerLevel) * 0.05f));
        }
        if(monsterLevel < playerLevel){
            int grayThresholdLevel = GetGrayLevelThreshold(monsterLevel);
            
            if(playerLevel >= grayThresholdLevel){
                rewards.xpReward = 0;
            } else {
                int difference = 0;
                if(playerLevel < 8) difference = 5;
                else if(playerLevel < 10) difference = 6;
                else if(playerLevel < 12) difference = 7;
                else if(playerLevel < 16) difference = 8;
                else if(playerLevel < 20) difference = 9;
                else if(playerLevel < 40) difference = 9 + Mathf.FloorToInt(playerLevel / 10);
                else difference = 5 + Mathf.FloorToInt(playerLevel / 5);
                rewards.xpReward = Mathf.RoundToInt((playerLevel * 5 + GameBalance.Combat.monsterBaseXPReward) * (1f - (float)(playerLevel - monsterLevel) / difference));
            }
        }
        
        rewards.goldReward = Mathf.RoundToInt(GameBalance.Combat.monsterBaseGoldReward * scalingMultiplier * monsterData.goldRewardModifier);

        return rewards;
    }
    
    /// <summary>
    /// Calculate the minimum player level at which a monster becomes gray (gives 0 XP)
    /// Returns the player level where the monster would start giving 0 XP
    /// </summary>
    public static int GetGrayLevelThreshold(int monsterLevel)
    {
        for (int playerLevel = monsterLevel; playerLevel <= 100; playerLevel++)
        {
            int grayLevel;
            if (playerLevel < 6) grayLevel = 0;
            else if (playerLevel < 40) grayLevel = playerLevel - 5 - Mathf.FloorToInt(playerLevel / 10);
            else grayLevel = playerLevel - 1 - Mathf.FloorToInt(playerLevel % 5);
            
            if (monsterLevel <= grayLevel)
            {
                return playerLevel;
            }
        }
        
        return 100;
    }
}

