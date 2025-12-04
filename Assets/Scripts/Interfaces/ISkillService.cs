using System.Collections.Generic;

/// <summary>
/// Interface for skill management service.
/// Handles action bar skills, cooldowns, and active effects.
/// </summary>
public interface ISkillService
{
    // ==================== Action Bar ====================
    
    /// <summary>
    /// Get skill in action bar slot (0-3)
    /// </summary>
    SkillData GetActionBarSkill(int slotIndex);
    
    /// <summary>
    /// Set skill in action bar slot (0-3)
    /// </summary>
    void SetActionBarSkill(int slotIndex, SkillData skill);
    
    /// <summary>
    /// Clear action bar slot
    /// </summary>
    void ClearActionBarSlot(int slotIndex);
    
    /// <summary>
    /// Clear all action bar slots
    /// </summary>
    void ClearAllActionBarSlots();
    
    /// <summary>
    /// Get all action bar skills
    /// </summary>
    SkillData[] GetAllActionBarSkills();
    
    // ==================== Cooldowns ====================
    
    /// <summary>
    /// Check if the global cooldown is active
    /// </summary>
    bool IsOnGlobalCooldown();
    
    /// <summary>
    /// Get remaining global cooldown time
    /// </summary>
    float GetGlobalCooldownRemaining();
    
    /// <summary>
    /// Check if a skill is on cooldown
    /// </summary>
    bool IsOnCooldown(SkillData skill);
    
    /// <summary>
    /// Get remaining cooldown for a skill
    /// </summary>
    float GetRemainingCooldown(SkillData skill);
    
    /// <summary>
    /// Get cooldown progress (0-1, where 1 = ready)
    /// </summary>
    float GetCooldownProgress(SkillData skill);
    
    // ==================== Skill Casting ====================
    
    /// <summary>
    /// Check if player can cast a skill (has mana, not on cooldown)
    /// </summary>
    bool CanCastSkill(SkillData skill);
    
    /// <summary>
    /// Cast a skill (player)
    /// </summary>
    bool CastSkill(SkillData skill, int targetIndex);
    
    // ==================== Active Effects ====================
    
    /// <summary>
    /// Get all active buffs on player
    /// </summary>
    List<ActiveEffect> GetPlayerBuffs();
    
    /// <summary>
    /// Get all active debuffs on player
    /// </summary>
    List<ActiveEffect> GetPlayerDebuffs();
    
    /// <summary>
    /// Get all active effects on a monster
    /// </summary>
    List<ActiveEffect> GetMonsterEffects(int monsterIndex);
    
    /// <summary>
    /// Apply a buff/curse to target
    /// </summary>
    /// <param name="skill">The skill applying the effect</param>
    /// <param name="targetIndex">Target index (-1 for player, 0+ for monsters)</param>
    /// <param name="casterAttackPower">Caster's attack power for DoT scaling</param>
    void ApplyEffect(SkillData skill, int targetIndex, float casterAttackPower = 0f);
    
    /// <summary>
    /// Get total stat modifiers from active effects on player
    /// </summary>
    StatModifiers GetPlayerEffectModifiers();
    
    /// <summary>
    /// Check if player is invulnerable
    /// </summary>
    bool IsPlayerInvulnerable();
    
    /// <summary>
    /// Check if player is stunned
    /// </summary>
    bool IsPlayerStunned();
    
    /// <summary>
    /// Check if a monster is stunned
    /// </summary>
    bool IsMonsterStunned(int monsterIndex);
    
    /// <summary>
    /// Get monster's slow percentage
    /// </summary>
    float GetMonsterSlowPercent(int monsterIndex);
    
    // ==================== Available Skills ====================
    
    /// <summary>
    /// Get all available skills for the player
    /// </summary>
    List<SkillData> GetAllAvailableSkills();
    
    /// <summary>
    /// Clear all active effects (combat ended)
    /// </summary>
    void ClearAllEffects();
    
    // ==================== Save/Load ====================
    
    /// <summary>
    /// Get action bar skill asset names for saving
    /// </summary>
    string[] GetActionBarSaveData();
    
    /// <summary>
    /// Load action bar skills from save data
    /// </summary>
    void LoadActionBarData(string[] saveData);
}

