using UnityEngine;
using System;

public class BankManager : MonoBehaviour, IBankService
{
    private AccountSaveData accountData;
    
    public event Action OnBankChanged;
    
    private void Awake()
    {
        // Prevent duplicates - GameBootstrap creates all managers
        if (Services.IsRegistered<IBankService>())
        {
            Debug.LogWarning("[BankManager] Service already registered. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject); // Persist between scenes
        
        Services.Register<IBankService>(this);
        LoadBankData();
    }
    
    private void OnDestroy()
    {
        SaveBankData();
        Services.Unregister<IBankService>();
    }
    
    private void OnApplicationQuit()
    {
        SaveBankData();
    }
    
    private void LoadBankData()
    {
        accountData = SaveSystem.LoadAccount();
        if (accountData == null)
        {
            accountData = AccountSaveData.CreateDefault();
        }
        
        // Restore item references
        if (accountData.bankData != null)
        {
            accountData.bankData.LoadAllItemReferences();
        }
        
        Debug.Log("[BankManager] Bank data loaded.");
    }
    
    public void SaveBankData()
    {
        if (accountData != null)
        {
            SaveSystem.SaveAccount(accountData);
            Debug.Log("[BankManager] Bank data saved.");
        }
    }
    
    public BankData GetBankData()
    {
        if (accountData == null) LoadBankData();
        return accountData.bankData;
    }
    
    public bool DepositItem(InventoryItem item, int slotIndex = -1)
    {
        if (item == null || item.IsEmpty()) return false;
        
        int depositedQuantity = item.quantity;
        
        if (accountData.bankData.AddItem(item))
        {
            // Item added successfully
            // Since we passed a reference or copy, we need to make sure the source is handled by the caller
            // But wait, AddItem modifies the bank state.
            
            // Publish event if we know the slot index (so inventory UI can update)
            if (slotIndex >= 0)
            {
                EventBus.Publish(new ItemRemovedEvent
                {
                    item = item,
                    quantity = depositedQuantity,
                    slotIndex = slotIndex
                });
            }
            
            OnBankChanged?.Invoke();
            SaveBankData(); // Auto-save on transaction
            return true;
        }
        
        return false;
    }
    
    public void DepositAllItems(InventoryData playerInventory)
    {
        if (playerInventory == null || playerInventory.items == null) return;
        
        bool changed = false;
        
        for (int i = 0; i < playerInventory.items.Length; i++)
        {
            InventoryItem item = playerInventory.items[i];
            
            // Skip empty items and equipped items (if any logic exists for that, though InventoryItem doesn't seem to have 'isEquipped' flag directly visible here, usually handled by slot logic)
            // Assuming we deposit everything that is valid.
            
            if (item != null && !item.IsEmpty())
            {
                // Store info for the event BEFORE clearing
                int depositedQuantity = item.quantity;
                string itemName = item.itemName;
                
                // Try to add to bank
                // We need to clone it because AddItem might modify it or we might need to clear the source
                InventoryItem toDeposit = new InventoryItem(item.itemName, item.quantity, item.icon);
                toDeposit.description = item.description;
                toDeposit.maxStackSize = item.maxStackSize;
                toDeposit.itemType = item.itemType;
                toDeposit.baseValue = item.baseValue;
                toDeposit.itemDataAssetName = item.itemDataAssetName;
                toDeposit.equipmentAssetName = item.equipmentAssetName;
                toDeposit.equipmentData = item.equipmentData;
                
                if (accountData.bankData.AddItem(toDeposit))
                {
                    // Successfully added to bank, remove from inventory using proper method
                    playerInventory.RemoveItem(i, depositedQuantity);
                    changed = true;
                    
                    // Publish event so inventory UI updates in real-time
                    // Use toDeposit (the clone) instead of item (which is now cleared)
                    EventBus.Publish(new ItemRemovedEvent
                    {
                        item = toDeposit,
                        quantity = depositedQuantity,
                        slotIndex = i
                    });
                }
            }
        }
        
        if (changed)
        {
            OnBankChanged?.Invoke();
            SaveBankData();
        }
    }
    
    public bool WithdrawItem(int bankSlotIndex, int quantity, InventoryData playerInventory)
    {
        InventoryItem bankItem = accountData.bankData.GetItem(bankSlotIndex);
        if (bankItem == null || bankItem.IsEmpty()) return false;
        
        if (quantity > bankItem.quantity) quantity = bankItem.quantity;
        
        // Create item to add to inventory
        InventoryItem itemToWithdraw = new InventoryItem(bankItem.itemName, quantity, bankItem.icon);
        itemToWithdraw.description = bankItem.description;
        // Restore original max stack size if possible, otherwise default to 99 or whatever
        // The bank item has maxStackSize = int.MaxValue. We need to revert this for the player inventory.
        // We should probably look up the original item data to get the true max stack size.
        // For now, let's try to load it.
        itemToWithdraw.itemDataAssetName = bankItem.itemDataAssetName;
        itemToWithdraw.LoadItemData(); // This might restore maxStackSize if it's in ItemData
        
        // If loading didn't restore a normal stack size (e.g. it's still int.MaxValue or 0), default to 99
        if (itemToWithdraw.maxStackSize == int.MaxValue || itemToWithdraw.maxStackSize <= 0)
        {
             // Try to find original max stack size from a fresh load if possible, or hardcode default
             // Since we don't have easy access to the original ScriptableObject without loading it (which we just tried),
             // we might need to rely on the fact that AddItem in InventoryData handles splitting.
             // But wait, InventoryData.AddItem respects the item's maxStackSize. 
             // If we pass an item with maxStackSize = int.MaxValue, it won't split it!
             
             // FIX: We must ensure the withdrawn item has a valid player-inventory stack size.
             // Let's assume 99 for now if we can't find better, or 1 for equipment.
             if (bankItem.itemType == ItemType.Equipment)
                 itemToWithdraw.maxStackSize = 1;
             else
                 itemToWithdraw.maxStackSize = 99; // Default fallback
                 
             // If we successfully loaded ItemData, use that
             // We need to check if LoadItemData actually sets maxStackSize. 
             // Looking at InventoryItem.cs, LoadItemData DOES NOT seem to set maxStackSize from the SO!
             // It only sets icon and equipmentData.
             // This is a potential issue in the existing codebase, but I should fix it here or workaround it.
             
             // Workaround: Re-load the SO manually to get maxStackSize
             if (!string.IsNullOrEmpty(bankItem.itemDataAssetName))
             {
                 ItemData data = Resources.Load<ItemData>("Items/" + bankItem.itemDataAssetName);
                 if (data == null) data = Resources.Load<ItemData>(bankItem.itemDataAssetName);
                 
                 if (data != null)
                 {
                     itemToWithdraw.maxStackSize = data.maxStackSize;
                 }
             }
        }
        
        // Add to player inventory
        var result = playerInventory.AddItem(itemToWithdraw);
        
        if (result.success || result.itemsAdded > 0)
        {
            // Remove from bank
            accountData.bankData.RemoveItem(bankSlotIndex, result.itemsAdded);
            
            // Publish event so inventory UI updates in real-time
            EventBus.Publish(new ItemAddedEvent
            {
                item = itemToWithdraw,
                quantity = result.itemsAdded,
                wasSuccessful = result.success
            });
            
            OnBankChanged?.Invoke();
            SaveBankData();
            return true;
        }
        
        return false;
    }
    
    public bool SwapBankItems(int slot1, int slot2)
    {
        if (accountData.bankData.items == null) return false;
        if (slot1 < 0 || slot1 >= accountData.bankData.items.Length) return false;
        if (slot2 < 0 || slot2 >= accountData.bankData.items.Length) return false;
        
        InventoryItem temp = accountData.bankData.items[slot1];
        accountData.bankData.items[slot1] = accountData.bankData.items[slot2];
        accountData.bankData.items[slot2] = temp;
        
        OnBankChanged?.Invoke();
        SaveBankData();
        return true;
    }
}
