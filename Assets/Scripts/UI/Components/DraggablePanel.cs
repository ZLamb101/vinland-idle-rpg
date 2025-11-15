using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Component that allows a UI panel to be dragged around the screen.
/// Attach this to the panel or a header/drag handle area.
/// If attached to a child element (like a drag handle), it will drag the parent panel.
/// </summary>
public class DraggablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Drag Settings")]
    public RectTransform targetPanel; // Panel to drag (if null, uses this GameObject's RectTransform)
    
    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 lastMousePosition;
    private bool isDragging = false;
    
    void Awake()
    {
        // Determine which RectTransform to move
        if (targetPanel != null)
        {
            rectTransform = targetPanel;
        }
        else
        {
            rectTransform = GetComponent<RectTransform>();
        }
        
        // If we're on a child element, try to find the parent panel
        if (targetPanel == null && rectTransform.parent != null)
        {
            RectTransform parentRect = rectTransform.parent.GetComponent<RectTransform>();
            if (parentRect != null && parentRect != rectTransform)
            {
                // Use parent as the target if this is likely a child element
                rectTransform = parentRect;
            }
        }
        
        // Find canvas in parent hierarchy
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (rectTransform == null) return;
        
        isDragging = true;
        
        // Store the initial mouse position relative to the panel's parent (Canvas or Container)
        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (parentRect == null)
        {
            // If no parent, use Canvas
            Canvas parentCanvas = rectTransform.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                parentRect = parentCanvas.GetComponent<RectTransform>();
            }
        }
        
        if (parentRect != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, 
                eventData.position, 
                eventData.pressEventCamera, 
                out lastMousePosition);
        }
        else
        {
            // Fallback: use panel's own coordinate system
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, 
                eventData.position, 
                eventData.pressEventCamera, 
                out lastMousePosition);
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null || !isDragging) return;
        
        // Get the parent RectTransform (Canvas or Container)
        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (parentRect == null)
        {
            Canvas parentCanvas = rectTransform.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                parentRect = parentCanvas.GetComponent<RectTransform>();
            }
        }
        
        if (parentRect != null)
        {
            // Convert mouse position to local coordinates relative to the panel's parent
            Vector2 localPointerPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventData.pressEventCamera,
                out localPointerPosition))
            {
                // Calculate the offset from the last mouse position
                Vector2 offset = localPointerPosition - lastMousePosition;
                
                // Move the panel by updating its anchoredPosition
                rectTransform.anchoredPosition += offset;
                
                // Update last mouse position
                lastMousePosition = localPointerPosition;
            }
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        // Optional: Add any cleanup or snap-to-grid logic here
    }
}

