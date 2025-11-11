using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks what activity the player was doing when they left the game.
/// Used for calculating offline/away rewards.
/// </summary>
public enum AwayActivityType
{
    None,       // No activity active
    Mining,     // Gathering resources (mining, woodcutting, etc.)
    Fighting    // Combat
}

/// <summary>
/// Singleton manager that tracks the player's active activity for away/offline rewards.
/// </summary>
public class AwayActivityManager : MonoBehaviour, IAwayActivityService
{
    public static AwayActivityManager Instance { get; private set; }
    
    private AwayActivityType currentActivity = AwayActivityType.None;
    private DateTime activityStartTime;
    private ResourceData currentResource; // For mining activities
    private MonsterData[] currentMonsters; // For fighting activities
    private int mobCount = 1; // For fighting activities
    private bool isLoadingState = false; // Flag to prevent saving during state loading
    
    // Helper methods to reduce duplication
    private string GetSlotPrefix(int characterSlotIndex)
    {
        return $"AwayActivity_Slot_{characterSlotIndex}_";
    }
    
    private int GetActiveSlot()
    {
        return PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
    }
    
    private DateTime ParseDateTimeFromPlayerPrefs(string key, DateTime defaultValue)
    {
        if (PlayerPrefs.HasKey(key))
        {
            string ticksString = PlayerPrefs.GetString(key);
            if (long.TryParse(ticksString, out long ticks))
            {
                return new DateTime(ticks);
            }
        }
        return defaultValue;
    }
    
    public string GetMonsterDisplayNameFromPlayerPrefs(int characterSlotIndex)
    {
        string slotPrefix = GetSlotPrefix(characterSlotIndex);
        if (PlayerPrefs.HasKey(slotPrefix + "MonsterDisplayNames"))
        {
            string displayNamesString = PlayerPrefs.GetString(slotPrefix + "MonsterDisplayNames");
            string[] displayNames = displayNamesString.Split(',');
            if (displayNames.Length > 0 && !string.IsNullOrEmpty(displayNames[0]))
            {
                return displayNames[0];
            }
        }
        return null;
    }
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Register with service locator
        Services.Register<IAwayActivityService>(this);
    }
    
    /// <summary>
    /// Start tracking a mining activity
    /// Only resets start time if starting a NEW activity (different resource or no current activity)
    /// </summary>
    public void StartMining(ResourceData resource)
    {
        if (resource == null)
        {
            StopActivity();
            return;
        }
        
        // If we're already mining the same resource, don't reset the start time
        bool isContinuingSameActivity = (currentActivity == AwayActivityType.Mining && 
                                         currentResource != null && 
                                         currentResource.name == resource.name);
        
        currentActivity = AwayActivityType.Mining;
        
        // Only set new start time if starting a different activity
        if (!isContinuingSameActivity)
        {
            activityStartTime = DateTime.Now;
        }
        // Otherwise, keep the existing activityStartTime
        
        currentResource = resource;
        currentMonsters = null;
        mobCount = 1;
    }
    
    /// <summary>
    /// Start tracking a fighting activity
    /// Only resets start time if starting a NEW activity (different monsters or no current activity)
    /// </summary>
    public void StartFighting(MonsterData[] monsters, int mobCount = 1)
    {
        if (monsters == null || monsters.Length == 0)
        {
            StopActivity();
            return;
        }
        
        // Check if we're continuing the same fight (same monster types)
        bool isContinuingSameActivity = false;
        if (currentActivity == AwayActivityType.Fighting && 
            currentMonsters != null && 
            currentMonsters.Length == monsters.Length)
        {
            // Check if all monsters match
            bool allMatch = true;
            for (int i = 0; i < monsters.Length; i++)
            {
                if (currentMonsters[i].name != monsters[i].name)
                {
                    allMatch = false;
                    break;
                }
            }
            isContinuingSameActivity = allMatch && this.mobCount == mobCount;
        }
        
        currentActivity = AwayActivityType.Fighting;
        
        // Only set new start time if starting a different activity
        if (!isContinuingSameActivity)
        {
            activityStartTime = DateTime.Now;
        }
        // Otherwise, keep the existing activityStartTime
        
        currentMonsters = monsters;
        this.mobCount = mobCount;
        currentResource = null;
    }
    
    /// <summary>
    /// Stop tracking any activity
    /// </summary>
    public void StopActivity()
    {
        currentActivity = AwayActivityType.None;
        currentResource = null;
        currentMonsters = null;
        mobCount = 1;
    }
    
    /// <summary>
    /// Get the current activity type
    /// </summary>
    public AwayActivityType GetCurrentActivity() => currentActivity;
    
    /// <summary>
    /// Get when the current activity started
    /// </summary>
    public DateTime GetActivityStartTime() => activityStartTime;
    
    /// <summary>
    /// Get the current resource (for mining activities)
    /// </summary>
    public ResourceData GetCurrentResource() => currentResource;
    
    /// <summary>
    /// Get the current monsters (for fighting activities)
    /// </summary>
    public MonsterData[] GetCurrentMonsters() => currentMonsters;
    
    /// <summary>
    /// Get the mob count (for fighting activities)
    /// </summary>
    public int GetMobCount() => mobCount;
    
    /// <summary>
    /// Save away activity state to PlayerPrefs (per character slot)
    /// </summary>
    public void SaveAwayState()
    {
        // Don't save if we're currently loading state (to prevent overwriting with stale data)
        if (isLoadingState)
        {
            Debug.LogWarning("[AwayActivity] SaveAwayState called during state loading - ignoring to prevent data corruption");
            return;
        }
        
        // Get active character slot to save with activity
        int activeSlot = GetActiveSlot();
        if (activeSlot < 0)
        {
            Debug.LogWarning("[AwayActivity] No active character slot, cannot save away state");
            return;
        }
        
        string slotPrefix = GetSlotPrefix(activeSlot);
        
        // If no active activity, save "None" activity with session start time
        if (currentActivity == AwayActivityType.None)
        {
            DateTime sessionStartTime = GetLastSessionStartForSlot(activeSlot);
            PlayerPrefs.SetInt(slotPrefix + "Type", (int)AwayActivityType.None);
            PlayerPrefs.SetString(slotPrefix + "StartTime", sessionStartTime.Ticks.ToString());
            PlayerPrefs.DeleteKey(slotPrefix + "ResourceName");
            PlayerPrefs.DeleteKey(slotPrefix + "MonsterNames");
            PlayerPrefs.DeleteKey(slotPrefix + "MobCount");
            PlayerPrefs.Save();
            return;
        }
        
        // Save activity type and start time
        PlayerPrefs.SetInt(slotPrefix + "Type", (int)currentActivity);
        PlayerPrefs.SetString(slotPrefix + "StartTime", activityStartTime.Ticks.ToString());
        
        if (currentActivity == AwayActivityType.Mining && currentResource != null)
        {
            PlayerPrefs.SetString(slotPrefix + "ResourceName", currentResource.name);
        }
        else if (currentActivity == AwayActivityType.Fighting && currentMonsters != null && currentMonsters.Length > 0)
        {
            // Save monster names (comma-separated) - save both asset name and display name
            string[] monsterNames = new string[currentMonsters.Length];
            string[] monsterDisplayNames = new string[currentMonsters.Length];
            for (int i = 0; i < currentMonsters.Length; i++)
            {
                monsterNames[i] = currentMonsters[i].name;
                monsterDisplayNames[i] = currentMonsters[i].monsterName;
            }
            PlayerPrefs.SetString(slotPrefix + "MonsterNames", string.Join(",", monsterNames));
            PlayerPrefs.SetString(slotPrefix + "MonsterDisplayNames", string.Join(",", monsterDisplayNames));
            PlayerPrefs.SetInt(slotPrefix + "MobCount", mobCount);
        }
        
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Get a readable string for the current activity (for display on character screen)
    /// Returns empty string if no activity for this slot
    /// Note: This method reads from PlayerPrefs directly without modifying instance state
    /// </summary>
    public string GetActivityDisplayString(int characterSlotIndex)
    {
        string slotPrefix = GetSlotPrefix(characterSlotIndex);
        
        if (!PlayerPrefs.HasKey(slotPrefix + "Type"))
        {
            return "";
        }
        
        int activityTypeInt = PlayerPrefs.GetInt(slotPrefix + "Type", 0);
        AwayActivityType savedActivity = (AwayActivityType)activityTypeInt;
        
        if (savedActivity == AwayActivityType.Mining)
        {
            if (PlayerPrefs.HasKey(slotPrefix + "ResourceName"))
            {
                string resourceName = PlayerPrefs.GetString(slotPrefix + "ResourceName");
                ResourceData resource = FindResourceByName(resourceName);
                if (resource != null)
                {
                    return $"Currently Mining {resource.resourceName}";
                }
                // Fallback to saved name if ScriptableObject not found
                return $"Currently Mining {resourceName}";
            }
        }
        else if (savedActivity == AwayActivityType.Fighting)
        {
            if (PlayerPrefs.HasKey(slotPrefix + "MonsterNames"))
            {
                string monsterNamesString = PlayerPrefs.GetString(slotPrefix + "MonsterNames");
                string[] monsterNames = monsterNamesString.Split(',');
                
                // Get display name from saved data or ScriptableObject
                string monsterDisplayName = GetMonsterDisplayNameFromPlayerPrefs(characterSlotIndex);
                
                // Try ScriptableObject first, then fallback to saved display name
                if (string.IsNullOrEmpty(monsterDisplayName) && monsterNames.Length > 0)
                {
                    MonsterData monster = FindMonsterByName(monsterNames[0]);
                    if (monster != null)
                    {
                        monsterDisplayName = monster.monsterName;
                    }
                    else
                    {
                        monsterDisplayName = monsterNames[0]; // Last resort: asset name
                    }
                }
                
                if (!string.IsNullOrEmpty(monsterDisplayName))
                {
                    return monsterNames.Length == 1 
                        ? $"Currently Fighting {monsterDisplayName}" 
                        : $"Currently Fighting {monsterNames.Length} Monster Types";
                }
            }
        }
        else if (savedActivity == AwayActivityType.None)
        {
            return "Currently doing Nothing";
        }
        
        return "";
    }
    
    /// <summary>
    /// Save the last played time for a character slot (called when leaving game scene)
    /// </summary>
    public void SaveLastPlayedTime(int characterSlotIndex = -1)
    {
        if (characterSlotIndex < 0)
        {
            characterSlotIndex = GetActiveSlot();
        }
        
        if (characterSlotIndex < 0)
        {
            return;
        }
        
        string slotPrefix = GetSlotPrefix(characterSlotIndex);
        PlayerPrefs.SetString(slotPrefix + "LastPlayed", DateTime.Now.Ticks.ToString());
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Get the last played time for a character slot
    /// Returns DateTime.MinValue if never played
    /// </summary>
    public DateTime GetLastPlayedTime(int characterSlotIndex)
    {
        string slotPrefix = GetSlotPrefix(characterSlotIndex);
        return ParseDateTimeFromPlayerPrefs(slotPrefix + "LastPlayed", DateTime.MinValue);
    }
    
    /// <summary>
    /// Get a formatted string for time since last played (e.g., "2 hours ago", "5 minutes ago")
    /// </summary>
    public string GetTimeSinceLastPlayed(int characterSlotIndex)
    {
        DateTime lastPlayed = GetLastPlayedTime(characterSlotIndex);
        if (lastPlayed == DateTime.MinValue)
        {
            return "Never";
        }
        
        TimeSpan timeSince = DateTime.Now - lastPlayed;
        
        if (timeSince.TotalDays >= 1)
        {
            int days = (int)timeSince.TotalDays;
            return $"{days} day{(days > 1 ? "s" : "")} ago";
        }
        else if (timeSince.TotalHours >= 1)
        {
            int hours = (int)timeSince.TotalHours;
            return $"{hours} hour{(hours > 1 ? "s" : "")} ago";
        }
        else if (timeSince.TotalMinutes >= 1)
        {
            int minutes = (int)timeSince.TotalMinutes;
            return $"{minutes} minute{(minutes > 1 ? "s" : "")} ago";
        }
        else
        {
            return "Just now";
        }
    }
    
    /// <summary>
    /// Mark the start of a new game session (called when entering game scene)
    /// Saves per-character slot to avoid overwriting other characters' session times
    /// </summary>
    public void MarkGameSessionStart()
    {
        int activeSlot = GetActiveSlot();
        if (activeSlot < 0)
        {
            return; // No active character
        }
        
        // Save session start time per character slot
        string slotPrefix = GetSlotPrefix(activeSlot);
        PlayerPrefs.SetString(slotPrefix + "LastSessionStart", DateTime.Now.Ticks.ToString());
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Get the last game session start time for a specific character slot
    /// </summary>
    private DateTime GetLastSessionStartForSlot(int characterSlotIndex)
    {
        string slotPrefix = GetSlotPrefix(characterSlotIndex);
        return ParseDateTimeFromPlayerPrefs(slotPrefix + "LastSessionStart", DateTime.Now);
    }
    
    /// <summary>
    /// Load away activity state from PlayerPrefs for a specific character slot
    /// Returns true if there was a saved activity (including "None")
    /// </summary>
    public bool LoadAwayState(int characterSlotIndex = -1)
    {
        // Set loading flag to prevent saves during loading
        isLoadingState = true;
        
        try
        {
            // If no slot specified, try to get active slot
            if (characterSlotIndex < 0)
            {
                characterSlotIndex = GetActiveSlot();
            }
            
            if (characterSlotIndex < 0)
            {
                Debug.LogWarning("[AwayActivity] LoadAwayState: No character slot index provided");
                return false;
            }
            
            // Use slot-specific keys
            string slotPrefix = GetSlotPrefix(characterSlotIndex);
            
            if (!PlayerPrefs.HasKey(slotPrefix + "Type"))
            {
                return false;
            }
            
            int activityTypeInt = PlayerPrefs.GetInt(slotPrefix + "Type", 0);
            currentActivity = (AwayActivityType)activityTypeInt;
            
            // Load start time
            DateTime loadedStartTime = ParseDateTimeFromPlayerPrefs(slotPrefix + "StartTime", DateTime.MinValue);
            if (loadedStartTime == DateTime.MinValue)
            {
                Debug.LogError($"[AwayActivity] No start time found for slot {characterSlotIndex}");
                StopActivity();
                return false;
            }
            activityStartTime = loadedStartTime;
            
            // Handle "None" activity - no additional data needed
            if (currentActivity == AwayActivityType.None)
            {
                return true;
            }
            
            // Load activity-specific data
            if (currentActivity == AwayActivityType.Mining)
            {
                if (PlayerPrefs.HasKey(slotPrefix + "ResourceName"))
                {
                    string resourceName = PlayerPrefs.GetString(slotPrefix + "ResourceName");
                    currentResource = FindResourceByName(resourceName);
                }
            }
            else if (currentActivity == AwayActivityType.Fighting)
            {
                if (PlayerPrefs.HasKey(slotPrefix + "MonsterNames"))
                {
                    string monsterNamesString = PlayerPrefs.GetString(slotPrefix + "MonsterNames");
                    string[] monsterNames = monsterNamesString.Split(',');
                    
                    List<MonsterData> loadedMonsters = new List<MonsterData>();
                    foreach (string monsterName in monsterNames)
                    {
                        MonsterData monster = FindMonsterByName(monsterName);
                        if (monster != null)
                        {
                            loadedMonsters.Add(monster);
                        }
                    }
                    
                    currentMonsters = loadedMonsters.Count > 0 ? loadedMonsters.ToArray() : null;
                    mobCount = PlayerPrefs.GetInt(slotPrefix + "MobCount", 1);
                }
            }
            
            return true;
        }
        finally
        {
            isLoadingState = false;
        }
    }
    
    /// <summary>
    /// Find a ResourceData ScriptableObject by name
    /// </summary>
    private ResourceData FindResourceByName(string resourceName)
    {
        // Load all ResourceData assets from Resources folder
        ResourceData[] allResources = Resources.LoadAll<ResourceData>("");
        foreach (var resource in allResources)
        {
            if (resource.name == resourceName || resource.resourceName == resourceName)
            {
                return resource;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Find a MonsterData ScriptableObject by name (public for use by other scripts)
    /// </summary>
    public MonsterData FindMonsterByNamePublic(string monsterName)
    {
        return FindMonsterByName(monsterName);
    }
    
    /// <summary>
    /// Try to load monsters from saved names (fallback when ScriptableObjects aren't found)
    /// Returns array of found monsters, or empty array if none found
    /// </summary>
    public MonsterData[] TryLoadMonstersFromSavedNames(int characterSlotIndex)
    {
        if (characterSlotIndex < 0)
        {
            return new MonsterData[0];
        }
        
        string slotPrefix = GetSlotPrefix(characterSlotIndex);
        if (!PlayerPrefs.HasKey(slotPrefix + "MonsterNames"))
        {
            return new MonsterData[0];
        }
        
        string monsterNamesString = PlayerPrefs.GetString(slotPrefix + "MonsterNames");
        string[] monsterNames = monsterNamesString.Split(',');
        
        List<MonsterData> foundMonsters = new List<MonsterData>();
        foreach (string name in monsterNames)
        {
            MonsterData m = FindMonsterByName(name);
            if (m != null)
            {
                foundMonsters.Add(m);
                break; // Just need one for calculation
            }
        }
        
        return foundMonsters.ToArray();
    }
    
    /// <summary>
    /// Find a MonsterData ScriptableObject by name
    /// </summary>
    private MonsterData FindMonsterByName(string monsterName)
    {
        MonsterData[] allMonsters = Resources.LoadAll<MonsterData>("");
        
        // Try exact name match (ScriptableObject asset name)
        foreach (var monster in allMonsters)
        {
            if (monster.name == monsterName)
            {
                return monster;
            }
        }
        
        // Try monsterName field match (display name)
        foreach (var monster in allMonsters)
        {
            if (monster.monsterName == monsterName)
            {
                return monster;
            }
        }
        
        // Try case-insensitive match
        foreach (var monster in allMonsters)
        {
            if (monster.name.Equals(monsterName, System.StringComparison.OrdinalIgnoreCase) ||
                monster.monsterName.Equals(monsterName, System.StringComparison.OrdinalIgnoreCase))
            {
                return monster;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Clear saved away state (after rewards have been collected)
    /// </summary>
    public void ClearAwayState(int characterSlotIndex = -1)
    {
        // If no slot specified, get active slot
        if (characterSlotIndex < 0)
        {
            characterSlotIndex = GetActiveSlot();
        }
        
        if (characterSlotIndex < 0)
        {
            return;
        }
        
        // Clear slot-specific keys
        string slotPrefix = GetSlotPrefix(characterSlotIndex);
        PlayerPrefs.DeleteKey(slotPrefix + "Type");
        PlayerPrefs.DeleteKey(slotPrefix + "StartTime");
        PlayerPrefs.DeleteKey(slotPrefix + "ResourceName");
        PlayerPrefs.DeleteKey(slotPrefix + "MonsterNames");
        PlayerPrefs.DeleteKey(slotPrefix + "MonsterDisplayNames");
        PlayerPrefs.DeleteKey(slotPrefix + "MobCount");
        // Note: We don't delete LastSessionStart - that should persist for tracking "doing Nothing" time
        PlayerPrefs.Save();
        
        StopActivity();
    }
}

