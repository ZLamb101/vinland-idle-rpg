using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI button component to return to character selection screen.
/// Delegates all business logic to CharacterSessionService.
/// </summary>
public class ReturnToCharacterSelectButton : MonoBehaviour
{
    [Header("Settings")]
    public string characterSceneName = "CharacterScene";
    
    private Button button;
    
    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
    }
    
    /// <summary>
    /// Handle button click - save and return to character selection
    /// </summary>
    public void OnButtonClick()
    {
        int currentSlot = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        
        if (currentSlot < 0)
        {
            Debug.LogWarning("[ReturnToCharacterSelectButton] No active character slot - cannot save");
            return;
        }
        
        // Delegate to CharacterSessionService
        if (Services.TryGet<ICharacterSessionService>(out var sessionService))
        {
            sessionService.SaveAndExitToCharacterSelection(currentSlot, characterSceneName);
        }
        else
        {
            Debug.LogError("[ReturnToCharacterSelectButton] CharacterSessionService not found!");
        }
    }
}

