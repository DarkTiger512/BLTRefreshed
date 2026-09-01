import type { Role } from "./types";

const localHosts = new Set(["127.0.0.1", "localhost"]);

export const isLocalHost = () => localHosts.has(window.location.hostname);
export const isLiveLocalIntegration = () => isLocalHost() && String(import.meta.env.VITE_BLT_LIVE_INTEGRATION).toLowerCase() === "true";
export const developmentIdentityConfig = () => ({
  channelId: import.meta.env.VITE_BLT_CHANNEL_ID ?? "development-channel",
  userId: import.meta.env.VITE_BLT_VIEWER_ID ?? "development-user",
  displayName: import.meta.env.VITE_BLT_VIEWER_NAME ?? "Rowan",
  role: (import.meta.env.VITE_BLT_VIEWER_ROLE ?? "viewer") as Role,
});
