using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the visual combat scene with hero, enemy, projectiles, and health bars.
/// </summary>
public class CombatSceneController : MonoBehaviour
{
    [Header("Scene References")]
    public RectTransform combatSceneContainer; // Container for all combat visuals
    
    [Header("Hero")]
    public HeroVisual heroVisual;
    public RectTransform heroPosition; // Where hero should be positioned (left side)
    public Projectile projectilePrefab; // Prefab for projectiles
    
    [Header("Enemy")]
    public EnemyVisual enemyVisualPrefab; // Prefab should have monsterDetailsContainer as a child
    public RectTransform enemySpawnPosition; // Where enemies spawn (right side)
    
    [Header("Projectile Pool")]
    public Transform projectilePool; // Parent for projectiles
    
    [Header("Item Drops")]
    [Tooltip("Horizontal spacing between multiple item drops (in pixels)")]
    public float itemDropSpacing = 60f;
    
    [Header("Monster Spacing")]
    [Tooltip("Y-axis spacing between monsters (in pixels)")]
    public float monsterYSpacing = 60f;
    [Tooltip("X-axis offset for alternating monsters to avoid overlap (in pixels)")]
    public float monsterXSpacing = 100f;
    
    private List<EnemyVisual> activeEnemies = new List<EnemyVisual>();
    private List<GameObject> activeItemDrops = new List<GameObject>(); // Track active drop visuals for cleanup
    private int currentTargetIndex = 0;
    private List<GameObject> activeDamageTexts = new List<GameObject>(); // Track active damage text GameObjects for cleanup
    
    private ICombatService combatService; // Cached combat service reference
    private TMP_FontAsset damageTextFont; // Cached font for damage text
    
    /// <summary>
    /// Get enemy visual by index
    /// </summary>
    public EnemyVisual GetEnemyVisual(int index)
    {
        if (index >= 0 && index < activeEnemies.Count)
            return activeEnemies[index];
        return null;
    }
    
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
        // Get combat service
        combatService = Services.Get<ICombatService>();
        
        // Load and cache "Charlie don't surf SDF" font for damage text
        damageTextFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Charlie don't surf SDF");
        
        // Subscribe to combat events for health bar updates
        if (combatService != null)
        {
            EventBus.Subscribe<MonsterHealthChangedEvent>(UpdateEnemyHealth);
            EventBus.Subscribe<MonsterAttackProgressEvent>(OnMonsterAttackProgress);
            EventBus.Subscribe<TargetChangedEvent>(SetTargetIndicator);
            EventBus.Subscribe<PlayerDamageDealtEvent>(OnPlayerDamageDealt);
        }
    }
    
    void OnDestroy()
    {
        // Clean up all visuals before destroying
        Cleanup();
        
        // Unsubscribe from events
        EventBus.Unsubscribe<MonsterHealthChangedEvent>(UpdateEnemyHealth);
        EventBus.Unsubscribe<MonsterAttackProgressEvent>(OnMonsterAttackProgress);
        EventBus.Unsubscribe<TargetChangedEvent>(SetTargetIndicator);
        EventBus.Unsubscribe<PlayerDamageDealtEvent>(OnPlayerDamageDealt);
    }
    
    void OnDisable()
    {
        // Also clean up when disabled (scene change, etc.)
        Cleanup();
    }
    
    void OnMonsterAttackProgress(MonsterAttackProgressEvent e)
    {
        UpdateMonsterSwingTimer(e.monsterIndex, e.progress);
    }
    
    void OnPlayerDamageDealt(PlayerDamageDealtEvent e)
    {
        // Show damage on current target
        if (combatService != null)
        {
            int targetIndex = combatService.GetCurrentTargetIndex();
            if (e.wasMiss)
            {
                ShowMissText(targetIndex, true); // true = player miss (blue)
            }
            else
            {
                ShowDamageHitText(targetIndex, e.damage, e.wasCritical);
            }
        }
    }
    
    /// <summary>
    /// Initialize combat with hero and multiple enemies
    /// </summary>
    public void InitializeCombat(Sprite heroSprite, List<MonsterData> monsters)
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
        
        // Spawn enemy group
        SpawnEnemyGroup(monsters);
    }
    
    /// <summary>
    /// Spawn a group of enemies with Y-axis spacing
    /// </summary>
    void SpawnEnemyGroup(List<MonsterData> monsters)
    {
        if (enemyVisualPrefab == null)
        {
            return;
        }
        
        if (monsters == null || monsters.Count == 0)
        {
            return;
        }
        
        // Clean up existing enemies
        CleanupEnemies();
        
        // Calculate Y positions based on count
        float[] yOffsets = CalculateYOffsets(monsters.Count);
        // Calculate X offsets for horizontal spacing (alternating to avoid overlap)
        float[] xOffsets = CalculateXOffsets(monsters.Count);
        
        // Get hero position for enemy targeting
        Vector2 heroPos;
        if (heroVisual != null)
        {
            heroPos = heroVisual.GetPosition(); // World position
        }
        else
        {
            heroPos = Vector2.zero;
        }
        
        // Spawn each enemy in reverse order (bottom to top) so bottom monsters render on top
        // This ensures health bars of bottom monsters are visible above top monsters
        for (int i = monsters.Count - 1; i >= 0; i--)
        {
            MonsterData monsterData = monsters[i];
            float yOffset = yOffsets[i];
            float xOffset = xOffsets[i];
            
            // Create new enemy - parent to enemySpawnPosition if it exists, otherwise combatSceneContainer
            Transform parentTransform = enemySpawnPosition != null ? enemySpawnPosition : combatSceneContainer;
            GameObject enemyObj = Instantiate(enemyVisualPrefab.gameObject, parentTransform);
            EnemyVisual enemyVisual = enemyObj.GetComponent<EnemyVisual>();
            
            if (enemyVisual == null)
            {
                Destroy(enemyObj);
                continue;
            }
            
            // Setup enemy positions
            Vector2 spawnPos;
            if (enemySpawnPosition != null && enemyVisual.transform.parent == enemySpawnPosition)
            {
                // Enemy is parented to spawn position, use local position with Y and X offsets
                spawnPos = new Vector2(xOffset, yOffset);
            }
            else
            {
                // Enemy is not parented, use world position with Y and X offsets
                spawnPos = enemySpawnPosition != null 
                    ? enemySpawnPosition.anchoredPosition + new Vector2(xOffset, yOffset)
                    : new Vector2(xOffset, yOffset);
            }
            
            // Convert hero position to same coordinate space as enemy
            Vector2 localHeroPos = heroPos;
            if (enemyVisual.transform.parent != null && enemyVisual.transform.parent is RectTransform enemyParentRect)
            {
                localHeroPos = heroPos - enemyParentRect.anchoredPosition;
            }
            
            // Get attack range from monster instance
            float attackRange = GameBalance.Combat.monsterMeleeRange; // Default fallback
            if (combatService != null)
            {
                var monsterInstances = combatService.GetActiveMonsters();
                if (i < monsterInstances.Count)
                {
                    attackRange = monsterInstances[i].attackRange;
                }
            }
            else
            {
                // Fallback: use attackRangeType if instance not available yet
                attackRange = monsterData.attackRangeType == MonsterAttackRangeType.Melee
                    ? GameBalance.Combat.monsterMeleeRange
                    : GameBalance.Combat.monsterRangedRange;
            }
            
            enemyVisual.Setup(monsterData.monsterSprite, spawnPos, localHeroPos, attackRange, monsterData.flipSprite);
            
            // Set monster index for identification
            enemyVisual.SetMonsterIndex(i);
            
            // Find monster details container in the prefab (should be a child of the enemy)
            // The container should already be set up in the prefab with proper positioning
            RectTransform detailsRect = null;
            
            // First, try to find by common names
            Transform containerTransform = enemyObj.transform.Find("MonsterDetailsContainer");
            if (containerTransform == null)
                containerTransform = enemyObj.transform.Find("DetailsContainer");
            if (containerTransform == null)
                containerTransform = enemyObj.transform.Find("HealthBarContainer");
            
            if (containerTransform != null)
            {
                detailsRect = containerTransform.GetComponent<RectTransform>();
            }
            else
            {
                // Search all children for one that has UI elements (sliders/text) - that's our container
                foreach (Transform child in enemyObj.transform)
                {
                    RectTransform childRect = child.GetComponent<RectTransform>();
                    if (childRect != null && childRect != enemyVisual.rectTransform)
                    {
                        // Check if this child has UI elements (sliders, text) - that's our container
                        Slider[] sliders = childRect.GetComponentsInChildren<Slider>();
                        if (sliders.Length > 0)
                        {
                            detailsRect = childRect;
                            break;
                        }
                    }
                }
            }
            
            if (detailsRect != null)
            {
                enemyVisual.monsterDetailsContainer = detailsRect;
                
                // Initialize monster name and level in monster details container
                var monsterInstance = combatService?.GetActiveMonsters();
                if (monsterInstance != null && i < monsterInstance.Count && monsterInstance[i].monsterData != null)
                {
                    TextMeshProUGUI[] allTexts = detailsRect.GetComponentsInChildren<TextMeshProUGUI>();
                    string monsterName = monsterInstance[i].monsterData.monsterName;
                    int monsterLevel = monsterInstance[i].level;
                    foreach (TextMeshProUGUI text in allTexts)
                    {
                        // Find name text (doesn't contain numbers or "/")
                        if (!text.text.Contains("/") && !System.Text.RegularExpressions.Regex.IsMatch(text.text, @"^\d+"))
                        {
                            text.text = $"{monsterName} Level {monsterLevel}";
                            break;
                        }
                    }
                }
            }
        
        // Set callback for when enemy reaches attack range
            enemyVisual.SetOnReachAttackRange(() => {
            // Enemy reached attack range - combat manager will handle attack timing
        });
            
            // Set click callback for targeting
            enemyVisual.SetOnClickCallback(() => {
                if (combatService != null)
                {
                    combatService.SetTarget(i);
                }
            });
            
            activeEnemies.Add(enemyVisual);
        }
        
        // Set first enemy as target
        currentTargetIndex = 0;
        UpdateTargetIndicators();
    }
    
    /// <summary>
    /// Calculate Y offsets for monsters based on count
    /// 1 monster: Y = 0
    /// 2 monsters: Y = -40, +40
    /// 3 monsters: Y = -60, 0, +60
    /// </summary>
    float[] CalculateYOffsets(int count)
    {
        float[] offsets = new float[count];
        
        if (count == 1)
        {
            offsets[0] = 0f;
        }
        else if (count == 2)
        {
            offsets[0] = -40f;
            offsets[1] = 40f;
        }
        else if (count == 3)
        {
            offsets[0] = -60f;
            offsets[1] = 0f;
            offsets[2] = 60f;
        }
        else
        {
            // For more than 3, distribute evenly
            float spacing = monsterYSpacing;
            float totalHeight = (count - 1) * spacing;
            float startY = -totalHeight * 0.5f;
            for (int i = 0; i < count; i++)
            {
                offsets[i] = startY + (i * spacing);
            }
        }
        
        return offsets;
    }
    
    /// <summary>
    /// Calculate X offsets for monsters to avoid overlap
    /// Alternates X position: 0, offset, 0, offset, ...
    /// 1 monster: X = 0
    /// 2 monsters: X = 0, +offset
    /// 3 monsters: X = 0, +offset, 0
    /// 4 monsters: X = 0, +offset, 0, +offset
    /// </summary>
    float[] CalculateXOffsets(int count)
    {
        float[] offsets = new float[count];
        
        for (int i = 0; i < count; i++)
        {
            // Alternate: even indices (0, 2, 4...) = 0, odd indices (1, 3, 5...) = offset
            if (i % 2 == 0)
            {
                offsets[i] = 0f; // 1st, 3rd, 5th, etc. - no offset
            }
            else
            {
                offsets[i] = monsterXSpacing; // 2nd, 4th, 6th, etc. - offset right
            }
        }
        
        return offsets;
    }
    
    /// <summary>
    /// Hero attacks - spawn projectile toward specific enemy
    /// </summary>
    public void HeroAttack(float damage, int targetIndex, System.Action<float, int> onHit)
    {
        if (heroVisual == null || targetIndex < 0 || targetIndex >= activeEnemies.Count)
            return;
        
        EnemyVisual targetEnemy = activeEnemies[targetIndex];
        if (targetEnemy == null)
            return;
        
        // Ensure projectile prefab is set
        if (heroVisual.projectilePrefab == null && projectilePrefab != null)
        {
            heroVisual.projectilePrefab = projectilePrefab;
        }
        
        Vector2 targetPos = targetEnemy.GetPosition();
        
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
                onHit?.Invoke(dealtDamage, targetIndex);
                Destroy(projectile.gameObject);
            },
            (projectile) => {
                // Projectile miss (shouldn't happen in auto-combat, but handle it)
                Destroy(projectile.gameObject);
            }
        );
    }
    
    /// <summary>
    /// Enemy attacks - play swipe animation for specific enemy
    /// </summary>
    public void EnemyAttack(int enemyIndex, System.Action onComplete)
    {
        if (enemyIndex < 0 || enemyIndex >= activeEnemies.Count)
        {
            onComplete?.Invoke();
            return;
        }
        
        EnemyVisual enemy = activeEnemies[enemyIndex];
        if (enemy == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        enemy.PerformAttack(onComplete);
    }
    
    /// <summary>
    /// Update enemy health bar for specific enemy
    /// </summary>
    public void UpdateEnemyHealth(MonsterHealthChangedEvent e)
    {
        if (e.monsterIndex < 0 || e.monsterIndex >= activeEnemies.Count)
            return;
        
        EnemyVisual enemy = activeEnemies[e.monsterIndex];
        if (enemy == null)
            return;
        
        // Update monster details container if enemy has one
        if (enemy.monsterDetailsContainer != null)
        {
            // Find all components in the monster details container
            Slider[] sliders = enemy.monsterDetailsContainer.GetComponentsInChildren<Slider>();
            TextMeshProUGUI[] texts = enemy.monsterDetailsContainer.GetComponentsInChildren<TextMeshProUGUI>();
            
            // Update health bar slider (usually the first slider)
            if (sliders.Length > 0)
            {
                Slider healthBar = sliders[0];
                healthBar.maxValue = e.maxHealth;
                healthBar.value = Mathf.Max(0f, e.currentHealth);
            }
            
            // Update health text (look for text that contains "/" or is numeric)
            foreach (TextMeshProUGUI text in texts)
            {
                if (text.text.Contains("/") || System.Text.RegularExpressions.Regex.IsMatch(text.text, @"^\d+"))
                {
                    text.text = $"{Mathf.Max(0f, e.currentHealth):F0}";
                    break; // Update first matching text (health text)
                }
            }
            
            // Update monster name and level (if we have access to monster data)
            if (combatService != null)
            {
                var monsterInstance = combatService.GetActiveMonsters();
                if (e.monsterIndex < monsterInstance.Count && monsterInstance[e.monsterIndex].monsterData != null)
                {
                    string monsterName = monsterInstance[e.monsterIndex].monsterData.monsterName;
                    int monsterLevel = monsterInstance[e.monsterIndex].level;
                    // Find name text (usually doesn't contain numbers or "/")
                    foreach (TextMeshProUGUI text in texts)
                    {
                        if (!text.text.Contains("/") && !System.Text.RegularExpressions.Regex.IsMatch(text.text, @"^\d+"))
                        {
                            text.text = $"{monsterName} Lvl{monsterLevel}";
                            break; // Update first non-numeric text (name text)
                        }
                    }
                }
            }
        }
        
        // Update swing timer bar (usually the second slider if it exists)
        UpdateMonsterSwingTimer(e.monsterIndex);
    }
    
    /// <summary>
    /// Update monster swing timer for specific enemy
    /// </summary>
    public void UpdateMonsterSwingTimer(int index, float progress = -1f)
    {
        if (index < 0 || index >= activeEnemies.Count)
            return;
        
        EnemyVisual enemy = activeEnemies[index];
        if (enemy == null || enemy.monsterDetailsContainer == null)
            return;
        
        // Get progress from combat service if not provided
        if (progress < 0f && combatService != null)
        {
            var monsters = combatService.GetActiveMonsters();
            if (index < monsters.Count)
            {
                var monster = monsters[index];
                if (monster.attackSpeed > 0)
                {
                    progress = Mathf.Clamp01(monster.attackTimer / monster.attackSpeed);
                }
            }
        }
        
        // Find swing timer slider (usually the second slider)
        Slider[] sliders = enemy.monsterDetailsContainer.GetComponentsInChildren<Slider>();
        if (sliders.Length > 1)
        {
            Slider swingBar = sliders[1];
            swingBar.value = Mathf.Clamp01(progress);
        }
    }
    
    /// <summary>
    /// Get local position for text above an enemy
    /// </summary>
    Vector2 GetEnemyLocalPosition(int enemyIndex)
    {
        if (enemyIndex < 0 || enemyIndex >= activeEnemies.Count || combatSceneContainer == null)
            return Vector2.zero;
        
        EnemyVisual enemy = activeEnemies[enemyIndex];
        if (enemy == null)
            return Vector2.zero;
        
        // Get enemy's current world position
        Vector2 enemyWorldPos = enemy.GetPosition();
        
        // Convert enemy position to local space of combat scene container
        Vector2 localPosition = enemyWorldPos;
        if (enemy.rectTransform != null)
        {
            // If enemy and container share the same parent, positions are in same space
            if (enemy.rectTransform.parent == combatSceneContainer)
            {
                localPosition = enemy.rectTransform.anchoredPosition;
            }
            else
            {
                // Convert world position to local position in combat container
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    combatSceneContainer,
                    RectTransformUtility.WorldToScreenPoint(null, enemy.rectTransform.position),
                    null,
                    out localPosition
                );
            }
        }
        
        return localPosition;
    }
    
    /// <summary>
    /// Create a floating text GameObject with common setup
    /// </summary>
    GameObject CreateFloatingText(string text, Vector2 position, Color color, float width = 100f, bool isMiss = false)
    {
        if (combatSceneContainer == null)
            return null;
        
        // Create new text GameObject
        GameObject textObj = new GameObject(isMiss ? "MissText" : "DamageText");
        textObj.transform.SetParent(combatSceneContainer, false);
        
        // Add to tracking list
        activeDamageTexts.Add(textObj);
        
        // Add RectTransform
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(width, 50f);
        // Position above target (offset Y by +30 pixels)
        textRect.anchoredPosition = position + new Vector2(0f, 30f);
        
        // Add CanvasGroup for fade effect
        CanvasGroup canvasGroup = textObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        
        // Add TextMeshProUGUI
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 32;
        textComponent.fontStyle = FontStyles.Bold;
        textComponent.color = color;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.enableWordWrapping = false; // Prevent text wrapping
        
        // Set font if available
        if (damageTextFont != null)
        {
            textComponent.font = damageTextFont;
        }
        
        // Add outline for better visibility
        var outline = textObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);
        
        return textObj;
    }
    
    /// <summary>
    /// Show damage hit text above specific enemy
    /// Creates independent damage text GameObject that floats above the enemy
    /// </summary>
    public void ShowDamageHitText(int enemyIndex, float damage, bool wasCritical = false)
    {
        Vector2 localPosition = GetEnemyLocalPosition(enemyIndex);
        if (localPosition == Vector2.zero && (enemyIndex < 0 || enemyIndex >= activeEnemies.Count))
            return;
        
        // Determine color based on crit
        Color textColor = wasCritical ? new Color(1f, 0.84f, 0f) : Color.white;
        
        // Create text GameObject
        GameObject damageObj = CreateFloatingText($"{damage:F0}", localPosition, textColor, 100f, false);
        if (damageObj == null)
            return;
        
        TextMeshProUGUI damageText = damageObj.GetComponent<TextMeshProUGUI>();
        
        // Animate damage text (rise up and fade out, with pop animation for crits)
        StartCoroutine(AnimateDamageText(damageText, damageObj, wasCritical));
    }
    
    /// <summary>
    /// Show "Miss" text above specific enemy (for player misses) or above player (for enemy misses)
    /// </summary>
    public void ShowMissText(int enemyIndex, bool isPlayerMiss)
    {
        if (combatSceneContainer == null)
            return;
        
        Vector2 localPosition;
        
        if (isPlayerMiss)
        {
            // Player miss - show above enemy
            if (enemyIndex < 0 || enemyIndex >= activeEnemies.Count)
                return;
            
            EnemyVisual enemy = activeEnemies[enemyIndex];
            if (enemy == null)
                return;
            
            // Get enemy's current world position
            Vector2 enemyWorldPos = enemy.GetPosition();
            
            // Convert enemy position to local space of combat scene container
            localPosition = enemyWorldPos;
            if (enemy.rectTransform != null)
            {
                // If enemy and container share the same parent, positions are in same space
                if (enemy.rectTransform.parent == combatSceneContainer)
                {
                    localPosition = enemy.rectTransform.anchoredPosition;
                }
                else
                {
                    // Convert world position to local position in combat container
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        combatSceneContainer,
                        RectTransformUtility.WorldToScreenPoint(null, enemy.rectTransform.position),
                        null,
                        out localPosition
                    );
                }
            }
        }
        else
        {
            // Enemy miss - show above hero (use hero position)
            if (heroVisual != null)
            {
                Vector2 heroPos = heroVisual.GetPosition();
                localPosition = heroPos;
                if (heroVisual.rectTransform != null && heroVisual.rectTransform.parent == combatSceneContainer)
                {
                    localPosition = heroVisual.rectTransform.anchoredPosition;
                }
            }
            else
            {
                return; // Can't show miss text without hero position
            }
        }
        
        // Create new miss text GameObject
        GameObject missObj = new GameObject($"MissText_{enemyIndex}");
        missObj.transform.SetParent(combatSceneContainer, false);
        
        // Add to tracking list
        activeDamageTexts.Add(missObj);
        
        // Add RectTransform
        RectTransform missRect = missObj.AddComponent<RectTransform>();
        missRect.sizeDelta = new Vector2(150f, 50f); // Wider to prevent text wrapping
        // Position above target (offset Y by +30 pixels)
        missRect.anchoredPosition = localPosition + new Vector2(0f, 30f);
        
        // Add CanvasGroup for fade effect
        CanvasGroup canvasGroup = missObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        
        // Add TextMeshProUGUI for miss text
        TextMeshProUGUI missText = missObj.AddComponent<TextMeshProUGUI>();
        missText.text = "Miss";
        missText.fontSize = 32;
        missText.fontStyle = FontStyles.Bold;
        // Darker blue for player misses, red for enemy misses
        missText.color = isPlayerMiss ? new Color(0f, 0.4f, 0.8f) : Color.red;
        missText.alignment = TextAlignmentOptions.Center;
        missText.enableWordWrapping = false; // Prevent text wrapping
        
        // Set font if available
        if (damageTextFont != null)
        {
            missText.font = damageTextFont;
        }
        
        // Add outline for better visibility
        var outline = missObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);
        
        // Animate miss text (rise up and fade out)
        StartCoroutine(AnimateDamageText(missText, missObj, false));
    }
    
    /// <summary>
    /// Animate damage text rising up and fading out
    /// For critical hits, adds a heartbeat pop animation (scale up then down)
    /// </summary>
    System.Collections.IEnumerator AnimateDamageText(TextMeshProUGUI damageText, GameObject damageObj, bool wasCritical = false)
    {
        if (damageText == null || damageText.rectTransform == null || damageObj == null) 
            yield break;
        
        RectTransform rectTransform = damageText.rectTransform;
        CanvasGroup canvasGroup = damageText.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = damageText.gameObject.AddComponent<CanvasGroup>();
        }
        
        // Store starting position and scale
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 endPosition = startPosition + new Vector2(0, 50f); // Rise 50 pixels
        Vector3 defaultScale = Vector3.one;
        
        // Reset alpha and position
        canvasGroup.alpha = 1f;
        rectTransform.anchoredPosition = startPosition;
        rectTransform.localScale = defaultScale;
        
        // For critical hits, play heartbeat pop animation first
        if (wasCritical)
        {
            float popDuration = 0.2f; // 0.1s for scale up
            float popElapsed = 0f;
            
            // Scale up (ease out)
            while (popElapsed < popDuration)
            {
                if (damageText == null || rectTransform == null || damageObj == null)
                    yield break;
                
                popElapsed += Time.deltaTime;
                float t = popElapsed / popDuration;
                float easedT = t * t; // Ease out
                rectTransform.localScale = Vector3.Lerp(defaultScale, defaultScale * 1.3f, easedT);
                yield return null;
            }
            
            popElapsed = 0f;
            popDuration = 0.2f; // 0.1s for scale down
            
            // Scale down (ease in)
            while (popElapsed < popDuration)
            {
                if (damageText == null || rectTransform == null || damageObj == null)
                    yield break;
                
                popElapsed += Time.deltaTime;
                float t = popElapsed / popDuration;
                float easedT = t * t; // Ease in
                rectTransform.localScale = Vector3.Lerp(defaultScale * 1.3f, defaultScale, easedT);
                yield return null;
            }
            
            // Ensure scale is reset
            rectTransform.localScale = defaultScale;
        }
        
        float duration = 1f; // 1 second animation
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Check if damage text has been destroyed
            if (damageText == null || rectTransform == null || damageObj == null)
            {
                yield break;
            }
            
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Ease out curve for smooth animation
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            
            // Interpolate position (rise up)
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, easedT);
            
            // Interpolate alpha (fade out)
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            }
            
            yield return null;
        }
        
        // Clean up: remove from tracking list and destroy
        if (damageObj != null)
        {
            activeDamageTexts.Remove(damageObj);
            Destroy(damageObj);
        }
    }
    
    /// <summary>
    /// Check if specific enemy is in attack range
    /// </summary>
    public bool IsEnemyInAttackRange(int index)
    {
        if (index < 0 || index >= activeEnemies.Count)
            return false;
        
        EnemyVisual enemy = activeEnemies[index];
        return enemy != null && enemy.IsInAttackRange();
    }
    
    /// <summary>
    /// Get enemy position by index
    /// </summary>
    public Vector2 GetEnemyPosition(int index)
    {
        if (index < 0 || index >= activeEnemies.Count)
            return Vector2.zero;
        
        EnemyVisual enemy = activeEnemies[index];
        return enemy != null ? enemy.GetPosition() : Vector2.zero;
    }
    
    /// <summary>
    /// Set target indicator (show/hide red arrow on targeted mob)
    /// </summary>
    public void SetTargetIndicator(TargetChangedEvent e)
    {
        currentTargetIndex = e.newTargetIndex;
        UpdateTargetIndicators();
    }
    
    /// <summary>
    /// Update target indicators for all enemies
    /// </summary>
    void UpdateTargetIndicators()
    {
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null)
            {
                activeEnemies[i].ShowTargetIndicator(i == currentTargetIndex);
            }
        }
    }
    
    /// <summary>
    /// Show visual item drops when a monster dies
    /// </summary>
    public void ShowItemDrops(List<MonsterDropEntry> droppedItems, Vector2 enemyDeathPosition, int monsterIndex)
    {
        if (droppedItems == null || droppedItems.Count == 0)
            return;
        
        if (combatSceneContainer == null)
        {
            return;
        }
        
        // Use the provided death position (already calculated correctly)
        Vector2 localEnemyPosition = enemyDeathPosition;
        
        // Try to get the actual enemy position if it still exists (for better accuracy)
        if (monsterIndex >= 0 && monsterIndex < activeEnemies.Count && activeEnemies[monsterIndex] != null)
        {
            EnemyVisual enemy = activeEnemies[monsterIndex];
            if (enemy.rectTransform != null)
            {
                RectTransform enemyRect = enemy.rectTransform;
            
            // Check if enemy is a direct child of combatSceneContainer
            if (enemyRect.parent == combatSceneContainer)
            {
                localEnemyPosition = enemyRect.anchoredPosition;
            }
            else
            {
                    // Convert to local space
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
                }
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
        CleanupEnemies();
        
        // Clean up projectiles
        if (projectilePool != null)
        {
            foreach (Transform child in projectilePool)
            {
                Destroy(child.gameObject);
            }
        }
        
        // Clean up any active damage texts
        foreach (GameObject damageText in activeDamageTexts)
        {
            if (damageText != null)
            {
                Destroy(damageText);
            }
        }
        activeDamageTexts.Clear();
        
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
    
    /// <summary>
    /// Clean up all enemies
    /// </summary>
    void CleanupEnemies()
    {
        foreach (EnemyVisual enemy in activeEnemies)
        {
            if (enemy != null && enemy.gameObject != null)
            {
                // Destroy enemy GameObject - container will be destroyed automatically since it's a child
                Destroy(enemy.gameObject);
            }
        }
        activeEnemies.Clear();
        currentTargetIndex = 0;
    }
    
    /// <summary>
    /// Clean up a specific enemy's visuals when it dies
    /// Note: Damage texts are now independent and will complete their animation even after enemy is hidden
    /// </summary>
    public void CleanupEnemyVisual(int enemyIndex)
    {
        if (enemyIndex < 0 || enemyIndex >= activeEnemies.Count)
            return;
        
        EnemyVisual enemy = activeEnemies[enemyIndex];
        if (enemy != null)
        {
            // Hide target arrow if this was the target
            if (enemyIndex == currentTargetIndex)
            {
                enemy.ShowTargetIndicator(false);
            }
            
            // Hide the enemy visual (but don't destroy yet - keep it for respawn logic)
            // The monster details container will be hidden automatically since it's a child
            // Damage texts are now independent GameObjects and will continue their animation
            if (enemy.gameObject != null)
            {
                enemy.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Spawn a new enemy with new monster data at the spawn position
    /// Reuses an existing enemy slot for continuous combat spawning
    /// </summary>
    public void SpawnNewEnemy(int enemyIndex, MonsterData newMonsterData, float currentHealth, float maxHealth)
    {
        if (enemyIndex < 0 || enemyIndex >= activeEnemies.Count)
            return;
        
        EnemyVisual enemy = activeEnemies[enemyIndex];
        if (enemy == null)
            return;
        
        // Calculate spawn position (same logic as InitializeCombat)
        float yOffset = 0f;
        float xOffset = 0f;
        
        // If there are multiple enemies, offset them vertically
        if (activeEnemies.Count > 1)
        {
            float totalHeight = 150f * (activeEnemies.Count - 1);
            float startY = totalHeight / 2f;
            yOffset = startY - (enemyIndex * 150f);
        }
        
        // Setup spawn position
        Vector2 spawnPos;
        if (enemySpawnPosition != null && enemy.transform.parent == enemySpawnPosition)
        {
            spawnPos = new Vector2(xOffset, yOffset);
        }
        else
        {
            spawnPos = enemySpawnPosition != null 
                ? enemySpawnPosition.anchoredPosition + new Vector2(xOffset, yOffset)
                : new Vector2(xOffset, yOffset);
        }
        
        // Get hero position
        Vector2 localHeroPos = Vector2.zero;
        if (heroVisual != null && heroVisual.transform is RectTransform heroRect)
        {
            localHeroPos = heroRect.anchoredPosition;
            if (enemy.transform.parent != null && enemy.transform.parent is RectTransform enemyParentRect)
            {
                localHeroPos = localHeroPos - enemyParentRect.anchoredPosition;
            }
        }
        
        // Get attack range from monster instance
        float attackRange = GameBalance.Combat.monsterMeleeRange; // Default fallback
        if (combatService != null)
        {
            var monsterInstances = combatService.GetActiveMonsters();
            if (enemyIndex < monsterInstances.Count)
            {
                attackRange = monsterInstances[enemyIndex].attackRange;
            }
        }
        else
        {
            // Fallback: use attackRangeType if instance not available yet
            attackRange = newMonsterData.attackRangeType == MonsterAttackRangeType.Melee
                ? GameBalance.Combat.monsterMeleeRange
                : GameBalance.Combat.monsterRangedRange;
        }
        
        // Setup enemy visual with new monster data
        enemy.Setup(newMonsterData.monsterSprite, spawnPos, localHeroPos, attackRange, newMonsterData.flipSprite);
        
        // Reactivate the GameObject (it was hidden when previous monster died)
        enemy.gameObject.SetActive(true);
        
        // Update health bar for the new monster
        UpdateEnemyHealth(new MonsterHealthChangedEvent 
        { 
            currentHealth = currentHealth, 
            maxHealth = maxHealth, 
            monsterIndex = enemyIndex 
        });
        
        Debug.Log($"[CombatSceneController] Spawned new {newMonsterData.monsterName} at enemy slot {enemyIndex}");
    }
}

