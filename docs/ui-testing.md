# UI testing

LifeCore Suite end-to-end tests live in `tests/LifeCore.PlaywrightTests`. They are NUnit tests built on `Microsoft.Playwright.NUnit`, use `data-testid` selectors from `docs/ui/testids-*.md`, and default to `BASE_URL=http://localhost:5119`.

## Run locally

```bash
dotnet build tests/LifeCore.PlaywrightTests/LifeCore.PlaywrightTests.csproj
pwsh tests/LifeCore.PlaywrightTests/bin/Debug/net10.0/playwright.ps1 install chromium
Admin__EnableReset=true dotnet run --project src/LifeCore.Web --urls http://localhost:5119
# in another shell, after /health/ready is 200:
dotnet test tests/LifeCore.PlaywrightTests/LifeCore.PlaywrightTests.csproj --settings tests/LifeCore.PlaywrightTests/.runsettings
```

If browser downloads are blocked, install Chrome or Edge locally and run with `BROWSER_CHANNEL=chrome` or `BROWSER_CHANNEL=msedge`.

## Run with Azure App Testing Playwright Workspaces

1. Sign in to the tenant that owns the workspace:

```bash
az login --tenant <tenant-id>
```

2. Set the workspace endpoint and target app URL:

```bash
export PLAYWRIGHT_SERVICE_URL="https://<region>.api.playwright.microsoft.com/..."
export BASE_URL="https://<deployed-app-hostname>"
```

The test setup uses `DefaultAzureCredential` with Entra ID and exposes `<loopback>` by default through `PLAYWRIGHT_SERVICE_EXPOSE_NETWORK`, which allows cloud browsers to reach a local forwarded app when supported. Omit `PLAYWRIGHT_SERVICE_URL` for local browsers.

## Categories

- `Smoke`: shell navigation, global search routing, footer disclaimer, and axe scans. Critical axe violations fail; serious violations are logged because MudBlazor defaults can produce framework-level issues.
- `Dashboard`: KPIs, worklist filtering/paging, row navigation, and persona switching.
- `CaseWorkbench`: pending requirements, blocked and successful decisions, tabs, and not-found state.
- `Policy360`: search, tab content, beneficiary validation, service actions, annuity funds, and not-found state.

Use NUnit filters, for example:

```bash
dotnet test tests/LifeCore.PlaywrightTests --settings tests/LifeCore.PlaywrightTests/.runsettings --filter "Category=Smoke"
```

## GitHub workflow

`.github/workflows/ui-tests.yml` restores and builds `tests/LifeCore.PlaywrightTests/LifeCore.PlaywrightTests.csproj`, then runs:

```bash
dotnet test tests/LifeCore.PlaywrightTests/LifeCore.PlaywrightTests.csproj --configuration Release --no-build --logger "trx;LogFileName=playwright-tests.trx" --results-directory TestResults/playwright -- NUnit.NumberOfTestWorkers=20
```

It sets `PLAYWRIGHT_SERVICE_URL` from `vars.PLAYWRIGHT_SERVICE_URL` and `BASE_URL` from the workflow input or `vars.WEB_URL`, matching the test implementation. The workflow does not pass `--settings tests/LifeCore.PlaywrightTests/.runsettings`; CLI worker and logger arguments still work, but runsettings defaults such as `PLAYWRIGHT_SERVICE_EXPOSE_NETWORK=<loopback>` are not applied unless the workflow is updated or the variable is configured in Actions.
