import { useEffect, useState } from "react";
import { getIntegrationHealth } from "../api";
import { isLiveLocalIntegration } from "../environment";
import type { ViewerIdentity } from "../types";
import { useI18n } from "../i18n";

export function IntegrationDiagnostics({ identity }: { identity: ViewerIdentity }) {
  const { t, time } = useI18n();
  const [health, setHealth] = useState<{ gameConnected: boolean; lastStateAt?: string }>();
  const [serviceConnected, setServiceConnected] = useState(false);
  useEffect(() => {
    if (!isLiveLocalIntegration()) return;
    let active = true;
    const refresh = async () => {
      try { const value = await getIntegrationHealth(identity); if (active) { setHealth(value); setServiceConnected(true); } }
      catch { if (active) setServiceConnected(false); }
    };
    void refresh();
    const timer = window.setInterval(refresh, 3000);
    return () => { active = false; window.clearInterval(timer); };
  }, [identity]);
  if (!isLiveLocalIntegration()) return null;
  return <div className="integration-diagnostics" aria-label={t("diagnostics.label")}>
    <span className={serviceConnected ? "ok" : "bad"}>{t(serviceConnected ? "diagnostics.serviceOnline" : "diagnostics.serviceOffline")}</span>
    <span className={health?.gameConnected ? "ok" : "bad"}>{t(health?.gameConnected ? "diagnostics.gameConnected" : "diagnostics.gameOffline")}</span>
    <span>{t("diagnostics.channel", { id: identity.channelId })}</span>
    <span>{t("diagnostics.state", { time: health?.lastStateAt ? time(health.lastStateAt) : t("diagnostics.none") })}</span>
  </div>;
}
