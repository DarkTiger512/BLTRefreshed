import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, expect, test, vi } from "vitest";
import type { ManifestAction } from "../types";
import { ActionDetail } from "./ActionDetail";

afterEach(cleanup);

const selectors = { cultures: [], heroes: [], clans: [], kingdoms: [], settlements: [], skills: [] };
const renderAction = (action: ManifestAction, onSubmit = vi.fn()) => {
  render(<ActionDetail action={action} linked unavailableReason={undefined} busy={false} selectors={selectors} onRequestIdentity={() => {}} onSubmit={onSubmit} />);
  return onSubmit;
};

test("required retire checkbox submits structured consent for the mod-side localized yes token", () => {
  const onSubmit = renderAction({
    id: "command.retire", legacyName: "retire", handler: "RetireMyHero", category: "Hero", description: "Retire hero",
    enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["game.started"], mutatesCampaign: true,
    inputs: [{ id: "confirm", label: "I understand this changes the campaign", type: "confirmation", required: true, confirmationPolicy: "legacy-token", legacyToken: "retire-yes" }],
  });
  fireEvent.click(screen.getByRole("checkbox"));
  fireEvent.click(screen.getByRole("button", { name: /confirm action/i }));
  expect(onSubmit).toHaveBeenCalledWith({ confirm: true });
});

test("numeric parameters remain numbers and dependent fields only appear for matching operations", () => {
  const onSubmit = renderAction({
    id: "command.auction", legacyName: "auction", handler: "AuctionItem", category: "Equipment", description: "Auction item",
    enabledByDefault: true, hiddenFromHelp: false, permissions: ["viewer"], availability: ["game.started"], mutatesCampaign: true,
    inputs: [{ id: "item", label: "Custom item number", type: "integer", required: true }, { id: "reserve", label: "Reserve price", type: "integer", required: true }],
  });
  const fields = screen.getAllByRole("spinbutton");
  fireEvent.change(fields[0], { target: { value: "4" } });
  fireEvent.change(fields[1], { target: { value: "250" } });
  fireEvent.click(screen.getByRole("button", { name: /confirm action/i }));
  expect(onSubmit).toHaveBeenCalledWith({ item: 4, reserve: 250 });
});
