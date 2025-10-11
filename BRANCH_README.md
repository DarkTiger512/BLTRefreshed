# Feature Branch: Random Events System (WIP)

**Branch:** `feature/random-events`  
**Base:** `main` (v5.0.0)  
**Status:** 🚧 Work In Progress  
**Created:** October 2025

## Overview

This branch implements a new **Random Events System** for Bannerlord Twitch Enhanced Edition. The system allows dynamic, configurable in-game events that can occur randomly during campaign gameplay, providing engaging content for streamers and their viewers.

## What's Being Worked On

### ✅ Completed

#### Core System
- **RandomEventManager**: Campaign behavior that manages event scheduling and execution
- **RandomEventBase**: Abstract base class for creating new events
- **RandomEventsGlobalConfig**: Configuration system for enabling/disabling events and setting spawn rates

#### Event: Priest's Crusade
- ✅ Fully functional
- A mysterious priest appears offering religious blessings and gold
- Simple text-based event with viewer participation
- Rewards distributed to all BLT adopted heroes

### 🚧 In Progress

#### Event: The Immortal Encounter
- **Status:** Partially complete, dialog issues remain
- A legendary immortal warrior challenges the player to combat
- **Completed Features:**
  - Event structure and spawning system
  - Equipment system (high-tier randomized gear for the immortal)
  - Army composition (Tier 5-6 elite troops for lategame challenge)
  - Battle mechanics with summon restrictions
  - Participant reward system
  - Dialog tree structure
- **Known Issues:**
  - Custom dialog not appearing (showing generic companion dialog instead)
  - Dialog system needs further investigation
- **Recent Changes:**
  - Rewrote army creation to use elite Tier 5-6 soldiers instead of basic troops
  - Fixed invisible equipment issues on army units
  - Improved lategame difficulty scaling

## Technical Architecture

### Key Files

**Core System:**
- `BannerlordTwitch/BLTAdoptAHero/Behaviors/RandomEventManager.cs` - Event scheduler and manager
- `BannerlordTwitch/BLTAdoptAHero/Events/RandomEventBase.cs` - Base class for all events
- `BannerlordTwitch/BLTAdoptAHero/GlobalConfigs/RandomEventsGlobalConfig.cs` - Configuration

**Events:**
- `BannerlordTwitch/BLTAdoptAHero/Events/PriestCrusadeEvent.cs` - Working example event
- `BannerlordTwitch/BLTAdoptAHero/Events/ImmortalEncounterEvent.cs` - Complex boss encounter (WIP)
- `BannerlordTwitch/BLTAdoptAHero/Events/ImmortalBattleRestrictions.cs` - Battle behavior restrictions

**Modified Files:**
- `BannerlordTwitch/BLTAdoptAHero/BLTAdoptAHero.cs` - Integrated RandomEventManager
- `BannerlordTwitch/BLTAdoptAHero/Actions/SummonHero.cs` - Added immortal battle restrictions

### How It Works

1. **Event Registration:** Events register themselves with RandomEventManager during initialization
2. **Scheduling:** Manager checks every in-game hour for eligible events based on:
   - Configuration (enabled/disabled)
   - Cooldown periods
   - Random chance (configurable weight)
3. **Execution:** Selected events run their custom logic (dialog, battles, rewards, etc.)
4. **Dialog System:** Events can register custom campaign dialogs with priority system
5. **Rewards:** Integration with BLT's participant system for distributing rewards

## Configuration

Events can be configured via the BLT configuration system:
- Enable/disable individual events
- Adjust spawn rates and cooldowns
- Customize event-specific parameters

## Future Plans

- [ ] Fix Immortal Encounter dialog system
- [ ] Add more random events (bandit ambushes, merchant caravans, etc.)
- [ ] Implement event chains (sequential related events)
- [ ] Add Twitch viewer voting for event outcomes
- [ ] Create event analytics/statistics tracking
- [ ] Balance testing for rewards and difficulty

## Testing Notes

### Priest's Crusade
- ✅ Spawns correctly
- ✅ Dialog works
- ✅ Rewards distributed properly

### Immortal Encounter
- ✅ Event triggers
- ✅ Immortal spawns with proper equipment
- ✅ Army spawns with elite Tier 5-6 troops
- ⚠️ Custom dialog shows generic companion dialog instead
- ⏳ Battle mechanics need testing
- ⏳ Reward distribution needs testing

## Development Notes

**Dialog System Challenges:**
The Immortal Encounter event has proven challenging due to Bannerlord's conversation system. Multiple approaches have been attempted:
- Priority values (tested up to 1000)
- Hero.OneToOneConversationHero conditions
- CompanionOf = null settings
- Different hero templates (Wanderer, Mercenary, Gangster)
- Manual conversation initiation vs EncounterManager

This remains the primary blocker for completing the Immortal Encounter event.

**Build Requirements:**
- Must use JetBrains Rider MSBuild: `C:\Program Files\JetBrains\JetBrains Rider 2025.2.2.1\tools\MSBuild\Current\Bin\MSBuild.exe`
- Target: .NET Framework 4.7.2
- Game Version: Mount & Blade II: Bannerlord v1.2.12

## How to Test

1. Build the solution (Debug configuration)
2. Launch Bannerlord with BLT modules enabled
3. Start a campaign
4. Events will trigger randomly during gameplay
5. Check logs for `[Random Event]` entries

---

**Note:** This is an experimental feature branch. Code may be unstable and is subject to significant changes.
