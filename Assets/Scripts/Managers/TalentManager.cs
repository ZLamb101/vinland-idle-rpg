using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager for the talent system.
/// Tracks unspent points, unlocked talents, and calculates total bonuses.
/// </summary>
public class TalentManager : MonoBehaviour, ITalentService
{
    
    [Header("Talent Points")]
    private int unspentTalentPoints = 0;
    private int totalTalentPoints = 0; // Total earned across all levels
    
    [Header("Unlocked Talents")]
    private Dictionary<TalentData, int> unlockedTalents = new Dictionary<TalentData, int>(); // Talent → current rank
    
    // Cached total bonuses from all talents
    private TalentBonuses totalBonuses = new TalentBonuses();
    
    // Events migrated to EventBus - see GameEvent.cs for event types

    private ICharacterService characterService;
    
    void Awake()
    {
        if (Services.IsRegistered<ITalentService>())
        {
            Debug.LogWarning("[TalentManager] Service already registered. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        
        // Register with service locator
        Services.Register<ITalentService>(this);
    }
    
    void Start()
    {
        characterService = Services.Get<ICharacterService>();
        // Subscribe to level up events
        if (characterService != null)
        {
            EventBus.Subscribe<CharacterLevelChangedEvent>(OnPlayerLevelUp);
        }
        
        RecalculateBonuses();
    }
    
    void OnDestroy()
    {
        EventBus.Unsubscribe<CharacterLevelChangedEvent>(OnPlayerLevelUp);
        
        Services.Unregister<ITalentService>();
    }
    
    /// <summary>
    /// Award talent point when player levels up
    /// </summary>
    void OnPlayerLevelUp(CharacterLevelChangedEvent e)
    {
        AddTalentPoints(1);
    }
    
    /// <summary>
    /// Add talent points
    /// </summary>
    public void AddTalentPoints(int amount)
    {
        unspentTalentPoints += amount;
        totalTalentPoints += amount;
        EventBus.Publish(new TalentPointsChangedEvent { unspentPoints = unspentTalentPoints, totalPoints = totalTalentPoints });
    }
    
    /// <summary>
    /// Try to unlock or upgrade a talent
    /// </summary>
    public bool UnlockTalent(TalentData talent)
    {
        if (talent == null) return false;
        
        // Check if we have points
        if (unspentTalentPoints <= 0)
        {
            return false;
        }
        
        // Get current rank
        int currentRank = GetTalentRank(talent);
        
        // Check if can unlock
        int pointsInTree = GetTotalPointsInTree(talent.talentTree);
        TalentData prereq = GetPrerequisiteTalent(talent);
        
        if (!talent.CanUnlock(currentRank, pointsInTree, prereq))
        {
            return false;
        }
        
        // Unlock talent
        int newRank = currentRank + 1;
        unlockedTalents[talent] = newRank;
        unspentTalentPoints--;
        
        // Recalculate bonuses
        RecalculateBonuses();
        
        // Notify listeners
        EventBus.Publish(new TalentPointsChangedEvent { unspentPoints = unspentTalentPoints, totalPoints = totalTalentPoints });
        EventBus.Publish(new TalentUnlockedEvent { talent = talent, newRank = newRank, talentPointsRemaining = unspentTalentPoints });
        return true;
    }
    
    /// <summary>
    /// Get current rank of a talent (0 if not unlocked)
    /// </summary>
    public int GetTalentRank(TalentData talent)
    {
        return unlockedTalents.ContainsKey(talent) ? unlockedTalents[talent] : 0;
    }
    
    /// <summary>
    /// Get total points spent in a specific tree
    /// </summary>
    public int GetTotalPointsInTree(TalentTree tree)
    {
        int total = 0;
        foreach (var kvp in unlockedTalents)
        {
            if (kvp.Key.talentTree == tree)
            {
                total += kvp.Value;
            }
        }
        return total;
    }
    
    /// <summary>
    /// Check if prerequisite talent is unlocked
    /// </summary>
    TalentData GetPrerequisiteTalent(TalentData talent)
    {
        if (talent.prerequisiteTalent == null) return null;
        
        int prereqRank = GetTalentRank(talent.prerequisiteTalent);
        return prereqRank >= talent.prerequisiteTalent.maxRanks ? talent.prerequisiteTalent : null;
    }
    
    /// <summary>
    /// Recalculate all bonuses from talents
    /// </summary>
    void RecalculateBonuses()
    {
        totalBonuses = new TalentBonuses();
        
        foreach (var kvp in unlockedTalents)
        {
            TalentData talent = kvp.Key;
            int rank = kvp.Value;
            
            // Add up all bonuses
            totalBonuses.attackDamage += talent.attackDamageBonus * rank;
            totalBonuses.maxHealth += talent.maxHealthBonus * rank;
            totalBonuses.attackSpeed += talent.attackSpeedBonus * rank;
            totalBonuses.damageMultiplier += talent.damageMultiplier * rank;
            totalBonuses.healthMultiplier += talent.healthMultiplier * rank;
            totalBonuses.criticalChance += talent.criticalChanceBonus * rank;
            totalBonuses.criticalDamage += talent.criticalDamageBonus * rank;
            totalBonuses.lifesteal += talent.lifestealBonus * rank;
            totalBonuses.dodge += talent.dodgeBonus * rank;
            totalBonuses.armor += talent.armorBonus * rank;
            totalBonuses.xpBonus += talent.xpBonus * rank;
            totalBonuses.goldBonus += talent.goldBonus * rank;
        }
        
        EventBus.Publish(new TalentBonusesRecalculatedEvent { talentBonuses = totalBonuses });
    }
    
    /// <summary>
    /// Reset all talents (for respec)
    /// </summary>
    public void ResetTalents()
    {
        int pointsToRefund = 0;
        foreach (var rank in unlockedTalents.Values)
        {
            pointsToRefund += rank;
        }
        
        unlockedTalents.Clear();
        unspentTalentPoints += pointsToRefund;
        
        RecalculateBonuses();
        EventBus.Publish(new TalentPointsChangedEvent { unspentPoints = unspentTalentPoints, totalPoints = totalTalentPoints });
    }
    
    // Getters
    public int GetUnspentPoints() => unspentTalentPoints;
    public int GetTotalPoints() => totalTalentPoints;
    public TalentBonuses GetTotalBonuses() => totalBonuses;
    public Dictionary<TalentData, int> GetAllUnlockedTalents() => new Dictionary<TalentData, int>(unlockedTalents);
}

/// <summary>
/// Container for total talent bonuses
/// </summary>
[System.Serializable]
public class TalentBonuses
{
    // Additive bonuses
    public float attackDamage = 0f;
    public float maxHealth = 0f;
    public float attackSpeed = 0f;
    
    // Percentage multipliers
    public float damageMultiplier = 0f;
    public float healthMultiplier = 0f;
    public float criticalChance = 0f;
    public float criticalDamage = 0f; // Added to crit multiplier (base 2.0x)
    
    // Special stats
    public float lifesteal = 0f;
    public float dodge = 0f;
    public float armor = 0f;
    public float xpBonus = 0f;
    public float goldBonus = 0f;
}





