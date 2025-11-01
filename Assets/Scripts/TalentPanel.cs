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
    
    [Header("Tooltip")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipNameText;
    public TextMeshProUGUI tooltipDescriptionText;
    
    [Header("Reset")]
    public Button resetButton;
    public int resetCost = 100; // Gold cost to reset
    
    [Header("Talent Data")]
    public TalentData[] allTalents; // Assign all talent assets here
    
    [Header("Tooltip Settings")]
    public Vector2 tooltipOffset = new Vector2(30f, -40f); // Offset from cursor
    
    private Dictionary<TalentData, TalentButton> talentButtons = new Dictionary<TalentData, TalentButton>();
    private RectTransform tooltipRect;
    
    void Start()
    {
        // Subscribe to events
        if (TalentManager.Instance != null)
        {
            TalentManager.Instance.OnTalentPointsChanged += UpdatePointsDisplay;
            TalentManager.Instance.OnTalentUnlocked += OnTalentUnlocked;
        }
        
        // Setup tab buttons
        if (combatTab != null)
            combatTab.onClick.AddListener(() => SwitchTree(TalentTree.Combat));
        if (defenseTab != null)
            defenseTab.onClick.AddListener(() => SwitchTree(TalentTree.Defense));
        if (utilityTab != null)
            utilityTab.onClick.AddListener(() => SwitchTree(TalentTree.Utility));
        
        // Setup reset button
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetTalents);
        
        // Hide panel initially
        if (talentPanel != null)
            talentPanel.SetActive(false);
        
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
            tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            
            // Disable raycast blocking on tooltip so it doesn't interfere with hover detection
            CanvasGroup tooltipCanvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
            if (tooltipCanvasGroup == null)
            {
                tooltipCanvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
            }
            tooltipCanvasGroup.blocksRaycasts = false;
            tooltipCanvasGroup.interactable = false;
            
            // Also disable raycasts on all child Image/Text components
            foreach (Graphic graphic in tooltipPanel.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }
        
        // Initialize
        CreateTalentButtons();
        UpdatePointsDisplay(TalentManager.Instance?.GetUnspentPoints() ?? 0);
    }
    
    void Update()
    {
        // Update tooltip position to follow cursor
        if (tooltipPanel != null && tooltipPanel.activeSelf && tooltipRect != null)
        {
            Vector2 mousePos = GetMousePosition();
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                talentPanel.GetComponent<RectTransform>(),
                mousePos,
                null,
                out localPoint
            );
            
            tooltipRect.anchoredPosition = localPoint + tooltipOffset;
        }
    }
    
    /// <summary>
    /// Get mouse position - compatible with both old and new Input System
    /// </summary>
    Vector2 GetMousePosition()
    {
        #if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            return UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        }
        #endif
        
        #if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
        #endif
        
        // Fallback
        return Vector2.zero;
    }
    
    void OnDestroy()
    {
        if (TalentManager.Instance != null)
        {
            TalentManager.Instance.OnTalentPointsChanged -= UpdatePointsDisplay;
            TalentManager.Instance.OnTalentUnlocked -= OnTalentUnlocked;
        }
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
        if (talent == null || TalentManager.Instance == null) return;
        
        bool success = TalentManager.Instance.UnlockTalent(talent);
        
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
    /// Show tooltip when hovering over a talent
    /// </summary>
    public void ShowTooltip(TalentData talent)
    {
        if (tooltipPanel == null || talent == null) return;
        
        int currentRank = TalentManager.Instance?.GetTalentRank(talent) ?? 0;
        
        if (tooltipNameText != null)
            tooltipNameText.text = talent.talentName;
        
        if (tooltipDescriptionText != null)
            tooltipDescriptionText.text = talent.GetFullDescription(currentRank);
        
        tooltipPanel.SetActive(true);
    }
    
    /// <summary>
    /// Hide tooltip when no longer hovering
    /// </summary>
    public void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
    
    void OnTalentUnlocked(TalentData talent, int newRank)
    {
        // Specifically refresh the button for the unlocked talent
        if (talent != null && talentButtons.ContainsKey(talent))
        {
            talentButtons[talent].Refresh();
        }
        
        // Also refresh all buttons to ensure everything is in sync
        RefreshAllButtons();
    }
    
    void UpdatePointsDisplay(int points)
    {
        if (talentPointsText != null)
            talentPointsText.text = $"Talent Points: {points}";
        
        UpdateTreeSummary();
    }
    
    void UpdateTreeSummary()
    {
        if (treeSummaryText == null || TalentManager.Instance == null) return;
        
        int pointsInTree = TalentManager.Instance.GetTotalPointsInTree(currentTree);
        treeSummaryText.text = $"{currentTree} Tree: {pointsInTree} points spent";
    }
    
    void ResetTalents()
    {
        if (CharacterManager.Instance == null || TalentManager.Instance == null) return;
        
        // Check if player has enough gold
        int currentGold = CharacterManager.Instance.GetGold();
        if (currentGold < resetCost)
        {
            Debug.LogWarning($"Not enough gold to reset talents! Need {resetCost}, have {currentGold}");
            return;
        }
        
        // Spend gold and reset
        CharacterManager.Instance.SpendGold(resetCost);
        TalentManager.Instance.ResetTalents();
        
        RefreshAllButtons();
        HideTooltip();
        
        Debug.Log($"Reset all talents for {resetCost} gold");
    }
    
    public void TogglePanel()
    {
        if (talentPanel != null)
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
        int currentRank = TalentManager.Instance?.GetTalentRank(talent) ?? 0;
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
            Debug.LogWarning($"TalentButton: rankText is null for talent {talent.talentName}. Make sure RankText is assigned in the Inspector.");
        }
        
        // Update border color and button state
        if (border != null && button != null)
        {
            int pointsInTree = TalentManager.Instance?.GetTotalPointsInTree(talent.talentTree) ?? 0;
            bool hasPrereq = talent.prerequisiteTalent == null || 
                           (TalentManager.Instance?.GetTalentRank(talent.prerequisiteTalent) ?? 0) > 0;
            bool canUnlock = talent.CanUnlock(currentRank, pointsInTree, hasPrereq ? talent.prerequisiteTalent : null);
            bool hasPoints = (TalentManager.Instance?.GetUnspentPoints() ?? 0) > 0;
            
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

