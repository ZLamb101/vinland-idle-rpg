using System;
using UnityEngine;

/// <summary>
/// Manages zone loading, navigation, and quest availability.
/// Singleton pattern for easy access across scenes.
/// </summary>
public class ZoneManager : MonoBehaviour, IZoneService
{
    
    [Header("Zone Settings")]
    public ZoneData[] allZones; // All zones in the game
    public ZoneData startingZone; // The first zone players start in
    
    private ZoneData currentZone;
    private int currentZoneIndex = 0;
    
    // Events migrated to EventBus - see GameEvent.cs for event types
    
    void Awake()
    {
        if (Services.IsRegistered<IZoneService>())
        {
            Debug.LogWarning("[ZoneManager] Service already registered. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        
        // Register with service locator
        Services.Register<IZoneService>(this);
    }

    void OnDestroy()
    {
        Services.Unregister<IZoneService>();
    }
    
    void Start()
    {
        // If zones aren't configured, try to load them from Resources
        if (allZones == null || allZones.Length == 0)
        {
            LoadZonesFromResources();
        }
    }
    
    /// <summary>
    /// Load all zones from Resources folder if not already configured.
    /// This allows ZoneManager to work even when created programmatically.
    /// </summary>
    void LoadZonesFromResources()
    {
        ZoneData[] loadedZones = Resources.LoadAll<ZoneData>("");
        
        if (loadedZones != null && loadedZones.Length > 0)
        {
            allZones = loadedZones;
            
            // Set starting zone to first zone if not already set
            if (startingZone == null && allZones.Length > 0)
            {
                startingZone = allZones[0];
            }
        }
        else
        {
            Debug.LogWarning("[ZoneManager] No zones found in Resources folder. Please either:");
            Debug.LogWarning("1. Place ZoneData ScriptableObjects in a Resources folder, OR");
            Debug.LogWarning("2. Create a ZoneManager GameObject in your scene with zones assigned in the Inspector.");
        }
    }
    
    /// <summary>
    /// Load the zone for the current character slot.
    /// Called automatically on Start, but can also be called manually after character load.
    /// </summary>
    public void LoadCurrentZone()
    {
        // Check if zones are configured (allZones array is assigned)
        if (allZones == null || allZones.Length == 0)
        {
            Debug.LogWarning("[ZoneManager] allZones array is not configured. Cannot load zone.");
            return;
        }
        
        // Try to load zone for current character slot
        int currentSlot = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        int savedIndex = -1;
        
        if (currentSlot >= 0)
        {
            // Try to load per-character zone
            if (PlayerPrefs.HasKey($"Character_{currentSlot}_ZoneIndex"))
            {
                savedIndex = PlayerPrefs.GetInt($"Character_{currentSlot}_ZoneIndex", -1);
            }
        }
        
        // Fallback to global zone if no per-character zone found
        if (savedIndex < 0 && PlayerPrefs.HasKey("CurrentZoneIndex"))
        {
            savedIndex = PlayerPrefs.GetInt("CurrentZoneIndex", 0);
        }
        
        if (savedIndex >= 0 && savedIndex < allZones.Length && allZones[savedIndex] != null)
        {
            currentZoneIndex = savedIndex;
            currentZone = allZones[currentZoneIndex];
        }
        else if (startingZone != null)
        {
            SetCurrentZone(startingZone);
        }
        else
        {
            // Default to zone 1-1 (index 0) if no saved zone and no starting zone configured
            if (allZones.Length > 0 && allZones[0] != null)
            {
                SetCurrentZone(allZones[0]);
            }
            else
            {
                Debug.LogWarning("[ZoneManager] No starting zone configured and no saved zone found.");
            }
        }
        
        UpdateZoneDisplay();
    }
    
    public void SetCurrentZone(ZoneData zone)
    {
        if (zone == null) return;
        
        // Check if zones are configured
        if (allZones == null || allZones.Length == 0)
        {
            Debug.LogWarning("[ZoneManager] allZones array is not configured. Cannot set zone.");
            return;
        }
        
        ZoneData oldZone = currentZone; // Track old zone before changing
        currentZone = zone;
        
        // Find the index of this zone
        for (int i = 0; i < allZones.Length; i++)
        {
            if (allZones[i] == zone)
            {
                currentZoneIndex = i;
                break;
            }
        }
        
        // Save current zone per character slot
        int currentSlot = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        if (currentSlot >= 0)
        {
            PlayerPrefs.SetInt($"Character_{currentSlot}_ZoneIndex", currentZoneIndex);
        }
        
        // Also save globally as fallback
        PlayerPrefs.SetInt("CurrentZoneIndex", currentZoneIndex);
        PlayerPrefs.Save();
        
        UpdateZoneDisplay(oldZone);
    }
    
    void UpdateZoneDisplay(ZoneData oldZone = null)
    {
        EventBus.Publish(new ZoneChangedEvent { oldZone = oldZone, newZone = currentZone });
        
        if (currentZone != null)
        {
            int playerLevel = 1; // Default level
            if (Services.TryGet<ICharacterService>(out var characterService))
            {
                playerLevel = characterService.GetLevel();
            }
            
            QuestData[] availableQuests = currentZone.GetAvailableQuests(playerLevel);
            EventBus.Publish(new QuestsUpdatedEvent { availableQuests = availableQuests });
        }
    }
    
    public bool CanGoToNextZone()
    {
        if (currentZone == null) return false;
        
        // Use the nextZone field from current zone instead of array order
        if (currentZone.nextZone == null) return false;
        
        int playerLevel = 1; // Default level
        if (Services.TryGet<ICharacterService>(out var characterService))
        {
            playerLevel = characterService.GetLevel();
        }
        
        return currentZone.nextZone.CanAccess(playerLevel, currentZone);
    }
    
    public bool CanGoToPreviousZone()
    {
        return currentZone != null && currentZone.previousZone != null;
    }
    
    public void GoToNextZone()
    {
        if (!CanGoToNextZone() || currentZone == null || currentZone.nextZone == null) return;
        
        // Use the nextZone field from current zone
        SetCurrentZone(currentZone.nextZone);
    }
    
    public void GoToPreviousZone()
    {
        if (!CanGoToPreviousZone() || currentZone == null || currentZone.previousZone == null) return;
        
        // Use the previousZone field from current zone
        SetCurrentZone(currentZone.previousZone);
    }
    
    public ZoneData GetCurrentZone() => currentZone;
    public int GetCurrentZoneIndex() => currentZoneIndex;
    public ZoneData GetNextZone() => (currentZone != null && currentZone.nextZone != null && CanGoToNextZone()) ? currentZone.nextZone : null;
    public ZoneData GetPreviousZone() => (currentZone != null && currentZone.previousZone != null) ? currentZone.previousZone : null;
    
    /// <summary>
    /// Get all extra connections available from the current zone.
    /// Returns an empty list if no connections exist.
    /// </summary>
    public System.Collections.Generic.List<ZoneConnection> GetExtraConnections()
    {
        if (currentZone == null || currentZone.extraConnections == null)
        {
            return new System.Collections.Generic.List<ZoneConnection>();
        }
        return currentZone.extraConnections;
    }
    
    /// <summary>
    /// Check if navigation to a specific extra connection is possible.
    /// </summary>
    /// <param name="index">Index of the connection in the extraConnections list</param>
    /// <returns>True if navigation is possible, false otherwise</returns>
    public bool CanNavigateToConnection(int index)
    {
        if (currentZone == null || currentZone.extraConnections == null)
        {
            return false;
        }
        
        if (index < 0 || index >= currentZone.extraConnections.Count)
        {
            return false;
        }
        
        ZoneConnection connection = currentZone.extraConnections[index];
        if (connection == null || connection.targetZone == null)
        {
            return false;
        }
        
        // Check if player meets requirements for target zone
        int playerLevel = 1;
        if (Services.TryGet<ICharacterService>(out var characterService))
        {
            playerLevel = characterService.GetLevel();
        }
        
        return connection.targetZone.CanAccess(playerLevel, currentZone);
    }
    
    /// <summary>
    /// Navigate to an extra connection by index.
    /// </summary>
    /// <param name="index">Index of the connection in the extraConnections list</param>
    public void NavigateToConnection(int index)
    {
        if (!CanNavigateToConnection(index)) return;
        
        ZoneConnection connection = currentZone.extraConnections[index];
        SetCurrentZone(connection.targetZone);
    }
    
    /// <summary>
    /// Navigate to an extra connection by display name.
    /// This is a convenience method for name-based navigation.
    /// </summary>
    /// <param name="displayName">The display name of the connection (e.g., "Hidden Cave")</param>
    /// <returns>True if navigation was successful, false otherwise</returns>
    public bool NavigateToConnection(string displayName)
    {
        if (currentZone == null || currentZone.extraConnections == null)
        {
            return false;
        }
        
        for (int i = 0; i < currentZone.extraConnections.Count; i++)
        {
            ZoneConnection connection = currentZone.extraConnections[i];
            if (connection != null && connection.displayName == displayName)
            {
                if (CanNavigateToConnection(i))
                {
                    NavigateToConnection(i);
                    return true;
                }
                return false;
            }
        }
        
        Debug.LogWarning($"[ZoneManager] No connection found with name '{displayName}' in zone {currentZone.zoneName}");
        return false;
    }
    
    /// <summary>
    /// Set default zone (zone 1-1, index 0) for a character slot.
    /// Used when creating new characters or when a character has no saved zone.
    /// </summary>
    public void SetDefaultZoneForSlot(int characterSlotIndex)
    {
        if (characterSlotIndex < 0)
        {
            return;
        }
        
        // Ensure zones are loaded
        if (allZones == null || allZones.Length == 0)
        {
            LoadZonesFromResources();
        }
        
        // Default to zone 1-1 (index 0)
        if (allZones != null && allZones.Length > 0 && allZones[0] != null)
        {
            PlayerPrefs.SetInt($"Character_{characterSlotIndex}_ZoneIndex", 0);
            PlayerPrefs.Save();
        }
        else
        {
            Debug.LogWarning("[ZoneManager] Cannot set default zone - no zones available.");
        }
    }
    
    /// <summary>
    /// Get the zone name for a specific character slot.
    /// Returns empty string if no zone found or slot is invalid.
    /// </summary>
    public string GetZoneNameForSlot(int characterSlotIndex)
    {
        if (characterSlotIndex < 0)
        {
            return "";
        }
        
        // Ensure zones are loaded (in case this is called before Start())
        if (allZones == null || allZones.Length == 0)
        {
            LoadZonesFromResources();
        }
        
        // Check if zones are configured
        if (allZones == null || allZones.Length == 0)
        {
            // Return starting zone name if available
            if (startingZone != null)
            {
                return startingZone.zoneName;
            }
            return "";
        }
        
        // Try to load zone index for this character slot
        int savedIndex = -1;
        if (PlayerPrefs.HasKey($"Character_{characterSlotIndex}_ZoneIndex"))
        {
            savedIndex = PlayerPrefs.GetInt($"Character_{characterSlotIndex}_ZoneIndex", -1);
        }
        
        // Fallback to global zone if no per-character zone found
        if (savedIndex < 0 && PlayerPrefs.HasKey("CurrentZoneIndex"))
        {
            savedIndex = PlayerPrefs.GetInt("CurrentZoneIndex", -1);
        }
        
        // Return zone name if valid index found
        if (savedIndex >= 0 && savedIndex < allZones.Length && allZones[savedIndex] != null)
        {
            return allZones[savedIndex].zoneName;
        }
        
        // Default to zone 1-1 (index 0) if no saved zone
        if (allZones.Length > 0 && allZones[0] != null)
        {
            // Set default zone for this slot if it doesn't have one
            SetDefaultZoneForSlot(characterSlotIndex);
            return allZones[0].zoneName;
        }
        
        // Return starting zone name if no zones array but starting zone exists
        if (startingZone != null)
        {
            return startingZone.zoneName;
        }
        
        return "";
    }
}
