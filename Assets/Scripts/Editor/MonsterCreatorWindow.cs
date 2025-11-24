#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Editor window for creating MonsterData assets with templates and validation.
/// Access via: Vinland > Create Content > Monster
/// </summary>
public class MonsterCreatorWindow : ContentCreatorBase
{
    // Template system
    private enum MonsterTemplate
    {
        Custom,
        BasicMelee,
        Ranged,
        Tank,
        FastAttacker,
        Elite,
        Boss
    }
    
    // Monster data fields
    private MonsterTemplate selectedTemplate = MonsterTemplate.Custom;
    private MonsterTemplate lastAppliedTemplate = MonsterTemplate.Custom;
    
    private string monsterName = "New Monster";
    private int level = 1; // Legacy field, kept for templates
    private Sprite monsterSprite;
    private bool flipSprite = false;
    
    // New level range system
    private int minLevel = 1;
    private int maxLevel = 1;
    
    // Legacy stats (kept for backward compatibility and templates)
    private float health = 50f;
    private float attackDamage = 5f;
    private float attackSpeed = 2f;
    private float attackRange = 100f;
    
    // New stat system
    private MonsterAttackRangeType attackRangeType = MonsterAttackRangeType.Melee;
    private float healthModifier = 1.0f;
    private float damageModifier = 1.0f;
    private float speedModifier = 1.0f;
    private float armorModifier = 1.0f;
    
    // Rewards
    private int xpReward = 10; // Legacy
    private int goldReward = 5; // Legacy
    private int baseXPReward = 10;
    private int baseGoldReward = 5;
    
    private List<MonsterDropEntry> dropTable = new List<MonsterDropEntry>();
    
    // UI state
    private Vector2 dropTableScrollPosition;
    
    [MenuItem("Vinland/Create Content/Monster")]
    public static void ShowWindow()
    {
        MonsterCreatorWindow window = GetWindow<MonsterCreatorWindow>("Monster Creator");
        window.minSize = new Vector2(600, 800);
        window.Show();
    }
    
    protected override void DrawContent()
    {
        DrawHeader("Monster Creator");
        
        GUILayout.Space(10);
        EditorGUILayout.HelpBox("Create monster data with balanced stats using templates or custom values.", MessageType.Info);
        GUILayout.Space(10);
        
        // Template selector
        DrawTemplateSection();
        
        // Basic info
        DrawBasicInfoSection();
        
        // Combat stats
        DrawCombatStatsSection();
        
        // Rewards
        DrawRewardsSection();
        
        // Drop table
        DrawDropTableSection();
    }
    
    #region Section Drawing
    
    private void DrawTemplateSection()
    {
        DrawHeader("Template");
        
        EditorGUI.BeginChangeCheck();
        selectedTemplate = DrawEnumPopup("Template Type", selectedTemplate, "Choose a preset template or Custom for manual values");
        
        if (EditorGUI.EndChangeCheck() && selectedTemplate != lastAppliedTemplate)
        {
            if (EditorUtility.DisplayDialog("Apply Template", 
                $"Apply {selectedTemplate} template? This will overwrite current values.", 
                "Apply", "Cancel"))
            {
                ApplyTemplate(selectedTemplate);
                lastAppliedTemplate = selectedTemplate;
            }
            else
            {
                selectedTemplate = lastAppliedTemplate;
            }
        }
        
        // Show template description
        if (selectedTemplate != MonsterTemplate.Custom)
        {
            EditorGUILayout.HelpBox(GetTemplateDescription(selectedTemplate), MessageType.None);
        }
    }
    
    private void DrawBasicInfoSection()
    {
        DrawHeader("Basic Info");
        
        monsterName = DrawTextField("Monster Name", monsterName, "Display name for the monster");
        monsterSprite = DrawSpriteField("Sprite", monsterSprite, 80);
        flipSprite = DrawToggle("Flip Sprite", flipSprite, "Mirror sprite horizontally");
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Level Range", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        minLevel = DrawIntField("Min Level", Mathf.Max(1, minLevel), "Minimum level this monster can spawn at");
        maxLevel = DrawIntField("Max Level", Mathf.Max(minLevel, maxLevel), "Maximum level this monster can spawn at");
        EditorGUILayout.EndHorizontal();
        
        if (minLevel > maxLevel)
        {
            EditorGUILayout.HelpBox("Min Level must be <= Max Level", MessageType.Warning);
        }
        
        // Show preview of stats at min/max level
        if (minLevel <= maxLevel)
        {
            CalculatedMonsterStats minStats = MonsterStatCalculator.CalculateMonsterStats(
                CreatePreviewMonsterData(), minLevel);
            CalculatedMonsterStats maxStats = MonsterStatCalculator.CalculateMonsterStats(
                CreatePreviewMonsterData(), maxLevel);
            
            EditorGUILayout.HelpBox(
                $"Level {minLevel}: HP={minStats.health:F0}, DMG={minStats.attackDamage:F1}, SPD={minStats.attackSpeed:F1}s\n" +
                $"Level {maxLevel}: HP={maxStats.health:F0}, DMG={maxStats.attackDamage:F1}, SPD={maxStats.attackSpeed:F1}s",
                MessageType.Info);
        }
    }
    
    private MonsterData CreatePreviewMonsterData()
    {
        MonsterData preview = CreateInstance<MonsterData>();
        preview.minLevel = minLevel;
        preview.maxLevel = maxLevel;
        preview.attackRangeType = attackRangeType;
        preview.healthModifier = healthModifier;
        preview.damageModifier = damageModifier;
        preview.speedModifier = speedModifier;
        preview.armorModifier = armorModifier;
        return preview;
    }
    
    private void DrawCombatStatsSection()
    {
        DrawHeader("Combat Stats");
        
        EditorGUILayout.HelpBox("Base stats come from GameBalance. Monsters use modifiers to adjust from base.", MessageType.Info);
        
        // Attack Range Type
        attackRangeType = (MonsterAttackRangeType)DrawEnumPopup("Attack Range Type", attackRangeType, 
            attackRangeType == MonsterAttackRangeType.Melee ? "Melee (120 range)" : "Ranged (600 range)");
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Stat Modifiers", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("1.0 = normal, 1.1 = +10%, 0.9 = -10%", MessageType.None);
        
        healthModifier = DrawSlider("Health Modifier", healthModifier, 0.1f, 2f, 
            $"Health modifier: {healthModifier:F2} ({((healthModifier - 1f) * 100):+F0}%)");
        
        damageModifier = DrawSlider("Damage Modifier", damageModifier, 0.1f, 2f, 
            $"Damage modifier: {damageModifier:F2} ({((damageModifier - 1f) * 100):+F0}%)");
        
        speedModifier = DrawSlider("Speed Modifier", speedModifier, 0.1f, 2f, 
            $"Speed modifier: {speedModifier:F2} ({((speedModifier - 1f) * 100):+F0}%) - Lower = Faster");
        
        armorModifier = DrawSlider("Armor Modifier", armorModifier, 0.1f, 2f, 
            $"Armor modifier: {armorModifier:F2} ({((armorModifier - 1f) * 100):+F0}%) - Armor doesn't scale with level");
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Legacy Stats (for templates)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("These fields are kept for template compatibility. New monsters use modifiers above.", MessageType.None);
        
        EditorGUILayout.BeginHorizontal();
        health = DrawFloatField("Health (Legacy)", Mathf.Max(1f, health), "Legacy field - not used in new system");
        if (GUILayout.Button("Auto", GUILayout.Width(50)))
        {
            health = CalculateSuggestedHealth();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        attackDamage = DrawFloatField("Attack Damage (Legacy)", Mathf.Max(0f, attackDamage), "Legacy field - not used in new system");
        if (GUILayout.Button("Auto", GUILayout.Width(50)))
        {
            attackDamage = CalculateSuggestedDamage();
        }
        EditorGUILayout.EndHorizontal();
        
        attackSpeed = DrawFloatField("Attack Speed (Legacy)", Mathf.Max(0.1f, attackSpeed), "Legacy field - not used in new system");
        attackRange = DrawFloatField("Attack Range (Legacy)", Mathf.Max(10f, attackRange), "Legacy field - not used in new system");
    }
    
    private void DrawRewardsSection()
    {
        DrawHeader("Rewards");
        
        EditorGUILayout.HelpBox("Base rewards at minimum level. Rewards scale with actual monster level.", MessageType.Info);
        
        EditorGUILayout.BeginHorizontal();
        baseXPReward = DrawIntField("Base XP Reward", Mathf.Max(0, baseXPReward), "XP reward at minimum level (scales with level)");
        if (GUILayout.Button("Auto", GUILayout.Width(50)))
        {
            baseXPReward = CalculateSuggestedXP();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        baseGoldReward = DrawIntField("Base Gold Reward", Mathf.Max(0, baseGoldReward), "Gold reward at minimum level (scales with level)");
        if (GUILayout.Button("Auto", GUILayout.Width(50)))
        {
            baseGoldReward = CalculateSuggestedGold();
        }
        EditorGUILayout.EndHorizontal();
        
        // Show scaled rewards at min/max level
        if (minLevel <= maxLevel)
        {
            int minXP, minGold, maxXP, maxGold;
            MonsterData preview = CreatePreviewMonsterData();
            MonsterStatCalculator.CalculateRewards(preview, minLevel, out minXP, out minGold);
            MonsterStatCalculator.CalculateRewards(preview, maxLevel, out maxXP, out maxGold);
            
            EditorGUILayout.HelpBox(
                $"Level {minLevel}: {minXP} XP, {minGold} Gold\n" +
                $"Level {maxLevel}: {maxXP} XP, {maxGold} Gold",
                MessageType.None);
        }
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Legacy Rewards (for templates)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        xpReward = DrawIntField("XP Reward (Legacy)", Mathf.Max(0, xpReward), "Legacy field - not used in new system");
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        goldReward = DrawIntField("Gold Reward (Legacy)", Mathf.Max(0, goldReward), "Legacy field - not used in new system");
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawDropTableSection()
    {
        DrawHeader("Drop Table");
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Drop Entries ({dropTable.Count})", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Add Drop", GUILayout.Width(100)))
        {
            dropTable.Add(new MonsterDropEntry());
        }
        
        if (dropTable.Count > 0 && GUILayout.Button("Clear All", GUILayout.Width(100)))
        {
            if (EditorUtility.DisplayDialog("Clear Drop Table", "Remove all drop entries?", "Yes", "No"))
            {
                dropTable.Clear();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        
        // Draw drop entries
        if (dropTable.Count > 0)
        {
            dropTableScrollPosition = EditorGUILayout.BeginScrollView(dropTableScrollPosition, GUILayout.MaxHeight(300));
            
            for (int i = dropTable.Count - 1; i >= 0; i--)
            {
                DrawDropEntry(i);
            }
            
            EditorGUILayout.EndScrollView();
            
            // Show total drop chance
            float totalChance = 0f;
            foreach (var drop in dropTable)
            {
                totalChance += drop.dropChance;
            }
            
            Color originalColor = GUI.color;
            if (totalChance > 1f)
            {
                GUI.color = Color.yellow;
            }
            EditorGUILayout.HelpBox($"Total Drop Chance: {totalChance * 100:F1}% (can exceed 100% for multiple drops)", MessageType.None);
            GUI.color = originalColor;
        }
        else
        {
            EditorGUILayout.HelpBox("No drops configured. Add drops to make this monster drop items.", MessageType.Info);
        }
    }
    
    private void DrawDropEntry(int index)
    {
        MonsterDropEntry entry = dropTable[index];
        
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Drop {index + 1}", EditorStyles.boldLabel, GUILayout.Width(60));
        
        if (GUILayout.Button("×", GUILayout.Width(30)))
        {
            dropTable.RemoveAt(index);
            return;
        }
        
        EditorGUILayout.EndHorizontal();
        
        entry.item = DrawObjectField("Item", entry.item, false, "Item that can drop");
        entry.quantity = DrawIntField("Quantity", Mathf.Max(1, entry.quantity), "Number of items to drop");
        entry.dropChance = DrawSlider("Drop Chance", entry.dropChance, 0f, 1f, $"Chance to drop ({entry.dropChance * 100:F1}%)");
        
        EditorGUILayout.EndVertical();
        GUILayout.Space(5);
    }
    
    #endregion
    
    #region Template System
    
    private void ApplyTemplate(MonsterTemplate template)
    {
        switch (template)
        {
            case MonsterTemplate.BasicMelee:
                health = 50f * level;
                attackDamage = 5f * level;
                attackSpeed = 2f;
                attackRange = 100f;
                xpReward = Mathf.RoundToInt(level * 10);
                goldReward = Mathf.RoundToInt(level * 5);
                break;
                
            case MonsterTemplate.Ranged:
                health = 30f * level;
                attackDamage = 8f * level;
                attackSpeed = 1.5f;
                attackRange = 500f;
                xpReward = Mathf.RoundToInt(level * 12);
                goldReward = Mathf.RoundToInt(level * 6);
                break;
                
            case MonsterTemplate.Tank:
                health = 100f * level;
                attackDamage = 3f * level;
                attackSpeed = 3f;
                attackRange = 100f;
                xpReward = Mathf.RoundToInt(level * 15);
                goldReward = Mathf.RoundToInt(level * 8);
                break;
                
            case MonsterTemplate.FastAttacker:
                health = 35f * level;
                attackDamage = 4f * level;
                attackSpeed = 1f;
                attackRange = 120f;
                xpReward = Mathf.RoundToInt(level * 11);
                goldReward = Mathf.RoundToInt(level * 5);
                break;
                
            case MonsterTemplate.Elite:
                health = 150f * level;
                attackDamage = 10f * level;
                attackSpeed = 1.8f;
                attackRange = 150f;
                xpReward = Mathf.RoundToInt(level * 25);
                goldReward = Mathf.RoundToInt(level * 15);
                break;
                
            case MonsterTemplate.Boss:
                health = 500f * level;
                attackDamage = 15f * level;
                attackSpeed = 2.5f;
                attackRange = 200f;
                xpReward = Mathf.RoundToInt(level * 100);
                goldReward = Mathf.RoundToInt(level * 50);
                break;
        }
        
        Debug.Log($"[MonsterCreator] Applied {template} template for level {level}");
    }
    
    private string GetTemplateDescription(MonsterTemplate template)
    {
        switch (template)
        {
            case MonsterTemplate.BasicMelee:
                return "Balanced melee fighter. Good starting template.";
            case MonsterTemplate.Ranged:
                return "Lower HP but hits from range. Higher damage to compensate.";
            case MonsterTemplate.Tank:
                return "High HP, low damage. Takes time to kill.";
            case MonsterTemplate.FastAttacker:
                return "Fast attacks, moderate damage. Keep player engaged.";
            case MonsterTemplate.Elite:
                return "Stronger than normal monsters. 2x rewards.";
            case MonsterTemplate.Boss:
                return "Very high HP and damage. 5-10x rewards. Zone bosses.";
            default:
                return "";
        }
    }
    
    #endregion
    
    #region Auto-Calculate Suggestions
    
    private float CalculateSuggestedHealth()
    {
        switch (selectedTemplate)
        {
            case MonsterTemplate.BasicMelee: return 50f * level;
            case MonsterTemplate.Ranged: return 30f * level;
            case MonsterTemplate.Tank: return 100f * level;
            case MonsterTemplate.FastAttacker: return 35f * level;
            case MonsterTemplate.Elite: return 150f * level;
            case MonsterTemplate.Boss: return 500f * level;
            default: return 50f * level; // Default to basic melee
        }
    }
    
    private float CalculateSuggestedDamage()
    {
        switch (selectedTemplate)
        {
            case MonsterTemplate.BasicMelee: return 5f * level;
            case MonsterTemplate.Ranged: return 8f * level;
            case MonsterTemplate.Tank: return 3f * level;
            case MonsterTemplate.FastAttacker: return 4f * level;
            case MonsterTemplate.Elite: return 10f * level;
            case MonsterTemplate.Boss: return 15f * level;
            default: return 5f * level;
        }
    }
    
    private int CalculateSuggestedXP()
    {
        // Base XP on health and level
        float baseXP = (health / 5f) + (level * 10);
        
        // Boss/Elite multipliers
        if (selectedTemplate == MonsterTemplate.Boss) baseXP *= 5f;
        else if (selectedTemplate == MonsterTemplate.Elite) baseXP *= 2f;
        
        return Mathf.RoundToInt(baseXP);
    }
    
    private int CalculateSuggestedGold()
    {
        // Gold is typically half of XP
        return Mathf.RoundToInt(CalculateSuggestedXP() * 0.5f);
    }
    
    #endregion
    
    #region Validation
    
    protected override void ValidateData()
    {
        ClearValidation();
        
        // Name validation
        if (string.IsNullOrWhiteSpace(monsterName))
        {
            AddError("Monster name is required");
        }
        else if (monsterName == "New Monster")
        {
            AddWarning("Consider giving the monster a unique name");
        }
        
        // Sprite validation
        if (monsterSprite == null)
        {
            AddError("Monster sprite is required");
        }
        
        // Level validation
        if (level < 1)
        {
            AddError("Level must be at least 1");
        }
        else if (level > 100)
        {
            AddWarning("Level is very high (>100). Is this intentional?");
        }
        
        // Combat stats validation
        if (health <= 0)
        {
            AddError("Health must be greater than 0");
        }
        
        if (attackDamage < 0)
        {
            AddError("Attack damage cannot be negative");
        }
        
        if (attackSpeed <= 0)
        {
            AddError("Attack speed must be greater than 0");
        }
        else if (attackSpeed > 10f)
        {
            AddWarning("Attack speed is very slow (>10s). Monster may feel unresponsive.");
        }
        
        if (attackRange < 10f)
        {
            AddWarning("Attack range is very small (<10). Monster may have trouble reaching player.");
        }
        
        // Rewards validation
        if (xpReward <= 0 && goldReward <= 0)
        {
            AddWarning("Monster gives no XP or Gold. Players won't be rewarded for killing it.");
        }
        
        // Balance validation
        float xpPerHp = health > 0 ? xpReward / health : 0;
        if (xpPerHp < 0.1f && xpReward > 0)
        {
            AddWarning($"XP/HP ratio is very low ({xpPerHp:F2}). Monster may not feel rewarding.");
        }
        else if (xpPerHp > 5f)
        {
            AddWarning($"XP/HP ratio is very high ({xpPerHp:F2}). May be too rewarding.");
        }
        
        // DPS validation
        float dps = attackDamage / attackSpeed;
        if (dps > 50f)
        {
            AddWarning($"DPS is very high ({dps:F1}). May kill player too quickly.");
        }
        else if (dps < 1f && attackDamage > 0)
        {
            AddWarning($"DPS is very low ({dps:F1}). Combat may feel too slow.");
        }
        
        // Drop table validation
        foreach (var drop in dropTable)
        {
            if (drop.item == null)
            {
                AddWarning("Drop table has entry with no item assigned");
                break;
            }
        }
    }
    
    #endregion
    
    #region Asset Creation
    
    protected override void CreateAsset()
    {
        // Create the MonsterData asset
        MonsterData monster = CreateInstance<MonsterData>();
        
        // Assign values
        monster.monsterName = monsterName;
        monster.monsterSprite = monsterSprite;
        monster.flipSprite = flipSprite;
        
        // New level range system
        monster.minLevel = minLevel;
        monster.maxLevel = maxLevel;
        
        // New stat modifiers
        monster.attackRangeType = attackRangeType;
        monster.healthModifier = healthModifier;
        monster.damageModifier = damageModifier;
        monster.speedModifier = speedModifier;
        monster.armorModifier = armorModifier;
        
        // New reward system
        monster.baseXPReward = baseXPReward;
        monster.baseGoldReward = baseGoldReward;
        
        // Legacy fields removed - all monsters now use the new system
        
        monster.dropTable = new List<MonsterDropEntry>(dropTable);
        
        // Save asset
        string fileName = $"Monster_{monsterName.Replace(" ", "")}";
        MonsterData savedMonster = SaveAsset(monster, "Monsters", fileName);
        
        if (savedMonster != null)
        {
            ShowSuccessNotification($"Monster '{monsterName}' created!");
        }
        else
        {
            ShowErrorNotification("Failed to create monster");
        }
    }
    
    protected override void ClearForm()
    {
        monsterName = "New Monster";
        level = 1;
        monsterSprite = null;
        flipSprite = false;
        
        minLevel = 1;
        maxLevel = 1;
        
        attackRangeType = MonsterAttackRangeType.Melee;
        healthModifier = 1.0f;
        damageModifier = 1.0f;
        speedModifier = 1.0f;
        armorModifier = 1.0f;
        
        baseXPReward = 10;
        baseGoldReward = 5;
        
        health = 50f;
        attackDamage = 5f;
        attackSpeed = 2f;
        attackRange = 100f;
        
        xpReward = 10;
        goldReward = 5;
        
        dropTable.Clear();
        
        selectedTemplate = MonsterTemplate.Custom;
        lastAppliedTemplate = MonsterTemplate.Custom;
        
        GUI.FocusControl(null); // Clear focus
    }
    
    #endregion
}
#endif

