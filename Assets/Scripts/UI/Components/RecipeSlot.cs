using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Individual recipe slot in the crafting panel's recipe list.
/// Displays recipe info and handles selection.
/// </summary>
public class RecipeSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public Image recipeIcon;
    public TextMeshProUGUI recipeNameText;
    public TextMeshProUGUI levelRequiredText;
    public GameObject lockedOverlay; // Optional overlay for locked recipes
    
    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    public Color lockedColor = Color.gray;
    
    private RecipeData recipe;
    private CraftingPanel craftingPanel;
    private Image backgroundImage;
    private bool isUnlocked = false;
    private bool isLevelMet = false;
    
    void Awake()
    {
        backgroundImage = GetComponent<Image>();
    }
    
    /// <summary>
    /// Initialize this recipe slot with data
    /// </summary>
    public void Initialize(RecipeData recipeData, CraftingPanel panel, bool unlocked, bool levelMet)
    {
        recipe = recipeData;
        craftingPanel = panel;
        isUnlocked = unlocked;
        isLevelMet = levelMet;
        
        UpdateDisplay();
    }
    
    /// <summary>
    /// Update the visual display of this slot
    /// </summary>
    public void UpdateDisplay()
    {
        if (recipe == null) return;
        
        // Set icon
        if (recipeIcon != null)
        {
            recipeIcon.sprite = recipe.recipeIcon;
            recipeIcon.color = (isUnlocked && isLevelMet) ? Color.white : lockedColor;
        }
        
        // Set name
        if (recipeNameText != null)
        {
            recipeNameText.text = recipe.recipeName;
            recipeNameText.color = (isUnlocked && isLevelMet) ? Color.white : lockedColor;
        }
        
        // Set level requirement
        if (levelRequiredText != null)
        {
            levelRequiredText.text = $"Level {recipe.levelRequired}";
            levelRequiredText.color = isLevelMet ? Color.green : Color.red;
        }
        
        // Show/hide locked overlay
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!isUnlocked || !isLevelMet);
        }
    }
    
    /// <summary>
    /// Set this slot as selected
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = selected ? selectedColor : normalColor;
        }
    }
    
    /// <summary>
    /// Handle click on this recipe slot
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (recipe != null && craftingPanel != null && isUnlocked && isLevelMet)
        {
            craftingPanel.SelectRecipe(recipe);
        }
    }
    
    public RecipeData GetRecipe()
    {
        return recipe;
    }
}
