using System;

/// <summary>
/// Interface for shop management services
/// </summary>
public interface IShopService
{
    // Events
    event Action<ShopData> OnShopOpened;
    event Action OnShopClosed;
    event Action<int> OnStockChanged;
    event Action OnBuyBackChanged;
    
    // Shop Control
    void OpenShop(ShopData shop);
    void CloseShop();
    
    // Transactions
    bool BuyItem(ShopItemEntry entry, int quantity = 1);
    bool SellItem(InventoryItem item, int slotIndex);
    bool BuyBackItem();
    
    // Getters
    bool IsShopOpen();
    ShopData GetCurrentShop();
    InventoryItem GetBuyBackItem();
    int GetBuyBackPrice();
    bool HasBuyBack();
    float GetTimeUntilRefresh();
}

