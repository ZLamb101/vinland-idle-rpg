# Window Resizing System

## Overview
The game now properly handles window resizing at any size. NPCs, monsters, and workbenches maintain their relative positions on the background, and draggable UI elements (like GameLog) stay on-screen and remember their position.

## How It Works

### 1. Normalized Position System
- World objects (NPCs/monsters/resources) use **normalized coordinates** (0-1 range) instead of absolute pixels
- Positions are calculated relative to the background image size
- When the window resizes, positions are recalculated automatically

### 2. Auto-Conversion
- **Legacy Support**: Existing absolute pixel positions are automatically converted
- If a position value is > 2.0, it's treated as legacy pixels (assuming 1920x1080 design resolution)
- If a position value is 0-1, it's treated as normalized coordinates
- No manual data migration needed!

### 3. Draggable UI
- GameLog and other draggable panels are constrained to stay on-screen
- Positions are saved as screen percentages (not pixels)
- When you resize the window, panels stay in the correct relative position
- If a panel would go off-screen, it's automatically clamped to visible area

## Setup Instructions

### Adding ResizeHandler to a Scene

The `ResizeHandler` component must be added to detect screen size changes:

1. Open your scene (e.g., `QuestingScene.unity` or `CharacterScene.unity`)
2. Select the **MainCanvas** GameObject (or create one if it doesn't exist)
3. Add Component → Search for "ResizeHandler"
4. The component will automatically detect resolution changes

**Alternatively**, add it programmatically to any persistent GameObject:
```csharp
gameObject.AddComponent<ResizeHandler>();
```

### Configuring Zone Objects

Zone objects (NPCs, monsters, resources) automatically use the new system. No changes needed!

**For New Zones:**
- When setting positions in the Inspector, you can use either:
  - **Normalized coordinates** (0-1): `position = (0.5, 0.5)` = center of background
  - **Legacy pixels** (will auto-convert): `position = (500, 300)` = 500px right, 300px up from center

**Position Reference:**
- `(0, 0)` = Bottom-left of background
- `(0.5, 0.5)` = Center of background
- `(1, 1)` = Top-right of background

### Configuring Draggable Panels

Draggable panels automatically constrain to screen bounds:

```csharp
DraggablePanel dragPanel = GetComponent<DraggablePanel>();
dragPanel.constrainToScreen = true; // Enable bounds checking (default: true)
```

## Technical Details

### Key Classes

#### `WorldPositionCalculator`
Static utility class for position conversions:
- `PixelToNormalized()` - Convert legacy pixels to 0-1 coordinates
- `NormalizedToPixel()` - Convert 0-1 to screen pixels based on background size
- `ScreenPercentageToPixel()` - Convert UI percentages to pixels
- `PixelToScreenPercentage()` - Convert pixels to percentages
- `ClampToScreen()` - Keep panels within visible bounds

#### `ResizeHandler`
MonoBehaviour that detects screen size changes:
- Checks every frame if resolution changed
- Fires `ScreenResizedEvent` via EventBus
- Can be attached to any GameObject

#### `ScreenResizedEvent`
Event published when screen size changes:
```csharp
EventBus.Subscribe<ScreenResizedEvent>(OnScreenResized);

void OnScreenResized(ScreenResizedEvent e)
{
    Debug.Log($"Resized from {e.oldWidth}x{e.oldHeight} to {e.newWidth}x{e.newHeight}");
}
```

### Modified Files

**Core Systems:**
- `Assets/Scripts/Utilities/WorldPositionCalculator.cs` - Position conversion utility
- `Assets/Scripts/Utilities/ResizeHandler.cs` - Resize detection component
- `Assets/Scripts/Utilities/Events/ScreenResizedEvent.cs` - Resize event

**Zone Positioning:**
- `Assets/Scripts/Data/ZoneData.cs` - Added `GetScreenPosition()` methods
- `Assets/Scripts/UI/Panels/ZonePanel.cs` - Uses new positioning system, subscribes to resize
- `Assets/Scripts/UI/Panels/NPCPanel.cs` - Added `UpdatePosition()` method
- `Assets/Scripts/UI/Panels/MonsterPanel.cs` - Added `UpdatePosition()` method
- `Assets/Scripts/UI/Panels/ResourcePanel.cs` - Added `UpdatePosition()` method

**Draggable UI:**
- `Assets/Scripts/UI/Components/DraggablePanel.cs` - Added bounds checking and resize handling
- `Assets/Scripts/UI/Components/GameLog.cs` - Saves/loads position as percentage
- `Assets/Scripts/Data/SettingsData.cs` - Added `gameLogPosition` field

## Testing Checklist

✅ **Zone Objects:**
- [ ] Resize window to various sizes (small, large, ultrawide)
- [ ] Verify NPCs stay aligned with background
- [ ] Verify monsters stay aligned with background
- [ ] Verify resource nodes stay aligned with background

✅ **Draggable UI:**
- [ ] Drag GameLog to different positions
- [ ] Resize window - verify GameLog stays on-screen
- [ ] Restart game - verify GameLog position is remembered
- [ ] Drag GameLog to edge, resize smaller - verify it doesn't go off-screen

✅ **Fixed UI:**
- [ ] Verify ActionBar stays anchored at bottom
- [ ] Verify other UI panels resize properly

## Canvas Scaler Settings

The game uses these Canvas Scaler settings (no changes needed):
- **UI Scale Mode**: Scale With Screen Size
- **Reference Resolution**: 1920 x 1080
- **Match Width or Height**: 0.5 (balanced)

This allows flexible window resizing without black bars.

## Troubleshooting

### "NPCs not repositioning on resize"
- Make sure `ResizeHandler` is added to the scene
- Check that ZonePanel is subscribing to `ScreenResizedEvent`

### "GameLog position not saving"
- Verify SettingsService is available in the scene
- Check that SaveSystem is properly initialized

### "Panel goes off-screen when resizing"
- Ensure `DraggablePanel.constrainToScreen = true`
- Check that panel has proper bounds (RectTransform size)

## Future Improvements

Potential enhancements:
- Editor tool to visualize normalized coordinates
- "Reset UI Layout" button in settings
- Support for saving multiple draggable panel positions
- Minimum window size enforcement


