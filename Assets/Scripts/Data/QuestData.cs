using UnityEngine;
using System.Collections.Generic;

public enum QuestObjectiveType
{
    Kill,
    Collect,
    Talk,
    Find
}

[System.Serializable]
public class QuestObjective
{
    public QuestObjectiveType type;
    public string targetID; // Name of Monster, Item, or NPC
    public int amount = 1;
    public string description; // Override description, e.g. "Defeat 8 Boars"
}

/// <summary>
/// ScriptableObject that defines a quest with all its properties.
/// Create instances via: Right-click in Project → Create → Vinland → Quest
/// </summary>
[CreateAssetMenu(fileName = "New Quest", menuName = "Vinland/Quest", order = 1)]
public class QuestData : ScriptableObject
{
    [Header("Quest Identity")]
    public string id; // Unique ID for the quest
    public string questName = "New Quest";
    [TextArea(2, 4)]
    public string description = "Quest description.";
    
    [Header("Dialogue")]
    [Tooltip("Optional: Custom dialogue pages shown when offering the quest. Leave empty to use description only.")]
    [TextArea(3, 6)]
    public List<string> dialoguePages = new List<string>();
    
    [Header("Requirements")]
    public int levelRequired = 1;
    public QuestData prerequisiteQuest; // Optional: Must complete this quest first
    
    [Header("Objectives")]
    public List<QuestObjective> objectives = new List<QuestObjective>();
    public NPCData turnInNPC; // NPC to turn the quest in to
    
    [Header("Rewards")]
    public int xpReward = 100;
    public int goldReward = 50;
    public ItemData itemReward; // Optional item reward
    public int itemRewardQuantity = 1;

    public void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString();
        }
    }
}
