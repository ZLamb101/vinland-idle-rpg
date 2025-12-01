using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Editor tool to import equipment data from CSV and create ScriptableObjects.
/// Creates both Equipment_ and Item_ assets with proper references.
/// Access via: Tools → Vinland → Import Equipment CSV
/// </summary>
public class EquipmentCSVImporter : EditorWindow
{
    private string csvPath = "Assets/Documentation/equipment_data.csv";
    private string equipmentFolder = "Assets/Resources/Equipment";
    private string itemsFolder = "Assets/Resources/Items/EquippableItems";
    private bool createItems = true;
    
    [MenuItem("Tools/Vinland/Import Equipment CSV")]
    public static void ShowWindow()
    {
        GetWindow<EquipmentCSVImporter>("Equipment Importer");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Equipment CSV Importer", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        csvPath = EditorGUILayout.TextField("CSV Path:", csvPath);
        equipmentFolder = EditorGUILayout.TextField("Equipment Folder:", equipmentFolder);
        itemsFolder = EditorGUILayout.TextField("Items Folder:", itemsFolder);
        
        EditorGUILayout.Space();
        
        createItems = EditorGUILayout.Toggle("Also Create Item_ Assets", createItems);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Import Equipment from CSV", GUILayout.Height(40)))
        {
            ImportEquipment();
        }
        
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "This will create ScriptableObjects for each equipment item in the CSV.\n\n" +
            "Creates:\n" +
            "• Equipment_ assets (stats) in Equipment folder\n" +
            "• Item_ assets (inventory items) in Items folder\n\n" +
            "Existing items with the same name will be overwritten.",
            MessageType.Info);
    }
    
    void ImportEquipment()
    {
        if (!File.Exists(csvPath))
        {
            EditorUtility.DisplayDialog("Error", $"CSV file not found at: {csvPath}", "OK");
            return;
        }
        
        // Ensure output folders exist
        EnsureFolderExists(equipmentFolder);
        if (createItems)
        {
            EnsureFolderExists(itemsFolder);
        }
        
        string[] lines = File.ReadAllLines(csvPath);
        int equipmentCreated = 0;
        int itemsCreated = 0;
        int skipped = 0;
        
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            
            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;
            
            // Skip header
            if (line.StartsWith("name,"))
                continue;
            
            string[] values = ParseCSVLine(line);
            
            if (values.Length < 37)
            {
                Debug.LogWarning($"Line {i + 1}: Not enough columns ({values.Length}/37), skipping");
                skipped++;
                continue;
            }
            
            try
            {
                // Create Equipment asset
                EquipmentData equipment = CreateEquipmentFromCSV(values);
                string assetName = SanitizeFileName(equipment.equipmentName);
                string equipmentPath = $"{equipmentFolder}/Equipment_{assetName}.asset";
                
                // Save or update Equipment asset
                EquipmentData savedEquipment = SaveOrUpdateAsset(equipment, equipmentPath);
                if (savedEquipment != null)
                {
                    equipmentCreated++;
                    Debug.Log($"Equipment: {equipment.equipmentName}");
                    
                    // Create corresponding Item asset
                    if (createItems)
                    {
                        ItemData item = CreateItemFromEquipment(savedEquipment);
                        string itemPath = $"{itemsFolder}/Item_{assetName}.asset";
                        
                        if (SaveOrUpdateItemAsset(item, itemPath, savedEquipment))
                        {
                            itemsCreated++;
                            Debug.Log($"Item: {item.itemName}");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Line {i + 1}: Error parsing - {e.Message}\n{e.StackTrace}");
                skipped++;
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        string message = $"Equipment Created/Updated: {equipmentCreated}";
        if (createItems)
        {
            message += $"\nItems Created/Updated: {itemsCreated}";
        }
        message += $"\nSkipped: {skipped} lines";
        
        EditorUtility.DisplayDialog("Import Complete", message, "OK");
        
        Debug.Log($"[EquipmentCSVImporter] Import complete. Equipment: {equipmentCreated}, Items: {itemsCreated}, Skipped: {skipped}");
    }
    
    void EnsureFolderExists(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string[] folders = folderPath.Split('/');
            string currentPath = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
        }
    }
    
    EquipmentData SaveOrUpdateAsset(EquipmentData equipment, string assetPath)
    {
        EquipmentData existing = AssetDatabase.LoadAssetAtPath<EquipmentData>(assetPath);
        if (existing != null)
        {
            // Update existing asset
            EditorUtility.CopySerialized(equipment, existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }
        else
        {
            // Create new asset
            AssetDatabase.CreateAsset(equipment, assetPath);
            return equipment;
        }
    }
    
    bool SaveOrUpdateItemAsset(ItemData item, string assetPath, EquipmentData equipmentRef)
    {
        ItemData existing = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
        if (existing != null)
        {
            // Update existing asset but preserve the equipment reference
            existing.itemName = item.itemName;
            existing.description = item.description;
            existing.itemType = item.itemType;
            existing.maxStackSize = item.maxStackSize;
            existing.baseValue = item.baseValue;
            existing.equipmentData = equipmentRef;
            existing.canBeQuestReward = item.canBeQuestReward;
            existing.questRewardQuantity = item.questRewardQuantity;
            // Preserve icon if it was manually assigned
            if (existing.icon == null)
            {
                existing.icon = item.icon;
            }
            EditorUtility.SetDirty(existing);
            return true;
        }
        else
        {
            // Create new asset with equipment reference
            item.equipmentData = equipmentRef;
            AssetDatabase.CreateAsset(item, assetPath);
            return true;
        }
    }
    
    ItemData CreateItemFromEquipment(EquipmentData equipment)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        
        item.itemName = equipment.equipmentName;
        item.description = equipment.GetFullDescription();
        item.icon = equipment.icon; // Will be null initially, can assign later
        item.itemType = ItemType.Equipment;
        item.maxStackSize = 1; // Equipment doesn't stack
        item.baseValue = CalculateBaseValue(equipment.levelRequired, equipment.tier);
        item.equipmentData = equipment;
        item.canBeQuestReward = true;
        item.questRewardQuantity = 1;
        
        return item;
    }
    
    /// <summary>
    /// Calculate gold value based on level and tier
    /// Formula: (levelRequired * 10) * tierMultiplier
    /// </summary>
    int CalculateBaseValue(int level, EquipmentTier tier)
    {
        int baseValue = level * 10;
        
        float tierMultiplier = tier switch
        {
            EquipmentTier.Common => 1f,
            EquipmentTier.Uncommon => 2f,
            EquipmentTier.Rare => 5f,
            EquipmentTier.Epic => 15f,
            EquipmentTier.Legendary => 50f,
            _ => 1f
        };
        
        return Mathf.RoundToInt(baseValue * tierMultiplier);
    }
    
    string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string current = "";
        
        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }
        result.Add(current);
        
        return result.ToArray();
    }
    
    string SanitizeFileName(string name)
    {
        return name.Replace(" ", "").Replace("'", "").Replace("\"", "");
    }
    
    EquipmentSlot ParseSlot(string slot)
    {
        return slot switch
        {
            "Chest" => EquipmentSlot.Chest,
            "Legs" => EquipmentSlot.Legs,
            "Hands" => EquipmentSlot.Hands,
            "Feet" => EquipmentSlot.Feet,
            "MainHand" => EquipmentSlot.MainHand,
            "OffHand" => EquipmentSlot.OffHand,
            "Head" => EquipmentSlot.Head,
            "Neck" => EquipmentSlot.Neck,
            "Shoulders" => EquipmentSlot.Shoulders,
            "Ring1" => EquipmentSlot.Ring1,
            "Ring2" => EquipmentSlot.Ring2,
            _ => EquipmentSlot.Chest
        };
    }
    
    EquipmentTier ParseTier(string tier)
    {
        return tier switch
        {
            "Common" => EquipmentTier.Common,
            "Uncommon" => EquipmentTier.Uncommon,
            "Rare" => EquipmentTier.Rare,
            "Epic" => EquipmentTier.Epic,
            "Legendary" => EquipmentTier.Legendary,
            _ => EquipmentTier.Common
        };
    }
    
    float ParseFloat(string value)
    {
        if (string.IsNullOrEmpty(value)) return 0f;
        float.TryParse(value, System.Globalization.NumberStyles.Float, 
            System.Globalization.CultureInfo.InvariantCulture, out float result);
        return result;
    }
    
    int ParseInt(string value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        int.TryParse(value, out int result);
        return result;
    }
    
    EquipmentData CreateEquipmentFromCSV(string[] v)
    {
        EquipmentData eq = ScriptableObject.CreateInstance<EquipmentData>();
        
        // Column indices based on CSV header:
        // name,slot,tier,levelRequired,attackDamage,attackDamagePercent,attackSpeed,maxHealth,healthRegen,
        // armor,dodge,criticalChance,lifesteal,hitRating,spellDamage,spellDamagePercent,spellHealing,
        // spellHealingPercent,fireDamage,fireDamagePercent,frostDamage,frostDamagePercent,natureDamage,
        // natureDamagePercent,shadowDamage,shadowDamagePercent,holyDamage,holyDamagePercent,bleedDamage,
        // bleedDamagePercent,poisonDamage,poisonDamagePercent,xpBonus,goldBonus,afkGainsPercent,
        // professionXPPercent,professionPowerPercent,description
        
        eq.equipmentName = v[0];
        eq.slot = ParseSlot(v[1]);
        eq.tier = ParseTier(v[2]);
        eq.levelRequired = ParseInt(v[3]);
        
        // Physical combat stats
        eq.attackDamage = ParseFloat(v[4]);
        eq.attackDamagePercent = ParseFloat(v[5]);
        eq.attackSpeed = ParseFloat(v[6]);
        
        // Health stats
        eq.maxHealth = ParseFloat(v[7]);
        eq.healthRegen = ParseFloat(v[8]);
        
        // Defensive stats
        eq.armor = ParseFloat(v[9]);
        eq.dodge = ParseFloat(v[10]);
        
        // Special combat stats
        eq.criticalChance = ParseFloat(v[11]);
        eq.lifesteal = ParseFloat(v[12]);
        eq.hitRating = ParseFloat(v[13]);
        
        // Spell stats
        eq.spellDamage = ParseFloat(v[14]);
        eq.spellDamagePercent = ParseFloat(v[15]);
        eq.spellHealing = ParseFloat(v[16]);
        eq.spellHealingPercent = ParseFloat(v[17]);
        
        // Elemental damage
        eq.fireDamage = ParseFloat(v[18]);
        eq.fireDamagePercent = ParseFloat(v[19]);
        eq.frostDamage = ParseFloat(v[20]);
        eq.frostDamagePercent = ParseFloat(v[21]);
        eq.natureDamage = ParseFloat(v[22]);
        eq.natureDamagePercent = ParseFloat(v[23]);
        eq.shadowDamage = ParseFloat(v[24]);
        eq.shadowDamagePercent = ParseFloat(v[25]);
        eq.holyDamage = ParseFloat(v[26]);
        eq.holyDamagePercent = ParseFloat(v[27]);
        
        // DoT damage
        eq.bleedDamage = ParseFloat(v[28]);
        eq.bleedDamagePercent = ParseFloat(v[29]);
        eq.poisonDamage = ParseFloat(v[30]);
        eq.poisonDamagePercent = ParseFloat(v[31]);
        
        // Utility stats
        eq.xpBonus = ParseFloat(v[32]);
        eq.goldBonus = ParseFloat(v[33]);
        eq.afkGainsPercent = ParseFloat(v[34]);
        
        // Profession stats
        eq.professionXPPercent = ParseFloat(v[35]);
        eq.professionPowerPercent = ParseFloat(v[36]);
        
        // Description (last column)
        eq.description = v.Length > 37 ? v[37] : "";
        
        // Set rarity color based on tier
        eq.rarityColor = eq.tier switch
        {
            EquipmentTier.Common => Color.white,
            EquipmentTier.Uncommon => new Color(0.12f, 1f, 0f),    // Green
            EquipmentTier.Rare => new Color(0f, 0.44f, 0.87f),     // Blue
            EquipmentTier.Epic => new Color(0.64f, 0.21f, 0.93f),  // Purple
            EquipmentTier.Legendary => new Color(1f, 0.5f, 0f),    // Orange
            _ => Color.white
        };
        
        return eq;
    }
}
