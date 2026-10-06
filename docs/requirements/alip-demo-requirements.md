# Requirements: Life & Annuity Platform Mock (ALIP-inspired) for Azure App Testing Demo

> **Purpose:** Input document for planning mode. It describes a mock web app based on the publicly documented capabilities of the **Accenture Life Insurance & Annuity Platform (ALIP)**. The app has up to three screens, a .NET 10 front end and back end, Playwright UI tests and JMeter load tests that both run in **Azure App Testing**, and Bicep infrastructure as code for an Azure deployment.
>
> **Status:** Approved for implementation, 2026-10-06. Items tagged **[DECIDED]** are settled planning decisions.

---

## 1. Background research: what ALIP is

### 1.1 Product summary (verified from public sources)

ALIP is Accenture's cloud-native policy administration and new-business platform for life insurance and annuity carriers. It's sold as a suite whose modules can be deployed separately or together (on-premises, cloud, or SaaS; it's also listed in Azure Marketplace). Celent has named it a "Luminary" several times (2020, 2022, 2023, and the 2024 NB&UW XCelent award).

| Module | Publicly documented capabilities |
|---|---|
| **Product Development / Configuration** | Library of prebuilt product templates. Drag-and-drop product builder (coverages, features, transactions, funds). **Business Configuration Workbench** with list or graphical rule views, interactive flowcharts, and drag-and-drop. Graphical rate tables (for example Age × Face Amount). **Business Configuration Debugger** (watch list of XML tags). **Product Testing Workbench**. |
| **New Business & Underwriting** | Direct-to-consumer and producer-initiated applications. Real-time data validation. Automated decision engine. **Case Workbench** that consolidates case data and alerts underwriters when values are out of bounds. Electronic requirements ordering (ACORD). **Case Dashboard**. GenAI underwriting "co-pilot" that summarizes case status. |
| **Policy Administration / Servicing** | In-force servicing: financial and non-financial transactions, riders, loans, withdrawals, beneficiary and address changes. Client management. Accounting. Pre-sale, post-issue, and in-force quotes and illustrations. Tax and regulatory rules. |
| **Billing** | Real-time premium processing at the product and transaction level, with transaction visibility. |
| **Claims & Payout** | Claims intake, adjudication, payout, settlement options. |
| **ALIP Portal (Agent & Consumer)** | Mobile-first, device-agnostic, MVC architecture, SSO/OAuth2. 360° view of in-force policies. Agent book of business, license and appointment status, tasks and follow-ups, commissions (paid vs. earned), and application/underwriting status tracking. Consumers can view policies and investments, start transactions, download forms, and update personal information. |
| **Integration** | Interface Exchange (prebuilt, pretested APIs). REST/JSON gateway with ACORD translation. Swagger docs. IPaaS. |

**Supported product lines:** Life (term, whole life, universal life, indexed UL, variable UL) and annuities (fixed, variable, deferred, immediate, indexed, MVA, RILA, GLB riders, structured settlements, single and flexible premium).

### 1.2 What is publicly known about the UI

Accenture hasn't published screenshots that can be reproduced. The descriptions below come from Accenture's video transcript ("Accelerate speed to market with empowered business users", 2021), the ALIP 5.2 release announcement (2018), and the ALIP Portal brochures (2018/2021):

- **Organized around workbenches.** Accenture says common new-business and administration tasks are grouped into workbenches so the functions a user needs are in one place.
- **Case Dashboard (underwriting):**
  - Shows each underwriter's or case manager's **workload and key metrics**.
  - A **case summary** section shows case details plus metrics such as the **number of pending Attending Physician Statements (APS)**.
  - A **task summary** section shows the user's **work queue grouped by task and by case**.
  - A **chat** feature lets underwriters ask peers for guidance.
- **Case Workbench:** a simplified view of one case with all its data in one place, "comprehensive analysis capabilities", **out-of-bounds data alerts**, and configurable permissions and data displays.
- **Product / Business Configuration Workbench:**
  - Consolidated screens with drag-and-drop.
  - A finished product shows **summary, approvals, transactions, features, and funds**.
  - Rules can be viewed **as a list or graphically**, and rate tables appear in a graphical grid.
- **Portal:** mobile-first, responsive, configurable browser UI with a "book of business at a glance" view and a 360° policy view.
- **Look and feel (assumed, not verified):** a modern enterprise single-page app with a left navigation rail, a top bar with search and user menu, tile- and card-based dashboards, data grids, tabbed detail views, and status chips. One unverified source says the front end is Angular. ALIP's back end has historically been Java/WebSphere/Oracle.

> **Note:** All mock screens are **inspired by** the public descriptions above. They aren't copies of proprietary UI.

---

## 2. Demo scope

### 2.1 Goals

1. Build a convincing mock of an L&A core-system UI that has **three screens**, each tied to a publicly documented ALIP capability.
2. Show **Azure App Testing** end to end:
   - **Playwright Workspaces**: cloud-parallel browser tests against the front end.
   - **Azure Load Testing**: JMeter scripts that load the back-end REST APIs, with server-side metrics from App Insights.
3. Provision Azure infrastructure with **Bicep** through `azd provision`, deploy the app through GitHub Actions, and run the tests from CI.

### 2.2 Non-goals

- No real actuarial calculations, real underwriting rules, or real integrations (ACORD, MIB, Rx, labs).
- No real PII. All data is synthetic.
- No production-grade multi-tenant auth. The demo uses a simple persona switcher (see 4.4).
- No copying of Accenture trademarks, logos, wordmarks, or proprietary screen layouts.

### 2.3 Branding

- **[DECIDED]** Use the fictional product name **"LifeCore Suite"** and the fictional carrier/tenant **"Contoso Life & Annuity"**.
- Show a footer disclaimer: *"Demo application – not affiliated with or endorsed by Accenture. Synthetic data only."*
- Refer to "ALIP" only in docs, never as an in-app brand.
- Do not use the Accenture logo or wordmark.

#### Design language

Because research found no distinct, publicly documented ALIP design system, the demo adopts Accenture's public design language rather than inventing a separate ALIP-specific style. Brand-color and typography references are grounded in Accenture public brand materials (2020 rebrand).

- **Palette:** Core Purple `#A100FF` for primary actions, accents, and active states; Black `#000000` for the top app bar and headings; White for surfaces; Dark Purple `#7500C0` for hover and secondary states; Deep Purple `#460073` for tertiary accents and chart series; light neutral `#F2F2F2` for page backgrounds; `#E6E6E6` for borders and dividers.
- **Style:** flat, high-contrast enterprise UI with generous whitespace, small border radius, restrained card shadows, dense but readable data grids, and the `>` chevron as a subtle navigation and emphasis motif.
- **Typography:** font stack `"Graphik", "Inter", Arial, sans-serif`. Graphik is Accenture's brand typeface and requires a commercial license from Commercial Type, so it is **not bundled**. Inter is self-hosted under the SIL Open Font License as the fallback.

---

## 3. Screens (maximum of three)

**[DECIDED]** These three screens follow the underwriting workflow that Accenture describes in its own demo video, and then the in-force servicing step. Together they cover New Business/UW, Policy Admin, and Billing. Product Configuration Workbench was considered as Screen 3 and not selected.

### Screen 1: Underwriting Case Dashboard (`/dashboard`)

**Maps to:** ALIP Case Dashboard (New Business & Underwriting).

**Persona:** Underwriter / Case Manager.

| Region | Contents |
|---|---|
| **KPI tiles** | Open cases, Cases pending requirements, **APS pending**, Avg days-in-UW, Cases at SLA risk, Decisions today. |
| **Work queue (task summary)** | Toggle between **By Task** and **By Case**. Grid columns: Case #, Applicant, Product, Face Amount, Task type (Review APS, Order Labs, Final Decision…), Priority, Age (days), Status chip, Assigned to. Supports server-side paging, sort, and filter (status, product, priority) and a search box. |
| **Case summary panel** | Opens when a row is clicked. Shows key details, requirement counts (ordered / received / outstanding), risk-class indication, and an "Open in Workbench" button. |
| **Charts** | Cases by status (donut) and Weekly intake vs. decisions (bar). |
| **Peer chat (stub)** | Collapsible side panel with a mocked thread. Messages are stored through the API but aren't real-time. |

**Key interactions to test:** filter and paging, switching queue view, opening the case summary, navigating to the Workbench.

### Screen 2: Case Workbench (`/cases/{caseId}`)

**Maps to:** ALIP Case Workbench (underwriting), with GenAI co-pilot case summary.

**Persona:** Underwriter.

| Region | Contents |
|---|---|
| **Case header** | Case #, Applicant, DOB/Age, Product, Face Amount, Agent, Status, Days open. |
| **Tabs** | **Overview** (application data, with out-of-bounds fields highlighted, for example BMI or face amount vs. income), **Requirements** (APS, Labs, Rx, MVR, MIB rows with status, ordered/received dates, and an "Order requirement" action), **Risk Assessment** (build/BMI, medical history, avocations, and a **suggested risk class** from a mock rules engine), **Notes & History** (timeline). |
| **AI Case Summary (mock)** | Card with a deterministic, template-generated summary such as "3 of 5 requirements received; BMI 31.2 outside preferred range…". **[DECIDED]** No LLM call by default and no Azure OpenAI dependency. This keeps load tests deterministic. |
| **Decision panel** | Choose a risk class (Preferred Plus, Preferred, Standard Plus, Standard, Table 2–8, Decline), add a reason and a note, then **Submit Decision**. The API validates the request: it rejects the decision if required requirements are outstanding, unless the user overrides. |

**Key interactions to test:** tab navigation, ordering a requirement, out-of-bounds alerts showing, the decision validation error path, the decision success path (status changes on the Dashboard).

### Screen 3: Policy 360 / In-Force Inquiry (`/policies/{policyNumber}`, plus a search at `/policies`)

**Maps to:** ALIP Policy Administration and the ALIP Portal 360° in-force view (servicing, billing, funds).

**Persona:** Customer Service Rep / Agent.

| Region | Contents |
|---|---|
| **Policy search** | Search by policy #, owner name, or SSN-last-4 (synthetic). Results appear in a grid. |
| **Policy header** | Policy #, Product (UL / Term / Fixed Indexed Annuity), Status (In Force, Lapsed, Pending), Issue date, Owner/Insured, Agent, Face/Account Value. |
| **Tabs** | **Coverages & Riders**, **Parties** (owner, insured, beneficiaries with %), **Billing** (mode, premium, next due date, payment history grid), **Values** (cash value and surrender value; for annuities, fund allocations shown as a table and pie chart), **Transactions** (history grid). |
| **Service actions** | **Change Address** and **Change Beneficiary** (non-financial; beneficiary percentages must total 100%), plus a **Quote Loan / Withdrawal** illustration (simple formula). Each action creates a pending transaction. |

**Key interactions to test:** search, tab switching, the beneficiary-change validation, a loan quote, and the new transaction appearing in history.

> **Alternative Screen 3 (not selected):** a *Product Configuration Workbench* with a product template list, a detail view (summary, approvals, transactions, features, funds), and a rate table grid (Age × Face Amount) with list and graphical rule views. Policy 360 was chosen because it gives the most realistic read-heavy load profile and covers in-force servicing.

### 3.4 Shared layout

- Left navigation: Dashboard, Cases, Policies. A disabled placeholder entry ("Product Config") hints at broader scope.
- Top bar: global search (policy or case #), persona switcher, environment badge.
- Responsive layout with a minimum width of 1280 px for desktop. Mobile is optional.
- Accessibility: semantic landmarks, labeled inputs, and **stable `data-testid` attributes on every interactive element** so Playwright tests are resilient.

---

## 4. Architecture

### 4.1 Technology stack

| Concern | **[DECIDED]** Recommendation | Rationale |
|---|---|---|
| Runtime | **.NET 10 (LTS)**, C# 14 | Latest GA (10.0.x, Sept 2026). .NET 11 isn't GA until Nov 2026. |
| Front end | **Blazor WebAssembly** client (in a Blazor Web App with **InteractiveWebAssembly** render mode). Component library: **MudBlazor** with a custom Accenture-palette theme. | All UI → back end traffic is **plain HTTP/JSON**, which JMeter can replay. Blazor **Server** routes UI events over a SignalR WebSocket, which JMeter can't realistically load test. |
| Back end | **ASP.NET Core Minimal APIs** under `/api/v1/*`, with OpenAPI (built-in `Microsoft.AspNetCore.OpenApi`) and a Scalar or Swagger UI | A REST surface that load tests can target directly, mirroring ALIP's REST/JSON gateway and Swagger docs. |
| Hosting model | **One deployable**: the ASP.NET Core host serves the WASM static assets *and* the API. The client and the API are separate projects. | Meets the "same binary is fine" requirement while keeping a clean API boundary for load tests. |
| Data | **EF Core 10**. **Azure SQL Database serverless** in Azure with Entra-only authentication through a user-assigned managed identity; **SQLite** locally. Schema is created with `EnsureCreated` for both providers, and seed data is deterministic at startup. | Gives realistic DB latency under load. Seeding makes test data predictable. Two providers keep local development lightweight without changing API behavior. |
| Auth to data | **User-assigned managed identity** for Azure SQL; local development uses SQLite. | Secure by default; no SQL passwords. |
| Orchestration (local) | **No .NET Aspire** in v1. | Keeps the demo focused and avoids extra projects that aren't needed for Azure App Testing. |
| Observability | **OpenTelemetry → Azure Monitor** (Application Insights + Log Analytics) | Server-side metrics are linked into Azure Load Testing results. |
| Health | `/health/live`, `/health/ready` | Used by App Service health check and as a load-test smoke step. |

### 4.2 Solution layout (proposed)

```
src/
  LifeCore.Web/                ASP.NET Core host: Minimal APIs + serves WASM client
  LifeCore.Web.Client/         Blazor WebAssembly UI (3 screens)
  LifeCore.Contracts/          Shared DTOs, routes, and seed conventions
  LifeCore.Domain/             Entities, enums, rules (risk class, validation)
  LifeCore.Data/               EF Core DbContext, EnsureCreated schema setup, seeder
tests/
  LifeCore.UnitTests/          xUnit v3 domain/API tests
  playwright/                  TypeScript + @playwright/test + @azure/playwright
loadtests/
  jmeter/*.jmx                 JMeter scripts
  jmeter/data/*.csv            Parameter data (case IDs, policy numbers)
  smoke.yaml                   Azure Load Testing smoke definition
  underwriter-journey.yaml     Azure Load Testing underwriting scenario definition
  csr-policy-servicing.yaml    Azure Load Testing servicing scenario definition
  mixed-peak.yaml              Azure Load Testing peak scenario definition
infra/
  main.bicep, modules/*.bicep, main.parameters.json
azure.yaml                     azd project for provisioning only
.github/workflows/             ci.yml, provision.yml, deploy-app.yml, ui-tests.yml, load-tests.yml
```

### 4.3 Domain model (minimum)

- **Party** (Id, Name, DOB, Gender, Address, SSNLast4, Role)
- **Agent** (Id, Name, LicenseState, Status)
- **Product** (Code, Name, Line [Life/Annuity], Type [Term/UL/IUL/FIA/VA])
- **UnderwritingCase** (CaseNumber, ApplicantId, ProductCode, FaceAmount, AnnualIncome, HeightIn, WeightLb, Tobacco, Status [Submitted/InUnderwriting/PendingRequirements/Decisioned/Withdrawn], AssignedUnderwriter, ReceivedDate, Priority, SuggestedRiskClass, FinalRiskClass)
- **Requirement** (Id, CaseNumber, Type [APS/Labs/Rx/MVR/MIB/Paramed], Status [Ordered/Received/Waived], OrderedDate, ReceivedDate)
- **WorkTask** (Id, CaseNumber, Type, Priority, DueDate, AssignedTo, Status)
- **CaseNote** / **ChatMessage**
- **Policy** (PolicyNumber, ProductCode, Status, IssueDate, OwnerId, InsuredId, AgentId, FaceAmount, AccountValue, CashSurrenderValue, BillingMode, ModalPremium, NextDueDate)
- **Coverage/Rider**, **Beneficiary** (PartyId, Type [Primary/Contingent], Percent), **FundAllocation**, **Payment**, **PolicyTransaction** (Type, Status, EffectiveDate, Amount)

**Seed volumes [DECIDED]:** about 50 agents, about 2,000 cases (with roughly 8,000 requirements and tasks), and about 10,000 policies. This is enough for meaningful paging and query cost under load.

### 4.4 Authentication [DECIDED]

- **Default:** no real authentication. A **persona switcher** (Underwriter / CSR) sets a header or cookie. This keeps JMeter and Playwright scripts simple.
- **Optional stretch:** App Service Easy Auth with Entra ID. In that case, tests use a test-user storage state, and load tests call the API with a client-credentials token. This is out of scope for v1.

### 4.5 API surface (contract for UI and load tests)

| Method & Route | Screen | Load profile |
|---|---|---|
| `GET /api/v1/dashboard/kpis?underwriter=` | 1 | Hot read |
| `GET /api/v1/worklist?view=task\|case&status=&product=&page=&pageSize=&sort=` | 1 | Hot read (paged query) |
| `GET /api/v1/cases/{caseNumber}` | 1, 2 | Read |
| `GET /api/v1/cases/{caseNumber}/requirements` | 2 | Read |
| `POST /api/v1/cases/{caseNumber}/requirements` | 2 | Write |
| `GET /api/v1/cases/{caseNumber}/risk-assessment` | 2 | Compute (mock rules engine) |
| `GET /api/v1/cases/{caseNumber}/summary` | 2 | Compute (templated "AI" summary; Azure OpenAI is out of scope) |
| `POST /api/v1/cases/{caseNumber}/decision` | 2 | Write and validation (returns 422 on rule failure) |
| `GET/POST /api/v1/cases/{caseNumber}/chat` | 1 | Light read/write |
| `GET /api/v1/policies?query=&page=` | 3 | Search (LIKE / indexed) |
| `GET /api/v1/policies/{policyNumber}` (+ `/coverages`, `/beneficiaries`, `/billing`, `/values`, `/transactions`) | 3 | Read-heavy |
| `PUT /api/v1/policies/{policyNumber}/address` | 3 | Write |
| `PUT /api/v1/policies/{policyNumber}/beneficiaries` | 3 | Write and validation (sum = 100%) |
| `POST /api/v1/policies/{policyNumber}/quotes/loan` | 3 | Compute |
| `POST /api/v1/admin/reset` | n/a | Re-seeds data between test runs (only enabled when a config flag is set) |

**Requirements for every endpoint:**
- Returns JSON.
- Uses ProblemDetails for errors.
- Accepts an `X-Correlation-Id` header and echoes it back.
- Has no server-side session affinity, so the API scales out cleanly.

---

## 5. Testing requirements

### 5.1 Playwright UI tests (Playwright Workspaces in Azure App Testing)

- **[DECIDED] Language:** **TypeScript**, using `@playwright/test` (1.57+) and **`@azure/playwright`** so Playwright Workspaces reporting can upload results, traces, screenshots, and videos to the Azure portal. Authenticate to the workspace with **Entra ID** (`AzureCliCredential` with `AZURE_TENANT_ID` locally, GitHub OIDC in CI). Access tokens and NUnit are not used for reporting.
- **Configuration:** the workspace region endpoint comes from the `PLAYWRIGHT_SERVICE_URL` environment variable. The base URL comes from `BASE_URL`. Run Chromium in parallel (for example 20 workers). Enable workspace reporting with a linked Storage account and Storage Blob Data Contributor for test runners.
- **Minimum test suites:**
  1. **Dashboard:** KPI tiles render with values. The By Task / By Case toggle works. Filtering by status narrows the rows. Paging works. Clicking a row opens the case summary. "Open in Workbench" navigates correctly.
  2. **Case Workbench:** each tab loads. An out-of-bounds field shows an alert. Ordering a requirement adds a row. Submitting a decision with outstanding requirements shows a validation error. A valid decision updates the status, and the Dashboard reflects it.
  3. **Policy 360:** search returns results. Each tab renders. A beneficiary total of 100% is enforced. Changing the address creates a pending transaction. A loan quote returns an amount.
  4. **Smoke / accessibility:** navigation shell, footer disclaimer, and an optional axe scan.
- Tests reset data through `/api/v1/admin/reset` or use isolated seed records, so runs are idempotent.
- Selectors use `data-testid` or role-based locators only.

### 5.2 Load tests (Azure Load Testing in Azure App Testing, JMeter)

- **Scripts live in** `loadtests/jmeter/`. They're parameterized with JMeter properties (`-Jhost`, `-Jthreads`, `-Jduration`, `-Jrampup`), which are passed as Azure Load Testing env vars. CSV data sets come from seed data (case numbers, policy numbers).
- **Scenarios:**

  | Script | Mix | Purpose |
  |---|---|---|
  | `underwriter-journey.jmx` | KPIs → worklist (paged) → case detail → requirements → risk → summary → (10%) decision | Realistic UW session with think time |
  | `csr-policy-servicing.jmx` | Policy search → policy 360 reads (5 calls) → (5%) address change → (5%) loan quote | Read-heavy servicing |
  | `mixed-peak.jmx` | 60% CSR / 40% UW, ramp to peak | Capacity test |
  | `smoke.jmx` | `/health/ready` plus one call per endpoint, 1 VU | Pipeline gate |

- **Test definitions:** one Azure Load Testing YAML per scenario (`smoke.yaml`, `underwriter-journey.yaml`, `csr-policy-servicing.yaml`, `mixed-peak.yaml`) contains:
  - `engineInstances`
  - **failure criteria**, for example `avg(response_time_ms) > 500`, `percentage(error) > 1`, and per-request p90 thresholds
  - `autoStop`
  - app components: App Service, SQL, and App Insights, so **server-side metrics** are collected
  - optional **multi-region** load
- **Execution:** run from GitHub Actions with `azure/load-testing@v1` (or `az load test create/update` plus `az load test-run create`). The workflow publishes results as artifacts and fails the build when failure criteria are breached.
- **Demo story:** the first Azure deployment defaults `Perf:UseOptimizedQueries` to `false`, so the first load run shows the intended bottleneck (for example an unindexed policy search or an N+1 query in the worklist). Flip the App Service app setting to `true` to show the improvement without a redeploy.

---

## 6. Azure infrastructure (Bicep, provisioned with `azd`)

**[DECIDED]** All resources go in one resource group per environment (`rg-lifecore-<env>`). Region is a parameter (default `eastus2`; it must support Playwright Workspaces and Load Testing).

| Resource | Type / API (verify at implementation time) | Notes |
|---|---|---|
| Log Analytics workspace | `Microsoft.OperationalInsights/workspaces` | |
| Application Insights | `Microsoft.Insights/components` (workspace-based) | Connection string goes to the app |
| User-assigned managed identities | `Microsoft.ManagedIdentity/userAssignedIdentities` | App identity for app → SQL; GitHub deployment identity for OIDC workflows |
| App Service plan (Linux) | `Microsoft.Web/serverfarms` (**P1v3**), autoscale rules | Scaling out is visible under load |
| Web App | `Microsoft.Web/sites` (linuxFxVersion `DOTNETCORE\|10.0`), health check `/health/ready`, Always On, HTTPS only | **[DECIDED]** App Service on Linux. Container Apps was considered and not selected. |
| Azure SQL server + database | `Microsoft.Sql/servers` (Entra-only admin), `databases` (serverless GP_S_Gen5_2, auto-pause off during demos) | |
| Key Vault (optional) | `Microsoft.KeyVault/vaults` (RBAC) | Only if secrets are needed |
| **Azure Load Testing** | `Microsoft.LoadTestService/loadTests@2022-12-01` (GA) | Test definitions are created through the data plane (CLI or GitHub Action), not Bicep |
| **Playwright Workspace** | `Microsoft.LoadTestService/playwrightWorkspaces@2025-09-01` (GA; previews up to 2026-08-01-preview) | Properties: `localAuth: 'Disabled'` (Entra only), `regionalAffinity: 'Enabled'`, `reporting: 'Enabled'`, optional `storageUri` for reports |
| Storage account (optional) | `Microsoft.Storage/storageAccounts` | Playwright report storage |
| Role assignments | `Microsoft.Authorization/roleAssignments` | GitHub user-assigned managed identity → Website Contributor on the web app, Load Test Contributor, Contributor on the Playwright workspace, and Reader on the resource group; app managed identity → SQL contained user through the post-provision hook |

**Outputs:** `WEB_URL`, `LOAD_TEST_RESOURCE_NAME`, `PLAYWRIGHT_SERVICE_URL`, `APPINSIGHTS_CONNECTION_STRING`, and the resource IDs for server-side metric components.

**Deployment constraint:** the target subscription has an Azure Policy that blocks zip deploy to App Service. `azd` is used for provisioning only, not for app deployment. Bicep exposes a `deploymentMode` parameter so the deployment path can be switched without redesigning the infrastructure. A post-provision hook creates the SQL contained user for the app identity.

**Primary deployment mode: `githubActions`.** App Service continuous deployment uses the GitHub Actions build provider described by Microsoft Learn: https://learn.microsoft.com/azure/app-service/deploy-continuous-deployment?tabs=github#github-actions. The `deploy-app.yml` workflow builds and publishes `src/LifeCore.Web`, then deploys with `azure/webapps-deploy` using GitHub OIDC and a GitHub user-assigned managed identity. Bicep creates the federated credentials and grants that identity Website Contributor on the web app. **Risk:** `azure/webapps-deploy` uses OneDeploy/ZipDeploy under the hood and may be blocked by the same policy, so validate this early with a deploy spike.

**Fallback deployment mode: `appServiceBuild`.** App Service Build Service (Kudu/Oryx) is configured with `Microsoft.Web/sites/sourcecontrols` and `isGitHubAction=false`; the GitHub webhook causes App Service to clone and build the repo. A root `.deployment` file sets `PROJECT=src/LifeCore.Web/LifeCore.Web.csproj`. Prerequisites and risks: one-time GitHub token registration, SCM basic publishing credentials must be allowed, and Oryx must support `net10.0`; otherwise use a custom deploy script alternative.

**CI/CD (GitHub Actions, OIDC federated credential, no secrets):**
1. `ci.yml`: restore, build, and run unit tests.
2. `provision.yml`: run `azd provision` and the post-provision SQL contained-user hook.
3. `deploy-app.yml`: build/publish the web app and deploy through the selected `deploymentMode` path.
4. `ui-tests.yml`: `npx playwright test -c tests/playwright/playwright.service.config.ts` against the Playwright Workspace. Publishes the HTML report artifact and uploads results through Azure Playwright reporting.
5. `load-tests.yml`: `azure/load-testing` with the scenario YAML files (smoke on every deploy, peak on manual dispatch).

---

## 7. Non-functional requirements

| Area | Requirement |
|---|---|
| Performance (baseline target) | p95 < 500 ms for reads and < 800 ms for writes at 200 concurrent virtual users on P1v3 × 2 instances. Error rate < 1%. |
| Scalability | Stateless API. Autoscale on CPU > 70%. No in-memory session. |
| Determinism | Fixed-seed data. Reset endpoint. No external API calls during tests. |
| Observability | Every request is traced, with correlation IDs. Custom metrics: `cases.decisioned`, `policy.transactions.created`. |
| Security | HTTPS only. Managed identity. No secrets in the repo. Admin reset endpoint disabled outside demo environments. |
| Cost | Can be torn down with `azd down`. SQL serverless. Playwright and load tests are billed per use. |

---

## 8. Planning decisions and remaining open items

### 8.1 Resolved decisions

| Question | Decision |
|---|---|
| Screen 3 | **Policy 360** is selected; Product Configuration Workbench remains a documented alternative but is not part of v1. |
| Visual design / branding | Adopt Accenture's public design language and palette because no distinct public ALIP design system was found; do not use Accenture logos or wordmarks. |
| Component library | **MudBlazor** with a custom Accenture-palette theme. |
| Playwright language | **TypeScript** with `@playwright/test`, `@azure/playwright`, Azure Playwright reporter, and Entra authentication. |
| Hosting | **App Service Linux P1v3** with `DOTNETCORE\|10.0`. |
| Deployment method | Primary: App Service continuous deployment with the GitHub Actions build provider and `azure/webapps-deploy` through OIDC. Fallback: App Service Build Service (Kudu/Oryx). `azd` provisions only. |
| Data and database auth | EF Core 10, Azure SQL serverless with Entra-only authentication via user-assigned managed identity, SQLite locally, `EnsureCreated` schema setup for both providers, deterministic startup seed. |
| CI platform and workflows | GitHub Actions: `ci.yml`, `provision.yml`, `deploy-app.yml`, `ui-tests.yml`, and `load-tests.yml`. |
| .NET Aspire | Out of scope for v1. |
| Azure OpenAI / live LLM | Out of scope; AI case summary is deterministic and templated. |

### 8.2 Open items

1. Whether Oryx supports building `net10.0` in fallback `appServiceBuild` mode in the target App Service environment.
2. Exact Playwright Workspaces region availability for the chosen Azure region.
3. Whether the subscription's zip-deploy policy also blocks `azure/webapps-deploy` / OneDeploy in the primary deployment mode.

---

## 9. Sources

- Accenture, *ALIP New Business and Underwriting*: https://www.accenture.com/en/products/new-business-underwriting
- Accenture, *ALIP Policy Administration* (supported annuity products): https://www.accenture.com/en-us/products/policy-administration
- Accenture, *Life and Annuity Software* (ecosystem, Interface Exchange, IPaaS): https://www.accenture.com/en-us/products/accenture-software-life-annuity
- Accenture, *Accelerate speed to market with empowered business users*, video transcript (2021). Describes the Case Dashboard, Business Configuration Workbench, and Product Testing Workbench: https://www.accenture.cn/content/dam/accenture/final/a-com-migration/pdf/pdf-165/accenture-accelerate-speed-market-empowered-business-users-transcript.pdf
- Accenture, *ALIP Release 5.2 announcement* (July 2018). Describes the Case Workbench, Business Configuration Workbench and Debugger, Product Testing Workbench, and portals: https://www.accenture.com/content/dam/accenture/final/corporate/company-information/document/ALIP-5-2-Announcement-July-2018.pdf
- Accenture, *ALIP Portal* brochures (2018/2021): https://www.accenture.com/content/dam/accenture/final/industry/insurance/document/Accenture-ALIP-Portal-May-2021.pdf
- Accenture Newsroom, *ALIP achieves Luminary status* (2022): https://newsroom.accenture.com/news/2022/accenture-life-insurance-and-annuity-platform-achieves-luminary-status-and-wins-two-xcelent-awards
- Accenture public brand materials (2020 rebrand) for brand colors and Graphik usage.
- Celent, *GenAI in Underwriting: ALIP Co-Pilot*: https://www.celent.com/insights/642683863
- Microsoft Learn, *What is Azure App Testing?*: https://learn.microsoft.com/azure/app-testing/overview-what-is-azure-app-testing
- Microsoft Learn, Bicep reference for `Microsoft.LoadTestService/playwrightWorkspaces`: https://learn.microsoft.com/azure/templates/microsoft.loadtestservice/playwrightworkspaces
- Microsoft Learn, Bicep reference for `Microsoft.LoadTestService/loadTests`: https://learn.microsoft.com/azure/templates/microsoft.loadtestservice/loadtests
- npm, *@azure/playwright*: https://www.npmjs.com/package/@azure/playwright
- .NET 10 download and support policy: https://dotnet.microsoft.com/download/dotnet/10.0
