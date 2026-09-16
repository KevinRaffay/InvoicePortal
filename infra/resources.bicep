// Resource-group scoped resources for the Invoice Portal Admin container.
//
//   Container Apps environment + app   hosts the Blazor Server container (sticky sessions, 1 replica)
//   Container Registry                 image pushed by `azd deploy`; pulled with the managed identity
//   User-assigned managed identity     app identity for SQL, Blob, Azure OpenAI and ACR (no passwords)
//   Storage account                    ASP.NET Core Data Protection key ring (antiforgery survives restarts)
//   Log Analytics + App Insights       diagnostics
//   Azure OpenAI + one deployment      real model behind the app's IChatClient (optional)
//
// Not created here: the Azure SQL database (existing) and the Entra app registration (see docs/azure-deploy.md).

param environmentName string
param location string
param tags object
param sqlServerFqdn string
param sqlDatabaseName string
param entraClientId string
param entraTenantId string
@secure()
param entraClientSecret string
param deployOpenAi bool
param openAiModelName string
param openAiModelVersion string
param openAiCapacity int
param appExists bool

var resourceToken = toLower(uniqueString(subscription().id, resourceGroup().id, environmentName))
var identityName = 'id-app-${resourceToken}'
var registryName = 'cr${resourceToken}'
var storageName = 'st${resourceToken}'
var logAnalyticsName = 'log-${resourceToken}'
var appInsightsName = 'appi-${resourceToken}'
var environmentNameCae = 'cae-${resourceToken}'
var appName = 'ca-app-${resourceToken}'
var openAiName = 'oai-${resourceToken}'
var dataProtectionContainer = 'dataprotection'
var placeholderImage = 'mcr.microsoft.com/dotnet/samples:aspnetapp' // listens on 8080 until azd deploy pushes the real image

// Built-in role definition IDs
var acrPullRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
var blobDataContributorRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
var openAiUserRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')

// ---------------------------------------------------------------- identity

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
  tags: tags
}

// ---------------------------------------------------------------- diagnostics

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// ---------------------------------------------------------------- container registry

resource registry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: registryName
  location: location
  tags: tags
  sku: { name: 'Basic' }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, identity.id, acrPullRole)
  scope: registry
  properties: {
    roleDefinitionId: acrPullRole
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// ---------------------------------------------------------------- data protection key storage

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource keyContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: dataProtectionContainer
  properties: { publicAccess: 'None' }
}

resource blobContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, identity.id, blobDataContributorRole)
  scope: storage
  properties: {
    roleDefinitionId: blobDataContributorRole
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// ---------------------------------------------------------------- Azure OpenAI (optional)

resource openAi 'Microsoft.CognitiveServices/accounts@2024-10-01' = if (deployOpenAi) {
  name: openAiName
  location: location
  tags: tags
  kind: 'OpenAI'
  sku: { name: 'S0' }
  properties: {
    customSubDomainName: openAiName
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true // Entra ID only; the app authenticates with its managed identity
  }
}

resource openAiDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = if (deployOpenAi) {
  parent: openAi
  name: openAiModelName
  sku: {
    name: 'GlobalStandard'
    capacity: openAiCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: openAiModelName
      version: openAiModelVersion
    }
  }
}

resource openAiUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (deployOpenAi) {
  name: guid(resourceGroup().id, identity.id, openAiUserRole, openAiName)
  scope: openAi
  properties: {
    roleDefinitionId: openAiUserRole
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// ---------------------------------------------------------------- container apps

resource containerEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: environmentNameCae
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// Keep the image that `azd deploy` pushed when re-running `azd provision`.
module currentImage 'modules/fetch-container-image.bicep' = {
  name: 'current-image'
  params: {
    exists: appExists
    name: appName
  }
}

var appImage = appExists ? currentImage.outputs.image : placeholderImage

var connectionString = 'Server=tcp:${sqlServerFqdn},1433;Database=${sqlDatabaseName};Authentication=Active Directory Managed Identity;User Id=${identity.properties.clientId};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

var appEnv = [
  { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
  { name: 'ASPNETCORE_HTTP_PORTS', value: '8080' }
  // TLS terminates at the Container Apps ingress; trust X-Forwarded-* so generated URLs are https.
  { name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
  // DefaultAzureCredential picks this user-assigned identity for SQL, Blob and Azure OpenAI.
  { name: 'AZURE_CLIENT_ID', value: identity.properties.clientId }
  { name: 'ConnectionStrings__InvoicePortal', value: connectionString }
  { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
  { name: 'DataProtection__BlobUri', value: '${storage.properties.primaryEndpoints.blob}${dataProtectionContainer}/keys.xml' }
  { name: 'Ai__Enabled', value: 'true' }
  { name: 'Ai__DocumentChatEnabled', value: 'true' }
  { name: 'Ai__Provider', value: deployOpenAi ? 'AzureOpenAI' : 'Mock' }
  { name: 'Ai__AzureOpenAI__Endpoint', value: deployOpenAi ? openAi.properties.endpoint : '' }
  { name: 'Ai__AzureOpenAI__Deployment', value: openAiModelName }
]

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: appName
  location: location
  tags: union(tags, { 'azd-service-name': 'app' }) // must match the service name in azure.yaml
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    managedEnvironmentId: containerEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto' // HTTP/1.1 + WebSockets for the Blazor Server circuit
        allowInsecure: false
        stickySessions: { affinity: 'sticky' } // Blazor Server needs the circuit to hit the same replica
      }
      registries: [
        {
          server: registry.properties.loginServer
          identity: identity.id
        }
      ]
      secrets: empty(entraClientSecret) ? [] : [
        {
          name: 'entra-client-secret'
          value: entraClientSecret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'app'
          image: appImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: appEnv
        }
      ]
      scale: {
        minReplicas: 1 // no scale-to-zero: circuits and the in-memory document index live in the process
        maxReplicas: 1 // more replicas would need Azure SignalR Service
      }
    }
  }
  dependsOn: [ acrPull ]
}

// Built-in authentication (Easy Auth) with Entra ID. Skipped when no client ID is supplied.
resource auth 'Microsoft.App/containerApps/authConfigs@2024-03-01' = if (!empty(entraClientId)) {
  parent: app
  name: 'current'
  properties: {
    platform: { enabled: true }
    globalValidation: {
      unauthenticatedClientAction: 'RedirectToLoginPage'
      redirectToProvider: 'azureactivedirectory'
    }
    identityProviders: {
      azureActiveDirectory: {
        enabled: true
        registration: {
          clientId: entraClientId
          clientSecretSettingName: 'entra-client-secret'
          openIdIssuer: '${environment().authentication.loginEndpoint}${entraTenantId}/v2.0'
        }
        validation: {
          allowedAudiences: [ 'api://${entraClientId}' ]
        }
      }
    }
    login: {
      preserveUrlFragmentsForLogins: false
    }
  }
}

output containerRegistryLoginServer string = registry.properties.loginServer
output managedIdentityName string = identity.name
output managedIdentityClientId string = identity.properties.clientId
output openAiEndpoint string = deployOpenAi ? openAi.properties.endpoint : ''
output appName string = app.name
output appUri string = 'https://${app.properties.configuration.ingress.fqdn}'
