using System;

/// <summary>
/// Interface for character management services
/// </summary>
public interface ICharacterService
{
    // Events
    event Action<int> OnXPChanged;
    event Action<int> OnLevelChanged;
    event Action<int, int> OnLevelUp;
    event Action<int> OnGoldChanged;
    event Action<string> OnNameChanged;
    event Action<float, float> OnHealthChanged;
    event Action OnPlayerDied;
    
    // XP Management
    void AddXP(int amount);
    int GetCurrentXP();
    int GetXPRequiredForNextLevel();
    
    // Gold Management
    void AddGold(int amount);
    bool SpendGold(int amount);
    int GetGold();
    
    // Level Management
    int GetLevel();
    
    // Name Management
    void SetName(string newName);
    string GetName();
    
    // Race/Class Management
    string GetRace();
    string GetCharacterClass();
    void SetRace(string newRace);
    void SetCharacterClass(string newClass);
    
    // Health Management
    void TakeDamage(float amount);
    void Heal(float amount);
    void HealToFull();
    float GetCurrentHealth();
    float GetMaxHealth();
    float GetMaxHealthWithTalents();
    float GetMaxHealthAtLevel(int level);
    float GetBaseAttackAtLevel(int level);
    float GetBaseCritChanceAtLevel(int level);
    
    // Inventory Management
    InventoryData GetInventoryData();
    bool AddItemToInventory(InventoryItem item);
    InventoryData.AddItemResult AddItemToInventoryDetailed(InventoryItem item);
    bool RemoveItemFromInventory(int slotIndex, int quantity = 1);
    InventoryItem GetInventoryItem(int slotIndex);
    
    // Data Management
    CharacterData GetCharacterData();
    void LoadCharacterData(CharacterData data);
}

