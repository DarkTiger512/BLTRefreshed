export type Role = "viewer" | "moderator" | "broadcaster";
export type InputType = "text" | "integer" | "number" | "boolean" | "choice" | "hero" | "item" | "confirmation";

export interface ActionInput {
  id: string;
  type: InputType;
  required: boolean;
  label?: string;
  description?: string;
  options?: Array<{ value: string; label: string }>;
  optionsSource?: "cultures";
}

export interface ManifestAction {
  id: string;
  legacyName: string;
  handler: string;
  category: string;
  description: string;
  enabledByDefault: boolean;
  hiddenFromHelp: boolean;
  permissions: Role[];
  availability: string[];
  mutatesCampaign: boolean;
  inputs: ActionInput[];
}

export interface ActionManifest { protocolVersion: number; actions: ManifestAction[] }

export interface ViewerIdentity {
  token: string;
  channelId: string;
  userId: string | null;
  displayName: string;
  roles: Role[];
  linked: boolean;
}

export interface GameState {
  connected: boolean;
  gameStarted: boolean;
  unavailable: Record<string, string>;
  cooldowns: Record<string, number>;
  selectors: { cultures: string[] };
  mission: MissionState;
}

export type MissionKind = "inactive" | "battle" | "tournament";
export interface MissionCombatant {
  id: string;
  name: string;
  hp: number;
  maxHp: number;
  state: string;
  isPlayerSide: boolean;
  tournamentTeam: number;
  cooldownFractionRemaining: number;
  cooldownSecondsRemaining: number;
  activePowerFractionRemaining: number;
  activePowerName?: string;
  activePowerActive?: boolean;
  kills: number;
  retinue: number;
  deadRetinue: number;
  eliteRetinue: number;
  deadEliteRetinue: number;
  retinueKills: number;
  goldEarned: number;
  xpEarned: number;
  ammoCurrent: number;
  ammoMaximum: number;
}
export interface MissionState {
  active: boolean;
  kind: MissionKind;
  revision: number;
  deploymentFinished: boolean;
  combatants: MissionCombatant[];
  actionAvailability: Record<string, string | null>;
}

export interface InventoryItem { index: number; name: string; type: string; equipped: boolean }
export interface EquipmentSlot { id: string; label: string; accepts: string; itemName?: string; customItemIndex?: number }
export interface InventorySnapshot { heroName: string; limit: number; items: InventoryItem[]; slots: EquipmentSlot[]; updatedAt?: string }
export interface RetinueTroop { slot: number; name: string; tier: number; culture?: string }
export interface RetinueSnapshot { heroName: string; retinue: RetinueTroop[]; eliteRetinue: RetinueTroop[]; updatedAt?: string }
export type CommandActivityStatus = "pending" | "succeeded" | "failed";
export interface CommandActivity { requestId: string; actionId: string; actionName: string; status: CommandActivityStatus; submittedAt: string; completedAt?: string; messages: string[] }

declare global {
  interface Window {
    Twitch?: {
      ext: {
        onAuthorized(callback: (auth: { token: string; channelId: string; userId?: string }) => void): void;
        onContext(callback: (context: { mode?: string }) => void): void;
        actions: { requestIdShare(): void };
        viewer: { displayName?: string; role?: Role; isLinked?: boolean };
      };
    };
  }
}
