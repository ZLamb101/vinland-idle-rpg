using UnityEngine;

/// <summary>
/// Represents a named connection from one zone to another.
/// Used for extra pathways like "Hidden Cave", "Boat", etc.
/// </summary>
[System.Serializable]
public class ZoneConnection
{
    [Tooltip("Display name for this connection (e.g., 'Hidden Cave', 'Boat', 'Secret Tunnel')")]
    public string displayName;
    
    [Tooltip("The zone this connection leads to")]
    public ZoneData targetZone;
    
    [Header("Visual Properties")]
    [Tooltip("Icon/image for the connection button")]
    public Sprite icon;
    
    [Tooltip("Position of the connection button (absolute pixel coordinates - anchored position)")]
    public Vector2 position = new Vector2(0f, 0f);
    
    [Tooltip("Scale of the connection button (1.0 = normal size)")]
    public float scale = 1.0f;
}

