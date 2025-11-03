using System;
using UnityEngine;

/// <summary>
/// Singleton manager for the resource gathering system.
/// Handles gathering state, progress, and item generation.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }
    
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
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
            Debug.LogWarning("ResourceManager: Cannot start gathering - resource is null!");
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
        
        OnGatheringStateChanged?.Invoke(isGathering);
        OnResourceChanged?.Invoke(currentResource);
        OnGatherProgressChanged?.Invoke(0f);
        
        Debug.Log($"Started gathering {currentResource.resourceName} at {currentResource.gatherRate} items/second");
        return true;
    }
    
    /// <summary>
    /// Start gathering a resource from the current zone (for backwards compatibility)
    /// </summary>
    public bool StartGathering(ZoneData zone)
    {
        if (zone == null)
        {
            Debug.LogWarning("ResourceManager: Cannot start gathering - zone is null!");
            return false;
        }
        
        ResourceData resource = zone.GetResource();
        
        if (resource == null)
        {
            Debug.LogWarning($"ResourceManager: Zone {zone.zoneName} has no resource to gather!");
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
        
        isGathering = false;
        gatherProgress = 0f;
        gatherTimer = 0f;
        currentResource = null;
        
        OnGatheringStateChanged?.Invoke(isGathering);
        OnGatherProgressChanged?.Invoke(0f);
        
        Debug.Log("Stopped gathering resources");
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
            Debug.LogWarning("ResourceManager: Cannot gather - resource or gathered item is null!");
            return;
        }
        
        // Add items to inventory
        if (CharacterManager.Instance != null)
        {
            InventoryItem items = currentResource.gatheredItem.CreateInventoryItem(currentResource.itemsPerGather);
            CharacterManager.Instance.AddItemToInventory(items);
            
            OnItemsGathered?.Invoke(currentResource.itemsPerGather);
            Debug.Log($"Gathered {currentResource.itemsPerGather}x {currentResource.gatheredItem.itemName}");
        }
    }
    
    // Getters
    public bool IsGathering() => isGathering;
    public ResourceData GetCurrentResource() => currentResource;
    public float GetGatherProgress() => gatherProgress;
    public float GetGatherRate() => currentResource != null ? currentResource.gatherRate : 0f;
}

