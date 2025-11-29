using System;
using System.Collections.Generic;

/// <summary>
/// Complete save data structure for the account.
/// Shared across all characters.
/// </summary>
[System.Serializable]
public class AccountSaveData
{
    // Version for migration support
    public int version = 1;
    
    // Shared Bank (account-wide)
    public BankData bankData = new BankData();
    
    // Shared Crafting (account-wide - all characters share unlocked recipes)
    public CraftingData craftingData = new CraftingData();
    
    // Settings (account-wide - same settings for all characters)
    public SettingsData settingsData = new SettingsData();
    
    // Metadata
    public string saveTime = "";
    
    /// <summary>
    /// Create default account data
    /// </summary>
    public static AccountSaveData CreateDefault()
    {
        AccountSaveData data = new AccountSaveData();
        data.saveTime = DateTime.Now.Ticks.ToString();
        return data;
    }
}
