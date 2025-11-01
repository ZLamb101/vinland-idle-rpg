using UnityEngine;

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
    public MonsterData[] monsters; // Monsters that can be fought in this zone
    
    [Header("Resources in this Zone")]
    public ResourceData resource; // Resource that can be gathered in this zone (optional)
    
    [Header("Navigation")]
    public ZoneData nextZone; // Next zone to unlock
    public bool isUnlocked = false;
    
    [Header("Zone Properties")]
    public Color zoneColor = Color.white;
    public string zoneTheme = "Forest"; // For future use (music, visuals, etc.)
    
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
    /// Get all monsters in this zone
    /// </summary>
    public MonsterData[] GetMonsters()
    {
        return monsters;
    }
    
    /// <summary>
    /// Get the resource in this zone (if any)
    /// </summary>
    public ResourceData GetResource()
    {
        return resource;
    }
}
