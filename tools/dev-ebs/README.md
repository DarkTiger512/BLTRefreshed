BetterBLT Extension EBS (dev)

A minimal Extension Backend Service for BetterBLT. It validates Extension JWTs, relays viewer actions to the mod over WebSocket, and broadcasts overlay updates via Twitch Extensions PubSub.

Quick start
- Prereqs: Node 18+
- Copy `.env.sample` to `.env` and fill values
- Install deps: `npm i` in `tools/dev-ebs`
- Run: `npm start`

Env vars
- `PORT` (default `8088`)
- `EXT_CLIENT_ID`: Your extension’s Client ID
- `EXT_SECRET`: Base64 secret from the Extension secret keys (use the value shown in console as-is)
- `TWITCH_CLIENT_ID`: Same Client ID for the extension (for Helix)
- `TWITCH_CLIENT_SECRET`: App secret for the extension (Manage Extension → Client secret)

Additional dev/test vars
- `MOD_SHARED_SECRET`: (recommended) a long random string used to authenticate mod <-> EBS WebSocket connections. The mod will compute a HMAC token per channel using this secret. Keep private.
- `MOD_ALLOWED_ORIGIN`: CORS allowlist for overlay/panel requests; use `*` for local dev.

Endpoints
- `GET /health` – simple health check
- `POST /ext/action` – from the overlay/panel, Authorization: `Bearer <extension JWT>`; body `{ action, payload }`
- `WS /mod?channel_id=...` – mod connects here to receive viewer actions; future: auth token

Behavior
- Verifies extension JWTs using `EXT_SECRET` (HMAC SHA256, base64 key)
- On `action: "test"`, publishes a small toast to the channel via Extensions PubSub and forwards action to any connected mod clients for that channel.
 - Adds optional WebSocket authentication for `/mod` connections using `MOD_SHARED_SECRET` (HMAC SHA256 of channel_id + '.' + secret).
 - Rate limits `POST /ext/action` in dev to protect the mod from spam; the limiter keys by the extension JWT's opaque_user_id or user_id and falls back to request IP.

Development notes
- To run locally with a tunnel (ngrok/Cloudflare): set `MOD_ALLOWED_ORIGIN` and your extension's EBS base URL accordingly in the extension UI files.
- Generate the mod token for testing with: `sha256(<channel_id> + '.' + MOD_SHARED_SECRET)` and set `EBSModToken` in the mod settings.

