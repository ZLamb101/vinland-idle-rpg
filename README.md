# Vinland - Idle RPG Game

A Unity-based idle/incremental RPG with WoW-inspired systems.

## 🎮 Game Features

### Core Systems
- **Character System** - Character creation, stats, leveling, and progression
- **Zone System** - Multiple zones with level requirements and progression
- **Quest System** - Idle quests with XP, gold, and item rewards
- **Combat System** - Auto-battle combat against zone monsters
- **Inventory System** - 20-slot grid inventory with item management
- **Equipment System** - 13 equipment slots (armor, weapons, accessories)
- **Talent System** - WoW-style talent trees with 3 specializations

### Game Loop
1. Create character and enter the world
2. Complete quests to gain XP, gold, and items
3. Level up to unlock new zones and gain talent points
4. Fight monsters in zones for rewards and loot
5. Equip better gear to become stronger
6. Invest talent points to specialize your build
7. Progress through increasingly difficult zones

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

## 🎯 Game Systems Overview

### Character Progression
- Level 1 starts with 50 HP
- XP required scales: 100 × level^1.5
- +10% max health per level
- +1 talent point per level

### Equipment
13 equipment slots with stats:
- Offense: Attack damage, speed, crit, lifesteal
- Defense: Max health, armor, dodge
- Utility: XP bonus, gold bonus

### Talents
3 talent trees with 7 tiers each:
- **Combat** - Damage, crit, attack speed
- **Defense** - Health, armor, dodge
- **Utility** - XP gain, gold gain

### Combat
- Auto-battle system
- Timer-based attacks
- Level-scaled monsters
- Rewards: XP, gold, item drops

## 🎨 Features

- **Character Slots** - 6 character slots with unlock progression
- **Save System** - Automatic character save/load
- **Resource Bars** - Animated health and XP bars
- **Tooltips** - Cursor-following tooltips for items, equipment, and talents
- **Visual Feedback** - Color-coded rarity, stat displays, progress indicators

## 🔮 Future Enhancements

- Active abilities with cooldowns
- Passive ability procs
- Equipment set bonuses
- Crafting system
- Trading/Shop system
- Achievements
- Boss fights
- Multiplayer/Leaderboards

## 📝 License

[Add your license here]

## 👤 Author

[Your name/studio]

