import { CheckCircle2, Copy, Link2, RotateCw } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { createPairingCode } from "../api";
import type { ViewerIdentity } from "../types";
import { LanguageSelector, useI18n } from "../i18n";

export function ConfigurationView({ identity }: { identity: ViewerIdentity }) {
  const { t, time } = useI18n();
  const [code, setCode] = useState(t("config.requesting"));
  const [expiresAt, setExpiresAt] = useState<string>();
  const [error, setError] = useState<string>();
  const [copied, setCopied] = useState(false);
  const refresh = useCallback(async () => {
    setError(undefined); setCode(t("config.requesting"));
    try { const response = await createPairingCode(identity); setCode(response.code); setExpiresAt(response.expiresAt); }
    catch (reason) { setCode(t("config.unavailable")); setError(reason instanceof Error ? reason.message : t("config.unavailable")); }
  }, [identity, t]);
  useEffect(() => { void refresh(); }, [refresh]);
  return <main className="configuration-view">
    <section className="configuration-card">
      <LanguageSelector /><div className="config-emblem"><Link2 /></div><h1>{t("config.title")}</h1>
      <p>{t("config.detail")}</p>
      <div className="pairing-code"><span>{code}</span><button aria-label={t("config.copy")} onClick={() => { navigator.clipboard?.writeText(code); setCopied(true); }}><Copy /></button></div>
      <div className="pairing-meta"><span>{expiresAt ? t("config.expires", { time: time(expiresAt) }) : t("config.shortLived")}</span><button onClick={() => { setCopied(false); void refresh(); }}><RotateCw />{t("config.new")}</button></div>
      {error ? <div role="alert" className="copied">{error}</div> : null}
      {copied ? <div className="copied"><CheckCircle2 />{t("config.copied")}</div> : null}
      <dl><div><dt>{t("config.channel")}</dt><dd>{identity.channelId}</dd></div><div><dt>{t("config.configuration")}</dt><dd>{t("config.managed")}</dd></div></dl>
    </section>
  </main>;
}
