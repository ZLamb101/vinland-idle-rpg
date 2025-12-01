using UnityEngine;

/// <summary>
/// Types of professions available in the game
/// </summary>
public enum ProfessionType
{
    // Gathering Professions
    Mining,         // Ore resources
    Herbalism,      // Herb resources
    Fishing,        // Fish resources
    Woodcutting,    // Wood resources (future)
    
    // Crafting Professions
    Cooking,        // Food and consumables
    Blacksmithing,  // Plate armor and weapons
    Leatherworking, // Leather armor
    Tailoring       // Cloth armor
}

/// <summary>
/// Helper methods for ProfessionType
/// </summary>
public static class ProfessionTypeExtensions
{
    /// <summary>
    /// Get the profession type that corresponds to a resource type
    /// </summary>
    public static ProfessionType GetProfessionForResourceType(ResourceType resourceType)
    {
        switch (resourceType)
        {
            case ResourceType.Ore:
                return ProfessionType.Mining;
            case ResourceType.Herbs:
                return ProfessionType.Herbalism;
            case ResourceType.Fish:
                return ProfessionType.Fishing;
            case ResourceType.Wood:
                return ProfessionType.Woodcutting;
            default:
                return ProfessionType.Mining;
        }
    }
    
    /// <summary>
    /// Get a display name for the profession
    /// </summary>
    public static string GetDisplayName(this ProfessionType profession)
    {
        switch (profession)
        {
            case ProfessionType.Mining:
                return "Mining";
            case ProfessionType.Herbalism:
                return "Herbalism";
            case ProfessionType.Fishing:
                return "Fishing";
            case ProfessionType.Woodcutting:
                return "Woodcutting";
            case ProfessionType.Cooking:
                return "Cooking";
            case ProfessionType.Blacksmithing:
                return "Blacksmithing";
            case ProfessionType.Leatherworking:
                return "Leatherworking";
            case ProfessionType.Tailoring:
                return "Tailoring";
            default:
                return profession.ToString();
        }
    }
}


