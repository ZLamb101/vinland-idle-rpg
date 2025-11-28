using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attack range type for monsters
/// </summary>
public enum MonsterAttackRangeType
{
    Melee,   // 120 range
    Ranged   // 600 range
}

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
    
    [Header("Level Range")]
    [Tooltip("Minimum level this monster can spawn at")]
    public int minLevel = 1;
    
    [Tooltip("Maximum level this monster can spawn at")]
    public int maxLevel = 1;
    
    [Header("Attack Range Type")]
    [Tooltip("Melee = 120 range, Ranged = 600 range")]
    public MonsterAttackRangeType attackRangeType = MonsterAttackRangeType.Melee;
    
    [Header("Stat Modifiers")]
    [Tooltip("Health modifier (1.0 = normal, 1.1 = +10% health, 0.9 = -10% health)")]
    [Range(0.1f, 2f)]
    public float healthModifier = 1.0f;
    
    [Tooltip("Damage modifier (1.0 = normal, 1.1 = +10% damage, 0.9 = -10% damage)")]
    [Range(0.1f, 2f)]
    public float damageModifier = 1.0f;
    
    [Tooltip("Speed modifier (1.0 = normal, 0.9 = 10% faster, 1.1 = 10% slower)")]
    [Range(0.1f, 2f)]
    public float speedModifier = 1.0f;
    
    [Tooltip("Armor modifier (1.0 = normal, 1.1 = +10% armor, 0.9 = -10% armor). Armor doesn't scale with level.")]
    [Range(0.1f, 2f)]
    public float armorModifier = 1.0f;
    
    [Header("Rewards")]
    [Tooltip("XP reward modifier (scales with actual level)")]
    public float xpRewardModifier = 1.0f;
    
    [Tooltip("Gold reward modifier (scales with actual level)")]
    public float goldRewardModifier = 1.0f;
    
    [Header("Drop Table")]
    [Tooltip("List of items that can drop from this monster. Each entry has its own drop chance.")]
    public List<MonsterDropEntry> dropTable = new List<MonsterDropEntry>();
    
    [Header("Skills")]
    [Tooltip("Skills this monster can use in combat. Will be cast on cooldown when in range.")]
    public List<SkillData> skills = new List<SkillData>();
}


