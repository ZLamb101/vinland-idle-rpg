using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// UI panel that displays character professions (Mining, Herbalism, Fishing)
/// Shows level and XP progress for each profession.
/// Uses a prefab-based approach to dynamically generate profession entries.
/// </summary>
public class ProfessionPanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject professionPanel;
    
    [Header("Prefab Setup")]
    [Tooltip("Parent container where profession entries will be spawned")]
    public Transform professionContainer;
    
    [Tooltip("Prefab for individual profession entries")]
    public GameObject professionEntryPrefab;
    
    [Tooltip("Array of profession definitions to display")]
    public ProfessionDefinitionData[] professions;
    
    private IProfessionService professionService;
    private Dictionary<ProfessionType, ProfessionEntry> professionEntries = new Dictionary<ProfessionType, ProfessionEntry>();
    
    void Start()
    {
        // Get profession service
        if (Services.TryGet<IProfessionService>(out professionService))
        {
            // Subscribe to profession events
            EventBus.Subscribe<ProfessionXPGainedEvent>(OnProfessionXPGained);
            EventBus.Subscribe<ProfessionLevelUpEvent>(OnProfessionLevelUp);
            
            // Generate profession entries from prefab
            GenerateProfessionEntries();
            
            // Initial display
            RefreshDisplay();
        }
        else
        {
            Debug.LogWarning("[ProfessionPanel] ProfessionService not found!");
        }
        
        // Start with panel closed
        if (professionPanel != null)
        {
            professionPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Generate profession entry UI elements from the prefab
    /// </summary>
    private void GenerateProfessionEntries()
    {
        if (professionContainer == null)
        {
            Debug.LogError("[ProfessionPanel] Profession container is not assigned!");
            return;
        }
        
        if (professionEntryPrefab == null)
        {
            Debug.LogError("[ProfessionPanel] Profession entry prefab is not assigned!");
            return;
        }
        
        if (professions == null || professions.Length == 0)
        {
            Debug.LogWarning("[ProfessionPanel] No profession definitions assigned!");
            return;
        }
        
        // Clear existing entries (in case of re-initialization)
        foreach (Transform child in professionContainer)
        {
            Destroy(child.gameObject);
        }
        professionEntries.Clear();
        
        // Create one entry for each profession definition
        foreach (var professionDef in professions)
        {
            if (professionDef == null)
            {
                Debug.LogWarning("[ProfessionPanel] Null profession definition found, skipping...");
                continue;
            }
            
            // Instantiate prefab
            GameObject entryObj = Instantiate(professionEntryPrefab, professionContainer);
            entryObj.name = $"Entry_{professionDef.professionType}";
            
            // Get ProfessionEntry component
            ProfessionEntry entry = entryObj.GetComponent<ProfessionEntry>();
            if (entry == null)
            {
                Debug.LogError($"[ProfessionPanel] Profession entry prefab is missing ProfessionEntry component!");
                Destroy(entryObj);
                continue;
            }
            
            // Initialize the entry
            entry.Initialize(professionDef);
            
            // Store reference
            professionEntries[professionDef.professionType] = entry;
            
            Debug.Log($"[ProfessionPanel] Created entry for {professionDef.displayName}");
        }
    }
    
    void OnDestroy()
    {
        EventBus.Unsubscribe<ProfessionXPGainedEvent>(OnProfessionXPGained);
        EventBus.Unsubscribe<ProfessionLevelUpEvent>(OnProfessionLevelUp);
    }
    
    /// <summary>
    /// Handle profession XP gained event
    /// </summary>
    private void OnProfessionXPGained(ProfessionXPGainedEvent evt)
    {
        RefreshProfessionDisplay(evt.profession);
    }
    
    /// <summary>
    /// Handle profession level up event
    /// </summary>
    private void OnProfessionLevelUp(ProfessionLevelUpEvent evt)
    {
        RefreshProfessionDisplay(evt.profession);
        
        // Show notification
        EventBus.Publish(new NotificationEvent
        {
            message = $"{evt.profession.GetDisplayName()} reached level {evt.newLevel}!",
            type = NotificationType.Success
        });
    }
    
    /// <summary>
    /// Refresh the entire panel display
    /// </summary>
    public void RefreshDisplay()
    {
        if (professionService == null)
        {
            if (!Services.TryGet<IProfessionService>(out professionService))
            {
                return;
            }
        }
        
        // Update all profession entries
        foreach (var kvp in professionEntries)
        {
            RefreshProfessionDisplay(kvp.Key);
        }
    }
    
    /// <summary>
    /// Refresh display for a specific profession
    /// </summary>
    private void RefreshProfessionDisplay(ProfessionType profession)
    {
        if (professionService == null) return;
        
        // Check if we have an entry for this profession
        if (!professionEntries.ContainsKey(profession))
        {
            return;
        }
        
        // Get current profession data
        int level = professionService.GetProfessionLevel(profession);
        int currentXP = professionService.GetProfessionXP(profession);
        int xpRequired = professionService.GetProfessionXPRequired(profession);
        
        // Update the entry
        ProfessionEntry entry = professionEntries[profession];
        if (entry != null)
        {
            entry.UpdateDisplay(level, currentXP, xpRequired);
        }
    }
    
    /// <summary>
    /// Toggle the profession panel visibility
    /// </summary>
    public void TogglePanel()
    {
        if (professionPanel != null)
        {
            bool isActive = professionPanel.activeSelf;
            professionPanel.SetActive(!isActive);
            
            // Refresh display when opening
            if (!isActive)
            {
                RefreshDisplay();
            }
        }
    }
    
    /// <summary>
    /// Show the profession panel
    /// </summary>
    public void ShowPanel()
    {
        if (professionPanel != null)
        {
            professionPanel.SetActive(true);
            RefreshDisplay();
        }
    }
    
    /// <summary>
    /// Hide the profession panel
    /// </summary>
    public void HidePanel()
    {
        if (professionPanel != null)
        {
            professionPanel.SetActive(false);
        }
    }
}

