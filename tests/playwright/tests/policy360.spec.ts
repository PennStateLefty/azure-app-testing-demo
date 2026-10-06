import { clickMudTab, expect, fillByTestId, goTo, test, urlRegex } from './lifecore-fixtures';

async function searchPolicies(page: import('@playwright/test').Page, query: string) {
  await fillByTestId(page, 'policy-search-input', query);
  await page.getByTestId('policy-search-submit').click();
  await expect(page.getByTestId('policy-results')).toBeVisible();
  await expect(page.getByTestId('policy-result-row').first()).toBeVisible({ timeout: 15_000 });
}

test.describe('Policy360', () => {
  test('search by policy prefix and owner last name opens policy', async ({ page }) => {
    await goTo(page, '/policies', 'policy-search-input');
    await searchPolicies(page, 'LC1000001');
    await expect(page.locator("[data-testid='policy-result-row'][data-policy-number='LC1000001']")).toBeVisible();

    const ownerName = await page.locator("[data-testid='policy-result-row'][data-policy-number='LC1000001'] td").nth(4).innerText();
    const lastName = ownerName.split(/\s+/).filter(Boolean).at(-1)!;
    await searchPolicies(page, lastName);
    await expect(page.getByTestId('policy-result-row').first()).toBeVisible();

    await page.locator("[data-testid='policy-result-row'][data-policy-number='LC1000001']").click();
    await expect(page.getByTestId('policy-header')).toBeVisible({ timeout: 30_000 });
    await expect(page).toHaveURL(urlRegex('/policies/LC1000001'));
  });

  test('policy tabs render expected content', async ({ page }) => {
    await goTo(page, '/policies/LC1000001', 'policy-header');

    await expect(page.getByTestId('coverages-grid')).toBeVisible();
    await expect(page.getByTestId('coverage-row').first()).toBeVisible();

    await clickMudTab(page, 'tab-parties', 'Parties');
    await expect(page.getByTestId('party-owner')).toBeVisible();
    await expect(page.getByTestId('party-insured')).toBeVisible();
    await expect(page.getByTestId('beneficiaries-grid')).toBeVisible();
    expect(await page.getByTestId('beneficiary-row').count()).toBe(2);

    await clickMudTab(page, 'tab-billing', 'Billing');
    await expect(page.getByTestId('billing-summary')).toBeVisible();
    await expect(page.getByTestId('payments-grid')).toBeVisible();

    await clickMudTab(page, 'tab-values', 'Values');
    await expect(page.getByTestId('values-summary')).toBeVisible();
  });

  test('beneficiary update shows validation when primary total is not 100', async ({ page }) => {
    await goTo(page, '/policies/LC1000001', 'policy-header');
    await page.getByTestId('action-change-beneficiary').click();
    await expect(page.getByTestId('beneficiary-percent').first()).toBeVisible();

    await page.getByTestId('beneficiary-remove-row').first().click();
    await expect(page.getByTestId('beneficiary-primary-total')).toContainText('must total 100');
    const responsePromise = page.waitForResponse(r => r.url().includes('/api/v1/policies/LC1000001/beneficiaries') && r.status() === 422);
    await page.getByTestId('beneficiary-submit').click();
    await responsePromise;
    await expect(page.getByTestId('beneficiary-error')).toBeVisible();
    await expect(page.getByTestId('beneficiary-error')).toContainText('100');
  });

  test('address change service action creates pending transaction', async ({ page }) => {
    await goTo(page, '/policies/LC1000001', 'policy-header');
    await page.getByTestId('action-change-address').click();

    await fillByTestId(page, 'address-line1', '100 E2E Test Ave');
    await fillByTestId(page, 'address-line2', 'Suite 10');
    await fillByTestId(page, 'address-city', 'Columbus');
    await fillByTestId(page, 'address-state', 'OH');
    await fillByTestId(page, 'address-postal', '43215');
    const responsePromise = page.waitForResponse(r => r.url().includes('/api/v1/policies/LC1000001/address') && r.status() >= 200 && r.status() < 300);
    await page.getByTestId('address-submit').click();
    await responsePromise;

    await clickMudTab(page, 'tab-transactions', 'Transactions');
    await expect(page.locator("[data-testid='transaction-row'][data-type='AddressChange'][data-status='Pending']").first()).toBeVisible();
  });

  test('quote service action returns net proceeds', async ({ page }) => {
    await goTo(page, '/policies/LC1000001', 'policy-header');
    await page.getByTestId('action-quote').click();
    await fillByTestId(page, 'quote-amount', '1000');
    const responsePromise = page.waitForResponse(r => r.url().includes('/api/v1/policies/LC1000001/quotes') && r.status() >= 200 && r.status() < 300);
    await page.getByTestId('quote-submit').click();
    await responsePromise;

    await expect(page.getByTestId('quote-result')).toBeVisible();
    await expect(page.getByTestId('quote-net-proceeds')).toContainText('$');
  });

  test('fixed indexed annuity values tab shows three funds', async ({ page }) => {
    await goTo(page, '/policies/LC1000002', 'policy-header');
    await clickMudTab(page, 'tab-values', 'Values');

    await expect(page.getByTestId('funds-grid')).toBeVisible();
    await expect(page.getByTestId('funds-chart')).toBeVisible();
    expect(await page.locator("[data-testid='funds-grid'] tbody tr").count()).toBe(3);
  });

  test('not found policy shows not found state', async ({ page }) => {
    await page.goto('/policies/LC9999999', { waitUntil: 'domcontentloaded' });
    await expect(page.getByTestId('policy-not-found')).toBeVisible({ timeout: 30_000 });
  });
});
