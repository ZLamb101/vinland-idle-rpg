using UnityEngine;

/// <summary>
/// Resource types available in the game
/// </summary>
public enum ResourceType
{
    Ore,    // Iron ore, Copper ore, etc.
    Wood,   // Oak tree, Pine tree, etc.
    Herbs,  // Various herbs
    Fish    // Salmon, Trout, etc.
}

/// <summary>
/// ScriptableObject that defines a resource that can be gathered.
/// Create instances via: Right-click in Project → Create → Vinland → Resource
/// </summary>
[CreateAssetMenu(fileName = "New Resource", menuName = "Vinland/Resource", order = 4)]
public class ResourceData : ScriptableObject
{
    [Header("Resource Info")]
    public string resourceName = "Iron Ore";
    public ResourceType resourceType = ResourceType.Ore;
    public Sprite resourceIcon;
    
    [Header("Gathering Stats")]
    [Tooltip("Items gathered per second")]
    public float gatherRate = 1f; // e.g., 1.0 = 1 item per second, 2.5 = 2.5 items per second
    
    [Header("Rewards")]
    [Tooltip("Item that is given when gathering this resource")]
    public ItemData gatheredItem;
    
    [Tooltip("Amount of items given per gather cycle")]
    public int itemsPerGather = 1;
}

