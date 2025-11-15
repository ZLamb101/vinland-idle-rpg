# Phase 3: Combat Manager Extraction

## ✅ Completed Changes

### 1. Extracted `CombatMonsterInstance` Class
**New File:** `Assets/Scripts/Combat/CombatMonsterInstance.cs`
- Moved the 35-line monster instance class to its own file
- Represents a single monster instance in combat with health, attack stats, and timers
- `[System.Serializable]` attribute preserved for Unity serialization

### 2. Extracted `CombatInputHandler` Component
**New File:** `Assets/Scripts/Combat/CombatInputHandler.cs`
- Separated Tab key handling from CombatManager
- MonoBehaviour component that listens for input during combat
- Uses `ICombatService` to call `CycleTarget()` on the combat manager
- Properly checks combat state before handling input

### 3. Updated `CombatManager`
**Changes:**
- Removed `CombatMonsterInstance` class definition (now imported)
- Removed Tab key input handling from `Update()` method
- Removed `using UnityEngine.InputSystem;` (no longer needed)
- Simplified Update loop to just `UpdateCombat()`
- Updated comment from "Singleton manager" to "Manager"

**Lines Reduced:** ~60 lines removed from CombatManager.cs

---

## 🎮 Required Unity Setup

### Add CombatInputHandler to Scene(s)

You need to add the new `CombatInputHandler` component to any scene with combat:

1. **Open `QuestingScene.unity`** (or any scene with combat)
2. Find or create a GameObject (e.g., use the existing Canvas or create "CombatInput")
3. **Add Component** → Search for `CombatInputHandler`
4. No configuration needed - it auto-finds the ICombatService

**Recommended:** Add it to the same GameObject that has `CombatPanel` or `CombatVisualManager` for organization.

---

## 🧠 Architecture Benefits

### Before:
```
CombatManager (840 lines)
├─ Combat Logic
├─ State Management
├─ Monster Instances (nested class)
├─ Input Handling (Tab key)
└─ Update Loop
```

### After:
```
CombatManager (780 lines)
├─ Combat Logic
├─ State Management
└─ Update Loop

CombatMonsterInstance (35 lines)
└─ Monster Data Structure

CombatInputHandler (35 lines)
└─ Input Handling via Services
```

### Why This is Better:
1. **Separation of Concerns**: Input handling is now separate from combat logic
2. **Reusability**: `CombatMonsterInstance` can be used independently
3. **Testability**: Input handler can be tested/replaced without touching CombatManager
4. **Maintainability**: Easier to find and modify specific functionality
5. **Scene-Based**: Input handler is a scene component (not persistent like CombatManager)

---

## 🔍 What Stayed in CombatManager

The core combat responsibilities remain together (and should stay together):
- Combat state machine (Idle/Fighting/Defeat)
- Monster spawning and management
- Attack timers for player and all monsters
- Damage calculation and resolution
- Event broadcasting for UI
- Integration with visual manager and rewards

**These are tightly coupled and belong together for performance and clarity.**

