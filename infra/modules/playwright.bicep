targetScope = 'resourceGroup'

param name string
param location string
param tags object
param githubIdentityPrincipalId string
param testRunnerPrincipalIds array = []

@description('Extra inbound IPv4 CIDRs allowed through the network security perimeter (for example, portal report viewers).')
param reportViewerAddressPrefixes array = []

var contributorRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
var storageBlobDataContributorRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
var storageAccountName = 'stpw${uniqueString(resourceGroup().id, name)}'
var storageBlobDataContributorPrincipalIds = union([
  githubIdentityPrincipalId
], testRunnerPrincipalIds)

resource reportingStorage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'SecuredByPerimeter'
    supportsHttpsTrafficOnly: true
  }
}

resource reportingBlobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: reportingStorage
  name: 'default'
  properties: {
    cors: {
      corsRules: [
        {
          allowedHeaders: [
            '*'
          ]
          allowedMethods: [
            'GET'
            'OPTIONS'
          ]
          allowedOrigins: [
            'https://trace.playwright.dev'
          ]
          exposedHeaders: [
            '*'
          ]
          maxAgeInSeconds: 3600
        }
      ]
    }
  }
}

// Tenant policy disables public network access on PaaS storage; a network security perimeter
// re-opens the endpoint only to identities in this subscription (GitHub OIDC runner identity).
resource reportingPerimeter 'Microsoft.Network/networkSecurityPerimeters@2023-08-01-preview' = {
  name: 'nsp-${name}'
  location: location
  tags: tags
}

resource reportingPerimeterProfile 'Microsoft.Network/networkSecurityPerimeters/profiles@2023-08-01-preview' = {
  parent: reportingPerimeter
  name: 'reporting'
  location: location
}

resource allowSubscriptionInbound 'Microsoft.Network/networkSecurityPerimeters/profiles/accessRules@2023-08-01-preview' = {
  parent: reportingPerimeterProfile
  name: 'allow-subscription'
  location: location
  properties: {
    direction: 'Inbound'
    subscriptions: [
      {
        id: subscription().id
      }
    ]
  }
}

resource allowViewerInbound 'Microsoft.Network/networkSecurityPerimeters/profiles/accessRules@2023-08-01-preview' = if (!empty(reportViewerAddressPrefixes)) {
  parent: reportingPerimeterProfile
  name: 'allow-report-viewers'
  location: location
  properties: {
    direction: 'Inbound'
    addressPrefixes: reportViewerAddressPrefixes
  }
}

resource reportingPerimeterAssociation 'Microsoft.Network/networkSecurityPerimeters/resourceAssociations@2023-08-01-preview' = {
  parent: reportingPerimeter
  name: 'stpw-reporting'
  location: location
  properties: {
    accessMode: 'Enforced'
    privateLinkResource: {
      id: reportingStorage.id
    }
    profile: {
      id: reportingPerimeterProfile.id
    }
  }
}

resource playwrightWorkspace 'Microsoft.LoadTestService/playwrightWorkspaces@2026-08-01-preview' = {
  name: name
  location: location
  tags: tags
  properties: {
    localAuth: 'Disabled'
    regionalAffinity: 'Enabled'
    reporting: 'Enabled'
    storageUri: reportingStorage.properties.primaryEndpoints.blob
  }
}

resource githubPlaywrightContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(playwrightWorkspace.id, githubIdentityPrincipalId, contributorRoleId)
  scope: playwrightWorkspace
  properties: {
    roleDefinitionId: contributorRoleId
    principalId: githubIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource reportingStorageBlobContributors 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for principalId in storageBlobDataContributorPrincipalIds: {
  name: guid(reportingStorage.id, principalId, storageBlobDataContributorRoleId)
  scope: reportingStorage
  properties: {
    roleDefinitionId: storageBlobDataContributorRoleId
    principalId: principalId
  }
}]

output PLAYWRIGHT_WORKSPACE_NAME string = playwrightWorkspace.name
output PLAYWRIGHT_SERVICE_URL string = '${replace(playwrightWorkspace.properties.dataplaneUri, 'https://', 'wss://')}/browsers'
output PLAYWRIGHT_REPORT_STORAGE_ACCOUNT_NAME string = reportingStorage.name
