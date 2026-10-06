# LifeCore Suite Azure deployment

LifeCore Suite deploys with `azd` + Bicep into one resource group per environment (`rg-lifecore-<env>`). The web app is an ASP.NET Core/.NET 10 App Service that serves the Blazor WebAssembly UI and Minimal APIs.

## Architecture

Provisioned resources:

- Log Analytics workspace and workspace-based Application Insights.
- App user-assigned managed identity (`id-app-*`) for Azure SQL access.
- GitHub Actions user-assigned managed identity (`id-github-*`) with OIDC federated credentials for `main`, the `demo` GitHub environment, and pull requests.
- Linux App Service plan P1v3, default capacity 2, with autoscale rules (CPU > 70% scale out, CPU < 30% scale in, min 2/max 5).
- Linux Web App with HTTPS only, Always On, `/health/ready`, disabled FTP publishing, .NET 10 runtime, Application Insights, and SQL managed identity connection string.
- Azure SQL server with Entra-only authentication and a serverless General Purpose database named `lifecore`.
- Azure Load Testing resource for JMeter test definitions.
- Playwright Workspace with Entra-only local auth disabled, reporting enabled, and regional affinity enabled.

Default region is `eastus`; Playwright Workspaces currently list support for East US, West US 3, West Europe, East Asia, Japan East, Australia East, and Switzerland North. Use `playwrightLocation` if your main app region differs.

## Deployment modes

### Primary: GitHub Actions (`DEPLOYMENT_MODE=githubActions`)

The primary deployment path is repo-owned GitHub Actions using OIDC and the GitHub managed identity. The workflow builds, tests, publishes, and deploys with `azure/webapps-deploy@v3`.

> Important: `azure/webapps-deploy` uses OneDeploy/ZipDeploy under the hood. This repo surfaces that clearly because some subscriptions block ZIP DEPLOY by policy.

### Fallback: App Service Build Service (`DEPLOYMENT_MODE=appServiceBuild`)

If GitHub Actions deployment fails with a policy/403 ZIP DEPLOY denial, switch to App Service Build Service. In this mode App Service/Kudu clones GitHub on push and builds with Oryx.

Switch procedure:

```bash
azd env set DEPLOYMENT_MODE appServiceBuild
azd provision
gh variable set DEPLOYMENT_MODE --body appServiceBuild
```

Register a GitHub token for App Service once, either in Portal Deployment Center or with:

```bash
az webapp deployment source update-token --git-token <PAT>
```

Then push to `main`; the App Service source-control webhook performs the build. If Oryx does not yet support `net10.0`, use the optional `deploy/kudu-deploy.sh` script by changing the root `.deployment` file to:

```ini
[config]
command = deploy/kudu-deploy.sh
```

The default `.deployment` remains project-based and is not changed by the infrastructure work.

## Optional Deployment Center link

Parameter `linkDeploymentCenter` defaults to `false`. Setting it to `true` in GitHub Actions mode creates a `Microsoft.Web/sites/sourcecontrols` resource with `isGitHubAction: true` so Portal Deployment Center shows GitHub Actions. It can fail unless App Service already has an authorized GitHub token, so keep it off for normal `azd provision`.

## First-time setup

1. Sign in locally:

   ```bash
   az login
   azd auth login
   ```

2. Create/select an environment:

   ```bash
   azd env new <env-name>
   azd env set AZURE_LOCATION eastus
   azd env set DEPLOYMENT_MODE githubActions
   ```

3. Ensure the SQL administrator values exist. The preprovision hook sets `AZURE_PRINCIPAL_NAME` from the signed-in user when possible. `AZURE_PRINCIPAL_ID` is supplied by azd.

4. Provision infrastructure locally for the first run:

   ```bash
   azd provision
   ```

   First provision generally needs Owner or User Access Administrator at subscription/resource-group scope because Bicep creates role assignments for the GitHub identity.

5. Let the postprovision hook create the SQL contained user for the app managed identity. It temporarily adds your client IP to the SQL firewall and runs:

   ```sql
   CREATE USER [<APP_IDENTITY_NAME>] FROM EXTERNAL PROVIDER;
   ALTER ROLE db_datareader ADD MEMBER [<APP_IDENTITY_NAME>];
   ALTER ROLE db_datawriter ADD MEMBER [<APP_IDENTITY_NAME>];
   ALTER ROLE db_ddladmin ADD MEMBER [<APP_IDENTITY_NAME>];
   ```

   Install `sqlcmd` first if needed: `brew install sqlcmd` on macOS or `winget install sqlcmd` on Windows.

6. Set GitHub repository variables. The postprovision hook prints them, or set `SET_GH_VARS=true` and rerun it with an authenticated `gh` CLI.

## Required GitHub repository variables

- `AZURE_CLIENT_ID` — GitHub managed identity client ID.
- `AZURE_TENANT_ID`.
- `AZURE_SUBSCRIPTION_ID`.
- `AZURE_ENV_NAME` — used by the manual provision workflow.
- `AZURE_LOCATION` — used by the manual provision workflow.
- `AZURE_PRINCIPAL_ID` and `AZURE_PRINCIPAL_NAME` — required only if running `provision.yml` from GitHub.
- `WEB_APP_NAME`.
- `AZURE_RESOURCE_GROUP`.
- `LOAD_TEST_RESOURCE_NAME`.
- `PLAYWRIGHT_SERVICE_URL`.
- `WEB_URL`.
- `DEPLOYMENT_MODE` — `githubActions` or `appServiceBuild`.

## Workflows

- `ci.yml`: restore, build, and unit tests on pull requests and non-main pushes.
- `deploy-app.yml`: main-branch deployment in GitHub Actions mode, readiness polling, then smoke load tests and UI tests.
- `provision.yml`: manual `azd provision` using OIDC. The GitHub identity needs elevated RBAC to create role assignments, so expect the first provision to be local.
- `ui-tests.yml`: reusable/manual Playwright Workspace test workflow using `PLAYWRIGHT_SERVICE_URL` and `WEB_URL`.
- `load-tests.yml`: reusable/manual Azure Load Testing workflow. Profiles map to `loadtests/smoke.yaml`, `loadtests/underwriter-journey.yaml`, `loadtests/csr-policy-servicing.yaml`, and `loadtests/mixed-peak.yaml`.

## Cost and teardown

P1v3 App Service, Azure SQL Database, Playwright Workspaces, and Azure Load Testing can incur costs. Tear down demo resources when not needed:

```bash
azd down --purge
```

`azd down --purge` is destructive and removes provisioned resources for the selected environment.
