# Talent System - Quick Start

Get your WoW-style talent tree running in 5 minutes!

## 🚀 **Quick Setup (3 Steps):**

### **Step 1: Create Talent Manager**
1. In your game scene, create empty GameObject: "TalentManager"
2. Add `TalentManager` component
3. Done! (Auto-persists with DontDestroyOnLoad)

### **Step 2: Create Your First Talent**
1. Right-click in Project → Create → Vinland → Talent
2. Name it "PowerStrike"
3. Configure:
   - Talent Name: "Power Strike"
   - Description: "Increases attack damage"
   - Tree: Combat
   - Tier: 1
   - Position: 0
   - Max Ranks: 5
   - Attack Damage Bonus: 5
4. Save!

### **Step 3: Test It!**
1. Level up your character (use debug or complete quests)
2. Open Talent Panel (hook up a button like you did for Equipment)
3. Click "Power Strike" → Click "Learn"
4. Fight a monster → See the damage increase! ⚔️

---

## 🌳 **3 Talent Trees:**

### **🗡️ Combat (Offense)**
- Attack damage
- Critical hits
- Attack speed
- Lifesteal

### **🛡️ Defense (Survival)**
- Max health
- Armor
- Dodge
- Damage reduction

### **💰 Utility (Farming)**
- XP bonus
- Gold bonus
- Quality of life

---

## 📊 **How Talents Work:**

### **Earning & Spending:**
```
Level Up → +1 talent point
Hover talent → See tooltip
Left-click → Invest point!
```

### **Tree Requirements:**
```
Tier 1: Available immediately (0 points in tree)
Tier 2: Need 5 points in tree first
Tier 3: Need 10 points in tree first
...
Tier 7: Need 30 points in tree (capstones!)
```

### **Ranked Talents:**
```
Power Strike (5/5 ranks):
Rank 1: +5 damage
Rank 2: +10 damage
Rank 3: +15 damage
Rank 4: +20 damage
Rank 5: +25 damage (maxed!)
```

---

## 🎯 **Quick Talent Ideas:**

### **Combat Tree:**
```
Brute Force (5 ranks) - +5 attack damage per rank
Deadly Strikes (3 ranks) - +3% crit chance per rank
Savage Blows (5 ranks) - +5% damage per rank
Blood Rage (1 rank) - +10% crit damage
```

### **Defense Tree:**
```
Thick Skin (5 ranks) - +50 health per rank
Hardy (3 ranks) - +10% max health per rank
Agile (3 ranks) - +2% dodge per rank
Iron Will (1 rank) - +20% armor
```

### **Utility Tree:**
```
Treasure Hunter (5 ranks) - +5% gold per rank
Fast Learner (5 ranks) - +5% XP per rank
Lucky Strike (3 ranks) - +2% crit, +5% gold per rank
Master Farmer (1 rank) - +25% XP and gold
```

---

## 🔧 **Hooking Up the Talent Button:**

Same as equipment - in your UI:

**Option 1 - Inspector:**
1. Select your Talent Button
2. In Button component → On Click ()
3. Drag TalentPanel GameObject
4. Select: TalentPanel → TogglePanel()

**Option 2 - Code:**
```csharp
talentButton.onClick.AddListener(() => talentPanel.TogglePanel());
```

---

## 💡 **Design Tips:**

### **Make Tier 1 Strong:**
Players get them early, make them feel impactful!
- +5 damage per rank (noticeable)
- +5% XP (speeds up leveling)
- +50 health per rank (survivability)

### **Make Capstones Epic:**
These are level 30+ only!
- Single rank (1 point = huge power)
- Unique effects
- Build-defining

### **Balance the Trees:**
- **Combat:** Pure damage, fastest kills
- **Defense:** Survive longer, slower kills
- **Utility:** Best for long-term efficiency
- **Hybrid:** Balanced, versatile

---

## 🎮 **Example Player Journey:**

**Level 1-5:** No talents yet
**Level 6:** First talent point! → Power Strike rank 1 (+5 damage)
**Level 7:** Power Strike rank 2 (+10 total damage)
**Level 10:** Power Strike maxed at rank 5 (+25 total damage!)
**Level 11:** Unlock tier 2! → Learn Deadly Strikes (+3% crit)
**Level 20:** 20 points total → Mix of damage, crit, health
**Level 30:** Unlock capstone! → HUGE power spike
**Level 40:** 40 points → Can max 2 trees or hybrid all 3

---

## ⚡ **Quick Comparison:**

### **Without Talents (Level 20):**
```
Attack: 10 damage
Crit: 0%
Health: 60
```

### **With Combat Talents (Level 20, 20 points):**
```
Attack: 35 damage (+25 from talents)
Crit: 15% chance for 2.3x damage
Health: 60
Lifesteal: 9%
Kill speed: 3x faster!
```

### **With Defense Talents (Level 20, 20 points):**
```
Attack: 10 damage
Health: 200 (+140 from talents)
Armor: 15% reduction
Dodge: 8%
Survival: 4x longer!
```

---

## 🔥 **Min-Max Builds:**

**Speed Leveler:**
- Max Utility tree first
- +50% XP gain
- Then switch to Combat for damage
- Respec at level 50 for endgame build

**Pure Power:**
- Max Combat tree only
- Highest damage output
- Glass cannon style
- One-shot everything

**Immortal Tank:**
- Max Defense tree only
- Never die
- Slow but steady
- AFK friendly

---

## ✅ **That's It!**

The talent system is fully integrated with:
- ✅ Combat system (damage, crit, armor, dodge)
- ✅ Equipment system (bonuses stack!)
- ✅ Reward system (XP/gold multipliers)
- ✅ Character progression

Every level gives meaningful choices and build variety! 🎉

