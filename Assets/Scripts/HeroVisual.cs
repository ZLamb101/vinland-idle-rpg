using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Visual representation of the hero character in combat.
/// Handles attack animations and projectile spawning.
/// </summary>
public class HeroVisual : MonoBehaviour
{
    [Header("Visual")]
    public Image heroImage;
    public RectTransform rectTransform;
    
    [Header("Projectile")]
    public Projectile projectilePrefab;
    public RectTransform projectileSpawnPoint; // Where projectiles spawn from
    
    [Header("Animation")]
    public float attackAnimationDuration = 0.2f;
    
    private Vector2 defaultPosition;
    private Vector3 defaultScale;
    private Coroutine attackAnimationCoroutine;
    
    void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        if (heroImage == null)
            heroImage = GetComponent<Image>();
        
        defaultPosition = rectTransform.anchoredPosition;
        defaultScale = rectTransform.localScale;
    }
    
    /// <summary>
    /// Set up the hero sprite
    /// </summary>
    public void Setup(Sprite heroSprite)
    {
        if (heroImage != null && heroSprite != null)
            heroImage.sprite = heroSprite;
    }
    
    /// <summary>
    /// Perform an attack - spawn projectile toward target
    /// </summary>
    public void Attack(Vector2 targetPosition, float damage, System.Action<Projectile> onProjectileHit, System.Action<Projectile> onProjectileMiss)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("HeroVisual: No projectile prefab assigned!");
            return;
        }
        
        // Get spawn position in world space
        Vector2 spawnWorldPos = GetPosition();
        if (projectileSpawnPoint != null)
        {
            // Convert projectile spawn point to world position
            if (projectileSpawnPoint.parent != null && projectileSpawnPoint.parent is RectTransform parentRect)
            {
                spawnWorldPos = parentRect.anchoredPosition + projectileSpawnPoint.anchoredPosition;
            }
            else
            {
                spawnWorldPos = projectileSpawnPoint.anchoredPosition;
            }
        }
        
        // Convert target position and spawn position to local space relative to projectile's parent
        Transform projectileParent = transform.parent != null ? transform.parent : transform.root;
        Vector2 spawnLocalPos = spawnWorldPos;
        Vector2 targetLocalPos = targetPosition;
        
        // Convert to local space if projectile will be parented
        if (projectileParent is RectTransform parentRectTransform)
        {
            spawnLocalPos = spawnWorldPos - parentRectTransform.anchoredPosition;
            targetLocalPos = targetPosition - parentRectTransform.anchoredPosition;
        }
        
        // Create projectile - use parent transform
        Projectile projectile = Instantiate(projectilePrefab, projectileParent);
        projectile.Launch(spawnLocalPos, targetLocalPos, damage, onProjectileHit, onProjectileMiss);
        
        // Play attack animation (simple scale bounce)
        if (attackAnimationCoroutine != null)
            StopCoroutine(attackAnimationCoroutine);
        attackAnimationCoroutine = StartCoroutine(PlayAttackAnimation());
    }
    
    IEnumerator PlayAttackAnimation()
    {
        float elapsed = 0f;
        float halfDuration = attackAnimationDuration * 0.5f;
        
        // Scale up
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            rectTransform.localScale = Vector3.Lerp(defaultScale, defaultScale * 1.2f, t * t); // Ease out
            yield return null;
        }
        
        elapsed = 0f;
        
        // Scale down
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            rectTransform.localScale = Vector3.Lerp(defaultScale * 1.2f, defaultScale, t * t); // Ease in
            yield return null;
        }
        
        rectTransform.localScale = defaultScale;
        attackAnimationCoroutine = null;
    }
    
    /// <summary>
    /// Reset hero to default state
    /// </summary>
    public void ResetVisual()
    {
        if (rectTransform == null)
            return;
            
        rectTransform.anchoredPosition = defaultPosition;
        rectTransform.localScale = defaultScale;
        if (attackAnimationCoroutine != null)
        {
            StopCoroutine(attackAnimationCoroutine);
            attackAnimationCoroutine = null;
        }
    }
    
    /// <summary>
    /// Unity's Reset() method - called in editor when component is reset
    /// </summary>
    void Reset()
    {
        // Initialize components if needed
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        if (heroImage == null)
            heroImage = GetComponent<Image>();
    }
    
    public Vector2 GetPosition()
    {
        // If parented, return world anchored position, otherwise return local anchored position
        if (rectTransform.parent != null && rectTransform.parent is RectTransform parentRect)
        {
            // Calculate world anchored position: parent's position + local position
            return parentRect.anchoredPosition + rectTransform.anchoredPosition;
        }
        return rectTransform.anchoredPosition;
    }
}

