using UnityEngine;

/// <summary>
/// Interface for character session management (loading, saving, initialization, cleanup)
/// </summary>
public interface ICharacterSessionService
{
    /// <summary>
    /// Load a character from a save slot and initialize all services
    /// </summary>
    /// <param name="slotIndex">Character slot to load</param>
    /// <returns>True if load was successful</returns>
    bool LoadCharacter(int slotIndex);
    
    /// <summary>
    /// Save current character and exit to character selection screen
    /// </summary>
    /// <param name="slotIndex">Character slot to save</param>
    /// <param name="characterSceneName">Scene name to load for character selection</param>
    void SaveAndExitToCharacterSelection(int slotIndex, string characterSceneName);
    
    /// <summary>
    /// Save current character without exiting
    /// </summary>
    /// <param name="slotIndex">Character slot to save</param>
    /// <returns>True if save was successful</returns>
    bool SaveCurrentCharacter(int slotIndex);
    
    /// <summary>
    /// Get the currently active character slot index
    /// </summary>
    int GetCurrentSlotIndex();
    
    /// <summary>
    /// Request showing the away rewards panel (must be called from MonoBehaviour with coroutine support)
    /// </summary>
    void RequestShowAwayRewardsPanel(AwayRewards rewards, MonoBehaviour coroutineRunner);
}

