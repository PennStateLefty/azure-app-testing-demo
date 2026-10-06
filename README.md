# Azure App Testing demo

A customer-neutral storefront showing a pull request moving through GitHub Actions,
Azure App Service deployment, Azure Playwright Testing, and Azure Load Testing.
The Node.js backend serves a static frontend and a product API; no database,
customer data, or payment service is needed.

## Run locally

Requires Node.js 22 and npm.

```bash
npm ci
npm start
```

Open http://localhost:3000 and add products to the demo cart. The backend exposes
`GET /api/health` and `GET /api/products`.

## Tests

All test assets live in `testing/`:

| Folder | Purpose |
| --- | --- |
| `unit/` | Node's built-in test runner: API behavior, asset serving, and route/method boundaries |
| `playwright/` | Chromium and Firefox: catalog, cart, backend errors, and API smoke tests |
| `load/` | Parameterized JMeter plan and Azure latency/error gates |

```bash
npm test
npx playwright install --with-deps chromium firefox
npm run test:e2e
```

Playwright starts the app automatically unless `BASE_URL` is set. In CI, stop any
existing local server first. To test an already running or deployed application:

```bash
BASE_URL=http://localhost:3000 npm run test:e2e
```

For a local load run, install Apache JMeter 5.6.3 and start the app, then run:

```bash
jmeter -n -t testing/load/storefront.jmx \
  -JtargetHost=localhost -JtargetProtocol=http -JtargetPort=3000 \
  -Jusers=10 -JrampUp=10 -Jduration=60 \
  -l /tmp/storefront-results.jtl -j /tmp/storefront-jmeter.log
```

The plan checks HTTP 200 and catalog content, with a 500 ms think time. Azure
supplies `TARGET_HOST`, `TARGET_PROTOCOL`, and `TARGET_PORT` as environment
variables; these take precedence over local JMeter properties. The Azure YAML
uses one engine and fails the job if average response time exceeds 1000 ms or
errors exceed 1%. Local JMeter records errors but does not enforce those Azure
thresholds; inspect its results. Tune the small default load for your demo budget.

## Azure setup (one time)

Azure jobs are **opt-in**. Without configuration, PRs still run unit and local
browser tests. Azure deployments and test runs incur charges.

1. Select an Azure subscription and a region supporting App Service, Azure Load
   Testing, and Playwright Workspaces. Install Azure CLI with Bicep support and
   sign in. Register the providers and create a dedicated resource group:

   ```bash
   az login
   az account set --subscription "<subscription-id>"
   az provider register --namespace Microsoft.Web --wait
   az provider register --namespace Microsoft.LoadTestService --wait
   az group create --name "<resource-group>" --location "<region>"
   az deployment group create --resource-group "<resource-group>" \
     --template-file infra/main.bicep --parameters namePrefix="<unique-prefix>"
   ```

   Choose a globally unique, 3–30 character lowercase prefix starting with a
   letter and ending with a letter or digit; internal hyphens are allowed.
   Bicep provisions a Linux B1 plan, `<prefix>-main` web app (Node.js 22, HTTPS
   only), and `<prefix>-load` load-testing resource. The initial app has no code
   until the pipeline deploys it.

2. In the Azure portal create a **Playwright Workspace** under Azure App Testing
   in the dedicated resource group. Enable **Reporting**, link a storage account,
   and copy its browser endpoint into `PLAYWRIGHT_SERVICE_URL`. Workspace/storage
   creation is a one-time portal step, not part of the Bicep template. Use
   Microsoft Entra authentication, not an access token.

3. Create a Microsoft Entra application/service principal for GitHub Actions and
   add a federated credential with:

   - Issuer: `https://token.actions.githubusercontent.com`
   - Subject: `repo:PennStateLefty/azure-app-testing-demo:environment:azure-demo`
     (replace the owner/repository if using a copy)
   - Audience: `api://AzureADTokenExchange`

   Grant the identity **Contributor** on the dedicated demo resource group for
   infrastructure/deployment, **Load Test Contributor** on `<prefix>-load`, and
   workspace test execution access following the
   [Playwright access guide](https://aka.ms/pww/docs/manage-access).
   Grant **Storage Blob Data Contributor** on the reporting storage account to
   the pipeline identity and to people running cloud tests locally. Configure
   Blob CORS for `https://trace.playwright.dev`, methods `GET, OPTIONS`, to view
   traces. Role assignment requires an administrator with role-assignment
   permission; the pipeline does not grant itself roles.

4. Create the GitHub environment **azure-demo**. **Require reviewers**, prevent
   self-review, and restrict deployment branches to `main` and approved demo PR
   branches. Review all PR code, dependency changes, and workflow changes before
   approving credentialed jobs. OIDC eliminates a stored Azure password; it does
   not make untrusted PR code safe. Do not use `pull_request_target` to run PR code.

   Configure these environment secrets (IDs, not a client secret):

   | Secret | Value |
   | --- | --- |
   | `AZURE_CLIENT_ID` | Entra application/client ID |
   | `AZURE_TENANT_ID` | Entra tenant ID |
   | `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |

   Configure these environment variables:

   | Variable | Value |
   | --- | --- |
   | `AZURE_RESOURCE_GROUP` | Dedicated resource group |
   | `AZURE_NAME_PREFIX` | Prefix used for Bicep provisioning |
   | `PLAYWRIGHT_SERVICE_URL` | Workspace browser endpoint copied from Azure |

   Finally, set the **repository** variable `AZURE_ENABLED` to `true`. It must be
   repository-scoped because it is evaluated before the environment is loaded.
   Keep the same prefix/resource group for the lifetime of a PR.

## CI/CD and demo flow

`.github/workflows/demo.yml` runs on PR open/update/reopen, pushes to `main`, and
manual dispatch. Manual cloud runs deploy only when the selected branch is `main`.

1. CI installs locked dependencies, runs unit tests and local browser tests, and
   packages only the application and npm manifests.
2. After environment approval, the pipeline provisions and deploys
   `<prefix>-pr-<number>` for a same-repository PR, or `<prefix>-main` for `main`.
   PR apps share the plan but never overwrite the main app. Deployment waits for
   API readiness before testing.
3. Two independent jobs run the same Playwright suite on **Azure-hosted browsers**
   and JMeter on **Azure Load Testing**. Azure browser reporting uploads the HTML
   report to the workspace's linked storage; load thresholds fail the load job.
   Both jobs are required for a successful workflow.
4. Show the PR's Actions checks, job summaries, `azure-playwright-results` (HTML
   report and failure traces), and `azure-load-results` (downloaded load results).
   Azure portal test runs use `demo-pr-<number>-<run-id>-<attempt>` names; load run
   descriptions include the commit SHA. Local CI reports are also uploaded.
5. Make a small app change on a demo branch, open a PR, approve the environment
   jobs, and compare its browser results and load metrics with the main run. A
   deliberate catalog regression demonstrates failing checks; revert it and
   rerun to demonstrate recovery. Merge only after the checks pass.

Fork PRs receive local CI only and never receive Azure credentials. Configure
branch protection to require the relevant checks for your trusted demo workflow.
If Azure is disabled, cloud checks are skipped. Browser and load runs execute in
parallel after deployment so one failing suite does not suppress the other's
results. Runs for the same PR/main target are serialized to prevent overlapping
deployments.

To run cloud browser tests locally after assigning your own identity the required
workspace/storage roles:

```bash
az login
export BASE_URL="https://<deployed-app>.azurewebsites.net"
export PLAYWRIGHT_SERVICE_URL="<workspace-browser-endpoint>"
npm run test:e2e:azure
```

## Cleanup and cost

Closing a same-repository PR triggers an environment-protected cleanup job that
deletes its web app. Approve that job as well. Shared plan, load-test definitions,
test history, main app, workspace, and reporting storage remain for the demo.
If a run is canceled or cleanup is not approved, remove leftover PR apps manually:

```bash
az webapp delete --resource-group "<resource-group>" --name "<prefix>-pr-<number>"
```

Delete the dedicated resource group when finished to stop charges (this removes
all resources in that group, including workspace/storage if created there):

```bash
az group delete --name "<resource-group>" --yes
```

The sample is not a production commerce application. Authentication, durable
storage, private networking, and customer-specific branding are intentionally
outside this demo.
