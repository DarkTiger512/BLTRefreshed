# BLTRefreshed 1.4.8 Migration Baseline

The active feature line was rebased from BLTRefreshed 5.1 onto Randomchair22/Bannerlord-Twitch 5.3.0 (`1.4.7`, commit `29520eb`) and then retargeted to Bannerlord 1.4.8 on `BLT/smart-troop-upgrades`.
The previous codebase remains available as `legacy/bltrefreshed-5.1` and tag `legacy-bltrefreshed-5.1-migration`.

## Verified without launching the game

- Legacy `packages.config` dependencies restore successfully with NuGet CLI.
- Bannerlord 1.4.8.119303 reference assemblies restore through central package management.
- Debug and Release builds produce all four module DLLs and staged module content.
- Deployment and packaging are explicit opt-ins.
- Smart troop graph and fallback policy tests pass.

## Existing upstream warnings

The baseline contains compiler warnings for unused variables, async methods without `await`, and nullable annotations outside a nullable context. These warnings predate the smart troop port and do not prevent a build.

The 1.4.8 isolated-module startup completed on 2026-08-27 and loaded Harmony plus all four BLT assemblies. The game log reached `Finished All` without a Harmony or BLT exception. Existing runtime noise includes an unresolved optional `TaleWorlds.PSAI.XmlSerializers` assembly and missing FMOD/particle resources; these are baseline warnings, not smart-troop regressions.

## Runtime verification still required

Module initialization and the error-free Harmony/BLT startup pass are complete. Manual authenticated/gameplay acceptance is still required for Twitch startup, campaign creation/loading, adoption, both retinues, repeated upgrades, class changes, saves, and modded troop trees. Old BLTRefreshed 1.2.12 saves are intentionally unsupported.
