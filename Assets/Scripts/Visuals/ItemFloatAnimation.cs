using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component for animating a crafted item floating upward and fading out.
/// Used to visually show when an item is successfully crafted.
/// </summary>
public class ItemFloatAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("How far the item floats upward (in pixels)")]
    public float floatDistance = 100f;
    
    [Tooltip("How long the animation takes (in seconds)")]
    public float duration = 1.5f;
    
    [Tooltip("Animation curve for the float movement")]
    public AnimationCurve floatCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Tooltip("Animation curve for the fade out")]
    public AnimationCurve fadeCurve = AnimationCurve.Linear(0, 1, 1, 0);
    
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Image image;
    
    private Vector2 startPosition;
    private float elapsedTime = 0f;
    private bool isAnimating = false;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        image = GetComponent<Image>();
        
        // Add CanvasGroup if it doesn't exist
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    /// <summary>
    /// Start the float animation with a specific item sprite
    /// </summary>
    public void StartAnimation(Sprite itemSprite, Vector2 startPos)
    {
        if (image != null)
        {
            image.sprite = itemSprite;
        }
        
        startPosition = startPos;
        rectTransform.anchoredPosition = startPosition;
        
        elapsedTime = 0f;
        isAnimating = true;
        
        // Make visible
        canvasGroup.alpha = 1f;
        gameObject.SetActive(true);
    }
    
    void Update()
    {
        if (!isAnimating) return;
        
        elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsedTime / duration);
        
        // Update position
        float floatProgress = floatCurve.Evaluate(progress);
        Vector2 newPosition = startPosition + Vector2.up * (floatDistance * floatProgress);
        rectTransform.anchoredPosition = newPosition;
        
        // Update alpha
        float alpha = fadeCurve.Evaluate(progress);
        canvasGroup.alpha = alpha;
        
        // Check if animation is complete
        if (progress >= 1f)
        {
            isAnimating = false;
            gameObject.SetActive(false);
            
            // Optionally destroy this object
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Create and start an item float animation
    /// </summary>
    public static void CreateAndAnimate(Sprite itemSprite, Vector2 startPos, Transform parent)
    {
        // Create a new GameObject for the animation
        GameObject animObj = new GameObject("ItemFloatAnimation");
        animObj.transform.SetParent(parent, false);
        
        // Add required components
        RectTransform rt = animObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(64, 64); // Default size
        
        Image img = animObj.AddComponent<Image>();
        img.sprite = itemSprite;
        img.raycastTarget = false;
        
        ItemFloatAnimation anim = animObj.AddComponent<ItemFloatAnimation>();
        anim.StartAnimation(itemSprite, startPos);
    }
}
