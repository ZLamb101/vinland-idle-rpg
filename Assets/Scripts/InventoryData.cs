using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the player's inventory data.
/// </summary>
[System.Serializable]
public class InventoryData
{
    [Header("Inventory Settings")]
    public int maxSlots = 20;
    
    [Header("Inventory Items")]
    public InventoryItem[] items;
    
    public InventoryData()
    {
        items = new InventoryItem[maxSlots];
        // Initialize empty slots
        for (int i = 0; i < maxSlots; i++)
        {
            items[i] = new InventoryItem(); // This now creates truly empty items
        }
    }
    
    /// <summary>
    /// Call this after loading data to restore equipment references
    /// </summary>
    public void LoadAllEquipmentReferences()
    {
        if (items == null) return;
        
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].itemType == ItemType.Equipment)
            {
                items[i].LoadEquipmentData();
            }
        }
    }
    
    /// <summary>
    /// Result of adding an item to inventory
    /// </summary>
    public class AddItemResult
    {
        public bool success; // True if all items were added
        public int itemsAdded; // Number of items successfully added
        public int itemsRemaining; // Number of items that couldn't be added (0 if success)
        
        public AddItemResult(bool success, int added, int remaining)
        {
            this.success = success;
            this.itemsAdded = added;
            this.itemsRemaining = remaining;
        }
    }
    
    /// <summary>
    /// Add an item to the inventory. Returns result with success status and quantities.
    /// </summary>
    public AddItemResult AddItem(InventoryItem newItem)
    {
        if (newItem == null || newItem.IsEmpty()) 
        {
            return new AddItemResult(false, 0, newItem != null ? newItem.quantity : 0);
        }
        
        int originalQuantity = newItem.quantity;
        
        // Create a copy to avoid modifying the original item
        InventoryItem itemToAdd = new InventoryItem(newItem.itemName, newItem.quantity, newItem.icon);
        itemToAdd.description = newItem.description;
        itemToAdd.maxStackSize = newItem.maxStackSize;
        itemToAdd.itemType = newItem.itemType;
        itemToAdd.baseValue = newItem.baseValue; // Copy base value for selling
        itemToAdd.equipmentAssetName = newItem.equipmentAssetName; // Copy equipment reference!
        itemToAdd.equipmentData = newItem.equipmentData; // Copy runtime reference too
        
        // First try to stack with existing items
        for (int i = 0; i < maxSlots; i++)
        {
            if (!items[i].IsEmpty() && items[i].CanStackWith(itemToAdd))
            {
                int canAdd = Mathf.Min(itemToAdd.quantity, items[i].maxStackSize - items[i].quantity);
                items[i].quantity += canAdd;
                itemToAdd.quantity -= canAdd;
                
                if (itemToAdd.quantity <= 0)
                {
                    return new AddItemResult(true, originalQuantity, 0);
                }
            }
        }
        
        // If there's still quantity left, find empty slots and split into multiple stacks
        while (itemToAdd.quantity > 0)
        {
            // Find an empty slot
            int emptySlotIndex = -1;
            for (int i = 0; i < maxSlots; i++)
            {
                if (items[i].IsEmpty())
                {
                    emptySlotIndex = i;
                    break;
                }
            }
            
            // No empty slots available
            if (emptySlotIndex == -1)
            {
                int itemsAdded = originalQuantity - itemToAdd.quantity;
                return new AddItemResult(false, itemsAdded, itemToAdd.quantity);
            }
            
            // Create a new stack with up to maxStackSize items
            int stackSize = Mathf.Min(itemToAdd.quantity, itemToAdd.maxStackSize);
            InventoryItem newStack = new InventoryItem(itemToAdd.itemName, stackSize, itemToAdd.icon);
            newStack.description = itemToAdd.description;
            newStack.maxStackSize = itemToAdd.maxStackSize;
            newStack.itemType = itemToAdd.itemType;
            newStack.baseValue = itemToAdd.baseValue;
            newStack.equipmentAssetName = itemToAdd.equipmentAssetName;
            newStack.equipmentData = itemToAdd.equipmentData;
            
            items[emptySlotIndex] = newStack;
            itemToAdd.quantity -= stackSize;
        }
        
        return new AddItemResult(true, originalQuantity, 0);
    }
    
    /// <summary>
    /// Add an item to the inventory. Returns true if successful (for backwards compatibility).
    /// </summary>
    public bool AddItemLegacy(InventoryItem newItem)
    {
        AddItemResult result = AddItem(newItem);
        return result.success;
    }
    
    /// <summary>
    /// Remove an item from the inventory at the specified slot.
    /// </summary>
    public bool RemoveItem(int slotIndex, int quantity = 1)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots) return false;
        if (items[slotIndex].IsEmpty()) return false;
        
        items[slotIndex].quantity -= quantity;
        
        if (items[slotIndex].quantity <= 0)
        {
            items[slotIndex].Clear();
        }
        
        return true;
    }
    
    /// <summary>
    /// Get an item at the specified slot.
    /// </summary>
    public InventoryItem GetItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots) return null;
        return items[slotIndex];
    }
    
    /// <summary>
    /// Check if the inventory has space for a new item.
    /// </summary>
    public bool HasSpace()
    {
        // Check for empty slots
        for (int i = 0; i < maxSlots; i++)
        {
            if (items[i].IsEmpty()) return true;
        }
        
        // Check for stackable items
        for (int i = 0; i < maxSlots; i++)
        {
            if (items[i].quantity < items[i].maxStackSize) return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Get the first empty slot index, or -1 if full.
    /// </summary>
    public int GetFirstEmptySlot()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (items[i].IsEmpty()) return i;
        }
        return -1;
    }
    
    /// <summary>
    /// Get total number of items in inventory.
    /// </summary>
    public int GetTotalItems()
    {
        int count = 0;
        for (int i = 0; i < maxSlots; i++)
        {
            if (!items[i].IsEmpty()) count++;
        }
        return count;
    }
    
    /// <summary>
    /// Swap items between two slots. Returns true if successful.
    /// </summary>
    public bool SwapItems(int slotIndex1, int slotIndex2)
    {
        if (slotIndex1 < 0 || slotIndex1 >= maxSlots) return false;
        if (slotIndex2 < 0 || slotIndex2 >= maxSlots) return false;
        if (slotIndex1 == slotIndex2) return false; // Can't swap with itself
        
        // Create temporary copies to avoid reference issues
        InventoryItem item1 = items[slotIndex1];
        InventoryItem item2 = items[slotIndex2];
        
        // Swap the items
        items[slotIndex1] = item2;
        items[slotIndex2] = item1;
        
        return true;
    }
}