# Overlay design reference

`overlay-concept-v1.png` is the accepted ImageGen design reference for the viewer overlay. It was generated with the built-in ImageGen workflow and refined once for a narrower command surface, unique navigation categories, and a consistent linked-viewer state.

## Design system

- Transparent full-video canvas with interaction chrome confined to the left side.
- Charcoal and cool-steel translucent surfaces with restrained antique-gold emphasis.
- Cinzel-style display headings paired with a compact, modern sans-serif for controls.
- Three regions: category rail, searchable action list, and structured action detail.
- Green, amber, and red communicate availability, cooldown, and unavailable state.
- Minimum 44px interactive targets, visible focus treatment, and reduced-motion support.

## Fidelity ledger

| Comparison | Implementation decision |
|---|---|
| Layout | Preserved rail/list/detail anatomy; production width is capped at 34% of a 1920px video as required. |
| Typography | Preserved serif display/sans control pairing with code-native text. |
| Palette | Preserved charcoal, steel, parchment-gold, and semantic state colors. |
| Controls | Preserved search, selected action, typed form, confirmation, and status toast. |
| Video treatment | Production background stays transparent; the battlefield exists only in the concept to demonstrate placement. |
| Responsive behavior | Narrow layouts stack browser and detail regions beside an icon-only category rail. |

The generated mockup is not shipped as application UI. All text, controls, state, and interaction are implemented in React and CSS.

## Generated command icons

`TwitchExtension/frontend/src/assets/blt-command-icons-v1.png` is the production category-icon sprite generated with the built-in ImageGen workflow. It contains eight transparent heraldic icons in manifest category order: Hero, Battle, Kingdom, Equipment, Progression, Tournament, Community, and General. Command cards and detail views inherit their manifest category icon, giving all actions a consistent visual identity without generated text or inaccessible image-only labels.

`TwitchExtension/frontend/src/assets/blt-logo-v1.png` is the matching transparent BLT shield-and-sword monogram used in the overlay header, category rail, and collapsed launcher.

`blt-command-icons-v2.png` replaces the first production pass with flatter, cartoon-like silhouettes, chunky navy outlines, simplified geometry, and minimal cel shading for stronger recognition at overlay scale. The detailed `v1` sheet remains as a rollback reference.
