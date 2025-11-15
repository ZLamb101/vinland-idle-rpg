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
    public string activityName; // e.g., "Mining Iron Ore" or "Fighting Goblins"
    
    // Mining rewards
    public Dictionary<string, int> itemsGathered = new Dictionary<string, int>(); // Item name -> quantity
    
    // Fighting rewards
    public int xpEarned = 0;
    public int goldEarned = 0;
    public Dictionary<string, int> itemsDropped = new Dictionary<string, int>(); // Item name -> quantity
    public int monstersKilled = 0;
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
        
        // Don't calculate rewards if time away is negative or zero
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
    /// Calculate rewards for "doing Nothing" (currently empty, but extensible)
    /// </summary>
    private static void CalculateNoneRewards(AwayRewards rewards, TimeSpan timeAway)
    {
        rewards.activityName = "doing Nothing";
        
        // Currently no rewards for doing nothing, but this can be extended later
        // For example: passive gold generation, small XP gain, etc.
    }
    
    /// <summary>
    /// Calculate mining rewards for time away
    /// </summary>
    private static void CalculateMiningRewards(AwayRewards rewards, ResourceData resource, TimeSpan timeAway)
    {
        rewards.activityName = $"Mining {resource.resourceName}";
        
        if (resource.gatheredItem == null)
        {
            return;
        }
        
        // Calculate how many gather cycles completed
        float timePerGather = 1f / resource.gatherRate; // Time in seconds per gather cycle
        float totalSeconds = (float)timeAway.TotalSeconds;
        int gatherCycles = Mathf.FloorToInt(totalSeconds / timePerGather);
        
        // Calculate items gathered
        int totalItems = gatherCycles * resource.itemsPerGather;
        
        Debug.Log($"[AwayRewards] Mining calculation - Time away: {totalSeconds}s, Time per gather: {timePerGather}s, Gather cycles: {gatherCycles}, Items per gather: {resource.itemsPerGather}, Total items: {totalItems}");
        
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
        
        // Use the first monster for naming (or combine names if multiple types)
        if (monsters.Length == 1)
        {
            rewards.activityName = $"Fighting {monsters[0].monsterName}";
        }
        else
        {
            rewards.activityName = $"Fighting {monsters.Length} Monster Types";
        }
        
        // Get player combat stats from CombatService
        float playerAttackDamage = 10f; // Base attack damage
        float playerAttackSpeed = 1.5f; // Base attack speed
        
        if (Services.TryGet<ICombatService>(out var combatService))
        {
            // Ensure player stats are up to date
            combatService.CalculatePlayerStats();
            
            // Get actual player stats
            playerAttackDamage = combatService.GetPlayerAttackDamage();
            playerAttackSpeed = combatService.GetPlayerAttackSpeed();
            
            Debug.Log($"[AwayRewards] Player stats from CombatService - Damage: {playerAttackDamage}, Speed: {playerAttackSpeed}");
        }
        else
        {
            Debug.LogWarning("[AwayRewards] CombatService is null, using base stats");
        }
        
        // Calculate time per kill based on:
        // 1. Monster health / player damage = hits needed to kill
        // 2. Hits needed * attack speed = time per kill
        // Use average monster health across all monster types
        float averageMonsterHealth = 0f;
        foreach (MonsterData monster in monsters)
        {
            averageMonsterHealth += monster.health;
        }
        averageMonsterHealth /= monsters.Length;
        
        // Calculate hits needed (accounting for crit chance - assume 10% crit with 2x damage)
        // Average damage = (90% normal + 10% crit) = 0.9 * damage + 0.1 * damage * 2 = damage * 1.1
        float averageDamage = playerAttackDamage * 1.1f; // 10% crit chance, 100% crit damage bonus
        float hitsNeeded = Mathf.Max(1f, Mathf.Ceil(averageMonsterHealth / averageDamage));
        
        // Time per kill = hits needed * attack speed
        float timePerKill = hitsNeeded * playerAttackSpeed;
        float totalSeconds = (float)timeAway.TotalSeconds;
        
        // Calculate monsters killed
        // Since monsters don't fight back, player can kill continuously
        // With mob count, player can kill multiple monsters simultaneously
        // Ensure we always get at least 1 kill if time away is significant
        float killsPerSecond = mobCount / Mathf.Max(0.1f, timePerKill); // Prevent division by zero
        int monstersKilled = Mathf.Max(1, Mathf.FloorToInt(totalSeconds * killsPerSecond));
        
        // If time away is significant (more than 1 minute), ensure at least some kills
        if (totalSeconds >= 60f && monstersKilled == 0)
        {
            // Force at least 1 kill if we've been away for a while
            monstersKilled = 1;
        }
        
        rewards.monstersKilled = monstersKilled;
        
        Debug.Log($"[AwayRewards] Fighting calculation - Time away: {totalSeconds}s ({timeAway.TotalMinutes:F2} min), Average monster health: {averageMonsterHealth}, Player damage: {playerAttackDamage}, Average damage: {averageDamage}, Hits needed: {hitsNeeded}, Time per kill: {timePerKill}s, Kills per second: {killsPerSecond}, Monsters killed: {monstersKilled}, Mob count: {mobCount}");
        
        // Always calculate rewards if we have kills (should always be at least 1)
        if (monstersKilled <= 0)
        {
            Debug.LogWarning($"[AwayRewards] No monsters killed despite {totalSeconds}s away - calculation issue! Time per kill: {timePerKill}s");
            // Force at least 1 kill to ensure rewards are calculated
            monstersKilled = 1;
            rewards.monstersKilled = monstersKilled;
        }
        
        // Calculate rewards per monster type
        int totalXP = 0;
        int totalGold = 0;
        Dictionary<string, int> totalItems = new Dictionary<string, int>();
        
        // Distribute kills across monster types (if multiple types)
        int killsPerMonsterType = monstersKilled / monsters.Length;
        int remainderKills = monstersKilled % monsters.Length;
        
        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterData monster = monsters[i];
            int killsForThisType = killsPerMonsterType + (i < remainderKills ? 1 : 0);
            
            // Calculate XP and gold with bonuses
            CombatStats stats = GetCombatStats();
            int xpPerKill = Mathf.RoundToInt(monster.xpReward * (1f + stats.xpBonus));
            int goldPerKill = Mathf.RoundToInt(monster.goldReward * (1f + stats.goldBonus));
            
            totalXP += xpPerKill * killsForThisType;
            totalGold += goldPerKill * killsForThisType;
            
            // Process drop table
            if (monster.dropTable != null && monster.dropTable.Count > 0)
            {
                foreach (MonsterDropEntry dropEntry in monster.dropTable)
                {
                    if (dropEntry.item != null)
                    {
                        // Calculate expected drops based on drop chance
                        float expectedDrops = dropEntry.dropChance * killsForThisType;
                        int actualDrops = Mathf.FloorToInt(expectedDrops);
                        
                        // Add random chance for fractional part
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
            }
        }
        
        rewards.xpEarned = totalXP;
        rewards.goldEarned = totalGold;
        rewards.itemsDropped = totalItems;
        
        Debug.Log($"[AwayRewards] Final rewards - XP: {totalXP}, Gold: {totalGold}, Items: {totalItems.Count}");
    }
    
    /// <summary>
    /// Get combat stats from equipment and talents (for calculating bonuses)
    /// </summary>
    private static CombatStats GetCombatStats()
    {
        CombatStats stats = new CombatStats
        {
            xpBonus = 0f,
            goldBonus = 0f
        };
        
        // Add equipment bonuses using service pattern
        if (Services.TryGet<IEquipmentService>(out var equipmentService))
        {
            EquipmentStats equipStats = equipmentService.GetTotalStats();
            stats.xpBonus += equipStats.xpBonus;
            stats.goldBonus += equipStats.goldBonus;
        }
        
        // Add talent bonuses using service pattern
        if (Services.TryGet<ITalentService>(out var talentService))
        {
            TalentBonuses talents = talentService.GetTotalBonuses();
            stats.xpBonus += talents.xpBonus;
            stats.goldBonus += talents.goldBonus;
        }
        
        return stats;
    }
    
    /// <summary>
    /// Helper struct for combat stats (matching CombatManager's internal struct)
    /// </summary>
    private struct CombatStats
    {
        public float xpBonus;
        public float goldBonus;
    }
}

