# UI testing

LifeCore Suite end-to-end tests live in `tests/playwright`. They use the JavaScript/TypeScript Playwright runner (`@playwright/test`) so Azure App Testing Playwright Workspaces can upload portal reports with traces, screenshots, and videos through `@azure/playwright/reporter`. Selectors come from `docs/ui/testids-*.md`; the default local target is `BASE_URL=http://localhost:5119`.

## Run locally

```bash
cd tests/playwright
npm ci
npx playwright install chromium

# in another shell from repo root:
Admin__EnableReset=true dotnet run --project src/LifeCore.Web --urls http://localhost:5119

# after /health/ready is 200:
npx playwright test
```

If browser downloads are blocked, install Chrome or Edge locally and run with a matching Playwright project/channel update.

## Run with Azure App Testing Playwright Workspaces

1. Sign in to the tenant that owns the workspace:

```bash
az login --tenant <tenant-id>
```

2. Set the workspace endpoint and target app URL:

```bash
export AZURE_TENANT_ID="<tenant-id>"
export PLAYWRIGHT_SERVICE_URL="wss://<region>.api.playwright.microsoft.com/playwrightworkspaces/<workspace-id>/browsers"
export BASE_URL="https://<deployed-app-hostname>"
cd tests/playwright
npx playwright test -c playwright.service.config.ts --workers=20
```

`playwright.service.config.ts` uses `AzureCliCredential({ tenantId: process.env.AZURE_TENANT_ID })` when a tenant is provided, falls back to `DefaultAzureCredential`, and exposes `<loopback>` by default through `PLAYWRIGHT_SERVICE_EXPOSE_NETWORK`. The HTML reporter must remain before `@azure/playwright/reporter`; do not set a custom HTML output folder because the Azure reporter uploads the standard report.

The Playwright Workspace must have reporting enabled, a linked Storage account, and Storage Blob Data Contributor for every local or CI principal that runs tests. Trace viewing also requires Blob CORS for `https://trace.playwright.dev` with `GET` and `OPTIONS`.

> **Network security perimeter:** a tenant policy forces `publicNetworkAccess=Disabled` on storage, and Playwright reporting cannot upload through private endpoints. `infra/modules/playwright.bicep` therefore sets the reporting account to `publicNetworkAccess=SecuredByPerimeter` and associates it (Enforced mode) with a network security perimeter whose inbound rule allows the deployment subscription. The GitHub OIDC identity lives in that subscription, so CI uploads succeed; other callers (for example, a laptop) are denied. Subscription rules don't accept SAS requests, so if portal trace/report viewing is blocked, pass viewer egress CIDRs through the `reportViewerAddressPrefixes` parameter. NSP access logs (`NSPAccessLogs`) show the source IP and matched rule for troubleshooting.

## Categories

- `Smoke`: shell navigation, global search routing, footer disclaimer, and axe scans. Critical axe violations fail; serious violations are logged because MudBlazor defaults can produce framework-level issues.
- `Dashboard`: KPIs, worklist filtering/paging, row navigation, and persona switching.
- `CaseWorkbench`: pending requirements, blocked and successful decisions, tabs, and not-found state.
- `Policy360`: search, tab content, beneficiary validation, service actions, annuity funds, and not-found state.

Use Playwright grep filters, for example:

```bash
cd tests/playwright
npx playwright test --grep Smoke
```

## GitHub workflow

`.github/workflows/ui-tests.yml` uses GitHub OIDC (`azure/login`), installs the TypeScript suite with `npm ci`, then runs:

```bash
npx playwright test -c playwright.service.config.ts --workers=20
```

It sets `AZURE_TENANT_ID`, `PLAYWRIGHT_SERVICE_URL`, and `BASE_URL` from repository variables/workflow input, and uploads `tests/playwright/playwright-report` plus `tests/playwright/test-results` as the `playwright-report` artifact.
