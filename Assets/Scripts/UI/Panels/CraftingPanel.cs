using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Main UI panel for the crafting interface (Cooking, Alchemy, Blacksmithing, etc.).
/// Displays recipes, handles crafting, and shows progress.
/// </summary>
public class CraftingPanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject craftingPanel;
    
    [Header("Panel Controller")]
    public UIPanelController panelController;
    
    [Header("Recipe List")]
    public Transform recipeListContainer;
    public GameObject recipeSlotPrefab;
    
    [Header("Recipe Details")]
    public Image selectedRecipeIcon;
    public TextMeshProUGUI selectedRecipeNameText;
    public TextMeshProUGUI selectedRecipeDescriptionText;
    public Transform materialsContainer;
    public GameObject materialEntryPrefab;
    public TextMeshProUGUI xpRewardText;
    
    [Header("Crafting Controls")]
    public TMP_InputField craftQuantityInput;
    public Button craftButton;
    public Button craftAllButton;
    public Button closeButton;
    
    [Header("Progress")]
    public GameObject progressSection;
    public Slider progressBar;
    public TextMeshProUGUI progressText;
    
    [Header("Item Float Animation")]
    public Transform itemFloatParent; // Where to spawn float animations
    
    private ICraftingService craftingService;
    private IProfessionService professionService;
    
    private RecipeData selectedRecipe = null;
    private List<GameObject> recipeSlots = new List<GameObject>();
    private List<GameObject> materialEntries = new List<GameObject>();
    
    void Start()
    {
        // Get services
        craftingService = Services.Get<ICraftingService>();
        professionService = Services.Get<IProfessionService>();
        
        // Subscribe to events
        EventBus.Subscribe<RecipeUnlockedEvent>(OnRecipeUnlocked);
        EventBus.Subscribe<CraftingStartedEvent>(OnCraftingStarted);
        EventBus.Subscribe<CraftingProgressEvent>(OnCraftingProgress);
        EventBus.Subscribe<CraftingCompletedEvent>(OnCraftingCompleted);
        EventBus.Subscribe<CraftingCancelledEvent>(OnCraftingCancelled);
        EventBus.Subscribe<ZoneChangedEvent>(OnZoneChanged);
        
        // Setup buttons
        if (craftButton != null)
            craftButton.onClick.AddListener(OnCraftClicked);
        
        if (craftAllButton != null)
            craftAllButton.onClick.AddListener(OnCraftAllClicked);
        
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
        
        // Hide panel initially
        if (craftingPanel != null)
            craftingPanel.SetActive(false);
        
        // Hide progress section initially
        if (progressSection != null)
            progressSection.SetActive(false);
    }
    
    void OnDestroy()
    {
        EventBus.Unsubscribe<RecipeUnlockedEvent>(OnRecipeUnlocked);
        EventBus.Unsubscribe<CraftingStartedEvent>(OnCraftingStarted);
        EventBus.Unsubscribe<CraftingProgressEvent>(OnCraftingProgress);
        EventBus.Unsubscribe<CraftingCompletedEvent>(OnCraftingCompleted);
        EventBus.Unsubscribe<CraftingCancelledEvent>(OnCraftingCancelled);
        EventBus.Unsubscribe<ZoneChangedEvent>(OnZoneChanged);
    }
    
    private void OnZoneChanged(ZoneChangedEvent evt)
    {
        ClosePanel();
    }
    
    /// <summary>
    /// Open the crafting panel
    /// </summary>
    public void OpenPanel()
    {
        if (craftingPanel != null)
        {
            if (panelController != null)
                panelController.OpenPanel(craftingPanel);
            else
                craftingPanel.SetActive(true);
        }
        
        // Clear selection when opening
        selectedRecipe = null;
        
        RefreshRecipeList();
        UpdateRecipeDetails(); // This will hide the icon since selectedRecipe is null
    }
    
    /// <summary>
    /// Close the crafting panel
    /// </summary>
    public void ClosePanel()
    {
        if (craftingPanel != null)
        {
            if (panelController != null)
                panelController.ClosePanel(craftingPanel);
            else
                craftingPanel.SetActive(false);
        }
        
        // Cancel any active crafting
        if (craftingService != null && craftingService.IsCrafting())
        {
            craftingService.CancelCrafting();
        }
    }
    
    /// <summary>
    /// Toggle the crafting panel
    /// </summary>
    public void TogglePanel()
    {
        if (craftingPanel != null)
        {
            if (panelController != null)
            {
                panelController.TogglePanel(craftingPanel);
                if (craftingPanel.activeSelf)
                {
                    selectedRecipe = null;
                    RefreshRecipeList();
                    UpdateRecipeDetails();
                }
            }
            else
            {
                if (craftingPanel.activeSelf)
                    ClosePanel();
                else
                    OpenPanel();
            }
        }
    }
    
    /// <summary>
    /// Refresh the list of recipes
    /// </summary>
    private void RefreshRecipeList()
    {
        if (craftingService == null || recipeListContainer == null) return;
        
        // Clear existing slots
        foreach (GameObject slot in recipeSlots)
        {
            if (slot != null) Destroy(slot);
        }
        recipeSlots.Clear();
        
        // Get all recipes for this profession (currently hardcoded to Cooking)
        RecipeData[] allRecipes = craftingService.GetAllCookingRecipes();
        int cookingLevel = professionService != null ? professionService.GetProfessionLevel(ProfessionType.Cooking) : 1;
        
        // Create slots for each recipe
        foreach (RecipeData recipe in allRecipes)
        {
            if (recipe == null || recipeSlotPrefab == null) continue;
            
            GameObject slotObj = Instantiate(recipeSlotPrefab, recipeListContainer);
            RecipeSlot slot = slotObj.GetComponent<RecipeSlot>();
            
            if (slot != null)
            {
                bool isUnlocked = craftingService.IsRecipeUnlocked(recipe);
                bool isLevelMet = cookingLevel >= recipe.levelRequired;
                
                slot.Initialize(recipe, this, isUnlocked, isLevelMet);
                recipeSlots.Add(slotObj);
            }
            else
            {
                Destroy(slotObj);
            }
        }
    }
    
    /// <summary>
    /// Select a recipe to view details
    /// </summary>
    public void SelectRecipe(RecipeData recipe)
    {
        selectedRecipe = recipe;
        
        // Update selection visuals
        foreach (GameObject slotObj in recipeSlots)
        {
            RecipeSlot slot = slotObj.GetComponent<RecipeSlot>();
            if (slot != null)
            {
                slot.SetSelected(slot.GetRecipe() == recipe);
            }
        }
        
        UpdateRecipeDetails();
        UpdateCraftingControls();
    }
    
    /// <summary>
    /// Update the recipe details panel
    /// </summary>
    private void UpdateRecipeDetails()
    {
        if (selectedRecipe == null)
        {
            // Hide recipe details when nothing is selected
            if (selectedRecipeIcon != null)
                selectedRecipeIcon.enabled = false;
            
            if (selectedRecipeNameText != null)
                selectedRecipeNameText.text = "";
            
            if (selectedRecipeDescriptionText != null)
                selectedRecipeDescriptionText.text = "";
            
            if (xpRewardText != null)
                xpRewardText.text = "";
            
            // Clear materials list
            UpdateMaterialsList();
            
            return;
        }
        
        // Show and update icon
        if (selectedRecipeIcon != null)
        {
            selectedRecipeIcon.enabled = true;
            selectedRecipeIcon.sprite = selectedRecipe.recipeIcon;
        }
        
        // Update name
        if (selectedRecipeNameText != null)
            selectedRecipeNameText.text = selectedRecipe.recipeName;
        
        // Update description
        if (selectedRecipeDescriptionText != null)
            selectedRecipeDescriptionText.text = selectedRecipe.description;
        
        // Update materials
        UpdateMaterialsList();
        
        // Update XP reward
        if (xpRewardText != null)
            xpRewardText.text = $"+{selectedRecipe.experienceGained} {selectedRecipe.profession} XP";
    }
    
    /// <summary>
    /// Update the materials list display
    /// </summary>
    private void UpdateMaterialsList()
    {
        if (materialsContainer == null) return;
        
        // Clear existing entries
        foreach (GameObject entry in materialEntries)
        {
            if (entry != null) Destroy(entry);
        }
        materialEntries.Clear();
        
        // If no recipe selected, just clear and return
        if (selectedRecipe == null) return;
        
        // Create entries for each material
        if (selectedRecipe.materials != null)
        {
            foreach (RecipeMaterial material in selectedRecipe.materials)
            {
                if (material.item == null || materialEntryPrefab == null) continue;
                
                GameObject entryObj = Instantiate(materialEntryPrefab, materialsContainer);
                
                // Assuming material entry has a TextMeshProUGUI component
                TextMeshProUGUI text = entryObj.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    // Check if player has enough materials
                    bool hasEnough = craftingService != null && craftingService.HasRequiredMaterials(selectedRecipe, 1);
                    text.text = $"{material.item.itemName} x{material.quantity}";
                    text.color = hasEnough ? Color.green : Color.red;
                }
                
                materialEntries.Add(entryObj);
            }
        }
    }
    
    /// <summary>
    /// Update the crafting controls (buttons, input field)
    /// </summary>
    private void UpdateCraftingControls()
    {
        if (selectedRecipe == null || craftingService == null)
        {
            if (craftButton != null) craftButton.interactable = false;
            if (craftAllButton != null) craftAllButton.interactable = false;
            return;
        }
        
        bool canCraft = craftingService.CanCraftRecipe(selectedRecipe, 1);
        int maxCraftable = craftingService.GetMaxCraftableAmount(selectedRecipe);
        
        // Update craft button
        if (craftButton != null)
            craftButton.interactable = canCraft && !craftingService.IsCrafting();
        
        // Update craft all button
        if (craftAllButton != null)
            craftAllButton.interactable = canCraft && maxCraftable > 0 && !craftingService.IsCrafting();
        
        // Set default quantity
        if (craftQuantityInput != null && string.IsNullOrEmpty(craftQuantityInput.text))
            craftQuantityInput.text = "1";
    }
    
    /// <summary>
    /// Handle craft button click
    /// </summary>
    private void OnCraftClicked()
    {
        if (selectedRecipe == null || craftingService == null) return;
        
        // Parse quantity from input field
        int quantity = 1;
        if (craftQuantityInput != null && !string.IsNullOrEmpty(craftQuantityInput.text))
        {
            int.TryParse(craftQuantityInput.text, out quantity);
        }
        
        quantity = Mathf.Max(1, quantity);
        
        // Start crafting
        craftingService.StartCrafting(selectedRecipe, quantity);
    }
    
    /// <summary>
    /// Handle craft all button click
    /// </summary>
    private void OnCraftAllClicked()
    {
        if (selectedRecipe == null || craftingService == null) return;
        
        int maxCraftable = craftingService.GetMaxCraftableAmount(selectedRecipe);
        
        if (maxCraftable > 0)
        {
            craftingService.StartCrafting(selectedRecipe, maxCraftable);
        }
    }
    
    // ==================== Event Handlers ====================
    
    private void OnRecipeUnlocked(RecipeUnlockedEvent evt)
    {
        RefreshRecipeList();
        Debug.Log($"Recipe unlocked: {evt.recipe.recipeName}!");
    }
    
    private void OnCraftingStarted(CraftingStartedEvent evt)
    {
        if (progressSection != null)
            progressSection.SetActive(true);
        
        UpdateCraftingControls();
    }
    
    private void OnCraftingProgress(CraftingProgressEvent evt)
    {
        if (progressBar != null)
            progressBar.value = evt.progress;
        
        if (progressText != null)
            progressText.text = $"Crafting... ({evt.currentIndex}/{evt.totalQuantity})";
    }
    
    private void OnCraftingCompleted(CraftingCompletedEvent evt)
    {
        // Only show animation if the panel is currently open
        bool isPanelOpen = craftingPanel != null && craftingPanel.activeSelf;
        
        // Show item float animation after EACH item completes (only if panel is open)
        if (isPanelOpen && itemFloatParent != null && evt.craftedItem != null && evt.craftedItem.icon != null)
        {
            Vector2 startPos = Vector2.zero; // Center of the panel
            ItemFloatAnimation.CreateAndAnimate(evt.craftedItem.icon, startPos, itemFloatParent);
        }
        
        // Check if ALL crafting is complete
        bool allCraftingComplete = craftingService != null && !craftingService.IsCrafting();
        
        if (allCraftingComplete)
        {
            // All done - hide progress section
            if (progressSection != null)
                progressSection.SetActive(false);
            
            UpdateCraftingControls();
            UpdateMaterialsList(); // Refresh materials display
        }
    }
    
    private void OnCraftingCancelled(CraftingCancelledEvent evt)
    {
        if (progressSection != null)
            progressSection.SetActive(false);
        
        UpdateCraftingControls();
    }
}
