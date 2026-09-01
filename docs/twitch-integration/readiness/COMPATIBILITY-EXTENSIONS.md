# Integration compatibility extensions

The Extension reuses the configured chat handlers. These additions extend input or presentation without replacing gameplay behavior:

- `equipcustom`: accepts the existing item index/name and an optional `@EquipmentIndex` suffix so Inventory drag-and-drop can target one compatible slot. Chat usage without the suffix retains the original equip-to-compatible-slots behavior.
- `inv`, `slots`, and `customitems`: open the private Inventory view from the command bar. The chat commands remain available and unchanged.
- `retinuelist`: opens the private Retinue view from the command bar. Mutating `retinue` and `eliteretinue` commands continue through their original handlers.
- `battle`, `stats`, and `ammo`: their information is rendered in the live battle HUD while their chat commands remain available.
