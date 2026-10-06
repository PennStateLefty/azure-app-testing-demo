import { AzureCliCredential, DefaultAzureCredential } from '@azure/identity';
import { createAzurePlaywrightConfig, ServiceAuth } from '@azure/playwright';
import { defineConfig } from '@playwright/test';
import playwrightConfig from './playwright.config';

const tenantId = process.env.AZURE_TENANT_ID;
const credential = tenantId ? new AzureCliCredential({ tenantId }) : new DefaultAzureCredential();

export default defineConfig(
  playwrightConfig,
  createAzurePlaywrightConfig(playwrightConfig, {
    credential,
    serviceAuthType: ServiceAuth.ENTRA_ID,
    exposeNetwork: process.env.PLAYWRIGHT_SERVICE_EXPOSE_NETWORK ?? '<loopback>'
  }),
  {
    reporter: [
      ['html', { open: 'never' }],
      ['@azure/playwright/reporter'],
      ['list']
    ]
  }
);
