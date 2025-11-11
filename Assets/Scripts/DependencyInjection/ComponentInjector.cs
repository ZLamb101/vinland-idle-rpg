using UnityEngine;

/// <summary>
/// Utilities for dependency injection without FindAnyObjectByType
/// </summary>
public static class ComponentInjector
{
    /// <summary>
    /// Try to get from service locator first, fallback to FindAnyObjectByType
    /// </summary>
    public static T GetOrFind<T>() where T : class
    {
        // Try service locator first
        if (Services.TryGet<T>(out T service))
        {
            return service;
        }
        
        // Fallback to finding in scene
        if (typeof(T).IsSubclassOf(typeof(MonoBehaviour)))
        {
            return Object.FindAnyObjectByType(typeof(T)) as T;
        }
        
        return null;
    }
    
    /// <summary>
    /// Manually inject a dependency into a component
    /// </summary>
    public static void Inject<T>(MonoBehaviour target, string fieldName, T dependency)
    {
        var field = target.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance);
            
        if (field != null)
        {
            field.SetValue(target, dependency);
        }
        else
        {
            Debug.LogWarning($"[ComponentInjector] Field '{fieldName}' not found on {target.GetType().Name}");
        }
    }
}

/// <summary>
/// Base class for MonoBehaviours that need dependency injection
/// Provides helper methods for getting services
/// </summary>
public abstract class Injectable : MonoBehaviour
{
    /// <summary>
    /// Get a service from the service locator or find it in the scene
    /// </summary>
    protected T GetService<T>() where T : class
    {
        return ComponentInjector.GetOrFind<T>();
    }
    
    /// <summary>
    /// Require a service - logs error if not found
    /// </summary>
    protected T RequireService<T>() where T : class
    {
        T service = GetService<T>();
        
        if (service == null)
        {
            Debug.LogError($"[{GetType().Name}] Required service {typeof(T).Name} not found!");
        }
        
        return service;
    }
    
    /// <summary>
    /// Try to get a service without logging errors
    /// </summary>
    protected bool TryGetService<T>(out T service) where T : class
    {
        service = GetService<T>();
        return service != null;
    }
}

