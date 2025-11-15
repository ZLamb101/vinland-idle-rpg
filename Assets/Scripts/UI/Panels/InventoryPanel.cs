using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the inventory UI grid and interactions.
/// </summary>
public class InventoryPanel : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int gridWidth = 5;
    public int gridHeight = 4;
    
    [Header("UI References")]
    public Transform inventoryGridParent;
    public GameObject inventorySlotPrefab;
    
    [Header("Current Setup")]
    public InventorySlot[] inventorySlots;
    
    [Header("Tooltip")]
    public Tooltip tooltip;
    
    private InventoryData inventoryData;
    private int selectedSlot = -1;
    private int draggingSlotIndex = -1; // Track which slot is being dragged
    
    void Start()
    {
        InitializeInventory();
        LoadEquipmentReferences();
        RefreshDisplay();
        
        // Subscribe to inventory events for auto-refresh
        EventBus.Subscribe<ItemAddedEvent>(OnItemAdded);
        EventBus.Subscribe<ItemRemovedEvent>(OnItemRemoved);
        
        // Tooltip is now handled by Tooltip component
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        EventBus.Unsubscribe<ItemAddedEvent>(OnItemAdded);
        EventBus.Unsubscribe<ItemRemovedEvent>(OnItemRemoved);
    }
    
    void OnItemAdded(ItemAddedEvent e)
    {
        RefreshDisplay();
    }
    
    void OnItemRemoved(ItemRemovedEvent e)
    {
        RefreshDisplay();
    }
    
    
    /// <summary>
    /// Load item references (icons, equipment, etc.) for all items after deserialization
    /// </summary>
    void LoadEquipmentReferences()
    {
        if (inventoryData == null) return;
        inventoryData.LoadAllItemReferences();
    }
    
    void InitializeInventory()
    {
        // Get inventory data from CharacterService
        var characterService = Services.Get<ICharacterService>();
        if (characterService != null)
        {
            inventoryData = characterService.GetInventoryData();
        }
        
        // If no inventory data exists, create new one
        if (inventoryData == null)
        {
            inventoryData = new InventoryData();
            inventoryData.maxSlots = gridWidth * gridHeight;
        }
        
        // Create inventory slots if they don't exist
        CreateInventorySlots();
        
        // Subscribe to slot click events and drag events
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] != null)
            {
                int slotIndex = i; // Capture for closure
                inventorySlots[i].OnSlotClicked += (index) => OnSlotClicked(index);
                inventorySlots[i].OnSlotDragEnd += (fromSlot, toSlot) => OnSlotDragEnd(fromSlot, toSlot);
            }
        }
    }
    
    void CreateInventorySlots()
    {
        // Clear existing slots
        if (inventorySlots != null)
        {
            foreach (var slot in inventorySlots)
            {
                if (slot != null) Destroy(slot.gameObject);
            }
        }
        
        // Set up grid layout
        SetupGridLayout();
        
        // Create new slots
        inventorySlots = new InventorySlot[gridWidth * gridHeight];
        
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            GameObject slotObj;
            
            if (inventorySlotPrefab != null)
            {
                slotObj = Instantiate(inventorySlotPrefab, inventoryGridParent);
            }
            else
            {
                // Create a simple slot if no prefab is provided
                slotObj = CreateSimpleSlot();
            }
            
            inventorySlots[i] = slotObj.GetComponent<InventorySlot>();
            if (inventorySlots[i] == null)
            {
                inventorySlots[i] = slotObj.AddComponent<InventorySlot>();
            }
            
            inventorySlots[i].Initialize(i);
            
            // Set inventoryPanel reference for the slot
            inventorySlots[i].SetInventoryPanel(this);
        }
    }
    
    void SetupGridLayout()
    {
        if (inventoryGridParent == null) return;
        
        // Remove existing layout components
        VerticalLayoutGroup vlg = inventoryGridParent.GetComponent<VerticalLayoutGroup>();
        if (vlg != null) Destroy(vlg);
        
        HorizontalLayoutGroup hlg = inventoryGridParent.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) Destroy(hlg);
        
        // Add Grid Layout Group
        GridLayoutGroup glg = inventoryGridParent.GetComponent<GridLayoutGroup>();
        if (glg == null)
        {
            glg = inventoryGridParent.gameObject.AddComponent<GridLayoutGroup>();
        }
        
        // Configure grid layout
        glg.cellSize = new Vector2(100, 100);
        glg.spacing = new Vector2(5, 5);
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.UpperCenter;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = gridWidth;
    }
    
    GameObject CreateSimpleSlot()
    {
        // Create a simple slot GameObject with basic UI components
        GameObject slot = new GameObject($"InventorySlot_{inventorySlots.Length}");
        slot.transform.SetParent(inventoryGridParent);
        
        // Add RectTransform
        RectTransform rect = slot.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(80, 80);
        
        // Add Image for background
        Image bg = slot.AddComponent<Image>();
        bg.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        
        // Add Button
        Button button = slot.AddComponent<Button>();
        
        // Create icon child
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slot.transform);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.sizeDelta = Vector2.zero;
        iconRect.anchoredPosition = Vector2.zero;
        
        Image icon = iconObj.AddComponent<Image>();
        icon.color = Color.white;
        icon.gameObject.SetActive(false);
        
        // Create quantity text child
        GameObject qtyObj = new GameObject("Quantity");
        qtyObj.transform.SetParent(slot.transform);
        RectTransform qtyRect = qtyObj.AddComponent<RectTransform>();
        qtyRect.anchorMin = new Vector2(0.6f, 0.6f);
        qtyRect.anchorMax = new Vector2(1f, 1f);
        qtyRect.sizeDelta = Vector2.zero;
        qtyRect.anchoredPosition = Vector2.zero;
        
        TMPro.TextMeshProUGUI qtyText = qtyObj.AddComponent<TMPro.TextMeshProUGUI>();
        qtyText.text = "";
        qtyText.fontSize = 12;
        qtyText.color = Color.white;
        qtyText.alignment = TMPro.TextAlignmentOptions.BottomRight;
        qtyText.gameObject.SetActive(false);
        
        // Get InventorySlot component and set references
        InventorySlot inventorySlot = slot.GetComponent<InventorySlot>();
        inventorySlot.itemIcon = icon;
        inventorySlot.quantityText = qtyText;
        inventorySlot.slotButton = button;
        
        return slot;
    }
    
    public void RefreshDisplay()
    {
        if (inventoryData == null || inventorySlots == null) return;
        
        // Update each slot
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (i < inventoryData.maxSlots)
            {
                InventoryItem item = inventoryData.GetItem(i);
                inventorySlots[i].SetItem(item);
                inventorySlots[i].SetSelected(i == selectedSlot);
            }
        }
    }
    
    void OnSlotClicked(int slotIndex)
    {
        // Handle slot selection
        if (selectedSlot == slotIndex)
        {
            // Deselect if clicking the same slot
            selectedSlot = -1;
        }
        else
        {
            selectedSlot = slotIndex;
        }
        
        RefreshDisplay();
        
        // Handle item interaction (for future use)
        // Can add item usage logic here
    }
    
    /// <summary>
    /// Add an item to the inventory.
    /// </summary>
    public bool AddItem(InventoryItem item)
    {
        if (inventoryData == null) return false;
        
        InventoryData.AddItemResult result = inventoryData.AddItem(item);
        if (result.itemsAdded > 0)
        {
            RefreshDisplay();
        }
        
        // Warn if some items couldn't be added
        if (!result.success && result.itemsRemaining > 0)
        {
            Debug.LogWarning($"[inventoryPanel] Inventory full! Could only add {result.itemsAdded} of {result.itemsAdded + result.itemsRemaining} {item.itemName}. {result.itemsRemaining} items were lost.");
        }
        
        return result.success;
    }
    
    /// <summary>
    /// Remove an item from the selected slot.
    /// </summary>
    public bool RemoveSelectedItem(int quantity = 1)
    {
        if (selectedSlot < 0 || inventoryData == null) return false;
        
        bool success = inventoryData.RemoveItem(selectedSlot, quantity);
        if (success)
        {
            RefreshDisplay();
        }
        
        return success;
    }
    
    public InventoryData GetInventoryData() => inventoryData;
    public int GetSelectedSlot() => selectedSlot;
    
    /// <summary>
    /// Show tooltip for an inventory item
    /// </summary>
    public void ShowTooltip(InventoryItem item)
    {
        if (tooltip != null)
        {
            tooltip.ShowForInventoryItem(item);
        }
    }
    
    /// <summary>
    /// Hide tooltip
    /// </summary>
    public void HideTooltip()
    {
        if (tooltip != null)
        {
            tooltip.Hide();
        }
    }
    
    /// <summary>
    /// Called when drag starts from a slot
    /// </summary>
    public void OnDragStart(int slotIndex)
    {
        draggingSlotIndex = slotIndex;
        HideTooltip(); // Hide tooltip during drag
    }
    
    /// <summary>
    /// Called when drag ends
    /// </summary>
    public void OnDragEnd()
    {
        draggingSlotIndex = -1;
    }
    
    /// <summary>
    /// Handle drag end event - swap items between slots
    /// </summary>
    void OnSlotDragEnd(int fromSlot, int toSlot)
    {
        if (inventoryData == null) return;
        
        // Swap the items
        bool success = inventoryData.SwapItems(fromSlot, toSlot);
        
        if (success)
        {
            // Refresh display to show the swapped items
            RefreshDisplay();
        }
    }
    
}