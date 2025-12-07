using System;

/// <summary>
/// Root configuration for all game balance values.
/// Loaded from JSON at runtime, editable in any text editor.
/// </summary>
[Serializable]
public class GameBalanceConfig
{
    public CombatConfig combat = new CombatConfig();
    public ProgressionConfig progression = new ProgressionConfig();
    public EconomyConfig economy = new EconomyConfig();
    public DropsConfig drops = new DropsConfig();
}

/// <summary>
/// Combat-related balance values
/// </summary>
[Serializable]
public class CombatConfig
{
    public float playerBaseAttackDamage = 10f;
    public float playerBaseAttackSpeed = 2.0f;
    public float playerStartingHealth = 100f;
    public float playerBaseCritDamage = 2f; // Base critical hit multiplier (2x = 200%)
    public float baseMissChance = 0.01f; // Base miss chance for all attacks (1% = 0.01)
    public float monsterRespawnDelay = 0.5f;
    
    // Monster base stats (shared by all monsters)
    public float monsterBaseHealth = 54f;
    public float monsterBaseAttackDamage = 10f;
    public float monsterBaseAttackSpeed = 2.0f;
    public float monsterBaseArmor = 0.2f; // 20% damage reduction (doesn't scale with level)
    public int monsterBaseXPReward = 10;
    public int monsterBaseGoldReward = 10;
    public float monsterMeleeRange = 120f;
    public float monsterRangedRange = 600f;
    public float monsterLevelScalingMultiplier = 0.15f; // 15% per level above minimum
    public float baseAfkGainsPercent = 0.25f; // Base AFK gains percentage (0.25 = 25% of normal gains)
    public float skillGlobalCooldown = 1.0f; // Global cooldown between spell casts (in seconds)
}

/// <summary>
/// Character progression and leveling balance values
/// </summary>
[Serializable]
public class ProgressionConfig
{
    public int baseXPPerLevel = 100;
    public float xpScalingPerLevel = 1.5f;
    public float healthPerLevel = 20f;
    public float attackDamagePerLevel = 2f;
    public float critChancePerLevel = 0.01f;
}

/// <summary>
/// Economy and currency balance values
/// </summary>
[Serializable]
public class EconomyConfig
{
    public int startingGold = 0;
    public int baseGoldReward = 10;
    public float goldScalingPerLevel = 1.2f;
    public int talentResetCost = 100;
}

/// <summary>
/// Item drop and loot balance values
/// </summary>
[Serializable]
public class DropsConfig
{
    public float baseDropChance = 0.25f;
    public int minDropQuantity = 1;
    public int maxDropQuantity = 3;
}

