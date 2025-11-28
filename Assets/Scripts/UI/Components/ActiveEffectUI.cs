using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// UI component for displaying a single active buff or debuff.
/// Shows icon with duration timer and stack count. Name/details shown via tooltip on hover.
/// </summary>
public class ActiveEffectUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Image effectIcon;
    public TextMeshProUGUI durationText;
    public TextMeshProUGUI stackText;
    public Image borderImage;
    
    [Header("Colors")]
    public Color buffBorderColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color debuffBorderColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    
    // Effect data
    private ActiveEffect activeEffect;
    private bool isBuff;
    
    /// <summary>
    /// Setup this UI element with effect data
    /// </summary>
    public void Setup(ActiveEffect effect, bool isBuffEffect)
    {
        activeEffect = effect;
        isBuff = isBuffEffect;
        
        // Effect can be from skill with aura, skill without aura, or aura directly
        if (effect == null || (effect.sourceSkill == null && effect.aura == null))
        {
            gameObject.SetActive(false);
            return;
        }
        
        // Set icon (uses aura icon if available, falls back to skill icon)
        if (effectIcon != null)
        {
            Sprite icon = effect.GetIcon();
            if (icon != null)
            {
                effectIcon.sprite = icon;
                // Apply aura tint if available
                if (effect.aura != null)
                {
                    effectIcon.color = effect.aura.iconTint;
                }
                else
                {
                    effectIcon.color = Color.white;
                }
            }
            else
            {
                // Default color for no icon
                effectIcon.color = isBuff ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.3f, 0.3f);
            }
        }
        
        // Set border color
        if (borderImage != null)
        {
            borderImage.color = isBuff ? buffBorderColor : debuffBorderColor;
        }
        
        gameObject.SetActive(true);
        
        // Initial update
        UpdateDisplay();
    }
    
    void Update()
    {
        UpdateDisplay();
    }
    
    void UpdateDisplay()
    {
        if (activeEffect == null)
            return;
        
        // Update duration text
        if (durationText != null)
        {
            float remaining = activeEffect.remainingDuration;
            if (remaining > 60f)
            {
                durationText.text = $"{remaining / 60f:F0}m";
            }
            else if (remaining > 0f)
            {
                durationText.text = $"{remaining:F0}";
            }
            else
            {
                durationText.text = "";
            }
        }
        
        // Update stack count
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
    /// Get the effect this UI is displaying
    /// </summary>
    public ActiveEffect GetEffect()
    {
        return activeEffect;
    }
    
    /// <summary>
    /// Check if this UI matches a specific effect
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
