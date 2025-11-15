using UnityEngine;

/// <summary>
/// Simple script to toggle the inventory panel on/off.
/// Attach to your Bag/Inventory Button.
/// </summary>
public class InventoryToggle : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject inventoryPanel;
    
    [Header("Settings")]
    public bool startInventoryOpen = false;
    
    void Start()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(startInventoryOpen);
        }
    }
    
    public void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            bool currentlyShowingInventory = inventoryPanel.activeSelf;
            
            inventoryPanel.SetActive(!currentlyShowingInventory);
            
            // Refresh inventory UI when opening inventory
            if (!currentlyShowingInventory)
            {
                InventoryPanel invPanel = inventoryPanel.GetComponent<InventoryPanel>();
                if (invPanel != null)
                {
                    invPanel.RefreshDisplay();
                }
            }
        }
        else
        {
        }
    }
    
    public void ShowInventoryQuick()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
    }
    
    public void ShowQuestPanel()
    {
        // Method kept for backwards compatibility so existing button bindings don't break.
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
    }
}
