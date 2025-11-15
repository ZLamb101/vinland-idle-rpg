using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Complete save data structure for a character
/// Everything in one place with versioning support
/// </summary>
[System.Serializable]
public class SaveData
{
    // Version for migration support
    public int version = 1;
    
    // Character basic data
    public string characterName = "";
    public string race = "Human";
    public string characterClass = "Warrior";
    public int level = 1;
    public int currentXP = 0;
    public int gold = 0;
    public float currentHealth = 50f;
    
    // Inventory
    public InventoryItem[] inventoryItems;
    
    // Equipment (stored as asset names for ScriptableObjects)
    public Dictionary<string, string> equippedItems = new Dictionary<string, string>();
    
    // Talents
    public Dictionary<string, int> unlockedTalents = new Dictionary<string, int>();
    public int unspentTalentPoints = 0;
    public int totalTalentPoints = 0;
    
    // Zone
    public int currentZoneIndex = 0;
    
    // Away activity
    public int awayActivityType = 0; // AwayActivityType enum
    public string awayActivityStartTime = ""; // DateTime.Ticks as string
    public string awayResourceName = "";
    public List<string> awayMonsterNames = new List<string>();
    public List<string> awayMonsterDisplayNames = new List<string>();
    public int awayMobCount = 1;
    public string lastPlayedTime = "";
    public string lastSessionStart = "";
    
    // Metadata
    public string saveTime = ""; // When this save was created
    
    /// <summary>
    /// Create SaveData from current game state
    /// </summary>
    public static SaveData CreateFromCurrentState()
    {
        SaveData data = new SaveData();
        data.saveTime = DateTime.Now.Ticks.ToString();
        
        // Character data - Use TryGet since services might be destroyed during shutdown
        if (Services.TryGet<ICharacterService>(out var characterService))
        {
            CharacterData charData = characterService.GetCharacterData();
            data.characterName = charData.characterName;
            data.race = charData.race;
            data.characterClass = charData.characterClass;
            data.level = charData.level;
            data.currentXP = charData.currentXP;
            data.gold = charData.gold;
            data.currentHealth = charData.currentHealth;
            
            // Inventory
            if (charData.inventory != null && charData.inventory.items != null)
            {
                data.inventoryItems = new InventoryItem[charData.inventory.items.Length];
                for (int i = 0; i < charData.inventory.items.Length; i++)
                {
                    // Create copies to avoid reference issues
                    data.inventoryItems[i] = new InventoryItem(
                        charData.inventory.items[i].itemName,
                        charData.inventory.items[i].quantity,
                        charData.inventory.items[i].icon
                    );
                    data.inventoryItems[i].description = charData.inventory.items[i].description;
                    data.inventoryItems[i].maxStackSize = charData.inventory.items[i].maxStackSize;
                    data.inventoryItems[i].itemType = charData.inventory.items[i].itemType;
                    data.inventoryItems[i].baseValue = charData.inventory.items[i].baseValue;
                    data.inventoryItems[i].itemDataAssetName = charData.inventory.items[i].itemDataAssetName;
                    data.inventoryItems[i].equipmentAssetName = charData.inventory.items[i].equipmentAssetName;
                }
            }
        }
        
        // Equipment - Use TryGet since services might be destroyed during shutdown
        if (Services.TryGet<IEquipmentService>(out var equipmentService))
        {
            var equipData = equipmentService.GetEquipmentSaveData();
            foreach (var kvp in equipData)
            {
                data.equippedItems[kvp.Key.ToString()] = kvp.Value;
            }
        }
        
        // Talents - Use TryGet since services might be destroyed during shutdown
        if (Services.TryGet<ITalentService>(out var talentService))
        {
            data.unspentTalentPoints = talentService.GetUnspentPoints();
            data.totalTalentPoints = talentService.GetTotalPoints();
            
            var talents = talentService.GetAllUnlockedTalents();
            foreach (var kvp in talents)
            {
                if (kvp.Key != null)
                {
                    data.unlockedTalents[kvp.Key.name] = kvp.Value;
                }
            }
        }
        
        // Zone - Use TryGet for consistency
        if (Services.TryGet<IZoneService>(out var zoneService))
        {
            data.currentZoneIndex = zoneService.GetCurrentZoneIndex();
        }
        
        // Away activity
        if (Services.TryGet<IAwayActivityService>(out var awayActivityService))
        {
            data.awayActivityType = (int)awayActivityService.GetCurrentActivity();
            data.awayActivityStartTime = awayActivityService.GetActivityStartTime().Ticks.ToString();
            
            if (awayActivityService.GetCurrentResource() != null)
            {
                data.awayResourceName = awayActivityService.GetCurrentResource().name;
            }
            
            if (awayActivityService.GetCurrentMonsters() != null)
            {
                foreach (var monster in awayActivityService.GetCurrentMonsters())
                {
                    if (monster != null)
                    {
                        data.awayMonsterNames.Add(monster.name);
                        data.awayMonsterDisplayNames.Add(monster.monsterName);
                    }
                }
            }
            
            data.awayMobCount = awayActivityService.GetMobCount();
        }
        
        return data;
    }
    
    /// <summary>
    /// Apply this save data to current game state
    /// </summary>
    public void ApplyToGameState()
    {
        // Character data
        if (Services.TryGet<ICharacterService>(out var characterService))
        {
            CharacterData charData = new CharacterData();
            charData.characterName = characterName;
            charData.race = race;
            charData.characterClass = characterClass;
            charData.level = level;
            charData.currentXP = currentXP;
            charData.gold = gold;
            charData.currentHealth = currentHealth;
            
            // Inventory
            if (inventoryItems != null)
            {
                charData.inventory = new InventoryData();
                charData.inventory.items = new InventoryItem[inventoryItems.Length];
                for (int i = 0; i < inventoryItems.Length; i++)
                {
                    charData.inventory.items[i] = new InventoryItem(
                        inventoryItems[i].itemName,
                        inventoryItems[i].quantity,
                        inventoryItems[i].icon
                    );
                    charData.inventory.items[i].description = inventoryItems[i].description;
                    charData.inventory.items[i].maxStackSize = inventoryItems[i].maxStackSize;
                    charData.inventory.items[i].itemType = inventoryItems[i].itemType;
                    charData.inventory.items[i].baseValue = inventoryItems[i].baseValue;
                    charData.inventory.items[i].itemDataAssetName = inventoryItems[i].itemDataAssetName;
                    charData.inventory.items[i].equipmentAssetName = inventoryItems[i].equipmentAssetName;
                }
                
                // Load equipment references
                charData.inventory.LoadAllEquipmentReferences();
            }
            
            characterService.LoadCharacterData(charData);
        }
        
        // Equipment
        if (Services.TryGet<IEquipmentService>(out var equipmentService) && equippedItems != null)
        {
            Dictionary<EquipmentSlot, string> equipDict = new Dictionary<EquipmentSlot, string>();
            foreach (var kvp in equippedItems)
            {
                if (Enum.TryParse(kvp.Key, out EquipmentSlot slot))
                {
                    equipDict[slot] = kvp.Value;
                }
            }
            equipmentService.LoadEquipmentData(equipDict);
        }
        
        // Talents (would need to implement LoadTalentData in TalentManager)
        // For now, we keep the existing PlayerPrefs-based talent loading
        
        // Zone - Use TryGet since ZoneManager might not exist yet during early loading
        if (Services.TryGet<IZoneService>(out var zoneService))
        {
            zoneService.LoadCurrentZone();
        }
    }
}

