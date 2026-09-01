import { CircleHelp, Coins, PackageOpen, Search, Send, ShieldCheck, Users, X } from "lucide-react";
import { useDeferredValue, useMemo, useState } from "react";
import type { GameState, ManifestAction, RuntimeCommand, ViewerIdentity } from "../types";
import { LanguageSelector, useI18n } from "../i18n";

interface Props {
  actions: ManifestAction[];
  commands: RuntimeCommand[];
  identity: ViewerIdentity;
  state: GameState;
  busy: boolean;
  onExecute(commandLine: string): void;
  onInventory(): void;
  onRetinue(): void;
}

interface Suggestion { value: string; title: string; detail: string; unavailable?: string }

const syntaxFor = (action?: ManifestAction) => action?.inputs.map(input => input.required ? `<${input.label ?? input.id}>` : `[${input.label ?? input.id}]`).join(" ") ?? "";

export function CommandWorkspace({ actions, commands, identity, state, busy, onExecute, onInventory, onRetinue }: Props) {
  const { t, number } = useI18n();
  const [line, setLine] = useState("");
  const [active, setActive] = useState(0);
  const [helpOpen, setHelpOpen] = useState(false);
  const [helpQuery, setHelpQuery] = useState("");
  const deferredHelpQuery = useDeferredValue(helpQuery.trim().toLowerCase());
  const elevated = identity.roles.includes("moderator") || identity.roles.includes("broadcaster");
  const runtime = useMemo(() => commands.length ? commands : actions.map(action => ({ name: action.legacyName, handler: action.handler, help: action.description, moderatorOnly: !action.permissions.includes("viewer") && (action.permissions.includes("moderator") || action.permissions.includes("broadcaster")), hideHelp: action.hiddenFromHelp })), [actions, commands]);
  const visibleCommands = useMemo(() => runtime.filter(command => !command.moderatorOnly || elevated), [runtime, elevated]);
  const actionFor = (command: RuntimeCommand) => actions.find(action => action.legacyName.toLowerCase() === command.name.toLowerCase()) ?? actions.find(action => action.handler === command.handler);
  const normalized = line.trimStart().replace(/^!/, "");
  const separator = normalized.indexOf(" ");
  const typedCommand = (separator < 0 ? normalized : normalized.slice(0, separator)).toLowerCase();
  const typedArgument = separator < 0 ? "" : normalized.slice(separator + 1).toLowerCase();
  const selectedCommand = visibleCommands.find(command => command.name.toLowerCase() === typedCommand);
  const suggestions = useMemo<Suggestion[]>(() => {
    if (!normalized || separator < 0) return visibleCommands.filter(command => command.name.toLowerCase().startsWith(typedCommand)).slice(0, 6).map(command => {
      const action = actionFor(command); const unavailable = action ? state.unavailable[action.id] : undefined;
      return { value: `!${command.name}${syntaxFor(action) ? " " : ""}`, title: `!${command.name}`, detail: `${syntaxFor(action)}${syntaxFor(action) && command.help ? " · " : ""}${command.help}`, unavailable };
    });
    if (!selectedCommand) return [];
    const action = actionFor(selectedCommand); const input = action?.inputs[0];
    const values = input?.optionsSource ? state.selectors[input.optionsSource] : input?.options?.map(option => option.value) ?? [];
    return values.filter(value => value.toLowerCase().startsWith(typedArgument)).slice(0, 6).map(value => ({ value: `!${selectedCommand.name} ${value}`, title: value, detail: input?.label ?? t("command.argument") }));
  // actionFor is a stable lookup over current props; explicit primitive dependencies keep typing responsive.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [normalized, separator, typedCommand, typedArgument, selectedCommand, visibleCommands, actions, state.selectors, state.unavailable]);
  const helpCommands = visibleCommands.filter(command => !command.hideHelp || command.moderatorOnly).filter(command => {
    if (!deferredHelpQuery) return true;
    const action = actionFor(command); return `${command.name} ${command.help} ${action?.category ?? ""} ${syntaxFor(action)}`.toLowerCase().includes(deferredHelpQuery);
  });

  function complete(value: string) { setLine(value); setActive(0); }
  function submit() {
    const value = line.trim(); if (!value) return;
    if (value.replace(/^!/, "").toLowerCase() === "help") { setHelpOpen(true); setLine(""); return; }
    onExecute(value); setLine(""); setActive(0);
  }

  return <section className="command-workspace" aria-label={t("command.workspace")}>
    <div className="viewer-strip">
      <div className="viewer-chip"><span className="viewer-avatar">{identity.displayName.slice(0, 1).toUpperCase()}</span><span><strong>{identity.displayName}</strong><small><ShieldCheck />{identity.linked ? t("identity.shared") : t("identity.required")}</small></span></div>
      <div className="gold-balance"><Coins /><span><small>{t("gold.label")}</small><strong>{state.connected && state.viewer.adopted && typeof state.viewer.gold === "number" ? number(state.viewer.gold) : "—"}</strong></span><em>{!state.connected ? t("gold.offline") : !state.viewer.adopted ? t("gold.adopt") : state.viewer.heroName}</em></div>
      <LanguageSelector />
    </div>
    <div className="command-center">
      <form className="command-bar" onSubmit={event => { event.preventDefault(); submit(); }}>
        <span aria-hidden="true">!</span>
        <input value={line.replace(/^!/, "")} onChange={event => { setLine(event.target.value); setActive(0); }} onKeyDown={event => {
          if (event.key === "ArrowDown" && suggestions.length) { event.preventDefault(); setActive(index => (index + 1) % suggestions.length); }
          else if (event.key === "ArrowUp" && suggestions.length) { event.preventDefault(); setActive(index => (index - 1 + suggestions.length) % suggestions.length); }
          else if (event.key === "Tab" && suggestions.length) { event.preventDefault(); complete(suggestions[active]?.value ?? suggestions[0].value); }
          else if (event.key === "Escape") { setLine(""); setActive(0); }
          else if (event.key === "Enter") { event.preventDefault(); submit(); }
        }} placeholder={t("command.placeholder")} aria-label={t("command.line")} autoComplete="off" />
        <button type="button" onClick={() => setHelpOpen(true)} aria-label={t("command.help.open")}><CircleHelp /></button>
        <button type="submit" disabled={busy || !line.trim()} aria-label={t("command.run")}><Send /></button>
      </form>
      {line.trim() && suggestions.length ? <div className="command-suggestions" role="listbox" aria-label={t("command.suggestions")}>
        {suggestions.map((suggestion, index) => <button type="button" role="option" aria-selected={index === active} className={index === active ? "active" : ""} key={`${suggestion.value}-${index}`} onMouseDown={event => event.preventDefault()} onClick={() => complete(suggestion.value)}><strong>{suggestion.title}</strong><span>{suggestion.detail}</span>{suggestion.unavailable ? <em>{suggestion.unavailable}</em> : null}</button>)}
      </div> : null}
      <p className="command-hint">{t("command.hint")}</p>
    </div>
    <div className="native-shortcuts">
      <button onClick={onInventory}><PackageOpen /><span><strong>{t("shortcut.inventory")}</strong><small>{t("shortcut.inventory.detail")}</small></span></button>
      <button onClick={onRetinue}><Users /><span><strong>{t("shortcut.retinue")}</strong><small>{t("shortcut.retinue.detail")}</small></span></button>
    </div>
    {helpOpen ? <div className="command-help" role="dialog" aria-modal="true" aria-label={t("command.help.dialog")}>
      <header><div><h2>{t("command.help.title")}</h2><p>{t("command.help.available", { count: helpCommands.length })}</p></div><button onClick={() => setHelpOpen(false)} aria-label={t("common.close")}><X /></button></header>
      <label><Search /><input value={helpQuery} onChange={event => setHelpQuery(event.target.value)} placeholder={t("command.help.search")} autoFocus /></label>
      <div className="help-command-list">{helpCommands.map(command => { const action = actionFor(command); return <button key={command.name} onClick={() => { complete(`!${command.name}${syntaxFor(action) ? " " : ""}`); setHelpOpen(false); }}><span><strong>!{command.name}</strong><small>{action?.category ?? t("command.help.category")}</small></span><code>{syntaxFor(action)}</code><p>{command.help || action?.description}</p></button>; })}</div>
    </div> : null}
  </section>;
}
