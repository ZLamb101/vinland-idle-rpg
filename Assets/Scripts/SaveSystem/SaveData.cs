using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable equipment slot data (Unity JsonUtility doesn't support Dictionary)
/// </summary>
[System.Serializable]
public class EquipmentSlotData
{
    public string slotName;
    public string equipmentAssetName;
    
    public EquipmentSlotData(string slot, string asset)
    {
        slotName = slot;
        equipmentAssetName = asset;
    }
}

/// <summary>
/// Serializable talent data (Unity JsonUtility doesn't support Dictionary)
/// </summary>
[System.Serializable]
public class TalentSaveData
{
    public string talentAssetName;
    public int rank;
    
    public TalentSaveData(string assetName, int talentRank)
    {
        talentAssetName = assetName;
        rank = talentRank;
    }
}

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
    
    // Equipment (stored as list because Unity JsonUtility doesn't serialize Dictionary)
    public List<EquipmentSlotData> equippedItemsList = new List<EquipmentSlotData>();
    
    // Talents (stored as list because Unity JsonUtility doesn't serialize Dictionary)
    public List<TalentSaveData> unlockedTalentsList = new List<TalentSaveData>();
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
            data.equippedItemsList.Clear();
            foreach (var kvp in equipData)
            {
                data.equippedItemsList.Add(new EquipmentSlotData(kvp.Key.ToString(), kvp.Value));
            }
        }
        
        // Talents - Use TryGet since services might be destroyed during shutdown
        if (Services.TryGet<ITalentService>(out var talentService))
        {
            data.unspentTalentPoints = talentService.GetUnspentPoints();
            data.totalTalentPoints = talentService.GetTotalPoints();
            
            data.unlockedTalentsList.Clear();
            var talents = talentService.GetAllUnlockedTalents();
            foreach (var kvp in talents)
            {
                if (kvp.Key != null)
                {
                    // Get the resource path for the talent
                    string resourcePath = GetTalentResourcePath(kvp.Key);
                    data.unlockedTalentsList.Add(new TalentSaveData(resourcePath, kvp.Value));
                    Debug.Log($"[SaveData] Saving talent: {kvp.Key.talentName} as {resourcePath} at rank {kvp.Value}");
                }
            }
            Debug.Log($"[SaveData] Saved {data.unlockedTalentsList.Count} talents, Unspent: {data.unspentTalentPoints}, Total: {data.totalTalentPoints}");
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
        if (Services.TryGet<IEquipmentService>(out var equipmentService))
        {
            if (equippedItemsList != null && equippedItemsList.Count > 0)
            {
                Dictionary<EquipmentSlot, string> equipDict = new Dictionary<EquipmentSlot, string>();
                foreach (var item in equippedItemsList)
                {
                    if (Enum.TryParse(item.slotName, out EquipmentSlot slot))
                    {
                        equipDict[slot] = item.equipmentAssetName;
                    }
                }
                equipmentService.LoadEquipmentData(equipDict);
            }
            else
            {
                equipmentService.LoadEquipmentData(null);
            }
        }
        else
        {
            Debug.LogWarning("[SaveData] EquipmentService not available for loading equipment");
        }
        
        // Talents
        if (Services.TryGet<ITalentService>(out var talentService))
        {
            Debug.Log($"[SaveData] Loading talent data. Unspent: {unspentTalentPoints}, Total: {totalTalentPoints}, Talents: {(unlockedTalentsList != null ? unlockedTalentsList.Count : 0)}");
            
            // Convert list to dictionary for TalentManager
            Dictionary<string, int> talentDict = new Dictionary<string, int>();
            if (unlockedTalentsList != null)
            {
                foreach (var talentData in unlockedTalentsList)
                {
                    talentDict[talentData.talentAssetName] = talentData.rank;
                }
            }
            
            talentService.LoadTalentData(talentDict, unspentTalentPoints, totalTalentPoints);
        }
        else
        {
            Debug.LogWarning("[SaveData] TalentService not available for loading talents");
        }
        
        // Zone - Use TryGet since ZoneManager might not exist yet during early loading
        if (Services.TryGet<IZoneService>(out var zoneService))
        {
            zoneService.LoadCurrentZone();
        }
    }
    
    /// <summary>
    /// Get the Resources folder path for a talent asset
    /// </summary>
    private static string GetTalentResourcePath(TalentData talent)
    {
#if UNITY_EDITOR
        // In editor, use AssetDatabase to get the correct path
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(talent);
        
        // Find "Resources/" in the path
        int resourcesIndex = assetPath.IndexOf("Resources/");
        if (resourcesIndex >= 0)
        {
            // Get everything after "Resources/"
            string relativePath = assetPath.Substring(resourcesIndex + "Resources/".Length);
            
            // Remove the file extension
            int extensionIndex = relativePath.LastIndexOf('.');
            if (extensionIndex >= 0)
            {
                relativePath = relativePath.Substring(0, extensionIndex);
            }
            
            return relativePath;
        }
#endif
        // Fallback: just use the asset name
        return talent.name;
    }
}

