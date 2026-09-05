import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, expect, test, vi } from "vitest";
import { PrestigeView } from "./PrestigeView";
import type { CommandActivity, ViewerState } from "../types";

afterEach(() => { cleanup(); vi.useRealTimers(); });
const viewer: ViewerState = { adopted: true, heroName: "Viewer [P1]", gold: 7500000, prestige: {
  count: 1, maximum: 50, runKills: 750, requiredKills: 750, requiredGold: 7500000, eligible: true,
  resetSummary: "All gold, equipment and progression will reset.",
  perks: [{ id: "might", name: "Might", description: "+2% damage per rank", rank: 1, cap: 10 },
    { id: "fortune", name: "Fortune", description: "+2% battle gold per rank", rank: 10, cap: 10 }] } };
const result = (status: CommandActivity["status"]): CommandActivity => ({ requestId: "preview", actionId: "command.prestige", actionName: "prestige",
  submittedAt: new Date().toISOString(), status, messages: status === "failed" ? ["Preview rejected by game"] : ["Preview accepted"] });

test("requires game acknowledgement, prevents capped choices and submits exact shared commands", async () => {
  const onCommand = vi.fn().mockResolvedValueOnce("preview").mockResolvedValueOnce("confirm");
  const props = { viewer, connected: true, linked: true, busy: false, activity: [] as CommandActivity[], onBack: vi.fn(), onCommand };
  const { rerender } = render(<PrestigeView {...props} />);
  expect(screen.getByRole("button", { name: /Fortune/ })).toBeDisabled();
  fireEvent.click(screen.getByRole("button", { name: /Might/ }));
  await waitFor(() => expect(onCommand).toHaveBeenCalledWith("prestige choose might"));
  await screen.findByRole("button", { name: "Reset character and gain perk" });
  expect(screen.getByRole("button", { name: "Reset character and gain perk" })).toBeDisabled();
  rerender(<PrestigeView {...props} activity={[result("succeeded")]} />);
  fireEvent.click(screen.getByRole("button", { name: "Reset character and gain perk" }));
  await waitFor(() => expect(onCommand).toHaveBeenCalledWith("prestige confirm might"));
  expect(screen.getByRole("button", { name: "Reset character and gain perk" })).toBeDisabled();
});

test("offline, missing snapshots and rejected previews cannot reset", async () => {
  const onCommand = vi.fn().mockResolvedValue("preview");
  const props = { viewer, connected: false, linked: true, busy: false, activity: [] as CommandActivity[], onBack: vi.fn(), onCommand };
  const { rerender } = render(<PrestigeView {...props} />);
  expect(screen.getByRole("button", { name: /Might/ })).toBeDisabled();
  rerender(<PrestigeView {...props} connected viewer={{ adopted: true }} />);
  expect(screen.getByText(/Prestige information is unavailable/)).toBeVisible();
  expect(screen.queryByRole("button", { name: /Reset character/ })).not.toBeInTheDocument();
  rerender(<PrestigeView {...props} connected />);
  fireEvent.click(screen.getByRole("button", { name: /Might/ }));
  await screen.findByRole("button", { name: "Reset character and gain perk" });
  rerender(<PrestigeView {...props} connected activity={[result("failed")]} />);
  expect(screen.getByText("Preview rejected by game")).toBeVisible();
  expect(screen.getByRole("button", { name: "Reset character and gain perk" })).toBeDisabled();
});

test("expired previews and changed eligibility disable confirmation", async () => {
  const onCommand = vi.fn().mockResolvedValue("preview");
  const props = { viewer, connected: true, linked: true, busy: false, activity: [] as CommandActivity[], onBack: vi.fn(), onCommand };
  const { rerender } = render(<PrestigeView {...props} />);
  fireEvent.click(screen.getByRole("button", { name: /Might/ }));
  await screen.findByRole("button", { name: "Reset character and gain perk" });
  rerender(<PrestigeView {...props} activity={[result("succeeded")]} viewer={{ ...viewer, prestige: { ...viewer.prestige!, eligible: false } }} />);
  expect(screen.getByRole("button", { name: "Reset character and gain perk" })).toBeDisabled();
  const date = vi.spyOn(Date, "now").mockReturnValue(Date.now() + 61000);
  rerender(<PrestigeView {...props} activity={[result("succeeded")]} />);
  await waitFor(() => expect(screen.getByText(/Preview expired/)).toBeVisible());
  expect(screen.getByRole("button", { name: "Reset character and gain perk" })).toBeDisabled();
  date.mockRestore();
});
