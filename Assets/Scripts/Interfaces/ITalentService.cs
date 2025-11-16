using System;
using System.Collections.Generic;

/// <summary>
/// Interface for talent management services
/// </summary>
public interface ITalentService
{
    // Events migrated to EventBus - see GameEvent.cs
    
    // Talent Management
    void AddTalentPoints(int amount);
    bool UnlockTalent(TalentData talent);
    int GetTalentRank(TalentData talent);
    
    // Tree Management
    int GetTotalPointsInTree(TalentTree tree);
    void ResetTalents();
    
    // Getters
    int GetUnspentPoints();
    int GetTotalPoints();
    TalentBonuses GetTotalBonuses();
    Dictionary<TalentData, int> GetAllUnlockedTalents();
    
    // Save/Load
    void LoadTalentData(Dictionary<string, int> talentsByName, int unspentPoints, int totalPoints);
}

