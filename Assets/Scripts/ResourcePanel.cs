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
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        resourceService = Services.Get<IResourceService>();

        // Setup gather button
        if (gatherButton != null)
            gatherButton.onClick.AddListener(OnGatherClicked);
        
        // Subscribe to ResourceManager events
        if (resourceService != null)
        {
            resourceService.OnGatheringStateChanged += OnGatheringStateChanged;
            resourceService.OnResourceChanged += OnResourceChanged;
            resourceService.OnGatherProgressChanged += OnGatherProgressChanged;
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (resourceService != null)
        {
            resourceService.OnGatheringStateChanged -= OnGatheringStateChanged;
            resourceService.OnResourceChanged -= OnResourceChanged;
            resourceService.OnGatherProgressChanged -= OnGatherProgressChanged;
        }
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
    
    void OnGatheringStateChanged(bool gathering)
    {
        // Check if we're gathering THIS resource
        bool gatheringThisResource = gathering && resourceService != null && 
                                     resourceService.GetCurrentResource() == resourceData;
        
        isGathering = gatheringThisResource;
        UpdateGatherButtonState();
        
        // Only show progress for this resource
        if (progressSlider != null)
        {
            progressSlider.gameObject.SetActive(gatheringThisResource);
        }
    }
    
    void OnResourceChanged(ResourceData resource)
    {
        // Update state if this panel's resource changed
        bool gatheringThisResource = resource == resourceData && 
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
    
    void OnGatherProgressChanged(float progress)
    {
        // Only update progress if we're gathering THIS resource
        if (isGathering && resourceService != null && 
            resourceService.GetCurrentResource() == resourceData)
        {
            if (progressSlider != null)
            {
                progressSlider.value = progress;
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
}

