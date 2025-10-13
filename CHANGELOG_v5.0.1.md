# Changelog - Version 5.0.1

**Release Date:** October 13, 2025

## 🐛 Bug Fixes

### Retinue Upgrade System - Dynamic Tier Detection

**Fixed critical issue preventing T6 troop upgrades**

- **Issue:** Troops with T6 upgrades (like Aserai Youth → Aserai Vanguard Faris) were incorrectly marked as unable to upgrade beyond T5
- **Cause:** Hardcoded `Tier >= 5` checks prevented the system from discovering T6+ troops
- **Solution:** Replaced all hardcoded tier checks with dynamic "no more upgrades" detection

### Technical Changes

#### `GetFinalTierDestinations` Method
- **Before:** Stopped recursion at `Tier >= 5`, returning T5 units even when T6 existed
- **After:** Only stops when `UpgradeTargets == null || !UpgradeTargets.Any()`, properly detecting ALL final destinations regardless of tier

#### `GetCompatibleFinalDestinations` Method  
- Removed hardcoded `Tier >= 5` check
- Now uses dynamic "no upgrades available" detection
- Improved cycle detection by using consistent visited set across all branches

#### `HasValidUpgradePathToClass` Method
- Removed hardcoded `Tier >= 5` check
- Validates paths based on actual upgrade availability, not arbitrary tier limits
- Fixed visited set handling for better branch traversal

### Benefits

✅ **Future-proof:** Works with any troop tree depth (T4, T5, T6, T7+)  
✅ **Mod compatible:** Handles mods that add higher tier troops  
✅ **Culture agnostic:** Works correctly whether cultures max at T4, T5, or T6  
✅ **Accurate validation:** All three validation methods now use consistent logic

### Impact

- **Aserai Youth** (and similar troops) can now properly upgrade to T6
- **All cultures** with T6 troops will now correctly identify their max tier units
- **No more "Can't upgrade any further"** errors for troops that have valid upgrade paths

## 📝 Notes

This is a patch release that fixes a critical bug in the retinue upgrade system. No new features were added. The fix improves code quality by removing hardcoded assumptions and making the system truly dynamic.

**Compatibility:** Save-compatible with v5.0.0 - no campaign restart required
