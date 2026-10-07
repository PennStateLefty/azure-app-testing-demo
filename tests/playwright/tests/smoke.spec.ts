import AxeBuilder from '@axe-core/playwright';
import { escapeRegExp, expect, fillByTestId, goTo, test, urlRegex } from './lifecore-fixtures';

test.describe('Smoke', () => {
  test('app loads, navigation works, and footer disclaimer is present', async ({ page }) => {
    await goTo(page, '/dashboard', 'worklist-grid');

    await expect(page.getByTestId('env-badge')).toHaveText('DEMO');
    await expect(page.getByTestId('footer-disclaimer')).toHaveText('Demo application. Synthetic data only.');
    await expect(page.getByTestId('brand-mark')).toHaveText('LC');
    await expect(page.getByTestId('brand-mark')).toHaveCSS('border-radius', '50%');
    await expect(page.locator('body')).not.toContainText(/accenture/i);
    await expect(page.locator('.lc-chevron')).toHaveCount(0);

    await page.getByRole('link', { name: 'LifeCore Suite dashboard' }).click();
    await expect(page).toHaveURL(urlRegex('/dashboard'));

    await page.getByTestId('nav-policies').click();
    await expect(page).toHaveURL(urlRegex('/policies'));
    await expect(page.getByTestId('policy-search-input')).toBeVisible();

    await page.getByTestId('nav-dashboard').click();
    await expect(page).toHaveURL(urlRegex('/dashboard'));
    await expect(page.getByTestId('worklist-grid')).toBeVisible();
  });

  const searchCases = [
    ['UW100001', '/cases/UW100001', 'case-header'],
    ['LC1000001', '/policies/LC1000001', 'policy-header'],
    ['smith', '/policies?query=smith', 'policy-results']
  ] as const;

  for (const [query, urlPart, readyTestId] of searchCases) {
    test(`global search routes by prefix: ${query}`, async ({ page }) => {
      await goTo(page, '/dashboard', 'worklist-grid');
      await fillByTestId(page, 'global-search', query);
      await page.getByTestId('global-search').press('Enter');

      await expect(page.getByTestId(readyTestId)).toBeVisible({ timeout: 30_000 });
      await expect(page).toHaveURL(urlRegex(escapeRegExp(urlPart).replace('\\?', '[?]')));
    });
  }

  const axeCases = [
    ['/dashboard', 'worklist-grid'],
    ['/cases/UW100001', 'case-header'],
    ['/policies/LC1000001', 'policy-header']
  ] as const;

  for (const [path, readyTestId] of axeCases) {
    test(`axe accessibility scan has no critical violations: ${path}`, async ({ page }) => {
      await goTo(page, path, readyTestId);
      const result = await new AxeBuilder({ page })
        .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
        .analyze();

      const critical = result.violations.filter(v => v.impact?.toLowerCase() === 'critical');
      const serious = result.violations.filter(v => v.impact?.toLowerCase() === 'serious');
      for (const violation of serious) {
        console.log(`Serious a11y violation (logged only): ${violation.id} - ${violation.help}`);
      }

      expect(critical, critical.map(v => `${v.id}: ${v.help}`).join('\n')).toEqual([]);
    });
  }
});
