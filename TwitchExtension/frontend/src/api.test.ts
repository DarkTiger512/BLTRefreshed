import { afterEach, expect, test, vi } from "vitest";
import { applyPairingDecisions, createPairingCode, getConfigurationContext, revokeInstallation, submitAction } from "./api";
import type { ManifestAction, ViewerIdentity } from "./types";

const identity: ViewerIdentity = { token: "development-token", channelId: "42", userId: "9", displayName: "TestHero", roles: ["viewer"], linked: true };
const action = { id: "command.heal", legacyName: "heal", inputs: [] } as unknown as ManifestAction;

afterEach(() => { vi.unstubAllEnvs(); vi.unstubAllGlobals(); });

test("live local mode sends development-authenticated requests to the real service", async () => {
  vi.stubEnv("VITE_BLT_LIVE_INTEGRATION", "true");
  const fetch = vi.fn()
    .mockResolvedValueOnce({ ok: true, json: async () => ({ code: "BLT-1234-5678", expiresAt: new Date().toISOString() }) })
    .mockResolvedValueOnce({ ok: true, json: async () => ({ requestId: "request-1", status: "accepted" }) });
  vi.stubGlobal("fetch", fetch);
  expect((await createPairingCode(identity)).code).toBe("BLT-1234-5678");
  await submitAction(identity, action, {}, "request-1");
  expect(fetch).toHaveBeenCalledTimes(2);
  expect(fetch.mock.calls[0][0]).toContain("/api/channels/42/pairing");
  expect(fetch.mock.calls[1][0]).toContain("/api/channels/42/actions");
  expect(JSON.parse(fetch.mock.calls[1][1].body).requestId).toBe("request-1");
});

test("ordinary localhost mode retains deterministic UI mocks", async () => {
  vi.stubEnv("VITE_BLT_LIVE_INTEGRATION", "false");
  const fetch = vi.fn();
  vi.stubGlobal("fetch", fetch);
  expect((await createPairingCode(identity)).code).toBe("BLT-DEMO-PAIR");
  await submitAction(identity, action, {});
  expect(fetch).not.toHaveBeenCalled();
});

test("mock installation revocation persists across configuration refreshes", async () => {
  vi.stubEnv("VITE_BLT_LIVE_INTEGRATION", "false");
  const before = await getConfigurationContext(identity);
  await revokeInstallation(identity, before.installations[0].installationId);
  const after = await getConfigurationContext(identity);
  expect(after.installations[0].revokedAt).toBeTruthy();
  expect(after.gameConnected).toBe(false);
});

test("pairing decisions use their own revision-independent endpoint", async () => {
  vi.stubEnv("VITE_BLT_LIVE_INTEGRATION", "true");
  const fetch = vi.fn().mockResolvedValue({ ok: true });
  vi.stubGlobal("fetch", fetch);
  await applyPairingDecisions(identity, [{ requestId: "0e16493c-60d2-49c0-a19e-d27d6f3db063", decision: "approved" }]);
  expect(fetch.mock.calls[0][0]).toContain("/api/channels/42/pairing/decisions");
  expect(JSON.parse(fetch.mock.calls[0][1].body).pairingDecisions[0].decision).toBe("approved");
});
