import { useEffect, useState } from "react";
import type { CommandActivity, ViewerState } from "../types";
import { useI18n } from "../i18n";
import "./prestige.css";

interface Props {
  viewer: ViewerState; connected: boolean; linked: boolean; busy: boolean;
  activity: CommandActivity[]; onBack(): void;
  onCommand(command: string): Promise<string | undefined>;
}

export function PrestigeView({ viewer, connected, linked, busy, activity, onBack, onCommand }: Props) {
  const { t, number } = useI18n();
  const [preview, setPreview] = useState<{ perk: string; requestId: string; expires: number }>();
  const [confirmId, setConfirmId] = useState<string>();
  const [sending, setSending] = useState(false);
  const [now, setNow] = useState(Date.now);
  useEffect(() => {
    if (!preview) return;
    const timer = window.setInterval(() => setNow(Date.now()), 500);
    return () => window.clearInterval(timer);
  }, [preview]);
  const prestige = viewer.prestige;
  const previewResult = activity.find(entry => entry.requestId === preview?.requestId);
  const confirmResult = activity.find(entry => entry.requestId === confirmId);
  const available = connected && linked && viewer.adopted && prestige?.eligible === true;
  const selected = prestige?.perks.find(perk => perk.id === preview?.perk);
  const waiting = sending || busy || previewResult?.status === "pending" || confirmResult?.status === "pending";
  const canConfirm = available && !waiting && !confirmId && previewResult?.status === "succeeded"
    && selected && selected.rank < selected.cap && preview && now < preview.expires;

  async function handleChoose(perk: string) {
    setSending(true); setPreview(undefined); setConfirmId(undefined);
    // eslint-disable-next-line react-hooks/purity -- This async function runs exclusively on a user click, never during render.
    const expires = Date.now() + 60_000;
    try {
      const requestId = await onCommand(`prestige choose ${perk}`);
      if (requestId) setPreview({ perk, requestId, expires });
    } finally { setSending(false); }
  }
  async function handleConfirm() {
    if (!canConfirm || !preview) return;
    setSending(true);
    try { const requestId = await onCommand(`prestige confirm ${preview.perk}`); if (requestId) setConfirmId(requestId); }
    finally { setSending(false); }
  }

  return <section className="prestige-view" aria-label={t("prestige.title")}>
    <header><button type="button" onClick={onBack}>{t("prestige.back")}</button><h2>{t("prestige.title")}</h2></header>
    {!connected ? <p role="status">{t("prestige.offline")}</p> : !linked ? <p>{t("prestige.identity")}</p>
      : !viewer.adopted ? <p>{t("prestige.adopt")}</p> : null}
    {!prestige ? <p>{t("prestige.unavailable")}</p> : <>
      <div className="prestige-heading"><strong>{viewer.heroName}</strong><span>{t("prestige.rank")} {prestige.count}/{prestige.maximum}</span></div>
      <div className="prestige-progress">
        <label>{t("prestige.kills")}<strong>{number(prestige.runKills)} / {number(prestige.requiredKills)}</strong><progress aria-label={t("prestige.kills")} max={Math.max(1, prestige.requiredKills)} value={prestige.runKills} /></label>
        <label>{t("prestige.gold")}<strong>{number(viewer.gold ?? 0)} / {number(prestige.requiredGold)}</strong><progress aria-label={t("prestige.gold")} max={Math.max(1, prestige.requiredGold)} value={viewer.gold ?? 0} /></label>
      </div>
      {prestige.blockingReason ? <p role="status">{prestige.blockingReason}</p> : null}
      <p>{t("prestige.permanence")}</p>
      <div className="prestige-perks">{prestige.perks.map(perk => <button type="button" key={perk.id}
        disabled={!available || waiting || perk.rank >= perk.cap} aria-pressed={selected?.id === perk.id}
        onClick={() => void handleChoose(perk.id)}><strong>{perk.name}<span>{perk.rank}/{perk.cap}</span></strong><small>{perk.description}</small></button>)}</div>
      <aside className="prestige-reset"><h3>{t("prestige.reset")}</h3><p>{prestige.resetSummary}</p></aside>
      {preview ? <div className="prestige-confirm" role="group" aria-label={t("prestige.confirmation")}>
        <strong>{selected?.name}</strong>
        <p role="status">{confirmResult ? confirmResult.messages.join(" ") || t("prestige.waiting") : previewResult?.status === "failed" ? previewResult.messages.join(" ")
          : now >= preview.expires ? t("prestige.expired") : previewResult?.status === "succeeded" ? t("prestige.ready") : t("prestige.waiting")}</p>
        <button type="button" disabled={!canConfirm} onClick={() => void handleConfirm()}>{t("prestige.confirm")}</button>
        <button type="button" disabled={waiting} onClick={() => { setPreview(undefined); setConfirmId(undefined); }}>{t("prestige.cancel")}</button>
      </div> : null}
    </>}
  </section>;
}
