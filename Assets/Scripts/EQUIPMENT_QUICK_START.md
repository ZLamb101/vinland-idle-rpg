# Equipment System - Quick Start Guide

Get your equipment system running in 5 minutes!

## 🚀 **Quick Setup (3 Steps):**

### **Step 1: Create Equipment Manager**
1. In your game scene, create empty GameObject named "EquipmentManager"
2. Add `EquipmentManager` component
3. Done! (It persists automatically)

### **Step 2: Create Your First Equipment**
1. Right-click in Project → Create → Vinland → Equipment
2. Name it "StarterSword"
3. Set these fields:
   - Equipment Name: "Starter Sword"
   - Slot: Main Hand
   - Tier: Common
   - Attack Damage: 15
   - Level Required: 1
4. Save!

### **Step 3: Test It!**
1. Create an ItemData for the sword:
   - Right-click → Create → Vinland → Item
   - Set Item Type to "Equipment"
   - Assign StarterSword to Equipment Data field
2. Add to your inventory via quest reward or directly
3. In game:
   - **Right-click the sword in inventory** → It equips!
   - Fight a monster → See the +15 damage!

---

## 📋 **All 13 Equipment Slots:**

### Body Armor (9 slots):
- Head, Neck, Shoulders, Back, Chest
- Hands, Waist, Legs, Feet

### Accessories & Weapons (4 slots):
- Ring 1, Ring 2
- Main Hand, Off Hand

---

## ⚔️ **Equipment Stats at a Glance:**

### Offense:
- **Attack Damage** - More damage
- **Attack Speed** - Attack faster (negative values)
- **Critical Chance** - 2x damage hits
- **Lifesteal** - Heal when attacking

### Defense:
- **Max Health** - More HP
- **Armor** - Reduce damage taken
- **Dodge** - Avoid attacks
- **Health Regen** - Passive healing

### Rewards:
- **XP Bonus** - Level up faster
- **Gold Bonus** - Get richer

---

## 🎮 **How to Use:**

**Equipping:**
- Right-click equipment in inventory
- Automatically equips to correct slot
- Old equipment unequipped to inventory

**Unequipping:**
- Click equipped item in Equipment Panel
- Returns to inventory
- Stats recalculated

**Combat:**
- All stats apply automatically
- Critical hits show 2x damage
- Lifesteal heals you
- Dodge shows "0 damage"
- XP/Gold bonuses after victory

---

## 🎯 **Example Equipment Progression:**

**Level 1:**
```
Iron Sword: +10 damage
Leather Cap: +15 health
Simple Ring: +5 health
```

**Level 5:**
```
Steel Sword: +20 damage, 5% crit
Steel Helmet: +30 health, 5% armor
Ring of Power: +5 damage
```

**Level 10:**
```
Enchanted Blade: +35 damage, 10% crit, 10% lifesteal
Plate Helmet: +50 health, 10% armor
Vampire Ring: +10 damage, 15% lifesteal
Lucky Cloak: 10% dodge, +20% gold
```

---

## 🛠️ **Create Full Equipment Panel UI:**

See `EQUIPMENT_SYSTEM_SETUP.md` for detailed UI setup.

Quick version:
1. Create UI Panel
2. Add `EquipmentPanel` component
3. Create 13 sub-panels (one per slot)
4. Each needs: Button (with Image child for icon)
5. Assign all slot references
6. **Optional:** Assign silhouette sprites to "Empty Slot Icon" for each slot
7. Add toggle button to open/close panel

---

## 💡 **Pro Tips:**

1. **Start Simple:** Just weapons first, add armor later
2. **Use Tiers:** Color-code rarity (green/blue/purple/orange)
3. **Balance:** Higher level = more stats
4. **Mix Stats:** Don't just add damage - try crit, lifesteal, dodge
5. **Accessorize:** Rings & trinkets are great for special effects

---

## 🎨 **Suggested Stat Combos:**

**Warrior Build:**
- Main Hand: High damage + crit
- Armor: Max health + armor
- Rings: Health + lifesteal

**Glass Cannon:**
- Main Hand: Massive damage
- Accessories: Crit + attack speed
- Armor: Minimal (all damage!)

**Tank Build:**
- Main Hand: Moderate damage
- Armor: Max health + armor
- Accessories: Dodge + health regen

**Treasure Hunter:**
- Weapons: Moderate damage
- Accessories: XP bonus + gold bonus
- Armor: Balanced stats

---

## ✅ **Verify It Works:**

1. ✅ Create equipment asset
2. ✅ Create item linking to equipment
3. ✅ Add item to inventory
4. ✅ Right-click to equip
5. ✅ Check stats in Equipment Panel
6. ✅ Fight monster - see damage increase
7. ✅ Click equipped item to unequip
8. ✅ Item returns to inventory

**All working? You're ready to build your equipment empire!** 🎉

---

## 📚 **More Info:**

- Full system details: `EQUIPMENT_SYSTEM_SETUP.md`
- Combat integration: `COMBAT_SYSTEM_SETUP.md`
- Inventory system: `INVENTORY_SETUP.md`

