using System;
using UnityEngine;

/// <summary>
/// Manages zone loading, navigation, and quest availability.
/// Singleton pattern for easy access across scenes.
/// </summary>
public class ZoneManager : MonoBehaviour, IZoneService
{
    public static ZoneManager Instance { get; private set; }
    
    [Header("Zone Settings")]
    public ZoneData[] allZones; // All zones in the game
    public ZoneData startingZone; // The first zone players start in
    
    private ZoneData currentZone;
    private int currentZoneIndex = 0;
    
    // Events
    public event Action<ZoneData> OnZoneChanged;
    public event Action<QuestData[]> OnQuestsChanged;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Register with service locator
        Services.Register<IZoneService>(this);
    }
    
    void Start()
    {
        // If zones aren't configured, try to load them from Resources
        if (allZones == null || allZones.Length == 0)
        {
            LoadZonesFromResources();
        }
        
        // Load current zone from save data or start with starting zone
        LoadCurrentZone();
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
            Debug.Log($"[ZoneManager] Loaded {allZones.Length} zones from Resources folder");
            
            // Set starting zone to first zone if not already set
            if (startingZone == null && allZones.Length > 0)
            {
                startingZone = allZones[0];
                Debug.Log($"[ZoneManager] Set starting zone to: {startingZone.zoneName}");
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
        
        UpdateZoneDisplay();
    }
    
    void UpdateZoneDisplay()
    {
        OnZoneChanged?.Invoke(currentZone);
        
        if (currentZone != null)
        {
            int playerLevel = CharacterManager.Instance != null ? CharacterManager.Instance.GetLevel() : 1;
            QuestData[] availableQuests = currentZone.GetAvailableQuests(playerLevel);
            OnQuestsChanged?.Invoke(availableQuests);
        }
    }
    
    public bool CanGoToNextZone()
    {
        if (currentZone == null || allZones == null || allZones.Length == 0 || currentZoneIndex >= allZones.Length - 1) return false;
        
        ZoneData nextZone = allZones[currentZoneIndex + 1];
        if (nextZone == null) return false;
        
        int playerLevel = CharacterManager.Instance != null ? CharacterManager.Instance.GetLevel() : 1;
        
        return nextZone.CanAccess(playerLevel, currentZone);
    }
    
    public bool CanGoToPreviousZone()
    {
        return currentZoneIndex > 0;
    }
    
    public void GoToNextZone()
    {
        if (!CanGoToNextZone() || allZones == null || currentZoneIndex + 1 >= allZones.Length) return;
        
        ZoneData nextZone = allZones[currentZoneIndex + 1];
        if (nextZone == null) return;
        
        SetCurrentZone(nextZone);
    }
    
    public void GoToPreviousZone()
    {
        if (!CanGoToPreviousZone() || allZones == null || currentZoneIndex - 1 < 0) return;
        
        ZoneData previousZone = allZones[currentZoneIndex - 1];
        if (previousZone == null) return;
        
        SetCurrentZone(previousZone);
    }
    
    public ZoneData GetCurrentZone() => currentZone;
    public int GetCurrentZoneIndex() => currentZoneIndex;
    public ZoneData GetNextZone() => (CanGoToNextZone() && allZones != null && currentZoneIndex + 1 < allZones.Length) ? allZones[currentZoneIndex + 1] : null;
    public ZoneData GetPreviousZone() => (CanGoToPreviousZone() && allZones != null && currentZoneIndex - 1 >= 0) ? allZones[currentZoneIndex - 1] : null;
    
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
