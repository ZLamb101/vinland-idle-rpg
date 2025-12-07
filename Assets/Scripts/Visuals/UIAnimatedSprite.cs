using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays a sprite animation on a UI Image component.
/// Attach to a GameObject with an Image component.
/// </summary>
public class UIAnimatedSprite : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Array of sprites to cycle through")]
    public Sprite[] frames;
    
    [Tooltip("Frames per second")]
    public float frameRate = 12f;
    
    [Tooltip("Should the animation loop?")]
    public bool loop = false;
    
    [Tooltip("Destroy this GameObject when animation completes (only if not looping)")]
    public bool destroyOnComplete = true;
    
    [Header("Optional: Scale Animation")]
    [Tooltip("Enable scale pulsing effect")]
    public bool animateScale = false;
    public float scaleMin = 0.8f;
    public float scaleMax = 1.2f;
    public float scaleSpeed = 5f;
    
    [Header("Optional: Fade Out")]
    [Tooltip("Fade out over the animation duration")]
    public bool fadeOut = false;
    
    private Image image;
    private int currentFrame = 0;
    private float timer = 0f;
    private float totalTime = 0f;
    private CanvasGroup canvasGroup;
    private float initialAlpha = 1f;
    
    void Awake()
    {
        image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup != null)
        {
            initialAlpha = canvasGroup.alpha;
        }
        
        if (image != null && frames != null && frames.Length > 0)
        {
            image.sprite = frames[0];
        }
    }
    
    void Update()
    {
        if (frames == null || frames.Length == 0 || image == null)
            return;
        
        timer += Time.deltaTime;
        totalTime += Time.deltaTime;
        
        float frameDuration = 1f / frameRate;
        
        // Advance frame
        if (timer >= frameDuration)
        {
            timer -= frameDuration;
            currentFrame++;
            
            if (currentFrame >= frames.Length)
            {
                if (loop)
                {
                    currentFrame = 0;
                }
                else
                {
                    // Animation complete
                    if (destroyOnComplete)
                    {
                        Destroy(gameObject);
                    }
                    else
                    {
                        enabled = false;
                    }
                    return;
                }
            }
            
            image.sprite = frames[currentFrame];
        }
        
        // Optional scale animation
        if (animateScale)
        {
            float scale = Mathf.Lerp(scaleMin, scaleMax, (Mathf.Sin(totalTime * scaleSpeed) + 1f) * 0.5f);
            transform.localScale = Vector3.one * scale;
        }
        
        // Optional fade out
        if (fadeOut && canvasGroup != null)
        {
            float animDuration = frames.Length / frameRate;
            float fadeProgress = Mathf.Clamp01(totalTime / animDuration);
            canvasGroup.alpha = Mathf.Lerp(initialAlpha, 0f, fadeProgress);
        }
    }
    
    /// <summary>
    /// Play the animation from the beginning
    /// </summary>
    public void Play()
    {
        currentFrame = 0;
        timer = 0f;
        totalTime = 0f;
        enabled = true;
        
        if (image != null && frames != null && frames.Length > 0)
        {
            image.sprite = frames[0];
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = initialAlpha;
        }
    }
}
