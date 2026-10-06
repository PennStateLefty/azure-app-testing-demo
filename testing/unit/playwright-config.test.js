import { test } from 'node:test';
import assert from 'node:assert/strict';

test('Azure config uses the shared default HTML report folder and a deployed target', async () => {
  const previousBaseURL = process.env.BASE_URL;
  const previousServiceURL = process.env.PLAYWRIGHT_SERVICE_URL;
  process.env.BASE_URL = 'https://demo.example.com';
  process.env.PLAYWRIGHT_SERVICE_URL = 'wss://eastus.api.playwright.microsoft.com/playwrightworkspaces/00000000-0000-0000-0000-000000000000/browsers';
  try {
    const { default: config } = await import('../playwright/playwright.service.config.js');
    assert.equal(config.use.baseURL, 'https://demo.example.com');
    assert.deepEqual(config.webServer, []);
    assert.deepEqual(config.reporter[0], ['html', { open: 'never' }]);
    assert.deepEqual(config.reporter[1], ['@azure/playwright/reporter']);
  } finally {
    if (previousBaseURL === undefined) delete process.env.BASE_URL;
    else process.env.BASE_URL = previousBaseURL;
    if (previousServiceURL === undefined) delete process.env.PLAYWRIGHT_SERVICE_URL;
    else process.env.PLAYWRIGHT_SERVICE_URL = previousServiceURL;
  }
});
