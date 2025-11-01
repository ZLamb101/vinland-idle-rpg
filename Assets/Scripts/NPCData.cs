using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a single dialogue line from an NPC
/// </summary>
[System.Serializable]
public class DialogueLine
{
    [TextArea(2, 4)]
    public string text = "Hello, traveler!";
}

/// <summary>
/// ScriptableObject that defines an NPC with dialogue and shop capabilities.
/// Create instances via: Right-click in Project → Create → Vinland → NPC
/// </summary>
[CreateAssetMenu(fileName = "New NPC", menuName = "Vinland/NPC", order = 5)]
public class NPCData : ScriptableObject
{
    [Header("NPC Info")]
    public string npcName = "Merchant";
    public Sprite npcSprite;
    
    [Header("Dialogue")]
    [Tooltip("Dialogue lines this NPC will say when talked to")]
    public DialogueLine[] dialogueLines = new DialogueLine[1];
    
    [Header("Shop")]
    [Tooltip("Does this NPC have a shop?")]
    public bool hasShop = false;
    
    // Future: ShopData can be added here later
    // public ShopData shopData;
}

