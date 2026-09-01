import { useEffect, useState } from "react";
import { getIntegrationHealth } from "../api";
import { isLiveLocalIntegration } from "../environment";
import type { ViewerIdentity } from "../types";

export function IntegrationDiagnostics({ identity }: { identity: ViewerIdentity }) {
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
  return <div className="integration-diagnostics" aria-label="Local integration diagnostics">
    <span className={serviceConnected ? "ok" : "bad"}>Service {serviceConnected ? "online" : "offline"}</span>
    <span className={health?.gameConnected ? "ok" : "bad"}>Game {health?.gameConnected ? "connected" : "offline"}</span>
    <span>Channel {identity.channelId}</span>
    <span>State {health?.lastStateAt ? new Date(health.lastStateAt).toLocaleTimeString() : "none"}</span>
  </div>;
}
