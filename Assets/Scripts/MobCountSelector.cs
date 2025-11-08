using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for selecting the number of monsters to fight (1-3).
/// Displays current count with +/- buttons to adjust.
/// </summary>
public class MobCountSelector : MonoBehaviour
{
    [Header("UI References")]
    public Button decreaseButton; // - button
    public Button increaseButton; // + button
    public TextMeshProUGUI countText; // Display current count
    
    private int currentMobCount = 1;
    private const int MIN_MOB_COUNT = 1;
    private const int MAX_MOB_COUNT = 3;
    
    void Awake()
    {
        // Setup button listeners
        if (decreaseButton != null)
        {
            // Clear any existing listeners first (in case inspector has something assigned)
            decreaseButton.onClick.RemoveAllListeners();
            decreaseButton.onClick.AddListener(DecreaseCount);
        }
        
        if (increaseButton != null)
        {
            // Clear any existing listeners first (in case inspector has something assigned)
            increaseButton.onClick.RemoveAllListeners();
            increaseButton.onClick.AddListener(IncreaseCount);
        }
        
        UpdateDisplay();
    }
    
    /// <summary>
    /// Decrease mob count (minimum 1)
    /// </summary>
    public void DecreaseCount()
    {
        if (currentMobCount > MIN_MOB_COUNT)
        {
            currentMobCount--;
            UpdateDisplay();
        }
    }
    
    /// <summary>
    /// Increase mob count (maximum 3)
    /// </summary>
    public void IncreaseCount()
    {
        if (currentMobCount < MAX_MOB_COUNT)
        {
            currentMobCount++;
            UpdateDisplay();
        }
    }
    
    /// <summary>
    /// Get current mob count
    /// </summary>
    public int GetMobCount()
    {
        return currentMobCount;
    }
    
    /// <summary>
    /// Set mob count (clamped to valid range)
    /// </summary>
    public void SetMobCount(int count)
    {
        currentMobCount = Mathf.Clamp(count, MIN_MOB_COUNT, MAX_MOB_COUNT);
        UpdateDisplay();
    }
    
    /// <summary>
    /// Update UI display and button states
    /// </summary>
    void UpdateDisplay()
    {
        // Update count text
        if (countText != null)
        {
            countText.text = currentMobCount.ToString();
        }
        
        // Update button interactability
        if (decreaseButton != null)
        {
            decreaseButton.interactable = currentMobCount > MIN_MOB_COUNT;
        }
        
        if (increaseButton != null)
        {
            increaseButton.interactable = currentMobCount < MAX_MOB_COUNT;
        }
    }
}

