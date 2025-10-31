# Equipment System Setup

WoW-style equipment system with 18 equipment slots and comprehensive stat system.

## 🎯 **Core Scripts:**

### **Equipment System:**
- `EquipmentData.cs` - ScriptableObject for equipment definitions
- `EquipmentManager.cs` - Singleton that manages equipped items
- `EquipmentPanel.cs` - UI panel showing all equipment slots
- `ItemData.cs` - Updated to support equipment
- `InventoryItem.cs` - Updated with equipment reference
- `CombatManager.cs` - Updated to use equipment stats

---

## 🛡️ **Equipment Slots (13 Total):**

### **Armor Slots:**
1. **Head** - Helmets, hats
2. **Neck** - Necklaces, amulets
3. **Shoulders** - Shoulder pads
4. **Back** - Cloaks, capes
5. **Chest** - Chest armor, robes
6. **Hands** - Gloves, gauntlets
7. **Waist** - Belts
8. **Legs** - Leg armor, pants
9. **Feet** - Boots

### **Accessory Slots:**
10. **Ring 1** - First ring slot
11. **Ring 2** - Second ring slot

### **Weapon Slots:**
12. **Main Hand** - Primary weapon
13. **Off Hand** - Shield or off-hand weapon

---

## 📊 **Equipment Stats:**

### **Combat Stats:**
- **Attack Damage** - Increases damage dealt
- **Attack Speed** - Negative = faster (e.g., -0.1 = 0.1s faster attacks)
- **Max Health** - Increases maximum health
- **Health Regen** - Passive health regeneration per second

### **Defensive Stats:**
- **Armor** - Damage reduction % (0.1 = 10% reduction)
- **Dodge** - Chance to avoid attacks (0.05 = 5% dodge)

### **Special Stats:**
- **Critical Chance** - Chance for 2x damage (0.1 = 10% crit)
- **Lifesteal** - Heal % of damage dealt (0.1 = 10% lifesteal)
- **XP Bonus** - Extra XP from monsters (0.1 = +10% XP)
- **Gold Bonus** - Extra gold from monsters (0.1 = +10% gold)

---

## 🛠️ **Setup Steps:**

### **1. Create Equipment Assets:**

1. Right-click in Project → Create → Vinland → Equipment
2. Name it (e.g., "IronSword", "LeatherHelm")
3. Configure equipment:
   - **Equipment Name:** Display name
   - **Description:** Item description
   - **Icon:** Equipment sprite
   - **Slot:** Which slot it goes in
   - **Tier:** Common/Uncommon/Rare/Epic/Legendary
   - **Level Required:** Minimum level to equip
   - **Stats:** Set combat/defensive/special stats

### **2. Create Item for Equipment (Optional):**

If you want the equipment to drop from monsters or be quest rewards:

1. Right-click → Create → Vinland → Item
2. Set Item Type to "Equipment"
3. Assign your EquipmentData to "Equipment Data" field
4. Now this item can be added to inventory and equipped

### **3. Setup Equipment Manager (in Scene):**

1. Create empty GameObject: "EquipmentManager"
2. Add `EquipmentManager` component
3. Leave in scene (it auto-persists)

### **4. Setup Equipment Panel UI:**

Create the equipment panel interface:

```
EquipmentPanel (Panel)
├── HeadSlot
│   ├── SlotButton (Button)
│   └── ItemIcon (Image)
├── NeckSlot
├── ShouldersSlot
├── ... (all armor slots)
├── Ring1Slot
├── Ring2Slot
├── MainHandSlot
├── OffHandSlot
└── StatsPanel
    ├── AttackText
    ├── HealthText
    ├── ArmorText
    └── ... (stat displays)
```

Assign all 13 slot references in `EquipmentPanel` component.

**Note:** Each slot only needs a Button and an Image (for the icon). No text fields required.

**Optional:** For each slot in the EquipmentPanel component, you can assign an "Empty Slot Icon" - a silhouette sprite that shows when the slot is empty. This helps players identify which slot is which!

### **5. Equip Items from Inventory:**

Update your inventory system to allow right-clicking equipment to equip:

```csharp
// In InventorySlot.cs or similar
void OnItemRightClick(InventoryItem item)
{
    if (item.IsEquipment() && EquipmentManager.Instance != null)
    {
        EquipmentManager.Instance.EquipItem(item.equipmentData);
        // Remove from inventory
        inventoryData.RemoveItem(slotIndex, 1);
    }
}
```

---

## 🎮 **How It Works:**

### **Equipping Items:**
1. Player gets equipment from quest/monster/shop
2. Equipment goes to inventory as an item
3. Right-click (or click Equip button) to equip
4. Item moves from inventory to equipment slot
5. Stats automatically recalculated

### **Unequipping Items:**
1. Click equipped item in equipment panel
2. Item unequipped and added back to inventory
3. Stats automatically recalculated

### **Combat Integration:**
- **Attack Damage:** Added to base damage in `PlayerAttack()`
- **Attack Speed:** Modified attack timer
- **Max Health:** Added to character's max HP
- **Armor:** Reduces incoming damage in `MonsterAttack()`
- **Dodge:** Chance to avoid enemy attacks
- **Critical:** Chance for 2x damage
- **Lifesteal:** Heals player when dealing damage
- **XP/Gold Bonus:** Multiplies rewards after combat

---

## 🎨 **Equipment Tiers:**

Use tiers to color-code equipment rarity:

- **Common** - White/Gray
- **Uncommon** - Green
- **Rare** - Blue
- **Epic** - Purple
- **Legendary** - Orange/Gold

Set `rarityColor` field to color equipment names in UI.

---

## 📦 **Example Equipment:**

### **Iron Sword (Common Weapon)**
```
Slot: Main Hand
Tier: Common
Level Required: 1
Attack Damage: +15
Attack Speed: 0
```

### **Steel Helmet (Uncommon Armor)**
```
Slot: Head
Tier: Uncommon
Level Required: 3
Max Health: +20
Armor: 0.05 (5% damage reduction)
```

### **Berserker Ring (Rare Accessory)**
```
Slot: Ring1
Tier: Rare
Level Required: 5
Attack Damage: +5
Critical Chance: 0.15 (15% crit chance)
```

### **Vampire Amulet (Epic Accessory)**
```
Slot: Neck
Tier: Epic
Level Required: 10
Attack Damage: +8
Lifesteal: 0.2 (20% lifesteal)
Max Health: +30
```

### **Cloak of Fortune (Legendary)**
```
Slot: Back
Tier: Legendary
Level Required: 15
XP Bonus: 0.25 (+25% XP)
Gold Bonus: 0.25 (+25% gold)
Dodge: 0.1 (10% dodge)
```

---

## 🔮 **Advanced Features:**

### **Set Bonuses (Future):**
Create equipment sets that give bonuses when wearing multiple pieces:
- Warrior Set (2pc): +10% max health
- Warrior Set (4pc): +15% attack damage

### **Enchantments (Future):**
Add temporary or permanent stat boosts to equipment

### **Durability (Future):**
Equipment degrades over time and needs repair

### **Sockets (Future):**
Add gem slots to equipment for additional customization

### **Transmogrification (Future):**
Change equipment appearance while keeping stats

---

## ✅ **Testing Checklist:**

- [ ] Created at least 1 equipment asset
- [ ] EquipmentManager exists in scene
- [ ] EquipmentPanel UI is set up
- [ ] All 13 equipment slots assigned in panel
- [ ] Can equip item from inventory
- [ ] Can unequip item to inventory
- [ ] Stats display updates when equipping
- [ ] Combat uses equipment stats (damage, armor, etc.)
- [ ] Critical hits proc correctly
- [ ] Lifesteal heals player
- [ ] Dodge avoids damage
- [ ] XP/Gold bonuses apply to rewards

---

## 💡 **Integration Tips:**

### **With Inventory:**
Equipment items should have:
- `itemType = ItemType.Equipment`
- `maxStackSize = 1` (don't stack equipment)
- `equipmentData` reference set

### **With Combat:**
All equipment stats automatically apply in combat:
- Offense: Damage, speed, crit, lifesteal
- Defense: Armor, dodge
- Rewards: XP bonus, gold bonus

### **With Quests:**
Add equipment as quest rewards:
1. Create EquipmentData
2. Create ItemData linking to it
3. Set as quest reward in QuestData

### **With Monster Drops:**
Add equipment to monster loot tables:
1. Create ItemData for equipment
2. Set as monster's itemDrop
3. Set appropriate drop chance

---

## 🎯 **Balancing Guidelines:**

### **Progression:**
- Level 1-5: +5-10 damage, +10-20 health per piece
- Level 6-10: +10-20 damage, +20-40 health per piece
- Level 11+: +20-30 damage, +40-60 health per piece

### **Rarity Multipliers:**
- Common: 1.0x stats
- Uncommon: 1.5x stats
- Rare: 2.0x stats + special stat
- Epic: 2.5x stats + 2 special stats
- Legendary: 3.0x stats + 3 special stats

### **Slot Priorities:**
- **Main Hand:** Primary damage source
- **Chest/Legs:** Most health
- **Rings/Trinkets:** Special effects
- **Back:** Good for utility stats (XP, gold, dodge)

---

## 🚀 **Quick Start:**

1. Create an Iron Sword (Main Hand, +10 damage)
2. Create as ItemData and add to your inventory
3. Equip it from inventory
4. Fight a monster - see increased damage!
5. Create more equipment for other slots
6. Build your ultimate character loadout!

