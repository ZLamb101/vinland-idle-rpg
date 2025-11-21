using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds all character stats for the idle/incremental game
/// </summary>
[System.Serializable]
public class CharacterData
{
    public string characterName = ""; // Empty by default - will be set when character is loaded
    public int level = 1;
    public int currentXP = 0;
    public int gold = 0;
    public float currentHealth = 50f;
    public InventoryData inventory = new InventoryData();
    public ProfessionData professions = new ProfessionData();
    
    public List<PlayerQuest> activeQuests = new List<PlayerQuest>();
    public List<string> completedQuestIDs = new List<string>();

    // Character creation data (persists through gameplay)
    public string race = "Human";
    public string characterClass = "Warrior";
    
    // Health: 50 at level 1, +10% per level
    public float GetMaxHealth()
    {
        // Starting health + (health per level * (level - 1))
        // Uses additive scaling from config
        return GameBalance.Combat.playerStartingHealth + (GameBalance.Progression.healthPerLevel * (level - 1));
    }
    
    /// <summary>
    /// Get max health at a specific level
    /// </summary>
    public float GetMaxHealthAtLevel(int targetLevel)
    {
        return GameBalance.Combat.playerStartingHealth + (GameBalance.Progression.healthPerLevel * (targetLevel - 1));
    }
    
    /// <summary>
    /// Get base attack damage at a specific level
    /// Uses additive scaling from config
    /// </summary>
    public float GetBaseAttackAtLevel(int targetLevel)
    {
        // Starting damage + (damage per level * (level - 1))
        return GameBalance.Combat.playerBaseAttackDamage + (GameBalance.Progression.attackDamagePerLevel * (targetLevel - 1));
    }
    
    /// <summary>
    /// Get base crit chance at a specific level
    /// Crit Chance: 0% at level 1, +0.5% per level (starts at level 2)
    /// </summary>
    public float GetBaseCritChanceAtLevel(int targetLevel)
    {
        if (targetLevel <= 1) return 0f;
        // Uses additive scaling from config (e.g., 1% per level)
        return (targetLevel - 1) * GameBalance.Progression.critChancePerLevel;
    }
    
    // XP required for next level (can be calculated dynamically)
    public int GetXPRequiredForNextLevel()
    {
        // Common formula for incremental games: baseXP * level^exponent
        return Mathf.FloorToInt(GameBalance.Progression.baseXPPerLevel * Mathf.Pow(level, GameBalance.Progression.xpScalingPerLevel));
    }
    
    // Check if character should level up
    public bool CanLevelUp()
    {
        return currentXP >= GetXPRequiredForNextLevel();
    }
    
    // Perform level up and return remaining XP
    public void LevelUp()
    {
        if (CanLevelUp())
        {
            currentXP -= GetXPRequiredForNextLevel();
            level++;
        }
    }
}
