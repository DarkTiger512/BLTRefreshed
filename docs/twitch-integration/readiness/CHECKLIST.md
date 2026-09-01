# Replacement-readiness checklist

## Implemented and automated

- [x] Integration branch starts at `main` commit `17516d4` and tracks `origin/BLT/twitch-integration`.
- [x] Inventory completeness verifier covers commands, rewards, settings, handlers, patches, overlays, and persistence.
- [x] All 61 ordinary commands are exposed by the runtime command manifest; the command bar preserves legacy argument parsing.
- [x] Legacy chat, command-bar requests, and structured native-view actions use the same `ActionManager` handlers.
- [x] Twitch JWT validation, channel isolation, expiring pairing, hashed installation credentials, replay protection, rate limiting, audit storage, and outbound game WebSockets are implemented.
- [x] The mod contains no Twitch Extension shared secret and no JWT minting code.
- [x] React overlay supports identity-gated command execution, autocomplete, searchable Help, native Inventory/Retinue views, private results, reconnect state, responsive layouts, keyboard access, and reduced motion.
- [x] Legacy chat commands, rewards, YAML profiles, and save behavior remain in place.
- [x] Command-profile parity, inventory verification, backend tests, frontend tests/build, and engine-independent mod tests pass.

## Required before replacing `main`

- [ ] Run the container smoke test on a host with Docker and PostgreSQL available.
- [ ] Run authenticated hosted tests with a registered Twitch Extension and production-like secret storage.
- [ ] Complete all 61 rows in `COMMAND-PARITY.md` in Bannerlord against a disposable campaign save, comparing chat and command-bar outcomes.
- [ ] Verify upgrade and rollback using the current public release package.
- [ ] Complete credential scan and reviewed branch diff against the then-current `main`.
- [ ] Complete Twitch review and verify overlay relevance, placement, and policy compliance.
- [ ] Obtain explicit user approval to replace `main`.

The branch must not be merged while any required item remains unchecked.
