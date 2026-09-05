import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, expect, test, vi } from "vitest";
import { BattleCommandStrip } from "./BattleCommandStrip";
import type { MissionState, ViewerIdentity } from "../types";

afterEach(cleanup);
const mission: MissionState = { active: true, kind: "battle", revision: 1, deploymentFinished: true, combatants: [], actionAvailability: {},
  battleBalance: { missionId: "battle-one", summoners: 8, attackers: 2, summonOffer: 0, attackOffer: .12 } };
const props = { actions: [], identity: { linked: true } as ViewerIdentity, mission, connected: true, cooldowns: {}, busy: false,
  onRequestIdentity: vi.fn(), onSubmit: vi.fn() };
test("distinguishes current participation offers from the viewer's locked reward", () => {
  render(<BattleCommandStrip {...props} viewer={{ adopted: true, battleBalance: { missionId: "battle-one", lockedBonus: .05 } }} />);
  expect(screen.getByText("Summon 8 / Attack 2")).toBeInTheDocument();
  expect(screen.getByText("Your locked bonus: +5% gold & XP")).toBeInTheDocument();
  expect(screen.getByText(/Offers are estimates/)).toBeInTheDocument();
});
test("hides old-mission locks, missing snapshots and disconnected offers", () => {
  const { rerender } = render(<BattleCommandStrip {...props} viewer={{ adopted: true, battleBalance: { missionId: "previous", lockedBonus: .2 } }} />);
  expect(screen.queryByText(/Your locked bonus/)).not.toBeInTheDocument();
  rerender(<BattleCommandStrip {...props} connected={false} />);
  expect(screen.queryByText(/Summon 8/)).not.toBeInTheDocument();
  rerender(<BattleCommandStrip {...props} mission={{ ...mission, battleBalance: undefined }} />);
  expect(screen.queryByText(/Offers are estimates/)).not.toBeInTheDocument();
});
