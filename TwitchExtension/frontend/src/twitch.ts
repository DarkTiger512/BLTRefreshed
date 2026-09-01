import type { ViewerIdentity } from "./types";
import { developmentIdentityConfig, isLocalHost } from "./environment";

function developmentIdentity(): ViewerIdentity {
  const config = developmentIdentityConfig();
  return { token: "development-token", channelId: config.channelId, userId: config.userId,
    displayName: config.displayName, roles: [config.role], linked: true };
}

export function authorizeViewer(): Promise<ViewerIdentity> {
  if (isLocalHost() || !window.Twitch?.ext) return Promise.resolve(developmentIdentity());
  return new Promise(resolve => {
    window.Twitch?.ext.onAuthorized(auth => {
      const viewer = window.Twitch?.ext.viewer;
      const role = viewer?.role ?? "viewer";
      resolve({
        token: auth.token,
        channelId: auth.channelId,
        userId: auth.userId ?? null,
        displayName: viewer?.displayName ?? "Viewer",
        roles: [role],
        linked: Boolean(auth.userId && viewer?.isLinked),
      });
    });
  });
}

export function requestIdentity() {
  window.Twitch?.ext.actions.requestIdShare();
}
