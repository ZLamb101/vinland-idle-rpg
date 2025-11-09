using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads the active character data into CharacterManager when entering the game scene.
/// Place this in your main game scene.
/// </summary>
public class CharacterLoader : MonoBehaviour
{
    [Header("Settings")]
    public bool loadOnStart = true;
    
    private string currentRace;
    private string currentClass;
    private int currentSlotIndex;
    
    void Start()
    {
        if (loadOnStart)
        {
            // Use coroutine to ensure CharacterManager has time to initialize
            StartCoroutine(LoadCharacterAfterDelay());
        }
    }
    
    System.Collections.IEnumerator LoadCharacterAfterDelay()
    {
        // Wait one frame to ensure all Awake() methods have run
        yield return null;
        
        LoadActiveCharacter();
    }
    
    // Removed auto-save on level up - will save only when returning to character screen
    
    void EnsureCharacterManagerExists()
    {
        // Check if CharacterManager already exists
        if (CharacterManager.Instance != null)
        {
            return;
        }
        
        // Try to find it in the scene first
        CharacterManager existingManager = FindAnyObjectByType<CharacterManager>();
        if (existingManager != null)
        {
            // It exists but Instance might not be set yet (Awake hasn't run)
            return;
        }
        
        // Create CharacterManager if it doesn't exist (we're in a game scene)
        GameObject managerObj = new GameObject("CharacterManager");
        CharacterManager manager = managerObj.AddComponent<CharacterManager>();
        
        // Give it a moment to initialize
        // The Instance will be set in Awake(), which should run immediately after AddComponent
    }
    
    public void LoadActiveCharacter()
    {
        // Check if there's an active character to load
        if (!PlayerPrefs.HasKey("ActiveCharacter"))
        {
            // This is normal on the character selection screen - no character selected yet
            return;
        }
        
        // Get slot index FIRST before loading (important for saving later)
        currentSlotIndex = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        
        // Check if we're on the character selection screen - if so, don't load here
        CharacterSelectionManager charSelectManager = FindAnyObjectByType<CharacterSelectionManager>();
        if (charSelectManager != null)
        {
            return;
        }
        
        // We're in a game scene - ensure CharacterManager exists
        EnsureCharacterManagerExists();
        
        // Wait a moment for CharacterManager to initialize if it was just created
        if (CharacterManager.Instance == null)
        {
            StartCoroutine(RetryLoadAfterDelay());
            return;
        }
        
        // Load character data
        string json = PlayerPrefs.GetString("ActiveCharacter");
        SavedCharacterData savedData = JsonUtility.FromJson<SavedCharacterData>(json);
        
        currentRace = PlayerPrefs.GetString("ActiveCharacterRace", "Human");
        currentClass = PlayerPrefs.GetString("ActiveCharacterClass", "Warrior");
        
        // Load into CharacterManager
        CharacterData gameData = new CharacterData();
        savedData.LoadInto(gameData);
        
        // Ensure race/class are set (use saved data, fallback to PlayerPrefs)
        if (string.IsNullOrEmpty(gameData.race))
        {
            gameData.race = !string.IsNullOrEmpty(savedData.race) ? savedData.race : currentRace;
        }
        if (string.IsNullOrEmpty(gameData.characterClass))
        {
            gameData.characterClass = !string.IsNullOrEmpty(savedData.characterClass) ? savedData.characterClass : currentClass;
        }
        
        // Update currentRace/currentClass from loaded data for saving later
        currentRace = gameData.race;
        currentClass = gameData.characterClass;
        
        // Ensure name is not empty or default - use saved name directly
        if (string.IsNullOrEmpty(gameData.characterName))
        {
            if (!string.IsNullOrEmpty(savedData.characterName))
            {
                gameData.characterName = savedData.characterName;
            }
        }
        
        CharacterManager.Instance.LoadCharacterData(gameData);
        
        // Verify name was set correctly
        string finalName = CharacterManager.Instance.GetName();
        
        // Always ensure the name matches what was saved
        if (!string.IsNullOrEmpty(savedData.characterName) && finalName != savedData.characterName)
        {
            CharacterManager.Instance.SetName(savedData.characterName);
        }
        
        // Ensure combat and activity are properly reset when entering game scene
        // Clear any leftover state from previous character/session
        // IMPORTANT: Do this BEFORE loading character data to ensure ActiveCharacterSlot is correct
        Debug.Log($"[CharacterLoader] Loading character - ActiveCharacterSlot is currently: {PlayerPrefs.GetInt("ActiveCharacterSlot", -1)}");
        
        if (CombatManager.Instance != null)
        {
            // End any existing combat from previous character/session
            // This will trigger OnCombatStateChanged event which will hide the combat panel
            if (CombatManager.Instance.GetCombatState() != CombatManager.CombatState.Idle)
            {
                Debug.Log("[CharacterLoader] Ending combat from previous character");
                CombatManager.Instance.EndCombat();
            }
            // Note: visualManager reference is cleared in EndCombat() and will be re-found when combat starts
        }
        
        // Stop any gathering from previous character
        if (ResourceManager.Instance != null)
        {
            Debug.Log($"[CharacterLoader] Stopping gathering from previous character");
            ResourceManager.Instance.StopGathering();
        }
        
        // Clear any leftover activity state from previous character
        // The activity for THIS character will be loaded in CheckForAwayRewards()
        if (AwayActivityManager.Instance != null)
        {
            Debug.Log($"[CharacterLoader] Clearing activity state before loading character {currentSlotIndex}");
            AwayActivityManager.Instance.StopActivity();
        }
        
        // Check for away rewards after character is loaded
        // This will load the saved activity state for THIS character
        // IMPORTANT: Do this BEFORE marking session start, so we don't overwrite the saved start time
        CheckForAwayRewards();
        
        // Mark new game session start AFTER loading away state
        // This ensures we have the correct session start time for "doing Nothing" tracking
        if (AwayActivityManager.Instance != null)
        {
            AwayActivityManager.Instance.MarkGameSessionStart();
            
            // If no saved activity was loaded, ensure we start with "None" activity
            if (AwayActivityManager.Instance.GetCurrentActivity() == AwayActivityType.None)
            {
                // This is fine - "None" activity will be saved when leaving
            }
        }
        
        // Load the zone for this character after character is loaded
        // This ensures ActiveCharacterSlot is set correctly
        if (ZoneManager.Instance != null)
        {
            // Check if character has a saved zone, if not set default to zone 1-1
            int savedZoneIndex = -1;
            if (currentSlotIndex >= 0 && PlayerPrefs.HasKey($"Character_{currentSlotIndex}_ZoneIndex"))
            {
                savedZoneIndex = PlayerPrefs.GetInt($"Character_{currentSlotIndex}_ZoneIndex", -1);
            }
            
            // If no saved zone, set default zone 1-1
            if (savedZoneIndex < 0)
            {
                ZoneManager.Instance.SetDefaultZoneForSlot(currentSlotIndex);
            }
            
            ZoneManager.Instance.LoadCurrentZone();
        }
    }
    
    /// <summary>
    /// Check if player was away and calculate rewards
    /// </summary>
    void CheckForAwayRewards()
    {
        // Ensure AwayActivityManager exists
        if (AwayActivityManager.Instance == null)
        {
            GameObject managerObj = new GameObject("AwayActivityManager");
            managerObj.AddComponent<AwayActivityManager>();
        }
        
        // Load away state for the current character slot
        int currentSlotIndex = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        Debug.Log($"[AwayRewards] Checking rewards for slot {currentSlotIndex}");
        
        bool hadAwayActivity = AwayActivityManager.Instance.LoadAwayState(currentSlotIndex);
        
        if (!hadAwayActivity)
        {
            Debug.Log($"[AwayRewards] No away activity found for slot {currentSlotIndex}");
            return; // No away activity to process
        }
        
        AwayActivityType activityType = AwayActivityManager.Instance.GetCurrentActivity();
        DateTime activityStartTime = AwayActivityManager.Instance.GetActivityStartTime();
        
        Debug.Log($"[AwayRewards] Found activity for slot {currentSlotIndex}: {activityType}, started at: {activityStartTime}, time away: {DateTime.Now - activityStartTime}");
        
        // Calculate rewards
        AwayRewards rewards = null;
        
        if (activityType == AwayActivityType.Mining)
        {
            ResourceData resource = AwayActivityManager.Instance.GetCurrentResource();
            if (resource != null)
            {
                rewards = AwayRewardsCalculator.CalculateRewards(activityStartTime, activityType, resource);
            }
        }
        else if (activityType == AwayActivityType.Fighting)
        {
            MonsterData[] monsters = AwayActivityManager.Instance.GetCurrentMonsters();
            int mobCount = AwayActivityManager.Instance.GetMobCount();
            
            Debug.Log($"[AwayRewards] Fighting activity - Loaded monsters: {(monsters != null ? monsters.Length : 0)}, Mob count: {mobCount}");
            
            // Even if monsters array is null/empty (ScriptableObjects not found),
            // try to load from saved names as fallback
            if (monsters == null || monsters.Length == 0)
            {
                monsters = AwayActivityManager.Instance.TryLoadMonstersFromSavedNames(currentSlotIndex);
                if (monsters.Length > 0)
                {
                    Debug.Log($"[AwayRewards] Successfully loaded {monsters.Length} monster(s) from saved names");
                }
            }
            
            if (monsters != null && monsters.Length > 0)
            {
                Debug.Log($"[AwayRewards] Calculating fighting rewards - Monsters: {monsters.Length}, Mob count: {mobCount}, Start time: {activityStartTime}");
                rewards = AwayRewardsCalculator.CalculateRewards(activityStartTime, activityType, null, monsters, mobCount);
                Debug.Log($"[AwayRewards] Calculated rewards - XP: {rewards?.xpEarned ?? 0}, Gold: {rewards?.goldEarned ?? 0}, Monsters killed: {rewards?.monstersKilled ?? 0}, Items: {rewards?.itemsDropped?.Count ?? 0}");
            }
            else
            {
                // Can't calculate rewards without monster data, but still show panel with correct name
                // Get monster display name from saved data
                string monsterDisplayName = AwayActivityManager.Instance.GetMonsterDisplayNameFromPlayerPrefs(currentSlotIndex);
                if (string.IsNullOrEmpty(monsterDisplayName))
                {
                    monsterDisplayName = "Unknown Monster";
                }
                
                rewards = new AwayRewards
                {
                    activityType = activityType,
                    timeAway = DateTime.Now - activityStartTime,
                    activityName = $"Fighting {monsterDisplayName}"
                };
            }
        }
        else if (activityType == AwayActivityType.None)
        {
            // Calculate rewards for "doing Nothing"
            rewards = AwayRewardsCalculator.CalculateRewards(activityStartTime, activityType);
        }
        
        // Show rewards panel only if player was away for at least 60 seconds
        if (rewards != null && rewards.timeAway.TotalSeconds >= 60)
        {
            Debug.Log($"[AwayRewards] Showing panel - Time away: {rewards.timeAway.TotalSeconds} seconds");
            
            // Find existing AwayRewardsPanel (including inactive ones)
            AwayRewardsPanel rewardsPanel = FindFirstObjectByType<AwayRewardsPanel>(FindObjectsInactive.Include);
            
            if (rewardsPanel == null)
            {
                Debug.LogWarning("[AwayRewards] No AwayRewardsPanel found in scene. Please add one to the scene or it will be created programmatically.");
                // Create the panel if it doesn't exist (fallback)
                // Try to find a Canvas to parent it to
                Canvas canvas = FindAnyObjectByType<Canvas>();
                GameObject panelObj = new GameObject("AwayRewardsPanel");
                
                if (canvas != null)
                {
                    panelObj.transform.SetParent(canvas.transform, false);
                    Debug.Log("[AwayRewards] Created new AwayRewardsPanel and parented to Canvas");
                }
                else
                {
                    Debug.LogWarning("[AwayRewards] No Canvas found - panel may not be visible");
                }
                
                rewardsPanel = panelObj.AddComponent<AwayRewardsPanel>();
            }
            else
            {
                Debug.Log("[AwayRewards] Found existing AwayRewardsPanel in scene");
            }
            
            if (rewardsPanel != null)
            {
                Debug.Log("[AwayRewards] Calling ShowRewards");
                rewardsPanel.ShowRewards(rewards);
            }
            else
            {
                Debug.LogError("[AwayRewards] Failed to create or find AwayRewardsPanel");
            }
        }
        else
        {
            if (rewards == null)
            {
                Debug.Log("[AwayRewards] Rewards is null");
            }
            else
            {
                Debug.Log($"[AwayRewards] Time away too short: {rewards.timeAway.TotalSeconds} seconds (need 60+)");
            }
            
            // Clear away state if not away long enough or no valid rewards
            AwayActivityManager.Instance.ClearAwayState(currentSlotIndex);
        }
        
        // Mark new game session start for next time
        if (AwayActivityManager.Instance != null)
        {
            AwayActivityManager.Instance.MarkGameSessionStart();
        }
    }
    
    System.Collections.IEnumerator RetryLoadAfterDelay()
    {
        // Wait a bit longer for CharacterManager to initialize
        yield return new WaitForSeconds(0.2f);
        
        // Double-check we're not on character selection screen
        CharacterSelectionManager charSelectManager = FindAnyObjectByType<CharacterSelectionManager>();
        if (charSelectManager != null)
        {
            yield break;
        }
        
        if (CharacterManager.Instance != null)
        {
            // Retry loading
            string json = PlayerPrefs.GetString("ActiveCharacter");
            SavedCharacterData savedData = JsonUtility.FromJson<SavedCharacterData>(json);
            
            currentRace = PlayerPrefs.GetString("ActiveCharacterRace", "Human");
            currentClass = PlayerPrefs.GetString("ActiveCharacterClass", "Warrior");
            currentSlotIndex = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
            
            CharacterData gameData = new CharacterData();
            savedData.LoadInto(gameData);
            
            // Ensure race/class are set (use saved data, fallback to PlayerPrefs)
            if (string.IsNullOrEmpty(gameData.race))
            {
                gameData.race = !string.IsNullOrEmpty(savedData.race) ? savedData.race : currentRace;
            }
            if (string.IsNullOrEmpty(gameData.characterClass))
            {
                gameData.characterClass = !string.IsNullOrEmpty(savedData.characterClass) ? savedData.characterClass : currentClass;
            }
            
            // Update currentRace/currentClass from loaded data
            currentRace = gameData.race;
            currentClass = gameData.characterClass;
            
            // Ensure name is not empty or default
            if (string.IsNullOrEmpty(gameData.characterName))
            {
                if (!string.IsNullOrEmpty(savedData.characterName))
                {
                    gameData.characterName = savedData.characterName;
                }
            }
            
            CharacterManager.Instance.LoadCharacterData(gameData);
            
            // Verify name was set correctly
            string finalName = CharacterManager.Instance.GetName();
            
            if (finalName != savedData.characterName && !string.IsNullOrEmpty(savedData.characterName))
            {
                CharacterManager.Instance.SetName(savedData.characterName);
            }
            
            // Check for away rewards after character is loaded
            CheckForAwayRewards();
            
            // Load the zone for this character after character is loaded
            if (ZoneManager.Instance != null)
            {
                // Check if character has a saved zone, if not set default to zone 1-1
                int savedZoneIndex = -1;
                if (currentSlotIndex >= 0 && PlayerPrefs.HasKey($"Character_{currentSlotIndex}_ZoneIndex"))
                {
                    savedZoneIndex = PlayerPrefs.GetInt($"Character_{currentSlotIndex}_ZoneIndex", -1);
                }
                
                // If no saved zone, set default zone 1-1
                if (savedZoneIndex < 0)
                {
                    ZoneManager.Instance.SetDefaultZoneForSlot(currentSlotIndex);
                }
                
                ZoneManager.Instance.LoadCurrentZone();
            }
        }
    }
    
    // Call this before returning to character select to save progress
    void OnApplicationQuit()
    {
        // Save away activity state
        if (AwayActivityManager.Instance != null)
        {
            AwayActivityManager.Instance.SaveAwayState();
        }
        
        // Save current zone before quitting
        if (ZoneManager.Instance != null)
        {
            int currentSlot = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
            if (currentSlot >= 0)
            {
                int zoneIndex = ZoneManager.Instance.GetCurrentZoneIndex();
                PlayerPrefs.SetInt($"Character_{currentSlot}_ZoneIndex", zoneIndex);
                PlayerPrefs.Save();
            }
        }
        
        SaveCurrentCharacter();
    }
    
    void OnDestroy()
    {
        // Don't save away activity state here - it's saved when:
        // 1. Returning to character select (ReturnToCharacterSelect)
        // 2. Application quits (OnApplicationQuit)
        // 3. Starting/stopping activities (ResourceManager, CombatManager)
        // Saving here causes issues when entering the world because the instance state
        // might be stale from a previous character
        
        // Only save character data if we're in a game scene (not character select)
        // CharacterManager only exists in game scenes
        if (CharacterManager.Instance != null)
        {
            // Save current zone before leaving scene
            if (ZoneManager.Instance != null)
            {
                int currentSlot = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
                if (currentSlot >= 0)
                {
                    int zoneIndex = ZoneManager.Instance.GetCurrentZoneIndex();
                    PlayerPrefs.SetInt($"Character_{currentSlot}_ZoneIndex", zoneIndex);
                    PlayerPrefs.Save();
                }
            }
            
            SaveCurrentCharacter();
        }
    }
    
    public void SaveCurrentCharacter()
    {
        if (CharacterManager.Instance == null)
        {
            // This is normal when returning to character select - CharacterManager doesn't exist there
            return;
        }
        
        // Get slot index from PlayerPrefs (in case it wasn't set during load)
        if (currentSlotIndex < 0)
        {
            currentSlotIndex = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        }
        
        if (currentSlotIndex < 0)
        {
            return;
        }
        
        // Get current character data from CharacterManager
        CharacterData currentData = CharacterManager.Instance.GetCharacterData();
        
        // Get race/class from CharacterData (preferred) or fallback to stored values
        string saveRace = !string.IsNullOrEmpty(currentData.race) ? currentData.race : currentRace;
        string saveClass = !string.IsNullOrEmpty(currentData.characterClass) ? currentData.characterClass : currentClass;
        
        // Update stored values for next time
        currentRace = saveRace;
        currentClass = saveClass;
        
        // Create saved character data
        SavedCharacterData savedData = new SavedCharacterData();
        savedData.SaveFrom(currentData, saveRace, saveClass);
        
        // Save back to PlayerPrefs
        string key = $"Character_{currentSlotIndex}";
        string json = JsonUtility.ToJson(savedData);
        PlayerPrefs.SetString(key, json);
        
        // Also update active character (only if this is still the active character)
        int activeSlot = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        if (activeSlot == currentSlotIndex)
        {
            PlayerPrefs.SetString("ActiveCharacter", json);
        }
        
        PlayerPrefs.Save();
    }
    
    public string GetCurrentRace() => currentRace;
    public string GetCurrentClass() => currentClass;
}

