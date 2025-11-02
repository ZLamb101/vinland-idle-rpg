using System;
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
        Victory,    // Player won
        Defeat      // Player died
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
        
        // Load first random monster
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
        
        // Reset timers
        playerAttackTimer = 0f;
        monsterAttackTimer = 0f;
        
        // Update state
        currentState = CombatState.Fighting;
        OnCombatStateChanged?.Invoke(currentState);
        OnMonsterChanged?.Invoke(currentMonster);
        OnPlayerHealthChanged?.Invoke(playerCurrentHealth, playerMaxHealth);
        OnMonsterHealthChanged?.Invoke(monsterCurrentHealth, monsterMaxHealth);
    }
    
    void UpdateCombat()
    {
        // Update player attack timer
        playerAttackTimer += Time.deltaTime;
        OnPlayerAttackProgress?.Invoke(Mathf.Clamp01(playerAttackTimer / playerAttackSpeed));
        
        if (playerAttackTimer >= playerAttackSpeed)
        {
            PlayerAttack();
            playerAttackTimer = 0f;
        }
        
        // Update monster attack timer
        monsterAttackTimer += Time.deltaTime;
        OnMonsterAttackProgress?.Invoke(Mathf.Clamp01(monsterAttackTimer / monsterAttackSpeed));
        
        if (monsterAttackTimer >= monsterAttackSpeed)
        {
            MonsterAttack();
            monsterAttackTimer = 0f;
        }
    }
    
    void PlayerAttack()
    {
        float damage = playerAttackDamage;
        
        // Get combined stats from equipment and talents
        float totalCritChance = 0f;
        float totalCritDamage = 2f; // Base crit is 2x
        float totalLifesteal = 0f;
        
        // Add equipment bonuses
        if (EquipmentManager.Instance != null)
        {
            EquipmentStats stats = EquipmentManager.Instance.GetTotalStats();
            totalCritChance += stats.criticalChance;
            totalLifesteal += stats.lifesteal;
        }
        
        // Add talent bonuses
        if (TalentManager.Instance != null)
        {
            TalentBonuses talents = TalentManager.Instance.GetTotalBonuses();
            totalCritChance += talents.criticalChance;
            totalCritDamage += talents.criticalDamage;
            totalLifesteal += talents.lifesteal;
        }
        
        // Apply critical hit
        if (totalCritChance > 0 && UnityEngine.Random.value <= totalCritChance)
        {
            damage *= totalCritDamage; // Critical hit damage
        }
        
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
        
        if (monsterCurrentHealth <= 0)
        {
            OnMonsterDefeated();
        }
    }
    
    void MonsterAttack()
    {
        float damage = monsterAttackDamage;
        
        // Get combined stats from equipment and talents
        float totalDodge = 0f;
        float totalArmor = 0f;
        
        // Add equipment bonuses
        if (EquipmentManager.Instance != null)
        {
            EquipmentStats stats = EquipmentManager.Instance.GetTotalStats();
            totalDodge += stats.dodge;
            totalArmor += stats.armor;
        }
        
        // Add talent bonuses
        if (TalentManager.Instance != null)
        {
            TalentBonuses talents = TalentManager.Instance.GetTotalBonuses();
            totalDodge += talents.dodge;
            totalArmor += talents.armor;
        }
        
        // Apply dodge
        if (totalDodge > 0 && UnityEngine.Random.value <= totalDodge)
        {
            OnMonsterDamageDealt?.Invoke(0); // Show "MISS" or "DODGE"
            return; // Attack dodged, no damage taken
        }
        
        // Apply armor damage reduction
        if (totalArmor > 0)
        {
            damage *= (1f - totalArmor); // Reduce damage by armor %
        }
        
        playerCurrentHealth -= damage;
        OnPlayerHealthChanged?.Invoke(playerCurrentHealth, playerMaxHealth);
        OnMonsterDamageDealt?.Invoke(damage);
        
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
    
    void OnMonsterDefeated()
    {
        // Give rewards with equipment bonuses
        if (CharacterManager.Instance != null && currentMonster != null)
        {
            int xpReward = currentMonster.xpReward;
            int goldReward = currentMonster.goldReward;
            
            // Apply equipment bonuses
            float totalXPBonus = 0f;
            float totalGoldBonus = 0f;
            
            if (EquipmentManager.Instance != null)
            {
                EquipmentStats stats = EquipmentManager.Instance.GetTotalStats();
                totalXPBonus += stats.xpBonus;
                totalGoldBonus += stats.goldBonus;
            }
            
            // Add talent bonuses
            if (TalentManager.Instance != null)
            {
                TalentBonuses talents = TalentManager.Instance.GetTotalBonuses();
                totalXPBonus += talents.xpBonus;
                totalGoldBonus += talents.goldBonus;
            }
            
            xpReward = Mathf.RoundToInt(xpReward * (1f + totalXPBonus));
            goldReward = Mathf.RoundToInt(goldReward * (1f + totalGoldBonus));
            
            CharacterManager.Instance.AddXP(xpReward);
            CharacterManager.Instance.AddGold(goldReward);
            
            // Process drop table - roll for each item independently
            if (currentMonster.dropTable != null && currentMonster.dropTable.Count > 0)
            {
                foreach (MonsterDropEntry dropEntry in currentMonster.dropTable)
                {
                    if (dropEntry.item != null && UnityEngine.Random.value <= dropEntry.dropChance)
                    {
                        InventoryItem drop = dropEntry.item.CreateInventoryItem(dropEntry.quantity);
                        CharacterManager.Instance.AddItemToInventory(drop);
                    }
                }
            }
        }
        
        // Load a new random monster immediately (combat continues forever)
        Invoke(nameof(LoadRandomMonster), 0.5f);
    }
    
    void OnPlayerDefeated()
    {
        currentState = CombatState.Defeat;
        OnCombatStateChanged?.Invoke(currentState);
        
        // Respawn player with full health
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.HealToFull();
            playerCurrentHealth = CharacterManager.Instance.GetCurrentHealth();
            
            // Recalculate stats in case player leveled up
            CalculatePlayerStats();
        }
        
        // Combat is paused - player must click Continue to resume
    }
    
    /// <summary>
    /// Resume combat after defeat - called by UI Continue button
    /// </summary>
    public void ResumeAfterDefeat()
    {
        if (currentState == CombatState.Defeat)
        {
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

