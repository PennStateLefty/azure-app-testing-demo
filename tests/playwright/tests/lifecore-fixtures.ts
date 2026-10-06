import { expect, Page, test as base } from '@playwright/test';

export const test = base;
export { expect };

export async function goTo(page: Page, path: string, readyTestId: string) {
  await page.goto(path, { waitUntil: 'domcontentloaded' });
  await waitForLifeCore(page, readyTestId);
}

export async function waitForLifeCore(page: Page, readyTestId: string) {
  await expect(page.locator('#blazor-error-ui')).toBeHidden({ timeout: 20_000 });
  await expect(page.getByTestId(readyTestId)).toBeVisible({ timeout: 30_000 });
}

export async function fillByTestId(page: Page, testId: string, value: string) {
  const locator = page.getByTestId(testId).first();
  const input = locator.locator('input,textarea').first();
  if (await input.count()) await input.fill(value);
  else await locator.fill(value);
}

export async function selectMudOption(page: Page, testId: string, optionText: string) {
  await page.locator(`[data-testid='${testId}']:not(input[type='hidden'])`).click();
  await page.getByRole('option', { name: optionText, exact: true }).click();
}

export async function clickMudTab(page: Page, testId: string, name: string) {
  const tabByTestId = page.locator('[role="tab"]').filter({ has: page.getByTestId(testId) }).first();
  if (await tabByTestId.count()) {
    await tabByTestId.evaluate((element: HTMLElement) => element.click());
    return;
  }

  const tabByName = page.getByRole('tab', { name: new RegExp(name, 'i') }).first();
  if (await tabByName.count()) await tabByName.evaluate((element: HTMLElement) => element.click());
  else await page.getByTestId(testId).evaluate((element: HTMLElement) => element.click());
}

export function urlRegex(pattern: string) {
  return new RegExp(pattern, 'i');
}

export function escapeRegExp(value: string) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
