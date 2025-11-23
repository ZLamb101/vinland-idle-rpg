# Character Panel Hookup Guide

Complete guide to wire up the Character Panel and Equipment System.

---

## 🔘 **1. Button to Open Character Panel**

### **Direct Button OnClick Setup**

**Steps:**
1. Find or create a UI Button (e.g., "Character" button in your main UI)
2. In the Button Inspector → OnClick() event:
   - Click `+` to add event
   - Drag your `CharacterPanel` GameObject to the object field
   - Select Function: `CharacterPanel → TogglePanel()`

**Done!** The button will toggle the panel open/close.

---

### **Option: Call from Code (Optional)**

If you want to open the panel from another script:

```csharp
// Get reference to character panel
CharacterPanel characterPanel = FindObjectOfType<CharacterPanel>();

// Toggle panel
characterPanel.TogglePanel();

// Or set directly
characterPanel.characterPanel.SetActive(true); // Open
characterPanel.characterPanel.SetActive(false); // Close
```

---

## 👕 **2. Equipping Items from Inventory**

### **Already Working!** ✅

The `InventorySlot` script already handles equipping:

**How it works:**
1. Right-click an equipment item in inventory
2. Selects "Equip" from context menu
3. Calls `InventorySlot.EquipItem()`
4. That calls `equipmentService.EquipItem()`
5. Item is equipped and shows in CharacterPanel

**What happens:**
- If slot is empty → Item equipped
- If slot has item → Items swap (old item returns to inventory)
- CharacterPanel automatically updates (listens to `EquipmentChangedEvent`)

---

## 🧪 **3. Testing Checklist**

### **Test Character Panel Opens:**
- [ ] Click your Character button
- [ ] Character panel should appear
- [ ] Panel shows equipment slots (left) and stats (right)
- [ ] Click button again to close

### **Test Stats Display:**
- [ ] Open Character panel
- [ ] Should show:
  - Attributes (Strength, Agility, Stamina, Spirit, Intellect)
  - Combat stats (Attack Power, Health, Armor, Crit, etc.)
- [ ] Stats should match your level
  - Level 1: ~30 Attack, 100 Health
  - Level 2: ~32 Attack, 110 Health

### **Test Equipment Display:**
- [ ] Equipment slots show icons for equipped items
- [ ] Empty slots show placeholder or are transparent
- [ ] Click equipped item → Returns to inventory

### **Test Equipping Items:**
1. [ ] Open Inventory
2. [ ] Right-click an equipment item
3. [ ] Select "Equip"
4. [ ] Item disappears from inventory
5. [ ] Open Character panel → Item shows in correct slot
6. [ ] Stats should increase (check Attack/Health/etc.)

### **Test Unequipping Items:**
1. [ ] Open Character panel
2. [ ] Click an equipped item
3. [ ] Item should return to inventory
4. [ ] Stats should decrease

### **Test Swapping Items:**
1. [ ] Equip an item (e.g., Iron Sword)
2. [ ] Equip another item of same type (e.g., Steel Sword)
3. [ ] Iron Sword should return to inventory
4. [ ] Steel Sword should now be equipped

### **Test Stats Update on Level Up:**
1. [ ] Open Character panel
2. [ ] Note your current Attack Power (e.g., 30)
3. [ ] Fight monsters until you level up
4. [ ] Attack Power should increase (+2 from STR gain)
5. [ ] Panel should update automatically

---

## 🔧 **4. Troubleshooting**

### **Problem: Button doesn't open panel**

**Check:**
- [ ] CharacterPanel GameObject exists in scene
- [ ] `CharacterPanel` component is attached
- [ ] Button OnClick event is configured correctly
- [ ] OnClick references CharacterPanel.TogglePanel()

**Fix:**
```
1. Select your button in Hierarchy
2. Look at Inspector → Button component → OnClick()
3. Click + to add event
4. Drag CharacterPanel GameObject to object field
5. Select: CharacterPanel → TogglePanel()
```

---

### **Problem: Equipment not showing in slots**

**Check:**
- [ ] All 11 `EquipmentSlotUI` fields assigned in CharacterPanel
- [ ] Each slot has:
  - `Button` component
  - `Image` component (for icon)
  - `EquipmentSlotUI` properly configured

**Fix:**
```
1. Select CharacterPanel in Hierarchy
2. In Inspector, expand "Equipment Slot UI Elements"
3. For each slot (Head, Neck, etc.):
   - Slot Type: Set correct type (Head, Neck, etc.)
   - Slot Button: Assign Button component
   - Item Icon: Assign Image component
   - Empty Slot Icon: (Optional) Assign placeholder sprite
```

---

### **Problem: Stats not displaying**

**Check:**
- [ ] All TextMeshProUGUI fields assigned in CharacterPanel
- [ ] Text objects are active in hierarchy
- [ ] Font and size are visible

**Fix:**
```
1. Select CharacterPanel in Hierarchy
2. In Inspector, expand "Stats Display"
3. Assign each TextMeshProUGUI field:
   - strengthText, agilityText, etc.
   - attackPowerText, maxHealthText, etc.
```

---

### **Problem: Can't equip items from inventory**

**Check:**
- [ ] Item has `equipmentData` assigned
- [ ] Item type is `Equipment`
- [ ] Right-click context menu shows "Equip" option
- [ ] EquipmentManager exists in scene
- [ ] Services are registered

**Fix:**
```
Check Console for errors like:
- "EquipmentService not found" → Add EquipmentManager to scene
- "Item is not equipment" → Check ItemData configuration
```

---

### **Problem: Stats don't update when equipping**

**Check:**
- [ ] CharacterPanel subscribes to `EquipmentChangedEvent`
- [ ] CharacterPanel subscribes to `StatsRecalculatedEvent`

**Fix:**
Already handled in CharacterPanel.Start() - if not working, check:
```
1. CharacterPanel is in scene when you equip
2. EventBus is working (check other events)
3. Console for any errors
```

---

## 📋 **5. Quick Setup Summary**

### **Minimum Required:**

**For Panel Toggle:**
1. Create "Character" button in UI
2. Add OnClick event → CharacterPanel.TogglePanel()

**For Equipment Display:**
1. Assign all 11 equipment slot references
2. Each slot needs Button + Image components

**For Stats Display:**
1. Assign all 12 stat text references
2. Text objects must be active

**For Equipping from Inventory:**
- Already works! Just right-click equipment items.

---

## 🎯 **6. Keyboard Shortcut (Optional)**

Want to open panel with 'C' key? Add this to any script:

```csharp
void Update()
{
    if (Input.GetKeyDown(KeyCode.C))
    {
        var characterPanel = FindObjectOfType<CharacterPanel>();
        if (characterPanel != null)
        {
            characterPanel.TogglePanel();
        }
    }
}
```

---

## ✅ **All Done!**

Your Character Panel should now be fully functional:
- ✅ Opens/closes with button
- ✅ Shows all equipment slots
- ✅ Shows attributes and combat stats
- ✅ Items can be equipped from inventory
- ✅ Stats update automatically

**Enjoy your unified Character Panel!** 🎉

