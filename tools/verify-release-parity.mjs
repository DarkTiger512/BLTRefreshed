import assert from "node:assert/strict";
import { classicRef, integrationRef, readSource } from "./verify-command-parity.mjs";

const base = "BannerlordTwitch/BLTAdoptAHero/";
const shared = [
  "Util/PrestigePolicy.cs", "Util/PrestigeHeroSnapshot.cs", "Util/BattleBalancePolicy.cs",
  "Behaviors/BLTAdoptAHeroCampaignBehavior.Prestige.cs", "Behaviors/BLTSummonBehavior.Balance.cs",
  "Behaviors/BLTSummonBehavior.cs", "Behaviors/BLTAdoptAHeroCustomMissionBehavior.cs",
  "Actions/PrestigeCommand.cs", "Actions/SummonHero.cs", "Actions/NavalSummonHero.cs", "Actions/SkillXP.cs",
];
for (const file of shared) assert.equal(readSource(classicRef, base + file), readSource(integrationRef, base + file), `Shared gameplay drift: ${file}`);
const replyNeutral = source => source.replace(/ActionManager\.Send(?:Reply|Success|Failure)/g, "ActionManager.SendResponse");
assert.equal(replyNeutral(readSource(classicRef, base + "Actions/BalanceCommand.cs")), replyNeutral(readSource(integrationRef, base + "Actions/BalanceCommand.cs")), "Balance command gameplay differs beyond reply routing");

// Compare reward code, leaving mission presentation free to differ between variants.
const rewards = source => source.slice(source.indexOf("public void ApplyStreakEffects("), source.indexOf("public void RecordGoldGain("));
for (const ref of [classicRef, integrationRef]) assert(rewards(readSource(ref, base + "Behaviors/BLTAdoptAHeroCommonMissionBehavior.cs")).length > 100, "Reward section missing");
assert.equal(rewards(readSource(classicRef, base + "Behaviors/BLTAdoptAHeroCommonMissionBehavior.cs")), rewards(readSource(integrationRef, base + "Behaviors/BLTAdoptAHeroCommonMissionBehavior.cs")), "Kill, streak or death rewards diverged");
const save = source => source.slice(source.indexOf("public override void SyncData("), source.indexOf("public override void SyncData(") + source.slice(source.indexOf("public override void SyncData(")).indexOf("\n        public ", 1)).replace(/^.*SaveCrashDiagnostics.*\n/gm, "");
const classicSave = save(readSource(classicRef, base + "Behaviors/BLTAdoptAHeroCampaignBehavior.cs"));
assert(classicSave.includes('SyncDataAsJson("ViewerPrestige"'), "Prestige save registration missing");
assert.equal(classicSave, save(readSource(integrationRef, base + "Behaviors/BLTAdoptAHeroCampaignBehavior.cs")), "Campaign persistence differs beyond diagnostics");
for (const ref of [classicRef, integrationRef]) {
  const config = readSource(ref, "BannerlordTwitch/BannerlordTwitch/_Module/Bannerlord-Twitch-v4.yaml");
  assert(/    Prestige:\n      Enabled: true/.test(config), `${ref}: prestige must ship enabled`);
  assert(/    BattleBalance:\n      Enabled: true/.test(config), `${ref}: balance must ship enabled`);
  assert(/DifficultyScalingOnEnemySide: false/.test(config) && /DifficultyScalingOnPlayersSide: false/.test(config), `${ref}: troop-strength scaling must remain disabled`);
  assert(!/AttackerRewardMultiplier:/.test(config), `${ref}: obsolete attacker setting remains in bundled config`);
}
console.log("Release parity verified: prestige resets, policies, ledger, rewards, campaign persistence and commands match; branch-specific config values and integration adapters remain independent.");
