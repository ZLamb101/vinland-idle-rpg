using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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
    public Button continueButton; // Button to continue after defeat
    
    [Header("Damage Display")]
    public TextMeshProUGUI playerDamageText; // Floating damage numbers
    public TextMeshProUGUI monsterDamageText; // Floating damage numbers
    public float damageAnimationDuration = 1f; // How long damage numbers animate
    public float damageRiseDistance = 50f; // How far damage numbers rise (pixels)
    
    private Coroutine playerDamageAnimation;
    private Coroutine monsterDamageAnimation;
    
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
            continueButton.onClick.AddListener(OnContinueClicked);
        
        // Hide buttons initially
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
        
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
                if (continueButton != null)
                    continueButton.gameObject.SetActive(false);
                break;
                
            case CombatManager.CombatState.Fighting:
                ShowCombatPanel();
                if (retreatButton != null)
                    retreatButton.gameObject.SetActive(true);
                if (continueButton != null)
                    continueButton.gameObject.SetActive(false);
                UpdateCombatLog("Battle started!");
                break;
                
            case CombatManager.CombatState.Defeat:
                // Combat paused - show Continue button
                if (retreatButton != null)
                    retreatButton.gameObject.SetActive(false);
                if (continueButton != null)
                    continueButton.gameObject.SetActive(true);
                UpdateCombatLog("Defeat! You have been respawned. Click Continue to resume combat.");
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
        // Clamp health to 0 minimum for display
        float displayCurrent = Mathf.Max(0f, current);
        
        if (playerHealthBar != null)
        {
            playerHealthBar.maxValue = max;
            playerHealthBar.value = displayCurrent;
        }
        
        if (playerHealthText != null)
            playerHealthText.text = $"{displayCurrent:F0} / {max:F0}";
    }
    
    void UpdateMonsterHealth(float current, float max)
    {
        // Clamp health to 0 minimum for display
        float displayCurrent = Mathf.Max(0f, current);
        
        if (monsterHealthBar != null)
        {
            monsterHealthBar.maxValue = max;
            monsterHealthBar.value = displayCurrent;
        }
        
        if (monsterHealthText != null)
            monsterHealthText.text = $"{displayCurrent:F0} / {max:F0}";
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
            
            // Stop any existing animation
            if (playerDamageAnimation != null)
            {
                StopCoroutine(playerDamageAnimation);
            }
            
            // Start new animation
            playerDamageAnimation = StartCoroutine(AnimateDamageNumber(playerDamageText, damageRiseDistance, damageAnimationDuration));
        }
    }
    
    void ShowMonsterDamage(float damage)
    {
        if (monsterDamageText != null)
        {
            monsterDamageText.text = $"-{damage:F0}";
            
            // Stop any existing animation
            if (monsterDamageAnimation != null)
            {
                StopCoroutine(monsterDamageAnimation);
            }
            
            // Start new animation
            monsterDamageAnimation = StartCoroutine(AnimateDamageNumber(monsterDamageText, damageRiseDistance, damageAnimationDuration));
        }
    }
    
    /// <summary>
    /// Animate a damage number rising up and fading out
    /// </summary>
    IEnumerator AnimateDamageNumber(TextMeshProUGUI damageText, float riseDistance, float duration)
    {
        if (damageText == null) yield break;
        
        RectTransform rectTransform = damageText.rectTransform;
        CanvasGroup canvasGroup = damageText.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = damageText.gameObject.AddComponent<CanvasGroup>();
        }
        
        // Store starting position
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 endPosition = startPosition + new Vector2(0, riseDistance);
        
        // Reset alpha and position
        canvasGroup.alpha = 1f;
        rectTransform.anchoredPosition = startPosition;
        damageText.gameObject.SetActive(true);
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Ease out curve for smooth animation
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            
            // Interpolate position (rise up)
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, easedT);
            
            // Interpolate alpha (fade out)
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            
            yield return null;
        }
        
        // Ensure final state
        rectTransform.anchoredPosition = endPosition;
        canvasGroup.alpha = 0f;
        damageText.gameObject.SetActive(false);
        
        // Reset position for next use
        rectTransform.anchoredPosition = startPosition;
    }
    
    void HidePlayerDamage()
    {
        if (playerDamageText != null)
        {
            if (playerDamageAnimation != null)
            {
                StopCoroutine(playerDamageAnimation);
                playerDamageAnimation = null;
            }
            playerDamageText.gameObject.SetActive(false);
        }
    }
    
    void HideMonsterDamage()
    {
        if (monsterDamageText != null)
        {
            if (monsterDamageAnimation != null)
            {
                StopCoroutine(monsterDamageAnimation);
                monsterDamageAnimation = null;
            }
            monsterDamageText.gameObject.SetActive(false);
        }
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
        // Resume combat after defeat
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.ResumeAfterDefeat();
        }
    }
}


