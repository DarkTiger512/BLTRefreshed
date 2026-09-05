# Battle balance joining bonuses

Viewers can freely summon or attack. The smaller viewer side offers a joining bonus, locked on each viewer's first successful voluntary join until the mission ends. This encourages equal participation, not equal army strength.

`bonus = MaximumBonus × (other viewers − own viewers) / max(total viewers, DenominatorFloor)` when joining the smaller side; otherwise zero. Counts are taken before joining. Defaults: enabled, maximum 20%, denominator floor 4.

| Summon / Attack | Summon offer | Attack offer |
|---|---|---|
| 0 / 0 | 0% | 0% |
| 1 / 0 | 0% | 5% |
| 3 / 1 | 0% | 10% |
| 8 / 2 | 0% | 12% |
| 10 / 0 | 0% | 20% |
| 2 / 8 | 12% | 0% |

Each viewer counts once, including automatic campaign participants and defeated heroes. Automatic participants get no bonus. Retinues, troops and mounts do not count. Resummoning or replacing a hero does not change the viewer's side or bonus. Failed joins do not earn a bonus.

Use `!balance` before joining for current counts and offers. Successful summon/attack replies and `!battle` report your locked bonus. Offers are estimates; the game determines the award when it accepts your join.

Positive personal kill, kill-streak and battle-result gold/XP receive the locked bonus on land and sea, including positive loss payouts. Fortune affects battle gold and Insight affects BLT skill XP once. Fees, refunds, penalties, retinues, passive income, transfers, practice and tournaments receive no balance bonus.

Common Config has a separate `BattleBalance` section with `Enabled`, `MaximumBonus` (finite and nonnegative), and `DenominatorFloor` (positive integer). Invalid settings yield neutral offers. Disabling yields zero new offers; bonuses already earned remain locked for that mission. The obsolete `Prestige.AttackerRewardMultiplier` is ignored and cannot stack. No campaign migration is needed. Existing troop-strength difficulty settings are unchanged.

Before release, test automatic/voluntary spawning, failed naval spawns, knockouts and replacements in a disposable campaign; compare actual reward and chat amounts on land and sea. Deployment is not part of this change.
