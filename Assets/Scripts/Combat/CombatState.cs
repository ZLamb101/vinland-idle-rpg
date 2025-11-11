using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages combat state: what monsters are active, who's targeting who, etc.
/// Extracted from CombatManager to follow Single Responsibility Principle
/// </summary>
public class CombatState
{
    // State
    public CombatManager.CombatState currentState = CombatManager.CombatState.Idle;
    public List<CombatMonsterInstance> activeMonsters = new List<CombatMonsterInstance>();
    public int currentTargetIndex = 0;
    public MonsterData[] zoneMonsters;
    
    // Player stats
    public float playerCurrentHealth;
    public float playerMaxHealth;
    public float playerAttackDamage;
    public float playerAttackSpeed;
    public float playerAttackTimer = 0f;
    
    // Events
    public event Action<CombatManager.CombatState> OnStateChanged;
    public event Action<int> OnTargetChanged;
    public event Action<int> OnMonsterSpawned;
    public event Action<int> OnMonsterDied;
    
    /// <summary>
    /// Get current target monster
    /// </summary>
    public CombatMonsterInstance GetCurrentTarget()
    {
        if (activeMonsters.Count == 0 || currentTargetIndex < 0 || currentTargetIndex >= activeMonsters.Count)
            return null;
        
        return activeMonsters[currentTargetIndex];
    }
    
    /// <summary>
    /// Find next alive target
    /// </summary>
    public bool FindNextAliveTarget()
    {
        if (activeMonsters.Count == 0) return false;
        
        int startIndex = currentTargetIndex;
        do
        {
            currentTargetIndex = (currentTargetIndex + 1) % activeMonsters.Count;
            if (activeMonsters[currentTargetIndex].IsAlive())
            {
                OnTargetChanged?.Invoke(currentTargetIndex);
                return true;
            }
        } while (currentTargetIndex != startIndex);
        
        return false;
    }
    
    /// <summary>
    /// Set target by index
    /// </summary>
    public bool SetTarget(int index)
    {
        if (index >= 0 && index < activeMonsters.Count && activeMonsters[index].IsAlive())
        {
            currentTargetIndex = index;
            OnTargetChanged?.Invoke(currentTargetIndex);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Check if all monsters are dead
    /// </summary>
    public bool AreAllMonstersDead()
    {
        foreach (var monster in activeMonsters)
        {
            if (monster.IsAlive())
                return false;
        }
        return true;
    }
    
    /// <summary>
    /// Change combat state
    /// </summary>
    public void ChangeState(CombatManager.CombatState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            OnStateChanged?.Invoke(newState);
            
            // Publish event through EventBus
            if (newState == CombatManager.CombatState.Fighting)
            {
                EventBus.Publish(new CombatStartedEvent 
                { 
                    monsters = zoneMonsters,
                    mobCount = activeMonsters.Count
                });
            }
            else if (newState == CombatManager.CombatState.Idle)
            {
                EventBus.Publish(new CombatEndedEvent 
                { 
                    wasVictory = true,
                    monstersDefeated = activeMonsters.Count
                });
            }
        }
    }
    
    /// <summary>
    /// Reset all combat state
    /// </summary>
    public void Reset()
    {
        currentState = CombatManager.CombatState.Idle;
        activeMonsters.Clear();
        currentTargetIndex = 0;
        zoneMonsters = null;
        playerAttackTimer = 0f;
    }
    
    /// <summary>
    /// Spawn a monster
    /// </summary>
    public void SpawnMonster(MonsterData monsterData, int index)
    {
        CombatMonsterInstance instance = new CombatMonsterInstance(monsterData, index);
        activeMonsters.Add(instance);
        
        OnMonsterSpawned?.Invoke(index);
        
        // Publish event through EventBus
        EventBus.Publish(new MonsterSpawnedEvent 
        { 
            monsterData = monsterData,
            monsterIndex = index
        });
    }
    
    /// <summary>
    /// Mark a monster as dead
    /// </summary>
    public void MonsterDied(int index, int xpAwarded, int goldAwarded)
    {
        if (index < 0 || index >= activeMonsters.Count)
            return;
        
        var monster = activeMonsters[index];
        
        OnMonsterDied?.Invoke(index);
        
        // Publish event through EventBus
        EventBus.Publish(new MonsterDiedEvent 
        { 
            monsterData = monster.monsterData,
            monsterIndex = index,
            xpAwarded = xpAwarded,
            goldAwarded = goldAwarded
        });
    }
}

