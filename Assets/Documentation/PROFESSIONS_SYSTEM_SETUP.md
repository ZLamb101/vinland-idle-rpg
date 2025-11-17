# Professions System Setup

Complete guide for the professions system (Mining, Herbalism, Fishing).

## 🎯 **Core Scripts:**

### **Profession System:**
- `ProfessionType.cs` - Enum for profession types (Mining, Herbalism, Fishing, Woodcutting)
- `ProfessionData.cs` - Holds profession levels and XP for a character
- `ProfessionDefinitionData.cs` - ScriptableObject defining profession visual data
- `ProfessionManager.cs` - Service that manages profession progression
- `IProfessionService.cs` - Service interface for profession management
- `ProfessionPanel.cs` - UI panel that dynamically generates profession entries
- `ProfessionEntry.cs` - UI component for individual profession display

### **Integration:**
- `ResourceData.cs` - Updated with profession fields (type, level required, XP reward)
- `ResourceManager.cs` - Awards profession XP when gathering
- `CharacterData.cs` - Stores profession data per character
- `SaveData.cs` - Saves/loads profession progress
- `GameBootstrap.cs` - Initializes ProfessionManager on startup

---

## 📊 **How It Works:**

### **Profession Leveling:**
- Each character has **independent** profession levels (Mining, Herbalism, Fishing)
- Start at **Level 1** with **0 XP**
- Gathering resources awards profession XP
- **Exponential XP curve** - each level requires more XP than the last
- **Unlimited progression** - no level cap

### **XP Formula:**
```
XP Required = 100 × (level - 1) × √level
```

**Example:**
- Level 1 → 2: ~83 XP
- Level 2 → 3: ~212 XP
- Level 3 → 4: ~346 XP

### **Gaining XP:**

**Active Gathering:**
```
Gather Copper Ore → +10 Mining XP (per item)
Gather Herb → +10 Herbalism XP (per item)
Gather Fish → +10 Fishing XP (per item)
```

**Away/Offline Gathering:**
```
Away for 10 minutes mining Copper Ore → +6000 Mining XP (600 cycles × 10 XP)
```
Profession XP is automatically calculated and awarded when you collect away rewards!

---

## 🛠️ **Setup Steps:**

### **1. ProfessionManager (Already Created):**
✅ ProfessionManager is automatically created by GameBootstrap
✅ Persists between scenes with DontDestroyOnLoad
✅ Registered with service locator

### **2. Create Profession Entry Prefab:**

**Prefab Structure:**
```
ProfessionEntry (Prefab)
├── BackgroundImage (Image) - Optional colored background
├── Icon (Image) - Profession icon
├── NameText (TMP) - "Mining"
├── LevelText (TMP) - "Level 1"
└── XPBar (Slider)
    └── XPText (TMP) - "0 / 83"
```

**Setup:**
1. Create the prefab structure in your scene
2. Add `ProfessionEntry` component to the root
3. Assign all UI references in the ProfessionEntry component:
   - `iconImage` - The icon Image
   - `backgroundImage` - Optional background Image (will be tinted by profession color)
   - `nameText` - TextMeshProUGUI for profession name
   - `levelText` - TextMeshProUGUI for level display
   - `xpBar` - Slider component
   - `xpBarFill` - The Fill Image inside the slider (will be colored by profession theme)
   - `xpText` - TextMeshProUGUI for XP text
4. Save as prefab

**Note:** The XP bar fill will automatically use the profession's theme color! Mining = gray, Herbalism = green, Fishing = blue.

### **3. Setup Profession Panel UI:**

**Panel Structure:**
```
ProfessionPanel (Panel GameObject)
├── ProfessionContainer (VerticalLayoutGroup or GridLayoutGroup)
│   └── [Entries spawn here dynamically]
└── Add ProfessionPanel component
```

**Assign References in ProfessionPanel Component:**
- `professionPanel` - The parent panel GameObject (for show/hide)
- `professionContainer` - Transform where entries will be spawned
- `professionEntryPrefab` - The ProfessionEntry prefab you created
- `professions` - Array of profession definitions (drag 3 assets: Mining, Herbalism, Fishing)

### **4. Setup Profession Definitions:**

Three profession definition assets are already created:
- `Assets/ScriptableObjects/Professions/Profession_Mining.asset`
- `Assets/ScriptableObjects/Professions/Profession_Herbalism.asset`
- `Assets/ScriptableObjects/Professions/Profession_Fishing.asset`

**Customize Each Profession:**
1. Select a profession asset in Project window
2. Configure:
   - **Profession Type** - Mining/Herbalism/Fishing
   - **Display Name** - What shows in UI
   - **Description** - Brief text about the profession
   - **Icon** - Sprite representing the profession
   - **Theme Color** - Used for background tinting

### **5. Add Toggle Button:**

Create a button to toggle the professions panel:

1. Create UI Button (e.g., "Professions" or "Skills")
2. Add onClick event → ProfessionPanel.TogglePanel()

---

## 📦 **Configure Resources:**

### **Copper Ore (Example - Already Configured):**
```
Resource_Copper.asset:
- Profession Type: Mining
- Profession Level Required: 1
- Profession XP Reward: 10
```

### **Add Profession to Any Resource:**

1. Select ResourceData asset in Project window
2. Set **Profession Type** (Mining, Herbalism, Fishing, Woodcutting)
3. Set **Profession Level Required** (1 = no requirement)
4. Set **Profession XP Reward** (suggested: 10-50 per gather)

**Examples:**
```
Iron Ore:
- Profession Type: Mining
- Level Required: 5
- XP Reward: 15

Silver Ore:
- Profession Type: Mining
- Level Required: 10
- XP Reward: 25

Wildflower:
- Profession Type: Herbalism
- Level Required: 1
- XP Reward: 10

Salmon:
- Profession Type: Fishing
- Level Required: 1
- XP Reward: 10
```

---

## 🎮 **Profession Requirements:**

### **Level Requirements on Resources:**
- Resources can require a minimum profession level
- Players can still **attempt to gather** if level is too low
- Game will show warning: **"Requires Mining Level X"**
- This is set per resource in the Inspector

### **Future Enhancement Ideas:**
- Reduce gather speed if under-leveled
- Add chance to fail gathering if too low level
- Show profession level in resource tooltip

---

## 💾 **Save System:**

### **Per-Character Professions:**
- Each character has **independent** profession progress
- Character 1: Mining Level 20
- Character 2: Mining Level 2
- Switching characters loads correct profession levels

### **Save/Load:**
✅ Professions automatically save with character
✅ Professions load when character is loaded
✅ No manual saving needed - handled by SaveSystem

---

## 🌙 **Away Rewards Integration:**

### **Profession XP While Offline:**
When you leave the game while gathering, you earn profession XP:

**Example:** Mining Copper Ore for 1 hour away
- Gather rate: 1/second = 3600 cycles
- 3600 cycles × 10 XP = **36,000 Mining XP**
- Could gain multiple profession levels!

### **Away Rewards Display:**
```
Welcome Back!
You were away for 1 hour Mining Copper Ore

Mining XP: +36,000  ← NEW!
Copper Ore: 3,600

[Collect]
```

### **How It Works:**
1. Leave game while gathering a resource
2. System calculates gather cycles based on time away
3. Profession XP = cycles × resource XP reward
4. Display shows profession XP gained
5. Click Collect → XP is applied
6. Profession panel updates with new level

✅ Works with all gathering activities (Mining, Herbalism, Fishing)
✅ Multiple level-ups possible from single away session
✅ Fully integrated with existing away rewards system

---

## 🎯 **Events:**

The system publishes these EventBus events:

### **ProfessionXPGainedEvent:**
```csharp
{
    ProfessionType profession;
    int xpGained;
    int currentXP;
    int xpRequired;
}
```

### **ProfessionLevelUpEvent:**
```csharp
{
    ProfessionType profession;
    int oldLevel;
    int newLevel;
}
```

**Usage:**
```csharp
EventBus.Subscribe<ProfessionLevelUpEvent>(OnProfessionLevelUp);

void OnProfessionLevelUp(ProfessionLevelUpEvent evt)
{
    Debug.Log($"{evt.profession} reached level {evt.newLevel}!");
}
```

---

## 🧪 **Testing Checklist:**

**Active Gathering:**
- [ ] Profession entries generate dynamically from definitions
- [ ] Icons and colors display correctly for each profession
- [ ] Gather Copper Ore → Mining XP increases
- [ ] Reach 83+ Mining XP → Level up to Mining 2
- [ ] XP bar updates in real-time
- [ ] Profession levels save per character
- [ ] Switch characters → different profession levels
- [ ] Save/load preserves profession progress
- [ ] Create high-level resource → shows level requirement
- [ ] Profession panel opens/closes with button
- [ ] All 3 professions display correctly
- [ ] Add new profession definition → automatically appears in panel

**Away Rewards:**
- [ ] Start gathering Copper Ore
- [ ] Leave game for a few minutes
- [ ] Return → away rewards panel shows
- [ ] "Mining XP: +X" appears in rewards
- [ ] Click Collect
- [ ] Mining profession level/XP updates
- [ ] Check profession panel shows correct values
- [ ] Test with different resources (herbs, fish)

---

## 🔮 **Future Enhancements:**

Easy to add later:
- **New professions** - Create new ProfessionDefinitionData asset, add to enum, done!
- **Profession perks** - Unlock abilities at certain levels
- **Rare resource chance** - Higher level = better drops
- **Gather speed bonus** - Higher level = faster gathering
- **Profession quests** - Complete tasks for bonus XP
- **Profession titles** - "Master Miner" at level 50
- **Cross-profession synergies** - Mining + Herbalism bonuses
- **Tooltips on hover** - Show profession description and benefits

---

## 📝 **Quick Reference:**

### **Access Profession Service:**
```csharp
IProfessionService profService = Services.Get<IProfessionService>();

int miningLevel = profService.GetProfessionLevel(ProfessionType.Mining);
int miningXP = profService.GetProfessionXP(ProfessionType.Mining);
int xpNeeded = profService.GetProfessionXPRequired(ProfessionType.Mining);

// Manually add XP (if needed)
profService.AddProfessionXP(ProfessionType.Mining, 50);
```

### **Get Profession from Resource Type:**
```csharp
ProfessionType profession = ProfessionTypeExtensions.GetProfessionForResourceType(ResourceType.Ore);
// Returns: ProfessionType.Mining
```

### **Check Resource Requirements:**
```csharp
ResourceData resource = // ... get resource
int playerLevel = profService.GetProfessionLevel(resource.professionType);
bool meetsRequirement = playerLevel >= resource.professionLevelRequired;

if (!meetsRequirement)
{
    string warning = $"Requires {resource.professionType.GetDisplayName()} Level {resource.professionLevelRequired}";
}
```

---

## ✅ **System Complete!**

Your professions system is fully functional:
- ✅ 3 professions (Mining, Herbalism, Fishing)
- ✅ Exponential XP progression
- ✅ Per-character profession levels
- ✅ Save/load support
- ✅ UI panel with XP bars
- ✅ Resource gathering awards XP
- ✅ Level requirements on resources
- ✅ EventBus integration

Gather resources to level up your professions! 🎉

