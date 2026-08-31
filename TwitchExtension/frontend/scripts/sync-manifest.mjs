import fs from "node:fs";
import path from "node:path";

const here = import.meta.dirname;
const source = path.resolve(here, "../../../docs/twitch-integration/inventory/action-manifest.json");
const destination = path.resolve(here, "../public/action-manifest.json");
fs.mkdirSync(path.dirname(destination), { recursive: true });
fs.copyFileSync(source, destination);
console.log(`Synced ${path.relative(process.cwd(), source)} -> ${path.relative(process.cwd(), destination)}`);
