using System;

/// <summary>
/// Interface for zone management services
/// </summary>
public interface IZoneService
{
    // Events
    event Action<ZoneData> OnZoneChanged;
    event Action<QuestData[]> OnQuestsChanged;
    
    // Zone Navigation
    void LoadCurrentZone();
    void SetCurrentZone(ZoneData zone);
    bool CanGoToNextZone();
    bool CanGoToPreviousZone();
    void GoToNextZone();
    void GoToPreviousZone();
    
    // Getters
    ZoneData GetCurrentZone();
    int GetCurrentZoneIndex();
    ZoneData GetNextZone();
    ZoneData GetPreviousZone();
    string GetZoneNameForSlot(int characterSlotIndex);
    
    // Character-specific
    void SetDefaultZoneForSlot(int characterSlotIndex);
}

