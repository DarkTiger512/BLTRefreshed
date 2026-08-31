import { render, screen, waitFor } from "@testing-library/react";
import { expect, test, vi } from "vitest";
import { App } from "./App";

const manifest = { protocolVersion: 1, actions: [{ id: "command.adopt", legacyName: "adopt", handler: "AdoptAHero", category: "Hero", description: "Adopt a hero", enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["game.started"], mutatesCampaign: true, inputs: [] }] };

vi.stubGlobal("fetch", vi.fn(() => Promise.resolve({ json: () => Promise.resolve(manifest) })));

test("renders the command browser and selected action", async () => {
  render(<App />);
  await waitFor(() => expect(screen.getByText("Bannerlord Twitch")).toBeInTheDocument());
  expect(screen.getAllByText("adopt").length).toBeGreaterThan(0);
  expect(screen.getByPlaceholderText("Search actions")).toBeInTheDocument();
  expect(screen.getByRole("button", { name: /confirm action/i })).toBeInTheDocument();
});
