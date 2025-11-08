using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager for auto-battle combat system.
/// Handles combat state, attack timers, and damage calculation.
/// </summary>
public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }
    
    [Header("Combat State")]
    private CombatState currentState = CombatState.Idle;
    private MonsterData currentMonster;
    private MonsterData[] zoneMonsters;
    
    [Header("Combat Stats")]
    private float playerCurrentHealth;
    private float playerMaxHealth;
    private float playerBaseAttackDamage = 10f;
    private float playerBaseAttackSpeed = 1.5f; // Player attacks every 1.5 seconds
    private float playerAttackDamage; // Includes equipment bonuses
    private float playerAttackSpeed; // Includes equipment bonuses
    
    private float monsterCurrentHealth;
    private float monsterMaxHealth;
    private float monsterAttackDamage;
    private float monsterAttackSpeed;
    
    [Header("Attack Timers")]
    private float playerAttackTimer = 0f;
    private float monsterAttackTimer = 0f;
    
    [Header("Visual Combat")]
    public CombatVisualManager visualManager;
    private bool isMonsterAttackInProgress = false; // Track if monster attack animation is playing
    
    // Events for UI updates
    public event Action<CombatState> OnCombatStateChanged;
    public event Action<float, float> OnPlayerHealthChanged; // (current, max)
    public event Action<float, float> OnMonsterHealthChanged; // (current, max)
    public event Action<MonsterData> OnMonsterChanged;
    public event Action<float> OnPlayerAttackProgress; // 0 to 1
    public event Action<float> OnMonsterAttackProgress; // 0 to 1
    public event Action<float> OnPlayerDamageDealt; // Visual feedback
    public event Action<float> OnMonsterDamageDealt; // Visual feedback
    
    public enum CombatState
    {
        Idle,       // Not in combat
        Fighting,   // Active combat
        Defeat      // Player died
    }
    
    /// <summary>
    /// Helper struct for combat stats from equipment and talents
    /// </summary>
    private struct CombatStats
    {
        public float critChance;
        public float critDamage;
        public float lifesteal;
        public float dodge;
        public float armor;
        public float xpBonus;
        public float goldBonus;
    }
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
    public void StartCombat(MonsterData[] monsters)
    {
        Debug.Log($"CombatManager.StartCombat called with {monsters?.Length ?? 0} monsters");
        
        if (monsters == null || monsters.Length == 0)
        {
            Debug.LogWarning("No monsters to fight in this zone!");
            return;
        }
        
        zoneMonsters = monsters;
        
        // Initialize player stats with equipment bonuses
        CalculatePlayerStats();
        
        Debug.Log($"Starting combat - player health: {playerCurrentHealth}/{playerMaxHealth}, attack: {playerAttackDamage}");
        
        // Load first random monster (will log combat start in LoadRandomMonster)
        LoadRandomMonster();
    }
    
    /// <summary>
    /// Calculate player stats including equipment and talent bonuses
    /// </summary>
    void CalculatePlayerStats()
    {
        // Base stats from character
        if (CharacterManager.Instance != null)
        {
            playerMaxHealth = CharacterManager.Instance.GetMaxHealthWithTalents();
            playerCurrentHealth = CharacterManager.Instance.GetCurrentHealth();
        }
        else
        {
            playerMaxHealth = 100f;
            playerCurrentHealth = 100f;
        }
        
        // Start with base values
        playerAttackDamage = playerBaseAttackDamage;
        playerAttackSpeed = playerBaseAttackSpeed;
        
        // Add equipment bonuses
        if (EquipmentManager.Instance != null)
        {
            EquipmentStats equipStats = EquipmentManager.Instance.GetTotalStats();
            
            playerMaxHealth += equipStats.maxHealth;
            playerAttackDamage += equipStats.attackDamage;
            playerAttackSpeed += equipStats.attackSpeed;
        }
        
        // Add talent bonuses
        if (TalentManager.Instance != null)
        {
            TalentBonuses talents = TalentManager.Instance.GetTotalBonuses();
            
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
    
    void LoadRandomMonster()
    {
        if (zoneMonsters == null || zoneMonsters.Length == 0)
        {
            Debug.LogWarning("No monsters available in zone!");
            return;
        }
        
        // Randomly select a monster from the zone's available monsters
        int randomIndex = UnityEngine.Random.Range(0, zoneMonsters.Length);
        currentMonster = zoneMonsters[randomIndex];
        
        // Initialize monster stats (fixed, no scaling with player level)
        monsterMaxHealth = currentMonster.health;
        monsterCurrentHealth = monsterMaxHealth;
        monsterAttackDamage = currentMonster.attackDamage;
        monsterAttackSpeed = currentMonster.attackSpeed;
        
        // Initialize visual combat
        if (visualManager != null)
        {
            // Get hero sprite (if available)
            Sprite heroSprite = null;
            // TODO: Get hero sprite from CharacterManager or CharacterData if available
            
            visualManager.InitializeCombat(heroSprite, currentMonster);
        }
        
        // Log combat start when monster is loaded
        if (GameLog.Instance != null && currentMonster != null)
        {
            GameLog.Instance.AddCombatLogEntry($"Combat started against {currentMonster.monsterName}!", LogType.Info);
        }
        
        // Reset timers
        playerAttackTimer = 0f;
        monsterAttackTimer = 0f;
        isMonsterAttackInProgress = false;
        
        // Immediately update UI to show reset progress bars
        OnPlayerAttackProgress?.Invoke(0f);
        OnMonsterAttackProgress?.Invoke(0f);
        
        // Update state
        currentState = CombatState.Fighting;
        OnCombatStateChanged?.Invoke(currentState);
        OnMonsterChanged?.Invoke(currentMonster);
        OnPlayerHealthChanged?.Invoke(playerCurrentHealth, playerMaxHealth);
        OnMonsterHealthChanged?.Invoke(monsterCurrentHealth, monsterMaxHealth);
    }
    
    void UpdateCombat()
    {
        // Update player attack timer - player can attack immediately when enemy spawns
        playerAttackTimer += Time.deltaTime;
        OnPlayerAttackProgress?.Invoke(Mathf.Clamp01(playerAttackTimer / playerAttackSpeed));
        
        if (playerAttackTimer >= playerAttackSpeed)
        {
            PlayerAttack();
            playerAttackTimer = 0f;
        }
        
        // Update monster attack timer - but only attack if in attack range and not already attacking
        if (!isMonsterAttackInProgress)
        {
            // Check if enemy is in attack range (if visual combat is active)
            bool canAttack = true;
            if (visualManager != null)
            {
                canAttack = visualManager.IsEnemyInAttackRange();
            }
            
            if (canAttack)
            {
                monsterAttackTimer += Time.deltaTime;
                OnMonsterAttackProgress?.Invoke(Mathf.Clamp01(monsterAttackTimer / monsterAttackSpeed));
                
                if (monsterAttackTimer >= monsterAttackSpeed)
                {
                    MonsterAttack();
                    monsterAttackTimer = 0f;
                }
            }
        }
    }
    
    /// <summary>
    /// Get combined combat stats from equipment and talents
    /// </summary>
    CombatStats GetCombatStats()
    {
        CombatStats stats = new CombatStats
        {
            critChance = 0f,
            critDamage = 2f, // Base crit is 2x
            lifesteal = 0f,
            dodge = 0f,
            armor = 0f,
            xpBonus = 0f,
            goldBonus = 0f
        };
        
        // Add equipment bonuses
        if (EquipmentManager.Instance != null)
        {
            EquipmentStats equipStats = EquipmentManager.Instance.GetTotalStats();
            stats.critChance += equipStats.criticalChance;
            stats.lifesteal += equipStats.lifesteal;
            stats.dodge += equipStats.dodge;
            stats.armor += equipStats.armor;
            stats.xpBonus += equipStats.xpBonus;
            stats.goldBonus += equipStats.goldBonus;
        }
        
        // Add talent bonuses
        if (TalentManager.Instance != null)
        {
            TalentBonuses talents = TalentManager.Instance.GetTotalBonuses();
            stats.critChance += talents.criticalChance;
            stats.critDamage += talents.criticalDamage;
            stats.lifesteal += talents.lifesteal;
            stats.dodge += talents.dodge;
            stats.armor += talents.armor;
            stats.xpBonus += talents.xpBonus;
            stats.goldBonus += talents.goldBonus;
        }
        
        return stats;
    }
    
    void PlayerAttack()
    {
        float damage = playerAttackDamage;
        
        // Get combined stats from equipment and talents
        CombatStats stats = GetCombatStats();
        
        // Apply critical hit
        if (stats.critChance > 0 && UnityEngine.Random.value <= stats.critChance)
        {
            damage *= stats.critDamage; // Critical hit damage
        }
        
        // Visual combat: spawn projectile
        if (visualManager != null)
        {
            visualManager.HeroAttack(damage, (dealtDamage) => {
                ApplyPlayerDamage(dealtDamage, stats.lifesteal);
            });
        }
        else
        {
            // Fallback: apply damage immediately if no visual manager
            ApplyPlayerDamage(damage, stats.lifesteal);
        }
    }
    
    /// <summary>
    /// Apply damage dealt by player (called after projectile hits)
    /// </summary>
    void ApplyPlayerDamage(float damage, float totalLifesteal)
    {
        // Apply lifesteal
        if (totalLifesteal > 0)
        {
            float healAmount = damage * totalLifesteal;
            playerCurrentHealth = Mathf.Min(playerCurrentHealth + healAmount, playerMaxHealth);
            OnPlayerHealthChanged?.Invoke(playerCurrentHealth, playerMaxHealth);
        }
        
        monsterCurrentHealth -= damage;
        OnMonsterHealthChanged?.Invoke(monsterCurrentHealth, monsterMaxHealth);
        OnPlayerDamageDealt?.Invoke(damage);
        
        // Log combat message
        if (GameLog.Instance != null && currentMonster != null)
        {
            string critText = (damage > playerAttackDamage) ? " (Critical!)" : "";
            GameLog.Instance.AddCombatLogEntry($"You deal {damage:F0} damage to {currentMonster.monsterName}{critText}", LogType.Info);
        }
        
        if (monsterCurrentHealth <= 0)
        {
            // Capture enemy position at moment of death before any cleanup
            Vector2 deathPosition = Vector2.zero;
            if (visualManager != null && visualManager.CurrentEnemy != null)
            {
                deathPosition = visualManager.CurrentEnemy.GetPosition();
            }
            OnMonsterDefeated(deathPosition);
        }
    }
    
    void MonsterAttack()
    {
        // Visual combat: play attack animation first, then apply damage
        if (visualManager != null)
        {
            isMonsterAttackInProgress = true;
            visualManager.EnemyAttack(() => {
                // Attack animation complete, apply damage
                ApplyMonsterDamage();
                isMonsterAttackInProgress = false;
            });
        }
        else
        {
            // Fallback: apply damage immediately if no visual manager
            ApplyMonsterDamage();
        }
    }
    
    /// <summary>
    /// Apply damage dealt by monster (called after attack animation completes)
    /// </summary>
    void ApplyMonsterDamage()
    {
        float damage = monsterAttackDamage;
        
        // Get combined stats from equipment and talents
        CombatStats stats = GetCombatStats();
        
        // Apply dodge
        if (stats.dodge > 0 && UnityEngine.Random.value <= stats.dodge)
        {
            OnMonsterDamageDealt?.Invoke(0); // Show "MISS" or "DODGE"
            
            // Log dodge message
            if (GameLog.Instance != null && currentMonster != null)
            {
                GameLog.Instance.AddCombatLogEntry($"{currentMonster.monsterName} attacks, but you dodge!", LogType.Success);
            }
            
            return; // Attack dodged, no damage taken
        }
        
        // Apply armor damage reduction
        if (stats.armor > 0)
        {
            damage *= (1f - stats.armor); // Reduce damage by armor %
        }
        
        playerCurrentHealth -= damage;
        OnPlayerHealthChanged?.Invoke(playerCurrentHealth, playerMaxHealth);
        OnMonsterDamageDealt?.Invoke(damage);
        
        // Log combat message
        if (GameLog.Instance != null && currentMonster != null)
        {
            GameLog.Instance.AddCombatLogEntry($"{currentMonster.monsterName} hits you for {damage:F0} damage", LogType.Warning);
        }
        
        // Sync with CharacterManager
        if (CharacterManager.Instance != null)
        {
            float damageTaken = CharacterManager.Instance.GetCurrentHealth() - playerCurrentHealth;
            if (damageTaken > 0)
            {
                CharacterManager.Instance.TakeDamage(damageTaken);
            }
        }
        
        if (playerCurrentHealth <= 0)
        {
            OnPlayerDefeated();
        }
    }
    
    void OnMonsterDefeated(Vector2 deathPosition)
    {
        // Log victory message
        if (GameLog.Instance != null && currentMonster != null)
        {
            GameLog.Instance.AddCombatLogEntry($"You defeated {currentMonster.monsterName}!", LogType.Success);
        }
        
        // Give rewards with equipment bonuses
        if (CharacterManager.Instance != null && currentMonster != null)
        {
            int xpReward = currentMonster.xpReward;
            int goldReward = currentMonster.goldReward;
            
            // Get bonus stats
            CombatStats stats = GetCombatStats();
            
            xpReward = Mathf.RoundToInt(xpReward * (1f + stats.xpBonus));
            goldReward = Mathf.RoundToInt(goldReward * (1f + stats.goldBonus));
            
            CharacterManager.Instance.AddXP(xpReward);
            CharacterManager.Instance.AddGold(goldReward);
            
            // Process drop table - roll for each item independently
            List<MonsterDropEntry> droppedItems = new List<MonsterDropEntry>();
            if (currentMonster.dropTable != null && currentMonster.dropTable.Count > 0)
            {
                foreach (MonsterDropEntry dropEntry in currentMonster.dropTable)
                {
                    if (dropEntry.item != null && UnityEngine.Random.value <= dropEntry.dropChance)
                    {
                        // Add to dropped items list for visual effect
                        droppedItems.Add(dropEntry);
                        
                        // Create and add item to inventory
                        InventoryItem drop = dropEntry.item.CreateInventoryItem(dropEntry.quantity);
                        CharacterManager.Instance.AddItemToInventory(drop);
                    }
                }
            }
            
            // Show visual drop effect if items were dropped
            if (droppedItems.Count > 0 && visualManager != null)
            {
                visualManager.ShowItemDrops(droppedItems, deathPosition);
            }
        }
        
        // Load a new random monster immediately (combat continues forever)
        Invoke(nameof(LoadRandomMonster), 0.5f);
    }
    
    void OnPlayerDefeated()
    {
        // Log defeat message
        if (GameLog.Instance != null && currentMonster != null)
        {
            GameLog.Instance.AddCombatLogEntry($"You were defeated by {currentMonster.monsterName}!", LogType.Error);
        }
        
        currentState = CombatState.Defeat;
        OnCombatStateChanged?.Invoke(currentState);
        
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
            if (CharacterManager.Instance != null)
            {
                CharacterManager.Instance.HealToFull();
                playerCurrentHealth = CharacterManager.Instance.GetCurrentHealth();
                
                // Recalculate stats in case player leveled up
                CalculatePlayerStats();
            }
            
            // Load a new random monster and continue combat
            LoadRandomMonster();
        }
    }
    
    /// <summary>
    /// End combat and return to zone
    /// </summary>
    public void EndCombat()
    {
        currentState = CombatState.Idle;
        currentMonster = null;
        zoneMonsters = null;
        isMonsterAttackInProgress = false;
        
        // Clean up visual combat
        if (visualManager != null)
        {
            visualManager.Cleanup();
        }
        
        OnCombatStateChanged?.Invoke(currentState);
    }
    
    // Getters
    public CombatState GetCombatState() => currentState;
    public MonsterData GetCurrentMonster() => currentMonster;
    public float GetPlayerCurrentHealth() => playerCurrentHealth;
    public float GetPlayerMaxHealth() => playerMaxHealth;
    public float GetMonsterCurrentHealth() => monsterCurrentHealth;
    public float GetMonsterMaxHealth() => monsterMaxHealth;
}

