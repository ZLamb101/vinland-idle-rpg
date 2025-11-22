using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds cooking-specific data for a character.
/// Tracks unlocked recipes.
/// </summary>
[System.Serializable]
public class CraftingData
{
    [Tooltip("List of recipe names that have been unlocked by this character")]
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
