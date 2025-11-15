using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player input during combat (e.g., target cycling with Tab key)
/// Separated from CombatManager to keep input handling decoupled
/// </summary>
public class CombatInputHandler : MonoBehaviour
{
    private ICombatService combatService;
    
    void Start()
    {
        combatService = Services.Get<ICombatService>();
        
        if (combatService == null)
        {
            Debug.LogError("[CombatInputHandler] Failed to get ICombatService from Services.");
        }
    }
    
    void Update()
    {
        // Only handle input if combat service is available and fighting
        if (combatService == null) return;
        
        if (combatService.GetCombatState() == CombatManager.CombatState.Fighting)
        {
            // Handle Tab key for target cycling
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                combatService.CycleTarget();
            }
        }
    }
}

