/// <summary>
/// Interface for crafting services (Cooking, Alchemy, Blacksmithing, etc.)
/// </summary>
public interface ICraftingService
{
    // Recipe management
    /// <summary>
    /// Check if a specific recipe is unlocked for the current character
    /// </summary>
    bool IsRecipeUnlocked(RecipeData recipe);
    
    /// <summary>
    /// Unlock a recipe for the current character
    /// </summary>
    void UnlockRecipe(RecipeData recipe);
    
    /// <summary>
    /// Get all recipes that have been unlocked
    /// </summary>
    RecipeData[] GetUnlockedRecipes();
    
    /// <summary>
    /// Get all recipes for a specific profession (both locked and unlocked)
    /// </summary>
    RecipeData[] GetRecipesForProfession(ProfessionType profession);
    
    // Crafting validation
    /// <summary>
    /// Check if the player can craft a recipe (has materials, level, inventory space)
    /// </summary>
    bool CanCraftRecipe(RecipeData recipe, int quantity = 1);
    
    /// <summary>
    /// Check if the player has the required materials for a recipe
    /// </summary>
    bool HasRequiredMaterials(RecipeData recipe, int quantity = 1);
    
    /// <summary>
    /// Check if the player has inventory space for crafted items
    /// </summary>
    bool HasInventorySpace(int slotsNeeded = 1);
    
    /// <summary>
    /// Get the maximum number of times a recipe can be crafted with current materials
    /// </summary>
    int GetMaxCraftableAmount(RecipeData recipe);
    
    // Crafting execution
    /// <summary>
    /// Start crafting a recipe for a specified quantity
    /// </summary>
    void StartCrafting(RecipeData recipe, int quantity);
    
    /// <summary>
    /// Cancel the current crafting operation
    /// </summary>
    void CancelCrafting();
    
    /// <summary>
    /// Check if currently crafting
    /// </summary>
    bool IsCrafting();
    
    /// <summary>
    /// Get the current crafting progress (0 to 1)
    /// </summary>
    float GetCraftingProgress();
    
    /// <summary>
    /// Get the recipe currently being crafted
    /// </summary>
    RecipeData GetCurrentRecipe();
    
    /// <summary>
    /// Get the current item index being crafted (1-based)
    /// </summary>
    int GetCurrentCraftIndex();
    
    /// <summary>
    /// Get the total quantity being crafted
    /// </summary>
    int GetTotalCraftQuantity();
    
    // Save/Load
    /// <summary>
    /// Get crafting data for saving
    /// </summary>
    CraftingData GetCraftingData();
    
    /// <summary>
    /// Load crafting data from save system
    /// </summary>
    void LoadCraftingData(CraftingData data);
}
