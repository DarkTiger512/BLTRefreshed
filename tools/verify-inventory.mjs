import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { execFileSync } from "node:child_process";

const root = path.resolve(import.meta.dirname, "..");
const inventory = path.join(root, "docs", "twitch-integration", "inventory");
const read = name => JSON.parse(fs.readFileSync(path.join(inventory, name), "utf8"));
const commands = read("commands.json");
const rewards = read("rewards.json");
const manifest = read("action-manifest.json");
const semantics = read("command-semantics.json");
for (const action of manifest.actions) {
  assert(action.description && action.description !== action.handler && !/^\{=/.test(action.description) && !/<[^>]+>/.test(action.description) && action.description.length <= 140,
    `Invalid viewer description for ${action.id}: ${action.description}`);
}
const settings = read("settings.json");
const components = read("components.json");
const cleanLoc = value => String(value ?? "").replace(/^\{=[^}]+\}/, "");

assert(commands.length > 0, "No commands were inventoried");
assert(rewards.length > 0, "No rewards were inventoried");
assert.equal(manifest.actions.length, commands.length, "Every configured command must have a manifest action");
assert.equal(semantics.length, commands.length, "Every configured command must have audited parameter semantics");
assert.deepEqual(semantics.map(item => item.actionId), manifest.actions.map(action => action.id), "Semantics must cover every manifest action in order");
assert(semantics.every(item => item.parserSource && item.auditBasis === "handler-source"), "Every command audit must identify its handler parser source");
assert.equal(new Set(manifest.actions.map(action => action.id)).size, manifest.actions.length, "Action IDs must be unique");
assert.deepEqual(manifest.actions.map(action => action.legacyName), commands.map(command => cleanLoc(command.Name)), "Manifest order and legacy mappings must match configured commands");
assert(manifest.actions.every(action => action.id === `command.${action.legacyName.toLowerCase().replace(/[^a-z0-9]+/g, "-")}`), "Action IDs must be stable derivatives of legacy names");
assert(manifest.actions.every(action => Array.isArray(action.inputs)), "Every action needs a structured input definition");
assert(manifest.actions.every(action => action.inputs.every(input => input.id && input.type && typeof input.required === "boolean")), "Every structured input needs an id, type, and required flag");
assert(manifest.actions.every(action => !action.inputs.some(input => input.id === "arguments" || input.id === "command")), "Raw command text inputs are forbidden");
assert(manifest.actions.every(action => action.response?.extension === true), "Every ordinary command must provide an Extension response");
assert(manifest.actions.every(action => action.permissions?.length > 0), "Every action must declare permissions");
const eliteRetinue = manifest.actions.find(action => action.id === "command.eliteretinue");
const adoptByCulture = manifest.actions.find(action => action.id === "command.adoptbyculture");
const attack = manifest.actions.find(action => action.id === "command.attack");
const summon = manifest.actions.find(action => action.id === "command.summon");
const retire = manifest.actions.find(action => action.id === "command.retire");
const rejuvenate = manifest.actions.find(action => action.id === "command.rejuvenate");
const auction = manifest.actions.find(action => action.id === "command.auction");
const giveItem = manifest.actions.find(action => action.id === "command.giveitem");
for (const action of [attack, summon]) {
  assert.equal(action?.handler, "SummonHero", `${action?.id} must use the configured SummonHero handler`);
  assert.deepEqual(action?.inputs?.map(input => [input.id, input.type, input.required]), [["shout", "text", false]], `${action?.id} only accepts an optional in-game shout`);
}
assert.equal(adoptByCulture?.inputs?.[0]?.type, "choice", "Adopt by culture must use a structured culture selector");
assert.equal(adoptByCulture.inputs[0].optionsSource, "cultures", "Adopt by culture must use cultures from the running campaign");
assert.deepEqual(adoptByCulture.inputs[0].options, [], "Overhaul-compatible culture choices must not be hard-coded in the manifest");
assert.equal(eliteRetinue?.handler, "Retinue2", "Elite retinue must use the existing Retinue2 handler");
assert.equal(eliteRetinue?.inputs?.[0]?.type, "choice", "Elite retinue must expose structured choices instead of raw command text");
assert.deepEqual(eliteRetinue.inputs[0].options.map(option => option.value), ["upgrade-one", "upgrade-count", "upgrade-all", "clear-slot", "clear-all"], "Elite retinue must expose every legacy operation");
assert.equal(eliteRetinue.inputs[1].type, "integer", "Elite retinue must accept a typed dismissal slot");
assert.equal(eliteRetinue.inputs.find(input => input.id === "count")?.type, "integer", "Elite retinue must accept a typed recruit or upgrade quantity");
assert.equal(retire.inputs[0].confirmationPolicy, "legacy-token", "Retire confirmation must serialize the handler's yes token");
assert.equal(retire.inputs[0].legacyToken, "retire-yes", "Retire confirmation must use the localized mod-side token");
assert.equal(rejuvenate.inputs[0].confirmationPolicy, "ui-only", "Rejuvenate confirmation must not emit a legacy argument");
assert.deepEqual(auction.inputs.map(input => input.id), ["item", "reserve"], "Auction requires custom item number followed by reserve price");
assert.deepEqual(giveItem.inputs.map(input => input.id), ["item", "target"], "Give item requires item number followed by recipient");
assert(settings.length > 0, "No settings were inventoried");
assert(components.some(component => component.kinds.includes("action-handler")), "Action handlers missing from component map");
assert(components.some(component => component.kinds.includes("harmony-patch")), "Harmony patches missing from component map");
assert(components.some(component => component.kinds.includes("overlay-hub")), "Overlay hubs missing from component map");
assert(components.some(component => component.kinds.includes("persistence")), "Persistence systems missing from component map");

const configuredHandlers = new Set([...commands, ...rewards].map(item => item.Handler).filter(Boolean));
const handlerSymbols = new Set(components.filter(component => component.kinds.includes("action-handler")).flatMap(component => component.symbols));
const missingHandlers = [...configuredHandlers].filter(handler => !handlerSymbols.has(handler));
assert.deepEqual(missingHandlers, [], `Configured handlers absent from component inventory: ${missingHandlers.join(", ")}`);

console.log(`Inventory verified: ${commands.length} commands, ${rewards.length} rewards, ${settings.length} settings, ${components.length} components.`);
execFileSync(process.execPath, [path.join(root, "tools", "verify-command-parity.mjs")], { stdio: "inherit", cwd: root });
