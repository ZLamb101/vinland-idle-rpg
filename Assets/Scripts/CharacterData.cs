using System;
using UnityEngine;

/// <summary>
/// Holds all character stats for the idle/incremental game
/// </summary>
[System.Serializable]
public class CharacterData
{
    public string characterName = "Hero";
    public int level = 1;
    public int currentXP = 0;
    public int gold = 0;
    public float currentHealth = 50f;
    public InventoryData inventory = new InventoryData();
    
    // Health: 50 at level 1, +10% per level
    public float GetMaxHealth()
    {
        // 50 * (1.1^(level-1))
        // Level 1: 50 * 1.0 = 50
        // Level 2: 50 * 1.1 = 55
        // Level 3: 50 * 1.21 = 60.5
        return 50f * Mathf.Pow(1.1f, level - 1);
    }
    
    /// <summary>
    /// Get max health at a specific level
    /// </summary>
    public float GetMaxHealthAtLevel(int targetLevel)
    {
        return 50f * Mathf.Pow(1.1f, targetLevel - 1);
    }
    
    /// <summary>
    /// Get base attack damage at a specific level
    /// Attack: 5 at level 1, +10% per level
    /// </summary>
    public float GetBaseAttackAtLevel(int targetLevel)
    {
        // 5 * (1.1^(level-1))
        // Level 1: 5 * 1.0 = 5
        // Level 2: 5 * 1.1 = 5.5
        // Level 3: 5 * 1.21 = 6.05
        return 5f * Mathf.Pow(1.1f, targetLevel - 1);
    }
    
    /// <summary>
    /// Get base crit chance at a specific level
    /// Crit Chance: 0% at level 1, +0.5% per level (starts at level 2)
    /// </summary>
    public float GetBaseCritChanceAtLevel(int targetLevel)
    {
        if (targetLevel <= 1) return 0f;
        // Level 2: 0.5%, Level 3: 1.0%, Level 4: 1.5%, etc.
        return (targetLevel - 1) * 0.005f; // 0.5% per level after level 1
    }
    
    // XP required for next level (can be calculated dynamically)
    public int GetXPRequiredForNextLevel()
    {
        // Common formula for incremental games: baseXP * level^exponent
        return Mathf.FloorToInt(100 * Mathf.Pow(level, 1.5f));
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

