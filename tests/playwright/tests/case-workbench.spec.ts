import { clickMudTab, expect, fillByTestId, goTo, selectMudOption, test } from './lifecore-fixtures';

test.describe('CaseWorkbench', () => {
  test('pending requirements case shows APS risk and blocks decision without override', async ({ page }) => {
    await goTo(page, '/cases/UW100001', 'case-header');

    await expect(page.locator("[data-testid='oob-alert'][data-field='Bmi']")).toBeVisible();
    await clickMudTab(page, 'tab-requirements', 'Requirements');
    await expect(page.getByTestId('requirements-grid')).toBeVisible();
    await expect(page.locator("[data-testid='requirement-row'][data-type='Aps']")).toBeVisible();

    await fillByTestId(page, 'decision-reason', 'E2E validation should block outstanding APS without override');
    const responsePromise = page.waitForResponse(r => r.url().includes('/api/v1/cases/UW100001/decision') && r.status() === 422);
    await page.getByTestId('decision-submit').click();
    await responsePromise;
    await expect(page.getByTestId('decision-error')).toBeVisible();
    await expect(page.getByTestId('decision-error')).toContainText(/outstanding/i);
  });

  test('decisionable case can be decisioned successfully', async ({ page }) => {
    await goTo(page, '/cases/UW100002', 'case-header');

    await selectMudOption(page, 'decision-risk-class', 'Standard');
    await fillByTestId(page, 'decision-reason', 'Meets standard underwriting guidelines');
    await fillByTestId(page, 'decision-note', 'Automated E2E decision flow');
    const responsePromise = page.waitForResponse(r => r.url().includes('/api/v1/cases/UW100002/decision') && r.status() >= 200 && r.status() < 300);
    await page.getByTestId('decision-submit').click();
    await responsePromise;

    await expect(page.getByTestId('decision-success')).toBeVisible();
    await expect(page.getByTestId('case-header')).toContainText('Decisioned');
    await clickMudTab(page, 'tab-notes', 'Notes');
    await expect(page.getByTestId('note-item').first()).toBeVisible();
  });

  test('workbench tabs switch to expected panels', async ({ page }) => {
    await goTo(page, '/cases/UW100001', 'case-header');

    await clickMudTab(page, 'tab-requirements', 'Requirements');
    await expect(page.getByTestId('requirements-grid')).toBeVisible();

    await clickMudTab(page, 'tab-risk', 'Risk');
    await expect(page.getByTestId('suggested-risk-class')).toBeVisible();

    await clickMudTab(page, 'tab-notes', 'Notes');
    await expect(page.getByTestId('note-input')).toBeVisible();

    await clickMudTab(page, 'tab-overview', 'Overview');
    await expect(page.locator("[data-testid='oob-alert']").first()).toBeVisible();
  });

  test('not found case shows not found state', async ({ page }) => {
    await page.goto('/cases/UW999999', { waitUntil: 'domcontentloaded' });
    await expect(page.getByTestId('case-not-found')).toBeVisible({ timeout: 30_000 });
    await expect(page.getByTestId('case-not-found')).toContainText('UW999999');
  });
});
