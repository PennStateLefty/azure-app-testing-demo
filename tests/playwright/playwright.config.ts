import { defineConfig, devices } from '@playwright/test';

export const baseURL = (process.env.BASE_URL ?? 'http://localhost:5119').replace(/\/$/, '');

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  timeout: 60_000,
  expect: { timeout: 15_000 },
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL,
    viewport: { width: 1440, height: 1100 },
    ignoreHTTPSErrors: true,
    locale: 'en-US',
    trace: 'on',
    screenshot: 'on',
    video: 'retain-on-failure'
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] }
    }
  ]
});
