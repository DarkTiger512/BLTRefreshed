import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { expect, test, vi } from "vitest";
import { App } from "./App";

const manifest = { protocolVersion: 1, actions: [
  { id: "command.adopt", legacyName: "adopt", handler: "AdoptAHero", category: "Hero", description: "Adopt a hero", enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["game.started"], mutatesCampaign: true, inputs: [] },
  { id: "command.adoptbyculture", legacyName: "adoptByCulture", handler: "AdoptAHero", category: "Hero", description: "Adopt by culture", enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["game.started"], mutatesCampaign: true, inputs: [{ id: "culture", label: "Culture", type: "choice", required: true, options: [{ value: "Aserai", label: "Aserai" }, { value: "Battania", label: "Battania" }, { value: "Empire", label: "Empire" }, { value: "Khuzait", label: "Khuzait" }, { value: "Sturgia", label: "Sturgia" }, { value: "Vlandia", label: "Vlandia" }] }] },
  { id: "command.equipcustom", legacyName: "equipcustom", handler: "EquipCustomItemAction", category: "Equipment", description: "Equip custom item", enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["game.started"], mutatesCampaign: true, inputs: [{ id: "item", type: "text", required: true }] },
  { id: "command.retinue", legacyName: "retinue", handler: "Retinue", category: "Retinue", description: "Manage battle retinue", enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["game.started"], mutatesCampaign: true, inputs: [{ id: "operation", type: "choice", required: true }] },
  { id: "command.eliteretinue", legacyName: "eliteretinue", handler: "Retinue2", category: "Retinue", description: "Manage elite retinue", enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["game.started"], mutatesCampaign: true, inputs: [{ id: "operation", type: "choice", required: true }] },
] };

vi.stubGlobal("fetch", vi.fn(() => Promise.resolve({ json: () => Promise.resolve(manifest) })));

test("renders the command browser and selected action", async () => {
  render(<App />);
  await waitFor(() => expect(screen.getByText("Bannerlord Twitch")).toBeInTheDocument());
  expect(screen.getAllByText("adopt").length).toBeGreaterThan(0);
  expect(screen.getByPlaceholderText("Search actions")).toBeInTheDocument();
  expect(screen.getByRole("button", { name: /confirm action/i })).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: /adoptbycultureadopt by culture/i }));
  expect(screen.getByRole("combobox", { name: /^Culture/ })).toBeInTheDocument();
  expect(screen.getByRole("option", { name: "Vlandia" })).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: /my inventory/i }));
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
  fireEvent.click(screen.getByRole("button", { name: /^retinue$/i }));
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
