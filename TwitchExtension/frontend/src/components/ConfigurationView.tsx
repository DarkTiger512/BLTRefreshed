import { CheckCircle2, Copy, Link2, RotateCw } from "lucide-react";
import { useState } from "react";
import type { ViewerIdentity } from "../types";

export function ConfigurationView({ identity }: { identity: ViewerIdentity }) {
  const [code, setCode] = useState("BLT-7K4M-92QF");
  const [copied, setCopied] = useState(false);
  return <main className="configuration-view">
    <section className="configuration-card">
      <div className="config-emblem"><Link2 /></div><h1>Connect Bannerlord Twitch</h1>
      <p>Enter this short-lived pairing code in BLT Configure. The mod connects outbound to the managed service; no local web server or Extension secret is required.</p>
      <div className="pairing-code"><span>{code}</span><button aria-label="Copy pairing code" onClick={() => { navigator.clipboard?.writeText(code); setCopied(true); }}><Copy /></button></div>
      <div className="pairing-meta"><span>Expires in 10 minutes</span><button onClick={() => { setCode(`BLT-${Math.random().toString(36).slice(2, 6).toUpperCase()}-${Math.random().toString(36).slice(2, 6).toUpperCase()}`); setCopied(false); }}><RotateCw />New code</button></div>
      {copied ? <div className="copied"><CheckCircle2 />Pairing code copied</div> : null}
      <dl><div><dt>Channel</dt><dd>{identity.channelId}</dd></div><div><dt>Configuration</dt><dd>Managed by Twitch Extension</dd></div></dl>
    </section>
  </main>;
}
