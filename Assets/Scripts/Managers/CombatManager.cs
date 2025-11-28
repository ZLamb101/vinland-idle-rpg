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
    
    [Header("Health Regeneration")]
    private float healthRegenTimer = 0f;
    private const float healthRegenTickRate = 1f; // Apply health regen every 1 second
    
    [Header("Mana")]
    private float playerCurrentMana;
    private float playerMaxMana;
    
    [Header("Visual Combat")]
    public CombatSceneController combatSceneController;
    
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
    private ISkillService skillService;
    
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
        skillService = Services.Get<ISkillService>();
        
        // Subscribe to events
        EventBus.Subscribe<TalentBonusesRecalculatedEvent>(OnTalentBonusesChanged);
        EventBus.Subscribe<StatsRecalculatedEvent>(OnStatsRecalculated);
        EventBus.Subscribe<CharacterLevelUpEvent>(OnCharacterLevelUp);
        EventBus.Subscribe<CharacterHealthChangedEvent>(OnCharacterHealthChanged);
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
        EventBus.Unsubscribe<TalentBonusesRecalculatedEvent>(OnTalentBonusesChanged);
        EventBus.Unsubscribe<StatsRecalculatedEvent>(OnStatsRecalculated);
        EventBus.Unsubscribe<CharacterLevelUpEvent>(OnCharacterLevelUp);
        EventBus.Unsubscribe<CharacterHealthChangedEvent>(OnCharacterHealthChanged);
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
        Debug.Log($"[CombatManager] StartCombat called with {mobCount} monsters");
        
        if (monsters == null || monsters.Length == 0)
        {
            Debug.LogWarning("[CombatManager] StartCombat failed: No monsters provided");
            return;
        }
        
        // Stop gathering if player is currently gathering resources
        IResourceService resourceService;
        if (Services.TryGet<IResourceService>(out resourceService) && resourceService.IsGathering())
        {
            resourceService.StopGathering();
        }
        
        // IMPORTANT: Re-find visual manager in case scene was reloaded
        // CombatManager persists between scenes, but CombatSceneController is recreated
        if (combatSceneController == null)
        {
            combatSceneController = ComponentInjector.FindComponent<CombatSceneController>();
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
        
        Debug.Log("[CombatManager] About to call CalculatePlayerStats()...");
        
        // Initialize player stats with equipment bonuses
        CalculatePlayerStats();
        
        Debug.Log($"[CombatManager] After CalculatePlayerStats - Attack: {playerAttackDamage}, Health: {playerMaxHealth}");
        
        // Publish health and mana change to update UI
        EventBus.Publish(new PlayerHealthChangedEvent { currentHealth = playerCurrentHealth, maxHealth = playerMaxHealth });
        EventBus.Publish(new CharacterManaChangedEvent { currentMana = playerCurrentMana, maxMana = playerMaxMana, manaChanged = 0 });
        
        // Spawn monster group (all at once)
        SpawnMonsterGroup(mobCount);
    }
    
    /// <summary>
    /// Handle talent bonus updates
    /// </summary>
    private void OnTalentBonusesChanged(TalentBonusesRecalculatedEvent e)
    {
        // Recalculate stats to apply new bonuses immediately
        CalculatePlayerStats();
        
        // Update health UI
        EventBus.Publish(new PlayerHealthChangedEvent { currentHealth = playerCurrentHealth, maxHealth = playerMaxHealth });
        
        // Log update if in combat
        if (currentState == CombatState.Fighting)
        {
            LogCombatMessage("Combat stats updated from talents!", LogType.Info);
        }
    }

    /// <summary>
    /// Handle equipment stat updates
    /// </summary>
    private void OnStatsRecalculated(StatsRecalculatedEvent e)
    {
        // Recalculate stats to apply new bonuses immediately
        CalculatePlayerStats();
        
        // Update health UI
        EventBus.Publish(new PlayerHealthChangedEvent { currentHealth = playerCurrentHealth, maxHealth = playerMaxHealth });
        
        // Log update if in combat
        if (currentState == CombatState.Fighting)
        {
            LogCombatMessage("Combat stats updated from equipment!", LogType.Info);
        }
    }
    
    /// <summary>
    /// Handle character level up - recalculate stats and heal to full
    /// </summary>
    private void OnCharacterLevelUp(CharacterLevelUpEvent e)
    {
        // Recalculate stats to apply new attribute increases immediately
        CalculatePlayerStats();
        
        // Heal player to full health on level up
        playerCurrentHealth = playerMaxHealth;
        EventBus.Publish(new PlayerHealthChangedEvent { currentHealth = playerCurrentHealth, maxHealth = playerMaxHealth });
        
        // Log level up if in combat
        if (currentState == CombatState.Fighting)
        {
            LogCombatMessage($"Level up! You are now level {e.newLevel}! Stats increased!", LogType.Success);
        }
    }
    
    /// <summary>
    /// Sync combat health with character manager (for DoT damage, etc.)
    /// </summary>
    private void OnCharacterHealthChanged(CharacterHealthChangedEvent e)
    {
        // Only sync during combat and if health decreased (damage taken)
        if (currentState == CombatState.Fighting && e.healthChanged < 0)
        {
            // Sync our local health with character manager
            playerCurrentHealth = e.currentHealth;
            playerMaxHealth = e.maxHealth;
            
            // Publish combat health event for UI updates
            EventBus.Publish(new PlayerHealthChangedEvent { currentHealth = playerCurrentHealth, maxHealth = playerMaxHealth });
            
            // Check for player death
            if (playerCurrentHealth <= 0)
            {
                OnPlayerDefeated();
            }
        }
    }
    
    /// <summary>
    /// Calculate player stats including equipment and talent bonuses
    /// Now uses attribute-based attack power from Strength + Agility
    /// </summary>
    public void CalculatePlayerStats()
    {
        Debug.Log($"[CombatManager] CalculatePlayerStats called. CharacterService exists: {characterService != null}");
        
        // Get character data to access attributes
        CharacterData charData = characterService?.GetCharacterData();
        
        Debug.Log($"[CombatManager] CharacterData exists: {charData != null}");
        
        // Base stats from character attributes
        if (charData != null)
        {
            // Get attack power from attributes (STR * 2 + AGI * 1)
            playerAttackDamage = charData.GetAttackPower();
            
            // Get max health from attributes (STA * 10)
            playerMaxHealth = charData.GetMaxHealth();
            playerCurrentHealth = characterService.GetCurrentHealth();
            
            // Get mana from attributes (INT * 10)
            playerMaxMana = charData.GetMaxMana();
            playerCurrentMana = charData.currentMana;
            
            // Debug logging to verify attribute values
            Debug.Log($"[CombatManager] Character attributes - STR: {charData.strength}, AGI: {charData.agility}, STA: {charData.stamina}, SPR: {charData.spirit}, INT: {charData.intellect}");
            Debug.Log($"[CombatManager] Calculated attack power: {playerAttackDamage} (STR:{charData.strength}*2 + AGI:{charData.agility}*1)");
        }
        else
        {
            // Fallback to old system if no character data
            Debug.LogWarning("[CombatManager] CharacterData is NULL! Using fallback stats.");
            playerMaxHealth = GameBalance.Combat.playerStartingHealth;
            playerCurrentHealth = GameBalance.Combat.playerStartingHealth;
            playerAttackDamage = playerBaseAttackDamage;
            playerMaxMana = 100f;
            playerCurrentMana = 100f;
        }
        
        // Start with base attack speed (not affected by attributes currently)
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
            
            // Add max health bonus from talents
            playerMaxHealth += talents.maxHealth;
            playerMaxHealth *= (1f + talents.healthMultiplier);
            
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
        
        // Log the calculated stats for debugging
        Debug.Log($"[CombatManager] Stats recalculated - Attack: {playerAttackDamage:F1}, Max Health: {playerMaxHealth:F0}, Current Health: {playerCurrentHealth:F0}");
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
            
            // Get random level for this monster instance
            int monsterLevel = MonsterStatCalculator.GetRandomLevel(selectedMonster);
            
            // Create combat instance
            CombatMonsterInstance instance = new CombatMonsterInstance(selectedMonster, monsterLevel, i);
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
        if (combatSceneController == null)
        {
            combatSceneController = ComponentInjector.FindComponent<CombatSceneController>();
        }
        
        if (combatSceneController != null)
        {
            // Get hero sprite (if available)
            Sprite heroSprite = null;
            // TODO: Get hero sprite from CharacterManager or CharacterData if available
            
            combatSceneController.InitializeCombat(heroSprite, monstersToSpawn);
        }
        
        // Log combat start
        if (monstersToSpawn.Count > 0)
        {
            string monsterNames = string.Join(", ", monstersToSpawn.ConvertAll(m => m.monsterName));
            LogCombatMessage($"Combat started against {monstersToSpawn.Count} monster(s): {monsterNames}!", LogType.Info);
        }
        
        // Reset timers
        playerAttackTimer = 0f;
        healthRegenTimer = 0f;
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
        // Re-fetch skill service if null
        if (skillService == null)
        {
            skillService = Services.Get<ISkillService>();
        }
        
        // Check if player is stunned
        bool playerStunned = skillService != null && skillService.IsPlayerStunned();
        
        // Update player attack timer - player can attack immediately when enemy spawns
        if (!playerStunned)
        {
            playerAttackTimer += Time.deltaTime;
            EventBus.Publish(new PlayerAttackProgressEvent { progress = Mathf.Clamp01(playerAttackTimer / playerAttackSpeed) });
            
            if (playerAttackTimer >= playerAttackSpeed)
            {
                PlayerAttack();
                playerAttackTimer = 0f;
            }
            
            // Auto-cast action bar skills
            UpdateSkillCasting();
        }
        
        // Update health and mana regeneration timer
        healthRegenTimer += Time.deltaTime;
        if (healthRegenTimer >= healthRegenTickRate)
        {
            ApplyHealthRegeneration();
            ApplyManaRegeneration();
            healthRegenTimer = 0f;
        }
        
        // Update each monster's attack timer independently
        for (int i = 0; i < activeMonsters.Count; i++)
        {
            var monster = activeMonsters[i];
            if (!monster.IsAlive() || monster.isAttackInProgress)
                continue;
            
            // Check if monster is stunned
            bool monsterStunned = skillService != null && skillService.IsMonsterStunned(i);
            if (monsterStunned)
                continue;
            
            // Check if this specific enemy is in attack range
            bool canAttack = true;
            if (combatSceneController != null)
            {
                canAttack = combatSceneController.IsEnemyInAttackRange(i);
            }
            
            if (canAttack)
            {
                // Apply slow from debuffs
                float slowPercent = skillService != null ? skillService.GetMonsterSlowPercent(i) : 0f;
                float effectiveAttackSpeed = monster.attackSpeed * (1f + slowPercent);
                
                monster.attackTimer += Time.deltaTime;
                EventBus.Publish(new MonsterAttackProgressEvent { progress = Mathf.Clamp01(monster.attackTimer / effectiveAttackSpeed), monsterIndex = i });
                
                if (monster.attackTimer >= effectiveAttackSpeed)
                {
                    MonsterAttack(i);
                    monster.attackTimer = 0f;
                }
            }
        }
        
        // Update monster skill casting
        UpdateMonsterSkillCasting();
    }
    
    /// <summary>
    /// Auto-cast action bar skills when off cooldown
    /// </summary>
    void UpdateSkillCasting()
    {
        if (skillService == null) return;
        
        SkillData[] actionBarSkills = skillService.GetAllActionBarSkills();
        if (actionBarSkills == null) return;
        
        for (int i = 0; i < actionBarSkills.Length; i++)
        {
            SkillData skill = actionBarSkills[i];
            if (skill == null) continue;
            
            // Check if skill can be cast
            if (!skillService.CanCastSkill(skill)) continue;
            
            // Determine target based on skill type
            int targetIndex = -1;
            if (skill.RequiresTarget())
            {
                targetIndex = currentTargetIndex;
                // Verify target is valid
                if (targetIndex < 0 || targetIndex >= activeMonsters.Count || !activeMonsters[targetIndex].IsAlive())
                {
                    // Find a valid target
                    targetIndex = -1;
                    for (int m = 0; m < activeMonsters.Count; m++)
                    {
                        if (activeMonsters[m].IsAlive())
                        {
                            targetIndex = m;
                            break;
                        }
                    }
                }
                
                if (targetIndex < 0) continue; // No valid target
            }
            
            // Cast the skill
            if (skillService.CastSkill(skill, targetIndex))
            {
                LogCombatMessage($"You cast {skill.skillName}!", LogType.Info);
            }
        }
    }
    
    /// <summary>
    /// Handle monster skill casting
    /// Monsters don't require mana - they cast skills purely based on cooldown
    /// Skills use their own range (melee/ranged), NOT the monster's attack type
    /// </summary>
    void UpdateMonsterSkillCasting()
    {
        for (int i = 0; i < activeMonsters.Count; i++)
        {
            var monster = activeMonsters[i];
            if (!monster.IsAlive())
                continue;
            
            // Check if monster is stunned
            bool monsterStunned = skillService != null && skillService.IsMonsterStunned(i);
            if (monsterStunned)
                continue;
            
            // Update skill cooldowns (always, even during attacks)
            monster.UpdateSkillCooldowns(Time.deltaTime);
            
            // Skip skill casting if attack animation is in progress
            // But still allow skill cooldowns to tick
            if (monster.isAttackInProgress)
                continue;
            
            // Check if monster has skills
            if (!monster.HasSkills())
                continue;
            
            // Try to cast an available skill that's in range
            // Each skill is checked against its OWN range (melee/ranged)
            SkillData availableSkill = GetAvailableMonsterSkillInRange(monster, i);
            if (availableSkill != null)
            {
                CastMonsterSkill(monster, availableSkill, i);
            }
        }
    }
    
    /// <summary>
    /// Get an available skill for a monster that's in range
    /// Skills use their own range setting, not the monster's attack type
    /// </summary>
    SkillData GetAvailableMonsterSkillInRange(CombatMonsterInstance monster, int monsterIndex)
    {
        if (monster.monsterData == null || monster.monsterData.skills == null)
            return null;
        
        foreach (SkillData skill in monster.monsterData.skills)
        {
            if (skill == null || !monster.CanUseSkill(skill))
                continue;
            
            // Check if monster is in range for THIS skill's range type
            bool inRange = true;
            if (combatSceneController != null)
            {
                inRange = combatSceneController.IsEnemyInSkillRange(monsterIndex, skill.skillRange);
            }
            
            if (inRange)
            {
                return skill;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Cast a skill from a monster
    /// Monsters don't need mana - they cast based on cooldown only
    /// </summary>
    void CastMonsterSkill(CombatMonsterInstance monster, SkillData skill, int monsterIndex)
    {
        if (monster == null || skill == null) return;
        
        // Start cooldown
        monster.StartSkillCooldown(skill);
        
        // Fire cast event
        EventBus.Publish(new SkillCastEvent { skill = skill, isPlayerCast = false, casterIndex = monsterIndex });
        
        // Log skill cast
        string monsterName = monster.monsterData != null ? monster.monsterData.monsterName : "Enemy";
        LogCombatMessage($"{monsterName} casts {skill.skillName}!", LogType.Warning);
        Debug.Log($"[CombatManager] Monster {monsterName} casting skill: {skill.skillName} (Type: {skill.skillType})");
        
        // Execute skill based on type
        switch (skill.skillType)
        {
            case SkillType.Direct:
                ExecuteMonsterDirectSkill(monster, skill, monsterIndex);
                break;
            case SkillType.Buff:
                // Monster buffs self - apply effect to self
                if (skillService != null)
                {
                    // For monster buffs, we'd need to track them separately
                    // For now, just log it
                    Debug.Log($"[CombatManager] Monster buff skill (not yet implemented): {skill.skillName}");
                }
                break;
            case SkillType.Curse:
                ExecuteMonsterCurseSkill(monster, skill, monsterIndex);
                break;
        }
    }
    
    /// <summary>
    /// Execute a direct damage skill from a monster on the player
    /// Handles projectile/instant visuals based on skill settings
    /// </summary>
    void ExecuteMonsterDirectSkill(CombatMonsterInstance monster, SkillData skill, int monsterIndex)
    {
        // Check if player is invulnerable
        if (skillService != null && skillService.IsPlayerInvulnerable())
        {
            LogCombatMessage($"You are invulnerable to {skill.skillName}!", LogType.Success);
            return;
        }
        
        // Calculate damage (monsters use their attack damage for scaling)
        float damage = skill.CalculateDamage(monster.attackDamage);
        
        // Execute based on visual type
        switch (skill.visualType)
        {
            case SkillVisualType.Projectile:
                // Spawn projectile from enemy to hero, apply damage on hit
                if (combatSceneController != null)
                {
                    combatSceneController.SpawnEnemySkillProjectile(skill, monsterIndex, damage, (finalDamage) => {
                        ApplyMonsterSkillDamageToPlayer(skill, finalDamage);
                    });
                }
                else
                {
                    ApplyMonsterSkillDamageToPlayer(skill, damage);
                }
                break;
                
            case SkillVisualType.Instant:
                // Apply damage immediately, spawn hit effect on hero
                ApplyMonsterSkillDamageToPlayer(skill, damage);
                if (combatSceneController != null)
                {
                    combatSceneController.SpawnHitEffectOnHero(skill);
                }
                break;
                
            case SkillVisualType.None:
            default:
                // Apply damage immediately, no visuals
                ApplyMonsterSkillDamageToPlayer(skill, damage);
                break;
        }
    }
    
    /// <summary>
    /// Apply damage from a monster skill to the player
    /// </summary>
    void ApplyMonsterSkillDamageToPlayer(SkillData skill, float damage)
    {
        // Check if player is invulnerable (might have become invulnerable during projectile travel)
        if (skillService != null && skillService.IsPlayerInvulnerable())
        {
            LogCombatMessage($"You are invulnerable to {skill.skillName}!", LogType.Success);
            return;
        }
        
        // Get player stats for defense
        CombatStats stats = CombatLogic.GetCombatStats();
        
        // Apply dodge
        if (stats.dodge > 0 && UnityEngine.Random.value <= stats.dodge)
        {
            LogCombatMessage($"You dodge {skill.skillName}!", LogType.Success);
            return;
        }
        
        // Apply armor
        if (stats.armor > 0)
        {
            damage *= (1f - stats.armor);
        }
        
        // Apply skill effect modifiers
        if (skillService != null)
        {
            StatModifiers effectMods = skillService.GetPlayerEffectModifiers();
            if (effectMods != null)
            {
                damage *= (1f + effectMods.damageTakenMultiplier);
                if (effectMods.armor > 0)
                {
                    damage *= (1f - effectMods.armor);
                }
            }
        }
        
        damage = Mathf.Max(0f, damage);
        
        // Apply damage
        playerCurrentHealth -= damage;
        EventBus.Publish(new PlayerHealthChangedEvent { currentHealth = playerCurrentHealth, maxHealth = playerMaxHealth });
        EventBus.Publish(new SkillHitEvent { skill = skill, damage = damage, wasCritical = false, targetIndex = -1 });
        
        LogCombatMessage($"{skill.skillName} hits you for {damage:F0} damage!", LogType.Warning);
        
        // Sync with CharacterManager
        if (characterService != null)
        {
            characterService.TakeDamage(damage);
        }
        
        if (playerCurrentHealth <= 0)
        {
            OnPlayerDefeated();
        }
    }
    
    /// <summary>
    /// Execute a curse skill from a monster on the player
    /// </summary>
    void ExecuteMonsterCurseSkill(CombatMonsterInstance monster, SkillData skill, int monsterIndex)
    {
        // Check if player is invulnerable
        if (skillService != null && skillService.IsPlayerInvulnerable())
        {
            LogCombatMessage($"You are invulnerable to {skill.skillName}!", LogType.Success);
            return;
        }
        
        // Apply the curse effect to player (requires aura)
        if (skillService != null && skill.appliedAura != null)
        {
            // Get monster's attack power for DoT scaling
            float monsterAttackPower = monster.attackDamage;
            skillService.ApplyEffect(skill, -1, monsterAttackPower); // -1 = player
            LogCombatMessage($"You are afflicted by {skill.skillName}!", LogType.Warning);
        }
        
        // Apply initial damage if any
        if (skill.baseDamage > 0 || skill.attackPowerScaling > 0)
        {
            ExecuteMonsterDirectSkill(monster, skill, monsterIndex);
        }
    }
    
    /// <summary>
    /// Apply health regeneration from Spirit attribute
    /// </summary>
    void ApplyHealthRegeneration()
    {
        // Get combat stats which includes Spirit-based health regen
        CombatStats stats = CombatLogic.GetCombatStats();
        
        if (stats.healthRegen > 0)
        {
            // Apply health regen if player is not at full health
            if (playerCurrentHealth < playerMaxHealth)
            {
                float healAmount = stats.healthRegen * healthRegenTickRate;
                playerCurrentHealth = Mathf.Min(playerCurrentHealth + healAmount, playerMaxHealth);
                EventBus.Publish(new PlayerHealthChangedEvent { currentHealth = playerCurrentHealth, maxHealth = playerMaxHealth });
                
                // Sync with CharacterManager
                if (characterService != null)
                {
                    characterService.Heal(healAmount);
                }
            }
        }
    }
    
    /// <summary>
    /// Apply mana regeneration from Spirit attribute
    /// </summary>
    void ApplyManaRegeneration()
    {
        if (characterService == null) return;
        
        CharacterData charData = characterService.GetCharacterData();
        if (charData == null) return;
        
        float manaRegen = charData.GetManaRegen();
        if (manaRegen > 0)
        {
            float maxMana = charData.GetMaxMana();
            if (charData.currentMana < maxMana)
            {
                float regenAmount = manaRegen * healthRegenTickRate;
                charData.currentMana = Mathf.Min(charData.currentMana + regenAmount, maxMana);
                playerCurrentMana = charData.currentMana;
                playerMaxMana = maxMana;
                
                EventBus.Publish(new CharacterManaChangedEvent 
                { 
                    currentMana = charData.currentMana, 
                    maxMana = maxMana, 
                    manaChanged = regenAmount 
                });
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
        
        // Apply skill effect modifiers (damage bonus from buffs)
        if (skillService != null)
        {
            StatModifiers effectMods = skillService.GetPlayerEffectModifiers();
            if (effectMods != null)
            {
                damage += effectMods.attackDamage;
                damage *= (1f + effectMods.damageMultiplier);
            }
        }
        
        // Get combined stats from equipment and talents
        CombatStats stats = CombatLogic.GetCombatStats();
        
        // Apply critical hit
        if (stats.critChance > 0 && UnityEngine.Random.value <= stats.critChance)
        {
            damage *= stats.critDamage; // Critical hit damage
        }
        
        // Visual combat: spawn projectile targeting current target
        if (combatSceneController != null)
        {
            combatSceneController.HeroAttack(damage, currentTargetIndex, (dealtDamage, targetIndex) => {
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
        
        // Check for miss chance after projectile hits (WoW-style level-based miss chance)
        int playerLevel = characterService != null ? characterService.GetLevel() : 1;
        int enemyLevel = target.level;
        
        // Calculate total miss chance: base 1% + level-based miss chance
        // Level-based miss chance can be negative (when player is higher level), but base 1% always applies
        float levelBasedMissChance = CombatLogic.CalculateMissChance(playerLevel, enemyLevel);
        float totalMissChance = GameBalance.Combat.baseMissChance + levelBasedMissChance;
        
        // Clamp miss chance: minimum is base miss chance (1%), maximum is 100%
        totalMissChance = Mathf.Clamp(totalMissChance, GameBalance.Combat.baseMissChance, 1.0f);
        
        // Check if attack misses
        bool willMiss = UnityEngine.Random.value <= totalMissChance;
        
        if (willMiss)
        {
            // Attack missed - show miss text and log
            EventBus.Publish(new PlayerDamageDealtEvent { damage = 0f, wasCritical = false, wasMiss = true });
            if (target.monsterData != null)
            {
                LogCombatMessage($"You attack {target.monsterData.monsterName}, but miss!", LogType.Info);
            }
            return; // Exit early, no damage dealt
        }
        
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
            if (combatSceneController != null)
            {
                deathPosition = combatSceneController.GetEnemyPosition(targetIndex);
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
        if (combatSceneController != null)
        {
            monster.isAttackInProgress = true;
            combatSceneController.EnemyAttack(monsterIndex, () => {
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
        
        // Check if player is invulnerable from buffs
        if (skillService != null && skillService.IsPlayerInvulnerable())
        {
            if (monster.monsterData != null)
            {
                LogCombatMessage($"{monster.monsterData.monsterName} attacks, but you are invulnerable!", LogType.Success);
            }
            return;
        }
        
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
        
        // Apply skill effect modifiers (damage taken multiplier)
        if (skillService != null)
        {
            StatModifiers effectMods = skillService.GetPlayerEffectModifiers();
            if (effectMods != null && effectMods.damageTakenMultiplier != 0)
            {
                damage *= (1f + effectMods.damageTakenMultiplier);
            }
            
            // Apply additional armor from buffs
            if (effectMods != null && effectMods.armor > 0)
            {
                damage *= (1f - effectMods.armor);
            }
        }
        
        // Ensure damage is at least 0
        damage = Mathf.Max(0f, damage);
        
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

            // Calculate scaled rewards based on monster level and player level
            int playerLevel = characterService.GetLevel();
            CalculatedMonsterRewards rewards = MonsterStatCalculator.CalculateRewards(defeatedMonster.monsterData, defeatedMonster.level, playerLevel);

            // Get bonus stats
            CombatStats stats = CombatLogic.GetCombatStats();
            
            rewards.xpReward = Mathf.RoundToInt(rewards.xpReward * (1f + stats.xpBonus));
            rewards.goldReward = Mathf.RoundToInt(rewards.goldReward * (1f + stats.goldBonus));
            
            characterService.AddXP(rewards.xpReward);
            characterService.AddGold(rewards.goldReward);
            
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
            if (droppedItems.Count > 0 && combatSceneController != null)
            {
                combatSceneController.ShowItemDrops(droppedItems, deathPosition, monsterIndex);
            }
        }
        
        // Notify that this monster died
        EventBus.Publish(new MonsterDiedEvent { monsterData = defeatedMonster.monsterData, monsterIndex = monsterIndex });
        
        // Clean up visual for dead enemy
        if (combatSceneController != null)
        {
            combatSceneController.CleanupEnemyVisual(monsterIndex);
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
        
        // Clear all buffs and debuffs on death
        if (skillService != null)
        {
            skillService.ClearAllEffects();
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
        
        // Clear skill effects
        if (skillService != null)
        {
            skillService.ClearAllEffects();
        }
        
        // Clean up visual combat
        if (combatSceneController != null)
        {
            combatSceneController.Cleanup();
        }
        
        // Clear visual manager reference so it gets re-found next time (in case scene was reloaded)
        combatSceneController = null;
        
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
    
    /// <summary>
    /// Handle monster death from external sources (e.g., skills)
    /// </summary>
    public void HandleMonsterDeath(int monsterIndex)
    {
        if (monsterIndex < 0 || monsterIndex >= activeMonsters.Count)
            return;
        
        var monster = activeMonsters[monsterIndex];
        if (monster.IsAlive())
            return; // Monster isn't dead yet
        
        // Get death position for item drops
        Vector2 deathPosition = Vector2.zero;
        if (combatSceneController != null)
        {
            deathPosition = combatSceneController.GetEnemyPosition(monsterIndex);
        }
        
        // Trigger full death handling
        OnMonsterDefeated(monsterIndex, deathPosition);
    }
}

