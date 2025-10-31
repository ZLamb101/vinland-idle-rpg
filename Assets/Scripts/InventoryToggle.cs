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
                InventoryUI inventoryUI = inventoryPanel.GetComponent<InventoryUI>();
                if (inventoryUI != null)
                {
                    inventoryUI.RefreshDisplay();
                    Debug.Log("Inventory UI refreshed when opened");
                }
                else
                {
                    Debug.LogWarning("InventoryUI component not found on inventory panel!");
                }
            }
            
            Debug.Log($"Inventory panel is now {(!currentlyShowingInventory ? "open" : "closed")}");
        }
        else
        {
            Debug.LogWarning("Inventory panel reference is missing!");
        }
    }
    
    public void ShowInventoryQuick()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        Debug.Log("Showing inventory panel");
    }
    
    public void ShowQuestPanel()
    {
        // Method kept for backwards compatibility so existing button bindings don't break.
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        Debug.Log("Hiding inventory panel (quest panel handling removed)");
    }
}
