import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";

const root = path.resolve(import.meta.dirname, "..");
const inventory = path.join(root, "docs", "twitch-integration", "inventory");
const read = name => JSON.parse(fs.readFileSync(path.join(inventory, name), "utf8"));
const commands = read("commands.json");
const rewards = read("rewards.json");
const manifest = read("action-manifest.json");
const settings = read("settings.json");
const components = read("components.json");

assert(commands.length > 0, "No commands were inventoried");
assert(rewards.length > 0, "No rewards were inventoried");
assert.equal(manifest.actions.length, commands.length, "Every configured command must have a manifest action");
assert.equal(new Set(manifest.actions.map(action => action.id)).size, manifest.actions.length, "Action IDs must be unique");
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
