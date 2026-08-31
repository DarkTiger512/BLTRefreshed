import type { ManifestAction, ViewerIdentity } from "./types";

const apiBase = import.meta.env.VITE_BLT_API_URL ?? "http://127.0.0.1:5188";

export async function createPairingCode(identity: ViewerIdentity) {
  if (identity.token === "development-token") {
    return { code: "BLT-DEMO-PAIR", expiresAt: new Date(Date.now() + 600_000).toISOString() };
  }
  const response = await fetch(`${apiBase}/api/channels/${encodeURIComponent(identity.channelId)}/pairing`, {
    method: "POST",
    headers: { Authorization: `Bearer ${identity.token}` },
  });
  if (!response.ok) throw new Error((await response.json().catch(() => null))?.detail ?? "A pairing code could not be created.");
  return response.json() as Promise<{ code: string; expiresAt: string }>;
}

export async function submitAction(identity: ViewerIdentity, action: ManifestAction, args: Record<string, unknown>) {
  if (identity.token === "development-token") {
    await new Promise(resolve => setTimeout(resolve, 350));
    return { requestId: crypto.randomUUID(), status: "accepted" };
  }
  const response = await fetch(`${apiBase}/api/channels/${encodeURIComponent(identity.channelId)}/actions`, {
    method: "POST",
    headers: { Authorization: `Bearer ${identity.token}`, "Content-Type": "application/json" },
    body: JSON.stringify({ requestId: crypto.randomUUID(), actionId: action.id, args, timestamp: new Date().toISOString() }),
  });
  if (!response.ok) throw new Error((await response.json().catch(() => null))?.detail ?? "The action could not be sent.");
  return response.json();
}

export async function requestInventory(identity: ViewerIdentity) {
  if (identity.token === "development-token") {
    await new Promise(resolve => setTimeout(resolve, 250));
    return { heroName: "Aldric the Bold", limit: 8, updatedAt: new Date().toISOString(), items: [
      { index: 1, name: "Wolf's Oath Longsword — +12 Damage, +8 Swing Speed", type: "One Handed Weapon", equipped: true },
      { index: 2, name: "Gilded Vlandian War Helm — +9 Head Armor", type: "Head Armor", equipped: true },
      { index: 3, name: "Ashen Tournament Bow — +10 Damage, +6 Speed", type: "Bow", equipped: false },
      { index: 4, name: "Banner of the Iron Stag — +14 Hit Points", type: "Shield", equipped: false },
      { index: 5, name: "Stormhoof — +8 Mount Speed, +12 Charge", type: "Horse", equipped: false },
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
