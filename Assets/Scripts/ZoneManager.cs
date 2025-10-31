using UnityEngine;

/// <summary>
/// Manages zone loading, navigation, and quest availability.
/// Singleton pattern for easy access across scenes.
/// </summary>
public class ZoneManager : MonoBehaviour
{
    public static ZoneManager Instance { get; private set; }
    
    [Header("Zone Settings")]
    public ZoneData[] allZones; // All zones in the game
    public ZoneData startingZone; // The first zone players start in
    
    private ZoneData currentZone;
    private int currentZoneIndex = 0;
    
    // Events
    public System.Action<ZoneData> OnZoneChanged;
    public System.Action<QuestData[]> OnQuestsChanged;
    
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
    }
    
    void Start()
    {
        // Load current zone from save data or start with starting zone
        LoadCurrentZone();
    }
    
    void LoadCurrentZone()
    {
        // Try to load from save data
        if (PlayerPrefs.HasKey("CurrentZoneIndex"))
        {
            int savedIndex = PlayerPrefs.GetInt("CurrentZoneIndex", 0);
            if (savedIndex < allZones.Length)
            {
                currentZoneIndex = savedIndex;
                currentZone = allZones[currentZoneIndex];
            }
            else
            {
                SetCurrentZone(startingZone);
            }
        }
        else
        {
            SetCurrentZone(startingZone);
        }
        
        UpdateZoneDisplay();
    }
    
    public void SetCurrentZone(ZoneData zone)
    {
        if (zone == null) return;
        
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
        
        // Save current zone
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
        if (currentZone == null || currentZoneIndex >= allZones.Length - 1) return false;
        
        ZoneData nextZone = allZones[currentZoneIndex + 1];
        int playerLevel = CharacterManager.Instance != null ? CharacterManager.Instance.GetLevel() : 1;
        
        return nextZone.CanAccess(playerLevel, currentZone);
    }
    
    public bool CanGoToPreviousZone()
    {
        return currentZoneIndex > 0;
    }
    
    public void GoToNextZone()
    {
        if (!CanGoToNextZone()) return;
        
        ZoneData nextZone = allZones[currentZoneIndex + 1];
        SetCurrentZone(nextZone);
    }
    
    public void GoToPreviousZone()
    {
        if (!CanGoToPreviousZone()) return;
        
        ZoneData previousZone = allZones[currentZoneIndex - 1];
        SetCurrentZone(previousZone);
    }
    
    public ZoneData GetCurrentZone() => currentZone;
    public int GetCurrentZoneIndex() => currentZoneIndex;
    public ZoneData GetNextZone() => CanGoToNextZone() ? allZones[currentZoneIndex + 1] : null;
    public ZoneData GetPreviousZone() => CanGoToPreviousZone() ? allZones[currentZoneIndex - 1] : null;
}
