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
    private int level = 1;
    private Sprite monsterSprite;
    private bool flipSprite = false;
    
    private float health = 50f;
    private float attackDamage = 5f;
    private float attackSpeed = 2f;
    private float attackRange = 100f;
    
    private int xpReward = 10;
    private int goldReward = 5;
    
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
        level = DrawIntField("Level", Mathf.Max(1, level), "Monster level (affects template calculations)");
        monsterSprite = DrawSpriteField("Sprite", monsterSprite, 80);
        flipSprite = DrawToggle("Flip Sprite", flipSprite, "Mirror sprite horizontally");
    }
    
    private void DrawCombatStatsSection()
    {
        DrawHeader("Combat Stats");
        
        EditorGUILayout.BeginHorizontal();
        health = DrawFloatField("Health", Mathf.Max(1f, health), "Monster's maximum health points");
        if (GUILayout.Button("Auto", GUILayout.Width(50)))
        {
            health = CalculateSuggestedHealth();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        attackDamage = DrawFloatField("Attack Damage", Mathf.Max(0f, attackDamage), "Damage dealt per attack");
        if (GUILayout.Button("Auto", GUILayout.Width(50)))
        {
            attackDamage = CalculateSuggestedDamage();
        }
        EditorGUILayout.EndHorizontal();
        
        attackSpeed = DrawFloatField("Attack Speed", Mathf.Max(0.1f, attackSpeed), "Time in seconds between attacks (higher = slower)");
        attackRange = DrawFloatField("Attack Range", Mathf.Max(10f, attackRange), "Attack range in pixels (melee: ~100, ranged: ~500)");
        
        // Show DPS calculation
        float dps = attackDamage / attackSpeed;
        EditorGUILayout.HelpBox($"DPS: {dps:F1} | Time to Kill (player 100 HP): {100f / dps:F1}s", MessageType.None);
    }
    
    private void DrawRewardsSection()
    {
        DrawHeader("Rewards");
        
        EditorGUILayout.BeginHorizontal();
        xpReward = DrawIntField("XP Reward", Mathf.Max(0, xpReward), "Experience points awarded on death");
        if (GUILayout.Button("Auto", GUILayout.Width(50)))
        {
            xpReward = CalculateSuggestedXP();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        goldReward = DrawIntField("Gold Reward", Mathf.Max(0, goldReward), "Gold awarded on death");
        if (GUILayout.Button("Auto", GUILayout.Width(50)))
        {
            goldReward = CalculateSuggestedGold();
        }
        EditorGUILayout.EndHorizontal();
        
        // Show reward ratios
        float xpPerHp = health > 0 ? xpReward / health : 0;
        float goldPerHp = health > 0 ? goldReward / health : 0;
        EditorGUILayout.HelpBox($"XP/HP Ratio: {xpPerHp:F2} | Gold/HP Ratio: {goldPerHp:F2}", MessageType.None);
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
        monster.level = level;
        monster.monsterSprite = monsterSprite;
        monster.flipSprite = flipSprite;
        
        monster.health = health;
        monster.attackDamage = attackDamage;
        monster.attackSpeed = attackSpeed;
        monster.attackRange = attackRange;
        
        monster.xpReward = xpReward;
        monster.goldReward = goldReward;
        
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

