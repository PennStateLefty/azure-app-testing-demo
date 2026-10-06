# Case Workbench test IDs

Route: `/cases/{CaseNumber}`. Well-known seed cases:

- `UW100001`: BMI > 30 out-of-bounds alert (`data-field="Bmi"`) and outstanding APS. Submitting a decision without `decision-override` should return a 422 validation alert in `decision-error`.
- `UW100002`: all required requirements are received. Submitting a populated decision should show `decision-success`, refresh the header status to Decisioned, and add/refetch notes.

## Page and header

| Test ID | Element | Notes |
| --- | --- | --- |
| `breadcrumb-dashboard` | Dashboard breadcrumb link | Click to return to `/dashboard`. |
| `case-loading` | Initial loading panel | Present while API calls are in flight. |
| `case-not-found` | Not-found alert | Shown when `GetCaseAsync` returns null. |
| `case-header` | Case header | Contains case #, applicant, DOB/age, product, face amount, agent, status, priority, days open, assigned underwriter, and requirement counts. |

## Tabs

| Test ID | Element | Notes |
| --- | --- | --- |
| `workbench-tabs` | MudTabs root | Contains all tab panels. |
| `tab-overview` | Overview tab header content | Click this span; it is rendered inside the MudBlazor clickable tab header and bubbles the click. |
| `tab-requirements` | Requirements tab header content | Same MudTabs pattern. |
| `tab-risk` | Risk Assessment tab header content | Same MudTabs pattern. |
| `tab-notes` | Notes & History tab header content | Same MudTabs pattern. |

Playwright example:

```ts
await page.getByTestId('tab-requirements').click();
await expect(page.getByTestId('requirements-grid')).toBeVisible();
```

## Overview

| Test ID | Element | Notes |
| --- | --- | --- |
| `oob-alert` | One out-of-bounds MudAlert | Has `data-field` with the contract field name, e.g. `Bmi` or `FaceAmount`. |

Example:

```ts
await page.goto('/cases/UW100001');
await expect(page.locator('[data-testid="oob-alert"][data-field="Bmi"]')).toBeVisible();
```

## Requirements

| Test ID | Element | Notes |
| --- | --- | --- |
| `requirements-grid` | Requirements table wrapper | Contains all rows. |
| `requirement-row` | Requirement row | Has `data-type` with enum value (`Aps`, `Labs`, `Rx`, `Mvr`, `Mib`, `Paramed`). |
| `order-requirement` | Opens/closes order form | Button. |
| `order-requirement-dialog` | Inline dialog/form | Role `dialog`. |
| `order-requirement-type` | MudSelect for type | Open then click an option by text. |
| `order-requirement-submit` | Confirm order button | Reloads requirements and header counts on success. |

MudSelect Playwright pattern:

```ts
await page.getByTestId('order-requirement').click();
await page.getByTestId('order-requirement-type').click();
await page.getByRole('option', { name: 'Labs' }).click();
await page.getByTestId('order-requirement-submit').click();
```

## Risk Assessment

| Test ID | Element | Notes |
| --- | --- | --- |
| `suggested-risk-class` | Highlight card | Displays friendly risk class. |

## Notes & History

| Test ID | Element | Notes |
| --- | --- | --- |
| `note-input` | Add note text area | Labeled `Add note`. |
| `note-add` | Add note button | Posts through `AddNoteAsync`. |
| `note-item` | Timeline item | One per note/history item. |

## AI summary

| Test ID | Element | Notes |
| --- | --- | --- |
| `ai-summary` | Summary card | Always visible beside/above tabs. |
| `ai-summary-text` | Summary text | Deterministic template-generated summary. |

## Decision panel

| Test ID | Element | Notes |
| --- | --- | --- |
| `decision-panel` | Decision card | Always visible beside/above tabs. |
| `decision-risk-class` | MudSelect of all `RiskClass` values | Friendly labels: Preferred Plus, Preferred, Standard Plus, Standard, Table 2 through Table 8, Decline. |
| `decision-reason` | Required reason input | Submit is disabled until populated. |
| `decision-note` | Optional note input | Sent with the decision. |
| `decision-override` | Override outstanding requirements checkbox | Required for `UW100001` success while APS is outstanding. |
| `decision-submit` | Submit button | Disabled while submitting. |
| `decision-error` | 422 validation alert | Lists `ApiResult.ErrorMessage`. |
| `decision-success` | Success alert | Shown after accepted decision. |

Decision MudSelect pattern:

```ts
await page.getByTestId('decision-risk-class').click();
await page.getByRole('option', { name: 'Standard' }).click();
await page.getByTestId('decision-reason').fill('Meets standard class guidelines');
await page.getByTestId('decision-submit').click();
```
