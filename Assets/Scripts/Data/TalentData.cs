using UnityEngine;

/// <summary>
/// ScriptableObject that defines a single talent in the talent tree.
/// Create instances via: Right-click in Project → Create → Vinland → Talent
/// </summary>
[CreateAssetMenu(fileName = "New Talent", menuName = "Vinland/Talent", order = 5)]
public class TalentData : ScriptableObject
{
    [Header("Basic Info")]
    public string talentName = "New Talent";
    [TextArea(2, 4)]
    public string description = "Talent description";
    public Sprite icon;
    
    [Header("Talent Tree Position")]
    public TalentTree talentTree = TalentTree.Combat;
    public int tier = 1; // Row in the tree (1-7, like WoW)
    public int position = 0; // Column position (0-3)
    
    [Header("Requirements")]
    public int pointsRequiredInTree = 0; // Total points needed in tree before this unlocks
    public TalentData prerequisiteTalent; // Must have this talent first (optional)
    public int maxRanks = 1; // How many times can be purchased (1 = single talent, 3-5 = ranked)
    
    [Header("Stat Bonuses - Additive")]
    [Tooltip("Attack damage bonus per rank")]
    public float attackDamageBonus = 0f;
    
    [Tooltip("Max health bonus per rank")]
    public float maxHealthBonus = 0f;
    
    [Tooltip("Attack speed reduction per rank (negative = faster)")]
    public float attackSpeedBonus = 0f;
    
    [Header("Stat Bonuses - Percentage (per rank)")]
    [Tooltip("Damage multiplier per rank (0.05 = 5% more damage)")]
    public float damageMultiplier = 0f;
    
    [Tooltip("Health multiplier per rank (0.1 = 10% more health)")]
    public float healthMultiplier = 0f;
    
    [Tooltip("Critical chance bonus per rank (0.02 = 2% crit)")]
    public float criticalChanceBonus = 0f;
    
    [Tooltip("Critical damage multiplier per rank (0.1 = +10% crit damage, so 2.0x → 2.1x)")]
    public float criticalDamageBonus = 0f;
    
    [Header("Special Stats")]
    [Tooltip("Lifesteal bonus per rank (0.03 = 3% lifesteal)")]
    public float lifestealBonus = 0f;
    
    [Tooltip("Dodge chance per rank (0.02 = 2% dodge)")]
    public float dodgeBonus = 0f;
    
    [Tooltip("Armor/damage reduction per rank (0.02 = 2% reduction)")]
    public float armorBonus = 0f;
    
    [Tooltip("XP gain bonus per rank (0.05 = 5% more XP)")]
    public float xpBonus = 0f;
    
    [Tooltip("Gold gain bonus per rank (0.05 = 5% more gold)")]
    public float goldBonus = 0f;
    
    [Header("Visual")]
    public Color tierColor = Color.white;
    
    /// <summary>
    /// Check if this talent can be unlocked
    /// </summary>
    public bool CanUnlock(int currentRank, int totalPointsInTree, TalentData prerequisiteUnlocked)
    {
        // Already maxed
        if (currentRank >= maxRanks) return false;
        
        // Check tree points requirement
        if (totalPointsInTree < pointsRequiredInTree) return false;
        
        // Check prerequisite talent
        if (prerequisiteTalent != null && prerequisiteUnlocked != prerequisiteTalent) return false;
        
        return true;
    }
    
    /// <summary>
    /// Get full description with current rank bonuses
    /// </summary>
    public string GetFullDescription(int currentRank)
    {
        string desc = description + "\n\n";
        
        if (maxRanks > 1)
        {
            desc += $"<color=yellow>Rank {currentRank}/{maxRanks}</color>\n\n";
        }
        
        // Show bonuses
        if (attackDamageBonus > 0)
            desc += $"+{attackDamageBonus * currentRank:F0} Attack Damage\n";
        
        if (maxHealthBonus > 0)
            desc += $"+{maxHealthBonus * currentRank:F0} Max Health\n";
        
        if (attackSpeedBonus != 0)
            desc += $"{(attackSpeedBonus < 0 ? "" : "+")}{attackSpeedBonus * currentRank:F2}s Attack Speed\n";
        
        if (damageMultiplier > 0)
            desc += $"+{damageMultiplier * currentRank * 100:F0}% Damage\n";
        
        if (healthMultiplier > 0)
            desc += $"+{healthMultiplier * currentRank * 100:F0}% Health\n";
        
        if (criticalChanceBonus > 0)
            desc += $"+{criticalChanceBonus * currentRank * 100:F1}% Critical Chance\n";
        
        if (criticalDamageBonus > 0)
            desc += $"+{criticalDamageBonus * currentRank * 100:F0}% Critical Damage\n";
        
        if (lifestealBonus > 0)
            desc += $"+{lifestealBonus * currentRank * 100:F1}% Lifesteal\n";
        
        if (dodgeBonus > 0)
            desc += $"+{dodgeBonus * currentRank * 100:F1}% Dodge\n";
        
        if (armorBonus > 0)
            desc += $"+{armorBonus * currentRank * 100:F1}% Armor\n";
        
        if (xpBonus > 0)
            desc += $"+{xpBonus * currentRank * 100:F0}% XP Gain\n";
        
        if (goldBonus > 0)
            desc += $"+{goldBonus * currentRank * 100:F0}% Gold Gain\n";
        
        // Show next rank if not maxed
        if (currentRank < maxRanks)
        {
            desc += $"\n<color=green>Next Rank:</color> (Shows increase for next point)";
        }
        
        return desc;
    }
}

/// <summary>
/// Different talent tree specializations
/// </summary>
public enum TalentTree
{
    Combat,     // Offensive talents
    Defense,    // Defensive/survival talents
    Utility     // Gold/XP/quality of life
}





