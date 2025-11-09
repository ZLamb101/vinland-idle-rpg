using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI panel that displays auto-battle combat.
/// Shows player health, attack progress bars, combat log, and buttons.
/// Monster information is displayed via TargetFrame and per-monster containers above enemies.
/// </summary>
public class CombatPanel : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject combatPanel; // The main panel to show/hide
    
    [Header("Player Display")]
    public Image playerSprite; // Optional
    public Slider playerHealthBar;
    public TextMeshProUGUI playerHealthText;
    public Slider playerAttackProgressBar;
    
    [Header("Combat Info")]
    public TextMeshProUGUI combatLogText;
    
    [Header("Buttons")]
    public Button retreatButton;
    public Button continueButton; // Button to continue after defeat
    
    [Header("Mob Count Selector")]
    [Tooltip("Mob count selector. If not assigned, will try to find it in the scene.")]
    public MobCountSelector mobCountSelector; // Selector for number of mobs to fight
    
    [Header("Damage Display")]
    [Tooltip("Prefab for player damage text. Required for showing damage numbers.")]
    public GameObject playerDamageTextPrefab; // Prefab for floating damage numbers
    [Tooltip("Parent container for damage text instances")]
    public RectTransform damageTextContainer; // Container for damage text instances
    public float damageAnimationDuration = 1f; // How long damage numbers animate
    public float damageRiseDistance = 50f; // How far damage numbers rise (pixels)
    [Tooltip("Horizontal spread for multiple damage texts (pixels)")]
    public float damageTextSpread = 30f; // Spread damage texts horizontally to avoid overlap
    
    private List<GameObject> activeDamageTexts = new List<GameObject>(); // Track active damage text instances
    private int damageTextCounter = 0; // Counter for positioning multiple damage texts
    
    void Start()
    {
        // Find mob count selector if not assigned
        if (mobCountSelector == null)
        {
            mobCountSelector = FindAnyObjectByType<MobCountSelector>();
        }
        
        // Subscribe to combat events
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnCombatStateChanged += OnCombatStateChanged;
            CombatManager.Instance.OnPlayerHealthChanged += UpdatePlayerHealth;
            CombatManager.Instance.OnPlayerAttackProgress += UpdatePlayerAttackProgress;
            CombatManager.Instance.OnPlayerDamageTaken += ShowPlayerDamage;
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
        
        // Setup damage text container if not assigned
        if (damageTextContainer == null && playerDamageTextPrefab != null)
        {
            // Use prefab's parent as container if it exists in scene
            if (playerDamageTextPrefab.transform.parent != null)
            {
                damageTextContainer = playerDamageTextPrefab.transform.parent.GetComponent<RectTransform>();
            }
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnCombatStateChanged -= OnCombatStateChanged;
            CombatManager.Instance.OnPlayerHealthChanged -= UpdatePlayerHealth;
            CombatManager.Instance.OnPlayerAttackProgress -= UpdatePlayerAttackProgress;
            CombatManager.Instance.OnPlayerDamageTaken -= ShowPlayerDamage;
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
                // Clear damage numbers when leaving combat
                HidePlayerDamage();
                break;
                
            case CombatManager.CombatState.Fighting:
                ShowCombatPanel();
                if (retreatButton != null)
                    retreatButton.gameObject.SetActive(true);
                if (continueButton != null)
                    continueButton.gameObject.SetActive(false);
                // Clear damage numbers when starting combat
                HidePlayerDamage();
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
    
    void UpdatePlayerAttackProgress(float progress)
    {
        if (playerAttackProgressBar != null)
            playerAttackProgressBar.value = progress;
    }
    
    void ShowPlayerDamage(float damage)
    {
        // Only show damage if the panel GameObject is active (can't start coroutines on inactive objects)
        // Also check if combatPanel is active since that controls visibility
        if (!gameObject.activeInHierarchy || (combatPanel != null && !combatPanel.activeInHierarchy))
        {
            return;
        }
        
        // Get or create damage text instance
        TextMeshProUGUI damageTextInstance = GetOrCreateDamageText();
        
        if (damageTextInstance != null)
        {
            // Calculate offset for this damage text (spread horizontally)
            float xOffset = (damageTextCounter % 3 - 1) * damageTextSpread; // -spread, 0, +spread pattern
            damageTextCounter++;
            
            // Set damage value
            damageTextInstance.text = $"-{damage:F0}";
            
            // Start animation with offset
            StartCoroutine(AnimateDamageNumber(damageTextInstance, damageRiseDistance, damageAnimationDuration, xOffset));
        }
    }
    
    /// <summary>
    /// Get or create a damage text instance
    /// </summary>
    TextMeshProUGUI GetOrCreateDamageText()
    {
        if (playerDamageTextPrefab == null)
        {
            return null;
        }
        
        Transform parent = damageTextContainer != null ? damageTextContainer : transform;
        GameObject damageObj = Instantiate(playerDamageTextPrefab, parent);
        TextMeshProUGUI text = damageObj.GetComponent<TextMeshProUGUI>();
        if (text == null)
            text = damageObj.GetComponentInChildren<TextMeshProUGUI>();
        
        if (text != null && damageObj != null)
        {
            activeDamageTexts.Add(damageObj);
            return text;
        }
        return null;
    }
    
    /// <summary>
    /// Animate a damage number rising up and fading out
    /// </summary>
    IEnumerator AnimateDamageNumber(TextMeshProUGUI damageText, float riseDistance, float duration, float xOffset = 0f)
    {
        if (damageText == null) yield break;
        
        RectTransform rectTransform = damageText.rectTransform;
        CanvasGroup canvasGroup = damageText.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = damageText.gameObject.AddComponent<CanvasGroup>();
        }
        
        // Get base position (from player health bar, player sprite, or damage text container)
        Vector2 basePosition = Vector2.zero;
        RectTransform referenceRect = null;
        
        if (playerHealthBar != null)
        {
            referenceRect = playerHealthBar.GetComponent<RectTransform>();
        }
        else if (playerSprite != null)
        {
            referenceRect = playerSprite.GetComponent<RectTransform>();
        }
        else if (damageTextContainer != null)
        {
            referenceRect = damageTextContainer;
        }
        
        if (referenceRect != null)
        {
            basePosition = referenceRect.anchoredPosition;
            // Offset upward from the reference (above health bar/sprite)
            basePosition += new Vector2(0f, 20f);
        }
        
        // Store starting position with X offset
        Vector2 startPosition = basePosition + new Vector2(xOffset, 0f);
        Vector2 endPosition = startPosition + new Vector2(0, riseDistance);
        
        // Reset alpha and position
        canvasGroup.alpha = 1f;
        rectTransform.anchoredPosition = startPosition;
        damageText.gameObject.SetActive(true);
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Check if text was destroyed
            if (damageText == null || damageText.gameObject == null)
                yield break;
                
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Ease out curve for smooth animation
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            
            // Interpolate position (rise up)
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, easedT);
            }
            
            // Interpolate alpha (fade out)
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            }
            
            yield return null;
        }
        
        // Clean up: destroy if it was instantiated, otherwise just hide
        if (damageText != null && damageText.gameObject != null)
        {
            GameObject damageObj = damageText.gameObject;
            
            // Remove from active list
            activeDamageTexts.Remove(damageObj);
            
            // Always destroy instantiated instances (they're all dynamically created)
            Destroy(damageObj);
        }
    }
    
    void HidePlayerDamage()
    {
        // Clean up all active damage text instances
        foreach (GameObject damageObj in activeDamageTexts)
        {
            if (damageObj != null)
            {
                Destroy(damageObj);
            }
        }
        activeDamageTexts.Clear();
        damageTextCounter = 0;
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
    
    /// <summary>
    /// Reset mob count selector to default value of 1
    /// </summary>
    void ResetMobCountSelector()
    {
        // Try to find selector if not assigned
        if (mobCountSelector == null)
        {
            mobCountSelector = FindAnyObjectByType<MobCountSelector>();
        }
        
        if (mobCountSelector != null)
        {
            mobCountSelector.SetMobCount(1);
        }
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
        
        // Reset mob count selector to 1 when retreating
        ResetMobCountSelector();
        
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


