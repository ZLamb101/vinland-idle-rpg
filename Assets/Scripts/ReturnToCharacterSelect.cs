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
            // Get current character data directly from CharacterManager
            if (CharacterManager.Instance != null)
            {
                // Try to find CharacterLoader, but don't worry if it's not found
                CharacterLoader loader = FindObjectOfType<CharacterLoader>();
                if (loader != null)
                {
                    loader.SaveCurrentCharacter();
                }
                else
                {
                    // This is normal - CharacterLoader might not be found during scene transition
                    SaveCharacterManually();
                }
            }
            else
            {
            }
        }
        
        // Load character selection scene
        SceneManager.LoadScene(characterSceneName);
    }
    
    void SaveCharacterManually()
    {
        // Get current character data from CharacterManager
        CharacterData currentData = CharacterManager.Instance.GetCharacterData();
        
        // Get character info from CharacterData (preferred) or PlayerPrefs (fallback)
        string currentRace = !string.IsNullOrEmpty(currentData.race) ? currentData.race : PlayerPrefs.GetString("ActiveCharacterRace", "Human");
        string currentClass = !string.IsNullOrEmpty(currentData.characterClass) ? currentData.characterClass : PlayerPrefs.GetString("ActiveCharacterClass", "Warrior");
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
        PlayerPrefs.SetString("ActiveCharacterRace", savedData.race);
        PlayerPrefs.SetString("ActiveCharacterClass", savedData.characterClass);
        PlayerPrefs.Save();
    }
}

