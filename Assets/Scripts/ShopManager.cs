using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager for handling shop interactions, stock management, and transactions.
/// </summary>
public class ShopManager : MonoBehaviour, IShopService
{
    public static ShopManager Instance { get; private set; }
    
    [Header("Shop State")]
    private ShopData currentShop;
    private bool isShopOpen = false;
    
    [Header("Stock Tracking")]
    // Dictionary to track stock state per shop (keyed by shop instance ID or name)
    private Dictionary<string, Dictionary<int, int>> shopStockStates = new Dictionary<string, Dictionary<int, int>>();
    private Dictionary<string, float> shopLastRefreshTimes = new Dictionary<string, float>();
    
    [Header("BuyBack")]
    private InventoryItem buyBackItem;
    private int buyBackPrice;
    private bool hasBuyBack = false;
    
    // Events
    public event Action<ShopData> OnShopOpened;
    public event Action OnShopClosed;
    public event Action<int> OnStockChanged; // ShopItemEntry index
    public event Action OnBuyBackChanged;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Register with service locator
        Services.Register<IShopService>(this);
    }
    
    /// <summary>
    /// Open a shop and initialize/refresh stock if needed
    /// </summary>
    public void OpenShop(ShopData shop)
    {
        if (shop == null)
        {
            return;
        }
        
        // Close dialogue if open
        if (Services.TryGet<IDialogueService>(out var dialogueService) && dialogueService.IsDialogueActive())
        {
            dialogueService.EndDialogue();
        }
        
        currentShop = shop;
        isShopOpen = true;
        
        string shopKey = shop.name; // Use shop name as key
        
        // Check if we need to refresh stock (10+ minutes since last refresh)
        bool needsRefresh = false;
        if (shopLastRefreshTimes.ContainsKey(shopKey))
        {
            float timeSinceRefresh = Time.time - shopLastRefreshTimes[shopKey];
            if (timeSinceRefresh >= shop.stockRefreshInterval)
            {
                needsRefresh = true;
            }
        }
        else
        {
            // First time opening this shop - initialize stock
            needsRefresh = true;
        }
        
        if (needsRefresh)
        {
            RefreshStock(shop);
            shopLastRefreshTimes[shopKey] = Time.time;
        }
        else
        {
            // Restore stock state from dictionary
            RestoreStockState(shop);
        }
        
        OnShopOpened?.Invoke(shop);
    }
    
    /// <summary>
    /// Close the current shop
    /// </summary>
    public void CloseShop()
    {
        if (!isShopOpen) return;
        
        // Save current stock state before closing
        if (currentShop != null)
        {
            SaveStockState(currentShop);
        }
        
        currentShop = null;
        isShopOpen = false;
        
        OnShopClosed?.Invoke();
    }
    
    /// <summary>
    /// Refresh stock for a shop (restore all items to full stock)
    /// </summary>
    void RefreshStock(ShopData shop)
    {
        if (shop == null) return;
        
        foreach (ShopItemEntry entry in shop.shopItems)
        {
            if (entry != null)
            {
                entry.RestoreStock();
            }
        }
    }
    
    /// <summary>
    /// Save current stock state to dictionary
    /// </summary>
    void SaveStockState(ShopData shop)
    {
        if (shop == null) return;
        
        string shopKey = shop.name;
        Dictionary<int, int> stockState = new Dictionary<int, int>();
        
        for (int i = 0; i < shop.shopItems.Count; i++)
        {
            if (shop.shopItems[i] != null)
            {
                stockState[i] = shop.shopItems[i].currentStock;
            }
        }
        
        shopStockStates[shopKey] = stockState;
    }
    
    /// <summary>
    /// Restore stock state from dictionary
    /// </summary>
    void RestoreStockState(ShopData shop)
    {
        if (shop == null) return;
        
        string shopKey = shop.name;
        if (!shopStockStates.ContainsKey(shopKey))
        {
            // No saved state, initialize to full stock
            RefreshStock(shop);
            return;
        }
        
        Dictionary<int, int> stockState = shopStockStates[shopKey];
        
        for (int i = 0; i < shop.shopItems.Count; i++)
        {
            if (shop.shopItems[i] != null && stockState.ContainsKey(i))
            {
                shop.shopItems[i].currentStock = stockState[i];
            }
            else if (shop.shopItems[i] != null)
            {
                // Entry not in saved state, restore to full
                shop.shopItems[i].RestoreStock();
            }
        }
    }
    
    /// <summary>
    /// Buy an item from the shop
    /// </summary>
    public bool BuyItem(ShopItemEntry entry, int quantity = 1)
    {
        if (!isShopOpen || currentShop == null)
        {
            return false;
        }
        
        if (entry == null || entry.item == null)
        {
            return false;
        }
        
        if (!entry.IsInStock())
        {
            return false;
        }
        
        if (quantity > entry.currentStock)
        {
            quantity = entry.currentStock; // Buy available stock
        }
        
        int totalCost = entry.price * quantity;
        
        // Check if player has enough gold
        if (CharacterManager.Instance == null || CharacterManager.Instance.GetGold() < totalCost)
        {
            return false;
        }
        
        // Check if player has inventory space
        if (CharacterManager.Instance != null)
        {
            // Create item and try to add to inventory
            InventoryItem itemToAdd = entry.item.CreateInventoryItem(quantity);
            bool added = CharacterManager.Instance.AddItemToInventory(itemToAdd);
            
            if (!added)
            {
                return false;
            }
            
            // Deduct gold
            CharacterManager.Instance.SpendGold(totalCost);
            
            // Reduce stock
            entry.currentStock -= quantity;
            
            // Find entry index for event
            int entryIndex = currentShop.shopItems.IndexOf(entry);
            if (entryIndex >= 0)
            {
                OnStockChanged?.Invoke(entryIndex);
            }
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Sell an item from inventory to the shop
    /// </summary>
    public bool SellItem(InventoryItem item, int slotIndex)
    {
        if (!isShopOpen)
        {
            return false;
        }
        
        if (item == null || item.IsEmpty())
        {
            return false;
        }
        
        // Calculate sell value (exact baseValue)
        int sellValue = item.baseValue * item.quantity;
        
        // Store for buyback
        buyBackItem = new InventoryItem(item.itemName, item.quantity, item.icon);
        buyBackItem.description = item.description;
        buyBackItem.maxStackSize = item.maxStackSize;
        buyBackItem.itemType = item.itemType;
        buyBackItem.baseValue = item.baseValue;
        buyBackItem.itemDataAssetName = item.itemDataAssetName;
        buyBackItem.SetEquipmentData(item.equipmentData);
        buyBackPrice = sellValue;
        hasBuyBack = true;
        
        // Remove item from inventory
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.RemoveItemFromInventory(slotIndex, item.quantity);
        }
        
        // Add gold
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.AddGold(sellValue);
        }
        
        OnBuyBackChanged?.Invoke();
        return true;
    }
    
    /// <summary>
    /// Buy back the last sold item
    /// </summary>
    public bool BuyBackItem()
    {
        if (!hasBuyBack || buyBackItem == null)
        {
            return false;
        }
        
        if (!isShopOpen)
        {
            return false;
        }
        
        // Check if player has enough gold
        if (CharacterManager.Instance == null || CharacterManager.Instance.GetGold() < buyBackPrice)
        {
            return false;
        }
        
        // Check if player has inventory space
        if (CharacterManager.Instance != null)
        {
            bool added = CharacterManager.Instance.AddItemToInventory(buyBackItem);
            
            if (!added)
            {
                return false;
            }
            
            // Store item name before clearing
            string itemName = buyBackItem.itemName;
            int price = buyBackPrice;
            
            // Deduct gold
            CharacterManager.Instance.SpendGold(buyBackPrice);
            
            // Clear buyback
            buyBackItem = null;
            buyBackPrice = 0;
            hasBuyBack = false;
            
            OnBuyBackChanged?.Invoke();
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Get time remaining until stock refresh (in seconds)
    /// </summary>
    public float GetTimeUntilRefresh()
    {
        if (!isShopOpen || currentShop == null) return 0f;
        
        string shopKey = currentShop.name;
        if (!shopLastRefreshTimes.ContainsKey(shopKey))
        {
            return currentShop.stockRefreshInterval;
        }
        
        float timeSinceRefresh = Time.time - shopLastRefreshTimes[shopKey];
        float timeRemaining = currentShop.stockRefreshInterval - timeSinceRefresh;
        
        return Mathf.Max(0f, timeRemaining);
    }
    
    // Getters
    public bool IsShopOpen() => isShopOpen;
    public ShopData GetCurrentShop() => currentShop;
    public InventoryItem GetBuyBackItem() => buyBackItem;
    public int GetBuyBackPrice() => buyBackPrice;
    public bool HasBuyBack() => hasBuyBack;
}

