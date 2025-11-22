using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BankPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject slotPrefab;
    public Transform slotsContainer;
    public Button closeButton;
    public Button depositAllButton;
    
    private List<InventorySlot> slots = new List<InventorySlot>();
    private IBankService bankService;
    private ICharacterService characterService;
    
    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }
        
        if (depositAllButton != null)
        {
            depositAllButton.onClick.AddListener(OnDepositAllClicked);
        }
        
        // Initialize services with proper error handling
        StartCoroutine(InitializeWhenReady());
        
        // Start hidden
        gameObject.SetActive(false);
    }
    
    private System.Collections.IEnumerator InitializeWhenReady()
    {
        // Wait until services are registered
        while (!Services.IsRegistered<IBankService>() || !Services.IsRegistered<ICharacterService>())
        {
            yield return null;
        }
        
        // Now safely get services
        if (Services.TryGet<IBankService>(out bankService))
        {
            bankService.OnBankChanged += RefreshUI;
        }
        else
        {
            Debug.LogError("[BankPanel] Failed to get IBankService!");
        }
        
        if (!Services.TryGet<ICharacterService>(out characterService))
        {
            Debug.LogError("[BankPanel] Failed to get ICharacterService!");
        }
    }
    
    private void OnDestroy()
    {
        if (bankService != null)
        {
            bankService.OnBankChanged -= RefreshUI;
        }
    }
    
    private void InitializeSlots()
    {
        if (slotsContainer == null)
        {
            Debug.LogError("[BankPanel] Slots Container is null!");
            return;
        }
        
        if (slotPrefab == null)
        {
            Debug.LogError("[BankPanel] Slot Prefab is null!");
            return;
        }

        // Clear existing
        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }
        slots.Clear();
        
        if (bankService == null)
        {
             if (!Services.TryGet<IBankService>(out bankService))
             {
                 Debug.LogError("[BankPanel] Failed to get BankService!");
                 return;
             }
        }
        
        BankData data = bankService.GetBankData();
        if (data == null)
        {
            Debug.LogError("[BankPanel] BankData is null!");
            return;
        }
        
        
        for (int i = 0; i < data.maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            
            if (slot != null)
            {
                int index = i; // Capture for lambda
                slot.Initialize(index);
                
                // Setup click and drag handling
                slot.OnSlotClicked += OnSlotLeftClicked;
                slot.OnSlotRightClicked += OnSlotRightClicked;
                slot.OnSlotDragEnd += OnSlotDragEnd;
                
                // Set bank panel reference so the slot knows it's a bank slot
                slot.SetBankPanel(this);
                
                slots.Add(slot);
            }
            else
            {
                Debug.LogError($"[BankPanel] Slot prefab does not have InventorySlot component!");
            }
        }
    }
    
    public void Show()
    {
        gameObject.SetActive(true);
        
        // Initialize slots if not already done
        if (slots.Count == 0)
        {
            InitializeSlots();
        }
        
        RefreshUI();
    }
    
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    public void Toggle()
    {
        if (gameObject.activeSelf) Hide();
        else Show();
    }
    
    private void RefreshUI()
    {
        if (bankService == null) return;
        
        BankData data = bankService.GetBankData();
        if (data == null) return;
        
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < data.items.Length)
            {
                slots[i].SetItem(data.items[i]);
            }
            else
            {
                slots[i].SetItem(null);
            }
        }
    }
    
    private void OnSlotLeftClicked(int index)
    {
        // Left-click - just select or show info (no action for now)
        // Future: Could show item details or allow selection
    }
    
    private void OnSlotRightClicked(int index)
    {
        // Right-click - withdraw item (max stack size, typically 99)
        WithdrawFromBank(index, 99);
    }
    
    private void WithdrawFromBank(int index, int maxQuantity = 99)
    {
        if (bankService == null || characterService == null) return;
        
        BankData data = bankService.GetBankData();
        if (data == null || data.items == null)
        {
            Debug.LogError("[BankPanel] Bank data is null!");
            return;
        }
        
        if (index < 0 || index >= data.items.Length) return;
        
        InventoryItem item = data.items[index];
        if (item != null && !item.IsEmpty())
        {
            // Withdraw up to maxQuantity (or all if less than maxQuantity)
            int quantityToWithdraw = Mathf.Min(item.quantity, maxQuantity);
            
            CharacterData charData = characterService.GetCharacterData();
            if (charData == null || charData.inventory == null)
            {
                Debug.LogError("[BankPanel] Character data or inventory is null!");
                return;
            }
            
            InventoryData playerInventory = charData.inventory;
            bool success = bankService.WithdrawItem(index, quantityToWithdraw, playerInventory);
            
            if (!success)
            {
                Debug.LogWarning("[BankPanel] Failed to withdraw item - inventory may be full");
            }
        }
    }
    
    private void OnSlotDragEnd(int fromSlot, int toSlot)
    {
        // Handle drag-and-drop within bank to reorder items
        if (bankService == null) return;
        
        bool success = bankService.SwapBankItems(fromSlot, toSlot);
        
        if (success)
        {
            RefreshUI();
        }
    }
    
    private void OnDepositAllClicked()
    {
        if (bankService == null || characterService == null) return;
        
        CharacterData charData = characterService.GetCharacterData();
        if (charData == null || charData.inventory == null)
        {
            Debug.LogError("[BankPanel] Character data or inventory is null!");
            return;
        }
        
        InventoryData playerInventory = charData.inventory;
        bankService.DepositAllItems(playerInventory);
        
        // Refresh UI is handled by event
    }
    
    // Drag and drop support methods (called by InventorySlot during drag operations)
    public void OnDragStart(int slotIndex)
    {
        // Could add visual feedback here
    }
    
    public void OnDragEnd()
    {
        // Could clear visual feedback here
    }
    
    // Public accessor for checking if bank is open
    public bool IsOpen()
    {
        return gameObject.activeSelf;
    }
}
