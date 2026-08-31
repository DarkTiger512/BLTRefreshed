import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { expect, test, vi } from "vitest";
import { App } from "./App";

const manifest = { protocolVersion: 1, actions: [
  { id: "command.adopt", legacyName: "adopt", handler: "AdoptAHero", category: "Hero", description: "Adopt a hero", enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["game.started"], mutatesCampaign: true, inputs: [] },
  { id: "command.equipcustom", legacyName: "equipcustom", handler: "EquipCustomItemAction", category: "Equipment", description: "Equip custom item", enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["game.started"], mutatesCampaign: true, inputs: [{ id: "item", type: "text", required: true }] },
] };

vi.stubGlobal("fetch", vi.fn(() => Promise.resolve({ json: () => Promise.resolve(manifest) })));

test("renders the command browser and selected action", async () => {
  render(<App />);
  await waitFor(() => expect(screen.getByText("Bannerlord Twitch")).toBeInTheDocument());
  expect(screen.getAllByText("adopt").length).toBeGreaterThan(0);
  expect(screen.getByPlaceholderText("Search actions")).toBeInTheDocument();
  expect(screen.getByRole("button", { name: /confirm action/i })).toBeInTheDocument();
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
});
