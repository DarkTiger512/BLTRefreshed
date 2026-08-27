# BLTRefreshed 1.4.8 Migration Baseline

The active feature line was rebased from BLTRefreshed 5.1 onto Randomchair22/Bannerlord-Twitch 5.3.0 (`1.4.7`, commit `29520eb`) and then retargeted to Bannerlord 1.4.8 on `BLT/smart-troop-upgrades`.
The previous codebase remains available as `legacy/bltrefreshed-5.1` and tag `legacy-bltrefreshed-5.1-migration`.

## Verified without launching the game

- Legacy `packages.config` dependencies restore successfully with NuGet CLI.
- Bannerlord 1.4.8.119303 reference assemblies restore through central package management.
- Debug and Release builds produce all four module DLLs and staged module content.
- Deployment and packaging are explicit opt-ins.
- Smart troop graph, fallback policy, and ammunition command tests pass.

## Existing upstream warnings

The baseline contains compiler warnings for unused variables, async methods without `await`, and nullable annotations outside a nullable context. These warnings predate the smart troop port and do not prevent a build.

Startup isolation on Bannerlord 1.4.8.119303 on 2026-08-27 established that the official modules and the core `BannerlordTwitch` module were stable. Adding `BLTAdoptAHero` reproduced the crash. A guarded diagnostic launch then exposed an outdated Harmony target: `SetPartyAiAction.GetActionForRaidingSettlement` gained a fifth `bool` parameter in 1.4.8. The patch now targets the current five-parameter overload, and Harmony startup failures are logged instead of escaping through Bannerlord's native callback boundary.

After the correction, a BLSE Standalone launch with Harmony and all four BLT modules remained alive and responsive beyond the previous crash window. The log reached `Finished All`, loaded the user configuration, reported BLT 5.3.0, and initialized the action manager without a Harmony or BLT error. Direct launches through `Bannerlord.Native.exe` are not a valid BLT runtime control because Bannerlord's Mono loader cannot resolve the mod's WPF configuration dependencies without BLSE's assembly resolver.

## Runtime verification still required

The known initialization crash is fixed and BLSE startup is verified. Manual authenticated/gameplay acceptance remains required for Twitch startup, campaign creation/loading, adoption, both retinues, repeated upgrades, class changes, saves, ammunition reporting, and modded troop trees. Old BLTRefreshed 1.2.12 saves are intentionally unsupported.
