using UnityEngine;

/// <summary>
/// Represents a single item in the inventory.
/// </summary>
[System.Serializable]
public class InventoryItem
{
    [Header("Item Info")]
    public string itemName = "Stone";
    public string description = "A simple stone";
    public Sprite icon;
    
    [Header("Stacking")]
    public int quantity = 1;
    public int maxStackSize = 99;
    
    [Header("Item Type")]
    public ItemType itemType = ItemType.Material;
    
    [Header("Item Value")]
    [Tooltip("Base gold value of this item (for selling)")]
    public int baseValue = 1;
    
    [Header("Item Data Reference")]
    [SerializeField] // Serialized name for reloading item data (including icon)
    public string itemDataAssetName = ""; // Name of the ItemData asset to reload from
    
    [Header("Equipment Reference")]
    [System.NonSerialized] // Don't serialize the direct reference
    public EquipmentData equipmentData; // Runtime reference to equipment data
    
    [SerializeField] // Force Unity to serialize this field
    public string equipmentAssetName = ""; // Serialized name for loading
    
    public InventoryItem(string name, int qty = 1, Sprite itemIcon = null)
    {
        itemName = name;
        quantity = qty;
        icon = itemIcon;
    }
    
    public InventoryItem()
    {
        // Default constructor for serialization - creates truly empty item
        itemName = "";
        quantity = 0;
        icon = null;
        description = "";
        itemDataAssetName = "";
        equipmentAssetName = "";
    }
    
    /// <summary>
    /// Set the ItemData reference for reloading item data after deserialization
    /// </summary>
    public void SetItemDataReference(ItemData itemData)
    {
        if (itemData != null)
        {
            itemDataAssetName = itemData.name;
        }
        else
        {
            itemDataAssetName = "";
        }
    }
    
    /// <summary>
    /// Set the equipment data and store its name for serialization
    /// </summary>
    public void SetEquipmentData(EquipmentData data)
    {
        equipmentData = data;
        if (data != null)
        {
            // Store the asset name (without path, just the name)
            equipmentAssetName = data.name;
        }
        else
        {
            equipmentAssetName = "";
        }
    }
    
    /// <summary>
    /// Load item data (including icon) from the stored asset name
    /// </summary>
    public void LoadItemData()
    {
        // If we have an asset name, try loading from that
        if (!string.IsNullOrEmpty(itemDataAssetName))
        {
            // Try loading from Items subfolder first
            ItemData itemData = Resources.Load<ItemData>("Items/" + itemDataAssetName);
            
            // If not found, try root Resources folder
            if (itemData == null)
            {
                itemData = Resources.Load<ItemData>(itemDataAssetName);
            }
            
            if (itemData != null)
            {
                // Reload the icon from the ItemData
                icon = itemData.icon;
                
                // Also reload equipment data if this is equipment
                if (itemData.equipmentData != null)
                {
                    equipmentData = itemData.equipmentData;
                    equipmentAssetName = itemData.equipmentData.name;
                }
                
                return;
            }
        }
        
        // Fallback: Try to find ItemData by matching item name (for legacy save files)
        if (icon == null && !string.IsNullOrEmpty(itemName))
        {
            // Try loading by item name in Items subfolder
            ItemData itemData = Resources.Load<ItemData>("Items/" + itemName);
            
            // If not found, try root Resources folder
            if (itemData == null)
            {
                itemData = Resources.Load<ItemData>(itemName);
            }
            
            if (itemData != null && itemData.itemName == itemName)
            {
                // Found matching ItemData - reload icon and store asset name for future saves
                icon = itemData.icon;
                itemDataAssetName = itemData.name;
                
                // Also reload equipment data if this is equipment
                if (itemData.equipmentData != null)
                {
                    equipmentData = itemData.equipmentData;
                    equipmentAssetName = itemData.equipmentData.name;
                }
            }
        }
    }
    
    /// <summary>
    /// Load the equipment data from the stored asset name
    /// </summary>
    public void LoadEquipmentData()
    {
        if (!string.IsNullOrEmpty(equipmentAssetName))
        {
            // Try loading from Equipment subfolder first
            equipmentData = Resources.Load<EquipmentData>("Equipment/" + equipmentAssetName);
            
            // If not found, try root Resources folder
            if (equipmentData == null)
            {
                equipmentData = Resources.Load<EquipmentData>(equipmentAssetName);
            }
            
            if (equipmentData == null)
            {
            }
            else
            {
            }
        }
        else
        {
        }
    }
    
    public bool CanStackWith(InventoryItem other)
    {
        return other != null && 
               itemName == other.itemName && 
               itemType == other.itemType &&
               quantity < maxStackSize;
    }
    
    public bool IsEmpty()
    {
        return quantity <= 0 || string.IsNullOrEmpty(itemName);
    }
    
    public void Clear()
    {
        itemName = "";
        quantity = 0;
        icon = null;
        itemDataAssetName = "";
        equipmentData = null;
        equipmentAssetName = "";
    }
    
    /// <summary>
    /// Check if this item is equipment
    /// </summary>
    public bool IsEquipment()
    {
        return itemType == ItemType.Equipment && equipmentData != null;
    }
}

public enum ItemType
{
    Material,
    Equipment,
    Consumable,
    Quest
}
