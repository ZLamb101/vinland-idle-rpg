using System;

public interface IBankService
{
    /// <summary>
    /// Get the current bank data
    /// </summary>
    BankData GetBankData();
    
    /// <summary>
    /// Deposit an item into the bank.
    /// Returns true if successful (fully deposited).
    /// </summary>
    bool DepositItem(InventoryItem item, int slotIndex = -1);
    
    /// <summary>
    /// Deposit all valid items from the player's inventory into the bank.
    /// </summary>
    void DepositAllItems(InventoryData playerInventory);
    
    /// <summary>
    /// Withdraw an item from the bank to the player's inventory.
    /// Returns true if successful.
    /// </summary>
    bool WithdrawItem(int bankSlotIndex, int quantity, InventoryData playerInventory);
    
    /// <summary>
    /// Swap two items within the bank.
    /// </summary>
    bool SwapBankItems(int slot1, int slot2);
    
    /// <summary>
    /// Event triggered when bank data changes
    /// </summary>
    event Action OnBankChanged;
}
