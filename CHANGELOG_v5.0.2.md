# Changelog - Version 5.0.2

**Release Date:** TBD

## ✨ New Features

### Fief Renaming Command (!clan rename fief)

**New clan management feature for settlement customization**

Clan leaders can now rename their fiefs (towns and castles) with the `!clan rename fief` command.

#### Features

- **Rename your settlements:** Change the name of any fief owned by your clan
- **Flexible syntax:** Use either `!clan rename fief <current name> to <new name>` format
- **Clan leader only:** Must be the clan leader to rename fiefs
- **Gold cost:** Configurable cost (default: 500,000 gold)
- **Validation:** Checks ownership, ensures fief exists, and validates hero has sufficient gold
- **Persistent:** Renamed fiefs persist in save games and appear on the campaign map

#### Usage

Type `!clan rename fief <current name> to <new name>` in chat as a clan leader.

**Example commands:**
- `!clan rename fief Danustica to Newtown`
- `!clan rename fief Castle of Rovalt to Iron Fortress`
- `!clan rename fief Epicrotea to Viking Bay`

**Example outputs:**
- `Renamed fief from 'Danustica' to 'Newtown'`
- `Could not find fief 'NonExistentTown' in your clan's possessions`
- `You are not the leader of your clan` (if not clan leader)
- `Not enough gold! Need: 500,000💰 | Have: 250,000💰` (insufficient gold)

#### Technical Implementation

- Command handler: `ClanManagement.HandleRenameFiefCommand`
- Uses `Settlement.Name` property setter with `TextObject`
- Validates clan ownership through `Clan.Fiefs` collection
- Case-insensitive fief name matching
- Broadcasts rename event to all players via `Log.ShowInformation`

#### Configuration

Fief renaming can be configured in BLT Configure:

**Settings:**
- **Enabled:** Toggle fief renaming feature (default: enabled)
- **Gold Cost:** Cost to rename a fief (default: 500,000 gold)

Located under: `Clan Management → Rename Fief` category

#### Requirements

- Must be in a clan
- Must be the clan leader
- Must own at least one fief
- Must have sufficient gold for the rename cost

### Ammo Check Command (!ammo)

**New quality-of-life feature for ranged heroes**

Archers and horse archers can now check their remaining ammunition during battles with the `!ammo` command.

#### Features

- **Real-time ammo tracking:** Shows current ammunition count for arrows, bolts, and throwing weapons
- **Detailed breakdown:** Displays ammo count per weapon type (e.g., "Barbed Arrows: 30, Bodkin Arrows: 25")
- **Total count:** Shows combined ammunition across all equipped ammo types
- **Class validation:** Automatically detects if hero is an archer or horse archer
- **Battle-only:** Command works during active missions where the hero is spawned

#### Usage

Type `!ammo` in chat during a battle to see your hero's remaining ammunition.

**Example outputs:**
- `💥 Ammunition Status (Archer): Barbed Arrows: 30 | Total: 30`
- `💥 Ammunition Status (Horse Archer): Steppe Arrows: 25, Javelins: 8 | Total: 33`
- `Out of ammo! You're running on empty! 🏹💨` (when ammunition is depleted)

#### Technical Implementation

- Command handler: `CheckAmmo.cs`
- Supported ammo types: Arrows, Bolts, Thrown weapons
- Uses `EquipmentElement.Amount` property for accurate ammo counting
- Scans all weapon equipment slots (WeaponItemBeginSlot through NumAllWeaponSlots)
- Validates hero is alive and spawned in active mission

#### Configuration

Command can be configured in `Bannerlord-Twitch-v4.yaml`:
```yaml
- Name: ammo
  Handler: CheckAmmo
  ID: 7a8f9e2b-1c3d-4f5e-8a6b-9c0d1e2f3a4b
  Enabled: true
  Help: check remaining ammunition
  RespondInOverlay: true
```

## 🐛 Bug Fixes

### Hero Floating Labels Feature Removed

**Disabled and cleaned up the hero widget feature entirely due to screen clutter issues**

- **Issue:** Hero floating labels (viewer names above adopted heroes) cause severe screen clutter for streamers with many adopted heroes
- **Previous state:** Feature was broken in commit 517d557 (removed `[DefaultView]` but never properly reinstantiated)
- **Decision:** Rather than fixing it, the feature has been **permanently disabled** as it creates a poor user experience for popular streamers
- **Impact:** Battles will have cleaner visuals without dozens of floating name labels cluttering the screen

#### Technical Changes

- ❌ **Deleted** `BLTHeroWidgetBehavior.cs` (324 lines of dead code)
- ❌ **Removed** `ShowHeroFloatingLabels` config property from `GlobalCommonConfig.cs`
- ❌ **Removed** `ShowNameMarkers` config property from `GlobalCommonConfig.cs` 
- 🧹 **Cleaned up** comments in `BLTAdoptAHero.cs` referencing the removed feature
- 🧹 **Removed** csproj reference to the deleted behavior file

#### Benefits

✅ **Cleaner battle visuals** - no cluttered floating labels during combat  
✅ **Cleaner codebase** - removed 324 lines of unused code  
✅ **No misleading config options** - removed non-functional settings  
✅ **Better performance** - no overhead from dead code in compilation

**Note:** The standard overhead name markers remain functional via the game's native `MissionNameMarkerUIHandler`.

### Clan Tier Stuck at Level 1 - Proper Fix

**Fixed the actual clan tier progression issue (previous fix was incorrect)**

- **Issue:** Clans created with the `!clan create` command would stay at tier 1 regardless of renown amount
- **Previous incorrect fix (commit 7be39f2):** Changed `AddRenown(renown, false)` to `AddRenown(renown, true)`, but this only controls UI notifications, not tier calculation
- **Root cause:** The `shouldNotify` parameter in `AddRenown()` only controls whether to show notifications to the player, **NOT** whether to trigger tier calculations. Bannerlord's `Clan.Tier` property is read-only and doesn't automatically recalculate when renown is added.
- **Proper solution:** After adding renown, manually calculate the correct tier based on Bannerlord's tier thresholds and set it using reflection

#### Technical Changes

**Bannerlord's Clan Tier Thresholds:**
- Tier 0: 0 renown
- Tier 1: 50 renown
- Tier 2: 150 renown
- Tier 3: 350 renown
- Tier 4: 650 renown
- Tier 5: 1150 renown
- Tier 6: 2050 renown

**Implementation:**
```csharp
newClan.AddRenown(settings.Renown, false); // shouldNotify only controls UI

// Calculate correct tier based on renown
int calculatedTier = 0;
if (newClan.Renown >= 2050) calculatedTier = 6;
else if (newClan.Renown >= 1150) calculatedTier = 5;
else if (newClan.Renown >= 650) calculatedTier = 4;
else if (newClan.Renown >= 350) calculatedTier = 3;
else if (newClan.Renown >= 150) calculatedTier = 2;
else if (newClan.Renown >= 50) calculatedTier = 1;

// Use reflection to set read-only Tier property
if (calculatedTier > 0)
{
    typeof(Clan).GetProperty("Tier")?.SetValue(newClan, calculatedTier);
}
```

#### Benefits

✅ **Clans now correctly start at the tier corresponding to their renown**  
✅ **No more stuck at tier 1** regardless of starting renown  
✅ **Properly implements Bannerlord's tier progression system**  
✅ **Future-proof** - uses official tier thresholds

#### Impact

- Clans created with high starting renown (e.g., 500 renown) will now correctly start at tier 4
- Rebellion commands and other tier-gated features will work properly
- Fixes the misunderstanding about the `shouldNotify` parameter in `AddRenown()`

## 📝 Notes

This release adds quality-of-life improvements for ranged combat heroes and fixes a critical clan tier progression bug that was previously addressed incorrectly.

**Compatibility:** Save-compatible with v5.0.0 and v5.0.1 - no campaign restart required
