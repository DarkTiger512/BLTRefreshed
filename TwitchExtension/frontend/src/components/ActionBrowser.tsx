import { ChevronRight, Clock3, Search } from "lucide-react";
import { useDeferredValue } from "react";
import type { ManifestAction } from "../types";

interface Props {
  actions: ManifestAction[];
  category: string;
  selectedId?: string;
  query: string;
  unavailable: Record<string, string>;
  cooldowns: Record<string, number>;
  onQuery(query: string): void;
  onSelect(action: ManifestAction): void;
}

const clean = (value: string) => value.replace(/^['"]?\{=[^}]+\}/, "").replace(/['"]$/, "");

export function ActionBrowser({ actions, category, selectedId, query, unavailable, cooldowns, onQuery, onSelect }: Props) {
  const deferredQuery = useDeferredValue(query.toLowerCase());
  const filtered = actions.filter(action => action.enabledByDefault && (action.category === category || deferredQuery) &&
    (!deferredQuery || `${action.legacyName} ${action.description} ${action.handler}`.toLowerCase().includes(deferredQuery)));
  return <section className="browser-panel" aria-label="Command browser">
    <div className="search-box"><Search /><input aria-label="Search actions" value={query} onChange={event => onQuery(event.target.value)} placeholder="Search actions" /></div>
    <div className="list-heading"><span>{query ? "Search results" : `${category} actions`}</span><span>{filtered.length}</span></div>
    <div className="action-list">
      {filtered.map(action => {
        const reason = unavailable[action.id];
        const cooldown = cooldowns[action.id];
        return <button key={action.id} className={selectedId === action.id ? "action-row selected" : "action-row"} onClick={() => onSelect(action)}>
          <span className="action-monogram">{clean(action.legacyName).slice(0, 2).toUpperCase()}</span>
          <span className="action-copy"><strong>{clean(action.legacyName)}</strong><small>{action.description}</small></span>
          <span className={reason ? "availability unavailable" : cooldown ? "availability cooldown" : "availability available"}>
            {cooldown ? <Clock3 /> : <i />}{reason ?? (cooldown ? `Ready in ${Math.ceil(cooldown / 1000)}s` : "Available")}
          </span>
          <ChevronRight className="row-chevron" />
        </button>;
      })}
      {!filtered.length ? <div className="empty-list">No matching actions in this category.</div> : null}
    </div>
  </section>;
}
