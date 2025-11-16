using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manager for auto-battle combat system.
/// Handles combat state, attack timers, and damage calculation.
/// </summary>
public class CombatManager : MonoBehaviour, ICombatService
{
    
    [Header("Combat State")]
    private CombatState currentState = CombatState.Idle;
    private List<CombatMonsterInstance> activeMonsters = new List<CombatMonsterInstance>();
    private int currentTargetIndex = 0; // Index of currently targeted monster
    private MonsterData[] zoneMonsters;
    
    [Header("Combat Stats")]
    private float playerCurrentHealth;
    private float playerMaxHealth;
    private float playerBaseAttackDamage => GameBalance.Combat.playerBaseAttackDamage;
    private float playerBaseAttackSpeed => GameBalance.Combat.playerBaseAttackSpeed;
    private float playerAttackDamage; // Includes equipment bonuses
    private float playerAttackSpeed; // Includes equipment bonuses
    
    [Header("Attack Timers")]
    private float playerAttackTimer = 0f;
    
    [Header("Visual Combat")]
    public CombatVisualManager visualManager;
    
    [Header("Mob Count")]
    [Tooltip("Mob count selector. If not assigned, will try to find it in the scene.")]
    public MobCountSelector mobCountSelector; // Selector for number of mobs to fight
    
    // Events migrated to EventBus - see GameEvent.cs for event types
    
    public enum CombatState
    {
        Idle,       // Not in combat
        Fighting,   // Active combat
        Defeat      // Player died
    }

    private IGameLogService gameLogService;
    private ICharacterService characterService;
    
    void Awake()
    {
        if (Services.IsRegistered<ICombatService>())
        {
            Debug.LogWarning("[CombatManager] Service already registered. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        
        // Register with service locator
        Services.Register<ICombatService>(this);
        
        // Find mob count selector if not assigned
        if (mobCountSelector == null)
        {
            mobCountSelector = ComponentInjector.FindComponent<MobCountSelector>();
        }
    }

    void Start()
    {
        Services.TryGet<IGameLogService>(out gameLogService);
        characterService = Services.Get<ICharacterService>();
    }
    
    /// <summary>
    /// Helper method to log combat messages with automatic service refresh
    /// </summary>
    private void LogCombatMessage(string message, LogType logType = LogType.Info)
    {
        // Re-fetch service if null (scene might have reloaded)
        if (gameLogService == null)
        {
            Services.TryGet<IGameLogService>(out gameLogService);
        }
        
        if (gameLogService != null)
        {
            gameLogService.AddCombatLogEntry(message, logType);
        }
    }
    
    void OnDestroy()
    {
        Services.Unregister<ICombatService>();
    }
    
    /// <summary>
    /// Get the current mob count from the selector (defaults to 1 if not found)
    /// </summary>
    int GetMobCountFromSelector()
    {
        // Try to find selector if not assigned
        if (mobCountSelector == null)
        {
            mobCountSelector = ComponentInjector.FindComponent<MobCountSelector>();
        }
        
        if (mobCountSelector != null)
        {
            return mobCountSelector.GetMobCount();
        }
        
        return 1; // Default to 1 if selector not found
    }
    
    void Update()
    {
        if (currentState == CombatState.Fighting)
        {
            UpdateCombat();
        }
    }
    
    /// <summary>
    /// Start combat with monsters from the current zone
    /// </summary>
    public void StartCombat(MonsterData[] monsters, int mobCount = 1)
    {
        if (monsters == null || monsters.Length == 0)
        {
            return;
        }
        
        // IMPORTANT: Re-find visual manager in case scene was reloaded
        // CombatManager persists between scenes, but CombatVisualManager is recreated
        if (visualManager == null)
        {
            visualManager = ComponentInjector.FindComponent<CombatVisualManager>();
        }
        
        // Re-fetch game log service in case scene was reloaded
        if (gameLogService == null)
        {
            Services.TryGet<IGameLogService>(out gameLogService);
        }
        
        // Ensure we're not in combat already (clean up any stale state)
        if (currentState == CombatState.Fighting)
        {
            EndCombat();
        }
        
        zoneMonsters = monsters;
        
        // Initialize player stats with equipment bonuses
        CalculatePlayerStats();
        
        // Spawn monster group (all at once)
        SpawnMonsterGroup(mobCount);
    }
    
    /// <summary>
    /// Calculate player stats including equipment and talent bonuses
    /// </summary>
    public void CalculatePlayerStats()
    {
        // Base stats from character
        if (characterService != null)
        {
            playerMaxHealth = characterService.GetMaxHealthWithTalents();
            playerCurrentHealth = characterService.GetCurrentHealth();
        }
        else
        {
            playerMaxHealth = GameBalance.Combat.playerStartingHealth;
            playerCurrentHealth = GameBalance.Combat.playerStartingHealth;
        }
        
        // Start with base values
        playerAttackDamage = playerBaseAttackDamage;
        playerAttackSpeed = playerBaseAttackSpeed;
        
        // Add equipment bonuses
        var equipmentService = Services.Get<IEquipmentService>();
        if (equipmentService != null)
        {
            EquipmentStats equipStats = equipmentService.GetTotalStats();
            
            playerMaxHealth += equipStats.maxHealth;
            playerAttackDamage += equipStats.attackDamage;
            playerAttackSpeed += equipStats.attackSpeed;
        }
        
        // Add talent bonuses
        var talentService = Services.Get<ITalentService>();
        if (talentService != null)
        {
            TalentBonuses talents = talentService.GetTotalBonuses();
            
            // Additive bonuses
            playerAttackDamage += talents.attackDamage;
            playerAttackSpeed += talents.attackSpeed;
            
            // Percentage multipliers
            playerAttackDamage *= (1f + talents.damageMultiplier);
        }
        
        // Ensure health doesn't exceed max
        if (playerCurrentHealth > playerMaxHealth)
        {
            playerCurrentHealth = playerMaxHealth;
        }
    }
    
    /// <summary>
    /// Spawn a group of monsters (all at once)
    /// </summary>
    void SpawnMonsterGroup(int count)
    {
        if (zoneMonsters == null || zoneMonsters.Length == 0)
        {
            return;
        }
        
        // Clear existing monsters
        activeMonsters.Clear();
        
        // Create list of monster data to spawn
        List<MonsterData> monstersToSpawn = new List<MonsterData>();

        
        
        for (int i = 0; i < count; i++)
        {
            // Randomly select a monster from the zone's available monsters
            int randomIndex = UnityEngine.Random.Range(0, zoneMonsters.Length);
            MonsterData selectedMonster = zoneMonsters[randomIndex];
            
            // Create combat instance
            CombatMonsterInstance instance = new CombatMonsterInstance(selectedMonster, i);
            activeMonsters.Add(instance);
            monstersToSpawn.Add(selectedMonster);
            
            // Log monster spawn
            LogCombatMessage($"Monster {i + 1}: {selectedMonster.monsterName} spawned!", LogType.Info);
            
            EventBus.Publish(new MonsterSpawnedEvent { monsterData = selectedMonster, monsterIndex = i });
        }
        
        // Set first monster as target
        currentTargetIndex = 0;
        
        // Initialize visual combat
        // Re-find visual manager if it's null (scene might have been reloaded)
        if (visualManager == null)
        {
            visualManager = ComponentInjector.FindComponent<CombatVisualManager>();
        }
        
        if (visualManager != null)
        {
            // Get hero sprite (if available)
            Sprite heroSprite = null;
            // TODO: Get hero sprite from CharacterManager or CharacterData if available
            
            visualManager.InitializeCombat(heroSprite, monstersToSpawn);
        }
        
        // Log combat start
        if (monstersToSpawn.Count > 0)
        {
            string monsterNames = string.Join(", ", monstersToSpawn.ConvertAll(m => m.monsterName));
            LogCombatMessage($"Combat started against {monstersToSpawn.Count} monster(s): {monsterNames}!", LogType.Info);
        }
        
        // Reset timers
        playerAttackTimer = 0f;
        foreach (var monster in activeMonsters)
        {
            monster.attackTimer = 0f;
            monster.isAttackInProgress = false;
        }
        
        // Immediately update UI to show reset progress bars
        EventBus.Publish(new PlayerAttackProgressEvent { progress = 0f });
        for (int i = 0; i < activeMonsters.Count; i++)
        {
            EventBus.Publish(new MonsterAttackProgressEvent { progress = 0f, monsterIndex = i });
        }
        
        // Register activity with AwayActivityManager - use unique monster types for display
        if (Services.TryGet<IAwayActivityService>(out var awayActivityService))
        {
            // Get unique monster types (no duplicates)
            List<MonsterData> uniqueMonsters = new List<MonsterData>();
            foreach (MonsterData monster in monstersToSpawn)
            {
                if (!uniqueMonsters.Contains(monster))
                {
                    uniqueMonsters.Add(monster);
                }
            }
            
            // Convert to array for AwayActivityManager
            MonsterData[] uniqueMonstersArray = uniqueMonsters.ToArray();
            awayActivityService.StartFighting(uniqueMonstersArray, count);
            
            // Save activity immediately so it shows on character screen
            awayActivityService.SaveAwayState();
        }
        
        // Update state
        CombatState oldState = currentState;
        currentState = CombatState.Fighting;
        EventBus.Publish(new CombatStateChangedEvent { oldState = oldState, newState = currentState });
        EventBus.Publish(new MonstersChangedEvent { monsters = monstersToSpawn });
        EventBus.Publish(new TargetChangedEvent { newTargetIndex = currentTargetIndex });
        EventBus.Publish(new PlayerHealthChangedEvent { currentHealth = playerCurrentHealth, maxHealth = playerMaxHealth });
        
        // Update health for all monsters
        for (int i = 0; i < activeMonsters.Count; i++)
        {
            var monster = activeMonsters[i];
            EventBus.Publish(new MonsterHealthChangedEvent { currentHealth = monster.currentHealth, maxHealth = monster.maxHealth, monsterIndex = i });
        }
    }
    
    /// <summary>
    /// Cycle to next target (Tab key)
    /// </summary>
    public void CycleTarget()
    {
        if (activeMonsters.Count == 0) return;
        
        // Find next alive monster
        int startIndex = currentTargetIndex;
        do
        {
            currentTargetIndex = (currentTargetIndex + 1) % activeMonsters.Count;
            if (activeMonsters[currentTargetIndex].IsAlive())
            {
                EventBus.Publish(new TargetChangedEvent { newTargetIndex = currentTargetIndex });
                return;
            }
        } while (currentTargetIndex != startIndex);
        
        // If we looped back, just update anyway
        EventBus.Publish(new TargetChangedEvent { newTargetIndex = currentTargetIndex });
    }
    
    /// <summary>
    /// Set target by index
    /// </summary>
    public void SetTarget(int index)
    {
        if (index >= 0 && index < activeMonsters.Count && activeMonsters[index].IsAlive())
        {
            currentTargetIndex = index;
            EventBus.Publish(new TargetChangedEvent { newTargetIndex = currentTargetIndex });
        }
    }
    
    /// <summary>
    /// Get current target monster instance
    /// </summary>
    public CombatMonsterInstance GetCurrentTarget()
    {
        if (activeMonsters.Count == 0 || currentTargetIndex < 0 || currentTargetIndex >= activeMonsters.Count)
            return null;
        
        return activeMonsters[currentTargetIndex];
    }
    
    /// <summary>
    /// Get all active monsters
    /// </summary>
    public List<CombatMonsterInstance> GetActiveMonsters()
    {
        return new List<CombatMonsterInstance>(activeMonsters);
    }
    
    void UpdateCombat()
    {
        // Update player attack timer - player can attack immediately when enemy spawns
        playerAttackTimer += Time.deltaTime;
        EventBus.Publish(new PlayerAttackProgressEvent { progress = Mathf.Clamp01(playerAttackTimer / playerAttackSpeed) });
        
        if (playerAttackTimer >= playerAttackSpeed)
        {
            PlayerAttack();
            playerAttackTimer = 0f;
        }
        
        // Update each monster's attack timer independently
        for (int i = 0; i < activeMonsters.Count; i++)
        {
            var monster = activeMonsters[i];
            if (!monster.IsAlive() || monster.isAttackInProgress)
                continue;
            
            // Check if this specific enemy is in attack range
            bool canAttack = true;
            if (visualManager != null)
            {
                canAttack = visualManager.IsEnemyInAttackRange(i);
            }
            
            if (canAttack)
            {
                monster.attackTimer += Time.deltaTime;
                EventBus.Publish(new MonsterAttackProgressEvent { progress = Mathf.Clamp01(monster.attackTimer / monster.attackSpeed), monsterIndex = i });
                
                if (monster.attackTimer >= monster.attackSpeed)
                {
                    MonsterAttack(i);
                    monster.attackTimer = 0f;
                }
            }
        }
    }
    
    void PlayerAttack()
    {
        // Get current target
        var target = GetCurrentTarget();
        if (target == null || !target.IsAlive())
        {
            // No valid target, try to find one
            for (int i = 0; i < activeMonsters.Count; i++)
            {
                if (activeMonsters[i].IsAlive())
                {
                    currentTargetIndex = i;
                    target = activeMonsters[i];
                    EventBus.Publish(new TargetChangedEvent { newTargetIndex = currentTargetIndex });
                    break;
                }
            }
            
            if (target == null || !target.IsAlive())
                return; // No alive monsters
        }
        
        float damage = playerAttackDamage;
        
        // Get combined stats from equipment and talents
        CombatStats stats = CombatLogic.GetCombatStats();
        
        // Apply critical hit
        if (stats.critChance > 0 && UnityEngine.Random.value <= stats.critChance)
        {
            damage *= stats.critDamage; // Critical hit damage
        }
        
        // Visual combat: spawn projectile targeting current target
        if (visualManager != null)
        {
            visualManager.HeroAttack(damage, currentTargetIndex, (dealtDamage, targetIndex) => {
                ApplyPlayerDamage(dealtDamage, stats.lifesteal, targetIndex);
            });
        }
        else
        {
            // Fallback: apply damage immediately if no visual manager
            ApplyPlayerDamage(damage, stats.lifesteal, currentTargetIndex);
        }
    }
    
    /// <summary>
    /// Apply damage dealt by player (called after projectile hits)
    /// </summary>
    void ApplyPlayerDamage(float damage, float totalLifesteal, int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= activeMonsters.Count)
            return;
        
        var target = activeMonsters[targetIndex];
        if (!target.IsAlive())
            return;
        
        // Apply lifesteal
        if (totalLifesteal > 0)
        {
            float healAmount = damage * totalLifesteal;
            playerCurrentHealth = Mathf.Min(playerCurrentHealth + healAmount, playerMaxHealth);
            EventBus.Publish(new PlayerHealthChangedEvent { currentHealth = playerCurrentHealth, maxHealth = playerMaxHealth });
        }
        
        target.currentHealth -= damage;
        EventBus.Publish(new MonsterHealthChangedEvent { currentHealth = target.currentHealth, maxHealth = target.maxHealth, monsterIndex = targetIndex });
        EventBus.Publish(new PlayerDamageDealtEvent { damage = damage, wasCritical = (damage > playerAttackDamage) }); // Fire event for damage dealt BY player TO monsters (shows above enemies)
        
        // Log combat message
        if (target.monsterData != null)
        {
            string critText = (damage > playerAttackDamage) ? " (Critical!)" : "";
            LogCombatMessage($"You deal {damage:F0} damage to {target.monsterData.monsterName}{critText}", LogType.Info);
        }
        
        if (target.currentHealth <= 0)
        {
            // Capture enemy position at moment of death before any cleanup
            Vector2 deathPosition = Vector2.zero;
            if (visualManager != null)
            {
                deathPosition = visualManager.GetEnemyPosition(targetIndex);
            }
            OnMonsterDefeated(targetIndex, deathPosition);
        }
    }
    
    void MonsterAttack(int monsterIndex)
    {
        if (monsterIndex < 0 || monsterIndex >= activeMonsters.Count)
            return;
        
        var monster = activeMonsters[monsterIndex];
        if (!monster.IsAlive() || monster.isAttackInProgress)
            return;
        
        // Visual combat: play attack animation first, then apply damage
        if (visualManager != null)
        {
            monster.isAttackInProgress = true;
            visualManager.EnemyAttack(monsterIndex, () => {
                // Attack animation complete, apply damage
                ApplyMonsterDamage(monsterIndex);
                monster.isAttackInProgress = false;
            });
        }
        else
        {
            // Fallback: apply damage immediately if no visual manager
            ApplyMonsterDamage(monsterIndex);
        }
    }
    
    /// <summary>
    /// Apply damage dealt by monster (called after attack animation completes)
    /// </summary>
    void ApplyMonsterDamage(int monsterIndex)
    {
        if (monsterIndex < 0 || monsterIndex >= activeMonsters.Count)
            return;
        
        var monster = activeMonsters[monsterIndex];
        if (!monster.IsAlive())
            return;
        
        float damage = monster.attackDamage;
        
        // Get combined stats from equipment and talents
        CombatStats stats = CombatLogic.GetCombatStats();
        
        // Apply dodge
        if (stats.dodge > 0 && UnityEngine.Random.value <= stats.dodge)
        {
            // Log dodge message
            if (monster.monsterData != null)
            {
                LogCombatMessage($"{monster.monsterData.monsterName} attacks, but you dodge!", LogType.Success);
            }
            
            return; // Attack dodged, no damage taken
        }
        
        // Apply armor damage reduction
        if (stats.armor > 0)
        {
            damage *= (1f - stats.armor); // Reduce damage by armor %
        }
        
        playerCurrentHealth -= damage;
        EventBus.Publish(new PlayerHealthChangedEvent { currentHealth = playerCurrentHealth, maxHealth = playerMaxHealth });
        EventBus.Publish(new PlayerDamageTakenEvent { damage = damage }); // Fire event for damage dealt TO player BY monsters (shows player damage)
        
        // Log combat message
        if (monster.monsterData != null)
        {
            LogCombatMessage($"{monster.monsterData.monsterName} hits you for {damage:F0} damage", LogType.Warning);
        }
        
        // Sync with CharacterManager
        if (characterService != null)
        {
            float damageTaken = characterService.GetCurrentHealth() - playerCurrentHealth;
            if (damageTaken > 0)
            {
                characterService.TakeDamage(damageTaken);
            }
        }
        
        if (playerCurrentHealth <= 0)
        {
            OnPlayerDefeated();
        }
    }
    
    void OnMonsterDefeated(int monsterIndex, Vector2 deathPosition)
    {
        if (monsterIndex < 0 || monsterIndex >= activeMonsters.Count)
            return;
        
        var defeatedMonster = activeMonsters[monsterIndex];
        if (defeatedMonster.monsterData == null)
            return;
        
        // Log victory message
        LogCombatMessage($"You defeated {defeatedMonster.monsterData.monsterName}!", LogType.Success);
        
        // Give rewards with equipment bonuses
        if (characterService != null)
        {
            int xpReward = defeatedMonster.monsterData.xpReward;
            int goldReward = defeatedMonster.monsterData.goldReward;
            
            // Get bonus stats
            CombatStats stats = CombatLogic.GetCombatStats();
            
            xpReward = Mathf.RoundToInt(xpReward * (1f + stats.xpBonus));
            goldReward = Mathf.RoundToInt(goldReward * (1f + stats.goldBonus));
            
            characterService.AddXP(xpReward);
            characterService.AddGold(goldReward);
            
            // Process drop table - roll for each item independently
            List<MonsterDropEntry> droppedItems = new List<MonsterDropEntry>();
            if (defeatedMonster.monsterData.dropTable != null && defeatedMonster.monsterData.dropTable.Count > 0)
            {
                foreach (MonsterDropEntry dropEntry in defeatedMonster.monsterData.dropTable)
                {
                    if (dropEntry.item != null && UnityEngine.Random.value <= dropEntry.dropChance)
                    {
                        // Add to dropped items list for visual effect
                        droppedItems.Add(dropEntry);
                        
                        // Create and add item to inventory
                        InventoryItem drop = dropEntry.item.CreateInventoryItem(dropEntry.quantity);
                        characterService.AddItemToInventory(drop);
                    }
                }
            }
            
            // Show visual drop effect if items were dropped (at this monster's death position)
            if (droppedItems.Count > 0 && visualManager != null)
            {
                visualManager.ShowItemDrops(droppedItems, deathPosition, monsterIndex);
            }
        }
        
        // Notify that this monster died
        EventBus.Publish(new MonsterDiedEvent { monsterData = defeatedMonster.monsterData, monsterIndex = monsterIndex });
        
        // Clean up visual for dead enemy
        if (visualManager != null)
        {
            visualManager.CleanupEnemyVisual(monsterIndex);
        }
        
        // Remove from active list (or mark as dead - we'll keep it for now but check IsAlive)
        // Actually, we'll keep it in the list but it's marked as dead via health <= 0
        
        // Check if all monsters are dead - if so, respawn new group
        bool allDead = true;
        for (int i = 0; i < activeMonsters.Count; i++)
        {
            if (activeMonsters[i].IsAlive())
            {
                allDead = false;
                break;
            }
        }
        
        if (allDead)
        {
            // All monsters defeated - respawn new group after a delay
            // Get mob count from selector (read current value, not cached)
            int mobCount = GetMobCountFromSelector();
            StartCoroutine(RespawnMonsterGroupAfterDelay(mobCount, GameBalance.Combat.monsterRespawnDelay));
        }
        else
        {
            // Some monsters still alive - switch target if current target died
            if (currentTargetIndex == monsterIndex)
            {
                // Find next alive monster
                for (int i = 0; i < activeMonsters.Count; i++)
                {
                    if (activeMonsters[i].IsAlive())
                    {
                        currentTargetIndex = i;
                        EventBus.Publish(new TargetChangedEvent { newTargetIndex = currentTargetIndex });
                        break;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Coroutine to respawn monster group after delay
    /// </summary>
    System.Collections.IEnumerator RespawnMonsterGroupAfterDelay(int mobCount, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnMonsterGroup(mobCount);
    }
    
    void OnPlayerDefeated()
    {
        // Log defeat message
        if (activeMonsters.Count > 0)
        {
            var target = GetCurrentTarget();
            string monsterName = target != null && target.monsterData != null ? target.monsterData.monsterName : "monsters";
            LogCombatMessage($"You were defeated by {monsterName}!", LogType.Error);
        }
        
        CombatState oldState = currentState;
        currentState = CombatState.Defeat;
        EventBus.Publish(new CombatStateChangedEvent { oldState = oldState, newState = currentState });
        
        // Combat is paused - player must click Continue to resume
        // Healing will happen when Continue is clicked
    }
    
    /// <summary>
    /// Resume combat after defeat - called by UI Continue button
    /// Heals player to full HP and starts combat again
    /// </summary>
    public void ResumeAfterDefeat()
    {
        if (currentState == CombatState.Defeat)
        {
            // Heal player to full health
            if (characterService != null)
            {
                characterService.HealToFull();
                playerCurrentHealth = characterService.GetCurrentHealth();
                
                // Recalculate stats in case player leveled up
                CalculatePlayerStats();
            }
            
            // Spawn a new monster group (use same count as before, or default to 1)
            int mobCount = activeMonsters.Count > 0 ? activeMonsters.Count : 1;
            SpawnMonsterGroup(mobCount);
        }
    }
    
    /// <summary>
    /// End combat and return to zone
    /// </summary>
    public void EndCombat()
    {
        currentState = CombatState.Idle;
        activeMonsters.Clear();
        zoneMonsters = null;
        currentTargetIndex = 0;
        
        // Stop tracking activity in AwayActivityManager
        if (Services.TryGet<IAwayActivityService>(out var awayActivityService))
        {
            awayActivityService.StopActivity();
        }
        
        // Clean up visual combat
        if (visualManager != null)
        {
            visualManager.Cleanup();
        }
        
        // Clear visual manager reference so it gets re-found next time (in case scene was reloaded)
        visualManager = null;
        
        CombatState oldState = currentState;
        EventBus.Publish(new CombatStateChangedEvent { oldState = oldState, newState = currentState });
    }
    
    // Getters
    public CombatState GetCombatState() => currentState;
    public CombatMonsterInstance GetCurrentTargetInstance() => GetCurrentTarget();
    public int GetCurrentTargetIndex() => currentTargetIndex;
    public float GetPlayerCurrentHealth() => playerCurrentHealth;
    public float GetPlayerMaxHealth() => playerMaxHealth;
    public float GetPlayerAttackDamage() => playerAttackDamage;
    public float GetPlayerAttackSpeed() => playerAttackSpeed;
    
    /// <summary>
    /// Get monster health by index
    /// </summary>
    public float GetMonsterCurrentHealth(int index)
    {
        if (index >= 0 && index < activeMonsters.Count)
            return activeMonsters[index].currentHealth;
        return 0f;
    }
    
    /// <summary>
    /// Get monster max health by index
    /// </summary>
    public float GetMonsterMaxHealth(int index)
    {
        if (index >= 0 && index < activeMonsters.Count)
            return activeMonsters[index].maxHealth;
        return 0f;
    }
}

