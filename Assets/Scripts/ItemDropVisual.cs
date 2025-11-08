using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Visual representation of an item drop that rises upward and fades out.
/// Used to show items that drop when monsters die.
/// </summary>
public class ItemDropVisual : MonoBehaviour
{
    [Header("Visual Components")]
    public Image itemIcon;
    public TextMeshProUGUI quantityText;
    public RectTransform rectTransform;
    public CanvasGroup canvasGroup;
    
    [Header("Animation Settings")]
    [Tooltip("Distance in pixels the item rises upward")]
    public float riseDistance = 120f;
    
    [Tooltip("Total duration of the animation in seconds")]
    public float animationDuration = 1.8f;
    
    [Tooltip("Delay before fade out starts (in seconds)")]
    public float fadeStartDelay = 0.5f;
    
    private Vector2 startPosition;
    private Coroutine animationCoroutine;
    
    void Awake()
    {
        // Ensure components exist
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Initialize canvas group
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
    
    /// <summary>
    /// Initialize the drop visual with item data
    /// </summary>
    public void Setup(Sprite icon, int quantity, Vector2 spawnPosition)
    {
        // Set icon
        if (itemIcon != null && icon != null)
        {
            itemIcon.sprite = icon;
            itemIcon.gameObject.SetActive(true);
        }
        
        // Set quantity text (only show if quantity > 1)
        if (quantityText != null)
        {
            if (quantity > 1)
            {
                quantityText.text = quantity.ToString();
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }
        
        // Set starting position
        startPosition = spawnPosition;
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = spawnPosition;
        }
        
        // Start animation
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        
        animationCoroutine = StartCoroutine(PlayDropAnimation());
    }
    
    /// <summary>
    /// Animation coroutine that moves the item upward and fades it out
    /// </summary>
    IEnumerator PlayDropAnimation()
    {
        Vector2 endPosition = startPosition + new Vector2(0, riseDistance);
        float elapsed = 0f;
        
        // Phase 1: Rise up (with fade starting after delay)
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            // Smooth upward movement (ease out)
            float easedT = 1f - Mathf.Pow(1f - t, 3f); // Cubic ease out
            Vector2 currentPos = Vector2.Lerp(startPosition, endPosition, easedT);
            
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = currentPos;
            }
            
            // Fade out starts after delay
            if (elapsed >= fadeStartDelay)
            {
                float fadeElapsed = elapsed - fadeStartDelay;
                float fadeDuration = animationDuration - fadeStartDelay;
                float fadeT = Mathf.Clamp01(fadeElapsed / fadeDuration);
                
                // Smooth fade out
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeT);
                }
            }
            
            yield return null;
        }
        
        // Ensure final state
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = endPosition;
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        // Destroy this GameObject
        Destroy(gameObject);
    }
    
    void OnDestroy()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
    }
}

