-- Run ONCE against the Azure SQL database, connected as the server's Entra ID admin.
-- Creates a contained user for the app's user-assigned managed identity so the container can connect
-- with "Authentication=Active Directory Managed Identity" (no password anywhere).
--
-- Replace <identity-name> with:  azd env get-value AZURE_MANAGED_IDENTITY_NAME
--
-- The POC UI edits lookups and invoices, hence db_datawriter. If the deployment is read-only,
-- drop the db_datawriter line. For the AI query page in particular, a separate db_datareader-only
-- login is the production-grade upgrade described in the README.

CREATE USER [<identity-name>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [<identity-name>];
ALTER ROLE db_datawriter ADD MEMBER [<identity-name>];
