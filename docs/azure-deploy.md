# Deploying to Azure with azd

The deployment follows the pattern of [DanWahlin/customer-insights](https://github.com/DanWahlin/customer-insights):
`azure.yaml` describes the service, `infra/*.bicep` describes the resources, and the Azure Developer CLI
(`azd`) provisions and deploys. GitHub Actions (`.github/workflows/azure-dev.yml`) runs the same `azd`
commands on every push to `main`.

Unlike the reference, the app itself is hosted in Azure (Container Apps). The **database is not created**:
the container points at the existing Azure SQL database. Nothing in this repo restores the bacpac to Azure,
and nothing should.

## What `azd up` provisions

| Resource | Purpose |
|---|---|
| Resource group `rg-<env>` | Everything below, tagged `azd-env-name` |
| Container Apps environment + app `ca-app-*` | The Blazor Server container: external HTTPS ingress, WebSockets, sticky sessions, exactly 1 replica |
| Container Registry `cr*` (Basic) | Image pushed by `azd deploy`, pulled with the managed identity |
| User-assigned managed identity `id-app-*` | The app's identity for SQL, Blob, Azure OpenAI and ACR. No passwords or keys anywhere |
| Storage account `st*` | ASP.NET Core Data Protection key ring, so antiforgery cookies survive restarts |
| Log Analytics + Application Insights | Container logs, plus the app's OpenTelemetry traces, metrics and logs (the template sets `APPLICATIONINSIGHTS_CONNECTION_STRING`; see README "Telemetry") |
| Azure OpenAI `oai-*` + `gpt-4.1-mini` deployment | Real model behind the same `IChatClient` as the mock. Skip with `DEPLOY_OPENAI=false` |
| Easy Auth (Entra ID) on the container app | Sign-in, only when `ENTRA_CLIENT_ID` is set |

## Prerequisites

- [Azure Developer CLI](https://aka.ms/azd-install), Docker Desktop, and Contributor + User Access Administrator
  (or Owner) on the target subscription (role assignments are created by the template).
- The subscription must be enabled for Azure OpenAI, or set `DEPLOY_OPENAI=false`.
- The existing Azure SQL server must have an **Entra ID admin** configured and allow connections from Azure
  services (or a private endpoint you add yourself). Creating the database user (below) needs that admin.

## First deployment

```bash
azd auth login
```

```bash
azd env new invoiceportal-poc
```

```bash
azd env set AZURE_LOCATION eastus2
```

```bash
azd env set AZURE_SQL_SERVER_FQDN <server>.database.windows.net
```

```bash
azd env set AZURE_SQL_DATABASE_NAME <database>
```

Provision first, because the SQL user needs the managed identity's name:

```bash
azd provision
```

Then, connected to the database as the server's Entra admin, run `infra/sql/create-app-user.sql` with
`<identity-name>` replaced by the value of:

```bash
azd env get-value AZURE_MANAGED_IDENTITY_NAME
```

Now build, push and deploy the container:

```bash
azd deploy
```

`azd env get-value SERVICE_APP_URI` prints the URL. After the first run, `azd up` does provision + deploy in one go.

## Adding sign-in (recommended before sharing the URL)

Without `ENTRA_CLIENT_ID` the app is reachable by anyone who knows the URL. To enable Entra ID sign-in:

1. Create an app registration with the redirect URI `https://<app-fqdn>/.auth/login/aad/callback`
   (the FQDN comes from `SERVICE_APP_URI`) and a client secret.
2. Store both in the azd environment and re-provision:

```bash
azd env set ENTRA_CLIENT_ID <application-client-id>
```

```bash
azd env set ENTRA_CLIENT_SECRET <client-secret>
```

```bash
azd provision
```

Easy Auth runs in front of the container, so no application code changes. Restrict who can sign in through the
app registration's "Assignment required" setting and the enterprise application's users and groups.

## GitHub Actions

`azd pipeline config --provider github` creates a service principal with a federated credential for this
repository (no secrets stored), and sets the `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`,
`AZURE_ENV_NAME` and `AZURE_LOCATION` repository variables. It also offers to generate a workflow; the one
committed at `.github/workflows/azure-dev.yml` is that shape, plus a smoke test, so keep the committed file.

Add the remaining repository variables by hand: `AZURE_SQL_SERVER_FQDN`, `AZURE_SQL_DATABASE_NAME`, and
optionally `DEPLOY_OPENAI`, `ENTRA_CLIENT_ID`; add `ENTRA_CLIENT_SECRET` as a repository **secret**. The
workflow uses a GitHub environment named `azure`, which you can protect with required reviewers.

`ci.yml` runs on pull requests and non-main branches: build, tests, Bicep validation and a Docker build.

## Running the real model locally

`az login` gives `DefaultAzureCredential` a token, so the AzureOpenAI provider also works from a laptop against
the provisioned resource, as long as your user has the "Cognitive Services OpenAI User" role on it:

```powershell
$env:Ai__Provider = 'AzureOpenAI'; $env:Ai__AzureOpenAI__Endpoint = (azd env get-value AZURE_OPENAI_ENDPOINT); dotnet run --project InvoicePortal.Admin --launch-profile http
```

## Costs (rough, per month)

| Item | Estimate |
|---|---|
| Container Apps, 0.5 vCPU / 1 GiB always on | ~$15 |
| Container Registry Basic | ~$5 |
| Storage, Log Analytics, App Insights | a few dollars at POC traffic |
| Azure OpenAI gpt-4.1-mini | cents per hundred queries |

Tear everything down (the SQL database is untouched because it is not part of the template):

```bash
azd down --purge
```
