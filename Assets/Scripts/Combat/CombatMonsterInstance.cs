using UnityEngine;

/// <summary>
/// Represents a single monster instance in combat
/// </summary>
[System.Serializable]
public class CombatMonsterInstance
{
    public MonsterData monsterData;
    public float currentHealth;
    public float maxHealth;
    public float attackDamage;
    public float attackSpeed;
    public float attackTimer;
    public bool isAttackInProgress;
    public int index; // Index in the active monsters list
    
    public CombatMonsterInstance(MonsterData data, int idx)
    {
        monsterData = data;
        index = idx;
        maxHealth = data.health;
        currentHealth = maxHealth;
        attackDamage = data.attackDamage;
        attackSpeed = data.attackSpeed;
        attackTimer = 0f;
        isAttackInProgress = false;
    }
    
    public bool IsAlive()
    {
        return currentHealth > 0f;
    }
}

