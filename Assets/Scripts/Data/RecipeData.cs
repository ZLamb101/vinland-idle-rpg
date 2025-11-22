using UnityEngine;

/// <summary>
/// Material required for a recipe
/// </summary>
[System.Serializable]
public class RecipeMaterial
{
    [Tooltip("The item required for this recipe")]
    public ItemData item;
    
    [Tooltip("Quantity of this item required")]
    public int quantity = 1;
}

/// <summary>
/// ScriptableObject that defines a crafting recipe.
/// Can be used for all professions (Cooking, Alchemy, Blacksmithing, etc.)
/// Create instances via: Right-click in Project → Create → Vinland → Recipe
/// </summary>
[CreateAssetMenu(fileName = "NewRecipe", menuName = "Vinland/Recipe", order = 10)]
public class RecipeData : ScriptableObject
{
    [Header("Recipe Info")]
    [Tooltip("Display name of the recipe")]
    public string recipeName = "New Recipe";
    
    [Tooltip("Icon to display for this recipe")]
    public Sprite recipeIcon;
    
    [TextArea(2, 4)]
    [Tooltip("Description of what this recipe creates")]
    public string description = "A crafted item";
    
    [Header("Profession")]
    [Tooltip("Which profession this recipe belongs to")]
    public ProfessionType profession = ProfessionType.Cooking;
    
    [Tooltip("Minimum profession level required to craft this recipe")]
    public int levelRequired = 1;
    
    [Header("Materials Required")]
    [Tooltip("List of materials needed to craft this recipe")]
    public RecipeMaterial[] materials;
    
    [Header("Output")]
    [Tooltip("The item that is created when crafting this recipe")]
    public ItemData outputItem;
    
    [Tooltip("How many of the output item are created per craft")]
    public int outputQuantity = 1;
    
    [Header("Rewards")]
    [Tooltip("How much profession XP is gained when crafting this recipe")]
    public int experienceGained = 10;
    
    [Tooltip("How long it takes to craft this recipe (in seconds)")]
    public float craftTime = 2.5f;
    
    /// <summary>
    /// Check if the player has all required materials in their inventory
    /// </summary>
    public bool HasRequiredMaterials(InventoryData inventory, int quantity = 1)
    {
        if (materials == null || materials.Length == 0) return true;
        if (inventory == null) return false;
        
        foreach (RecipeMaterial material in materials)
        {
            if (material.item == null) continue;
            
            int requiredAmount = material.quantity * quantity;
            int availableAmount = GetMaterialCount(inventory, material.item);
            
            if (availableAmount < requiredAmount)
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Get the maximum number of times this recipe can be crafted with available materials
    /// </summary>
    public int GetMaxCraftableAmount(InventoryData inventory)
    {
        if (materials == null || materials.Length == 0) return 999;
        if (inventory == null) return 0;
        
        int maxCraftable = int.MaxValue;
        
        foreach (RecipeMaterial material in materials)
        {
            if (material.item == null) continue;
            
            int availableAmount = GetMaterialCount(inventory, material.item);
            int possibleCrafts = availableAmount / material.quantity;
            
            maxCraftable = Mathf.Min(maxCraftable, possibleCrafts);
        }
        
        return maxCraftable == int.MaxValue ? 0 : maxCraftable;
    }
    
    /// <summary>
    /// Count how many of a specific item are in the inventory
    /// </summary>
    private int GetMaterialCount(InventoryData inventory, ItemData item)
    {
        if (inventory.items == null) return 0;
        
        int count = 0;
        foreach (InventoryItem invItem in inventory.items)
        {
            if (invItem != null && !invItem.IsEmpty() && invItem.itemName == item.itemName)
            {
                count += invItem.quantity;
            }
        }
        
        return count;
    }
}
