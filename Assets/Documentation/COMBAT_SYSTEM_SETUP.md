# Combat System Setup

Auto-battle combat system for zone-based monster fighting.

## 🎯 **Core Scripts:**

### **Combat System:**
- `MonsterData.cs` - ScriptableObject for monster definitions
- `CombatManager.cs` - Singleton that manages combat logic and timers
- `CombatPanel.cs` - UI panel that displays combat and health bars
- `ZoneData.cs` - Updated to include monsters array
- `ZonePanel.cs` - Updated with Fight button

---

## 🛠️ **Setup Steps:**

### **1. Create Monster Data Assets:**
1. Right-click in Project → Create → Vinland → Monster
2. Name it (e.g., "GoblinMonster", "WolfMonster")
3. Configure monster stats:
   - **Monster Name:** Display name
   - **Monster Sprite:** Visual representation
   - **Base Health:** Starting health (scales with player level)
   - **Attack Damage:** Damage per attack
   - **Attack Speed:** Seconds between attacks
   - **Health Scaling:** Multiplier per level (1.05 = +5% per level)
   - **Damage Scaling:** Damage multiplier per level
   - **XP Reward:** XP gained on defeat
   - **Gold Reward:** Gold gained on defeat
   - **Item Drop:** Optional item (uses existing ItemData)
   - **Drop Chance:** 0.0 to 1.0 (0.25 = 25% chance)

### **2. Add Monsters to Zones:**
1. Select your ZoneData ScriptableObject
2. Find "Monsters in this Zone" section
3. Set array size (e.g., 5 for 5 monsters)
4. Assign your MonsterData assets

### **3. Setup Combat Manager (in Scene):**
1. Create empty GameObject: "CombatManager"
2. Add `CombatManager` component
3. Leave in scene (it auto-persists with DontDestroyOnLoad)

### **4. Setup Combat Panel UI:**
1. Create UI Panel for combat display
2. Add `CombatPanel` component
3. Assign references:
   - **Combat Panel:** The main panel GameObject
   - **Monster Sprite:** Image for monster
   - **Monster Name Text:** TextMeshProUGUI for name
   - **Monster Health Bar:** Slider for health
   - **Monster Health Text:** TextMeshProUGUI for HP numbers
   - **Monster Attack Progress Bar:** Slider for attack timer
   - **Player Health Bar:** Slider for player health
   - **Player Health Text:** TextMeshProUGUI for player HP
   - **Player Attack Progress Bar:** Slider for player attack timer
   - **Combat Log Text:** TextMeshProUGUI for messages
   - **Retreat Button:** Button to exit combat
   - **Continue Button:** Button shown after victory/defeat

### **5. Setup Fight Button:**
1. Select your ZonePanel GameObject
2. Create a "Fight" button in your zone UI
3. In ZonePanel component, assign the button to "Fight Button"

---

## 🎮 **How It Works:**

### **Combat Flow:**
1. Player clicks "Fight" button on ZonePanel
2. CombatPanel opens and displays first monster from zone
3. Auto-battle begins:
   - Player and monster both have attack timers
   - When timer fills, they attack automatically
   - Damage is dealt and health bars update
4. Monster defeated → Rewards given → Next monster loads
5. All monsters defeated → Victory screen
6. Player defeated → Respawn with full health

### **Combat Stats:**
- **Player Attack Speed:** 1.5 seconds (attacks every 1.5s)
- **Player Attack Damage:** 10 (fixed for now, can be upgraded later)
- **Monster Stats:** Scale with player level using scalin multipliers

### **Rewards:**
- **XP and Gold:** Always given on monster defeat
- **Item Drops:** Based on drop chance (e.g., 25% chance)
- Items automatically added to inventory

---

## 🔮 **Future Enhancements:**

These features are supported by the architecture but not yet implemented:

1. **Player Equipment System:**
   - Equip weapons to increase attack damage
   - Equip armor to increase max health
   - Equip accessories for special effects

2. **Active Abilities:**
   - Special attacks with cooldowns
   - Healing abilities
   - Buff/debuff abilities

3. **Passive Abilities:**
   - Lifesteal (heal on attack)
   - Critical hits (chance for 2x damage)
   - Dodge chance (chance to avoid damage)

4. **Monster Abilities:**
   - Special attacks with cooldowns
   - Status effects (poison, stun, etc.)

5. **Combat Stats:**
   - Track total monsters defeated
   - Track damage dealt/taken
   - Achievement system

---

## ✅ **Testing Checklist:**

- [ ] Created at least 1 MonsterData asset
- [ ] Added monsters to a ZoneData
- [ ] CombatManager exists in scene
- [ ] CombatPanel UI is set up with all references
- [ ] Fight button on ZonePanel is assigned
- [ ] Clicking Fight opens combat panel
- [ ] Auto-battle works (both sides attack)
- [ ] Monster death gives rewards
- [ ] Next monster loads automatically
- [ ] Victory/Defeat screens work
- [ ] Retreat button closes combat

---

## 📝 **Example Monster Setup:**

**Goblin (Level 1 Zone):**
- Base Health: 30
- Attack Damage: 3
- Attack Speed: 2.0s
- Health Scaling: 1.05 (grows 5% per player level)
- Damage Scaling: 1.05
- XP Reward: 10
- Gold Reward: 5

**Wolf (Level 1 Zone):**
- Base Health: 40
- Attack Damage: 4
- Attack Speed: 1.8s
- XP Reward: 15
- Gold Reward: 8

**Boss Orc (Level 2 Zone):**
- Base Health: 100
- Attack Damage: 8
- Attack Speed: 2.5s
- XP Reward: 50
- Gold Reward: 25
- Item Drop: Rare Material
- Drop Chance: 0.5 (50%)





