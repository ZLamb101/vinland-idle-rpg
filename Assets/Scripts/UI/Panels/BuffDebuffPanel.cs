using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Panel showing active buffs and debuffs on the player.
/// Displays effect icons with radial countdown timers.
/// </summary>
public class BuffDebuffPanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject buffDebuffPanel;
    
    [Header("Buff Section")]
    public Transform buffContainer;
    public EffectIconUI buffEntryPrefab;
    public TextMeshProUGUI buffHeaderText;
    
    [Header("Debuff Section")]
    public Transform debuffContainer;
    public EffectIconUI debuffEntryPrefab;
    public TextMeshProUGUI debuffHeaderText;
    
    [Header("Settings")]
    [Tooltip("Show panel only during combat")]
    public bool showOnlyInCombat = true;
    [Tooltip("Hide section headers when no effects")]
    public bool hideEmptyHeaders = true;
    
    // Services
    private ISkillService skillService;
    
    // Active UI entries
    private List<EffectIconUI> buffEntries = new List<EffectIconUI>();
    private List<EffectIconUI> debuffEntries = new List<EffectIconUI>();
    
    void Start()
    {
        skillService = Services.Get<ISkillService>();
        
        // Subscribe to events
        EventBus.Subscribe<BuffAppliedEvent>(OnBuffApplied);
        EventBus.Subscribe<BuffExpiredEvent>(OnBuffExpired);
        EventBus.Subscribe<DebuffAppliedEvent>(OnDebuffApplied);
        EventBus.Subscribe<DebuffExpiredEvent>(OnDebuffExpired);
        EventBus.Subscribe<CombatStateChangedEvent>(OnCombatStateChanged);
        EventBus.Subscribe<CombatEndedEvent>(OnCombatEnded);
        
        // Initial state
        if (showOnlyInCombat && buffDebuffPanel != null)
        {
            buffDebuffPanel.SetActive(false);
        }
    }
    
    void OnDestroy()
    {
        EventBus.Unsubscribe<BuffAppliedEvent>(OnBuffApplied);
        EventBus.Unsubscribe<BuffExpiredEvent>(OnBuffExpired);
        EventBus.Unsubscribe<DebuffAppliedEvent>(OnDebuffApplied);
        EventBus.Unsubscribe<DebuffExpiredEvent>(OnDebuffExpired);
        EventBus.Unsubscribe<CombatStateChangedEvent>(OnCombatStateChanged);
        EventBus.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
    }
    
    void Update()
    {
        // Clean up expired icons
        CleanupExpiredIcons(buffEntries);
        CleanupExpiredIcons(debuffEntries);
        UpdateHeaders();
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
    
    void OnBuffApplied(BuffAppliedEvent e)
    {
        if (e.isOnPlayer)
        {
            RefreshBuffs();
        }
    }
    
    void OnBuffExpired(BuffExpiredEvent e)
    {
        if (e.wasOnPlayer)
        {
            RefreshBuffs();
        }
    }
    
    void OnDebuffApplied(DebuffAppliedEvent e)
    {
        if (e.isOnPlayer)
        {
            RefreshDebuffs();
        }
    }
    
    void OnDebuffExpired(DebuffExpiredEvent e)
    {
        if (e.wasOnPlayer)
        {
            RefreshDebuffs();
        }
    }
    
    void OnCombatStateChanged(CombatStateChangedEvent e)
    {
        if (showOnlyInCombat && buffDebuffPanel != null)
        {
            bool inCombat = e.newState == CombatManager.CombatState.Fighting;
            buffDebuffPanel.SetActive(inCombat);
            
            if (inCombat)
            {
                RefreshDisplay();
            }
        }
    }
    
    void OnCombatEnded(CombatEndedEvent e)
    {
        ClearAllEntries();
        
        if (showOnlyInCombat && buffDebuffPanel != null)
        {
            buffDebuffPanel.SetActive(false);
        }
    }
    
    void RefreshDisplay()
    {
        RefreshBuffs();
        RefreshDebuffs();
        UpdateHeaders();
    }
    
    void RefreshBuffs()
    {
        if (skillService == null)
        {
            skillService = Services.Get<ISkillService>();
        }
        
        if (skillService == null || buffContainer == null || buffEntryPrefab == null)
            return;
        
        List<ActiveEffect> buffs = skillService.GetPlayerBuffs();
        
        // Remove entries for effects that no longer exist
        for (int i = buffEntries.Count - 1; i >= 0; i--)
        {
            EffectIconUI entry = buffEntries[i];
            if (entry == null)
            {
                buffEntries.RemoveAt(i);
                continue;
            }
            
            // Check if effect still exists
            bool found = false;
            foreach (var buff in buffs)
            {
                if (entry.MatchesEffect(buff))
                {
                    found = true;
                    break;
                }
            }
            
            if (!found || entry.IsExpired())
            {
                Destroy(entry.gameObject);
                buffEntries.RemoveAt(i);
            }
        }
        
        // Add new buffs
        foreach (ActiveEffect buff in buffs)
        {
            if (buff == null || buff.IsExpired())
                continue;
            
            // Check if already displayed
            bool exists = false;
            foreach (var entry in buffEntries)
            {
                if (entry != null && entry.MatchesEffect(buff))
                {
                    exists = true;
                    break;
                }
            }
            
            if (!exists)
            {
                EffectIconUI newEntry = Instantiate(buffEntryPrefab, buffContainer);
                newEntry.Setup(buff, true);
                buffEntries.Add(newEntry);
            }
        }
    }
    
    void RefreshDebuffs()
    {
        if (skillService == null)
        {
            skillService = Services.Get<ISkillService>();
        }
        
        if (skillService == null || debuffContainer == null || debuffEntryPrefab == null)
            return;
        
        List<ActiveEffect> debuffs = skillService.GetPlayerDebuffs();
        
        // Remove entries for effects that no longer exist
        for (int i = debuffEntries.Count - 1; i >= 0; i--)
        {
            EffectIconUI entry = debuffEntries[i];
            if (entry == null)
            {
                debuffEntries.RemoveAt(i);
                continue;
            }
            
            // Check if effect still exists
            bool found = false;
            foreach (var debuff in debuffs)
            {
                if (entry.MatchesEffect(debuff))
                {
                    found = true;
                    break;
                }
            }
            
            if (!found || entry.IsExpired())
            {
                Destroy(entry.gameObject);
                debuffEntries.RemoveAt(i);
            }
        }
        
        // Add new debuffs
        foreach (ActiveEffect debuff in debuffs)
        {
            if (debuff == null || debuff.IsExpired())
                continue;
            
            // Check if already displayed
            bool exists = false;
            foreach (var entry in debuffEntries)
            {
                if (entry != null && entry.MatchesEffect(debuff))
                {
                    exists = true;
                    break;
                }
            }
            
            if (!exists)
            {
                EffectIconUI newEntry = Instantiate(debuffEntryPrefab, debuffContainer);
                newEntry.Setup(debuff, false);
                debuffEntries.Add(newEntry);
            }
        }
    }
    
    void UpdateHeaders()
    {
        if (hideEmptyHeaders)
        {
            if (buffHeaderText != null)
            {
                buffHeaderText.gameObject.SetActive(buffEntries.Count > 0);
            }
            
            if (debuffHeaderText != null)
            {
                debuffHeaderText.gameObject.SetActive(debuffEntries.Count > 0);
            }
        }
    }
    
    void ClearAllEntries()
    {
        foreach (var entry in buffEntries)
        {
            if (entry != null && entry.gameObject != null)
            {
                Destroy(entry.gameObject);
            }
        }
        buffEntries.Clear();
        
        foreach (var entry in debuffEntries)
        {
            if (entry != null && entry.gameObject != null)
            {
                Destroy(entry.gameObject);
            }
        }
        debuffEntries.Clear();
    }
    
    /// <summary>
    /// Toggle panel visibility
    /// </summary>
    public void TogglePanel()
    {
        if (buffDebuffPanel != null)
        {
            bool newState = !buffDebuffPanel.activeSelf;
            buffDebuffPanel.SetActive(newState);
            
            if (newState)
            {
                RefreshDisplay();
            }
        }
    }
    
    /// <summary>
    /// Show the panel
    /// </summary>
    public void ShowPanel()
    {
        if (buffDebuffPanel != null)
        {
            buffDebuffPanel.SetActive(true);
            RefreshDisplay();
        }
    }
    
    /// <summary>
    /// Hide the panel
    /// </summary>
    public void HidePanel()
    {
        if (buffDebuffPanel != null)
        {
            buffDebuffPanel.SetActive(false);
        }
    }
}

