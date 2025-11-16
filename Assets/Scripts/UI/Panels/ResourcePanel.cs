using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component for displaying a single resource panel with image, name, and gather button.
/// This component is used as a prefab that can be instantiated multiple times.
/// </summary>
public class ResourcePanel : MonoBehaviour
{
    [Header("Resource Display")]
    public Image resourceImage; // Image showing the resource icon
    public TextMeshProUGUI resourceNameText; // Text showing resource name
    public TextMeshProUGUI resourceDetailsText; // Text showing gather rate info
    
    [Header("Gathering")]
    public Button gatherButton; // Button to start/stop gathering
    public TextMeshProUGUI gatherButtonText; // Text on the gather button
    public Slider progressSlider; // Progress bar for gathering
    
    private ResourceData resourceData;
    private RectTransform rectTransform;
    private bool isGathering = false;
    private IResourceService resourceService;
    private RectTransform canvasContainer; // Parent container for spawning visuals
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        resourceService = Services.Get<IResourceService>();

        // Find the canvas container for spawning visuals
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasContainer = canvas.GetComponent<RectTransform>();
        }

        // Setup gather button
        if (gatherButton != null)
            gatherButton.onClick.AddListener(OnGatherClicked);
        
        // Subscribe to ResourceManager events
        if (resourceService != null)
        {
            EventBus.Subscribe<GatheringStateChangedEvent>(OnGatheringStateChanged);
            EventBus.Subscribe<ResourceChangedEvent>(OnResourceChanged);
            EventBus.Subscribe<GatherProgressChangedEvent>(OnGatherProgressChanged);
            EventBus.Subscribe<ItemsGatheredEvent>(OnItemsGathered);
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        EventBus.Unsubscribe<GatheringStateChangedEvent>(OnGatheringStateChanged);
        EventBus.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
        EventBus.Unsubscribe<GatherProgressChangedEvent>(OnGatherProgressChanged);
        EventBus.Unsubscribe<ItemsGatheredEvent>(OnItemsGathered);
    }
    
    /// <summary>
    /// Initialize this resource panel with resource data and position
    /// </summary>
    public void Initialize(ResourceData resource, Vector2 position)
    {
        resourceData = resource;
        
        if (resource == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        // Set position (use absolute pixel coordinates directly)
        if (rectTransform != null)
        {
            // Use position directly as anchored position (absolute pixel coordinates)
            rectTransform.anchoredPosition = position;
        }
        
        // Update resource image
        if (resourceImage != null)
        {
            resourceImage.gameObject.SetActive(true);
            if (resource.resourceIcon != null)
            {
                resourceImage.sprite = resource.resourceIcon;
            }
        }
        
        // Update resource name text
        if (resourceNameText != null)
        {
            resourceNameText.text = resource.resourceName;
            resourceNameText.gameObject.SetActive(true);
        }
        
        // Update resource details text
        if (resourceDetailsText != null)
        {
            resourceDetailsText.text = $"{resource.gatherRate:F1}/sec";
            resourceDetailsText.gameObject.SetActive(true);
        }
        
        // Show/hide gather button
        if (gatherButton != null)
        {
            gatherButton.gameObject.SetActive(true);
        }
        
        // Show/hide progress slider
        if (progressSlider != null)
        {
            progressSlider.gameObject.SetActive(true);
            progressSlider.value = 0f;
        }
        
        // Update initial state
        UpdateGatherButtonState();
        
        gameObject.SetActive(true);
    }
    
    void OnGatherClicked()
    {
        if (resourceData == null)
        {
            return;
        }
        
        if (resourceService == null)
        {
            return;
        }
        
        // Toggle gathering for this specific resource
        if (isGathering && resourceService.GetCurrentResource() == resourceData)
        {
            // Currently gathering this resource - stop
            resourceService.StopGathering();
        }
        else
        {
            // Not gathering or gathering different resource - start gathering this one
            resourceService.StartGathering(resourceData);
        }
    }
    
    void OnGatheringStateChanged(GatheringStateChangedEvent e)
    {
        // Check if we're gathering THIS resource
        bool gatheringThisResource = e.isGathering && resourceService != null && 
                                     resourceService.GetCurrentResource() == resourceData;
        
        isGathering = gatheringThisResource;
        UpdateGatherButtonState();
        
        // Only show progress for this resource
        if (progressSlider != null)
        {
            progressSlider.gameObject.SetActive(gatheringThisResource);
        }
    }
    
    void OnResourceChanged(ResourceChangedEvent e)
    {
        // Update state if this panel's resource changed
        bool gatheringThisResource = e.resource == resourceData && 
                                     resourceService != null && 
                                     resourceService.IsGathering();
        
        isGathering = gatheringThisResource;
        UpdateGatherButtonState();
        
        // Only show progress for this resource
        if (progressSlider != null)
        {
            progressSlider.gameObject.SetActive(gatheringThisResource);
            if (!gatheringThisResource)
            {
                progressSlider.value = 0f;
            }
        }
    }
    
    void OnGatherProgressChanged(GatherProgressChangedEvent e)
    {
        // Only update progress if we're gathering THIS resource
        if (isGathering && resourceService != null && 
            resourceService.GetCurrentResource() == resourceData)
        {
            if (progressSlider != null)
            {
                progressSlider.value = e.progress;
            }
        }
    }
    
    void UpdateGatherButtonState()
    {
        if (gatherButtonText == null) return;
        
        if (isGathering)
        {
            gatherButtonText.text = "Stop Gather";
        }
        else
        {
            gatherButtonText.text = "Gather";
        }
    }
    
    /// <summary>
    /// Handle ItemsGathered event and spawn visual feedback
    /// </summary>
    void OnItemsGathered(ItemsGatheredEvent e)
    {
        // Only show visual if this panel's resource was gathered
        if (e.resource != resourceData)
        {
            return;
        }
        
        // Ensure we have the gathered item data
        if (e.resource == null || e.resource.gatheredItem == null || e.resource.gatheredItem.icon == null)
        {
            return;
        }
        
        SpawnGatherVisual(e.resource.gatheredItem.icon, e.itemsGathered);
    }
    
    /// <summary>
    /// Spawn a floating visual that shows the gathered item rising up
    /// </summary>
    void SpawnGatherVisual(Sprite itemIcon, int quantity)
    {
        // Ensure we have required components
        if (canvasContainer == null || rectTransform == null)
        {
            return;
        }
        
        // Calculate spawn position near the top of the resource panel
        Vector2 spawnPosition = CalculateSpawnPosition();
        
        // Create item drop visual GameObject
        GameObject dropObj = new GameObject($"ResourceGatherVisual_{resourceData.resourceName}");
        dropObj.transform.SetParent(canvasContainer);
        
        // Add RectTransform
        RectTransform dropRect = dropObj.AddComponent<RectTransform>();
        dropRect.sizeDelta = new Vector2(64f, 64f); // Standard item icon size
        dropRect.anchoredPosition = spawnPosition;
        
        // Add CanvasGroup for fade effect
        CanvasGroup canvasGroup = dropObj.AddComponent<CanvasGroup>();
        
        // Add Image for item icon
        Image iconImage = dropObj.AddComponent<Image>();
        iconImage.sprite = itemIcon;
        iconImage.preserveAspect = true;
        
        // Add quantity text if quantity > 1
        TextMeshProUGUI quantityText = null;
        if (quantity > 1)
        {
            GameObject textObj = new GameObject("QuantityText");
            textObj.transform.SetParent(dropObj.transform);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.6f, 0.6f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            quantityText = textObj.AddComponent<TextMeshProUGUI>();
            quantityText.text = quantity.ToString();
            quantityText.fontSize = 16;
            quantityText.color = Color.white;
            quantityText.alignment = TextAlignmentOptions.BottomRight;
            quantityText.fontStyle = FontStyles.Bold;
            
            // Add outline for better visibility
            var outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1f, -1f);
        }
        
        // Add ItemDropVisual component
        ItemDropVisual dropVisual = dropObj.AddComponent<ItemDropVisual>();
        dropVisual.itemIcon = iconImage;
        dropVisual.rectTransform = dropRect;
        dropVisual.canvasGroup = canvasGroup;
        dropVisual.quantityText = quantityText;
        
        // Setup and start animation
        dropVisual.Setup(itemIcon, quantity, spawnPosition);
    }
    
    /// <summary>
    /// Calculate the spawn position for the gather visual near the top of the resource panel
    /// </summary>
    Vector2 CalculateSpawnPosition()
    {
        // Get the world position of the resource panel
        Vector3 panelWorldPos = rectTransform.position;
        
        // Get the size of the panel
        Vector2 panelSize = rectTransform.rect.size;
        
        // Offset to spawn near the top center of the panel
        Vector3 spawnWorldPos = panelWorldPos + new Vector3(0, panelSize.y * 0.4f, 0);
        
        // Convert world position to local position in canvas container
        Canvas canvas = canvasContainer.GetComponentInParent<Canvas>();
        Camera uiCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }
        
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasContainer,
            RectTransformUtility.WorldToScreenPoint(uiCamera, spawnWorldPos),
            uiCamera,
            out localPoint))
        {
            return localPoint;
        }
        
        // Fallback: use panel's anchored position with Y offset
        return rectTransform.anchoredPosition + new Vector2(0, panelSize.y * 0.4f);
    }
}

