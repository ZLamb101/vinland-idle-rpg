using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for MonoBehaviours that need to subscribe to events
/// Automatically handles subscription/unsubscription on Enable/Disable
/// </summary>
public abstract class EventSubscriber : MonoBehaviour
{
    private List<Action> unsubscribeActions = new List<Action>();
    
    /// <summary>
    /// Called when the component is enabled
    /// Override this to subscribe to events
    /// </summary>
    protected virtual void OnEnable()
    {
        // Override in derived classes
    }
    
    /// <summary>
    /// Called when the component is disabled
    /// Automatically unsubscribes from all events
    /// </summary>
    protected virtual void OnDisable()
    {
        UnsubscribeAll();
    }
    
    /// <summary>
    /// Subscribe to an event and track it for automatic cleanup
    /// </summary>
    protected void Subscribe<T>(Action<T> handler) where T : GameEvent
    {
        EventBus.Subscribe(handler);
        
        // Store unsubscribe action
        unsubscribeActions.Add(() => EventBus.Unsubscribe(handler));
    }
    
    /// <summary>
    /// Manually unsubscribe from all tracked events
    /// </summary>
    protected void UnsubscribeAll()
    {
        foreach (var unsubscribe in unsubscribeActions)
        {
            try
            {
                unsubscribe?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[EventSubscriber] Error unsubscribing: {e.Message}");
            }
        }
        
        unsubscribeActions.Clear();
    }
}

/// <summary>
/// Example usage of EventSubscriber
/// </summary>
public class ExampleEventSubscriber : EventSubscriber
{
    protected override void OnEnable()
    {
        base.OnEnable();
        
        // Subscribe to events using the helper method
        Subscribe<CharacterLevelUpEvent>(OnLevelUp);
        Subscribe<ItemAddedEvent>(OnItemAdded);
    }
    
    private void OnLevelUp(CharacterLevelUpEvent e)
    {
        Debug.Log($"Level up! {e.oldLevel} -> {e.newLevel}");
    }
    
    private void OnItemAdded(ItemAddedEvent e)
    {
        Debug.Log($"Item added: {e.item.itemName} x{e.quantity}");
    }
}

