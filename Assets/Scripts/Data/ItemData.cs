using UnityEngine;

/// <summary>
/// ScriptableObject for defining item data.
/// Create these in the Project window: Right-click > Create > Vinland > Item
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Vinland/Item", order = 1)]
public class ItemData : ScriptableObject
{
    [Header("Item Info")]
    public string itemName = "New Item";
    [TextArea]
    public string description = "Item description";
    public Sprite icon;
    
    [Header("Item Properties")]
    public ItemType itemType = ItemType.Material;
    public int maxStackSize = 99;
    public int baseValue = 1; // Gold value
    
    [Header("Equipment (if itemType = Equipment)")]
    public EquipmentData equipmentData; // Reference to equipment data if this is equipment
    
    [Header("Recipe (if itemType = Recipe)")]
    public RecipeData recipeData; // Recipe to unlock when this item is used
    
    [Header("Quest Rewards")]
    public bool canBeQuestReward = true;
    public int questRewardQuantity = 1;
    
    /// <summary>
    /// Create an InventoryItem from this ItemData
    /// </summary>
    public InventoryItem CreateInventoryItem(int quantity = -1)
    {
        int qty = quantity > 0 ? quantity : questRewardQuantity;
        
        InventoryItem item = new InventoryItem(itemName, qty, icon);
        
        // If this is equipment, remove flavor text as requested
        if (IsEquipment() && equipmentData != null)
        {
            item.description = "";
        }
        else
        {
            item.description = description;
        }
        
        item.maxStackSize = maxStackSize;
        item.itemType = itemType;
        item.baseValue = baseValue; // Store base value for selling
        item.SetItemDataReference(this); // Store reference to this ItemData for reloading
        item.SetEquipmentData(equipmentData); // Link equipment data if exists
        
        return item;
    }
    
    /// <summary>
    /// Check if this item is equipment
    /// </summary>
    public bool IsEquipment()
    {
        return itemType == ItemType.Equipment && equipmentData != null;
    }
}
