using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Animated resource bar with smooth transitions and optional effects.
/// Perfect for HP and XP bars with visual polish.
/// </summary>
public class AnimatedResourceBar : MonoBehaviour
{
    [Header("Resource Type")]
    public ResourceType resourceType = ResourceType.Health;
    
    [Header("UI References")]
    public Slider mainSlider;
    public Slider backgroundSlider; // Optional: for damage/heal preview effect
    public TextMeshProUGUI valueText;
    public Image fillImage;
    
    [Header("Animation Settings")]
    public bool useSmoothing = true;
    public float smoothSpeed = 5f;
    
    [Header("Display Settings")]
    public bool showValues = true;
    public bool showPercentage = false;
    public string displayFormat = "{0:F0} / {1:F0}";
    
    [Header("Visual Effects")]
    public bool useColorGradient = true;
    public Color lowColor = new Color(1f, 0.2f, 0.2f); // Red
    public Color midColor = new Color(1f, 0.8f, 0f);   // Yellow
    public Color highColor = new Color(0.2f, 1f, 0.2f); // Green
    [Range(0f, 1f)] public float midColorThreshold = 0.5f;
    
    [Header("Background Bar (Damage Preview)")]
    public bool useBackgroundBar = true;
    public float backgroundBarDelay = 0.5f;
    public Color backgroundBarColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    
    public enum ResourceType
    {
        Health,
        Experience
    }
    
    private float targetValue = 1f;
    private float currentValue = 1f;
    private float currentAmount = 0f;
    private float maxAmount = 1f;
    
    void Start()
    {
        InitializeSliders();
        SubscribeToEvents();
    }
    
    void InitializeSliders()
    {
        if (mainSlider == null)
            mainSlider = GetComponent<Slider>();
        
        if (mainSlider != null)
        {
            mainSlider.minValue = 0f;
            mainSlider.maxValue = 1f;
            
            if (fillImage == null)
                fillImage = mainSlider.fillRect?.GetComponent<Image>();
        }
        
        if (backgroundSlider != null && useBackgroundBar)
        {
            backgroundSlider.minValue = 0f;
            backgroundSlider.maxValue = 1f;
            
            var bgImage = backgroundSlider.fillRect?.GetComponent<Image>();
            if (bgImage != null)
                bgImage.color = backgroundBarColor;
        }
    }
    
    void SubscribeToEvents()
    {
        if (CharacterManager.Instance != null)
        {
            switch (resourceType)
            {
                case ResourceType.Health:
                    CharacterManager.Instance.OnHealthChanged += UpdateHealthBar;
                    UpdateHealthBar(CharacterManager.Instance.GetCurrentHealth(), 
                                   CharacterManager.Instance.GetMaxHealth());
                    break;
                    
                case ResourceType.Experience:
                    CharacterManager.Instance.OnXPChanged += UpdateXPBar;
                    CharacterManager.Instance.OnLevelChanged += OnLevelChanged;
                    UpdateXPBar(CharacterManager.Instance.GetCurrentXP());
                    break;
            }
        }
    }
    
    void OnDestroy()
    {
        if (CharacterManager.Instance != null)
        {
            switch (resourceType)
            {
                case ResourceType.Health:
                    CharacterManager.Instance.OnHealthChanged -= UpdateHealthBar;
                    break;
                    
                case ResourceType.Experience:
                    CharacterManager.Instance.OnXPChanged -= UpdateXPBar;
                    CharacterManager.Instance.OnLevelChanged -= OnLevelChanged;
                    break;
            }
        }
    }
    
    void Update()
    {
        if (useSmoothing && mainSlider != null)
        {
            // Smoothly animate to target value
            currentValue = Mathf.Lerp(currentValue, targetValue, Time.deltaTime * smoothSpeed);
            mainSlider.value = currentValue;
            
            // Update color based on current value
            UpdateBarColor(currentValue);
        }
    }
    
    void UpdateHealthBar(float current, float max)
    {
        currentAmount = current;
        maxAmount = max;
        
        float newTargetValue = max > 0 ? current / max : 0;
        
        // Handle background bar for damage preview
        if (useBackgroundBar && backgroundSlider != null)
        {
            if (newTargetValue < currentValue) // Taking damage
            {
                // Background bar stays at old value temporarily
                CancelInvoke(nameof(UpdateBackgroundBar));
                Invoke(nameof(UpdateBackgroundBar), backgroundBarDelay);
            }
            else // Healing or gaining HP
            {
                // Background bar updates immediately
                backgroundSlider.value = newTargetValue;
            }
        }
        
        targetValue = newTargetValue;
        
        if (!useSmoothing && mainSlider != null)
        {
            mainSlider.value = targetValue;
            currentValue = targetValue;
            UpdateBarColor(targetValue);
        }
        
        UpdateText();
    }
    
    void UpdateXPBar(int currentXP)
    {
        if (CharacterManager.Instance == null) return;
        
        int xpNeeded = CharacterManager.Instance.GetXPRequiredForNextLevel();
        currentAmount = currentXP;
        maxAmount = xpNeeded;
        
        targetValue = xpNeeded > 0 ? (float)currentXP / xpNeeded : 0;
        
        if (!useSmoothing && mainSlider != null)
        {
            mainSlider.value = targetValue;
            currentValue = targetValue;
            UpdateBarColor(targetValue);
        }
        
        // Background bar for XP always follows main bar
        if (backgroundSlider != null)
        {
            backgroundSlider.value = targetValue;
        }
        
        UpdateText();
    }
    
    void OnLevelChanged(int newLevel)
    {
        if (CharacterManager.Instance != null)
        {
            UpdateXPBar(CharacterManager.Instance.GetCurrentXP());
        }
    }
    
    void UpdateBackgroundBar()
    {
        if (backgroundSlider != null)
        {
            backgroundSlider.value = targetValue;
        }
    }
    
    void UpdateBarColor(float fillAmount)
    {
        if (!useColorGradient || fillImage == null) return;
        
        Color targetColor;
        if (fillAmount <= midColorThreshold)
        {
            // Interpolate between low and mid color
            float t = fillAmount / midColorThreshold;
            targetColor = Color.Lerp(lowColor, midColor, t);
        }
        else
        {
            // Interpolate between mid and high color
            float t = (fillAmount - midColorThreshold) / (1f - midColorThreshold);
            targetColor = Color.Lerp(midColor, highColor, t);
        }
        
        fillImage.color = targetColor;
    }
    
    void UpdateText()
    {
        if (!showValues || valueText == null) return;
        
        if (showPercentage)
        {
            valueText.text = $"{(targetValue * 100):F0}%";
        }
        else
        {
            valueText.text = string.Format(displayFormat, currentAmount, maxAmount);
        }
    }
}