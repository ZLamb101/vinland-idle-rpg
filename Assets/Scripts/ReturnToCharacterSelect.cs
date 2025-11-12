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
        // Save away activity state BEFORE ending anything (so we save the current activity)
        if (AwayActivityManager.Instance != null)
        {
            AwayActivityManager.Instance.SaveAwayState();
            
            // Save last played time for this character
            int currentSlot = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
            if (currentSlot >= 0)
            {
                AwayActivityManager.Instance.SaveLastPlayedTime(currentSlot);
            }
        }
        
        // Stop gathering when leaving (to prevent gathering continuing in background)
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.StopGathering();
        }
        
        // Don't end combat/activity here - just save the state
        // Combat/activity will be cleared when entering a new character
        
        // Save current zone per character slot before leaving
        if (ZoneManager.Instance != null)
        {
            int currentSlot = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
            if (currentSlot >= 0)
            {
                int zoneIndex = ZoneManager.Instance.GetCurrentZoneIndex();
                PlayerPrefs.SetInt($"Character_{currentSlot}_ZoneIndex", zoneIndex);
                PlayerPrefs.Save();
            }
        }
        
        // Save current character if needed
        if (saveBeforeReturning)
        {
            // Get current character data directly from CharacterService
            var characterService = Services.Get<ICharacterService>();
            if (characterService != null)
            {
                // Try to find CharacterLoader, but don't worry if it's not found
                CharacterLoader loader = ComponentInjector.GetOrFind<CharacterLoader>();
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
        
        // Destroy persistent managers before returning to character selection
        // This ensures clean slate for loading a different character
        DestroyPersistentManagers();
        
        // Load character selection scene
        SceneManager.LoadScene(characterSceneName);
    }
    
    void DestroyPersistentManagers()
    {
        Debug.Log("[ReturnToCharacterSelect] Cleaning up persistent managers for character switch");
        
        // Destroy CharacterManager
        if (CharacterManager.Instance != null)
        {
            Destroy(CharacterManager.Instance.gameObject);
        }
        
        // Destroy CombatManager
        if (CombatManager.Instance != null)
        {
            Destroy(CombatManager.Instance.gameObject);
        }
        
        // Destroy ResourceManager
        if (ResourceManager.Instance != null)
        {
            Destroy(ResourceManager.Instance.gameObject);
        }
        
        // Destroy ZoneManager
        if (ZoneManager.Instance != null)
        {
            Destroy(ZoneManager.Instance.gameObject);
        }
        
        // Destroy EquipmentManager
        if (EquipmentManager.Instance != null)
        {
            Destroy(EquipmentManager.Instance.gameObject);
        }
        
        // Destroy AwayActivityManager
        if (AwayActivityManager.Instance != null)
        {
            Destroy(AwayActivityManager.Instance.gameObject);
        }
    }
    
    void SaveCharacterManually()
    {
        // Get current character data from CharacterService
        var characterService = Services.Get<ICharacterService>();
        if (characterService == null) return;
        
        CharacterData currentData = characterService.GetCharacterData();
        
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

