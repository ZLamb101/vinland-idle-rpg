using UnityEngine;

/// <summary>
/// Skill range type - determines attack distance
/// </summary>
public enum SkillRange
{
    Melee,      // Close range (120 units)
    Ranged      // Long range (600 units)
}

/// <summary>
/// Skill type - determines how the skill behaves
/// </summary>
public enum SkillType
{
    Direct,     // Immediate damage/effect on enemy
    Buff,       // Positive effect on self
    Curse       // Negative effect on enemy (DoT, debuff)
}

/// <summary>
/// How the skill visually executes
/// </summary>
public enum SkillVisualType
{
    None,       // No visual - damage applies instantly
    Projectile, // Spawns a projectile that travels to target, damage on hit
    Instant     // Plays an effect at target location, damage applies instantly
}

/// <summary>
/// ScriptableObject that defines a skill with all its properties.
/// Create instances via: Right-click in Project → Create → Vinland → Skill
/// </summary>
[CreateAssetMenu(fileName = "New Skill", menuName = "Vinland/Skill", order = 5)]
public class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName = "New Skill";
    [TextArea(2, 4)]
    public string description = "A powerful ability";
    public Sprite icon;
    
    [Header("Class Restriction")]
    [Tooltip("Which classes can use this skill. Use 'All' for universal skills.")]
    public CharacterClass allowedClasses = CharacterClass.All;
    
    [Tooltip("Minimum character level required to use this skill (0 = no requirement)")]
    public int levelRequired = 1;
    
    [Header("Skill Type")]
    public SkillType skillType = SkillType.Direct;
    public SkillRange skillRange = SkillRange.Melee;
    
    [Header("Cost & Timing")]
    [Tooltip("Mana cost to cast this skill (only applies to player - monsters ignore mana)")]
    public float manaCost = 10f;
    
    [Tooltip("Cooldown in seconds before skill can be used again")]
    public float cooldown = 5f;
    
    [Tooltip("Cast time in seconds (0 = instant)")]
    public float castTime = 0f;
    
    [Header("Direct Damage")]
    [Tooltip("Base damage dealt (for Direct and some Curse skills)")]
    public float baseDamage = 0f;
    
    [Tooltip("Damage scales with attack power (0.5 = 50% of attack power added)")]
    public float attackPowerScaling = 0f;
    
    [Tooltip("Can this skill critically hit?")]
    public bool canCrit = true;
    
    [Header("Aura (Buffs/Curses)")]
    [Tooltip("The aura/effect this skill applies when cast. Required for Buff and Curse skill types.")]
    public AuraData appliedAura;
    
    [Header("AoE Settings")]
    [Tooltip("Does this skill hit multiple targets?")]
    public bool isAoE = false;
    
    [Tooltip("Number of additional targets (0 = hit ALL enemies)")]
    public int aoeTargetCount = 0;
    
    [Header("Visuals")]
    [Tooltip("How the skill visually executes")]
    public SkillVisualType visualType = SkillVisualType.None;
    
    [Tooltip("Custom projectile prefab for Projectile visual type (optional - uses default if null)")]
    public GameObject projectilePrefab;
    
    [Tooltip("Sprite for projectile (overrides default projectile image if set)")]
    public Sprite projectileSprite;
    
    [Tooltip("Projectile color tint (white = no tint)")]
    public Color projectileColor = Color.white;
    
    [Tooltip("Projectile speed multiplier (1 = default speed)")]
    public float projectileSpeedMultiplier = 1f;
    
    [Tooltip("Visual effect prefab spawned on hit/apply for Instant visual type (optional)")]
    public GameObject hitEffectPrefab;
    
    [Tooltip("How long the hit effect lasts before being destroyed (0 = 2 seconds default)")]
    public float hitEffectDuration = 0f;
    
    /// <summary>
    /// Calculate total damage including attack power scaling
    /// </summary>
    public float CalculateDamage(float attackPower)
    {
        return baseDamage + (attackPower * attackPowerScaling);
    }
    
    /// <summary>
    /// Get the range in units based on SkillRange type
    /// </summary>
    public float GetRangeUnits()
    {
        return skillRange == SkillRange.Melee 
            ? GameBalance.Combat.monsterMeleeRange 
            : GameBalance.Combat.monsterRangedRange;
    }
    
    /// <summary>
    /// Check if this skill requires a target
    /// </summary>
    public bool RequiresTarget()
    {
        return skillType != SkillType.Buff;
    }
    
    /// <summary>
    /// Check if this skill targets self
    /// </summary>
    public bool TargetsSelf()
    {
        return skillType == SkillType.Buff;
    }
    
    /// <summary>
    /// Get the duration from aura or DoT
    /// </summary>
    public float GetDuration()
    {
        if (appliedAura != null)
            return appliedAura.duration;
        return 0f;
    }
    
    /// <summary>
    /// Check if a player class can use this skill
    /// </summary>
    public bool CanBeUsedBy(CharacterClass playerClass)
    {
        return CharacterClassHelper.IsClassAllowed(allowedClasses, playerClass);
    }
    
    /// <summary>
    /// Check if a player class (from string) can use this skill
    /// </summary>
    public bool CanBeUsedBy(string className)
    {
        CharacterClass playerClass = CharacterClassHelper.ParseFromString(className);
        return CanBeUsedBy(playerClass);
    }
    
    /// <summary>
    /// Get formatted description with stats
    /// </summary>
    public string GetFullDescription(float attackPower = 0f)
    {
        string desc = description + "\n\n";
        
        // Cost and timing
        if (manaCost > 0) desc += $"Mana Cost: {manaCost:F0}\n";
        if (cooldown > 0) desc += $"Cooldown: {cooldown:F1}s\n";
        if (castTime > 0) desc += $"Cast Time: {castTime:F1}s\n";
        
        desc += "\n";
        
        // Damage
        if (baseDamage > 0 || attackPowerScaling > 0)
        {
            float totalDamage = CalculateDamage(attackPower);
            desc += $"Damage: {totalDamage:F0}";
            if (attackPowerScaling > 0)
            {
                desc += $" ({baseDamage:F0} + {attackPowerScaling * 100:F0}% AP)";
            }
            desc += "\n";
        }
        
        // Aura effects
        if (appliedAura != null)
        {
            desc += $"\nApplies: {appliedAura.auraName}";
            if (appliedAura.duration > 0)
            {
                desc += $" ({appliedAura.duration:F0}s)";
            }
            desc += "\n";
            
            // Show aura effects
            if (appliedAura.damagePerSecond > 0)
            {
                desc += $"  {appliedAura.damagePerSecond:F0} damage/sec\n";
            }
            
            if (appliedAura.statModifiers != null && appliedAura.statModifiers.HasAnyEffect())
            {
                desc += appliedAura.statModifiers.GetFormattedDescription("  ");
            }
            
            if (appliedAura.canStack)
            {
                desc += $"  Stacks up to {appliedAura.maxStacks}x\n";
            }
        }
        
        // AoE
        if (isAoE)
        {
            if (aoeTargetCount == 0)
                desc += "\nHits ALL enemies\n";
            else
                desc += $"\nHits {aoeTargetCount + 1} targets\n";
        }
        
        return desc.TrimEnd();
    }
}

