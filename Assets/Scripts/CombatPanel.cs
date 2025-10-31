using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI panel that displays auto-battle combat.
/// Shows player health, monster health, attack progress bars, and combat results.
/// </summary>
public class CombatPanel : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject combatPanel; // The main panel to show/hide
    
    [Header("Monster Display")]
    public Image monsterSprite;
    public TextMeshProUGUI monsterNameText;
    public Slider monsterHealthBar;
    public TextMeshProUGUI monsterHealthText;
    public Slider monsterAttackProgressBar;
    
    [Header("Player Display")]
    public Image playerSprite; // Optional
    public Slider playerHealthBar;
    public TextMeshProUGUI playerHealthText;
    public Slider playerAttackProgressBar;
    
    [Header("Combat Info")]
    public TextMeshProUGUI combatLogText;
    public TextMeshProUGUI monsterCountText; // "Monster 1/5"
    
    [Header("Buttons")]
    public Button retreatButton;
    public Button continueButton; // Shows after victory/defeat
    
    [Header("Damage Display")]
    public TextMeshProUGUI playerDamageText; // Floating damage numbers
    public TextMeshProUGUI monsterDamageText; // Floating damage numbers
    
    void Start()
    {
        // Subscribe to combat events
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnCombatStateChanged += OnCombatStateChanged;
            CombatManager.Instance.OnPlayerHealthChanged += UpdatePlayerHealth;
            CombatManager.Instance.OnMonsterHealthChanged += UpdateMonsterHealth;
            CombatManager.Instance.OnMonsterChanged += OnMonsterChanged;
            CombatManager.Instance.OnPlayerAttackProgress += UpdatePlayerAttackProgress;
            CombatManager.Instance.OnMonsterAttackProgress += UpdateMonsterAttackProgress;
            CombatManager.Instance.OnPlayerDamageDealt += ShowPlayerDamage;
            CombatManager.Instance.OnMonsterDamageDealt += ShowMonsterDamage;
        }
        
        // Setup buttons
        if (retreatButton != null)
            retreatButton.onClick.AddListener(OnRetreatClicked);
        
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
            continueButton.gameObject.SetActive(false);
        }
        
        // Hide panel initially
        if (combatPanel != null)
            combatPanel.SetActive(false);
        
        // Hide damage text initially
        if (playerDamageText != null)
            playerDamageText.gameObject.SetActive(false);
        if (monsterDamageText != null)
            monsterDamageText.gameObject.SetActive(false);
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnCombatStateChanged -= OnCombatStateChanged;
            CombatManager.Instance.OnPlayerHealthChanged -= UpdatePlayerHealth;
            CombatManager.Instance.OnMonsterHealthChanged -= UpdateMonsterHealth;
            CombatManager.Instance.OnMonsterChanged -= OnMonsterChanged;
            CombatManager.Instance.OnPlayerAttackProgress -= UpdatePlayerAttackProgress;
            CombatManager.Instance.OnMonsterAttackProgress -= UpdateMonsterAttackProgress;
            CombatManager.Instance.OnPlayerDamageDealt -= ShowPlayerDamage;
            CombatManager.Instance.OnMonsterDamageDealt -= ShowMonsterDamage;
        }
    }
    
    void OnCombatStateChanged(CombatManager.CombatState newState)
    {
        switch (newState)
        {
            case CombatManager.CombatState.Idle:
                HideCombatPanel();
                break;
                
            case CombatManager.CombatState.Fighting:
                ShowCombatPanel();
                if (retreatButton != null)
                    retreatButton.gameObject.SetActive(true);
                if (continueButton != null)
                    continueButton.gameObject.SetActive(false);
                UpdateCombatLog("Battle started!");
                break;
                
            case CombatManager.CombatState.Victory:
                if (retreatButton != null)
                    retreatButton.gameObject.SetActive(false);
                if (continueButton != null)
                    continueButton.gameObject.SetActive(true);
                UpdateCombatLog("Victory! All monsters defeated!");
                break;
                
            case CombatManager.CombatState.Defeat:
                if (retreatButton != null)
                    retreatButton.gameObject.SetActive(false);
                if (continueButton != null)
                    continueButton.gameObject.SetActive(true);
                UpdateCombatLog("Defeat! You have been respawned.");
                break;
        }
    }
    
    void OnMonsterChanged(MonsterData monster)
    {
        if (monster == null) return;
        
        // Update monster display
        if (monsterNameText != null)
            monsterNameText.text = monster.monsterName;
        
        if (monsterSprite != null && monster.monsterSprite != null)
            monsterSprite.sprite = monster.monsterSprite;
        
        UpdateCombatLog($"Fighting {monster.monsterName}!");
    }
    
    void UpdatePlayerHealth(float current, float max)
    {
        if (playerHealthBar != null)
        {
            playerHealthBar.maxValue = max;
            playerHealthBar.value = current;
        }
        
        if (playerHealthText != null)
            playerHealthText.text = $"{current:F0} / {max:F0}";
    }
    
    void UpdateMonsterHealth(float current, float max)
    {
        if (monsterHealthBar != null)
        {
            monsterHealthBar.maxValue = max;
            monsterHealthBar.value = current;
        }
        
        if (monsterHealthText != null)
            monsterHealthText.text = $"{current:F0} / {max:F0}";
    }
    
    void UpdatePlayerAttackProgress(float progress)
    {
        if (playerAttackProgressBar != null)
            playerAttackProgressBar.value = progress;
    }
    
    void UpdateMonsterAttackProgress(float progress)
    {
        if (monsterAttackProgressBar != null)
            monsterAttackProgressBar.value = progress;
    }
    
    void ShowPlayerDamage(float damage)
    {
        if (playerDamageText != null)
        {
            playerDamageText.text = $"-{damage:F0}";
            playerDamageText.gameObject.SetActive(true);
            
            // Hide after short delay
            CancelInvoke(nameof(HidePlayerDamage));
            Invoke(nameof(HidePlayerDamage), 0.5f);
        }
    }
    
    void ShowMonsterDamage(float damage)
    {
        if (monsterDamageText != null)
        {
            monsterDamageText.text = $"-{damage:F0}";
            monsterDamageText.gameObject.SetActive(true);
            
            // Hide after short delay
            CancelInvoke(nameof(HideMonsterDamage));
            Invoke(nameof(HideMonsterDamage), 0.5f);
        }
    }
    
    void HidePlayerDamage()
    {
        if (playerDamageText != null)
            playerDamageText.gameObject.SetActive(false);
    }
    
    void HideMonsterDamage()
    {
        if (monsterDamageText != null)
            monsterDamageText.gameObject.SetActive(false);
    }
    
    void UpdateCombatLog(string message)
    {
        if (combatLogText != null)
            combatLogText.text = message;
    }
    
    void ShowCombatPanel()
    {
        if (combatPanel != null)
            combatPanel.SetActive(true);
    }
    
    void HideCombatPanel()
    {
        if (combatPanel != null)
            combatPanel.SetActive(false);
    }
    
    void OnRetreatClicked()
    {
        // End combat and return to zone
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.EndCombat();
        }
        
        HideCombatPanel();
    }
    
    void OnContinueClicked()
    {
        // Return to zone after victory/defeat
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.EndCombat();
        }
        
        HideCombatPanel();
    }
}


