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

