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
    
    // Talents
    public Dictionary<string, int> unlockedTalents = new Dictionary<string, int>();
    public int unspentTalentPoints = 0;
    public int totalTalentPoints = 0;
    
    // Professions (stored as string keys because Unity JsonUtility doesn't serialize enum keys)
    public List<string> professionNames = new List<string>();
    public List<int> professionLevels = new List<int>();
    public List<int> professionXP = new List<int>();
    
    // Zone
    public int currentZoneIndex = 0;
    
    // Quests
    public List<PlayerQuest> activeQuests = new List<PlayerQuest>();
    public List<string> completedQuestIDs = new List<string>();
    
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
            
            // Quests
            if (charData.activeQuests != null)
            {
                data.activeQuests = new List<PlayerQuest>(charData.activeQuests);
                Debug.Log($"[SaveData] Saved {data.activeQuests.Count} active quests");
            }
            
            if (charData.completedQuestIDs != null)
            {
                data.completedQuestIDs = new List<string>(charData.completedQuestIDs);
                Debug.Log($"[SaveData] Saved {data.completedQuestIDs.Count} completed quests");
            }
        }
        
        // Equipment - Use TryGet since services might be destroyed during shutdown
        if (Services.TryGet<IEquipmentService>(out var equipmentService))
        {
            var equipData = equipmentService.GetEquipmentSaveData();
            data.equippedItemsList.Clear();
            foreach (var kvp in equipData)
            {
                Debug.Log($"[SaveData] Saving equipment: Slot={kvp.Key}, Asset={kvp.Value}");
                data.equippedItemsList.Add(new EquipmentSlotData(kvp.Key.ToString(), kvp.Value));
            }
            Debug.Log($"[SaveData] Saved {data.equippedItemsList.Count} equipment items");
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
        
        // Professions
        if (Services.TryGet<IProfessionService>(out var professionService))
        {
            ProfessionData profData = professionService.GetProfessionData();
            if (profData != null)
            {
                var levels = profData.GetAllLevels();
                var xps = profData.GetAllXP();
                
                data.professionNames.Clear();
                data.professionLevels.Clear();
                data.professionXP.Clear();
                
                foreach (var kvp in levels)
                {
                    data.professionNames.Add(kvp.Key.ToString());
                    data.professionLevels.Add(kvp.Value);
                    data.professionXP.Add(xps.ContainsKey(kvp.Key) ? xps[kvp.Key] : 0);
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
            
            // Professions
            if (professionNames != null && professionNames.Count > 0)
            {
                charData.professions = new ProfessionData();
                for (int i = 0; i < professionNames.Count; i++)
                {
                    if (System.Enum.TryParse(professionNames[i], out ProfessionType profType))
                    {
                        if (i < professionLevels.Count)
                        {
                            charData.professions.SetProfessionLevel(profType, professionLevels[i]);
                        }
                        if (i < professionXP.Count)
                        {
                            charData.professions.SetProfessionXP(profType, professionXP[i]);
                        }
                    }
                }
            }
            
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
            
            // Quests
            if (activeQuests != null)
            {
                charData.activeQuests = new List<PlayerQuest>(activeQuests);
                Debug.Log($"[SaveData] Loaded {charData.activeQuests.Count} active quests");
            }
            
            if (completedQuestIDs != null)
            {
                charData.completedQuestIDs = new List<string>(completedQuestIDs);
                Debug.Log($"[SaveData] Loaded {charData.completedQuestIDs.Count} completed quests");
            }
            
            characterService.LoadCharacterData(charData);
        }
        
        // Professions
        if (Services.TryGet<IProfessionService>(out var professionService))
        {
            if (professionNames != null && professionNames.Count > 0)
            {
                ProfessionData profData = new ProfessionData();
                for (int i = 0; i < professionNames.Count; i++)
                {
                    if (System.Enum.TryParse(professionNames[i], out ProfessionType profType))
                    {
                        if (i < professionLevels.Count)
                        {
                            profData.SetProfessionLevel(profType, professionLevels[i]);
                        }
                        if (i < professionXP.Count)
                        {
                            profData.SetProfessionXP(profType, professionXP[i]);
                        }
                    }
                }
                professionService.LoadProfessionData(profData);
            }
            else
            {
                // Initialize fresh profession data
                professionService.LoadProfessionData(new ProfessionData());
            }
        }
        
        // Equipment
        if (Services.TryGet<IEquipmentService>(out var equipmentService))
        {
            Debug.Log($"[SaveData] Loading equipment data. Items in list: {(equippedItemsList != null ? equippedItemsList.Count : 0)}");
            
            if (equippedItemsList != null && equippedItemsList.Count > 0)
            {
                Dictionary<EquipmentSlot, string> equipDict = new Dictionary<EquipmentSlot, string>();
                foreach (var item in equippedItemsList)
                {
                    Debug.Log($"[SaveData] Loading equipment: Slot={item.slotName}, Asset={item.equipmentAssetName}");
                    if (Enum.TryParse(item.slotName, out EquipmentSlot slot))
                    {
                        equipDict[slot] = item.equipmentAssetName;
                    }
                }
                equipmentService.LoadEquipmentData(equipDict);
                Debug.Log($"[SaveData] Loaded {equipDict.Count} equipment items");
            }
            else
            {
                Debug.Log("[SaveData] No equipment to load - clearing all equipment");
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
            talentService.LoadTalentData(unlockedTalents, unspentTalentPoints, totalTalentPoints);
            Debug.Log($"[SaveData] Loaded talents: {unlockedTalents.Count} unlocked, {unspentTalentPoints} unspent points");
        }
        
        // Zone - Use TryGet since ZoneManager might not exist yet during early loading
        if (Services.TryGet<IZoneService>(out var zoneService))
        {
            zoneService.LoadCurrentZone();
        }
    }
}

