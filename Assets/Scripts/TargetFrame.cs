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
    
    void Start()
    {
        // Subscribe to combat events
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnTargetChanged += OnTargetChanged;
            CombatManager.Instance.OnMonsterHealthChanged += OnMonsterHealthChanged;
            CombatManager.Instance.OnMonsterAttackProgress += OnMonsterAttackProgress;
            CombatManager.Instance.OnMonstersChanged += OnMonstersChanged;
            CombatManager.Instance.OnCombatStateChanged += OnCombatStateChanged;
        }
        
        // Hide initially
        if (targetFramePanel != null)
        {
            targetFramePanel.SetActive(false);
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnTargetChanged -= OnTargetChanged;
            CombatManager.Instance.OnMonsterHealthChanged -= OnMonsterHealthChanged;
            CombatManager.Instance.OnMonsterAttackProgress -= OnMonsterAttackProgress;
            CombatManager.Instance.OnMonstersChanged -= OnMonstersChanged;
            CombatManager.Instance.OnCombatStateChanged -= OnCombatStateChanged;
        }
    }
    
    void OnCombatStateChanged(CombatManager.CombatState state)
    {
        // Hide target frame when not in combat
        if (targetFramePanel != null)
        {
            targetFramePanel.SetActive(state == CombatManager.CombatState.Fighting);
        }
        
        if (state != CombatManager.CombatState.Fighting)
        {
            currentTargetIndex = -1;
        }
    }
    
    void OnMonstersChanged(System.Collections.Generic.List<MonsterData> monsters)
    {
        // Update target frame when monsters spawn
        if (monsters != null && monsters.Count > 0 && CombatManager.Instance != null)
        {
            currentTargetIndex = CombatManager.Instance.GetCurrentTargetIndex();
            UpdateTargetFrame();
        }
    }
    
    void OnTargetChanged(int targetIndex)
    {
        currentTargetIndex = targetIndex;
        UpdateTargetFrame();
    }
    
    void OnMonsterHealthChanged(float current, float max, int index)
    {
        if (index == currentTargetIndex)
        {
            UpdateHealthDisplay(current, max);
        }
    }
    
    void OnMonsterAttackProgress(float progress, int index)
    {
        if (index == currentTargetIndex)
        {
            UpdateSwingTimer(progress);
        }
    }
    
    void UpdateTargetFrame()
    {
        if (CombatManager.Instance == null)
            return;
        
        var target = CombatManager.Instance.GetCurrentTargetInstance();
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

