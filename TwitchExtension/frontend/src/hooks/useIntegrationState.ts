import { useEffect, useState } from "react";
import type { CommandActivity, GameState, InventorySnapshot, RetinueSnapshot, ViewerIdentity } from "../types";

const initialState: GameState = { connected: true, gameStarted: true, unavailable: {}, cooldowns: {} };

export function useIntegrationState(identity: ViewerIdentity | null) {
  const [state, setState] = useState<GameState>(initialState);
  const [inventory, setInventory] = useState<InventorySnapshot>();
  const [inventoryError, setInventoryError] = useState<string>();
  const [retinue, setRetinue] = useState<RetinueSnapshot>();
  const [retinueError, setRetinueError] = useState<string>();
  const [commandActivity, setCommandActivity] = useState<CommandActivity[]>([]);
  useEffect(() => {
    if (!identity || identity.token === "development-token") return;
    const apiBase = import.meta.env.VITE_BLT_API_URL ?? window.location.origin;
    const url = new URL(`/ws/viewer/${encodeURIComponent(identity.channelId)}`, apiBase);
    url.protocol = url.protocol === "https:" ? "wss:" : "ws:";
    url.searchParams.set("token", identity.token);
    const socket = new WebSocket(url);
    socket.addEventListener("open", () => setState(value => ({ ...value, connected: true })));
    socket.addEventListener("close", () => setState(value => ({ ...value, connected: false })));
    socket.addEventListener("message", event => {
      const envelope = JSON.parse(String(event.data));
      if (envelope.kind === "state.snapshot" || envelope.kind === "state.patch") {
        setState(value => ({ ...value, ...envelope.data }));
      } else if (envelope.kind === "inventory.snapshot") {
        setInventory({ ...envelope.data, updatedAt: envelope.timestamp });
        setInventoryError(undefined);
      } else if (envelope.kind === "inventory.error") {
        setInventoryError(envelope.data.error);
      } else if (envelope.kind === "retinue.snapshot") {
        setRetinue({ ...envelope.data, updatedAt: envelope.timestamp });
        setRetinueError(undefined);
      } else if (envelope.kind === "retinue.error") {
        setRetinueError(envelope.data.error);
      } else if (envelope.kind === "action.result" || envelope.kind === "action.error") {
        const requestId = String(envelope.data.requestId ?? envelope.id);
        const succeeded = envelope.kind === "action.result";
        const messages = succeeded ? (envelope.data.messages ?? []) : [envelope.data.error ?? "The command failed."];
        setCommandActivity(entries => entries.map(entry => entry.requestId === requestId ? { ...entry, status: succeeded ? "succeeded" : "failed", messages, completedAt: envelope.timestamp } : entry));
      }
    });
    return () => socket.close();
  }, [identity]);
  function recordCommand(entry: Omit<CommandActivity, "submittedAt" | "messages">) {
    setCommandActivity(entries => [{ ...entry, submittedAt: new Date().toISOString(), messages: [] }, ...entries.filter(item => item.requestId !== entry.requestId)].slice(0, 100));
  }
  function completeDevelopmentCommand(requestId: string, message: string) {
    setCommandActivity(entries => entries.map(entry => entry.requestId === requestId ? { ...entry, status: "succeeded", messages: [message], completedAt: new Date().toISOString() } : entry));
  }
  return { ...state, inventory, inventoryError, retinue, retinueError, commandActivity, setInventory, setInventoryError, setRetinue, setRetinueError, recordCommand, completeDevelopmentCommand, clearCommandActivity: () => setCommandActivity([]) };
}
