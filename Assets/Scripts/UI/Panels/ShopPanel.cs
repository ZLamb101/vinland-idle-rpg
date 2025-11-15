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
    public Tooltip tooltip;
    
    // Cached reference to inventoryPanel (for passing to shop slots)
    private InventoryPanel inventoryPanel;
    
    private List<GameObject> currentShopItemSlots = new List<GameObject>();
    private float refreshTimerUpdateInterval = 1f; // Update timer every second
    private float lastTimerUpdate = 0f;
    private IShopService shopService; // Cached shop service reference
    
    void Start()
    {
        // Cache inventoryPanel reference once
        inventoryPanel = ComponentInjector.GetOrFind<InventoryPanel>();
        
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
        
        // Tooltip is now handled by Tooltip component
    }
    
    void Update()
    {
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
                    slot.Initialize(entry, this, inventoryPanel);
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
        if (tooltip != null)
        {
            tooltip.ShowForItemData(item);
        }
    }
    
    /// <summary>
    /// Hide tooltip
    /// </summary>
    public void HideTooltip()
    {
        if (tooltip != null)
        {
            tooltip.Hide();
        }
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

