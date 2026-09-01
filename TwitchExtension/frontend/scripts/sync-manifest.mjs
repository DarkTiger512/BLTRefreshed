import fs from "node:fs";
import path from "node:path";

const here = import.meta.dirname;
const source = path.resolve(here, "../../../docs/twitch-integration/inventory/action-manifest.json");
const destination = path.resolve(here, "../public/action-manifest.json");
fs.mkdirSync(path.dirname(destination), { recursive: true });
const manifest = JSON.parse(fs.readFileSync(source, "utf8"));
manifest.manifestVersion ??= 2;
for (const action of manifest.actions) {
  const key = action.id.replace(/^command\./, "");
  action.nameKey ??= `command.${key}.name`;
  action.descriptionKey ??= `command.${key}.description`;
  action.categoryKey ??= `category.${String(action.category).toLowerCase().replace(/[^a-z0-9]+/g, ".")}`;
  for (const input of action.inputs ?? []) {
    input.labelKey ??= `command.${key}.input.${input.id}.label`;
    input.descriptionKey ??= `command.${key}.input.${input.id}.description`;
    for (const option of input.options ?? []) option.labelKey ??= `command.${key}.input.${input.id}.option.${option.value}`;
  }
}
fs.writeFileSync(destination, `${JSON.stringify(manifest, null, 2)}\n`);
console.log(`Synced ${path.relative(process.cwd(), source)} -> ${path.relative(process.cwd(), destination)}`);
