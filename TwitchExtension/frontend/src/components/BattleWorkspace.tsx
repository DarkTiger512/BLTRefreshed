import { Activity, ChevronRight, Crosshair, Search, Shield, Skull, Swords, Users, X, Zap } from "lucide-react";
import { memo, useDeferredValue, useMemo, useState } from "react";
import type { GameState, ManifestAction, MissionCombatant, ViewerIdentity } from "../types";
import { ActionDetail } from "./ActionDetail";

interface Props {
  mission: GameState["mission"];
  actions: ManifestAction[];
  identity: ViewerIdentity;
  cooldowns: Record<string, number>;
  selectors: GameState["selectors"];
  busy: boolean;
  error?: string;
  onRequestIdentity(): void;
  onSubmit(action: ManifestAction, args: Record<string, unknown>): void;
}

const healthPercent = (hero: MissionCombatant) => Math.max(0, Math.min(100, hero.maxHp ? hero.hp * 100 / hero.maxHp : 0));
const powerPercent = (hero: MissionCombatant) => Math.round(Math.max(0, Math.min(1, hero.activePowerFractionRemaining ?? 0)) * 100);
const sideName = (hero: MissionCombatant, tournament: boolean) => tournament ? `Team ${hero.tournamentTeam + 1}` : hero.isPlayerSide ? "Streamer side" : "Opposing side";

const RosterCard = memo(function RosterCard({ hero, tournament }: { hero: MissionCombatant; tournament: boolean }) {
  return <article className={`combatant-card ${hero.state !== "active" ? "inactive" : ""}`}>
    <div className="combatant-heading"><div><small>{sideName(hero, tournament)}</small><strong>{hero.name}</strong></div><span className={`combatant-state ${hero.state}`}>{hero.state}</span></div>
    <div className="health-track" aria-label={`${hero.name} health ${Math.round(hero.hp)} of ${Math.round(hero.maxHp)}`}><i style={{ width: `${healthPercent(hero)}%` }} /></div>
    <div className="health-copy"><span>{Math.round(hero.hp)} / {Math.round(hero.maxHp)} HP</span>{hero.ammoMaximum > 0 ? <span><Crosshair />{hero.ammoCurrent}/{hero.ammoMaximum}</span> : null}</div>
    <div className="combatant-stats"><span><Swords />{hero.kills} kills</span><span><Users />{hero.retinue + hero.eliteRetinue} retinue</span><span><Skull />{hero.deadRetinue + hero.deadEliteRetinue} fallen</span><span><Shield />+{hero.retinueKills}</span></div>
  </article>;
});

function HeroHud({ hero, tournament }: { hero: MissionCombatant; tournament: boolean }) {
  const power = powerPercent(hero);
  const powerActive = hero.activePowerActive ?? power > 0;
  return <article className={`hero-hud ${hero.state !== "active" ? "inactive" : ""}`}>
    <div className="hero-hud-heading"><div><small>{sideName(hero, tournament)}</small><strong>{hero.name}</strong></div><span className={`combatant-state ${hero.state}`}>{hero.state}</span></div>
    <div className="health-track hero-health" aria-label={`${hero.name} health ${Math.round(hero.hp)} of ${Math.round(hero.maxHp)}`}><i style={{ width: `${healthPercent(hero)}%` }} /></div>
    <div className="health-copy"><span>{Math.round(hero.hp)} / {Math.round(hero.maxHp)} HP</span>{hero.ammoMaximum > 0 ? <span><Crosshair />{hero.ammoCurrent}/{hero.ammoMaximum} ammo</span> : null}</div>
    <div className="power-status"><div className="power-label"><span><Zap />{hero.activePowerName || "Power"}</span><b>{powerActive ? `${power}%` : "Ready"}</b></div><div className="power-track" aria-label={`${hero.activePowerName || "Power"} ${powerActive ? `${power} percent remaining` : "ready"}`}><i style={{ width: `${powerActive ? power : 0}%` }} /></div></div>
    <div className="hero-metrics"><span>Cooldown <b>{Math.ceil(hero.cooldownSecondsRemaining)}s</b></span><span>Kills <b>{hero.kills}</b></span><span>Retinue <b>{hero.retinue + hero.eliteRetinue}</b></span><span>Gold <b>{hero.goldEarned.toLocaleString()}</b></span><span>XP <b>{hero.xpEarned.toLocaleString()}</b></span></div>
  </article>;
}

export function BattleWorkspace({ mission, actions, identity, cooldowns, selectors, busy, error, onRequestIdentity, onSubmit }: Props) {
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [query, setQuery] = useState("");
  const deferredQuery = useDeferredValue(query.trim().toLocaleLowerCase());
  const ownIndex = mission.combatants.findIndex(hero => hero.name.localeCompare(identity.displayName, undefined, { sensitivity: "accent" }) === 0);
  const ownHero = ownIndex >= 0 ? mission.combatants[ownIndex] : undefined;
  const groups = useMemo(() => mission.combatants.reduce((result, hero, index) => {
    if (index === ownIndex) return result;
    const key = sideName(hero, mission.kind === "tournament");
    const group = result.get(key);
    if (group) group.push(hero); else result.set(key, [hero]);
    return result;
  }, new Map<string, MissionCombatant[]>()), [mission.combatants, mission.kind, ownIndex]);
  const filteredActions = useMemo(() => deferredQuery ? actions.filter(action => `${action.legacyName} ${action.description}`.toLocaleLowerCase().includes(deferredQuery)).sort((left, right) => Number(!left.legacyName.toLocaleLowerCase().includes(deferredQuery)) - Number(!right.legacyName.toLocaleLowerCase().includes(deferredQuery))) : actions, [actions, deferredQuery]);

  return <section className="battle-workspace" aria-label="Live battle">
    <header className="battle-heading"><div className="battle-emblem"><Swords /></div><div><p>Live {mission.kind === "tournament" ? "tournament" : "battle"}</p><h2>BLT Combatants</h2></div><span><Activity />{mission.combatants.length} active</span></header>
    <section className="viewer-combatant"><h3>Your hero</h3>{ownHero ? <HeroHud hero={ownHero} tournament={mission.kind === "tournament"} /> : <div className="viewer-absent"><Shield /><div><strong>Your hero is not currently deployed</strong><span>Open mission commands to summon or attack.</span></div></div>}</section>
    <button className="open-battle-commands" type="button" onClick={() => setDrawerOpen(true)} aria-haspopup="dialog" aria-expanded={drawerOpen}><span><Swords /><span><strong>Mission commands</strong><small>{actions.length} available for this mission</small></span></span><ChevronRight /></button>
    <div className="battle-lower">
      <section className="battle-roster"><div className="battle-roster-heading"><h3>Battle roster</h3><span>{mission.combatants.length - (ownHero ? 1 : 0)} combatants</span></div><div className="team-groups">{Array.from(groups, ([name, heroes]) => <div className="team-group" key={name}><h4>{name}<span>{heroes.length}</span></h4><div className="combatant-grid">{heroes.map(hero => <RosterCard key={hero.id} hero={hero} tournament={mission.kind === "tournament"} />)}</div></div>)}</div></section>
      {drawerOpen ? <section className="battle-actions" role="dialog" aria-modal="false" aria-label="Mission commands"><div className="battle-command-toolbar"><div className="battle-actions-heading"><div><p>Available now</p><h3>Mission commands</h3></div><button type="button" onClick={() => setDrawerOpen(false)} aria-label="Close mission commands"><X /></button></div><label className="battle-command-search"><Search /><span className="sr-only">Search mission commands</span><input autoFocus value={query} onChange={event => setQuery(event.target.value)} placeholder="Search mission commands" /></label><span className="battle-phase">{mission.deploymentFinished ? "Battle underway" : "Deployment phase"}</span></div><div className="battle-action-grid">{filteredActions.map(action => <ActionDetail key={action.id} action={action} linked={identity.linked} unavailableReason={mission.actionAvailability[action.id] ?? (cooldowns[action.id] ? `Cooldown: ${Math.ceil(cooldowns[action.id])}s` : undefined)} busy={busy} error={error} selectors={selectors} onRequestIdentity={onRequestIdentity} onSubmit={args => onSubmit(action, args)} />)}{!filteredActions.length ? <div className="battle-command-empty">No mission commands match “{query}”.</div> : null}</div></section> : null}
    </div>
  </section>;
}
