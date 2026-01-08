using UnityEngine;

/// <summary>
/// Detects screen resolution changes and fires events to notify other systems.
/// Attach this component to a persistent GameObject in the scene (e.g., MainCanvas or GameManager).
/// </summary>
public class ResizeHandler : MonoBehaviour
{
    private int lastWidth;
    private int lastHeight;
    private bool isInitialized = false;
    
    void Start()
    {
        // Store initial screen size
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        isInitialized = true;
        
        Debug.Log($"[ResizeHandler] Initialized. Initial resolution: {lastWidth}x{lastHeight}");
    }
    
    void Update()
    {
        if (!isInitialized)
            return;
        
        // Check if screen size has changed
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            int oldWidth = lastWidth;
            int oldHeight = lastHeight;
            
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            
            Debug.Log($"[ResizeHandler] Screen resized from {oldWidth}x{oldHeight} to {lastWidth}x{lastHeight}");
            
            // Fire resize event
            EventBus.Publish(new ScreenResizedEvent(oldWidth, oldHeight, lastWidth, lastHeight));
        }
    }
    
    /// <summary>
    /// Get current screen dimensions
    /// </summary>
    public Vector2Int GetScreenSize()
    {
        return new Vector2Int(Screen.width, Screen.height);
    }
    
    /// <summary>
    /// Force a resize event (useful for testing or manual refresh)
    /// </summary>
    public void ForceResizeEvent()
    {
        if (isInitialized)
        {
            EventBus.Publish(new ScreenResizedEvent(lastWidth, lastHeight, Screen.width, Screen.height));
            lastWidth = Screen.width;
            lastHeight = Screen.height;
        }
    }
}


