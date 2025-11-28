using System;
using UnityEngine;

/// <summary>
/// Singleton manager for character data in the idle/incremental game.
/// This is the central hub for all character stat modifications.
/// </summary>
public class CharacterManager : MonoBehaviour, ICharacterService
{
    
    private CharacterData characterData = new CharacterData(); // Not serialized - loaded at runtime
    private bool dataHasBeenLoaded = false; // Track if character data has been loaded from save
    
    // Events migrated to EventBus - see GameEvent.cs for event types

    void Awake()
    {
        // Prevent duplicates - GameBootstrap creates all managers
        if (Services.IsRegistered<ICharacterService>())
        {
            Debug.LogWarning("[CharacterManager] Service already registered. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject); // Persist between scenes
        
        Services.Register<ICharacterService>(this);
        
        // Reset loaded flag when singleton is first created or reset
        // This ensures fresh data is loaded when entering a new scene
        dataHasBeenLoaded = false;
        
        // Ensure game runs in background (essential for idle games)
        Application.runInBackground = true;
 
    }
    
    void Start()
    {
        // Only initialize if character data hasn't been loaded yet
        // If data has been loaded (from CharacterLoader), don't overwrite it
        if (!dataHasBeenLoaded)
        {
            // Initialize health to max for new characters
            characterData.currentHealth = characterData.GetMaxHealth();
            
        }
        else
        {
            // Data has been loaded, trigger events to update UI
            EventBus.Publish(new CharacterXPChangedEvent { newXP = characterData.currentXP, xpGained = 0 });
            EventBus.Publish(new CharacterLevelChangedEvent { newLevel = characterData.level });
            EventBus.Publish(new CharacterGoldChangedEvent { newGold = characterData.gold, goldChanged = 0 });
            EventBus.Publish(new CharacterNameChangedEvent { newName = characterData.characterName });
            EventBus.Publish(new CharacterHealthChangedEvent { currentHealth = characterData.currentHealth, maxHealth = characterData.GetMaxHealth(), healthChanged = 0 });
        }
    }
    
    void Update()
    {
        // Apply out-of-combat regeneration
        ApplyOutOfCombatRegen();
    }
    
    void OnDestroy()
    {
        Services.Unregister<ICharacterService>();
    }
    
    /// <summary>
    /// Apply health and mana regeneration when not in combat
    /// </summary>
    void ApplyOutOfCombatRegen()
    {
        // Check if we're in combat
        if (Services.TryGet<ICombatService>(out var combatService))
        {
            var combatState = combatService.GetCombatState();
            if (combatState == CombatManager.CombatState.Fighting)
            {
                // In combat - CombatManager handles regen
                return;
            }
        }
        
        // Out of combat - apply regen
        float deltaTime = Time.deltaTime;
        
        // Health regen
        float maxHealth = characterData.GetMaxHealth();
        if (characterData.currentHealth < maxHealth)
        {
            float healthRegen = characterData.GetHealthRegen();
            
            // Add equipment health regen
            if (Services.TryGet<IEquipmentService>(out var equipmentService))
            {
                healthRegen += equipmentService.GetTotalHealthRegen();
            }
            
            if (healthRegen > 0)
            {
                float healthGained = healthRegen * deltaTime;
                characterData.currentHealth = Mathf.Min(characterData.currentHealth + healthGained, maxHealth);
                
                EventBus.Publish(new CharacterHealthChangedEvent 
                { 
                    currentHealth = characterData.currentHealth, 
                    maxHealth = maxHealth, 
                    healthChanged = healthGained 
                });
            }
        }
        
        // Mana regen
        float maxMana = characterData.GetMaxMana();
        if (characterData.currentMana < maxMana)
        {
            float manaRegen = characterData.GetManaRegen();
            
            if (manaRegen > 0)
            {
                float manaGained = manaRegen * deltaTime;
                characterData.currentMana = Mathf.Min(characterData.currentMana + manaGained, maxMana);
                
                EventBus.Publish(new CharacterManaChangedEvent 
                { 
                    currentMana = characterData.currentMana, 
                    maxMana = maxMana, 
                    manaChanged = manaGained 
                });
            }
        }
    }
    
    void OnApplicationQuit()
    {
        // Save away activity state when game closes
        if (Services.TryGet<IAwayActivityService>(out var awayActivityService))
        {
            awayActivityService.SaveAwayState();
        }
        
        // Auto-save character data
        AutoSave("application quit");
    }
    
    // --- XP Management ---
    public void AddXP(int amount)
    {
        if (amount <= 0) return;
        StartCoroutine(AnimateXPGain(amount));
    }
    
    private System.Collections.IEnumerator AnimateXPGain(int totalAmount)
    {
        int remainingXP = totalAmount;
        
        while (remainingXP > 0)
        {
            int xpNeeded = characterData.GetXPRequiredForNextLevel();
            int currentXP = characterData.currentXP;
            int xpToNextLevel = xpNeeded - currentXP;
            
            if (remainingXP >= xpToNextLevel && xpToNextLevel > 0)
            {
                // Fill to level up
                int xpToAdd = xpToNextLevel;
                characterData.currentXP += xpToAdd;
                remainingXP -= xpToAdd;
                
                EventBus.Publish(new CharacterXPChangedEvent { newXP = characterData.currentXP, xpGained = xpToAdd });
                
                // Wait for bar to fill
                yield return new UnityEngine.WaitForSeconds(0.5f);
                
                // Level up
                int oldLevel = characterData.level;
                characterData.LevelUp();
                int newLevel = characterData.level;
                
                EventBus.Publish(new CharacterLevelChangedEvent { newLevel = newLevel });
                EventBus.Publish(new CharacterLevelUpEvent { oldLevel = oldLevel, newLevel = newLevel });
                EventBus.Publish(new CharacterXPChangedEvent { newXP = characterData.currentXP, xpGained = 0 });
                
                // Update max health on level up and heal to full
                characterData.currentHealth = characterData.GetMaxHealth();
                EventBus.Publish(new CharacterHealthChangedEvent { currentHealth = characterData.currentHealth, maxHealth = characterData.GetMaxHealth(), healthChanged = characterData.currentHealth });
                
                Debug.Log($"[CharacterManager] Level up! {oldLevel} -> {newLevel}");
                
                // Wait a bit before continuing to next level
                yield return new UnityEngine.WaitForSeconds(0.5f);
            }
            else
            {
                // Add remaining XP (doesn't cause level up)
                characterData.currentXP += remainingXP;
                EventBus.Publish(new CharacterXPChangedEvent { newXP = characterData.currentXP, xpGained = remainingXP });
                remainingXP = 0;
            }
        }
        
        // Auto-save after XP gain
        AutoSave("XP gain");
    }
    
    public int GetCurrentXP() => characterData.currentXP;
    public int GetXPRequiredForNextLevel() => characterData.GetXPRequiredForNextLevel();
    
    // --- Gold Management ---
    public void AddGold(int amount)
    {
        characterData.gold += amount;
        EventBus.Publish(new CharacterGoldChangedEvent { newGold = characterData.gold, goldChanged = amount });
    }
    
    public bool SpendGold(int amount)
    {
        if (characterData.gold >= amount)
        {
            characterData.gold -= amount;
            EventBus.Publish(new CharacterGoldChangedEvent { newGold = characterData.gold, goldChanged = -amount });
            return true;
        }
        return false;
    }
    
    public int GetGold() => characterData.gold;
    
    // --- Level Management ---
    public int GetLevel() => characterData.level;
    
    // --- Name Management ---
    public void SetName(string newName)
    {
        characterData.characterName = newName;
        EventBus.Publish(new CharacterNameChangedEvent { newName = characterData.characterName });
    }
    
    public string GetName() => characterData.characterName;
    
    // --- Race/Class Management ---
    public string GetRace() => characterData.race;
    public string GetCharacterClass() => characterData.characterClass;
    
    public void SetRace(string newRace)
    {
        characterData.race = newRace;
    }
    
    public void SetCharacterClass(string newClass)
    {
        characterData.characterClass = newClass;
    }
    
    // --- Talent-Modified Stats ---
    /// <summary>
    /// Get max health with talent bonuses applied
    /// </summary>
    public float GetMaxHealthWithTalents()
    {
        float baseHealth = characterData.GetMaxHealth();
        
        var talentService = Services.Get<ITalentService>();
        if (talentService != null)
        {
            TalentBonuses talents = talentService.GetTotalBonuses();
            baseHealth += talents.maxHealth; // Additive
            baseHealth *= (1f + talents.healthMultiplier); // Percentage
        }
        
        return baseHealth;
    }
    
    // --- Health Management ---
    public void TakeDamage(float amount)
    {
        characterData.currentHealth -= amount;
        
        // Check if dead
        if (characterData.currentHealth <= 0)
        {
            characterData.currentHealth = 0;
            EventBus.Publish(new CharacterHealthChangedEvent { currentHealth = characterData.currentHealth, maxHealth = characterData.GetMaxHealth(), healthChanged = -amount });
            EventBus.Publish(new CharacterDiedEvent { deathReason = "Defeated in combat" });
            
            // Reset to max health
            characterData.currentHealth = characterData.GetMaxHealth();
            EventBus.Publish(new CharacterHealthChangedEvent { currentHealth = characterData.currentHealth, maxHealth = characterData.GetMaxHealth(), healthChanged = characterData.currentHealth });
        }
        else
        {
            EventBus.Publish(new CharacterHealthChangedEvent { currentHealth = characterData.currentHealth, maxHealth = characterData.GetMaxHealth(), healthChanged = -amount });
        }
    }
    
    public void Heal(float amount)
    {
        characterData.currentHealth = Mathf.Min(characterData.currentHealth + amount, characterData.GetMaxHealth());
        EventBus.Publish(new CharacterHealthChangedEvent { currentHealth = characterData.currentHealth, maxHealth = characterData.GetMaxHealth(), healthChanged = amount });
    }
    
    public void HealToFull()
    {
        characterData.currentHealth = characterData.GetMaxHealth();
        EventBus.Publish(new CharacterHealthChangedEvent { currentHealth = characterData.currentHealth, maxHealth = characterData.GetMaxHealth(), healthChanged = characterData.currentHealth });
    }
    
    public float GetCurrentHealth() => characterData.currentHealth;
    public float GetMaxHealth() => characterData.GetMaxHealth();
    
    /// <summary>
    /// Get max health at a specific level (for stat calculations)
    /// </summary>
    public float GetMaxHealthAtLevel(int level)
    {
        return characterData.GetMaxHealthAtLevel(level);
    }
    
    /// <summary>
    /// Get base attack damage at a specific level (for stat calculations)
    /// </summary>
    public float GetBaseAttackAtLevel(int level)
    {
        return characterData.GetBaseAttackAtLevel(level);
    }
    
    /// <summary>
    /// Get base crit chance at a specific level (for stat calculations)
    /// </summary>
    public float GetBaseCritChanceAtLevel(int level)
    {
        return characterData.GetBaseCritChanceAtLevel(level);
    }
    
    // --- Inventory Management ---
    public InventoryData GetInventoryData() => characterData.inventory;
    
    public bool AddItemToInventory(InventoryItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("[CharacterManager] Attempted to add null item to inventory");
            return false;
        }
        
        if (characterData == null)
        {
            Debug.LogError("[CharacterManager] characterData is null! Cannot add item to inventory.");
            return false;
        }
        
        if (characterData.inventory == null)
        {
            Debug.LogWarning("[CharacterManager] characterData.inventory is null! Initializing new inventory.");
            characterData.inventory = new InventoryData();
        }
        
        InventoryData.AddItemResult result = characterData.inventory.AddItem(item);
        
        if (result.itemsAdded > 0)
        {
            // Notify inventory UI to refresh via EventBus
            EventBus.Publish(new ItemAddedEvent 
            { 
                item = item,
                quantity = result.itemsAdded,
                wasSuccessful = result.success
            });
        }
        
        // Warn if some items couldn't be added
        if (!result.success && result.itemsRemaining > 0)
        {
            Debug.LogWarning($"[Inventory] Inventory full! Could only add {result.itemsAdded} of {result.itemsAdded + result.itemsRemaining} {item.itemName}. {result.itemsRemaining} items were lost.");
        }
        
        return result.success;
    }
    
    /// <summary>
    /// Add an item to inventory and get detailed result
    /// </summary>
    public InventoryData.AddItemResult AddItemToInventoryDetailed(InventoryItem item)
    {
        if (item == null)
        {
            return new InventoryData.AddItemResult(false, 0, 0);
        }
        
        InventoryData.AddItemResult result = characterData.inventory.AddItem(item);
        if (result.itemsAdded > 0)
        {
            // Notify inventory UI to refresh via EventBus
            EventBus.Publish(new ItemAddedEvent 
            { 
                item = item,
                quantity = result.itemsAdded,
                wasSuccessful = result.success
            });
        }
        
        return result;
    }
    
    public bool RemoveItemFromInventory(int slotIndex, int quantity = 1)
    {
        return characterData.inventory.RemoveItem(slotIndex, quantity);
    }
    
    public InventoryItem GetInventoryItem(int slotIndex)
    {
        return characterData.inventory.GetItem(slotIndex);
    }
    
    /// <summary>
    /// Use/consume an item from inventory (consumables, recipes, etc.)
    /// </summary>
    public bool UseItem(int slotIndex)
    {
        InventoryItem item = GetInventoryItem(slotIndex);
        
        if (item == null || item.IsEmpty())
        {
            Debug.LogWarning("[CharacterManager] Cannot use item: slot is empty");
            return false;
        }
        
        // Handle different item types
        switch (item.itemType)
        {
            case ItemType.Recipe:
                return UseRecipeItem(item, slotIndex);
                
            case ItemType.Consumable:
                return UseConsumableItem(item, slotIndex);
                
            case ItemType.Equipment:
                Debug.LogWarning("[CharacterManager] Equipment items should be equipped, not used");
                return false;
                
            default:
                Debug.LogWarning($"[CharacterManager] Item type {item.itemType} cannot be used");
                return false;
        }
    }
    
    /// <summary>
    /// Use a recipe item to unlock a recipe
    /// </summary>
    private bool UseRecipeItem(InventoryItem item, int slotIndex)
    {
        // Get the recipe data from the item
        ItemData itemData = Resources.Load<ItemData>($"Items/{item.itemDataAssetName}");
        if (itemData == null || itemData.recipeData == null)
        {
            Debug.LogError($"[CharacterManager] Recipe item {item.itemName} has no recipe data!");
            return false;
        }
        
        RecipeData recipe = itemData.recipeData;
        
        // Get cooking service
        ICraftingService craftingService = Services.Get<ICraftingService>();
        if (craftingService == null)
        {
            Debug.LogError("[CharacterManager] craftingService not found!");
            return false;
        }
        
        // Check if already unlocked
        if (craftingService.IsRecipeUnlocked(recipe))
        {
            Debug.Log($"[CharacterManager] Recipe '{recipe.recipeName}' is already known");
            EventBus.Publish(new MessageEvent 
            { 
                message = $"You already know this recipe!",
                messageType = GameMessageType.Warning
            });
            return false;
        }
        
        // Check level requirement
        IProfessionService professionService = Services.Get<IProfessionService>();
        if (professionService != null)
        {
            int professionLevel = professionService.GetProfessionLevel(recipe.profession);
            if (professionLevel < recipe.levelRequired)
            {
                Debug.Log($"[CharacterManager] Recipe requires {recipe.profession} level {recipe.levelRequired}");
                EventBus.Publish(new MessageEvent 
                { 
                    message = $"Requires {recipe.profession} level {recipe.levelRequired}",
                    messageType = GameMessageType.Error
                });
                return false;
            }
        }
        
        // Unlock the recipe
        craftingService.UnlockRecipe(recipe);
        
        // Remove the recipe item from inventory
        RemoveItemFromInventory(slotIndex, 1);
        
        // Publish success message
        EventBus.Publish(new MessageEvent 
        { 
            message = $"Learned: {recipe.recipeName}!",
            messageType = GameMessageType.Success
        });
        
        Debug.Log($"[CharacterManager] Learned recipe: {recipe.recipeName}");
        return true;
    }
    
    /// <summary>
    /// Use a consumable item (health potions, buffs, etc.)
    /// </summary>
    private bool UseConsumableItem(InventoryItem item, int slotIndex)
    {
        // TODO: Implement consumable effects (health potions, buffs, etc.)
        // For now, just log a message
        Debug.Log($"[CharacterManager] Used consumable: {item.itemName}");
        
        // Remove the consumable from inventory
        RemoveItemFromInventory(slotIndex, 1);
        
        EventBus.Publish(new MessageEvent 
        { 
            message = $"Used {item.itemName}",
            messageType = GameMessageType.Info
        });
        
        return true;
    }
    
    // --- Save/Load (for future implementation) ---
    public CharacterData GetCharacterData() => characterData;
    
    public void LoadCharacterData(CharacterData data)
    {
        characterData = data;
        dataHasBeenLoaded = true; // Mark that data has been loaded
        
        // Load equipment references for all inventory items
        if (characterData.inventory != null)
        {
            characterData.inventory.LoadAllEquipmentReferences();
        }
        
        // Trigger all events to update UI
        EventBus.Publish(new CharacterXPChangedEvent { newXP = characterData.currentXP, xpGained = 0 });
        EventBus.Publish(new CharacterLevelChangedEvent { newLevel = characterData.level });
        EventBus.Publish(new CharacterGoldChangedEvent { newGold = characterData.gold, goldChanged = 0 });
        EventBus.Publish(new CharacterNameChangedEvent { newName = characterData.characterName });
        EventBus.Publish(new CharacterHealthChangedEvent { currentHealth = characterData.currentHealth, maxHealth = characterData.GetMaxHealth(), healthChanged = 0 });
    }
    
    /// <summary>
    /// Auto-save character data (called on important events like leveling up)
    /// </summary>
    private void AutoSave(string reason = "auto")
    {
        // Get active character slot
        int characterSlot = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        
        if (characterSlot < 0)
        {
            Debug.LogWarning($"[CharacterManager] Cannot auto-save ({reason}): no active character slot");
            return;
        }
        
        bool success = SaveSystem.SaveCurrentCharacter(characterSlot);
        
        if (success)
        {
            Debug.Log($"[CharacterManager] Auto-saved character ({reason})");
        }
        else
        {
            Debug.LogError($"[CharacterManager] Failed to auto-save character ({reason})");
        }
    }
}
