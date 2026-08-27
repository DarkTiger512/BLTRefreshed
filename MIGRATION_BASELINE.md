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

An isolated-module launch on Bannerlord 1.4.8.119303 on 2026-08-27 loaded Harmony plus all four BLT assemblies, and the game log reached `Finished All` without recording a Harmony or BLT exception. The process subsequently terminated after about 106 seconds with Windows exception code `0xE0434352` and produced a crash dump, but neither the game error log nor the Windows event contained a managed exception type or stack trace. Existing log noise includes an unresolved optional `TaleWorlds.PSAI.XmlSerializers` assembly and missing FMOD/particle resources. The crash has not been attributed to the smart-troop feature, but it prevents runtime acceptance until it is reproduced with an official-modules-only baseline and isolated further.

## Runtime verification still required

Assembly loading and the logged initialization path are verified, but the launch is not an accepted runtime baseline because of the subsequent crash. An official-modules-only control launch, followed by module-set isolation if necessary, is still required. Manual authenticated/gameplay acceptance remains required for Twitch startup, campaign creation/loading, adoption, both retinues, repeated upgrades, class changes, saves, and modded troop trees. Old BLTRefreshed 1.2.12 saves are intentionally unsupported.
