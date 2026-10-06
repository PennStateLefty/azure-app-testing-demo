import { defineConfig } from '@playwright/test';
import { createAzurePlaywrightConfig } from '@azure/playwright';
import { AzureCliCredential } from '@azure/identity';
import config from './playwright.config.js';

if (!process.env.BASE_URL || !process.env.PLAYWRIGHT_SERVICE_URL) {
  throw new Error('Azure tests require BASE_URL and PLAYWRIGHT_SERVICE_URL.');
}

export default defineConfig(
  config,
  createAzurePlaywrightConfig(config, {
    credential: new AzureCliCredential(),
    runName: process.env.PLAYWRIGHT_RUN_NAME,
  }),
  {
    reporter: [
      ['html', { open: 'never' }],
      ['@azure/playwright/reporter'],
      ['list'],
    ],
  },
);
