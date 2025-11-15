using UnityEngine;

/// <summary>
/// Helper class to ease migration from singleton pattern to service locator
/// Provides fallback logic: try service locator first, fall back to Instance pattern
/// 
/// PURPOSE:
/// During migration, some code may access services before they're registered.
/// This helper provides a transition path that works with both old and new patterns.
/// 
/// USAGE:
/// Instead of: CharacterManager.Instance.AddGold(10);
/// Use: ServiceMigrationHelper.GetCharacterService()?.AddGold(10);
/// 
/// FUTURE:
/// Once all managers are properly registered, this can be removed and
/// replaced with direct Services.Get<T>() calls.
/// </summary>
public static class ServiceMigrationHelper
{
    /// <summary>
    /// Get character service with fallback to singleton Instance
    /// </summary>
    public static ICharacterService GetCharacterService()
    {
        // Try service locator first (new way)
        if (Services.TryGet<ICharacterService>(out var service))
        {
            return service;
        }
        
        // Fallback to singleton (old way)
        if (CharacterManager.Instance != null)
        {
            // Note: This works because CharacterManager implements ICharacterService
            return CharacterManager.Instance;
        }
        
        Debug.LogWarning("[ServiceMigrationHelper] CharacterService not found via Services or Instance");
        return null;
    }
    
    /// <summary>
    /// Get equipment service with fallback to singleton Instance
    /// </summary>
    public static IEquipmentService GetEquipmentService()
    {
        if (Services.TryGet<IEquipmentService>(out var service))
        {
            return service;
        }
        
        if (EquipmentManager.Instance != null)
        {
            return EquipmentManager.Instance;
        }
        
        Debug.LogWarning("[ServiceMigrationHelper] EquipmentService not found via Services or Instance");
        return null;
    }
    
    /// <summary>
    /// Get combat service with fallback to singleton Instance
    /// </summary>
    public static ICombatService GetCombatService()
    {
        if (Services.TryGet<ICombatService>(out var service))
        {
            return service;
        }
        
        if (CombatManager.Instance != null)
        {
            return CombatManager.Instance;
        }
        
        Debug.LogWarning("[ServiceMigrationHelper] CombatService not found via Services or Instance");
        return null;
    }
    
    /// <summary>
    /// Get talent service with fallback to singleton Instance
    /// </summary>
    public static ITalentService GetTalentService()
    {
        if (Services.TryGet<ITalentService>(out var service))
        {
            return service;
        }
        
        if (TalentManager.Instance != null)
        {
            return TalentManager.Instance;
        }
        
        Debug.LogWarning("[ServiceMigrationHelper] TalentService not found via Services or Instance");
        return null;
    }
    
    /// <summary>
    /// Get resource service with fallback to singleton Instance
    /// </summary>
    public static IResourceService GetResourceService()
    {
        if (Services.TryGet<IResourceService>(out var service))
        {
            return service;
        }
        
        if (ResourceManager.Instance != null)
        {
            return ResourceManager.Instance;
        }
        
        Debug.LogWarning("[ServiceMigrationHelper] ResourceService not found via Services or Instance");
        return null;
    }
    
    /// <summary>
    /// Get shop service with fallback to singleton Instance
    /// </summary>
    public static IShopService GetShopService()
    {
        if (Services.TryGet<IShopService>(out var service))
        {
            return service;
        }
        
        if (ShopManager.Instance != null)
        {
            return ShopManager.Instance;
        }
        
        Debug.LogWarning("[ServiceMigrationHelper] ShopService not found via Services or Instance");
        return null;
    }
    
    /// <summary>
    /// Get zone service with fallback to singleton Instance
    /// </summary>
    public static IZoneService GetZoneService()
    {
        if (Services.TryGet<IZoneService>(out var service))
        {
            return service;
        }
        
        if (ZoneManager.Instance != null)
        {
            return ZoneManager.Instance;
        }
        
        Debug.LogWarning("[ServiceMigrationHelper] ZoneService not found via Services or Instance");
        return null;
    }
    
    /// <summary>
    /// Get away activity service with fallback to singleton Instance
    /// </summary>
    public static IAwayActivityService GetAwayActivityService()
    {
        if (Services.TryGet<IAwayActivityService>(out var service))
        {
            return service;
        }
        
        if (AwayActivityManager.Instance != null)
        {
            return AwayActivityManager.Instance;
        }
        
        Debug.LogWarning("[ServiceMigrationHelper] AwayActivityService not found via Services or Instance");
        return null;
    }
    
    /// <summary>
    /// Get game log service with fallback to singleton Instance
    /// </summary>
    public static IGameLogService GetGameLogService()
    {
        if (Services.TryGet<IGameLogService>(out var service))
        {
            return service;
        }
        
        if (GameLog.Instance != null)
        {
            return GameLog.Instance;
        }
        
        Debug.LogWarning("[ServiceMigrationHelper] GameLogService not found via Services or Instance");
        return null;
    }
    
    /// <summary>
    /// Check if a service is available (either via Services or Instance)
    /// </summary>
    public static bool IsServiceAvailable<T>() where T : class
    {
        if (Services.IsRegistered<T>())
        {
            return true;
        }
        
        // Check singleton instances as fallback
        if (typeof(T) == typeof(ICharacterService))
            return CharacterManager.Instance != null;
        if (typeof(T) == typeof(IEquipmentService))
            return EquipmentManager.Instance != null;
        if (typeof(T) == typeof(ICombatService))
            return CombatManager.Instance != null;
        if (typeof(T) == typeof(ITalentService))
            return TalentManager.Instance != null;
        if (typeof(T) == typeof(IResourceService))
            return ResourceManager.Instance != null;
        if (typeof(T) == typeof(IShopService))
            return ShopManager.Instance != null;
        if (typeof(T) == typeof(IZoneService))
            return ZoneManager.Instance != null;
        if (typeof(T) == typeof(IAwayActivityService))
            return AwayActivityManager.Instance != null;
        if (typeof(T) == typeof(IGameLogService))
            return GameLog.Instance != null;
        
        return false;
    }
}

/*
 * MIGRATION GUIDE:
 * 
 * This helper is a temporary bridge during the migration from singleton to service locator.
 * 
 * CURRENT STATE (with this helper):
 * var service = ServiceMigrationHelper.GetCharacterService();
 * service?.AddGold(100);
 * 
 * FUTURE STATE (after full migration):
 * var service = Services.Get<ICharacterService>();
 * service?.AddGold(100);
 * 
 * Or even better with Injectable base class:
 * var service = GetService<ICharacterService>();
 * service?.AddGold(100);
 * 
 * WHY THIS EXISTS:
 * - Managers register themselves in Awake()
 * - Some code might run before Awake() completes
 * - This helper provides graceful fallback during initialization
 * - Once all code is migrated, this can be removed
 * 
 * WHEN TO REMOVE:
 * 1. All managers properly register in Awake()
 * 2. All code uses Services.Get<T>() or Injectable base class
 * 3. No timing issues with service registration
 * 4. All tests pass without fallback logic
 */

