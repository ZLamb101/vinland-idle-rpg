using System;
using UnityEngine;

/// <summary>
/// Singleton manager for the resource gathering system.
/// Handles gathering state, progress, and item generation.
/// </summary>
public class ResourceManager : MonoBehaviour, IResourceService
{
    
    [Header("Gathering State")]
    private bool isGathering = false;
    private ResourceData currentResource;
    
    [Header("Gathering Progress")]
    private float gatherProgress = 0f; // 0 to 1
    private float gatherTimer = 0f;
    private float timePerGather = 1f; // Time in seconds to complete one gather cycle
    
    // Events migrated to EventBus - see GameEvent.cs for event types
    
    private ICharacterService characterService;
    private IAwayActivityService awayActivityService;
    private IProfessionService professionService;

    void Awake()
    {
        if (Services.IsRegistered<IResourceService>())
        {
            Debug.LogWarning("[ResourceManager] Service already registered. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        
        // Register with service locator
        Services.Register<IResourceService>(this);
    }

    void Start()
    {
        characterService = Services.Get<ICharacterService>();
        awayActivityService = Services.Get<IAwayActivityService>();
        
        // ProfessionService may not be available immediately if ProfessionManager hasn't loaded yet
        if (Services.TryGet<IProfessionService>(out var profService))
        {
            professionService = profService;
        }
    }

    void OnDestroy()
    {
        Services.Unregister<IResourceService>();
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
        
        // Check profession level requirement
        if (!CanGatherResource(resource))
        {
            Debug.LogWarning($"[ResourceManager] Cannot gather {resource.resourceName}. Requires {resource.professionType.GetDisplayName()} level {resource.professionLevelRequired}.");
            return false;
        }
        
        // Stop combat if player is currently in combat
        ICombatService combatService;
        if (Services.TryGet<ICombatService>(out combatService) && 
            combatService.GetCombatState() == CombatManager.CombatState.Fighting)
        {
            combatService.EndCombat();
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
        
        EventBus.Publish(new GatheringStateChangedEvent { isGathering = isGathering, resource = currentResource });
        EventBus.Publish(new ResourceChangedEvent { resource = currentResource });
        EventBus.Publish(new GatherProgressChangedEvent { progress = 0f });
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
        
        EventBus.Publish(new GatheringStateChangedEvent { isGathering = isGathering, resource = currentResource });
        EventBus.Publish(new GatherProgressChangedEvent { progress = 0f });
    }
    
    void UpdateGathering()
    {
        gatherTimer += Time.deltaTime;
        gatherProgress = Mathf.Clamp01(gatherTimer / timePerGather);
        
        EventBus.Publish(new GatherProgressChangedEvent { progress = gatherProgress });
        
        // When gather cycle completes
        if (gatherProgress >= 1f)
        {
            GatherItems();
            
            // Reset for next cycle
            gatherTimer = 0f;
            gatherProgress = 0f;
            EventBus.Publish(new GatherProgressChangedEvent { progress = 0f });
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
            
            EventBus.Publish(new ItemsGatheredEvent { itemsGathered = currentResource.itemsPerGather, resource = currentResource });
        }
        
        // Grant profession XP
        if (professionService == null)
        {
            Services.TryGet<IProfessionService>(out professionService);
        }
        
        if (professionService != null && currentResource.professionXPReward > 0)
        {
            professionService.AddProfessionXP(currentResource.professionType, currentResource.professionXPReward);
        }
    }
    
    /// <summary>
    /// Check if the player meets the requirements to gather a resource
    /// </summary>
    public bool CanGatherResource(ResourceData resource)
    {
        if (resource == null)
        {
            return false;
        }
        
        // Check profession level requirement
        if (professionService == null)
        {
            Services.TryGet<IProfessionService>(out professionService);
        }
        
        if (professionService != null)
        {
            int playerProfessionLevel = professionService.GetProfessionLevel(resource.professionType);
            if (playerProfessionLevel < resource.professionLevelRequired)
            {
                return false;
            }
        }
        
        return true;
    }
    
    // Getters
    public bool IsGathering() => isGathering;
    public ResourceData GetCurrentResource() => currentResource;
    public float GetGatherProgress() => gatherProgress;
    public float GetGatherRate() => currentResource != null ? currentResource.gatherRate : 0f;
}

