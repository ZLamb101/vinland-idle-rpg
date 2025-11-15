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
        if (Services.TryGet<IAwayActivityService>(out var awayActivityService))
        {
            awayActivityService.SaveAwayState();
            
            // Save last played time for this character
            int currentSlot = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
            if (currentSlot >= 0)
            {
                awayActivityService.SaveLastPlayedTime(currentSlot);
            }
        }
        
        // Stop gathering when leaving (to prevent gathering continuing in background)
        var resourceService = Services.Get<IResourceService>();
        if (resourceService != null)
        {
            resourceService.StopGathering();
        }
        
        // Don't end combat/activity here - just save the state
        // Combat/activity will be cleared when entering a new character
        
        // Save current zone per character slot before leaving
        if (Services.TryGet<IZoneService>(out var zoneService))
        {
            int currentSlot = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
            if (currentSlot >= 0)
            {
                int zoneIndex = zoneService.GetCurrentZoneIndex();
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
        var characterManager = Services.Get<ICharacterService>();
        if (characterManager != null)
        {
            Destroy((characterManager as MonoBehaviour).gameObject);
        }
        
        // Destroy CombatManager
        var combatManager = Services.Get<ICombatService>();
        if (combatManager != null)
        {
            Destroy((combatManager as MonoBehaviour).gameObject);
        }
        
        // Destroy ResourceManager
        var resourceManager = Services.Get<IResourceService>();
        if (resourceManager != null)
        {
            Destroy((resourceManager as MonoBehaviour).gameObject);
        }

        // Destroy TalentManager
        var talentManager = Services.Get<ITalentService>();
        if (talentManager != null)
        {
            Destroy((talentManager as MonoBehaviour).gameObject);
        }
        
        
        // Destroy EquipmentManager
        var equipmentManager = Services.Get<IEquipmentService>();
        if (equipmentManager != null)
        {
            Destroy((equipmentManager as MonoBehaviour).gameObject);
        }
        
        // Destroy AwayActivityManager
        var awayActivityManager = Services.Get<IAwayActivityService>();
        if (awayActivityManager != null)
        {
            Destroy((awayActivityManager as MonoBehaviour).gameObject);
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

