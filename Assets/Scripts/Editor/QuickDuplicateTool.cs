#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Context menu tool for quickly duplicating ScriptableObjects with smart increments.
/// Right-click any Monster/Item/Equipment/Quest → "Duplicate & Modify"
/// </summary>
public static class QuickDuplicateTool
{
    // Menu item validation - show menu only for supported types
    [MenuItem("Assets/Duplicate & Modify", true)]
    private static bool ValidateDuplicate()
    {
        Object selected = Selection.activeObject;
        return selected is MonsterData || 
               selected is ItemData || 
               selected is EquipmentData || 
               selected is QuestData;
    }
    
    // Menu item action
    [MenuItem("Assets/Duplicate & Modify", false, 20)]
    private static void DuplicateAndModify()
    {
        Object selected = Selection.activeObject;
        
        if (selected is MonsterData monster)
        {
            DuplicateMonster(monster);
        }
        else if (selected is EquipmentData equipment)
        {
            DuplicateEquipment(equipment);
        }
        else if (selected is ItemData item)
        {
            DuplicateItem(item);
        }
        else if (selected is QuestData quest)
        {
            DuplicateQuest(quest);
        }
    }
    
    #region Monster Duplication
    
    private static void DuplicateMonster(MonsterData original)
    {
        // Prompt for new name
        string newName = EditorInputDialog.Show(
            "Duplicate Monster", 
            "Enter name for new monster:", 
            original.monsterName + " Copy"
        );
        
        if (string.IsNullOrEmpty(newName))
        {
            Debug.Log("[QuickDuplicate] Cancelled monster duplication");
            return;
        }
        
        // Create new instance
        MonsterData duplicate = Object.Instantiate(original);
        
        // Apply smart increments
        duplicate.monsterName = newName;
        duplicate.level = original.level + 1;
        duplicate.health = Mathf.Round(original.health * 1.1f);
        duplicate.attackDamage = Mathf.Round(original.attackDamage * 1.05f);
        duplicate.xpReward = Mathf.RoundToInt(original.xpReward * 1.1f);
        duplicate.goldReward = Mathf.RoundToInt(original.goldReward * 1.1f);
        
        // Save asset
        string path = AssetDatabase.GetAssetPath(original);
        string directory = System.IO.Path.GetDirectoryName(path);
        string fileName = $"Monster_{newName.Replace(" ", "")}.asset";
        string newPath = $"{directory}/{fileName}";
        
        // Handle existing file
        if (System.IO.File.Exists(newPath))
        {
            if (!EditorUtility.DisplayDialog("File Exists", 
                $"Monster '{newName}' already exists. Overwrite?", 
                "Overwrite", "Cancel"))
            {
                return;
            }
            AssetDatabase.DeleteAsset(newPath);
        }
        
        AssetDatabase.CreateAsset(duplicate, newPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Select new asset
        Selection.activeObject = duplicate;
        EditorGUIUtility.PingObject(duplicate);
        
        Debug.Log($"[QuickDuplicate] Created monster '{newName}' with +10% HP, +5% damage, +1 level");
    }
    
    #endregion
    
    #region Equipment Duplication
    
    private static void DuplicateEquipment(EquipmentData original)
    {
        // Prompt for new name
        string newName = EditorInputDialog.Show(
            "Duplicate Equipment", 
            "Enter name for new equipment:", 
            original.equipmentName + " II"
        );
        
        if (string.IsNullOrEmpty(newName))
        {
            Debug.Log("[QuickDuplicate] Cancelled equipment duplication");
            return;
        }
        
        // Create new instance
        EquipmentData duplicate = Object.Instantiate(original);
        
        // Apply smart increments
        duplicate.equipmentName = newName;
        duplicate.levelRequired = original.levelRequired + 1;
        
        // Increase all non-zero stats by 10%
        if (duplicate.attackDamage > 0) duplicate.attackDamage = Mathf.Round(duplicate.attackDamage * 1.1f);
        if (duplicate.maxHealth > 0) duplicate.maxHealth = Mathf.Round(duplicate.maxHealth * 1.1f);
        if (duplicate.healthRegen > 0) duplicate.healthRegen = Mathf.Round(duplicate.healthRegen * 1.1f * 10f) / 10f; // 1 decimal
        
        // Percentage stats (round to 2 decimals)
        if (duplicate.armor > 0) duplicate.armor = Mathf.Round(duplicate.armor * 1.1f * 100f) / 100f;
        if (duplicate.dodge > 0) duplicate.dodge = Mathf.Round(duplicate.dodge * 1.1f * 100f) / 100f;
        if (duplicate.criticalChance > 0) duplicate.criticalChance = Mathf.Round(duplicate.criticalChance * 1.1f * 100f) / 100f;
        if (duplicate.lifesteal > 0) duplicate.lifesteal = Mathf.Round(duplicate.lifesteal * 1.1f * 100f) / 100f;
        if (duplicate.xpBonus > 0) duplicate.xpBonus = Mathf.Round(duplicate.xpBonus * 1.1f * 100f) / 100f;
        if (duplicate.goldBonus > 0) duplicate.goldBonus = Mathf.Round(duplicate.goldBonus * 1.1f * 100f) / 100f;
        
        // Attack speed (negative values)
        if (duplicate.attackSpeed < 0) duplicate.attackSpeed = Mathf.Round(duplicate.attackSpeed * 1.1f * 100f) / 100f;
        
        // Save asset
        string path = AssetDatabase.GetAssetPath(original);
        string directory = System.IO.Path.GetDirectoryName(path);
        string fileName = $"Equipment_{newName.Replace(" ", "")}.asset";
        string newPath = $"{directory}/{fileName}";
        
        // Handle existing file
        if (System.IO.File.Exists(newPath))
        {
            if (!EditorUtility.DisplayDialog("File Exists", 
                $"Equipment '{newName}' already exists. Overwrite?", 
                "Overwrite", "Cancel"))
            {
                return;
            }
            AssetDatabase.DeleteAsset(newPath);
        }
        
        AssetDatabase.CreateAsset(duplicate, newPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Select new asset
        Selection.activeObject = duplicate;
        EditorGUIUtility.PingObject(duplicate);
        
        Debug.Log($"[QuickDuplicate] Created equipment '{newName}' with +10% stats, +1 level requirement");
    }
    
    #endregion
    
    #region Item Duplication
    
    private static void DuplicateItem(ItemData original)
    {
        // Prompt for new name
        string newName = EditorInputDialog.Show(
            "Duplicate Item", 
            "Enter name for new item:", 
            original.itemName + " Copy"
        );
        
        if (string.IsNullOrEmpty(newName))
        {
            Debug.Log("[QuickDuplicate] Cancelled item duplication");
            return;
        }
        
        // Create new instance
        ItemData duplicate = Object.Instantiate(original);
        
        // Only change name - keep all other values
        duplicate.itemName = newName;
        
        // Save asset
        string path = AssetDatabase.GetAssetPath(original);
        string directory = System.IO.Path.GetDirectoryName(path);
        string fileName = $"Item_{newName.Replace(" ", "")}.asset";
        string newPath = $"{directory}/{fileName}";
        
        // Handle existing file
        if (System.IO.File.Exists(newPath))
        {
            if (!EditorUtility.DisplayDialog("File Exists", 
                $"Item '{newName}' already exists. Overwrite?", 
                "Overwrite", "Cancel"))
            {
                return;
            }
            AssetDatabase.DeleteAsset(newPath);
        }
        
        AssetDatabase.CreateAsset(duplicate, newPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Select new asset
        Selection.activeObject = duplicate;
        EditorGUIUtility.PingObject(duplicate);
        
        Debug.Log($"[QuickDuplicate] Created item '{newName}' (manual editing recommended)");
    }
    
    #endregion
    
    #region Quest Duplication
    
    private static void DuplicateQuest(QuestData original)
    {
        // Prompt for new name
        string newName = EditorInputDialog.Show(
            "Duplicate Quest", 
            "Enter name for new quest:", 
            original.questName + " II"
        );
        
        if (string.IsNullOrEmpty(newName))
        {
            Debug.Log("[QuickDuplicate] Cancelled quest duplication");
            return;
        }
        
        // Create new instance
        QuestData duplicate = Object.Instantiate(original);
        
        // Apply smart increments
        duplicate.questName = newName;
        duplicate.levelRequired = original.levelRequired + 1;
        duplicate.xpReward = Mathf.RoundToInt(original.xpReward * 1.2f);
        duplicate.goldReward = Mathf.RoundToInt(original.goldReward * 1.2f);
        
        // Save asset
        string path = AssetDatabase.GetAssetPath(original);
        string directory = System.IO.Path.GetDirectoryName(path);
        string fileName = $"Quest_{newName.Replace(" ", "")}.asset";
        string newPath = $"{directory}/{fileName}";
        
        // Handle existing file
        if (System.IO.File.Exists(newPath))
        {
            if (!EditorUtility.DisplayDialog("File Exists", 
                $"Quest '{newName}' already exists. Overwrite?", 
                "Overwrite", "Cancel"))
            {
                return;
            }
            AssetDatabase.DeleteAsset(newPath);
        }
        
        AssetDatabase.CreateAsset(duplicate, newPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Select new asset
        Selection.activeObject = duplicate;
        EditorGUIUtility.PingObject(duplicate);
        
        Debug.Log($"[QuickDuplicate] Created quest '{newName}' with +20% rewards, +1 level requirement");
    }
    
    #endregion
}

/// <summary>
/// Simple input dialog for editor
/// </summary>
public static class EditorInputDialog
{
    public static string Show(string title, string message, string defaultValue = "")
    {
        // Unity doesn't have a built-in input dialog, so we'll use a workaround
        // This uses Unity's internal dialog system
        
        string result = defaultValue;
        
        // Create a temporary EditorWindow for input
        var window = ScriptableObject.CreateInstance<InputDialogWindow>();
        window.titleContent = new GUIContent(title);
        window.message = message;
        window.inputValue = defaultValue;
        window.ShowModal();
        
        return window.confirmed ? window.inputValue : null;
    }
}

/// <summary>
/// Simple input dialog window
/// </summary>
public class InputDialogWindow : EditorWindow
{
    public string message = "";
    public string inputValue = "";
    public bool confirmed = false;
    
    private void OnGUI()
    {
        minSize = new Vector2(400, 120);
        maxSize = new Vector2(400, 120);
        
        GUILayout.Space(10);
        EditorGUILayout.LabelField(message, EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);
        
        GUI.SetNextControlName("InputField");
        inputValue = EditorGUILayout.TextField(inputValue);
        
        GUILayout.Space(10);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("OK", GUILayout.Width(80)))
        {
            confirmed = true;
            Close();
        }
        
        if (GUILayout.Button("Cancel", GUILayout.Width(80)))
        {
            confirmed = false;
            inputValue = null;
            Close();
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Focus input field on first frame
        if (Event.current.type == EventType.Layout)
        {
            EditorGUI.FocusTextInControl("InputField");
        }
        
        // Handle Enter key
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            confirmed = true;
            Close();
        }
        
        // Handle Escape key
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            confirmed = false;
            inputValue = null;
            Close();
        }
    }
}
#endif

