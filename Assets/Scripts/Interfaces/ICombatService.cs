using System;
using System.Collections.Generic;

/// <summary>
/// Interface for combat management services
/// </summary>
public interface ICombatService
{
    // Events migrated to EventBus - see GameEvent.cs
    
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
    
    // Death Handling
    void HandleMonsterDeath(int monsterIndex);
}

