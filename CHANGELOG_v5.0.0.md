# 🎉 BannerlordTwitch v5.0.0 - Enhanced Edition

> **For Mount & Blade II: Bannerlord v1.2.12**  
> Released: October 11, 2025

---

## 📑 Table of Contents

- [🎮 What's New](#-whats-new)
  - [⚔️ Cursed Artifact Event](#️-cursed-artifact-event)
  - [👻 Immortal Encounter Event](#-immortal-encounter-event)
  - [⛪ Priest Crusade Event](#-priest-crusade-event)
- [🔧 Major Fixes](#-major-fixes)
- [⚙️ Important Changes](#️-important-changes)
- [📦 Installation](#-installation)
- [❓ FAQ](#-faq)
- [🔗 Links & Support](#-links--support)

---

## 🎮 What's New

BannerlordTwitch v5.0.0 brings exciting new random events that add drama and excitement to your streams! Your viewers' heroes can now face curses, battle immortal enemies, and participate in religious crusades.

### ⚔️ Cursed Artifact Event

**What is it?**  
Random heroes get cursed and must fight through 10 battles to break the curse and earn legendary rewards!

**How it works:**
- 🎯 **Cursed heroes** appear with an asterisk (*) next to their name in battle
- ⚔️ **They fight weaker** - deal only 50% damage and take 150% more damage
- 💰 **Daily costs** - Lose 500 gold and 250 XP per battle
- 🎲 **10 battles to freedom** - Complete 10 battles to break the curse

**Rewards for breaking the curse:**
- 🗡️ **Legendary Tier 6 weapon** with 150% damage bonus
- 💪 **+3 to ALL attributes** (Vigor, Control, Endurance, Cunning, Social, Intelligence)
- ⭐ **+100 weapon skill XP** for the weapon type

**Risk:**
- � **10% chance** the cursed weapon vanishes and you get nothing (can be cursed again later)
- 🏆 **90% chance** you get the rewards and become immune to future curses!

**Event Settings:**
- Happens rarely: 0.3% chance per day (about once every 2-3 weeks)
- 45-day cooldown between curse events

---

### 👻 Immortal Encounter Event

**What is it?**  
A mysterious immortal warrior appears and challenges your entire clan to an epic battle!

**How it works:**
- 🏆 **Epic Boss Fight** - The immortal brings an army to challenge you
- 👥 **All viewers can join** - Any BLT hero can participate in the battle
- 💰 **Massive Rewards** - Each participating hero gets **100,000 gold** if you win!

**Requirements:**
- Streamer must be at least level 10 to trigger this event
- Only happens when you're ready for a challenge

**Event Settings:**
- Very rare: 0.4% chance per day (about once per season)
- 120-day cooldown between immortal encounters

---

### ⛪ Priest Crusade Event

**What is it?**  
Religious warfare erupts! A priest faction declares a crusade and spawns hostile parties.

**How it works:**
- 🛡️ **Kingdom-wide event** - Affects your entire realm
- ⚔️ **Strategic challenge** - Deal with religious warfare parties
- 📈 **Mid-game event** - Requires Kingdom Tier 2+

**Event Settings:**
- Moderate rarity: 0.64% chance per day
- 90-day cooldown between crusades

---

## 🔧 Major Fixes

### ✅ Twitch Direct Messages Now Work!

**The Problem:**  
The "Respond in DM" feature was completely broken and didn't send any messages to your viewers.

**The Fix:**  
We've rewritten the system to use @ mentions as a fallback. Now when someone should get a DM, they'll get an @ mention in chat instead. This works reliably with modern Twitch!

---

## ⚙️ Important Changes

### What Changed From v4.x

**🎲 Random Events Rebalanced**
- Events now trigger 20% less often (better pacing for your streams)
- All three random events have been completely rebalanced
- Event cooldowns increased to prevent spam

**❌ Removed Features**
- `!simgold` command has been disabled (prevented economy exploits)
- "Hero Floating Labels" feature removed (it didn't work properly)

**💾 Better Save System**
- Curse tracking now saves properly between sessions
- Your progress won't be lost if you quit the game

---

## 📦 Installation

### Requirements
- Mount & Blade II: Bannerlord **v1.2.12**
- .NET Framework 4.8 or higher
- Twitch account with API credentials configured

### How to Install

1. **Download** `BLT-v5.0.0-For-Game-Version-v1.2.12.rar`
2. **Extract** the archive
3. **Copy** all mod folders to your Bannerlord `Modules` folder
4. **Launch** Bannerlord and enable the modules in the launcher:
   - BannerlordTwitch
   - BLTAdoptAHero
   - BLTBuffet
   - BLTConfigure
5. **Configure** your Twitch credentials using BLTConfigure
6. **Start** the game and enjoy!

### Upgrading from v4.x
Your existing saves are compatible! The mod will automatically update your config files on first load.

---

## ❓ FAQ

**Q: Can cursed heroes still fight effectively?**  
A: They're weaker (50% damage dealt, 150% damage taken), but with 10 battles they can earn amazing legendary rewards!

**Q: What happens if my viewer quits before completing the curse?**  
A: The curse progress is saved! They can continue where they left off next time.

**Q: Can a hero be cursed multiple times?**  
A: If they successfully break the curse (90% chance), they become immune. If the weapon vanishes (10% chance), they can be cursed again later.

**Q: Does the Immortal Encounter affect my viewers?**  
A: All viewers can join the battle! The level 10 requirement is only for YOUR character (the streamer) to trigger the event.

**Q: How do I adjust event frequency?**  
A: You can edit `GlobalChanceMultiplier` in the config file. Lower = fewer events, higher = more events. Default is 0.8.

**Q: Are the events too hard/easy?**  
A: All settings are configurable! Check the config files to adjust difficulty, rewards, and frequency to your liking.

---

## � Links & Support

- **📥 Download**: [GitHub Releases](https://github.com/DarkTiger512/BLTRefreshed/releases)
- **🐛 Report Issues**: [Issue Tracker](https://github.com/DarkTiger512/BLTRefreshed/issues)
- **📖 Documentation**: [Installation Guide](INSTALLATION_GUIDE.md)
- **💬 Discord**: [Join our community]
- **� Source Code**: [GitHub Repository](https://github.com/DarkTiger512/BLTRefreshed)

---

## 👥 Credits

- **Lead Developer**: DarkTiger512
- **Original Author**: randomchair (BLT 4.x foundation)
- **Community Testing**: BLT Discord community
- **Special Thanks**: TaleWorlds for the amazing Bannerlord modding API

---

## 📝 Quick Summary of Changes

### ✨ Added
- Three new balanced random events (Cursed Artifact, Immortal Encounter, Priest Crusade)
- Legendary weapon rewards for completing curses
- Asterisk (*) indicators for cursed heroes in battle
- Persistent curse tracking across game sessions
- Working Twitch DM system using @ mentions

### 🔄 Changed
- Event frequencies reduced by 20% for better pacing
- Curse system now uses 10 battles instead of time-based
- Overhead name markers always show (green for allies, red for enemies)
- Better balance for all events

### 🐛 Fixed
- Twitch DMs now work properly with @ mentions
- Curse progress saves correctly between sessions
- Battle overlay shows curse indicators properly

### ❌ Removed
- `!simgold` command (economy exploit prevention)
- Hero floating labels (didn't work properly)

---

*Released with ❤️ by the BannerlordTwitch team*

**Full technical diff**: [`v4.0.0...v5.0.0`](https://github.com/DarkTiger512/BLTRefreshed/compare/v4.0.0...v5.0.0)
