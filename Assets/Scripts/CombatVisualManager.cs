using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the visual combat scene with hero, enemy, projectiles, and health bars.
/// </summary>
public class CombatVisualManager : MonoBehaviour
{
    [Header("Scene References")]
    public RectTransform combatSceneContainer; // Container for all combat visuals
    
    [Header("Hero")]
    public HeroVisual heroVisual;
    public RectTransform heroPosition; // Where hero should be positioned (left side)
    public Projectile projectilePrefab; // Prefab for projectiles
    
    [Header("Enemy")]
    public EnemyVisual enemyVisualPrefab;
    public RectTransform enemySpawnPosition; // Where enemies spawn (right side)
    public RectTransform enemyHealthBarContainer; // Container for enemy health bar
    
    [Header("Enemy Health Bar")]
    public Slider enemyHealthBar;
    public TextMeshProUGUI enemyHealthText;
    
    [Header("Projectile Pool")]
    public Transform projectilePool; // Parent for projectiles
    
    [Header("Item Drops")]
    [Tooltip("Horizontal spacing between multiple item drops (in pixels)")]
    public float itemDropSpacing = 60f;
    
    private EnemyVisual currentEnemy;
    private List<GameObject> activeItemDrops = new List<GameObject>(); // Track active drop visuals for cleanup
    
    /// <summary>
    /// Get the current enemy visual (for accessing enemy position)
    /// </summary>
    public EnemyVisual CurrentEnemy => currentEnemy;
    
    void Awake()
    {
        // Create hero visual if needed
        if (heroVisual == null)
        {
            GameObject heroObj = new GameObject("HeroVisual");
            // Make HeroVisual a child of heroPosition if it exists, otherwise combatSceneContainer
            Transform parentTransform = heroPosition != null ? heroPosition : combatSceneContainer;
            heroObj.transform.SetParent(parentTransform);
            heroVisual = heroObj.AddComponent<HeroVisual>();
            heroVisual.rectTransform = heroObj.AddComponent<RectTransform>();
            heroVisual.heroImage = heroObj.AddComponent<Image>();
            
            // If parented to heroPosition, set local position to (0,0) so it follows heroPosition
            if (heroPosition != null)
            {
                heroVisual.rectTransform.anchoredPosition = Vector2.zero;
            }
        }
        
        // Set hero position (only if not already parented to heroPosition)
        if (heroPosition != null && heroVisual != null && heroVisual.transform.parent != heroPosition)
        {
            heroVisual.rectTransform.anchoredPosition = heroPosition.anchoredPosition;
        }
        
        // Setup projectile pool
        if (projectilePool == null)
        {
            GameObject poolObj = new GameObject("ProjectilePool");
            poolObj.transform.SetParent(combatSceneContainer);
            projectilePool = poolObj.transform;
        }
    }
    
    void Start()
    {
        // Subscribe to combat events for health bar updates
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnMonsterHealthChanged += UpdateEnemyHealth;
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnMonsterHealthChanged -= UpdateEnemyHealth;
        }
    }
    
    /// <summary>
    /// Initialize combat with hero and enemy
    /// </summary>
    public void InitializeCombat(Sprite heroSprite, MonsterData monsterData)
    {
        // Setup hero
        if (heroVisual != null)
        {
            heroVisual.Setup(heroSprite);
            
            // Position hero - if parented to heroPosition, use local position, otherwise use world position
            if (heroPosition != null)
            {
                if (heroVisual.transform.parent == heroPosition)
                {
                    // Already parented, just ensure local position is (0,0)
                    heroVisual.rectTransform.anchoredPosition = Vector2.zero;
                }
                else
                {
                    // Not parented, copy position
                    heroVisual.rectTransform.anchoredPosition = heroPosition.anchoredPosition;
                }
            }
            
            // Set projectile prefab if not already set
            if (heroVisual.projectilePrefab == null && projectilePrefab != null)
            {
                heroVisual.projectilePrefab = projectilePrefab;
            }
        }
        
        // Spawn enemy
        SpawnEnemy(monsterData);
    }
    
    /// <summary>
    /// Spawn a new enemy
    /// </summary>
    void SpawnEnemy(MonsterData monsterData)
    {
        if (enemyVisualPrefab == null)
        {
            Debug.LogWarning("CombatVisualManager: No enemy visual prefab assigned!");
            return;
        }
        
        // Destroy old enemy if exists
        if (currentEnemy != null)
        {
            Destroy(currentEnemy.gameObject);
        }
        
        // Create new enemy - parent to enemySpawnPosition if it exists, otherwise combatSceneContainer
        Transform parentTransform = enemySpawnPosition != null ? enemySpawnPosition : combatSceneContainer;
        GameObject enemyObj = Instantiate(enemyVisualPrefab.gameObject, parentTransform);
        currentEnemy = enemyObj.GetComponent<EnemyVisual>();
        
        // Setup enemy positions
        Vector2 spawnPos;
        if (enemySpawnPosition != null && currentEnemy.transform.parent == enemySpawnPosition)
        {
            // Enemy is parented to spawn position, use local position (0,0)
            spawnPos = Vector2.zero;
        }
        else
        {
            // Enemy is not parented, use world position
            spawnPos = enemySpawnPosition != null ? enemySpawnPosition.anchoredPosition : Vector2.zero;
        }
        
        // Get hero position - convert to same coordinate space as enemy
        Vector2 heroPos;
        if (heroVisual != null)
        {
            heroPos = heroVisual.GetPosition(); // World position
            
            // If enemy is parented, convert hero position to local space relative to enemy's parent
            if (currentEnemy.transform.parent != null && currentEnemy.transform.parent is RectTransform enemyParentRect)
            {
                heroPos = heroPos - enemyParentRect.anchoredPosition;
            }
        }
        else
        {
            heroPos = Vector2.zero;
        }
        
        currentEnemy.Setup(monsterData.monsterSprite, spawnPos, heroPos, monsterData.attackRange, monsterData.flipSprite);
        
        // Set up health bar following
        if (enemyHealthBarContainer != null)
        {
            currentEnemy.healthBarContainer = enemyHealthBarContainer;
        }
        
        // Set callback for when enemy reaches attack range
        currentEnemy.SetOnReachAttackRange(() => {
            // Enemy reached attack range - combat manager will handle attack timing
        });
    }
    
    /// <summary>
    /// Hero attacks - spawn projectile toward enemy
    /// </summary>
    public void HeroAttack(float damage, System.Action<float> onHit)
    {
        if (heroVisual == null || currentEnemy == null)
            return;
        
        // Ensure projectile prefab is set
        if (heroVisual.projectilePrefab == null && projectilePrefab != null)
        {
            heroVisual.projectilePrefab = projectilePrefab;
        }
        
        Vector2 targetPos = currentEnemy.GetPosition();
        
        heroVisual.Attack(
            targetPos,
            damage,
            (projectile) => {
                // Projectile hit - move to projectile pool for organization
                if (projectilePool != null && projectile.transform.parent != projectilePool)
                {
                    projectile.transform.SetParent(projectilePool);
                }
                
                float dealtDamage = projectile.GetDamage();
                onHit?.Invoke(dealtDamage);
                Destroy(projectile.gameObject);
            },
            (projectile) => {
                // Projectile miss (shouldn't happen in auto-combat, but handle it)
                Destroy(projectile.gameObject);
            }
        );
    }
    
    /// <summary>
    /// Enemy attacks - play swipe animation
    /// </summary>
    public void EnemyAttack(System.Action onComplete)
    {
        if (currentEnemy == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        currentEnemy.PerformAttack(onComplete);
    }
    
    /// <summary>
    /// Update enemy health bar
    /// </summary>
    public void UpdateEnemyHealth(float current, float max)
    {
        if (enemyHealthBar != null)
        {
            enemyHealthBar.maxValue = max;
            enemyHealthBar.value = Mathf.Max(0f, current);
        }
        
        if (enemyHealthText != null)
        {
            enemyHealthText.text = $"{Mathf.Max(0f, current):F0} / {max:F0}";
        }
    }
    
    /// <summary>
    /// Check if enemy is in attack range
    /// </summary>
    public bool IsEnemyInAttackRange()
    {
        return currentEnemy != null && currentEnemy.IsInAttackRange();
    }
    
    /// <summary>
    /// Show visual item drops when a monster dies
    /// </summary>
    public void ShowItemDrops(List<MonsterDropEntry> droppedItems, Vector2 enemyDeathPosition)
    {
        if (droppedItems == null || droppedItems.Count == 0)
            return;
        
        if (combatSceneContainer == null)
        {
            Debug.LogWarning("CombatVisualManager: No combatSceneContainer assigned for item drops!");
            return;
        }
        
        // Get the enemy position in the correct coordinate space
        Vector2 localEnemyPosition = Vector2.zero;
        
        // Try to get the actual enemy position if it still exists
        if (currentEnemy != null && currentEnemy.rectTransform != null)
        {
            RectTransform enemyRect = currentEnemy.rectTransform;
            
            // Check if enemy is a direct child of combatSceneContainer
            if (enemyRect.parent == combatSceneContainer)
            {
                // Enemy is a child of combatSceneContainer, use its local anchored position directly
                localEnemyPosition = enemyRect.anchoredPosition;
            }
            else
            {
                // Enemy is not a direct child - use RectTransformUtility to convert position
                Vector3 enemyWorldPos = enemyRect.position;
                Camera uiCamera = null;
                Canvas canvas = combatSceneContainer.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    uiCamera = canvas.worldCamera;
                }
                
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    combatSceneContainer,
                    RectTransformUtility.WorldToScreenPoint(uiCamera, enemyWorldPos),
                    uiCamera,
                    out localPoint))
                {
                    localEnemyPosition = localPoint;
                }
                else
                {
                    // Fallback: use world anchored position conversion
                    Vector2 enemyWorldAnchoredPos = currentEnemy.GetPosition();
                    RectTransform parentRect = combatSceneContainer.parent as RectTransform;
                    if (parentRect != null && enemyRect.parent != parentRect)
                    {
                        localEnemyPosition = enemyWorldAnchoredPos - parentRect.anchoredPosition;
                    }
                    else
                    {
                        localEnemyPosition = enemyWorldAnchoredPos;
                    }
                }
            }
        }
        else
        {
            // No current enemy - use the provided death position and convert if needed
            RectTransform parentRect = combatSceneContainer.parent as RectTransform;
            if (parentRect != null)
            {
                localEnemyPosition = enemyDeathPosition - parentRect.anchoredPosition;
            }
            else
            {
                localEnemyPosition = enemyDeathPosition;
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
            
            // Create item drop visual GameObject
            GameObject dropObj = new GameObject($"ItemDrop_{dropEntry.item.itemName}");
            dropObj.transform.SetParent(combatSceneContainer);
            
            // Track for cleanup
            activeItemDrops.Add(dropObj);
            
            // Add RectTransform
            RectTransform dropRect = dropObj.AddComponent<RectTransform>();
            dropRect.sizeDelta = new Vector2(64f, 64f); // Standard item icon size
            dropRect.anchoredPosition = spawnPos;
            
            // Add CanvasGroup for fade effect
            CanvasGroup canvasGroup = dropObj.AddComponent<CanvasGroup>();
            
            // Add Image for item icon
            Image iconImage = dropObj.AddComponent<Image>();
            iconImage.sprite = dropEntry.item.icon;
            iconImage.preserveAspect = true;
            
            // Add quantity text if quantity > 1
            if (dropEntry.quantity > 1)
            {
                GameObject textObj = new GameObject("QuantityText");
                textObj.transform.SetParent(dropObj.transform);
                
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.6f, 0.6f);
                textRect.anchorMax = new Vector2(1f, 1f);
                textRect.sizeDelta = Vector2.zero;
                textRect.anchoredPosition = Vector2.zero;
                
                TextMeshProUGUI quantityText = textObj.AddComponent<TextMeshProUGUI>();
                quantityText.text = dropEntry.quantity.ToString();
                quantityText.fontSize = 16;
                quantityText.color = Color.white;
                quantityText.alignment = TextAlignmentOptions.BottomRight;
                quantityText.fontStyle = FontStyles.Bold;
                
                // Add outline for better visibility
                var outline = textObj.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(1f, -1f);
            }
            
            // Add ItemDropVisual component
            ItemDropVisual dropVisual = dropObj.AddComponent<ItemDropVisual>();
            dropVisual.itemIcon = iconImage;
            dropVisual.rectTransform = dropRect;
            dropVisual.canvasGroup = canvasGroup;
            
            // Find quantity text if it exists
            if (dropEntry.quantity > 1)
            {
                TextMeshProUGUI qtyText = dropObj.GetComponentInChildren<TextMeshProUGUI>();
                dropVisual.quantityText = qtyText;
            }
            
            // Setup and start animation
            dropVisual.Setup(dropEntry.item.icon, dropEntry.quantity, spawnPos);
            
            // Remove from tracking list when destroyed
            dropVisual.gameObject.AddComponent<ItemDropCleanup>().OnDestroyed += () => {
                activeItemDrops.Remove(dropObj);
            };
        }
    }
    
    /// <summary>
    /// Helper component to track when item drops are destroyed
    /// </summary>
    private class ItemDropCleanup : MonoBehaviour
    {
        public System.Action OnDestroyed;
        
        void OnDestroy()
        {
            OnDestroyed?.Invoke();
        }
    }
    
    /// <summary>
    /// Clean up combat visuals
    /// </summary>
    public void Cleanup()
    {
        if (currentEnemy != null)
        {
            Destroy(currentEnemy.gameObject);
            currentEnemy = null;
        }
        
        // Clean up projectiles
        if (projectilePool != null)
        {
            foreach (Transform child in projectilePool)
            {
                Destroy(child.gameObject);
            }
        }
        
        // Clean up any active item drop visuals
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
}

