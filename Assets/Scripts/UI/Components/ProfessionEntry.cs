using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for a single profession entry.
/// Displays profession name, level, XP bar, and icon.
/// Attach to profession entry prefab.
/// </summary>
public class ProfessionEntry : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public Image backgroundImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public Slider xpBar;
    public Image xpBarFill; // The fill image of the slider
    public TextMeshProUGUI xpText;
    
    private ProfessionDefinitionData professionDef;
    private ProfessionType professionType;
    
    /// <summary>
    /// Initialize this entry with a profession definition
    /// </summary>
    public void Initialize(ProfessionDefinitionData definition)
    {
        if (definition == null)
        {
            Debug.LogWarning("[ProfessionEntry] Tried to initialize with null definition!");
            return;
        }
        
        professionDef = definition;
        professionType = definition.professionType;
        
        // Set static visual data
        if (iconImage != null && definition.icon != null)
        {
            iconImage.sprite = definition.icon;
        }
        
        if (nameText != null)
        {
            nameText.text = definition.displayName;
        }
        
        // Apply theme color to background if available
        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(
                definition.themeColor.r * 0.3f,
                definition.themeColor.g * 0.3f,
                definition.themeColor.b * 0.3f,
                0.5f
            );
        }
        
        // Apply theme color to XP bar fill
        if (xpBarFill != null)
        {
            xpBarFill.color = definition.themeColor;
        }
        else if (xpBar != null)
        {
            // Try to find the fill image automatically if not assigned
            Image fillImage = xpBar.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = definition.themeColor;
            }
        }
        
        // Initialize XP bar
        if (xpBar != null)
        {
            xpBar.minValue = 0;
            xpBar.value = 0;
        }
    }
    
    /// <summary>
    /// Update the display with current level and XP
    /// </summary>
    public void UpdateDisplay(int level, int currentXP, int xpRequired)
    {
        // Update level text
        if (levelText != null)
        {
            levelText.text = $"Level {level}";
        }
        
        // Update XP bar
        if (xpBar != null)
        {
            xpBar.maxValue = xpRequired;
            xpBar.value = currentXP;
        }
        
        // Update XP text
        if (xpText != null)
        {
            xpText.text = $"{currentXP} / {xpRequired}";
        }
    }
    
    /// <summary>
    /// Get the profession type this entry represents
    /// </summary>
    public ProfessionType GetProfessionType()
    {
        return professionType;
    }
    
    /// <summary>
    /// Get the profession definition
    /// </summary>
    public ProfessionDefinitionData GetDefinition()
    {
        return professionDef;
    }
}

