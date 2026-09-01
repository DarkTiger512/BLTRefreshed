import type { ManifestAction } from "./types";
export type ConfigurationPreset = "defaults" | "viewers" | "safe" | "battle" | "off";
export function applyConfigurationPreset(actions: ManifestAction[], preset: ConfigurationPreset) {
  return actions.map(action => ({ actionId: action.id, enabled: preset === "defaults" ? action.enabledByDefault
    : preset === "viewers" ? action.permissions.includes("viewer")
    : preset === "safe" ? !action.mutatesCampaign
    : preset === "battle" ? ["Battle", "Tournament"].includes(action.category)
    : false }));
}
