import { useEffect, useState } from "react";
import type { CommandActivity, GameState, InventorySnapshot, RetinueSnapshot, ViewerIdentity } from "../types";

const initialState: GameState = {
  connected: true, gameStarted: true, unavailable: {}, cooldowns: {}, selectors: { cultures: ["Vlandia", "Calradic Empire", "Realm of Thrones"] },
  mission: {
    active: true, kind: "battle", revision: 12, deploymentFinished: true,
    actionAvailability: { "command.summon": null, "command.attack": null, "command.heal": null, "command.power": null, "command.formation": null },
    combatants: [
      { id: "rowan", name: "Rowan", hp: 86, maxHp: 112, state: "active", isPlayerSide: true, tournamentTeam: -1, cooldownFractionRemaining: .18, cooldownSecondsRemaining: 9, activePowerFractionRemaining: .42, kills: 6, retinue: 4, deadRetinue: 1, eliteRetinue: 2, deadEliteRetinue: 0, retinueKills: 11, goldEarned: 1840, xpEarned: 920, ammoCurrent: 18, ammoMaximum: 32 },
      { id: "shieldmaiden", name: "Shieldmaiden", hp: 54, maxHp: 100, state: "active", isPlayerSide: true, tournamentTeam: -1, cooldownFractionRemaining: 0, cooldownSecondsRemaining: 0, activePowerFractionRemaining: 0, kills: 3, retinue: 2, deadRetinue: 2, eliteRetinue: 0, deadEliteRetinue: 0, retinueKills: 4, goldEarned: 760, xpEarned: 380, ammoCurrent: 0, ammoMaximum: 0 },
      { id: "blackwolf", name: "BlackWolf", hp: 31, maxHp: 105, state: "active", isPlayerSide: false, tournamentTeam: -1, cooldownFractionRemaining: .6, cooldownSecondsRemaining: 28, activePowerFractionRemaining: 0, kills: 4, retinue: 1, deadRetinue: 4, eliteRetinue: 1, deadEliteRetinue: 1, retinueKills: 7, goldEarned: 1030, xpEarned: 515, ammoCurrent: 6, ammoMaximum: 24 },
      { id: "ironstag", name: "IronStag", hp: 72, maxHp: 98, state: "active", isPlayerSide: true, tournamentTeam: -1, cooldownFractionRemaining: 0, cooldownSecondsRemaining: 0, activePowerFractionRemaining: .2, kills: 2, retinue: 3, deadRetinue: 0, eliteRetinue: 0, deadEliteRetinue: 0, retinueKills: 3, goldEarned: 510, xpEarned: 260, ammoCurrent: 12, ammoMaximum: 20 },
      { id: "ravenna", name: "Ravenna", hp: 44, maxHp: 91, state: "active", isPlayerSide: true, tournamentTeam: -1, cooldownFractionRemaining: 0, cooldownSecondsRemaining: 0, activePowerFractionRemaining: 0, kills: 1, retinue: 1, deadRetinue: 1, eliteRetinue: 0, deadEliteRetinue: 0, retinueKills: 2, goldEarned: 290, xpEarned: 145, ammoCurrent: 0, ammoMaximum: 0 },
      { id: "oakheart", name: "Oakheart", hp: 80, maxHp: 104, state: "active", isPlayerSide: true, tournamentTeam: -1, cooldownFractionRemaining: 0, cooldownSecondsRemaining: 0, activePowerFractionRemaining: 0, kills: 3, retinue: 2, deadRetinue: 0, eliteRetinue: 1, deadEliteRetinue: 0, retinueKills: 5, goldEarned: 870, xpEarned: 435, ammoCurrent: 8, ammoMaximum: 18 },
      { id: "grimhollow", name: "GrimHollow", hp: 67, maxHp: 110, state: "active", isPlayerSide: false, tournamentTeam: -1, cooldownFractionRemaining: 0, cooldownSecondsRemaining: 0, activePowerFractionRemaining: .65, kills: 5, retinue: 4, deadRetinue: 2, eliteRetinue: 0, deadEliteRetinue: 0, retinueKills: 6, goldEarned: 1320, xpEarned: 660, ammoCurrent: 0, ammoMaximum: 0 },
      { id: "silverwolf", name: "SilverWolf", hp: 0, maxHp: 103, state: "unconscious", isPlayerSide: false, tournamentTeam: -1, cooldownFractionRemaining: 0, cooldownSecondsRemaining: 0, activePowerFractionRemaining: 0, kills: 2, retinue: 0, deadRetinue: 3, eliteRetinue: 0, deadEliteRetinue: 1, retinueKills: 4, goldEarned: 640, xpEarned: 320, ammoCurrent: 0, ammoMaximum: 0 },
      { id: "viper", name: "Viper", hp: 59, maxHp: 94, state: "active", isPlayerSide: false, tournamentTeam: -1, cooldownFractionRemaining: .15, cooldownSecondsRemaining: 7, activePowerFractionRemaining: 0, kills: 3, retinue: 2, deadRetinue: 1, eliteRetinue: 0, deadEliteRetinue: 0, retinueKills: 3, goldEarned: 720, xpEarned: 360, ammoCurrent: 14, ammoMaximum: 26 },
    ],
  },
};

export function useIntegrationState(identity: ViewerIdentity | null) {
  const [state, setState] = useState<GameState>(() => new URLSearchParams(window.location.search).get("mission") === "inactive"
    ? { ...initialState, mission: { active: false, kind: "inactive", revision: 0, deploymentFinished: false, combatants: [], actionAvailability: {} } }
    : initialState);
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
      if (envelope.kind === "connection.status") {
        setState(value => ({ ...value, ...envelope.data, mission: envelope.data.connected ? value.mission : { active: false, kind: "inactive", revision: value.mission.revision + 1, deploymentFinished: false, combatants: [], actionAvailability: {} } }));
      } else if (envelope.kind === "state.snapshot" || envelope.kind === "state.patch") {
        setState(value => {
          const nextMission = envelope.data.mission;
          if (nextMission && nextMission.revision < value.mission.revision) return value;
          return { ...value, ...envelope.data, mission: nextMission ? { ...value.mission, ...nextMission } : value.mission };
        });
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
