# Campaign prestige

Use `!prestige` to view progress and `!prestige perks` for the five permanent choices.
Use `!prestige choose might` (or resilience, vitality, fortune, insight), read the reset
preview, then `!prestige confirm might` within 60 seconds. Confirmation is destructive
and only works outside missions/encounters, with no tournament queue entry or auction.

The first prestige requires 500 new personal human battle defeats and 5,000,000 held
BLT gold. Each prestige adds 250 kills and 2,500,000 gold to the next requirements.
Knockouts count; mounts, friendly troops, practice, tournaments and retinue kills do
not. Installation does not credit historical kills. Death/replacement starts a new run.

Each prestige grants one chosen rank: +2% damage, 2% damage reduction, +5 maximum
battle HP, +2% battle gold, or +2% BLT XP. Each choice caps at 10 ranks; prestige caps
at 50. Perks and the `[P#]` name marker survive replacement heroes in the campaign,
but do not transfer into new campaigns. Perks apply to the hero, not retinues.

A reset removes progression, equipment, custom inventory, both retinues and achievement
unlocks/powers. It keeps appearance, age, class, personality, family, clan, titles,
property, relationships and lifetime stats. Defaults restart at level 1, attributes 2,
zero focus/unspent points, melee/ranged 50, riding/athletics 25, other skills 0,
tier-1 equipment and 50,000 BLT gold. The entire prior gold balance is replaced;
the eligibility cost is not subtracted in addition. Ordinary inheritance cannot restore
the cleared record. Existing campaign holdings continue producing their normal income.

Attacking the streamer gives 10% extra personal kill and battle-result gold/XP,
including positive loss payouts. Fees, refunds, penalties, passive income, transfers,
retinue rewards and tournament/practice rewards do not get that attacker bonus.
Fortune boosts positive battle gold; Insight boosts positive XP awarded through BLT's
skill XP entry point, never direct skill levels. Modifiers multiply and saturate at the
integer reward limit. Combat damage reduction stacks multiplicatively with other effects.

Configure the nested `Prestige` section of Common Config. Invalid settings block resets
and disable perk application. Disabling prestige retains saved progression for later
re-enablement; the separately configured attacker multiplier remains active.

## Release validation

Run the BLTAdoptAHero console test project and build against the installed supported
Bannerlord assemblies. Before releasing, use a disposable campaign save to verify:

- Install on an old save: zero qualifying kills; save/reload preserves new progress.
- Land and naval battle personal kills/knockouts count once; excluded kills do not.
- Confirm a reset, inspect all skill XP/focus/perks, equipment and both retinues, then reload.
- Compare friendly/enemy result and kill payouts, including positive loss rewards/refunds.
- Verify all five perks, repeated spawn callbacks, death/re-adoption, name lookup and inheritance.
- Force a reset failure and verify exact engine/BLT state rollback, without awarding a rank.
- Verify existing clan/family/property/traits and lifetime statistics remain intact.
- On Twitch, verify choose/confirm, stale or missing snapshots, offline state and capped perks.

No live campaign smoke test is implied by automated build/test success. Do not deploy
until these campaign checks have been completed. Both branches retain their own config baseline.
