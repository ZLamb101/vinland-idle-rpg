using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Projectile fired by the hero toward an enemy.
/// Moves in a straight line and damages the target on hit.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Visual")]
    public Image projectileImage;
    public RectTransform rectTransform;
    
    [Header("Movement")]
    public float speed = 1000f; // pixels per second
    public float hitRadius = 30f; // Distance at which projectile hits target (pixels)
    
    private Vector2 targetPosition;
    private float damage;
    private bool isMoving = false;
    
    private System.Action<Projectile> onHitCallback;
    private System.Action<Projectile> onMissCallback;
    
    void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        if (projectileImage == null)
            projectileImage = GetComponent<Image>();
    }
    
    /// <summary>
    /// Initialize and launch the projectile toward a target
    /// </summary>
    public void Launch(Vector2 startPosition, Vector2 targetPos, float projectileDamage, System.Action<Projectile> onHit, System.Action<Projectile> onMiss = null)
    {
        rectTransform.anchoredPosition = startPosition;
        // Only use X component for horizontal movement, keep Y from start position
        targetPosition = new Vector2(targetPos.x, startPosition.y);
        damage = projectileDamage;
        onHitCallback = onHit;
        onMissCallback = onMiss;
        isMoving = true;
        gameObject.SetActive(true);
    }
    
    void Update()
    {
        if (!isMoving) return;
        
        // Move toward target horizontally only
        Vector2 currentPos = rectTransform.anchoredPosition;
        
        // Only calculate horizontal distance
        float distanceToTarget = Mathf.Abs(targetPosition.x - currentPos.x);
        float moveDistance = speed * Time.deltaTime;
        
        // Check if we're within hit radius
        if (distanceToTarget <= hitRadius)
        {
            // Hit target - position at hit point
            float hitX = targetPosition.x - (Mathf.Sign(targetPosition.x - currentPos.x) * hitRadius);
            rectTransform.anchoredPosition = new Vector2(hitX, currentPos.y);
            OnHit();
        }
        else if (moveDistance >= distanceToTarget)
        {
            // Would overshoot, hit now
            rectTransform.anchoredPosition = new Vector2(targetPosition.x, currentPos.y);
            OnHit();
        }
        else
        {
            // Move horizontally only
            float directionX = Mathf.Sign(targetPosition.x - currentPos.x); // -1 for left, +1 for right
            float newX = currentPos.x + directionX * moveDistance;
            
            // Clamp to not overshoot target
            if (directionX > 0)
                newX = Mathf.Min(newX, targetPosition.x - hitRadius);
            else
                newX = Mathf.Max(newX, targetPosition.x + hitRadius);
            
            rectTransform.anchoredPosition = new Vector2(newX, currentPos.y);
        }
    }
    
    void OnHit()
    {
        isMoving = false;
        onHitCallback?.Invoke(this);
    }
    
    /// <summary>
    /// Call when projectile misses or is destroyed
    /// </summary>
    public void OnMiss()
    {
        isMoving = false;
        onMissCallback?.Invoke(this);
    }
    
    public float GetDamage() => damage;
    
    public void ResetProjectile()
    {
        isMoving = false;
        gameObject.SetActive(false);
    }
}

