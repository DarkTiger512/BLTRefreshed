# Managed service deployment

For the pre-publication Bannerlord connection workflow, see [LOCAL_GAME_TEST.md](LOCAL_GAME_TEST.md). The live-local development identity described there is intentionally unavailable in production.

## Required infrastructure

- A TLS-terminating host for the ASP.NET Core service and WebSockets.
- PostgreSQL 17 or a compatible managed PostgreSQL service with durable backups.
- A Twitch Overlay Extension configured with the deployed frontend asset URLs and backend origin.
- Secret storage that injects `TWITCH_EXTENSION_SECRET` and `BLT_DATABASE` at runtime. Neither value belongs in frontend assets, mod packages, source-controlled production configuration, or logs.

Build `TwitchExtension/frontend` with `VITE_BLT_API_URL` set to the public HTTPS backend. Publish the generated `dist` directory to the Twitch Extension asset host. Build `TwitchExtension/backend/BLT.ExtensionService/Dockerfile`, inject production environment values, run database migrations on startup, and expose port 8080 behind TLS.

The included Compose file is development-only. Its example database password and development-auth switch must not be used in production. Production must set an explicit allowed origin, disable `BLT_ALLOW_DEVELOPMENT_AUTH`, and use a randomly generated database credential.

## Pairing and revocation

The broadcaster opens Twitch Config and creates a ten-minute, single-use code. They paste only that code into BLT Configure and explicitly press **Pair**. Twitch Config then shows the pending installation; **Accept** or **Deny** is staged until the broadcaster presses **Save**. The desktop polls while it remains open and promotes the candidate credential only after approval. The managed-service URL is compiled into BLT Configure (`ManagedServiceUrl`); production builds require an HTTPS value. Credentials are scoped to one channel, stored hashed in PostgreSQL, and remain revocable from Twitch Config.

## Monitoring

Probe `/health`, monitor WebSocket disconnect rates, pairing failures, rejected JWTs, rate-limit events, and action audit failures. Alert on database unavailability and repeated channel mismatch failures. Never record Twitch JWTs, installation credentials, pairing codes, or the Extension shared secret.
