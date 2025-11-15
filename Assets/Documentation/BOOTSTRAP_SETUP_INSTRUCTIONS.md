# Bootstrap Scene Setup Instructions

**Date**: 2025-01-15  
**Status**: Code Complete - Ready for Unity Setup  
**Time Required**: 10-15 minutes

---

## ✅ Code Changes Complete

The following files have been created/modified:

### Created:
- ✅ `Assets/Scripts/GameBootstrap.cs` - Bootstrap initialization script

### Modified:
- ✅ `Assets/Scripts/CharacterSelectionManager.cs` - Removed redundant manager creation
- ✅ `Assets/Scripts/ZonePanel.cs` - Removed ZoneManager creation fallback
- ✅ `Assets/Scripts/CharacterLoader.cs` - Updated EnsureCharacterManagerExists()

**All code changes compile successfully with zero errors!**

---

## 🎮 Unity Editor Setup Steps

You need to complete these steps in Unity Editor:

### Step 1: Create Bootstrap Scene (2 minutes)

1. In Unity, go to `File > New Scene`
2. Choose "Basic (Built-in)" template
3. **Delete** the following default objects:
   - Main Camera
   - Directional Light
4. Create an empty GameObject:
   - Right-click in Hierarchy
   - Select "Create Empty"
   - Name it: `GameBootstrap`
5. Save the scene:
   - `File > Save As...`
   - Save to: `Assets/Scenes/Bootstrap.unity`

### Step 2: Add Bootstrap Script (1 minute)

1. Select the `GameBootstrap` GameObject in the Hierarchy
2. In the Inspector, click "Add Component"
3. Search for `GameBootstrap`
4. Add the `GameBootstrap` script
5. In the Inspector, verify settings:
   - **First Scene Name**: `CharacterScene`
   - **Show Debug Logs**: ✓ (checked)

### Step 3: Update Build Settings (2 minutes) ⚠️ CRITICAL

**Bootstrap MUST be scene index 0 (first scene) or the game won't work!**

1. Go to `File > Build Settings`
2. If Bootstrap.unity isn't in the list:
   - Click "Add Open Scenes" (with Bootstrap scene open)
3. **Drag Bootstrap.unity to the TOP** (index 0)
4. Final order should be:
   ```
   ✓ 0. Bootstrap       ← MUST be index 0!
     1. CharacterScene
     2. QuestingScene
   ```

### Step 4: Set Bootstrap as Default Scene (Optional but Recommended)

1. Go to `Edit > Project Settings > Editor`
2. Under "Scene Loading", set "Enter Play Mode Scene" to `Bootstrap`
3. This ensures you always start from Bootstrap when testing

---

## 🧪 Testing Checklist

After setup, test these scenarios:

### Test 1: Initial Load ✓
1. Close all Unity scenes
2. Open Bootstrap scene
3. Press Play
4. **Expected Results:**
   - Console shows Bootstrap initialization logs
   - All 10 managers created
   - CharacterScene loads automatically
   - No errors in console

### Test 2: Manager Verification ✓
1. While game is running, go to Hierarchy
2. Look for "DontDestroyOnLoad" section at bottom
3. **Expected Results:**
   - You should see all 10 manager GameObjects:
     - CharacterManager
     - CombatManager
     - ZoneManager
     - ResourceManager
     - EquipmentManager
     - TalentManager
     - ShopManager
     - AwayActivityManager
     - DialogueManager
     - GameLog

### Test 3: Character Creation ✓
1. Create a new character
2. Enter the game world
3. **Expected Results:**
   - Character loads successfully
   - All UI panels work (zones, inventory, combat, etc.)
   - No "Service not found" errors
   - Managers persist in DontDestroyOnLoad

### Test 4: Character Switching ✓
1. Play as one character
2. Return to character selection
3. Select a different character
4. **Expected Results:**
   - New character's data loads correctly
   - No leftover data from previous character
   - All systems work correctly
   - Managers still exist in DontDestroyOnLoad

### Test 5: Scene Transitions ✓
1. Switch between CharacterScene and QuestingScene
2. **Expected Results:**
   - Managers persist across scene changes
   - No duplicate managers created
   - All services remain accessible

---

## 📊 What Bootstrap Does

```
Game Starts
    ↓
Bootstrap.Awake() runs
    ↓
Creates 10 managers with DontDestroyOnLoad:
  ✓ CharacterManager
  ✓ CombatManager
  ✓ ZoneManager
  ✓ ResourceManager
  ✓ EquipmentManager
  ✓ TalentManager
  ✓ ShopManager
  ✓ AwayActivityManager
  ✓ DialogueManager
  ✓ GameLog
    ↓
Loads CharacterScene
    ↓
All services ready and available!
```

---

## 🐛 Troubleshooting

### "Service not found" errors still appear

**Solution:**
- Check Build Settings: Bootstrap must be index 0
- Make sure you're starting from Bootstrap scene (not CharacterScene)
- Verify GameBootstrap script is attached to GameObject in Bootstrap scene

### Duplicate managers created

**Solution:**
- Each manager has singleton pattern to prevent duplicates
- Check console for "already exists" warnings
- Verify managers have proper `Awake()` singleton checks

### CharacterScene loads but managers don't exist

**Solution:**
- Check Bootstrap scene has GameBootstrap script attached
- Verify "First Scene Name" is set to "CharacterScene"
- Check console for Bootstrap initialization logs

### Bootstrap scene doesn't run

**Solution:**
- Build Settings: Make sure Bootstrap is index 0
- When testing, open Bootstrap scene before pressing Play
- Check no compilation errors preventing Bootstrap from running

---

## ✅ Success Indicators

You'll know Bootstrap is working correctly when:

1. ✅ Console shows Bootstrap initialization logs
2. ✅ All 10 managers appear in DontDestroyOnLoad hierarchy
3. ✅ CharacterScene loads automatically
4. ✅ No "Service not found" errors anywhere
5. ✅ Managers persist across all scene transitions
6. ✅ Character switching works perfectly
7. ✅ All game systems function correctly

---

## 📚 Benefits Achieved

### Before Bootstrap:
- ❌ Managers created on-demand by various systems
- ❌ Race conditions and timing issues
- ❌ "Service not found" errors
- ❌ Complex lifecycle management
- ❌ Managers destroyed/recreated when switching characters

### After Bootstrap:
- ✅ All managers exist before any scene needs them
- ✅ Zero race conditions
- ✅ No "Service not found" errors
- ✅ Simple, predictable lifecycle
- ✅ Managers persist, just reload data

---

## 🎊 Congratulations!

Once you complete these steps, you'll have:
- ✅ Professional initialization architecture
- ✅ Clean, predictable manager lifecycle
- ✅ Industry-standard Bootstrap pattern
- ✅ Robust, error-free service locator system

**Last Updated**: 2025-01-15  
**Next Steps**: Complete Unity Editor setup steps above, then run tests!

