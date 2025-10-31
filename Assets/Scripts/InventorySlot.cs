using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Individual inventory slot UI component.
/// Supports left-click to select and right-click to use/equip items.
/// </summary>
public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public Image itemIcon;
    public TextMeshProUGUI quantityText;
    public Button slotButton;
    
    [Header("Visual Settings")]
    // Removed selectedSlotColor - no selection highlighting
    
    private int slotIndex;
    private InventoryItem currentItem;
    private bool isSelected = false;
    
    // Events
    public System.Action<int> OnSlotClicked;
    public System.Action<int> OnSlotRightClicked;
    
    void Start()
    {
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClick);
            
            // Add hover events for tooltip
            UnityEngine.EventSystems.EventTrigger trigger = slotButton.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null)
                trigger = slotButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            
            // Pointer enter (hover start)
            UnityEngine.EventSystems.EventTrigger.Entry pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => { OnHoverStart(); });
            trigger.triggers.Add(pointerEnter);
            
            // Pointer exit (hover end)
            UnityEngine.EventSystems.EventTrigger.Entry pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => { OnHoverEnd(); });
            trigger.triggers.Add(pointerExit);
        }
        
        // Initialize item icon to alpha 0
        if (itemIcon != null)
        {
            Color iconColor = itemIcon.color;
            iconColor.a = 0f;
            itemIcon.color = iconColor;
        }
        
        UpdateDisplay();
    }
    
    public void Initialize(int index)
    {
        slotIndex = index;
        UpdateDisplay();
    }
    
    public void SetItem(InventoryItem item)
    {
        currentItem = item;
        UpdateDisplay();
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateDisplay();
    }
    
    private void UpdateDisplay()
    {
        // Update item icon
        if (itemIcon != null)
        {
            if (currentItem != null && !currentItem.IsEmpty() && currentItem.icon != null)
            {
                itemIcon.sprite = currentItem.icon;
                Debug.Log($"Slot {slotIndex}: Setting icon to {currentItem.icon.name}");
                // Set alpha to 255 (fully visible)
                Color iconColor = itemIcon.color;
                iconColor.a = 1f;
                itemIcon.color = iconColor;
                itemIcon.gameObject.SetActive(true);
            }
            else
            {
                itemIcon.sprite = null;
                // Set alpha to 0 (invisible)
                Color iconColor = itemIcon.color;
                iconColor.a = 0f;
                itemIcon.color = iconColor;
                itemIcon.gameObject.SetActive(true); // Keep active but transparent
            }
        }
        
        // Update quantity text
        if (quantityText != null)
        {
            if (currentItem != null && !currentItem.IsEmpty())
            {
                quantityText.text = currentItem.quantity.ToString();
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.text = "";
                quantityText.gameObject.SetActive(false);
            }
        }
        
        // No selection highlighting - keep original button color
    }
    
    private void OnSlotClick()
    {
        OnSlotClicked?.Invoke(slotIndex);
    }
    
    void OnHoverStart()
    {
        if (currentItem != null && !currentItem.IsEmpty())
        {
            InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
            if (inventoryUI != null)
            {
                inventoryUI.ShowTooltip(currentItem);
            }
        }
    }
    
    void OnHoverEnd()
    {
        InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.HideTooltip();
        }
    }
    
    /// <summary>
    /// Handle both left and right clicks
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Right click - try to use/equip item
            OnRightClick();
        }
    }
    
    void OnRightClick()
    {
        if (currentItem == null || currentItem.IsEmpty()) return;
        
        Debug.Log($"OnRightClick: {currentItem.itemName}, itemType={currentItem.itemType}, equipmentData={(currentItem.equipmentData != null ? "EXISTS" : "NULL")}, equipmentAssetName='{currentItem.equipmentAssetName}'");
        
        // Check if item is equipment
        if (currentItem.IsEquipment())
        {
            EquipItem();
        }
        else
        {
            // Future: Handle consumables or other item types
            Debug.Log($"Right-clicked {currentItem.itemName} - not equipment (itemType={currentItem.itemType}, equipmentData is null={currentItem.equipmentData == null})");
        }
    }
    
    void EquipItem()
    {
        if (currentItem.equipmentData == null)
        {
            Debug.LogWarning("Equipment item has no equipment data!");
            return;
        }
        
        if (EquipmentManager.Instance == null)
        {
            Debug.LogWarning("EquipmentManager not found!");
            return;
        }
        
        // Store info before removing from inventory
        string itemName = currentItem.itemName;
        EquipmentSlot equipSlot = currentItem.equipmentData.slot;
        
        // Try to equip the item
        bool equipped = EquipmentManager.Instance.EquipItem(currentItem.equipmentData);
        
        if (equipped)
        {
            // Remove from inventory
            if (CharacterManager.Instance != null)
            {
                CharacterManager.Instance.RemoveItemFromInventory(slotIndex, 1);
                
                // Refresh inventory UI
                InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
                if (inventoryUI != null)
                {
                    inventoryUI.RefreshDisplay();
                }
            }
            
            Debug.Log($"Equipped {itemName} to {equipSlot}");
        }
    }
    
    public int GetSlotIndex() => slotIndex;
    public InventoryItem GetItem() => currentItem;
    public bool IsEmpty() => currentItem == null || currentItem.IsEmpty();
}
