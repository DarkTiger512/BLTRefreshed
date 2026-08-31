# Validation evidence

Validated on 2026-08-31 from `BLT/twitch-integration`.

| Check | Result |
|---|---|
| Inventory and structured parity | Pass: 61 commands, 15 rewards, 1,321 settings, 343 components |
| Bannerlord solution build | Pass: 0 errors; 26 existing/general compiler warnings |
| Backend unit and contract tests | Pass: 5/5 |
| Frontend TypeScript and production build | Pass |
| Frontend component tests | Pass: 1/1 |
| In-app browser, 1920×1080 | Pass: rendered meaningful DOM; search → Ammo → confirm produced personalized result |
| In-app browser, 640×900 | Pass: responsive layout and primary interaction remained usable |
| Credential-pattern scan | Pass: no embedded private key, API token, or concrete Extension secret; only the runtime environment-variable placeholder is present |
| Branch isolation | Pass: current branch and merge-base verified; local `main` remains at `17516d4` |
| Standalone Playwright suite | Environment blocked: Playwright Chromium executable is not installed; the same critical flow passed with the in-app browser |
| Container smoke test | Environment blocked: Docker is not installed on this host |
| Hosted Twitch/Bannerlord live parity | Pending registered Extension, deployed service, and disposable campaign validation |

The blocked and pending checks are release gates in `CHECKLIST.md`; this evidence does not authorize merging to `main`.
