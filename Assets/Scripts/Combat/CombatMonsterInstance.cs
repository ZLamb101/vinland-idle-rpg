using UnityEngine;
using System.Collections.Generic;

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
    
    // Skill cooldowns - keyed by skill name
    public Dictionary<string, float> skillCooldowns = new Dictionary<string, float>();
    
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
        
        // Initialize skill cooldowns
        skillCooldowns = new Dictionary<string, float>();
    }
    
    public bool IsAlive()
    {
        return currentHealth > 0f;
    }
    
    /// <summary>
    /// Check if a skill is off cooldown
    /// </summary>
    public bool CanUseSkill(SkillData skill)
    {
        if (skill == null) return false;
        if (!skillCooldowns.ContainsKey(skill.skillName)) return true;
        return skillCooldowns[skill.skillName] <= 0f;
    }
    
    /// <summary>
    /// Start cooldown for a skill
    /// </summary>
    public void StartSkillCooldown(SkillData skill)
    {
        if (skill == null) return;
        skillCooldowns[skill.skillName] = skill.cooldown;
    }
    
    /// <summary>
    /// Update all skill cooldowns
    /// </summary>
    public void UpdateSkillCooldowns(float deltaTime)
    {
        List<string> keys = new List<string>(skillCooldowns.Keys);
        foreach (string skillName in keys)
        {
            skillCooldowns[skillName] -= deltaTime;
            if (skillCooldowns[skillName] < 0f)
            {
                skillCooldowns[skillName] = 0f;
            }
        }
    }
    
    /// <summary>
    /// Get the first skill that can be used (off cooldown)
    /// Monsters don't need mana - they only check cooldowns
    /// </summary>
    public SkillData GetAvailableSkill()
    {
        if (monsterData == null || monsterData.skills == null || monsterData.skills.Count == 0) 
            return null;
        
        foreach (SkillData skill in monsterData.skills)
        {
            if (skill != null && CanUseSkill(skill))
            {
                return skill;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Check if monster has any skills assigned
    /// </summary>
    public bool HasSkills()
    {
        return monsterData != null && monsterData.skills != null && monsterData.skills.Count > 0;
    }
}

