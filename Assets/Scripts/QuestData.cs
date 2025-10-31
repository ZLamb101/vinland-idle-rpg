using UnityEngine;

/// <summary>
/// ScriptableObject that defines a quest with all its properties.
/// Create instances via: Right-click in Project → Create → Vinland → Quest
/// </summary>
[CreateAssetMenu(fileName = "New Quest", menuName = "Vinland/Quest", order = 1)]
public class QuestData : ScriptableObject
{
    [Header("Quest Info")]
    public string questName = "Gather Resources";
    [TextArea(2, 4)]
    public string description = "A simple quest to gather resources.";
    
    [Header("Requirements")]
    public int levelRequired = 1;
    
    [Header("Quest Duration")]
    public float duration = 5f; // Time in seconds to complete
    
    [Header("Rewards")]
    public int xpReward = 10;
    public int goldReward = 5;
    public ItemData itemReward; // Optional item reward
    public int itemRewardQuantity = 1;
    
    [Header("Visual (Optional)")]
    public Sprite questIcon;
    public Color questColor = Color.white;
    
    // You can add more properties later like:
    // public bool isRepeatable = true;
    // public QuestData[] prerequisiteQuests;
    // public ItemData[] itemRewards;
}

