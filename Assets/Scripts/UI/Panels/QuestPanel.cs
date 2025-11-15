using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestPanel : MonoBehaviour
{
    [Header("Quest Data")]
    public QuestData questData;
    
    // Quest rewards are now defined in QuestData ScriptableObject
    
    [Header("UI References")]
    public Slider slider;
    public Button actionButton;
    public TextMeshProUGUI buttonText;
    public TextMeshProUGUI questNameText;
    public TextMeshProUGUI questDescriptionText;
    public TextMeshProUGUI rewardText;
    public Image questIcon; // Optional
    
    private float currentTime = 0f;
    private bool isActive = false;
    
    // Static reference to track currently active quest
    private static QuestPanel currentlyActiveQuest = null;

    private ICharacterService characterService;
    
    void Start()
    {
        characterService = Services.Get<ICharacterService>();
        if (questData == null)
        {
            return;
        }
        
        // Initialize UI components if they exist
        if (slider != null)
        {
            slider.value = 0f;
        }
        
        if (actionButton != null)
        {
            actionButton.onClick.AddListener(ToggleQuest);
        }
        
        // Subscribe to level changes to unlock quests
        if (characterService != null)
        {
            characterService.OnLevelChanged += OnPlayerLevelChanged;
        }
        
        // Initialize quest UI
        UpdateQuestDisplay();
        UpdateButtonText();
        CheckQuestAvailability();
    }
    
    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (characterService != null)
        {
            characterService.OnLevelChanged -= OnPlayerLevelChanged;
        }
        
        // Clear active quest reference if this was the active one
        if (currentlyActiveQuest == this)
        {
            currentlyActiveQuest = null;
        }
    }
    
    void OnDisable()
    {
        // Also clear active quest reference when disabled (scene change, etc.)
        if (currentlyActiveQuest == this)
        {
            currentlyActiveQuest = null;
            isActive = false;
            currentTime = 0f;
            if (slider != null)
                slider.value = 0f;
        }
    }
    
    void OnPlayerLevelChanged(int newLevel)
    {
        // Re-check quest availability when player levels up
        CheckQuestAvailability();
        
        // If quest just became available, show a notification
        if (questData != null && newLevel == questData.levelRequired)
        {
            // Optional: Add visual feedback (flash, highlight, etc.)
            if (questNameText != null)
            {
                questNameText.text = $"{questData.questName} [NEW!]";
                Invoke(nameof(ResetQuestNameDisplay), 2f); // Clear [NEW!] after 2 seconds
            }
        }
    }
    
    void ResetQuestNameDisplay()
    {
        if (questNameText != null && questData != null)
        {
            questNameText.text = questData.questName;
        }
    }
    
    void UpdateQuestDisplay()
    {
        if (questData == null) return;
        
        // Update quest name
        if (questNameText != null)
            questNameText.text = questData.questName;
        
        // Update quest description
        if (questDescriptionText != null)
            questDescriptionText.text = questData.description;
        
        // Update reward display
        if (rewardText != null)
            rewardText.text = $"Rewards: {questData.xpReward} XP, {questData.goldReward} Gold";
        
        // Update quest icon
        if (questIcon != null && questData.questIcon != null)
            questIcon.sprite = questData.questIcon;
    }
    
    void CheckQuestAvailability()
    {
        if (characterService == null || questData == null) return;
        
        int playerLevel = characterService.GetLevel();
        bool canDoQuest = playerLevel >= questData.levelRequired;
        
        // Enable/disable button based on level requirement
        if (actionButton != null)
            actionButton.interactable = canDoQuest;
        
        // Update button text to show locked status
        if (buttonText != null)
        {
            if (canDoQuest)
            {
                buttonText.text = isActive ? "Stop" : "Start";
            }
            else
            {
                buttonText.text = $"Locked (Lv.{questData.levelRequired})";
            }
        }
        
        // Update quest name display based on availability
        if (questNameText != null)
        {
            if (!canDoQuest)
            {
                // Quest is locked - show level requirement
                questNameText.text = $"{questData.questName} (Requires Level {questData.levelRequired})";
            }
            else
            {
                // Quest is available - show normal name
                questNameText.text = questData.questName;
            }
        }
        
        // Optional: Visual feedback for locked vs unlocked
        if (questDescriptionText != null)
        {
            Color descColor = canDoQuest ? Color.white : new Color(0.6f, 0.6f, 0.6f); // Gray out locked quests
            questDescriptionText.color = descColor;
        }
    }
    
    void Update()
    {
        if (isActive && questData != null)
        {
            currentTime += Time.deltaTime;
            slider.value = currentTime / questData.duration;
            
            if (currentTime >= questData.duration)
            {
                OnProgressComplete();
            }
        }
    }
    
    void ToggleQuest()
    {
        if (!isActive)
        {
            // Trying to start this quest
            StartQuest();
        }
        else
        {
            // Trying to stop this quest
            StopQuest();
        }
    }
    
    void StartQuest()
    {
        // Stop any currently active quest
        if (currentlyActiveQuest != null && currentlyActiveQuest != this)
        {
            currentlyActiveQuest.StopQuest();
        }
        
        // Start this quest
        isActive = true;
        currentlyActiveQuest = this;
        UpdateButtonText();
    }
    
    void StopQuest()
    {
        // Stop this quest
        isActive = false;
        currentTime = 0f;
        slider.value = 0f;
        
        // Clear the active quest reference if it's this one
        if (currentlyActiveQuest == this)
        {
            currentlyActiveQuest = null;
        }
        
        UpdateButtonText();
    }
    
    void OnProgressComplete()
    {
        if (questData == null) return;
        
        // Add XP and Gold through CharacterManager
        if (characterService != null)
        {
            characterService.AddXP(questData.xpReward);
            characterService.AddGold(questData.goldReward);
            
            // Add item reward from QuestData
            if (questData.itemReward != null)
            {
                InventoryItem itemReward = questData.itemReward.CreateInventoryItem(questData.itemRewardQuantity);
                
                // Try to add item to inventory
                bool itemAdded = characterService.AddItemToInventory(itemReward);
                if (!itemAdded)
                {
                }
            }
        }
        
        // Reset for next cycle (quest remains active and restarts)
        currentTime = 0f;
        slider.value = 0f;
    }
    
    void UpdateButtonText()
    {
        if (buttonText != null)
            buttonText.text = isActive ? "Stop" : "Start";
    }
    
    // Public method to set quest data dynamically
    public void SetQuest(QuestData newQuestData)
    {
        // Stop current quest if active
        if (isActive)
        {
            StopQuest();
        }
        
        // Reset quest state completely
        isActive = false;
        currentTime = 0f;
        if (slider != null)
            slider.value = 0f;
        
        questData = newQuestData;
        
        // Re-initialize quest UI
        UpdateQuestDisplay();
        UpdateButtonText();
        CheckQuestAvailability();
    }
    
    // Public method to check if this quest is currently active
    public bool IsActive()
    {
        return isActive;
    }
    
    // Static method to get the currently active quest
    public static QuestPanel GetActiveQuest()
    {
        return currentlyActiveQuest;
    }
    
    // Static method to clear the active quest reference (useful when re-initializing)
    public static void ClearActiveQuestReference()
    {
        if (currentlyActiveQuest != null)
        {
            // Stop the quest if it's still valid
            if (currentlyActiveQuest.gameObject != null)
            {
                currentlyActiveQuest.StopQuest();
            }
        }
        currentlyActiveQuest = null;
    }
}