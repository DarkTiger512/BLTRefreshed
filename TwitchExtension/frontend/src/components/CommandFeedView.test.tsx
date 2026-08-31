import { fireEvent, render, screen } from "@testing-library/react";
import { expect, test, vi } from "vitest";
import { CommandFeedView } from "./CommandFeedView";
import type { CommandActivity } from "../types";

const entries: CommandActivity[] = [
  { requestId: "1", actionId: "command.retinue", actionName: "retinue", status: "pending", submittedAt: "2026-08-31T18:00:00Z", messages: [] },
  { requestId: "2", actionId: "command.adopt", actionName: "adopt", status: "succeeded", submittedAt: "2026-08-31T17:59:00Z", completedAt: "2026-08-31T17:59:01Z", messages: ["Hero adopted."] },
  { requestId: "3", actionId: "command.summon", actionName: "summon", status: "failed", submittedAt: "2026-08-31T17:58:00Z", completedAt: "2026-08-31T17:58:01Z", messages: ["No mission is active."] },
  { requestId: "4", actionId: "command.gold", actionName: "gold", status: "succeeded", submittedAt: "2026-08-31T17:57:00Z", messages: ["25,000 gold."] },
];

test("shows three compact results and exposes full private history", () => {
  const toggle = vi.fn();
  const clear = vi.fn();
  const { container, rerender } = render(<CommandFeedView entries={entries} expanded={false} onToggle={toggle} onClear={clear} />);
  expect(container.querySelectorAll(".command-feed-entry.compact")).toHaveLength(3);
  expect(screen.getByText("Hero adopted.")).toBeInTheDocument();
  expect(screen.getByText("No mission is active.")).toBeInTheDocument();
  expect(screen.queryByText("25,000 gold.")).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: /command feed/i }));
  expect(toggle).toHaveBeenCalledOnce();

  rerender(<CommandFeedView entries={entries} expanded onToggle={toggle} onClear={clear} />);
  expect(screen.getByText("25,000 gold.")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: /clear feed/i }));
  expect(clear).toHaveBeenCalledOnce();
});
