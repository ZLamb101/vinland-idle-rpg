using System;

/// <summary>
/// Interface for away activity tracking services
/// </summary>
public interface IAwayActivityService
{
    // Activity Control
    void StartMining(ResourceData resource);
    void StartFighting(MonsterData[] monsters, int mobCount = 1);
    void StopActivity();
    
    // State Management
    void SaveAwayState();
    bool LoadAwayState(int characterSlotIndex = -1);
    void ClearAwayState(int characterSlotIndex = -1);
    void MarkGameSessionStart();
    
    // Getters
    AwayActivityType GetCurrentActivity();
    DateTime GetActivityStartTime();
    ResourceData GetCurrentResource();
    MonsterData[] GetCurrentMonsters();
    int GetMobCount();
    string GetActivityDisplayString(int characterSlotIndex);
    
    // Time Tracking
    void SaveLastPlayedTime(int characterSlotIndex = -1);
    DateTime GetLastPlayedTime(int characterSlotIndex);
    string GetTimeSinceLastPlayed(int characterSlotIndex);
    
    // Helper Methods
    MonsterData FindMonsterByNamePublic(string monsterName);
    MonsterData[] TryLoadMonstersFromSavedNames(int characterSlotIndex);
    string GetMonsterDisplayNameFromPlayerPrefs(int characterSlotIndex);
}

