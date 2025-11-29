using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple helper component for Settings buttons.
/// Attach to any button to make it toggle the Settings panel via the service.
/// </summary>
[RequireComponent(typeof(Button))]
public class SettingsButton : MonoBehaviour
{
    private Button button;
    
    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }
    
    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }
    
    private void OnClick()
    {
        var settingsService = Services.Get<ISettingsService>();
        if (settingsService != null)
        {
            settingsService.TogglePanel();
        }
        else
        {
            Debug.LogWarning("[SettingsButton] ISettingsService not found. Is SettingsPanel in the scene?");
        }
    }
}

