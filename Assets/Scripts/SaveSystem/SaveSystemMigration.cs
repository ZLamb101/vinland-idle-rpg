using System;
using UnityEngine;

/// <summary>
/// Handles migration from old PlayerPrefs-based save system to new JSON-based SaveSystem
/// Can be run once to convert existing player saves
/// </summary>
public static class SaveSystemMigration
{
    private const string MIGRATION_COMPLETE_KEY = "SaveSystemMigration_Completed";
    
    /// <summary>
    /// Check if a character slot needs migration from PlayerPrefs to JSON
    /// </summary>
    public static bool NeedsMigration(int characterSlot)
    {
        // If JSON save already exists, no migration needed
        if (SaveSystem.SaveFileExists(characterSlot))
        {
            return false;
        }
        
        // Check if old PlayerPrefs save exists
        string oldKey = $"Character_{characterSlot}";
        return PlayerPrefs.HasKey(oldKey);
    }
    
    /// <summary>
    /// Migrate a single character from PlayerPrefs to JSON SaveSystem
    /// </summary>
    public static bool MigrateCharacter(int characterSlot)
    {
        if (!NeedsMigration(characterSlot))
        {
            Debug.Log($"[SaveSystemMigration] Character slot {characterSlot} doesn't need migration");
            return false;
        }
        
        Debug.Log($"[SaveSystemMigration] Starting migration for character slot {characterSlot}");
        
        try
        {
            // Create SaveData from PlayerPrefs
            SaveData migratedData = MigrateFromPlayerPrefs(characterSlot);
            
            if (migratedData == null)
            {
                Debug.LogError($"[SaveSystemMigration] Failed to create SaveData from PlayerPrefs for slot {characterSlot}");
                return false;
            }
            
            // Save to new JSON system
            bool success = SaveSystem.SaveCharacter(characterSlot, migratedData);
            
            if (success)
            {
                Debug.Log($"[SaveSystemMigration] Successfully migrated character slot {characterSlot} to JSON");
                
                // Don't delete old PlayerPrefs yet - keep as backup
                // User can manually delete after verifying migration worked
                return true;
            }
            else
            {
                Debug.LogError($"[SaveSystemMigration] Failed to save migrated data for slot {characterSlot}");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystemMigration] Exception during migration of slot {characterSlot}: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Migrate all characters from PlayerPrefs to JSON
    /// </summary>
    public static void MigrateAllCharacters()
    {
        if (PlayerPrefs.GetInt(MIGRATION_COMPLETE_KEY, 0) == 1)
        {
            Debug.Log("[SaveSystemMigration] Migration already completed previously");
            return;
        }
        
        Debug.Log("[SaveSystemMigration] Starting migration of all characters");
        
        int successCount = 0;
        int failCount = 0;
        
        // Try to migrate up to 10 character slots (adjust if you have more)
        for (int slot = 0; slot < 10; slot++)
        {
            if (NeedsMigration(slot))
            {
                if (MigrateCharacter(slot))
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }
            }
        }
        
        Debug.Log($"[SaveSystemMigration] Migration complete. Success: {successCount}, Failed: {failCount}");
        
        if (failCount == 0)
        {
            // Mark migration as complete
            PlayerPrefs.SetInt(MIGRATION_COMPLETE_KEY, 1);
            PlayerPrefs.Save();
        }
    }
    
    /// <summary>
    /// Create SaveData from old PlayerPrefs format
    /// </summary>
    private static SaveData MigrateFromPlayerPrefs(int characterSlot)
    {
        string oldKey = $"Character_{characterSlot}";
        if (!PlayerPrefs.HasKey(oldKey))
        {
            return null;
        }
        
        SaveData data = new SaveData();
        
        try
        {
            // Load old character data
            string oldJson = PlayerPrefs.GetString(oldKey, "");
            if (string.IsNullOrEmpty(oldJson))
            {
                return null;
            }
            
            // Try to deserialize old format
            // Note: This assumes the old format was also JSON with CharacterData
            CharacterData oldCharData = JsonUtility.FromJson<CharacterData>(oldJson);
            
            if (oldCharData != null)
            {
                // Map old data to new SaveData format
                data.characterName = oldCharData.characterName;
                data.race = oldCharData.race;
                data.characterClass = oldCharData.characterClass;
                data.level = oldCharData.level;
                data.currentXP = oldCharData.currentXP;
                data.gold = oldCharData.gold;
                data.currentHealth = oldCharData.currentHealth;
                
                // Inventory
                if (oldCharData.inventory != null && oldCharData.inventory.items != null)
                {
                    data.inventoryItems = oldCharData.inventory.items;
                }
            }
            
            // Migrate zone data
            string zoneKey = $"Character_{characterSlot}_CurrentZone";
            if (PlayerPrefs.HasKey(zoneKey))
            {
                data.currentZoneIndex = PlayerPrefs.GetInt(zoneKey, 0);
            }
            
            // Migrate away activity data
            string activityKey = $"AwayActivity_Slot_{characterSlot}_Type";
            if (PlayerPrefs.HasKey(activityKey))
            {
                data.awayActivityType = PlayerPrefs.GetInt(activityKey, 0);
            }
            
            string activityStartKey = $"AwayActivity_Slot_{characterSlot}_StartTime";
            if (PlayerPrefs.HasKey(activityStartKey))
            {
                data.awayActivityStartTime = PlayerPrefs.GetString(activityStartKey, "");
            }
            
            // Add more migration logic here as needed for other PlayerPrefs keys
            
            data.saveTime = DateTime.Now.Ticks.ToString();
            data.version = 1;
            
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystemMigration] Error parsing PlayerPrefs data for slot {characterSlot}: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Delete old PlayerPrefs data for a character slot
    /// CAUTION: Only call this after verifying the migration was successful!
    /// </summary>
    public static void DeleteOldPlayerPrefsData(int characterSlot)
    {
        string oldKey = $"Character_{characterSlot}";
        
        if (PlayerPrefs.HasKey(oldKey))
        {
            PlayerPrefs.DeleteKey(oldKey);
            
            // Delete other related keys
            PlayerPrefs.DeleteKey($"Character_{characterSlot}_CurrentZone");
            PlayerPrefs.DeleteKey($"AwayActivity_Slot_{characterSlot}_Type");
            PlayerPrefs.DeleteKey($"AwayActivity_Slot_{characterSlot}_StartTime");
            PlayerPrefs.DeleteKey($"AwayActivity_Slot_{characterSlot}_ResourceName");
            PlayerPrefs.DeleteKey($"AwayActivity_Slot_{characterSlot}_MonsterNames");
            PlayerPrefs.DeleteKey($"AwayActivity_Slot_{characterSlot}_MonsterDisplayNames");
            PlayerPrefs.DeleteKey($"AwayActivity_Slot_{characterSlot}_MobCount");
            PlayerPrefs.DeleteKey($"LastPlayedTime_{characterSlot}");
            PlayerPrefs.DeleteKey($"LastSessionStart_{characterSlot}");
            
            PlayerPrefs.Save();
            
            Debug.Log($"[SaveSystemMigration] Deleted old PlayerPrefs data for slot {characterSlot}");
        }
    }
    
    /// <summary>
    /// Check if migration has been completed
    /// </summary>
    public static bool HasMigrationCompleted()
    {
        return PlayerPrefs.GetInt(MIGRATION_COMPLETE_KEY, 0) == 1;
    }
    
    /// <summary>
    /// Reset migration flag (for testing)
    /// </summary>
    public static void ResetMigrationFlag()
    {
        PlayerPrefs.DeleteKey(MIGRATION_COMPLETE_KEY);
        PlayerPrefs.Save();
        Debug.Log("[SaveSystemMigration] Migration flag reset");
    }
}

/*
 * USAGE GUIDE:
 * 
 * 1. AUTOMATIC MIGRATION (Recommended):
 *    Add this to your game initialization:
 *    
 *    void Start()
 *    {
 *        if (!SaveSystemMigration.HasMigrationCompleted())
 *        {
 *            SaveSystemMigration.MigrateAllCharacters();
 *        }
 *    }
 * 
 * 2. MANUAL MIGRATION:
 *    For each character slot:
 *    
 *    if (SaveSystemMigration.NeedsMigration(0))
 *    {
 *        SaveSystemMigration.MigrateCharacter(0);
 *    }
 * 
 * 3. VERIFY MIGRATION:
 *    Load the game and verify all character data is correct
 *    Check the JSON files in Application.persistentDataPath/Saves/
 * 
 * 4. CLEANUP (Optional):
 *    After verifying migration worked, you can delete old PlayerPrefs:
 *    
 *    SaveSystemMigration.DeleteOldPlayerPrefsData(0);
 * 
 * NOTES:
 * - Old PlayerPrefs data is NOT automatically deleted (kept as backup)
 * - Migration is idempotent (safe to run multiple times)
 * - If JSON file exists, migration is skipped for that slot
 * - Migration flag prevents running multiple times automatically
 */

