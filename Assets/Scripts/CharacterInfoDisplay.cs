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
    public TextMeshProUGUI levelText; // Can show "Level X" or "Level X Race Class"
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI healthText;
    
    [Header("Display Format")]
    public bool showXPToNextLevel = true;
    public bool showMaxHealth = true;
    
    void Start()
    {
        // Use coroutine to wait for CharacterManager to be ready (in case it's being created dynamically)
        StartCoroutine(InitializeAfterDelay());
    }
    
    System.Collections.IEnumerator InitializeAfterDelay()
    {
        // Wait a frame to ensure CharacterManager and CharacterLoader have initialized
        yield return null;
        
        // Try to find CharacterManager - wait a bit if it's being created
        int attempts = 0;
        while (CharacterManager.Instance == null && attempts < 10)
        {
            yield return new WaitForSeconds(0.1f);
            attempts++;
        }
        
        // Subscribe to all character data changes
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.OnNameChanged += UpdateNameDisplay;
            CharacterManager.Instance.OnLevelChanged += UpdateLevelDisplay;
            CharacterManager.Instance.OnXPChanged += UpdateXPDisplay;
            CharacterManager.Instance.OnGoldChanged += UpdateGoldDisplay;
            CharacterManager.Instance.OnHealthChanged += UpdateHealthDisplay;
            
            // Initialize displays with current values (after character has been loaded)
            UpdateNameDisplay(CharacterManager.Instance.GetName());
            UpdateLevelDisplay(CharacterManager.Instance.GetLevel());
            UpdateXPDisplay(CharacterManager.Instance.GetCurrentXP());
            UpdateGoldDisplay(CharacterManager.Instance.GetGold());
            UpdateHealthDisplay(CharacterManager.Instance.GetCurrentHealth(), CharacterManager.Instance.GetMaxHealth());
        }
        else
        {
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
        {
            // Show level with race/class combo if available
            if (CharacterManager.Instance != null)
            {
                string race = CharacterManager.Instance.GetRace();
                string charClass = CharacterManager.Instance.GetCharacterClass();
                
                if (!string.IsNullOrEmpty(race) && !string.IsNullOrEmpty(charClass))
                {
                    levelText.text = $"Level {level} {race} {charClass}";
                }
                else
                {
                    levelText.text = "Level: " + level;
                }
            }
            else
            {
                levelText.text = "Level: " + level;
            }
        }
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
            // Clamp health to 0 minimum for display
            float displayHealth = Mathf.Max(0f, currentHealth);
            
            if (showMaxHealth)
            {
                healthText.text = $"HP: {displayHealth:F0} / {maxHealth:F0}";
            }
            else
            {
                healthText.text = $"HP: {displayHealth:F0}";
            }
        }
    }
}


