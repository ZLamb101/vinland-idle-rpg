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
    
    [Header("Class-Based Auto Attacks")]
    [Tooltip("Auto-attack configurations for each character class")]
    public AutoAttackConfig[] autoAttackConfigs;
    
    private List<EnemyVisual> activeEnemies = new List<EnemyVisual>();
    private int currentTargetIndex = 0;
    
    private ICombatService combatService; // Cached combat service reference
    
    // Sub-controllers (extracted for single responsibility)
    private DamageTextController damageTextController;
    private ProjectileController projectileController;
    private ItemDropController itemDropController;
    
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
        
        // Initialize sub-controllers
        InitializeControllers();
    }
    
    void InitializeControllers()
    {
        // Create and initialize DamageTextController
        damageTextController = gameObject.AddComponent<DamageTextController>();
        damageTextController.Initialize(
            combatSceneContainer,
            GetEnemyLocalPosition,
            GetHeroLocalPosition
        );
        
        // Create and initialize ProjectileController
        projectileController = gameObject.AddComponent<ProjectileController>();
        projectileController.Initialize(
            combatSceneContainer,
            projectilePool,
            projectilePrefab,
            heroVisual,
            GetHeroLocalPosition,
            (index) => GetEnemyLocalPosition(index),
            GetEnemyVisual
        );
        
        // Set up class-based auto-attack configurations
        if (autoAttackConfigs != null && autoAttackConfigs.Length > 0)
        {
            projectileController.SetAutoAttackConfigs(autoAttackConfigs);
        }
        
        // Create and initialize ItemDropController
        itemDropController = gameObject.AddComponent<ItemDropController>();
        itemDropController.itemDropSpacing = itemDropSpacing;
        itemDropController.Initialize(combatSceneContainer, GetEnemyVisual);
    }
    
    /// <summary>
    /// Set the character class for combat visual selection
    /// Called by CombatManager when combat starts
    /// </summary>
    public void SetCharacterClass(CharacterClass characterClass)
    {
        if (projectileController != null)
        {
            projectileController.SetCharacterClass(characterClass);
        }
        
        // Find the config for this class and apply hero sprite
        if (autoAttackConfigs != null)
        {
            foreach (var config in autoAttackConfigs)
            {
                if (config != null && config.characterClass == characterClass)
                {
                    if (config.heroSprite != null && heroVisual != null)
                    {
                        heroVisual.Setup(config.heroSprite);
                    }
                    break;
                }
            }
        }
    }
    
    void Start()
    {
        // Get combat service
        combatService = Services.Get<ICombatService>();
        
        // Subscribe to combat events for health bar updates
        if (combatService != null)
        {
            EventBus.Subscribe<MonsterHealthChangedEvent>(UpdateEnemyHealth);
            EventBus.Subscribe<MonsterAttackProgressEvent>(OnMonsterAttackProgress);
            EventBus.Subscribe<TargetChangedEvent>(SetTargetIndicator);
            EventBus.Subscribe<PlayerDamageDealtEvent>(OnPlayerDamageDealt);
            EventBus.Subscribe<SkillHitEvent>(OnSkillHit);
            EventBus.Subscribe<PlayerDamageTakenEvent>(OnPlayerDamageTaken);
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
        EventBus.Unsubscribe<SkillHitEvent>(OnSkillHit);
        EventBus.Unsubscribe<PlayerDamageTakenEvent>(OnPlayerDamageTaken);
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
    
    void OnSkillHit(SkillHitEvent e)
    {
        // Show damage from skill hits
        if (e.targetIndex >= 0)
        {
            // Skill hit a monster - show damage text
            ShowDamageHitText(e.targetIndex, e.damage, e.wasCritical);
        }
        else if (e.targetIndex == -1)
        {
            // Skill hit player (enemy skill) - show damage on hero
            ShowDamageOnHero(e.damage, e.wasCritical);
        }
    }
    
    void OnPlayerDamageTaken(PlayerDamageTakenEvent e)
    {
        // Show damage from enemy auto-attacks on hero
        if (!e.wasMiss && e.damage > 0)
        {
            ShowDamageOnHero(e.damage, false);
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
                    int monsterLevel = monsterInstance[i].level;
                    
                    // Get player level to check if monster level should be hidden
                    int playerLevel = 1;
                    if (Services.TryGet<ICharacterService>(out var charService))
                    {
                        playerLevel = charService.GetLevel();
                    }
                    
                    // Show "???" if monster is 10+ levels higher than player
                    string levelText = (monsterLevel >= playerLevel + 10) ? "Lvl ???" : $"Lvl {monsterLevel}";
                    
                    foreach (TextMeshProUGUI text in allTexts)
                    {
                        // Find level text (doesn't contain numbers or "/")
                        if (!text.text.Contains("/") && !System.Text.RegularExpressions.Regex.IsMatch(text.text, @"^\d+"))
                        {
                            text.text = levelText;
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
            
            // Insert at index 0 instead of Add() because we're spawning in reverse order
            // This ensures activeEnemies[i] corresponds to monster index i
            activeEnemies.Insert(0, enemyVisual);
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
        if (projectileController != null)
        {
            projectileController.HeroAttack(damage, targetIndex, onHit);
        }
    }
    
    /// <summary>
    /// Spawn a skill projectile that travels to target and triggers callback on hit
    /// </summary>
    public void SpawnSkillProjectile(SkillData skill, int targetIndex, float damage, System.Action<float, int> onHit)
    {
        if (projectileController != null)
        {
            projectileController.SpawnSkillProjectile(skill, targetIndex, damage, onHit);
        }
        else
        {
            // Fallback: apply damage immediately if no controller
            onHit?.Invoke(damage, targetIndex);
        }
    }
    
    /// <summary>
    /// Spawn a hit effect at the target's position (for instant skills)
    /// </summary>
    public void SpawnHitEffect(SkillData skill, int targetIndex)
    {
        if (projectileController != null)
        {
            projectileController.SpawnHitEffect(skill, targetIndex);
        }
    }
    
    /// <summary>
    /// Enemy attacks - play swipe animation for specific enemy
    /// </summary>
    public void EnemyAttack(int enemyIndex, System.Action onComplete)
    {
        if (projectileController != null)
        {
            projectileController.EnemyAttack(enemyIndex, onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
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
    /// Show damage hit text above specific enemy
    /// </summary>
    public void ShowDamageHitText(int enemyIndex, float damage, bool wasCritical = false)
    {
        if (damageTextController != null)
        {
            damageTextController.ShowDamageHitText(enemyIndex, damage, wasCritical);
        }
    }
    
    /// <summary>
    /// Show damage text on the hero when hit by enemy skills
    /// </summary>
    public void ShowDamageOnHero(float damage, bool wasCritical = false)
    {
        if (damageTextController != null)
        {
            damageTextController.ShowDamageOnHero(damage, wasCritical);
        }
    }
    
    /// <summary>
    /// Get the hero's local position relative to combat scene container
    /// </summary>
    Vector2 GetHeroLocalPosition()
    {
        if (heroVisual == null || heroVisual.rectTransform == null || combatSceneContainer == null)
            return Vector2.zero;
        
        // If hero is direct child of combat scene container, use anchored position
        if (heroVisual.rectTransform.parent == combatSceneContainer)
        {
            return heroVisual.rectTransform.anchoredPosition;
        }
        
        // Otherwise, convert world position to local position
        Vector3 heroWorldPos = heroVisual.rectTransform.position;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            combatSceneContainer,
            RectTransformUtility.WorldToScreenPoint(null, heroWorldPos),
            null,
            out localPoint
        );
        return localPoint;
    }
    
    /// <summary>
    /// Show "Miss" text above specific enemy (for player misses) or above player (for enemy misses)
    /// </summary>
    public void ShowMissText(int enemyIndex, bool isPlayerMiss)
    {
        if (damageTextController != null)
        {
            damageTextController.ShowMissText(enemyIndex, isPlayerMiss);
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
    /// Check if enemy is in range to cast a skill with a specific range type
    /// Ranged skills can be cast from spawn, melee skills need enemy to be in melee range
    /// </summary>
    public bool IsEnemyInSkillRange(int index, SkillRange skillRange)
    {
        if (index < 0 || index >= activeEnemies.Count)
            return false;
        
        EnemyVisual enemy = activeEnemies[index];
        if (enemy == null)
            return false;
        
        // Ranged skills can always be cast if enemy exists and is alive
        if (skillRange == SkillRange.Ranged)
        {
            return true; // Ranged skills have unlimited range
        }
        
        // Melee skills require enemy to be in melee range
        return enemy.IsInAttackRange();
    }
    
    /// <summary>
    /// Spawn a skill projectile from an enemy toward the hero
    /// </summary>
    public void SpawnEnemySkillProjectile(SkillData skill, int enemyIndex, float damage, System.Action<float> onHit)
    {
        if (projectileController != null)
        {
            projectileController.SpawnEnemySkillProjectile(skill, enemyIndex, damage, onHit);
        }
        else
        {
            // Fallback: apply damage immediately
            onHit?.Invoke(damage);
        }
    }
    
    /// <summary>
    /// Spawn a hit effect on the hero (for instant enemy skills)
    /// </summary>
    public void SpawnHitEffectOnHero(SkillData skill)
    {
        if (projectileController != null)
        {
            projectileController.SpawnHitEffectOnHero(skill);
        }
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
        if (itemDropController != null)
        {
            itemDropController.ShowItemDrops(droppedItems, enemyDeathPosition, monsterIndex);
        }
    }
    
    /// <summary>
    /// Clean up combat visuals
    /// </summary>
    public void Cleanup()
    {
        CleanupEnemies();
        
        // Delegate cleanup to sub-controllers
        if (projectileController != null)
        {
            projectileController.Cleanup();
        }
        
        if (damageTextController != null)
        {
            damageTextController.Cleanup();
        }
        
        if (itemDropController != null)
        {
            itemDropController.Cleanup();
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

