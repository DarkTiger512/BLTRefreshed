import { Activity, Crosshair, Shield, Skull, Swords, Users } from "lucide-react";
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

const percent = (hero: MissionCombatant) => Math.max(0, Math.min(100, hero.maxHp ? hero.hp * 100 / hero.maxHp : 0));
const sideName = (hero: MissionCombatant, tournament: boolean) => tournament ? `Team ${hero.tournamentTeam + 1}` : hero.isPlayerSide ? "Streamer side" : "Opposing side";

function CombatantCard({ hero, featured = false, tournament = false }: { hero: MissionCombatant; featured?: boolean; tournament?: boolean }) {
  return <article className={`combatant-card ${featured ? "featured" : ""} ${hero.state !== "active" ? "inactive" : ""}`}>
    <div className="combatant-heading"><div><small>{sideName(hero, tournament)}</small><strong>{hero.name}</strong></div><span className={`combatant-state ${hero.state}`}>{hero.state}</span></div>
    <div className="health-track" aria-label={`${hero.name} health ${Math.round(hero.hp)} of ${Math.round(hero.maxHp)}`}><i style={{ width: `${percent(hero)}%` }} /></div>
    <div className="health-copy"><span>{Math.round(hero.hp)} / {Math.round(hero.maxHp)} HP</span>{hero.ammoMaximum > 0 ? <span><Crosshair />{hero.ammoCurrent}/{hero.ammoMaximum}</span> : null}</div>
    <div className="combatant-stats"><span><Swords />{hero.kills} kills</span><span><Users />{hero.retinue + hero.eliteRetinue} retinue</span><span><Skull />{hero.deadRetinue + hero.deadEliteRetinue} fallen</span><span><Shield />+{hero.retinueKills}</span></div>
    {featured ? <div className="featured-stats"><span>Cooldown <b>{Math.ceil(hero.cooldownSecondsRemaining)}s</b></span><span>Power <b>{Math.round(hero.activePowerFractionRemaining * 100)}%</b></span><span>Gold <b>{hero.goldEarned.toLocaleString()}</b></span><span>XP <b>{hero.xpEarned.toLocaleString()}</b></span></div> : null}
  </article>;
}

export function BattleWorkspace({ mission, actions, identity, cooldowns, selectors, busy, error, onRequestIdentity, onSubmit }: Props) {
  const ownIndex = mission.combatants.findIndex(hero => hero.name.localeCompare(identity.displayName, undefined, { sensitivity: "accent" }) === 0);
  const ownHero = ownIndex >= 0 ? mission.combatants[ownIndex] : undefined;
  const roster = mission.combatants.filter((_, index) => index !== ownIndex);
  const groups = roster.reduce((result, hero) => {
    const key = sideName(hero, mission.kind === "tournament");
    result.set(key, [...(result.get(key) ?? []), hero]);
    return result;
  }, new Map<string, MissionCombatant[]>());
  return <section className="battle-workspace" aria-label="Live battle">
    <header className="battle-heading"><div className="battle-emblem"><Swords /></div><div><p>Live {mission.kind === "tournament" ? "tournament" : "battle"}</p><h2>BLT Combatants</h2></div><span><Activity />Live · {mission.combatants.length} active BLTs</span></header>
    <div className="battle-scroll">
      <section className="viewer-combatant"><h3>Your hero</h3>{ownHero ? <CombatantCard hero={ownHero} featured tournament={mission.kind === "tournament"} /> : <div className="viewer-absent"><Shield /><div><strong>Your hero is not currently deployed</strong><span>Use an available summon action below to enter the battle.</span></div></div>}</section>
      <section className="battle-roster"><h3>Battle roster</h3><div className="team-groups">{Array.from(groups, ([name, heroes]) => <div className="team-group" key={name}><h4>{name}<span>{heroes.length}</span></h4><div className="combatant-grid">{heroes.map(hero => <CombatantCard key={hero.id} hero={hero} tournament={mission.kind === "tournament"} />)}</div></div>)}</div></section>
      <section className="battle-actions"><div className="battle-actions-heading"><div><p>Available now</p><h3>Mission commands</h3></div><span>{mission.deploymentFinished ? "Battle underway" : "Deployment phase"}</span></div><div className="battle-action-grid">{actions.map(action => <ActionDetail key={action.id} action={action} linked={identity.linked} unavailableReason={mission.actionAvailability[action.id] ?? (cooldowns[action.id] ? `Cooldown: ${Math.ceil(cooldowns[action.id])}s` : undefined)} busy={busy} error={error} selectors={selectors} onRequestIdentity={onRequestIdentity} onSubmit={args => onSubmit(action, args)} />)}</div></section>
    </div>
  </section>;
}
