using UnityEngine;

/// <summary>
/// Thin MonoBehaviour wrapper for CharacterSessionService.
/// Handles scene lifecycle timing and coroutines for character loading.
/// All business logic is in CharacterSessionManager.
/// </summary>
public class CharacterLoader : MonoBehaviour
{
    [Header("Settings")]
    public bool loadOnStart = true;
    
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
    
    /// <summary>
    /// Load the active character from PlayerPrefs slot
    /// </summary>
    public void LoadActiveCharacter()
    {
        int slotIndex = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        
        if (slotIndex < 0)
        {
            Debug.Log("[CharacterLoader] No active character slot specified");
            return;
        }
        
        // Delegate to CharacterSessionService
        if (Services.TryGet<ICharacterSessionService>(out var sessionService))
        {
            sessionService.LoadCharacter(slotIndex);
        }
        else
        {
            Debug.LogError("[CharacterLoader] CharacterSessionService not found! Bootstrap scene must run first.");
        }
    }
    
    /// <summary>
    /// Save the current character
    /// </summary>
    public void SaveCurrentCharacter()
    {
        int slotIndex = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        
        if (slotIndex < 0)
        {
            Debug.LogWarning("[CharacterLoader] Cannot save - no active character slot");
            return;
        }
        
        // Delegate to CharacterSessionService
        if (Services.TryGet<ICharacterSessionService>(out var sessionService))
        {
            sessionService.SaveCurrentCharacter(slotIndex);
        }
        else
        {
            Debug.LogError("[CharacterLoader] CharacterSessionService not found!");
        }
    }
    
    /// <summary>
    /// Get the current character slot index
    /// </summary>
    public int GetCurrentSlotIndex()
    {
        if (Services.TryGet<ICharacterSessionService>(out var sessionService))
        {
            return sessionService.GetCurrentSlotIndex();
        }
        return -1;
    }
}
