using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI panel that displays shop inventory, handles purchases, and manages buyback.
/// </summary>
public class ShopPanel : MonoBehaviour
{
    [Header("Shop Panel")]
    public GameObject shopPanel; // The main panel to show/hide
    
    [Header("Shop Display")]
    [Tooltip("Optional: Shop name display (leave empty if not displaying shop name)")]
    public TextMeshProUGUI shopNameText;
    public Transform shopItemsContainer; // Container for shop item slots
    public GameObject shopItemSlotPrefab; // Prefab for shop item slots
    
    [Header("Stock Refresh")]
    public TextMeshProUGUI refreshTimerText;
    
    [Header("BuyBack")]
    public GameObject buyBackSection;
    public Image buyBackItemIcon;
    public TextMeshProUGUI buyBackItemNameText;
    public TextMeshProUGUI buyBackPriceText;
    public Button buyBackButton;
    
    [Header("Controls")]
    public Button closeButton;
    
    [Header("Tooltip")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipNameText;
    public TextMeshProUGUI tooltipDescriptionText;
    public Vector2 tooltipOffset = new Vector2(30f, -30f);
    
    // Cached reference to InventoryUI (for passing to shop slots)
    private InventoryUI inventoryUI;
    
    private List<GameObject> currentShopItemSlots = new List<GameObject>();
    private RectTransform tooltipRect;
    private Canvas tooltipCanvas;
    private RectTransform canvasRect;
    private float refreshTimerUpdateInterval = 1f; // Update timer every second
    private float lastTimerUpdate = 0f;
    private IShopService shopService; // Cached shop service reference
    
    void Start()
    {
        // Cache InventoryUI reference once
        inventoryUI = ComponentInjector.GetOrFind<InventoryUI>();
        
        // Get shop service
        shopService = Services.Get<IShopService>();
        
        // Subscribe to shop events
        if (shopService != null)
        {
            shopService.OnShopOpened += OnShopOpened;
            shopService.OnShopClosed += OnShopClosed;
            shopService.OnStockChanged += OnStockChanged;
            shopService.OnBuyBackChanged += OnBuyBackChanged;
        }
        
        // Setup buttons
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
        
        if (buyBackButton != null)
            buyBackButton.onClick.AddListener(OnBuyBackClicked);
        
        // Hide panel initially
        if (shopPanel != null)
            shopPanel.SetActive(false);
        
        // Hide buyback section initially
        if (buyBackSection != null)
            buyBackSection.SetActive(false);
        
        // Setup tooltip
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
            tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            if (tooltipRect != null)
            {
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
            
            foreach (UnityEngine.UI.Graphic graphic in tooltipPanel.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }
    }
    
    void Update()
    {
        // Update tooltip position
        if (tooltipPanel != null && tooltipPanel.activeSelf && tooltipRect != null)
        {
            Vector2 mousePos = GetMousePosition();
            Vector2 localPoint;
            RectTransform targetRect = tooltipRect.parent as RectTransform;
            if (targetRect == null)
            {
                targetRect = canvasRect;
            }
            if (targetRect == null)
            {
                return;
            }
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
        
        // Update refresh timer display every second
        if (shopPanel != null && shopPanel.activeSelf)
        {
            if (Time.time - lastTimerUpdate >= refreshTimerUpdateInterval)
            {
                UpdateRefreshTimer();
                lastTimerUpdate = Time.time;
            }
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (shopService != null)
        {
            shopService.OnShopOpened -= OnShopOpened;
            shopService.OnShopClosed -= OnShopClosed;
            shopService.OnStockChanged -= OnStockChanged;
            shopService.OnBuyBackChanged -= OnBuyBackChanged;
        }
    }
    
    void OnShopOpened(ShopData shop)
    {
        if (shopPanel != null)
            shopPanel.SetActive(true);
        
        if (shopNameText != null && shop != null)
            shopNameText.text = shop.shopName;
        
        RefreshShopItems(shop);
        UpdateBuyBackDisplay();
        UpdateRefreshTimer();
    }
    
    void OnShopClosed()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
        
        ClearShopItems();
    }
    
    void OnStockChanged(int itemIndex)
    {
        RefreshShopItems(shopService?.GetCurrentShop());
    }
    
    void OnBuyBackChanged()
    {
        UpdateBuyBackDisplay();
    }
    
    /// <summary>
    /// Refresh the display of shop items
    /// </summary>
    void RefreshShopItems(ShopData shop)
    {
        if (shop == null || shopItemsContainer == null) return;
        
        // Clear existing slots
        ClearShopItems();
        
        // Create slots for each shop item
        for (int i = 0; i < shop.shopItems.Count; i++)
        {
            ShopItemEntry entry = shop.shopItems[i];
            if (entry == null || entry.item == null) continue;
            
            if (shopItemSlotPrefab != null)
            {
                GameObject slotObj = Instantiate(shopItemSlotPrefab, shopItemsContainer);
                ShopItemSlot slot = slotObj.GetComponent<ShopItemSlot>();
                
                if (slot != null)
                {
                    slot.Initialize(entry, this, inventoryUI);
                    currentShopItemSlots.Add(slotObj);
                }
                else
                {
                    Destroy(slotObj);
                }
            }
        }
    }
    
    /// <summary>
    /// Clear all shop item slots
    /// </summary>
    void ClearShopItems()
    {
        foreach (GameObject slot in currentShopItemSlots)
        {
            if (slot != null)
            {
                Destroy(slot);
            }
        }
        currentShopItemSlots.Clear();
    }
    
    /// <summary>
    /// Update the refresh timer display
    /// </summary>
    void UpdateRefreshTimer()
    {
        if (refreshTimerText == null || shopService == null) return;
        
        float timeRemaining = shopService.GetTimeUntilRefresh();
        
        if (timeRemaining <= 0f)
        {
            refreshTimerText.text = "Stock Refreshed!";
        }
        else
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            refreshTimerText.text = $"Stock Refresh: {minutes:00}:{seconds:00}";
        }
    }
    
    /// <summary>
    /// Update buyback display
    /// </summary>
    void UpdateBuyBackDisplay()
    {
        if (buyBackSection == null) return;
        
        bool hasBuyBack = shopService != null && shopService.HasBuyBack();
        
        buyBackSection.SetActive(hasBuyBack);
        
        if (hasBuyBack && shopService != null)
        {
            InventoryItem buyBackItem = shopService.GetBuyBackItem();
            int buyBackPrice = shopService.GetBuyBackPrice();
            
            if (buyBackItem != null)
            {
                if (buyBackItemIcon != null && buyBackItem.icon != null)
                {
                    buyBackItemIcon.sprite = buyBackItem.icon;
                    buyBackItemIcon.gameObject.SetActive(true);
                }
                
                if (buyBackItemNameText != null)
                    buyBackItemNameText.text = buyBackItem.itemName;
                
                if (buyBackPriceText != null)
                    buyBackPriceText.text = $"{buyBackPrice} Gold";
            }
        }
    }
    
    /// <summary>
    /// Show tooltip for a shop item
    /// </summary>
    public void ShowTooltip(ItemData item)
    {
        if (tooltipPanel == null || item == null) return;
        
        if (tooltipNameText != null)
            tooltipNameText.text = item.itemName;
        
        if (tooltipDescriptionText != null)
        {
            string desc = item.description;
            
            // Add equipment stats if it's equipment
            if (item.IsEquipment() && item.equipmentData != null)
            {
                desc += "\n\n" + GetEquipmentStatsText(item.equipmentData);
            }
            
            tooltipDescriptionText.text = desc;
        }
        
        tooltipPanel.SetActive(true);
    }
    
    /// <summary>
    /// Hide tooltip
    /// </summary>
    public void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
    
    /// <summary>
    /// Get equipment stats text (reused from InventoryUI pattern)
    /// </summary>
    string GetEquipmentStatsText(EquipmentData equipment)
    {
        if (equipment == null) return "";
        
        System.Text.StringBuilder stats = new System.Text.StringBuilder();
        
        if (equipment.attackDamage != 0)
            stats.AppendLine($"Attack Damage: +{equipment.attackDamage:F1}");
        if (equipment.attackSpeed != 0)
            stats.AppendLine($"Attack Speed: {equipment.attackSpeed:+#.##;-#.##;0}s");
        if (equipment.maxHealth != 0)
            stats.AppendLine($"Max Health: +{equipment.maxHealth:F0}");
        if (equipment.healthRegen != 0)
            stats.AppendLine($"Health Regen: +{equipment.healthRegen:F1}/s");
        if (equipment.armor != 0)
            stats.AppendLine($"Armor: {equipment.armor:P0}");
        if (equipment.dodge != 0)
            stats.AppendLine($"Dodge: {equipment.dodge:P0}");
        if (equipment.criticalChance != 0)
            stats.AppendLine($"Crit Chance: {equipment.criticalChance:P0}");
        if (equipment.lifesteal != 0)
            stats.AppendLine($"Lifesteal: {equipment.lifesteal:P0}");
        
        return stats.ToString();
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
    
    void OnCloseClicked()
    {
        if (shopService != null)
        {
            shopService.CloseShop();
        }
    }
    
    void OnBuyBackClicked()
    {
        if (shopService != null)
        {
            shopService.BuyBackItem();
        }
    }
}

