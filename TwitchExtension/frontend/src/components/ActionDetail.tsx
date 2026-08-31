import { AlertCircle, CheckCircle2, Send, ShieldAlert } from "lucide-react";
import { useEffect, useState } from "react";
import type { ManifestAction } from "../types";
import { CommandIcon } from "./CommandIcon";

interface Props {
  action?: ManifestAction;
  linked: boolean;
  unavailableReason?: string;
  busy: boolean;
  error?: string;
  onRequestIdentity(): void;
  onSubmit(args: Record<string, unknown>): void;
}

const labelFor = (value: string) => value.replace(/([a-z])([A-Z])/g, "$1 $2").replace(/[._-]/g, " ").replace(/^./, c => c.toUpperCase());
const clean = (value: string) => value.replace(/^['"]?\{=[^}]+\}/, "").replace(/['"]$/, "");

export function ActionDetail({ action, linked, unavailableReason, busy, error, onRequestIdentity, onSubmit }: Props) {
  const [values, setValues] = useState<Record<string, unknown>>({});
  useEffect(() => setValues({}), [action?.id]);
  if (!action) return <section className="detail-panel empty-detail"><ShieldAlert /><h2>Select an action</h2><p>Browse the available interactions for this campaign.</p></section>;
  const blocked = Boolean(unavailableReason);
  return <section className="detail-panel">
    <header className="detail-header"><CommandIcon category={action.category} className="detail-icon" /><div><h2>{clean(action.legacyName)}</h2><p>{action.description}</p></div></header>
    <div className={blocked ? "detail-status blocked" : "detail-status ready"}>{blocked ? <AlertCircle /> : <CheckCircle2 />}{unavailableReason ?? "Available"}</div>
    <div className="detail-rule" />
    <form onSubmit={event => { event.preventDefault(); onSubmit(values); }}>
      {action.inputs.map(input => <label className="field" key={input.id}>
        <span>{input.label ?? labelFor(input.id)}{input.required ? " *" : ""}</span>
        {input.type === "boolean" || input.type === "confirmation" ?
          <input type="checkbox" checked={Boolean(values[input.id])} onChange={event => setValues(current => ({ ...current, [input.id]: event.target.checked }))} required={input.required} /> :
          input.type === "choice" && input.options ?
            <select value={String(values[input.id] ?? "")} onChange={event => setValues(current => ({ ...current, [input.id]: event.target.value }))} required={input.required}><option value="">Select an option</option>{input.options.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}</select> :
            <input
              type={input.type === "integer" || input.type === "number" ? "number" : "text"}
              value={String(values[input.id] ?? "")}
              onChange={event => setValues(current => ({ ...current, [input.id]: event.target.value }))}
              required={input.required}
            />}
        {input.description ? <small>{input.description}</small> : null}
      </label>)}
      {action.mutatesCampaign ? <div className="impact-note"><AlertCircle />This action can change the current campaign.</div> : null}
      {error ? <div className="form-error" role="alert">{error}</div> : null}
      {!linked ? <button type="button" className="primary-action" onClick={onRequestIdentity}>Share identity to interact</button> :
        <button className="primary-action" disabled={blocked || busy} type="submit"><Send />{busy ? "Sending…" : "Confirm action"}</button>}
    </form>
  </section>;
}
