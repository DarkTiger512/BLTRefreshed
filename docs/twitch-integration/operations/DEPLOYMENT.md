# Managed service deployment

## Required infrastructure

- A TLS-terminating host for the ASP.NET Core service and WebSockets.
- PostgreSQL 17 or a compatible managed PostgreSQL service with durable backups.
- A Twitch Overlay Extension configured with the deployed frontend asset URLs and backend origin.
- Secret storage that injects `TWITCH_EXTENSION_SECRET` and `BLT_DATABASE` at runtime. Neither value belongs in frontend assets, mod packages, source-controlled production configuration, or logs.

Build `TwitchExtension/frontend` with `VITE_BLT_API_URL` set to the public HTTPS backend. Publish the generated `dist` directory to the Twitch Extension asset host. Build `TwitchExtension/backend/BLT.ExtensionService/Dockerfile`, inject production environment values, run database migrations on startup, and expose port 8080 behind TLS.

The included Compose file is development-only. Its example database password and development-auth switch must not be used in production. Production must set an explicit allowed origin, disable `BLT_ALLOW_DEVELOPMENT_AUTH`, and use a randomly generated database credential.

## Pairing and revocation

The broadcaster opens the Extension configuration view and creates a ten-minute, single-use code. They paste the code and the public service URL into BLT Configure, then restart or reconnect BLT. The mod exchanges the code for a random installation credential and stores it in `Bannerlord-Twitch-Auth.yaml`. The credential is scoped to one channel, stored hashed in PostgreSQL, and can be revoked by setting `installations.revoked_at` through the operator administration path.

## Monitoring

Probe `/health`, monitor WebSocket disconnect rates, pairing failures, rejected JWTs, rate-limit events, and action audit failures. Alert on database unavailability and repeated channel mismatch failures. Never record Twitch JWTs, installation credentials, pairing codes, or the Extension shared secret.
