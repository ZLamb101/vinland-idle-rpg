using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component for a single equipment slot.
/// Contains references to the button, icon, and slot type.
/// </summary>
[System.Serializable]
public class EquipmentSlotUI
{
    public EquipmentSlot slotType;
    public Button slotButton;
    public Image itemIcon;
    
    [Tooltip("Optional: Silhouette/background image shown when slot is empty")]
    public Sprite emptySlotIcon;
}

