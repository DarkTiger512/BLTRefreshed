import fs from "node:fs";
import path from "node:path";

const root = path.resolve(import.meta.dirname, "..");
const sourceRoot = path.join(root, "BannerlordTwitch");
const outputRoot = path.join(root, "docs", "twitch-integration", "inventory");
const yamlPath = path.join(sourceRoot, "BannerlordTwitch", "_Module", "Bannerlord-Twitch-v4.yaml");

const normalize = value => value?.replace(/^['"]|['"]$/g, "").trim() ?? "";
const scalar = value => {
  const text = normalize(value);
  if (text === "") return null;
  if (text === "true") return true;
  if (text === "false") return false;
  if (/^-?\d+(\.\d+)?$/.test(text)) return Number(text);
  return text;
};

function walk(dir) {
  const result = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    if (["bin", "obj", "build", "deploy", "packages", ".git"].includes(entry.name)) continue;
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) result.push(...walk(full));
    else result.push(full);
  }
  return result;
}

function parseTopLevelSequence(lines, start, end) {
  const items = [];
  let current = null;
  let nestedRoot = null;
  for (let index = start; index < end; index += 1) {
    const line = lines[index];
    const startMatch = line.match(/^- ([^:]+):\s*(.*)$/);
    if (startMatch) {
      if (current) items.push(current);
      current = { sourceLine: index + 1, settings: {} };
      current[startMatch[1]] = scalar(startMatch[2]);
      nestedRoot = null;
      continue;
    }
    if (!current) continue;
    const top = line.match(/^  ([^:]+):\s*(.*)$/);
    if (top) {
      const [, key, value] = top;
      if (value.trim() === "" && ["HandlerConfig", "RewardSpec"].includes(key)) {
        nestedRoot = key;
        current[key] = {};
      } else {
        nestedRoot = null;
        current[key] = scalar(value);
      }
      continue;
    }
    const nested = line.match(/^    ([^:]+):\s*(.*)$/);
    if (nested && nestedRoot) current[nestedRoot][nested[1]] = scalar(nested[2]);
  }
  if (current) items.push(current);
  return items;
}

function categoryFor(command) {
  const haystack = `${command.Name ?? ""} ${command.Handler ?? ""} ${command.Documentation ?? ""}`.toLowerCase();
  const categories = [
    ["Tournament", /tournament|arena|bet/],
    ["Battle", /battle|mission|summon|ammo|duel|guard|formation/],
    ["Kingdom", /kingdom|clan|vassal|fief|diplom|party|army|capital/],
    ["Equipment", /equip|item|smith|retinue|upgrade/],
    ["Progression", /skill|attribute|focus|class|power|gold|xp/],
    ["Community", /objective|leaderboard|auction|bid/],
    ["Hero", /hero|adopt|family|heir|follow|retire|rejuven/],
  ];
  return categories.find(([, pattern]) => pattern.test(haystack))?.[0] ?? "General";
}

function actionInput(command) {
  const text = `${command.Help ?? ""} ${command.Documentation ?? ""}`.toLowerCase();
  if (/list|status|info|check|leaderboard/.test(text)) return [];
  if (/name/.test(text)) return [{ id: "name", type: "text", required: true }];
  if (/amount|gold|points|xp/.test(text)) return [{ id: "amount", type: "integer", required: false }];
  return [{ id: "arguments", type: "text", required: false, legacyFallback: true }];
}

function buildActions(commands) {
  return commands.map(command => ({
    id: `command.${String(command.Name ?? command.Handler).replace(/^\{=[^}]+\}/, "").trim().toLowerCase().replace(/[^a-z0-9]+/g, ".").replace(/^\.|\.$/g, "")}`,
    legacyName: command.Name,
    handler: command.Handler,
    category: categoryFor(command),
    description: command.Documentation || command.Help || command.Handler,
    enabledByDefault: command.Enabled === true,
    hiddenFromHelp: command.HideHelp === true,
    permissions: command.ModeratorOnly ? ["moderator", "broadcaster"] : ["viewer", "moderator", "broadcaster"],
    response: {
      twitch: command.RespondInTwitch === true,
      overlay: command.RespondInOverlay === true,
      extension: true,
      privateByDefault: command.ModeratorOnly !== true,
    },
    availability: ["game.started"],
    cooldown: { strategy: "legacy" },
    mutatesCampaign: !/info|status|check|list|leaderboard|help|logs|ammo/i.test(`${command.Handler} ${command.Name}`),
    inputs: actionInput(command),
    source: { file: path.relative(root, yamlPath).replaceAll("\\", "/"), line: command.sourceLine },
  }));
}

function extractComponents(files) {
  const patterns = [
    ["action-handler", /(?:ActionHandlerBase|HeroActionHandlerBase|HeroCommandHandlerBase|ImproveAdoptedHero|ICommandHandler|IRewardHandler)/],
    ["behavior", /\b(?:CampaignBehaviorBase|MissionBehavior|Behavior)\b/],
    ["harmony-patch", /\bHarmonyPatch\b|\.Patch\(/],
    ["overlay-hub", /:\s*Hub\b|IHubContext|GetHubContext/],
    ["persistence", /Saveable|IDataStore|SyncData|ScopedJsonSync|Yaml/],
    ["twitch-service", /TwitchService|EventSub|ExtensionPubSub/],
    ["test", /\[Test\]|Assert\.|Tests?\b/],
    ["configuration", /LocDisplayName|GlobalConfig|Settings\b/],
  ];
  return files.filter(file => /\.(cs|csproj|xaml|html|js|css)$/.test(file)).map(file => {
    const content = fs.readFileSync(file, "utf8");
    const kinds = patterns.filter(([, pattern]) => pattern.test(content)).map(([kind]) => kind);
    return {
      path: path.relative(root, file).replaceAll("\\", "/"),
      project: path.relative(sourceRoot, file).split(path.sep)[0],
      kinds: kinds.length ? kinds : ["support"],
      symbols: [...content.matchAll(/\b(?:class|interface|enum)\s+([A-Za-z0-9_]+)/g)].map(match => match[1]),
    };
  });
}

function extractSettings(files) {
  const results = [];
  for (const file of files.filter(file => file.endsWith(".cs"))) {
    const lines = fs.readFileSync(file, "utf8").split(/\r?\n/);
    let displayName = null;
    let description = null;
    for (let index = 0; index < lines.length; index += 1) {
      const line = lines[index];
      const display = line.match(/LocDisplayName\("([^"]*)"/);
      const desc = line.match(/LocDescription\("([^"]*)"/);
      if (display) displayName = display[1];
      if (desc) description = desc[1];
      const property = line.match(/\bpublic\s+([A-Za-z0-9_<>,.?\[\]]+)\s+([A-Za-z0-9_]+)\s*\{\s*get;/);
      if (!property) continue;
      const defaultMatch = line.match(/=\s*([^;]+);/);
      results.push({
        path: `${path.basename(file, ".cs")}.${property[2]}`,
        type: property[1],
        default: defaultMatch ? defaultMatch[1].trim() : null,
        displayName,
        description,
        constraints: /RangeInt|RangeFloat/.test(property[1]) ? "range object" : null,
        persistence: "Bannerlord-Twitch-v4-p{profile}.yaml",
        source: { file: path.relative(root, file).replaceAll("\\", "/"), line: index + 1 },
      });
      displayName = null;
      description = null;
    }
  }
  return results;
}

const allFiles = walk(sourceRoot);
const yamlLines = fs.readFileSync(yamlPath, "utf8").split(/\r?\n/);
const commandStart = yamlLines.findIndex(line => line === "Commands:") + 1;
const globalStart = yamlLines.findIndex(line => line === "GlobalConfigs:");
const rewardStart = yamlLines.findIndex(line => line === "Rewards:") + 1;
const commands = parseTopLevelSequence(yamlLines, commandStart, globalStart);
const rewards = parseTopLevelSequence(yamlLines, rewardStart, yamlLines.length);
const actions = buildActions(commands);
const components = extractComponents(allFiles);
const settings = extractSettings(allFiles);

fs.mkdirSync(outputRoot, { recursive: true });
const writeJson = (name, data) => fs.writeFileSync(path.join(outputRoot, name), `${JSON.stringify(data, null, 2)}\n`);
writeJson("commands.json", commands);
writeJson("rewards.json", rewards);
writeJson("action-manifest.json", { protocolVersion: 1, generatedAt: new Date().toISOString(), actions });
writeJson("settings.json", settings);
writeJson("components.json", components);

const categoryCounts = Object.entries(actions.reduce((acc, action) => ({ ...acc, [action.category]: (acc[action.category] ?? 0) + 1 }), {}));
const componentCounts = Object.entries(components.flatMap(item => item.kinds).reduce((acc, kind) => ({ ...acc, [kind]: (acc[kind] ?? 0) + 1 }), {}));
const markdown = `# BLT Twitch Integration Inventory\n\n` +
  `Generated from tracked source and the default v4 YAML configuration. Re-run with \`node tools/generate-inventory.mjs\`.\n\n` +
  `## Coverage\n\n| Area | Count |\n|---|---:|\n| Commands | ${commands.length} |\n| Rewards | ${rewards.length} |\n| Settings | ${settings.length} |\n| Source components | ${components.length} |\n\n` +
  `## Action categories\n\n| Category | Commands |\n|---|---:|\n${categoryCounts.map(([name, count]) => `| ${name} | ${count} |`).join("\n")}\n\n` +
  `## Component map\n\n| Kind | Files |\n|---|---:|\n${componentCounts.map(([name, count]) => `| ${name} | ${count} |`).join("\n")}\n\n` +
  `## Current data flow\n\n` +
  `Twitch chat/EventSub and channel-point redemptions are normalized into \`ReplyContext\`, resolved through \`ActionManager\`, and executed by registered handlers. Settings come from per-profile YAML and are edited by BLTConfigure. Self-hosted overlays use SignalR hubs. The experimental Extension code signs privileged JWTs in the mod and the local relay forwards raw command strings; both paths are replaced by the structured managed-service protocol.\n\n` +
  `## Machine-readable references\n\n` +
  `- \`commands.json\`: configured ordinary commands and handler settings.\n` +
  `- \`rewards.json\`: native channel-point reward definitions.\n` +
  `- \`action-manifest.json\`: initial Extension-facing action catalog.\n` +
  `- \`settings.json\`: public configurable properties and source locations.\n` +
  `- \`components.json\`: project files, symbols, and architectural roles.\n`;
fs.writeFileSync(path.join(outputRoot, "README.md"), markdown);

console.log(JSON.stringify({ commands: commands.length, rewards: rewards.length, actions: actions.length, settings: settings.length, components: components.length }, null, 2));
