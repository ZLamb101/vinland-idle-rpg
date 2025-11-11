using System;
using System.Collections.Generic;

/// <summary>
/// Interface for combat management services
/// </summary>
public interface ICombatService
{
    // Events
    event Action<CombatManager.CombatState> OnCombatStateChanged;
    event Action<float, float> OnPlayerHealthChanged;
    event Action<float, float, int> OnMonsterHealthChanged;
    event Action<List<MonsterData>> OnMonstersChanged;
    event Action<int> OnTargetChanged;
    event Action<int> OnMonsterSpawned;
    event Action<int> OnMonsterDied;
    event Action<float> OnPlayerAttackProgress;
    event Action<float, int> OnMonsterAttackProgress;
    event Action<float> OnPlayerDamageDealt;
    event Action<float> OnPlayerDamageTaken;
    
    // Combat Control
    void StartCombat(MonsterData[] monsters, int mobCount = 1);
    void EndCombat();
    void ResumeAfterDefeat();
    void CalculatePlayerStats();
    
    // Target Management
    void CycleTarget();
    void SetTarget(int index);
    CombatMonsterInstance GetCurrentTarget();
    int GetCurrentTargetIndex();
    List<CombatMonsterInstance> GetActiveMonsters();
    
    // Getters
    CombatManager.CombatState GetCombatState();
    CombatMonsterInstance GetCurrentTargetInstance();
    float GetPlayerCurrentHealth();
    float GetPlayerMaxHealth();
    float GetPlayerAttackDamage();
    float GetPlayerAttackSpeed();
    float GetMonsterCurrentHealth(int index);
    float GetMonsterMaxHealth(int index);
}

