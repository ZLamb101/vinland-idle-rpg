using UnityEngine;

/// <summary>
/// Represents a crafting station in a zone (cooking pot, anvil, cauldron, etc.)
/// </summary>
[System.Serializable]
public class CraftingStation
{
    [Tooltip("Type of crafting profession this station supports")]
    public ProfessionType professionType;
    
    [Tooltip("Display name of the station (e.g., 'Cooking Pot', 'Anvil', 'Cauldron')")]
    public string stationName;
    
    [Tooltip("Position of the station button/image in the zone (pixel coordinates)")]
    public Vector2 position;
    
    [Tooltip("Icon/sprite for this station (optional)")]
    public Sprite stationIcon;
    
    [Tooltip("Is this station currently available/unlocked?")]
    public bool isAvailable = true;
}
