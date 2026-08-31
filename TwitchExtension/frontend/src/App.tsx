import { CheckCircle2, CircleDot, X } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { requestInventory, requestRetinue, submitAction } from "./api";
import { ActionBrowser } from "./components/ActionBrowser";
import { ActionDetail } from "./components/ActionDetail";
import { CategoryRail } from "./components/CategoryRail";
import { ConfigurationView } from "./components/ConfigurationView";
import { InventoryView } from "./components/InventoryView";
import { RetinueView } from "./components/RetinueView";
import { useIntegrationState } from "./hooks/useIntegrationState";
import { authorizeViewer, requestIdentity } from "./twitch";
import type { ActionManifest, ManifestAction, ViewerIdentity } from "./types";
import bltLogo from "./assets/blt-logo-v2.png";

const categories = ["Hero", "Battle", "Retinue", "Kingdom", "Equipment", "Progression", "Tournament", "Community", "General"];

export function App() {
  const [manifest, setManifest] = useState<ActionManifest | null>(null);
  const [identity, setIdentity] = useState<ViewerIdentity | null>(null);
  const [category, setCategory] = useState("Hero");
  const [selected, setSelected] = useState<ManifestAction>();
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const [toast, setToast] = useState<string>();
  const [showInventory, setShowInventory] = useState(false);
  const [inventoryLoading, setInventoryLoading] = useState(false);
  const [retinueLoading, setRetinueLoading] = useState(false);
  const state = useIntegrationState(identity);

  useEffect(() => {
    Promise.all([
      fetch("./action-manifest.json").then(response => response.json() as Promise<ActionManifest>),
      authorizeViewer(),
    ]).then(([loadedManifest, viewer]) => {
      setManifest(loadedManifest);
      setIdentity(viewer);
      setSelected(loadedManifest.actions.find(action => action.id === "command.adopt") ?? loadedManifest.actions.find(action => action.enabledByDefault));
    }).catch(reason => setError(String(reason)));
  }, []);

  const isConfiguration = useMemo(() => new URLSearchParams(window.location.search).get("anchor") === "configuration", []);
  if (!identity || !manifest) return <div className="loading-screen"><CircleDot />Connecting to Bannerlord Twitch…</div>;
  if (isConfiguration) return <ConfigurationView identity={identity} />;

  async function handleSubmit(args: Record<string, unknown>) {
    if (!selected) return;
    setBusy(true); setError(undefined);
    try {
      await submitAction(identity!, selected, args);
      setToast(`${selected.legacyName.replace(/^['"]?\{=[^}]+\}/, "").replace(/['"]$/, "")} sent to your hero`);
      window.setTimeout(() => setToast(undefined), 4200);
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Action failed"); }
    finally { setBusy(false); }
  }

  async function loadInventory() {
    setInventoryLoading(true); state.setInventoryError(undefined);
    try { const snapshot = await requestInventory(identity!); if (snapshot) state.setInventory(snapshot); }
    catch (reason) { state.setInventoryError(reason instanceof Error ? reason.message : "Your inventory could not be loaded."); }
    finally { setInventoryLoading(false); }
  }

  function openInventory() { setShowInventory(true); if (identity!.linked && !state.inventory && !inventoryLoading) void loadInventory(); }

  async function loadRetinue() {
    setRetinueLoading(true); state.setRetinueError(undefined);
    try { const snapshot = await requestRetinue(identity!); if (snapshot) state.setRetinue(snapshot); }
    catch (reason) { state.setRetinueError(reason instanceof Error ? reason.message : "Your retinue could not be loaded."); }
    finally { setRetinueLoading(false); }
  }

  function selectCategory(value: string) {
    setShowInventory(false); setCategory(value); setQuery("");
    if (value === "Retinue" && identity!.linked && !state.retinue && !retinueLoading) void loadRetinue();
  }

  async function manageRetinue(actionId: "command.retinue" | "command.eliteretinue", operation: string, slot?: number) {
    const action = manifest!.actions.find(candidate => candidate.id === actionId);
    if (!action) { state.setRetinueError("Retinue controls are unavailable."); return; }
    setBusy(true); state.setRetinueError(undefined);
    try {
      await submitAction(identity!, action, slot ? { operation, slot } : { operation });
      setToast(operation.startsWith("clear") ? "Retinue updated" : "Retinue order sent");
      window.setTimeout(() => setToast(undefined), 3200);
      window.setTimeout(() => void loadRetinue(), identity!.token === "development-token" ? 350 : 700);
    } catch (reason) { state.setRetinueError(reason instanceof Error ? reason.message : "That retinue order could not be completed."); }
    finally { setBusy(false); }
  }

  async function equipInventoryItem(itemIndex: number, slotId: string) {
    const equipAction = manifest!.actions.find(action => action.id === "command.equipcustom");
    if (!equipAction) { state.setInventoryError("Equipment controls are unavailable."); return; }
    setInventoryLoading(true); state.setInventoryError(undefined);
    try {
      await submitAction(identity!, equipAction, { item: `${itemIndex} @${slotId}` });
      state.setInventory(current => current ? {
        ...current,
        items: current.items.map(item => ({ ...item, equipped: item.index === itemIndex || current.slots.some(slot => slot.customItemIndex === item.index && slot.id !== slotId) })),
        slots: current.slots.map(slot => slot.id === slotId ? { ...slot, itemName: current.items.find(item => item.index === itemIndex)?.name, customItemIndex: itemIndex } : slot),
        updatedAt: new Date().toISOString(),
      } : current);
      setToast("Equipment updated"); window.setTimeout(() => setToast(undefined), 3200);
      if (identity!.token !== "development-token") window.setTimeout(() => void loadInventory(), 700);
    } catch (reason) { state.setInventoryError(reason instanceof Error ? reason.message : "That item could not be equipped."); }
    finally { setInventoryLoading(false); }
  }

  const integratedActions = new Set(["command.equipcustom", "command.retinue", "command.eliteretinue", "command.retinuelist"]);
  const browserActions = manifest.actions.filter(action => !integratedActions.has(action.id));
  const showRetinue = !showInventory && category === "Retinue";

  return <main className={open ? "overlay-shell open" : "overlay-shell collapsed"}>
    {!open ? <button className="open-launcher" onClick={() => setOpen(true)} aria-label="Open Bannerlord Twitch"><img src={bltLogo} alt="" /><span>BLT</span></button> : null}
    {open ? <div className="overlay-window">
      <header className="top-bar"><img className="app-logo" src={bltLogo} alt="" /><h1>Bannerlord Twitch</h1><span className={state.connected ? "connection connected" : "connection disconnected"}><i />{state.connected ? "Connected" : "Game offline"}</span><button className="close-button" onClick={() => setOpen(false)} aria-label="Collapse overlay"><X /></button></header>
      <div className="overlay-content">
        <CategoryRail categories={categories} selected={category} inventorySelected={showInventory} onSelect={selectCategory} onInventory={openInventory} identityName={identity.displayName} linked={identity.linked} />
        {showInventory ? <InventoryView inventory={state.inventory} loading={inventoryLoading} error={state.inventoryError} linked={identity.linked} onRefresh={loadInventory} onEquip={equipInventoryItem} onRequestIdentity={requestIdentity} /> : showRetinue ? <RetinueView retinue={state.retinue} loading={retinueLoading} error={state.retinueError} linked={identity.linked} busy={busy} onRefresh={loadRetinue} onManage={manageRetinue} onRequestIdentity={requestIdentity} /> : <><ActionBrowser actions={browserActions} category={category} selectedId={selected?.id} query={query} unavailable={state.unavailable} cooldowns={state.cooldowns} onQuery={setQuery} onSelect={setSelected} /><ActionDetail action={selected} linked={identity.linked} unavailableReason={selected ? state.unavailable[selected.id] : undefined} busy={busy} error={error} onRequestIdentity={requestIdentity} onSubmit={handleSubmit} /></>}
      </div>
    </div> : null}
    {toast ? <div className="success-toast" role="status"><CheckCircle2 />{toast}</div> : null}
  </main>;
}
