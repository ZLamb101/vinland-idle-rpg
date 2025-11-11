using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Centralized save system using JSON files
/// Replaces scattered PlayerPrefs usage
/// </summary>
public static class SaveSystem
{
    private const string SAVE_FOLDER = "Saves";
    private const string SAVE_EXTENSION = ".json";
    private const int CURRENT_VERSION = 1;
    
    /// <summary>
    /// Get the full path to the save folder
    /// </summary>
    public static string GetSaveFolderPath()
    {
        string path = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
        
        // Create folder if it doesn't exist
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Debug.Log($"[SaveSystem] Created save folder at: {path}");
        }
        
        return path;
    }
    
    /// <summary>
    /// Get the full path to a character's save file
    /// </summary>
    public static string GetSaveFilePath(int characterSlot)
    {
        return Path.Combine(GetSaveFolderPath(), $"Character_{characterSlot}{SAVE_EXTENSION}");
    }
    
    /// <summary>
    /// Save character data to file
    /// </summary>
    public static bool SaveCharacter(int characterSlot, SaveData data)
    {
        try
        {
            string path = GetSaveFilePath(characterSlot);
            
            // Ensure version is set
            data.version = CURRENT_VERSION;
            data.saveTime = DateTime.Now.Ticks.ToString();
            
            // Serialize to JSON
            string json = JsonUtility.ToJson(data, true);
            
            // Write to file
            File.WriteAllText(path, json);
            
            Debug.Log($"[SaveSystem] Saved character {characterSlot} to: {path}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to save character {characterSlot}: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Save current game state
    /// </summary>
    public static bool SaveCurrentCharacter(int characterSlot)
    {
        SaveData data = SaveData.CreateFromCurrentState();
        return SaveCharacter(characterSlot, data);
    }
    
    /// <summary>
    /// Load character data from file
    /// </summary>
    public static SaveData LoadCharacter(int characterSlot)
    {
        try
        {
            string path = GetSaveFilePath(characterSlot);
            
            if (!File.Exists(path))
            {
                Debug.Log($"[SaveSystem] No save file found for character {characterSlot}");
                return null;
            }
            
            // Read from file
            string json = File.ReadAllText(path);
            
            // Deserialize from JSON
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            
            // Validate
            if (data == null)
            {
                Debug.LogError($"[SaveSystem] Failed to deserialize save file for character {characterSlot}");
                return null;
            }
            
            // Check version for migration
            if (data.version < CURRENT_VERSION)
            {
                Debug.LogWarning($"[SaveSystem] Save file version {data.version} is older than current version {CURRENT_VERSION}. Migration may be needed.");
                data = MigrateSaveData(data, data.version, CURRENT_VERSION);
            }
            
            Debug.Log($"[SaveSystem] Loaded character {characterSlot} from: {path}");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to load character {characterSlot}: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Check if a save file exists for a character slot
    /// </summary>
    public static bool SaveFileExists(int characterSlot)
    {
        string path = GetSaveFilePath(characterSlot);
        return File.Exists(path);
    }
    
    /// <summary>
    /// Delete a character's save file
    /// </summary>
    public static bool DeleteCharacter(int characterSlot)
    {
        try
        {
            string path = GetSaveFilePath(characterSlot);
            
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[SaveSystem] Deleted character {characterSlot} save file");
                return true;
            }
            
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to delete character {characterSlot}: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Get all character slots that have save files
    /// </summary>
    public static int[] GetSavedCharacterSlots()
    {
        try
        {
            string folder = GetSaveFolderPath();
            string[] files = Directory.GetFiles(folder, $"Character_*{SAVE_EXTENSION}");
            
            int[] slots = new int[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                string fileName = Path.GetFileNameWithoutExtension(files[i]);
                string slotStr = fileName.Replace("Character_", "");
                if (int.TryParse(slotStr, out int slot))
                {
                    slots[i] = slot;
                }
            }
            
            return slots;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to get saved character slots: {e.Message}");
            return new int[0];
        }
    }
    
    /// <summary>
    /// Create a backup of a save file
    /// </summary>
    public static bool BackupSaveFile(int characterSlot)
    {
        try
        {
            string sourcePath = GetSaveFilePath(characterSlot);
            
            if (!File.Exists(sourcePath))
            {
                return false;
            }
            
            string backupPath = Path.Combine(
                GetSaveFolderPath(),
                $"Character_{characterSlot}_backup_{DateTime.Now:yyyyMMdd_HHmmss}{SAVE_EXTENSION}"
            );
            
            File.Copy(sourcePath, backupPath);
            Debug.Log($"[SaveSystem] Created backup at: {backupPath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to backup character {characterSlot}: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Migrate save data from old version to new version
    /// </summary>
    private static SaveData MigrateSaveData(SaveData data, int fromVersion, int toVersion)
    {
        Debug.Log($"[SaveSystem] Migrating save data from version {fromVersion} to {toVersion}");
        
        // Add migration logic here as needed when version changes
        // For now, just update the version number
        data.version = toVersion;
        
        return data;
    }
    
    /// <summary>
    /// Get basic info about a save file without fully loading it
    /// </summary>
    public static SaveFileInfo GetSaveFileInfo(int characterSlot)
    {
        try
        {
            string path = GetSaveFilePath(characterSlot);
            
            if (!File.Exists(path))
            {
                return null;
            }
            
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            
            if (data == null)
            {
                return null;
            }
            
            return new SaveFileInfo
            {
                characterName = data.characterName,
                level = data.level,
                race = data.race,
                characterClass = data.characterClass,
                saveTime = long.TryParse(data.saveTime, out long ticks) 
                    ? new DateTime(ticks) 
                    : DateTime.MinValue
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to get save file info for character {characterSlot}: {e.Message}");
            return null;
        }
    }
}

/// <summary>
/// Basic info about a save file
/// </summary>
public class SaveFileInfo
{
    public string characterName;
    public int level;
    public string race;
    public string characterClass;
    public DateTime saveTime;
}

