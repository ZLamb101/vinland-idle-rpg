# Bootstrap Scene Implementation Guide

**Status**: Not Yet Implemented  
**Prerequisites**: Complete Service Locator Migration (Phase 6)  
**Estimated Time**: 2-3 hours  
**Risk Level**: Low

---

## Overview

A Bootstrap scene is the first scene that runs when the game starts. It creates all persistent managers with `DontDestroyOnLoad`, then loads the actual first game scene. This ensures all managers exist before any scene needs them, eliminating race conditions and "service not found" errors.

---

## Benefits

### ✅ Guaranteed Initialization
- All managers exist before any scene needs them
- No more "Service not found" errors
- No race conditions or timing issues

### ✅ Clean Architecture
- Scenes don't create managers
- Single responsibility: Bootstrap creates, scenes consume
- Clear separation of concerns

### ✅ Easier Testing
- Test individual scenes knowing managers exist
- Consistent state across all scenes

### ✅ Performance
- Managers created once, persist forever
- No destroy/recreate cycles between scenes

---

## Current Problem

**Without Bootstrap Scene:**
```
CharacterScene loads
  ↓
CharacterSelectionManager tries to create ZoneManager
  ↓
ZonePanel also tries to create ZoneManager
  ↓
Singleton conflicts, service registration issues
  ↓
Manual cleanup in ReturnToCharacterSelect
  ↓
Complex, error-prone lifecycle management
```

**With Bootstrap Scene:**
```
Bootstrap scene runs ONCE at game start
  ↓
All managers created with DontDestroyOnLoad
  ↓
CharacterScene loads
  ↓
All services already available
  ↓
GameScene loads
  ↓
Same managers persist, just reload data
  ↓
Simple, predictable lifecycle
```

---

## Implementation Steps

### Step 1: Create Bootstrap Scene

1. **Create new scene**: `File > New Scene`
2. **Save as**: `Assets/Scenes/Bootstrap.unity`
3. **Delete**: Main Camera, Directional Light (not needed)
4. **Create**: Empty GameObject named "GameBootstrap"

### Step 2: Create Bootstrap Script

**File**: `Assets/Scripts/GameBootstrap.cs`

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bootstrap script that initializes all persistent managers before loading the first scene.
/// This ensures all managers exist with DontDestroyOnLoad before any scene needs them.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("The first scene to load after bootstrap completes")]
    [SerializeField] private string firstSceneName = "CharacterScene";
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    void Awake()
    {
        Log("=== Game Bootstrap Started ===");
        
        // Create all persistent managers
        // Order matters! Create dependencies first
        CreateManager<Services>("Services"); // Service locator must exist first
        
        // Core gameplay managers
        CreateManager<CharacterManager>("CharacterManager");
        CreateManager<CombatManager>("CombatManager");
        CreateManager<ZoneManager>("ZoneManager");
        CreateManager<ResourceManager>("ResourceManager");
        
        // Equipment and progression
        CreateManager<EquipmentManager>("EquipmentManager");
        CreateManager<TalentManager>("TalentManager");
        CreateManager<ShopManager>("ShopManager");
        
        // Activities
        CreateManager<AwayActivityManager>("AwayActivityManager");
        
        // UI and utilities
        CreateManager<DialogueManager>("DialogueManager");
        CreateManager<GameLog>("GameLog");
        
        Log("=== All Managers Created ===");
        Log($"Loading first scene: {firstSceneName}");
        
        // Load the actual first scene
        // Managers will persist due to DontDestroyOnLoad
        SceneManager.LoadScene(firstSceneName);
    }
    
    /// <summary>
    /// Create a manager if it doesn't already exist
    /// </summary>
    private void CreateManager<T>(string managerName) where T : MonoBehaviour
    {
        // Check if manager already exists (shouldn't happen, but safe)
        T existing = FindObjectOfType<T>();
        if (existing != null)
        {
            Log($"⚠️ {managerName} already exists, skipping creation");
            return;
        }
        
        // Create new GameObject with manager component
        GameObject managerObj = new GameObject(managerName);
        T manager = managerObj.AddComponent<T>();
        
        // Manager's Awake() will handle:
        // - Singleton pattern
        // - DontDestroyOnLoad
        // - Service registration
        
        Log($"✓ Created {managerName}");
    }
    
    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[Bootstrap] {message}");
        }
    }
}
```

### Step 3: Setup Bootstrap Scene

1. **Select** the "GameBootstrap" GameObject
2. **Add Component** → GameBootstrap script
3. **Configure** in Inspector:
   - First Scene Name: `CharacterScene`
   - Show Debug Logs: `✓` (check)

### Step 4: Update Build Settings

**Critical**: Bootstrap must be scene index 0!

1. Open `File > Build Settings`
2. **Reorder scenes**:
   ```
   0. Bootstrap          ← MUST be first!
   1. CharacterScene
   2. GameScene
   3. [Other scenes]
   ```
3. Click **"Add Open Scenes"** if Bootstrap isn't in the list
4. Drag Bootstrap to the top (index 0)

### Step 5: Cleanup Existing Manager Creation

Now that Bootstrap creates all managers, remove redundant creation code:

#### A. Remove from CharacterSelectionManager.cs

**Delete these lines from Start():**
```csharp
// DELETE THIS BLOCK:
if (!Services.TryGet<IAwayActivityService>(out var awayActivityService))
{
    GameObject awayManagerObj = new GameObject("AwayActivityManager");
    awayActivityService = awayManagerObj.AddComponent<AwayActivityManager>();
}

// DELETE THIS BLOCK:
if (!Services.TryGet<IZoneService>(out var zoneService))
{
    GameObject zoneManagerObj = new GameObject("ZoneManager");
    zoneService = zoneManagerObj.AddComponent<ZoneManager>();
}
```

#### B. Remove from ZonePanel.cs

**Delete or comment out this block from Start():**
```csharp
// DELETE OR COMMENT OUT:
if (!Services.TryGet<IZoneService>(out zoneService))
{
    Debug.LogWarning("[ZonePanel] ZoneService not found, creating ZoneManager...");
    GameObject zoneManagerObj = new GameObject("ZoneManager");
    ZoneManager zoneManager = zoneManagerObj.AddComponent<ZoneManager>();
    StartCoroutine(WaitForZoneManager());
    return;
}
```

**Simplify to:**
```csharp
// Get Zone Service - should always exist due to Bootstrap
Services.TryGet<IZoneService>(out zoneService);

if (zoneService == null)
{
    Debug.LogError("[ZonePanel] ZoneService not found! Did Bootstrap scene run?");
    return;
}
```

#### C. Remove from CharacterLoader.cs

Check `EnsureCharacterManagerExists()` - if it creates CharacterManager, remove that logic.

### Step 6: Update ReturnToCharacterSelect.cs

Managers should **persist** when returning to character select, not be destroyed:

```csharp
void DestroyPersistentManagers()
{
    Debug.Log("[ReturnToCharacterSelect] Cleaning up for character switch");
    
    // DON'T destroy managers - they persist with DontDestroyOnLoad
    // Just clear their per-character state
    
    // Destroy CharacterManager - has character-specific data
    var characterManager = Services.Get<ICharacterService>();
    if (characterManager != null)
    {
        Destroy((characterManager as MonoBehaviour).gameObject);
    }
    
    // Destroy CombatManager - has combat state
    var combatManager = Services.Get<ICombatService>();
    if (combatManager != null)
    {
        Destroy((combatManager as MonoBehaviour).gameObject);
    }
    
    // KEEP ResourceManager, ZoneManager, EquipmentManager, etc.
    // They'll reload data for the new character
}
```

**OR** (Better approach): Don't destroy any managers, just reset their state:
```csharp
void PrepareForCharacterSwitch()
{
    Debug.Log("[ReturnToCharacterSelect] Preparing for character switch");
    
    // Managers persist, just clear character-specific state
    // They'll reload data when new character loads
    
    // Combat should be ended
    var combatService = Services.Get<ICombatService>();
    if (combatService != null && combatService.GetCombatState() != CombatManager.CombatState.Idle)
    {
        combatService.EndCombat();
    }
    
    // Resource gathering should stop
    var resourceService = Services.Get<IResourceService>();
    if (resourceService != null && resourceService.IsGathering())
    {
        resourceService.StopGathering();
    }
    
    // Away activity should be saved
    var awayService = Services.Get<IAwayActivityService>();
    if (awayService != null)
    {
        awayService.SaveAwayState();
        int currentSlot = PlayerPrefs.GetInt("ActiveCharacterSlot", -1);
        if (currentSlot >= 0)
        {
            awayService.SaveLastPlayedTime(currentSlot);
        }
    }
    
    // Managers will reload data when SaveData.ApplyToGameState() is called
}
```

---

## Testing Checklist

After implementing Bootstrap:

### Initial Load Test
- [ ] Start game from Bootstrap scene
- [ ] Verify all managers are created (check Debug logs)
- [ ] CharacterScene loads correctly
- [ ] All services are available in CharacterScene

### Character Creation Test
- [ ] Create a new character
- [ ] Enter game world
- [ ] All systems work (combat, inventory, zones, etc.)
- [ ] No "Service not found" errors

### Character Switch Test
- [ ] Play as Character 1
- [ ] Return to character select
- [ ] Select Character 2
- [ ] Enter game world
- [ ] Character 2's data loads correctly
- [ ] No leftover data from Character 1

### Scene Reload Test
- [ ] Play game normally
- [ ] Reload current scene (for testing)
- [ ] Managers still exist
- [ ] No duplicate managers created

### Build Test
- [ ] Build executable
- [ ] Run from executable (not editor)
- [ ] Bootstrap runs first
- [ ] Game works correctly

---

## Troubleshooting

### "Service not found" errors still occur
- Check Build Settings: Bootstrap must be index 0
- Check Bootstrap runs first when starting game
- Check all managers are in Bootstrap's CreateManager() calls

### Duplicate managers created
- Check managers have proper singleton pattern in Awake()
- Check Bootstrap's CreateManager() checks for existing managers
- Check no other scenes try to create managers

### Managers destroyed between scenes
- Verify managers call `DontDestroyOnLoad(gameObject)` in Awake()
- Check ReturnToCharacterSelect isn't destroying managers
- Check scene transitions don't unload DontDestroyOnLoad objects

### Wrong scene loads first
- Build Settings: Bootstrap must be scene index 0
- When testing, always start from Bootstrap scene
- Set Bootstrap as default scene in Unity preferences

---

## Advanced: Lazy Bootstrap

For even more flexibility, use lazy initialization:

```csharp
public static class ManagerBootstrap
{
    private static bool initialized = false;
    
    /// <summary>
    /// Automatically initializes managers before any scene loads
    /// Can be called from Bootstrap scene or runs automatically
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        if (initialized) return;
        initialized = true;
        
        Debug.Log("[ManagerBootstrap] Auto-initializing managers...");
        
        // Create all managers
        EnsureManager<CharacterManager>();
        EnsureManager<CombatManager>();
        EnsureManager<ZoneManager>();
        EnsureManager<ResourceManager>();
        EnsureManager<EquipmentManager>();
        EnsureManager<TalentManager>();
        EnsureManager<ShopManager>();
        EnsureManager<AwayActivityManager>();
        EnsureManager<DialogueManager>();
        EnsureManager<GameLog>();
    }
    
    private static void EnsureManager<T>() where T : MonoBehaviour
    {
        // Check if already exists
        if (Object.FindObjectOfType<T>() != null)
        {
            return;
        }
        
        // Create if needed
        GameObject obj = new GameObject(typeof(T).Name);
        obj.AddComponent<T>();
        Debug.Log($"[ManagerBootstrap] Created {typeof(T).Name}");
    }
}
```

This runs automatically before any scene loads using Unity's `RuntimeInitializeOnLoadMethod`. No Bootstrap scene needed, but less explicit control.

---

## Migration Timeline

**Current Status**: Service Locator Migration in progress

**Phase 6 (Cleanup & Finalization)**:
1. Complete service locator migration
2. Remove all `.Instance` usage except in managers themselves
3. Test thoroughly

**After Phase 6**:
1. Implement Bootstrap scene (this guide)
2. Remove redundant manager creation code
3. Simplify manager lifecycle
4. Test all scenarios

**Benefits After Bootstrap**:
- Cleaner code
- More reliable initialization
- Easier to test
- Industry-standard architecture

---

## References

### Unity Documentation
- [RuntimeInitializeOnLoadMethod](https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html)
- [DontDestroyOnLoad](https://docs.unity3d.com/ScriptReference/Object.DontDestroyOnLoad.html)
- [SceneManagement](https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.html)

### Best Practices
- Unity Manual: "Scene Management Best Practices"
- Game Programming Patterns: "Service Locator Pattern"
- Clean Code in Unity: "Initialization and Bootstrap"

---

**Last Updated**: 2025-01-15  
**Next Steps**: Complete Service Locator Migration (Phase 6), then implement this guide


