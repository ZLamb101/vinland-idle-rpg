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
        
        CharacterSelectionManager charSelectManager = FindAnyObjectByType<CharacterSelectionManager>();
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
        if (Services.IsRegistered<ICharacterService>()) return;
        if (CharacterManager.Instance != null) return;
        
        CharacterManager existingManager = FindAnyObjectByType<CharacterManager>();
        if (existingManager != null) return;
        
        GameObject managerObj = new GameObject("CharacterManager");
        CharacterManager manager = managerObj.AddComponent<CharacterManager>();
    }
    
    System.Collections.IEnumerator RetryLoadAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        
        CharacterSelectionManager charSelectManager = FindAnyObjectByType<CharacterSelectionManager>();
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
        if (AwayActivityManager.Instance == null) return;
        
        bool hadAwayActivity = AwayActivityManager.Instance.LoadAwayState(currentSlotIndex);
        if (!hadAwayActivity) return;
        
        // Calculate and show rewards...
        // (Simplified for brevity - full implementation in actual file)
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
