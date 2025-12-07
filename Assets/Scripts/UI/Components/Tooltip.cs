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
    public Vector2 tooltipOffset = new Vector2(15f, -10f); // Reduced offset to be closer to mouse
    public float maxTooltipWidth = 350f; // Max width before wrapping
    
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
    /// <summary>
    /// Initialize tooltip setup
    /// </summary>
    void SetupTooltip()
    {
        if (tooltipPanel == null) return;
        
        tooltipPanel.SetActive(false);
        tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        
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

        // Fix: Ensure Rich Text is enabled so color tags work
        if (tooltipNameText != null) tooltipNameText.richText = true;
        if (tooltipDescriptionText != null) tooltipDescriptionText.richText = true;
    }

    /// <summary>
    /// Update tooltip position to follow mouse cursor
    /// </summary>
    void UpdateTooltipPosition()
    {
        Vector2 mousePos = GetMousePosition();
        
        // Fix: Use World Position to be independent of Anchors/Pivots
        Camera uiCamera = null;
        if (tooltipCanvas != null && tooltipCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = tooltipCanvas.worldCamera;
        }
        
        Vector3 worldPoint;
        // Convert screen point to world point on the plane of the parent
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            tooltipPanel.transform.parent as RectTransform,
            mousePos,
            uiCamera,
            out worldPoint
        );
        
        // Add offset (we need to adjust offset based on lossyScale if we want it pixel-perfect, 
        // but adding it to the screen pos before conversion is safer?)
        // Let's add it to the screen pos first!
        
        Vector2 offsetMousePos = mousePos + tooltipOffset;
         RectTransformUtility.ScreenPointToWorldPointInRectangle(
            tooltipPanel.transform.parent as RectTransform,
            offsetMousePos,
            uiCamera,
            out worldPoint
        );

        tooltipPanel.transform.position = worldPoint;
    }
    
    /// <summary>
    /// Show tooltip with custom content
    /// </summary>
    /// <param name="title">Tooltip title/name</param>
    /// <param name="description">Tooltip description</param>
    public void Show(string title, string description)
    {
        if (tooltipPanel == null) return;
        
        // 1. Activate FIRST so layout calculations actually work
        tooltipPanel.SetActive(true);
        isVisible = true;

        // Ensure setup is complete
        if (tooltipRect == null)
        {
            SetupTooltip();
        }
        
        // 2. Reset Width to max to give ContentSizeFitter/LayoutGroup a constraint
        // This is crucial: If width is weird/zero, Text wraps to infinite height.
        Vector2 size = tooltipRect.sizeDelta;
        size.x = maxTooltipWidth;
        tooltipRect.sizeDelta = size;
        
        // 3. Set Text
        if (tooltipNameText != null)
        {
            tooltipNameText.richText = true; // FORCE RICH TEXT
            tooltipNameText.text = title;
        }
        
        if (tooltipDescriptionText != null)
        {
            tooltipDescriptionText.richText = true; // FORCE RICH TEXT
            tooltipDescriptionText.text = description;
        }
            
        // 4. Update Position immediately so it doesn't flicker at (0,0)
        UpdateTooltipPosition();
        
        // 5. Force Rebuild to recalculate Height based on new Width + Text Content
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
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
            string stats = GetEquipmentStatsText(item.equipmentData);
            if (!string.IsNullOrEmpty(stats))
            {
                if (!string.IsNullOrEmpty(description)) description += "\n\n";
                description += stats;
            }
        }
        
        // Add quantity info (only if > 1)
        if (item.quantity > 1)
        {
             if (!string.IsNullOrEmpty(description)) description += "\n\n";
             description += $"<color=#FFFF00>Quantity: {item.quantity}</color>";
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
        
        string stats = $"<color=#00FFFF>Slot: {equipment.slot}</color>\n";
        
        if (equipment.levelRequired > 1)
            stats += $"<color=#FF0000>Requires Level {equipment.levelRequired}</color>\n";
        
        stats += "\n" + equipment.GetStatsDescription();
        
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

