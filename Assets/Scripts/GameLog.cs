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
    public GameObject logPanel;
    public RectTransform logPanelRect;
    public GameObject dragHandle;
    public ScrollRect scrollRect;
    public Scrollbar verticalScrollbar;
    public Transform logContentContainer;
    public GameObject logEntryPrefab;
    public Button toggleButton;
    public Button viewportToggleButton;
    public TextMeshProUGUI logEntryTextPrefab;
    public CanvasGroup canvasGroup;
    
    [Header("Settings")]
    public int maxLogEntries = 100;
    public bool autoScrollToBottom = true;
    public bool allowClickThrough = true;
    
    private List<GameObject> logEntries = new List<GameObject>();
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void Start()
    {
        SetupContentContainer();
        CleanupExcessEntries();
        
        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleLog);
        
        if (viewportToggleButton != null)
            viewportToggleButton.onClick.AddListener(ToggleViewport);
        
        SetupDragging();
        SetupClickThrough();
        
        if (CharacterManager.Instance != null)
            CharacterManager.Instance.OnLevelUp += OnLevelUp;
    }
    
    void OnDestroy()
    {
        if (CharacterManager.Instance != null)
            CharacterManager.Instance.OnLevelUp -= OnLevelUp;
    }
    
    private void SetupContentContainer()
    {
        if (logContentContainer == null) return;
        
        // Setup viewport
        if (scrollRect != null && scrollRect.viewport != null)
        {
            SetupViewport();
        }
        
        // Setup content
        RectTransform contentRect = logContentContainer.GetComponent<RectTransform>();
        if (contentRect == null)
            contentRect = logContentContainer.gameObject.AddComponent<RectTransform>();
        
        if (scrollRect != null && scrollRect.viewport != null)
        {
            if (contentRect.parent != scrollRect.viewport.transform)
                contentRect.SetParent(scrollRect.viewport.transform, false);
        }
        
        // Anchor content to bottom for bottom-up scrolling
        contentRect.anchorMin = new Vector2(0, 0);
        contentRect.anchorMax = new Vector2(1, 0);
        contentRect.pivot = new Vector2(0.5f, 0);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;
        
        // Setup layout group
        VerticalLayoutGroup layoutGroup = logContentContainer.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
            layoutGroup = logContentContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        
        layoutGroup.childAlignment = TextAnchor.LowerLeft;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.spacing = 2f;
        
        // Disable ContentSizeFitter - we handle height manually
        ContentSizeFitter sizeFitter = logContentContainer.GetComponent<ContentSizeFitter>();
        if (sizeFitter != null)
        {
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }
        
        // Setup ScrollRect
        if (scrollRect != null)
        {
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.elasticity = 0.1f;
            SetupVerticalScrollbar();
        }
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }
    
    private void SetupViewport()
    {
        RectTransform viewportRect = scrollRect.viewport.GetComponent<RectTransform>();
        if (viewportRect == null) return;
        
        viewportRect.anchorMin = new Vector2(0, 0);
        viewportRect.anchorMax = new Vector2(1, 1);
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportRect.pivot = new Vector2(0.5f, 0.5f);
        viewportRect.anchoredPosition = Vector2.zero;
        
        // Add RectMask2D for clipping
        RectMask2D rectMask = viewportRect.GetComponent<RectMask2D>();
        if (rectMask == null)
            rectMask = viewportRect.gameObject.AddComponent<RectMask2D>();
        rectMask.enabled = true;
        
        // Remove old Mask component if exists
        Mask oldMask = viewportRect.GetComponent<Mask>();
        if (oldMask != null)
            Destroy(oldMask);
    }
    
    private void SetupVerticalScrollbar()
    {
        if (scrollRect == null) return;
        
        if (verticalScrollbar == null && logPanel != null)
            verticalScrollbar = logPanel.GetComponentInChildren<Scrollbar>();
        
        if (verticalScrollbar != null)
        {
            verticalScrollbar.gameObject.SetActive(true);
            verticalScrollbar.enabled = true;
            verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollRect.verticalScrollbar = verticalScrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scrollRect.verticalScrollbarSpacing = -3f;
            Canvas.ForceUpdateCanvases();
        }
        else
        {
            scrollRect.verticalScrollbar = null;
        }
    }
    
    private void SetupDragging()
    {
        if (logPanelRect == null && logPanel != null)
            logPanelRect = logPanel.GetComponent<RectTransform>();
        
        if (logPanelRect == null)
        {
            Debug.LogWarning("GameLog: logPanelRect is not assigned!");
            return;
        }
        
        EnsureContentSetup();
        
        if (dragHandle != null)
        {
            DraggablePanel dragHandler = dragHandle.GetComponent<DraggablePanel>();
            if (dragHandler == null)
                dragHandler = dragHandle.AddComponent<DraggablePanel>();
            dragHandler.targetPanel = logPanelRect;
            
            if (dragHandle.GetComponent<Graphic>() == null)
            {
                Image image = dragHandle.GetComponent<Image>();
                if (image == null)
                {
                    image = dragHandle.AddComponent<Image>();
                    image.color = new Color(1, 1, 1, 0);
                }
            }
        }
        else
        {
            DraggablePanel dragHandler = logPanelRect.GetComponent<DraggablePanel>();
            if (dragHandler == null)
                dragHandler = logPanelRect.gameObject.AddComponent<DraggablePanel>();
            dragHandler.targetPanel = logPanelRect;
        }
    }
    
    private void EnsureContentSetup()
    {
        if (logContentContainer == null || scrollRect == null || scrollRect.viewport == null)
            return;
        
        RectTransform contentRect = logContentContainer.GetComponent<RectTransform>();
        if (contentRect == null) return;
        
        if (contentRect.parent != scrollRect.viewport.transform)
            contentRect.SetParent(scrollRect.viewport.transform, false);
        
        contentRect.anchorMin = new Vector2(0, 0);
        contentRect.anchorMax = new Vector2(1, 0);
        contentRect.pivot = new Vector2(0.5f, 0);
        contentRect.anchoredPosition = Vector2.zero;
        
        if (scrollRect.content != contentRect)
            scrollRect.content = contentRect;
    }
    
    private void SetupClickThrough()
    {
        if (logPanel == null) return;
        
        if (canvasGroup == null)
        {
            canvasGroup = logPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = logPanel.AddComponent<CanvasGroup>();
        }
        
        if (logContentContainer != null && allowClickThrough)
        {
            Image contentImage = logContentContainer.GetComponent<Image>();
            if (contentImage != null)
                contentImage.raycastTarget = false;
        }
    }
    
    private void CleanupExcessEntries()
    {
        if (logContentContainer == null) return;
        
        // Sync list with actual children
        logEntries.RemoveAll(entry => entry == null);
        List<GameObject> actualEntries = new List<GameObject>();
        
        foreach (Transform child in logContentContainer.transform)
        {
            if (child != null && child.gameObject.name.Contains("LogEntry"))
                actualEntries.Add(child.gameObject);
        }
        
        logEntries.Clear();
        logEntries.AddRange(actualEntries);
        
        // Remove oldest entries until we're at max
        while (logEntries.Count >= maxLogEntries && logEntries.Count > 0)
        {
            GameObject oldestEntry = logEntries[0];
            logEntries.RemoveAt(0);
            
            if (oldestEntry != null)
            {
                if (oldestEntry.transform.parent != null)
                    oldestEntry.transform.SetParent(null);
                
                #if UNITY_EDITOR
                DestroyImmediate(oldestEntry);
                #else
                Destroy(oldestEntry);
                #endif
            }
        }
    }
    
    public void AddLogEntry(string message, LogType logType = LogType.Info)
    {
        if (logContentContainer == null)
        {
            Debug.LogWarning("GameLog: logContentContainer is not assigned!");
            return;
        }
        
        CleanupExcessEntries();
        
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
            logEntry = CreateFallbackLogEntry(message);
        }
        
        // Setup text component
        TextMeshProUGUI textComponent = logEntry.GetComponent<TextMeshProUGUI>();
        if (textComponent == null)
            textComponent = logEntry.GetComponentInChildren<TextMeshProUGUI>();
        
        if (textComponent != null)
        {
            textComponent.text = FormatLogMessage(message, logType);
            textComponent.alignment = TextAlignmentOptions.TopLeft;
            textComponent.enableWordWrapping = true;
            textComponent.maskable = true;
            
            switch (logType)
            {
                case LogType.Success: textComponent.color = Color.green; break;
                case LogType.Warning: textComponent.color = Color.yellow; break;
                case LogType.Error: textComponent.color = Color.red; break;
                default: textComponent.color = Color.white; break;
            }
            
            ContentSizeFitter sizeFitter = logEntry.GetComponent<ContentSizeFitter>();
            if (sizeFitter != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(logEntry.GetComponent<RectTransform>());
        }
        
        logEntries.Add(logEntry);
        
        // Update layout
        if (logContentContainer != null)
        {
            RectTransform contentRect = logContentContainer.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            
            if (scrollRect != null && scrollRect.viewport != null)
            {
                RectTransform viewportRect = scrollRect.viewport.GetComponent<RectTransform>();
                if (viewportRect != null)
                {
                    viewportRect.anchoredPosition = Vector2.zero;
                    ClampContentPosition(contentRect, viewportRect);
                }
            }
            
            StartCoroutine(ClampContentSize());
        }
        
        // Update ScrollRect
        if (scrollRect != null && logContentContainer != null)
        {
            RectTransform contentRect = logContentContainer.GetComponent<RectTransform>();
            if (contentRect != null && scrollRect.content != contentRect)
                scrollRect.content = contentRect;
            
            Canvas.ForceUpdateCanvases();
            scrollRect.CalculateLayoutInputVertical();
            scrollRect.SetLayoutVertical();
        }
        
        if (autoScrollToBottom && scrollRect != null)
            StartCoroutine(ScrollToBottom());
    }
    
    private GameObject CreateFallbackLogEntry(string message)
    {
        GameObject logEntry = new GameObject("LogEntry");
        logEntry.transform.SetParent(logContentContainer);
        
        RectTransform rectTransform = logEntry.GetComponent<RectTransform>();
        if (rectTransform == null)
            rectTransform = logEntry.AddComponent<RectTransform>();
        
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 0);
        rectTransform.pivot = new Vector2(0.5f, 0);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI text = logEntry.AddComponent<TextMeshProUGUI>();
        
        if (text.font == null)
        {
            TMP_FontAsset defaultFont = Resources.GetBuiltinResource<TMP_FontAsset>("Legacy/SDF - Default");
            if (defaultFont != null)
                text.font = defaultFont;
            else
            {
                TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                if (fonts.Length > 0)
                    text.font = fonts[0];
            }
        }
        
        text.fontSize = 18;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = Color.white;
        text.raycastTarget = false;
        text.maskable = true;
        
        ContentSizeFitter sizeFitter = logEntry.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        return logEntry;
    }
    
    private void ClampContentPosition(RectTransform contentRect, RectTransform viewportRect)
    {
        float contentHeight = contentRect.sizeDelta.y;
        float viewportHeight = viewportRect.rect.height;
        Vector2 anchoredPos = contentRect.anchoredPosition;
        
        if (contentHeight > viewportHeight)
        {
            float maxScrollY = contentHeight - viewportHeight;
            anchoredPos.y = Mathf.Clamp(anchoredPos.y, 0, maxScrollY);
        }
        else
        {
            anchoredPos.y = 0;
        }
        
        contentRect.anchoredPosition = anchoredPos;
    }
    
    private System.Collections.IEnumerator ClampContentSize()
    {
        yield return null;
        yield return null;
        
        if (logContentContainer == null || scrollRect == null || scrollRect.viewport == null)
            yield break;
        
        RectTransform contentRect = logContentContainer.GetComponent<RectTransform>();
        RectTransform viewportRect = scrollRect.viewport.GetComponent<RectTransform>();
        
        if (contentRect == null || viewportRect == null)
            yield break;
        
        // Calculate total height
        VerticalLayoutGroup layoutGroup = logContentContainer.GetComponent<VerticalLayoutGroup>();
        float totalHeight = 0f;
        int childCount = 0;
        
        foreach (Transform child in logContentContainer)
        {
            RectTransform childRect = child.GetComponent<RectTransform>();
            if (childRect != null && childRect.gameObject.activeSelf)
            {
                totalHeight += childRect.rect.height;
                childCount++;
            }
        }
        
        if (layoutGroup != null)
        {
            if (childCount > 1)
                totalHeight += layoutGroup.spacing * (childCount - 1);
            totalHeight += layoutGroup.padding.top + layoutGroup.padding.bottom;
        }
        
        // Update size
        Vector2 sizeDelta = contentRect.sizeDelta;
        sizeDelta.x = 0;
        sizeDelta.y = Mathf.Max(totalHeight, 0);
        
        const float MAX_HEIGHT = 10000f;
        if (sizeDelta.y > MAX_HEIGHT)
        {
            Debug.LogWarning($"GameLog: Content height ({sizeDelta.y}) exceeded maximum ({MAX_HEIGHT})");
            sizeDelta.y = MAX_HEIGHT;
        }
        
        contentRect.sizeDelta = sizeDelta;
        
        // Update ScrollRect bounds
        if (scrollRect != null)
        {
            scrollRect.CalculateLayoutInputHorizontal();
            scrollRect.CalculateLayoutInputVertical();
            scrollRect.SetLayoutVertical();
            scrollRect.SetLayoutHorizontal();
            Canvas.ForceUpdateCanvases();
            
            if (scrollRect.movementType != ScrollRect.MovementType.Clamped)
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
            
            ClampContentPosition(contentRect, viewportRect);
        }
    }
    
    private string FormatLogMessage(string message, LogType logType)
    {
        string timeStamp = System.DateTime.Now.ToString("HH:mm:ss");
        return $"[{timeStamp}] {message}";
    }
    
    private System.Collections.IEnumerator ScrollToBottom()
    {
        yield return null;
        yield return null;
        
        if (scrollRect != null && logContentContainer != null && scrollRect.viewport != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.verticalNormalizedPosition = 0f;
            scrollRect.CalculateLayoutInputVertical();
            scrollRect.SetLayoutVertical();
            Canvas.ForceUpdateCanvases();
        }
    }
    
    private void OnLevelUp(int oldLevel, int newLevel)
    {
        List<StatChange> statChanges = CalculateStatChanges(oldLevel, newLevel);
        
        AddLogEntry($"Level Up! You went from level {oldLevel} to level {newLevel}", LogType.Success);
        
        foreach (StatChange change in statChanges)
            AddLogEntry($"  • {change.GetDisplayText()}", LogType.Info);
    }
    
    private List<StatChange> CalculateStatChanges(int oldLevel, int newLevel)
    {
        List<StatChange> changes = new List<StatChange>();
        
        if (CharacterManager.Instance == null) return changes;
        
        float oldHealth = CharacterManager.Instance.GetMaxHealthAtLevel(oldLevel);
        float newHealth = CharacterManager.Instance.GetMaxHealthAtLevel(newLevel);
        if (oldHealth != newHealth)
            changes.Add(new StatChange("Health", oldHealth, newHealth, "{0:F0}"));
        
        float oldAttack = CharacterManager.Instance.GetBaseAttackAtLevel(oldLevel);
        float newAttack = CharacterManager.Instance.GetBaseAttackAtLevel(newLevel);
        if (oldAttack != newAttack)
            changes.Add(new StatChange("Attack", oldAttack, newAttack, "{0:F0}"));
        
        float oldCritChance = CharacterManager.Instance.GetBaseCritChanceAtLevel(oldLevel);
        float newCritChance = CharacterManager.Instance.GetBaseCritChanceAtLevel(newLevel);
        if (oldCritChance != newCritChance)
            changes.Add(new StatChange("Crit Chance", oldCritChance * 100f, newCritChance * 100f, "{0:F1}%"));
        
        return changes;
    }
    
    public void ToggleLog()
    {
        if (logPanel != null)
            logPanel.SetActive(!logPanel.activeSelf);
    }
    
    public void ShowLog()
    {
        if (logPanel != null)
            logPanel.SetActive(true);
    }
    
    public void HideLog()
    {
        if (logPanel != null)
            logPanel.SetActive(false);
    }
    
    public void ClearLog()
    {
        foreach (GameObject entry in logEntries)
        {
            if (entry != null)
                Destroy(entry);
        }
        logEntries.Clear();
    }
    
    public void ToggleViewport()
    {
        if (scrollRect != null && scrollRect.viewport != null)
            scrollRect.viewport.gameObject.SetActive(!scrollRect.viewport.gameObject.activeSelf);
    }
    
    public void ShowViewport()
    {
        if (scrollRect != null && scrollRect.viewport != null)
            scrollRect.viewport.gameObject.SetActive(true);
    }
    
    public void HideViewport()
    {
        if (scrollRect != null && scrollRect.viewport != null)
            scrollRect.viewport.gameObject.SetActive(false);
    }
}

public enum LogType
{
    Info,
    Success,
    Warning,
    Error
}

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
