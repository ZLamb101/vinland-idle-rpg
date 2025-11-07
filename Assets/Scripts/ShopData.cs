using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a single item entry in a shop with pricing and stock information
/// </summary>
[System.Serializable]
public class ShopItemEntry
{
    [Tooltip("The item being sold")]
    public ItemData item;
    
    [Tooltip("Price to buy this item (custom price, not based on baseValue)")]
    public int price = 10;
    
    [Tooltip("Maximum stock available (full stock amount)")]
    public int maxStock = 10;
    
    [Tooltip("Current stock available (runtime value, managed by ShopManager)")]
    [System.NonSerialized]
    public int currentStock;
    
    /// <summary>
    /// Initialize current stock to max stock
    /// </summary>
    public void InitializeStock()
    {
        currentStock = maxStock;
    }
    
    /// <summary>
    /// Check if this item is in stock
    /// </summary>
    public bool IsInStock()
    {
        return currentStock > 0;
    }
    
    /// <summary>
    /// Restore stock to full
    /// </summary>
    public void RestoreStock()
    {
        currentStock = maxStock;
    }
}

/// <summary>
/// ScriptableObject that defines shop inventory, pricing, and stock refresh settings.
/// Create instances via: Right-click in Project → Create → Vinland → Shop
/// </summary>
[CreateAssetMenu(fileName = "New Shop", menuName = "Vinland/Shop", order = 6)]
public class ShopData : ScriptableObject
{
    [Header("Shop Info")]
    public string shopName = "General Store";
    
    [Header("Shop Items")]
    [Tooltip("List of items available in this shop with their prices and stock")]
    public List<ShopItemEntry> shopItems = new List<ShopItemEntry>();
    
    [Header("Stock Refresh")]
    [Tooltip("Time in seconds until stock refreshes (default: 600 = 10 minutes)")]
    public float stockRefreshInterval = 600f; // 10 minutes
    
    /// <summary>
    /// Initialize all shop items' stock to max
    /// </summary>
    public void InitializeStock()
    {
        foreach (ShopItemEntry entry in shopItems)
        {
            if (entry != null)
            {
                entry.InitializeStock();
            }
        }
    }
}

