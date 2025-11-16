#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Base class for all content creator editor windows.
/// Provides shared UI components, validation, and asset management.
/// </summary>
public abstract class ContentCreatorBase : EditorWindow
{
    protected Vector2 scrollPosition;
    protected List<string> validationWarnings = new List<string>();
    protected List<string> validationErrors = new List<string>();
    
    // UI Style constants
    protected const float LABEL_WIDTH = 150f;
    protected const float BUTTON_HEIGHT = 30f;
    protected const float SECTION_SPACING = 15f;
    protected const float FIELD_SPACING = 5f;
    
    // Colors
    protected static readonly Color headerColor = new Color(0.3f, 0.5f, 0.7f, 0.3f);
    protected static readonly Color warningColor = new Color(1f, 0.8f, 0f, 0.3f);
    protected static readonly Color errorColor = new Color(1f, 0.3f, 0.3f, 0.3f);
    protected static readonly Color validColor = new Color(0.3f, 1f, 0.3f, 0.3f);
    
    /// <summary>
    /// Called when window needs to validate current data
    /// </summary>
    protected abstract void ValidateData();
    
    /// <summary>
    /// Called when user clicks create/save button
    /// </summary>
    protected abstract void CreateAsset();
    
    /// <summary>
    /// Called to reset form to default values
    /// </summary>
    protected abstract void ClearForm();
    
    /// <summary>
    /// Draw the main content of the window
    /// </summary>
    protected abstract void DrawContent();
    
    protected virtual void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Draw main content
        DrawContent();
        
        GUILayout.Space(SECTION_SPACING);
        
        // Draw validation panel
        DrawValidationPanel();
        
        GUILayout.Space(SECTION_SPACING);
        
        // Draw action buttons
        DrawActionButtons();
        
        EditorGUILayout.EndScrollView();
        
        // Validate on every frame (lightweight checks only)
        ValidateData();
    }
    
    #region UI Drawing Methods
    
    /// <summary>
    /// Draw a styled header section
    /// </summary>
    protected void DrawHeader(string title)
    {
        GUILayout.Space(SECTION_SPACING);
        
        // Draw background
        Rect headerRect = EditorGUILayout.BeginHorizontal();
        EditorGUI.DrawRect(headerRect, headerColor);
        
        GUILayout.Space(10);
        GUILayout.Label(title, EditorStyles.boldLabel);
        
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(FIELD_SPACING);
    }
    
    /// <summary>
    /// Draw a sprite field with preview
    /// </summary>
    protected Sprite DrawSpriteField(string label, Sprite sprite, int previewSize = 64)
    {
        EditorGUILayout.BeginHorizontal();
        
        // Label
        EditorGUILayout.LabelField(label, GUILayout.Width(LABEL_WIDTH));
        
        // Sprite field
        Sprite newSprite = (Sprite)EditorGUILayout.ObjectField(sprite, typeof(Sprite), false, GUILayout.Height(previewSize));
        
        // Preview
        if (newSprite != null)
        {
            Rect previewRect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.Width(previewSize), GUILayout.Height(previewSize));
            GUI.DrawTexture(previewRect, newSprite.texture, ScaleMode.ScaleToFit);
        }
        
        EditorGUILayout.EndHorizontal();
        
        return newSprite;
    }
    
    /// <summary>
    /// Draw an int field with label
    /// </summary>
    protected int DrawIntField(string label, int value, string tooltip = "")
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(LABEL_WIDTH));
        int newValue = EditorGUILayout.IntField(value);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(FIELD_SPACING);
        return newValue;
    }
    
    /// <summary>
    /// Draw a float field with label
    /// </summary>
    protected float DrawFloatField(string label, float value, string tooltip = "")
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(LABEL_WIDTH));
        float newValue = EditorGUILayout.FloatField(value);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(FIELD_SPACING);
        return newValue;
    }
    
    /// <summary>
    /// Draw a string field with label
    /// </summary>
    protected string DrawTextField(string label, string value, string tooltip = "")
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(LABEL_WIDTH));
        string newValue = EditorGUILayout.TextField(value);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(FIELD_SPACING);
        return newValue;
    }
    
    /// <summary>
    /// Draw a text area with label
    /// </summary>
    protected string DrawTextArea(string label, string value, string tooltip = "", int minLines = 3)
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(new GUIContent(label, tooltip));
        string newValue = EditorGUILayout.TextArea(value, GUILayout.MinHeight(minLines * 18));
        EditorGUILayout.EndVertical();
        GUILayout.Space(FIELD_SPACING);
        return newValue;
    }
    
    /// <summary>
    /// Draw a toggle with label
    /// </summary>
    protected bool DrawToggle(string label, bool value, string tooltip = "")
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(LABEL_WIDTH));
        bool newValue = EditorGUILayout.Toggle(value);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(FIELD_SPACING);
        return newValue;
    }
    
    /// <summary>
    /// Draw a slider with label
    /// </summary>
    protected float DrawSlider(string label, float value, float min, float max, string tooltip = "")
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(LABEL_WIDTH));
        float newValue = EditorGUILayout.Slider(value, min, max);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(FIELD_SPACING);
        return newValue;
    }
    
    /// <summary>
    /// Draw an enum popup with label
    /// </summary>
    protected T DrawEnumPopup<T>(string label, T value, string tooltip = "") where T : System.Enum
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(LABEL_WIDTH));
        T newValue = (T)EditorGUILayout.EnumPopup(value);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(FIELD_SPACING);
        return newValue;
    }
    
    /// <summary>
    /// Draw an object field with label
    /// </summary>
    protected T DrawObjectField<T>(string label, T obj, bool allowSceneObjects = false, string tooltip = "") where T : Object
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(LABEL_WIDTH));
        T newObj = (T)EditorGUILayout.ObjectField(obj, typeof(T), allowSceneObjects);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(FIELD_SPACING);
        return newObj;
    }
    
    /// <summary>
    /// Draw a horizontal separator line
    /// </summary>
    protected void DrawSeparator()
    {
        GUILayout.Space(SECTION_SPACING);
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
        GUILayout.Space(SECTION_SPACING);
    }
    
    /// <summary>
    /// Draw validation panel showing warnings and errors
    /// </summary>
    protected void DrawValidationPanel()
    {
        if (validationErrors.Count == 0 && validationWarnings.Count == 0)
        {
            // Show valid state
            Rect validRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(new Rect(validRect.x, validRect.y, validRect.width, validRect.height + 10), validColor);
            GUILayout.Space(5);
            EditorGUILayout.LabelField("✓ Ready to create", EditorStyles.boldLabel);
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
            return;
        }
        
        // Draw errors
        if (validationErrors.Count > 0)
        {
            Rect errorRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(new Rect(errorRect.x, errorRect.y, errorRect.width, errorRect.height + 10), errorColor);
            GUILayout.Space(5);
            
            EditorGUILayout.LabelField("❌ Errors (must fix before creating):", EditorStyles.boldLabel);
            foreach (string error in validationErrors)
            {
                EditorGUILayout.LabelField("  • " + error, EditorStyles.wordWrappedLabel);
            }
            
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
            GUILayout.Space(FIELD_SPACING);
        }
        
        // Draw warnings
        if (validationWarnings.Count > 0)
        {
            Rect warningRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(new Rect(warningRect.x, warningRect.y, warningRect.width, warningRect.height + 10), warningColor);
            GUILayout.Space(5);
            
            EditorGUILayout.LabelField("⚠ Warnings (can still create, but review):", EditorStyles.boldLabel);
            foreach (string warning in validationWarnings)
            {
                EditorGUILayout.LabelField("  • " + warning, EditorStyles.wordWrappedLabel);
            }
            
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }
    }
    
    /// <summary>
    /// Draw action buttons (Create, Clear, etc.)
    /// </summary>
    protected void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();
        
        // Disable create button if there are errors
        GUI.enabled = validationErrors.Count == 0;
        
        if (GUILayout.Button("Create Asset", GUILayout.Height(BUTTON_HEIGHT)))
        {
            CreateAsset();
        }
        
        GUI.enabled = true;
        
        if (GUILayout.Button("Create & New", GUILayout.Height(BUTTON_HEIGHT)))
        {
            CreateAsset();
            ClearForm();
        }
        
        if (GUILayout.Button("Clear Form", GUILayout.Height(BUTTON_HEIGHT)))
        {
            if (EditorUtility.DisplayDialog("Clear Form", "Are you sure you want to clear the form?", "Yes", "No"))
            {
                ClearForm();
            }
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    #endregion
    
    #region Asset Management
    
    /// <summary>
    /// Save a ScriptableObject asset to the Resources folder
    /// </summary>
    protected T SaveAsset<T>(T asset, string resourcePath, string fileName) where T : ScriptableObject
    {
        // Ensure directory exists
        string fullPath = $"Assets/Resources/{resourcePath}";
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }
        
        // Sanitize filename (remove invalid characters)
        fileName = SanitizeFileName(fileName);
        
        // Create full asset path
        string assetPath = $"{fullPath}/{fileName}.asset";
        
        // Check if file exists
        if (File.Exists(assetPath))
        {
            if (!EditorUtility.DisplayDialog("File Exists", 
                $"Asset '{fileName}' already exists. Overwrite?", 
                "Overwrite", "Cancel"))
            {
                return null;
            }
            
            // Delete existing asset
            AssetDatabase.DeleteAsset(assetPath);
        }
        
        // Create and save asset
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Select the new asset
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
        
        Debug.Log($"[ContentCreator] Created asset: {assetPath}");
        
        return asset;
    }
    
    /// <summary>
    /// Sanitize filename by removing invalid characters
    /// </summary>
    protected string SanitizeFileName(string fileName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char c in invalidChars)
        {
            fileName = fileName.Replace(c, '_');
        }
        return fileName;
    }
    
    /// <summary>
    /// Show success notification
    /// </summary>
    protected void ShowSuccessNotification(string message)
    {
        ShowNotification(new GUIContent($"✓ {message}"));
    }
    
    /// <summary>
    /// Show error notification
    /// </summary>
    protected void ShowErrorNotification(string message)
    {
        ShowNotification(new GUIContent($"❌ {message}"));
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Clear validation lists
    /// </summary>
    protected void ClearValidation()
    {
        validationWarnings.Clear();
        validationErrors.Clear();
    }
    
    /// <summary>
    /// Add validation warning
    /// </summary>
    protected void AddWarning(string message)
    {
        if (!validationWarnings.Contains(message))
        {
            validationWarnings.Add(message);
        }
    }
    
    /// <summary>
    /// Add validation error
    /// </summary>
    protected void AddError(string message)
    {
        if (!validationErrors.Contains(message))
        {
            validationErrors.Add(message);
        }
    }
    
    #endregion
}
#endif

