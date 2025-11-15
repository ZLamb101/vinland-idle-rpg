using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Enhanced dependency injection utilities for large games.
/// Provides clear separation between services and scene components with performance caching.
/// </summary>
public static class ComponentInjector
{
    // Cache for scene components to avoid expensive repeated searches
    private static Dictionary<Type, object> componentCache = new Dictionary<Type, object>();
    private static bool isInitialized = false;
    
    /// <summary>
    /// Initialize the ComponentInjector (automatic on first use)
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        if (isInitialized) return;
        
        // Clear cache when scenes change
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        
        isInitialized = true;
    }
    
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Clear cache when a new scene loads
        ClearCache();
    }
    
    static void OnSceneUnloaded(Scene scene)
    {
        // Clear cache when a scene unloads
        ClearCache();
    }
    
    /// <summary>
    /// Get a registered service from the Services locator.
    /// Services are persistent managers that should always exist.
    /// </summary>
    /// <typeparam name="T">Service interface type</typeparam>
    /// <returns>The service, or null if not registered</returns>
    public static T GetService<T>() where T : class
    {
        return Services.Get<T>();
    }
    
    /// <summary>
    /// Find a scene component with caching for performance.
    /// Use this for UI components, visuals, etc. that exist in the scene hierarchy.
    /// </summary>
    /// <typeparam name="T">Component type (must be MonoBehaviour)</typeparam>
    /// <param name="useCache">Whether to use cached result (default: true)</param>
    /// <returns>The component, or null if not found</returns>
    public static T FindComponent<T>(bool useCache = true) where T : MonoBehaviour
    {
        Type type = typeof(T);
        
        // Check cache first
        if (useCache && componentCache.TryGetValue(type, out object cached))
        {
            // Verify cached component still exists (might have been destroyed)
            if (cached != null && (cached as MonoBehaviour) != null)
            {
                return cached as T;
            }
            else
            {
                // Remove stale cache entry
                componentCache.Remove(type);
            }
        }
        
        // Find in scene (expensive operation)
        T component = UnityEngine.Object.FindAnyObjectByType<T>();
        
        // Cache for future use
        if (component != null && useCache)
        {
            componentCache[type] = component;
        }
        
        return component;
    }
    
    /// <summary>
    /// Clear the component cache.
    /// Automatically called on scene changes, but can be called manually if needed.
    /// </summary>
    public static void ClearCache()
    {
        componentCache.Clear();
    }
    
    /// <summary>
    /// Manually inject a dependency into a component using reflection.
    /// Useful for testing or dynamic dependency injection.
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
/// Base class for MonoBehaviours that need dependency injection.
/// Provides helper methods for getting services and finding components.
/// </summary>
public abstract class Injectable : MonoBehaviour
{
    /// <summary>
    /// Get a registered service from the service locator
    /// </summary>
    protected T GetService<T>() where T : class
    {
        return ComponentInjector.GetService<T>();
    }
    
    /// <summary>
    /// Find a scene component (with caching)
    /// </summary>
    protected T FindComponent<T>() where T : MonoBehaviour
    {
        return ComponentInjector.FindComponent<T>();
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
        return Services.TryGet<T>(out service);
    }
}

