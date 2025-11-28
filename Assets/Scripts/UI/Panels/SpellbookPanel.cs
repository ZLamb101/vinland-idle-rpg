using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Spellbook panel showing all available skills.
/// Skills can be dragged to the action bar.
/// </summary>
public class SpellbookPanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject spellbookPanel;
    
    [Header("Content")]
    public Transform skillListContent;
    public SpellbookSkillUI skillEntryPrefab;
    
    [Header("Filters")]
    public Toggle showAllToggle;
    public Toggle showDirectToggle;
    public Toggle showBuffsToggle;
    public Toggle showCursesToggle;
    
    [Header("Search")]
    public TMP_InputField searchInput;
    
    [Header("Canvas")]
    public Canvas parentCanvas;
    
    // Services
    private ISkillService skillService;
    
    // Skill list
    private List<SkillData> allSkills = new List<SkillData>();
    private List<SpellbookSkillUI> skillEntries = new List<SpellbookSkillUI>();
    
    // Current filter
    private SkillType? currentTypeFilter = null;
    private string currentSearchFilter = "";
    
    void Start()
    {
        skillService = Services.Get<ISkillService>();
        
        // Find parent canvas if not set
        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }
        
        // Setup filter toggles
        SetupFilters();
        
        // Setup search
        if (searchInput != null)
        {
            searchInput.onValueChanged.AddListener(OnSearchChanged);
        }
        
        // Hide panel initially
        if (spellbookPanel != null)
        {
            spellbookPanel.SetActive(false);
        }
    }
    
    void OnEnable()
    {
        RefreshSkillList();
    }
    
    void SetupFilters()
    {
        if (showAllToggle != null)
        {
            showAllToggle.onValueChanged.AddListener(isOn => { if (isOn) SetFilter(null); });
            showAllToggle.isOn = true;
        }
        
        if (showDirectToggle != null)
        {
            showDirectToggle.onValueChanged.AddListener(isOn => { if (isOn) SetFilter(SkillType.Direct); });
        }
        
        if (showBuffsToggle != null)
        {
            showBuffsToggle.onValueChanged.AddListener(isOn => { if (isOn) SetFilter(SkillType.Buff); });
        }
        
        if (showCursesToggle != null)
        {
            showCursesToggle.onValueChanged.AddListener(isOn => { if (isOn) SetFilter(SkillType.Curse); });
        }
    }
    
    void SetFilter(SkillType? typeFilter)
    {
        currentTypeFilter = typeFilter;
        RefreshSkillList();
    }
    
    void OnSearchChanged(string searchText)
    {
        currentSearchFilter = searchText.ToLower();
        RefreshSkillList();
    }
    
    void RefreshSkillList()
    {
        // Get all available skills
        if (skillService == null)
        {
            skillService = Services.Get<ISkillService>();
        }
        
        if (skillService != null)
        {
            allSkills = skillService.GetAllAvailableSkills();
        }
        
        // Clear existing entries
        foreach (var entry in skillEntries)
        {
            if (entry != null && entry.gameObject != null)
            {
                Destroy(entry.gameObject);
            }
        }
        skillEntries.Clear();
        
        if (skillListContent == null || skillEntryPrefab == null)
            return;
        
        // Filter and create entries
        foreach (SkillData skill in allSkills)
        {
            if (skill == null) continue;
            
            // Apply type filter
            if (currentTypeFilter.HasValue && skill.skillType != currentTypeFilter.Value)
                continue;
            
            // Apply search filter
            if (!string.IsNullOrEmpty(currentSearchFilter))
            {
                if (!skill.skillName.ToLower().Contains(currentSearchFilter) &&
                    !skill.description.ToLower().Contains(currentSearchFilter))
                {
                    continue;
                }
            }
            
            // Create entry
            SpellbookSkillUI entry = Instantiate(skillEntryPrefab, skillListContent);
            entry.Setup(skill, parentCanvas);
            skillEntries.Add(entry);
        }
    }
    
    /// <summary>
    /// Toggle spellbook visibility
    /// </summary>
    public void TogglePanel()
    {
        if (spellbookPanel != null)
        {
            bool newState = !spellbookPanel.activeSelf;
            spellbookPanel.SetActive(newState);
            
            if (newState)
            {
                RefreshSkillList();
            }
        }
    }
    
    /// <summary>
    /// Show the spellbook
    /// </summary>
    public void ShowPanel()
    {
        if (spellbookPanel != null)
        {
            spellbookPanel.SetActive(true);
            RefreshSkillList();
        }
    }
    
    /// <summary>
    /// Hide the spellbook
    /// </summary>
    public void HidePanel()
    {
        if (spellbookPanel != null)
        {
            spellbookPanel.SetActive(false);
        }
    }
}

