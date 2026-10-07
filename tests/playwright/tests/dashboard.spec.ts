import { expect, fillByTestId, goTo, selectMudOption, test, urlRegex } from './lifecore-fixtures';

test.describe('Dashboard', () => {
  test('KPI tiles and worklist rows render', async ({ page }) => {
    await goTo(page, '/dashboard', 'worklist-grid');

    for (const testId of ['kpi-open-cases', 'kpi-pending-requirements', 'kpi-aps-pending', 'kpi-avg-days', 'kpi-sla-risk', 'kpi-decisions-today']) {
      await expect(page.getByTestId(testId)).toBeVisible();
      await expect(page.getByTestId(testId).getByTestId('kpi-value')).not.toHaveText('—');
    }

    await expect(page.getByTestId('worklist-row').first()).toBeVisible();
    expect(await page.getByTestId('worklist-row').count()).toBeGreaterThan(0);
  });

  test('filtering, paging, and view toggle work', async ({ page }) => {
    await goTo(page, '/dashboard', 'worklist-grid');

    await page.getByTestId('worklist-view-case').click();
    await expect(page.getByTestId('worklist-grid')).toContainText('Open tasks');

    await selectMudOption(page, 'worklist-filter-status', 'Pending Requirements');
    await expect(page.getByTestId('worklist-grid')).toContainText('Pending Requirements');

    await fillByTestId(page, 'worklist-search', 'UW100001');
    await expect(page.getByTestId('worklist-grid')).toContainText('UW100001', { timeout: 15_000 });

    await fillByTestId(page, 'worklist-search', '');
    const next = page.getByTestId('worklist-next-page');
    if (await next.isEnabled()) {
      const before = await page.getByTestId('worklist-page-info').innerText();
      await next.click();
      await expect(page.getByTestId('worklist-page-info')).not.toHaveText(before);
    }
  });

  test('clicking worklist row opens case workbench', async ({ page }) => {
    await goTo(page, '/dashboard', 'worklist-grid');
    await fillByTestId(page, 'worklist-search', 'UW100001');
    await expect(page.locator("[data-testid='worklist-row'][data-case-number='UW100001']").first()).toBeVisible({ timeout: 15_000 });

    await page.locator("[data-testid='worklist-row'][data-case-number='UW100001'] button").first().click();
    await expect(page.getByTestId('case-summary-panel')).toBeVisible();
    await page.getByTestId('case-summary-open-workbench').click();

    await expect(page.getByTestId('case-header')).toBeVisible({ timeout: 30_000 });
    await expect(page).toHaveURL(urlRegex('/cases/UW100001'));
  });

  test('persona switcher can select underwriter when present', async ({ page }) => {
    await goTo(page, '/dashboard', 'worklist-grid');
    const switcher = page.getByTestId('persona-switcher');
    test.skip((await switcher.count()) === 0, 'Persona switcher is not present in this deployment.');

    await switcher.click();
    await page.getByRole('option', { name: 'Dana Whitfield (Underwriter)', exact: true }).click();
    await expect(switcher).toContainText('Dana Whitfield');
  });

  test('persona switcher text is readable when closed', async ({ page }) => {
    await goTo(page, '/dashboard', 'worklist-grid');
    const selected = page.getByTestId('persona-switcher').locator('.mud-input-slot, .mud-select-input').first();
    await expect(selected).toHaveCSS('color', 'rgb(255, 255, 255)');
  });

  test('switching persona re-scopes KPIs and worklist', async ({ page }) => {
    await goTo(page, '/dashboard', 'worklist-grid');
    const openCases = page.getByTestId('kpi-open-cases').getByTestId('kpi-value');
    await expect(page.getByTestId('dashboard-scope-label')).toContainText('Dana Whitfield');
    await expect(openCases).not.toHaveText('—');
    const danaCount = await openCases.innerText();

    await page.getByTestId('persona-switcher').click();
    await page.getByRole('option', { name: 'Jordan Blake (CSR)', exact: true }).click();
    await expect(page.getByTestId('dashboard-scope-label')).toContainText('all underwriters');
    await expect(openCases).not.toHaveText(danaCount);

    await page.getByTestId('persona-switcher').click();
    await page.getByRole('option', { name: 'Marcus Lee (Underwriter)', exact: true }).click();
    await expect(page.getByTestId('dashboard-scope-label')).toContainText('Marcus Lee');
    await expect(page.locator('[data-testid="worklist-grid"] tbody')).not.toContainText('Dana Whitfield');
  });

  test('product config page lists catalog', async ({ page }) => {
    await goTo(page, '/dashboard', 'worklist-grid');
    await page.getByTestId('nav-product-config').click();
    await expect(page).toHaveURL(urlRegex('/products'));
    await expect(page.getByTestId('product-row')).toHaveCount(5);
    await expect(page.getByTestId('product-catalog')).toContainText('Universal Life');
  });
});
