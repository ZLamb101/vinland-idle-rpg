using UnityEngine;
using TMPro;

/// <summary>
/// Reusable tooltip component that handles display, positioning, and content for tooltips.
/// Can be used by any UI panel that needs tooltip functionality.
/// </summary>
public class Tooltip : MonoBehaviour
{
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
    
    void Awake()
    {
        SetupTooltip();
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
        }
        
        tooltipCanvas = tooltipPanel.GetComponentInParent<Canvas>();
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
        Vector2 localPoint;
        RectTransform targetRect = tooltipRect.parent as RectTransform;
        
        if (targetRect == null)
        {
            targetRect = canvasRect;
        }
        
        if (targetRect == null) return;
        
        Camera uiCamera = null;
        if (tooltipCanvas != null && tooltipCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = tooltipCanvas.worldCamera;
        }
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect,
            mousePos,
            uiCamera,
            out localPoint
        );
        
        tooltipRect.anchoredPosition = localPoint + tooltipOffset;
    }
    
    /// <summary>
    /// Show tooltip with custom content
    /// </summary>
    /// <param name="title">Tooltip title/name</param>
    /// <param name="description">Tooltip description</param>
    public void Show(string title, string description)
    {
        if (tooltipPanel == null) return;
        
        if (tooltipNameText != null)
            tooltipNameText.text = title;
        
        if (tooltipDescriptionText != null)
            tooltipDescriptionText.text = description;
        
        tooltipPanel.SetActive(true);
        isVisible = true;
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

