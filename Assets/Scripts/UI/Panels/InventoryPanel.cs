using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the inventory UI grid and interactions.
/// </summary>
public class InventoryPanel : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int gridWidth = 4;
    public int gridHeight = 4;
    
    [Header("UI References")]
    public Transform inventoryGridParent;
    public GameObject inventorySlotPrefab;
    
    [Header("Gold Display")]
    public TextMeshProUGUI goldText;
    
    [Header("Current Setup")]
    public InventorySlot[] inventorySlots;
    
    private InventoryData inventoryData;
    private ICharacterService characterService;
    private int selectedSlot = -1;
    private int draggingSlotIndex = -1; // Track which slot is being dragged
    
    void Start()
    {
        // Try to initialize immediately
        if (!TryInitializeInventory())
        {
            // If it fails, subscribe to character load events and retry
            EventBus.Subscribe<CharacterLoadedEvent>(OnCharacterLoaded);
        }
        
        // Subscribe to inventory events for auto-refresh
        EventBus.Subscribe<ItemAddedEvent>(OnItemAdded);
        EventBus.Subscribe<ItemRemovedEvent>(OnItemRemoved);
        EventBus.Subscribe<CharacterGoldChangedEvent>(OnGoldChanged);
        
        // Initialize gold display
        UpdateGoldDisplay();
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        EventBus.Unsubscribe<CharacterLoadedEvent>(OnCharacterLoaded);
        EventBus.Unsubscribe<ItemAddedEvent>(OnItemAdded);
        EventBus.Unsubscribe<ItemRemovedEvent>(OnItemRemoved);
        EventBus.Unsubscribe<CharacterGoldChangedEvent>(OnGoldChanged);
    }
    
    void OnGoldChanged(CharacterGoldChangedEvent e)
    {
        UpdateGoldDisplay(e.newGold);
    }
    
    void UpdateGoldDisplay()
    {
        if (goldText == null) return;
        
        if (characterService == null)
            characterService = Services.Get<ICharacterService>();
        
        if (characterService != null)
        {
            UpdateGoldDisplay(characterService.GetGold());
        }
    }
    
    void UpdateGoldDisplay(int gold)
    {
        if (goldText != null)
            goldText.text = $"Gold: {gold}";
    }

    void OnCharacterLoaded(CharacterLoadedEvent e)
    {
        // Character is now loaded, try to initialize inventory panel
        if (inventoryData == null)
        {
            if (TryInitializeInventory())
            {
                // Successfully initialized, unsubscribe from this event
                EventBus.Unsubscribe<CharacterLoadedEvent>(OnCharacterLoaded);
            }
        }
    }
    
    void OnItemAdded(ItemAddedEvent e)
    {
        // If panel isn't initialized yet, try to initialize it
        if (inventoryData == null || inventorySlots == null)
        {
            if (TryInitializeInventory())
            {
                LoadEquipmentReferences();
            }
        }
        
        // Ensure we have a fresh reference to inventory data
        RefreshInventoryDataReference();
        RefreshDisplay();
    }
    
    void OnItemRemoved(ItemRemovedEvent e)
    {
        // Ensure we have a fresh reference to inventory data
        RefreshInventoryDataReference();
        RefreshDisplay();
    }
    
    /// <summary>
    /// Refresh the inventory data reference to ensure it's current
    /// </summary>
    void RefreshInventoryDataReference()
    {
        var characterService = Services.Get<ICharacterService>();
        if (characterService != null)
        {
            var freshInventoryData = characterService.GetInventoryData();
            if (freshInventoryData != null)
            {
                inventoryData = freshInventoryData;
            }
        }
    }
    
    
    /// <summary>
    /// Load item references (icons, equipment, etc.) for all items after deserialization
    /// </summary>
    void LoadEquipmentReferences()
    {
        if (inventoryData == null) return;
        inventoryData.LoadAllItemReferences();
    }
    
    bool TryInitializeInventory()
    {
        // Get inventory data from CharacterService
        var characterService = Services.Get<ICharacterService>();
        if (characterService != null)
        {
            inventoryData = characterService.GetInventoryData();
        }
        
        // If no inventory data exists, return false (will retry later)
        if (inventoryData == null)
        {
            return false;
        }
        
        // Ensure maxSlots matches our grid
        int targetSlots = gridWidth * gridHeight;
        if (inventoryData.maxSlots != targetSlots)
        {
            // Resize items array if needed
            if (inventoryData.items == null || inventoryData.items.Length != targetSlots)
            {
                InventoryItem[] oldItems = inventoryData.items;
                inventoryData.items = new InventoryItem[targetSlots];
                
                // Initialize empty slots
                for (int i = 0; i < targetSlots; i++)
                {
                    inventoryData.items[i] = new InventoryItem();
                }
                
                // Copy old items if they existed
                if (oldItems != null)
                {
                    int copyCount = Mathf.Min(oldItems.Length, targetSlots);
                    for (int i = 0; i < copyCount; i++)
                    {
                        if (oldItems[i] != null && !oldItems[i].IsEmpty())
                        {
                            inventoryData.items[i] = oldItems[i];
                        }
                    }
                }
            }
            inventoryData.maxSlots = targetSlots;
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

        // Load equipment references and refresh display
        LoadEquipmentReferences();
        RefreshDisplay();
        gameObject.SetActive(false);
        
        return true;
    }
    
    void CreateInventorySlots()
    {
        // Validate prefab exists
        if (inventorySlotPrefab == null)
        {
            Debug.LogError("[InventoryPanel] inventorySlotPrefab is null! Cannot create inventory slots.");
            return;
        }
        
        if (inventoryGridParent == null)
        {
            Debug.LogError("[InventoryPanel] inventoryGridParent is null! Cannot create inventory slots.");
            return;
        }
        
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
            GameObject slotObj = Instantiate(inventorySlotPrefab, inventoryGridParent);
            
            if (slotObj == null)
            {
                Debug.LogError($"[InventoryPanel] Failed to instantiate slot {i}!");
                continue;
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

    }
    
    public void RefreshDisplay()
    {
        // Ensure we have a fresh reference to inventory data
        RefreshInventoryDataReference();
        
        if (inventoryData == null || inventorySlots == null) return;
        
        // Update each slot
        int slotsToUpdate = Mathf.Min(inventorySlots.Length, inventoryData.maxSlots);
        for (int i = 0; i < slotsToUpdate; i++)
        {
            InventoryItem item = inventoryData.GetItem(i);
            inventorySlots[i].SetItem(item);
            inventorySlots[i].SetSelected(i == selectedSlot);
        }
    }
    
    void OnSlotClicked(int slotIndex)
    {
        // Handle double-click to use item
        if (selectedSlot == slotIndex)
        {
            // Double-click detected - try to use the item
            UseItemInSlot(slotIndex);
            selectedSlot = -1;
        }
        else
        {
            selectedSlot = slotIndex;
        }
        
        RefreshDisplay();
    }

    private void UseItemInSlot(int slotIndex)
    {
        if (inventoryData == null) return;
        
        InventoryItem item = inventoryData.GetItem(slotIndex);
        if (item == null || item.IsEmpty()) return;
        
        if (item.itemType == ItemType.Recipe || item.itemType == ItemType.Consumable)
        {
            var characterService = Services.Get<ICharacterService>();
            if (characterService != null)
            {
                bool success = characterService.UseItem(slotIndex);
                if (success)
                {
                    Debug.Log($"[InventoryPanel] Used item: {item.itemName}");
                    RefreshDisplay();
                }
            }
        }
    }
    
    public bool AddItem(InventoryItem item)
    {
        if (inventoryData == null) return false;
        
        InventoryData.AddItemResult result = inventoryData.AddItem(item);
        if (result.itemsAdded > 0)
        {
            RefreshDisplay();
        }
        
        return result.success;
    }
    
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
    
    public void OnDragStart(int slotIndex)
    {
        draggingSlotIndex = slotIndex;
    }
    
    public void OnDragEnd()
    {
        draggingSlotIndex = -1;
    }
    
    void OnSlotDragEnd(int fromSlot, int toSlot)
    {
        if (inventoryData == null) return;
        
        bool success = inventoryData.SwapItems(fromSlot, toSlot);
        
        if (success)
        {
            RefreshDisplay();
        }
    }

    public void TogglePanel()
    {
        if (gameObject != null)
        {
            bool newState = !gameObject.activeSelf;
            gameObject.SetActive(newState);
            
            // Update displays when opening
            if (newState)
            {
                RefreshDisplay();
            }

        }
    } 
}