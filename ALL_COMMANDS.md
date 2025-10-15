# ALL ! Commands (Bannerlord Twitch)

This file was generated from the bundled `Bannerlord-Twitch-v4.yaml` Commands list. It lists each chat command (the `!` command name) and a short description or usage notes taken from the module defaults.

- ach — Show the hero's achievements and tracked stats.
- adopt — Adopt a newly created hero. (Most other actions require an adopted hero.)
- adoptByClan — Adopt a randomly selected hero filtered by clan (disabled by default in bundled config).
- adoptByCulture — Adopt a randomly selected hero filtered by culture. Use `!adoptByCulture list` to list cultures.
- adoptByFaction — Adopt a randomly selected hero filtered by faction (disabled by default).
- adoptByName — Adopt a randomly selected hero filtered by hero name (disabled by default).
- adoptNew — Adopt a newly created hero variant (disabled in default config).
- adoptRandom — Adopt a randomly selected hero (disabled by default).
- attack — (Summon) Use this when streamer enters a battle to spawn the adopted hero on the enemy side (with retinue).
- ammo — Check remaining ammunition for an archer/horse-archer in the current battle.
- auction — Start an auction for one of your custom items. Usage: `!auction (custom item index) (reserve price)`.
- bid — Place a bid on the current auction. Usage: `!bid (amount)`.
- bltbet — Place a bet on a tournament match. Usage: `!bltbet (team) (gold)`.
- buymount — Buy a randomly tiered mount (cost configured in YAML).
- clan — Manage clan actions. Usage: `!clan (join/create/lead/rename/stats/leave/buy title/banner) name`.
- class — Select a new class for your hero. Usage: `!class (new class name)`.
- customitems — Shows your hero's custom item storage (to find custom item indices).
- discarditem — Throw away one of your custom items. Usage: `!discarditem (custom item index)`.
- duel — Challenge another viewer to a PvP duel. Usage: `!duel (viewer_name)` to challenge or `!duel accept` to accept a pending challenge. Winner gets gold and XP, loser pays the entry fee. Results tracked on the leaderboard.
- equip — Upgrade/reroll equipment. Usage: `!equip` or `!equip (tier)`.
- giveitem — Give one of your custom items to another viewer. Usage: `!giveitem (custom index) (viewer)`.
- gold — Show your hero's current gold.
- healing — (internal / effect name) Named effect used by `!heal`.
- heal — Heals your hero over time (works while summoned in battle).
- hero — Adjust physical features of hero (gender, looks, marry). Usage: e.g. `!hero gender male`.
- inv — Show your hero's equipped loadout and inventory (excluding custom items).
- kingdom — Manage kingdom actions. Usage: `!kingdom (join/rebel/create/leave/stats)`.
- nameitem — Name a custom item. Usage: `!nameitem (custom index) (name)`.
- power — Use a hero power (internal handler).
- powers — Show your available powers.
- reequip — Randomize equipment at current tier (reequip mode).
- retinue — Hire/upgrade retinue troops. Usage: `!retinue (blank/all/(amount))`.
- retinuelist — Show current retinue troops.
- retire — Retire your adopted hero.
- smitharmor — Smith a custom armor item (cost in YAML).
- smithweapon — Smith a custom weapon (cost in YAML).
- stats — Show hero general information (clan, gold, location, HP, skills, attributes, retinue).
- summon — Summon your adopted hero on the streamer's side when a battle starts.
- tournament — Join the tournament queue with your adopted hero.
- family — Show your hero's family info.
- itemstats — Show item stats (internal command).
- buyattribute — Buy attribute points for your hero (cost configured in YAML).
- givegold — Transfer gold from your hero to another viewer's hero. Usage: `!givegold (viewer) (amount)`.
- simgold — Simulation-only command to give unlimited gold (disabled by default).
- info — Campaign info / general campaign handler.
- leaderboard — Display the top duel winners from the current save. Shows wins, losses, win rate, and current win streak. Usage: `!leaderboard`.
- rejuvenate — Reduce hero age / rejuvenate (cost configured in YAML).
- campaigninfo — Another campaign info alias.

Notes:
- The `Name` fields in YAML sometimes contain localization tokens (e.g. `{=W5uX9HCJ}gold`). The actual command used in chat is the visible alias (e.g. `gold`, `givegold`, `retinue`).
- Many commands are disabled or hidden by default (see the `Enabled` and `HideHelp` fields in the YAML). Costs, cooldowns and special behavior are configured per-handler in the `HandlerConfig` sections of the YAML.
- For full details (handler config, costs, cooldowns, and example usage) open `BannerlordTwitch/_Module/Bannerlord-Twitch-v4.yaml` and inspect the relevant command block.

If you want, I can:
- Expand each command into a one-paragraph description including HandlerConfig details (costs, cooldowns, subscriber-only flags).
- Produce a filtered list (only enabled commands, or only commands that respond in Twitch chat).

## Events

Below are the major BLT events implemented under `BLTAdoptAHero/Events/` with their default settings (as coded) and a short description of what they do.

- The Cursed Artifact (id: `cursed_artifact`)
	- Default trigger chance: 0.5% per day (settings.TrigerChancePerDay = 0.5)
	- Cooldown: 30 days
	- Key effects: randomly curses an adopted hero. While cursed the hero deals reduced damage and takes increased damage, and the player may suffer daily drains (gold, XP). The cursed hero must participate in a configured number of battles (default 10) to break the curse. When broken there is a chance the artifact vanishes; otherwise the hero receives a legendary weapon (generated weapon, tier 6) plus attribute and weapon-skill bonuses. The event reports progress to the overlay and tracks which heroes have completed the curse so they can't be cursed again.
	- Config highlights: GoldDrainPerDay=1000, XPDrainPerDay=500, DamageDealtPercent=50, DamageTakenPercent=150, BattlesToWin=10, WeaponVanishChance=10, WeaponSkillBonus=100, WeaponDamageBonus=150, AttributeBonus=3.

- The Immortal Encounter (id: `immortal_encounter`)
	- Default trigger chance: 0.2% per day (settings.TriggerChance = 0.002)
	- Cooldown: 90 days
	- Key effects: spawns a very powerful immortal hero and a cult army, forces an on-map encounter (dialogue challenge). If the player accepts, the encounter becomes a special restricted battle (the mod tracks BLT participants so only their adopted heroes are rewarded). If BLT participants win, each participating BLT hero receives a gold reward (default 100,000 gold per participant). The event prevents normal join/command behavior for the duration and cleans up the immortal and army afterwards.
	- Config highlights: ArmySizePercent=120 (party size relative to player), MinimumPlayerLevel=10, GoldRewardPerParticipant=100000, ImmortalName="The Immortal", CultName="Cult of the Eternal".

- Priest's Crusade (id: `priest_crusade`)
	- Default trigger chance: 0.5% per day (settings.TriggerChance = 0.005)
	- Cooldown: 60 days
	- Key effects: creates a crusader clan led by a zealot priest and raises a crusader army (peasant/low-tier composition by default). The crusader clan declares war on the player's kingdom (if the player is in a kingdom) and the event shows an in-game notification announcing the crusade. The army size scales with the player's clan strength (default 80% of player clan strength).
	- Config highlights: ArmySizePercent=80, MinimumKingdomTier=2, PriestName="Father Zealot", CrusadeClanName="Holy Crusaders".

If you'd like, I can:
- Add these event entries into a separate `EVENTS.md` file and include direct links to source files.
- Expand each event into a 1-paragraph developer summary that lists every configurable field and exact code behavior (including which static helpers are exposed).
- Also expand the command section to include HandlerConfig defaults (costs, cooldowns, RespondInTwitch flags) inline for each command.

