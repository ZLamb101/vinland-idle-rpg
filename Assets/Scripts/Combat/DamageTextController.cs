using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles floating damage text, miss text, and other combat text displays.
/// Extracted from CombatSceneController for single responsibility.
/// </summary>
public class DamageTextController : MonoBehaviour
{
    [Header("References")]
    public RectTransform combatSceneContainer;
    
    [Header("Settings")]
    [Tooltip("Font to use for damage text")]
    public TMP_FontAsset damageTextFont;
    
    // Tracking for cleanup
    private List<GameObject> activeDamageTexts = new List<GameObject>();
    
    // Callbacks for getting positions (set by CombatSceneController)
    private System.Func<int, Vector2> getEnemyLocalPosition;
    private System.Func<Vector2> getHeroLocalPosition;
    
    /// <summary>
    /// Initialize the controller with required dependencies
    /// </summary>
    public void Initialize(RectTransform container, System.Func<int, Vector2> enemyPositionFunc, System.Func<Vector2> heroPositionFunc)
    {
        combatSceneContainer = container;
        getEnemyLocalPosition = enemyPositionFunc;
        getHeroLocalPosition = heroPositionFunc;
        
        // Load font if not set
        if (damageTextFont == null)
        {
            damageTextFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Charlie don't surf SDF");
        }
    }
    
    /// <summary>
    /// Show damage hit text above specific enemy
    /// </summary>
    public void ShowDamageHitText(int enemyIndex, float damage, bool wasCritical = false)
    {
        if (getEnemyLocalPosition == null)
            return;
        
        Vector2 localPosition = getEnemyLocalPosition(enemyIndex);
        if (localPosition == Vector2.zero)
            return;
        
        // Determine color based on crit (gold for crit, white for normal)
        Color textColor = wasCritical ? new Color(1f, 0.84f, 0f) : Color.white;
        
        // Create text GameObject
        GameObject damageObj = CreateFloatingText($"{damage:F0}", localPosition, textColor, 100f);
        if (damageObj == null)
            return;
        
        TextMeshProUGUI damageText = damageObj.GetComponent<TextMeshProUGUI>();
        
        // Animate damage text (rise up and fade out, with pop animation for crits)
        StartCoroutine(AnimateDamageText(damageText, damageObj, wasCritical));
    }
    
    /// <summary>
    /// Show damage text on the hero when hit by enemy attacks/skills
    /// </summary>
    public void ShowDamageOnHero(float damage, bool wasCritical = false)
    {
        if (getHeroLocalPosition == null || combatSceneContainer == null)
            return;
        
        Vector2 heroPosition = getHeroLocalPosition();
        if (heroPosition == Vector2.zero)
            return;
        
        // Use red color for damage on player to distinguish from damage dealt
        Color textColor = wasCritical ? new Color(1f, 0.5f, 0f) : new Color(1f, 0.3f, 0.3f);
        
        // Create text GameObject
        GameObject damageObj = CreateFloatingText($"-{damage:F0}", heroPosition, textColor, 100f);
        if (damageObj == null)
            return;
        
        TextMeshProUGUI damageText = damageObj.GetComponent<TextMeshProUGUI>();
        
        // Animate damage text (rise up and fade out)
        StartCoroutine(AnimateDamageText(damageText, damageObj, wasCritical));
    }
    
    /// <summary>
    /// Show "Miss" text above specific enemy (for player misses) or above player (for enemy misses)
    /// </summary>
    public void ShowMissText(int enemyIndex, bool isPlayerMiss)
    {
        if (combatSceneContainer == null)
            return;
        
        Vector2 localPosition;
        
        if (isPlayerMiss)
        {
            // Player miss - show above enemy
            if (getEnemyLocalPosition == null)
                return;
            
            localPosition = getEnemyLocalPosition(enemyIndex);
            if (localPosition == Vector2.zero)
                return;
        }
        else
        {
            // Enemy miss - show above hero
            if (getHeroLocalPosition == null)
                return;
            
            localPosition = getHeroLocalPosition();
            if (localPosition == Vector2.zero)
                return;
        }
        
        // Create new miss text GameObject
        GameObject missObj = new GameObject($"MissText_{enemyIndex}");
        missObj.transform.SetParent(combatSceneContainer, false);
        
        // Add to tracking list
        activeDamageTexts.Add(missObj);
        
        // Add RectTransform
        RectTransform missRect = missObj.AddComponent<RectTransform>();
        missRect.sizeDelta = new Vector2(150f, 50f);
        missRect.anchoredPosition = localPosition + new Vector2(0f, 30f);
        
        // Add CanvasGroup for fade effect
        CanvasGroup canvasGroup = missObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        
        // Add TextMeshProUGUI for miss text
        TextMeshProUGUI missText = missObj.AddComponent<TextMeshProUGUI>();
        missText.text = "Miss";
        missText.fontSize = 32;
        missText.fontStyle = FontStyles.Bold;
        missText.color = isPlayerMiss ? new Color(0f, 0.4f, 0.8f) : Color.red;
        missText.alignment = TextAlignmentOptions.Center;
        missText.enableWordWrapping = false;
        
        // Set font if available
        if (damageTextFont != null)
        {
            missText.font = damageTextFont;
        }
        
        // Add outline for better visibility
        var outline = missObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);
        
        // Animate miss text (rise up and fade out)
        StartCoroutine(AnimateDamageText(missText, missObj, false));
    }
    
    /// <summary>
    /// Create a floating text GameObject with common setup
    /// </summary>
    GameObject CreateFloatingText(string text, Vector2 position, Color color, float width = 100f)
    {
        if (combatSceneContainer == null)
            return null;
        
        // Create new text GameObject
        GameObject textObj = new GameObject("DamageText");
        textObj.transform.SetParent(combatSceneContainer, false);
        
        // Add to tracking list
        activeDamageTexts.Add(textObj);
        
        // Add RectTransform
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(width, 50f);
        textRect.anchoredPosition = position + new Vector2(0f, 30f);
        
        // Add CanvasGroup for fade effect
        CanvasGroup canvasGroup = textObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        
        // Add TextMeshProUGUI
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 32;
        textComponent.fontStyle = FontStyles.Bold;
        textComponent.color = color;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.enableWordWrapping = false;
        
        // Set font if available
        if (damageTextFont != null)
        {
            textComponent.font = damageTextFont;
        }
        
        // Add outline for better visibility
        var outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);
        
        return textObj;
    }
    
    /// <summary>
    /// Animate damage text rising up and fading out
    /// For critical hits, adds a heartbeat pop animation
    /// </summary>
    IEnumerator AnimateDamageText(TextMeshProUGUI damageText, GameObject damageObj, bool wasCritical = false)
    {
        if (damageText == null || damageText.rectTransform == null || damageObj == null)
            yield break;
        
        RectTransform rectTransform = damageText.rectTransform;
        CanvasGroup canvasGroup = damageText.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = damageText.gameObject.AddComponent<CanvasGroup>();
        }
        
        // Store starting position and scale
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 endPosition = startPosition + new Vector2(0, 50f);
        Vector3 defaultScale = Vector3.one;
        
        // Reset alpha and position
        canvasGroup.alpha = 1f;
        rectTransform.anchoredPosition = startPosition;
        rectTransform.localScale = defaultScale;
        
        // For critical hits, play heartbeat pop animation first
        if (wasCritical)
        {
            float popDuration = 0.2f;
            float popElapsed = 0f;
            
            // Scale up
            while (popElapsed < popDuration)
            {
                if (damageText == null || rectTransform == null || damageObj == null)
                    yield break;
                
                popElapsed += Time.deltaTime;
                float t = popElapsed / popDuration;
                float easedT = t * t;
                rectTransform.localScale = Vector3.Lerp(defaultScale, defaultScale * 1.3f, easedT);
                yield return null;
            }
            
            popElapsed = 0f;
            popDuration = 0.2f;
            
            // Scale down
            while (popElapsed < popDuration)
            {
                if (damageText == null || rectTransform == null || damageObj == null)
                    yield break;
                
                popElapsed += Time.deltaTime;
                float t = popElapsed / popDuration;
                float easedT = t * t;
                rectTransform.localScale = Vector3.Lerp(defaultScale * 1.3f, defaultScale, easedT);
                yield return null;
            }
            
            rectTransform.localScale = defaultScale;
        }
        
        float duration = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            if (damageText == null || rectTransform == null || damageObj == null)
                yield break;
            
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Ease out curve
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            
            // Interpolate position (rise up)
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, easedT);
            
            // Interpolate alpha (fade out)
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            }
            
            yield return null;
        }
        
        // Clean up
        if (damageObj != null)
        {
            activeDamageTexts.Remove(damageObj);
            Destroy(damageObj);
        }
    }
    
    /// <summary>
    /// Clean up all active damage texts
    /// </summary>
    public void Cleanup()
    {
        foreach (var textObj in activeDamageTexts)
        {
            if (textObj != null)
            {
                Destroy(textObj);
            }
        }
        activeDamageTexts.Clear();
    }
    
    void OnDestroy()
    {
        Cleanup();
    }
}

