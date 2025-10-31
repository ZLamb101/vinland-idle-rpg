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
    /// Add an item to the inventory. Returns true if successful.
    /// </summary>
    public bool AddItem(InventoryItem newItem)
    {
        if (newItem == null || newItem.IsEmpty()) 
        {
            return false;
        }
        
        // Create a copy to avoid modifying the original item
        InventoryItem itemToAdd = new InventoryItem(newItem.itemName, newItem.quantity, newItem.icon);
        itemToAdd.description = newItem.description;
        itemToAdd.maxStackSize = newItem.maxStackSize;
        itemToAdd.itemType = newItem.itemType;
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
                
                if (itemToAdd.quantity <= 0) return true;
            }
        }
        
        // If there's still quantity left, find an empty slot
        if (itemToAdd.quantity > 0)
        {
            for (int i = 0; i < maxSlots; i++)
            {
                if (items[i].IsEmpty())
                {
                    items[i] = itemToAdd;
                    return true;
                }
            }
        }
        
        return false; // Inventory is full
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
}