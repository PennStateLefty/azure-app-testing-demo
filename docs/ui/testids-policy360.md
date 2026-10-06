# Policy 360 test IDs

## Search (`/policies`)

- `policy-search-input` — search field labeled `Policy #, owner name or SSN last 4`.
- `policy-search-submit` — runs search.
- `policy-results` — results container.
- `policy-result-row` — clickable row; includes `data-policy-number`.
- `policy-results-next-page` — next server page.
- `policy-results-page-info` — current page and total count.
- `policy-results-empty` — pre-search, no-results, or empty state.

The page supports `/policies?query=LC1000001` and runs the search on load.

## Policy 360 (`/policies/{policyNumber}`)

- `policy-header` — header summary region.
- `policy-status` — status chip.
- `pending-transactions-count` — pending transaction badge.
- `policy-not-found` — not-found alert.
- `service-actions` — service action toolbar.
- `policy-tabs` — MudTabs root.
- Tab header spans: `tab-coverages`, `tab-parties`, `tab-billing`, `tab-values`, `tab-transactions`. The test ID is on a span inside the MudBlazor clickable tab header; Playwright can click it directly.

### Tab content

- Coverages: `coverages-grid`, row `coverage-row`.
- Parties: `party-owner`, `party-insured`, `beneficiaries-grid`, row `beneficiary-row`.
- Billing: `billing-summary`, `payments-grid`.
- Values: `values-summary`, optional `funds-grid`, optional `funds-chart` for fund policies.
- Transactions: `transactions-grid`, row `transaction-row`; rows include `data-type` and `data-status`.

## Service actions

### Change Address

- Button: `action-change-address`.
- Fields: `address-line1`, `address-line2`, `address-city`, `address-state`, `address-postal`.
- Submit: `address-submit`.
- Success refreshes header and transactions; expect a pending `AddressChange` transaction.

### Change Beneficiary

- Button: `action-change-beneficiary`.
- Row controls: `beneficiary-name`, `beneficiary-relationship`, `beneficiary-type`, `beneficiary-percent`, `beneficiary-remove-row`.
- Add row: `beneficiary-add-row`.
- Live total: `beneficiary-primary-total`.
- Server validation alert: `beneficiary-error`.
- Submit: `beneficiary-submit`.
- The UI shows a client-side total hint but always submits to the API so 422 `ProblemDetails` responses can be asserted.

### Quote Loan / Withdrawal

- Button: `action-quote`.
- Kind select: `quote-kind`.
- Amount: `quote-amount`.
- Submit: `quote-submit`.
- Result card: `quote-result`; net proceeds value wrapper: `quote-net-proceeds`.
- Server validation alert: `quote-error`.

## MudBlazor popover tips for Playwright

MudBlazor select options render in a popover outside the action drawer. Click the select first, then click the open popover item:

```ts
await page.getByTestId('beneficiary-type').click();
await page.locator('.mud-popover-open .mud-list-item:has-text("Contingent")').click();

await page.getByTestId('quote-kind').click();
await page.locator('.mud-popover-open .mud-list-item:has-text("Withdrawal")').click();
```

MudDialog is not used on this screen; service actions render as inline drawers. MudBlazor snackbars render in their normal provider region and do not have stable test IDs.

## Well-known seed data

- `LC1000001` — in-force Universal Life policy with two primary beneficiaries at 50/50 and cash value.
- `LC1000002` — in-force Fixed Indexed Annuity with three fund allocations; use it to assert `funds-grid` and `funds-chart`.
- Policy numbers range from `LC1000001` through `LC1010000`.
