# Architecture Improvements Documentation

This document describes the architectural improvements made to the codebase to improve maintainability, testability, and scalability.

## Overview

The following improvements have been implemented:
1. **Service Locator Pattern** - Centralized dependency management
2. **Interface-based Services** - Decoupled manager implementations
3. **JSON-based Save System** - Robust, versioned character data persistence

---

## 1. Service Locator Pattern

### What Changed

Previously, managers used static singletons with `DontDestroyOnLoad`:
```csharp
// OLD WAY
if (CharacterManager.Instance != null) {
    CharacterManager.Instance.AddGold(100);
}
```

Now, managers register with a central service locator:
```csharp
// NEW WAY (using interface)
var characterService = Services.Get<ICharacterService>();
if (characterService != null) {
    characterService.AddGold(100);
}
```

### Benefits

- **Testable**: Can inject mock services for unit testing
- **Flexible**: Can swap implementations without changing calling code
- **Clear Dependencies**: Explicit about what services each class needs
- **No Static State**: Easier to reason about and debug

### Usage

#### Registering a Service

Managers automatically register themselves in `Awake()`:
```csharp
void Awake() {
    // ... singleton setup ...
    Services.Register<ICharacterService>(this);
}
```

#### Using a Service

```csharp
// Get a service
var characterService = Services.Get<ICharacterService>();

// Or try-get (doesn't log errors if not found)
if (Services.TryGet<ICharacterService>(out var service)) {
    service.AddGold(100);
}

// Check if registered
if (Services.IsRegistered<ICharacterService>()) {
    // Service is available
}
```

### Backward Compatibility

**The old static Instance pattern still works!** This is intentional for gradual migration:

```csharp
// Still works (for now)
CharacterManager.Instance.AddGold(100);

// New way (preferred)
Services.Get<ICharacterService>().AddGold(100);
```

You can gradually migrate code to use the service locator without breaking existing functionality.

---

## 2. Interface-based Services

### Available Interfaces

All major managers now implement interfaces:

- `ICharacterService` - Character data, health, gold, XP, inventory
- `IEquipmentService` - Equipment management and stats
- `ICombatService` - Combat state and actions
- `ITalentService` - Talent tree management
- `IResourceService` - Resource gathering
- `IShopService` - Shop transactions
- `IZoneService` - Zone navigation
- `IAwayActivityService` - Offline/away rewards tracking

### Benefits

- **Testing**: Can create mock implementations for unit tests
- **Documentation**: Interface serves as contract for what service does
- **Flexibility**: Multiple implementations possible (e.g., online vs offline mode)
- **Clarity**: Clear separation between interface and implementation

### Example: Creating a Mock for Testing

```csharp
public class MockCharacterService : ICharacterService
{
    private int testGold = 100;
    
    public void AddGold(int amount) { testGold += amount; }
    public int GetGold() { return testGold; }
    // ... implement other interface methods ...
}

// In test
Services.Register<ICharacterService>(new MockCharacterService());
```

---

## 3. JSON-based Save System

### What Changed

**OLD SYSTEM (PlayerPrefs)**:
- Character data split across 15+ PlayerPrefs keys
- Hard to debug and maintain
- No versioning
- Risk of data loss or corruption

**NEW SYSTEM (JSON Files)**:
- Single JSON file per character
- Versioning support for migrations
- Easy to inspect and debug
- Backup capabilities
- All data in one place

### File Structure

Save files are stored at:
```
Application.persistentDataPath/Saves/
├── Character_0.json
├── Character_1.json
└── Character_2.json
```

### Save Data Format

```json
{
  "version": 1,
  "characterName": "Hero",
  "level": 10,
  "gold": 1000,
  "inventoryItems": [...],
  "equippedItems": {...},
  "unlockedTalents": {...},
  "currentZoneIndex": 2,
  "saveTime": "638000000000000000"
}
```

### Usage

#### Saving

```csharp
// Save current game state
int characterSlot = 0;
SaveSystem.SaveCurrentCharacter(characterSlot);

// Or save specific data
SaveData data = SaveData.CreateFromCurrentState();
SaveSystem.SaveCharacter(characterSlot, data);
```

#### Loading

```csharp
// Load character data
int characterSlot = 0;
SaveData data = SaveSystem.LoadCharacter(characterSlot);

if (data != null) {
    data.ApplyToGameState();
}
```

#### Checking Save Files

```csharp
// Check if save exists
if (SaveSystem.SaveFileExists(characterSlot)) {
    // Load it
}

// Get basic info without full load
SaveFileInfo info = SaveSystem.GetSaveFileInfo(characterSlot);
Debug.Log($"Character: {info.characterName}, Level: {info.level}");

// Get all saved character slots
int[] slots = SaveSystem.GetSavedCharacterSlots();
```

#### Backups

```csharp
// Create backup before risky operation
SaveSystem.BackupSaveFile(characterSlot);
```

### Migration from PlayerPrefs

A migration system is included to convert old PlayerPrefs saves to new JSON format:

```csharp
// Check if migration needed
if (SaveSystemMigration.NeedsMigration(characterSlot)) {
    SaveSystemMigration.MigrateCharacter(characterSlot);
}

// Or migrate all characters at once
SaveSystemMigration.MigrateAllCharacters();
```

**Note**: Old PlayerPrefs data is preserved during migration for safety. You can manually delete it after confirming the migration worked.

### Versioning

When you add new fields to `SaveData`, increment the version number:

```csharp
public class SaveData
{
    public int version = 2; // Increment this
    
    // New field
    public int newFeatureData = 0;
}
```

Then add migration logic in `SaveSystem.MigrateSaveData()`:

```csharp
private static SaveData MigrateSaveData(SaveData data, int fromVersion, int toVersion)
{
    if (fromVersion < 2 && toVersion >= 2) {
        // Migrate from version 1 to 2
        data.newFeatureData = 0; // Set default value
    }
    
    data.version = toVersion;
    return data;
}
```

---

## Migration Guide

### For Existing Code

The improvements are designed for **gradual adoption**. You don't need to refactor everything at once.

#### Phase 1: Start Using Services (Optional)

When writing new code, use the service locator:
```csharp
// New code
var characterService = Services.Get<ICharacterService>();
characterService.AddGold(100);
```

Old code still works:
```csharp
// Old code - still works!
CharacterManager.Instance.AddGold(100);
```

#### Phase 2: Use New Save System

Update `CharacterLoader` to use new save system:
```csharp
// Old
string json = PlayerPrefs.GetString("ActiveCharacter");
SavedCharacterData savedData = JsonUtility.FromJson<SavedCharacterData>(json);

// New
SaveData saveData = SaveSystem.LoadCharacter(characterSlot);
saveData.ApplyToGameState();
```

#### Phase 3: Test and Migrate

1. Run migration on test account
2. Verify all data loads correctly
3. Test save/load cycles
4. Roll out to all accounts

---

## Best Practices

### Using Services

1. **Prefer interfaces**: Use `ICharacterService` not `CharacterManager`
2. **Null check**: Always check if service exists
3. **Cache sparingly**: Services can be retrieved multiple times safely
4. **Don't hold references**: Get service when you need it

```csharp
// Good
void UpdateGold() {
    var service = Services.Get<ICharacterService>();
    if (service != null) {
        service.AddGold(10);
    }
}

// Avoid
ICharacterService cachedService; // Might become stale
```

### Saving Data

1. **Save on important events**: Level up, zone change, returning to menu
2. **Don't save every frame**: Too expensive
3. **Create backups before risky operations**: Major updates, migrations
4. **Handle failures gracefully**: Save might fail (disk full, permissions)

```csharp
// Good - save on leaving scene
void OnDestroy() {
    if (characterSlot >= 0) {
        SaveSystem.SaveCurrentCharacter(characterSlot);
    }
}

// Bad - save every frame
void Update() {
    SaveSystem.SaveCurrentCharacter(characterSlot); // DON'T DO THIS
}
```

---

## Troubleshooting

### "Service not found" Error

**Problem**: `Services.Get<ICharacterService>()` returns null

**Solution**:
1. Check manager's `Awake()` calls `Services.Register<T>(this)`
2. Verify manager exists in scene
3. Check order of execution (might be called before Awake)

### Save File Corruption

**Problem**: Save file won't load or loads incorrectly

**Solution**:
1. Check `version` field in JSON file
2. Look for backup files: `Character_X_backup_*.json`
3. Verify JSON is valid (use online JSON validator)
4. Check Unity console for error messages

### Migration Fails

**Problem**: PlayerPrefs data won't migrate

**Solution**:
1. Verify PlayerPrefs key exists: `PlayerPrefs.HasKey("Character_0")`
2. Check console for migration error messages
3. Manually inspect PlayerPrefs data
4. Old data format might be incompatible

---

## Future Improvements

Potential next steps for the architecture:

1. **Full Event Bus**: Replace individual events with centralized bus
2. **Remove DontDestroyOnLoad**: Use scene-based manager lifecycle
3. **Dependency Injection Container**: More powerful than service locator
4. **Addressables**: Replace Resources.Load for asset management
5. **Cloud Saves**: Extend SaveSystem to support cloud sync
6. **Unit Tests**: Now possible with interfaces!

---

## Questions?

For questions or issues with the new architecture:
1. Check this documentation first
2. Review code comments in Services.cs, SaveSystem.cs
3. Look at existing manager implementations for examples
4. Check Unity console for helpful debug messages

