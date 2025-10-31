using UnityEngine;

/// <summary>
/// ScriptableObject that defines a monster with combat stats.
/// Create instances via: Right-click in Project → Create → Vinland → Monster
/// </summary>
[CreateAssetMenu(fileName = "New Monster", menuName = "Vinland/Monster", order = 3)]
public class MonsterData : ScriptableObject
{
    [Header("Monster Info")]
    public string monsterName = "Goblin";
    public Sprite monsterSprite;
    
    [Header("Combat Stats")]
    [Tooltip("Base health for this monster")]
    public float baseHealth = 50f;
    
    [Tooltip("Damage dealt per attack")]
    public float attackDamage = 5f;
    
    [Tooltip("Time in seconds between attacks")]
    public float attackSpeed = 2f;
    
    [Header("Level Scaling")]
    [Tooltip("Health multiplier per player level (1.1 = +10% per level)")]
    public float healthScaling = 1.05f;
    
    [Tooltip("Damage multiplier per player level")]
    public float damageScaling = 1.05f;
    
    [Header("Rewards")]
    public int xpReward = 10;
    public int goldReward = 5;
    public ItemData itemDrop; // Optional item drop
    [Range(0f, 1f)] public float dropChance = 0.25f; // 25% chance to drop item
    
    /// <summary>
    /// Get scaled health based on player level
    /// </summary>
    public float GetScaledHealth(int playerLevel)
    {
        return baseHealth * Mathf.Pow(healthScaling, playerLevel - 1);
    }
    
    /// <summary>
    /// Get scaled damage based on player level
    /// </summary>
    public float GetScaledDamage(int playerLevel)
    {
        return attackDamage * Mathf.Pow(damageScaling, playerLevel - 1);
    }
}


