using UnityEngine;

/// <summary>
/// Utility class for converting between normalized (0-1) positions and screen pixel positions.
/// Positions are calculated relative to a background RectTransform.
/// </summary>
public static class WorldPositionCalculator
{
    // Design resolution used for converting legacy absolute pixel positions
    public const float DESIGN_WIDTH = 1920f;
    public const float DESIGN_HEIGHT = 1080f;
    
    /// <summary>
    /// Convert an absolute pixel position (from design resolution) to a normalized position (0-1)
    /// </summary>
    /// <param name="pixelPosition">Absolute pixel position from design resolution</param>
    /// <returns>Normalized position (0-1) where (0,0) is bottom-left and (1,1) is top-right</returns>
    public static Vector2 PixelToNormalized(Vector2 pixelPosition)
    {
        // Convert from center-anchored coordinates to bottom-left anchored
        // Design resolution is 1920x1080, center is at (0, 0)
        // So we need to offset by half the design resolution
        float normalizedX = (pixelPosition.x + DESIGN_WIDTH / 2f) / DESIGN_WIDTH;
        float normalizedY = (pixelPosition.y + DESIGN_HEIGHT / 2f) / DESIGN_HEIGHT;
        
        return new Vector2(normalizedX, normalizedY);
    }
    
    /// <summary>
    /// Convert a normalized position (0-1) to screen pixel position relative to a background RectTransform
    /// </summary>
    /// <param name="normalizedPosition">Normalized position where (0,0) is bottom-left and (1,1) is top-right</param>
    /// <param name="backgroundRect">The background RectTransform to calculate position relative to</param>
    /// <returns>Anchored position in pixels relative to the background's center</returns>
    public static Vector2 NormalizedToPixel(Vector2 normalizedPosition, RectTransform backgroundRect)
    {
        if (backgroundRect == null)
        {
            Debug.LogWarning("[WorldPositionCalculator] Background RectTransform is null. Returning zero position.");
            return Vector2.zero;
        }
        
        // Get the actual size of the background
        Vector2 backgroundSize = backgroundRect.rect.size;
        
        // Convert normalized (0-1) to pixel position relative to center
        // (0, 0) normalized = bottom-left = (-width/2, -height/2) in pixels
        // (1, 1) normalized = top-right = (width/2, height/2) in pixels
        float pixelX = (normalizedPosition.x * backgroundSize.x) - (backgroundSize.x / 2f);
        float pixelY = (normalizedPosition.y * backgroundSize.y) - (backgroundSize.y / 2f);
        
        return new Vector2(pixelX, pixelY);
    }
    
    /// <summary>
    /// Convert a screen percentage position (0-1) to anchored position for a UI element
    /// </summary>
    /// <param name="screenPercentage">Position as percentage of screen (0-1)</param>
    /// <param name="canvasRect">The canvas RectTransform</param>
    /// <returns>Anchored position in pixels</returns>
    public static Vector2 ScreenPercentageToPixel(Vector2 screenPercentage, RectTransform canvasRect)
    {
        if (canvasRect == null)
        {
            Debug.LogWarning("[WorldPositionCalculator] Canvas RectTransform is null. Returning zero position.");
            return Vector2.zero;
        }
        
        Vector2 canvasSize = canvasRect.rect.size;
        
        // Convert percentage to pixel position relative to center
        float pixelX = (screenPercentage.x * canvasSize.x) - (canvasSize.x / 2f);
        float pixelY = (screenPercentage.y * canvasSize.y) - (canvasSize.y / 2f);
        
        return new Vector2(pixelX, pixelY);
    }
    
    /// <summary>
    /// Convert an anchored position to screen percentage (0-1)
    /// </summary>
    /// <param name="anchoredPosition">Position in pixels relative to center</param>
    /// <param name="canvasRect">The canvas RectTransform</param>
    /// <returns>Position as percentage of screen (0-1)</returns>
    public static Vector2 PixelToScreenPercentage(Vector2 anchoredPosition, RectTransform canvasRect)
    {
        if (canvasRect == null)
        {
            Debug.LogWarning("[WorldPositionCalculator] Canvas RectTransform is null. Returning 0.5, 0.5 (center).");
            return new Vector2(0.5f, 0.5f);
        }
        
        Vector2 canvasSize = canvasRect.rect.size;
        
        // Convert pixel position relative to center to percentage (0-1)
        float percentX = (anchoredPosition.x + (canvasSize.x / 2f)) / canvasSize.x;
        float percentY = (anchoredPosition.y + (canvasSize.y / 2f)) / canvasSize.y;
        
        return new Vector2(percentX, percentY);
    }
    
    /// <summary>
    /// Clamp a position to stay within screen bounds, accounting for panel size
    /// </summary>
    /// <param name="position">Target position</param>
    /// <param name="panelSize">Size of the panel</param>
    /// <param name="canvasRect">The canvas RectTransform</param>
    /// <returns>Clamped position that keeps panel on-screen</returns>
    public static Vector2 ClampToScreen(Vector2 position, Vector2 panelSize, RectTransform canvasRect)
    {
        if (canvasRect == null)
        {
            return position;
        }
        
        Vector2 canvasSize = canvasRect.rect.size;
        
        // Calculate bounds (accounting for panel size)
        float minX = -(canvasSize.x / 2f) + (panelSize.x / 2f);
        float maxX = (canvasSize.x / 2f) - (panelSize.x / 2f);
        float minY = -(canvasSize.y / 2f) + (panelSize.y / 2f);
        float maxY = (canvasSize.y / 2f) - (panelSize.y / 2f);
        
        // Clamp position
        float clampedX = Mathf.Clamp(position.x, minX, maxX);
        float clampedY = Mathf.Clamp(position.y, minY, maxY);
        
        return new Vector2(clampedX, clampedY);
    }
}


