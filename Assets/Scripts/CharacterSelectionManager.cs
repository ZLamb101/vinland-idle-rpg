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
    
    [Header("Selected Hero Display")]
    public GameObject selectedHeroPanel; // Panel showing currently selected hero info
    public TextMeshProUGUI selectedHeroNameText; // Name of selected hero
    public TextMeshProUGUI selectedHeroLevelText; // Level of selected hero
    public TextMeshProUGUI selectedHeroActivityText; // Current activity (e.g., "Currently Mining")
    public TextMeshProUGUI selectedHeroZoneText; // Current zone (e.g., "Zone 1-1")
    
    [Header("Action Button")]
    public Button actionButton;
    public TextMeshProUGUI actionButtonText;
    
    [Header("Delete Button")]
    public Button deleteButton; // Button to delete currently selected hero
    
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
    public int[] slotUnlockLevels = { 0, 3, 7, 15, 25, 35 }; // Account level required for each slot
    
    private SavedCharacterData[] savedCharacters = new SavedCharacterData[6];
    private CharacterSlot selectedSlot = null;
    private int selectedSlotIndex = -1;
    private bool[] slotHasBeenUnlocked = new bool[6]; // Track if slot has ever had a character (stays unlocked)
    
    void Start()
    {
        // Ensure AwayActivityManager exists (for checking activity status)
        if (!Services.TryGet<IAwayActivityService>(out var awayActivityService))
        {
            GameObject awayManagerObj = new GameObject("AwayActivityManager");
            awayActivityService = awayManagerObj.AddComponent<AwayActivityManager>();
            //Services.Register<IAwayActivityService>(awayActivityService);
        }
        
        // Ensure ZoneManager exists (for checking zone information)
        if (!Services.TryGet<IZoneService>(out var zoneService))
        {
            GameObject zoneManagerObj = new GameObject("ZoneManager");
            zoneService = zoneManagerObj.AddComponent<ZoneManager>();
            //Services.Register<IZoneService>(zoneService);
        }
        
        InitializeSlots();
        LoadAllCharacters();
        LoadSlotUnlockStates(); // Load which slots have been unlocked before
        UpdateSlotLocks();
        UpdateTotalLevelDisplay();
        
        // Setup buttons
        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionButtonClick);
        
        if (deleteButton != null)
            deleteButton.onClick.AddListener(OnDeleteButtonClick);
        
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
        
        // Initialize selected hero display
        UpdateSelectedHeroDisplay();
        
        // Start coroutine to update last played time display every minute
        StartCoroutine(UpdateLastPlayedTimeCoroutine());
    }
    
    /// <summary>
    /// Coroutine to update last played time display every minute
    /// </summary>
    System.Collections.IEnumerator UpdateLastPlayedTimeCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(60f); // Wait 60 seconds (1 minute)
            
            // Update all character slots' last played time display
            foreach (CharacterSlot slot in characterSlots)
            {
                if (slot != null && !slot.IsEmpty())
                {
                    slot.UpdateDisplay();
                }
            }
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
        // Load from NEW SaveSystem (JSON files) first, fallback to old PlayerPrefs
        for (int i = 0; i < savedCharacters.Length; i++)
        {
            // Try NEW save system first
            if (SaveSystem.SaveFileExists(i))
            {
                SaveData saveData = SaveSystem.LoadCharacter(i);
                if (saveData != null)
                {
                    // Convert SaveData to SavedCharacterData for display
                    savedCharacters[i] = new SavedCharacterData
                    {
                        characterName = saveData.characterName,
                        race = saveData.race,
                        characterClass = saveData.characterClass,
                        level = saveData.level,
                        currentXP = saveData.currentXP,
                        gold = saveData.gold,
                        currentHealth = saveData.currentHealth,
                        isEmpty = false,
                        // Parse save time if available
                        lastPlayedDate = !string.IsNullOrEmpty(saveData.saveTime) && long.TryParse(saveData.saveTime, out long ticks)
                            ? new System.DateTime(ticks)
                            : System.DateTime.Now
                    };
                    
                    slotHasBeenUnlocked[i] = true;
                    continue;
                }
            }
            
            // Fallback to OLD PlayerPrefs system (for backwards compatibility)
            string key = $"Character_{i}";
            if (PlayerPrefs.HasKey(key))
            {
                string json = PlayerPrefs.GetString(key);
                savedCharacters[i] = JsonUtility.FromJson<SavedCharacterData>(json);
                
                // If character exists, mark slot as unlocked
                if (!savedCharacters[i].isEmpty)
                {
                    slotHasBeenUnlocked[i] = true;
                }
            }
            else
            {
                // Initialize empty slot
                savedCharacters[i] = new SavedCharacterData { isEmpty = true };
            }
        }
    }
    
    void LoadSlotUnlockStates()
    {
        // Load which slots have been unlocked (had characters created) from PlayerPrefs
        for (int i = 0; i < slotHasBeenUnlocked.Length; i++)
        {
            string key = $"Slot_{i}_Unlocked";
            slotHasBeenUnlocked[i] = PlayerPrefs.GetInt(key, 0) == 1;
        }
    }
    
    void SaveSlotUnlockState(int slotIndex)
    {
        // Save that this slot has been unlocked (has had a character created)
        string key = $"Slot_{slotIndex}_Unlocked";
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        slotHasBeenUnlocked[slotIndex] = true;
    }
    
    void SaveCharacter(int slotIndex, SavedCharacterData data)
    {
        // Convert SavedCharacterData to SaveData for new save system
        SaveData saveData = new SaveData
        {
            characterName = data.characterName,
            race = data.race,
            characterClass = data.characterClass,
            level = data.level,
            currentXP = data.currentXP,
            gold = data.gold,
            currentHealth = data.currentHealth,
            saveTime = System.DateTime.Now.Ticks.ToString(),
            version = 1
        };
        
        // Save using NEW SaveSystem (JSON files)
        bool success = SaveSystem.SaveCharacter(slotIndex, saveData);
        
        if (success)
        {
            Debug.Log($"[CharacterSelection] Saved character to slot {slotIndex} using new SaveSystem");
        }
        else
        {
            Debug.LogError($"[CharacterSelection] Failed to save character to slot {slotIndex}");
        }
        
        // Also save to OLD PlayerPrefs system for backwards compatibility (temporary)
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
                // Slot is unlocked if:
                // 1. Account level meets requirement, OR
                // 2. Slot has been unlocked before (had a character created in it)
                bool meetsLevelRequirement = totalAccountLevel >= slotUnlockLevels[i];
                bool hasBeenUnlocked = slotHasBeenUnlocked[i];
                bool isLocked = !meetsLevelRequirement && !hasBeenUnlocked;
                
                int unlockLevel = slotUnlockLevels[i];
                characterSlots[i].Initialize(i, savedCharacters[i], isLocked, unlockLevel);
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
        UpdateSelectedHeroDisplay(); // Update display when selection changes
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
        
        // Update delete button state
        if (deleteButton != null)
        {
            // Only enable delete button if a non-empty slot is selected
            deleteButton.interactable = selectedSlot != null && !selectedSlot.IsEmpty();
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
        
        // Set default zone to zone 1-1 (index 0) for new characters
        if (Services.TryGet<IZoneService>(out var zoneService))
        {
            zoneService.SetDefaultZoneForSlot(selectedSlotIndex);
        }
        else
        {
            // If ZoneManager doesn't exist yet, save zone index directly
            PlayerPrefs.SetInt($"Character_{selectedSlotIndex}_ZoneIndex", 0);
            PlayerPrefs.Save();
        }
        
        // Mark this slot as unlocked (has had a character created)
        SaveSlotUnlockState(selectedSlotIndex);
        
        // Update slot display
        if (selectedSlot != null)
        {
            selectedSlot.UpdateCharacterData(newChar);
        }
        
        // Hide creation panel
        if (characterCreationPanel != null)
            characterCreationPanel.SetActive(false);
        
        UpdateActionButton();
        UpdateSlotLocks(); // Update slot locks after creating character
        UpdateTotalLevelDisplay();
        UpdateSelectedHeroDisplay(); // Update selected hero display
        
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
        
        // Save the slot index FIRST before saving character data
        // This ensures CharacterLoader knows which slot to save back to
        PlayerPrefs.SetInt("ActiveCharacterSlot", selectedSlotIndex);
        
        // Store selected character data to load in game scene
        string json = JsonUtility.ToJson(charData);
        PlayerPrefs.SetString("ActiveCharacter", json);
        PlayerPrefs.SetString("ActiveCharacterRace", charData.race);
        PlayerPrefs.SetString("ActiveCharacterClass", charData.characterClass);
        
        // Save synchronously (required before scene load)
        PlayerPrefs.Save();
        
        // Check if scene exists before loading
        if (string.IsNullOrEmpty(gameSceneName))
        {
            return;
        }
        
        // Load game scene immediately
        SceneManager.LoadScene(gameSceneName);
    }
    
    // Call this when returning from game scene to refresh character data
    public void RefreshCharacterData()
    {
        LoadAllCharacters();
        UpdateSlotLocks();
        UpdateActionButton();
        UpdateTotalLevelDisplay();
        UpdateSelectedHeroDisplay(); // Update selected hero display
    }
    
    void UpdateSelectedHeroDisplay()
    {
        if (selectedHeroPanel != null)
        {
            // Show panel only if a slot is selected and it's not empty
            bool shouldShow = selectedSlot != null && !selectedSlot.IsEmpty();
            selectedHeroPanel.SetActive(shouldShow);
            
            if (shouldShow && selectedSlot != null)
            {
                SavedCharacterData charData = selectedSlot.GetCharacterData();
                
                // Update name
                if (selectedHeroNameText != null)
                {
                    selectedHeroNameText.text = charData.characterName;
                }
                
                // Update level
                if (selectedHeroLevelText != null)
                {
                    selectedHeroLevelText.text = $"Level {charData.level}";
                }
                
                // Update activity (if AwayActivityManager exists)
                if (selectedHeroActivityText != null)
                {
                    string activityText = "";
                    if (Services.TryGet<IAwayActivityService>(out var awayActivityService))
                    {
                        activityText = awayActivityService.GetActivityDisplayString(selectedSlotIndex);
                    }
                    selectedHeroActivityText.text = activityText;
                }
                
                // Update zone (if ZoneManager exists)
                if (selectedHeroZoneText != null)
                {
                    string zoneText = "";
                    if (Services.TryGet<IZoneService>(out var zoneService))
                    {
                        zoneText = zoneService.GetZoneNameForSlot(selectedSlotIndex);
                    }
                    selectedHeroZoneText.text = !string.IsNullOrEmpty(zoneText) ? $"Zone: {zoneText}" : "";
                }
            }
            else
            {
                // Clear text if panel is hidden
                if (selectedHeroNameText != null)
                    selectedHeroNameText.text = "";
                if (selectedHeroLevelText != null)
                    selectedHeroLevelText.text = "";
                if (selectedHeroActivityText != null)
                    selectedHeroActivityText.text = "";
                if (selectedHeroZoneText != null)
                    selectedHeroZoneText.text = "";
            }
        }
    }
    
    // Public getter for total account level (for other systems to use)
    public int GetTotalLevel()
    {
        return GetTotalAccountLevel();
    }
    
    void OnDeleteButtonClick()
    {
        if (selectedSlot == null || selectedSlot.IsEmpty())
        {
            return;
        }
        
        // Confirm deletion (you could add a confirmation dialog here)
        SavedCharacterData charData = selectedSlot.GetCharacterData();
        string charName = charData.characterName;
        
        
        // Delete the character
        DeleteCharacter(selectedSlotIndex);
        
        // Clear selection or select first available slot
        selectedSlot = null;
        selectedSlotIndex = -1;
        SelectFirstAvailableSlot();
        
        // Update displays
        UpdateActionButton();
        UpdateSlotLocks();
        UpdateTotalLevelDisplay();
        UpdateSelectedHeroDisplay();
    }
    
    // Public method to delete a character (for future use)
    public void DeleteCharacter(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= savedCharacters.Length) return;
        
        // Delete from NEW SaveSystem (JSON files)
        if (SaveSystem.SaveFileExists(slotIndex))
        {
            SaveSystem.DeleteCharacter(slotIndex);
            Debug.Log($"[CharacterSelection] Deleted character from slot {slotIndex} (NEW system)");
        }
        
        // Delete from OLD PlayerPrefs system
        string key = $"Character_{slotIndex}";
        PlayerPrefs.DeleteKey(key);
        
        // If this was the active character, clear active character data
        int activeSlot = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        if (activeSlot == slotIndex)
        {
            PlayerPrefs.DeleteKey("ActiveCharacter");
            PlayerPrefs.DeleteKey("ActiveCharacterRace");
            PlayerPrefs.DeleteKey("ActiveCharacterClass");
            PlayerPrefs.DeleteKey("ActiveCharacterSlot");
        }
        
        PlayerPrefs.Save(); // Save changes
        
        savedCharacters[slotIndex] = new SavedCharacterData { isEmpty = true };
        
        if (characterSlots[slotIndex] != null)
        {
            characterSlots[slotIndex].UpdateCharacterData(savedCharacters[slotIndex]);
        }
        
        UpdateSlotLocks();
    }
}

