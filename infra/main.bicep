param location string = resourceGroup().location

@minLength(3)
@maxLength(30)
param namePrefix string

param deploymentSuffix string = 'main'

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${namePrefix}-plan'
  location: location
  kind: 'linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: true
  }
}

resource app 'Microsoft.Web/sites@2023-12-01' = {
  name: '${namePrefix}-${deploymentSuffix}'
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'NODE|22-lts'
      appCommandLine: 'node src/backend/server.js'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'NODE_ENV'
          value: 'production'
        }
        {
          name: 'SCM_DO_BUILD_DURING_DEPLOYMENT'
          value: 'false'
        }
      ]
    }
  }
}

resource loadTesting 'Microsoft.LoadTestService/loadTests@2022-12-01' = {
  name: '${namePrefix}-load'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {}
}

output appName string = app.name
output appUrl string = 'https://${app.properties.defaultHostName}'
output loadTestingName string = loadTesting.name
