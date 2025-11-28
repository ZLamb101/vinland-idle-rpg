using UnityEngine;

/// <summary>
/// Type of aura effect
/// </summary>
public enum AuraType
{
    Buff,   // Positive effect on self/ally
    Debuff  // Negative effect on enemy
}

/// <summary>
/// ScriptableObject that defines a buff or debuff effect (aura).
/// Skills apply auras to targets, and auras are displayed on the buff/debuff panel.
/// Create via: Right-click in Project → Create → Vinland → Aura
/// </summary>
[CreateAssetMenu(fileName = "New Aura", menuName = "Vinland/Aura", order = 6)]
public class AuraData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Display name of the aura")]
    public string auraName = "New Aura";
    
    [TextArea(2, 4)]
    [Tooltip("Description of what this aura does")]
    public string description = "";
    
    [Tooltip("Icon to display on buff/debuff panel")]
    public Sprite icon;
    
    [Header("Type & Duration")]
    [Tooltip("Whether this is a buff (positive) or debuff (negative)")]
    public AuraType auraType = AuraType.Buff;
    
    [Tooltip("Duration in seconds (0 = permanent until removed)")]
    public float duration = 10f;
    
    [Tooltip("Can this aura stack multiple times?")]
    public bool canStack = false;
    
    [Tooltip("Maximum stacks (if canStack is true)")]
    public int maxStacks = 1;
    
    [Header("Stat Modifiers")]
    [Tooltip("Stat changes while aura is active")]
    public StatModifiers statModifiers;
    
    [Header("Damage Over Time")]
    [Tooltip("Base damage per second (for DoT debuffs like poison)")]
    public float damagePerSecond = 0f;
    
    [Tooltip("Attack power scaling for DoT (e.g., 0.1 = 10% of attack power added per second)")]
    [Range(0f, 2f)]
    public float dotAttackPowerScaling = 0f;
    
    [Header("Visual Effects")]
    [Tooltip("Visual effect prefab to spawn on target while aura is active")]
    public GameObject activeEffectPrefab;
    
    [Tooltip("Color tint for the aura icon")]
    public Color iconTint = Color.white;
    
    /// <summary>
    /// Get a formatted description with all stats
    /// </summary>
    public string GetFullDescription()
    {
        string desc = description;
        
        if (duration > 0)
        {
            desc += $"\n\nDuration: {duration}s";
        }
        
        if (statModifiers != null && statModifiers.HasAnyEffect())
        {
            string modDesc = statModifiers.GetFormattedDescription();
            if (!string.IsNullOrEmpty(modDesc))
            {
                desc += "\n" + modDesc.TrimEnd();
            }
        }
        
        if (damagePerSecond > 0 || dotAttackPowerScaling > 0)
        {
            string dotDesc = "\n";
            if (damagePerSecond > 0)
            {
                dotDesc += $"{damagePerSecond:F0}";
            }
            if (dotAttackPowerScaling > 0)
            {
                if (damagePerSecond > 0)
                    dotDesc += $" + {dotAttackPowerScaling * 100:F0}% AP";
                else
                    dotDesc += $"{dotAttackPowerScaling * 100:F0}% AP";
            }
            dotDesc += " damage per second";
            desc += dotDesc;
        }
        
        return desc;
    }
    
    /// <summary>
    /// Calculate the actual DoT damage per second given an attack power value
    /// </summary>
    public float GetScaledDotDamage(float attackPower)
    {
        return damagePerSecond + (attackPower * dotAttackPowerScaling);
    }
}

