using System;
using UnityEngine;

/// <summary>
/// Singleton manager for character data in the idle/incremental game.
/// This is the central hub for all character stat modifications.
/// </summary>
public class CharacterManager : MonoBehaviour, ICharacterService
{
    public static CharacterManager Instance { get; private set; }
    
    private CharacterData characterData = new CharacterData(); // Not serialized - loaded at runtime
    private bool dataHasBeenLoaded = false; // Track if character data has been loaded from save
    
    // Events for UI and other systems to subscribe to
    public event Action<int> OnXPChanged;
    public event Action<int> OnLevelChanged;
    public event Action<int, int> OnLevelUp; // (oldLevel, newLevel)
    public event Action<int> OnGoldChanged;
    public event Action<string> OnNameChanged;
    public event Action<float, float> OnHealthChanged; // (currentHealth, maxHealth)
    public event Action OnPlayerDied; // When health reaches 0
    
    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist between scenes
        
        // Register with service locator
        Services.Register<ICharacterService>(this);
        
        // Reset loaded flag when singleton is first created or reset
        // This ensures fresh data is loaded when entering a new scene
        dataHasBeenLoaded = false;
        
        // Ensure game runs in background (essential for idle games)
        Application.runInBackground = true;
        
        // Ensure AwayActivityManager exists
        if (AwayActivityManager.Instance == null)
        {
            GameObject awayManagerObj = new GameObject("AwayActivityManager");
            awayManagerObj.AddComponent<AwayActivityManager>();
        }
    }
    
    void Start()
    {
        // Only initialize if character data hasn't been loaded yet
        // If data has been loaded (from CharacterLoader), don't overwrite it
        if (!dataHasBeenLoaded)
        {
            // Initialize health to max for new characters
            characterData.currentHealth = characterData.GetMaxHealth();
            
            // Don't fire events here if data hasn't been loaded - CharacterLoader will do it
            // This prevents empty/default values from being displayed before character is loaded
        }
        else
        {
            // Data has been loaded, trigger events to update UI
            OnXPChanged?.Invoke(characterData.currentXP);
            OnLevelChanged?.Invoke(characterData.level);
            OnGoldChanged?.Invoke(characterData.gold);
            OnNameChanged?.Invoke(characterData.characterName);
            OnHealthChanged?.Invoke(characterData.currentHealth, characterData.GetMaxHealth());
        }
    }
    
    void OnDestroy()
    {
        // Unregister from service locator
        Services.Unregister<ICharacterService>();
    }
    
    void OnApplicationQuit()
    {
        // Save away activity state when game closes
        if (AwayActivityManager.Instance != null)
        {
            AwayActivityManager.Instance.SaveAwayState();
            
            // Save last played time
            int currentSlot = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
            if (currentSlot >= 0)
            {
                AwayActivityManager.Instance.SaveLastPlayedTime(currentSlot);
            }
        }
        
        // Auto-save character data
        AutoSave("application quit");
    }
    
    // --- XP Management ---
    public void AddXP(int amount)
    {
        characterData.currentXP += amount;
        OnXPChanged?.Invoke(characterData.currentXP);
        
        // Check for level up
        bool leveledUp = false;
        while (characterData.CanLevelUp())
        {
            int oldLevel = characterData.level;
            characterData.LevelUp();
            int newLevel = characterData.level;
            
            OnLevelChanged?.Invoke(newLevel);
            OnLevelUp?.Invoke(oldLevel, newLevel); // Emit level-up event with both levels
            OnXPChanged?.Invoke(characterData.currentXP); // Update XP after level up
            
            // Update max health on level up and heal to full
            characterData.currentHealth = characterData.GetMaxHealth();
            OnHealthChanged?.Invoke(characterData.currentHealth, characterData.GetMaxHealth());
            
            leveledUp = true;
            
            Debug.Log($"[CharacterManager] Level up! {oldLevel} -> {newLevel}");
        }
        
        // Auto-save after leveling up (important milestone)
        if (leveledUp)
        {
            AutoSave("level up");
        }
    }
    
    public int GetCurrentXP() => characterData.currentXP;
    public int GetXPRequiredForNextLevel() => characterData.GetXPRequiredForNextLevel();
    
    // --- Gold Management ---
    public void AddGold(int amount)
    {
        characterData.gold += amount;
        OnGoldChanged?.Invoke(characterData.gold);
    }
    
    public bool SpendGold(int amount)
    {
        if (characterData.gold >= amount)
        {
            characterData.gold -= amount;
            OnGoldChanged?.Invoke(characterData.gold);
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
        OnNameChanged?.Invoke(characterData.characterName);
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
        
        if (TalentManager.Instance != null)
        {
            TalentBonuses talents = TalentManager.Instance.GetTotalBonuses();
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
            OnHealthChanged?.Invoke(characterData.currentHealth, characterData.GetMaxHealth());
            OnPlayerDied?.Invoke();
            
            // Reset to max health
            characterData.currentHealth = characterData.GetMaxHealth();
            OnHealthChanged?.Invoke(characterData.currentHealth, characterData.GetMaxHealth());
        }
        else
        {
            OnHealthChanged?.Invoke(characterData.currentHealth, characterData.GetMaxHealth());
        }
    }
    
    public void Heal(float amount)
    {
        characterData.currentHealth = Mathf.Min(characterData.currentHealth + amount, characterData.GetMaxHealth());
        OnHealthChanged?.Invoke(characterData.currentHealth, characterData.GetMaxHealth());
    }
    
    public void HealToFull()
    {
        characterData.currentHealth = characterData.GetMaxHealth();
        OnHealthChanged?.Invoke(characterData.currentHealth, characterData.GetMaxHealth());
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
            return false;
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
        OnXPChanged?.Invoke(characterData.currentXP);
        OnLevelChanged?.Invoke(characterData.level);
        OnGoldChanged?.Invoke(characterData.gold);
        OnNameChanged?.Invoke(characterData.characterName);
        OnHealthChanged?.Invoke(characterData.currentHealth, characterData.GetMaxHealth());
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
