using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages UI panels in a horizontal container, limiting the number of visible panels.
/// When the max is reached, the rightmost panel is closed to make room for new ones.
/// </summary>
public class UIPanelController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Maximum number of panels that can be visible at once")]
    public int maxVisiblePanels = 3;
    
    [Header("Position")]
    [Tooltip("Position to move the panel container to on start")]
    public Vector2 startPosition = new Vector2(0f, 40f);
    
    // Track open panels in order (index 0 = leftmost/oldest, last = rightmost/newest)
    private List<GameObject> openPanels = new List<GameObject>();
    private RectTransform rectTransform;
    
    void Start()
    {
        // Move to specified position on game start
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = startPosition;
        }
    }
    
    /// <summary>
    /// Opens a panel. If already at max visible panels, closes the oldest (leftmost) one first.
    /// </summary>
    public void OpenPanel(GameObject panel)
    {
        if (panel == null) return;
        
        Debug.Log($"[UIPanelController] OpenPanel called for: {panel.name}, current count: {openPanels.Count}");
        
        // If panel is already open, just bring it to the right
        if (openPanels.Contains(panel))
        {
            openPanels.Remove(panel);
            openPanels.Add(panel);
            panel.transform.SetAsLastSibling();
            return;
        }
        
        // If at max capacity, close the oldest panel (first in list / leftmost)
        while (openPanels.Count >= maxVisiblePanels && openPanels.Count > 0)
        {
            GameObject oldestPanel = openPanels[0];
            Debug.Log($"[UIPanelController] At max capacity, closing oldest panel: {oldestPanel.name}");
            ClosePanel(oldestPanel);
        }
        
        // Open the new panel
        openPanels.Add(panel);
        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
        
        Debug.Log($"[UIPanelController] Panel opened: {panel.name}, new count: {openPanels.Count}");
    }
    
    /// <summary>
    /// Closes a specific panel.
    /// </summary>
    public void ClosePanel(GameObject panel)
    {
        if (panel == null) return;
        
        Debug.Log($"[UIPanelController] ClosePanel: {panel.name}");
        openPanels.Remove(panel);
        panel.SetActive(false);
    }
    
    /// <summary>
    /// Toggles a panel open/closed.
    /// </summary>
    public void TogglePanel(GameObject panel)
    {
        if (panel == null) return;
        
        if (IsPanelOpen(panel))
        {
            ClosePanel(panel);
        }
        else
        {
            OpenPanel(panel);
        }
    }
    
    /// <summary>
    /// Checks if a panel is currently open and tracked by this controller.
    /// </summary>
    public bool IsPanelOpen(GameObject panel)
    {
        return panel != null && openPanels.Contains(panel);
    }
    
    /// <summary>
    /// Gets the number of currently open panels.
    /// </summary>
    public int GetOpenPanelCount()
    {
        return openPanels.Count;
    }
    
    /// <summary>
    /// Closes all panels.
    /// </summary>
    public void CloseAllPanels()
    {
        // Iterate backwards to avoid collection modification issues
        for (int i = openPanels.Count - 1; i >= 0; i--)
        {
            if (openPanels[i] != null)
            {
                openPanels[i].SetActive(false);
            }
        }
        openPanels.Clear();
    }
}

