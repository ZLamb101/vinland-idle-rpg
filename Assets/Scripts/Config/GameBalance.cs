/// <summary>
/// Static helper for convenient access to game balance configuration.
/// Usage: GameBalance.Combat.playerBaseAttackDamage
/// </summary>
public static class GameBalance
{
    private static IConfigService ConfigService => Services.Get<IConfigService>();
    
    /// <summary>
    /// Combat balance values (damage, speed, health)
    /// </summary>
    public static CombatConfig Combat => ConfigService?.GetConfig().combat ?? new CombatConfig();
    
    /// <summary>
    /// Progression balance values (XP, leveling, stat scaling)
    /// </summary>
    public static ProgressionConfig Progression => ConfigService?.GetConfig().progression ?? new ProgressionConfig();
    
    /// <summary>
    /// Economy balance values (gold, costs, rewards)
    /// </summary>
    public static EconomyConfig Economy => ConfigService?.GetConfig().economy ?? new EconomyConfig();
    
    /// <summary>
    /// Drop balance values (loot chances, quantities)
    /// </summary>
    public static DropsConfig Drops => ConfigService?.GetConfig().drops ?? new DropsConfig();
    
    /// <summary>
    /// Get the full config object
    /// </summary>
    public static GameBalanceConfig GetFullConfig() => ConfigService?.GetConfig() ?? new GameBalanceConfig();
    
    /// <summary>
    /// Reload config from disk (hot-reload during development)
    /// </summary>
    public static void Reload() => ConfigService?.ReloadConfig();
}

