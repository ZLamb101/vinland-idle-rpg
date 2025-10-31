using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages character selection screen with multiple character slots.
/// Handles save/load, character creation, and slot unlocking.
/// </summary>
public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Character Slots")]
    public CharacterSlot[] characterSlots = new CharacterSlot[6];
    
    [Header("Account Level Display")]
    public TextMeshProUGUI totalLevelText;
    public TextMeshProUGUI nextUnlockText; // Optional: Shows what unlocks next
    
    [Header("Action Button")]
    public Button actionButton;
    public TextMeshProUGUI actionButtonText;
    
    [Header("Character Creation Panel")]
    public GameObject characterCreationPanel;
    public TMP_InputField nameInputField;
    public TMP_Dropdown raceDropdown;
    public TMP_Dropdown classDropdown;
    public Button confirmCreateButton;
    public Button cancelCreateButton;
    
    [Header("Settings")]
    public string gameSceneName = "SampleScene"; // Scene to load when entering world
    
    [Header("Slot Unlock Levels")]
    public int[] slotUnlockLevels = { 0, 15, 30, 50, 75, 100 }; // Account level required for each slot
    
    private SavedCharacterData[] savedCharacters = new SavedCharacterData[6];
    private CharacterSlot selectedSlot = null;
    private int selectedSlotIndex = -1;
    
    void Start()
    {
        InitializeSlots();
        LoadAllCharacters();
        UpdateSlotLocks();
        UpdateTotalLevelDisplay();
        
        // Setup buttons
        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionButtonClick);
        
        if (confirmCreateButton != null)
            confirmCreateButton.onClick.AddListener(OnConfirmCreate);
        
        if (cancelCreateButton != null)
            cancelCreateButton.onClick.AddListener(OnCancelCreate);
        
        // Hide creation panel initially
        if (characterCreationPanel != null)
            characterCreationPanel.SetActive(false);
        
        // Auto-select first available slot if none selected
        if (selectedSlot == null)
        {
            SelectFirstAvailableSlot();
        }
    }
    
    void InitializeSlots()
    {
        // Initialize saved character data
        for (int i = 0; i < savedCharacters.Length; i++)
        {
            savedCharacters[i] = new SavedCharacterData { isEmpty = true };
        }
        
        // Setup slot event listeners
        for (int i = 0; i < characterSlots.Length; i++)
        {
            if (characterSlots[i] != null)
            {
                int index = i; // Capture for lambda
                characterSlots[i].OnSlotClicked += OnSlotSelected;
            }
        }
    }
    
    void LoadAllCharacters()
    {
        // Load from PlayerPrefs
        for (int i = 0; i < savedCharacters.Length; i++)
        {
            string key = $"Character_{i}";
            if (PlayerPrefs.HasKey(key))
            {
                string json = PlayerPrefs.GetString(key);
                savedCharacters[i] = JsonUtility.FromJson<SavedCharacterData>(json);
                Debug.Log($"Loaded {key}: {savedCharacters[i].characterName} - Level {savedCharacters[i].level}");
            }
            else
            {
                // Initialize empty slot
                savedCharacters[i] = new SavedCharacterData { isEmpty = true };
            }
        }
    }
    
    void SaveCharacter(int slotIndex, SavedCharacterData data)
    {
        string key = $"Character_{slotIndex}";
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
        
        savedCharacters[slotIndex] = data;
    }
    
    void UpdateSlotLocks()
    {
        int totalAccountLevel = GetTotalAccountLevel();
        
        for (int i = 0; i < characterSlots.Length; i++)
        {
            if (characterSlots[i] != null)
            {
                bool isLocked = totalAccountLevel < slotUnlockLevels[i];
                characterSlots[i].Initialize(i, savedCharacters[i], isLocked);
            }
        }
        
        UpdateTotalLevelDisplay();
    }
    
    void UpdateTotalLevelDisplay()
    {
        int totalLevel = GetTotalAccountLevel();
        
        // Update total level display
        if (totalLevelText != null)
        {
            totalLevelText.text = $"Account Level: {totalLevel}";
        }
        
        // Update next unlock info (optional)
        if (nextUnlockText != null)
        {
            int nextUnlock = GetNextUnlockLevel(totalLevel);
            if (nextUnlock > 0)
            {
                int levelsNeeded = nextUnlock - totalLevel;
                nextUnlockText.text = $"Next Slot Unlocks: {levelsNeeded} levels";
            }
            else
            {
                nextUnlockText.text = "All Slots Unlocked!";
            }
        }
    }
    
    int GetNextUnlockLevel(int currentLevel)
    {
        // Find the next slot unlock level that's higher than current level
        for (int i = 0; i < slotUnlockLevels.Length; i++)
        {
            if (slotUnlockLevels[i] > currentLevel)
            {
                return slotUnlockLevels[i];
            }
        }
        return -1; // All slots unlocked
    }
    
    int GetTotalAccountLevel()
    {
        int total = 0;
        foreach (var character in savedCharacters)
        {
            if (!character.isEmpty)
            {
                total += character.level;
            }
        }
        return total;
    }
    
    void OnSlotSelected(CharacterSlot slot)
    {
        // Deselect previous slot
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }
        
        // Select new slot
        selectedSlot = slot;
        selectedSlotIndex = slot.GetSlotIndex();
        slot.SetSelected(true);
        
        UpdateActionButton();
    }
    
    void SelectFirstAvailableSlot()
    {
        for (int i = 0; i < characterSlots.Length; i++)
        {
            if (characterSlots[i] != null && !characterSlots[i].IsLocked())
            {
                OnSlotSelected(characterSlots[i]);
                return;
            }
        }
    }
    
    void UpdateActionButton()
    {
        if (actionButton == null || actionButtonText == null) return;
        
        if (selectedSlot == null)
        {
            actionButton.interactable = false;
            actionButtonText.text = "Select a Slot";
        }
        else if (selectedSlot.IsEmpty())
        {
            actionButton.interactable = true;
            actionButtonText.text = "Create New";
        }
        else
        {
            actionButton.interactable = true;
            actionButtonText.text = "Enter World";
        }
    }
    
    void OnActionButtonClick()
    {
        if (selectedSlot == null) return;
        
        if (selectedSlot.IsEmpty())
        {
            // Show character creation panel
            ShowCharacterCreation();
        }
        else
        {
            // Load character and enter world
            EnterWorld();
        }
    }
    
    void ShowCharacterCreation()
    {
        if (characterCreationPanel != null)
        {
            characterCreationPanel.SetActive(true);
            
            // Clear/reset creation fields
            if (nameInputField != null)
                nameInputField.text = "";
            
            // Setup race dropdown if needed
            if (raceDropdown != null && raceDropdown.options.Count == 0)
            {
                raceDropdown.ClearOptions();
                raceDropdown.AddOptions(new List<string> { "Human", "Orc", "Elf", "Dwarf", "Troll", "Undead" });
            }
            
            // Setup class dropdown if needed
            if (classDropdown != null && classDropdown.options.Count == 0)
            {
                classDropdown.ClearOptions();
                classDropdown.AddOptions(new List<string> { "Warrior", "Hunter", "Mage", "Rogue", "Priest", "Paladin" });
            }
        }
    }
    
    void OnConfirmCreate()
    {
        // Validate name
        string charName = nameInputField != null ? nameInputField.text.Trim() : "";
        if (string.IsNullOrEmpty(charName))
        {
            Debug.LogWarning("Character name cannot be empty!");
            return;
        }
        
        // Get race and class
        string race = raceDropdown != null ? raceDropdown.options[raceDropdown.value].text : "Human";
        string charClass = classDropdown != null ? classDropdown.options[classDropdown.value].text : "Warrior";
        
        // Create new character data
        SavedCharacterData newChar = new SavedCharacterData
        {
            characterName = charName,
            race = race,
            characterClass = charClass,
            level = 1,
            currentXP = 0,
            gold = 0,
            currentHealth = 50f,
            createdDate = System.DateTime.Now,
            lastPlayedDate = System.DateTime.Now,
            isEmpty = false
        };
        
        // Save character
        SaveCharacter(selectedSlotIndex, newChar);
        
        // Update slot display
        if (selectedSlot != null)
        {
            selectedSlot.UpdateCharacterData(newChar);
        }
        
        // Hide creation panel
        if (characterCreationPanel != null)
            characterCreationPanel.SetActive(false);
        
        UpdateActionButton();
        UpdateTotalLevelDisplay();
        
        Debug.Log($"Created character: {charName} - Level {newChar.level} {race} {charClass}");
    }
    
    void OnCancelCreate()
    {
        if (characterCreationPanel != null)
            characterCreationPanel.SetActive(false);
    }
    
    void EnterWorld()
    {
        if (selectedSlot == null || selectedSlot.IsEmpty()) return;
        
        SavedCharacterData charData = selectedSlot.GetCharacterData();
        
        // Store selected character data to load in game scene
        string json = JsonUtility.ToJson(charData);
        PlayerPrefs.SetString("ActiveCharacter", json);
        PlayerPrefs.SetString("ActiveCharacterRace", charData.race);
        PlayerPrefs.SetString("ActiveCharacterClass", charData.characterClass);
        PlayerPrefs.SetInt("ActiveCharacterSlot", selectedSlotIndex);
        PlayerPrefs.Save();
        
        Debug.Log($"Loading character: {charData.characterName}");
        
        // Check if scene exists before loading
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("Game Scene Name is not set! Please set it in CharacterSelectionManager.");
            return;
        }
        
        // Validate scene count
        if (SceneManager.sceneCountInBuildSettings == 0)
        {
            Debug.LogError("No scenes in Build Settings! Add scenes via File → Build Settings.");
            return;
        }
        
        Debug.Log($"Loading scene: {gameSceneName}");
        
        // Load game scene
        SceneManager.LoadScene(gameSceneName);
    }
    
    // Call this when returning from game scene to refresh character data
    public void RefreshCharacterData()
    {
        LoadAllCharacters();
        UpdateSlotLocks();
        UpdateActionButton();
        UpdateTotalLevelDisplay();
    }
    
    // Public getter for total account level (for other systems to use)
    public int GetTotalLevel()
    {
        return GetTotalAccountLevel();
    }
    
    // Public method to delete a character (for future use)
    public void DeleteCharacter(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= savedCharacters.Length) return;
        
        string key = $"Character_{slotIndex}";
        PlayerPrefs.DeleteKey(key);
        
        savedCharacters[slotIndex] = new SavedCharacterData { isEmpty = true };
        
        if (characterSlots[slotIndex] != null)
        {
            characterSlots[slotIndex].UpdateCharacterData(savedCharacters[slotIndex]);
        }
        
        UpdateSlotLocks();
    }
}

