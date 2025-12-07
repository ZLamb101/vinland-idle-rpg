using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// UI component for displaying a single buff/debuff effect icon with radial countdown timer.
/// Shows icon, colored border, radial fill overlay, duration number, and tooltip on hover.
/// </summary>
public class EffectIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Elements")]
    public Image iconImage;           // The aura/effect icon
    public Image borderImage;         // Colored border (buff=green, debuff=red)
    public Image radialFillImage;     // Filled type=Radial360, clockwise depletion
    public TextMeshProUGUI durationText; // Seconds remaining
    public TextMeshProUGUI stackText;    // Stack count (hidden if <= 1)
    
    [Header("Colors")]
    public Color buffBorderColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color debuffBorderColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    
    [Header("Settings")]
    [Tooltip("Minimum duration to show decimal (e.g., 0.5s). Below this shows decimal.")]
    public float decimalThreshold = 1f;
    
    // Effect data
    private ActiveEffect activeEffect;
    private bool isBuff;
    
    /// <summary>
    /// Setup this icon with effect data
    /// </summary>
    public void Setup(ActiveEffect effect, bool isBuffEffect)
    {
        activeEffect = effect;
        isBuff = isBuffEffect;
        
        if (effect == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        // Set icon sprite
        if (iconImage != null)
        {
            Sprite icon = effect.GetIcon();
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.color = Color.white;
                
                // Apply aura tint if available
                if (effect.aura != null && effect.aura.iconTint != Color.white)
                {
                    iconImage.color = effect.aura.iconTint;
                }
            }
            else
            {
                // Default color if no icon
                iconImage.color = isBuff ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.3f, 0.3f);
            }
        }
        
        // Set border color
        if (borderImage != null)
        {
            borderImage.color = isBuff ? buffBorderColor : debuffBorderColor;
        }
        
        // Initialize radial fill
        if (radialFillImage != null)
        {
            radialFillImage.type = Image.Type.Filled;
            radialFillImage.fillMethod = Image.FillMethod.Radial360;
            radialFillImage.fillOrigin = (int)Image.Origin360.Top;
            radialFillImage.fillClockwise = true;
            radialFillImage.fillAmount = 1f;
        }
        
        gameObject.SetActive(true);
        UpdateDisplay();
    }
    
    void Update()
    {
        if (activeEffect == null)
            return;
        
        // Check if expired
        if (activeEffect.IsExpired())
        {
            gameObject.SetActive(false);
            return;
        }
        
        UpdateDisplay();
    }
    
    void UpdateDisplay()
    {
        if (activeEffect == null)
            return;
        
        // Update radial fill (depletes as time passes)
        if (radialFillImage != null && activeEffect.totalDuration > 0f)
        {
            float percent = Mathf.Clamp01(activeEffect.remainingDuration / activeEffect.totalDuration);
            radialFillImage.fillAmount = percent;
        }
        
        // Update duration text
        if (durationText != null)
        {
            float remaining = activeEffect.remainingDuration;
            
            if (remaining > 60f)
            {
                // Show minutes for long durations
                durationText.text = $"{remaining / 60f:F0}m";
            }
            else if (remaining > decimalThreshold)
            {
                // Show whole seconds
                durationText.text = $"{remaining:F0}";
            }
            else if (remaining > 0f)
            {
                // Show decimal for last second
                durationText.text = $"{remaining:F1}";
            }
            else
            {
                durationText.text = "";
            }
        }
        
        // Update stack count (only show if > 1)
        if (stackText != null)
        {
            if (activeEffect.stacks > 1)
            {
                stackText.text = activeEffect.stacks.ToString();
                stackText.gameObject.SetActive(true);
            }
            else
            {
                stackText.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Get the effect this icon is displaying
    /// </summary>
    public ActiveEffect GetEffect()
    {
        return activeEffect;
    }
    
    /// <summary>
    /// Check if this icon matches a specific effect
    /// </summary>
    public bool MatchesEffect(ActiveEffect effect)
    {
        if (activeEffect == null || effect == null)
            return false;
        
        // Match by aura if both have auras
        if (activeEffect.aura != null && effect.aura != null)
        {
            return activeEffect.aura == effect.aura || activeEffect.aura.auraName == effect.aura.auraName;
        }
        
        // Match by skill
        if (activeEffect.sourceSkill != null && effect.sourceSkill != null)
        {
            return activeEffect.sourceSkill == effect.sourceSkill;
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if the effect has expired
    /// </summary>
    public bool IsExpired()
    {
        return activeEffect == null || activeEffect.IsExpired();
    }
    
    /// <summary>
    /// Clear the effect reference
    /// </summary>
    public void Clear()
    {
        activeEffect = null;
        gameObject.SetActive(false);
    }
    
    // ==================== Tooltip ====================
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (activeEffect == null)
            return;
        
        string effectName = activeEffect.GetName();
        string title = (isBuff ? "[Buff] " : "[Debuff] ") + effectName;
        string desc = "";
        
        // Get description from aura or skill
        if (activeEffect.aura != null)
        {
            desc = activeEffect.aura.GetFullDescription();
        }
        else if (activeEffect.sourceSkill != null)
        {
            float attackPower = 0f;
            var characterService = Services.Get<ICharacterService>();
            if (characterService != null)
            {
                CharacterData charData = characterService.GetCharacterData();
                if (charData != null)
                {
                    attackPower = charData.GetAttackPower();
                }
            }
            desc = activeEffect.sourceSkill.GetFullDescription(attackPower);
        }
        
        // Add stack info
        if (activeEffect.stacks > 1)
        {
            desc += $"\n\nStacks: {activeEffect.stacks}";
        }
        
        desc += $"\nTime remaining: {activeEffect.remainingDuration:F1}s";
        
        EventBus.Publish(new TooltipShowEvent { title = title, description = desc });
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        EventBus.Publish(new TooltipHideEvent());
    }
}
