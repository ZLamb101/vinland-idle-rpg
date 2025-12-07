using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI component that displays target information in the top right corner.
/// Shows monster name, health, and swing timer for the currently targeted monster.
/// </summary>
public class TargetFrame : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI monsterNameText;
    public Slider healthBar;
    public TextMeshProUGUI healthText;
    public Slider swingTimerBar;
    public TextMeshProUGUI swingTimerText;
    public GameObject targetFramePanel; // The panel container (for showing/hiding)
    
    [Header("Effects Display")]
    public Transform debuffContainer;  // Container for debuff icons (HorizontalLayoutGroup)
    public Transform buffContainer;    // Container for buff icons (HorizontalLayoutGroup)
    public EffectIconUI effectIconPrefab;
    public int maxDebuffsShown = 3;
    public int maxBuffsShown = 3;
    
    private int currentTargetIndex = -1;
    private ICombatService combatService; // Cached combat service reference
    private ISkillService skillService;   // For getting monster effects
    
    // Active effect icons
    private List<EffectIconUI> activeDebuffIcons = new List<EffectIconUI>();
    private List<EffectIconUI> activeBuffIcons = new List<EffectIconUI>();
    
    void Start()
    {
        // Get services (doesn't log errors during scene transitions)
        if (Services.TryGet<ICombatService>(out combatService))
        {
            SubscribeToEvents();
            // Check if combat is already active and update immediately
            CheckCurrentCombatState();
        }
        else
        {
            // Try again after a short delay
            StartCoroutine(RetryGetCombatService());
        }
        
        skillService = Services.Get<ISkillService>();
        
        // Hide initially (will be shown by CheckCurrentCombatState if combat is active)
        if (targetFramePanel != null && (combatService == null || combatService.GetCombatState() != CombatManager.CombatState.Fighting))
        {
            targetFramePanel.SetActive(false);
        }
    }
    
    void SubscribeToEvents()
    {
        EventBus.Subscribe<TargetChangedEvent>(OnTargetChanged);
        EventBus.Subscribe<MonsterHealthChangedEvent>(OnMonsterHealthChanged);
        EventBus.Subscribe<MonsterAttackProgressEvent>(OnMonsterAttackProgress);
        EventBus.Subscribe<MonstersChangedEvent>(OnMonstersChanged);
        EventBus.Subscribe<CombatStateChangedEvent>(OnCombatStateChanged);
        
        // Effect events
        EventBus.Subscribe<DebuffAppliedEvent>(OnDebuffApplied);
        EventBus.Subscribe<DebuffExpiredEvent>(OnDebuffExpired);
        EventBus.Subscribe<BuffAppliedEvent>(OnBuffApplied);
        EventBus.Subscribe<BuffExpiredEvent>(OnBuffExpired);
    }
    
    /// <summary>
    /// Check if combat is already active and update the target frame accordingly.
    /// This handles the case where combat started before we subscribed to events.
    /// </summary>
    void CheckCurrentCombatState()
    {
        if (combatService == null)
            return;
        
        // If already in combat, show the target frame with current target
        if (combatService.GetCombatState() == CombatManager.CombatState.Fighting)
        {
            currentTargetIndex = combatService.GetCurrentTargetIndex();
            UpdateTargetFrame();
        }
    }
    
    System.Collections.IEnumerator RetryGetCombatService()
    {
        yield return new WaitForSeconds(0.2f);
        
        if (Services.TryGet<ICombatService>(out combatService))
        {
            SubscribeToEvents();
            // Check if combat is already active (we might have missed the initial events)
            CheckCurrentCombatState();
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from EventBus
        EventBus.Unsubscribe<TargetChangedEvent>(OnTargetChanged);
        EventBus.Unsubscribe<MonsterHealthChangedEvent>(OnMonsterHealthChanged);
        EventBus.Unsubscribe<MonsterAttackProgressEvent>(OnMonsterAttackProgress);
        EventBus.Unsubscribe<MonstersChangedEvent>(OnMonstersChanged);
        EventBus.Unsubscribe<CombatStateChangedEvent>(OnCombatStateChanged);
        
        // Effect events
        EventBus.Unsubscribe<DebuffAppliedEvent>(OnDebuffApplied);
        EventBus.Unsubscribe<DebuffExpiredEvent>(OnDebuffExpired);
        EventBus.Unsubscribe<BuffAppliedEvent>(OnBuffApplied);
        EventBus.Unsubscribe<BuffExpiredEvent>(OnBuffExpired);
    }
    
    void OnCombatStateChanged(CombatStateChangedEvent e)
    {
        // Hide target frame when not in combat
        if (targetFramePanel != null)
        {
            targetFramePanel.SetActive(e.newState == CombatManager.CombatState.Fighting);
        }
        
        if (e.newState != CombatManager.CombatState.Fighting)
        {
            currentTargetIndex = -1;
            ClearAllEffectIcons();
        }
    }
    
    void OnMonstersChanged(MonstersChangedEvent e)
    {
        // Update target frame when monsters spawn
        if (e.monsters != null && e.monsters.Count > 0 && combatService != null)
        {
            currentTargetIndex = combatService.GetCurrentTargetIndex();
            UpdateTargetFrame();
        }
    }
    
    void OnTargetChanged(TargetChangedEvent e)
    {
        currentTargetIndex = e.newTargetIndex;
        UpdateTargetFrame();
    }
    
    void OnMonsterHealthChanged(MonsterHealthChangedEvent e)
    {
        if (e.monsterIndex == currentTargetIndex)
        {
            UpdateHealthDisplay(e.currentHealth, e.maxHealth);
        }
    }
    
    void OnMonsterAttackProgress(MonsterAttackProgressEvent e)
    {
        if (e.monsterIndex == currentTargetIndex)
        {
            UpdateSwingTimer(e.progress);
        }
    }
    
    void UpdateTargetFrame()
    {
        if (combatService == null)
            return;
        
        var target = combatService.GetCurrentTargetInstance();
        if (target == null || target.monsterData == null)
        {
            // Hide target frame if no valid target
            if (targetFramePanel != null)
            {
                targetFramePanel.SetActive(false);
            }
            ClearAllEffectIcons();
            return;
        }
        
        // Show target frame
        if (targetFramePanel != null)
        {
            targetFramePanel.SetActive(true);
        }
        
        // Update monster name
        if (monsterNameText != null)
        {
            monsterNameText.text = target.monsterData.monsterName;
        }
        
        // Update health
        UpdateHealthDisplay(target.currentHealth, target.maxHealth);
        
        // Reset swing timer (will be updated by OnMonsterAttackProgress)
        UpdateSwingTimer(0f);
        
        // Update effect icons for new target
        RefreshEffects();
    }
    
    void UpdateHealthDisplay(float current, float max)
    {
        float displayCurrent = Mathf.Max(0f, current);
        
        if (healthBar != null)
        {
            healthBar.maxValue = max;
            healthBar.value = displayCurrent;
        }
        
        if (healthText != null)
        {
            healthText.text = $"{displayCurrent:F0} / {max:F0}";
        }
    }
    
    void UpdateSwingTimer(float progress)
    {
        if (swingTimerBar != null)
        {
            swingTimerBar.value = Mathf.Clamp01(progress);
        }
        
        if (swingTimerText != null)
        {
            // Show percentage or time remaining
            float percentage = progress * 100f;
            swingTimerText.text = $"{percentage:F0}%";
        }
    }
    
    // ==================== Effect Display ====================
    
    void OnDebuffApplied(DebuffAppliedEvent e)
    {
        if (e.targetIndex == currentTargetIndex)
        {
            RefreshEffects();
        }
    }
    
    void OnDebuffExpired(DebuffExpiredEvent e)
    {
        if (e.targetIndex == currentTargetIndex)
        {
            RefreshEffects();
        }
    }
    
    void OnBuffApplied(BuffAppliedEvent e)
    {
        if (e.targetIndex == currentTargetIndex)
        {
            RefreshEffects();
        }
    }
    
    void OnBuffExpired(BuffExpiredEvent e)
    {
        if (e.targetIndex == currentTargetIndex)
        {
            RefreshEffects();
        }
    }
    
    void RefreshEffects()
    {
        if (currentTargetIndex < 0 || effectIconPrefab == null)
            return;
        
        if (skillService == null)
            skillService = Services.Get<ISkillService>();
        
        if (skillService == null)
            return;
        
        // Get all effects on current target
        List<ActiveEffect> effects = skillService.GetMonsterEffects(currentTargetIndex);
        
        // Separate into buffs and debuffs
        List<ActiveEffect> buffs = new List<ActiveEffect>();
        List<ActiveEffect> debuffs = new List<ActiveEffect>();
        
        foreach (var effect in effects)
        {
            if (effect == null || effect.IsExpired())
                continue;
            
            if (effect.isBuff)
                buffs.Add(effect);
            else
                debuffs.Add(effect);
        }
        
        // Update debuff icons
        if (debuffContainer != null)
        {
            UpdateEffectIcons(debuffs, activeDebuffIcons, debuffContainer, maxDebuffsShown, false);
        }
        
        // Update buff icons
        if (buffContainer != null)
        {
            UpdateEffectIcons(buffs, activeBuffIcons, buffContainer, maxBuffsShown, true);
        }
    }
    
    void UpdateEffectIcons(List<ActiveEffect> effects, List<EffectIconUI> icons, Transform container, int maxShown, bool isBuff)
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
            
            if (!found || icon.IsExpired())
            {
                Destroy(icon.gameObject);
                icons.RemoveAt(i);
            }
        }
        
        // Add icons for new effects (up to max)
        int shown = icons.Count;
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
                    break;
                }
            }
            
            if (!exists)
            {
                EffectIconUI newIcon = Instantiate(effectIconPrefab, container);
                newIcon.Setup(effect, isBuff);
                icons.Add(newIcon);
                shown++;
            }
        }
    }
    
    void ClearAllEffectIcons()
    {
        foreach (var icon in activeDebuffIcons)
        {
            if (icon != null)
                Destroy(icon.gameObject);
        }
        activeDebuffIcons.Clear();
        
        foreach (var icon in activeBuffIcons)
        {
            if (icon != null)
                Destroy(icon.gameObject);
        }
        activeBuffIcons.Clear();
    }
}

