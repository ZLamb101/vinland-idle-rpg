# Zone System Setup

Guide for setting up the zone-based quest system.

## 🎯 **Zone System Overview:**

### **Structure:**
- **Zones** contain multiple quests
- **Players navigate** between zones with arrows
- **Quests unlock** based on player level and zone access
- **Save system** remembers current zone

## 🛠️ **Setup Steps:**

### **1. Create Zones:**
1. **Right-click in Project** → Create → Vinland → Zone
2. **Create multiple zones:**
   - `Zone_1_1` (Forest Start)
   - `Zone_1_2` (Deep Forest) 
   - `Zone_1_3` (Mountain Pass)

### **2. Configure Each Zone:**
```
Zone 1-1:
- Zone Name: "Forest Start"
- Level Required: 1
- Available Quests: [Gather Berries, Hunt Rabbits]

Zone 1-2:
- Zone Name: "Deep Forest"
- Level Required: 5
- Prerequisite Zone: Zone_1_1
- Available Quests: [Fight Wolves, Find Herbs]

Zone 1-3:
- Zone Name: "Mountain Pass"
- Level Required: 10
- Prerequisite Zone: Zone_1_2
- Available Quests: [Climb Cliffs, Mine Ore]
```

### **3. Setup ZoneManager:**
1. **Create empty GameObject** called "ZoneManager"
2. **Add ZoneManager component**
3. **Assign all zones** to "All Zones" array
4. **Set Starting Zone** to Zone_1_1

### **4. Setup ZonePanel UI:**
1. **Create ZonePanel GameObject** in your scene
2. **Add ZonePanel component**
3. **Assign UI references:**
   - Zone Name Text
   - Previous/Next Zone Buttons
   - Quest Icon Button (to open quest zone)
   - Quest Container (for quest panels - hidden initially)

### **5. Quest Container Setup:**
1. **Create Vertical Layout Group** for quest container
2. **Set up quest panel prefab** (optional)
3. **Assign to ZonePanel's Quest Container**

## 🎮 **How It Works:**

### **Zone Navigation:**
- **Previous Arrow** ← Takes you to previous zone
- **Next Arrow** → Takes you to next zone (if unlocked)
- **Auto-save** current zone progress

### **Quest System:**
- **Zone loads** → Quest container is hidden initially
- **Click Quest Icon** → Opens quest zone and shows available quests
- **Level up** → New quests may become available
- **Zone change** → Loads different set of quests

### **Progression:**
- **Complete quests** → Gain XP and level up
- **Reach level 5** → Unlock Zone 1-2
- **Reach level 10** → Unlock Zone 1-3

## ✅ **Benefits:**

- **Organized content** - Quests grouped by zones
- **Progressive difficulty** - Higher level zones have harder quests
- **Clean navigation** - Easy zone switching
- **Scalable system** - Easy to add new zones and quests

The zone system provides a structured way to organize your game content! 🎉
