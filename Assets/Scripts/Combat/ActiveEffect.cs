using UnityEngine;

/// <summary>
/// Represents an active buff or curse effect on an entity.
/// Tracks duration, applies stat modifiers, and handles DoT ticks.
/// Can be created from a SkillData or directly from an AuraData.
/// </summary>
[System.Serializable]
public class ActiveEffect
{
    /// <summary>
    /// The skill that created this effect (may be null if created from aura directly)
    /// </summary>
    public SkillData sourceSkill;
    
    /// <summary>
    /// The aura data for this effect (may be null for legacy skills without aura)
    /// </summary>
    public AuraData aura;
    
    /// <summary>
    /// Remaining duration in seconds
    /// </summary>
    public float remainingDuration;
    
    /// <summary>
    /// Total duration for UI display
    /// </summary>
    public float totalDuration;
    
    /// <summary>
    /// Index of the target (-1 for player, 0+ for monsters)
    /// </summary>
    public int targetIndex;
    
    /// <summary>
    /// Whether this is a buff (true) or debuff/curse (false)
    /// </summary>
    public bool isBuff;
    
    /// <summary>
    /// Current stack count (if aura supports stacking)
    /// </summary>
    public int stacks = 1;
    
    /// <summary>
    /// Caster's attack power at the time of effect creation (for DoT scaling)
    /// </summary>
    public float casterAttackPower = 0f;
    
    /// <summary>
    /// Timer for DoT tick (ticks every 1 second)
    /// </summary>
    private float dotTickTimer = 0f;
    private const float DOT_TICK_RATE = 1f;
    
    /// <summary>
    /// Create a new active effect from a skill
    /// </summary>
    /// <param name="skill">The skill creating this effect</param>
    /// <param name="target">Target index (-1 for player, 0+ for monsters)</param>
    /// <param name="attackPower">Caster's attack power for DoT scaling</param>
    public ActiveEffect(SkillData skill, int target, float attackPower = 0f)
    {
        sourceSkill = skill;
        aura = skill.appliedAura;
        casterAttackPower = attackPower;
        
        // Aura is required for buff/curse skills
        if (aura == null)
        {
            Debug.LogWarning($"[ActiveEffect] Skill '{skill.skillName}' has no aura assigned!");
            remainingDuration = 0f;
            totalDuration = 0f;
        }
        else
        {
            remainingDuration = aura.duration;
            totalDuration = aura.duration;
        }
        
        targetIndex = target;
        
        // Determine if buff based on aura type
        if (aura != null)
        {
            isBuff = aura.auraType == AuraType.Buff;
        }
        else
        {
            isBuff = skill.skillType == SkillType.Buff;
        }
        
        dotTickTimer = 0f;
        stacks = 1;
    }
    
    /// <summary>
    /// Create a new active effect directly from an aura (without a source skill)
    /// </summary>
    /// <param name="auraData">The aura data for this effect</param>
    /// <param name="target">Target index (-1 for player, 0+ for monsters)</param>
    /// <param name="attackPower">Caster's attack power for DoT scaling</param>
    public ActiveEffect(AuraData auraData, int target, float attackPower = 0f)
    {
        sourceSkill = null;
        aura = auraData;
        casterAttackPower = attackPower;
        remainingDuration = auraData.duration;
        totalDuration = auraData.duration;
        targetIndex = target;
        isBuff = auraData.auraType == AuraType.Buff;
        dotTickTimer = 0f;
        stacks = 1;
    }
    
    /// <summary>
    /// Update the effect, returning DoT damage if applicable
    /// </summary>
    /// <param name="deltaTime">Time since last update</param>
    /// <returns>DoT damage dealt this frame (0 if none or not a DoT)</returns>
    public float Update(float deltaTime)
    {
        float dotDamage = 0f;
        
        // Decrease remaining duration
        remainingDuration -= deltaTime;
        
        // Get DoT damage from aura or skill
        float dotPerSecond = GetDotDamagePerSecond();
        
        // Handle DoT tick
        if (dotPerSecond > 0)
        {
            dotTickTimer += deltaTime;
            if (dotTickTimer >= DOT_TICK_RATE)
            {
                dotTickTimer -= DOT_TICK_RATE;
                dotDamage = dotPerSecond * stacks; // Multiply by stacks
            }
        }
        
        return dotDamage;
    }
    
    /// <summary>
    /// Get DoT damage per second from aura, including attack power scaling
    /// </summary>
    float GetDotDamagePerSecond()
    {
        if (aura == null) return 0f;
        return aura.GetScaledDotDamage(casterAttackPower);
    }
    
    /// <summary>
    /// Check if the effect has expired
    /// </summary>
    public bool IsExpired()
    {
        return remainingDuration <= 0f;
    }
    
    /// <summary>
    /// Get the stat modifiers from this effect's aura
    /// </summary>
    public StatModifiers GetStatModifiers()
    {
        return aura?.statModifiers;
    }
    
    /// <summary>
    /// Check if this effect grants invulnerability
    /// </summary>
    public bool GrantsInvulnerability()
    {
        StatModifiers mods = GetStatModifiers();
        return mods?.invulnerable ?? false;
    }
    
    /// <summary>
    /// Check if this effect stuns the target
    /// </summary>
    public bool IsStunned()
    {
        StatModifiers mods = GetStatModifiers();
        return mods?.stunned ?? false;
    }
    
    /// <summary>
    /// Get the slow percentage (0-1)
    /// </summary>
    public float GetSlowPercent()
    {
        StatModifiers mods = GetStatModifiers();
        return mods?.slowPercent ?? 0f;
    }
    
    /// <summary>
    /// Get the name of this effect (prefers aura name, falls back to skill name)
    /// </summary>
    public string GetName()
    {
        if (aura != null)
            return aura.auraName;
        return sourceSkill?.skillName ?? "Unknown Effect";
    }
    
    /// <summary>
    /// Get the icon of this effect (prefers aura icon, falls back to skill icon)
    /// </summary>
    public Sprite GetIcon()
    {
        if (aura != null && aura.icon != null)
            return aura.icon;
        return sourceSkill?.icon;
    }
    
    /// <summary>
    /// Get the description of this effect
    /// </summary>
    public string GetDescription()
    {
        if (aura != null)
            return aura.GetFullDescription();
        return sourceSkill?.description ?? "";
    }
    
    /// <summary>
    /// Get remaining duration as a percentage (0-1)
    /// </summary>
    public float GetDurationPercent()
    {
        if (totalDuration <= 0f) return 0f;
        return Mathf.Clamp01(remainingDuration / totalDuration);
    }
    
    /// <summary>
    /// Check if this effect is from the same skill or aura (for refreshing/stacking)
    /// </summary>
    public bool IsFromSameSkill(SkillData skill)
    {
        // Check if auras match
        if (aura != null && skill.appliedAura != null)
        {
            return aura == skill.appliedAura || aura.auraName == skill.appliedAura.auraName;
        }
        
        // Fall back to skill name comparison
        return sourceSkill == skill || 
               (sourceSkill != null && skill != null && sourceSkill.skillName == skill.skillName);
    }
    
    /// <summary>
    /// Check if this effect is from the same aura
    /// </summary>
    public bool IsFromSameAura(AuraData checkAura)
    {
        if (aura == null || checkAura == null)
            return false;
        return aura == checkAura || aura.auraName == checkAura.auraName;
    }
    
    /// <summary>
    /// Refresh the effect duration (when reapplied)
    /// </summary>
    public void Refresh()
    {
        remainingDuration = totalDuration;
    }
    
    /// <summary>
    /// Add a stack to this effect (if supported by aura)
    /// Returns true if stack was added, false if at max stacks
    /// </summary>
    public bool AddStack()
    {
        if (aura == null || !aura.canStack)
            return false;
        
        if (stacks >= aura.maxStacks)
            return false;
        
        stacks++;
        Refresh(); // Also refresh duration
        return true;
    }
    
    /// <summary>
    /// Check if this effect can have more stacks added
    /// </summary>
    public bool CanAddStack()
    {
        if (aura == null || !aura.canStack)
            return false;
        return stacks < aura.maxStacks;
    }
}

