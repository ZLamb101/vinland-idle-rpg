using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component for displaying a single monster panel with image and name.
/// This component is used as a prefab that can be instantiated multiple times.
/// </summary>
public class MonsterPanel : MonoBehaviour
{
    [Header("Monster Display")]
    public Image monsterImage; // Image showing the monster sprite
    public TextMeshProUGUI monsterNameText; // Text showing monster name
    
    [Header("Combat")]
    [Tooltip("Optional fight button on this monster panel. If not assigned, the panel itself will be clickable.")]
    public Button fightButton; // Button to start combat (optional - can click panel or button)
    [Tooltip("Mob count selector. If not assigned, will try to find it in the scene.")]
    public MobCountSelector mobCountSelector; // Selector for number of mobs to fight
    
    private MonsterData monsterData;
    private RectTransform rectTransform;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Find mob count selector if not assigned
        if (mobCountSelector == null)
        {
            mobCountSelector = FindObjectOfType<MobCountSelector>();
        }
        
        // Setup fight button if assigned
        if (fightButton != null)
        {
            fightButton.onClick.AddListener(OnFightClicked);
        }
        else
        {
            // Fallback: If no button assigned, make the whole panel clickable
            Button panelButton = GetComponent<Button>();
            if (panelButton == null)
            {
                panelButton = gameObject.AddComponent<Button>();
            }
            panelButton.onClick.AddListener(OnFightClicked);
        }
    }
    
    /// <summary>
    /// Initialize this monster panel with monster data and position
    /// </summary>
    public void Initialize(MonsterData monster, Vector2 position)
    {
        monsterData = monster;
        
        if (monster == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        // Set position (use absolute pixel coordinates directly)
        if (rectTransform != null)
        {
            // Use position directly as anchored position (absolute pixel coordinates)
            rectTransform.anchoredPosition = position;
        }
        
        // Update monster image
        if (monsterImage != null)
        {
            monsterImage.gameObject.SetActive(true);
            if (monster.monsterSprite != null)
            {
                monsterImage.sprite = monster.monsterSprite;
            }
        }
        
        // Update monster name text
        if (monsterNameText != null)
        {
            monsterNameText.text = monster.monsterName;
            monsterNameText.gameObject.SetActive(true);
        }
        
        // Show/hide fight button (if assigned)
        if (fightButton != null)
        {
            fightButton.gameObject.SetActive(true);
        }
        
        gameObject.SetActive(true);
    }
    
    void OnFightClicked()
    {
        if (monsterData == null)
        {
            return;
        }
        
        if (CombatManager.Instance == null)
        {
            return;
        }
        
        // Check if combat is already active
        if (CombatManager.Instance.GetCombatState() != CombatManager.CombatState.Idle)
        {
            return;
        }
        
        // Get mob count from selector (default to 1 if not found)
        // Try to find selector again if not assigned (in case it wasn't found in Awake)
        if (mobCountSelector == null)
        {
            mobCountSelector = FindObjectOfType<MobCountSelector>();
        }
        
        int mobCount = 1;
        if (mobCountSelector != null)
        {
            mobCount = mobCountSelector.GetMobCount();
        }
        else
        {
        }
        
        // Get all monsters from current zone for combat
        if (ZoneManager.Instance != null)
        {
            ZoneData currentZone = ZoneManager.Instance.GetCurrentZone();
            if (currentZone != null)
            {
                MonsterData[] allMonsters = currentZone.GetMonsters();
                if (allMonsters != null && allMonsters.Length > 0)
                {
                    CombatManager.Instance.StartCombat(allMonsters, mobCount);
                }
            }
        }
    }
}

