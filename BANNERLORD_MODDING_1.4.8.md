# Bannerlord 1.4.8 Modding Guide for BLT

Verified 2026-08-27. This is a project-focused guide for BLT, not a complete Bannerlord modding handbook.

## Supported baseline

- Game: Mount & Blade II: Bannerlord `v1.4.8` (Steam build `24573425`).
- Compile-time metadata: `Bannerlord.ReferenceAssemblies` and `Bannerlord.ReferenceAssemblies.NavalDLC` `1.4.8.119303`.
- Runtime patch library: Harmony `2.3.3`, supplied through the `Bannerlord.Harmony` module.
- Runtime: .NET Framework 4.8, x64, `Win64_Shipping_Client`.
- Build: Visual Studio 2022 MSBuild. The reference packages allow builds without copying TaleWorlds binaries into the repository.

Reference assemblies prove that code compiles against the public metadata for a game build. They do not execute the game, validate Harmony targets, prove save compatibility, or replace an in-game acceptance pass.

## Build, stage and test

From the repository root:

```powershell
C:\tmp\nuget.exe restore BannerlordTwitch\BannerlordTwitch.sln -PackagesDirectory BannerlordTwitch\packages -NonInteractive
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' BannerlordTwitch\BannerlordTwitch.sln /restore /p:UseBannerlordReferenceAssemblies=true /p:Configuration=Debug /m
dotnet run --project BannerlordTwitch\BLTAdoptAHero.Tests\BLTAdoptAHero.Tests.csproj -c Release
```

Use `Configuration=Release` for release output. Add `CreatePackage=true` for an archive. Add `DeployToGame=true` only when intentionally replacing the four installed BLT module directories. Deployment is disabled by default.

The solution stages `BannerlordTwitch`, `BLTAdoptAHero`, `BLTBuffet`, and `BLTConfigure`. Each module needs `SubModule.xml`; compiled assemblies belong in `bin\Win64_Shipping_Client`. The main module also stages its web assets.

## Lifecycle and data availability

- `MBSubModuleBase` callbacks initialize modules and Harmony. Keep early initialization independent of campaign objects.
- Register campaign events and behaviors when the campaign game starts.
- Build indexes over `CharacterObject`, cultures, heroes, and upgrade targets after object loading completes. BLT uses `OnGameLoadFinishedEvent` for its troop index.
- Use `CampaignBehaviorBase.SyncData` and BLT's existing serialization conventions for campaign state. New fields need safe defaults because older 5.3.0 saves will not contain them.
- Loaded `CharacterObject` data is the source of truth for native, custom-culture, bandit, militia, elite, and overhaul troop trees. Never assume a fixed tier count or a tree without cycles.

## Smart troop constraints

- `CharacterObject.UpgradeTargets` exposes outgoing upgrade choices; there is no supported engine API that chooses a branch according to a BLT hero class.
- BLT must index reachable terminals, classify those terminals, and select the next direct branch itself.
- `DefaultFormationClass` and `IsMounted` are useful metadata, but modded troops can use unusual combinations. Unknown class names therefore use a logged foot-infantry fallback.
- A class-compatible path means at least one reachable terminal matches the interpreted role. A closed cycle has no terminal and is rejected.
- Selection can be deterministic for testing, but loaded object availability and other mods still require runtime validation.

## Capability matrix

| Technique | What BLT can do | Boundary or risk |
| --- | --- | --- |
| Public campaign APIs | Register behaviors/events, read loaded objects, manage heroes and campaign state | APIs and timing can change between game versions |
| XML/module extension points | Declare modules, dependencies, load order, localization and content XML | XML schemas and official module versions must match the target game |
| Harmony runtime patches | Prefix, postfix, transpile or replace reachable methods | Private targets, IL layouts, patch order and interactions are version-sensitive |
| Gauntlet/UI code | Add or alter views when suitable hooks and assets exist | Not required for this troop port; the abandoned divergent web UI stays out of scope |
| Modding Tools/editor | Author or publish scenes, meshes, materials and other asset packages | Requires the matching external Modding Kit and is outside BLT's code-only build |
| Reference assemblies | Compile against versioned TaleWorlds API metadata | Cannot launch the game or validate behavior |
| Save migration | Add optional/defaulted BLT 5.3.0 fields conservatively | BLTRefreshed 1.2.12 saves are unsupported |

## Harmony and runtime acceptance

Harmony can change behavior that public extension points do not expose, but a successful compile says nothing about whether a patch still finds the intended method. On every Bannerlord update, verify patch application and behavior in logs. Prefer campaign events and public APIs where they provide the needed control.

Runtime acceptance for 1.4.8 must cover all four modules, configuration loading, Twitch startup, new/load campaign flows, Adopt-a-Hero, both retinues, class changes, repeated upgrades, insufficient gold, full rosters, and representative modded troop graphs. Record pre-existing upstream warnings separately from feature regressions.

## Sources

Retrieved 2026-08-27:

- TaleWorlds, [Bannerlord Modding Documentation](https://moddocs.bannerlord.com/) (official).
- TaleWorlds, [Creating a Module — Quick Guide](https://moddocs.bannerlord.com/asset-management/quickguide_create_a_mod/) (official).
- TaleWorlds, [Bannerlord API Documentation](https://apidoc.bannerlord.com/) (official public API browser).
- BUTR, [Bannerlord.ReferenceAssemblies](https://github.com/BUTR/Bannerlord.ReferenceAssemblies) and [generated API documentation](https://butr.github.io/Bannerlord.ReferenceAssemblies.Documentation/) (community-maintained build metadata).
- BUTR, [Bannerlord.BuildResources](https://github.com/BUTR/Bannerlord.BuildResources) (community-maintained module build tooling).
- Bannerlord Modding Documentation community, [Harmony overview](https://docs.bannerlordmodding.com/_intro/advanced.html) (community-maintained).
