import { expect, test } from "@playwright/test";

// Browser plugin not available: use the repository's Playwright runner with a mocked
// private game connection. This exercises UI/wire behavior, not a live campaign reset.
test("prestige preview, confirmation and offline state render on desktop and mobile", async ({ page }) => {
  const errors: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  page.on("console", message => { if (message.type() === "error") errors.push(message.text()); });
  const viewer = { adopted: true, heroName: "Viewer [P1]", gold: 7500000, prestige: {
    count: 1, maximum: 50, runKills: 750, requiredKills: 750, requiredGold: 7500000, eligible: true,
    resetSummary: "Resets level, XP, skills, attributes, focus, perks, gold, equipment, custom items, both retinues and achievement unlocks. Keeps identity, class, family, property, relationships and lifetime statistics. Starting gold: 50000; equipment tier: 1.",
    perks: [
      { id: "might", name: "Might", description: "+2% outgoing damage per rank", rank: 1, cap: 10 },
      { id: "resilience", name: "Resilience", description: "2% incoming damage reduction per rank", rank: 0, cap: 10 },
      { id: "vitality", name: "Vitality", description: "+5 maximum battle HP per rank", rank: 0, cap: 10 },
      { id: "fortune", name: "Fortune", description: "+2% battle gold per rank", rank: 0, cap: 10 },
      { id: "insight", name: "Insight", description: "+2% BLT skill XP per rank", rank: 0, cap: 10 },
    ] } };
  let send: (kind: string, data: object) => void = () => {};
  await page.route("https://extension-files.twitch.tv/**", route => route.fulfill({ body: "", contentType: "application/javascript" }));
  await page.route("https://fonts.googleapis.com/**", route => route.fulfill({ body: "", contentType: "text/css" }));
  await page.route("**/api/channels/*/health", route => route.fulfill({ json: { gameConnected: true, lastStateAt: new Date().toISOString() } }));
  await page.routeWebSocket(/\/ws\/viewer\//, socket => {
    send = (kind, data) => socket.send(JSON.stringify({ v: 1, channelId: "prestige-test", kind, data, timestamp: new Date().toISOString() }));
    send("state.snapshot", { connected: true, gameStarted: true, mission: { active: false, kind: "inactive", revision: 1, deploymentFinished: false, combatants: [], actionAvailability: {} } });
    send("viewer.state", viewer);
  });
  const commands: string[] = [];
  await page.route("**/api/channels/*/commands", async route => {
    const body = route.request().postDataJSON();
    commands.push(body.commandLine);
    await route.fulfill({ json: { requestId: body.requestId } });
    send("action.result", { requestId: body.requestId, messages: [body.commandLine.includes("choose") ? "Preview accepted" : "Prestige complete"] });
  });
  await page.goto("/");
  await expect(page).toHaveTitle(/Bannerlord|BLT/i);
  await page.getByRole("button", { name: /Prestige\s*Start again/ }).click();
  await expect(page.getByRole("heading", { name: "Prestige", exact: true })).toBeVisible();
  await expect(page.getByRole("progressbar", { name: "Personal battle kills" })).toHaveAttribute("value", "750");
  await page.getByRole("button", { name: /Might/ }).click();
  await expect(page.getByRole("button", { name: "Reset character and gain perk" })).toBeEnabled();
  await page.screenshot({ path: "test-results/prestige-desktop.png", fullPage: true });
  await page.getByRole("button", { name: "Reset character and gain perk" }).click();
  await expect(page.getByRole("group", { name: "Confirm prestige" })).toContainText("Prestige complete");
  expect(commands).toEqual(["prestige choose might", "prestige confirm might"]);
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(page.getByRole("heading", { name: "Prestige", exact: true })).toBeVisible();
  await page.screenshot({ path: "test-results/prestige-mobile.png", fullPage: true });
  send("connection.status", { connected: false, gameStarted: true });
  await expect(page.getByText("Bannerlord is offline. Reconnect before prestiging.")).toBeVisible();
  await expect(page.getByRole("button", { name: /Might/ })).toBeDisabled();
  expect(errors).toEqual([]);
});
