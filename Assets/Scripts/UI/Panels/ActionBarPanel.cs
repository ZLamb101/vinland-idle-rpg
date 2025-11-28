using UnityEngine;

/// <summary>
/// Action bar panel showing 4 skill slots.
/// Skills are auto-cast on cooldown during combat.
/// </summary>
public class ActionBarPanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject actionBarPanel;
    
    [Header("Skill Slots")]
    public SkillSlotUI[] skillSlots = new SkillSlotUI[4];
    
    // Services
    private ISkillService skillService;
    
    void Start()
    {
        skillService = Services.Get<ISkillService>();
        
        // Subscribe to events
        EventBus.Subscribe<ActionBarChangedEvent>(OnActionBarChanged);
        
        // Initialize slots
        InitializeSlots();
    }
    
    void OnDestroy()
    {
        EventBus.Unsubscribe<ActionBarChangedEvent>(OnActionBarChanged);
    }
    
    void InitializeSlots()
    {
        // Set slot indices
        for (int i = 0; i < skillSlots.Length; i++)
        {
            if (skillSlots[i] != null)
            {
                skillSlots[i].slotIndex = i;
            }
        }
    }
    
    void OnActionBarChanged(ActionBarChangedEvent e)
    {
        // Slots handle their own updates via events
    }
    
    /// <summary>
    /// Toggle action bar visibility
    /// </summary>
    public void TogglePanel()
    {
        if (actionBarPanel != null)
        {
            actionBarPanel.SetActive(!actionBarPanel.activeSelf);
        }
    }
    
    /// <summary>
    /// Show the action bar
    /// </summary>
    public void ShowPanel()
    {
        if (actionBarPanel != null)
        {
            actionBarPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// Hide the action bar
    /// </summary>
    public void HidePanel()
    {
        if (actionBarPanel != null)
        {
            actionBarPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Set skill in a specific slot
    /// </summary>
    public void SetSkillInSlot(int slotIndex, SkillData skill)
    {
        if (slotIndex < 0 || slotIndex >= skillSlots.Length)
            return;
        
        if (skillSlots[slotIndex] != null)
        {
            skillSlots[slotIndex].SetSkill(skill);
        }
    }
    
    /// <summary>
    /// Clear a specific slot
    /// </summary>
    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= skillSlots.Length)
            return;
        
        if (skillSlots[slotIndex] != null)
        {
            skillSlots[slotIndex].ClearSlot();
        }
    }
    
    /// <summary>
    /// Clear all slots
    /// </summary>
    public void ClearAllSlots()
    {
        for (int i = 0; i < skillSlots.Length; i++)
        {
            ClearSlot(i);
        }
    }
}

