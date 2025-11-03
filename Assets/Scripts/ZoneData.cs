using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents an NPC placement in a zone with position
/// </summary>
[System.Serializable]
public class ZoneNPCEntry
{
    [Tooltip("The NPC data")]
    public NPCData npc;
    
    [Tooltip("Position of the NPC in the zone (absolute pixel coordinates - anchored position)")]
    public Vector2 position = new Vector2(0f, 0f);
}

/// <summary>
/// Represents a monster placement in a zone with position
/// </summary>
[System.Serializable]
public class ZoneMonsterEntry
{
    [Tooltip("The monster data")]
    public MonsterData monster;
    
    [Tooltip("Position of the monster in the zone (absolute pixel coordinates - anchored position)")]
    public Vector2 position = new Vector2(0f, 0f);
}

/// <summary>
/// Represents a resource placement in a zone with position
/// </summary>
[System.Serializable]
public class ZoneResourceEntry
{
    [Tooltip("The resource data")]
    public ResourceData resource;
    
    [Tooltip("Position of the resource in the zone (absolute pixel coordinates - anchored position)")]
    public Vector2 position = new Vector2(0f, 0f);
}

/// <summary>
/// ScriptableObject that defines a zone with its quests and properties.
/// Create instances via: Right-click in Project → Create → Vinland → Zone
/// </summary>
[CreateAssetMenu(fileName = "New Zone", menuName = "Vinland/Zone", order = 2)]
public class ZoneData : ScriptableObject
{
    [Header("Zone Info")]
    public string zoneName = "Zone 1-1";
    
    [Header("Requirements")]
    public int levelRequired = 1;
    public ZoneData prerequisiteZone; // Previous zone that must be completed
    
    [Header("Quests in this Zone")]
    public QuestData[] availableQuests;
    
    [Header("Monsters in this Zone")]
    [Tooltip("List of monsters in this zone with their positions. Same monster can appear multiple times.")]
    public List<ZoneMonsterEntry> monsters = new List<ZoneMonsterEntry>();
    
    [Header("Resources in this Zone")]
    [Tooltip("List of resources in this zone with their positions. Same resource can appear multiple times.")]
    public List<ZoneResourceEntry> resources = new List<ZoneResourceEntry>();
    
    [Header("NPCs in this Zone")]
    [Tooltip("List of NPCs in this zone with their positions. Same NPC can appear multiple times.")]
    public List<ZoneNPCEntry> npcs = new List<ZoneNPCEntry>();
    
    [Header("Navigation")]
    public ZoneData nextZone; // Next zone to unlock
    public bool isUnlocked = false;
    
    [Header("Zone Properties")]
    public Sprite backgroundImage; // Background image for this zone
    
    /// <summary>
    /// Check if this zone is accessible to the player
    /// </summary>
    public bool CanAccess(int playerLevel, ZoneData currentZone)
    {
        // Check level requirement
        if (playerLevel < levelRequired) return false;
        
        // Check prerequisite zone
        if (prerequisiteZone != null && currentZone != prerequisiteZone) return false;
        
        return true;
    }
    
    /// <summary>
    /// Get quests available for the player's level
    /// </summary>
    public QuestData[] GetAvailableQuests(int playerLevel)
    {
        System.Collections.Generic.List<QuestData> available = new System.Collections.Generic.List<QuestData>();
        
        foreach (QuestData quest in availableQuests)
        {
            if (quest != null && playerLevel >= quest.levelRequired)
            {
                available.Add(quest);
            }
        }
        
        return available.ToArray();
    }
    
    /// <summary>
    /// Get all quests in this zone (including locked ones)
    /// </summary>
    public QuestData[] GetAllQuests()
    {
        System.Collections.Generic.List<QuestData> allQuests = new System.Collections.Generic.List<QuestData>();
        
        foreach (QuestData quest in availableQuests)
        {
            if (quest != null)
            {
                allQuests.Add(quest);
            }
        }
        
        return allQuests.ToArray();
    }
    
    /// <summary>
    /// Get all monsters in this zone (for combat compatibility - returns array)
    /// </summary>
    public MonsterData[] GetMonsters()
    {
        List<MonsterData> monsterList = new List<MonsterData>();
        foreach (ZoneMonsterEntry entry in monsters)
        {
            if (entry.monster != null)
            {
                monsterList.Add(entry.monster);
            }
        }
        return monsterList.ToArray();
    }
    
    /// <summary>
    /// Get all monsters in this zone with their positions
    /// </summary>
    public List<ZoneMonsterEntry> GetMonsterEntries()
    {
        return monsters;
    }
    
    /// <summary>
    /// Get all resources in this zone (for backwards compatibility - returns first resource)
    /// </summary>
    public ResourceData GetResource()
    {
        if (resources != null && resources.Count > 0 && resources[0].resource != null)
        {
            return resources[0].resource;
        }
        return null;
    }
    
    /// <summary>
    /// Get all resources in this zone with their positions
    /// </summary>
    public List<ZoneResourceEntry> GetResourceEntries()
    {
        return resources;
    }
    
    /// <summary>
    /// Get all NPCs in this zone with their positions
    /// </summary>
    public List<ZoneNPCEntry> GetNPCs()
    {
        return npcs;
    }
}
