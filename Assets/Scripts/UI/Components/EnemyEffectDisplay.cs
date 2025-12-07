using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Component attached to EnemyVisual to display buff/debuff icons above the enemy.
/// Shows up to 3 debuffs and 3 buffs in separate rows.
/// </summary>
public class EnemyEffectDisplay : MonoBehaviour
{
    [Header("Containers")]
    [Tooltip("Container for debuff icons (HorizontalLayoutGroup recommended)")]
    public Transform debuffContainer;
    
    [Tooltip("Container for buff icons (HorizontalLayoutGroup recommended)")]
    public Transform buffContainer;
    
    [Header("Settings")]
    public int maxDebuffsShown = 3;
    public int maxBuffsShown = 3;
    
    [Header("Icon Prefab")]
    public EffectIconUI iconPrefab;
    
    // Monster index this display is tracking
    private int monsterIndex = -1;
    
    // Active icon instances
    private List<EffectIconUI> debuffIcons = new List<EffectIconUI>();
    private List<EffectIconUI> buffIcons = new List<EffectIconUI>();
    
    // Service reference
    private ISkillService skillService;
    
    void Start()
    {
        skillService = Services.Get<ISkillService>();
        
        // Subscribe to effect events
        EventBus.Subscribe<DebuffAppliedEvent>(OnDebuffApplied);
        EventBus.Subscribe<DebuffExpiredEvent>(OnDebuffExpired);
        EventBus.Subscribe<BuffAppliedEvent>(OnBuffApplied);
        EventBus.Subscribe<BuffExpiredEvent>(OnBuffExpired);
    }
    
    void OnDestroy()
    {
        EventBus.Unsubscribe<DebuffAppliedEvent>(OnDebuffApplied);
        EventBus.Unsubscribe<DebuffExpiredEvent>(OnDebuffExpired);
        EventBus.Unsubscribe<BuffAppliedEvent>(OnBuffApplied);
        EventBus.Unsubscribe<BuffExpiredEvent>(OnBuffExpired);
    }
    
    /// <summary>
    /// Set the monster index this display tracks
    /// </summary>
    public void SetMonsterIndex(int index)
    {
        monsterIndex = index;
        RefreshAllEffects();
    }
    
    /// <summary>
    /// Get the monster index this display is tracking
    /// </summary>
    public int GetMonsterIndex()
    {
        return monsterIndex;
    }
    
    void OnDebuffApplied(DebuffAppliedEvent e)
    {
        // Only refresh if this debuff is on our monster
        if (e.targetIndex == monsterIndex)
        {
            RefreshDebuffs();
        }
    }
    
    void OnDebuffExpired(DebuffExpiredEvent e)
    {
        // Check if expired debuff was on our monster
        if (e.targetIndex == monsterIndex)
        {
            RefreshDebuffs();
        }
    }
    
    void OnBuffApplied(BuffAppliedEvent e)
    {
        // Only refresh if this buff is on our monster
        if (e.targetIndex == monsterIndex)
        {
            RefreshBuffs();
        }
    }
    
    void OnBuffExpired(BuffExpiredEvent e)
    {
        // Check if expired buff was on our monster
        if (e.targetIndex == monsterIndex)
        {
            RefreshBuffs();
        }
    }
    
    void Update()
    {
        // Clean up expired icons
        CleanupExpiredIcons(debuffIcons);
        CleanupExpiredIcons(buffIcons);
    }
    
    void CleanupExpiredIcons(List<EffectIconUI> icons)
    {
        for (int i = icons.Count - 1; i >= 0; i--)
        {
            if (icons[i] == null || icons[i].IsExpired())
            {
                if (icons[i] != null)
                {
                    Destroy(icons[i].gameObject);
                }
                icons.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// Refresh all effect icons
    /// </summary>
    public void RefreshAllEffects()
    {
        RefreshDebuffs();
        RefreshBuffs();
    }
    
    void RefreshDebuffs()
    {
        if (monsterIndex < 0 || debuffContainer == null || iconPrefab == null)
            return;
        
        if (skillService == null)
            skillService = Services.Get<ISkillService>();
        
        if (skillService == null)
            return;
        
        // Get all effects on this monster
        List<ActiveEffect> effects = skillService.GetMonsterEffects(monsterIndex);
        
        // Filter to debuffs only
        List<ActiveEffect> debuffs = new List<ActiveEffect>();
        foreach (var effect in effects)
        {
            if (effect != null && !effect.IsExpired() && !effect.isBuff)
            {
                debuffs.Add(effect);
            }
        }
        
        // Update icons
        UpdateIconList(debuffs, debuffIcons, debuffContainer, maxDebuffsShown, false);
    }
    
    void RefreshBuffs()
    {
        if (monsterIndex < 0 || buffContainer == null || iconPrefab == null)
            return;
        
        if (skillService == null)
            skillService = Services.Get<ISkillService>();
        
        if (skillService == null)
            return;
        
        // Get all effects on this monster
        List<ActiveEffect> effects = skillService.GetMonsterEffects(monsterIndex);
        
        // Filter to buffs only
        List<ActiveEffect> buffs = new List<ActiveEffect>();
        foreach (var effect in effects)
        {
            if (effect != null && !effect.IsExpired() && effect.isBuff)
            {
                buffs.Add(effect);
            }
        }
        
        // Update icons
        UpdateIconList(buffs, buffIcons, buffContainer, maxBuffsShown, true);
    }
    
    void UpdateIconList(List<ActiveEffect> effects, List<EffectIconUI> icons, Transform container, int maxShown, bool isBuff)
    {
        // Remove icons for effects that no longer exist
        for (int i = icons.Count - 1; i >= 0; i--)
        {
            EffectIconUI icon = icons[i];
            if (icon == null)
            {
                icons.RemoveAt(i);
                continue;
            }
            
            // Check if effect still exists
            bool found = false;
            foreach (var effect in effects)
            {
                if (icon.MatchesEffect(effect))
                {
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                Destroy(icon.gameObject);
                icons.RemoveAt(i);
            }
        }
        
        // Add icons for new effects (up to max)
        int shown = 0;
        foreach (var effect in effects)
        {
            if (shown >= maxShown)
                break;
            
            // Check if already displayed
            bool exists = false;
            foreach (var icon in icons)
            {
                if (icon != null && icon.MatchesEffect(effect))
                {
                    exists = true;
                    shown++;
                    break;
                }
            }
            
            if (!exists && icons.Count < maxShown)
            {
                EffectIconUI newIcon = Instantiate(iconPrefab, container);
                newIcon.Setup(effect, isBuff);
                icons.Add(newIcon);
                shown++;
            }
        }
    }
    
    /// <summary>
    /// Clear all effect icons
    /// </summary>
    public void ClearAllIcons()
    {
        foreach (var icon in debuffIcons)
        {
            if (icon != null)
                Destroy(icon.gameObject);
        }
        debuffIcons.Clear();
        
        foreach (var icon in buffIcons)
        {
            if (icon != null)
                Destroy(icon.gameObject);
        }
        buffIcons.Clear();
    }
}
