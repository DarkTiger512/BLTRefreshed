import { CheckCircle2, CircleDot, Clock3, ScrollText, Trash2, XCircle } from "lucide-react";
import type { CommandActivity } from "../types";

function StatusIcon({ status }: { status: CommandActivity["status"] }) {
  if (status === "succeeded") return <CheckCircle2 />;
  if (status === "failed") return <XCircle />;
  return <CircleDot className="activity-pending-icon" />;
}

export function CommandFeedView({ entries, onClear }: { entries: CommandActivity[]; onClear(): void }) {
  return <section className="command-feed-view">
    <header className="command-feed-header"><div><span className="eyebrow">Private viewer history</span><h2>Command Feed</h2><p>Your commands and their live results from the game.</p></div>{entries.length ? <button className="clear-command-feed" onClick={onClear}><Trash2 /> Clear feed</button> : null}</header>
    {entries.length ? <div className="command-feed-list" aria-live="polite">{entries.map(entry => <article className={`command-feed-entry ${entry.status}`} key={entry.requestId}>
      <span className="command-status"><StatusIcon status={entry.status} /></span>
      <div className="command-feed-copy"><div className="command-feed-title"><strong>{entry.actionName}</strong><span>{entry.status === "pending" ? "Waiting for game" : entry.status === "succeeded" ? "Succeeded" : "Failed"}</span></div>
        {entry.messages.length ? entry.messages.map((message, index) => <p key={index}>{message}</p>) : <p>Command accepted and queued for Bannerlord.</p>}
        <small><Clock3 /> {new Date(entry.completedAt ?? entry.submittedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit" })}</small>
      </div>
    </article>)}</div> : <div className="empty-command-feed"><ScrollText /><h3>No commands yet</h3><p>Actions you send from the Extension will appear here with their success or failure response.</p></div>}
    <footer className="private-note">This feed is visible only to you and keeps the latest 100 commands for this overlay session.</footer>
  </section>;
}
