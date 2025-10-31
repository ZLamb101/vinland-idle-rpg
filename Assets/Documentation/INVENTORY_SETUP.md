# Inventory System Setup

Simple setup guide for the inventory system.

## 🎯 **Core Scripts (Keep These):**

### **Inventory System:**
- `InventoryItem.cs` - Individual item data
- `InventoryData.cs` - Inventory management (20 slots)
- `InventorySlot.cs` - Individual slot UI component
- `InventoryUI.cs` - Main inventory grid manager
- `ItemData.cs` - ScriptableObject for item definitions

### **Character System:**
- `CharacterData.cs` - Character stats including inventory
- `CharacterManager.cs` - Singleton character manager
- `CharacterLoader.cs` - Loads character data in game scene
- `SavedCharacterData.cs` - Serialized character data

### **UI & Navigation:**
- `InventoryToggle.cs` - Switch between quest/inventory panels
- `ReturnToCharacterSelect.cs` - Return to character selection

### **Quest System:**
- `QuestPanel.cs` - Handles quest display, progress, and rewards
- `QuestData.cs` - ScriptableObject for quest definitions

## 🛠️ **Setup Steps:**

### **1. Inventory Panel:**
1. Add `InventoryUI` component to your inventory panel
2. Set `Grid Width` to 5 and `Grid Height` to 4
3. Set `Inventory Grid Parent` to your grid container

### **2. Bag Button:**
1. Add `InventoryToggle` component to your Bag Button
2. Assign quest panel and inventory panel references
3. Connect button click to `ToggleInventory()`

### **3. Create Stone Item:**
1. Right-click in Project → Create → Vinland → Item
2. Name it "StoneItem"
3. Configure: Name="Stone", assign icon sprite, Type=Material

### **4. Quest Rewards:**
1. Select your QuestData ScriptableObject in the Project window
2. Assign your StoneItem to "Item Reward" field
3. Set "Item Reward Quantity" to 1 (or desired amount)

## 🎮 **How It Works:**

- **Quest completion** → Adds stone to inventory
- **Bag Button** → Opens inventory panel
- **5x4 grid** → Shows 20 inventory slots
- **Auto-save** → Inventory saves with character data

## ✅ **That's It!**

The inventory system is now clean and minimal with only the essential scripts needed for functionality.
