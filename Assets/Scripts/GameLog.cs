using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Singleton system for displaying game log entries.
/// Shows important game events like level-ups, quest completions, etc.
/// </summary>
public class GameLog : MonoBehaviour
{
    public static GameLog Instance { get; private set; }
    
    [Header("UI Components")]
    public GameObject logPanel; // Main log panel GameObject
    public RectTransform logPanelRect; // RectTransform of the log panel (for dragging)
    public GameObject dragHandle; // Optional drag handle area (if null, whole panel is draggable)
    public ScrollRect scrollRect; // ScrollRect for scrolling through log entries
    public Scrollbar verticalScrollbar; // Optional vertical scrollbar (will be created automatically if not assigned)
    public Transform logContentContainer; // Container for log entries (usually inside ScrollRect > Viewport > Content)
    public GameObject logEntryPrefab; // Prefab for individual log entries
    public Button toggleButton; // Optional button to toggle log visibility
    public Button viewportToggleButton; // Button to toggle viewport visibility (keeps drag handle and button visible)
    public TextMeshProUGUI logEntryTextPrefab; // Alternative: if no prefab, use this text component
    public CanvasGroup canvasGroup; // CanvasGroup for controlling raycast blocking
    
    [Header("Settings")]
    public int maxLogEntries = 100; // Maximum number of log entries to keep (supports scrolling up to 100 messages)
    public bool autoScrollToBottom = true; // Automatically scroll to newest entry
    public bool allowClickThrough = true; // Allow clicks to pass through when not interacting with log
    
    private List<GameObject> logEntries = new List<GameObject>();
    
    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }
    
    void Start()
    {
        // Setup Content container first (must be done before dragging)
        SetupContentContainer();
        
        // Clean up any excess entries that might exist
        CleanupExcessEntries();
        
        // Setup toggle button if assigned
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleLog);
        }
        
        // Setup viewport toggle button if assigned
        if (viewportToggleButton != null)
        {
            viewportToggleButton.onClick.AddListener(ToggleViewport);
        }
        
        // Setup dragging
        SetupDragging();
        
        // Setup click-through behavior
        SetupClickThrough();
        
        // Subscribe to level up events
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.OnLevelUp += OnLevelUp;
        }
    }
    
    /// <summary>
    /// Setup Content container with proper anchoring for bottom-to-top scrolling
    /// New entries appear at bottom and stay visible, old entries scroll up
    /// </summary>
    private void SetupContentContainer()
    {
        if (logContentContainer == null) return;
        
        // Setup viewport first (must be done before content setup)
        if (scrollRect != null && scrollRect.viewport != null)
        {
            RectTransform viewportRect = scrollRect.viewport.GetComponent<RectTransform>();
            if (viewportRect != null)
            {
                // Ensure Viewport is anchored to fill the panel
                viewportRect.anchorMin = new Vector2(0, 0);
                viewportRect.anchorMax = new Vector2(1, 1);
                viewportRect.offsetMin = Vector2.zero;
                viewportRect.offsetMax = Vector2.zero;
                viewportRect.pivot = new Vector2(0.5f, 0.5f);
                viewportRect.anchoredPosition = Vector2.zero;
                
                // CRITICAL: Add RectMask2D to viewport to clip content outside bounds
                UnityEngine.UI.RectMask2D rectMask = viewportRect.GetComponent<UnityEngine.UI.RectMask2D>();
                if (rectMask == null)
                {
                    rectMask = viewportRect.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
                }
                rectMask.enabled = true;
                
                // Remove old Mask component if it exists
                UnityEngine.UI.Mask oldMask = viewportRect.GetComponent<UnityEngine.UI.Mask>();
                if (oldMask != null)
                {
                    Destroy(oldMask);
                }
            }
        }
        
        RectTransform contentRect = logContentContainer.GetComponent<RectTransform>();
        if (contentRect == null)
        {
            contentRect = logContentContainer.gameObject.AddComponent<RectTransform>();
        }
        
        // Ensure Content is a child of Viewport (critical for masking to work)
        if (scrollRect != null && scrollRect.viewport != null)
        {
            if (contentRect.parent != scrollRect.viewport.transform)
            {
                contentRect.SetParent(scrollRect.viewport.transform, false);
            }
        }
        
        // Anchor Content to bottom of Viewport (new entries appear at bottom, scroll up to see old ones)
        contentRect.anchorMin = new Vector2(0, 0); // Bottom-left anchor
        contentRect.anchorMax = new Vector2(1, 0); // Bottom-right anchor
        contentRect.pivot = new Vector2(0.5f, 0); // Pivot at bottom-center (critical for upward expansion)
        contentRect.anchoredPosition = Vector2.zero; // Start at bottom (Y = 0 means bottom of viewport)
        
        // Set initial size to zero (will expand upward as content is added)
        contentRect.sizeDelta = new Vector2(0, 0);
        
        // Ensure Vertical Layout Group is set up correctly
        VerticalLayoutGroup layoutGroup = logContentContainer.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = logContentContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        
        // Configure Vertical Layout Group for bottom-to-top layout
        layoutGroup.childAlignment = TextAnchor.LowerLeft; // Align children to bottom-left
        layoutGroup.childForceExpandWidth = true; // Children stretch horizontally
        layoutGroup.childForceExpandHeight = false; // Children use their preferred height
        layoutGroup.childControlWidth = true; // Control child width
        layoutGroup.childControlHeight = false; // Don't control child height (let ContentSizeFitter handle it)
        layoutGroup.spacing = 2f; // Small spacing between entries
        
        // Note: We'll manually control Content height instead of using ContentSizeFitter
        // ContentSizeFitter can cause infinite expansion issues
        // We'll calculate height manually in ClampContentSize()
        ContentSizeFitter sizeFitter = logContentContainer.GetComponent<ContentSizeFitter>();
        if (sizeFitter != null)
        {
            // Disable vertical fit - we'll handle it manually
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }
        
        // Ensure ScrollRect has proper movement constraints
        if (scrollRect != null)
        {
            scrollRect.movementType = ScrollRect.MovementType.Clamped; // Prevent scrolling beyond bounds
            scrollRect.vertical = true; // Enable vertical scrolling
            scrollRect.horizontal = false; // Disable horizontal scrolling
            
            // Ensure ScrollRect has proper elasticity settings
            scrollRect.elasticity = 0.1f; // Small bounce when reaching edges
            
            // Setup vertical scrollbar
            SetupVerticalScrollbar();
        }
        
        // Force layout rebuild to apply settings
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }
    
    /// <summary>
    /// Setup vertical scrollbar for the ScrollRect
    /// </summary>
    private void SetupVerticalScrollbar()
    {
        if (scrollRect == null) return;
        
        // If scrollbar is not assigned, try to find one in the log panel hierarchy
        if (verticalScrollbar == null && logPanel != null)
        {
            // Search for a Scrollbar component in the log panel's children
            verticalScrollbar = logPanel.GetComponentInChildren<Scrollbar>();
        }
        
        // If still not found, you can create one programmatically (optional)
        // For now, we'll just assign it if found
        if (verticalScrollbar != null)
        {
            // Ensure scrollbar GameObject is active
            verticalScrollbar.gameObject.SetActive(true);
            
            // Ensure scrollbar component is enabled
            verticalScrollbar.enabled = true;
            
            // Configure scrollbar direction to Bottom To Top (0 = bottom, 1 = top)
            verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;
            
            // Assign to ScrollRect
            scrollRect.verticalScrollbar = verticalScrollbar;
            
            // Use AutoHide instead of AutoHideAndExpandViewport for better compatibility
            // AutoHide will show/hide scrollbar when needed without modifying viewport size
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            
            // Set spacing between scrollbar and viewport (optional, can be adjusted)
            scrollRect.verticalScrollbarSpacing = -3f;
            
            // Force ScrollRect to update immediately
            Canvas.ForceUpdateCanvases();
        }
        else
        {
            // If no scrollbar found, ScrollRect will still work with mouse wheel/drag scrolling
            // Just won't have a visible scrollbar handle
            scrollRect.verticalScrollbar = null;
            Debug.LogWarning("GameLog: No vertical scrollbar found. Assign one in the Inspector or create one in the log panel hierarchy.");
        }
    }
    
    /// <summary>
    /// Setup dragging functionality for the log panel
    /// </summary>
    private void SetupDragging()
    {
        // Get or create RectTransform reference
        if (logPanelRect == null && logPanel != null)
        {
            logPanelRect = logPanel.GetComponent<RectTransform>();
        }
        
        if (logPanelRect == null)
        {
            Debug.LogWarning("GameLog: logPanelRect is not assigned and could not be found on logPanel!");
            return;
        }
        
        // Ensure Viewport (if exists) is properly anchored to move with the panel
        if (scrollRect != null && scrollRect.viewport != null)
        {
            RectTransform viewportRect = scrollRect.viewport.GetComponent<RectTransform>();
            if (viewportRect != null)
            {
                // Ensure Viewport is anchored to fill the panel (should move with parent)
                viewportRect.anchorMin = new Vector2(0, 0);
                viewportRect.anchorMax = new Vector2(1, 1);
                viewportRect.offsetMin = Vector2.zero;
                viewportRect.offsetMax = Vector2.zero;
                
                // Ensure Viewport pivot is at center (critical for staying in place)
                viewportRect.pivot = new Vector2(0.5f, 0.5f);
                viewportRect.anchoredPosition = Vector2.zero;
                
                // Critical: Add RectMask2D component to clip content that extends beyond Viewport bounds
                // RectMask2D is preferred for UI scrolling as it clips rectangular areas efficiently
                UnityEngine.UI.RectMask2D rectMask = scrollRect.viewport.GetComponent<UnityEngine.UI.RectMask2D>();
                if (rectMask == null)
                {
                    rectMask = scrollRect.viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
                }
                
                // Ensure RectMask2D is enabled and active
                rectMask.enabled = true;
                
                // Also ensure there's no old Mask component conflicting
                UnityEngine.UI.Mask oldMask = scrollRect.viewport.GetComponent<UnityEngine.UI.Mask>();
                if (oldMask != null)
                {
                    Destroy(oldMask);
                }
                
                // Force the viewport to update its clipping immediately
                Canvas.ForceUpdateCanvases();
            }
        }
        
        // Ensure Content is properly set up
        EnsureContentSetup();
        
        // If drag handle is assigned, make only that draggable
        if (dragHandle != null)
        {
            // Add DraggablePanel to drag handle
            DraggablePanel dragHandler = dragHandle.GetComponent<DraggablePanel>();
            if (dragHandler == null)
            {
                dragHandler = dragHandle.AddComponent<DraggablePanel>();
            }
            
            // Set the target panel to drag
            dragHandler.targetPanel = logPanelRect;
            
            // Make sure drag handle has a Graphic component for raycast detection
            if (dragHandle.GetComponent<UnityEngine.UI.Graphic>() == null)
            {
                // Add an Image component if none exists (invisible, just for raycasting)
                UnityEngine.UI.Image image = dragHandle.GetComponent<UnityEngine.UI.Image>();
                if (image == null)
                {
                    image = dragHandle.AddComponent<UnityEngine.UI.Image>();
                    image.color = new Color(1, 1, 1, 0); // Transparent
                }
            }
        }
        else
        {
            // No drag handle, make the whole panel draggable
            DraggablePanel dragHandler = logPanelRect.GetComponent<DraggablePanel>();
            if (dragHandler == null)
            {
                dragHandler = logPanelRect.gameObject.AddComponent<DraggablePanel>();
            }
            dragHandler.targetPanel = logPanelRect;
        }
    }
    
    /// <summary>
    /// Add RectMask2D component to a GameObject (helper method)
    /// RectMask2D is preferred for UI scrolling as it clips rectangular areas efficiently
    /// </summary>
    private void AddRectMask2D(GameObject target)
    {
        UnityEngine.UI.RectMask2D rectMask = target.GetComponent<UnityEngine.UI.RectMask2D>();
        if (rectMask == null)
        {
            rectMask = target.AddComponent<UnityEngine.UI.RectMask2D>();
        }
        
        // Remove any old Mask component if it exists
        UnityEngine.UI.Mask oldMask = target.GetComponent<UnityEngine.UI.Mask>();
        if (oldMask != null)
        {
            Destroy(oldMask);
        }
    }
    
    /// <summary>
    /// Ensure Content is properly set up and constrained
    /// </summary>
    private void EnsureContentSetup()
    {
        // Ensure Content is properly set up and constrained
        if (logContentContainer != null && scrollRect != null)
        {
            RectTransform contentRect = logContentContainer.GetComponent<RectTransform>();
            if (contentRect != null && scrollRect.viewport != null)
            {
                // Make sure Content is a child of Viewport
                if (contentRect.parent != scrollRect.viewport.transform)
                {
                    contentRect.SetParent(scrollRect.viewport.transform, false);
                }
                
                // Force Content to stay anchored to bottom (for bottom-up scrolling)
                contentRect.anchorMin = new Vector2(0, 0);
                contentRect.anchorMax = new Vector2(1, 0);
                contentRect.pivot = new Vector2(0.5f, 0);
                
                // Ensure content starts at bottom (Y = 0 means bottom of viewport)
                // This prevents content from starting above the viewport
                Vector2 anchoredPos = contentRect.anchoredPosition;
                anchoredPos.y = 0;
                contentRect.anchoredPosition = anchoredPos;
                
                // Ensure ScrollRect content reference is set
                if (scrollRect.content != contentRect)
                {
                    scrollRect.content = contentRect;
                }
            }
        }
    }
    
    /// <summary>
    /// Setup click-through behavior using CanvasGroup
    /// </summary>
    private void SetupClickThrough()
    {
        if (logPanel == null) return;
        
        // Get or create CanvasGroup
        if (canvasGroup == null)
        {
            canvasGroup = logPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = logPanel.AddComponent<CanvasGroup>();
            }
        }
        
        // Note: Don't modify Content Image alpha - it may be needed for masking
        // If you want transparent background, set it manually in Unity Editor
        // or disable raycast target if you want click-through
        if (logContentContainer != null)
        {
            UnityEngine.UI.Image contentImage = logContentContainer.GetComponent<UnityEngine.UI.Image>();
            if (contentImage != null && allowClickThrough)
            {
                // Only disable raycast target if click-through is enabled
                // This allows clicks to pass through the background while keeping content visible
                contentImage.raycastTarget = false;
            }
        }
        
        // Set blocksRaycasts based on allowClickThrough setting
        // When allowClickThrough is true, we'll dynamically block raycasts only when interacting
        // For now, we'll keep it simple - if allowClickThrough is true, set blocksRaycasts to false
        // But we need to be careful - we still want the scrollrect and buttons to work
        // So we'll leave blocksRaycasts as true, but the background image can be made non-raycastable
        
        // Note: Setting blocksRaycasts to false would prevent ALL interactions including scrolling
        // Instead, make the background image non-raycastable if you want click-through
        // This is handled in the Unity Editor by setting the Image component's Raycast Target to false
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.OnLevelUp -= OnLevelUp;
        }
    }
    
    /// <summary>
    /// Clean up any excess log entries beyond the max limit
    /// This ensures we never exceed maxLogEntries
    /// </summary>
    private void CleanupExcessEntries()
    {
        if (logContentContainer == null) return;
        
        // Sync logEntries list with actual children (remove any null/destroyed entries)
        logEntries.RemoveAll(entry => entry == null);
        
        // Get actual child count from hierarchy - only count LogEntry objects
        int actualLogEntryCount = 0;
        List<GameObject> actualEntries = new List<GameObject>();
        foreach (Transform child in logContentContainer.transform)
        {
            if (child != null && child.gameObject.name.Contains("LogEntry"))
            {
                actualLogEntryCount++;
                actualEntries.Add(child.gameObject);
            }
        }
        
        // Sync logEntries list with actual entries
        logEntries.Clear();
        logEntries.AddRange(actualEntries);
        
        // Remove oldest entries until we're at max
        // Use >= to make room BEFORE adding (if we have 9 and want to add 1, remove 1 first)
        while (logEntries.Count >= maxLogEntries)
        {
            if (logEntries.Count > 0)
            {
                GameObject oldestEntry = logEntries[0];
                logEntries.RemoveAt(0);
                if (oldestEntry != null)
                {
                    // Remove from hierarchy immediately
                    if (oldestEntry.transform.parent != null)
                    {
                        oldestEntry.transform.SetParent(null);
                    }
                    
                    #if UNITY_EDITOR
                    if (Application.isPlaying)
                    {
                        Destroy(oldestEntry);
                    }
                    else
                    {
                        DestroyImmediate(oldestEntry);
                    }
                    #else
                    Destroy(oldestEntry);
                    #endif
                }
            }
            else
            {
                break; // Safety break
            }
        }
        
        // Double-check: if children still exceed max, remove directly from hierarchy
        actualLogEntryCount = 0;
        foreach (Transform child in logContentContainer.transform)
        {
            if (child != null && child.gameObject.name.Contains("LogEntry"))
            {
                actualLogEntryCount++;
            }
        }
        
        while (actualLogEntryCount >= maxLogEntries) // Use >= to make room BEFORE adding
        {
            Transform oldestChild = null;
            foreach (Transform child in logContentContainer.transform)
            {
                if (child != null && child.gameObject.name.Contains("LogEntry"))
                {
                    oldestChild = child;
                    break; // Get first LogEntry child
                }
            }
            
            if (oldestChild != null)
            {
                #if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    Destroy(oldestChild.gameObject);
                }
                else
                {
                    DestroyImmediate(oldestChild.gameObject);
                }
                #else
                Destroy(oldestChild.gameObject);
                #endif
                actualLogEntryCount--;
            }
            else
            {
                break; // Safety break
            }
        }
    }
    
    /// <summary>
    /// Add a log entry to the game log
    /// </summary>
    public void AddLogEntry(string message, LogType logType = LogType.Info)
    {
        if (logContentContainer == null)
        {
            Debug.LogWarning("GameLog: logContentContainer is not assigned!");
            return;
        }
        
        // Create log entry
        GameObject logEntry = null;
        
        if (logEntryPrefab != null)
        {
            logEntry = Instantiate(logEntryPrefab, logContentContainer);
        }
        else if (logEntryTextPrefab != null)
        {
            logEntry = Instantiate(logEntryTextPrefab.gameObject, logContentContainer);
        }
        else
        {
            // Fallback: create simple text entry
            logEntry = new GameObject("LogEntry");
            logEntry.transform.SetParent(logContentContainer);
            
            // Add RectTransform setup FIRST (before adding TextMeshProUGUI)
            RectTransform rectTransform = logEntry.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = logEntry.AddComponent<RectTransform>();
            }
            
            // Set up RectTransform to stretch horizontally and fit content vertically
            // Anchor to bottom so entries stack upward (newest at bottom, oldest at top)
            rectTransform.anchorMin = new Vector2(0, 0); // Bottom-left
            rectTransform.anchorMax = new Vector2(1, 0); // Bottom-right
            rectTransform.pivot = new Vector2(0.5f, 0); // Pivot at bottom-center
            rectTransform.anchoredPosition = Vector2.zero; // Will be positioned by Vertical Layout Group
            rectTransform.sizeDelta = new Vector2(0, 0); // Width will stretch, height will be set by ContentSizeFitter
            
            // Add TextMeshProUGUI component
            TextMeshProUGUI text = logEntry.AddComponent<TextMeshProUGUI>();
            
            // Ensure TextMeshProUGUI has a font asset (use default if none assigned)
            if (text.font == null)
            {
                // Try to get default TextMeshProUGUI font
                TMPro.TMP_FontAsset defaultFont = Resources.GetBuiltinResource<TMPro.TMP_FontAsset>("Legacy/SDF - Default");
                if (defaultFont != null)
                {
                    text.font = defaultFont;
                }
                else
                {
                    // Fallback: use any TMP font in resources
                    TMPro.TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMPro.TMP_FontAsset>();
                    if (fonts.Length > 0)
                    {
                        text.font = fonts[0];
                    }
                }
            }
            
            text.text = message;
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.TopLeft; // Left-align text
            text.enableWordWrapping = true; // Enable word wrapping so text wraps within the width
            text.overflowMode = TextOverflowModes.Overflow; // Allow text to wrap instead of truncate
            text.color = Color.white; // Ensure text is visible (will be overridden by log type color later)
            text.raycastTarget = false; // Don't block raycasts on text itself
            text.maskable = true; // CRITICAL: Enable masking so RectMask2D can clip this text
            
            // Add ContentSizeFitter to automatically size height based on text content
            ContentSizeFitter sizeFitter = logEntry.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Ensure the RectTransform uses the size from ContentSizeFitter
            rectTransform.sizeDelta = new Vector2(0, 0);
        }
        
        // Set text content
        TextMeshProUGUI textComponent = logEntry.GetComponent<TextMeshProUGUI>();
        if (textComponent == null)
        {
            textComponent = logEntry.GetComponentInChildren<TextMeshProUGUI>();
        }
        
        if (textComponent != null)
        {
            // Format message with timestamp and color based on log type
            string formattedMessage = FormatLogMessage(message, logType);
            textComponent.text = formattedMessage;
            
            // Ensure text component has proper settings for width
            textComponent.alignment = TextAlignmentOptions.TopLeft;
            textComponent.enableWordWrapping = true;
            
            // CRITICAL: Enable masking so RectMask2D can clip this text
            textComponent.maskable = true;
            
            // Apply color based on log type
            switch (logType)
            {
                case LogType.Success:
                    textComponent.color = Color.green;
                    break;
                case LogType.Warning:
                    textComponent.color = Color.yellow;
                    break;
                case LogType.Error:
                    textComponent.color = Color.red;
                    break;
                default:
                    textComponent.color = Color.white;
                    break;
            }
            
            // Force layout update if ContentSizeFitter exists
            ContentSizeFitter sizeFitter = logEntry.GetComponent<ContentSizeFitter>();
            if (sizeFitter != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(logEntry.GetComponent<RectTransform>());
            }
        }
        
        // CRITICAL: Clean up BEFORE adding new entry to ensure we never exceed max
        // This ensures the count is accurate before we add
        CleanupExcessEntries();
        
        // Track entry
        logEntries.Add(logEntry);
        
        // Force layout rebuild to ensure proper positioning
        if (logContentContainer != null)
        {
            RectTransform contentRect = logContentContainer.GetComponent<RectTransform>();
            
            // Rebuild layout
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            
            // Immediately clamp content position to prevent it from extending above viewport
            if (scrollRect != null && scrollRect.viewport != null)
            {
                RectTransform viewportRect = scrollRect.viewport.GetComponent<RectTransform>();
                if (viewportRect != null)
                {
                    // Lock Viewport position - it should always be at (0,0) relative to panel
                    viewportRect.anchoredPosition = Vector2.zero;
                    
                    // Immediately clamp content position
                    float contentHeight = contentRect.sizeDelta.y;
                    float viewportHeight = viewportRect.rect.height;
                    Vector2 anchoredPos = contentRect.anchoredPosition;
                    
                    if (contentHeight > viewportHeight)
                    {
                        // Content exceeds viewport - clamp to valid scroll range
                        float maxScrollY = contentHeight - viewportHeight;
                        if (anchoredPos.y < 0)
                        {
                            anchoredPos.y = 0;
                        }
                        else if (anchoredPos.y > maxScrollY)
                        {
                            anchoredPos.y = maxScrollY;
                        }
                    }
                    else
                    {
                        // Content fits in viewport - keep at bottom
                        anchoredPos.y = 0;
                    }
                    
                    contentRect.anchoredPosition = anchoredPos;
                }
            }
            
            // Wait a frame for layout to update, then clamp content size
            StartCoroutine(ClampContentSize());
        }
        
        // Ensure ScrollRect content reference is set correctly
        if (scrollRect != null && logContentContainer != null)
        {
            RectTransform contentRect = logContentContainer.GetComponent<RectTransform>();
            if (contentRect != null && scrollRect.content != contentRect)
            {
                scrollRect.content = contentRect;
            }
            
            // Force ScrollRect to update bounds so scrollbar appears when needed
            Canvas.ForceUpdateCanvases();
            scrollRect.CalculateLayoutInputVertical();
            scrollRect.SetLayoutVertical();
        }
        
        // Auto-scroll to bottom (newest entries) - but only if user hasn't manually scrolled up
        if (autoScrollToBottom && scrollRect != null)
        {
            StartCoroutine(ScrollToBottom());
        }
    }
    
    /// <summary>
    /// Clamp Content size to prevent infinite expansion
    /// Also ensures newest entries stay visible at bottom
    /// </summary>
    private System.Collections.IEnumerator ClampContentSize()
    {
        yield return null; // Wait a frame for layout to update
        yield return null; // Wait another frame for layout to fully stabilize
        
        if (logContentContainer == null || scrollRect == null || scrollRect.viewport == null) yield break;
        
        RectTransform contentRect = logContentContainer.GetComponent<RectTransform>();
        RectTransform viewportRect = scrollRect.viewport.GetComponent<RectTransform>();
        
        if (contentRect == null || viewportRect == null) yield break;
        
        // Get the actual layout height from Vertical Layout Group
        VerticalLayoutGroup layoutGroup = logContentContainer.GetComponent<VerticalLayoutGroup>();
        
        // Calculate total height properly using rect height of children (not sizeDelta)
        float totalHeight = 0f;
        int childCount = 0;
        foreach (Transform child in logContentContainer)
        {
            RectTransform childRect = child.GetComponent<RectTransform>();
            if (childRect != null && childRect.gameObject.activeSelf)
            {
                // Use rect height (actual displayed height) instead of sizeDelta
                totalHeight += childRect.rect.height;
                childCount++;
            }
        }
        
        // Add spacing from Vertical Layout Group
        if (layoutGroup != null && childCount > 1)
        {
            totalHeight += layoutGroup.spacing * (childCount - 1);
        }
        
        // Add padding
        if (layoutGroup != null)
        {
            totalHeight += layoutGroup.padding.top + layoutGroup.padding.bottom;
        }
        
        // Clamp content height to actual calculated content size
        Vector2 sizeDelta = contentRect.sizeDelta;
        float newHeight = Mathf.Max(totalHeight, 0);
        
        // Ensure Content width matches Viewport width (stretch horizontally)
        // Content should be anchored to stretch horizontally, so sizeDelta.x should be 0
        sizeDelta.x = 0; // Force width to match parent (viewport)
        
        // Only update if height changed significantly (prevent infinite loop)
        if (Mathf.Abs(sizeDelta.y - newHeight) > 0.1f)
        {
            sizeDelta.y = newHeight;
            contentRect.sizeDelta = sizeDelta;
        }
        else
        {
            // Even if height didn't change, ensure width is still constrained
            contentRect.sizeDelta = sizeDelta;
        }
        
        // Safety clamp: prevent content from exceeding reasonable bounds
        // If content gets too large, we're probably calculating wrong - clamp it
        const float MAX_REASONABLE_HEIGHT = 10000f;
        if (sizeDelta.y > MAX_REASONABLE_HEIGHT)
        {
            Debug.LogWarning($"GameLog: Content height ({sizeDelta.y}) exceeded maximum ({MAX_REASONABLE_HEIGHT}), clamping. This may indicate a layout issue.");
            sizeDelta.y = MAX_REASONABLE_HEIGHT;
            contentRect.sizeDelta = sizeDelta;
        }
        
        // Force ScrollRect to recalculate bounds after content size changes
        if (scrollRect != null && viewportRect != null)
        {
            // Update ScrollRect bounds
            scrollRect.CalculateLayoutInputHorizontal();
            scrollRect.CalculateLayoutInputVertical();
            scrollRect.SetLayoutVertical();
            scrollRect.SetLayoutHorizontal();
            
            // Critical: For bottom-anchored content, when it exceeds viewport height,
            // we need to ensure ScrollRect properly clamps the content position
            // ScrollRect handles this automatically, but we need to ensure bounds are correct
            
            // Force ScrollRect to update its bounds calculation
            Canvas.ForceUpdateCanvases();
            
            // Ensure content position is clamped within scrollable bounds
            // When contentHeight > viewportHeight, content should be scrollable
            // ScrollRect's verticalNormalizedPosition handles this:
            // - 0.0 = bottom visible (newest entries)
            // - 1.0 = top visible (oldest entries)
            
            // If content exceeds viewport, ensure we're at bottom (newest visible)
            float contentHeight = contentRect.sizeDelta.y;
            float viewportHeight = viewportRect.rect.height;
            
            // Critical: For bottom-anchored content, we need to ensure it never extends above viewport top
            // When contentHeight > viewportHeight, ScrollRect should clamp scrolling
            // But we also need to ensure the content's anchoredPosition is correct
            
            // For bottom-anchored content that grows upward:
            // - When contentHeight <= viewportHeight: anchoredPosition.y should be 0 (content sits at bottom)
            // - When contentHeight > viewportHeight: anchoredPosition.y should be 0 (bottom visible, scrollable upward)
            // ScrollRect handles the scrolling, but we need to ensure anchoredPosition doesn't push content above viewport
            
            Vector2 anchoredPos = contentRect.anchoredPosition;
            
            // For bottom-anchored content, anchoredPosition.y represents the offset from the bottom anchor
            // When content exceeds viewport, ScrollRect will adjust anchoredPosition.y as user scrolls
            // But we need to clamp it to prevent content from extending beyond bounds
            
            if (contentHeight > viewportHeight)
            {
                // Content exceeds viewport - ensure ScrollRect bounds are correct
                // The max scroll distance is (contentHeight - viewportHeight)
                // anchoredPosition.y should be clamped between 0 and (contentHeight - viewportHeight)
                // - 0 = bottom visible (newest entries)
                // - (contentHeight - viewportHeight) = top visible (oldest entries)
                
                float maxScrollY = contentHeight - viewportHeight;
                
                // Clamp anchoredPosition.y to valid scroll range
                if (anchoredPos.y < 0)
                {
                    anchoredPos.y = 0;
                    contentRect.anchoredPosition = anchoredPos;
                }
                else if (anchoredPos.y > maxScrollY)
                {
                    anchoredPos.y = maxScrollY;
                    contentRect.anchoredPosition = anchoredPos;
                }
                
                // Force ScrollRect to recalculate bounds
                Canvas.ForceUpdateCanvases();
                
                // Ensure ScrollRect's movement type is clamped (should already be set in SetupContentContainer)
                if (scrollRect.movementType != ScrollRect.MovementType.Clamped)
                {
                    scrollRect.movementType = ScrollRect.MovementType.Clamped;
                }
                
                // Force scrollbar to update visibility when content exceeds viewport
                if (verticalScrollbar != null)
                {
                    // Trigger ScrollRect to update scrollbar visibility
                    scrollRect.verticalScrollbarVisibility = scrollRect.verticalScrollbarVisibility;
                    Canvas.ForceUpdateCanvases();
                }
            }
            else
            {
                // Content fits in viewport - keep it anchored at bottom (y = 0)
                // This ensures newest entries stay visible at bottom
                if (anchoredPos.y != 0)
                {
                    anchoredPos.y = 0;
                    contentRect.anchoredPosition = anchoredPos;
                }
                
                // Force scrollbar to update visibility when content fits in viewport
                if (verticalScrollbar != null)
                {
                    // Trigger ScrollRect to update scrollbar visibility
                    scrollRect.verticalScrollbarVisibility = scrollRect.verticalScrollbarVisibility;
                    Canvas.ForceUpdateCanvases();
                }
            }
        }
    }
    
    /// <summary>
    /// Format log message with timestamp
    /// </summary>
    private string FormatLogMessage(string message, LogType logType)
    {
        // Simple timestamp (you can make this more sophisticated)
        string timeStamp = System.DateTime.Now.ToString("HH:mm:ss");
        return $"[{timeStamp}] {message}";
    }
    
    /// <summary>
    /// Scroll to bottom of log (coroutine to wait a frame for layout update)
    /// Scrolls to show newest entries at the bottom
    /// </summary>
    private System.Collections.IEnumerator ScrollToBottom()
    {
        yield return null; // Wait a frame for layout to update
        yield return null; // Wait another frame for bounds to be fully calculated
        
        if (scrollRect != null && logContentContainer != null && scrollRect.viewport != null)
        {
            RectTransform contentRect = logContentContainer.GetComponent<RectTransform>();
            RectTransform viewportRect = scrollRect.viewport.GetComponent<RectTransform>();
            
            if (contentRect != null && viewportRect != null)
            {
                // Force ScrollRect to update bounds calculation
                Canvas.ForceUpdateCanvases();
                
                // Ensure ScrollRect has MovementType.Clamped set
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                
                // verticalNormalizedPosition: 0 = bottom, 1 = top
                // Set to 0 to show newest entries at bottom
                scrollRect.verticalNormalizedPosition = 0f;
                
                // Force layout update after setting position
                scrollRect.CalculateLayoutInputVertical();
                scrollRect.SetLayoutVertical();
                
                // One more force update to ensure bounds are correct
                Canvas.ForceUpdateCanvases();
            }
        }
    }
    
    /// <summary>
    /// Handle level up event
    /// </summary>
    private void OnLevelUp(int oldLevel, int newLevel)
    {
        // Calculate stat changes
        List<StatChange> statChanges = CalculateStatChanges(oldLevel, newLevel);
        
        // Create log message
        string message = $"Level Up! You went from level {oldLevel} to level {newLevel}";
        AddLogEntry(message, LogType.Success);
        
        // Add stat changes as separate log entries
        foreach (StatChange change in statChanges)
        {
            AddLogEntry($"  • {change.GetDisplayText()}", LogType.Info);
        }
    }
    
    /// <summary>
    /// Calculate stat changes between two levels
    /// </summary>
    private List<StatChange> CalculateStatChanges(int oldLevel, int newLevel)
    {
        List<StatChange> changes = new List<StatChange>();
        
        if (CharacterManager.Instance == null) return changes;
        
        // Calculate health change
        float oldHealth = CharacterManager.Instance.GetMaxHealthAtLevel(oldLevel);
        float newHealth = CharacterManager.Instance.GetMaxHealthAtLevel(newLevel);
        if (oldHealth != newHealth)
        {
            changes.Add(new StatChange("Health", oldHealth, newHealth, "{0:F0}"));
        }
        
        // Calculate attack change
        float oldAttack = CharacterManager.Instance.GetBaseAttackAtLevel(oldLevel);
        float newAttack = CharacterManager.Instance.GetBaseAttackAtLevel(newLevel);
        if (oldAttack != newAttack)
        {
            changes.Add(new StatChange("Attack", oldAttack, newAttack, "{0:F0}"));
        }
        
        // Calculate crit chance change
        float oldCritChance = CharacterManager.Instance.GetBaseCritChanceAtLevel(oldLevel);
        float newCritChance = CharacterManager.Instance.GetBaseCritChanceAtLevel(newLevel);
        if (oldCritChance != newCritChance)
        {
            changes.Add(new StatChange("Crit Chance", oldCritChance * 100f, newCritChance * 100f, "{0:F1}%"));
        }
        
        return changes;
    }
    
    /// <summary>
    /// Toggle log panel visibility
    /// </summary>
    public void ToggleLog()
    {
        if (logPanel != null)
        {
            logPanel.SetActive(!logPanel.activeSelf);
        }
    }
    
    /// <summary>
    /// Show log panel
    /// </summary>
    public void ShowLog()
    {
        if (logPanel != null)
        {
            logPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// Hide log panel
    /// </summary>
    public void HideLog()
    {
        if (logPanel != null)
        {
            logPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Clear all log entries
    /// </summary>
    public void ClearLog()
    {
        foreach (GameObject entry in logEntries)
        {
            if (entry != null)
            {
                Destroy(entry);
            }
        }
        logEntries.Clear();
    }
    
    /// <summary>
    /// Toggle viewport visibility (hides/shows the viewport while keeping drag handle and button visible)
    /// </summary>
    public void ToggleViewport()
    {
        if (scrollRect != null && scrollRect.viewport != null)
        {
            GameObject viewport = scrollRect.viewport.gameObject;
            viewport.SetActive(!viewport.activeSelf);
        }
    }
    
    /// <summary>
    /// Show viewport
    /// </summary>
    public void ShowViewport()
    {
        if (scrollRect != null && scrollRect.viewport != null)
        {
            scrollRect.viewport.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Hide viewport (keeps drag handle and button visible)
    /// </summary>
    public void HideViewport()
    {
        if (scrollRect != null && scrollRect.viewport != null)
        {
            scrollRect.viewport.gameObject.SetActive(false);
        }
    }
}

/// <summary>
/// Types of log entries
/// </summary>
public enum LogType
{
    Info,       // General information (white)
    Success,    // Success messages (green)
    Warning,    // Warning messages (yellow)
    Error       // Error messages (red)
}

/// <summary>
/// Container for stat change information
/// </summary>
public class StatChange
{
    public string statName;
    public float oldValue;
    public float newValue;
    public string format;
    
    public StatChange(string name, float old, float newVal, string formatString = "{0:F0}")
    {
        statName = name;
        oldValue = old;
        newValue = newVal;
        format = formatString;
    }
    
    public string GetDisplayText()
    {
        string oldFormatted = string.Format(format, oldValue);
        string newFormatted = string.Format(format, newValue);
        return $"{statName}: {oldFormatted} > {newFormatted}";
    }
}

