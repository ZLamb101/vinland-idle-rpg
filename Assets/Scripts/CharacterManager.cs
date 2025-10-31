using System;
using UnityEngine;

/// <summary>
/// Singleton manager for character data in the idle/incremental game.
/// This is the central hub for all character stat modifications.
/// </summary>
public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }
    
    [SerializeField] private CharacterData characterData = new CharacterData();
    
    // Events for UI and other systems to subscribe to
    public event Action<int> OnXPChanged;
    public event Action<int> OnLevelChanged;
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
    }
    
    void Start()
    {
        // Initialize health to max
        characterData.currentHealth = characterData.GetMaxHealth();
        
        // Initialize by triggering events with current values
        OnXPChanged?.Invoke(characterData.currentXP);
        OnLevelChanged?.Invoke(characterData.level);
        OnGoldChanged?.Invoke(characterData.gold);
        OnNameChanged?.Invoke(characterData.characterName);
        OnHealthChanged?.Invoke(characterData.currentHealth, characterData.GetMaxHealth());
    }
    
    // --- XP Management ---
    public void AddXP(int amount)
    {
        characterData.currentXP += amount;
        OnXPChanged?.Invoke(characterData.currentXP);
        
        // Check for level up
        while (characterData.CanLevelUp())
        {
            characterData.LevelUp();
            OnLevelChanged?.Invoke(characterData.level);
            OnXPChanged?.Invoke(characterData.currentXP); // Update XP after level up
            
            // Update max health on level up and heal to full
            characterData.currentHealth = characterData.GetMaxHealth();
            OnHealthChanged?.Invoke(characterData.currentHealth, characterData.GetMaxHealth());
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
    
    // --- Inventory Management ---
    public InventoryData GetInventoryData() => characterData.inventory;
    
    public bool AddItemToInventory(InventoryItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("Trying to add null item to inventory!");
            return false;
        }
        
        bool success = characterData.inventory.AddItem(item);
        if (success)
        {
            // Refresh inventory UI if it exists
            InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
            if (inventoryUI != null)
            {
                inventoryUI.RefreshDisplay();
            }
        }
        return success;
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
}
