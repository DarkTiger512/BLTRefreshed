import { useEffect, useState } from "react";
import type { GameState, InventorySnapshot, ViewerIdentity } from "../types";

const initialState: GameState = { connected: true, gameStarted: true, unavailable: {}, cooldowns: {} };

export function useIntegrationState(identity: ViewerIdentity | null) {
  const [state, setState] = useState<GameState>(initialState);
  const [inventory, setInventory] = useState<InventorySnapshot>();
  const [inventoryError, setInventoryError] = useState<string>();
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
      }
    });
    return () => socket.close();
  }, [identity]);
  return { ...state, inventory, inventoryError, setInventory, setInventoryError };
}
