# Service Locator Migration Plan - Step by Step

**Date Created**: 2025-01-10  
**Status**: ✅ **COMPLETE**  
**Current Phase**: Migration Complete! 🎉

---

## Overview

This document provides a step-by-step plan to migrate from singleton pattern (`Manager.Instance`) to service locator pattern (`Services.Get<T>()`). The migration is broken into phases to minimize risk and allow incremental progress.

**Current State:**
- 10 managers with `Instance` properties
- 414 `.Instance` usages across 35 files
- 8 core managers already have interfaces and register with Services
- 2 managers (DialogueManager, GameLog) need interfaces created

**Target State:**
- All managers accessible via `Services.Get<T>()`
- No `.Instance` usage in production code
- All managers have interfaces
- Clean, testable architecture

---

## Migration Principles

1. **Backward Compatibility**: Keep `Instance` properties during migration
2. **Incremental**: One phase at a time, test after each
3. **Low Risk First**: Start with low-risk changes, build confidence
4. **Test Thoroughly**: Verify each phase before moving to next
5. **Rollback Ready**: Can pause/resume at any phase

---

## Phase 1: Foundation - Create Missing Interfaces

**Risk Level**: Low  
**Estimated Time**: 1-2 hours  
**Impact**: Foundation for all future phases

### Goals
- Create interfaces for DialogueManager and GameLog
- Make managers implement interfaces
- Register with Services (keep Instance for now)

### Step 1.1: Create IDialogueService Interface

**File to Create**: `Assets/Scripts/Interfaces/IDialogueService.cs`

**Interface Contract** (based on DialogueManager public API):
- Events: `OnDialogueStarted`, `OnDialogueTextChanged`, `OnDialogueEnded`
- Methods: `StartDialogue(NPCData)`, `NextDialogue()`, `EndDialogue()`
- Getters: `IsDialogueActive()`, `GetCurrentNPC()`, `HasMoreDialogue()`

### Step 1.2: Create IGameLogService Interface

**File to Create**: `Assets/Scripts/Interfaces/IGameLogService.cs`

**Interface Contract** (based on GameLog public API):
- Methods: `AddLogEntry(string, LogType)`, `AddCombatLogEntry(string, LogType)`, `ToggleLog()`, `ClearLog()`
- Getters: `IsLogVisible()`, `GetLogEntryCount()`

**Note**: GameLog might be better as a utility class, but we'll create interface for consistency.

### Step 1.3: Update DialogueManager

**File to Modify**: `Assets/Scripts/DialogueManager.cs`

**Changes**:
- Add `: IDialogueService` to class declaration
- Add `Services.Register<IDialogueService>(this);` in `Awake()`
- Add `Services.Unregister<IDialogueService>();` in `OnDestroy()`
- Keep `Instance` property (for backward compatibility)

### Step 1.4: Update GameLog

**File to Modify**: `Assets/Scripts/GameLog.cs`

**Changes**:
- Add `: IGameLogService` to class declaration
- Add `Services.Register<IGameLogService>(this);` in `Awake()`
- Add `Services.Unregister<IGameLogService>();` in `OnDestroy()`
- Keep `Instance` property (for backward compatibility)

### Testing Checklist for Phase 1
- [ ] Code compiles without errors
- [ ] DialogueManager registers with Services
- [ ] GameLog registers with Services
- [ ] Can access via `Services.Get<IDialogueService>()`
- [ ] Can access via `Services.Get<IGameLogService>()`
- [ ] Old `Instance` access still works
- [ ] Game runs without errors

---

## Phase 2: Core Systems Migration

**Risk Level**: Medium  
**Estimated Time**: 5-7 hours  
**Impact**: High (core gameplay systems)

### Phase 2.1: Migrate Combat System

**Files Affected** (40 usages):
- `CombatManager.cs` - Replace internal Instance checks
- `CombatPanel.cs` - Replace 14 usages
- `CombatVisualManager.cs` - Replace 19 usages
- `MonsterPanel.cs` - Replace 5 usages
- `TargetFrame.cs` - Replace 16 usages
- `Combat/CombatLogic.cs` - Replace 11 usages

**Strategy**:
- Cache `ICombatService` reference in `Start()` for UI components
- Use `Services.Get<ICombatService>()` for one-off calls
- Update event subscriptions to use service reference

**Pattern Example**:
```csharp
// Before
if (CombatManager.Instance != null)
{
    CombatManager.Instance.OnCombatStateChanged += OnCombatStateChanged;
}

// After
private ICombatService combatService;

void Start()
{
    combatService = Services.Get<ICombatService>();
    if (combatService != null)
    {
        combatService.OnCombatStateChanged += OnCombatStateChanged;
    }
}
```

### Phase 2.2: Migrate Character System

**Files Affected** (80+ usages):
- `CharacterManager.cs` - Internal usage
- `CharacterLoader.cs` - 1 usage
- `CharacterInfoDisplay.cs` - 23 usages
- `CharacterSlot.cs` - 4 usages
- `CharacterSelectionManager.cs` - 8 usages
- `InventoryUI.cs` - 2 usages
- `ReturnToCharacterSelect.cs` - 9 usages
- `GameLog.cs` - 11 usages
- `SaveSystem/SaveData.cs` - 24 usages

**Strategy**:
- Cache `ICharacterService` reference where frequently accessed
- Update event subscriptions carefully
- Handle initialization order (CharacterLoader creates CharacterManager)

---

## Phase 3: Game Systems Migration

**Risk Level**: Medium  
**Estimated Time**: 3-6 hours  
**Impact**: Medium

### Phase 3.1: Migrate Equipment System
- `EquipmentManager.cs`, `EquipmentPanel.cs`, `InventorySlot.cs`
- ~23 usages total

### Phase 3.2: Migrate Talent System
- `TalentManager.cs`, `TalentPanel.cs`
- ~24 usages total

### Phase 3.3: Migrate Shop System
- `ShopManager.cs`, `ShopPanel.cs`, `ShopItemSlot.cs`
- ~43 usages total

---

## Phase 4: Zone & Resource Systems

**Risk Level**: Low-Medium  
**Estimated Time**: 3-6 hours  
**Impact**: Medium

### Phase 4.1: Migrate Zone System
- `ZoneManager.cs`, `ZonePanel.cs`
- ~29 usages total

### Phase 4.2: Migrate Resource System
- `ResourceManager.cs`, `ResourcePanel.cs`
- ~27 usages total

### Phase 4.3: Migrate Away Activity System
- `AwayActivityManager.cs`
- ~20+ usages across multiple files

---

## Phase 5: UI & Utility Systems

**Risk Level**: Low  
**Estimated Time**: 2 hours  
**Impact**: Low

### Phase 5.1: Migrate Dialogue System
- `DialogueManager.cs`, `DialoguePanel.cs`
- ~18 usages total

### Phase 5.2: Migrate GameLog System
- `GameLog.cs`
- ~11 usages total

---

## Phase 6: Cleanup & Finalization

**Risk Level**: Low  
**Estimated Time**: 3-4 hours  
**Impact**: Low (cosmetic)

### Step 6.1: Remove Instance Properties
- Remove `public static XManager Instance` from all managers
- Remove singleton initialization code
- Keep `DontDestroyOnLoad` if needed

### Step 6.2: Remove ServiceMigrationHelper
- Delete `ServiceMigrationHelper.cs`
- Update `CharacterLoader.cs` to use direct `Services.Get<T>()`

### Step 6.3: Update Documentation
- Mark migration complete
- Update architecture diagrams
- Add usage examples

---

## Migration Patterns

### Pattern 1: Simple Replacement
```csharp
// Before
if (CharacterManager.Instance != null)
{
    CharacterManager.Instance.AddGold(100);
}

// After
var characterService = Services.Get<ICharacterService>();
if (characterService != null)
{
    characterService.AddGold(100);
}
```

### Pattern 2: Cached Reference (For Frequent Access)
```csharp
// Before
void Update()
{
    if (CombatManager.Instance != null)
    {
        CombatManager.Instance.DoSomething();
    }
}

// After
private ICombatService combatService;

void Start()
{
    combatService = Services.Get<ICombatService>();
}

void Update()
{
    if (combatService != null)
    {
        combatService.DoSomething();
    }
}
```

### Pattern 3: Event Subscriptions
```csharp
// Before
void Start()
{
    if (CharacterManager.Instance != null)
    {
        CharacterManager.Instance.OnLevelUp += OnLevelUp;
    }
}

void OnDestroy()
{
    if (CharacterManager.Instance != null)
    {
        CharacterManager.Instance.OnLevelUp -= OnLevelUp;
    }
}

// After
private ICharacterService characterService;

void Start()
{
    characterService = Services.Get<ICharacterService>();
    if (characterService != null)
    {
        characterService.OnLevelUp += OnLevelUp;
    }
}

void OnDestroy()
{
    if (characterService != null)
    {
        characterService.OnLevelUp -= OnLevelUp;
    }
}
```

### Pattern 4: Using Injectable Base Class
```csharp
// For new UI components
public class MyPanel : Injectable
{
    private ICharacterService characterService;
    
    void Start()
    {
        characterService = GetService<ICharacterService>();
    }
}
```

---

## Testing Strategy

### After Each Phase:
1. **Compile Check** - Ensure no compilation errors
2. **Runtime Test** - Verify game runs without errors
3. **Feature Test** - Test affected systems manually
4. **Log Check** - Look for service registration warnings

### Testing Checklist Per Manager:
- [ ] Service registers correctly in Awake()
- [ ] Service accessible via Services.Get<T>()
- [ ] All features work as before
- [ ] No null reference exceptions
- [ ] Event subscriptions work correctly
- [ ] Save/load still works

---

## Risk Assessment

| Phase | Risk Level | Impact if Broken | Rollback Difficulty |
|-------|------------|------------------|---------------------|
| Phase 1 (Interfaces) | Low | Low | Easy (just add registration) |
| Phase 2 (Core Systems) | Medium | High | Medium (revert Instance usage) |
| Phase 3 (Game Systems) | Medium | Medium | Medium |
| Phase 4 (Zone/Resource) | Low-Medium | Medium | Easy |
| Phase 5 (UI/Utility) | Low | Low | Easy |
| Phase 6 (Cleanup) | Low | Low | Easy (keep Instance properties) |

---

## Estimated Timeline

- **Phase 1:** 1-2 hours ✅ (Current)
- **Phase 2:** 5-7 hours
- **Phase 3:** 3-6 hours
- **Phase 4:** 3-6 hours
- **Phase 5:** 2 hours
- **Phase 6:** 3-4 hours

**Total:** 17-27 hours (2-3 days of focused work)

---

## Success Criteria

- [ ] All managers accessible via `Services.Get<T>()`
- [ ] Zero `.Instance` usage in production code (except managers themselves)
- [ ] All managers have interfaces
- [ ] All tests pass
- [ ] Game runs without errors
- [ ] No performance regressions
- [ ] Documentation updated

---

## Progress Tracking

- [x] Phase 1: Foundation - Create Missing Interfaces ✅ **COMPLETE**
  - [x] Created IDialogueService interface
  - [x] Created IGameLogService interface
  - [x] Updated DialogueManager to implement IDialogueService and register with Services
  - [x] Updated GameLog to implement IGameLogService and register with Services
- [x] Phase 2.1: Migrate Combat System ✅ **COMPLETE**
  - [x] Migrated CombatPanel.cs (cached service reference)
  - [x] Migrated CombatVisualManager.cs (cached service reference)
  - [x] Migrated MonsterPanel.cs (one-off service calls)
  - [x] Migrated TargetFrame.cs (cached service reference)
  - [x] Migrated CombatLogic.cs (static class, one-off service calls)
- [x] Phase 2.2: Migrate Character System ✅ **COMPLETE**
  - [x] Migrated CharacterInfoDisplay.cs (cached service reference)
  - [x] Migrated ReturnToCharacterSelect.cs (one-off service calls)
  - [x] Migrated SaveData.cs (static methods, one-off service calls)
  - [x] Migrated InventoryUI.cs (one-off service calls)
  - [x] Migrated GameLog.cs (cached service reference)
  - [x] Migrated CharacterLoader.cs (service check)
- [x] Phase 3.1: Migrate Equipment System ✅ **COMPLETE**
  - [x] Migrated EquipmentPanel.cs (cached service reference)
  - [x] Migrated InventorySlot.cs (one-off service calls)
  - [x] Migrated CombatManager.cs (one-off service calls)
  - [x] Migrated SaveData.cs (static methods, one-off service calls)
- [x] Phase 3.2: Migrate Talent System ✅ **COMPLETE**
  - [x] Migrated TalentPanel.cs (cached service reference)
  - [x] Migrated TalentButton.cs (access via TalentPanel.GetTalentService())
  - [x] Migrated CharacterManager.cs (one-off service calls)
  - [x] Migrated CombatManager.cs (one-off service calls)
  - [x] Migrated SaveData.cs (static methods, one-off service calls)
- [x] Phase 3.3: Migrate Shop System ✅ **COMPLETE**
  - [x] Migrated ShopPanel.cs (cached service reference)
  - [x] Migrated ShopItemSlot.cs (one-off service calls)
  - [x] Migrated InventorySlot.cs (one-off service calls)
  - [x] Migrated DialogueManager.cs (one-off service calls)
  - [x] Migrated NPCPanel.cs (one-off service calls)
  - [x] Migrated ZonePanel.cs (one-off service calls)
- [x] Phase 4.1: Migrate Zone System ✅ **COMPLETE**
  - [x] Migrated ZoneManager.cs (implements IZoneService, registers with Services, uses ICharacterService)
  - [x] Migrated ZonePanel.cs (cached service reference)
  - [x] Migrated CharacterSelectionManager.cs (one-off service calls)
  - [x] Migrated SaveData.cs (one-off service calls)
  - [x] Migrated ReturnToCharacterSelect.cs (service-based cleanup)
  - [x] Fixed singleton conflict issue with OnDestroy() checks
  - [x] Implemented TryGet pattern for optional services
- [x] Phase 4.2: Migrate Resource System ✅ **COMPLETE**
  - [x] ResourceManager.cs already implements IResourceService and registers with Services
  - [x] ResourcePanel.cs uses service references
  - [x] Fixed OnDestroy() to check Instance before unregistering
- [x] Phase 4.3: Migrate Away Activity System ✅ **COMPLETE**
  - [x] Created comprehensive IAwayActivityService interface with 36 methods
  - [x] AwayActivityManager implements IAwayActivityService and registers with Services
  - [x] Migrated all consumer files to use TryGet pattern (SaveData, ReturnToCharacterSelect, CombatManager, CharacterSelectionManager, CharacterManager, CharacterSlot, ResourceManager)
  - [x] Fixed OnDestroy() to check Instance before unregistering
  - [x] Zero .Instance usages in production code
- [x] Phase 5.1: Migrate Dialogue System ✅ **COMPLETE**
  - [x] Created IDialogueService interface with events and control methods
  - [x] DialogueManager implements IDialogueService and registers with Services
  - [x] Migrated DialoguePanel.cs with proper event subscription/unsubscription pattern
  - [x] Migrated NPCPanel.cs to use TryGet pattern
  - [x] Migrated ShopManager.cs to use TryGet pattern
  - [x] Fixed OnDestroy() to check Instance before unregistering
  - [x] Zero .Instance usages in production code
- [x] Phase 5.2: Migrate GameLog System ✅ **COMPLETE**
  - [x] Created IGameLogService interface with log entry and control methods
  - [x] GameLog implements IGameLogService and registers with Services
  - [x] GameLog caches ICharacterService reference for event subscriptions
  - [x] Migrated CombatManager.cs to cache gameLogService (14 usages)
  - [x] Fixed OnDestroy() to check Instance before unregistering
  - [x] Zero .Instance usages in production code
  - [x] All manager migrations complete!

🎉 **ALL MANAGER MIGRATIONS COMPLETE!** 🎉
All 10 managers now use the Service Locator pattern with proper interfaces, registration, and cleanup.

- [x] Phase 6: Cleanup & Finalization ✅ **COMPLETE**
  - [x] Replaced all 21 ServiceMigrationHelper usages across 6 files
  - [x] Deleted ServiceMigrationHelper.cs (no longer needed)
  - [x] Added [Obsolete] attributes to all Instance properties (triggers compile errors)
  - [x] Refactored managers to use private instance fields internally
  - [x] Zero `.Instance` usages in production code
  - [x] Zero compilation errors
  - [x] Migration documentation updated

## 🎊 **MIGRATION COMPLETE!** 🎊

### Final Statistics:
- **10 Managers Migrated**: All using Service Locator pattern
- **21 ServiceMigrationHelper Usages**: All replaced with direct Services.Get<T>()
- **28 Files Modified**: Including managers, UI components, calculators, and loaders
- **All `.Instance` Usages**: Eliminated from production code (only in example files)
- **[Obsolete] Attributes**: Added to all Instance properties to prevent future misuse
- **Zero Compilation Errors**: Clean codebase ready for production

### Key Achievements:
✅ **Loose Coupling**: All managers accessed via interfaces  
✅ **Testability**: Easy to mock services for unit testing  
✅ **Maintainability**: Clear separation of concerns  
✅ **Flexibility**: Can swap implementations easily  
✅ **Type Safety**: Compile-time checks for service availability  
✅ **Clean Architecture**: Service locator pattern properly implemented

### Architecture Benefits:
- **Before**: Tight coupling via `CharacterManager.Instance.AddGold(10)`
- **After**: Loose coupling via `Services.Get<ICharacterService>().AddGold(10)`
- **Interfaces**: All 10 managers have well-defined service interfaces
- **Registration**: Managers self-register in `Awake()`, auto-cleanup in `OnDestroy()`
- **Singleton Safety**: Internal singleton pattern prevents duplicates while external code uses service locator

### Optional Future Enhancements:
- [ ] Implement Bootstrap scene for centralized manager creation (see BOOTSTRAP_SCENE_GUIDE.md)
- [ ] Add `Services.Require<T>()` extension that throws if service not found
- [ ] Create service dependency injection for MonoBehaviours
- [ ] Add service lifetime management (transient, scoped, singleton)

---

## Notes

- Keep `Instance` properties during migration for backward compatibility
- Test thoroughly after each phase
- Can pause migration at any phase - code will still work
- Consider creating a helper extension method: `Services.Require<T>()` that throws if null
- May want to add `[Obsolete]` attributes to Instance properties during migration

---

**Last Updated**: 2025-01-15  
**Status**: ✅ **MIGRATION COMPLETE** - All phases finished successfully!

