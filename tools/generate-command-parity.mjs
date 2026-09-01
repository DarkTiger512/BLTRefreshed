import fs from "node:fs";
import path from "node:path";
import { cleanName, parseCommands, readProfile } from "./command-profile.mjs";

const commands = parseCommands(readProfile("main"));
const lines = [
  "# Command parity matrix", "",
  "Generated from the active `main` v4 profile. Outcomes are valid only when backed by a real Bannerlord session; structural mapping is not a pass.", "",
  "| Command | Handler | Permission | Valid | Invalid/missing | Boundary/multi-word | Reconnect | Outcome | Evidence |", "|---|---|---|---|---|---|---|---|---|",
  ...commands.map(command => `| \`!${cleanName(command.Name)}\` | \`${command.Handler}\` | ${command.ModeratorOnly ? "Moderator/broadcaster" : "Viewer"} | ☐ | ☐ | ☐ | ☐ | Not Run | — |`),
  "", "Allowed outcomes: `Passed`, `Failed`, `Blocked`, or `Not Applicable`. A failed row requires an issue/evidence reference; no unexplained Pending or Failed row is release-ready.", ""
];
fs.writeFileSync(path.resolve("docs/twitch-integration/readiness/COMMAND-PARITY.md"), lines.join("\n"));
console.log(`Generated ${commands.length} live command rows from main.`);
