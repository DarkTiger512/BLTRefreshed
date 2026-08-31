import { Crown, RefreshCw, Shield, UserRoundCheck } from "lucide-react";
import type { RetinueSnapshot, RetinueTroop } from "../types";

interface Props {
  retinue?: RetinueSnapshot;
  loading: boolean;
  error?: string;
  linked: boolean;
  busy: boolean;
  onRefresh(): void;
  onManage(actionId: "command.retinue" | "command.eliteretinue", operation: string, slot?: number): void;
  onRequestIdentity(): void;
}

function Roster({ title, elite, troops, actionId, disabled, onManage }: { title: string; elite?: boolean; troops: RetinueTroop[]; actionId: "command.retinue" | "command.eliteretinue"; disabled: boolean; onManage: Props["onManage"] }) {
  return <section className={elite ? "retinue-roster elite" : "retinue-roster"}>
    <div className="retinue-roster-heading">
      <span className="retinue-emblem">{elite ? <Crown /> : <Shield />}</span>
      <div><h3>{title}</h3><small>{troops.length} {troops.length === 1 ? "troop" : "troops"}</small></div>
      <div className="retinue-actions">
        <button disabled={disabled} onClick={() => onManage(actionId, "upgrade-one")}>Recruit / upgrade one</button>
        <button disabled={disabled} onClick={() => onManage(actionId, "upgrade-all")}>Upgrade all</button>
      </div>
    </div>
    <div className="troop-list">
      {troops.length ? troops.map(troop => <article className="troop-card" key={troop.slot}>
        <span className="troop-slot">{troop.slot}</span>
        <span className="troop-silhouette"><UserRoundCheck /></span>
        <span className="troop-name"><strong>{troop.name}</strong><small>Tier {troop.tier}{troop.culture ? ` · ${troop.culture}` : ""}</small></span>
        <button className="dismiss-troop" disabled={disabled} onClick={() => onManage(actionId, "clear-slot", troop.slot)}>Dismiss</button>
      </article>) : <div className="empty-roster">No troops recruited yet.</div>}
    </div>
    {troops.length ? <button className="dismiss-all" disabled={disabled} onClick={() => onManage(actionId, "clear-all")}>Dismiss entire {elite ? "elite " : ""}retinue</button> : null}
  </section>;
}

export function RetinueView({ retinue, loading, error, linked, busy, onRefresh, onManage, onRequestIdentity }: Props) {
  if (!linked) return <section className="retinue-view identity-gate"><Crown /><h2>Your retinue is private</h2><p>Share your Twitch identity to see and manage the troops attached to your adopted hero.</p><button onClick={onRequestIdentity}>Share identity</button></section>;
  return <section className="retinue-view">
    <header className="retinue-header"><div><span className="eyebrow">Private live roster</span><h2>My Retinue</h2><p>{retinue ? `${retinue.heroName}'s personal forces` : "Loading your hero's personal forces…"}</p></div><button className="inventory-refresh" onClick={onRefresh} disabled={loading || busy}><RefreshCw className={loading ? "spinning" : ""} /> Refresh</button></header>
    {error ? <div className="inventory-error" role="alert">{error}</div> : null}
    {loading && !retinue ? <div className="retinue-loading">Calling the banners…</div> : null}
    {retinue ? <div className="retinue-grid">
      <Roster title="Battle Retinue" troops={retinue.retinue} actionId="command.retinue" disabled={loading || busy} onManage={onManage} />
      <Roster title="Elite Retinue" elite troops={retinue.eliteRetinue} actionId="command.eliteretinue" disabled={loading || busy} onManage={onManage} />
    </div> : null}
    <footer className="private-note">Only you can see this roster. It stays open while you browse and refreshes after each change.</footer>
  </section>;
}
