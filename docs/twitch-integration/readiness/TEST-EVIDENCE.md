# Validation evidence

Validated on 2026-09-01 from `BLT/twitch-integration`.

| Check | Result |
|---|---|
| Main-to-integration command profile | Pass: all 61 names, handlers, enabled states, permissions, help visibility, and handler configs match |
| Inventory and structured parity | Pass: 61 commands, 15 rewards, 1,375 settings, 343 components |
| Engine-independent mod tests | Pass, including optional-prefix and multi-word raw argument parsing |
| Bannerlord solution build | Blocked by the existing WPF `CollectionPropertyEditor`/`FrameworkElement` reference failure on this host |
| Backend unit and contract tests | Pass: 8/8 |
| Frontend TypeScript and production build | Pass |
| Frontend component tests | Pass: 8/8 |
| In-app browser, 1920×1080 | Pass: rendered meaningful DOM; search → Ammo → confirm produced personalized result |
| In-app browser, 640×900 | Pass: responsive layout and primary interaction remained usable |
| Credential-pattern scan | Pass: no embedded private key, API token, or concrete Extension secret; only the runtime environment-variable placeholder is present |
| Branch isolation | Pass: current branch and merge-base verified; local `main` remains at `17516d4` |
| Standalone Playwright suite | Environment blocked: Playwright Chromium executable is not installed; the same critical flow passed with the in-app browser |
| Container smoke test | Environment blocked: Docker is not installed on this host |
| Local/hosted Twitch-to-Bannerlord live parity | Not Run: all 61 generated rows require disposable-campaign evidence |

The blocked and pending checks are release gates in `CHECKLIST.md`; this evidence does not authorize merging to `main`.
