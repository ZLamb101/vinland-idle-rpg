# Shop System Setup Guide

## Step-by-Step Unity Setup Instructions

### 1. Create ShopManager GameObject
1. In your scene hierarchy, create an empty GameObject: Right-click → Create Empty
2. Name it "ShopManager"
3. Add Component → `ShopManager` script
4. This GameObject will persist across scenes (handled automatically)

---

### 2. Create ShopPanel UI Structure

#### 2.1 Main Shop Panel
1. Create a UI Panel: Right-click in Canvas → UI → Panel
2. Name it "ShopPanel"
3. Add Component → `ShopPanel` script
4. Set the panel to inactive initially (uncheck the checkbox in Inspector)

#### 2.2 Shop Panel Layout Structure
Create this hierarchy under ShopPanel:
```
ShopPanel (Panel with ShopPanel component)
├── Header
│   └── CloseButton (Button) - Close shop
├── ContentArea
│   ├── ShopItemsContainer (Empty GameObject with RectTransform)
│   │   └── [ShopItemSlot instances will spawn here]
│   └── RefreshTimerText (TextMeshProUGUI) - Stock refresh countdown
└── BuyBackSection (Panel or GameObject)
    ├── BuyBackItemIcon (Image) - Item icon
    ├── BuyBackItemNameText (TextMeshProUGUI) - Item name
    ├── BuyBackPriceText (TextMeshProUGUI) - Buyback price
    └── BuyBackButton (Button) - Buy back button
```

#### 2.3 Configure ShopPanel Component
In the ShopPanel component, assign:
- **Shop Panel**: Drag the ShopPanel GameObject itself
- **Shop Name Text**: Leave empty (optional - not displayed)
- **Shop Items Container**: ShopItemsContainer (the empty GameObject)
- **Shop Item Slot Prefab**: [Create this in step 3]
- **Refresh Timer Text**: RefreshTimerText
- **Buy Back Section**: BuyBackSection GameObject
- **Buy Back Item Icon**: BuyBackItemIcon Image
- **Buy Back Item Name Text**: BuyBackItemNameText
- **Buy Back Price Text**: BuyBackPriceText
- **Buy Back Button**: BuyBackButton
- **Close Button**: CloseButton
- **Tooltip Panel**: [Can reuse InventoryUI tooltip or create new]
- **Tooltip Name Text**: Tooltip name text component
- **Tooltip Description Text**: Tooltip description text component

#### 2.4 Setup ShopItemsContainer
1. Add a **Vertical Layout Group** or **Grid Layout Group** component to ShopItemsContainer
2. Configure spacing and padding as desired
3. Enable **Content Size Fitter** if you want it to auto-resize

---

### 3. Create ShopItemSlot Prefab

#### 3.1 Create ShopItemSlot GameObject
1. Create a UI Panel: Right-click → UI → Panel
2. Name it "ShopItemSlot"
3. Add Component → `ShopItemSlot` script
4. Set size: Width ~200-300px, Height ~100-150px

#### 3.2 ShopItemSlot Layout Structure
```
ShopItemSlot (Panel with ShopItemSlot component)
├── ItemIcon (Image) - Item sprite
├── ItemNameText (TextMeshProUGUI) - Item name
├── PriceText (TextMeshProUGUI) - "XXX Gold"
├── StockText (TextMeshProUGUI) - "Stock: X/Y"
└── BuyButton (Button) - Buy button
```

#### 3.3 Configure ShopItemSlot Component
Assign all UI references:
- **Item Icon**: ItemIcon Image
- **Item Name Text**: ItemNameText
- **Price Text**: PriceText
- **Stock Text**: StockText
- **Buy Button**: BuyButton

#### 3.4 Create Prefab
1. Drag ShopItemSlot from Hierarchy to Project window (Prefabs folder)
2. Delete the instance from Hierarchy (keep the prefab)
3. Assign this prefab to ShopPanel's "Shop Item Slot Prefab" field

---

### 4. Create ShopData ScriptableObjects

#### 4.1 Create ShopData Assets
1. Right-click in Project window → Create → Vinland → Shop
2. Name it (e.g., "GeneralStoreShop", "WeaponShop")
3. Configure the ShopData:
   - **Shop Name**: Display name (e.g., "General Store")
   - **Stock Refresh Interval**: 600 (10 minutes in seconds)
   - **Shop Items**: Click "+" to add entries

#### 4.2 Configure ShopItemEntry
For each shop item:
- **Item**: Drag an ItemData ScriptableObject
- **Price**: Custom buy price (can differ from baseValue)
- **Max Stock**: Full stock amount (e.g., 500 for potions, 10 for rare items)

**Example Shop Items:**
- Health Potion: Price 50, Max Stock 500
- Teleport Stone: Price 200, Max Stock 10
- Iron Sword: Price 500, Max Stock 5

---

### 5. Update NPCData ScriptableObjects

#### 5.1 For Shop NPCs
1. Select an NPCData ScriptableObject
2. Change **NPC Type** dropdown to "ShopNPC"
3. Assign **Shop Data** field: Drag the ShopData ScriptableObject you created

#### 5.2 For Talkable NPCs
1. Select an NPCData ScriptableObject
2. Ensure **NPC Type** is set to "TalkableNPC"
3. Ensure **Shop Data** is empty/null

---

### 6. Update NPC Panel Prefab

#### 6.1 Modify Existing NPC Prefab
1. Find your NPC Panel prefab in Project
2. Open it for editing (double-click or click "Open Prefab" button)

#### 6.2 Update Button Structure
**Remove:**
- `talkButton` reference (if separate button exists)
- `shopButton` reference (if separate button exists)

**Add/Update:**
- Create a single **Button** named "InteractButton"
- Add `ShopPanel` component reference (if not already added)
- In NPCPanel component:
  - Assign **Interact Button** to the new button
  - Remove old talkButton and shopButton references

#### 6.3 Button Text Setup
1. Add a **TextMeshProUGUI** child to InteractButton
2. Set text to "Interact" (will be updated dynamically based on NPC type)
3. Style the button as desired

---

### 7. Tooltip Setup (Optional - Can Reuse Inventory Tooltip)

#### Option A: Reuse Inventory Tooltip
If you have an existing tooltip system:
1. In ShopPanel component, assign the same tooltip panel used by InventoryUI
2. Ensure tooltip is on a high Canvas sort order to appear above shop items

#### Option B: Create New Tooltip
1. Create a UI Panel: Right-click → UI → Panel
2. Name it "ShopTooltip"
3. Add two TextMeshProUGUI children:
   - TooltipNameText (for item name)
   - TooltipDescriptionText (for description)
4. Set panel to inactive initially
5. Assign to ShopPanel component

---

### 8. Testing Checklist

#### Test Shop Opening
- [ ] Click "Interact" on a ShopNPC → Shop panel opens
- [ ] Shop name displays correctly
- [ ] Shop items appear in container
- [ ] Item icons, names, prices, and stock display correctly

#### Test Buying
- [ ] Click Buy button on an item → Item added to inventory
- [ ] Gold deducted correctly
- [ ] Stock decreases
- [ ] Buy button disables when out of stock
- [ ] Buy button disables when insufficient gold

#### Test Selling
- [ ] Open shop
- [ ] Right-click item in inventory → Item sold
- [ ] Gold increases by baseValue
- [ ] Item removed from inventory
- [ ] Buyback section appears

#### Test Buyback
- [ ] After selling, buyback section shows item
- [ ] Click BuyBack button → Item restored to inventory
- [ ] Gold deducted by sell price
- [ ] Buyback section disappears

#### Test Stock Refresh
- [ ] Wait 10 minutes (or temporarily reduce refresh interval)
- [ ] Close and reopen shop
- [ ] Stock restored to maxStock

#### Test Mutual Exclusivity
- [ ] Open shop → Dialogue closes (if open)
- [ ] Open dialogue → Shop closes (if open)

---

### 9. UI Styling Tips

#### ShopItemSlot Styling
- Use a background image/border for visual separation
- Color-code stock text (green for in stock, red for out)
- Disable buy button visual state when not interactable
- Add hover effects for better UX

#### ShopPanel Layout
- Consider using a ScrollRect if many items
- Add a background/border for the panel
- Style buttons consistently with your game's UI theme
- Make refresh timer prominent but not intrusive

---

### 10. Common Issues & Solutions

**Issue**: Shop doesn't open
- **Solution**: Check ShopManager exists in scene, check NPCData has ShopData assigned

**Issue**: Items don't display
- **Solution**: Verify ShopItemSlot prefab is assigned, check ShopItemsContainer has Layout Group

**Issue**: Tooltip doesn't show
- **Solution**: Ensure tooltip panel is assigned, check Canvas sort order, verify EventSystem exists

**Issue**: Buy button always disabled
- **Solution**: Check gold amount, verify stock > 0, ensure CharacterManager exists

**Issue**: Selling doesn't work
- **Solution**: Verify shop is open, check InventoryItem has baseValue set, ensure CharacterManager exists

---

### 11. Performance Considerations

- ShopItemSlot prefabs are instantiated/destroyed dynamically (good for memory)
- Stock state persists per shop (saved in ShopManager dictionary)
- Consider object pooling for ShopItemSlots if you have many items
- Tooltip updates every frame when visible (minimal performance impact)

---

## Quick Reference: Required Components

### Scene Objects
- ✅ ShopManager GameObject (with ShopManager component)
- ✅ ShopPanel GameObject (with ShopPanel component)
- ✅ Canvas with EventSystem

### Prefabs
- ✅ ShopItemSlot prefab (with ShopItemSlot component)

### ScriptableObjects
- ✅ ShopData assets (one per shop)
- ✅ Updated NPCData assets (with NPCType and ShopData)

### UI Elements
- ✅ Shop panel with all child elements
- ✅ Tooltip panel (can reuse from inventory)

---

## Notes

- The system automatically handles stock persistence per shop
- Stock refreshes when shop is opened if 10+ minutes have passed
- Buyback only stores the last sold item (not a history)
- Right-click selling only works when shop panel is open
- All gold transactions go through CharacterManager

