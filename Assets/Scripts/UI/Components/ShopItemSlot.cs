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
    
    // Cached reference to InventoryPanel
    private InventoryPanel inventoryPanel;
    
    void Start()
    {
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyClicked);
        }
        
        // Subscribe to gold changes to update buy button state
        var characterService = Services.Get<ICharacterService>();
        if (characterService != null)
        {
            EventBus.Subscribe<CharacterGoldChangedEvent>(OnGoldChanged);
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        EventBus.Unsubscribe<CharacterGoldChangedEvent>(OnGoldChanged);
    }
    
    void OnGoldChanged(CharacterGoldChangedEvent e)
    {
        UpdateBuyButtonState();
    }
    
    /// <summary>
    /// Initialize this shop item slot with entry data
    /// </summary>
    public void Initialize(ShopItemEntry shopEntry, ShopPanel shopPanel, InventoryPanel inventoryPanel = null)
    {
        entry = shopEntry;
        this.shopPanel = shopPanel;
        
        // Use provided inventoryPanel reference, or find it if not provided
        if (inventoryPanel != null)
        {
            this.inventoryPanel = inventoryPanel;
        }
        else if (inventoryPanel == null)
        {
            inventoryPanel = ComponentInjector.FindComponent<InventoryPanel>();
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
        var characterService = Services.Get<ICharacterService>();
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
        if (entry == null) return;
        
        var shopService = Services.Get<IShopService>();
        if (shopService == null) return;
        
        if (shopService.BuyItem(entry, 1))
        {
            // Refresh display after purchase
            UpdateDisplay();
            
            // Refresh inventory if needed
            if (inventoryPanel != null)
            {
                inventoryPanel.RefreshDisplay();
            }
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (entry != null && entry.item != null)
        {
            // Build tooltip description
            string description = entry.item.description;
            
            // Add equipment stats if it's equipment
            if (entry.item.IsEquipment() && entry.item.equipmentData != null)
            {
                description += "\n\n" + GetEquipmentStatsText(entry.item.equipmentData);
            }
            
            // Add price info
            description += $"\n\n<color=yellow>Price: {entry.price} Gold</color>";
            
            EventBus.Publish(new TooltipShowEvent { title = entry.item.itemName, description = description });
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        EventBus.Publish(new TooltipHideEvent());
    }
    
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
}

