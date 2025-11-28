using UnityEngine;
using TMPro;

/// <summary>
/// Reusable tooltip component that handles display, positioning, and content for tooltips.
/// Uses a singleton pattern to ensure tooltip events work even if GameObject starts inactive.
/// </summary>
public class Tooltip : MonoBehaviour
{
    // Singleton instance
    private static Tooltip _instance;
    public static Tooltip Instance => _instance;
    
    // Static flag to track if we've subscribed to events
    private static bool _eventsSubscribed = false;
    
    [Header("Tooltip UI")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipNameText;
    public TextMeshProUGUI tooltipDescriptionText;
    
    [Header("Settings")]
    public Vector2 tooltipOffset = new Vector2(30f, -30f);
    
    private RectTransform tooltipRect;
    private Canvas tooltipCanvas;
    private RectTransform canvasRect;
    private bool isVisible = false;
    
    // Static initialization - runs when the class is first accessed
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StaticInit()
    {
        SubscribeToEvents();
    }
    
    static void SubscribeToEvents()
    {
        if (_eventsSubscribed) return;
        
        EventBus.Subscribe<TooltipShowEvent>(OnTooltipShowStatic);
        EventBus.Subscribe<TooltipHideEvent>(OnTooltipHideStatic);
        _eventsSubscribed = true;
    }
    
    static void OnTooltipShowStatic(TooltipShowEvent e)
    {
        // Find instance if not set
        if (_instance == null)
        {
            _instance = FindObjectOfType<Tooltip>(true); // true = include inactive
        }
        
        if (_instance != null)
        {
            // Ensure the GameObject is active so the tooltip can show
            if (!_instance.gameObject.activeInHierarchy)
            {
                _instance.gameObject.SetActive(true);
            }
            _instance.Show(e.title, e.description);
        }
    }
    
    static void OnTooltipHideStatic(TooltipHideEvent e)
    {
        if (_instance != null)
        {
            _instance.Hide();
        }
    }
    
    void Awake()
    {
        // Register as singleton instance
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            // Another tooltip exists, destroy this one
            Destroy(gameObject);
            return;
        }
        
        SetupTooltip();
        
        // Ensure events are subscribed
        SubscribeToEvents();
    }
    
    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
    
    void Update()
    {
        // Update tooltip position to follow cursor when visible
        if (isVisible && tooltipPanel != null && tooltipPanel.activeSelf && tooltipRect != null)
        {
            UpdateTooltipPosition();
        }
    }
    
    /// <summary>
    /// Initialize tooltip setup
    /// </summary>
    void SetupTooltip()
    {
        if (tooltipPanel == null) return;
        
        tooltipPanel.SetActive(false);
        tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        
        if (tooltipRect != null)
        {
            // Top-left pivot so offset behaves intuitively
            tooltipRect.pivot = new Vector2(0f, 1f);
            
            // Set anchors to center so positioning works correctly with canvas
            tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
            tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        }
        
        // Find the root canvas for positioning
        tooltipCanvas = tooltipPanel.GetComponentInParent<Canvas>();
        while (tooltipCanvas != null && !tooltipCanvas.isRootCanvas)
        {
            Canvas parentCanvas = tooltipCanvas.transform.parent?.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
                tooltipCanvas = parentCanvas;
            else
                break;
        }
        
        if (tooltipCanvas != null)
        {
            canvasRect = tooltipCanvas.GetComponent<RectTransform>();
        }
        else
        {
            canvasRect = tooltipPanel.transform.parent as RectTransform;
        }
        
        // Disable raycast blocking on tooltip
        CanvasGroup tooltipCanvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
        if (tooltipCanvasGroup == null)
        {
            tooltipCanvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
        }
        tooltipCanvasGroup.blocksRaycasts = false;
        tooltipCanvasGroup.interactable = false;
        
        // Disable raycasts on all child elements
        foreach (UnityEngine.UI.Graphic graphic in tooltipPanel.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
        {
            graphic.raycastTarget = false;
        }
    }
    
    /// <summary>
    /// Update tooltip position to follow mouse cursor
    /// </summary>
    void UpdateTooltipPosition()
    {
        Vector2 mousePos = GetMousePosition();
        
        if (canvasRect == null) return;
        
        Camera uiCamera = null;
        if (tooltipCanvas != null && tooltipCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = tooltipCanvas.worldCamera;
        }
        
        // Convert screen position to canvas local position
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            mousePos,
            uiCamera,
            out localPoint
        );
        
        // Apply offset
        Vector2 targetPosition = localPoint + tooltipOffset;
        
        // Clamp to screen bounds to prevent tooltip going off-screen
        Vector2 tooltipSize = tooltipRect.sizeDelta;
        Vector2 canvasSize = canvasRect.sizeDelta;
        
        // Account for canvas pivot (usually 0.5, 0.5)
        Vector2 canvasPivotOffset = canvasSize * (canvasRect.pivot - new Vector2(0.5f, 0.5f));
        
        float minX = -canvasSize.x * 0.5f - canvasPivotOffset.x;
        float maxX = canvasSize.x * 0.5f - canvasPivotOffset.x - tooltipSize.x;
        float minY = -canvasSize.y * 0.5f - canvasPivotOffset.y + tooltipSize.y;
        float maxY = canvasSize.y * 0.5f - canvasPivotOffset.y;
        
        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        
        tooltipRect.anchoredPosition = targetPosition;
    }
    
    /// <summary>
    /// Show tooltip with custom content
    /// </summary>
    /// <param name="title">Tooltip title/name</param>
    /// <param name="description">Tooltip description</param>
    public void Show(string title, string description)
    {
        if (tooltipPanel == null) return;
        
        // Ensure setup is complete (in case Awake didn't run)
        if (tooltipRect == null)
        {
            SetupTooltip();
        }
        
        if (tooltipNameText != null)
            tooltipNameText.text = title;
        
        if (tooltipDescriptionText != null)
            tooltipDescriptionText.text = description;
        
        tooltipPanel.SetActive(true);
        isVisible = true;
        
        // Update position immediately
        UpdateTooltipPosition();
    }
    
    /// <summary>
    /// Show tooltip for an inventory item
    /// </summary>
    public void ShowForInventoryItem(InventoryItem item)
    {
        if (item == null || item.IsEmpty())
        {
            Hide();
            return;
        }
        
        string description = item.description;
        
        // Add equipment stats if it's equipment
        if (item.IsEquipment() && item.equipmentData != null)
        {
            description += "\n\n" + GetEquipmentStatsText(item.equipmentData);
        }
        
        // Add quantity info
        if (item.quantity > 1)
        {
            description += $"\n\n<color=yellow>Quantity: {item.quantity}</color>";
        }
        
        Show(item.itemName, description);
    }
    
    /// <summary>
    /// Show tooltip for an item data (shop items, etc.)
    /// </summary>
    public void ShowForItemData(ItemData item)
    {
        if (item == null)
        {
            Hide();
            return;
        }
        
        string description = item.description;
        
        // Add equipment stats if it's equipment
        if (item.IsEquipment() && item.equipmentData != null)
        {
            description += "\n\n" + GetEquipmentStatsText(item.equipmentData);
        }
        
        Show(item.itemName, description);
    }
    
    /// <summary>
    /// Hide tooltip
    /// </summary>
    public void Hide()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
            isVisible = false;
        }
    }
    
    /// <summary>
    /// Event handler for TooltipShowEvent
    /// </summary>
    void OnTooltipShow(TooltipShowEvent e)
    {
        Show(e.title, e.description);
    }
    
    /// <summary>
    /// Event handler for TooltipHideEvent
    /// </summary>
    void OnTooltipHide(TooltipHideEvent e)
    {
        Hide();
    }
    
    /// <summary>
    /// Get equipment stats text for display
    /// </summary>
    string GetEquipmentStatsText(EquipmentData equipment)
    {
        if (equipment == null) return "";
        
        string stats = $"<color=cyan>Slot: {equipment.slot}</color>\n";
        
        if (equipment.levelRequired > 1)
            stats += $"<color=red>Requires Level {equipment.levelRequired}</color>\n";
        
        stats += "\n";
        
        if (equipment.attackDamage > 0) stats += $"+{equipment.attackDamage:F0} Attack Damage\n";
        if (equipment.maxHealth > 0) stats += $"+{equipment.maxHealth:F0} Max Health\n";
        if (equipment.attackSpeed != 0) stats += $"{(equipment.attackSpeed < 0 ? "" : "+")}{equipment.attackSpeed:F2}s Attack Speed\n";
        if (equipment.armor > 0) stats += $"+{equipment.armor * 100:F0}% Armor\n";
        if (equipment.criticalChance > 0) stats += $"+{equipment.criticalChance * 100:F0}% Critical Chance\n";
        if (equipment.dodge > 0) stats += $"+{equipment.dodge * 100:F0}% Dodge\n";
        if (equipment.lifesteal > 0) stats += $"+{equipment.lifesteal * 100:F0}% Lifesteal\n";
        if (equipment.xpBonus > 0) stats += $"+{equipment.xpBonus * 100:F0}% XP Gain\n";
        if (equipment.goldBonus > 0) stats += $"+{equipment.goldBonus * 100:F0}% Gold Gain\n";
        
        return stats;
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
        
        return Vector2.zero;
    }
}

