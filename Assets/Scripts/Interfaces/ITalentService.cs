using System;
using System.Collections.Generic;

/// <summary>
/// Interface for talent management services
/// </summary>
public interface ITalentService
{
    // Events
    event Action<int> OnTalentPointsChanged;
    event Action<TalentData, int> OnTalentUnlocked; // Talent, new rank
    event Action OnTalentBonusesRecalculated;
    
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
}

