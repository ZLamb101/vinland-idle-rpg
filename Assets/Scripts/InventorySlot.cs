using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Individual inventory slot UI component.
/// Supports left-click to select, right-click to use/equip items, and drag-and-drop to reorganize.
/// </summary>
public class InventorySlot : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
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
    
    // Drag and drop
    private GameObject dragIcon;
    private Canvas dragCanvas;
    private bool isDragging = false;
    private int draggedSlotIndex = -1;
    
    // Events
    public System.Action<int> OnSlotClicked;
    public System.Action<int> OnSlotRightClicked;
    public System.Action<int, int> OnSlotDragEnd; // (fromSlot, toSlot)
    
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
        
        // Check if shop is open - if so, sell the item
        if (ShopManager.Instance != null && ShopManager.Instance.IsShopOpen())
        {
            SellItem();
            return;
        }
        
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
    
    void SellItem()
    {
        if (currentItem == null || currentItem.IsEmpty()) return;
        if (ShopManager.Instance == null) return;
        
        // Sell the item
        if (ShopManager.Instance.SellItem(currentItem, slotIndex))
        {
            // Refresh inventory UI
            InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
            if (inventoryUI != null)
            {
                inventoryUI.RefreshDisplay();
            }
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
    
    // --- Drag and Drop Implementation ---
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Only allow dragging if there's an item in this slot
        if (currentItem == null || currentItem.IsEmpty()) return;
        
        isDragging = true;
        draggedSlotIndex = slotIndex;
        
        // Find canvas for drag icon
        dragCanvas = GetComponentInParent<Canvas>();
        if (dragCanvas == null)
        {
            dragCanvas = FindObjectOfType<Canvas>();
        }
        
        if (dragCanvas != null && itemIcon != null && itemIcon.sprite != null)
        {
            // Create drag icon
            dragIcon = new GameObject("DragIcon");
            dragIcon.transform.SetParent(dragCanvas.transform, false);
            dragIcon.transform.SetAsLastSibling(); // Make sure it's on top
            
            Image dragImage = dragIcon.AddComponent<Image>();
            dragImage.sprite = itemIcon.sprite;
            dragImage.raycastTarget = false; // Don't block raycasts
            
            RectTransform dragRect = dragIcon.GetComponent<RectTransform>();
            dragRect.sizeDelta = new Vector2(80, 80); // Match slot size
            
            // Make the original icon semi-transparent
            if (itemIcon != null)
            {
                Color iconColor = itemIcon.color;
                iconColor.a = 0.5f;
                itemIcon.color = iconColor;
            }
        }
        
        // Notify UI that drag started
        InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.OnDragStart(slotIndex);
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || dragIcon == null) return;
        
        // Update drag icon position to follow mouse
        RectTransform dragRect = dragIcon.GetComponent<RectTransform>();
        if (dragCanvas != null)
        {
            Vector2 localPoint;
            RectTransform canvasRect = dragCanvas.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint
            );
            dragRect.anchoredPosition = localPoint;
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        // Clean up drag icon
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }
        
        // Restore original icon opacity
        if (itemIcon != null)
        {
            Color iconColor = itemIcon.color;
            iconColor.a = 1f;
            itemIcon.color = iconColor;
        }
        
        isDragging = false;
        
        // Check if we dropped on a valid slot
        // Try multiple methods to find the drop target
        InventorySlot dropSlot = null;
        
        // Method 1: Check current raycast target
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            dropSlot = eventData.pointerCurrentRaycast.gameObject.GetComponent<InventorySlot>();
            if (dropSlot == null)
            {
                dropSlot = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<InventorySlot>();
            }
        }
        
        // Method 2: Check all raycast results if method 1 failed
        if (dropSlot == null && eventData.pointerCurrentRaycast.gameObject != null)
        {
            // Search through all raycast results
            foreach (var result in eventData.hovered)
            {
                dropSlot = result.GetComponent<InventorySlot>();
                if (dropSlot != null) break;
                
                dropSlot = result.GetComponentInParent<InventorySlot>();
                if (dropSlot != null) break;
            }
        }
        
        // Method 3: Check hovered list
        if (dropSlot == null)
        {
            foreach (GameObject hovered in eventData.hovered)
            {
                dropSlot = hovered.GetComponent<InventorySlot>();
                if (dropSlot != null) break;
                
                dropSlot = hovered.GetComponentInParent<InventorySlot>();
                if (dropSlot != null) break;
            }
        }
        
        if (dropSlot != null && dropSlot != this)
        {
            // Valid drop target - swap items
            OnSlotDragEnd?.Invoke(draggedSlotIndex, dropSlot.slotIndex);
        }
        
        // Notify UI that drag ended
        InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.OnDragEnd();
        }
        
        draggedSlotIndex = -1;
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        // This is handled in OnEndDrag, but we implement the interface
        // to ensure the slot can receive drop events
    }
}
