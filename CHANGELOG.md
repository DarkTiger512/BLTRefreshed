# Changelog

## 5.5.0 — 2026-09-09

Stable **Classic** release for **Bannerlord 1.4.8**, built from `main`. This entry covers changes since the published 5.3.0 beta, including unpublished 5.4.0 work.

### Added

- Campaign prestige with a preview/confirmation flow, progression resets, five permanent perk choices, and prestige markers that survive replacement heroes within the campaign. Use `!prestige` and `!prestige perks` to get started.
- Symmetric battle-balance joining bonuses for the smaller viewer side. `!balance` shows current offers; `!battle` reports the bonus locked at the first successful voluntary join. The default maximum is 20%; automatic participants and retinues do not earn the joining bonus.
- Moderator-launched stream objectives with shared progress, contributor rewards, and a compact OBS overlay.
- Class-guided retinue hiring and upgrades, an adopted-hero spectator campaign map, and live `!ammo` reporting from active mission equipment.
- Restored legacy random events, including the rebuilt cursed artifact event, with configuration and localization improvements.

### Fixed

- Adopt-a-Hero startup compatibility with Bannerlord 1.4.8.
- Adopted-hero reassignment, null objective state during campaign saves, and kingdom sponsor cost overflow.
- Failed and duplicate battle joins, replacement ownership reconciliation, and reward reporting. Personal prestige/balance rewards remain separate from retinue rewards.
- Campaign map projection, live state, and compact overlay layout.
- Stored HTML injection in the viewer command overlay: responses render as text, and custom item names reject HTML.

### Installation and upgrade notes

- Back up campaign saves and local configuration before upgrading. Preserve your existing authorization/settings files; credentials are not shipped in the release.
- Install Bannerlord Harmony, then extract the four module folders into Bannerlord's `Modules` directory. Load Harmony first, BLT after the game modules, and BLT extensions after BLT.
- Install only one BLT variant at a time. This archive is Classic (chat and existing overlays); it contains no Twitch Extension frontend or Linux/Proton package. Cross-variant save compatibility remains unverified.
- Prestige and battle balance ship enabled. Prestige confirmation resets hero progression and replaces the gold balance; read the preview and [prestige guide](docs/PRESTIGE.md) before confirming. Historical kills are not credited on installation. See the [battle-balance guide](docs/BATTLE-BALANCE.md) for reward exclusions.
- In-game campaign testing was confirmed by the release owner. Automated build, policy, and archive checks are recorded separately in the release validation report; this statement does not imply the release agent performed in-game tests.

[Full source comparison](https://github.com/DarkTiger512/BLTRefreshed/compare/5.3.0...v5.5.0)
