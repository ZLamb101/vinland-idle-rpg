using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds crafting-related data (Cooking, Alchemy, Blacksmithing, etc.).
/// Tracks unlocked recipes across all professions.
/// ACCOUNT-WIDE: Shared across all characters (stored in AccountSaveData).
/// </summary>
[System.Serializable]
public class CraftingData
{
    [Tooltip("List of recipe names (asset names) that have been unlocked account-wide")]
    public List<string> unlockedRecipeNames = new List<string>();
    
    public CraftingData()
    {
        // Recipes are learned via consumable items, not unlocked by default
        unlockedRecipeNames = new List<string>();
    }
    
    /// <summary>
    /// Check if a recipe is unlocked
    /// </summary>
    public bool IsRecipeUnlocked(string recipeName)
    {
        return unlockedRecipeNames.Contains(recipeName);
    }
    
    /// <summary>
    /// Unlock a recipe by name
    /// </summary>
    public void UnlockRecipe(string recipeName)
    {
        if (!unlockedRecipeNames.Contains(recipeName))
        {
            unlockedRecipeNames.Add(recipeName);
        }
    }
    
    /// <summary>
    /// Get all unlocked recipe names
    /// </summary>
    public List<string> GetUnlockedRecipes()
    {
        return new List<string>(unlockedRecipeNames);
    }
}
