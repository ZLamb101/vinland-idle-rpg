using UnityEngine;

/// <summary>
/// Simple script to toggle between quest panel and inventory panel.
/// Attach to your Bag Button.
/// </summary>
public class InventoryToggle : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject questPanel;
    public GameObject inventoryPanel;
    
    [Header("Settings")]
    public bool startWithQuestPanel = true;
    
    void Start()
    {
        // Initialize panel states
        if (questPanel != null) questPanel.SetActive(startWithQuestPanel);
        if (inventoryPanel != null) inventoryPanel.SetActive(!startWithQuestPanel);
    }
    
    public void ToggleInventory()
    {
        if (questPanel != null && inventoryPanel != null)
        {
            bool currentlyShowingInventory = inventoryPanel.activeSelf;
            
            // Toggle the panels
            questPanel.SetActive(currentlyShowingInventory);
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
            
            Debug.Log($"Switched to {(!currentlyShowingInventory ? "Inventory" : "Quest")} panel");
            Debug.Log($"Quest Panel Active: {questPanel.activeSelf}, Inventory Panel Active: {inventoryPanel.activeSelf}");
        }
        else
        {
            Debug.LogWarning($"Quest Panel: {questPanel != null}, Inventory Panel: {inventoryPanel != null}");
        }
    }
    
    public void ShowInventoryQuick()
    {
        if (questPanel != null) questPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        Debug.Log("Showing inventory panel");
    }
    
    public void ShowQuestPanel()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (questPanel != null) questPanel.SetActive(true);
        Debug.Log("Showing quest panel");
    }
}
