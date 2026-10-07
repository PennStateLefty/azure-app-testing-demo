# azure-app-testing-demo — LifeCore Suite

An end-to-end demo of **Azure App Testing** (Azure Load Testing + Playwright Workspaces) in a GitHub Actions CI/CD flow.

The system under test is **LifeCore Suite**, a fictional life & annuity administration platform. Its UI uses a purple `#A100FF` accent, a black app bar, and a circular **LC** monogram with vendor-neutral demo branding.

> Demo application. Synthetic data only.

## Architecture

```
                 GitHub Actions (OIDC → user-assigned MI)
   ci.yml ─(label: stage)─► staging slot ─► ui-tests.yml + load-tests.yml (all profiles) ─► Stage gate
   deploy-app.yml (merge to main) ─► slot swap staging → production ─► smoke load + UI tests
       load-tests.yml ─► Azure Load Testing (JMeter)   ui-tests.yml ─► Playwright Workspaces
                                   │
                                   ▼
 ┌───────────── App Service (Linux, P1v3, autoscale 2–5) ─────────────┐
 │ LifeCore.Web  (.NET 10, single binary)                             │
 │  ├─ Blazor WebAssembly UI (MudBlazor) ─ LifeCore.Web.Client        │
 │  └─ Minimal APIs /api/v1, /health, /scalar (OpenAPI)               │
 └───────────────┬───────────────────────────────┬────────────────────┘
                 │ Entra-only, managed identity  │ OpenTelemetry
                 ▼                               ▼
        Azure SQL (serverless)          Application Insights / Log Analytics
```

| Path | Purpose |
|---|---|
| `src/LifeCore.Contracts` | DTOs, enums, API routes, seed conventions shared by API/UI/tests |
| `src/LifeCore.Domain` / `src/LifeCore.Data` | Entities, business rules, EF Core (SQLite locally, Azure SQL in Azure), deterministic seeder |
| `src/LifeCore.Web` / `src/LifeCore.Web.Client` | Host + APIs / Blazor WASM UI |
| `tests/LifeCore.UnitTests` | xUnit v3 domain + API integration tests |
| `tests/playwright` | TypeScript Playwright E2E tests (local or Playwright Workspaces with Azure reporting) |
| `loadtests/` | JMeter `.jmx` scripts, CSV data, Azure Load Testing YAML configs |
| `infra/`, `azure.yaml`, `hooks/` | Bicep + azd provisioning |
| `.github/workflows/` | CI, provision, deploy, UI tests, load tests |
| `docs/` | Requirements, deployment, load testing, UI testing, test-id catalogs |

## Screens

1. **Underwriting Dashboard** (`/dashboard`) — KPI tiles and a filterable/pageable case worklist.
2. **Case Workbench** (`/cases/{caseNumber}`) — overview, requirements, risk, notes, AI-style summary, and decision panel with rule validation.
3. **Policy 360** (`/policies`, `/policies/{policyNumber}`) — policy search and a 360° view with coverages, beneficiaries, values/funds, transactions, and service actions (address change, loan quote, beneficiary update).

Well-known seed records: `UW100001` (outstanding APS — decision blocked), `UW100002` (decisionable), `LC1000001` (UL, 50/50 primaries), `LC1000002` (FIA, 3 funds).

## Run locally

Prerequisites: .NET SDK 10.0.100+.

```bash
dotnet run --project src/LifeCore.Web --urls http://localhost:5119
# UI:      http://localhost:5119
# API doc: http://localhost:5119/scalar
# Ready:   http://localhost:5119/health/ready
```

SQLite is used by default and seeded on startup (50 agents, 2,000 cases, 10,000 policies).

## Tests

```bash
dotnet test tests/LifeCore.UnitTests                 # unit + API integration
cd tests/playwright && npm ci && npx playwright install chromium
cd tests/playwright && npx playwright test        # E2E against BASE_URL (default http://localhost:5119)
```

- UI tests and Playwright Workspaces: see [docs/ui-testing.md](docs/ui-testing.md)
- Load tests (smoke / underwriter journey / CSR servicing / mixed peak): see [docs/load-testing.md](docs/load-testing.md). The Azure Load Testing configs include App Service, App Service Plan, Azure SQL, and Application Insights app components so runs capture server-side Azure Monitor metrics.

## Deploy to Azure

```bash
azd auth login
azd env new lifecore-demo
azd provision            # Bicep: App Service (VNet-integrated), private Azure SQL, App Insights, Load Testing, Playwright Workspace, identities
```

Then set the GitHub repository variables printed by the postprovision hook and push to `main`. To gate a PR on the full UI and load suite in a production-like **staging slot**, add the `stage` label. Merging then promotes the slot with a swap; see [docs/deployment.md](docs/deployment.md#stage-gate-and-promotion). Deployment is **App Service continuous deployment via GitHub Actions** (`DEPLOYMENT_MODE=githubActions`); if your subscription blocks ZIP deploy, switch to the **App Service Build Service** fallback (`DEPLOYMENT_MODE=appServiceBuild`). Full details: [docs/deployment.md](docs/deployment.md).

Defaults: app in **West US 3** and the Playwright Workspace in **East US**. Azure SQL is private-endpoint only, and the app managed identity is its Entra admin. The Playwright Workspace has reporting enabled with a linked StorageV2 account for portal reports, traces, screenshots, and videos.

## Performance demo (before/after)

The app setting `Perf__UseOptimizedQueries` (response header `X-Perf-Optimized`) toggles between a deliberately naive data path (tracking queries, eager loading, no caching) and an optimized one. Bicep deploys it as `false`; run the `peak` load profile, flip it to `true`, re-run, and compare in Azure Load Testing. See [docs/load-testing.md](docs/load-testing.md#beforeafter-performance-demo-story).

## License

See [LICENSE](LICENSE).
