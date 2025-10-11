# 🎉 BannerlordTwitch v5.0.0 - Enhanced Edition

## 📋 Release Overview

| Info | Details |
|------|---------|
| **Release Date** | October 11, 2025 |
| **Game Version** | Mount & Blade II: Bannerlord v1.2.12 |
| **Build Type** | Stable Release |
| **Package** | `BLT-v5.0.0-For-Game-Version-v1.2.12.rar` |

---

## ✨ Major Features

### 🎭 Random Events System

Complete overhaul of the random events system with three balanced events:

#### **⚔️ Cursed Artifact Event**

**Full Implementation:** Persistent curse tracking system with visual indicators

**Mechanics:**
- Heroes cursed for **10 consecutive battles**
- **Asterisk (*)** indicator next to cursed hero names in battle overlay
- **Combat Penalties:**
  - 🗡️ 50% damage dealt penalty
  - 🛡️ 150% damage received penalty
- **Trigger:** 15% chance, 45-day cooldown
- **Economic Impact:**
  - 💰 500 gold drain per battle
  - ⭐ 250 XP drain per battle
  - 🎯 5% chance of weapon vanishing during curse
- **Legendary Rewards:** Tier 6 legendary weapons upon curse completion
- **Persistence:** Dictionary-based tracking across saves and sessions

#### **👻 Immortal Encounter Event**

**Status:** Enabled and balanced

**Mechanics:**
- **Trigger:** 5% chance, 120-day cooldown
- **Reward:** 75,000 gold for defeating immortal enemies
- **Requirement:** Minimum streamer hero level 10
- **Challenge:** Epic late-game encounter with massive rewards

#### **⛪ Priest Crusade Event**

**Mechanics:**
- **Trigger:** 8% chance, 90-day cooldown
- **Requirement:** Kingdom tier 2+ for balanced progression
- **Impact:** Religious warfare event with strategic implications

#### **⚖️ Global Balancing**

| Setting | Value | Effect |
|---------|-------|--------|
| `GlobalChanceMultiplier` | 0.8 | 20% reduction across all events |
| `!simgold` | Disabled | Prevents economy exploits |

---

## 🔧 Critical Fixes

### 💬 Twitch Direct Messaging

| Aspect | Details |
|--------|---------|
| **Issue** | "Respond in DM" feature completely non-functional |
| **Root Cause** | TwitchLib 3.8.0 removed IRC-based whisper support |
| **Solution** | Implemented @ mention fallback system |

**Technical Implementation:**
```csharp
// TwitchService.Bot.cs changes:
- Stores TwitchAPI instance and botUserId
- SendWhisper() now uses @ mentions: @username message
- Graceful degradation when Helix whispers endpoint unavailable
- Full compatibility with modern Twitch API
```

**Files Modified:**
- `TwitchService.Bot.cs` (Lines 27-29, 40, 57-58, 172-195)

---

## 🛠️ Technical Improvements

### 📦 Build System

**Enforced Compiler:**
```
C:\Program Files\JetBrains\JetBrains Rider 2025.2.2.1\tools\MSBuild\Current\Bin\MSBuild.exe
```

**Build Configurations:**
- ✅ Debug builds validated
- ✅ Release builds validated
- ✅ Automated WinRAR packaging

**Output Package:**
```
BLT-v5.0.0-For-Game-Version-v1.2.12.rar
```

### 🗂️ Repository Cleanup

**Merged Branches:**
- ✅ `feature/random-events` → `main`

**Deleted Branches:**
- 🗑️ `feature/random-events` (merged)
- 🗑️ `dynamic-troop-tier-detection` (obsolete)
- 🗑️ `Kanboru-New-1` (merged)
- 🗑️ `randomchair-4.7.3-analysis` (temporary analysis)

**Commits:**
- Main commit: `517d557` (pushed to origin/main)
- Final cleanup commit for v5.0.0

### 📝 Code Quality

**Improvements:**
- Configuration cleanup in `GlobalCommonConfig.cs`
- Removed deprecated config settings
- Harmony patch optimizations
- Persistent data structure improvements
- Dictionary-based curse tracking system

---

## 📦 Installation

### System Requirements

| Requirement | Version |
|-------------|---------|
| **Game** | Mount & Blade II: Bannerlord v1.2.12 |
| **Framework** | .NET Framework 4.8+ |
| **Twitch** | Valid account and API credentials |

### Included Modules

1. **BannerlordTwitch** - Core integration module
2. **BLTAdoptAHero** - Hero adoption and curse system
3. **BLTBuffet** - Reward and buff system
4. **BLTConfigure** - Configuration interface

### Installation Steps

1. Extract `BLT-v5.0.0-For-Game-Version-v1.2.12.rar`
2. Copy all modules to Bannerlord `Modules` folder
3. Enable modules in Bannerlord launcher
4. Configure Twitch API credentials in BLTConfigure
5. Launch game and enjoy!

---

## ⚙️ Configuration Changes

### New Settings

| Setting | Location | Default | Description |
|---------|----------|---------|-------------|
| `GlobalChanceMultiplier` | `Bannerlord-Twitch-v4.yaml` | 0.8 | Global event trigger multiplier |
| Curse Tracking | Automatic | N/A | Dictionary-based persistence |

### Removed Settings

| Setting | Reason |
|---------|--------|
| `ShowHeroFloatingLabels` | Feature removed (non-functional) |
| `ShowNameMarkers` | Always enabled (native feature) |

### Modified Settings

| Setting | Old Value | New Value | Impact |
|---------|-----------|-----------|--------|
| `GlobalChanceMultiplier` | 1.0 | 0.8 | 20% reduction in event triggers |
| `CursedArtifact.TriggerChance` | N/A | 15% | Balanced trigger rate |
| `CursedArtifact.Cooldown` | N/A | 45 days | Prevents spam |
| `ImmortalEncounter.TriggerChance` | N/A | 5% | Rare late-game event |
| `ImmortalEncounter.Cooldown` | N/A | 120 days | Epic event pacing |
| `PriestCrusade.TriggerChance` | N/A | 8% | Mid-tier frequency |
| `PriestCrusade.Cooldown` | N/A | 90 days | Balanced pacing |
| `simgold.Enabled` | true | false | Prevents economy exploits |

---

## 🐛 Known Issues

**Current Status:** ✅ No known critical issues

**Warnings (Non-Critical):**
- MSB3884: Missing ruleset files (cosmetic warning)
- CS1998: Async methods without await (planned refactor)
- CS0169: Unused `pubSub` field (legacy code)

---

## 📊 Performance Metrics

| Metric | Status |
|--------|--------|
| Build Time | ~15 seconds (Release) |
| Memory Footprint | Optimized |
| Save/Load Performance | Enhanced with Dictionary caching |
| Twitch API Latency | < 100ms average |

---

## 🔜 Future Roadmap

### Planned for v5.1.0
- [ ] Additional random events (Plague, Invasion, etc.)
- [ ] Enhanced curse system with multiple curse types
- [ ] Improved battle overlay customization

### Planned for v5.2.0
- [ ] Advanced Twitch integration (Predictions, Channel Points)
- [ ] Performance optimizations
- [ ] Multi-language support expansion

### Long-Term Goals
- [ ] Custom event scripting system
- [ ] Enhanced viewer interaction mechanics
- [ ] Tournament mode improvements

---

## 👥 Credits

| Role | Contributor |
|------|-------------|
| **Lead Developer** | DarkTiger512 |
| **Original Author** | randomchair (BLT 4.x foundation) |
| **Community Testing** | BLT Discord community |
| **Special Thanks** | TaleWorlds for Bannerlord modding API |
| **AI Assistant** | GitHub Copilot (code review and optimization) |

---

## 📄 License

This project is licensed under the terms specified in the LICENSE file.

See [LICENSE](LICENSE) for full details.

---

## 🔗 Links & Resources

| Resource | URL |
|----------|-----|
| **GitHub Repository** | https://github.com/DarkTiger512/BLTRefreshed |
| **Issue Tracker** | https://github.com/DarkTiger512/BLTRefreshed/issues |
| **Releases** | https://github.com/DarkTiger512/BLTRefreshed/releases |
| **Documentation** | [Installation Guide](INSTALLATION_GUIDE.md) |
| **Discord** | [Join our community] |

---

## 📈 Version Comparison

### v5.0.0 vs v4.x

| Feature | v4.x | v5.0.0 |
|---------|------|--------|
| Random Events | Basic/Broken | Fully Balanced ✅ |
| Curse System | Placeholder | Complete Implementation ✅ |
| Twitch DMs | Broken | Working (@ mentions) ✅ |
| Overhead Names | Optional | Always On ✅ |
| Hero Floating Labels | Buggy | Removed ❌ |
| Event Balancing | Unbalanced | Professionally Tuned ✅ |
| Code Quality | Mixed | Cleaned & Optimized ✅ |

---

## 🎯 Breaking Changes

⚠️ **Important:** This release contains breaking changes from v4.x

1. **Removed Features:**
   - Hero floating labels configuration removed
   - ShowHeroFloatingLabels setting no longer exists

2. **Configuration Changes:**
   - `!simgold` command disabled by default
   - Event trigger rates globally reduced by 20%

3. **API Changes:**
   - Twitch whispers replaced with @ mentions
   - Curse tracking now uses persistent Dictionary

**Migration Guide:** Existing saves are compatible, but config files will be automatically updated on first load.

---

## 📝 Detailed Changelog

### Added
- ✨ Complete cursed artifact tracking system with Dictionary persistence
- ✨ Visual asterisk indicators for cursed heroes in battle overlay
- ✨ Legendary tier 6 weapon rewards for completing curses
- ✨ Balanced Immortal Encounter event with 75k gold rewards
- ✨ Priest Crusade event with kingdom tier requirements
- ✨ Global event multiplier system (0.8 default)
- ✨ @ mention fallback for Twitch DMs
- ✨ Automated release packaging with WinRAR

### Changed
- 🔄 Event trigger chances globally reduced by 20%
- 🔄 Curse duration standardized to 10 battles
- 🔄 Overhead name markers now always enabled
- 🔄 Event cooldowns increased for better pacing
- 🔄 Build system enforces JetBrains Rider MSBuild

### Fixed
- 🐛 "Respond in DM" feature completely rewritten
- 🐛 Curse tracking now persists across saves
- 🐛 Battle overlay indicators update correctly
- 🐛 TwitchLib 3.8.0 compatibility issues resolved
- 🐛 Config setting conflicts removed

### Removed
- ❌ ShowHeroFloatingLabels config setting
- ❌ Hero floating labels feature (non-functional)
- ❌ ShowNameMarkers config (always-on now)
- ❌ !simgold command (exploit prevention)
- ❌ Obsolete merged branches cleaned up

---

**Full Diff:** [`v4.0.0...v5.0.0`](https://github.com/DarkTiger512/BLTRefreshed/compare/v4.0.0...v5.0.0)

---

*Released with ❤️ by the BannerlordTwitch team*
