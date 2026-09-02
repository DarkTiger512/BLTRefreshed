# Twitch listing assets — 0.0.7

General category: **Extensions for Games**. Game: **Mount & Blade II: Bannerlord**.
Public author: FNC_Chair. Author/support contact: ghostwing05@gmail.com.
Privacy notice: https://bltrefreshed.evepirate.nl/privacy

## Assets

- `logo-100.png`: 100×100 export of the existing `blt-logo-v2.png` crest.
- `icon-24.png`: 24×24 export of the same crest.
- `discovery-300x200.png`: Twitch discovery artwork; high-resolution source retained.
- `screenshot-*-1024x768.png`: actual browser captures of the Extension running locally with sample data. The footer explicitly labels the data and omitted video backdrop. These are interface demonstrations, not live-game parity evidence.

The screenshot harness is `../frontend/listing-preview.html`; serve the frontend locally with `VITE_BLT_LIVE_INTEGRATION=false`. The default harness route shows the command workspace; `?mode=battle` shows the battle fixture. Inventory and Retinue are opened through the actual interface. Capture at 1024×768. The harness is not a production entry point and changes no gameplay code.

Run `./export-assets.ps1` for deterministic Twitch-size PNG exports. It preserves all source files.

## Discovery artwork prompt

Generated with the built-in ImageGen tool, using the existing BLT crest as a reference:

> Use case: ads-marketing. Create one Twitch Extension discovery image, landscape 3:2 aspect ratio, intended final display 300x200 pixels. Reference image is the existing BLT crest: preserve its gold/navy/ivory palette and unmistakable BLT lettering. Large crisp crest on left half, bold clean ivory text 'BLTredone' on right with two short stacked lines 'Your hero.' and 'Your adventure.' Dark midnight navy background with very subtle stylized medieval banners and castle silhouettes, restrained gold accent lines. Friendly cartoon silhouette style, not photorealistic, no tiny decoration or UI mockups. Generous safe margins, readable at thumbnail scale. This is promotional artwork, not a gameplay screenshot. No Twitch logo, no additional copy. Output only the single finished discovery graphic.

## Release boundary

Listing preparation does not establish full gameplay parity or Twitch approval. Do not submit for review or publish solely because the listing asset checks pass. Review submission and release remain explicit user-approved steps.
