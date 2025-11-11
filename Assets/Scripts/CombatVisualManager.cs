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
    private Dictionary<int, Coroutine> activeDamageAnimations = new Dictionary<int, Coroutine>(); // Track damage text animations by enemy index
    private Dictionary<int, Vector2> damageTextStartPositions = new Dictionary<int, Vector2>(); // Store original starting positions for damage text
    
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
        // Subscribe to combat events for health bar updates
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnMonsterHealthChanged += UpdateEnemyHealth;
            CombatManager.Instance.OnMonsterAttackProgress += OnMonsterAttackProgress;
            CombatManager.Instance.OnTargetChanged += SetTargetIndicator;
            CombatManager.Instance.OnPlayerDamageDealt += OnPlayerDamageDealt;
        }
    }
    
    void OnDestroy()
    {
        // Clean up all visuals before destroying
        Cleanup();
        
        // Unsubscribe from events
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnMonsterHealthChanged -= UpdateEnemyHealth;
            CombatManager.Instance.OnMonsterAttackProgress -= OnMonsterAttackProgress;
            CombatManager.Instance.OnTargetChanged -= SetTargetIndicator;
            CombatManager.Instance.OnPlayerDamageDealt -= OnPlayerDamageDealt;
        }
    }
    
    void OnDisable()
    {
        // Also clean up when disabled (scene change, etc.)
        Cleanup();
    }
    
    void OnMonsterAttackProgress(float progress, int index)
    {
        UpdateMonsterSwingTimer(index, progress);
    }
    
    void OnPlayerDamageDealt(float damage)
    {
        // Show damage on current target
        if (CombatManager.Instance != null)
        {
            int targetIndex = CombatManager.Instance.GetCurrentTargetIndex();
            ShowDamageHitText(targetIndex, damage);
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
            
            enemyVisual.Setup(monsterData.monsterSprite, spawnPos, localHeroPos, monsterData.attackRange, monsterData.flipSprite);
            
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
                
                // Find and store the starting position of damage text for this enemy
                TextMeshProUGUI[] allTexts = detailsRect.GetComponentsInChildren<TextMeshProUGUI>();
                foreach (TextMeshProUGUI text in allTexts)
                {
                    // Check if this is the damage text
                    if (text.name.ToLower().Contains("damage") || text.name.ToLower().Contains("hit"))
                    {
                        if (text.rectTransform != null)
                        {
                            damageTextStartPositions[i] = text.rectTransform.anchoredPosition;
                            break;
                        }
                    }
                }
                
                // If not found by name, try to find by content pattern
                if (!damageTextStartPositions.ContainsKey(i))
                {
                    foreach (TextMeshProUGUI text in allTexts)
                    {
                        // Skip health text (contains "/") and name text (letters only)
                        if (!text.text.Contains("/") && !System.Text.RegularExpressions.Regex.IsMatch(text.text, @"^[A-Za-z\s]+$"))
                        {
                            if (text.rectTransform != null)
                            {
                                damageTextStartPositions[i] = text.rectTransform.anchoredPosition;
                                break;
                            }
                        }
                    }
                }
                
                // Initialize monster name in monster details container
                var monsterInstance = CombatManager.Instance?.GetActiveMonsters();
                if (monsterInstance != null && i < monsterInstance.Count && monsterInstance[i].monsterData != null)
                {
                    foreach (TextMeshProUGUI text in allTexts)
                    {
                        // Find name text (doesn't contain numbers or "/")
                        if (!text.text.Contains("/") && !System.Text.RegularExpressions.Regex.IsMatch(text.text, @"^\d+"))
                        {
                            text.text = monsterInstance[i].monsterData.monsterName;
                            break;
                    }
                }
            }
        }
        else
        {
            // Monster details container not found - continue without it
        }
        
        // Set callback for when enemy reaches attack range
            enemyVisual.SetOnReachAttackRange(() => {
            // Enemy reached attack range - combat manager will handle attack timing
        });
            
            // Set click callback for targeting
            enemyVisual.SetOnClickCallback(() => {
                if (CombatManager.Instance != null)
                {
                    CombatManager.Instance.SetTarget(i);
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
    public void UpdateEnemyHealth(float current, float max, int index)
    {
        if (index < 0 || index >= activeEnemies.Count)
            return;
        
        EnemyVisual enemy = activeEnemies[index];
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
                healthBar.maxValue = max;
                healthBar.value = Mathf.Max(0f, current);
            }
            
            // Update health text (look for text that contains "/" or is numeric)
            foreach (TextMeshProUGUI text in texts)
            {
                if (text.text.Contains("/") || System.Text.RegularExpressions.Regex.IsMatch(text.text, @"^\d+"))
                {
                    text.text = $"{Mathf.Max(0f, current):F0} / {max:F0}";
                    break; // Update first matching text (health text)
                }
            }
            
            // Update monster name (if we have access to monster data)
            if (CombatManager.Instance != null)
            {
                var monsterInstance = CombatManager.Instance.GetActiveMonsters();
                if (index < monsterInstance.Count && monsterInstance[index].monsterData != null)
                {
                    string monsterName = monsterInstance[index].monsterData.monsterName;
                    // Find name text (usually doesn't contain numbers or "/")
                    foreach (TextMeshProUGUI text in texts)
                    {
                        if (!text.text.Contains("/") && !System.Text.RegularExpressions.Regex.IsMatch(text.text, @"^\d+"))
                        {
                            text.text = monsterName;
                            break; // Update first non-numeric text (name text)
                        }
                    }
                }
            }
        }
        
        // Update swing timer bar (usually the second slider if it exists)
        UpdateMonsterSwingTimer(index);
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
        
        // Get progress from CombatManager if not provided
        if (progress < 0f && CombatManager.Instance != null)
        {
            var monsters = CombatManager.Instance.GetActiveMonsters();
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
    /// Show damage hit text above specific enemy
    /// </summary>
    public void ShowDamageHitText(int enemyIndex, float damage)
    {
        if (enemyIndex < 0 || enemyIndex >= activeEnemies.Count)
            return;
        
        EnemyVisual enemy = activeEnemies[enemyIndex];
        if (enemy == null || enemy.monsterDetailsContainer == null)
            return;
        
        // Find damage hit text component in monster details container (include inactive objects)
        TextMeshProUGUI[] texts = enemy.monsterDetailsContainer.GetComponentsInChildren<TextMeshProUGUI>(true);
        TextMeshProUGUI damageText = null;
        
        // Look for damage text (might be named "DamageText" or similar, or might be the one that's currently inactive)
        foreach (TextMeshProUGUI text in texts)
        {
            // Check if this text component has a name suggesting it's for damage
            if (text.name.ToLower().Contains("damage") || text.name.ToLower().Contains("hit"))
            {
                damageText = text;
                break;
            }
        }
        
        // If not found by name, try to find one that's not the health or name text
        if (damageText == null)
        {
            foreach (TextMeshProUGUI text in texts)
            {
                // Skip health text (contains "/") and name text (doesn't match damage pattern)
                // Also skip if it's the container itself (rectTransform parent check)
                if (text.rectTransform != null && text.rectTransform != enemy.monsterDetailsContainer)
                {
                    string textContent = text.text ?? "";
                    if (!textContent.Contains("/") && !System.Text.RegularExpressions.Regex.IsMatch(textContent, @"^[A-Za-z\s]+$"))
                    {
                        damageText = text;
                        break;
                    }
                }
            }
        }
        
        // Show damage text if found
        if (damageText != null)
        {
            // Stop any existing animation for this enemy
            if (activeDamageAnimations.ContainsKey(enemyIndex))
            {
                if (activeDamageAnimations[enemyIndex] != null)
                {
                    StopCoroutine(activeDamageAnimations[enemyIndex]);
                }
                activeDamageAnimations.Remove(enemyIndex);
            }
            
            // Ensure the text GameObject and its parent are active
            if (damageText.gameObject != null)
            {
                damageText.gameObject.SetActive(true);
            }
            
            // Reset position to original starting position before setting new damage
            RectTransform textRect = damageText.rectTransform;
            if (textRect != null)
            {
                // Use stored starting position if available, otherwise use current position
                if (damageTextStartPositions.ContainsKey(enemyIndex))
                {
                    textRect.anchoredPosition = damageTextStartPositions[enemyIndex];
                }
            }
            
            // Reset alpha and state
            CanvasGroup canvasGroup = damageText.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = damageText.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 1f;
            
            // Set new damage value
            damageText.text = $"{damage:F0}";
            
            // Animate damage text (rise up and fade out)
            Coroutine animCoroutine = StartCoroutine(AnimateDamageText(damageText, enemyIndex));
            activeDamageAnimations[enemyIndex] = animCoroutine;
        }
    }
    
    /// <summary>
    /// Animate damage text rising up and fading out
    /// </summary>
    System.Collections.IEnumerator AnimateDamageText(TextMeshProUGUI damageText, int enemyIndex)
    {
        if (damageText == null || damageText.rectTransform == null) yield break;
        
        RectTransform rectTransform = damageText.rectTransform;
        CanvasGroup canvasGroup = damageText.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = damageText.gameObject.AddComponent<CanvasGroup>();
        }
        
        // Store starting position
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 endPosition = startPosition + new Vector2(0, 50f); // Rise 50 pixels
        
        // Reset alpha and position
        canvasGroup.alpha = 1f;
        rectTransform.anchoredPosition = startPosition;
        damageText.gameObject.SetActive(true);
        
        float duration = 1f; // 1 second animation
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Check if damage text or rectTransform has been destroyed
            if (damageText == null || damageText.rectTransform == null || !damageText.gameObject.activeInHierarchy)
            {
                // Clean up animation tracking
                if (activeDamageAnimations.ContainsKey(enemyIndex))
                {
                    activeDamageAnimations.Remove(enemyIndex);
                }
                yield break;
            }
            
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Ease out curve for smooth animation
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            
            // Interpolate position (rise up) - check again before accessing
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, easedT);
            }
            
            // Interpolate alpha (fade out)
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            }
            
            yield return null;
        }
        
        // Hide damage text after animation (check if still valid)
        if (damageText != null && damageText.gameObject != null)
        {
            damageText.gameObject.SetActive(false);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = startPosition;
            }
        }
        
        // Clean up animation tracking
        if (activeDamageAnimations.ContainsKey(enemyIndex))
        {
            activeDamageAnimations.Remove(enemyIndex);
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
    public void SetTargetIndicator(int targetIndex)
    {
        currentTargetIndex = targetIndex;
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
        // Stop all damage text animations
        foreach (var kvp in activeDamageAnimations)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        activeDamageAnimations.Clear();
        damageTextStartPositions.Clear();
        
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
    /// </summary>
    public void CleanupEnemyVisual(int enemyIndex)
    {
        if (enemyIndex < 0 || enemyIndex >= activeEnemies.Count)
            return;
        
        EnemyVisual enemy = activeEnemies[enemyIndex];
        if (enemy != null)
        {
            // Stop any running damage text animations for this enemy
            if (activeDamageAnimations.ContainsKey(enemyIndex))
            {
                if (activeDamageAnimations[enemyIndex] != null)
                {
                    StopCoroutine(activeDamageAnimations[enemyIndex]);
                }
                activeDamageAnimations.Remove(enemyIndex);
            }
            
            // Hide target arrow if this was the target
            if (enemyIndex == currentTargetIndex)
            {
                enemy.ShowTargetIndicator(false);
            }
            
            // Hide the enemy visual (but don't destroy yet - keep it for respawn logic)
            // The monster details container will be hidden automatically since it's a child
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
        
        // Setup enemy visual with new monster data
        enemy.Setup(newMonsterData.monsterSprite, spawnPos, localHeroPos, newMonsterData.attackRange, newMonsterData.flipSprite);
        
        // Reactivate the GameObject (it was hidden when previous monster died)
        enemy.gameObject.SetActive(true);
        
        // Update health bar for the new monster
        UpdateEnemyHealth(currentHealth, maxHealth, enemyIndex);
        
        Debug.Log($"[CombatVisualManager] Spawned new {newMonsterData.monsterName} at enemy slot {enemyIndex}");
    }
}

