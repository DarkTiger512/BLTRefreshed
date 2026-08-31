import { PackageOpen, RefreshCw, Search, ShieldCheck } from "lucide-react";
import { useDeferredValue, useState } from "react";
import type { InventorySnapshot } from "../types";

interface Props { inventory?: InventorySnapshot; loading: boolean; error?: string; linked: boolean; onRefresh(): void; onRequestIdentity(): void }

export function InventoryView({ inventory, loading, error, linked, onRefresh, onRequestIdentity }: Props) {
  const [query, setQuery] = useState("");
  const deferredQuery = useDeferredValue(query.trim().toLowerCase());
  const items = inventory?.items.filter(item => !deferredQuery || `${item.name} ${item.type}`.toLowerCase().includes(deferredQuery)) ?? [];

  return <section className="inventory-view" aria-labelledby="inventory-title">
    <header className="inventory-header">
      <div className="inventory-emblem"><PackageOpen /></div>
      <div><p>PERSONAL STORAGE</p><h2 id="inventory-title">My inventory</h2><span>{inventory ? `${inventory.heroName} · ${inventory.items.length} of ${inventory.limit} customs` : "Your custom items, kept here"}</span></div>
      <button className="inventory-refresh" onClick={onRefresh} disabled={loading || !linked}><RefreshCw className={loading ? "spinning" : ""} />{loading ? "Loading" : "Refresh"}</button>
    </header>
    {!linked ? <div className="inventory-message"><ShieldCheck /><h3>Your inventory is private</h3><p>Share your Twitch identity so BLT can find your adopted hero. Only you can receive this inventory.</p><button className="primary-action" onClick={onRequestIdentity}>Share Twitch identity</button></div> : null}
    {linked && error ? <div className="inventory-message"><PackageOpen /><h3>Inventory unavailable</h3><p>{error}</p><button className="primary-action" onClick={onRefresh}>Try again</button></div> : null}
    {linked && !error && !inventory ? <div className="inventory-message"><RefreshCw className={loading ? "spinning" : ""} /><h3>{loading ? "Checking your saddlebags…" : "Open your inventory"}</h3><p>This list stays open until you close it—no chat command and no disappearing overlay message.</p>{!loading ? <button className="primary-action" onClick={onRefresh}>Load my inventory</button> : null}</div> : null}
    {linked && inventory ? <>
      <label className="inventory-search"><Search /><input value={query} onChange={event => setQuery(event.target.value)} placeholder="Search your items" /></label>
      <div className="inventory-list" aria-live="polite">
        {items.map(item => <article className="inventory-item" key={item.index}>
          <span className="inventory-index">#{item.index}</span><div><strong>{item.name}</strong><small>{item.type}</small></div>
          {item.equipped ? <span className="equipped-badge"><ShieldCheck />Equipped</span> : <span className="stored-badge">Stored</span>}
        </article>)}
        {items.length === 0 ? <p className="empty-list">No custom items match that search.</p> : null}
      </div>
      <footer className="inventory-footer">Updated {inventory.updatedAt ? new Date(inventory.updatedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }) : "just now"} · This view is visible only to you.</footer>
    </> : null}
  </section>;
}
