using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages character session lifecycle: loading, saving, initialization, and cleanup.
/// Centralizes all character session logic that was previously scattered across CharacterLoader and ReturnToCharacterSelectButton.
/// </summary>
public class CharacterSessionManager : MonoBehaviour, ICharacterSessionService
{
    private int currentSlotIndex = -1;
    
    void Awake()
    {
        // Prevent duplicates - Bootstrap creates all managers
        if (Services.IsRegistered<ICharacterSessionService>())
        {
            Debug.LogWarning("[CharacterSessionManager] Service already registered. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject); // Persist between scenes
        
        Services.Register<ICharacterSessionService>(this);
        
        Debug.Log("[CharacterSessionManager] Service registered and ready");
    }
    
    void OnDestroy()
    {
        Services.Unregister<ICharacterSessionService>();
    }
    
    /// <summary>
    /// Load a character from a save slot and initialize all services
    /// </summary>
    public bool LoadCharacter(int slotIndex)
    {
        currentSlotIndex = slotIndex;
        
        if (currentSlotIndex < 0 || !SaveSystem.SaveFileExists(currentSlotIndex))
        {
            Debug.Log("[CharacterSessionManager] No character to load in slot " + currentSlotIndex);
            return false;
        }
        
        // Check if we're in character selection scene (don't load if we are)
        CharacterSelectionScreen charSelectScreen = ComponentInjector.FindComponent<CharacterSelectionScreen>();
        if (charSelectScreen != null)
        {
            Debug.Log("[CharacterSessionManager] Skipping load - in character selection scene");
            return false;
        }
        
        // Ensure CharacterService exists
        if (!Services.IsRegistered<ICharacterService>())
        {
            Debug.LogError("[CharacterSessionManager] CharacterService not found! Bootstrap scene must run first.");
            return false;
        }
        
        Debug.Log($"[CharacterSessionManager] Loading character from slot {currentSlotIndex}");
        
        // Clear existing talent data before loading
        if (Services.TryGet<ITalentService>(out var talentService))
        {
            talentService.ClearAllTalents();
        }
        
        SaveData saveData = SaveSystem.LoadCharacter(currentSlotIndex);
        
        if (saveData != null)
        {
            saveData.ApplyToGameState();
            PostLoadInitialization();
            return true;
        }
        else
        {
            Debug.LogError($"[CharacterSessionManager] Failed to load character from slot {currentSlotIndex}");
            return false;
        }
    }
    
    /// <summary>
    /// Initialize services after character is loaded
    /// </summary>
    private void PostLoadInitialization()
    {
        if (!Services.TryGet<ICharacterService>(out var characterService))
        {
            Debug.LogError("[CharacterSessionManager] CharacterService not available for initialization");
            return;
        }
        
        // Publish character loaded event so UI can refresh
        CharacterData charData = characterService.GetCharacterData();
        EventBus.Publish(new CharacterLoadedEvent 
        { 
            slotIndex = currentSlotIndex,
            characterName = charData.characterName
        });
        
        Debug.Log($"[CharacterSessionManager] Character loaded: {charData.characterName} (Slot {currentSlotIndex})");
        
        Debug.Log($"[CharacterSessionManager] Initializing character from slot {currentSlotIndex}");
        
        // Refresh GameLog subscription to new character's events
        if (Services.TryGet<IGameLogService>(out var gameLogService) && gameLogService is GameLog gameLog)
        {
            gameLog.RefreshCharacterSubscription();
        }
        
        // Clear combat state if active
        if (Services.TryGet<ICombatService>(out var combatService) && combatService.GetCombatState() != CombatManager.CombatState.Idle)
        {
            combatService.EndCombat();
        }
        
        // Stop any gathering
        if (Services.TryGet<IResourceService>(out var resourceService))
        {
            resourceService.StopGathering();
        }
        
        // Handle away activity and rewards
        if (Services.TryGet<IAwayActivityService>(out var awayService))
        {
            awayService.StopActivity();
            CheckForAwayRewards();
            awayService.MarkGameSessionStart();
        }
        else
        {
            CheckForAwayRewards();
        }
        
        LoadCharacterZone();
    }
    
    /// <summary>
    /// Load the character's saved zone
    /// </summary>
    private void LoadCharacterZone()
    {
        if (!Services.TryGet<IZoneService>(out var zoneService)) return;
        
        SaveData saveData = SaveSystem.LoadCharacter(currentSlotIndex);
        if (saveData != null && saveData.currentZoneIndex >= 0)
        {
            zoneService.LoadCurrentZone();
        }
        else
        {
            zoneService.SetDefaultZoneForSlot(currentSlotIndex);
            zoneService.LoadCurrentZone();
        }
    }
    
    /// <summary>
    /// Check for and calculate away rewards
    /// </summary>
    private void CheckForAwayRewards()
    {
        if (!Services.TryGet<IAwayActivityService>(out var awayService)) return;
        
        // Load saved away activity state
        bool hadAwayActivity = awayService.LoadAwayState(currentSlotIndex);
        if (!hadAwayActivity) return;
        
        // Get activity details
        AwayActivityType activity = awayService.GetCurrentActivity();
        DateTime startTime = awayService.GetActivityStartTime();
        TimeSpan timeAway = DateTime.Now - startTime;
        
        // Need at least 30 seconds away to grant rewards (prevent exploits)
        if (timeAway.TotalSeconds < 30)
        {
            Debug.Log($"[CharacterSessionManager] Away time too short: {timeAway.TotalSeconds:F0}s (need 30s minimum)");
            awayService.ClearAwayState(currentSlotIndex);
            return;
        }
        
        // Cap maximum away time (e.g., 24 hours to prevent overflow)
        DateTime cappedStartTime = DateTime.Now.AddHours(-24);
        if (startTime < cappedStartTime)
        {
            startTime = cappedStartTime;
            Debug.Log($"[CharacterSessionManager] Capped away time to 24 hours maximum");
        }
        
        Debug.Log($"[CharacterSessionManager] Processing away rewards for {activity} ({timeAway.TotalMinutes:F0} minutes)");
        
        // Calculate rewards using the AwayRewardsCalculator
        AwayRewards rewards = AwayRewardsCalculator.CalculateRewards(
            startTime,
            activity,
            awayService.GetCurrentResource(),
            awayService.GetCurrentMonsters(),
            awayService.GetMobCount()
        );
        
        // Fallback: If activity name wasn't set, use a generic name based on activity type
        if (string.IsNullOrEmpty(rewards.activityName))
        {
            if (activity == AwayActivityType.Mining)
            {
                rewards.activityName = "Gathering";
            }
            else if (activity == AwayActivityType.Fighting)
            {
                string monsterName = awayService.GetMonsterDisplayNameFromPlayerPrefs(currentSlotIndex);
                if (!string.IsNullOrEmpty(monsterName))
                {
                    rewards.activityName = $"Fighting {monsterName}";
                }
                else
                {
                    rewards.activityName = "Fighting";
                }
            }
            else
            {
                rewards.activityName = "doing Nothing";
            }
        }
        
        // Request to show rewards panel (will be handled by CharacterLoader with coroutine)
        CharacterLoader loader = ComponentInjector.FindComponent<CharacterLoader>();
        if (loader != null)
        {
            RequestShowAwayRewardsPanel(rewards, loader);
        }
        else
        {
            Debug.LogWarning("[CharacterSessionManager] Cannot show away rewards - CharacterLoader not found");
        }
    }
    
    /// <summary>
    /// Request showing the away rewards panel (requires coroutine support from MonoBehaviour)
    /// </summary>
    public void RequestShowAwayRewardsPanel(AwayRewards rewards, MonoBehaviour coroutineRunner)
    {
        if (coroutineRunner != null)
        {
            coroutineRunner.StartCoroutine(ShowAwayRewardsPanelDelayed(rewards));
        }
    }
    
    /// <summary>
    /// Show the away rewards panel after a short delay to ensure scene is loaded
    /// </summary>
    private IEnumerator ShowAwayRewardsPanelDelayed(AwayRewards rewards)
    {
        // Wait a frame to ensure all GameObjects are initialized
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f); // Small delay to ensure scene is fully loaded
        
        ShowAwayRewardsPanel(rewards);
    }
    
    /// <summary>
    /// Show the away rewards panel with calculated rewards
    /// </summary>
    private void ShowAwayRewardsPanel(AwayRewards rewards)
    {
        if (rewards == null)
        {
            Debug.LogWarning("[CharacterSessionManager] Cannot show away rewards panel - rewards are null");
            return;
        }
        
        Debug.Log($"[CharacterSessionManager] Searching for AwayRewardsPanel in scene...");
        
        // Find the AwayRewardsPanel in the scene
        AwayRewardsPanel panel = ComponentInjector.FindComponent<AwayRewardsPanel>();
        
        // If not found, try searching inactive objects
        if (panel == null)
        {
            AwayRewardsPanel[] allPanels = Resources.FindObjectsOfTypeAll<AwayRewardsPanel>();
            if (allPanels != null && allPanels.Length > 0)
            {
                panel = allPanels[0];
            }
        }
        
        if (panel != null)
        {
            Debug.Log($"[CharacterSessionManager] ✓ Found AwayRewardsPanel on GameObject '{panel.gameObject.name}'");
            Debug.Log($"[CharacterSessionManager] Showing away rewards: {rewards.activityName}, Time: {FormatTimeSpan(rewards.timeAway)}, XP: {rewards.xpEarned}, Gold: {rewards.goldEarned}, Monsters: {rewards.monstersKilled}");
            panel.ShowRewards(rewards);
        }
        else
        {
            Debug.LogError("[CharacterSessionManager] ✗ AwayRewardsPanel component not found in scene!");
            Debug.Log($"[Away Rewards] Activity: {rewards.activityName}");
            Debug.Log($"[Away Rewards] Time away: {FormatTimeSpan(rewards.timeAway)}");
            Debug.Log($"[Away Rewards] XP: {rewards.xpEarned}, Gold: {rewards.goldEarned}, Monsters Killed: {rewards.monstersKilled}");
            
            // Clear away state since we can't show the panel
            if (Services.TryGet<IAwayActivityService>(out var awayService))
            {
                awayService.ClearAwayState(currentSlotIndex);
            }
        }
    }
    
    /// <summary>
    /// Format a TimeSpan into a readable string
    /// </summary>
    private string FormatTimeSpan(TimeSpan time)
    {
        if (time.TotalDays >= 1)
        {
            return $"{(int)time.TotalDays}d {time.Hours}h";
        }
        else if (time.TotalHours >= 1)
        {
            return $"{time.Hours}h {time.Minutes}m";
        }
        else if (time.TotalMinutes >= 1)
        {
            return $"{time.Minutes}m {time.Seconds}s";
        }
        else
        {
            return $"{time.Seconds}s";
        }
    }
    
    /// <summary>
    /// Save current character without exiting
    /// </summary>
    public bool SaveCurrentCharacter(int slotIndex)
    {
        if (!Services.TryGet<ICharacterService>(out var characterService))
        {
            Debug.LogWarning("[CharacterSessionManager] Cannot save - CharacterService not found");
            return false;
        }
        
        if (slotIndex < 0)
        {
            Debug.LogWarning("[CharacterSessionManager] Cannot save - invalid slot index");
            return false;
        }
        
        bool saved = SaveSystem.SaveCurrentCharacter(slotIndex);
        
        if (saved)
        {
            Debug.Log($"[CharacterSessionManager] Saved character to slot {slotIndex}");
            currentSlotIndex = slotIndex;
        }
        else
        {
            Debug.LogError($"[CharacterSessionManager] Failed to save character to slot {slotIndex}");
        }
        
        return saved;
    }
    
    /// <summary>
    /// Save current character and exit to character selection screen
    /// </summary>
    public void SaveAndExitToCharacterSelection(int slotIndex, string characterSceneName)
    {
        Debug.Log($"[CharacterSessionManager] Saving and exiting to character selection (slot {slotIndex})");
        
        // 1. Save current character state to disk (JSON) FIRST
        // This ensures we capture the active state (fighting/mining) before we stop it
        if (Services.TryGet<ICharacterService>(out var characterService))
        {
            SaveCurrentCharacter(slotIndex);
        }
        
        // 2. Save away activity state to PlayerPrefs (for Character Selection UI)
        if (Services.TryGet<IAwayActivityService>(out var awayActivityService))
        {
            awayActivityService.SaveAwayState();
            
            // Save last played time for this character
            if (slotIndex >= 0)
            {
                awayActivityService.SaveLastPlayedTime(slotIndex);
            }
        }
        
        // 3. Save current zone per character slot
        if (Services.TryGet<IZoneService>(out var zoneService))
        {
            if (slotIndex >= 0)
            {
                int zoneIndex = zoneService.GetCurrentZoneIndex();
                PlayerPrefs.SetInt($"Character_{slotIndex}_ZoneIndex", zoneIndex);
                PlayerPrefs.Save();
            }
        }
        
        // 4. Clear active character slot to prevent auto-save on quit
        // This "disconnects" the session so CharacterManager.OnApplicationQuit won't overwrite our save
        PlayerPrefs.SetInt("ActiveCharacterSlot", -1);
        PlayerPrefs.Save();
        Debug.Log("[CharacterSessionManager] Cleared active character slot to prevent overwrite on quit");
        
        // 5. Now it's safe to stop activities
        
        // Stop gathering
        if (Services.TryGet<IResourceService>(out var resourceService))
        {
            resourceService.StopGathering();
        }
        
        // Prepare managers for character switch (reset state, but keep managers alive)
        PrepareForCharacterSwitch();
        
        // Load character selection scene
        SceneManager.LoadScene(characterSceneName);
    }
    
    /// <summary>
    /// Reset character-specific state in managers without destroying them.
    /// Managers are created by Bootstrap and persist for the entire game session.
    /// </summary>
    private void PrepareForCharacterSwitch()
    {
        Debug.Log("[CharacterSessionManager] Preparing managers for character switch (resetting state)");
        
        // Stop any active combat
        if (Services.TryGet<ICombatService>(out var combatService))
        {
            combatService.EndCombat();
        }
        
        // Stop any gathering activity
        if (Services.TryGet<IResourceService>(out var resourceService))
        {
            resourceService.StopGathering();
        }
        
        // Stop away activity tracking (already saved above)
        if (Services.TryGet<IAwayActivityService>(out var awayActivityService))
        {
            awayActivityService.StopActivity();
        }
        
        // Clear talent data
        if (Services.TryGet<ITalentService>(out var talentService))
        {
            talentService.ClearAllTalents();
        }
        
        Debug.Log("[CharacterSessionManager] ✓ State reset complete - managers remain active for next character");
    }
    
    /// <summary>
    /// Get the currently active character slot index
    /// </summary>
    public int GetCurrentSlotIndex()
    {
        return currentSlotIndex;
    }
}

