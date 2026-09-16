targetScope = 'subscription'

// Entry point for `azd provision`. Creates the resource group and delegates to resources.bicep.
// The SQL database is NOT created here: the app points at the existing Azure SQL database, whose
// server name and database name are passed in (see main.parameters.json / docs/azure-deploy.md).

@description('Short azd environment name; used for the resource group tag and resource naming.')
@minLength(1)
@maxLength(64)
param environmentName string

@description('Azure region for every resource.')
param location string

@description('Resource group name. Defaults to rg-<environmentName>.')
param resourceGroupName string = 'rg-${environmentName}'

@description('Fully qualified name of the existing Azure SQL server, as shown on its Overview page in the portal.')
param sqlServerFqdn string

@description('Name of the existing database on that server.')
param sqlDatabaseName string

@description('Entra ID application (client) ID used for sign-in. Leave empty to deploy WITHOUT authentication (POC only).')
param entraClientId string = ''

@description('Tenant that issues sign-in tokens. Defaults to the deployment tenant.')
param entraTenantId string = tenant().tenantId

@secure()
@description('Client secret of the Entra ID application. Required when entraClientId is set.')
param entraClientSecret string = ''

@description('Provision Azure OpenAI and switch the app to the AzureOpenAI provider. When false the app runs the offline Mock provider.')
param deployOpenAi bool = true

@description('Chat model to deploy.')
param openAiModelName string = 'gpt-4.1-mini'

@description('Model version available in the selected region.')
param openAiModelVersion string = '2025-04-14'

@description('GlobalStandard throughput (thousands of tokens per minute).')
@minValue(1)
param openAiCapacity int = 10

@description('Set by azd (SERVICE_APP_RESOURCE_EXISTS) so re-provisioning keeps the currently deployed image.')
param appExists bool = false

var tags = {
  'azd-env-name': environmentName
  project: 'invoiceportal-admin'
}

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module resources 'resources.bicep' = {
  name: 'resources'
  scope: rg
  params: {
    environmentName: environmentName
    location: location
    tags: tags
    sqlServerFqdn: sqlServerFqdn
    sqlDatabaseName: sqlDatabaseName
    entraClientId: entraClientId
    entraTenantId: entraTenantId
    entraClientSecret: entraClientSecret
    deployOpenAi: deployOpenAi
    openAiModelName: openAiModelName
    openAiModelVersion: openAiModelVersion
    openAiCapacity: openAiCapacity
    appExists: appExists
  }
}

// azd reads these outputs into the environment (.azure/<env>/.env).
// AZURE_CONTAINER_REGISTRY_ENDPOINT is the conventional name azd uses to find where to push images.
output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = resources.outputs.containerRegistryLoginServer
output AZURE_MANAGED_IDENTITY_NAME string = resources.outputs.managedIdentityName
output AZURE_MANAGED_IDENTITY_CLIENT_ID string = resources.outputs.managedIdentityClientId
output AZURE_OPENAI_ENDPOINT string = resources.outputs.openAiEndpoint
output SERVICE_APP_URI string = resources.outputs.appUri
output SERVICE_APP_NAME string = resources.outputs.appName
