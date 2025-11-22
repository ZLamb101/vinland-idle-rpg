using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the shared bank data.
/// Items in the bank have infinite stack size.
/// </summary>
[System.Serializable]
public class BankData
{
    [Header("Bank Settings")]
    public int maxSlots = 20;
    
    [Header("Bank Items")]
    public InventoryItem[] items;
    
    public BankData()
    {
        items = new InventoryItem[maxSlots];
        // Initialize empty slots
        for (int i = 0; i < maxSlots; i++)
        {
            items[i] = new InventoryItem();
        }
    }
    
    /// <summary>
    /// Call this after loading data to restore item references
    /// </summary>
    public void LoadAllItemReferences()
    {
        if (items == null) return;
        
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && !items[i].IsEmpty())
            {
                items[i].LoadItemData();
            }
        }
    }
    
    /// <summary>
    /// Check if the bank has space for a new item.
    /// </summary>
    public bool HasSpace(InventoryItem item)
    {
        if (items == null) return false;
        
        // Check if we can stack it (infinite stacking)
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && !items[i].IsEmpty() && items[i].itemName == item.itemName)
            {
                return true;
            }
        }
        
        // Check for empty slots
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].IsEmpty()) return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Add an item to the bank. Returns true if successful.
    /// Bank items have infinite stack size.
    /// </summary>
    public bool AddItem(InventoryItem newItem)
    {
        if (newItem == null || newItem.IsEmpty()) return false;
        
        // 1. Try to stack with existing item first
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && !items[i].IsEmpty() && items[i].itemName == newItem.itemName)
            {
                items[i].quantity += newItem.quantity;
                return true;
            }
        }
        
        // 2. Find first empty slot
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].IsEmpty())
            {
                // Create a deep copy
                items[i] = new InventoryItem(newItem.itemName, newItem.quantity, newItem.icon);
                items[i].description = newItem.description;
                items[i].maxStackSize = int.MaxValue; // Infinite stack in bank
                items[i].itemType = newItem.itemType;
                items[i].baseValue = newItem.baseValue;
                items[i].itemDataAssetName = newItem.itemDataAssetName;
                items[i].equipmentAssetName = newItem.equipmentAssetName;
                items[i].equipmentData = newItem.equipmentData;
                
                return true;
            }
        }
        
        Debug.LogWarning($"[BankData] Bank is full! Cannot add {newItem.itemName} x{newItem.quantity}");
        return false;
    }
    
    /// <summary>
    /// Remove an item from the bank.
    /// </summary>
    public bool RemoveItem(int slotIndex, int quantity)
    {
        if (items == null) return false;
        if (slotIndex < 0 || slotIndex >= items.Length) return false;
        if (items[slotIndex] == null || items[slotIndex].IsEmpty()) return false;
        
        if (items[slotIndex].quantity < quantity) return false;
        
        items[slotIndex].quantity -= quantity;
        
        if (items[slotIndex].quantity <= 0)
        {
            items[slotIndex].Clear();
        }
        
        return true;
    }
    
    public InventoryItem GetItem(int slotIndex)
    {
        if (items == null) return null;
        if (slotIndex < 0 || slotIndex >= items.Length) return null;
        return items[slotIndex];
    }
}
