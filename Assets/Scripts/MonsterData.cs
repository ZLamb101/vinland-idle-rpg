using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a single item drop entry in a monster's drop table
/// </summary>
[System.Serializable]
public class MonsterDropEntry
{
    [Tooltip("The item that can drop")]
    public ItemData item;
    
    [Tooltip("Quantity of items to drop")]
    public int quantity = 1;
    
    [Tooltip("Chance this item will drop (0.0 to 1.0, where 1.0 = 100%)")]
    [Range(0f, 1f)]
    public float dropChance = 0.25f;
}

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
    [Tooltip("Flip sprite horizontally (for monsters facing the wrong direction)")]
    public bool flipSprite = false;
    
    [Header("Combat Stats")]
    [Tooltip("Monster level (fixed, does not scale with player)")]
    public int level = 1;
    
    [Tooltip("Health for this monster (fixed, does not scale)")]
    public float health = 50f;
    
    [Tooltip("Damage dealt per attack (fixed, does not scale)")]
    public float attackDamage = 5f;
    
    [Tooltip("Time in seconds between attacks")]
    public float attackSpeed = 2f;
    
    [Tooltip("Attack range in pixels. Melee monsters should use a small value (e.g., 100). Ranged/magic monsters can use larger values (e.g., 500+)")]
    public float attackRange = 100f;
    
    [Header("Rewards")]
    public int xpReward = 10;
    public int goldReward = 5;
    
    [Header("Drop Table")]
    [Tooltip("List of items that can drop from this monster. Each entry has its own drop chance.")]
    public List<MonsterDropEntry> dropTable = new List<MonsterDropEntry>();
}


