using UnityEngine;

/// <summary>
/// Represents a single monster instance in combat
/// </summary>
[System.Serializable]
public class CombatMonsterInstance
{
    public MonsterData monsterData;
    public int level; // Actual level used for this instance
    public float currentHealth;
    public float maxHealth;
    public float attackDamage;
    public float attackSpeed;
    public float armor; // Damage reduction percentage
    public float attackRange;
    public float attackTimer;
    public bool isAttackInProgress;
    public int index; // Index in the active monsters list
    
    /// <summary>
    /// Create a monster instance with calculated stats for a specific level
    /// </summary>
    public CombatMonsterInstance(MonsterData data, int level, int idx)
    {
        monsterData = data;
        index = idx;
        
        // Calculate stats using MonsterStatCalculator
        CalculatedMonsterStats stats = MonsterStatCalculator.CalculateMonsterStats(data, level);
        
        this.level = stats.level;
        maxHealth = stats.health;
        currentHealth = maxHealth;
        attackDamage = stats.attackDamage;
        attackSpeed = stats.attackSpeed;
        armor = stats.armor;
        attackRange = stats.attackRange;
        attackTimer = 0f;
        isAttackInProgress = false;
    }
    
    public bool IsAlive()
    {
        return currentHealth > 0f;
    }
}

