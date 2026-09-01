import { describe, expect, it } from "vitest";
import { applyConfigurationPreset } from "./configuration";
import type { ManifestAction } from "./types";
const action = (id: string, category: string, mutatesCampaign: boolean, enabledByDefault = true): ManifestAction => ({ id, category, mutatesCampaign, enabledByDefault, legacyName:id, handler:"test", description:"test", hiddenFromHelp:false, permissions:["viewer"], availability:[], inputs:[] });
describe("configuration presets", () => {
  const actions = [action("command.heal","Battle",true), action("command.stats","Hero",false), action("command.retire","Hero",true,false)];
  it("disables campaign mutations in safe mode", () => expect(applyConfigurationPreset(actions,"safe").map(x => x.enabled)).toEqual([false,true,false]));
  it("limits battle mode to battle and tournament categories", () => expect(applyConfigurationPreset(actions,"battle").map(x => x.enabled)).toEqual([true,false,false]));
  it("restores bundled defaults", () => expect(applyConfigurationPreset(actions,"defaults").map(x => x.enabled)).toEqual([true,true,false]));
});
