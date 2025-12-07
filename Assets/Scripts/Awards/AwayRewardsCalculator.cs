using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contains the rewards earned while away
/// </summary>
[System.Serializable]
public class AwayRewards
{
    public AwayActivityType activityType;
    public TimeSpan timeAway;
    public string activityName;
    
    public Dictionary<string, int> itemsGathered = new Dictionary<string, int>();
    
    public int xpEarned = 0;
    public int goldEarned = 0;
    public Dictionary<string, int> itemsDropped = new Dictionary<string, int>();
    public int monstersKilled = 0;
    
    public Dictionary<ProfessionType, int> professionXPEarned = new Dictionary<ProfessionType, int>();
}

/// <summary>
/// Calculates rewards earned while the player was away
/// </summary>
public static class AwayRewardsCalculator
{
    /// <summary>
    /// Calculate rewards for time spent away
    /// </summary>
    public static AwayRewards CalculateRewards(DateTime activityStartTime, AwayActivityType activityType, 
        ResourceData resource = null, MonsterData[] monsters = null, int mobCount = 1)
    {
        AwayRewards rewards = new AwayRewards
        {
            activityType = activityType,
            timeAway = DateTime.Now - activityStartTime
        };
        
        if (rewards.timeAway.TotalSeconds <= 0)
        {
            return rewards;
        }
        
        if (activityType == AwayActivityType.Mining && resource != null)
        {
            CalculateMiningRewards(rewards, resource, rewards.timeAway);
        }
        else if (activityType == AwayActivityType.Fighting && monsters != null && monsters.Length > 0)
        {
            CalculateFightingRewards(rewards, monsters, mobCount, rewards.timeAway);
        }
        else if (activityType == AwayActivityType.None)
        {
            CalculateNoneRewards(rewards, rewards.timeAway);
        }
        
        return rewards;
    }
    
    /// <summary>
    /// Calculate rewards for "doing Nothing"
    /// </summary>
    private static void CalculateNoneRewards(AwayRewards rewards, TimeSpan timeAway)
    {
        rewards.activityName = "doing Nothing";
    }
    
    /// <summary>
    /// Calculate mining rewards for time away
    /// </summary>
    private static void CalculateMiningRewards(AwayRewards rewards, ResourceData resource, TimeSpan timeAway)
    {
        rewards.activityName = $"Gathering {resource.resourceName}";
        
        if (resource.gatheredItem == null)
        {
            return;
        }
        
        float timePerGather = 1f / resource.gatherRate;
        float totalSeconds = (float)timeAway.TotalSeconds;
        int gatherCycles = Mathf.FloorToInt(totalSeconds / timePerGather);
        
        int baseTotalItems = gatherCycles * resource.itemsPerGather;
        
        // Apply AFK gains multiplier
        CombatStats stats = CombatLogic.GetCombatStats();
        int totalItems = Mathf.FloorToInt(baseTotalItems * stats.afkGainsPercent);
        
        Debug.Log($"[AwayRewards] Gathering calculation - Time away: {totalSeconds}s, Time per gather: {timePerGather}s, Gather cycles: {gatherCycles}, Items per gather: {resource.itemsPerGather}, Base items: {baseTotalItems}, AFK multiplier: {stats.afkGainsPercent:P0}, Final items: {totalItems}");
        
        if (totalItems > 0)
        {
            string itemName = resource.gatheredItem.itemName;
            rewards.itemsGathered[itemName] = totalItems;
            Debug.Log($"[AwayRewards] Added {totalItems} {itemName} to rewards");
        }
        else
        {
            Debug.LogWarning($"[AwayRewards] No items gathered - gatherCycles: {gatherCycles}, itemsPerGather: {resource.itemsPerGather}");
        }
        
        if (resource.professionXPReward > 0 && gatherCycles > 0)
        {
            int baseProfessionXP = gatherCycles * resource.professionXPReward;
            // Apply AFK gains multiplier to profession XP
            int totalProfessionXP = Mathf.FloorToInt(baseProfessionXP * stats.afkGainsPercent);
            rewards.professionXPEarned[resource.professionType] = totalProfessionXP;
            Debug.Log($"[AwayRewards] Earned {totalProfessionXP} {resource.professionType.GetDisplayName()} XP (base: {baseProfessionXP}, AFK: {stats.afkGainsPercent:P0})");
        }
    }
    
    /// <summary>
    /// Calculate fighting rewards for time away
    /// </summary>
    private static void CalculateFightingRewards(AwayRewards rewards, MonsterData[] monsters, int mobCount, TimeSpan timeAway)
    {
        if (monsters == null || monsters.Length == 0)
        {
            return;
        }

        if (monsters.Length == 1)
        {
            rewards.activityName = $"Fighting {monsters[0].monsterName}";
        }
        else
        {
            rewards.activityName = $"Fighting {monsters.Length} Monster Types";
        }
        
        int baseMonstersKilled = CalculateMonstersKilled(monsters, mobCount, timeAway);
        
        // Apply AFK gains multiplier to kill count first
        CombatStats stats = CombatLogic.GetCombatStats();
        int monstersKilled = Mathf.FloorToInt(baseMonstersKilled * stats.afkGainsPercent);
        rewards.monstersKilled = monstersKilled;
        
        if (monstersKilled <= 0)
        {
            return;
        }
        
        int playerLevel = GetPlayerLevel();
        int maxSafeLevel = CalculateMaxSafeLevel(monsters);
        
        // Calculate rewards from the reduced kill count (already has AFK multiplier applied)
        int totalXP = CalculateProgressiveXP(monsters, monstersKilled, playerLevel, maxSafeLevel, stats);
        
        int totalGold;
        Dictionary<string, int> totalItems;
        CalculateGoldAndItems(monsters, monstersKilled, playerLevel, stats, out totalGold, out totalItems);
        
        rewards.xpEarned = totalXP;
        rewards.goldEarned = totalGold;
        rewards.itemsDropped = totalItems;
        
        Debug.Log($"[AwayRewards] Total rewards - Base kills: {baseMonstersKilled}, AFK kills: {monstersKilled} ({stats.afkGainsPercent:P0}), XP: {totalXP}, Gold: {totalGold}, Items: {totalItems.Count} types, Max safe level: {maxSafeLevel}");
    }
    
    /// <summary>
    /// Calculate how many monsters were killed during away time
    /// </summary>
    private static int CalculateMonstersKilled(MonsterData[] monsters, int mobCount, TimeSpan timeAway)
    {
        float playerAttackPower = GameBalance.Combat.playerBaseAttackDamage;
        float playerAttackSpeed = GameBalance.Combat.playerBaseAttackSpeed;
        
        if (Services.TryGet<ICombatService>(out var combatService))
        {
            combatService.CalculatePlayerStats();
            playerAttackPower = combatService.GetPlayerAttackPower();
            playerAttackSpeed = combatService.GetPlayerAttackSpeed();
            Debug.Log($"[AwayRewards] Player stats from CombatService - Damage: {playerAttackPower}, Speed: {playerAttackSpeed}");
        }
        else
        {
            Debug.LogWarning("[AwayRewards] CombatService is null, using base config stats");
        }
        
        float averageMonsterHealth = 0f;
        foreach (MonsterData monster in monsters)
        {
            int avgLevel = Mathf.RoundToInt((monster.minLevel + monster.maxLevel) / 2f);
            CalculatedMonsterStats monsterStats = MonsterStatCalculator.CalculateMonsterStats(monster, avgLevel);
            averageMonsterHealth += monsterStats.health;
        }
        averageMonsterHealth /= monsters.Length;
        
        CombatStats stats = CombatLogic.GetCombatStats();
        float critMultiplier = 1f + stats.critChance * (stats.critDamage - 1f);
        float averageDamage = playerAttackPower * critMultiplier;
        
        float hitsNeeded = Mathf.Max(1f, Mathf.Ceil(averageMonsterHealth / averageDamage));
        float timePerKill = hitsNeeded * playerAttackSpeed;
        float totalSeconds = (float)timeAway.TotalSeconds;
        float killsPerSecond = mobCount / Mathf.Max(0.1f, timePerKill);
        int monstersKilled = Mathf.Max(1, Mathf.FloorToInt(totalSeconds * killsPerSecond));
        
        Debug.Log($"[AwayRewards] Fighting calculation - Time away: {totalSeconds}s ({timeAway.TotalMinutes:F2} min), Average monster health: {averageMonsterHealth}, Player damage: {playerAttackPower}, Crit chance: {stats.critChance:P0}, Crit damage: {stats.critDamage}x, Crit multiplier: {critMultiplier:F2}x, Average damage: {averageDamage:F1}, Hits needed: {hitsNeeded}, Time per kill: {timePerKill:F2}s, Kills per second: {killsPerSecond:F3}, Monsters killed: {monstersKilled}, Mob count: {mobCount}");
        
        return monstersKilled;
    }
    
    /// <summary>
    /// Get player level from character service
    /// </summary>
    private static int GetPlayerLevel()
    {
        if (Services.TryGet<ICharacterService>(out var characterService))
        {
            return characterService.GetLevel();
        }
        else
        {
            Debug.LogWarning("[AwayRewards] CharacterService not found, using default player level 1");
            return 1;
        }
    }
    
    /// <summary>
    /// Calculate maximum safe level where monsters become gray
    /// Iterates through all levels in each monster's range and uses the maximum gray threshold
    /// (when the highest level in range becomes gray, meaning all levels are gray)
    /// </summary>
    private static int CalculateMaxSafeLevel(MonsterData[] monsters)
    {
        if (monsters == null || monsters.Length == 0)
        {
            return 100;
        }
        
        int maxSafeLevel = 100;
        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterData monster = monsters[i];
            if (monster == null) continue;
            
            var (minLevel, maxLevel) = GetMonsterLevelRange(monster);
            
            // Find the maximum gray threshold across all levels in this monster's range
            // This ensures we stop when ALL levels in the range become gray
            int monsterMaxGrayThreshold = 0;
            for (int level = minLevel; level <= maxLevel; level++)
            {
                int grayThresholdLevel = MonsterStatCalculator.GetGrayLevelThreshold(level);
                if (grayThresholdLevel > monsterMaxGrayThreshold)
                {
                    monsterMaxGrayThreshold = grayThresholdLevel;
                }
            }
            
            // Use the most restrictive (lowest) maxSafeLevel across all monsters
            if (i == 0 || monsterMaxGrayThreshold < maxSafeLevel)
            {
                maxSafeLevel = monsterMaxGrayThreshold;
            }
        }
        return maxSafeLevel;
    }
    
    /// <summary>
    /// Get validated level range for a monster
    /// </summary>
    private static (int minLevel, int maxLevel) GetMonsterLevelRange(MonsterData monster)
    {
        if (monster == null)
        {
            return (1, 1);
        }
        
        int minLevel = Mathf.Max(1, monster.minLevel);
        int maxLevel = Mathf.Max(minLevel, monster.maxLevel);
        return (minLevel, maxLevel);
    }
    
    /// <summary>
    /// Distribute kills across all monsters and their level ranges
    /// Returns a dictionary mapping (monster, level) combinations to kill counts
    /// </summary>
    private static Dictionary<MonsterLevelKey, int> DistributeKillsAcrossLevelRanges(MonsterData[] monsters, int totalKills)
    {
        Dictionary<MonsterLevelKey, int> killsPerMonsterLevel = new Dictionary<MonsterLevelKey, int>();
        
        if (monsters == null || monsters.Length == 0 || totalKills <= 0)
        {
            return killsPerMonsterLevel;
        }
        
        int killsPerMonsterType = totalKills / monsters.Length;
        int remainderKills = totalKills % monsters.Length;
        
        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterData monster = monsters[i];
            if (monster == null) continue;
            
            int totalKillsForMonster = killsPerMonsterType + (i < remainderKills ? 1 : 0);
            if (totalKillsForMonster <= 0) continue;
            
            var (minLevel, maxLevel) = GetMonsterLevelRange(monster);
            int levelRange = maxLevel - minLevel + 1;
            
            int killsPerLevel = totalKillsForMonster / levelRange;
            int remainderKillsForMonster = totalKillsForMonster % levelRange;
            
            for (int level = minLevel; level <= maxLevel; level++)
            {
                int killsForThisLevel = killsPerLevel + (level - minLevel < remainderKillsForMonster ? 1 : 0);
                if (killsForThisLevel > 0)
                {
                    MonsterLevelKey key = new MonsterLevelKey(monster, level);
                    killsPerMonsterLevel[key] = killsForThisLevel;
                }
            }
        }
        
        return killsPerMonsterLevel;
    }
    
    /// <summary>
    /// Helper struct to track kills per monster level combination
    /// </summary>
    private struct MonsterLevelKey
    {
        public MonsterData monster;
        public int level;
        
        public MonsterLevelKey(MonsterData monster, int level)
        {
            this.monster = monster;
            this.level = level;
        }
        
        public override bool Equals(object obj)
        {
            if (obj is MonsterLevelKey other)
            {
                return monster == other.monster && level == other.level;
            }
            return false;
        }
        
        public override int GetHashCode()
        {
            return monster.GetHashCode() ^ level.GetHashCode();
        }
    }
    
    /// <summary>
    /// Process level-ups if player has enough XP
    /// </summary>
    private static void ProcessLevelUps(ref int currentLevel, ref int currentXP, int maxSafeLevel)
    {
        while (currentLevel < maxSafeLevel - 1)
        {
            int xpNeededForNextLevel = CombatLogic.GetXPRequiredForLevel(currentLevel);
            if (currentXP < xpNeededForNextLevel)
            {
                break;
            }
            
            currentXP -= xpNeededForNextLevel;
            currentLevel++;
        }
    }
    
    /// <summary>
    /// Calculate XP per kill for each monster/level combination at the current player level
    /// Returns dictionary of XP per kill and total weighted XP stats
    /// </summary>
    private static (Dictionary<MonsterLevelKey, float> xpPerKill, float totalWeightedXp, int totalNonGrayKills) 
        CalculateXPPerKillForMonsters(Dictionary<MonsterLevelKey, int> remainingKillsPerMonsterLevel, int currentLevel, CombatStats stats)
    {
        Dictionary<MonsterLevelKey, float> xpPerKillAtLevel = new Dictionary<MonsterLevelKey, float>();
        float totalWeightedXp = 0f;
        int totalRemainingNonGrayKills = 0;
        
        foreach (var kvp in remainingKillsPerMonsterLevel)
        {
            MonsterLevelKey key = kvp.Key;
            int remainingKills = kvp.Value;
            if (remainingKills <= 0) continue;
            
            CalculatedMonsterRewards monsterRewards = MonsterStatCalculator.CalculateRewards(key.monster, key.level, currentLevel);
            float xpPerKill = monsterRewards.xpReward * (1f + stats.xpBonus);
            xpPerKillAtLevel[key] = xpPerKill;
            
            if (xpPerKill <= 0) continue;
            
            totalWeightedXp += xpPerKill * remainingKills;
            totalRemainingNonGrayKills += remainingKills;
        }
        
        return (xpPerKillAtLevel, totalWeightedXp, totalRemainingNonGrayKills);
    }
    
    /// <summary>
    /// Process kills and calculate XP gained for the current level
    /// </summary>
    private static int ProcessKillsAndCalculateXP(
        Dictionary<MonsterLevelKey, int> remainingKillsPerMonsterLevel,
        Dictionary<MonsterLevelKey, float> xpPerKillAtLevel,
        int totalRemainingNonGrayKills,
        int killsAtThisLevel)
    {
        int xpGainedThisLevel = 0;
        int killsProcessed = 0;
        
        List<MonsterLevelKey> keysToProcess = new List<MonsterLevelKey>(remainingKillsPerMonsterLevel.Keys);
        
        foreach (MonsterLevelKey key in keysToProcess)
        {
            int remainingKills = remainingKillsPerMonsterLevel[key];
            if (remainingKills <= 0) continue;
            
            float xpPerKill = xpPerKillAtLevel[key];
            if (xpPerKill <= 0) continue;
            
            int killsThisIteration = Mathf.RoundToInt((float)remainingKills / totalRemainingNonGrayKills * killsAtThisLevel);
            killsThisIteration = Mathf.Min(killsThisIteration, remainingKills);
            killsThisIteration = Mathf.Min(killsThisIteration, killsAtThisLevel - killsProcessed);
            
            if (killsThisIteration > 0)
            {
                xpGainedThisLevel += Mathf.RoundToInt(xpPerKill * killsThisIteration);
                remainingKillsPerMonsterLevel[key] -= killsThisIteration;
                killsProcessed += killsThisIteration;
            }
        }
        
        return xpGainedThisLevel;
    }
    
    /// <summary>
    /// Calculate progressive XP that reduces as player levels up
    /// Distributes kills equally across all levels in each monster's range
    /// </summary>
    private static int CalculateProgressiveXP(MonsterData[] monsters, int monstersKilled, int playerLevel, int maxSafeLevel, CombatStats stats)
    {
        if (monsters == null || monsters.Length == 0 || monstersKilled <= 0)
        {
            return 0;
        }
        
        int totalXP = 0;
        int currentLevel = playerLevel;
        int currentXP = Services.TryGet<ICharacterService>(out var characterService) 
            ? characterService.GetCurrentXP() 
            : 0;
        
        Dictionary<MonsterLevelKey, int> remainingKillsPerMonsterLevel = DistributeKillsAcrossLevelRanges(monsters, monstersKilled);
        
        while (currentLevel < maxSafeLevel)
        {
            int totalRemainingKills = 0;
            foreach (int kills in remainingKillsPerMonsterLevel.Values)
            {
                totalRemainingKills += kills;
            }
            if (totalRemainingKills <= 0) break;
            
            ProcessLevelUps(ref currentLevel, ref currentXP, maxSafeLevel);
            
            if (currentLevel >= maxSafeLevel) break;
            
            int xpNeededForNextLevel = CombatLogic.GetXPRequiredForLevel(currentLevel);
            int xpToFillCurrentLevel = xpNeededForNextLevel - currentXP;
            
            if (xpToFillCurrentLevel <= 0) break;
            
            var (xpPerKillAtLevel, totalWeightedXp, totalRemainingNonGrayKills) = 
                CalculateXPPerKillForMonsters(remainingKillsPerMonsterLevel, currentLevel, stats);
            
            if (totalRemainingNonGrayKills == 0 || totalWeightedXp <= 0) break;
            
            float avgXpPerKill = totalWeightedXp / totalRemainingNonGrayKills;
            if (avgXpPerKill <= 0) break;
            
            int killsAtThisLevel = Mathf.Min(totalRemainingKills, Mathf.Max(1, Mathf.CeilToInt(xpToFillCurrentLevel / avgXpPerKill)));
            
            int xpGainedThisLevel = ProcessKillsAndCalculateXP(
                remainingKillsPerMonsterLevel,
                xpPerKillAtLevel,
                totalRemainingNonGrayKills,
                killsAtThisLevel);
            
            if (xpGainedThisLevel == 0) break;
            
            totalXP += xpGainedThisLevel;
            currentXP += xpGainedThisLevel;
        }

        return totalXP;
    }
    
    /// <summary>
    /// Calculate item drops for a given number of kills
    /// </summary>
    private static void CalculateItemDrops(MonsterData monster, int kills, Dictionary<string, int> totalItems)
    {
        if (monster.dropTable == null || monster.dropTable.Count == 0)
        {
            return;
        }
        
        foreach (MonsterDropEntry dropEntry in monster.dropTable)
        {
            if (dropEntry.item == null) continue;
            
            float expectedDrops = dropEntry.dropChance * kills;
            int actualDrops = Mathf.FloorToInt(expectedDrops);
            
            if (UnityEngine.Random.value < (expectedDrops - actualDrops))
            {
                actualDrops++;
            }
            
            if (actualDrops > 0)
            {
                int quantity = actualDrops * dropEntry.quantity;
                string itemName = dropEntry.item.itemName;
                
                if (totalItems.ContainsKey(itemName))
                {
                    totalItems[itemName] += quantity;
                }
                else
                {
                    totalItems[itemName] = quantity;
                }
            }
        }
    }
    
    /// <summary>
    /// Calculate gold and items from monster kills
    /// Distributes kills equally across all levels in each monster's range
    /// </summary>
    private static void CalculateGoldAndItems(MonsterData[] monsters, int monstersKilled, int playerLevel, CombatStats stats, 
        out int totalGold, out Dictionary<string, int> totalItems)
    {
        totalGold = 0;
        totalItems = new Dictionary<string, int>();
        
        if (monsters == null || monsters.Length == 0 || monstersKilled <= 0)
        {
            return;
        }
        
        Dictionary<MonsterLevelKey, int> killsPerMonsterLevel = DistributeKillsAcrossLevelRanges(monsters, monstersKilled);
        
        foreach (var kvp in killsPerMonsterLevel)
        {
            MonsterLevelKey key = kvp.Key;
            int killsForThisLevel = kvp.Value;
            if (killsForThisLevel <= 0) continue;
            
            CalculatedMonsterRewards monsterRewards = MonsterStatCalculator.CalculateRewards(key.monster, key.level, playerLevel);
            
            totalGold += Mathf.RoundToInt(monsterRewards.goldReward * (1f + stats.goldBonus)) * killsForThisLevel;
            
            CalculateItemDrops(key.monster, killsForThisLevel, totalItems);
        }
    }

}

