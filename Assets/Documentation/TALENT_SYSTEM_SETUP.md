# Talent System Setup

WoW-inspired talent tree system perfectly balanced for idle gameplay!

## 🎯 **Core Scripts:**

### **Talent System:**
- `TalentData.cs` - ScriptableObject for individual talents
- `TalentManager.cs` - Singleton that manages points and unlocks
- `TalentPanel.cs` - UI panel for talent tree display
- `CharacterManager.cs` - Updated to provide talent-modified stats
- `CombatManager.cs` - Updated to use talent bonuses

---

## 🌳 **Talent Tree Structure:**

### **3 Talent Trees:**
1. **Combat** - Offensive talents (damage, crit, attack speed)
2. **Defense** - Survival talents (health, armor, dodge)
3. **Utility** - Quality of life (XP, gold, special effects)

### **Talent Tiers:**
- **Tier 1** - Entry talents (0 points required)
- **Tier 2** - Requires 5 points in tree
- **Tier 3** - Requires 10 points in tree
- **Tier 4** - Requires 15 points in tree
- **Tier 5** - Requires 20 points in tree
- **Tier 6** - Requires 25 points in tree
- **Tier 7** - Capstone (requires 30 points in tree)

### **Talent Types:**
- **Single-point talents** - One-time unlock (maxRanks = 1)
- **Ranked talents** - Can invest multiple points (maxRanks = 3-5)

---

## 📊 **Talent Stats:**

### **Additive Bonuses (Flat Numbers):**
- **Attack Damage** - +5, +10, +15 per rank
- **Max Health** - +20, +50, +100 per rank
- **Attack Speed** - -0.1s, -0.2s per rank (faster attacks)

### **Percentage Multipliers:**
- **Damage Multiplier** - 5%, 10% more damage per rank
- **Health Multiplier** - 10% more max health per rank
- **Critical Chance** - 2%, 3% crit chance per rank
- **Critical Damage** - Increases crit from 2x to 2.1x, 2.2x, etc.

### **Special Stats:**
- **Lifesteal** - 3%, 5% heal per damage dealt
- **Dodge** - 2%, 3% chance to avoid attacks
- **Armor** - 2%, 3% damage reduction
- **XP Bonus** - 5%, 10% more XP from kills
- **Gold Bonus** - 5%, 10% more gold from kills

---

## 🛠️ **Setup Steps:**

### **1. Create Talent Manager (in Scene):**
1. Create empty GameObject: "TalentManager"
2. Add `TalentManager` component
3. Leave in scene (it persists automatically)

### **2. Create Talents:**

Right-click in Project → Create → Vinland → Talent

**Example Combat Talent:**
```
Name: "Brute Force"
Description: "Increases your attack damage"
Tree: Combat
Tier: 1
Position: 0
Points Required: 0
Max Ranks: 5
Attack Damage Bonus: 5 (gains +5 per rank)
```

**Example Defense Talent:**
```
Name: "Thick Skin"
Description: "Increases your maximum health"
Tree: Defense
Tier: 1
Position: 0
Points Required: 0
Max Ranks: 3
Max Health Bonus: 50 (gains +50 per rank)
```

**Example Utility Talent:**
```
Name: "Treasure Hunter"
Description: "Increases gold gained from monsters"
Tree: Utility
Tier: 1
Position: 0
Points Required: 0
Max Ranks: 5
Gold Bonus: 0.05 (5% more gold per rank)
```

### **3. Setup Talent Panel UI:**

Create the talent panel interface:

```
TalentPanel (Panel)
├── Header
│   ├── TalentPointsText ("Talent Points: X")
│   └── TreeSummaryText ("Combat: 15 points")
├── TreeTabs
│   ├── CombatTab (Button)
│   ├── DefenseTab (Button)
│   └── UtilityTab (Button)
├── TalentContainer (GridLayout or custom)
│   └── [Talent buttons spawn here]
├── TooltipPanel (Hidden by default)
│   ├── NameText
│   └── DescriptionText
└── ResetButton
```

Assign references in `TalentPanel` component:
- All UI elements
- **All Talents** array (assign all TalentData assets)
- Talent Button Prefab

### **4. Create Talent Button Prefab:**

Create a prefab for talent buttons:

```
TalentButton (Button)
├── Icon (Image)
├── Border (Image) - Color changes based on state
└── RankText (TextMeshProUGUI) - Shows "3/5" or "✓"
```

Add `TalentButton` component (it's automatically added by TalentPanel).

---

## 🎮 **How It Works:**

### **Earning Points:**
- Level up → **+1 talent point** (automatic)
- Points are tracked by TalentManager
- Can spend them in any of the 3 trees

### **Unlocking Talents:**
1. Open talent panel
2. **Hover over a talent** to see tooltip with details
3. **Left-click to invest a point** directly
4. Talent unlocked → bonuses apply immediately
5. Can continue adding ranks (if maxRanks > 1)

### **Requirements:**
- **Tree Points:** Must have X points in tree to unlock higher tiers
- **Prerequisites:** Some talents require another talent first
- **Max Ranks:** Can't exceed max ranks

### **Respec:**
- Click "Reset" button
- Costs gold (configurable)
- Refunds all talent points
- Start fresh!

---

## 🎯 **Example Talent Tree Ideas:**

### **Combat Tree:**

**Tier 1 (0 points required):**
- **Strength (5 ranks)** - +5 attack damage per rank
- **Precision (3 ranks)** - +2% crit chance per rank
- **Fury (5 ranks)** - +5% damage per rank

**Tier 2 (5 points required):**
- **Deadly Strike (3 ranks)** - +10% crit damage per rank
- **Swift Strikes (3 ranks)** - -0.1s attack speed per rank

**Tier 3 (10 points required):**
- **Bloodthirst (5 ranks)** - +3% lifesteal per rank
- **Berserker Rage (1 rank)** - +20% damage, +10% crit chance

**Tier 7 Capstone (30 points):**
- **Execute (1 rank)** - Deal 50% more damage to enemies below 20% health

---

### **Defense Tree:**

**Tier 1 (0 points required):**
- **Vitality (5 ranks)** - +50 max health per rank
- **Toughness (5 ranks)** - +10% max health per rank
- **Evasion (3 ranks)** - +2% dodge per rank

**Tier 2 (5 points required):**
- **Iron Skin (5 ranks)** - +2% armor per rank
- **Second Wind (1 rank)** - Heal to 50% when you would die (once per combat)

**Tier 3 (10 points required):**
- **Fortified (3 ranks)** - +3% armor, +100 health per rank
- **Nimble (3 ranks)** - +3% dodge, +5% lifesteal per rank

**Tier 7 Capstone (30 points):**
- **Immortal (1 rank)** - +50% health, +10% armor, ignore fatal damage once per hour

---

### **Utility Tree:**

**Tier 1 (0 points required):**
- **Prospector (5 ranks)** - +5% gold per rank
- **Scholar (5 ranks)** - +5% XP per rank
- **Efficient (3 ranks)** - +10% XP and gold per rank

**Tier 2 (5 points required):**
- **Lucky (3 ranks)** - +5% item drop chance per rank
- **Quick Learner (1 rank)** - +25% XP gain

**Tier 3 (10 points required):**
- **Treasure Sense (5 ranks)** - +10% gold, +3% crit per rank
- **Savant (1 rank)** - +50% XP, +5 attack damage

**Tier 7 Capstone (30 points):**
- **Master of All (1 rank)** - +20% to all stats, +50% XP/gold

---

## 💡 **Idle Game Balancing Tips:**

### **Early Game (Levels 1-10):**
Focus on:
- **+% damage** - Kills monsters faster
- **+% XP** - Levels up faster
- Simple, impactful bonuses

### **Mid Game (Levels 11-25):**
Diversify:
- **Crit chance + crit damage** - Big damage spikes
- **Lifesteal** - Survive longer fights
- **Gold bonus** - Buy better equipment

### **Late Game (Level 26+):**
Specialize:
- **Capstone talents** - Game-changing abilities
- **Hybrid builds** - Mix trees for unique builds
- **Respec** - Try different strategies

---

## 🔄 **Talent Reset System:**

Players can reset all talents for a gold cost:
- **Default cost:** 100 gold (configurable in TalentPanel)
- Refunds all points
- Allows experimenting with different builds
- Cost can scale with level if desired

---

## 📱 **Integration with Other Systems:**

### **With Combat:**
- ✅ Attack damage bonuses
- ✅ Attack speed bonuses
- ✅ Critical hits (chance + damage)
- ✅ Lifesteal healing
- ✅ Dodge avoidance
- ✅ Armor damage reduction

### **With Rewards:**
- ✅ XP multipliers on monster kills
- ✅ Gold multipliers on monster kills

### **With Character:**
- ✅ Max health increases
- ✅ Health multipliers

### **With Equipment:**
- ✅ Stacks additively with equipment bonuses
- ✅ Both systems work together

---

## ✅ **Testing Checklist:**

- [ ] TalentManager exists in scene
- [ ] TalentPanel UI is set up
- [ ] Created at least 3 talents
- [ ] Assigned all talents to TalentPanel.allTalents array
- [ ] Talent button prefab assigned
- [ ] Level up → gain talent point
- [ ] Can open talent panel
- [ ] Can click talent to see details
- [ ] Can learn talent with point
- [ ] Bonuses apply in combat
- [ ] Can reset talents for gold
- [ ] Tree tabs switch correctly

---

## 🎨 **UI Layout Suggestions:**

### **Classic WoW Style:**
```
   Tier 1
[T] [T] [T] [T]

   Tier 2
[ ] [T] [T] [ ]

   Tier 3
[T] [ ] [T] [T]
```

- 4 columns per tier
- Lines connecting prerequisites
- Higher tiers locked until enough points spent

### **Minimalist Style:**
```
List view with categories:
- Basic (Tier 1-2)
- Advanced (Tier 3-4)
- Master (Tier 5-6)
- Capstone (Tier 7)
```

---

## 🚀 **Quick Start:**

**Create your first talent in 2 minutes:**

1. Right-click → Create → Vinland → Talent
2. Set:
   - Name: "Power Strike"
   - Tree: Combat
   - Tier: 1
   - Max Ranks: 5
   - Attack Damage Bonus: 5
3. Assign to TalentPanel.allTalents array
4. Play game → Level up → Open talent panel → Learn it!

---

## 💎 **Pro Talent Design:**

### **Good Tier 1 Talents:**
- Simple stat increases
- 3-5 ranks
- Clear value ("+25 damage" not "+5% of base")

### **Good Mid-Tier Talents:**
- Synergies (crit chance + crit damage)
- Mix of flat + percentage
- Build-defining choices

### **Good Capstones:**
- Powerful single-rank talents
- Game-changing effects
- Worth the 30-point investment

---

## 🔮 **Future Enhancements:**

These are easy to add later:
- **Active abilities** - Special attacks with cooldowns
- **Passive procs** - "On kill" or "on hit" effects
- **Conditional bonuses** - "Deal 50% more to bosses"
- **Resource system** - Mana/energy for abilities
- **Talent loadouts** - Save/load different specs

---

## 📝 **Balancing Formula:**

For idle games, use this formula:

**Ranked Talents:**
- Cost: 1 point per rank
- Value: Should equal ~1 level of natural progression
- Example: If you gain 10 damage per level, talent should give 5-10 damage

**Percentage Talents:**
- Better at high levels (scales with gear)
- Start small: 5-10% per rank
- Cap at reasonable values: 50% max total

**Capstones:**
- Very powerful (20-50% boost)
- Hard to reach (30 points = level 30+)
- Build-defining

---

## 💰 **Recommended Reset Costs:**

- **Free respec:** First one free, then costs gold
- **Scaling cost:** 100 * character level
- **Fixed cost:** 500-1000 gold
- **Item cost:** Special "Respec Token" consumable

---

## ✨ **Example Full Build (Level 30):**

**Damage Build (Combat Tree):**
- 30 points in Combat
- +150 attack damage (5 ranks × 5 damage × 3 talents)
- +15% crit chance
- +30% crit damage
- Capstone: Execute for burst

**Tank Build (Defense Tree):**
- 30 points in Defense
- +500 max health
- +30% health multiplier
- +15% armor
- +10% dodge
- Capstone: Immortal

**Farmer Build (Split):**
- 15 Combat, 15 Utility
- +50% XP gain
- +50% gold gain
- Enough damage to kill efficiently
- Perfect for grinding levels

---

This system gives players meaningful choices and progression every single level! 🎉

