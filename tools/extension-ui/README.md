Twitch Extension UI (Panel + Video Overlay)

This folder contains minimal Twitch Extension views for a Panel and a Video - Fullscreen (overlay) to support both chat replacement and on-stream overlay use cases.

What’s included
- `index.html`: Entry point for the Panel view.
- `index.js`: Wires up `Twitch.ext` auth and provides a placeholder call to an EBS endpoint.
- `styles.css`: Minimal styles for the panel.
- `video-overlay.html`: Entry point for the Video - Fullscreen overlay.
- `video-overlay.js`: Overlay-specific logic (auth, context updates, click-through behavior).
- `video-overlay.css`: Transparent, fullscreen overlay styles with safe click targets.
 - `config.html`: Broadcaster-only configuration view.
 - `config.js`: Reads/writes per-channel config via EBS.
 - `config.css`: Simple form styles.

Local development
1) Serve this folder with any static server (e.g. `npx http-server .`), or use the Twitch Developer Rig to load the Panel view.
2) The UI expects to run inside the Twitch Extension iframe so that `window.Twitch` is available. Loading it directly in a browser won’t initialize auth.

Packaging for Twitch-hosted test
1) Create a ZIP of this folder’s contents (don’t include `node_modules` or build artifacts).
2) In the Twitch Developer Console for your Extension version, choose Asset Hosting → Upload.
3) Configure views:
   - Panel: set entry to `index.html` (optional).
   - Video - Fullscreen: set entry to `video-overlay.html`.
   - Config: set entry to `config.html`.

EBS integration
- `index.js` includes a `sendAction()` stub that POSTs to a placeholder `EXT_EBS_BASE_URL`. Replace it with your real EBS URL and verify JWT on the server using your Extension secret.
 - `video-overlay.js` uses the same EBS placeholder and click-through friendly UI elements.
 - `config.js` calls `/ext/config` GET/POST to manage per-channel config (broadcaster role required for writes).
