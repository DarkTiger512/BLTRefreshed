import type { ManifestAction, ViewerIdentity } from "./types";
import { isLiveLocalIntegration } from "./environment";

const apiBase = import.meta.env.VITE_BLT_API_URL ?? "http://127.0.0.1:5188";

export async function createPairingCode(identity: ViewerIdentity) {
  if (identity.token === "development-token" && !isLiveLocalIntegration()) {
    return { code: "BLT-DEMO-PAIR", expiresAt: new Date(Date.now() + 600_000).toISOString() };
  }
  const response = await fetch(`${apiBase}/api/channels/${encodeURIComponent(identity.channelId)}/pairing`, {
    method: "POST",
    headers: { Authorization: `Bearer ${identity.token}` },
  });
  if (!response.ok) throw new Error((await response.json().catch(() => null))?.detail ?? "A pairing code could not be created.");
  return response.json() as Promise<{ code: string; expiresAt: string }>;
}

export async function submitAction(identity: ViewerIdentity, action: ManifestAction, args: Record<string, unknown>, requestId: string = crypto.randomUUID()) {
  if (identity.token === "development-token" && !isLiveLocalIntegration()) {
    await new Promise(resolve => setTimeout(resolve, 350));
    return { requestId, status: "accepted" };
  }
  const response = await fetch(`${apiBase}/api/channels/${encodeURIComponent(identity.channelId)}/actions`, {
    method: "POST",
    headers: { Authorization: `Bearer ${identity.token}`, "Content-Type": "application/json" },
    body: JSON.stringify({ requestId, actionId: action.id, args, timestamp: new Date().toISOString() }),
  });
  if (!response.ok) throw new Error((await response.json().catch(() => null))?.detail ?? "The action could not be sent.");
  return response.json();
}

export async function submitCommand(identity: ViewerIdentity, commandLine: string, requestId: string = crypto.randomUUID()) {
  if (identity.token === "development-token" && !isLiveLocalIntegration()) {
    await new Promise(resolve => setTimeout(resolve, 260));
    return { requestId, status: "accepted" };
  }
  const response = await fetch(`${apiBase}/api/channels/${encodeURIComponent(identity.channelId)}/commands`, {
    method: "POST",
    headers: { Authorization: `Bearer ${identity.token}`, "Content-Type": "application/json" },
    body: JSON.stringify({ requestId, commandLine, timestamp: new Date().toISOString() }),
  });
  if (!response.ok) throw new Error((await response.json().catch(() => null))?.detail ?? "The command could not be sent.");
  return response.json();
}

export async function requestInventory(identity: ViewerIdentity) {
  if (identity.token === "development-token" && !isLiveLocalIntegration()) {
    await new Promise(resolve => setTimeout(resolve, 250));
    return { heroName: "Aldric the Bold", limit: 8, updatedAt: new Date().toISOString(), items: [
      { index: 1, name: "Wolf's Oath Longsword — +12 Damage, +8 Swing Speed", type: "One Handed Weapon", equipped: true },
      { index: 2, name: "Gilded Vlandian War Helm — +9 Head Armor", type: "Head Armor", equipped: true },
      { index: 3, name: "Ashen Tournament Bow — +10 Damage, +6 Speed", type: "Bow", equipped: false },
      { index: 4, name: "Banner of the Iron Stag — +14 Hit Points", type: "Shield", equipped: false },
      { index: 5, name: "Stormhoof — +8 Mount Speed, +12 Charge", type: "Horse", equipped: false },
    ], slots: [
      { id: "Weapon0", label: "Weapon 1", accepts: "Weapon", itemName: "Wolf's Oath Longsword", customItemIndex: 1 },
      { id: "Weapon1", label: "Weapon 2", accepts: "Weapon" }, { id: "Weapon2", label: "Weapon 3", accepts: "Weapon" }, { id: "Weapon3", label: "Weapon 4", accepts: "Weapon" },
      { id: "Head", label: "Head", accepts: "Head", itemName: "Gilded Vlandian War Helm", customItemIndex: 2 },
      { id: "Cape", label: "Shoulders", accepts: "Shoulders" }, { id: "Body", label: "Body", accepts: "Body" },
      { id: "Gloves", label: "Hands", accepts: "Hands" }, { id: "Leg", label: "Legs", accepts: "Legs" },
      { id: "Horse", label: "Mount", accepts: "Mount" }, { id: "HorseHarness", label: "Harness", accepts: "Harness" },
    ] };
  }
  const requestId = crypto.randomUUID();
  const response = await fetch(`${apiBase}/api/channels/${encodeURIComponent(identity.channelId)}/inventory`, {
    method: "POST",
    headers: { Authorization: `Bearer ${identity.token}`, "Content-Type": "application/json" },
    body: JSON.stringify({ requestId, timestamp: new Date().toISOString() }),
  });
  if (!response.ok) throw new Error((await response.json().catch(() => null))?.detail ?? "Your inventory could not be loaded.");
  return null;
}

export async function requestRetinue(identity: ViewerIdentity) {
  if (identity.token === "development-token" && !isLiveLocalIntegration()) {
    await new Promise(resolve => setTimeout(resolve, 220));
    return { heroName: "Aldric the Bold", updatedAt: new Date().toISOString(), retinue: [
      { slot: 1, name: "Vlandian Sergeant", tier: 5, culture: "Vlandia" },
      { slot: 2, name: "Vlandian Sharpshooter", tier: 5, culture: "Vlandia" },
      { slot: 3, name: "Imperial Elite Menavliaton", tier: 5, culture: "Empire" },
    ], eliteRetinue: [
      { slot: 1, name: "Vlandian Banner Knight", tier: 6, culture: "Vlandia" },
      { slot: 2, name: "Battanian Fian Champion", tier: 6, culture: "Battania" },
    ] };
  }
  const requestId = crypto.randomUUID();
  const response = await fetch(`${apiBase}/api/channels/${encodeURIComponent(identity.channelId)}/retinue`, {
    method: "POST",
    headers: { Authorization: `Bearer ${identity.token}`, "Content-Type": "application/json" },
    body: JSON.stringify({ requestId, timestamp: new Date().toISOString() }),
  });
  if (!response.ok) throw new Error((await response.json().catch(() => null))?.detail ?? "Your retinue could not be loaded.");
  return null;
}

export async function getIntegrationHealth(identity: ViewerIdentity) {
  const response = await fetch(`${apiBase}/api/channels/${encodeURIComponent(identity.channelId)}/health`, {
    headers: { Authorization: `Bearer ${identity.token}` },
  });
  if (!response.ok) throw new Error("The local integration service is unavailable.");
  return response.json() as Promise<{ status: string; channelId: string; gameConnected: boolean; lastStateAt?: string }>;
}
