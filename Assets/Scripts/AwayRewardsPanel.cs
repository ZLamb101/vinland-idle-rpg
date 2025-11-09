using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

/// <summary>
/// UI panel that displays away/offline rewards when the player returns to the game.
/// </summary>
public class AwayRewardsPanel : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject panelObject; // The main panel GameObject
    public TextMeshProUGUI titleText; // "Welcome Back!" or similar
    public TextMeshProUGUI activityText; // "You were away for X minutes doing Y"
    public TextMeshProUGUI timeAwayText; // Formatted time away
    
    [Header("Rewards Display")]
    public TextMeshProUGUI xpText; // Text field for XP earned
    public TextMeshProUGUI goldText; // Text field for Gold earned
    
    [Header("Item Display")]
    [Tooltip("Array of text fields for displaying items (up to 10 items). Leave empty fields unused.")]
    public TextMeshProUGUI[] itemTextFields = new TextMeshProUGUI[10]; // Text fields for items (name: quantity)
    
    [Header("Buttons")]
    public Button collectButton; // Button to collect rewards and close panel
    
    private AwayRewards currentRewards;
    
    void Awake()
    {
        // Create panel GameObject if not assigned
        if (panelObject == null)
        {
            panelObject = gameObject;
        }
        
        // Ensure panelObject is active so it can be shown
        if (panelObject != null)
        {
            panelObject.SetActive(true);
        }
        
        // Setup collect button
        if (collectButton != null)
        {
            collectButton.onClick.AddListener(OnCollectClicked);
        }
        
        // Hide panel initially (will be shown when rewards are available)
        if (panelObject != null)
        {
            panelObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Show the away rewards panel with calculated rewards
    /// </summary>
    public void ShowRewards(AwayRewards rewards)
    {
        if (rewards == null)
        {
            return;
        }
        
        currentRewards = rewards;
        
        // Update title
        if (titleText != null)
        {
            titleText.text = "Welcome Back!";
        }
        
        // Update activity and time text
        if (activityText != null)
        {
            activityText.text = $"You were away for {FormatTimeAway(rewards.timeAway)} {rewards.activityName}";
        }
        
        if (timeAwayText != null)
        {
            timeAwayText.text = FormatTimeAway(rewards.timeAway);
        }
        
        // Clear and display rewards
        ClearRewards();
        DisplayRewards(rewards);
        
        // Show panel even if no rewards (for "doing Nothing" case)
        if (panelObject != null)
        {
            panelObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Format time away as a readable string
    /// </summary>
    string FormatTimeAway(TimeSpan timeAway)
    {
        if (timeAway.TotalDays >= 1)
        {
            return $"{(int)timeAway.TotalDays} day{(timeAway.TotalDays >= 2 ? "s" : "")}";
        }
        else if (timeAway.TotalHours >= 1)
        {
            return $"{(int)timeAway.TotalHours} hour{(timeAway.TotalHours >= 2 ? "s" : "")}";
        }
        else if (timeAway.TotalMinutes >= 1)
        {
            return $"{(int)timeAway.TotalMinutes} minute{(timeAway.TotalMinutes >= 2 ? "s" : "")}";
        }
        else
        {
            return $"{(int)timeAway.TotalSeconds} second{(timeAway.TotalSeconds >= 2 ? "s" : "")}";
        }
    }
    
    /// <summary>
    /// Display all rewards in text fields
    /// </summary>
    void DisplayRewards(AwayRewards rewards)
    {
        // Display XP
        if (xpText != null)
        {
            if (rewards.xpEarned > 0)
            {
                xpText.text = $"XP: {rewards.xpEarned}";
                xpText.gameObject.SetActive(true);
            }
            else
            {
                xpText.text = "";
                xpText.gameObject.SetActive(false);
            }
        }
        
        // Display Gold
        if (goldText != null)
        {
            if (rewards.goldEarned > 0)
            {
                goldText.text = $"Gold: {rewards.goldEarned}";
                goldText.gameObject.SetActive(true);
            }
            else
            {
                goldText.text = "";
                goldText.gameObject.SetActive(false);
            }
        }
        
        // Display items
        int itemIndex = 0;
        
        // Display mining rewards (items gathered)
        if (rewards.activityType == AwayActivityType.Mining)
        {
            foreach (var kvp in rewards.itemsGathered)
            {
                if (itemIndex < itemTextFields.Length && itemTextFields[itemIndex] != null)
                {
                    itemTextFields[itemIndex].text = $"{kvp.Key}: {kvp.Value}";
                    itemTextFields[itemIndex].gameObject.SetActive(true);
                    itemIndex++;
                }
            }
        }
        
        // Display fighting rewards (items dropped)
        if (rewards.activityType == AwayActivityType.Fighting)
        {
            // Display monsters killed count first
            if (rewards.monstersKilled > 0 && itemIndex < itemTextFields.Length && itemTextFields[itemIndex] != null)
            {
                itemTextFields[itemIndex].text = $"Monsters Killed: {rewards.monstersKilled}";
                itemTextFields[itemIndex].gameObject.SetActive(true);
                itemIndex++;
            }
            
            // Then display items dropped
            foreach (var kvp in rewards.itemsDropped)
            {
                if (itemIndex < itemTextFields.Length && itemTextFields[itemIndex] != null)
                {
                    itemTextFields[itemIndex].text = $"{kvp.Key}: {kvp.Value}";
                    itemTextFields[itemIndex].gameObject.SetActive(true);
                    itemIndex++;
                }
            }
        }
        
        // Show "No rewards" only if there are no XP, Gold, Items, or Monsters Killed
        bool hasAnyRewards = (rewards.xpEarned > 0 || rewards.goldEarned > 0 || 
                              rewards.itemsGathered.Count > 0 || rewards.itemsDropped.Count > 0 ||
                              rewards.monstersKilled > 0);
        
        if (!hasAnyRewards && itemIndex < itemTextFields.Length && itemTextFields[itemIndex] != null)
        {
            itemTextFields[itemIndex].text = "No rewards";
            itemTextFields[itemIndex].gameObject.SetActive(true);
            itemIndex++;
        }
        
        // Hide unused item text fields
        for (int i = itemIndex; i < itemTextFields.Length; i++)
        {
            if (itemTextFields[i] != null)
            {
                itemTextFields[i].text = "";
                itemTextFields[i].gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Clear all reward text fields
    /// </summary>
    void ClearRewards()
    {
        // Clear XP and Gold
        if (xpText != null)
        {
            xpText.text = "";
            xpText.gameObject.SetActive(false);
        }
        
        if (goldText != null)
        {
            goldText.text = "";
            goldText.gameObject.SetActive(false);
        }
        
        // Clear all item text fields
        for (int i = 0; i < itemTextFields.Length; i++)
        {
            if (itemTextFields[i] != null)
            {
                itemTextFields[i].text = "";
                itemTextFields[i].gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Called when collect button is clicked
    /// </summary>
    void OnCollectClicked()
    {
        if (currentRewards == null)
        {
            HidePanel();
            return;
        }
        
        // Apply rewards to character
        ApplyRewards(currentRewards);
        
        // Clear away state
        if (AwayActivityManager.Instance != null)
        {
            AwayActivityManager.Instance.ClearAwayState();
        }
        
        // Hide panel
        HidePanel();
    }
    
    /// <summary>
    /// Apply rewards to the character
    /// </summary>
    void ApplyRewards(AwayRewards rewards)
    {
        if (CharacterManager.Instance == null)
        {
            return;
        }
        
        // Apply mining rewards (items)
        if (rewards.activityType == AwayActivityType.Mining)
        {
            foreach (var kvp in rewards.itemsGathered)
            {
                // Find the item by name and add to inventory
                ItemData itemData = FindItemByName(kvp.Key);
                if (itemData != null)
                {
                    InventoryItem item = itemData.CreateInventoryItem(kvp.Value);
                    InventoryData.AddItemResult result = CharacterManager.Instance.AddItemToInventoryDetailed(item);
                    
                    if (!result.success && result.itemsRemaining > 0)
                    {
                        Debug.LogWarning($"[AwayRewards] Inventory full! Could only add {result.itemsAdded} of {kvp.Value} {kvp.Key}. {result.itemsRemaining} items were lost.");
                    }
                }
            }
        }
        
        // Apply fighting rewards (XP, gold, items)
        if (rewards.activityType == AwayActivityType.Fighting)
        {
            if (rewards.xpEarned > 0)
            {
                CharacterManager.Instance.AddXP(rewards.xpEarned);
            }
            
            if (rewards.goldEarned > 0)
            {
                CharacterManager.Instance.AddGold(rewards.goldEarned);
            }
            
            foreach (var kvp in rewards.itemsDropped)
            {
                ItemData itemData = FindItemByName(kvp.Key);
                if (itemData != null)
                {
                    InventoryItem item = itemData.CreateInventoryItem(kvp.Value);
                    InventoryData.AddItemResult result = CharacterManager.Instance.AddItemToInventoryDetailed(item);
                    
                    if (!result.success && result.itemsRemaining > 0)
                    {
                        Debug.LogWarning($"[AwayRewards] Inventory full! Could only add {result.itemsAdded} of {kvp.Value} {kvp.Key}. {result.itemsRemaining} items were lost.");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Find an ItemData ScriptableObject by name
    /// </summary>
    ItemData FindItemByName(string itemName)
    {
        ItemData[] allItems = Resources.LoadAll<ItemData>("");
        foreach (var item in allItems)
        {
            if (item.name == itemName || item.itemName == itemName)
            {
                return item;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Hide the panel
    /// </summary>
    void HidePanel()
    {
        if (panelObject != null)
        {
            panelObject.SetActive(false);
        }
        currentRewards = null;
    }
}

