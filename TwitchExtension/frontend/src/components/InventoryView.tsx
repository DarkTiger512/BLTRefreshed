import { ArrowLeft, GripVertical, PackageOpen, RefreshCw, Search, ShieldCheck, Swords } from "lucide-react";
import { useDeferredValue, useState } from "react";
import type { InventorySnapshot } from "../types";

interface Props { inventory?: InventorySnapshot; loading: boolean; error?: string; linked: boolean; initialFilter?: string; onBack(): void; onRefresh(): void; onEquip(itemIndex: number, slotId: string): void; onRequestIdentity(): void }

function canEquip(itemType: string | undefined, slotId: string) {
  const type = itemType?.replace(/\s/g, "").toLowerCase() ?? "";
  if (slotId.startsWith("Weapon")) return /weapon|bow|crossbow|polearm|thrown|shield|arrow|bolt|ammo/.test(type);
  const expected: Record<string, RegExp> = {
    Head: /headarmor/, Body: /bodyarmor/, Leg: /legarmor/, Gloves: /handarmor/, Cape: /cape/,
    Horse: /^horse$|mount/, HorseHarness: /horseharness|harness/,
  };
  return expected[slotId]?.test(type) ?? false;
}

export function InventoryView({ inventory, loading, error, linked, initialFilter = "", onBack, onRefresh, onEquip, onRequestIdentity }: Props) {
  const [query, setQuery] = useState(initialFilter);
  const [selectedItem, setSelectedItem] = useState<number>();
  const [draggedItem, setDraggedItem] = useState<number>();
  const deferredQuery = useDeferredValue(query.trim().toLowerCase());
  const items = inventory?.items.filter(item => !deferredQuery || `${item.name} ${item.type}`.toLowerCase().includes(deferredQuery)) ?? [];

  return <section className="inventory-view" aria-labelledby="inventory-title">
    <header className="inventory-header">
      <button className="workspace-back" onClick={onBack} aria-label="Back to command bar"><ArrowLeft /></button>
      <div className="inventory-emblem"><PackageOpen /></div>
      <div><p>PERSONAL STORAGE</p><h2 id="inventory-title">My inventory</h2><span>{inventory ? `${inventory.heroName} · ${inventory.items.length} of ${inventory.limit} customs` : "Your custom items, kept here"}</span></div>
      <button className="inventory-refresh" onClick={onRefresh} disabled={loading || !linked}><RefreshCw className={loading ? "spinning" : ""} />{loading ? "Loading" : "Refresh"}</button>
    </header>
    {!linked ? <div className="inventory-message"><ShieldCheck /><h3>Your inventory is private</h3><p>Share your Twitch identity so BLT can find your adopted hero. Only you can receive this inventory.</p><button className="primary-action" onClick={onRequestIdentity}>Share Twitch identity</button></div> : null}
    {linked && error ? <div className="inventory-message"><PackageOpen /><h3>Inventory unavailable</h3><p>{error}</p><button className="primary-action" onClick={onRefresh}>Try again</button></div> : null}
    {linked && !error && !inventory ? <div className="inventory-message"><RefreshCw className={loading ? "spinning" : ""} /><h3>{loading ? "Checking your saddlebags…" : "Open your inventory"}</h3><p>This list stays open until you close it—no chat command and no disappearing overlay message.</p>{!loading ? <button className="primary-action" onClick={onRefresh}>Load my inventory</button> : null}</div> : null}
    {linked && inventory ? <>
      <div className="equipment-section">
        <div className="section-heading"><span><Swords />Equipped slots</span><small>{selectedItem ? "Choose a slot for the selected item" : "Drag an item here or select it, then choose a slot"}</small></div>
        <div className="equipment-slots">
          {inventory.slots.map(slot => {
            const pendingItem = inventory.items.find(item => item.index === (selectedItem ?? draggedItem));
            const compatible = pendingItem ? canEquip(pendingItem.type, slot.id) : false;
            return <button key={slot.id} className={`equipment-slot${compatible ? " receiving" : pendingItem ? " incompatible" : ""}`} disabled={loading} onClick={() => { if (selectedItem && compatible) { onEquip(selectedItem, slot.id); setSelectedItem(undefined); } }} onDragOver={event => { if (compatible) event.preventDefault(); }} onDrop={event => { event.preventDefault(); if (draggedItem && compatible) onEquip(draggedItem, slot.id); setDraggedItem(undefined); setSelectedItem(undefined); }}>
            <span>{slot.label}</span><strong>{slot.itemName ?? "Empty slot"}</strong><small>{slot.itemName ? slot.accepts : `Accepts ${slot.accepts.toLowerCase()}`}</small>
          </button>})}
        </div>
      </div>
      <div className="inventory-toolbar"><div className="section-heading"><span><PackageOpen />Custom storage</span><small>{selectedItem ? `Item #${selectedItem} selected` : "Click or drag an item to equip it"}</small></div><label className="inventory-search"><Search /><input value={query} onChange={event => setQuery(event.target.value)} placeholder="Search your items" /></label></div>
      <div className="inventory-list" aria-live="polite">
        {items.map(item => <article className={`inventory-item${selectedItem === item.index ? " selected" : ""}`} key={item.index} draggable onDragStart={() => setDraggedItem(item.index)} onDragEnd={() => setDraggedItem(undefined)} onClick={() => setSelectedItem(value => value === item.index ? undefined : item.index)} tabIndex={0} role="button" aria-pressed={selectedItem === item.index} onKeyDown={event => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); setSelectedItem(value => value === item.index ? undefined : item.index); } }}>
          <GripVertical className="drag-handle" /><span className="inventory-index">#{item.index}</span><div><strong>{item.name}</strong><small>{item.type}</small></div>
          {item.equipped ? <span className="equipped-badge"><ShieldCheck />Equipped</span> : <span className="stored-badge">Stored</span>}
        </article>)}
        {items.length === 0 ? <p className="empty-list">No custom items match that search.</p> : null}
      </div>
      <footer className="inventory-footer">Updated {inventory.updatedAt ? new Date(inventory.updatedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }) : "just now"} · This view is visible only to you.</footer>
    </> : null}
  </section>;
}
