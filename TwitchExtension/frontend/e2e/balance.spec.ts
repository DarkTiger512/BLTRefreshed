import { expect, test } from "@playwright/test";

// Browser plugin is unavailable; use repository Playwright with a mocked private socket.
test("balance offers, locked bonus, mission changes and offline compatibility", async ({ page }) => {
  const errors: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  const mission = { active: true, kind: "battle", revision: 1, deploymentFinished: true, combatants: [], actionAvailability: { "command.summon": null, "command.attack": null, "command.heal": null, "command.power": null, "command.formation": null },
    battleBalance: { missionId: "one", summoners: 8, attackers: 2, summonOffer: 0, attackOffer: .12 } };
  let send: (kind: string, data: object) => void = () => {};
  await page.route("https://extension-files.twitch.tv/**", route => route.fulfill({ body: "", contentType: "application/javascript" }));
  await page.route("https://fonts.googleapis.com/**", route => route.fulfill({ body: "", contentType: "text/css" }));
  await page.route("**/api/channels/*/health", route => route.fulfill({ json: { gameConnected: true, lastStateAt: new Date().toISOString() } }));
  await page.routeWebSocket(/\/ws\/viewer\//, socket => {
    send = (kind, data) => socket.send(JSON.stringify({ v: 1, channelId: "prestige-test", kind, data, timestamp: new Date().toISOString() }));
    send("state.snapshot", { connected: true, gameStarted: true, mission });
    send("viewer.state", { adopted: true, heroName: "Viewer", gold: 50000 });
  });
  await page.goto("/");
  await expect(page.getByText("Summon 8 / Attack 2", { exact: true })).toBeVisible();
  await expect(page.locator(".command-attack")).toContainText("Next join +12%");
  await expect(page.locator(".command-summon")).toContainText("Next join +0%");
  send("viewer.state", { adopted: true, heroName: "Viewer", battleBalance: { missionId: "one", lockedBonus: .05 } });
  await expect(page.getByText("Your locked bonus: +5% gold & XP", { exact: true })).toBeVisible();
  await page.screenshot({ path: "test-results/balance-desktop.png", fullPage: true });
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(page.getByText("Your locked bonus: +5% gold & XP", { exact: true })).toBeVisible();
  await page.screenshot({ path: "test-results/balance-mobile.png", fullPage: true });
  send("state.patch", { mission: { ...mission, revision: 2, battleBalance: { ...mission.battleBalance, missionId: "two" } } });
  await expect(page.getByText(/Your locked bonus/)).toHaveCount(0);
  send("state.patch", { mission: { ...mission, revision: 3, battleBalance: undefined } });
  await expect(page.locator(".battle-balance-summary")).toHaveCount(0);
  send("state.patch", { mission: { ...mission, revision: 4 } });
  await expect(page.locator(".battle-balance-summary")).toBeVisible();
  send("connection.status", { connected: false, gameStarted: true });
  await expect(page.locator(".battle-balance-summary")).toHaveCount(0);
  expect(errors).toEqual([]);
});
