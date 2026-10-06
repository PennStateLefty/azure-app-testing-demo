#!/usr/bin/env sh
set -eu

required_vars="SQL_SERVER_FQDN SQL_DATABASE_NAME APP_IDENTITY_NAME GITHUB_IDENTITY_CLIENT_ID GITHUB_IDENTITY_PRINCIPAL_ID LOAD_TEST_RESOURCE_NAME PLAYWRIGHT_SERVICE_URL WEB_URL WEB_APP_NAME AZURE_RESOURCE_GROUP DEPLOYMENT_MODE AZURE_TENANT_ID AZURE_SUBSCRIPTION_ID"
missing=""
for var_name in $required_vars; do
  eval "value=\${$var_name:-}"
  if [ -z "$value" ]; then
    missing="$missing $var_name"
  fi
done
if [ -n "$missing" ]; then
  echo "Postprovision warning: missing azd output/environment values:$missing" >&2
fi

cat <<VARS

GitHub repository variables to set:
  AZURE_CLIENT_ID=${GITHUB_IDENTITY_CLIENT_ID:-}
  AZURE_TENANT_ID=${AZURE_TENANT_ID:-}
  AZURE_SUBSCRIPTION_ID=${AZURE_SUBSCRIPTION_ID:-}
  WEB_APP_NAME=${WEB_APP_NAME:-}
  AZURE_RESOURCE_GROUP=${AZURE_RESOURCE_GROUP:-}
  LOAD_TEST_RESOURCE_NAME=${LOAD_TEST_RESOURCE_NAME:-}
  PLAYWRIGHT_SERVICE_URL=${PLAYWRIGHT_SERVICE_URL:-}
  WEB_URL=${WEB_URL:-}
  DEPLOYMENT_MODE=${DEPLOYMENT_MODE:-}
VARS

if [ "${SET_GH_VARS:-false}" = "true" ]; then
  if command -v gh >/dev/null 2>&1 && gh auth status >/dev/null 2>&1; then
    gh variable set AZURE_CLIENT_ID --body "${GITHUB_IDENTITY_CLIENT_ID:-}"
    gh variable set AZURE_TENANT_ID --body "${AZURE_TENANT_ID:-}"
    gh variable set AZURE_SUBSCRIPTION_ID --body "${AZURE_SUBSCRIPTION_ID:-}"
    gh variable set WEB_APP_NAME --body "${WEB_APP_NAME:-}"
    gh variable set AZURE_RESOURCE_GROUP --body "${AZURE_RESOURCE_GROUP:-}"
    gh variable set LOAD_TEST_RESOURCE_NAME --body "${LOAD_TEST_RESOURCE_NAME:-}"
    gh variable set PLAYWRIGHT_SERVICE_URL --body "${PLAYWRIGHT_SERVICE_URL:-}"
    gh variable set WEB_URL --body "${WEB_URL:-}"
    gh variable set DEPLOYMENT_MODE --body "${DEPLOYMENT_MODE:-}"
  else
    echo "SET_GH_VARS=true was set, but gh is unavailable or not authenticated; skipping repo variable updates." >&2
  fi
else
  echo "To set these with GitHub CLI automatically, rerun with SET_GH_VARS=true after 'gh auth login'."
fi

if [ "${SQL_ADMIN_IS_APP_IDENTITY:-true}" = "true" ]; then
  echo "App managed identity is the SQL Entra admin (SQL is private-endpoint only); no contained user needed."
  exit 0
fi

if ! command -v sqlcmd >/dev/null 2>&1; then
  echo "sqlcmd is required to create the Azure SQL contained user for the app managed identity." >&2
  echo "Install go-sqlcmd: brew install sqlcmd (macOS) or winget install sqlcmd (Windows), then rerun: azd hooks run postprovision" >&2
  exit 1
fi

if ! command -v az >/dev/null 2>&1; then
  echo "Azure CLI is required to temporarily add your client IP to the SQL firewall." >&2
  exit 1
fi

client_ip="$(curl -s https://api.ipify.org || true)"
if [ -z "$client_ip" ]; then
  echo "Could not determine client public IP for temporary SQL firewall rule." >&2
  exit 1
fi

rule_name="azd-postprovision-$(printf '%s' "$client_ip" | tr '.' '-')"
echo "Adding temporary SQL firewall rule $rule_name for $client_ip"
az sql server firewall-rule create \
  --resource-group "${AZURE_RESOURCE_GROUP}" \
  --server "${SQL_SERVER_NAME}" \
  --name "$rule_name" \
  --start-ip-address "$client_ip" \
  --end-ip-address "$client_ip" \
  --only-show-errors >/dev/null

cleanup() {
  echo "Removing temporary SQL firewall rule $rule_name"
  az sql server firewall-rule delete \
    --resource-group "${AZURE_RESOURCE_GROUP}" \
    --server "${SQL_SERVER_NAME}" \
    --name "$rule_name" \
    --only-show-errors >/dev/null 2>&1 || true
}
trap cleanup EXIT INT TERM

sql="IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'${APP_IDENTITY_NAME}')
BEGIN
    CREATE USER [${APP_IDENTITY_NAME}] FROM EXTERNAL PROVIDER;
END;
IF NOT EXISTS (SELECT 1 FROM sys.database_role_members rm JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id JOIN sys.database_principals m ON rm.member_principal_id = m.principal_id WHERE r.name = N'db_datareader' AND m.name = N'${APP_IDENTITY_NAME}')
BEGIN
    ALTER ROLE db_datareader ADD MEMBER [${APP_IDENTITY_NAME}];
END;
IF NOT EXISTS (SELECT 1 FROM sys.database_role_members rm JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id JOIN sys.database_principals m ON rm.member_principal_id = m.principal_id WHERE r.name = N'db_datawriter' AND m.name = N'${APP_IDENTITY_NAME}')
BEGIN
    ALTER ROLE db_datawriter ADD MEMBER [${APP_IDENTITY_NAME}];
END;
IF NOT EXISTS (SELECT 1 FROM sys.database_role_members rm JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id JOIN sys.database_principals m ON rm.member_principal_id = m.principal_id WHERE r.name = N'db_ddladmin' AND m.name = N'${APP_IDENTITY_NAME}')
BEGIN
    ALTER ROLE db_ddladmin ADD MEMBER [${APP_IDENTITY_NAME}];
END;"

echo "Creating/updating contained database user for ${APP_IDENTITY_NAME} on ${SQL_SERVER_FQDN}/${SQL_DATABASE_NAME}"
sqlcmd -S "${SQL_SERVER_FQDN}" -d "${SQL_DATABASE_NAME}" --authentication-method ActiveDirectoryDefault -Q "$sql"
