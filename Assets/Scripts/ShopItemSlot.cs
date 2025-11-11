using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Individual shop item slot UI component.
/// Displays item info, price, stock, and handles buying.
/// </summary>
public class ShopItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI stockText;
    public Button buyButton;
    
    private ShopItemEntry entry;
    private ShopPanel shopPanel;
    
    // Cached reference to InventoryUI
    private InventoryUI inventoryUI;
    
    void Start()
    {
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyClicked);
        }
        
        // Subscribe to gold changes to update buy button state
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.OnGoldChanged += OnGoldChanged;
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.OnGoldChanged -= OnGoldChanged;
        }
    }
    
    void OnGoldChanged(int newGold)
    {
        UpdateBuyButtonState();
    }
    
    /// <summary>
    /// Initialize this shop item slot with entry data
    /// </summary>
    public void Initialize(ShopItemEntry shopEntry, ShopPanel panel, InventoryUI ui = null)
    {
        entry = shopEntry;
        shopPanel = panel;
        
        // Use provided InventoryUI reference, or find it if not provided
        if (ui != null)
        {
            inventoryUI = ui;
        }
        else if (inventoryUI == null)
        {
            inventoryUI = ComponentInjector.GetOrFind<InventoryUI>();
        }
        
        UpdateDisplay();
    }
    
    /// <summary>
    /// Update the display of this slot
    /// </summary>
    void UpdateDisplay()
    {
        if (entry == null || entry.item == null) return;
        
        // Update item icon
        if (itemIcon != null)
        {
            if (entry.item.icon != null)
            {
                itemIcon.sprite = entry.item.icon;
                itemIcon.gameObject.SetActive(true);
            }
            else
            {
                itemIcon.gameObject.SetActive(false);
            }
        }
        
        // Update item name
        if (itemNameText != null)
            itemNameText.text = entry.item.itemName;
        
        // Update price
        if (priceText != null)
            priceText.text = $"{entry.price} Gold";
        
        // Update stock
        if (stockText != null)
        {
            if (entry.IsInStock())
            {
                stockText.text = $"Stock: {entry.currentStock}/{entry.maxStock}";
                stockText.color = Color.white;
            }
            else
            {
                stockText.text = "Out of Stock";
                stockText.color = Color.red;
            }
        }
        
        UpdateBuyButtonState();
    }
    
    /// <summary>
    /// Update buy button state based on stock and gold
    /// </summary>
    void UpdateBuyButtonState()
    {
        if (buyButton == null || entry == null) return;
        
        buyButton.interactable = entry.IsInStock();
        
        // Check if player has enough gold
        if (entry.IsInStock() && CharacterManager.Instance != null)
        {
            if (CharacterManager.Instance.GetGold() < entry.price)
            {
                buyButton.interactable = false;
            }
        }
    }
    
    void OnBuyClicked()
    {
        if (entry == null || ShopManager.Instance == null) return;
        
        if (ShopManager.Instance.BuyItem(entry, 1))
        {
            // Refresh display after purchase
            UpdateDisplay();
            
            // Refresh inventory if needed
            if (inventoryUI != null)
            {
                inventoryUI.RefreshDisplay();
            }
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (entry != null && entry.item != null && shopPanel != null)
        {
            shopPanel.ShowTooltip(entry.item);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (shopPanel != null)
        {
            shopPanel.HideTooltip();
        }
    }
}

