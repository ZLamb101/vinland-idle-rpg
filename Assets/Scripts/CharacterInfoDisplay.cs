using UnityEngine;
using TMPro;

/// <summary>
/// Displays character information on the UI.
/// Automatically updates when CharacterManager data changes.
/// </summary>
public class CharacterInfoDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI healthText;
    
    [Header("Display Format")]
    public bool showXPToNextLevel = true;
    public bool showMaxHealth = true;
    
    void Start()
    {
        // Subscribe to all character data changes
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.OnNameChanged += UpdateNameDisplay;
            CharacterManager.Instance.OnLevelChanged += UpdateLevelDisplay;
            CharacterManager.Instance.OnXPChanged += UpdateXPDisplay;
            CharacterManager.Instance.OnGoldChanged += UpdateGoldDisplay;
            CharacterManager.Instance.OnHealthChanged += UpdateHealthDisplay;
            
            // Initialize displays with current values
            UpdateNameDisplay(CharacterManager.Instance.GetName());
            UpdateLevelDisplay(CharacterManager.Instance.GetLevel());
            UpdateXPDisplay(CharacterManager.Instance.GetCurrentXP());
            UpdateGoldDisplay(CharacterManager.Instance.GetGold());
            UpdateHealthDisplay(CharacterManager.Instance.GetCurrentHealth(), CharacterManager.Instance.GetMaxHealth());
        }
        else
        {
            Debug.LogWarning("CharacterInfoDisplay: CharacterManager.Instance is null! Make sure CharacterManager exists in the scene.");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.OnNameChanged -= UpdateNameDisplay;
            CharacterManager.Instance.OnLevelChanged -= UpdateLevelDisplay;
            CharacterManager.Instance.OnXPChanged -= UpdateXPDisplay;
            CharacterManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
            CharacterManager.Instance.OnHealthChanged -= UpdateHealthDisplay;
        }
    }
    
    void UpdateNameDisplay(string characterName)
    {
        if (nameText != null)
            nameText.text = characterName;
    }
    
    void UpdateLevelDisplay(int level)
    {
        if (levelText != null)
            levelText.text = "Level: " + level;
    }
    
    void UpdateXPDisplay(int xp)
    {
        if (xpText != null)
        {
            if (showXPToNextLevel && CharacterManager.Instance != null)
            {
                int xpNeeded = CharacterManager.Instance.GetXPRequiredForNextLevel();
                xpText.text = $"XP: {xp} / {xpNeeded}";
            }
            else
            {
                xpText.text = "XP: " + xp;
            }
        }
    }
    
    void UpdateGoldDisplay(int gold)
    {
        if (goldText != null)
            goldText.text = "Gold: " + gold;
    }
    
    void UpdateHealthDisplay(float currentHealth, float maxHealth)
    {
        if (healthText != null)
        {
            if (showMaxHealth)
            {
                healthText.text = $"HP: {currentHealth:F0} / {maxHealth:F0}";
            }
            else
            {
                healthText.text = $"HP: {currentHealth:F0}";
            }
        }
    }
}


