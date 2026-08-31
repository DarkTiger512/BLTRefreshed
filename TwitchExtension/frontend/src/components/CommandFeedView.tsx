import { CheckCircle2, ChevronDown, ChevronUp, CircleDot, Clock3, ScrollText, Trash2, XCircle } from "lucide-react";
import type { CommandActivity } from "../types";

function StatusIcon({ status }: { status: CommandActivity["status"] }) {
  if (status === "succeeded") return <CheckCircle2 />;
  if (status === "failed") return <XCircle />;
  return <CircleDot className="activity-pending-icon" />;
}

function FeedEntry({ entry, compact }: { entry: CommandActivity; compact?: boolean }) {
  const message = entry.messages[0] ?? "Command accepted and queued for Bannerlord.";
  return <article className={`command-feed-entry ${entry.status}${compact ? " compact" : ""}`}>
    <span className="command-status"><StatusIcon status={entry.status} /></span>
    <div className="command-feed-copy">
      <div className="command-feed-title"><strong>{entry.actionName}</strong><span>{entry.status === "pending" ? "Pending" : entry.status === "succeeded" ? "Succeeded" : "Failed"}</span></div>
      <p>{message}</p>
      <small><Clock3 /> {new Date(entry.completedAt ?? entry.submittedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit" })}</small>
    </div>
  </article>;
}

interface Props { entries: CommandActivity[]; expanded: boolean; onToggle(): void; onClear(): void }

export function CommandFeedView({ entries, expanded, onToggle, onClear }: Props) {
  const recent = entries.slice(0, 3);
  return <section className={expanded ? "command-feed-dock expanded" : "command-feed-dock"} aria-label="Private command feed">
    <header className="command-feed-dock-header">
      <button className="feed-expand-button" onClick={onToggle} aria-expanded={expanded}><ScrollText /><span><strong>Command Feed</strong><small>{entries.length ? `${entries.length} command${entries.length === 1 ? "" : "s"}` : "Waiting for your first command"}</small></span>{expanded ? <ChevronDown /> : <ChevronUp />}</button>
      {expanded && entries.length ? <button className="clear-command-feed" onClick={onClear}><Trash2 /> Clear feed</button> : null}
    </header>
    {expanded ? <div className="command-feed-history">
      <div className="command-feed-history-title"><span className="eyebrow">Private viewer history</span><h2>Your Command Results</h2></div>
      {entries.length ? <div className="command-feed-list" aria-live="polite">{entries.map(entry => <FeedEntry entry={entry} key={entry.requestId} />)}</div> : <div className="empty-command-feed"><ScrollText /><h3>No commands yet</h3><p>Actions you send from the Extension will appear here with their success or failure response.</p></div>}
    </div> : <div className="command-feed-compact" aria-live="polite">
      {recent.length ? recent.map(entry => <FeedEntry entry={entry} compact key={entry.requestId} />) : <div className="compact-feed-empty"><CircleDot />Your private command results will stay visible here.</div>}
    </div>}
  </section>;
}
