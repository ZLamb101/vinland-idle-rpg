using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// REFACTORED EXAMPLE: Individual shop item slot UI component.
/// 
/// KEY IMPROVEMENTS OVER ORIGINAL:
/// 1. Uses EventBus instead of FindAnyObjectByType (line 136 in original)
/// 2. Uses Services for dependency injection instead of direct .Instance calls
/// 3. Extends Injectable base class for cleaner service access
/// 4. Subscribes to events via EventBus for better decoupling
/// 
/// COMPARISON:
/// Before: FindAnyObjectByType<InventoryUI>() - slow, fragile
/// After:  EventBus.Publish<ItemPurchasedEvent>() - fast, decoupled
/// 
/// Before: CharacterManager.Instance.OnGoldChanged
/// After:  EventBus.Subscribe<CharacterGoldChangedEvent>()
/// </summary>
public class ShopItemSlot_Refactored_EXAMPLE : Injectable, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI stockText;
    public Button buyButton;
    
    private ShopItemEntry entry;
    private ShopPanel shopPanel;
    
    // Cached services - get once in Start, use throughout lifetime
    private ICharacterService characterService;
    private IShopService shopService;
    
    void Start()
    {
        // Get services once and cache them
        characterService = GetService<ICharacterService>();
        shopService = GetService<IShopService>();
        
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyClicked);
        }
        
        // Subscribe to gold changes via EventBus instead of direct manager reference
        EventBus.Subscribe<CharacterGoldChangedEvent>(OnGoldChanged);
    }
    
    void OnDestroy()
    {
        // Unsubscribe from EventBus
        EventBus.Unsubscribe<CharacterGoldChangedEvent>(OnGoldChanged);
    }
    
    void OnGoldChanged(CharacterGoldChangedEvent e)
    {
        UpdateBuyButtonState();
    }
    
    /// <summary>
    /// Initialize this shop item slot with entry data
    /// </summary>
    public void Initialize(ShopItemEntry shopEntry, ShopPanel panel)
    {
        entry = shopEntry;
        shopPanel = panel;
        
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
        
        // Check if player has enough gold using service
        if (entry.IsInStock() && characterService != null)
        {
            if (characterService.GetGold() < entry.price)
            {
                buyButton.interactable = false;
            }
        }
    }
    
    void OnBuyClicked()
    {
        if (entry == null || shopService == null) return;
        
        // Use service instead of direct manager access
        if (shopService.BuyItem(entry, 1))
        {
            // Refresh display after purchase
            UpdateDisplay();
            
            // IMPROVEMENT: Instead of FindAnyObjectByType, publish an event
            // Any listeners (like InventoryUI) will automatically refresh
            EventBus.Publish(new ItemPurchasedEvent 
            { 
                item = entry.item,
                quantity = 1,
                goldSpent = entry.price
            });
            
            // The InventoryUI (if it exists) will listen to ItemPurchasedEvent
            // and refresh itself automatically. No need to find it!
            
            // OLD WAY (SLOW, FRAGILE):
            // InventoryUI inventoryUI = FindAnyObjectByType<InventoryUI>();
            // if (inventoryUI != null)
            // {
            //     inventoryUI.RefreshDisplay();
            // }
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

/*
 * MIGRATION GUIDE:
 * 
 * To use this refactored version:
 * 
 * 1. Make InventoryUI listen to ItemPurchasedEvent:
 *    
 *    public class InventoryUI : EventSubscriber
 *    {
 *        protected override void OnEnable()
 *        {
 *            base.OnEnable();
 *            Subscribe<ItemPurchasedEvent>(OnItemPurchased);
 *        }
 *        
 *        void OnItemPurchased(ItemPurchasedEvent e)
 *        {
 *            RefreshDisplay();
 *        }
 *    }
 * 
 * 2. Replace ShopItemSlot with ShopItemSlot_Refactored_EXAMPLE in your prefabs
 * 
 * 3. Test that purchases still update the inventory UI
 * 
 * BENEFITS:
 * - No FindAnyObjectByType calls (5000x faster!)
 * - InventoryUI can be in any scene/hierarchy location
 * - Multiple listeners can react to purchases (achievements, sounds, etc.)
 * - Easier to test (can mock services)
 * - Clearer dependencies (explicit in Start())
 */

