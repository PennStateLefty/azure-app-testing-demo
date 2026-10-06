targetScope = 'subscription'

@description('azd environment name. Used for resource naming and tags.')
param environmentName string

@description('Primary Azure region for the resource group and most resources.')
param location string = 'westus3'

@description('Region for the Playwright Workspace. Must have a Playwright Workspaces endpoint (westus3 is listed but has none). A different region from location gets its own resource group.')
param playwrightLocation string = 'eastus'

@description('Object ID for the principal that becomes the Entra-only Azure SQL administrator.')
param sqlAdminObjectId string

@description('Display/login name for the Entra-only Azure SQL administrator.')
param sqlAdminLogin string

@description('When true (default), the app managed identity is the SQL Entra admin. Needed when SQL public access is disabled, because no contained user can be created from outside the VNet.')
param sqlAdminIsAppIdentity bool = true

@allowed([
  'User'
  'Group'
  'Application'
])
@description('Principal type for the Azure SQL Entra administrator.')
param sqlAdminPrincipalType string = 'User'

@description('Tenant ID used for SQL Entra admin and GitHub OIDC federated credentials.')
param tenantId string = tenant().tenantId

@allowed([
  'githubActions'
  'appServiceBuild'
])
@description('Application deployment mode. githubActions is primary; appServiceBuild configures App Service/Kudu GitHub build fallback.')
param deploymentMode string = 'githubActions'

@description('Repository URL used by App Service Build Service fallback.')
param repoUrl string = 'https://github.com/PennStateLefty/azure-app-testing-demo'

@description('Branch used by GitHub Actions federation and App Service Build Service fallback.')
param branch string = 'main'

@description('GitHub Environment name for protected deploys and OIDC federation.')
param githubEnvironment string = 'demo'

@description('When true, creates an App Service Deployment Center sourcecontrol link for GitHub Actions portal visibility. Requires App Service to have a registered GitHub token.')
param linkDeploymentCenter bool = false

@description('Linux App Service plan SKU name.')
param appServicePlanSku string = 'P1v3'

@description('Linux App Service plan worker count.')
@minValue(2)
param appServicePlanCapacity int = 2

@description('Feature flag for optimized EF queries. Default false supports the first load-test bottleneck demo.')
param useOptimizedQueries bool = false

@description('SQL database name.')
param sqlDatabaseName string = 'lifecore'

@description('SQL database serverless compute capacity.')
param sqlDatabaseCapacity int = 2

@description('SQL database serverless minimum capacity.')
param sqlDatabaseMinCapacity string = '0.5'

@description('SQL database auto-pause delay in minutes. -1 disables auto-pause for demos.')
param sqlDatabaseAutoPauseDelay int = -1

var resourceGroupName = 'rg-lifecore-${environmentName}'
var tags = {
  'azd-env-name': environmentName
  workload: 'lifecore-suite'
}

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

// Playwright Workspaces preflight validation is routed by resource group region, so a workspace
// outside the primary region gets its own resource group in that region.
var separatePlaywrightGroup = toLower(replace(playwrightLocation, ' ', '')) != toLower(replace(location, ' ', ''))
var playwrightResourceGroupName = separatePlaywrightGroup ? '${resourceGroupName}-pw' : resourceGroupName

resource playwrightRg 'Microsoft.Resources/resourceGroups@2024-03-01' = if (separatePlaywrightGroup) {
  name: playwrightResourceGroupName
  location: playwrightLocation
  tags: tags
}

module resources 'modules/lifecore.bicep' = {
  name: 'lifecore-${environmentName}'
  scope: rg
  params: {
    environmentName: environmentName
    location: location
    tags: tags
    sqlAdminObjectId: sqlAdminObjectId
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPrincipalType: sqlAdminPrincipalType
    sqlAdminIsAppIdentity: sqlAdminIsAppIdentity
    tenantId: tenantId
    deploymentMode: deploymentMode
    repoUrl: repoUrl
    branch: branch
    githubEnvironment: githubEnvironment
    linkDeploymentCenter: linkDeploymentCenter
    appServicePlanSku: appServicePlanSku
    appServicePlanCapacity: appServicePlanCapacity
    useOptimizedQueries: useOptimizedQueries
    sqlDatabaseName: sqlDatabaseName
    sqlDatabaseCapacity: sqlDatabaseCapacity
    sqlDatabaseMinCapacity: sqlDatabaseMinCapacity
    sqlDatabaseAutoPauseDelay: sqlDatabaseAutoPauseDelay
  }
}

module playwright 'modules/playwright.bicep' = {
  name: 'lifecore-playwright-${environmentName}'
  scope: resourceGroup(playwrightResourceGroupName)
  dependsOn: [
    playwrightRg
  ]
  params: {
    name: 'pw-${toLower(uniqueString(subscription().id, resourceGroupName, environmentName))}'
    location: playwrightLocation
    tags: tags
    githubIdentityPrincipalId: resources.outputs.GITHUB_IDENTITY_PRINCIPAL_ID
  }
}

output AZURE_RESOURCE_GROUP string = rg.name
output WEB_APP_NAME string = resources.outputs.WEB_APP_NAME
output WEB_URL string = resources.outputs.WEB_URL
output SQL_SERVER_NAME string = resources.outputs.SQL_SERVER_NAME
output SQL_SERVER_FQDN string = resources.outputs.SQL_SERVER_FQDN
output SQL_DATABASE_NAME string = resources.outputs.SQL_DATABASE_NAME
output APP_IDENTITY_NAME string = resources.outputs.APP_IDENTITY_NAME
output APP_IDENTITY_CLIENT_ID string = resources.outputs.APP_IDENTITY_CLIENT_ID
output GITHUB_IDENTITY_CLIENT_ID string = resources.outputs.GITHUB_IDENTITY_CLIENT_ID
output GITHUB_IDENTITY_PRINCIPAL_ID string = resources.outputs.GITHUB_IDENTITY_PRINCIPAL_ID
output LOAD_TEST_RESOURCE_NAME string = resources.outputs.LOAD_TEST_RESOURCE_NAME
output PLAYWRIGHT_WORKSPACE_NAME string = playwright.outputs.PLAYWRIGHT_WORKSPACE_NAME
output PLAYWRIGHT_RESOURCE_GROUP string = playwrightResourceGroupName
output PLAYWRIGHT_SERVICE_URL string = playwright.outputs.PLAYWRIGHT_SERVICE_URL
#disable-next-line outputs-should-not-contain-secrets
output APPLICATIONINSIGHTS_CONNECTION_STRING string = resources.outputs.APPLICATIONINSIGHTS_CONNECTION_STRING
output APP_SERVICE_PLAN_ID string = resources.outputs.APP_SERVICE_PLAN_ID
output WEB_APP_ID string = resources.outputs.WEB_APP_ID
output SQL_DATABASE_ID string = resources.outputs.SQL_DATABASE_ID
output APP_INSIGHTS_ID string = resources.outputs.APP_INSIGHTS_ID
output DEPLOYMENT_MODE string = deploymentMode
output SQL_ADMIN_IS_APP_IDENTITY bool = resources.outputs.SQL_ADMIN_IS_APP_IDENTITY
