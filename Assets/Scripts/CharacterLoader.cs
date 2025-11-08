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
            // Use coroutine to ensure CharacterManager has time to initialize
            StartCoroutine(LoadCharacterAfterDelay());
        }
    }
    
    System.Collections.IEnumerator LoadCharacterAfterDelay()
    {
        // Wait one frame to ensure all Awake() methods have run
        yield return null;
        
        LoadActiveCharacter();
    }
    
    // Removed auto-save on level up - will save only when returning to character screen
    
    void EnsureCharacterManagerExists()
    {
        // Check if CharacterManager already exists
        if (CharacterManager.Instance != null)
        {
            return;
        }
        
        // Try to find it in the scene first
        CharacterManager existingManager = FindObjectOfType<CharacterManager>();
        if (existingManager != null)
        {
            // It exists but Instance might not be set yet (Awake hasn't run)
            return;
        }
        
        // Create CharacterManager if it doesn't exist (we're in a game scene)
        GameObject managerObj = new GameObject("CharacterManager");
        CharacterManager manager = managerObj.AddComponent<CharacterManager>();
        
        // Give it a moment to initialize
        // The Instance will be set in Awake(), which should run immediately after AddComponent
    }
    
    public void LoadActiveCharacter()
    {
        // Check if there's an active character to load
        if (!PlayerPrefs.HasKey("ActiveCharacter"))
        {
            // This is normal on the character selection screen - no character selected yet
            return;
        }
        
        // Get slot index FIRST before loading (important for saving later)
        currentSlotIndex = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        
        // Check if we're on the character selection screen - if so, don't load here
        CharacterSelectionManager charSelectManager = FindObjectOfType<CharacterSelectionManager>();
        if (charSelectManager != null)
        {
            return;
        }
        
        // We're in a game scene - ensure CharacterManager exists
        EnsureCharacterManagerExists();
        
        // Wait a moment for CharacterManager to initialize if it was just created
        if (CharacterManager.Instance == null)
        {
            StartCoroutine(RetryLoadAfterDelay());
            return;
        }
        
        // Load character data
        string json = PlayerPrefs.GetString("ActiveCharacter");
        SavedCharacterData savedData = JsonUtility.FromJson<SavedCharacterData>(json);
        
        currentRace = PlayerPrefs.GetString("ActiveCharacterRace", "Human");
        currentClass = PlayerPrefs.GetString("ActiveCharacterClass", "Warrior");
        
        // Load into CharacterManager
        CharacterData gameData = new CharacterData();
        savedData.LoadInto(gameData);
        
        // Ensure race/class are set (use saved data, fallback to PlayerPrefs)
        if (string.IsNullOrEmpty(gameData.race))
        {
            gameData.race = !string.IsNullOrEmpty(savedData.race) ? savedData.race : currentRace;
        }
        if (string.IsNullOrEmpty(gameData.characterClass))
        {
            gameData.characterClass = !string.IsNullOrEmpty(savedData.characterClass) ? savedData.characterClass : currentClass;
        }
        
        // Update currentRace/currentClass from loaded data for saving later
        currentRace = gameData.race;
        currentClass = gameData.characterClass;
        
        // Ensure name is not empty or default - use saved name directly
        if (string.IsNullOrEmpty(gameData.characterName))
        {
            if (!string.IsNullOrEmpty(savedData.characterName))
            {
                gameData.characterName = savedData.characterName;
            }
        }
        
        CharacterManager.Instance.LoadCharacterData(gameData);
        
        // Verify name was set correctly
        string finalName = CharacterManager.Instance.GetName();
        
        // Always ensure the name matches what was saved
        if (!string.IsNullOrEmpty(savedData.characterName) && finalName != savedData.characterName)
        {
            CharacterManager.Instance.SetName(savedData.characterName);
        }
    }
    
    System.Collections.IEnumerator RetryLoadAfterDelay()
    {
        // Wait a bit longer for CharacterManager to initialize
        yield return new WaitForSeconds(0.2f);
        
        // Double-check we're not on character selection screen
        CharacterSelectionManager charSelectManager = FindObjectOfType<CharacterSelectionManager>();
        if (charSelectManager != null)
        {
            yield break;
        }
        
        if (CharacterManager.Instance != null)
        {
            // Retry loading
            string json = PlayerPrefs.GetString("ActiveCharacter");
            SavedCharacterData savedData = JsonUtility.FromJson<SavedCharacterData>(json);
            
            currentRace = PlayerPrefs.GetString("ActiveCharacterRace", "Human");
            currentClass = PlayerPrefs.GetString("ActiveCharacterClass", "Warrior");
            currentSlotIndex = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
            
            CharacterData gameData = new CharacterData();
            savedData.LoadInto(gameData);
            
            // Ensure race/class are set (use saved data, fallback to PlayerPrefs)
            if (string.IsNullOrEmpty(gameData.race))
            {
                gameData.race = !string.IsNullOrEmpty(savedData.race) ? savedData.race : currentRace;
            }
            if (string.IsNullOrEmpty(gameData.characterClass))
            {
                gameData.characterClass = !string.IsNullOrEmpty(savedData.characterClass) ? savedData.characterClass : currentClass;
            }
            
            // Update currentRace/currentClass from loaded data
            currentRace = gameData.race;
            currentClass = gameData.characterClass;
            
            // Ensure name is not empty or default
            if (string.IsNullOrEmpty(gameData.characterName))
            {
                if (!string.IsNullOrEmpty(savedData.characterName))
                {
                    gameData.characterName = savedData.characterName;
                }
            }
            
            CharacterManager.Instance.LoadCharacterData(gameData);
            
            // Verify name was set correctly
            string finalName = CharacterManager.Instance.GetName();
            
            if (finalName != savedData.characterName && !string.IsNullOrEmpty(savedData.characterName))
            {
                CharacterManager.Instance.SetName(savedData.characterName);
            }
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
        
        // Get slot index from PlayerPrefs (in case it wasn't set during load)
        if (currentSlotIndex < 0)
        {
            currentSlotIndex = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        }
        
        if (currentSlotIndex < 0)
        {
            return;
        }
        
        // Get current character data from CharacterManager
        CharacterData currentData = CharacterManager.Instance.GetCharacterData();
        
        // Get race/class from CharacterData (preferred) or fallback to stored values
        string saveRace = !string.IsNullOrEmpty(currentData.race) ? currentData.race : currentRace;
        string saveClass = !string.IsNullOrEmpty(currentData.characterClass) ? currentData.characterClass : currentClass;
        
        // Update stored values for next time
        currentRace = saveRace;
        currentClass = saveClass;
        
        // Create saved character data
        SavedCharacterData savedData = new SavedCharacterData();
        savedData.SaveFrom(currentData, saveRace, saveClass);
        
        // Save back to PlayerPrefs
        string key = $"Character_{currentSlotIndex}";
        string json = JsonUtility.ToJson(savedData);
        PlayerPrefs.SetString(key, json);
        
        // Also update active character (only if this is still the active character)
        int activeSlot = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        if (activeSlot == currentSlotIndex)
        {
            PlayerPrefs.SetString("ActiveCharacter", json);
        }
        
        PlayerPrefs.Save();
    }
    
    public string GetCurrentRace() => currentRace;
    public string GetCurrentClass() => currentClass;
}

