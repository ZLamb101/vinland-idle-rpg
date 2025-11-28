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
    public float currentMana = 100f;
    public InventoryData inventory = new InventoryData();
    public ProfessionData professions = new ProfessionData();
    
    public List<PlayerQuest> activeQuests = new List<PlayerQuest>();
    public List<string> completedQuestIDs = new List<string>();

    // Character creation data (persists through gameplay)
    public string race = "Human";
    public string characterClass = "Warrior";
    
    // --- Core Character Attributes ---
    // These are the primary stats that derive all other combat stats
    public int strength = 10;      // +2 Attack Power, +1 Armor per point
    public int agility = 10;       // +0.5% Crit, +0.5% Dodge, +1 Attack Power per point
    public int intellect = 10;     // +10 Max Mana per point
    public int stamina = 10;       // +10 Health per point
    public int spirit = 1;        // +1 Health/sec regen per point, +0.5 Mana/sec regen per point
    
    // Health is now derived from Stamina (10 health per point)
    public float GetMaxHealth()
    {
        return stamina * 10f;
    }
    
    /// <summary>
    /// Get max health at a specific level
    /// </summary>
    public float GetMaxHealthAtLevel(int targetLevel)
    {
        // Calculate stamina at target level: base 10 + 1 per level after 1
        int staminaAtLevel = 10 + (targetLevel - 1);
        return staminaAtLevel * 10f;
    }
    
    /// <summary>
    /// Get attack power from attributes
    /// Strength: +2 Attack Power per point
    /// Agility: +1 Attack Power per point
    /// </summary>
    public float GetAttackPower()
    {
        return (strength * 2f) + (agility * 1f);
    }
    
    /// <summary>
    /// Get armor from attributes as damage reduction percentage
    /// Strength: +1 Armor per point
    /// Each point of armor = 1% damage reduction (0.01)
    /// </summary>
    public float GetArmor()
    {
        return strength * 0.01f; // Convert to percentage: 1 STR = 1% = 0.01
    }
    
    /// <summary>
    /// Get crit chance from attributes
    /// Agility: +0.5% Crit Chance per point (0.005 as decimal)
    /// </summary>
    public float GetCritChance()
    {
        return agility * 0.005f;
    }
    
    /// <summary>
    /// Get dodge chance from attributes
    /// Agility: +0.5% Dodge Chance per point (0.005 as decimal)
    /// </summary>
    public float GetDodgeChance()
    {
        return agility * 0.005f;
    }
    
    /// <summary>
    /// Get health regen per second from attributes
    /// Spirit: +1 Health/sec per point
    /// </summary>
    public float GetHealthRegen()
    {
        return spirit * 1f;
    }
    
    /// <summary>
    /// Get max mana from Intellect attribute
    /// Intellect: +10 Max Mana per point
    /// </summary>
    public float GetMaxMana()
    {
        return intellect * 10f;
    }
    
    /// <summary>
    /// Get mana regen per second from Spirit attribute
    /// Spirit: +0.5 Mana/sec per point
    /// </summary>
    public float GetManaRegen()
    {
        return spirit * 0.5f;
    }
    
    /// <summary>
    /// Get base attack damage at a specific level (now based on attributes)
    /// Uses Strength and Agility
    /// </summary>
    public float GetBaseAttackAtLevel(int targetLevel)
    {
        // Calculate attributes at target level
        int strAtLevel = 10 + (targetLevel - 1); // +1 STR per level
        int agiAtLevel = 10; // No agility gain per level currently
        
        // Attack Power = (STR * 2) + (AGI * 1)
        return (strAtLevel * 2f) + (agiAtLevel * 1f);
    }
    
    /// <summary>
    /// Get base crit chance at a specific level (now based on Agility)
    /// </summary>
    public float GetBaseCritChanceAtLevel(int targetLevel)
    {
        // Calculate agility at target level
        int agiAtLevel = 10; // No agility gain per level currently
        
        // Crit Chance = AGI * 0.5%
        return agiAtLevel * 0.005f;
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
    
    // Perform level up and grant attribute increases
    public void LevelUp()
    {
        if (CanLevelUp())
        {
            currentXP -= GetXPRequiredForNextLevel();
            level++;
            
            // Grant stat increases on level up
            // +1 Strength, +1 Stamina, +1 Intellect per level
            strength += 1;
            stamina += 1;
            intellect += 1;
        }
    }
}
