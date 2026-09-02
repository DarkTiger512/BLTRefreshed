import { ArrowLeft, GripVertical, PackageOpen, RefreshCw, Search, ShieldCheck, Swords } from "lucide-react";
import { useDeferredValue, useState } from "react";
import type { InventorySnapshot } from "../types";
import { useI18n } from "../i18n";

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
  const { t, time } = useI18n();
  const [query, setQuery] = useState(initialFilter);
  const [selectedItem, setSelectedItem] = useState<number>();
  const [draggedItem, setDraggedItem] = useState<number>();
  const deferredQuery = useDeferredValue(query.trim().toLowerCase());
  const inventoryItems = inventory?.items ?? [];
  const inventorySlots = inventory?.slots ?? [];
  const items = inventoryItems.filter(item => !deferredQuery || `${item.name} ${item.type}`.toLowerCase().includes(deferredQuery));

  return <section className="inventory-view" aria-labelledby="inventory-title">
    <header className="inventory-header">
      <button className="workspace-back" onClick={onBack} aria-label={t("common.back")}><ArrowLeft /></button>
      <div className="inventory-emblem"><PackageOpen /></div>
      <div><p>{t("inventory.storage")}</p><h2 id="inventory-title">{t("inventory.title")}</h2><span>{inventory ? t("inventory.summary", { hero: inventory.heroName, count: inventoryItems.length, limit: inventory.limit }) : t("inventory.subtitle")}</span></div>
      <button className="inventory-refresh" onClick={onRefresh} disabled={loading || !linked}><RefreshCw className={loading ? "spinning" : ""} />{loading ? t("common.loading") : t("common.refresh")}</button>
    </header>
    {!linked ? <div className="inventory-message"><ShieldCheck /><h3>{t("inventory.private.title")}</h3><p>{t("inventory.private.detail")}</p><button className="primary-action" onClick={onRequestIdentity}>{t("identity.share")}</button></div> : null}
    {linked && error ? <div className="inventory-message"><PackageOpen /><h3>{t("inventory.unavailable")}</h3><p>{error}</p><button className="primary-action" onClick={onRefresh}>{t("common.tryAgain")}</button></div> : null}
    {linked && !error && !inventory ? <div className="inventory-message"><RefreshCw className={loading ? "spinning" : ""} /><h3>{loading ? t("inventory.checking") : t("inventory.open")}</h3><p>{t("inventory.open.detail")}</p>{!loading ? <button className="primary-action" onClick={onRefresh}>{t("inventory.load")}</button> : null}</div> : null}
    {linked && inventory ? <>
      <div className="equipment-section">
        <div className="section-heading"><span><Swords />{t("inventory.slots")}</span><small>{selectedItem ? t("inventory.slot.choose") : t("inventory.slot.drag")}</small></div>
        <div className="equipment-slots">
          {inventorySlots.map(slot => {
            const pendingItem = inventoryItems.find(item => item.index === (selectedItem ?? draggedItem));
            const compatible = pendingItem ? canEquip(pendingItem.type, slot.id) : false;
            return <button key={slot.id} className={`equipment-slot${compatible ? " receiving" : pendingItem ? " incompatible" : ""}`} disabled={loading} onClick={() => { if (selectedItem && compatible) { onEquip(selectedItem, slot.id); setSelectedItem(undefined); } }} onDragOver={event => { if (compatible) event.preventDefault(); }} onDrop={event => { event.preventDefault(); if (draggedItem && compatible) onEquip(draggedItem, slot.id); setDraggedItem(undefined); setSelectedItem(undefined); }}>
            <span>{slot.label}</span><strong>{slot.itemName ?? t("inventory.emptySlot")}</strong><small>{slot.itemName ? slot.accepts : t("inventory.accepts", { type: slot.accepts.toLowerCase() })}</small>
          </button>})}
        </div>
      </div>
      <div className="inventory-toolbar"><div className="section-heading"><span><PackageOpen />{t("inventory.customStorage")}</span><small>{selectedItem ? t("inventory.selected", { index: selectedItem }) : t("inventory.equipHint")}</small></div><label className="inventory-search"><Search /><input value={query} onChange={event => setQuery(event.target.value)} placeholder={t("inventory.search")} /></label></div>
      <div className="inventory-list" aria-live="polite">
        {items.map(item => <article className={`inventory-item${selectedItem === item.index ? " selected" : ""}`} key={item.index} draggable onDragStart={() => setDraggedItem(item.index)} onDragEnd={() => setDraggedItem(undefined)} onClick={() => setSelectedItem(value => value === item.index ? undefined : item.index)} tabIndex={0} role="button" aria-pressed={selectedItem === item.index} onKeyDown={event => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); setSelectedItem(value => value === item.index ? undefined : item.index); } }}>
          <GripVertical className="drag-handle" /><span className="inventory-index">#{item.index}</span><div><strong>{item.name}</strong><small>{item.type}</small></div>
          {item.equipped ? <span className="equipped-badge"><ShieldCheck />{t("inventory.equipped")}</span> : <span className="stored-badge">{t("inventory.stored")}</span>}
        </article>)}
        {items.length === 0 ? <p className="empty-list">{t("inventory.noMatches")}</p> : null}
      </div>
      <footer className="inventory-footer">{t("inventory.updated", { time: inventory.updatedAt ? time(inventory.updatedAt) : t("inventory.justNow") })}</footer>
    </> : null}
  </section>;
}
