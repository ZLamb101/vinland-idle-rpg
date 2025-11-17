# Professions System - Quick Start

Get professions running in 5 minutes!

## ✅ **Already Done:**

The core system is fully implemented:
- ✅ ProfessionManager automatically created by GameBootstrap
- ✅ Save/load system integrated
- ✅ Resource gathering awards profession XP (active & away)
- ✅ Away rewards grant profession XP automatically
- ✅ 3 profession definitions created (Mining, Herbalism, Fishing)
- ✅ Copper Ore configured to award Mining XP

## 🚀 **Quick Setup (3 Steps):**

### **Step 1: Create Profession Entry Prefab (2 minutes)**

1. In your scene, create this UI structure:
```
ProfessionEntry
├── Background (Image) - Optional
├── Icon (Image)
├── NameText (TextMeshProUGUI)
├── LevelText (TextMeshProUGUI)
└── XPBar (Slider)
    └── Fill Area
        └── Fill (Image)
    └── XPText (TextMeshProUGUI) - as sibling
```

2. Add `ProfessionEntry` component to root
3. Assign all references in inspector:
   - Icon, NameText, LevelText, XPBar, **XPBarFill** (the Fill image), XPText
   - Background (optional)
4. Drag to Project to create prefab
5. Delete from scene

**Tip:** The XP bar fill will automatically match each profession's theme color!

### **Step 2: Setup Profession Panel (1 minute)**

1. In your game UI, create:
```
ProfessionPanel (Panel)
└── ProfessionContainer (Empty GameObject)
    └── Add VerticalLayoutGroup component
```

2. Add `ProfessionPanel` component to root
3. Assign in inspector:
   - `professionPanel` → the root panel
   - `professionContainer` → the container Transform
   - `professionEntryPrefab` → your prefab from Step 1
   - `professions` → Set size to 3, drag in:
     - Profession_Mining
     - Profession_Herbalism
     - Profession_Fishing

4. Set panel inactive by default

### **Step 3: Add Toggle Button (1 minute)**

1. Create UI Button (or use existing one)
2. In onClick() event:
   - Drag ProfessionPanel object
   - Select `ProfessionPanel.TogglePanel()`

### **🎉 Done! Test It:**

1. Play game
2. Click profession button → panel opens
3. Gather Copper Ore
4. Watch Mining XP increase!
5. At 83 XP → Level 2!

---

## 📦 **Profession Definitions:**

Three assets are ready to customize:
- `Assets/ScriptableObjects/Professions/Profession_Mining.asset`
- `Assets/ScriptableObjects/Professions/Profession_Herbalism.asset`
- `Assets/ScriptableObjects/Professions/Profession_Fishing.asset`

**Add Icons:**
1. Select a profession asset
2. Drag sprite to `Icon` field
3. Customize `Theme Color` if desired

---

## 🎯 **How It Works:**

### **Automatic Entry Generation:**
- Panel reads the `professions` array
- Creates one entry per profession automatically
- No manual UI setup needed!

### **Adding New Professions:**
1. Right-click → Create → Vinland → Profession
2. Configure name, icon, color
3. Add to enum in `ProfessionType.cs`
4. Drag to ProfessionPanel's `professions` array
5. Done! Entry appears automatically

---

## 🔧 **Configure Resources:**

Make any resource award profession XP:

1. Select ResourceData asset
2. Set:
   - **Profession Type** → Mining/Herbalism/Fishing
   - **Profession Level Required** → 1 (or higher)
   - **Profession XP Reward** → 10-50

**Example:**
```
Iron Ore:
- Profession Type: Mining
- Level Required: 5
- XP Reward: 15
```

---

## 💡 **Pro Tips:**

1. **Layout Groups:** Add VerticalLayoutGroup or GridLayoutGroup to container for automatic spacing
2. **Theme Colors:** Each profession has a color that tints its background
3. **Scalability:** Want Woodcutting? Just create the definition, entries auto-generate!
4. **Icons:** Use clear, recognizable icons (pickaxe for mining, herbs for herbalism, etc.)

---

## 📊 **XP Progression:**

- Level 1→2: 83 XP (8-9 Copper Ore)
- Level 2→3: 212 XP (~21 Copper Ore)
- Level 3→4: 346 XP (~35 Copper Ore)
- Exponential curve continues...

---

## ✅ **Verify It Works:**

**Active Gathering:**
- [ ] Profession panel toggles open/close
- [ ] 3 profession entries display (Mining, Herbalism, Fishing)
- [ ] Icons and names show correctly
- [ ] XP bars start at 0/83
- [ ] Gather Copper Ore → Mining XP increases
- [ ] XP bar fills visually
- [ ] At 83 XP → level up notification
- [ ] Level changes to "Level 2"
- [ ] XP resets for next level
- [ ] Save/load preserves progress

**Away Gathering:**
- [ ] Start gathering, wait a few minutes (or close game)
- [ ] Return → see away rewards with "Mining XP: +X"
- [ ] Collect rewards → profession levels up
- [ ] Profession panel reflects new level/XP

---

## 🎨 **Customize Look:**

**In ProfessionEntry Prefab:**
- Adjust layout spacing
- Change font sizes
- Add borders/shadows
- Customize slider colors
- Add hover effects

**In Profession Definitions:**
- Set unique icons per profession
- Customize theme colors
- Write descriptive text

---

## 🚀 **That's It!**

Your profession system is fully functional with:
- ✅ Prefab-based UI generation
- ✅ ScriptableObject data definitions
- ✅ Automatic XP tracking
- ✅ Per-character progression
- ✅ Save/load support

Start gathering and level up your professions! 🎉

