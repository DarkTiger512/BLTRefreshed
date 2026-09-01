import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, expect, test, vi } from "vitest";
import { App } from "./App";

const manifest = { protocolVersion: 1, actions: [
  { id: "command.adopt", legacyName: "adopt", handler: "AdoptAHero", category: "Hero", description: "Adopt a hero", enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["game.started"], mutatesCampaign: true, inputs: [] },
  { id: "command.adoptbyculture", legacyName: "adoptByCulture", handler: "AdoptAHero", category: "Hero", description: "Adopt by culture", enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["game.started"], mutatesCampaign: true, inputs: [{ id: "culture", label: "Culture", type: "choice", required: true, options: [], optionsSource: "cultures" }] },
  { id: "command.equipcustom", legacyName: "equipcustom", handler: "EquipCustomItemAction", category: "Equipment", description: "Equip custom item", enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["game.started"], mutatesCampaign: true, inputs: [{ id: "item", type: "text", required: true }] },
  { id: "command.retinue", legacyName: "retinue", handler: "Retinue", category: "Retinue", description: "Manage battle retinue", enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["game.started"], mutatesCampaign: true, inputs: [{ id: "operation", type: "choice", required: true }] },
  { id: "command.eliteretinue", legacyName: "eliteretinue", handler: "Retinue2", category: "Retinue", description: "Manage elite retinue", enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["game.started"], mutatesCampaign: true, inputs: [{ id: "operation", type: "choice", required: true }] },
  { id: "command.summon", legacyName: "summon", handler: "SummonHero", category: "Battle", description: "Summon your hero on the streamer's side", enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["mission.active"], mutatesCampaign: true, inputs: [{ id: "shout", label: "Optional battle shout", type: "text", required: false }] },
  { id: "command.attack", legacyName: "attack", handler: "SummonHero", category: "Battle", description: "Summon your hero on the enemy side", enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["mission.active"], mutatesCampaign: true, inputs: [{ id: "shout", label: "Optional battle shout", type: "text", required: false }] },
  { id: "command.heal", legacyName: "heal", handler: "HealHero", category: "Battle", description: "Heal your hero", enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["mission.active"], mutatesCampaign: true, inputs: [] },
  { id: "command.power", legacyName: "power", handler: "UsePower", category: "Battle", description: "Activate your hero's power", enabledByDefault: true, hiddenFromHelp: true, permissions: ["viewer"], availability: ["mission.active"], mutatesCampaign: true, inputs: [] },
  { id: "command.formation", legacyName: "formation", handler: "FormationCommand", category: "Battle", description: "Choose your hero's formation", enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["mission.active"], mutatesCampaign: true, inputs: [{ id: "formation", label: "Formation", type: "choice", required: true, options: [{ value: "infantry", label: "Infantry" }, { value: "ranged", label: "Ranged" }] }] },
  { id: "command.battle", legacyName: "battle", handler: "BattleInfo", category: "Battle", description: "Battle info", enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["mission.active"], mutatesCampaign: false, inputs: [] },
] };

vi.stubGlobal("fetch", vi.fn(() => Promise.resolve({ json: () => Promise.resolve(manifest) })));
afterEach(cleanup);

test("renders the minimal command workspace, autocomplete, help, and native views", async () => {
  window.history.pushState({}, "", "/?mission=inactive");
  render(<App />);
  await waitFor(() => expect(screen.getByText("Bannerlord Twitch")).toBeInTheDocument());
  expect(screen.getByText("50.000")).toBeInTheDocument();
  const commandLine = screen.getByRole("textbox", { name: "Command line" });
  fireEvent.change(commandLine, { target: { value: "ado" } });
  expect(screen.getByRole("option", { name: /^!adoptAdopt a hero$/i })).toBeInTheDocument();
  fireEvent.keyDown(commandLine, { key: "Tab" });
  expect(commandLine).toHaveValue("adopt");
  fireEvent.click(screen.getByRole("button", { name: "Open command help" }));
  expect(screen.getByRole("dialog", { name: "Command help" })).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: /!adoptbyculture/i }));
  expect(commandLine).toHaveValue("adoptByCulture ");
  fireEvent.click(screen.getByRole("button", { name: /^inventorycustom items/i }));
  await waitFor(() => expect(screen.getByText("Wolf's Oath Longsword — +12 Damage, +8 Swing Speed")).toBeInTheDocument());
  expect(screen.getByText(/visible only to you/i)).toBeInTheDocument();
  expect(screen.getByText("Equipped slots")).toBeInTheDocument();
  expect(screen.queryByText("EquipCustomItemAction")).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: /ashen tournament bow/i }));
  await waitFor(() => expect(screen.getByRole("button", { name: /ashen tournament bow/i })).toHaveAttribute("aria-pressed", "true"));
  fireEvent.click(screen.getByRole("button", { name: /weapon 2/i }));
  await waitFor(() => expect(screen.getByRole("button", { name: /weapon 2/i })).toHaveTextContent("Ashen Tournament Bow"));
  const horse = screen.getByRole("button", { name: /stormhoof/i });
  fireEvent.dragStart(horse);
  fireEvent.drop(screen.getByRole("button", { name: /^mount/i }));
  await waitFor(() => expect(screen.getByRole("button", { name: /^mount/i })).toHaveTextContent("Stormhoof"));
  fireEvent.click(screen.getByRole("button", { name: "Back to command bar" }));
  fireEvent.click(screen.getByRole("button", { name: /^retinuelive troops/i }));
  await waitFor(() => expect(screen.getByText("Battle Retinue")).toBeInTheDocument());
  expect(screen.getByText("Elite Retinue")).toBeInTheDocument();
  expect(screen.getByText("Vlandian Banner Knight")).toBeInTheDocument();
  fireEvent.change(screen.getByRole("spinbutton", { name: /battle retinue recruit or upgrade quantity/i }), { target: { value: "3" } });
  fireEvent.click(screen.getAllByRole("button", { name: /send order/i })[0]);
  await waitFor(() => expect(screen.getByText("retinue completed successfully.")).toBeInTheDocument());
  expect(screen.queryByRole("button", { name: /my feed/i })).not.toBeInTheDocument();
  const feedToggle = screen.getByRole("button", { name: /command feed/i });
  expect(feedToggle).toHaveAttribute("aria-expanded", "false");
  fireEvent.click(feedToggle);
  await waitFor(() => expect(screen.getByRole("heading", { name: "Your Command Results" })).toBeInTheDocument());
  expect(screen.getAllByText("Succeeded").length).toBeGreaterThan(0);
  expect(screen.getByRole("button", { name: /clear feed/i })).toBeInTheDocument();
});

test("renders the minimal battle HUD with one-click commands and formation popover", async () => {
  window.history.pushState({}, "", "/");
  render(<App />);
  await waitFor(() => expect(screen.getByRole("region", { name: "Live battle" })).toBeInTheDocument());
  expect(screen.getByRole("region", { name: "Your hero" })).toBeInTheDocument();
  expect(screen.getByText("86 / 112 HP")).toBeInTheDocument();
  expect(screen.getByText("Shieldmaiden")).toBeInTheDocument();
  expect(screen.getByText("BlackWolf")).toBeInTheDocument();
  expect(screen.getByText("War Cry")).toBeInTheDocument();
  expect(screen.getByText("42%")).toBeInTheDocument();
  expect(screen.getByLabelText("Shieldmaiden health 54 of 100")).toHaveTextContent("54 / 100");
  expect(screen.queryByPlaceholderText("Search mission commands")).not.toBeInTheDocument();
  expect(screen.queryByRole("button", { name: /my inventory/i })).not.toBeInTheDocument();
  expect(screen.queryByText("Battle info")).not.toBeInTheDocument();

  const heal = screen.getByRole("button", { name: "Heal" });
  fireEvent.focus(heal);
  expect(screen.getByRole("tooltip")).toHaveTextContent(/Heal your hero/i);
  fireEvent.click(heal);
  await waitFor(() => expect(screen.getByText("heal completed successfully.")).toBeInTheDocument());

  fireEvent.click(screen.getByRole("button", { name: "Formation" }));
  expect(screen.getByRole("dialog", { name: "Choose formation" })).toBeInTheDocument();
  fireEvent.keyDown(document, { key: "Escape" });
  expect(screen.queryByRole("dialog", { name: "Choose formation" })).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Formation" }));
  fireEvent.pointerDown(document.body);
  expect(screen.queryByRole("dialog", { name: "Choose formation" })).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Formation" }));
  fireEvent.click(screen.getByRole("button", { name: "Infantry" }));
  await waitFor(() => expect(screen.queryByRole("dialog", { name: "Choose formation" })).not.toBeInTheDocument());
});
