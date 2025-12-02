using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Generic manager for all crafting professions (Cooking, Alchemy, Blacksmithing, etc.).
/// Handles recipe unlocking, crafting validation, and crafting execution.
/// Registered in GameBootstrap.cs
/// </summary>
public class CraftingManager : MonoBehaviour, ICraftingService
{
    [Header("Recipe Database")]
    [Tooltip("All recipes in the game (assign in Inspector or load from Resources)")]
    public RecipeData[] allRecipes;
    
    private AccountSaveData accountData;
    private IProfessionService professionService;
    private ICharacterService characterService;
    
    // Crafting state
    private bool isCrafting = false;
    private RecipeData currentRecipe = null;
    private int currentCraftIndex = 0; // Current item being crafted (1-based)
    private int totalCraftQuantity = 0; // Total items to craft
    private float craftingTimer = 0f;
    private float craftingDuration = 0f;
    
    void Awake()
    {
        if (Services.IsRegistered<ICraftingService>())
        {
            Debug.LogWarning("[CraftingManager] Service already registered. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        
        // Load all recipes from Resources if not assigned
        if (allRecipes == null || allRecipes.Length == 0)
        {
            allRecipes = Resources.LoadAll<RecipeData>("Recipes");
            Debug.Log($"[CraftingManager] Loaded {allRecipes.Length} recipes from Resources/Recipes");
        }
        
        // Register with service locator
        Services.Register<ICraftingService>(this);
        
        // Load account data (contains crafting recipes)
        LoadAccountData();
        
        // Auto-unlock all default recipes
        AutoUnlockDefaultRecipes();
        
        Debug.Log("[CraftingManager] Service registered and ready");
    }
    
    void Start()
    {
        // Get service references
        professionService = Services.Get<IProfessionService>();
        characterService = Services.Get<ICharacterService>();
    }
    
    void Update()
    {
        if (isCrafting)
        {
            UpdateCrafting();
        }
    }
    
    void OnDestroy()
    {
        SaveAccountData();
        Services.Unregister<ICraftingService>();
    }
    
    void OnApplicationQuit()
    {
        SaveAccountData();
    }
    
    private void LoadAccountData()
    {
        accountData = SaveSystem.LoadAccount();
        if (accountData == null)
        {
            accountData = AccountSaveData.CreateDefault();
        }
        
        Debug.Log($"[CraftingManager] Loaded account data with {accountData.craftingData.unlockedRecipeNames.Count} unlocked recipes");
    }
    
    /// <summary>
    /// Auto-unlock all recipes marked as isDefaultUnlocked = true
    /// </summary>
    private void AutoUnlockDefaultRecipes()
    {
        if (allRecipes == null || accountData?.craftingData == null) return;
        
        int unlockedCount = 0;
        foreach (RecipeData recipe in allRecipes)
        {
            if (recipe != null && recipe.isDefaultUnlocked && !IsRecipeUnlocked(recipe))
            {
                // Directly add to unlocked list without publishing events
                accountData.craftingData.UnlockRecipe(recipe.name);
                unlockedCount++;
            }
        }
        
        if (unlockedCount > 0)
        {
            SaveAccountData();
            Debug.Log($"[CraftingManager] Auto-unlocked {unlockedCount} default recipes");
        }
    }
    
    private void SaveAccountData()
    {
        if (accountData != null)
        {
            SaveSystem.SaveAccount(accountData);
            Debug.Log("[CraftingManager] Saved account data (crafting recipes)");
        }
    }
    
    // ==================== Recipe Management ====================
    
    public bool IsRecipeUnlocked(RecipeData recipe)
    {
        if (recipe == null || accountData?.craftingData == null) return false;
        return accountData.craftingData.IsRecipeUnlocked(recipe.name);
    }
    
    public void UnlockRecipe(RecipeData recipe)
    {
        if (recipe == null || accountData?.craftingData == null) return;
        
        if (!IsRecipeUnlocked(recipe))
        {
            accountData.craftingData.UnlockRecipe(recipe.name);
            
            // Publish event
            EventBus.Publish(new RecipeUnlockedEvent
            {
                recipe = recipe
            });
            
            // Auto-save when recipe is unlocked
            SaveAccountData();
            
            Debug.Log($"[CraftingManager] Recipe unlocked: {recipe.recipeName}");
        }
    }
    
    public RecipeData[] GetUnlockedRecipes()
    {
        if (accountData?.craftingData == null || allRecipes == null) return new RecipeData[0];
        
        List<RecipeData> unlocked = new List<RecipeData>();
        
        foreach (RecipeData recipe in allRecipes)
        {
            if (recipe != null && IsRecipeUnlocked(recipe))
            {
                unlocked.Add(recipe);
            }
        }
        
        return unlocked.ToArray();
    }
    
    public RecipeData[] GetRecipesForProfession(ProfessionType profession)
    {
        if (allRecipes == null) return new RecipeData[0];
        
        // Filter to only recipes for the specified profession
        return allRecipes.Where(r => r != null && r.profession == profession).ToArray();
    }
    
    // ==================== Crafting Validation ====================
    
    public bool CanCraftRecipe(RecipeData recipe, int quantity = 1)
    {
        if (recipe == null) return false;
        if (isCrafting) return false; // Already crafting
        if (!IsRecipeUnlocked(recipe)) return false;
        
        // Check profession level
        if (professionService != null)
        {
            int professionLevel = professionService.GetProfessionLevel(recipe.profession);
            if (professionLevel < recipe.levelRequired) return false;
        }
        
        // Check materials
        if (!HasRequiredMaterials(recipe, quantity)) return false;
        
        // Check inventory space
        if (!HasInventorySpace(quantity)) return false;
        
        return true;
    }
    
    public bool HasRequiredMaterials(RecipeData recipe, int quantity = 1)
    {
        if (recipe == null) return false;
        if (characterService == null) return false;
        
        InventoryData inventory = characterService.GetInventoryData();
        return recipe.HasRequiredMaterials(inventory, quantity);
    }
    
    public bool HasInventorySpace(int slotsNeeded = 1)
    {
        if (characterService == null) return false;
        
        InventoryData inventory = characterService.GetInventoryData();
        if (inventory == null) return false;
        
        return inventory.HasSpace();
    }
    
    public int GetMaxCraftableAmount(RecipeData recipe)
    {
        if (recipe == null) return 0;
        if (characterService == null) return 0;
        
        InventoryData inventory = characterService.GetInventoryData();
        return recipe.GetMaxCraftableAmount(inventory);
    }
    
    // ==================== Crafting Execution ====================
    
    public void StartCrafting(RecipeData recipe, int quantity)
    {
        if (!CanCraftRecipe(recipe, quantity))
        {
            Debug.LogWarning($"[CraftingManager] Cannot craft {recipe.recipeName} x{quantity}");
            return;
        }
        
        // Start crafting
        isCrafting = true;
        currentRecipe = recipe;
        currentCraftIndex = 1;
        totalCraftQuantity = quantity;
        craftingTimer = 0f;
        craftingDuration = recipe.craftTime;
        
        // Publish event
        EventBus.Publish(new CraftingStartedEvent
        {
            recipe = recipe,
            quantity = quantity,
            currentIndex = currentCraftIndex
        });
        
        Debug.Log($"[CraftingManager] Started crafting {recipe.recipeName} x{quantity}");
    }
    
    public void CancelCrafting()
    {
        if (!isCrafting) return;
        
        int itemsCrafted = currentCraftIndex - 1;
        int itemsRemaining = totalCraftQuantity - itemsCrafted;
        
        // Publish event
        EventBus.Publish(new CraftingCancelledEvent
        {
            recipe = currentRecipe,
            itemsCrafted = itemsCrafted,
            itemsRemaining = itemsRemaining
        });
        
        Debug.Log($"[CraftingManager] Cancelled crafting. Crafted: {itemsCrafted}, Remaining: {itemsRemaining}");
        
        // Reset state
        ResetCraftingState();
    }
    
    public bool IsCrafting()
    {
        return isCrafting;
    }
    
    public float GetCraftingProgress()
    {
        if (!isCrafting || craftingDuration <= 0) return 0f;
        return Mathf.Clamp01(craftingTimer / craftingDuration);
    }
    
    public RecipeData GetCurrentRecipe()
    {
        return currentRecipe;
    }
    
    public int GetCurrentCraftIndex()
    {
        return currentCraftIndex;
    }
    
    public int GetTotalCraftQuantity()
    {
        return totalCraftQuantity;
    }
    
    // ==================== Private Methods ====================
    
    private void UpdateCrafting()
    {
        if (!isCrafting || currentRecipe == null) return;
        
        craftingTimer += Time.deltaTime;
        
        // Publish progress event
        EventBus.Publish(new CraftingProgressEvent
        {
            progress = GetCraftingProgress(),
            recipe = currentRecipe,
            currentIndex = currentCraftIndex,
            totalQuantity = totalCraftQuantity
        });
        
        // Check if current craft is complete
        if (craftingTimer >= craftingDuration)
        {
            CompleteCraft();
        }
    }
    
    private void CompleteCraft()
    {
        if (currentRecipe == null || characterService == null) return;
        
        // Consume materials
        if (!ConsumeMaterials(currentRecipe, 1))
        {
            Debug.LogError("[CraftingManager] Failed to consume materials!");
            CancelCrafting();
            return;
        }
        
        // Add output item to inventory using CharacterService (this publishes ItemAddedEvent)
        InventoryItem craftedItem = currentRecipe.outputItem.CreateInventoryItem(currentRecipe.outputQuantity);
        
        var result = characterService.AddItemToInventoryDetailed(craftedItem);
        if (!result.success)
        {
            Debug.LogWarning("[CraftingManager] Inventory full! Crafting cancelled.");
            CancelCrafting();
            
            // Publish inventory full event
            EventBus.Publish(new InventoryFullEvent
            {
                attemptedItem = craftedItem,
                itemsLost = result.itemsRemaining
            });
            return;
        }
        
        // Award XP
        if (professionService != null)
        {
            professionService.AddProfessionXP(currentRecipe.profession, currentRecipe.experienceGained);
        }
        
        Debug.Log($"[CraftingManager] Crafted {currentRecipe.recipeName} ({currentCraftIndex}/{totalCraftQuantity})");
        
        // Check if more items to craft
        if (currentCraftIndex < totalCraftQuantity)
        {
            // Publish completion event for this item
            EventBus.Publish(new CraftingCompletedEvent
            {
                recipe = currentRecipe,
                craftedItem = currentRecipe.outputItem,
                quantity = currentRecipe.outputQuantity,
                xpGained = currentRecipe.experienceGained
            });
            
            // Check if we still have materials and space (don't use CanCraftRecipe as it checks isCrafting)
            bool hasMaterials = HasRequiredMaterials(currentRecipe, 1);
            bool hasSpace = HasInventorySpace(1);
            
            if (hasMaterials && hasSpace)
            {
                currentCraftIndex++;
                craftingTimer = 0f;
                
                // Publish started event for next item
                EventBus.Publish(new CraftingStartedEvent
                {
                    recipe = currentRecipe,
                    quantity = totalCraftQuantity,
                    currentIndex = currentCraftIndex
                });
            }
            else
            {
                Debug.LogWarning("[CraftingManager] Cannot continue crafting (out of materials or inventory full)");
                CancelCrafting();
            }
        }
        else
        {
            // All items crafted - reset state BEFORE publishing event
            Debug.Log($"[CraftingManager] Completed crafting {totalCraftQuantity}x {currentRecipe.recipeName}");
            
            // Store recipe reference before resetting state
            RecipeData completedRecipe = currentRecipe;
            ItemData completedItem = currentRecipe.outputItem;
            int completedQuantity = currentRecipe.outputQuantity;
            int completedXP = currentRecipe.experienceGained;
            
            ResetCraftingState();
            
            // Publish completion event for the last item
            EventBus.Publish(new CraftingCompletedEvent
            {
                recipe = completedRecipe,
                craftedItem = completedItem,
                quantity = completedQuantity,
                xpGained = completedXP
            });
        }
    }
    
    private bool ConsumeMaterials(RecipeData recipe, int quantity)
    {
        if (recipe == null || characterService == null) return false;
        
        InventoryData inventory = characterService.GetInventoryData();
        if (inventory == null) return false;
        
        // Consume each material
        foreach (RecipeMaterial material in recipe.materials)
        {
            if (material.item == null) continue;
            
            int amountToConsume = material.quantity * quantity;
            int consumed = 0;
            
            // Find and remove materials from inventory
            for (int i = 0; i < inventory.items.Length && consumed < amountToConsume; i++)
            {
                InventoryItem invItem = inventory.items[i];
                if (invItem != null && !invItem.IsEmpty() && invItem.itemName == material.item.itemName)
                {
                    int toRemove = Mathf.Min(invItem.quantity, amountToConsume - consumed);
                    inventory.RemoveItem(i, toRemove);
                    consumed += toRemove;
                }
            }
            
            if (consumed < amountToConsume)
            {
                Debug.LogError($"[CraftingManager] Failed to consume {material.item.itemName} x{amountToConsume}");
                return false;
            }
        }
        
        return true;
    }
    
    private void ResetCraftingState()
    {
        isCrafting = false;
        currentRecipe = null;
        currentCraftIndex = 0;
        totalCraftQuantity = 0;
        craftingTimer = 0f;
        craftingDuration = 0f;
    }
    
    // ==================== Save/Load (Account-Wide) ====================
    
    public CraftingData GetCraftingData()
    {
        return accountData?.craftingData;
    }
    
    public void LoadCraftingData(CraftingData data)
    {
        // This method is kept for interface compatibility but is now handled by LoadAccountData()
        // Crafting data is loaded automatically from AccountSaveData in Awake()
        if (accountData != null && data != null)
        {
            accountData.craftingData = data;
            Debug.Log($"[CraftingManager] Loaded crafting data with {data.unlockedRecipeNames.Count} unlocked recipes");
        }
    }
}

