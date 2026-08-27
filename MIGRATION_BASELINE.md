# BLTRefreshed 1.4.7 Migration Baseline

The active codebase was rebased from BLTRefreshed 5.1 onto Randomchair22/Bannerlord-Twitch 5.3.0 (`1.4.7`, commit `29520eb`).
The previous codebase remains available as `legacy/bltrefreshed-5.1` and tag `legacy-bltrefreshed-5.1-migration`.

## Verified without launching the game

- Legacy `packages.config` dependencies restore successfully with NuGet CLI.
- Bannerlord 1.4.7 reference assemblies restore through central package management.
- Debug and Release builds produce all four module DLLs and staged module content.
- Deployment and packaging are explicit opt-ins.
- Smart troop graph and fallback policy tests pass.

## Existing upstream warnings

The baseline contains compiler warnings for unused variables, async methods without `await`, and nullable annotations outside a nullable context. These warnings predate the smart troop port and do not prevent a build.

## Runtime verification still required

After Steam has installed Bannerlord 1.4.7, deploy with `DeployToGame=true`, start the game, and verify module initialization, Harmony patches, configuration, Twitch startup, campaign load, adoption, and both retinue commands. Old BLTRefreshed 1.2.12 saves are intentionally unsupported.
