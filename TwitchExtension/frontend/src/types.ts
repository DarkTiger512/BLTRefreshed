export type Role = "viewer" | "moderator" | "broadcaster";
export type InputType = "text" | "integer" | "number" | "boolean" | "choice" | "hero" | "item" | "confirmation";

export interface ActionInput {
  id: string;
  type: InputType;
  required: boolean;
  label?: string;
  description?: string;
  options?: Array<{ value: string; label: string }>;
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
}

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
