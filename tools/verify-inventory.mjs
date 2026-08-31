import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";

const root = path.resolve(import.meta.dirname, "..");
const inventory = path.join(root, "docs", "twitch-integration", "inventory");
const read = name => JSON.parse(fs.readFileSync(path.join(inventory, name), "utf8"));
const commands = read("commands.json");
const rewards = read("rewards.json");
const manifest = read("action-manifest.json");
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
assert.equal(new Set(manifest.actions.map(action => action.id)).size, manifest.actions.length, "Action IDs must be unique");
assert.deepEqual(manifest.actions.map(action => action.legacyName), commands.map(command => cleanLoc(command.Name)), "Manifest order and legacy mappings must match configured commands");
assert(manifest.actions.every(action => action.id === `command.${action.legacyName.toLowerCase().replace(/[^a-z0-9]+/g, "-")}`), "Action IDs must be stable derivatives of legacy names");
assert(manifest.actions.every(action => Array.isArray(action.inputs)), "Every action needs a structured input definition");
assert(manifest.actions.every(action => action.inputs.every(input => input.id && input.type && typeof input.required === "boolean")), "Every structured input needs an id, type, and required flag");
assert(manifest.actions.every(action => !action.inputs.some(input => input.id === "arguments" || input.id === "command")), "Raw command text inputs are forbidden");
assert(manifest.actions.every(action => action.response?.extension === true), "Every ordinary command must provide an Extension response");
assert(manifest.actions.every(action => action.permissions?.length > 0), "Every action must declare permissions");
const eliteRetinue = manifest.actions.find(action => action.id === "command.eliteretinue");
assert.equal(eliteRetinue?.handler, "Retinue2", "Elite retinue must use the existing Retinue2 handler");
assert.equal(eliteRetinue?.inputs?.[0]?.type, "choice", "Elite retinue must expose structured choices instead of raw command text");
assert.deepEqual(eliteRetinue.inputs[0].options.map(option => option.value), ["upgrade-one", "upgrade-all", "clear-slot", "clear-all"], "Elite retinue must expose every legacy operation");
assert.equal(eliteRetinue.inputs[1].type, "integer", "Elite retinue must accept a typed dismissal slot");
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
