import { expect, test } from "@playwright/test";

test("viewer browses, searches, and submits an action", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { name: "Bannerlord Twitch" })).toBeVisible();
  await page.getByPlaceholder("Search actions").fill("ammo");
  await expect(page.getByText(/ammo/i).first()).toBeVisible();
  await page.getByText(/ammo/i).first().click();
  await page.getByRole("button", { name: /confirm action/i }).click();
  await expect(page.getByRole("status")).toContainText(/sent to your hero/i);
});
