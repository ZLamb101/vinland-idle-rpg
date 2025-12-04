using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manager for skill system.
/// Handles action bar skills, cooldowns, skill casting, and active effects.
/// </summary>
public class SkillManager : MonoBehaviour, ISkillService
{
    [Header("Available Skills")]
    [Tooltip("All skills available to the player (loaded from Resources/Skills)")]
    public List<SkillData> availableSkills = new List<SkillData>();
    
    [Header("Action Bar")]
    private SkillData[] actionBarSkills = new SkillData[6];
    
    [Header("Cooldowns")]
    private Dictionary<string, float> skillCooldowns = new Dictionary<string, float>();
    private float globalCooldownTimer = 0f;
    
    [Header("Active Effects")]
    private List<ActiveEffect> playerBuffs = new List<ActiveEffect>();
    private List<ActiveEffect> playerDebuffs = new List<ActiveEffect>();
    private Dictionary<int, List<ActiveEffect>> monsterEffects = new Dictionary<int, List<ActiveEffect>>();
    
    // Cached services
    private ICharacterService characterService;
    private ICombatService combatService;
    private CombatSceneController combatSceneController;
    
    void Awake()
    {
        if (Services.IsRegistered<ISkillService>())
        {
            Debug.LogWarning("[SkillManager] Service already registered. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        Services.Register<ISkillService>(this);
        
        // Load available skills from Resources
        LoadAvailableSkills();
    }
    
    void Start()
    {
        characterService = Services.Get<ICharacterService>();
        combatService = Services.Get<ICombatService>();
        
        // Subscribe to combat events
        EventBus.Subscribe<CombatEndedEvent>(OnCombatEnded);
        EventBus.Subscribe<MonsterDiedEvent>(OnMonsterDied);
    }
    
    void OnDestroy()
    {
        EventBus.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
        EventBus.Unsubscribe<MonsterDiedEvent>(OnMonsterDied);
        Services.Unregister<ISkillService>();
    }
    
    void Update()
    {
        UpdateCooldowns();
        UpdateActiveEffects();
    }
    
    /// <summary>
    /// Load all available skills from Resources/Skills folder
    /// </summary>
    void LoadAvailableSkills()
    {
        availableSkills.Clear();
        SkillData[] skills = Resources.LoadAll<SkillData>("Skills");
        if (skills != null)
        {
            availableSkills.AddRange(skills);
            Debug.Log($"[SkillManager] Loaded {skills.Length} skills from Resources/Skills");
        }
    }
    
    // ==================== Action Bar ====================
    
    public SkillData GetActionBarSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= actionBarSkills.Length)
            return null;
        return actionBarSkills[slotIndex];
    }
    
    public void SetActionBarSkill(int slotIndex, SkillData skill)
    {
        if (slotIndex < 0 || slotIndex >= actionBarSkills.Length)
            return;
        
        SkillData oldSkill = actionBarSkills[slotIndex];
        actionBarSkills[slotIndex] = skill;
        
        EventBus.Publish(new ActionBarChangedEvent { slotIndex = slotIndex, skill = skill });
        
        Debug.Log($"[SkillManager] Set action bar slot {slotIndex} to {(skill != null ? skill.skillName : "empty")}");
    }
    
    public void ClearActionBarSlot(int slotIndex)
    {
        SetActionBarSkill(slotIndex, null);
    }
    
    public void ClearAllActionBarSlots()
    {
        for (int i = 0; i < actionBarSkills.Length; i++)
        {
            actionBarSkills[i] = null;
            EventBus.Publish(new ActionBarChangedEvent { slotIndex = i, skill = null });
        }
        Debug.Log("[SkillManager] Cleared all action bar slots");
    }
    
    public SkillData[] GetAllActionBarSkills()
    {
        return (SkillData[])actionBarSkills.Clone();
    }
    
    // ==================== Cooldowns ====================
    
    void UpdateCooldowns()
    {
        // Update global cooldown
        if (globalCooldownTimer > 0f)
        {
            globalCooldownTimer -= Time.deltaTime;
            if (globalCooldownTimer < 0f)
            {
                globalCooldownTimer = 0f;
            }
        }
        
        // Update all skill cooldowns
        List<string> expiredCooldowns = new List<string>();
        List<string> keys = new List<string>(skillCooldowns.Keys);
        
        foreach (string skillName in keys)
        {
            skillCooldowns[skillName] -= Time.deltaTime;
            if (skillCooldowns[skillName] <= 0f)
            {
                expiredCooldowns.Add(skillName);
            }
        }
        
        // Remove expired cooldowns
        foreach (string skillName in expiredCooldowns)
        {
            skillCooldowns.Remove(skillName);
        }
    }
    
    public bool IsOnGlobalCooldown()
    {
        return globalCooldownTimer > 0f;
    }
    
    public float GetGlobalCooldownRemaining()
    {
        return Mathf.Max(0f, globalCooldownTimer);
    }
    
    void TriggerGlobalCooldown()
    {
        globalCooldownTimer = GameBalance.Combat.skillGlobalCooldown;
    }
    
    public bool IsOnCooldown(SkillData skill)
    {
        if (skill == null) return true;
        return skillCooldowns.ContainsKey(skill.skillName) && skillCooldowns[skill.skillName] > 0f;
    }
    
    public float GetRemainingCooldown(SkillData skill)
    {
        if (skill == null) return 0f;
        if (skillCooldowns.TryGetValue(skill.skillName, out float remaining))
        {
            return Mathf.Max(0f, remaining);
        }
        return 0f;
    }
    
    public float GetCooldownProgress(SkillData skill)
    {
        if (skill == null || skill.cooldown <= 0f) return 1f;
        float remaining = GetRemainingCooldown(skill);
        return 1f - (remaining / skill.cooldown);
    }
    
    /// <summary>
    /// Get the player's total attack power including base stats, equipment, and talents
    /// </summary>
    float GetTotalPlayerAttackPower()
    {
        float totalAttackPower = 0f;
        
        // Base attack power from attributes
        if (characterService != null)
        {
            CharacterData charData = characterService.GetCharacterData();
            if (charData != null)
            {
                totalAttackPower = charData.GetAttackPower();
            }
        }
        
        // Add equipment bonuses
        var equipmentService = Services.Get<IEquipmentService>();
        if (equipmentService != null)
        {
            EquipmentStats equipStats = equipmentService.GetTotalStats();
            totalAttackPower += equipStats.attackDamage;
        }
        
        // Add talent bonuses
        var talentService = Services.Get<ITalentService>();
        if (talentService != null)
        {
            TalentBonuses talents = talentService.GetTotalBonuses();
            totalAttackPower += talents.attackDamage;
            totalAttackPower *= (1f + talents.damageMultiplier);
        }
        
        return totalAttackPower;
    }
    
    void StartCooldown(SkillData skill)
    {
        if (skill == null || skill.cooldown <= 0f) return;
        skillCooldowns[skill.skillName] = skill.cooldown;
        
        EventBus.Publish(new SkillCooldownUpdatedEvent 
        { 
            skill = skill, 
            remainingCooldown = skill.cooldown, 
            totalCooldown = skill.cooldown 
        });
    }
    
    // ==================== Skill Casting ====================
    
    public bool CanCastSkill(SkillData skill)
    {
        if (skill == null) return false;
        
        // Check global cooldown
        if (IsOnGlobalCooldown())
            return false;
        
        // Check skill-specific cooldown
        if (IsOnCooldown(skill))
            return false;
        
        // Check mana
        if (characterService != null)
        {
            CharacterData charData = characterService.GetCharacterData();
            if (charData != null && charData.currentMana < skill.manaCost)
                return false;
        }
        
        return true;
    }
    
    public bool CastSkill(SkillData skill, int targetIndex)
    {
        if (!CanCastSkill(skill))
            return false;
        
        // Consume mana
        if (characterService != null && skill.manaCost > 0)
        {
            CharacterData charData = characterService.GetCharacterData();
            if (charData != null)
            {
                float oldMana = charData.currentMana;
                charData.currentMana -= skill.manaCost;
                EventBus.Publish(new CharacterManaChangedEvent 
                { 
                    currentMana = charData.currentMana, 
                    maxMana = charData.GetMaxMana(), 
                    manaChanged = -skill.manaCost 
                });
            }
        }
        
        // Trigger global cooldown
        TriggerGlobalCooldown();
        
        // Start skill-specific cooldown
        StartCooldown(skill);
        
        // Get player's total attack power for scaling (base + equipment + talents)
        float playerAttackPower = GetTotalPlayerAttackPower();
        
        // Execute skill based on type
        switch (skill.skillType)
        {
            case SkillType.Direct:
                ExecuteDirectSkill(skill, targetIndex);
                break;
            case SkillType.Buff:
                ApplyEffect(skill, -1, playerAttackPower); // -1 = player (self)
                break;
            case SkillType.Curse:
                ApplyEffect(skill, targetIndex, playerAttackPower);
                if (skill.baseDamage > 0 || skill.attackPowerScaling > 0)
                {
                    // Curse with initial damage
                    ExecuteDirectSkill(skill, targetIndex);
                }
                break;
        }
        
        EventBus.Publish(new SkillCastEvent { skill = skill, isPlayerCast = true, casterIndex = -1 });
        
        Debug.Log($"[SkillManager] Player cast {skill.skillName} on target {targetIndex}");
        return true;
    }
    
    void ExecuteDirectSkill(SkillData skill, int targetIndex)
    {
        if (combatService == null)
        {
            combatService = Services.Get<ICombatService>();
            if (combatService == null) return;
        }
        
        // Find combat scene controller if needed
        if (combatSceneController == null)
        {
            combatSceneController = Object.FindFirstObjectByType<CombatSceneController>();
        }
        
        // Calculate damage
        float attackPower = 0f;
        if (characterService != null)
        {
            CharacterData charData = characterService.GetCharacterData();
            if (charData != null)
            {
                attackPower = charData.GetAttackPower();
            }
        }
        
        float damage = skill.CalculateDamage(attackPower);
        
        // Apply crit
        bool wasCrit = false;
        if (skill.canCrit)
        {
            CombatStats stats = CombatLogic.GetCombatStats();
            if (stats.critChance > 0 && Random.value <= stats.critChance)
            {
                damage *= stats.critDamage;
                wasCrit = true;
            }
        }
        
        // Get targets (AoE or single)
        List<int> targets = GetTargets(skill, targetIndex);
        
        // Execute based on visual type
        switch (skill.visualType)
        {
            case SkillVisualType.Projectile:
                // Spawn projectile, apply damage when it hits
                foreach (int target in targets)
                {
                    SpawnSkillProjectile(skill, target, damage, wasCrit);
                }
                break;
                
            case SkillVisualType.Instant:
                // Apply damage immediately, spawn hit effect
                foreach (int target in targets)
                {
                    ApplyDamageToMonster(target, damage, wasCrit, skill);
                    SpawnHitEffect(skill, target);
                }
                break;
                
            case SkillVisualType.None:
            default:
                // Apply damage immediately, no visuals
                foreach (int target in targets)
                {
                    ApplyDamageToMonster(target, damage, wasCrit, skill);
                }
                break;
        }
    }
    
    void SpawnSkillProjectile(SkillData skill, int targetIndex, float damage, bool wasCrit)
    {
        if (combatSceneController == null) 
        {
            // No visual controller, apply damage immediately
            ApplyDamageToMonster(targetIndex, damage, wasCrit, skill);
            return;
        }
        
        combatSceneController.SpawnSkillProjectile(skill, targetIndex, damage, (dealtDamage, target) => {
            ApplyDamageToMonster(target, dealtDamage, wasCrit, skill);
        });
    }
    
    void SpawnHitEffect(SkillData skill, int targetIndex)
    {
        if (combatSceneController == null || skill.hitEffectPrefab == null)
            return;
        
        combatSceneController.SpawnHitEffect(skill, targetIndex);
    }
    
    List<int> GetTargets(SkillData skill, int primaryTarget)
    {
        List<int> targets = new List<int>();
        
        if (!skill.isAoE)
        {
            // Single target
            targets.Add(primaryTarget);
            return targets;
        }
        
        // AoE - get all alive monsters
        if (combatService != null)
        {
            var monsters = combatService.GetActiveMonsters();
            if (skill.aoeTargetCount == 0)
            {
                // Hit ALL enemies
                for (int i = 0; i < monsters.Count; i++)
                {
                    if (monsters[i].IsAlive())
                    {
                        targets.Add(i);
                    }
                }
            }
            else
            {
                // Hit primary + additional targets
                targets.Add(primaryTarget);
                int additionalTargets = skill.aoeTargetCount;
                
                for (int i = 0; i < monsters.Count && additionalTargets > 0; i++)
                {
                    if (i != primaryTarget && monsters[i].IsAlive())
                    {
                        targets.Add(i);
                        additionalTargets--;
                    }
                }
            }
        }
        else
        {
            targets.Add(primaryTarget);
        }
        
        return targets;
    }
    
    /// <summary>
    /// Apply DoT damage to the player, syncing with combat manager and showing damage numbers
    /// </summary>
    void ApplyDotDamageToPlayer(float damage, ActiveEffect effect)
    {
        // Publish damage taken event for visual feedback (damage numbers on hero)
        EventBus.Publish(new PlayerDamageTakenEvent { damage = damage, wasMiss = false });
        
        // Sync with character manager
        if (characterService != null)
        {
            characterService.TakeDamage(damage);
        }
        
        // Log the DoT tick
        string effectName = effect.GetName();
        Debug.Log($"[SkillManager] {effectName} deals {damage:F0} DoT damage to player");
    }
    
    void ApplyDamageToMonster(int monsterIndex, float damage, bool wasCrit, SkillData skill)
    {
        if (combatService == null) return;
        
        var monsters = combatService.GetActiveMonsters();
        if (monsterIndex < 0 || monsterIndex >= monsters.Count) return;
        
        var monster = monsters[monsterIndex];
        if (!monster.IsAlive()) return;
        
        monster.currentHealth -= damage;
        
        // Fire events
        EventBus.Publish(new MonsterHealthChangedEvent 
        { 
            currentHealth = monster.currentHealth, 
            maxHealth = monster.maxHealth, 
            monsterIndex = monsterIndex 
        });
        
        EventBus.Publish(new SkillHitEvent 
        { 
            skill = skill, 
            damage = damage, 
            wasCritical = wasCrit, 
            targetIndex = monsterIndex 
        });
        
        // Check for death - let CombatManager handle full death logic
        if (monster.currentHealth <= 0)
        {
            combatService.HandleMonsterDeath(monsterIndex);
        }
    }
    
    // ==================== Active Effects ====================
    
    void UpdateActiveEffects()
    {
        // Update player buffs
        UpdateEffectList(playerBuffs, -1, true);
        
        // Update player debuffs
        UpdateEffectList(playerDebuffs, -1, false);
        
        // Update monster effects
        List<int> monsterKeys = new List<int>(monsterEffects.Keys);
        foreach (int monsterIndex in monsterKeys)
        {
            UpdateEffectList(monsterEffects[monsterIndex], monsterIndex, false);
        }
    }
    
    void UpdateEffectList(List<ActiveEffect> effects, int targetIndex, bool isPlayerBuff)
    {
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            ActiveEffect effect = effects[i];
            float dotDamage = effect.Update(Time.deltaTime);
            
            // Apply DoT damage
            if (dotDamage > 0)
            {
                if (targetIndex == -1 && !isPlayerBuff)
                {
                    // DoT on player - apply through combat manager to sync health and show damage
                    ApplyDotDamageToPlayer(dotDamage, effect);
                }
                else if (targetIndex >= 0)
                {
                    // DoT on monster
                    ApplyDamageToMonster(targetIndex, dotDamage, false, effect.sourceSkill);
                }
            }
            
            // Check expiration
            if (effect.IsExpired())
            {
                effects.RemoveAt(i);
                
                if (isPlayerBuff)
                {
                    EventBus.Publish(new BuffExpiredEvent 
                    { 
                        sourceSkill = effect.sourceSkill, 
                        wasOnPlayer = true, 
                        targetIndex = -1 
                    });
                }
                else
                {
                    EventBus.Publish(new DebuffExpiredEvent 
                    { 
                        sourceSkill = effect.sourceSkill, 
                        wasOnPlayer = targetIndex == -1, 
                        targetIndex = targetIndex 
                    });
                }
            }
        }
    }
    
    public void ApplyEffect(SkillData skill, int targetIndex, float casterAttackPower = 0f)
    {
        // Aura is required for buff/curse effects
        if (skill == null || skill.appliedAura == null)
        {
            Debug.LogWarning($"[SkillManager] Cannot apply effect - skill '{skill?.skillName}' has no aura assigned!");
            return;
        }
        
        float effectDuration = skill.appliedAura.duration;
        if (effectDuration <= 0f) return;
        
        ActiveEffect newEffect = new ActiveEffect(skill, targetIndex, casterAttackPower);
        
        if (skill.skillType == SkillType.Buff)
        {
            // Buff on player
            // Check for refresh or stacking
            ActiveEffect existing = playerBuffs.Find(e => e.IsFromSameSkill(skill));
            if (existing != null)
            {
                // Try to add stack, otherwise refresh
                if (!existing.AddStack())
                {
                    existing.Refresh();
                }
            }
            else
            {
                playerBuffs.Add(newEffect);
            }
            
            EventBus.Publish(new BuffAppliedEvent 
            { 
                sourceSkill = skill, 
                duration = effectDuration, 
                isOnPlayer = true, 
                targetIndex = -1 
            });
        }
        else if (skill.skillType == SkillType.Curse)
        {
            if (targetIndex == -1)
            {
                // Debuff on player
                ActiveEffect existing = playerDebuffs.Find(e => e.IsFromSameSkill(skill));
                if (existing != null)
                {
                    // Try to add stack, otherwise refresh
                    if (!existing.AddStack())
                    {
                        existing.Refresh();
                    }
                }
                else
                {
                    playerDebuffs.Add(newEffect);
                }
            }
            else
            {
                // Debuff on monster
                if (!monsterEffects.ContainsKey(targetIndex))
                {
                    monsterEffects[targetIndex] = new List<ActiveEffect>();
                }
                
                ActiveEffect existing = monsterEffects[targetIndex].Find(e => e.IsFromSameSkill(skill));
                if (existing != null)
                {
                    // Try to add stack, otherwise refresh
                    if (!existing.AddStack())
                    {
                        existing.Refresh();
                    }
                }
                else
                {
                    monsterEffects[targetIndex].Add(newEffect);
                }
            }
            
            EventBus.Publish(new DebuffAppliedEvent 
            { 
                sourceSkill = skill, 
                duration = effectDuration, 
                isOnPlayer = targetIndex == -1, 
                targetIndex = targetIndex 
            });
        }
    }
    
    public List<ActiveEffect> GetPlayerBuffs()
    {
        return new List<ActiveEffect>(playerBuffs);
    }
    
    public List<ActiveEffect> GetPlayerDebuffs()
    {
        return new List<ActiveEffect>(playerDebuffs);
    }
    
    public List<ActiveEffect> GetMonsterEffects(int monsterIndex)
    {
        if (monsterEffects.TryGetValue(monsterIndex, out var effects))
        {
            return new List<ActiveEffect>(effects);
        }
        return new List<ActiveEffect>();
    }
    
    public StatModifiers GetPlayerEffectModifiers()
    {
        StatModifiers total = new StatModifiers();
        
        // Combine all buff modifiers
        foreach (var buff in playerBuffs)
        {
            var mods = buff.GetStatModifiers();
            if (mods != null)
            {
                total.attackDamage += mods.attackDamage;
                total.attackSpeed += mods.attackSpeed;
                total.maxHealth += mods.maxHealth;
                total.armor += mods.armor;
                total.damageMultiplier += mods.damageMultiplier;
                total.damageTakenMultiplier += mods.damageTakenMultiplier;
                total.invulnerable |= mods.invulnerable;
                total.stunned |= mods.stunned;
            }
        }
        
        // Combine all debuff modifiers
        foreach (var debuff in playerDebuffs)
        {
            var mods = debuff.GetStatModifiers();
            if (mods != null)
            {
                total.attackDamage += mods.attackDamage;
                total.attackSpeed += mods.attackSpeed;
                total.maxHealth += mods.maxHealth;
                total.armor += mods.armor;
                total.damageMultiplier += mods.damageMultiplier;
                total.damageTakenMultiplier += mods.damageTakenMultiplier;
                total.slowPercent = Mathf.Max(total.slowPercent, mods.slowPercent);
                total.stunned |= mods.stunned;
            }
        }
        
        return total;
    }
    
    public bool IsPlayerInvulnerable()
    {
        foreach (var buff in playerBuffs)
        {
            if (buff.GrantsInvulnerability())
                return true;
        }
        return false;
    }
    
    public bool IsPlayerStunned()
    {
        foreach (var debuff in playerDebuffs)
        {
            if (debuff.IsStunned())
                return true;
        }
        return false;
    }
    
    public bool IsMonsterStunned(int monsterIndex)
    {
        if (monsterEffects.TryGetValue(monsterIndex, out var effects))
        {
            foreach (var effect in effects)
            {
                if (effect.IsStunned())
                    return true;
            }
        }
        return false;
    }
    
    public float GetMonsterSlowPercent(int monsterIndex)
    {
        float maxSlow = 0f;
        if (monsterEffects.TryGetValue(monsterIndex, out var effects))
        {
            foreach (var effect in effects)
            {
                maxSlow = Mathf.Max(maxSlow, effect.GetSlowPercent());
            }
        }
        return maxSlow;
    }
    
    public List<SkillData> GetAllAvailableSkills()
    {
        // Get the player's class and level
        string playerClassString = "";
        int playerLevel = 1;
        
        if (characterService != null)
        {
            playerClassString = characterService.GetCharacterClass();
            playerLevel = characterService.GetLevel();
        }
        else
        {
            characterService = Services.Get<ICharacterService>();
            if (characterService != null)
            {
                playerClassString = characterService.GetCharacterClass();
                playerLevel = characterService.GetLevel();
            }
        }
        
        CharacterClass playerClass = CharacterClassHelper.ParseFromString(playerClassString);
        
        // Filter skills by class and level
        List<SkillData> filteredSkills = new List<SkillData>();
        foreach (SkillData skill in availableSkills)
        {
            if (skill != null && 
                skill.CanBeUsedBy(playerClass) && 
                playerLevel >= skill.levelRequired)
            {
                filteredSkills.Add(skill);
            }
        }
        
        return filteredSkills;
    }
    
    public void ClearAllEffects()
    {
        playerBuffs.Clear();
        playerDebuffs.Clear();
        monsterEffects.Clear();
        skillCooldowns.Clear();
        globalCooldownTimer = 0f;
    }
    
    // ==================== Event Handlers ====================
    
    void OnCombatEnded(CombatEndedEvent e)
    {
        // Clear all effects when combat ends
        ClearAllEffects();
    }
    
    void OnMonsterDied(MonsterDiedEvent e)
    {
        // Remove effects on dead monster
        if (monsterEffects.ContainsKey(e.monsterIndex))
        {
            monsterEffects.Remove(e.monsterIndex);
        }
    }
    
    // ==================== Save/Load ====================
    
    /// <summary>
    /// Get action bar save data
    /// </summary>
    public string[] GetActionBarSaveData()
    {
        string[] saveData = new string[actionBarSkills.Length];
        for (int i = 0; i < actionBarSkills.Length; i++)
        {
            saveData[i] = actionBarSkills[i] != null ? actionBarSkills[i].name : "";
        }
        return saveData;
    }
    
    /// <summary>
    /// Load action bar from save data
    /// </summary>
    public void LoadActionBarData(string[] saveData)
    {
        if (saveData == null) return;
        
        for (int i = 0; i < saveData.Length && i < actionBarSkills.Length; i++)
        {
            if (!string.IsNullOrEmpty(saveData[i]))
            {
                SkillData skill = Resources.Load<SkillData>("Skills/" + saveData[i]);
                if (skill != null)
                {
                    actionBarSkills[i] = skill;
                    Debug.Log($"[SkillManager] Loaded skill '{skill.skillName}' to action bar slot {i}");
                }
                else
                {
                    Debug.LogWarning($"[SkillManager] Could not find skill asset: Skills/{saveData[i]}");
                }
            }
            else
            {
                actionBarSkills[i] = null;
            }
        }
        
        // Fire events to update UI
        for (int i = 0; i < actionBarSkills.Length; i++)
        {
            EventBus.Publish(new ActionBarChangedEvent 
            { 
                slotIndex = i, 
                skill = actionBarSkills[i] 
            });
        }
    }
}

