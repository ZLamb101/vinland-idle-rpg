#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window for creating ItemData assets.
/// Access via: Vinland > Create Content > Item
/// </summary>
public class ItemCreatorWindow : ContentCreatorBase
{
    // Item data fields
    private string itemName = "New Item";
    private string description = "An item";
    private Sprite icon;
    private ItemType itemType = ItemType.Material;
    private int maxStackSize = 99;
    private int baseValue = 1;
    
    // Quest/drop settings
    private bool canBeQuestReward = true;
    private int questRewardQuantity = 1;
    
    // Equipment linking
    private EquipmentData equipmentData;
    
    [MenuItem("Vinland/Create Content/Item")]
    public static void ShowWindow()
    {
        ItemCreatorWindow window = GetWindow<ItemCreatorWindow>("Item Creator");
        window.minSize = new Vector2(600, 700);
        window.Show();
    }
    
    protected override void DrawContent()
    {
        DrawHeader("Item Creator");
        
        GUILayout.Space(10);
        EditorGUILayout.HelpBox("Create items for inventory, quests, and loot drops.", MessageType.Info);
        GUILayout.Space(10);
        
        // Type selector
        DrawTypeSection();
        
        // Basic info
        DrawBasicInfoSection();
        
        // Properties
        DrawPropertiesSection();
        
        // Quest/Drop settings
        DrawQuestDropSection();
        
        // Equipment linking (if type is Equipment)
        if (itemType == ItemType.Equipment)
        {
            DrawEquipmentLinkSection();
        }
        
        // Quick reference
        DrawQuickReferenceSection();
    }
    
    #region Section Drawing
    
    private void DrawTypeSection()
    {
        DrawHeader("Item Type");
        
        EditorGUI.BeginChangeCheck();
        itemType = DrawEnumPopup("Type", itemType, "What kind of item is this?");
        
        if (EditorGUI.EndChangeCheck())
        {
            // Adjust defaults based on type
            switch (itemType)
            {
                case ItemType.Equipment:
                    maxStackSize = 1;
                    canBeQuestReward = true;
                    break;
                case ItemType.Consumable:
                    maxStackSize = 20;
                    canBeQuestReward = false;
                    break;
                case ItemType.Material:
                    maxStackSize = 99;
                    canBeQuestReward = false;
                    break;
                case ItemType.Quest:
                    maxStackSize = 1;
                    canBeQuestReward = false;
                    break;
            }
        }
        
        // Show type description
        EditorGUILayout.HelpBox(GetTypeDescription(itemType), MessageType.None);
    }
    
    private void DrawBasicInfoSection()
    {
        DrawHeader("Basic Info");
        
        itemName = DrawTextField("Item Name", itemName, "Display name for the item");
        description = DrawTextArea("Description", description, "Item description (shown in tooltips)", 3);
        icon = DrawSpriteField("Icon", icon, 64);
    }
    
    private void DrawPropertiesSection()
    {
        DrawHeader("Properties");
        
        maxStackSize = DrawIntField("Max Stack Size", Mathf.Max(1, maxStackSize), "Maximum number of items per inventory slot");
        baseValue = DrawIntField("Base Value (Gold)", Mathf.Max(0, baseValue), "Sell value in gold");
        
        // Show stack size suggestions
        if (itemType == ItemType.Equipment && maxStackSize != 1)
        {
            EditorGUILayout.HelpBox("Equipment typically has stack size of 1", MessageType.Info);
        }
        else if (itemType == ItemType.Material && maxStackSize < 50)
        {
            EditorGUILayout.HelpBox("Materials usually stack to 50-99", MessageType.Info);
        }
    }
    
    private void DrawQuestDropSection()
    {
        DrawHeader("Quest & Drop Settings");
        
        canBeQuestReward = DrawToggle("Can Be Quest Reward", canBeQuestReward, "Allow this item as a quest reward");
        
        if (canBeQuestReward)
        {
            questRewardQuantity = DrawIntField("Default Quantity", Mathf.Max(1, questRewardQuantity), "Default quantity when given as reward");
        }
    }
    
    private void DrawEquipmentLinkSection()
    {
        DrawHeader("Equipment Link");
        
        EditorGUILayout.HelpBox("Equipment items must link to an EquipmentData asset for stats.", MessageType.Info);
        
        equipmentData = DrawObjectField("Equipment Data", equipmentData, false, "Link to equipment stats");
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Create New Equipment", GUILayout.Height(30)))
        {
            // Open Equipment Creator window
            EquipmentCreatorWindow.ShowWindow();
        }
        
        if (equipmentData != null)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Linked Equipment:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Name: {equipmentData.equipmentName}");
            EditorGUILayout.LabelField($"Slot: {equipmentData.slot}");
            EditorGUILayout.LabelField($"Tier: {equipmentData.tier}");
            EditorGUILayout.LabelField($"Level Req: {equipmentData.levelRequired}");
            EditorGUILayout.EndVertical();
        }
    }
    
    private void DrawQuickReferenceSection()
    {
        DrawHeader("Quick Reference");
        
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Item Type Guide:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("• Material - Crafting resources, monster drops");
        EditorGUILayout.LabelField("• Consumable - Potions, food, temporary buffs");
        EditorGUILayout.LabelField("• Equipment - Weapons, armor (requires EquipmentData)");
        EditorGUILayout.LabelField("• Quest - Quest-specific items, keys, etc.");
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(5);
        
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Typical Values:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("• Common materials: 1-5 gold");
        EditorGUILayout.LabelField("• Uncommon materials: 10-25 gold");
        EditorGUILayout.LabelField("• Rare materials: 50-100 gold");
        EditorGUILayout.LabelField("• Equipment: Auto-calculated from tier");
        EditorGUILayout.EndVertical();
    }
    
    #endregion
    
    #region Helper Methods
    
    private string GetTypeDescription(ItemType type)
    {
        switch (type)
        {
            case ItemType.Material:
                return "Materials are crafting resources and basic items. Can stack to high amounts.";
            case ItemType.Consumable:
                return "Consumables are used once (potions, food). Moderate stack size.";
            case ItemType.Equipment:
                return "Equipment provides stats when worn. Must link to EquipmentData. Stack size 1.";
            case ItemType.Quest:
                return "Quest items are for specific quests. Usually unique (stack size 1).";
            default:
                return "";
        }
    }
    
    #endregion
    
    #region Validation
    
    protected override void ValidateData()
    {
        ClearValidation();
        
        // Name validation
        if (string.IsNullOrWhiteSpace(itemName))
        {
            AddError("Item name is required");
        }
        else if (itemName == "New Item")
        {
            AddWarning("Consider giving the item a unique name");
        }
        
        // Icon validation
        if (icon == null)
        {
            AddWarning("No icon assigned. Item will have no visual representation.");
        }
        
        // Description validation
        if (string.IsNullOrWhiteSpace(description) || description == "An item")
        {
            AddWarning("Consider adding a descriptive text");
        }
        
        // Stack size validation
        if (maxStackSize < 1)
        {
            AddError("Max stack size must be at least 1");
        }
        else if (maxStackSize > 999)
        {
            AddWarning("Stack size is very large (>999). Is this intentional?");
        }
        
        // Value validation
        if (baseValue < 0)
        {
            AddError("Base value cannot be negative");
        }
        else if (itemType == ItemType.Material && baseValue == 0)
        {
            AddWarning("Material has no gold value. Consider setting a sell price.");
        }
        
        // Quest reward validation
        if (canBeQuestReward && questRewardQuantity < 1)
        {
            AddError("Quest reward quantity must be at least 1");
        }
        
        // Equipment-specific validation
        if (itemType == ItemType.Equipment)
        {
            if (equipmentData == null)
            {
                AddError("Equipment type requires an EquipmentData reference");
            }
            
            if (maxStackSize != 1)
            {
                AddWarning("Equipment should have stack size of 1");
            }
        }
        else
        {
            if (equipmentData != null)
            {
                AddWarning("Non-equipment item has EquipmentData linked. This will be ignored.");
            }
        }
        
        // Type-specific suggestions
        switch (itemType)
        {
            case ItemType.Material:
                if (maxStackSize < 50)
                {
                    AddWarning("Materials typically stack to 50-99 for convenience");
                }
                break;
                
            case ItemType.Consumable:
                if (maxStackSize > 50)
                {
                    AddWarning("Consumables usually stack to 20-50");
                }
                break;
                
            case ItemType.Quest:
                if (canBeQuestReward)
                {
                    AddWarning("Quest items are typically not used as quest rewards");
                }
                break;
        }
    }
    
    #endregion
    
    #region Asset Creation
    
    protected override void CreateAsset()
    {
        // Create the ItemData asset
        ItemData item = CreateInstance<ItemData>();
        
        // Assign values
        item.itemName = itemName;
        item.description = description;
        item.icon = icon;
        item.itemType = itemType;
        item.maxStackSize = maxStackSize;
        item.baseValue = baseValue;
        item.canBeQuestReward = canBeQuestReward;
        item.questRewardQuantity = questRewardQuantity;
        
        // Equipment link
        if (itemType == ItemType.Equipment)
        {
            item.equipmentData = equipmentData;
        }
        
        // Save asset
        string fileName = $"Item_{itemName.Replace(" ", "")}";
        ItemData savedItem = SaveAsset(item, "Items", fileName);
        
        if (savedItem != null)
        {
            ShowSuccessNotification($"Item '{itemName}' created!");
        }
        else
        {
            ShowErrorNotification("Failed to create item");
        }
    }
    
    protected override void ClearForm()
    {
        itemName = "New Item";
        description = "An item";
        icon = null;
        itemType = ItemType.Material;
        maxStackSize = 99;
        baseValue = 1;
        canBeQuestReward = true;
        questRewardQuantity = 1;
        equipmentData = null;
        
        GUI.FocusControl(null);
    }
    
    #endregion
}
#endif

