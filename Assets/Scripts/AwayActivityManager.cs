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
public class AwayActivityManager : MonoBehaviour
{
    public static AwayActivityManager Instance { get; private set; }
    
    private AwayActivityType currentActivity = AwayActivityType.None;
    private DateTime activityStartTime;
    private DateTime lastGameSessionStart; // Track when player entered the game scene
    private ResourceData currentResource; // For mining activities
    private MonsterData[] currentMonsters; // For fighting activities
    private int mobCount = 1; // For fighting activities
    private bool isLoadingState = false; // Flag to prevent saving during state loading
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Track when this session started
        lastGameSessionStart = DateTime.Now;
    }
    
    void OnEnable()
    {
        // Track when entering game scene (if not already set)
        if (lastGameSessionStart == default(DateTime))
        {
            lastGameSessionStart = DateTime.Now;
        }
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
        int activeSlot = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        if (activeSlot < 0)
        {
            Debug.LogWarning("[AwayActivity] No active character slot, cannot save away state");
            return; // No active character, don't save
        }
        
        // CRITICAL CHECK: Verify that the instance state matches what should be saved for this slot
        // If we're saving Mining but the slot's saved state is Fighting, we're saving stale data
        string slotPrefix = $"AwayActivity_Slot_{activeSlot}_";
        if (PlayerPrefs.HasKey(slotPrefix + "Type"))
        {
            int savedActivityType = PlayerPrefs.GetInt(slotPrefix + "Type", 0);
            AwayActivityType savedActivity = (AwayActivityType)savedActivityType;
            
            // If the saved activity doesn't match the instance activity, and we're not explicitly clearing it,
            // this might be stale data from a previous character
            if (savedActivity != currentActivity && currentActivity != AwayActivityType.None)
            {
                // Check if this is a legitimate activity change (e.g., switching from Mining to Fighting)
                // by checking if the instance state matches what we're trying to save
                bool isLegitimateChange = false;
                
                if (currentActivity == AwayActivityType.Mining && currentResource != null)
                {
                    // Check if the saved state has a different resource
                    if (PlayerPrefs.HasKey(slotPrefix + "ResourceName"))
                    {
                        string savedResourceName = PlayerPrefs.GetString(slotPrefix + "ResourceName");
                        if (savedResourceName == currentResource.name)
                        {
                            isLegitimateChange = true; // Same resource, might be restarting activity
                        }
                    }
                }
                else if (currentActivity == AwayActivityType.Fighting && currentMonsters != null && currentMonsters.Length > 0)
                {
                    // Check if the saved state has different monsters
                    if (PlayerPrefs.HasKey(slotPrefix + "MonsterNames"))
                    {
                        string savedMonsterNames = PlayerPrefs.GetString(slotPrefix + "MonsterNames");
                        string[] savedMonsters = savedMonsterNames.Split(',');
                        if (savedMonsters.Length == currentMonsters.Length)
                        {
                            // Check if monsters match
                            bool allMatch = true;
                            for (int i = 0; i < savedMonsters.Length; i++)
                            {
                                if (savedMonsters[i] != currentMonsters[i].name)
                                {
                                    allMatch = false;
                                    break;
                                }
                            }
                            if (allMatch)
                            {
                                isLegitimateChange = true; // Same monsters, might be restarting activity
                            }
                        }
                    }
                }
                
                if (!isLegitimateChange)
                {
                    Debug.LogError($"[AwayActivity] POTENTIAL DATA CORRUPTION: Attempting to save {currentActivity} for slot {activeSlot}, but slot's saved state is {savedActivity}. This might be stale data from a previous character. Aborting save.");
                    return;
                }
            }
        }
        
        // Verify we're saving to the correct slot by checking if the instance state matches what we're about to save
        // This prevents saving stale data from a previous character
        Debug.Log($"[AwayActivity] SaveAwayState called - ActiveCharacterSlot: {activeSlot}, Instance activity: {currentActivity}");
        
        // Use slot-specific keys to avoid overwriting other characters' activities
        Debug.Log($"[AwayActivity] Saving activity for slot {activeSlot}: {currentActivity} (instance state)");
        
        // Verify we're saving to the correct slot by double-checking ActiveCharacterSlot
        int verifySlot = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        if (verifySlot != activeSlot)
        {
            Debug.LogError($"[AwayActivity] Slot mismatch! activeSlot={activeSlot}, verifySlot={verifySlot}");
        }
        
        // If no active activity, save "None" activity with session start time
        if (currentActivity == AwayActivityType.None)
        {
            // Get the last session start time for this slot (or use current time if not set)
            DateTime sessionStartTime = GetLastSessionStartForSlot(activeSlot);
            
            // Save "None" activity with the last game session start time for this slot
            PlayerPrefs.SetInt(slotPrefix + "Type", (int)AwayActivityType.None);
            PlayerPrefs.SetString(slotPrefix + "StartTime", sessionStartTime.Ticks.ToString());
            PlayerPrefs.DeleteKey(slotPrefix + "ResourceName");
            PlayerPrefs.DeleteKey(slotPrefix + "MonsterNames");
            PlayerPrefs.DeleteKey(slotPrefix + "MobCount");
            PlayerPrefs.Save();
            Debug.Log($"[AwayActivity] Saved 'None' activity for slot {activeSlot}");
            return;
        }
        
        // Save activity type
        PlayerPrefs.SetInt(slotPrefix + "Type", (int)currentActivity);
        
        // Save start time as ticks (long)
        PlayerPrefs.SetString(slotPrefix + "StartTime", activityStartTime.Ticks.ToString());
        Debug.Log($"[AwayActivity] Saved start time {activityStartTime} for slot {activeSlot}");
        
        if (currentActivity == AwayActivityType.Mining && currentResource != null)
        {
            // Save resource name (we'll look it up by name when loading)
            PlayerPrefs.SetString(slotPrefix + "ResourceName", currentResource.name);
            Debug.Log($"[AwayActivity] Saved Mining activity for slot {activeSlot}: {currentResource.name} (ResourceName key: {slotPrefix}ResourceName)");
        }
        else if (currentActivity == AwayActivityType.Fighting && currentMonsters != null && currentMonsters.Length > 0)
        {
            // Save monster names (comma-separated) - save both asset name and display name
            string[] monsterNames = new string[currentMonsters.Length];
            string[] monsterDisplayNames = new string[currentMonsters.Length];
            for (int i = 0; i < currentMonsters.Length; i++)
            {
                monsterNames[i] = currentMonsters[i].name; // ScriptableObject asset name
                monsterDisplayNames[i] = currentMonsters[i].monsterName; // Display name
            }
            PlayerPrefs.SetString(slotPrefix + "MonsterNames", string.Join(",", monsterNames));
            PlayerPrefs.SetString(slotPrefix + "MonsterDisplayNames", string.Join(",", monsterDisplayNames));
            PlayerPrefs.SetInt(slotPrefix + "MobCount", mobCount);
            Debug.Log($"[AwayActivity] Saved Fighting activity for slot {activeSlot}: {monsterDisplayNames[0]} (MonsterNames key: {slotPrefix}MonsterNames)");
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
        // Use slot-specific keys
        string slotPrefix = $"AwayActivity_Slot_{characterSlotIndex}_";
        
        Debug.Log($"[AwayActivity] GetActivityDisplayString called for slot {characterSlotIndex}");
        
        // Check if activity type exists for this slot
        if (!PlayerPrefs.HasKey(slotPrefix + "Type"))
        {
            Debug.Log($"[AwayActivity] No activity type found for slot {characterSlotIndex}");
            return "";
        }
        
        int activityTypeInt = PlayerPrefs.GetInt(slotPrefix + "Type", 0);
        AwayActivityType savedActivity = (AwayActivityType)activityTypeInt;
        
        Debug.Log($"[AwayActivity] Found activity type {savedActivity} for slot {characterSlotIndex}");
        
        // Return readable activity string based on saved data
        if (savedActivity == AwayActivityType.Mining)
        {
            if (PlayerPrefs.HasKey(slotPrefix + "ResourceName"))
            {
                string resourceName = PlayerPrefs.GetString(slotPrefix + "ResourceName");
                Debug.Log($"[AwayActivity] Slot {characterSlotIndex} is mining resource: {resourceName}");
                ResourceData resource = FindResourceByName(resourceName);
                if (resource != null)
                {
                    return $"Currently Mining {resource.resourceName}";
                }
            }
        }
        else if (savedActivity == AwayActivityType.Fighting)
        {
            if (PlayerPrefs.HasKey(slotPrefix + "MonsterNames"))
            {
                string monsterNamesString = PlayerPrefs.GetString(slotPrefix + "MonsterNames");
                string[] monsterNames = monsterNamesString.Split(',');
                
                Debug.Log($"[AwayActivity] Slot {characterSlotIndex} is fighting monsters: {monsterNamesString}");
                
                // Try to get display names (fallback if ScriptableObject lookup fails)
                string displayName = "";
                if (PlayerPrefs.HasKey(slotPrefix + "MonsterDisplayNames"))
                {
                    string displayNamesString = PlayerPrefs.GetString(slotPrefix + "MonsterDisplayNames");
                    string[] displayNames = displayNamesString.Split(',');
                    if (displayNames.Length > 0)
                    {
                        displayName = displayNames[0];
                    }
                }
                
                if (monsterNames.Length > 0)
                {
                    // Try to find the monster ScriptableObject first
                    MonsterData monster = FindMonsterByName(monsterNames[0]);
                    string monsterDisplayName = "";
                    
                    if (monster != null)
                    {
                        monsterDisplayName = monster.monsterName;
                    }
                    else if (!string.IsNullOrEmpty(displayName))
                    {
                        // Fallback to saved display name if ScriptableObject not found
                        monsterDisplayName = displayName;
                    }
                    else
                    {
                        // Last resort: use the asset name
                        monsterDisplayName = monsterNames[0];
                    }
                    
                    if (!string.IsNullOrEmpty(monsterDisplayName))
                    {
                        if (monsterNames.Length == 1)
                        {
                            return $"Currently Fighting {monsterDisplayName}";
                        }
                        else
                        {
                            return $"Currently Fighting {monsterNames.Length} Monster Types";
                        }
                    }
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
            characterSlotIndex = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        }
        
        if (characterSlotIndex < 0)
        {
            return;
        }
        
        string slotPrefix = $"AwayActivity_Slot_{characterSlotIndex}_";
        PlayerPrefs.SetString(slotPrefix + "LastPlayed", DateTime.Now.Ticks.ToString());
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Get the last played time for a character slot
    /// Returns DateTime.MinValue if never played
    /// </summary>
    public DateTime GetLastPlayedTime(int characterSlotIndex)
    {
        string slotPrefix = $"AwayActivity_Slot_{characterSlotIndex}_";
        if (PlayerPrefs.HasKey(slotPrefix + "LastPlayed"))
        {
            string ticksString = PlayerPrefs.GetString(slotPrefix + "LastPlayed");
            if (long.TryParse(ticksString, out long ticks))
            {
                return new DateTime(ticks);
            }
        }
        return DateTime.MinValue;
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
        int activeSlot = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        if (activeSlot < 0)
        {
            return; // No active character
        }
        
        // Save session start time per character slot
        string slotPrefix = $"AwayActivity_Slot_{activeSlot}_";
        PlayerPrefs.SetString(slotPrefix + "LastSessionStart", DateTime.Now.Ticks.ToString());
        PlayerPrefs.Save();
        
        // Also update the instance variable for current use
        lastGameSessionStart = DateTime.Now;
    }
    
    /// <summary>
    /// Get the last game session start time for a specific character slot
    /// </summary>
    private DateTime GetLastSessionStartForSlot(int characterSlotIndex)
    {
        string slotPrefix = $"AwayActivity_Slot_{characterSlotIndex}_";
        if (PlayerPrefs.HasKey(slotPrefix + "LastSessionStart"))
        {
            string ticksString = PlayerPrefs.GetString(slotPrefix + "LastSessionStart");
            if (long.TryParse(ticksString, out long ticks))
            {
                return new DateTime(ticks);
            }
        }
        return DateTime.Now; // Default to now if not found
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
                characterSlotIndex = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
            }
            
            if (characterSlotIndex < 0)
            {
                Debug.LogWarning("[AwayActivity] LoadAwayState: No character slot index provided");
                return false;
            }
            
            Debug.Log($"[AwayActivity] Loading away state for slot {characterSlotIndex} (isLoadingState = true)");
            
            // Use slot-specific keys
            string slotPrefix = $"AwayActivity_Slot_{characterSlotIndex}_";
            
            if (!PlayerPrefs.HasKey(slotPrefix + "Type"))
            {
                Debug.Log($"[AwayActivity] No saved activity found for slot {characterSlotIndex}");
                return false;
            }
            
            int activityTypeInt = PlayerPrefs.GetInt(slotPrefix + "Type", 0);
            currentActivity = (AwayActivityType)activityTypeInt;
            
            Debug.Log($"[AwayActivity] Loaded activity type: {currentActivity} for slot {characterSlotIndex}");
            
            // Load start time
            if (PlayerPrefs.HasKey(slotPrefix + "StartTime"))
            {
                string ticksString = PlayerPrefs.GetString(slotPrefix + "StartTime");
                if (long.TryParse(ticksString, out long ticks))
                {
                    activityStartTime = new DateTime(ticks);
                    Debug.Log($"[AwayActivity] Loaded start time: {activityStartTime} for slot {characterSlotIndex}");
                }
                else
                {
                    // Invalid time, clear activity
                    Debug.LogError($"[AwayActivity] Invalid start time for slot {characterSlotIndex}");
                    StopActivity();
                    return false;
                }
            }
            else
            {
                // No start time, clear activity
                Debug.LogError($"[AwayActivity] No start time found for slot {characterSlotIndex}");
                StopActivity();
                return false;
            }
            
            // Handle "None" activity - no additional data needed
            if (currentActivity == AwayActivityType.None)
            {
                return true; // Return true so we can show "doing Nothing" rewards
            }
            
            // Load activity-specific data
            if (currentActivity == AwayActivityType.Mining)
            {
                if (PlayerPrefs.HasKey(slotPrefix + "ResourceName"))
                {
                    string resourceName = PlayerPrefs.GetString(slotPrefix + "ResourceName");
                    // Try to find the resource by name (we'll need to search ScriptableObjects)
                    currentResource = FindResourceByName(resourceName);
                    Debug.Log($"[AwayActivity] Loaded resource: {resourceName} for slot {characterSlotIndex}");
                    // Don't fail if resource not found - we can still calculate rewards with the name
                    // The display will use the saved name if ScriptableObject isn't found
                }
            }
            else if (currentActivity == AwayActivityType.Fighting)
            {
                if (PlayerPrefs.HasKey(slotPrefix + "MonsterNames"))
                {
                    string monsterNamesString = PlayerPrefs.GetString(slotPrefix + "MonsterNames");
                    string[] monsterNames = monsterNamesString.Split(',');
                    
                    Debug.Log($"[AwayActivity] Loading monsters: {monsterNamesString} for slot {characterSlotIndex}");
                    
                    // Try to load monsters, but don't fail if they're not found
                    // We have display names saved as fallback
                    List<MonsterData> loadedMonsters = new List<MonsterData>();
                    foreach (string monsterName in monsterNames)
                    {
                        MonsterData monster = FindMonsterByName(monsterName);
                        if (monster != null)
                        {
                            loadedMonsters.Add(monster);
                        }
                    }
                    
                    // If we found at least some monsters, use them
                    // Otherwise, set to null (we'll handle this in reward calculation)
                    if (loadedMonsters.Count > 0)
                    {
                        currentMonsters = loadedMonsters.ToArray();
                    }
                    else
                    {
                        // No monsters found - set to null
                        // Reward calculation will need to load from saved data
                        currentMonsters = null;
                    }
                    
                    mobCount = PlayerPrefs.GetInt(slotPrefix + "MobCount", 1);
                }
            }
            
            return true;
        }
        finally
        {
            // Always clear loading flag when done
            isLoadingState = false;
            Debug.Log($"[AwayActivity] Finished loading state for slot {characterSlotIndex}, isLoadingState = false");
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
    /// Find a MonsterData ScriptableObject by name
    /// </summary>
    private MonsterData FindMonsterByName(string monsterName)
    {
        Debug.Log($"[AwayActivity] Searching for monster: '{monsterName}'");
        
        // Load all MonsterData assets from Resources folder
        MonsterData[] allMonsters = Resources.LoadAll<MonsterData>("");
        Debug.Log($"[AwayActivity] Found {allMonsters.Length} monsters in Resources folder");
        
        // First try exact name match (ScriptableObject asset name)
        foreach (var monster in allMonsters)
        {
            if (monster.name == monsterName)
            {
                Debug.Log($"[AwayActivity] Found monster by asset name: {monster.name} -> {monster.monsterName}");
                return monster;
            }
        }
        
        // Then try monsterName field match (display name)
        foreach (var monster in allMonsters)
        {
            if (monster.monsterName == monsterName)
            {
                Debug.Log($"[AwayActivity] Found monster by display name: {monster.name} -> {monster.monsterName}");
                return monster;
            }
        }
        
        // Try case-insensitive match
        foreach (var monster in allMonsters)
        {
            if (monster.name.Equals(monsterName, System.StringComparison.OrdinalIgnoreCase) ||
                monster.monsterName.Equals(monsterName, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"[AwayActivity] Found monster by case-insensitive match: {monster.name} -> {monster.monsterName}");
                return monster;
            }
        }
        
        // Log all available monsters for debugging
        if (allMonsters.Length > 0)
        {
            Debug.LogWarning($"[AwayActivity] Available monsters: {string.Join(", ", System.Array.ConvertAll(allMonsters, m => $"{m.name}({m.monsterName})"))}");
        }
        else
        {
            Debug.LogError($"[AwayActivity] No monsters found in Resources folder! Make sure MonsterData assets are in a 'Resources' folder.");
        }
        
        Debug.LogError($"[AwayActivity] Failed to find monster: '{monsterName}'");
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
            characterSlotIndex = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        }
        
        if (characterSlotIndex < 0)
        {
            return;
        }
        
        // Clear slot-specific keys
        string slotPrefix = $"AwayActivity_Slot_{characterSlotIndex}_";
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

