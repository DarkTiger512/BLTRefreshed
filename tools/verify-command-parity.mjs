import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { cleanName, parseCommands, readProfile } from "./command-profile.mjs";

const main = parseCommands(readProfile("main"));
const integration = parseCommands(readProfile("HEAD"));
const compared = command => ({
  name: cleanName(command.Name), handler: command.Handler, enabled: command.Enabled,
  moderatorOnly: command.ModeratorOnly, hideHelp: command.HideHelp,
  handlerConfig: command.HandlerConfig ?? {}
});

assert.equal(main.length, 61, "main must contain the authoritative 61-command profile");
assert.deepEqual(integration.map(compared), main.map(compared),
  "Integration command names, handlers, permissions, enabled/help state, or handler configuration diverged from main");

const matrix = fs.readFileSync(path.resolve("docs/twitch-integration/readiness/COMMAND-PARITY.md"), "utf8");
const rows = [...matrix.matchAll(/^\| `!([^`]+)` \| `([^`]+)` \|/gm)];
assert.equal(rows.length, main.length, "The live parity matrix must contain exactly one row per main command");
assert.deepEqual(rows.map(row => row[1]), main.map(command => cleanName(command.Name)), "The live parity matrix must follow the active main profile order");
console.log(`Command parity verified: ${main.length} main commands match the integration profile and live-test matrix.`);
