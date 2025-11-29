using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI panel for displaying and interacting with the talent tree.
/// Shows available talents, allows spending points, and displays bonuses.
/// </summary>
public class TalentPanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject talentPanel;
    
    [Header("Panel Controller")]
    public UIPanelController panelController;
    
    [Header("Talent Tree Tabs")]
    public Button combatTab;
    public Button defenseTab;
    public Button utilityTab;
    private TalentTree currentTree = TalentTree.Combat;
    
    [Header("Talent Display")]
    public Transform talentContainer; // Parent for talent buttons
    public GameObject talentButtonPrefab; // Prefab for each talent
    
    [Header("Info Display")]
    public TextMeshProUGUI talentPointsText;
    public TextMeshProUGUI treeSummaryText; // Shows points in current tree
    
    [Header("Controls")]
    public Button closeButton;
    public Button resetButton;
    // Reset cost now comes from GameBalance.Economy.talentResetCost
    
    [Header("Talent Data")]
    public TalentData[] allTalents; // Assign all talent assets here
    
    private Dictionary<TalentData, TalentButton> talentButtons = new Dictionary<TalentData, TalentButton>();
    private ITalentService talentService; // Cached talent service reference
    
    void Start()
    {
        // Get talent service
        talentService = Services.Get<ITalentService>();
        
        // Subscribe to events
        if (talentService != null)
        {
            EventBus.Subscribe<TalentPointsChangedEvent>(UpdatePointsDisplay);
            EventBus.Subscribe<TalentUnlockedEvent>(OnTalentUnlocked);
        }
        
        // Setup tab buttons
        if (combatTab != null)
            combatTab.onClick.AddListener(() => SwitchTree(TalentTree.Combat));
        if (defenseTab != null)
            defenseTab.onClick.AddListener(() => SwitchTree(TalentTree.Defense));
        if (utilityTab != null)
            utilityTab.onClick.AddListener(() => SwitchTree(TalentTree.Utility));
        
        // Setup close button
        if (closeButton != null)
            closeButton.onClick.AddListener(HidePanel);
        
        // Setup reset button
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetTalents);
        
        // Hide panel initially
        if (talentPanel != null)
            talentPanel.SetActive(false);
        
        // Initialize
        CreateTalentButtons();
        UpdatePointsDisplay(new TalentPointsChangedEvent 
        { 
            unspentPoints = talentService?.GetUnspentPoints() ?? 0,
            totalPoints = talentService?.GetTotalPoints() ?? 0
        });
    }
    
    void OnDestroy()
    {
        EventBus.Unsubscribe<TalentPointsChangedEvent>(UpdatePointsDisplay);
        EventBus.Unsubscribe<TalentUnlockedEvent>(OnTalentUnlocked);
    }
    
    void CreateTalentButtons()
    {
        if (talentContainer == null || talentButtonPrefab == null) return;
        
        // Clear existing buttons
        foreach (Transform child in talentContainer)
        {
            Destroy(child.gameObject);
        }
        talentButtons.Clear();
        
        // Create button for each talent
        foreach (TalentData talent in allTalents)
        {
            if (talent == null) continue;
            
            GameObject buttonObj = Instantiate(talentButtonPrefab, talentContainer);
            TalentButton talentButton = buttonObj.GetComponent<TalentButton>();
            
            if (talentButton == null)
                talentButton = buttonObj.AddComponent<TalentButton>();
            
            talentButton.Initialize(talent, this);
            talentButtons[talent] = talentButton;
            
            // Position based on tier and position
            // You'll want to adjust this based on your UI layout
            RectTransform rect = buttonObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                float x = talent.position * 120f; // 120 pixels apart
                float y = -(talent.tier - 1) * 120f; // Tiers go down
                rect.anchoredPosition = new Vector2(x, y);
            }
        }
        
        RefreshAllButtons();
    }
    
    void SwitchTree(TalentTree tree)
    {
        currentTree = tree;
        RefreshTreeDisplay();
    }
    
    void RefreshTreeDisplay()
    {
        // Show/hide talents based on current tree
        foreach (var kvp in talentButtons)
        {
            bool showTalent = kvp.Key.talentTree == currentTree;
            kvp.Value.gameObject.SetActive(showTalent);
        }
        
        UpdateTreeSummary();
    }
    
    void RefreshAllButtons()
    {
        foreach (var button in talentButtons.Values)
        {
            button.Refresh();
        }
        
        RefreshTreeDisplay();
    }
    
    /// <summary>
    /// Called when talent button is clicked - invests a point
    /// </summary>
    public void OnTalentButtonClicked(TalentData talent)
    {
        if (talent == null || talentService == null) return;
        
        bool success = talentService.UnlockTalent(talent);
        
        if (success)
        {
            // Immediately refresh the specific button that was clicked
            if (talentButtons.ContainsKey(talent))
            {
                talentButtons[talent].Refresh();
            }
            
            // Refresh all buttons to ensure everything is in sync
            RefreshAllButtons();
        }
    }
    
    /// <summary>
    /// Show tooltip when hovering over a talent (uses global tooltip)
    /// </summary>
    public void ShowTooltip(TalentData talent)
    {
        if (talent == null) return;
        
        int currentRank = talentService?.GetTalentRank(talent) ?? 0;
        string description = talent.GetFullDescription(currentRank);
        
        EventBus.Publish(new TooltipShowEvent { title = talent.talentName, description = description });
    }
    
    /// <summary>
    /// Hide tooltip when no longer hovering (uses global tooltip)
    /// </summary>
    public void HideTooltip()
    {
        EventBus.Publish(new TooltipHideEvent());
    }
    
    void OnTalentUnlocked(TalentUnlockedEvent e)
    {
        // Specifically refresh the button for the unlocked talent
        if (e.talent != null && talentButtons.ContainsKey(e.talent))
        {
            talentButtons[e.talent].Refresh();
        }
        
        // Also refresh all buttons to ensure everything is in sync
        RefreshAllButtons();
    }
    
    void UpdatePointsDisplay(TalentPointsChangedEvent e)
    {
        if (talentPointsText != null)
            talentPointsText.text = $"Talent Points: {e.unspentPoints}";
        
        UpdateTreeSummary();
    }
    
    void UpdateTreeSummary()
    {
        if (treeSummaryText == null || talentService == null) return;
        
        int pointsInTree = talentService.GetTotalPointsInTree(currentTree);
        treeSummaryText.text = $"{currentTree} Tree: {pointsInTree} points spent";
    }
    
    void ResetTalents()
    {
        var characterService = Services.Get<ICharacterService>();
        if (characterService == null || talentService == null) return;
        
        // Check if player has enough gold
        int currentGold = characterService.GetGold();
        int cost = GameBalance.Economy.talentResetCost;
        if (currentGold < cost)
        {
            return;
        }
        
        // Spend gold and reset
        characterService.SpendGold(cost);
        talentService.ResetTalents();
        
        RefreshAllButtons();
        HideTooltip();
    }
    
    public void TogglePanel()
    {
        if (talentPanel != null)
        {
            if (panelController != null)
            {
                panelController.TogglePanel(talentPanel);
                if (talentPanel.activeSelf)
                {
                    RefreshAllButtons();
                }
            }
            else
            {
                bool isActive = !talentPanel.activeSelf;
                talentPanel.SetActive(isActive);
                
                if (isActive)
                {
                    RefreshAllButtons();
                }
            }
        }
    }
    
    public void ShowPanel()
    {
        if (talentPanel != null)
        {
            if (panelController != null)
                panelController.OpenPanel(talentPanel);
            else
                talentPanel.SetActive(true);
            
            RefreshAllButtons();
        }
    }
    
    public void HidePanel()
    {
        if (talentPanel != null)
        {
            if (panelController != null)
                panelController.ClosePanel(talentPanel);
            else
                talentPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Get the talent service (for TalentButton access)
    /// </summary>
    public ITalentService GetTalentService() => talentService;
}

/// <summary>
/// Individual talent button component
/// </summary>
public class TalentButton : MonoBehaviour
{
    public Image icon;
    public Image border;
    public TextMeshProUGUI rankText;
    public Button button;
    
    private TalentData talent;
    private TalentPanel panel;
    
    public void Initialize(TalentData talentData, TalentPanel talentPanel)
    {
        talent = talentData;
        panel = talentPanel;
        
        if (button == null)
            button = GetComponent<Button>();
        
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
            
            // Add hover events for tooltip
            UnityEngine.EventSystems.EventTrigger trigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null)
                trigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            
            // Pointer enter (hover start)
            UnityEngine.EventSystems.EventTrigger.Entry pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => { panel?.ShowTooltip(talent); });
            trigger.triggers.Add(pointerEnter);
            
            // Pointer exit (hover end)
            UnityEngine.EventSystems.EventTrigger.Entry pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => { panel?.HideTooltip(); });
            trigger.triggers.Add(pointerExit);
        }
        
        Refresh();
    }
    
    public void Refresh()
    {
        if (talent == null) return;
        
        // Find rankText if not assigned (try common names)
        if (rankText == null)
        {
            rankText = GetComponentInChildren<TextMeshProUGUI>();
            // Try to find by name if there are multiple TextMeshProUGUI components
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in texts)
            {
                if (text.name.Contains("Rank") || text.name.Contains("rank"))
                {
                    rankText = text;
                    break;
                }
            }
        }
        
        // Update icon
        if (icon != null && talent.icon != null)
            icon.sprite = talent.icon;
        
        // Update rank display
        int currentRank = panel?.GetTalentService()?.GetTalentRank(talent) ?? 0;
        if (rankText != null)
        {
            if (talent.maxRanks > 1)
            {
                rankText.text = $"{currentRank}/{talent.maxRanks}";
                rankText.gameObject.SetActive(true);
            }
            else
            {
                rankText.gameObject.SetActive(currentRank > 0);
                rankText.text = "✓";
            }
        }
        else
        {
        }
        
        // Update border color and button state
        if (border != null && button != null)
        {
            var service = panel?.GetTalentService();
            int pointsInTree = service?.GetTotalPointsInTree(talent.talentTree) ?? 0;
            bool hasPrereq = talent.prerequisiteTalent == null || 
                           (service?.GetTalentRank(talent.prerequisiteTalent) ?? 0) > 0;
            bool canUnlock = talent.CanUnlock(currentRank, pointsInTree, hasPrereq ? talent.prerequisiteTalent : null);
            bool hasPoints = (service?.GetUnspentPoints() ?? 0) > 0;
            
            if (currentRank >= talent.maxRanks)
            {
                border.color = Color.green; // Maxed
                button.interactable = false;
            }
            else if (currentRank > 0)
            {
                border.color = Color.yellow; // Partially learned
                button.interactable = canUnlock && hasPoints;
            }
            else if (canUnlock && hasPoints)
            {
                border.color = new Color(0.8f, 0.8f, 0.8f); // Available (lighter gray)
                button.interactable = true;
            }
            else
            {
                border.color = new Color(0.3f, 0.3f, 0.3f); // Locked (dark gray)
                button.interactable = false;
            }
        }
    }
    
    void OnClick()
    {
        panel?.OnTalentButtonClicked(talent);
    }
}

