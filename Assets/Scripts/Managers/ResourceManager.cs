using System;
using UnityEngine;

/// <summary>
/// Singleton manager for the resource gathering system.
/// Handles gathering state, progress, and item generation.
/// </summary>
public class ResourceManager : MonoBehaviour, IResourceService
{
    private static ResourceManager instance;
    
    [System.Obsolete("Use Services.Get<IResourceService>() instead. Direct Instance access is deprecated.", true)]
    public static ResourceManager Instance => instance;
    
    [Header("Gathering State")]
    private bool isGathering = false;
    private ResourceData currentResource;
    
    [Header("Gathering Progress")]
    private float gatherProgress = 0f; // 0 to 1
    private float gatherTimer = 0f;
    private float timePerGather = 1f; // Time in seconds to complete one gather cycle
    
    // Events
    public event Action<bool> OnGatheringStateChanged; // bool = isGathering
    public event Action<ResourceData> OnResourceChanged; // When resource changes
    public event Action<float> OnGatherProgressChanged; // 0 to 1
    public event Action<int> OnItemsGathered; // Amount of items gathered
    
    private ICharacterService characterService;
    private IAwayActivityService awayActivityService;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Register with service locator
        Services.Register<IResourceService>(this);
    }

    void Start()
    {
        characterService = Services.Get<ICharacterService>();
        awayActivityService = Services.Get<IAwayActivityService>();
    }

    void OnDestroy()
    {
        // Only unregister if we're the actual instance
        if (instance == this)
        {
            Services.Unregister<IResourceService>();
            instance = null;
        }
    }
    
    void Update()
    {
        if (isGathering && currentResource != null)
        {
            UpdateGathering();
        }
    }
    
    /// <summary>
    /// Start gathering a specific resource
    /// </summary>
    public bool StartGathering(ResourceData resource)
    {
        if (resource == null)
        {
            return false;
        }
        
        // Stop any current gathering
        StopGathering();
        
        currentResource = resource;
        isGathering = true;
        
        // Calculate time per gather cycle (inverse of gather rate)
        timePerGather = 1f / currentResource.gatherRate;
        gatherProgress = 0f;
        gatherTimer = 0f;
        
        // Register activity with AwayActivityManager
        if (awayActivityService != null)
        {
            awayActivityService.StartMining(resource);
            
            // Save activity immediately so it shows on character screen
            awayActivityService.SaveAwayState();
        }
        
        OnGatheringStateChanged?.Invoke(isGathering);
        OnResourceChanged?.Invoke(currentResource);
        OnGatherProgressChanged?.Invoke(0f);
        return true;
    }
    
    /// <summary>
    /// Start gathering a resource from the current zone (for backwards compatibility)
    /// </summary>
    public bool StartGathering(ZoneData zone)
    {
        if (zone == null)
        {
            return false;
        }
        
        ResourceData resource = zone.GetResource();
        
        if (resource == null)
        {
            return false;
        }
        
        return StartGathering(resource);
    }
    
    /// <summary>
    /// Stop gathering resources
    /// </summary>
    public void StopGathering()
    {
        if (!isGathering) return;
        
        // Save activity state BEFORE stopping (so we save the mining activity, not "None")
        if (awayActivityService != null)
        {
            awayActivityService.SaveAwayState();
        }
        
        isGathering = false;
        gatherProgress = 0f;
        gatherTimer = 0f;
        currentResource = null;
        
        // Stop tracking activity in AwayActivityManager (after saving)
        if (awayActivityService != null)
        {
            awayActivityService.StopActivity();
        }
        
        OnGatheringStateChanged?.Invoke(isGathering);
        OnGatherProgressChanged?.Invoke(0f);
    }
    
    void UpdateGathering()
    {
        gatherTimer += Time.deltaTime;
        gatherProgress = Mathf.Clamp01(gatherTimer / timePerGather);
        
        OnGatherProgressChanged?.Invoke(gatherProgress);
        
        // When gather cycle completes
        if (gatherProgress >= 1f)
        {
            GatherItems();
            
            // Reset for next cycle
            gatherTimer = 0f;
            gatherProgress = 0f;
            OnGatherProgressChanged?.Invoke(0f);
        }
    }
    
    void GatherItems()
    {
        if (currentResource == null || currentResource.gatheredItem == null)
        {
            return;
        }
        
        // Add items to inventory
        if (characterService != null)
        {
            InventoryItem items = currentResource.gatheredItem.CreateInventoryItem(currentResource.itemsPerGather);
            characterService.AddItemToInventory(items);
            
            OnItemsGathered?.Invoke(currentResource.itemsPerGather);
        }
    }
    
    // Getters
    public bool IsGathering() => isGathering;
    public ResourceData GetCurrentResource() => currentResource;
    public float GetGatherProgress() => gatherProgress;
    public float GetGatherRate() => currentResource != null ? currentResource.gatherRate : 0f;
}

