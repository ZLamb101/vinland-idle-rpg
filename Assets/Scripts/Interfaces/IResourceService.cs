using System;

/// <summary>
/// Interface for resource gathering services
/// </summary>
public interface IResourceService
{
    // Events migrated to EventBus - see GameEvent.cs
    
    // Gathering Control
    bool StartGathering(ResourceData resource);
    bool StartGathering(ZoneData zone);
    void StopGathering();
    
    // Requirements
    bool CanGatherResource(ResourceData resource);
    
    // Getters
    bool IsGathering();
    ResourceData GetCurrentResource();
    float GetGatherProgress();
    float GetGatherRate();
}

