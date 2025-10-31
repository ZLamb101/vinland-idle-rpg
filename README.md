# Vinland - Idle RPG Game

A Unity-based idle/incremental RPG with WoW-inspired systems.


## 📂 Project Structure

```
Assets/
├── Scripts/
│   ├── Character System
│   │   ├── CharacterData.cs
│   │   ├── CharacterManager.cs
│   │   ├── CharacterLoader.cs
│   │   └── CharacterSelectionManager.cs
│   ├── Zone System
│   │   ├── ZoneData.cs
│   │   ├── ZoneManager.cs
│   │   └── ZonePanel.cs
│   ├── Quest System
│   │   ├── QuestData.cs
│   │   └── QuestPanel.cs
│   ├── Combat System
│   │   ├── MonsterData.cs
│   │   ├── CombatManager.cs
│   │   └── CombatPanel.cs
│   ├── Inventory System
│   │   ├── InventoryData.cs
│   │   ├── InventoryUI.cs
│   │   ├── InventorySlot.cs
│   │   ├── InventoryItem.cs
│   │   └── ItemData.cs
│   ├── Equipment System
│   │   ├── EquipmentData.cs
│   │   ├── EquipmentManager.cs
│   │   └── EquipmentPanel.cs
│   ├── Talent System
│   │   ├── TalentData.cs
│   │   ├── TalentManager.cs
│   │   └── TalentPanel.cs
│   ├── UI Components
│   │   ├── AnimatedResourceBar.cs
│   │   ├── CharacterInfoDisplay.cs
│   │   ├── InventoryToggle.cs
│   │   └── ReturnToCharacterSelect.cs
│   └── Documentation
│       ├── INVENTORY_SETUP.md
│       ├── ZONE_SYSTEM_SETUP.md
│       ├── COMBAT_SYSTEM_SETUP.md
│       ├── EQUIPMENT_SYSTEM_SETUP.md
│       ├── EQUIPMENT_QUICK_START.md
│       ├── TALENT_SYSTEM_SETUP.md
│       └── TALENT_QUICK_START.md
├── Scenes/
├── Prefabs/
├── ScriptableObjects/
└── Art/
```

## 🛠️ Setup

### Requirements
- Unity 2022.3 LTS or later
- TextMeshPro
- Unity Input System (new)

### Quick Start
1. Open the project in Unity
2. Open Scenes/CharacterSelection scene
3. Create managers in scene:
   - CharacterManager
   - ZoneManager
   - CombatManager
   - EquipmentManager
   - TalentManager
4. Create ScriptableObjects for:
   - Zones (ZoneData)
   - Quests (QuestData)
   - Monsters (MonsterData)
   - Items (ItemData)
   - Equipment (EquipmentData - must be in Resources folder)
   - Talents (TalentData)
5. Play!

## 📚 Documentation

Each system has detailed setup documentation in `Assets/Scripts/`:
- `INVENTORY_SETUP.md` - Inventory system guide
- `ZONE_SYSTEM_SETUP.md` - Zone progression guide
- `COMBAT_SYSTEM_SETUP.md` - Combat system guide
- `EQUIPMENT_SYSTEM_SETUP.md` - Equipment system guide
- `TALENT_SYSTEM_SETUP.md` - Talent tree guide



