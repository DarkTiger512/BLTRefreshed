# Migration and rollback

## Upgrade

1. Back up the current BLT module, YAML profiles, `Bannerlord-Twitch-Auth.yaml`, and campaign saves.
2. Deploy the managed backend and Twitch Extension assets before distributing the new mod.
3. Replace the mod files while retaining existing YAML and save data.
4. Install the Extension, create a pairing code, and enter the service URL and code in BLT Configure.
5. Confirm the managed connector reports connected and that the manifest contains all enabled commands.
6. Exercise one read-only and one mutating action through both chat and the Extension before going live.

Legacy profiles and save-game serialization are unchanged. Chat commands and native channel-point rewards continue to use the existing handlers during the hybrid period. Structured Extension actions enter those same handlers but suppress Twitch chat replies and return personalized Extension results.

## Rollback

Stop the managed connector, restore the previous module files, and retain the unchanged YAML/save data. Revoke the installation credential in the service. The Extension can be deactivated independently; chat commands and channel-point rewards remain the operational fallback. Do not delete the integration branch until the replacement has been verified in production.
