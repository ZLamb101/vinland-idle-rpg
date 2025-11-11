using System;

/// <summary>
/// Interface for resource gathering services
/// </summary>
public interface IResourceService
{
    // Events
    event Action<bool> OnGatheringStateChanged;
    event Action<ResourceData> OnResourceChanged;
    event Action<float> OnGatherProgressChanged;
    event Action<int> OnItemsGathered;
    
    // Gathering Control
    bool StartGathering(ResourceData resource);
    bool StartGathering(ZoneData zone);
    void StopGathering();
    
    // Getters
    bool IsGathering();
    ResourceData GetCurrentResource();
    float GetGatherProgress();
    float GetGatherRate();
}

