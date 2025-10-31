using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple button script to return to character selection.
/// Auto-saves current character before returning.
/// </summary>
public class ReturnToCharacterSelect : MonoBehaviour
{
    [Header("Settings")]
    public string characterSceneName = "CharacterScene";
    public bool saveBeforeReturning = true;
    
    private Button button;
    
    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(ReturnToSelect);
        }
    }
    
    public void ReturnToSelect()
    {
        // Save current character if needed
        if (saveBeforeReturning)
        {
            Debug.Log("🔄 Returning to character select - saving current progress...");
            
            // Get current character data directly from CharacterManager
            if (CharacterManager.Instance != null)
            {
                // Try to find CharacterLoader, but don't worry if it's not found
                CharacterLoader loader = FindObjectOfType<CharacterLoader>();
                if (loader != null)
                {
                    loader.SaveCurrentCharacter();
                    Debug.Log("✅ Character saved via CharacterLoader");
                }
                else
                {
                    // This is normal - CharacterLoader might not be found during scene transition
                    Debug.Log("📝 CharacterLoader not found - using manual save (this is normal)");
                    SaveCharacterManually();
                }
            }
            else
            {
                Debug.LogWarning("CharacterManager not found - cannot save character data");
            }
        }
        
        // Load character selection scene
        SceneManager.LoadScene(characterSceneName);
    }
    
    void SaveCharacterManually()
    {
        // Get current character data from CharacterManager
        CharacterData currentData = CharacterManager.Instance.GetCharacterData();
        
        // Get character info from PlayerPrefs
        string currentRace = PlayerPrefs.GetString("ActiveCharacterRace", "Human");
        string currentClass = PlayerPrefs.GetString("ActiveCharacterClass", "Warrior");
        int currentSlotIndex = PlayerPrefs.GetInt("ActiveCharacterSlot", 0);
        
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
        
        Debug.Log($"💾 Manual save to {key}: {savedData.characterName} - Level {savedData.level} ({savedData.currentXP} XP, {savedData.gold} Gold)");
    }
}

