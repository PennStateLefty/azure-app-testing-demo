param environmentName string
param location string
param tags object
param sqlAdminObjectId string
param sqlAdminLogin string
param sqlAdminPrincipalType string
param sqlAdminIsAppIdentity bool
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

var readerRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'acdd72a7-3385-48ef-bd42-f606fba81ae7')
var websiteContributorRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'de139f84-1756-47ae-9be6-808fbbe84772')
var loadTestContributorRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '749a398d-560b-491b-bb21-08924219302e')
var monitoringReaderRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '43d0d8ad-25c7-4714-9337-8ba259a9fe05')

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
  dependsOn: [
    githubMainFederation
  ]
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
  dependsOn: [
    githubEnvironmentFederation
  ]
  properties: {
    issuer: oidcIssuer
    subject: pullRequestSubject
    audiences: [
      oidcAudience
    ]
  }
}

resource vnet 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: 'vnet-lifecore-${nameSuffix}'
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.20.0.0/16'
      ]
    }
    subnets: [
      {
        name: 'snet-app'
        properties: {
          addressPrefix: '10.20.1.0/24'
          delegations: [
            {
              name: 'appservice'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
      {
        name: 'snet-private-endpoints'
        properties: {
          addressPrefix: '10.20.2.0/24'
        }
      }
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
    virtualNetworkSubnetId: vnet.properties.subnets[0].id
    vnetRouteAllEnabled: true
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      appCommandLine: 'dotnet LifeCore.Web.dll'
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
      login: sqlAdminIsAppIdentity ? appIdentity.name : sqlAdminLogin
      sid: sqlAdminIsAppIdentity ? appIdentity.properties.principalId : sqlAdminObjectId
      principalType: sqlAdminIsAppIdentity ? 'Application' : sqlAdminPrincipalType
      tenantId: tenantId
    }
    minimalTlsVersion: '1.2'
    // Reached only through the private endpoint; many subscriptions enforce this by policy.
    publicNetworkAccess: 'Disabled'
  }
}

resource sqlPrivateDnsZone 'Microsoft.Network/privateDnsZones@2024-06-01' = {
  name: 'privatelink${environment().suffixes.sqlServerHostname}'
  location: 'global'
  tags: tags
}

resource sqlPrivateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = {
  parent: sqlPrivateDnsZone
  name: 'link-${vnet.name}'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.id
    }
  }
}

resource sqlPrivateEndpoint 'Microsoft.Network/privateEndpoints@2024-05-01' = {
  name: 'pe-${sqlServerName}'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: vnet.properties.subnets[1].id
    }
    privateLinkServiceConnections: [
      {
        name: 'sql'
        properties: {
          privateLinkServiceId: sqlServer.id
          groupIds: [
            'sqlServer'
          ]
        }
      }
    ]
  }
}

resource sqlPrivateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-05-01' = {
  parent: sqlPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'sql'
        properties: {
          privateDnsZoneId: sqlPrivateDnsZone.id
        }
      }
    ]
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

resource githubResourceGroupReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, githubIdentity.id, readerRoleId)
  properties: {
    roleDefinitionId: readerRoleId
    principalId: githubIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource loadTestMonitoringReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, loadTest.id, monitoringReaderRoleId)
  properties: {
    roleDefinitionId: monitoringReaderRoleId
    principalId: loadTest.identity.principalId
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
output LOAD_TEST_PRINCIPAL_ID string = loadTest.identity.principalId
#disable-next-line outputs-should-not-contain-secrets
output APPLICATIONINSIGHTS_CONNECTION_STRING string = appInsights.properties.ConnectionString
output APP_SERVICE_PLAN_ID string = plan.id
output WEB_APP_ID string = webApp.id
output SQL_DATABASE_ID string = sqlDb.id
output APP_INSIGHTS_ID string = appInsights.id
output SQL_ADMIN_IS_APP_IDENTITY bool = sqlAdminIsAppIdentity
