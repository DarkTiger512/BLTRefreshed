import { CheckCircle2, ChevronDown, ChevronUp, CircleDot, Clock3, ScrollText, Trash2, XCircle } from "lucide-react";
import type { CommandActivity } from "../types";
import { useI18n } from "../i18n";

function StatusIcon({ status }: { status: CommandActivity["status"] }) {
  if (status === "succeeded") return <CheckCircle2 />;
  if (status === "failed") return <XCircle />;
  return <CircleDot className="activity-pending-icon" />;
}

function FeedEntry({ entry, compact }: { entry: CommandActivity; compact?: boolean }) {
  const { t, time } = useI18n();
  const message = entry.messages[0] ?? t("feed.waiting");
  return <article className={`command-feed-entry ${entry.status}${compact ? " compact" : ""}`}>
    <span className="command-status"><StatusIcon status={entry.status} /></span>
    <div className="command-feed-copy">
      <div className="command-feed-title"><strong>{entry.actionName}</strong><span>{entry.status === "pending" ? t("feed.pending") : entry.status === "succeeded" ? t("feed.succeeded") : t("feed.failed")}</span></div>
      <p>{message}</p>
      <small><Clock3 /> {time(entry.completedAt ?? entry.submittedAt)}</small>
    </div>
  </article>;
}

interface Props { entries: CommandActivity[]; expanded: boolean; onToggle(): void; onClear(): void }

export function CommandFeedView({ entries, expanded, onToggle, onClear }: Props) {
  const { t } = useI18n();
  const recent = entries.slice(0, 3);
  return <section className={expanded ? "command-feed-dock expanded" : "command-feed-dock"} aria-label={t("feed.label")}>
    <header className="command-feed-dock-header">
      <button className="feed-expand-button" onClick={onToggle} aria-expanded={expanded}><ScrollText /><span><strong>{t("feed.title")}</strong><small>{entries.length ? t(entries.length === 1 ? "feed.count" : "feed.count.other", { count: entries.length }) : t("feed.waitingFirst")}</small></span>{expanded ? <ChevronDown /> : <ChevronUp />}</button>
      {expanded && entries.length ? <button className="clear-command-feed" onClick={onClear}><Trash2 /> {t("feed.clear")}</button> : null}
    </header>
    {expanded ? <div className="command-feed-history">
      <div className="command-feed-history-title"><span className="eyebrow">{t("feed.history")}</span><h2>{t("feed.results")}</h2></div>
      {entries.length ? <div className="command-feed-list" aria-live="polite">{entries.map(entry => <FeedEntry entry={entry} key={entry.requestId} />)}</div> : <div className="empty-command-feed"><ScrollText /><h3>{t("feed.none.title")}</h3><p>{t("feed.none.detail")}</p></div>}
    </div> : <div className="command-feed-compact" aria-live="polite">
      {recent.length ? recent.map(entry => <FeedEntry entry={entry} compact key={entry.requestId} />) : <div className="compact-feed-empty"><CircleDot />{t("feed.empty")}</div>}
    </div>}
  </section>;
}
