using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Visual representation of an enemy in combat.
/// Handles movement toward hero, attack animations, and health bar positioning.
/// </summary>
public class EnemyVisual : MonoBehaviour, IPointerClickHandler
{
    [Header("Visual")]
    public Image enemyImage;
    public RectTransform rectTransform;
    
    [Header("Target Indicator")]
    public Image targetArrow; // Red arrow pointing at targeted enemy
    
    [Header("Movement")]
    public float moveSpeed = 200f; // pixels per second
    
    [Header("Attack")]
    public RectTransform attackSwipeEffect; // Visual effect for attack swipe
    public float attackSwipeDuration = 0.3f;
    public float attackAnimationDuration = 0.5f;
    
    [Header("Health Bar")]
    public RectTransform monsterDetailsContainer; // Container that follows enemy (contains health bar, name, swing timer, etc.)
    
    private Vector2 spawnPosition; // Where enemy starts
    private Vector2 targetPosition; // Hero position
    private float attackRange = 100f; // Distance at which enemy can attack
    private bool isMoving = false;
    private bool isInAttackRange = false;
    private bool isRangedEnemy = false; // If true, enemy doesn't need to move to attack
    private int monsterIndex = -1; // Index in the active monsters list
    
    private System.Action onReachAttackRange; // Called when enemy reaches attack range
    private System.Action onAttackComplete; // Called when attack animation completes
    private System.Action onClickCallback; // Called when enemy is clicked
    private Coroutine attackAnimationCoroutine;
    private CanvasGroup swipeCanvasGroup;
    private Button clickButton; // Button component for click detection
    
    void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        if (enemyImage == null)
            enemyImage = GetComponent<Image>();
        
        // Setup canvas group for swipe effect if it doesn't exist
        if (attackSwipeEffect != null)
        {
            swipeCanvasGroup = attackSwipeEffect.GetComponent<CanvasGroup>();
            if (swipeCanvasGroup == null)
                swipeCanvasGroup = attackSwipeEffect.gameObject.AddComponent<CanvasGroup>();
        }
        
        // Setup click detection - add Button component if it doesn't exist
        clickButton = GetComponent<Button>();
        if (clickButton == null)
        {
            clickButton = gameObject.AddComponent<Button>();
        }
        clickButton.onClick.AddListener(OnEnemyClicked);
        
        // Hide target arrow initially
        if (targetArrow != null)
        {
            targetArrow.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Initialize enemy with sprite and positions
    /// </summary>
    public void Setup(Sprite enemySprite, Vector2 spawnPos, Vector2 heroPos, float range, bool flipSprite = false)
    {
        if (enemyImage != null && enemySprite != null)
        {
            enemyImage.sprite = enemySprite;
            
            // Flip only the image sprite, not the whole transform (to avoid flipping children like UI containers)
            if (flipSprite)
            {
                enemyImage.transform.localScale = new Vector3(-1f, 1f, 1f);
            }
            else
            {
                enemyImage.transform.localScale = new Vector3(1f, 1f, 1f);
            }
            
            // Ensure main transform scale is normal (children will use their own scales)
            rectTransform.localScale = Vector3.one;
        }
        
        spawnPosition = spawnPos;
        attackRange = range;
        
        // heroPos is already in local space relative to our parent (converted in CombatVisualManager)
        // Only use X component for horizontal movement, keep Y from spawn position
        targetPosition = new Vector2(heroPos.x, spawnPos.y);
        
        rectTransform.anchoredPosition = spawnPosition;
        
        // Check if this is a ranged enemy (attack range is large enough to attack from spawn)
        // If attack range >= distance to hero, enemy can attack from spawn position
        float distanceToHero = Mathf.Abs(targetPosition.x - spawnPos.x);
        isRangedEnemy = attackRange >= distanceToHero;
        
        // Melee enemies need to move, ranged enemies can attack from spawn
        isMoving = !isRangedEnemy;
        isInAttackRange = isRangedEnemy; // Ranged enemies are immediately in range
        
        // If ranged enemy, immediately notify that we're in range
        if (isRangedEnemy)
        {
            onReachAttackRange?.Invoke();
        }
        
        // Hide attack swipe initially
        if (attackSwipeEffect != null)
        {
            attackSwipeEffect.gameObject.SetActive(false);
            if (swipeCanvasGroup != null)
                swipeCanvasGroup.alpha = 0f;
        }
    }
    
    /// <summary>
    /// Set monster index for identification
    /// </summary>
    public void SetMonsterIndex(int index)
    {
        monsterIndex = index;
    }
    
    /// <summary>
    /// Get monster index
    /// </summary>
    public int GetMonsterIndex()
    {
        return monsterIndex;
    }
    
    /// <summary>
    /// Show or hide target indicator (red arrow)
    /// </summary>
    public void ShowTargetIndicator(bool show)
    {
        if (targetArrow != null)
        {
            targetArrow.gameObject.SetActive(show);
            
            // Position arrow above enemy
            if (show && rectTransform != null)
            {
                RectTransform arrowRect = targetArrow.rectTransform;
                if (arrowRect != null)
                {
                    // Position arrow relative to its parent (should be same parent as enemy or enemy itself)
                    if (arrowRect.parent == rectTransform.parent)
                    {
                        // Same parent - use local position
                        arrowRect.anchoredPosition = rectTransform.anchoredPosition + new Vector2(0, 100f);
                    }
                    else if (arrowRect.parent == rectTransform)
                    {
                        // Arrow is child of enemy - use local position (0, 100)
                        arrowRect.anchoredPosition = new Vector2(0, 100f);
                    }
                    else
                    {
                        // Different parents - convert position
                        Vector2 enemyWorldPos = GetPosition();
                        if (arrowRect.parent != null && arrowRect.parent is RectTransform arrowParentRect)
                        {
                            Vector2 localPos = enemyWorldPos - arrowParentRect.anchoredPosition;
                            arrowRect.anchoredPosition = localPos + new Vector2(0, 100f);
                        }
                        else
                        {
                            arrowRect.anchoredPosition = enemyWorldPos + new Vector2(0, 100f);
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Set callback for when enemy is clicked
    /// </summary>
    public void SetOnClickCallback(System.Action callback)
    {
        onClickCallback = callback;
    }
    
    /// <summary>
    /// Handle click on enemy
    /// </summary>
    void OnEnemyClicked()
    {
        onClickCallback?.Invoke();
    }
    
    /// <summary>
    /// IPointerClickHandler implementation (alternative click detection)
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        OnEnemyClicked();
    }
    
    void Update()
    {
        if (isMoving && !isInAttackRange)
        {
            // Get current position (local space)
            Vector2 currentPos = rectTransform.anchoredPosition;
            
            // Only move horizontally - use X component of target, keep Y from spawn position
            Vector2 horizontalTarget = new Vector2(targetPosition.x, currentPos.y);
            
            // Check distance to target (only horizontal distance)
            float distanceToTarget = Mathf.Abs(horizontalTarget.x - currentPos.x);
            
            if (distanceToTarget <= attackRange)
            {
                // Reached attack range - stop moving
                isInAttackRange = true;
                isMoving = false;
                onReachAttackRange?.Invoke();
            }
            else
            {
                // Move toward target horizontally only
                float directionX = Mathf.Sign(horizontalTarget.x - currentPos.x); // -1 for left, +1 for right
                float moveDistance = moveSpeed * Time.deltaTime;
                float newX = currentPos.x + directionX * moveDistance;
                
                // Clamp to not overshoot target
                if (directionX > 0)
                    newX = Mathf.Min(newX, horizontalTarget.x);
                else
                    newX = Mathf.Max(newX, horizontalTarget.x);
                
                rectTransform.anchoredPosition = new Vector2(newX, currentPos.y);
            }
        }
        
        // Update target arrow position if visible
        if (targetArrow != null && targetArrow.gameObject.activeSelf)
        {
            RectTransform arrowRect = targetArrow.rectTransform;
            if (arrowRect != null)
            {
                // Position arrow relative to its parent
                if (arrowRect.parent == rectTransform.parent)
                {
                    // Same parent - use local position
                    arrowRect.anchoredPosition = rectTransform.anchoredPosition + new Vector2(0, 100f);
                }
                else if (arrowRect.parent == rectTransform)
                {
                    // Arrow is child of enemy - use local position
                    arrowRect.anchoredPosition = new Vector2(0, 100f);
                }
                else
                {
                    // Different parents - convert position
                    Vector2 enemyWorldPos = GetPosition();
                    if (arrowRect.parent != null && arrowRect.parent is RectTransform arrowParentRect)
                    {
                        Vector2 localPos = enemyWorldPos - arrowParentRect.anchoredPosition;
                        arrowRect.anchoredPosition = localPos + new Vector2(0, 100f);
                    }
                    else
                    {
                        arrowRect.anchoredPosition = enemyWorldPos + new Vector2(0, 100f);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Perform attack animation - swipe across screen
    /// </summary>
    public void PerformAttack(System.Action onComplete)
    {
        if (!isInAttackRange)
        {
            onComplete?.Invoke();
            return;
        }
        
        onAttackComplete = onComplete;
        
        // Show swipe effect
        if (attackSwipeEffect != null && swipeCanvasGroup != null)
        {
            if (attackAnimationCoroutine != null)
                StopCoroutine(attackAnimationCoroutine);
            attackAnimationCoroutine = StartCoroutine(PlayAttackSwipeAnimation());
        }
        else
        {
            // No swipe effect, just delay
            if (attackAnimationCoroutine != null)
                StopCoroutine(attackAnimationCoroutine);
            attackAnimationCoroutine = StartCoroutine(PlayAttackDelay());
        }
    }
    
    IEnumerator PlayAttackSwipeAnimation()
    {
        attackSwipeEffect.gameObject.SetActive(true);
        
        // Position swipe at enemy position, extend toward hero
        Vector2 swipeStart = rectTransform.anchoredPosition;
        Vector2 swipeEnd = targetPosition;
        
        // Calculate swipe direction and length
        Vector2 swipeDirection = (swipeEnd - swipeStart).normalized;
        float swipeLength = Vector2.Distance(swipeStart, swipeEnd);
        
        // Set swipe position and rotation
        attackSwipeEffect.anchoredPosition = swipeStart;
        attackSwipeEffect.sizeDelta = new Vector2(swipeLength, 50f); // Width of swipe
        
        // Rotate swipe toward target
        float angle = Mathf.Atan2(swipeDirection.y, swipeDirection.x) * Mathf.Rad2Deg;
        attackSwipeEffect.localEulerAngles = new Vector3(0, 0, angle);
        
        // Fade in
        float elapsed = 0f;
        float fadeInDuration = attackSwipeDuration * 0.3f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            swipeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        
        swipeCanvasGroup.alpha = 1f;
        
        // Fade out
        elapsed = 0f;
        float fadeOutDuration = attackSwipeDuration * 0.7f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            swipeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        
        swipeCanvasGroup.alpha = 0f;
        attackSwipeEffect.gameObject.SetActive(false);
        attackAnimationCoroutine = null;
        onAttackComplete?.Invoke();
    }
    
    IEnumerator PlayAttackDelay()
    {
        yield return new WaitForSeconds(attackAnimationDuration);
        attackAnimationCoroutine = null;
        onAttackComplete?.Invoke();
    }
    
    /// <summary>
    /// Set callback for when enemy reaches attack range
    /// </summary>
    public void SetOnReachAttackRange(System.Action callback)
    {
        onReachAttackRange = callback;
    }
    
    /// <summary>
    /// Get current position
    /// </summary>
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
    
    /// <summary>
    /// Check if enemy is in attack range
    /// </summary>
    public bool IsInAttackRange()
    {
        // For ranged enemies, always check current distance
        if (isRangedEnemy)
        {
            Vector2 currentPos = rectTransform.anchoredPosition;
            float distanceToHero = Mathf.Abs(targetPosition.x - currentPos.x);
            return distanceToHero <= attackRange;
        }
        
        // For melee enemies, use cached state
        return isInAttackRange;
    }
    
    /// <summary>
    /// Reset enemy to spawn position
    /// </summary>
    public void ResetVisual()
    {
        if (rectTransform == null)
            return;
            
        rectTransform.anchoredPosition = spawnPosition;
        isMoving = !isRangedEnemy; // Ranged enemies don't move
        isInAttackRange = isRangedEnemy; // Ranged enemies are immediately in range
        if (attackSwipeEffect != null)
            attackSwipeEffect.gameObject.SetActive(false);
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
        if (enemyImage == null)
            enemyImage = GetComponent<Image>();
    }
}

