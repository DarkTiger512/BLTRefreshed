import { Flag, HeartPulse, ShieldPlus, Swords, UserPlus, Zap } from "lucide-react";
import { useEffect, useRef, useState, type ComponentType, type CSSProperties, type SVGProps } from "react";
import type { GameState, ManifestAction, ViewerIdentity } from "../types";

interface Props {
  actions: ManifestAction[];
  identity: ViewerIdentity;
  mission: GameState["mission"];
  cooldowns: Record<string, number>;
  busy: boolean;
  onRequestIdentity(): void;
  onSubmit(action: ManifestAction, args: Record<string, unknown>): void;
}

const order = ["command.summon", "command.attack", "command.heal", "command.power", "command.formation"];
const icons: Record<string, ComponentType<SVGProps<SVGSVGElement>>> = {
  "command.summon": UserPlus,
  "command.attack": Swords,
  "command.heal": HeartPulse,
  "command.power": Zap,
  "command.formation": Flag,
};

function commandLabel(action: ManifestAction) {
  return action.legacyName.charAt(0).toUpperCase() + action.legacyName.slice(1);
}

export function BattleCommandStrip({ actions, identity, mission, cooldowns, busy, onRequestIdentity, onSubmit }: Props) {
  const [tooltipId, setTooltipId] = useState<string>();
  const [formationOpen, setFormationOpen] = useState(false);
  const timer = useRef<number | undefined>(undefined);
  const strip = useRef<HTMLDivElement>(null);
  const battleActions = order.map(id => actions.find(action => action.id === id)).filter((action): action is ManifestAction => Boolean(action));
  const formation = battleActions.find(action => action.id === "command.formation");
  const formationOptions = formation?.inputs.find(input => input.id === "formation")?.options ?? [];

  useEffect(() => {
    function dismiss(event: PointerEvent) {
      if (formationOpen && !strip.current?.contains(event.target as Node)) setFormationOpen(false);
    }
    function escape(event: KeyboardEvent) {
      if (event.key === "Escape") setFormationOpen(false);
    }
    document.addEventListener("pointerdown", dismiss);
    document.addEventListener("keydown", escape);
    return () => {
      document.removeEventListener("pointerdown", dismiss);
      document.removeEventListener("keydown", escape);
      if (timer.current) window.clearTimeout(timer.current);
    };
  }, [formationOpen]);

  function showLater(id: string) {
    if (timer.current) window.clearTimeout(timer.current);
    timer.current = window.setTimeout(() => setTooltipId(id), 450);
  }
  function hideTooltip() {
    if (timer.current) window.clearTimeout(timer.current);
    setTooltipId(undefined);
  }
  function activate(action: ManifestAction, blocked: boolean) {
    setTooltipId(undefined);
    if (blocked) return;
    if (!identity.linked) { onRequestIdentity(); return; }
    if (action.id === "command.formation") { setFormationOpen(value => !value); return; }
    setFormationOpen(false);
    onSubmit(action, {});
  }

  return <div className="battle-command-strip" ref={strip} aria-label="Battle commands">
    {battleActions.map(action => {
      const Icon = icons[action.id] ?? ShieldPlus;
      const reason = mission.actionAvailability[action.id];
      const cooldown = cooldowns[action.id] ?? 0;
      const blocked = busy || Boolean(reason) || cooldown > 0;
      const label = commandLabel(action);
      const tooltipVisible = tooltipId === action.id;
      return <div className={`battle-command-slot command-${action.legacyName}`} key={action.id}>
        <button type="button" className="battle-command-button" aria-label={`${label}${reason ? ` unavailable: ${reason}` : cooldown > 0 ? ` cooldown ${Math.ceil(cooldown)} seconds` : ""}`} aria-disabled={blocked} aria-expanded={action.id === "command.formation" ? formationOpen : undefined} onClick={() => activate(action, blocked)} onMouseEnter={() => showLater(action.id)} onMouseLeave={hideTooltip} onFocus={() => setTooltipId(action.id)} onBlur={event => { if (!event.currentTarget.parentElement?.contains(event.relatedTarget)) hideTooltip(); }}>
          <Icon aria-hidden="true" /><span>{label}</span>{cooldown > 0 ? <i className="command-cooldown" style={{ "--cooldown": `${Math.min(100, cooldown)}%` } as CSSProperties}>{Math.ceil(cooldown)}</i> : null}
        </button>
        {tooltipVisible ? <div className="battle-command-tooltip" role="tooltip"><strong>{label}</strong><p>{action.description}</p><span className={blocked ? "blocked" : "ready"}>{reason ?? (cooldown > 0 ? `Cooldown · ${Math.ceil(cooldown)}s` : "Ready")}</span></div> : null}
        {action.id === "command.formation" && formationOpen ? <div className="formation-popover" role="dialog" aria-label="Choose formation"><strong>Formation</strong>{formationOptions.map(option => <button type="button" key={option.value} onClick={() => { onSubmit(action, { formation: option.value }); setFormationOpen(false); }}><Flag aria-hidden="true" />{option.label}</button>)}</div> : null}
      </div>;
    })}
  </div>;
}
