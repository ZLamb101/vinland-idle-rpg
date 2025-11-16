#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window for creating EquipmentData assets with stat budget system.
/// Access via: Vinland > Create Content > Equipment
/// </summary>
public class EquipmentCreatorWindow : ContentCreatorBase
{
    // Equipment data fields
    private string equipmentName = "New Equipment";
    private string description = "A piece of equipment";
    private Sprite icon;
    private EquipmentSlot slot = EquipmentSlot.MainHand;
    private EquipmentTier tier = EquipmentTier.Common;
    private int levelRequired = 1;
    
    // Combat stats
    private float attackDamage = 0f;
    private float attackSpeed = 0f;
    private float maxHealth = 0f;
    private float healthRegen = 0f;
    
    // Defensive stats
    private float armor = 0f;
    private float dodge = 0f;
    
    // Special stats
    private float criticalChance = 0f;
    private float lifesteal = 0f;
    private float xpBonus = 0f;
    private float goldBonus = 0f;
    
    // UI state
    private bool showStatBudget = true;
    private bool createLinkedItem = true;
    
    // Stat budget system
    private static readonly int[] TIER_BUDGETS = { 10, 20, 35, 60, 100 }; // Common to Legendary
    private static readonly Color[] TIER_COLORS = {
        new Color(0.7f, 0.7f, 0.7f), // Common - Gray
        new Color(0.3f, 1f, 0.3f),   // Uncommon - Green
        new Color(0.3f, 0.5f, 1f),   // Rare - Blue
        new Color(0.7f, 0.3f, 1f),   // Epic - Purple
        new Color(1f, 0.6f, 0f)      // Legendary - Orange
    };
    
    [MenuItem("Vinland/Create Content/Equipment")]
    public static void ShowWindow()
    {
        EquipmentCreatorWindow window = GetWindow<EquipmentCreatorWindow>("Equipment Creator");
        window.minSize = new Vector2(600, 900);
        window.Show();
    }
    
    protected override void DrawContent()
    {
        DrawHeader("Equipment Creator");
        
        GUILayout.Space(10);
        EditorGUILayout.HelpBox("Create equipment with balanced stats using the stat budget system.", MessageType.Info);
        GUILayout.Space(10);
        
        // Rarity section
        DrawRaritySection();
        
        // Basic info
        DrawBasicInfoSection();
        
        // Stat budget display
        if (showStatBudget)
        {
            DrawStatBudgetSection();
        }
        
        // Combat stats
        DrawCombatStatsSection();
        
        // Defensive stats
        DrawDefensiveStatsSection();
        
        // Special stats
        DrawSpecialStatsSection();
        
        // Preview
        DrawPreviewSection();
        
        // Options
        DrawOptionsSection();
    }
    
    #region Section Drawing
    
    private void DrawRaritySection()
    {
        DrawHeader("Rarity");
        
        EditorGUI.BeginChangeCheck();
        tier = DrawEnumPopup("Tier", tier, "Equipment rarity/quality tier");
        
        if (EditorGUI.EndChangeCheck())
        {
            // Auto-update rarity color when tier changes
            int tierIndex = (int)tier;
            if (tierIndex >= 0 && tierIndex < TIER_COLORS.Length)
            {
                // Color will be applied when creating the asset
            }
        }
        
        // Show tier info
        int budget = GetStatBudget();
        Color tierColor = GetTierColor();
        
        GUILayout.BeginHorizontal();
        GUILayout.Space(LABEL_WIDTH);
        
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = tierColor;
        GUILayout.Box($"{tier} - {budget} Stat Points", GUILayout.Height(30));
        GUI.backgroundColor = originalColor;
        
        GUILayout.EndHorizontal();
        GUILayout.Space(FIELD_SPACING);
    }
    
    private void DrawBasicInfoSection()
    {
        DrawHeader("Basic Info");
        
        equipmentName = DrawTextField("Name", equipmentName, "Equipment display name");
        description = DrawTextArea("Description", description, "Equipment description", 3);
        icon = DrawSpriteField("Icon", icon, 64);
        slot = DrawEnumPopup("Equipment Slot", slot, "Where this equipment is equipped");
        levelRequired = DrawIntField("Level Required", Mathf.Max(1, levelRequired), "Minimum level to equip");
    }
    
    private void DrawStatBudgetSection()
    {
        DrawHeader("Stat Budget");
        
        int budget = GetStatBudget();
        int used = CalculateUsedBudget();
        int remaining = budget - used;
        
        // Budget bar
        Rect budgetRect = EditorGUILayout.BeginHorizontal();
        
        float fillPercent = budget > 0 ? (float)used / budget : 0f;
        Color barColor = remaining >= 0 ? new Color(0.3f, 0.7f, 1f) : Color.red;
        
        EditorGUI.DrawRect(new Rect(budgetRect.x, budgetRect.y, budgetRect.width * fillPercent, 25), barColor);
        
        GUILayout.Space(10);
        string budgetText = remaining >= 0 
            ? $"Budget: {used}/{budget} ({remaining} remaining)" 
            : $"Over Budget: {used}/{budget} ({-remaining} over!)";
        GUILayout.Label(budgetText, EditorStyles.boldLabel);
        
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(FIELD_SPACING);
        
        // Show cost breakdown
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Stat Costs:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Attack Damage: 1pt per 5 damage");
        EditorGUILayout.LabelField("Attack Speed: 5pts per 0.1s reduction");
        EditorGUILayout.LabelField("Max Health: 1pt per 10 HP");
        EditorGUILayout.LabelField("Health Regen: 2pts per 1 HP/sec");
        EditorGUILayout.LabelField("Armor: 2pts per 1%");
        EditorGUILayout.LabelField("Dodge: 3pts per 1%");
        EditorGUILayout.LabelField("Crit Chance: 4pts per 1%");
        EditorGUILayout.LabelField("Lifesteal: 3pts per 1%");
        EditorGUILayout.LabelField("XP Bonus: 2pts per 1%");
        EditorGUILayout.LabelField("Gold Bonus: 2pts per 1%");
        EditorGUILayout.EndVertical();
    }
    
    private void DrawCombatStatsSection()
    {
        DrawHeader("Combat Stats");
        
        attackDamage = DrawStatSlider("Attack Damage", attackDamage, 0f, 100f, 1, 5f, "Increases attack damage");
        attackSpeed = DrawStatSlider("Attack Speed", attackSpeed, -2f, 0f, 5, 0.1f, "Reduces time between attacks (negative = faster)");
        maxHealth = DrawStatSlider("Max Health", maxHealth, 0f, 500f, 1, 10f, "Increases maximum health");
        healthRegen = DrawStatSlider("Health Regen/sec", healthRegen, 0f, 10f, 2, 1f, "Health regeneration per second");
    }
    
    private void DrawDefensiveStatsSection()
    {
        DrawHeader("Defensive Stats");
        
        armor = DrawStatSlider("Armor", armor, 0f, 0.5f, 2, 0.01f, "Damage reduction percentage", true);
        dodge = DrawStatSlider("Dodge", dodge, 0f, 0.3f, 3, 0.01f, "Chance to dodge attacks", true);
    }
    
    private void DrawSpecialStatsSection()
    {
        DrawHeader("Special Stats");
        
        criticalChance = DrawStatSlider("Critical Chance", criticalChance, 0f, 0.5f, 4, 0.01f, "Chance for critical hits", true);
        lifesteal = DrawStatSlider("Lifesteal", lifesteal, 0f, 0.3f, 3, 0.01f, "Heal % of damage dealt", true);
        xpBonus = DrawStatSlider("XP Bonus", xpBonus, 0f, 1f, 2, 0.01f, "Extra XP gain", true);
        goldBonus = DrawStatSlider("Gold Bonus", goldBonus, 0f, 1f, 2, 0.01f, "Extra gold gain", true);
    }
    
    private void DrawPreviewSection()
    {
        DrawHeader("Preview");
        
        EditorGUILayout.BeginVertical("box");
        
        // Name with tier color
        Color tierColor = GetTierColor();
        Color originalColor = GUI.color;
        GUI.color = tierColor;
        EditorGUILayout.LabelField(equipmentName, EditorStyles.boldLabel);
        GUI.color = originalColor;
        
        EditorGUILayout.LabelField($"{slot} - {tier}");
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField(description, EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(5);
        
        if (levelRequired > 1)
        {
            EditorGUILayout.LabelField($"Requires Level {levelRequired}");
        }
        
        EditorGUILayout.Space(5);
        
        // Show stats
        if (attackDamage > 0) EditorGUILayout.LabelField($"+{attackDamage:F0} Attack Damage");
        if (attackSpeed < 0) EditorGUILayout.LabelField($"{attackSpeed:F2}s Attack Speed");
        if (maxHealth > 0) EditorGUILayout.LabelField($"+{maxHealth:F0} Max Health");
        if (healthRegen > 0) EditorGUILayout.LabelField($"+{healthRegen:F1} Health/sec");
        if (armor > 0) EditorGUILayout.LabelField($"+{armor * 100:F0}% Damage Reduction");
        if (dodge > 0) EditorGUILayout.LabelField($"+{dodge * 100:F0}% Dodge Chance");
        if (criticalChance > 0) EditorGUILayout.LabelField($"+{criticalChance * 100:F0}% Critical Chance");
        if (lifesteal > 0) EditorGUILayout.LabelField($"+{lifesteal * 100:F0}% Lifesteal");
        if (xpBonus > 0) EditorGUILayout.LabelField($"+{xpBonus * 100:F0}% XP Gain");
        if (goldBonus > 0) EditorGUILayout.LabelField($"+{goldBonus * 100:F0}% Gold Gain");
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawOptionsSection()
    {
        DrawHeader("Options");
        
        showStatBudget = DrawToggle("Show Stat Budget", showStatBudget, "Display stat budget helper");
        createLinkedItem = DrawToggle("Create Linked Item", createLinkedItem, "Also create ItemData that references this equipment");
    }
    
    private float DrawStatSlider(string label, float value, float min, float max, int costPerUnit, float unitSize, string tooltip, bool asPercent = false)
    {
        EditorGUILayout.BeginHorizontal();
        
        // Label
        EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(LABEL_WIDTH - 40));
        
        // Cost display
        int cost = CalculateStatCost(value, costPerUnit, unitSize);
        EditorGUILayout.LabelField($"({cost}pt)", GUILayout.Width(40));
        
        // Slider
        float newValue = EditorGUILayout.Slider(value, min, max);
        
        // Value display
        if (asPercent)
        {
            EditorGUILayout.LabelField($"{newValue * 100:F0}%", GUILayout.Width(50));
        }
        else
        {
            EditorGUILayout.LabelField($"{newValue:F1}", GUILayout.Width(50));
        }
        
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(FIELD_SPACING);
        
        return newValue;
    }
    
    #endregion
    
    #region Stat Budget Calculations
    
    private int GetStatBudget()
    {
        int tierIndex = (int)tier;
        if (tierIndex >= 0 && tierIndex < TIER_BUDGETS.Length)
        {
            return TIER_BUDGETS[tierIndex];
        }
        return TIER_BUDGETS[0];
    }
    
    private Color GetTierColor()
    {
        int tierIndex = (int)tier;
        if (tierIndex >= 0 && tierIndex < TIER_COLORS.Length)
        {
            return TIER_COLORS[tierIndex];
        }
        return TIER_COLORS[0];
    }
    
    private int CalculateUsedBudget()
    {
        int total = 0;
        
        // Combat stats
        total += CalculateStatCost(attackDamage, 1, 5f);      // 1pt per 5 damage
        total += CalculateStatCost(Mathf.Abs(attackSpeed), 5, 0.1f); // 5pts per 0.1s (use absolute value)
        total += CalculateStatCost(maxHealth, 1, 10f);        // 1pt per 10 HP
        total += CalculateStatCost(healthRegen, 2, 1f);       // 2pts per 1 HP/sec
        
        // Defensive stats
        total += CalculateStatCost(armor, 2, 0.01f);          // 2pts per 1%
        total += CalculateStatCost(dodge, 3, 0.01f);          // 3pts per 1%
        
        // Special stats
        total += CalculateStatCost(criticalChance, 4, 0.01f); // 4pts per 1%
        total += CalculateStatCost(lifesteal, 3, 0.01f);      // 3pts per 1%
        total += CalculateStatCost(xpBonus, 2, 0.01f);        // 2pts per 1%
        total += CalculateStatCost(goldBonus, 2, 0.01f);      // 2pts per 1%
        
        return total;
    }
    
    private int CalculateStatCost(float value, int costPerUnit, float unitSize)
    {
        if (value <= 0) return 0;
        int units = Mathf.CeilToInt(value / unitSize);
        return units * costPerUnit;
    }
    
    #endregion
    
    #region Validation
    
    protected override void ValidateData()
    {
        ClearValidation();
        
        // Name validation
        if (string.IsNullOrWhiteSpace(equipmentName))
        {
            AddError("Equipment name is required");
        }
        else if (equipmentName == "New Equipment")
        {
            AddWarning("Consider giving the equipment a unique name");
        }
        
        // Icon validation
        if (icon == null)
        {
            AddWarning("No icon assigned. Equipment will have no visual representation.");
        }
        
        // Description validation
        if (string.IsNullOrWhiteSpace(description) || description == "A piece of equipment")
        {
            AddWarning("Consider adding a descriptive text");
        }
        
        // Level validation
        if (levelRequired < 1)
        {
            AddError("Level required must be at least 1");
        }
        
        // Budget validation
        int budget = GetStatBudget();
        int used = CalculateUsedBudget();
        int remaining = budget - used;
        
        if (remaining < 0)
        {
            AddWarning($"Over stat budget by {-remaining} points. Equipment may be too powerful for its tier.");
        }
        else if (used == 0)
        {
            AddWarning("Equipment has no stats. Consider adding some bonuses.");
        }
        else if (remaining > budget * 0.5f)
        {
            AddWarning($"{remaining} stat points unused. Equipment could be stronger.");
        }
        
        // Slot-specific validation
        if (slot == EquipmentSlot.MainHand || slot == EquipmentSlot.OffHand)
        {
            if (attackDamage == 0 && attackSpeed == 0)
            {
                AddWarning("Weapon has no attack damage or speed. Consider adding offensive stats.");
            }
        }
        else
        {
            if (attackDamage > 0 || attackSpeed != 0)
            {
                AddWarning($"{slot} has weapon stats. This is unusual (but not wrong).");
            }
        }
        
        // Defensive armor pieces should have defensive stats
        if (slot == EquipmentSlot.Chest || slot == EquipmentSlot.Head || slot == EquipmentSlot.Legs)
        {
            if (armor == 0 && maxHealth == 0)
            {
                AddWarning("Armor piece has no defensive stats. Consider adding armor or health.");
            }
        }
    }
    
    #endregion
    
    #region Asset Creation
    
    protected override void CreateAsset()
    {
        // Create the EquipmentData asset
        EquipmentData equipment = CreateInstance<EquipmentData>();
        
        // Assign values
        equipment.equipmentName = equipmentName;
        equipment.description = description;
        equipment.icon = icon;
        equipment.slot = slot;
        equipment.tier = tier;
        equipment.levelRequired = levelRequired;
        
        // Combat stats
        equipment.attackDamage = attackDamage;
        equipment.attackSpeed = attackSpeed;
        equipment.maxHealth = maxHealth;
        equipment.healthRegen = healthRegen;
        
        // Defensive stats
        equipment.armor = armor;
        equipment.dodge = dodge;
        
        // Special stats
        equipment.criticalChance = criticalChance;
        equipment.lifesteal = lifesteal;
        equipment.xpBonus = xpBonus;
        equipment.goldBonus = goldBonus;
        
        // Rarity color
        equipment.rarityColor = GetTierColor();
        
        // Save asset
        string fileName = $"Equipment_{equipmentName.Replace(" ", "")}";
        EquipmentData savedEquipment = SaveAsset(equipment, "Equipment", fileName);
        
        if (savedEquipment != null)
        {
            ShowSuccessNotification($"Equipment '{equipmentName}' created!");
            
            // Create linked item if requested
            if (createLinkedItem)
            {
                CreateLinkedItemData(savedEquipment);
            }
        }
        else
        {
            ShowErrorNotification("Failed to create equipment");
        }
    }
    
    private void CreateLinkedItemData(EquipmentData equipment)
    {
        ItemData item = CreateInstance<ItemData>();
        
        item.itemName = equipment.equipmentName;
        item.description = equipment.description;
        item.icon = equipment.icon;
        item.itemType = ItemType.Equipment;
        item.maxStackSize = 1;
        item.baseValue = CalculateItemValue(equipment);
        item.equipmentData = equipment;
        item.canBeQuestReward = true;
        item.questRewardQuantity = 1;
        
        string itemFileName = $"Item_{equipment.equipmentName.Replace(" ", "")}";
        ItemData savedItem = SaveAsset(item, "Items", itemFileName);
        
        if (savedItem != null)
        {
            Debug.Log($"[EquipmentCreator] Created linked ItemData: {itemFileName}");
        }
    }
    
    private int CalculateItemValue(EquipmentData equipment)
    {
        // Base value on tier
        int baseValue = 10;
        switch (equipment.tier)
        {
            case EquipmentTier.Common: baseValue = 10; break;
            case EquipmentTier.Uncommon: baseValue = 50; break;
            case EquipmentTier.Rare: baseValue = 200; break;
            case EquipmentTier.Epic: baseValue = 1000; break;
            case EquipmentTier.Legendary: baseValue = 5000; break;
        }
        
        // Multiply by level requirement
        return baseValue * Mathf.Max(1, equipment.levelRequired);
    }
    
    protected override void ClearForm()
    {
        equipmentName = "New Equipment";
        description = "A piece of equipment";
        icon = null;
        slot = EquipmentSlot.MainHand;
        tier = EquipmentTier.Common;
        levelRequired = 1;
        
        attackDamage = 0f;
        attackSpeed = 0f;
        maxHealth = 0f;
        healthRegen = 0f;
        
        armor = 0f;
        dodge = 0f;
        
        criticalChance = 0f;
        lifesteal = 0f;
        xpBonus = 0f;
        goldBonus = 0f;
        
        GUI.FocusControl(null);
    }
    
    #endregion
}
#endif

