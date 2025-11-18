using UnityEngine;

/// <summary>
/// ScriptableObject that defines the visual and descriptive data for a profession.
/// Create instances via: Right-click in Project → Create → Vinland → Profession
/// </summary>
[CreateAssetMenu(fileName = "Profession_Mining", menuName = "Vinland/Profession", order = 7)]
public class ProfessionDefinitionData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Which profession this represents")]
    public ProfessionType professionType = ProfessionType.Mining;
    
    [Tooltip("Display name shown in UI")]
    public string displayName = "Mining";
    
    [Tooltip("Brief description of what this profession does")]
    [TextArea(2, 4)]
    public string description = "Extract valuable ores from rocks and minerals.";
    
    [Header("Visual")]
    [Tooltip("Icon representing this profession")]
    public Sprite icon;
    
    [Tooltip("Theme color for this profession (used for UI elements)")]
    public Color themeColor = new Color(0.6f, 0.6f, 0.6f); // Gray for mining
}


