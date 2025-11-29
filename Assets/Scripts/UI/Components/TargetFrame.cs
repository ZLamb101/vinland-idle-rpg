using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    
    private int currentTargetIndex = -1;
    private ICombatService combatService; // Cached combat service reference
    
    void Start()
    {
        // Get combat service (doesn't log errors during scene transitions)
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
}

