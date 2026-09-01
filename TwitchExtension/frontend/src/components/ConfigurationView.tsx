import { CheckCircle2, Copy, Link2, RotateCw } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { createPairingCode } from "../api";
import type { ViewerIdentity } from "../types";

export function ConfigurationView({ identity }: { identity: ViewerIdentity }) {
  const [code, setCode] = useState("Requesting…");
  const [expiresAt, setExpiresAt] = useState<string>();
  const [error, setError] = useState<string>();
  const [copied, setCopied] = useState(false);
  const refresh = useCallback(async () => {
    setError(undefined); setCode("Requesting…");
    try { const response = await createPairingCode(identity); setCode(response.code); setExpiresAt(response.expiresAt); }
    catch (reason) { setCode("Unavailable"); setError(reason instanceof Error ? reason.message : "Pairing is unavailable."); }
  }, [identity]);
  useEffect(() => { void refresh(); }, [refresh]);
  return <main className="configuration-view">
    <section className="configuration-card">
      <div className="config-emblem"><Link2 /></div><h1>Connect Bannerlord Twitch</h1>
      <p>Enter this short-lived pairing code in BLT Configure. The mod connects outbound to the managed service; no local web server or Extension secret is required.</p>
      <div className="pairing-code"><span>{code}</span><button aria-label="Copy pairing code" onClick={() => { navigator.clipboard?.writeText(code); setCopied(true); }}><Copy /></button></div>
      <div className="pairing-meta"><span>{expiresAt ? `Expires ${new Date(expiresAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}` : "Short-lived code"}</span><button onClick={() => { setCopied(false); void refresh(); }}><RotateCw />New code</button></div>
      {error ? <div role="alert" className="copied">{error}</div> : null}
      {copied ? <div className="copied"><CheckCircle2 />Pairing code copied</div> : null}
      <dl><div><dt>Channel</dt><dd>{identity.channelId}</dd></div><div><dt>Configuration</dt><dd>Managed by Twitch Extension</dd></div></dl>
    </section>
  </main>;
}
