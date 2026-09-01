# Local game integration test

This workflow connects the Extension UI to the real managed service and Bannerlord connector. It does not use the frontend's demo responses and it must only be used with `ASPNETCORE_ENVIRONMENT=Development`.

## One-time PostgreSQL setup

Install PostgreSQL locally, then create a development-only login and database:

```sql
CREATE ROLE blt WITH LOGIN PASSWORD 'blt-development-only';
CREATE DATABASE blt OWNER blt;
```

The example password is local-only. Do not reuse it for a hosted environment.

## Identity values

Find the broadcaster's numeric Twitch channel ID from the BLT log after normal Twitch authentication succeeds. Choose a Twitch username that will own an adopted hero in the test campaign.

Copy `TwitchExtension/frontend/.env.live.example` to `.env.live.local` and replace every placeholder. Vite loads it with `--mode live`. The channel ID must match the ID resolved by the game connector, and `VITE_BLT_VIEWER_NAME` must match the adopted Twitch name.

## Start the service

From `TwitchExtension/backend/BLT.ExtensionService`, set these process-local environment variables and start the service:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5188'
$env:BLT_DATABASE = 'Host=127.0.0.1;Port=5432;Database=blt;Username=blt;Password=blt-development-only'
$env:BLT_ALLOW_DEVELOPMENT_AUTH = 'true'
$env:BLT_DEVELOPMENT_USER_ID = 'replace-with-test-viewer-numeric-user-id'
$env:BLT_DEVELOPMENT_VIEWER_NAME = 'replace-with-adopted-twitch-name'
$env:BLT_DEVELOPMENT_ROLE = 'broadcaster'
dotnet run --no-launch-profile
```

Use the broadcaster role only to create the pairing code. Restart with `BLT_DEVELOPMENT_ROLE=viewer` for ordinary viewer permission tests, or `moderator` for moderator tests. Development-token authentication is rejected outside the Development environment.

Start the live frontend from `TwitchExtension/frontend`:

```powershell
npm run dev -- --mode live
```

The diagnostics strip must show `Service online`, the expected channel, and initially `Game offline`.

## Pair Bannerlord

1. Open `http://127.0.0.1:5173/?anchor=configuration` and generate a pairing code.
2. In BLT Configure, set the integration service URL to `http://127.0.0.1:5188` and enter the code.
3. Restart BLT so the connector exchanges the single-use code.
4. Confirm the strip changes to `Game connected` and receives a state timestamp.
5. Request another code and verify the consumed code is rejected if reused.

The installation credential is stored in `Bannerlord-Twitch-Auth.yaml`. Back up that file and the installed BLT module directories before deploying test binaries. Never commit the credential.

## Test order

Use a new campaign named clearly as an integration test. Validate transport and reconnects first, followed by read-only inventory/retinue/state, controlled campaign mutations, regular battle commands, tournament behavior, and component interruption tests. Record the real outcome in the command parity matrix; do not mark a command passed based on demo mode.

Stop immediately if the diagnostics channel differs from the numeric channel logged by BLT, because requests would intentionally be isolated from the game connector.
