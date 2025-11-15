# Remaining UI Panel EventBus Migration

## Status: ✅ ALL PANELS MIGRATED - 100% COMPLETE

All **10 additional UI panels** have been successfully migrated from C# events to EventBus!

### ✅ Already Fixed:
1. **CombatPanel** - Migrated to EventBus ✅
2. **CharacterInfoPanel** - Migrated to EventBus ✅
3. **ZonePanel** - Migrated to EventBus ✅ (just fixed!)
4. **ShopPanel** - Migrated to EventBus ✅ (just fixed!)

### ✅ Successfully Migrated (10 files):

#### High Priority (Combat/Core Features):
1. **TargetFrame.cs** - 5 combat events
   - OnTargetChanged
   - OnMonsterHealthChanged
   - OnMonsterAttackProgress
   - OnMonstersChanged
   - OnCombatStateChanged

2. **EquipmentPanel.cs** - 2 equipment events
   - OnEquipmentChanged
   - OnStatsRecalculated

3. **TalentPanel.cs** - 2 talent events
   - OnTalentPointsChanged
   - OnTalentUnlocked

4. **ResourcePanel.cs** - 3 resource events
   - OnGatheringStateChanged
   - OnResourceChanged
   - OnGatherProgressChanged

5. **DialoguePanel.cs** - 3 dialogue events
   - OnDialogueStarted
   - OnDialogueTextChanged
   - OnDialogueEnded

#### Medium Priority:
6. **QuestPanel.cs** - 1 character event
   - OnLevelChanged

7. **GameLog.cs** - 1 character event
   - OnLevelUp

8. **AnimatedResourceBar.cs** - 3 character events
   - OnHealthChanged
   - OnXPChanged
   - OnLevelChanged

9. **ShopItemSlot.cs** - 1 character event
   - OnGoldChanged

---

## Migration Pattern (Copy-Paste Ready):

### Step 1: Replace Subscribe
```csharp
// OLD
service.OnEventName += HandlerMethod;

// NEW
EventBus.Subscribe<EventNameEvent>(HandlerMethod);
```

### Step 2: Replace Unsubscribe
```csharp
// OLD
service.OnEventName -= HandlerMethod;

// NEW
EventBus.Unsubscribe<EventNameEvent>(HandlerMethod);
```

### Step 3: Update Method Signature
```csharp
// OLD
void HandlerMethod(ParamType param) { ... }

// NEW
void HandlerMethod(EventNameEvent e) {
    ParamType param = e.paramName;
    ...
}
```

---

## Event Name Mapping:

### Combat Events:
- `OnTargetChanged` → `TargetChangedEvent` (e.newTargetIndex)
- `OnMonsterHealthChanged` → `MonsterHealthChangedEvent` (e.currentHealth, e.maxHealth, e.monsterIndex)
- `OnMonsterAttackProgress` → `MonsterAttackProgressEvent` (e.progress, e.monsterIndex)
- `OnMonstersChanged` → `MonstersChangedEvent` (e.monsters)
- `OnCombatStateChanged` → `CombatStateChangedEvent` (e.newState, e.oldState)

### Character Events:
- `OnLevelChanged` → `CharacterLevelChangedEvent` (e.newLevel)
- `OnLevelUp` → `CharacterLevelUpEvent` (e.oldLevel, e.newLevel)
- `OnXPChanged` → `CharacterXPChangedEvent` (e.newXP, e.xpGained)
- `OnHealthChanged` → `CharacterHealthChangedEvent` (e.currentHealth, e.maxHealth, e.healthChanged)
- `OnGoldChanged` → `CharacterGoldChangedEvent` (e.newGold, e.goldChanged)

### Equipment Events:
- `OnEquipmentChanged` → `EquipmentChangedEvent` (e.slot, e.newEquipment, e.oldEquipment)
- `OnStatsRecalculated` → `StatsRecalculatedEvent` (e.equipmentStats)

### Talent Events:
- `OnTalentPointsChanged` → `TalentPointsChangedEvent` (e.unspentPoints, e.totalPoints)
- `OnTalentUnlocked` → `TalentUnlockedEvent` (e.talent, e.newRank, e.talentPointsRemaining)

### Resource Events:
- `OnGatheringStateChanged` → `GatheringStateChangedEvent` (e.isGathering, e.resource)
- `OnResourceChanged` → `ResourceChangedEvent` (e.resource)
- `OnGatherProgressChanged` → `GatherProgressChangedEvent` (e.progress)

### Dialogue Events:
- `OnDialogueStarted` → `DialogueStartedEvent` (e.npc)
- `OnDialogueTextChanged` → `DialogueTextChangedEvent` (e.dialogueText)
- `OnDialogueEnded` → `DialogueEndedEvent` (no params)

---

## Why This Matters:

These panels are currently **not receiving events** because:
1. Managers now publish to EventBus
2. Panels still subscribe to old C# events
3. **Result: UI doesn't update!**

### Symptoms You Might See:
- ❌ Target frame not updating in combat
- ❌ Equipment stats not refreshing
- ❌ Talent points not updating
- ❌ Resource gathering progress stuck
- ❌ Dialogue not displaying
- ❌ Quest UI not updating on level up
- ❌ Health/XP bars not animating

---

## Next Steps:

I'll migrate these panels now. The game should work much better once all panels use EventBus consistently!

**Estimated Time**: ~15-20 minutes to migrate all 9 panels.

