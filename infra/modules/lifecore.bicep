param environmentName string
param location string
param playwrightLocation string
param tags object
param sqlAdminObjectId string
param sqlAdminLogin string
param sqlAdminPrincipalType string
param tenantId string

@allowed([
  'githubActions'
  'appServiceBuild'
])
param deploymentMode string

param repoUrl string
param branch string
param githubEnvironment string
param linkDeploymentCenter bool
param appServicePlanSku string
param appServicePlanCapacity int
param useOptimizedQueries bool
param sqlDatabaseName string
param sqlDatabaseCapacity int
param sqlDatabaseMinCapacity string
param sqlDatabaseAutoPauseDelay int

var resourceToken = toLower(uniqueString(subscription().id, resourceGroup().id, environmentName, location))
var nameSuffix = '${environmentName}-${resourceToken}'
var repoOwnerName = 'PennStateLefty/azure-app-testing-demo'
var mainSubject = 'repo:${repoOwnerName}:ref:refs/heads/${branch}'
var environmentSubject = 'repo:${repoOwnerName}:environment:${githubEnvironment}'
var pullRequestSubject = 'repo:${repoOwnerName}:pull_request'
var oidcIssuer = 'https://token.actions.githubusercontent.com'
var oidcAudience = 'api://AzureADTokenExchange'

var logAnalyticsName = 'log-lifecore-${nameSuffix}'
var appInsightsName = 'appi-lifecore-${nameSuffix}'
var appIdentityName = 'id-app-${nameSuffix}'
var githubIdentityName = 'id-github-${nameSuffix}'
var planName = 'plan-lifecore-${nameSuffix}'
var webAppName = 'app-lifecore-${resourceToken}'
var sqlServerName = 'sql-lifecore-${resourceToken}'
var loadTestName = 'lt-lifecore-${resourceToken}'
var playwrightName = 'pw-${resourceToken}'

var contributorRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab3b-0e9a2e1b6b8b')
var readerRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'acdd72a7-3385-48ef-bd42-f606fba81ae7')
var websiteContributorRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'de139f84-1756-47ae-9be6-808fbbe84772')
var loadTestContributorRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '749a398d-560b-491b-bb21-08924219302e')

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
  }
}

resource appIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: appIdentityName
  location: location
  tags: tags
}

resource githubIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: githubIdentityName
  location: location
  tags: tags
}

resource githubMainFederation 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = {
  parent: githubIdentity
  name: 'github-main'
  properties: {
    issuer: oidcIssuer
    subject: mainSubject
    audiences: [
      oidcAudience
    ]
  }
}

resource githubEnvironmentFederation 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = {
  parent: githubIdentity
  name: 'github-environment-${githubEnvironment}'
  properties: {
    issuer: oidcIssuer
    subject: environmentSubject
    audiences: [
      oidcAudience
    ]
  }
}

resource githubPullRequestFederation 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = {
  parent: githubIdentity
  name: 'github-pull-request'
  properties: {
    issuer: oidcIssuer
    subject: pullRequestSubject
    audiences: [
      oidcAudience
    ]
  }
}

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  tags: tags
  sku: {
    name: appServicePlanSku
    capacity: appServicePlanCapacity
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  tags: union(tags, {
    'azd-service-name': 'web'
  })
  kind: 'app,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${appIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      healthCheckPath: '/health/ready'
      appSettings: [
        {
          name: 'Database__Provider'
          value: 'SqlServer'
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: appIdentity.properties.clientId
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'Perf__UseOptimizedQueries'
          value: string(useOptimizedQueries)
        }
        {
          name: 'Admin__EnableReset'
          value: 'true'
        }
        {
          name: 'Seed__OnStartup'
          value: 'true'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'WEBSITE_HEALTHCHECK_MAXPINGFAILURES'
          value: '5'
        }
        {
          name: 'SCM_DO_BUILD_DURING_DEPLOYMENT'
          value: deploymentMode == 'appServiceBuild' ? 'true' : 'false'
        }
        {
          name: 'PROJECT'
          value: 'src/LifeCore.Web/LifeCore.Web.csproj'
        }
      ]
      connectionStrings: [
        {
          name: 'LifeCore'
          connectionString: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDatabaseName};Authentication=Active Directory Managed Identity;User Id=${appIdentity.properties.clientId};Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;'
          type: 'SQLAzure'
        }
      ]
    }
  }
}

resource ftpPublishingCredentials 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2023-12-01' = {
  parent: webApp
  name: 'ftp'
  properties: {
    allow: false
  }
}

resource scmPublishingCredentials 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2023-12-01' = {
  parent: webApp
  name: 'scm'
  properties: {
    allow: deploymentMode == 'appServiceBuild'
  }
}

resource fallbackSourceControl 'Microsoft.Web/sites/sourcecontrols@2023-12-01' = if (deploymentMode == 'appServiceBuild') {
  parent: webApp
  name: 'web'
  properties: {
    repoUrl: repoUrl
    branch: branch
    isManualIntegration: false
    isGitHubAction: false
    deploymentRollbackEnabled: false
  }
}

resource githubActionsSourceControl 'Microsoft.Web/sites/sourcecontrols@2023-12-01' = if (deploymentMode == 'githubActions' && linkDeploymentCenter) {
  parent: webApp
  name: 'web'
  properties: {
    repoUrl: repoUrl
    branch: branch
    isManualIntegration: true
    isGitHubAction: true
    deploymentRollbackEnabled: false
    gitHubActionConfiguration: {
      generateWorkflowFile: false
      isLinux: true
      codeConfiguration: {
        runtimeStack: 'dotnetcore'
        runtimeVersion: '10.0'
      }
    }
  }
}

resource autoscale 'Microsoft.Insights/autoscalesettings@2022-10-01' = {
  name: 'autoscale-${plan.name}'
  location: location
  tags: tags
  properties: {
    enabled: true
    targetResourceUri: plan.id
    profiles: [
      {
        name: 'cpu-autoscale'
        capacity: {
          minimum: '2'
          maximum: '5'
          default: string(appServicePlanCapacity)
        }
        rules: [
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: plan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT5M'
              timeAggregation: 'Average'
              operator: 'GreaterThan'
              threshold: 70
            }
            scaleAction: {
              direction: 'Increase'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT5M'
            }
          }
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: plan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT10M'
              timeAggregation: 'Average'
              operator: 'LessThan'
              threshold: 30
            }
            scaleAction: {
              direction: 'Decrease'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT10M'
            }
          }
        ]
      }
    ]
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: sqlAdminLogin
      sid: sqlAdminObjectId
      principalType: sqlAdminPrincipalType
      tenantId: tenantId
    }
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource allowAzureSqlFirewall 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDb 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: tags
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: sqlDatabaseCapacity
  }
  properties: {
    minCapacity: json(sqlDatabaseMinCapacity)
    autoPauseDelay: sqlDatabaseAutoPauseDelay
  }
}

resource loadTest 'Microsoft.LoadTestService/loadTests@2022-12-01' = {
  name: loadTestName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    description: 'LifeCore Suite Azure Load Testing resource for JMeter pipeline runs.'
  }
}

resource playwrightWorkspace 'Microsoft.LoadTestService/playwrightWorkspaces@2025-09-01' = {
  name: playwrightName
  location: playwrightLocation
  tags: tags
  properties: {
    localAuth: 'Disabled'
    regionalAffinity: 'Enabled'
    #disable-next-line BCP037
    reporting: 'Enabled'
  }
}

resource githubWebContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(webApp.id, githubIdentity.id, websiteContributorRoleId)
  scope: webApp
  properties: {
    roleDefinitionId: websiteContributorRoleId
    principalId: githubIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource githubLoadTestContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(loadTest.id, githubIdentity.id, loadTestContributorRoleId)
  scope: loadTest
  properties: {
    roleDefinitionId: loadTestContributorRoleId
    principalId: githubIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource githubPlaywrightContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(playwrightWorkspace.id, githubIdentity.id, contributorRoleId)
  scope: playwrightWorkspace
  properties: {
    roleDefinitionId: contributorRoleId
    principalId: githubIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource githubResourceGroupReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, githubIdentity.id, readerRoleId)
  properties: {
    roleDefinitionId: readerRoleId
    principalId: githubIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

output WEB_APP_NAME string = webApp.name
output WEB_URL string = 'https://${webApp.properties.defaultHostName}'
output SQL_SERVER_NAME string = sqlServer.name
output SQL_SERVER_FQDN string = sqlServer.properties.fullyQualifiedDomainName
output SQL_DATABASE_NAME string = sqlDb.name
output APP_IDENTITY_NAME string = appIdentity.name
output APP_IDENTITY_CLIENT_ID string = appIdentity.properties.clientId
output GITHUB_IDENTITY_CLIENT_ID string = githubIdentity.properties.clientId
output GITHUB_IDENTITY_PRINCIPAL_ID string = githubIdentity.properties.principalId
output LOAD_TEST_RESOURCE_NAME string = loadTest.name
output PLAYWRIGHT_WORKSPACE_NAME string = playwrightWorkspace.name
output PLAYWRIGHT_SERVICE_URL string = playwrightWorkspace.properties.dataplaneUri
#disable-next-line outputs-should-not-contain-secrets
output APPLICATIONINSIGHTS_CONNECTION_STRING string = appInsights.properties.ConnectionString
output APP_SERVICE_PLAN_ID string = plan.id
output WEB_APP_ID string = webApp.id
output SQL_DATABASE_ID string = sqlDb.id
output APP_INSIGHTS_ID string = appInsights.id
