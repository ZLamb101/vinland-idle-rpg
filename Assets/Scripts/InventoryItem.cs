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
        equipmentAssetName = "";
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
            Debug.Log($"SetEquipmentData: Stored '{equipmentAssetName}' for {itemName}");
        }
        else
        {
            equipmentAssetName = "";
        }
    }
    
    /// <summary>
    /// Load the equipment data from the stored asset name
    /// </summary>
    public void LoadEquipmentData()
    {
        Debug.Log($"LoadEquipmentData called for {itemName}, equipmentAssetName: '{equipmentAssetName}'");
        
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
                Debug.LogWarning($"Could not load equipment: {equipmentAssetName}. Make sure it's in Resources/Equipment/ or Resources/ folder!");
            }
            else
            {
                Debug.Log($"Successfully loaded equipment: {equipmentData.name}");
            }
        }
        else
        {
            Debug.LogWarning($"equipmentAssetName is empty for {itemName}!");
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
