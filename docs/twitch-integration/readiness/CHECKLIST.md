# Replacement-readiness checklist

## Implemented and automated

- [x] Integration branch starts at `main` commit `17516d4` and tracks `origin/BLT/twitch-integration`.
- [x] Inventory completeness verifier covers commands, rewards, settings, handlers, patches, overlays, and persistence.
- [x] All 61 ordinary commands have stable IDs and structured inputs; raw command fields are rejected.
- [x] Legacy chat and structured actions use `ActionManager` handlers.
- [x] Twitch JWT validation, channel isolation, expiring pairing, hashed installation credentials, replay protection, rate limiting, audit storage, and outbound game WebSockets are implemented.
- [x] The mod contains no Twitch Extension shared secret and no JWT minting code.
- [x] React overlay supports anonymous browsing, identity-gated actions, search, categories, structured forms, confirmation, results, reconnect state, responsive layouts, keyboard access, and reduced motion.
- [x] Legacy chat commands, rewards, YAML profiles, and save behavior remain in place.
- [x] Bannerlord solution build, inventory verifier, backend tests, and frontend build/tests pass.

## Required before replacing `main`

- [ ] Run the container smoke test on a host with Docker and PostgreSQL available.
- [ ] Run authenticated hosted tests with a registered Twitch Extension and production-like secret storage.
- [ ] Run the complete action matrix in Bannerlord against a disposable campaign save, comparing chat and Extension outcomes.
- [ ] Verify upgrade and rollback using the current public release package.
- [ ] Complete credential scan and reviewed branch diff against the then-current `main`.
- [ ] Complete Twitch review and verify overlay relevance, placement, and policy compliance.
- [ ] Obtain explicit user approval to replace `main`.

The branch must not be merged while any required item remains unchecked.
