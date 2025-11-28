using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Handles item drop visuals when enemies are defeated.
/// Extracted from CombatSceneController for single responsibility.
/// </summary>
public class ItemDropController : MonoBehaviour
{
    [Header("References")]
    public RectTransform combatSceneContainer;
    
    [Header("Settings")]
    [Tooltip("Horizontal spacing between multiple item drops (in pixels)")]
    public float itemDropSpacing = 60f;
    
    // Tracking for cleanup
    private List<GameObject> activeItemDrops = new List<GameObject>();
    
    // Callback for getting enemy position
    private System.Func<int, EnemyVisual> getEnemyVisual;
    
    /// <summary>
    /// Initialize the controller with required dependencies
    /// </summary>
    public void Initialize(RectTransform container, System.Func<int, EnemyVisual> enemyVisualFunc)
    {
        combatSceneContainer = container;
        getEnemyVisual = enemyVisualFunc;
    }
    
    /// <summary>
    /// Show visual item drops when a monster dies
    /// </summary>
    public void ShowItemDrops(List<MonsterDropEntry> droppedItems, Vector2 enemyDeathPosition, int monsterIndex)
    {
        if (droppedItems == null || droppedItems.Count == 0)
            return;
        
        if (combatSceneContainer == null)
            return;
        
        // Use the provided death position
        Vector2 localEnemyPosition = enemyDeathPosition;
        
        // Try to get the actual enemy position if it still exists
        if (getEnemyVisual != null && monsterIndex >= 0)
        {
            EnemyVisual enemy = getEnemyVisual(monsterIndex);
            if (enemy != null && enemy.rectTransform != null)
            {
                localEnemyPosition = GetLocalPosition(enemy.rectTransform);
            }
        }
        
        // Calculate horizontal offset for multiple items (centered around enemy position)
        float totalWidth = (droppedItems.Count - 1) * itemDropSpacing;
        float startX = localEnemyPosition.x - (totalWidth * 0.5f);
        
        for (int i = 0; i < droppedItems.Count; i++)
        {
            MonsterDropEntry dropEntry = droppedItems[i];
            
            if (dropEntry.item == null || dropEntry.item.icon == null)
                continue;
            
            // Calculate spawn position with horizontal offset
            Vector2 spawnPos = new Vector2(startX + (i * itemDropSpacing), localEnemyPosition.y);
            
            // Create item drop visual
            CreateItemDropVisual(dropEntry, spawnPos);
        }
    }
    
    /// <summary>
    /// Create a visual for a dropped item
    /// </summary>
    void CreateItemDropVisual(MonsterDropEntry dropEntry, Vector2 spawnPos)
    {
        // Create item drop visual GameObject
        GameObject dropObj = new GameObject($"ItemDrop_{dropEntry.item.itemName}");
        dropObj.transform.SetParent(combatSceneContainer);
        
        // Track for cleanup
        activeItemDrops.Add(dropObj);
        
        // Add RectTransform
        RectTransform dropRect = dropObj.AddComponent<RectTransform>();
        dropRect.sizeDelta = new Vector2(64f, 64f);
        dropRect.anchoredPosition = spawnPos;
        
        // Add CanvasGroup for fade effect
        CanvasGroup canvasGroup = dropObj.AddComponent<CanvasGroup>();
        
        // Add Image for item icon
        Image iconImage = dropObj.AddComponent<Image>();
        iconImage.sprite = dropEntry.item.icon;
        iconImage.preserveAspect = true;
        
        // Add quantity text if quantity > 1
        TextMeshProUGUI qtyText = null;
        if (dropEntry.quantity > 1)
        {
            qtyText = CreateQuantityText(dropObj, dropEntry.quantity);
        }
        
        // Add ItemDropVisual component
        ItemDropVisual dropVisual = dropObj.AddComponent<ItemDropVisual>();
        dropVisual.itemIcon = iconImage;
        dropVisual.rectTransform = dropRect;
        dropVisual.canvasGroup = canvasGroup;
        dropVisual.quantityText = qtyText;
        
        // Setup and start animation
        dropVisual.Setup(dropEntry.item.icon, dropEntry.quantity, spawnPos);
        
        // Remove from tracking list when destroyed
        ItemDropCleanup cleanup = dropObj.AddComponent<ItemDropCleanup>();
        cleanup.controller = this;
        cleanup.trackedObject = dropObj;
    }
    
    /// <summary>
    /// Create quantity text for item drop
    /// </summary>
    TextMeshProUGUI CreateQuantityText(GameObject parent, int quantity)
    {
        GameObject textObj = new GameObject("QuantityText");
        textObj.transform.SetParent(parent.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.6f, 0.6f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI quantityText = textObj.AddComponent<TextMeshProUGUI>();
        quantityText.text = quantity.ToString();
        quantityText.fontSize = 16;
        quantityText.color = Color.white;
        quantityText.alignment = TextAlignmentOptions.BottomRight;
        quantityText.fontStyle = FontStyles.Bold;
        
        // Add outline for better visibility
        var outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1f, -1f);
        
        return quantityText;
    }
    
    /// <summary>
    /// Get local position of a RectTransform relative to combatSceneContainer
    /// </summary>
    Vector2 GetLocalPosition(RectTransform rectTransform)
    {
        if (rectTransform == null || combatSceneContainer == null)
            return Vector2.zero;
        
        if (rectTransform.parent == combatSceneContainer)
        {
            return rectTransform.anchoredPosition;
        }
        else
        {
            Vector3 worldPos = rectTransform.position;
            Camera uiCamera = null;
            Canvas canvas = combatSceneContainer.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = canvas.worldCamera;
            }
            
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                combatSceneContainer,
                RectTransformUtility.WorldToScreenPoint(uiCamera, worldPos),
                uiCamera,
                out Vector2 localPoint))
            {
                return localPoint;
            }
            return Vector2.zero;
        }
    }
    
    /// <summary>
    /// Remove a tracked item drop from the list
    /// </summary>
    public void RemoveTrackedDrop(GameObject dropObj)
    {
        activeItemDrops.Remove(dropObj);
    }
    
    /// <summary>
    /// Clean up all active item drops
    /// </summary>
    public void Cleanup()
    {
        foreach (GameObject drop in activeItemDrops)
        {
            if (drop != null)
            {
                Destroy(drop);
            }
        }
        activeItemDrops.Clear();
        
        // Also clean up any ItemDrop visuals that might be children of combatSceneContainer
        if (combatSceneContainer != null)
        {
            ItemDropVisual[] remainingDrops = combatSceneContainer.GetComponentsInChildren<ItemDropVisual>();
            foreach (ItemDropVisual drop in remainingDrops)
            {
                if (drop != null && drop.gameObject != null)
                {
                    Destroy(drop.gameObject);
                }
            }
        }
    }
    
    void OnDestroy()
    {
        Cleanup();
    }
}

/// <summary>
/// Helper component to track when item drops are destroyed
/// </summary>
public class ItemDropCleanup : MonoBehaviour
{
    public ItemDropController controller;
    public GameObject trackedObject;
    
    void OnDestroy()
    {
        if (controller != null && trackedObject != null)
        {
            controller.RemoveTrackedDrop(trackedObject);
        }
    }
}

