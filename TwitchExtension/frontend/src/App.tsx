import { CircleDot, X } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { requestInventory, requestRetinue, submitAction } from "./api";
import { isLiveLocalIntegration } from "./environment";
import { ActionBrowser } from "./components/ActionBrowser";
import { ActionDetail } from "./components/ActionDetail";
import { BattleWorkspace } from "./components/BattleWorkspace";
import { CategoryRail } from "./components/CategoryRail";
import { ConfigurationView } from "./components/ConfigurationView";
import { CommandFeedView } from "./components/CommandFeedView";
import { InventoryView } from "./components/InventoryView";
import { IntegrationDiagnostics } from "./components/IntegrationDiagnostics";
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
  const [feedExpanded, setFeedExpanded] = useState(false);
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

  async function handleActionSubmit(action: ManifestAction, args: Record<string, unknown>) {
    setBusy(true); setError(undefined);
    try {
      const response = await submitAction(identity!, action, args);
      state.recordCommand({ requestId: response.requestId, actionId: action.id, actionName: action.legacyName, status: "pending" });
      if (identity!.token === "development-token" && !isLiveLocalIntegration()) state.completeDevelopmentCommand(response.requestId, `${action.legacyName} completed successfully.`);
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Action failed"); }
    finally { setBusy(false); }
  }
  async function handleSubmit(args: Record<string, unknown>) { if (selected) await handleActionSubmit(selected, args); }

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

  async function manageRetinue(actionId: "command.retinue" | "command.eliteretinue", operation: string, value?: number) {
    const action = manifest!.actions.find(candidate => candidate.id === actionId);
    if (!action) { state.setRetinueError("Retinue controls are unavailable."); return; }
    setBusy(true); state.setRetinueError(undefined);
    try {
      const args = operation === "upgrade-count" ? { operation, count: value } : operation === "clear-slot" ? { operation, slot: value } : { operation };
      const response = await submitAction(identity!, action, args);
      state.recordCommand({ requestId: response.requestId, actionId: action.id, actionName: action.legacyName, status: "pending" });
      if (identity!.token === "development-token" && !isLiveLocalIntegration()) state.completeDevelopmentCommand(response.requestId, `${action.legacyName} completed successfully.`);
      window.setTimeout(() => void loadRetinue(), identity!.token === "development-token" && !isLiveLocalIntegration() ? 350 : 700);
    } catch (reason) { state.setRetinueError(reason instanceof Error ? reason.message : "That retinue order could not be completed."); }
    finally { setBusy(false); }
  }

  async function equipInventoryItem(itemIndex: number, slotId: string) {
    const equipAction = manifest!.actions.find(action => action.id === "command.equipcustom");
    if (!equipAction) { state.setInventoryError("Equipment controls are unavailable."); return; }
    setInventoryLoading(true); state.setInventoryError(undefined);
    try {
      const response = await submitAction(identity!, equipAction, { item: `${itemIndex} @${slotId}` });
      state.recordCommand({ requestId: response.requestId, actionId: equipAction.id, actionName: "Equip custom item", status: "pending" });
      if (identity!.token === "development-token" && !isLiveLocalIntegration()) state.completeDevelopmentCommand(response.requestId, "Equipment updated successfully.");
      if (!isLiveLocalIntegration()) state.setInventory(current => current ? {
        ...current,
        items: current.items.map(item => ({ ...item, equipped: item.index === itemIndex || current.slots.some(slot => slot.customItemIndex === item.index && slot.id !== slotId) })),
        slots: current.slots.map(slot => slot.id === slotId ? { ...slot, itemName: current.items.find(item => item.index === itemIndex)?.name, customItemIndex: itemIndex } : slot),
        updatedAt: new Date().toISOString(),
      } : current);
      if (identity!.token !== "development-token" || isLiveLocalIntegration()) window.setTimeout(() => void loadInventory(), 700);
    } catch (reason) { state.setInventoryError(reason instanceof Error ? reason.message : "That item could not be equipped."); }
    finally { setInventoryLoading(false); }
  }

  const integratedActions = new Set(["command.equipcustom", "command.retinue", "command.eliteretinue", "command.retinuelist"]);
  const browserActions = manifest.actions.filter(action => !integratedActions.has(action.id));
  const effectiveUnavailable = state.connected ? state.unavailable : Object.fromEntries(manifest.actions.map(action => [action.id, "The streamer's game is offline."]));
  const showRetinue = !showInventory && category === "Retinue";
  const redundantBattleActions = new Set(["command.battle", "command.stats", "command.ammo"]);
  const battleActions = manifest.actions.filter(action => Object.hasOwn(state.mission.actionAvailability, action.id) && !redundantBattleActions.has(action.id));
  const battleActive = state.mission.active && (state.mission.kind === "battle" || state.mission.kind === "tournament");

  return <main className={open ? "overlay-shell open" : "overlay-shell collapsed"}>
    {!open ? <button className="open-launcher" onClick={() => setOpen(true)} aria-label="Open Bannerlord Twitch"><img src={bltLogo} alt="" /><span>BLT</span></button> : null}
    {open ? <div className="overlay-window">
      <header className="top-bar"><img className="app-logo" src={bltLogo} alt="" /><h1>Bannerlord Twitch</h1><span className={state.connected ? "connection connected" : "connection disconnected"}><i />{state.connected ? "Connected" : "Game offline"}</span><button className="close-button" onClick={() => setOpen(false)} aria-label="Collapse overlay"><X /></button></header>
      <IntegrationDiagnostics identity={identity} />
      <div className={`overlay-content ${battleActive ? "battle-active" : ""}`}>
        {!battleActive ? <CategoryRail categories={categories} selected={category} inventorySelected={showInventory} onSelect={selectCategory} onInventory={openInventory} identityName={identity.displayName} linked={identity.linked} /> : null}
        <div className="workspace-stack">
          <div className="workspace-main">
            {battleActive ? <BattleWorkspace mission={state.mission} actions={battleActions} identity={identity} cooldowns={state.cooldowns} selectors={state.selectors} busy={busy} error={error} onRequestIdentity={requestIdentity} onSubmit={handleActionSubmit} /> : showInventory ? <InventoryView inventory={state.inventory} loading={inventoryLoading} error={state.inventoryError} linked={identity.linked} onRefresh={loadInventory} onEquip={equipInventoryItem} onRequestIdentity={requestIdentity} /> : showRetinue ? <RetinueView retinue={state.retinue} loading={retinueLoading} error={state.retinueError} linked={identity.linked} busy={busy} onRefresh={loadRetinue} onManage={manageRetinue} onRequestIdentity={requestIdentity} /> : <><ActionBrowser actions={browserActions} category={category} selectedId={selected?.id} query={query} unavailable={effectiveUnavailable} cooldowns={state.cooldowns} onQuery={setQuery} onSelect={setSelected} /><ActionDetail action={selected} linked={identity.linked} unavailableReason={selected ? effectiveUnavailable[selected.id] : undefined} busy={busy} error={error} selectors={state.selectors} onRequestIdentity={requestIdentity} onSubmit={handleSubmit} /></>}
          </div>
          <CommandFeedView entries={state.commandActivity} expanded={feedExpanded} onToggle={() => setFeedExpanded(value => !value)} onClear={state.clearCommandActivity} />
        </div>
      </div>
    </div> : null}
  </main>;
}
