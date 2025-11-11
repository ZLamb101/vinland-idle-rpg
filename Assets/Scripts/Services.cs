using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Service Locator pattern for dependency injection.
/// Provides centralized access to game services without static singletons.
/// </summary>
public static class Services
{
    private static Dictionary<Type, object> services = new Dictionary<Type, object>();
    
    /// <summary>
    /// Register a service implementation
    /// </summary>
    public static void Register<T>(T service) where T : class
    {
        Type type = typeof(T);
        
        if (services.ContainsKey(type))
        {
            Debug.LogWarning($"[Services] Service {type.Name} is already registered. Replacing with new instance.");
        }
        
        services[type] = service;
        Debug.Log($"[Services] Registered service: {type.Name}");
    }
    
    /// <summary>
    /// Get a registered service
    /// </summary>
    public static T Get<T>() where T : class
    {
        Type type = typeof(T);
        
        if (services.TryGetValue(type, out object service))
        {
            return service as T;
        }
        
        Debug.LogError($"[Services] Service {type.Name} not found. Did you forget to register it?");
        return null;
    }
    
    /// <summary>
    /// Try to get a service without logging errors
    /// </summary>
    public static bool TryGet<T>(out T service) where T : class
    {
        Type type = typeof(T);
        
        if (services.TryGetValue(type, out object serviceObj))
        {
            service = serviceObj as T;
            return service != null;
        }
        
        service = null;
        return false;
    }
    
    /// <summary>
    /// Check if a service is registered
    /// </summary>
    public static bool IsRegistered<T>() where T : class
    {
        return services.ContainsKey(typeof(T));
    }
    
    /// <summary>
    /// Unregister a service
    /// </summary>
    public static void Unregister<T>() where T : class
    {
        Type type = typeof(T);
        
        if (services.Remove(type))
        {
            Debug.Log($"[Services] Unregistered service: {type.Name}");
        }
        else
        {
            Debug.LogWarning($"[Services] Attempted to unregister service {type.Name}, but it wasn't registered.");
        }
    }
    
    /// <summary>
    /// Clear all registered services (useful for scene transitions or testing)
    /// </summary>
    public static void Clear()
    {
        Debug.Log($"[Services] Clearing all {services.Count} registered services");
        services.Clear();
    }
    
    /// <summary>
    /// Get all registered service types (for debugging)
    /// </summary>
    public static IEnumerable<Type> GetRegisteredTypes()
    {
        return services.Keys;
    }
}

