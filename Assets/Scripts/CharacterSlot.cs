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
    
    public event Action<CharacterSlot> OnSlotClicked;
    
    void Start()
    {
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnClick);
        }
    }
    
    public void Initialize(int index, SavedCharacterData data, bool locked)
    {
        slotIndex = index;
        characterData = data;
        isLocked = locked;
        
        if (!locked && !data.isEmpty)
        {
            Debug.Log($"Slot {index} initialized: {data.characterName} - Level {data.level} {data.race} {data.characterClass}");
        }
        
        UpdateDisplay();
    }
    
    void UpdateDisplay()
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
        }
        
        // Update selection indicator
        if (selectedIndicator != null)
            selectedIndicator.SetActive(isSelected);
    }
    
    string GetUnlockRequirement()
    {
        // Slot unlock requirements based on slot index
        switch (slotIndex)
        {
            case 0: return "Available";
            case 1: return "Unlocks at Account Level 15";
            case 2: return "Unlocks at Account Level 30";
            case 3: return "Unlocks at Account Level 50";
            case 4: return "Unlocks at Account Level 75";
            case 5: return "Unlocks at Account Level 100";
            default: return "Locked";
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

