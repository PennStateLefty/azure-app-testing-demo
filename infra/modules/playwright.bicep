targetScope = 'resourceGroup'

param name string
param location string
param tags object
param githubIdentityPrincipalId string

var contributorRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')

resource playwrightWorkspace 'Microsoft.LoadTestService/playwrightWorkspaces@2025-09-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    localAuth: 'Disabled'
    regionalAffinity: 'Enabled'
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

output PLAYWRIGHT_WORKSPACE_NAME string = playwrightWorkspace.name
output PLAYWRIGHT_SERVICE_URL string = '${replace(playwrightWorkspace.properties.dataplaneUri, 'https://', 'wss://')}/browsers'
