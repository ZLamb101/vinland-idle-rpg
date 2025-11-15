using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Individual character slot in the character selection screen.
/// Displays character info and handles selection.
/// </summary>
public class CharacterSlot : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI activityText; // Current activity (e.g., "Currently Mining")
    public TextMeshProUGUI lastPlayedText; // Last played time (e.g., "2 hours ago")
    public Image portraitImage;
    public Button slotButton;
    
    [Header("Visual Feedback")]
    public GameObject selectedIndicator; // Border/highlight when selected
    public GameObject lockedOverlay; // Shows when slot is locked
    
    [Header("Locked Slot Visuals")]
    public Color lockedTextColor = new Color(0.5f, 0.5f, 0.5f);
    public Color unlockedTextColor = Color.white;
    
    private int slotIndex;
    private SavedCharacterData characterData;
    private bool isLocked = true;
    private bool isSelected = false;
    private int unlockLevel = 0; // Store the unlock level for this slot
    
    public event Action<CharacterSlot> OnSlotClicked;
    
    void Start()
    {
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnClick);
        }
    }
    
    public void Initialize(int index, SavedCharacterData data, bool locked, int unlockLevelRequired = 0)
    {
        slotIndex = index;
        characterData = data;
        isLocked = locked;
        unlockLevel = unlockLevelRequired;
        
        if (!locked && !data.isEmpty)
        {
        }
        
        UpdateDisplay();
    }
    
    public void UpdateDisplay()
    {
        // Update locked state
        if (lockedOverlay != null)
            lockedOverlay.SetActive(isLocked);
        
        if (slotButton != null)
            slotButton.interactable = !isLocked;
        
        if (isLocked)
        {
            // Show locked slot
            if (nameText != null)
            {
                nameText.text = "Locked";
                nameText.color = lockedTextColor;
            }
            
            if (descriptionText != null)
            {
                descriptionText.text = GetUnlockRequirement();
                descriptionText.color = lockedTextColor;
            }
            
            // Hide activity and last played text for locked slots
            if (activityText != null)
            {
                activityText.text = "";
            }
            if (lastPlayedText != null)
            {
                lastPlayedText.text = "";
            }
        }
        else
        {
            // Show character or empty slot
            if (nameText != null)
            {
                nameText.text = characterData.isEmpty ? "Empty Slot" : characterData.characterName;
                nameText.color = unlockedTextColor;
            }
            
            if (descriptionText != null)
            {
                descriptionText.text = characterData.GetDescription();
                descriptionText.color = unlockedTextColor;
            }
            
            // Update activity display (only for non-empty characters)
            if (activityText != null)
            {
                if (!characterData.isEmpty)
                {
                    // Get activity from AwayActivityManager if available
                    string activityDisplay = "";
                    if (Services.TryGet<IAwayActivityService>(out var awayActivityService))
                    {
                        activityDisplay = awayActivityService.GetActivityDisplayString(slotIndex);
                    }
                    activityText.text = activityDisplay;
                    activityText.color = unlockedTextColor;
                }
                else
                {
                    activityText.text = "";
                }
            }
            
            // Update last played time display (only for non-empty characters)
            if (lastPlayedText != null)
            {
                if (!characterData.isEmpty)
                {
                    string lastPlayedDisplay = "";
                    if (Services.TryGet<IAwayActivityService>(out var awayActivityService))
                    {
                        lastPlayedDisplay = awayActivityService.GetTimeSinceLastPlayed(slotIndex);
                    }
                    lastPlayedText.text = $"Last played: {lastPlayedDisplay}";
                    lastPlayedText.color = unlockedTextColor;
                }
                else
                {
                    lastPlayedText.text = "";
                }
            }
        }
        
        // Update selection indicator
        if (selectedIndicator != null)
            selectedIndicator.SetActive(isSelected);
    }
    
    string GetUnlockRequirement()
    {
        if (unlockLevel == 0)
        {
            return "Available";
        }
        else
        {
            return $"Unlocks at Account Level {unlockLevel}";
        }
    }
    
    void OnClick()
    {
        if (!isLocked)
        {
            OnSlotClicked?.Invoke(this);
        }
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectedIndicator != null)
            selectedIndicator.SetActive(selected);
    }
    
    public int GetSlotIndex() => slotIndex;
    public SavedCharacterData GetCharacterData() => characterData;
    public bool IsEmpty() => characterData == null || characterData.isEmpty;
    public bool IsLocked() => isLocked;
    
    public void UpdateCharacterData(SavedCharacterData newData)
    {
        characterData = newData;
        UpdateDisplay();
    }
}

