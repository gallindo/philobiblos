import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: 'list',
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'], headless: true },
    },
  ],
  globalSetup: './e2e/setup.ts',
  webServer: {
    command: 'docker compose up --build -d',
    url: 'http://localhost:4200',
    timeout: 300_000,
    reuseExistingServer: true,
  },
  globalTeardown: './e2e/teardown.ts',
});
