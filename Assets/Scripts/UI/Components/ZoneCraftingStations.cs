using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Automatically generates clickable crafting station buttons in a zone
/// based on the zone's CraftingStation data.
/// Attach this to your Zone UI panel.
/// </summary>
public class ZoneCraftingStations : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Parent transform where station buttons will be spawned")]
    public Transform stationsContainer;
    
    [Header("Prefab")]
    [Tooltip("Prefab for station buttons (should have Image + Button components)")]
    public GameObject stationButtonPrefab;
    
    private List<GameObject> activeStationButtons = new List<GameObject>();
    private IZoneService zoneService;
    private CraftingPanel cachedCraftingPanel; // Cache the panel reference
    
    void Start()
    {
        zoneService = Services.Get<IZoneService>();
        
        // Subscribe to zone change events
        EventBus.Subscribe<ZoneChangedEvent>(OnZoneChanged);
        
        // Generate stations for current zone
        RefreshStations();
    }
    
    void OnDestroy()
    {
        EventBus.Unsubscribe<ZoneChangedEvent>(OnZoneChanged);
    }
    
    private void OnZoneChanged(ZoneChangedEvent evt)
    {
        RefreshStations();
    }
    
    /// <summary>
    /// Refresh the station buttons for the current zone
    /// </summary>
    public void RefreshStations()
    {
        // Clear existing buttons
        ClearStations();
        
        if (zoneService == null)
        {
            Debug.LogWarning("[ZoneCraftingStations] ZoneService is null!");
            return;
        }
        
        if (stationsContainer == null)
        {
            Debug.LogWarning("[ZoneCraftingStations] StationsContainer is null! Assign it in Inspector.");
            return;
        }
        
        // Get current zone data
        ZoneData currentZone = zoneService.GetCurrentZone();
        if (currentZone == null)
        {
            Debug.LogWarning("[ZoneCraftingStations] Current zone is null!");
            return;
        }
        
        if (currentZone.craftingStations == null || currentZone.craftingStations.Length == 0)
        {
            return;
        }
        
        // Create a button for each crafting station
        foreach (CraftingStation station in currentZone.craftingStations)
        {
            if (station == null)
            {
                Debug.LogWarning("[ZoneCraftingStations] Null station in array, skipping");
                continue;
            }
            
            if (station.isAvailable)
            {
                CreateStationButton(station);
            }
        }
    }
    
    /// <summary>
    /// Create a clickable button for a crafting station
    /// </summary>
    private void CreateStationButton(CraftingStation station)
    {
        GameObject buttonObj;
        
        if (stationButtonPrefab != null)
        {
            // Use prefab if provided
            buttonObj = Instantiate(stationButtonPrefab, stationsContainer);
        }
        else
        {
            // Create button from scratch
            buttonObj = new GameObject($"{station.stationName}_Button");
            buttonObj.transform.SetParent(stationsContainer, false);
            
            // Add RectTransform
            RectTransform rt = buttonObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(64, 64); // Default size
            
            // Add Image
            Image img = buttonObj.AddComponent<Image>();
            img.sprite = GetStationIcon(station);
            
            // Add Button
            buttonObj.AddComponent<Button>();
        }
        
        // Position the button
        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = station.position;
        }
        
        // Set the icon
        Image image = buttonObj.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = GetStationIcon(station);
        }
        
        // Setup button click handler
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            ProfessionType professionType = station.professionType;
            button.onClick.AddListener(() => OnStationClicked(professionType));
        }
        
        // Add tooltip (optional)
        // You can add a tooltip component here if you have one
        
        activeStationButtons.Add(buttonObj);
    }
    
    /// <summary>
    /// Get the icon for a station (from zone data)
    /// </summary>
    private Sprite GetStationIcon(CraftingStation station)
    {
        if (station.stationIcon == null)
        {
            Debug.LogWarning($"[ZoneCraftingStations] Station '{station.stationName}' has no icon assigned in zone data!");
        }
        
        return station.stationIcon;
    }
    
    /// <summary>
    /// Handle station button click
    /// </summary>
    private void OnStationClicked(ProfessionType professionType)
    {
        Debug.Log($"Crafting station clicked: {professionType}");
        
        switch (professionType)
        {
            case ProfessionType.Cooking:
                OpenCraftingPanel();
                break;
                
            // Add more cases as you implement other professions
            // case ProfessionType.Alchemy:
            //     OpenAlchemyPanel();
            //     break;
            
            default:
                Debug.LogWarning($"No panel implemented for profession: {professionType}");
                break;
        }
    }
    
    /// <summary>
    /// Open the crafting panel
    /// </summary>
    private void OpenCraftingPanel()
    {
        // Use cached reference if available
        if (cachedCraftingPanel == null)
        {
            // FindAnyObjectByType with includeInactive = true to find disabled panels
            cachedCraftingPanel = FindAnyObjectByType<CraftingPanel>(FindObjectsInactive.Include);
        }
        
        if (cachedCraftingPanel != null)
        {
            cachedCraftingPanel.OpenPanel();
        }
        else
        {
            Debug.LogWarning("[ZoneCraftingStations] CraftingPanel not found in scene! Make sure you have a CraftingPanel GameObject with the CraftingPanel component.");
        }
    }
    
    /// <summary>
    /// Clear all station buttons
    /// </summary>
    private void ClearStations()
    {
        foreach (GameObject button in activeStationButtons)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }
        activeStationButtons.Clear();
    }
}
