import type { ViewerIdentity } from "./types";

const developmentIdentity: ViewerIdentity = {
  token: "development-token",
  channelId: "development-channel",
  userId: "development-user",
  displayName: "Rowan",
  roles: ["viewer"],
  linked: true,
};

export function authorizeViewer(): Promise<ViewerIdentity> {
  if (["127.0.0.1", "localhost"].includes(window.location.hostname) || !window.Twitch?.ext) return Promise.resolve(developmentIdentity);
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
