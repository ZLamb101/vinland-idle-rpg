using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    
    private EnemyVisual currentEnemy;
    
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
    }
}

