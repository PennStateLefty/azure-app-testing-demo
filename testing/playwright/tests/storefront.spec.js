import { test, expect } from '@playwright/test';

test('loads the backend catalog and updates the demo cart', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveTitle('Azure App Testing Demo');
  await expect(page.getByRole('status')).toHaveText('Products ready');
  await expect(page.getByRole('article')).toHaveCount(3);
  await page.getByRole('button', { name: 'Add Demo notebook', exact: true }).click();
  await page.getByRole('button', { name: 'Add Demo mug', exact: true }).click();
  await expect(page.locator('#cart')).toHaveText('2 items — $30.00');
  await page.getByRole('button', { name: 'Clear cart' }).click();
  await expect(page.locator('#cart')).toHaveText('0 items — $0.00');
});

test('shows an actionable message when the backend is unavailable', async ({ page }) => {
  await page.route('**/api/products', (route) => route.fulfill({
    status: 503,
    contentType: 'application/json',
    body: JSON.stringify({ error: 'Unavailable' }),
  }));
  await page.goto('/');
  await expect(page.getByRole('status')).toHaveText('Unable to load products. Please reload to try again.');
  await expect(page.getByRole('article')).toHaveCount(0);
});

test('deployed API is healthy and returns the expected catalog', async ({ request }) => {
  const health = await request.get('/api/health');
  expect(health.ok()).toBeTruthy();
  expect(await health.json()).toEqual({ status: 'ok' });
  const catalog = await request.get('/api/products');
  expect(catalog.ok()).toBeTruthy();
  expect(await catalog.json()).toEqual([
    { id: 1, name: 'Demo notebook', price: 12 },
    { id: 2, name: 'Demo mug', price: 18 },
    { id: 3, name: 'Demo backpack', price: 45 },
  ]);
});
