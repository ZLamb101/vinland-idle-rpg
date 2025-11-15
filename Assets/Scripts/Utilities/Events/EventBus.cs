using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized event bus for game-wide event communication
/// Decouples systems by allowing publish/subscribe pattern
/// </summary>
public static class EventBus
{
    // Dictionary of event type -> list of handlers
    private static Dictionary<Type, List<Delegate>> subscribers = new Dictionary<Type, List<Delegate>>();
    
    // For debugging - track event counts
    private static Dictionary<Type, int> eventCounts = new Dictionary<Type, int>();
    
    // Enable/disable debug logging
    public static bool EnableDebugLogging = false;
    
    /// <summary>
    /// Subscribe to an event type
    /// </summary>
    public static void Subscribe<T>(Action<T> handler) where T : GameEvent
    {
        Type eventType = typeof(T);
        
        if (!subscribers.ContainsKey(eventType))
        {
            subscribers[eventType] = new List<Delegate>();
        }
        
        // Check for duplicate subscription
        if (subscribers[eventType].Contains(handler))
        {
            Debug.LogWarning($"[EventBus] Handler already subscribed to {eventType.Name}");
            return;
        }
        
        subscribers[eventType].Add(handler);
        
        if (EnableDebugLogging)
        {
            Debug.Log($"[EventBus] Subscribed to {eventType.Name} (Total subscribers: {subscribers[eventType].Count})");
        }
    }
    
    /// <summary>
    /// Unsubscribe from an event type
    /// </summary>
    public static void Unsubscribe<T>(Action<T> handler) where T : GameEvent
    {
        Type eventType = typeof(T);
        
        if (!subscribers.ContainsKey(eventType))
        {
            return;
        }
        
        subscribers[eventType].Remove(handler);
        
        if (EnableDebugLogging)
        {
            Debug.Log($"[EventBus] Unsubscribed from {eventType.Name} (Remaining subscribers: {subscribers[eventType].Count})");
        }
        
        // Clean up empty lists
        if (subscribers[eventType].Count == 0)
        {
            subscribers.Remove(eventType);
        }
    }
    
    /// <summary>
    /// Publish an event to all subscribers
    /// </summary>
    public static void Publish<T>(T eventData) where T : GameEvent
    {
        Type eventType = typeof(T);
        
        // Track event counts
        if (!eventCounts.ContainsKey(eventType))
        {
            eventCounts[eventType] = 0;
        }
        eventCounts[eventType]++;
        
        if (EnableDebugLogging)
        {
            Debug.Log($"[EventBus] Publishing {eventType.Name} (Count: {eventCounts[eventType]})");
        }
        
        if (!subscribers.ContainsKey(eventType))
        {
            if (EnableDebugLogging)
            {
                Debug.Log($"[EventBus] No subscribers for {eventType.Name}");
            }
            return;
        }
        
        // Create a copy of subscribers list to avoid modification during iteration
        List<Delegate> handlers = new List<Delegate>(subscribers[eventType]);
        
        // Invoke all handlers
        foreach (Delegate handler in handlers)
        {
            try
            {
                (handler as Action<T>)?.Invoke(eventData);
            }
            catch (Exception e)
            {
                Debug.LogError($"[EventBus] Error invoking handler for {eventType.Name}: {e.Message}\n{e.StackTrace}");
            }
        }
    }
    
    /// <summary>
    /// Clear all subscriptions (useful for scene transitions or testing)
    /// </summary>
    public static void Clear()
    {
        int totalSubscribers = 0;
        foreach (var kvp in subscribers)
        {
            totalSubscribers += kvp.Value.Count;
        }
        
        Debug.Log($"[EventBus] Clearing all subscriptions ({totalSubscribers} total handlers)");
        subscribers.Clear();
    }
    
    /// <summary>
    /// Clear subscriptions for a specific event type
    /// </summary>
    public static void Clear<T>() where T : GameEvent
    {
        Type eventType = typeof(T);
        
        if (subscribers.ContainsKey(eventType))
        {
            int count = subscribers[eventType].Count;
            subscribers.Remove(eventType);
            Debug.Log($"[EventBus] Cleared {count} subscriptions for {eventType.Name}");
        }
    }
    
    /// <summary>
    /// Get subscriber count for an event type
    /// </summary>
    public static int GetSubscriberCount<T>() where T : GameEvent
    {
        Type eventType = typeof(T);
        return subscribers.ContainsKey(eventType) ? subscribers[eventType].Count : 0;
    }
    
    /// <summary>
    /// Check if any subscribers exist for an event type
    /// </summary>
    public static bool HasSubscribers<T>() where T : GameEvent
    {
        return GetSubscriberCount<T>() > 0;
    }
    
    /// <summary>
    /// Get statistics about event usage (for debugging)
    /// </summary>
    public static Dictionary<string, int> GetEventStatistics()
    {
        Dictionary<string, int> stats = new Dictionary<string, int>();
        
        foreach (var kvp in eventCounts)
        {
            stats[kvp.Key.Name] = kvp.Value;
        }
        
        return stats;
    }
    
    /// <summary>
    /// Reset event statistics
    /// </summary>
    public static void ResetStatistics()
    {
        eventCounts.Clear();
        Debug.Log("[EventBus] Event statistics reset");
    }
    
    /// <summary>
    /// Print debug information about the event bus
    /// </summary>
    public static void PrintDebugInfo()
    {
        Debug.Log("=== EventBus Debug Info ===");
        Debug.Log($"Total event types: {subscribers.Count}");
        
        foreach (var kvp in subscribers)
        {
            Debug.Log($"  {kvp.Key.Name}: {kvp.Value.Count} subscribers");
        }
        
        Debug.Log("\n=== Event Statistics ===");
        foreach (var kvp in eventCounts)
        {
            Debug.Log($"  {kvp.Key.Name}: {kvp.Value} published");
        }
    }
}

