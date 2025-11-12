using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterLoader : MonoBehaviour
{
    [Header("Settings")]
    public bool loadOnStart = true;
    
    private int currentSlotIndex = -1;
    
    void Start()
    {
        if (loadOnStart)
        {
            StartCoroutine(LoadCharacterAfterDelay());
        }
    }
    
    System.Collections.IEnumerator LoadCharacterAfterDelay()
    {
        yield return null;
        LoadActiveCharacter();
    }
    
    public void LoadActiveCharacter()
    {
        currentSlotIndex = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        
        if (currentSlotIndex < 0 || !SaveSystem.SaveFileExists(currentSlotIndex))
        {
            Debug.Log("[CharacterLoader] No active character to load");
            return;
        }
        
        // Check if we're in character selection scene (don't load if we are)
        CharacterSelectionManager charSelectManager = ComponentInjector.GetOrFind<CharacterSelectionManager>();
        if (charSelectManager != null)
        {
            return;
        }
        
        EnsureCharacterManagerExists();
        
        var characterService = ServiceMigrationHelper.GetCharacterService();
        if (characterService == null)
        {
            StartCoroutine(RetryLoadAfterDelay());
            return;
        }
        
        Debug.Log($"[CharacterLoader] Loading character from slot {currentSlotIndex}");
        SaveData saveData = SaveSystem.LoadCharacter(currentSlotIndex);
        
        if (saveData != null)
        {
            saveData.ApplyToGameState();
            PostLoadInitialization();
        }
        else
        {
            Debug.LogError($"[CharacterLoader] Failed to load character from slot {currentSlotIndex}");
        }
    }
    
    void EnsureCharacterManagerExists()
    {
        // Check if service is already registered
        if (Services.IsRegistered<ICharacterService>()) 
        {
            Debug.Log("[CharacterLoader] CharacterService already registered");
            return;
        }
        
        // Check if an existing manager exists but hasn't registered yet
        CharacterManager existingManager = ComponentInjector.GetOrFind<CharacterManager>();
        if (existingManager != null) 
        {
            Debug.Log("[CharacterLoader] Found existing CharacterManager, waiting for it to register");
            return;
        }
        
        // Create new manager if none exists
        Debug.Log("[CharacterLoader] Creating new CharacterManager");
        GameObject managerObj = new GameObject("CharacterManager");
        CharacterManager manager = managerObj.AddComponent<CharacterManager>();
    }
    
    System.Collections.IEnumerator RetryLoadAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        
        CharacterSelectionManager charSelectManager = ComponentInjector.GetOrFind<CharacterSelectionManager>();
        if (charSelectManager != null) yield break;
        
        var characterService = ServiceMigrationHelper.GetCharacterService();
        if (characterService == null)
        {
            Debug.LogError("[CharacterLoader] CharacterService still not available after retry");
            yield break;
        }
        
        SaveData saveData = SaveSystem.LoadCharacter(currentSlotIndex);
        if (saveData != null)
        {
            saveData.ApplyToGameState();
            PostLoadInitialization();
        }
    }
    
    private void PostLoadInitialization()
    {
        var characterService = ServiceMigrationHelper.GetCharacterService();
        if (characterService == null) return;
        
        Debug.Log($"[CharacterLoader] Initializing character from slot {currentSlotIndex}");
        
        var combatService = ServiceMigrationHelper.GetCombatService();
        if (combatService != null && combatService.GetCombatState() != CombatManager.CombatState.Idle)
        {
            combatService.EndCombat();
        }
        
        var resourceService = ServiceMigrationHelper.GetResourceService();
        resourceService?.StopGathering();
        
        var awayService = ServiceMigrationHelper.GetAwayActivityService();
        awayService?.StopActivity();
        
        CheckForAwayRewards();
        
        awayService?.MarkGameSessionStart();
        
        LoadCharacterZone();
    }
    
    private void LoadCharacterZone()
    {
        var zoneService = ServiceMigrationHelper.GetZoneService();
        if (zoneService == null) return;
        
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
    
    void CheckForAwayRewards()
    {
        var awayService = ServiceMigrationHelper.GetAwayActivityService();
        if (awayService == null) return;
        
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
            Debug.Log($"[CharacterLoader] Away time too short: {timeAway.TotalSeconds:F0}s (need 30s minimum)");
            awayService.ClearAwayState(currentSlotIndex);
            return;
        }
        
        // Cap maximum away time (e.g., 24 hours to prevent overflow)
        DateTime cappedStartTime = DateTime.Now.AddHours(-24);
        if (startTime < cappedStartTime)
        {
            startTime = cappedStartTime;
            Debug.Log($"[CharacterLoader] Capped away time to 24 hours maximum");
        }
        
        Debug.Log($"[CharacterLoader] Processing away rewards for {activity} ({timeAway.TotalMinutes:F0} minutes)");
        
        // Calculate rewards using the AwayRewardsCalculator
        AwayRewards rewards = AwayRewardsCalculator.CalculateRewards(
            startTime,
            activity,
            awayService.GetCurrentResource(),
            awayService.GetCurrentMonsters(),
            awayService.GetMobCount()
        );
        
        // Show rewards panel after a short delay to ensure scene is fully loaded
        StartCoroutine(ShowAwayRewardsPanelDelayed(rewards));
    }
    
    /// <summary>
    /// Show the away rewards panel after a short delay to ensure scene is loaded
    /// </summary>
    System.Collections.IEnumerator ShowAwayRewardsPanelDelayed(AwayRewards rewards)
    {
        // Wait a frame to ensure all GameObjects are initialized
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f); // Small delay to ensure scene is fully loaded
        
        ShowAwayRewardsPanel(rewards);
    }
    
    /// <summary>
    /// Show the away rewards panel with calculated rewards
    /// </summary>
    void ShowAwayRewardsPanel(AwayRewards rewards)
    {
        if (rewards == null)
        {
            Debug.LogWarning("[CharacterLoader] Cannot show away rewards panel - rewards are null");
            return;
        }
        
        Debug.Log($"[CharacterLoader] Searching for AwayRewardsPanel in scene...");
        
        // Find the AwayRewardsPanel in the scene (searches active and inactive objects)
        AwayRewardsPanel panel = ComponentInjector.GetOrFind<AwayRewardsPanel>();
        
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
            Debug.Log($"[CharacterLoader] ✓ Found AwayRewardsPanel on GameObject '{panel.gameObject.name}'");
            Debug.Log($"[CharacterLoader] Showing away rewards: {rewards.activityName}, Time: {FormatTimeSpan(rewards.timeAway)}, XP: {rewards.xpEarned}, Gold: {rewards.goldEarned}, Monsters: {rewards.monstersKilled}");
            panel.ShowRewards(rewards);
        }
        else
        {
            Debug.LogError("[CharacterLoader] ✗ AwayRewardsPanel component not found in scene!");
            Debug.LogError("[CharacterLoader] Make sure there's a GameObject with the AwayRewardsPanel component in the questingScene.");
            Debug.Log($"[Away Rewards] Activity: {rewards.activityName}");
            Debug.Log($"[Away Rewards] Time away: {FormatTimeSpan(rewards.timeAway)}");
            Debug.Log($"[Away Rewards] XP: {rewards.xpEarned}, Gold: {rewards.goldEarned}, Monsters Killed: {rewards.monstersKilled}");
            
            // Clear away state since we can't show the panel
            var awayService = ServiceMigrationHelper.GetAwayActivityService();
            awayService?.ClearAwayState(currentSlotIndex);
        }
    }
    
    /// <summary>
    /// Format a TimeSpan into a readable string
    /// </summary>
    string FormatTimeSpan(TimeSpan time)
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
    
    public void SaveCurrentCharacter()
    {
        var characterService = ServiceMigrationHelper.GetCharacterService();
        if (characterService == null) return;
        
        if (currentSlotIndex < 0)
        {
            currentSlotIndex = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        }
        
        if (currentSlotIndex < 0)
        {
            Debug.LogWarning("[CharacterLoader] Cannot save - no active character slot");
            return;
        }
        
        bool saved = SaveSystem.SaveCurrentCharacter(currentSlotIndex);
        
        if (saved)
        {
            Debug.Log($"[CharacterLoader] Saved character to slot {currentSlotIndex}");
        }
        else
        {
            Debug.LogError($"[CharacterLoader] Failed to save character to slot {currentSlotIndex}");
        }
    }
    
    public int GetCurrentSlotIndex() => currentSlotIndex;
}
