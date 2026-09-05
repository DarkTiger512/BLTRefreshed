import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  testMatch: "balance.spec.ts",
  use: { channel: "chrome", baseURL: "http://127.0.0.1:5175", viewport: { width: 1440, height: 1000 } },
  webServer: {
    command: "npm run dev -- --port 5175", url: "http://127.0.0.1:5175", reuseExistingServer: true,
    env: { VITE_BLT_LIVE_INTEGRATION: "true", VITE_BLT_CHANNEL_ID: "prestige-test", VITE_BLT_API_URL: "http://127.0.0.1:5175" },
  },
});
