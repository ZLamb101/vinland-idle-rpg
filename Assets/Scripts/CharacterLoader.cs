using UnityEngine;

/// <summary>
/// Loads the active character data into CharacterManager when entering the game scene.
/// Place this in your main game scene.
/// </summary>
public class CharacterLoader : MonoBehaviour
{
    [Header("Settings")]
    public bool loadOnStart = true;
    
    private string currentRace;
    private string currentClass;
    private int currentSlotIndex;
    
    void Start()
    {
        if (loadOnStart)
        {
            LoadActiveCharacter();
        }
    }
    
    // Removed auto-save on level up - will save only when returning to character screen
    
    public void LoadActiveCharacter()
    {
        // Check if there's an active character to load
        if (!PlayerPrefs.HasKey("ActiveCharacter"))
        {
            Debug.LogWarning("No active character found! Create a character first.");
            return;
        }
        
        // Load character data
        string json = PlayerPrefs.GetString("ActiveCharacter");
        SavedCharacterData savedData = JsonUtility.FromJson<SavedCharacterData>(json);
        
        currentRace = PlayerPrefs.GetString("ActiveCharacterRace", "Human");
        currentClass = PlayerPrefs.GetString("ActiveCharacterClass", "Warrior");
        currentSlotIndex = PlayerPrefs.GetInt("ActiveCharacterSlot", 0);
        
        // Load into CharacterManager
        if (CharacterManager.Instance != null)
        {
            CharacterData gameData = new CharacterData();
            savedData.LoadInto(gameData);
            
            CharacterManager.Instance.LoadCharacterData(gameData);
            
            Debug.Log($"✅ Loaded character: {savedData.characterName} - Level {savedData.level} {currentRace} {currentClass}");
        }
        else
        {
            Debug.LogError("CharacterManager not found in scene!");
        }
    }
    
    // Call this before returning to character select to save progress
    void OnApplicationQuit()
    {
        SaveCurrentCharacter();
    }
    
    void OnDestroy()
    {
        // Save when leaving the scene
        SaveCurrentCharacter();
    }
    
    public void SaveCurrentCharacter()
    {
        if (CharacterManager.Instance == null)
        {
            // This is normal when returning to character select - CharacterManager doesn't exist there
            return;
        }
        
        // Get current character data from CharacterManager
        CharacterData currentData = CharacterManager.Instance.GetCharacterData();
        
        // Create saved character data
        SavedCharacterData savedData = new SavedCharacterData();
        savedData.SaveFrom(currentData, currentRace, currentClass);
        
        // Save back to PlayerPrefs
        string key = $"Character_{currentSlotIndex}";
        string json = JsonUtility.ToJson(savedData);
        PlayerPrefs.SetString(key, json);
        
        // Also update active character
        PlayerPrefs.SetString("ActiveCharacter", json);
        PlayerPrefs.Save();
        
        Debug.Log($"💾 Saved to {key}: {savedData.characterName} - Level {savedData.level} ({savedData.currentXP} XP, {savedData.gold} Gold)");
    }
    
    public string GetCurrentRace() => currentRace;
    public string GetCurrentClass() => currentClass;
}

