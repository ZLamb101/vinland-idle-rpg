# Future Improvements & Cleanup Tasks

**Status**: Backlog  
**Last Updated**: November 16, 2025  
**Purpose**: Track potential improvements to implement after content creation phase

---

## 🎯 Priority System

- **🔥 High Priority**: Impacts player experience or development velocity significantly
- **⚡ Medium Priority**: Nice to have, improves quality of life
- **💡 Low Priority**: Polish and optimization, implement if time allows

---

## 🏗️ Architecture & Code Quality

### 🔥 High Priority

#### 1. Data-Driven Content Tools
**Problem**: Creating new content (monsters, items, quests) requires manual ScriptableObject creation  
**Solution**: Build editor tools for rapid content creation
- [ ] Bulk monster/item creator (CSV import)
- [ ] Visual talent tree editor
- [ ] Quest template system
- [ ] Loot table generator with preview

**Estimated Time**: 8-12 hours  
**Benefit**: 10x faster content creation, less error-prone

#### 2. Save System Robustness
**Problem**: Current save system uses PlayerPrefs, no cloud backup or corruption recovery  
**Solution**: Implement robust save system
- [ ] Multiple save slots (already partially implemented)
- [ ] Auto-save intervals (configurable)
- [ ] Save file versioning for migration
- [ ] Cloud save integration (Steam, Google Play)
- [ ] Save corruption detection and recovery

**Estimated Time**: 10-15 hours  
**Benefit**: Player trust, reduces support burden

### ⚡ Medium Priority

#### 3. Analytics & Balance Tracking
**Problem**: No visibility into how players progress or where they get stuck  
**Solution**: Add non-intrusive analytics
- [ ] Track average time per level
- [ ] Monitor gold/XP earn rates
- [ ] Identify unused talents/items
- [ ] Track away rewards usage patterns
- [ ] Export balance data to CSV for analysis

**Estimated Time**: 6-8 hours  
**Benefit**: Data-driven balancing decisions

#### 4. Tooltip System Enhancement
**Problem**: Tooltip system works but could be more flexible  
**Solution**: Enhance tooltip capabilities
- [ ] Rich text support (colors, icons)
- [ ] Dynamic tooltip positioning (avoid screen edges)
- [ ] Tooltip templates for common types
- [ ] Comparison tooltips (e.g., equipped vs new item)
- [ ] Delay before showing (prevent flicker on mouse-over)

**Estimated Time**: 4-6 hours  
**Benefit**: Better UX, clearer information

#### 5. Event System Optimization
**Problem**: EventBus currently doesn't track subscription counts  
**Solution**: Add debugging and optimization features
- [ ] Event subscription tracker (who's listening to what)
- [ ] Event frequency profiler (identify spam)
- [ ] Debug panel showing live events
- [ ] Warn on common mistakes (subscribing without unsubscribing)

**Estimated Time**: 3-4 hours  
**Benefit**: Easier debugging, catch memory leaks early

### 💡 Low Priority

#### 6. Combat Visual Enhancements
**Problem**: Combat works but could feel more impactful  
**Ideas**:
- [ ] Screen shake on critical hits
- [ ] Slow-motion on kill (optional)
- [ ] Particle effects for status effects
- [ ] Sound variations (different hit sounds for crits)
- [ ] Combat log color coding

**Estimated Time**: 6-10 hours  
**Benefit**: Game feels more "juicy"

#### 7. UI Animation System
**Problem**: UI transitions are instant, could be smoother  
**Solution**: Add animation system
- [ ] Panel slide-in/out animations
- [ ] Button hover effects
- [ ] Number count-up animations (gold, XP)
- [ ] Level-up celebration animation
- [ ] Notification toast system

**Estimated Time**: 8-12 hours  
**Benefit**: Polish, professional feel

---

## 🚀 Performance & Optimization

### ⚡ Medium Priority

#### 8. Object Pooling
**Problem**: Frequent instantiation/destruction causes GC spikes  
**Targets for pooling**:
- [ ] Damage number popups
- [ ] Projectiles (arrows, spells)
- [ ] Particle effects
- [ ] UI item slots (inventory, shop)
- [ ] Combat visual effects

**Estimated Time**: 4-6 hours  
**Benefit**: Smoother frame rate, less GC

#### 9. Addressables Migration
**Problem**: `Resources.Load` loads everything into memory  
**Solution**: Migrate to Addressables
- [ ] Convert ScriptableObjects to Addressables
- [ ] Async loading with progress bars
- [ ] Memory management (unload unused assets)
- [ ] Asset bundle preparation for builds

**Estimated Time**: 12-20 hours  
**Benefit**: Faster load times, lower memory usage, easier updates

### 💡 Low Priority

#### 10. Audio System
**Problem**: No audio management system currently  
**Solution**: Implement audio manager
- [ ] Music manager (fade in/out, crossfade)
- [ ] SFX pooling (don't create AudioSource per sound)
- [ ] Volume settings persistence
- [ ] Audio mixing (separate music/SFX/UI volumes)
- [ ] Positional audio for combat

**Estimated Time**: 6-10 hours  
**Benefit**: Professional audio experience

---

## 🎮 Gameplay & Content

### 🔥 High Priority

#### 11. Tutorial System
**Problem**: New players don't know what to do  
**Solution**: Create guided tutorial
- [ ] First-time user experience (FTUX)
- [ ] Highlight important UI elements
- [ ] Progressive feature unlocking
- [ ] Context-sensitive help tooltips
- [ ] Skip tutorial option for returning players

**Estimated Time**: 10-15 hours  
**Benefit**: Player retention, reduced confusion

### ⚡ Medium Priority

#### 12. Settings Menu
**Problem**: No way for players to customize experience  
**Solution**: Add comprehensive settings
- [ ] Audio (music/SFX volume)
- [ ] Graphics (quality, resolution, fullscreen)
- [ ] Gameplay (damage numbers, auto-loot, etc.)
- [ ] Keybinds (if adding keyboard controls)
- [ ] Language selection (if localizing)

**Estimated Time**: 6-8 hours  
**Benefit**: Accessibility, player satisfaction

#### 13. Achievement System
**Problem**: No long-term goals or milestones  
**Solution**: Add achievements
- [ ] Achievement data structure
- [ ] Progress tracking
- [ ] Notification on unlock
- [ ] Achievement rewards (cosmetics, titles)
- [ ] Steam/Platform integration

**Estimated Time**: 8-12 hours  
**Benefit**: Player engagement, replayability

#### 14. Daily/Weekly Challenges
**Problem**: No reason to log in daily  
**Solution**: Add recurring challenges
- [ ] Daily quest system
- [ ] Weekly boss challenges
- [ ] Login streak rewards
- [ ] Limited-time events
- [ ] Challenge UI panel

**Estimated Time**: 10-15 hours  
**Benefit**: Player retention, engagement

### 💡 Low Priority

#### 15. Pet/Companion System
**Idea**: Add companions that assist in combat or gathering  
- [ ] Pet ScriptableObjects
- [ ] Pet management UI
- [ ] Pet stats and leveling
- [ ] Pet abilities (auto-loot, bonus XP, etc.)
- [ ] Pet cosmetics

**Estimated Time**: 20+ hours  
**Benefit**: Additional progression system, monetization opportunity

#### 16. Guild/Social System
**Idea**: Add multiplayer social features  
- [ ] Player profiles
- [ ] Friend lists
- [ ] Leaderboards
- [ ] Guilds/Clans
- [ ] Chat system

**Estimated Time**: 40+ hours (backend required)  
**Benefit**: Community building, retention

---

## 🐛 Known Technical Debt

### ⚡ Medium Priority

#### 17. Config System Enhancements
**Current State**: JSON config works but could be better  
**Improvements**:
- [ ] Config validation on load (catch typos early)
- [ ] Per-environment configs (dev/staging/prod)
- [ ] Config hot-reload without entering play mode
- [ ] Config diff viewer (compare balance changes)
- [ ] Config version control integration

**Estimated Time**: 4-6 hours  
**Benefit**: Faster iteration, fewer config errors

#### 18. Error Handling & Logging
**Problem**: Inconsistent error handling and logging  
**Improvements**:
- [ ] Centralized error handler
- [ ] Log levels (Debug, Info, Warning, Error)
- [ ] Log filtering in editor
- [ ] Crash reporting integration
- [ ] Player-friendly error messages

**Estimated Time**: 6-8 hours  
**Benefit**: Easier debugging, better crash reports

#### 19. Unit Tests
**Problem**: No automated testing  
**Solution**: Add critical path tests
- [ ] CombatLogic tests (damage calculations)
- [ ] CharacterData tests (XP, leveling)
- [ ] Inventory tests (add/remove items)
- [ ] Save/load tests
- [ ] Config parsing tests

**Estimated Time**: 10-15 hours  
**Benefit**: Confidence in refactoring, catch regressions

### 💡 Low Priority

#### 20. Code Documentation
**Problem**: Inconsistent XML documentation  
**Improvements**:
- [ ] Document all public APIs
- [ ] Add usage examples in comments
- [ ] Generate API documentation (Doxygen)
- [ ] Architecture diagrams
- [ ] Onboarding guide for new developers

**Estimated Time**: 8-12 hours  
**Benefit**: Easier onboarding, better collaboration

---

## 🔄 Refactoring Opportunities

### ⚡ Medium Priority

#### 21. ScriptableObject Validation
**Problem**: Invalid ScriptableObject data can cause runtime errors  
**Solution**: Add validation
- [ ] `OnValidate()` checks in ScriptableObjects
- [ ] Editor warnings for missing references
- [ ] Min/max value clamping
- [ ] Duplicate name detection
- [ ] Circular dependency checks

**Estimated Time**: 3-4 hours  
**Benefit**: Fewer runtime errors, better designer experience

#### 22. Inventory System Expansion
**Current State**: Basic inventory works  
**Potential Improvements**:
- [ ] Item stacking with max stack size
- [ ] Item sorting (by type, rarity, name)
- [ ] Item filtering
- [ ] Quick-use items (hotbar)
- [ ] Item sets with bonuses
- [ ] Item durability (if desired)

**Estimated Time**: 8-15 hours  
**Benefit**: Better inventory management, deeper itemization

### 💡 Low Priority

#### 23. Character Customization
**Idea**: Visual character customization  
- [ ] Hair styles and colors
- [ ] Skin tones
- [ ] Facial features
- [ ] Armor appearance system
- [ ] Cosmetic slots separate from stats

**Estimated Time**: 20+ hours (art-heavy)  
**Benefit**: Player identity, monetization

---

## 📋 Quick Wins (< 2 hours each)

These are small improvements that provide value without much effort:

- [ ] Add confirmation dialog for important actions (reset talents, delete character)
- [ ] Show "New!" indicator on unlocked content
- [ ] Add keyboard shortcuts (ESC to close panels, etc.)
- [ ] Implement "Sell All Junk" button
- [ ] Add item comparison in tooltip (current vs equipped)
- [ ] Show next level XP requirement in character panel
- [ ] Add "Collected!" feedback when picking up items
- [ ] Implement auto-equip for better items
- [ ] Add "Compare" button in equipment panel
- [ ] Show total gold spent in shop history
- [ ] Add "Time played" stat to character screen
- [ ] Implement "Collect All" button for quest rewards
- [ ] Add visual feedback for insufficient resources
- [ ] Show talent respec cost before clicking
- [ ] Add "Return to Town" quick travel button

---

## 🎯 Recommended Implementation Order

When you're ready to resume architectural improvements:

### Phase 1: Quality of Life (1-2 weeks)
1. Data-Driven Content Tools (Priority #1)
2. Tutorial System (Priority #11)
3. Settings Menu (Priority #12)
4. Quick Wins (pick 5-10 items)

### Phase 2: Retention & Engagement (1-2 weeks)
1. Save System Robustness (Priority #2)
2. Achievement System (Priority #13)
3. Daily/Weekly Challenges (Priority #14)
4. Analytics & Balance Tracking (Priority #3)

### Phase 3: Polish & Performance (1-2 weeks)
1. Object Pooling (Priority #8)
2. UI Animation System (Priority #7)
3. Tooltip System Enhancement (Priority #4)
4. Combat Visual Enhancements (Priority #6)

### Phase 4: Advanced Features (as needed)
1. Addressables Migration (Priority #9)
2. Audio System (Priority #10)
3. Unit Tests (Priority #19)
4. Any custom features specific to your game's design

---

## 📝 Notes

- **Don't implement everything**: Pick what makes sense for your game
- **Prioritize player-facing features**: Architecture is solid now, focus on fun
- **Test with real players**: Before spending time on features, validate they're wanted
- **Iterate quickly**: Small improvements > large refactors at this stage

---

## 🤝 Contributing

When implementing items from this list:
1. Create a branch for the feature
2. Update this document (mark with ✅ or strikethrough ~~like this~~)
3. Add implementation notes if helpful for future reference
4. Update `CHANGELOG.md` with user-facing changes

---

**Remember**: Your architecture is solid. Now make the game fun! 🎮

