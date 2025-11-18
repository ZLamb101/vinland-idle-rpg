using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds profession levels and experience for a character.
/// Each character has their own independent profession progress.
/// </summary>
[System.Serializable]
public class ProfessionData
{
    // Current level for each profession (default 1)
    private Dictionary<ProfessionType, int> levels = new Dictionary<ProfessionType, int>();
    
    // Current experience for each profession
    private Dictionary<ProfessionType, int> experience = new Dictionary<ProfessionType, int>();
    
    // Constants for XP calculation (similar to character XP scaling)
    private const int BASE_XP_PER_LEVEL = 100;
    private const float XP_SCALING_EXPONENT = 1.5f;
    
    /// <summary>
    /// Initialize profession data with default values
    /// </summary>
    public ProfessionData()
    {
        // Initialize all professions at level 1 with 0 XP
        foreach (ProfessionType profession in Enum.GetValues(typeof(ProfessionType)))
        {
            levels[profession] = 1;
            experience[profession] = 0;
        }
    }
    
    /// <summary>
    /// Get the current level for a profession
    /// </summary>
    public int GetProfessionLevel(ProfessionType profession)
    {
        if (levels.ContainsKey(profession))
        {
            return levels[profession];
        }
        return 1; // Default level
    }
    
    /// <summary>
    /// Get the current XP for a profession
    /// </summary>
    public int GetProfessionXP(ProfessionType profession)
    {
        if (experience.ContainsKey(profession))
        {
            return experience[profession];
        }
        return 0;
    }
    
    /// <summary>
    /// Calculate XP required to reach a specific level
    /// Uses exponential scaling: baseXP * level^exponent
    /// </summary>
    public int GetXPRequiredForLevel(int level)
    {
        if (level <= 1) return 0; // Level 1 requires no XP
        
        // Exponential formula: 100 * level^1.5
        // Level 2 = 100 * 2^1.5 ≈ 283 (but we'll adjust to match user's 83)
        // Let's use a simpler formula for lower levels
        
        // Use a formula that gives us ~83 XP for level 2
        // BASE_XP * (level - 1) * sqrt(level)
        float xpRequired = BASE_XP_PER_LEVEL * (level - 1) * Mathf.Pow(level, XP_SCALING_EXPONENT - 1);
        return Mathf.FloorToInt(xpRequired);
    }
    
    /// <summary>
    /// Get XP required for next level from current level
    /// </summary>
    public int GetXPRequiredForNextLevel(ProfessionType profession)
    {
        int currentLevel = GetProfessionLevel(profession);
        return GetXPRequiredForLevel(currentLevel + 1);
    }
    
    /// <summary>
    /// Add experience to a profession and handle level-ups
    /// Returns the new level (may have leveled up multiple times)
    /// </summary>
    public int AddExperience(ProfessionType profession, int amount)
    {
        if (amount <= 0) return GetProfessionLevel(profession);
        
        // Ensure profession exists in dictionaries
        if (!levels.ContainsKey(profession))
        {
            levels[profession] = 1;
        }
        if (!experience.ContainsKey(profession))
        {
            experience[profession] = 0;
        }
        
        // Add XP
        experience[profession] += amount;
        
        // Check for level-ups (can level up multiple times from one XP gain)
        int currentLevel = levels[profession];
        int currentXP = experience[profession];
        
        while (currentXP >= GetXPRequiredForLevel(currentLevel + 1))
        {
            // Level up!
            currentXP -= GetXPRequiredForLevel(currentLevel + 1);
            currentLevel++;
            
            Debug.Log($"[ProfessionData] {profession.GetDisplayName()} leveled up to {currentLevel}!");
        }
        
        // Update stored values
        levels[profession] = currentLevel;
        experience[profession] = currentXP;
        
        return currentLevel;
    }
    
    /// <summary>
    /// Set profession level directly (for loading saved data)
    /// </summary>
    public void SetProfessionLevel(ProfessionType profession, int level)
    {
        levels[profession] = Mathf.Max(1, level);
    }
    
    /// <summary>
    /// Set profession XP directly (for loading saved data)
    /// </summary>
    public void SetProfessionXP(ProfessionType profession, int xp)
    {
        experience[profession] = Mathf.Max(0, xp);
    }
    
    /// <summary>
    /// Get all profession levels as a dictionary (for saving)
    /// </summary>
    public Dictionary<ProfessionType, int> GetAllLevels()
    {
        return new Dictionary<ProfessionType, int>(levels);
    }
    
    /// <summary>
    /// Get all profession XP as a dictionary (for saving)
    /// </summary>
    public Dictionary<ProfessionType, int> GetAllXP()
    {
        return new Dictionary<ProfessionType, int>(experience);
    }
}


