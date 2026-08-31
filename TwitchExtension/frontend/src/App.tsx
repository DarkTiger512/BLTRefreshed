import { CheckCircle2, CircleDot, X } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { submitAction } from "./api";
import { ActionBrowser } from "./components/ActionBrowser";
import { ActionDetail } from "./components/ActionDetail";
import { CategoryRail } from "./components/CategoryRail";
import { ConfigurationView } from "./components/ConfigurationView";
import { useIntegrationState } from "./hooks/useIntegrationState";
import { authorizeViewer, requestIdentity } from "./twitch";
import type { ActionManifest, ManifestAction, ViewerIdentity } from "./types";
import bltLogo from "./assets/blt-logo-v1.png";

const categories = ["Hero", "Battle", "Kingdom", "Equipment", "Progression", "Tournament", "Community", "General"];

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

  return <main className={open ? "overlay-shell open" : "overlay-shell collapsed"}>
    {!open ? <button className="open-launcher" onClick={() => setOpen(true)} aria-label="Open Bannerlord Twitch"><img src={bltLogo} alt="" /><span>BLT</span></button> : null}
    {open ? <div className="overlay-window">
      <header className="top-bar"><img className="app-logo" src={bltLogo} alt="" /><h1>Bannerlord Twitch</h1><span className={state.connected ? "connection connected" : "connection disconnected"}><i />{state.connected ? "Connected" : "Game offline"}</span><button className="close-button" onClick={() => setOpen(false)} aria-label="Collapse overlay"><X /></button></header>
      <div className="overlay-content">
        <CategoryRail categories={categories} selected={category} onSelect={value => { setCategory(value); setQuery(""); }} identityName={identity.displayName} linked={identity.linked} />
        <ActionBrowser actions={manifest.actions} category={category} selectedId={selected?.id} query={query} unavailable={state.unavailable} cooldowns={state.cooldowns} onQuery={setQuery} onSelect={setSelected} />
        <ActionDetail action={selected} linked={identity.linked} unavailableReason={selected ? state.unavailable[selected.id] : undefined} busy={busy} error={error} onRequestIdentity={requestIdentity} onSubmit={handleSubmit} />
      </div>
    </div> : null}
    {toast ? <div className="success-toast" role="status"><CheckCircle2 />{toast}</div> : null}
  </main>;
}
