using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Editor tool to import recipe data from CSV and create Recipe ScriptableObjects.
/// ONLY creates Recipe_ assets - does NOT modify existing Item_ or Equipment_ assets.
/// Access via: Tools → Vinland → Import Recipe CSV
/// </summary>
public class RecipeCSVImporter : EditorWindow
{
    private string csvPath = "Assets/Documentation/recipe_data.csv";
    private string outputFolder = "Assets/Resources/Recipes";
    private string itemsFolder = "Assets/Resources/Items";
    private string equippableItemsFolder = "Assets/Resources/Items/EquippableItems";
    
    [MenuItem("Tools/Vinland/Import Recipe CSV")]
    public static void ShowWindow()
    {
        GetWindow<RecipeCSVImporter>("Recipe Importer");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Recipe CSV Importer", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        csvPath = EditorGUILayout.TextField("CSV Path:", csvPath);
        outputFolder = EditorGUILayout.TextField("Output Folder:", outputFolder);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "This will create Recipe_ ScriptableObjects from the CSV.\n\n" +
            "• ONLY creates Recipe_ assets\n" +
            "• Does NOT modify existing Item_ or Equipment_ assets\n" +
            "• Links outputItem to existing Item_ assets by name\n\n" +
            "Existing recipes with the same name will be overwritten.",
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Import Recipes from CSV", GUILayout.Height(40)))
        {
            ImportRecipes();
        }
    }
    
    void ImportRecipes()
    {
        if (!File.Exists(csvPath))
        {
            EditorUtility.DisplayDialog("Error", $"CSV file not found at: {csvPath}", "OK");
            return;
        }
        
        // Ensure output folder exists
        EnsureFolderExists(outputFolder);
        
        // Load all existing items for lookup
        Dictionary<string, ItemData> itemLookup = LoadAllItems();
        Debug.Log($"[RecipeCSVImporter] Loaded {itemLookup.Count} items for lookup");
        
        string[] lines = File.ReadAllLines(csvPath);
        int created = 0;
        int skipped = 0;
        List<string> missingItems = new List<string>();
        
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            
            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;
            
            // Skip header
            if (line.StartsWith("recipeName,"))
                continue;
            
            string[] values = ParseCSVLine(line);
            
            if (values.Length < 6)
            {
                Debug.LogWarning($"Line {i + 1}: Not enough columns ({values.Length}/6), skipping");
                skipped++;
                continue;
            }
            
            try
            {
                string recipeName = values[0];
                string professionStr = values[1];
                int levelRequired = ParseInt(values[2]);
                string outputItemName = values[3];
                int xpGained = ParseInt(values[4]);
                float craftTime = ParseFloat(values[5]);
                bool isDefaultUnlocked = values.Length > 6 ? ParseBool(values[6]) : true;
                
                // Find the output item
                ItemData outputItem = FindItem(itemLookup, outputItemName);
                if (outputItem == null)
                {
                    if (!missingItems.Contains(outputItemName))
                    {
                        missingItems.Add(outputItemName);
                        Debug.LogWarning($"Line {i + 1}: Output item '{outputItemName}' not found, skipping");
                    }
                    skipped++;
                    continue;
                }
                
                // Create the recipe
                RecipeData recipe = ScriptableObject.CreateInstance<RecipeData>();
                recipe.recipeName = recipeName;
                recipe.profession = ParseProfession(professionStr);
                recipe.levelRequired = levelRequired;
                recipe.outputItem = outputItem;
                recipe.outputQuantity = 1;
                recipe.experienceGained = xpGained;
                recipe.craftTime = craftTime;
                recipe.isDefaultUnlocked = isDefaultUnlocked;
                recipe.description = $"Craft {outputItemName}";
                recipe.materials = new RecipeMaterial[0]; // Empty - user will add later
                
                // Generate asset name with profession suffix for starter items
                string assetName = SanitizeFileName(recipeName);
                if (levelRequired == 1)
                {
                    // Add profession suffix for starter items (same name, different profession)
                    assetName += "_" + professionStr;
                }
                
                string assetPath = $"{outputFolder}/Recipe_{assetName}.asset";
                
                // Save or update
                RecipeData existing = AssetDatabase.LoadAssetAtPath<RecipeData>(assetPath);
                if (existing != null)
                {
                    EditorUtility.CopySerialized(recipe, existing);
                    EditorUtility.SetDirty(existing);
                    Debug.Log($"Updated: Recipe_{assetName}");
                }
                else
                {
                    AssetDatabase.CreateAsset(recipe, assetPath);
                    Debug.Log($"Created: Recipe_{assetName}");
                }
                
                created++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Line {i + 1}: Error parsing - {e.Message}");
                skipped++;
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        string message = $"Recipes Created/Updated: {created}\nSkipped: {skipped}";
        if (missingItems.Count > 0)
        {
            message += $"\n\nMissing Items ({missingItems.Count}):\n";
            foreach (string item in missingItems)
            {
                message += $"• {item}\n";
            }
        }
        
        EditorUtility.DisplayDialog("Import Complete", message, "OK");
        Debug.Log($"[RecipeCSVImporter] Import complete. Created: {created}, Skipped: {skipped}");
    }
    
    Dictionary<string, ItemData> LoadAllItems()
    {
        Dictionary<string, ItemData> lookup = new Dictionary<string, ItemData>();
        
        // Load from main Items folder
        LoadItemsFromFolder(itemsFolder, lookup);
        
        // Load from EquippableItems subfolder
        LoadItemsFromFolder(equippableItemsFolder, lookup);
        
        return lookup;
    }
    
    void LoadItemsFromFolder(string folder, Dictionary<string, ItemData> lookup)
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null && !string.IsNullOrEmpty(item.itemName))
            {
                // Store by item name (not asset name)
                if (!lookup.ContainsKey(item.itemName))
                {
                    lookup[item.itemName] = item;
                }
            }
        }
    }
    
    ItemData FindItem(Dictionary<string, ItemData> lookup, string itemName)
    {
        if (lookup.TryGetValue(itemName, out ItemData item))
        {
            return item;
        }
        return null;
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
    
    ProfessionType ParseProfession(string profession)
    {
        return profession switch
        {
            "Blacksmithing" => ProfessionType.Blacksmithing,
            "Leatherworking" => ProfessionType.Leatherworking,
            "Tailoring" => ProfessionType.Tailoring,
            "Cooking" => ProfessionType.Cooking,
            "Mining" => ProfessionType.Mining,
            "Herbalism" => ProfessionType.Herbalism,
            "Fishing" => ProfessionType.Fishing,
            "Woodcutting" => ProfessionType.Woodcutting,
            _ => ProfessionType.Blacksmithing
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
    
    bool ParseBool(string value)
    {
        if (string.IsNullOrEmpty(value)) return true; // Default to unlocked
        string lower = value.Trim().ToLower();
        return lower == "true" || lower == "1" || lower == "yes";
    }
}


