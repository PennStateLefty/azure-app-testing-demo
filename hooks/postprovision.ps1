$ErrorActionPreference = 'Stop'

$repoVars = [ordered]@{
    AZURE_CLIENT_ID = $env:GITHUB_IDENTITY_CLIENT_ID
    AZURE_TENANT_ID = $env:AZURE_TENANT_ID
    AZURE_SUBSCRIPTION_ID = $env:AZURE_SUBSCRIPTION_ID
    WEB_APP_NAME = $env:WEB_APP_NAME
    AZURE_RESOURCE_GROUP = $env:AZURE_RESOURCE_GROUP
    LOAD_TEST_RESOURCE_NAME = $env:LOAD_TEST_RESOURCE_NAME
    PLAYWRIGHT_SERVICE_URL = $env:PLAYWRIGHT_SERVICE_URL
    WEB_URL = $env:WEB_URL
    STAGING_WEB_URL = $env:STAGING_WEB_URL
    DEPLOYMENT_MODE = $env:DEPLOYMENT_MODE
}

Write-Host "`nGitHub repository variables to set:"
foreach ($item in $repoVars.GetEnumerator()) {
    Write-Host "  $($item.Key)=$($item.Value)"
}

if ($env:SET_GH_VARS -eq 'true') {
    $ghAvailable = Get-Command gh -ErrorAction SilentlyContinue
    if ($ghAvailable) {
        gh auth status 2>$null
    }
    if ($ghAvailable -and $LASTEXITCODE -eq 0) {
        foreach ($item in $repoVars.GetEnumerator()) {
            gh variable set $item.Key --body "$($item.Value)"
        }
    }
    else {
        Write-Warning "SET_GH_VARS=true was set, but gh is unavailable or not authenticated; skipping repo variable updates."
    }
}
else {
    Write-Host "To set these with GitHub CLI automatically, rerun with SET_GH_VARS=true after 'gh auth login'."
}

if (($env:SQL_ADMIN_IS_APP_IDENTITY ?? 'true') -eq 'true') {
    Write-Host "App managed identity is the SQL Entra admin (SQL is private-endpoint only); no contained user needed."
    exit 0
}

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw "sqlcmd is required to create the Azure SQL contained user. Install go-sqlcmd: brew install sqlcmd (macOS) or winget install sqlcmd (Windows), then rerun: azd hooks run postprovision"
}
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw 'Azure CLI is required to temporarily add your client IP to the SQL firewall.'
}

$clientIp = (Invoke-RestMethod -Uri 'https://api.ipify.org' -UseBasicParsing)
if ([string]::IsNullOrWhiteSpace($clientIp)) {
    throw 'Could not determine client public IP for temporary SQL firewall rule.'
}

$ruleName = "azd-postprovision-$($clientIp -replace '\.', '-')"
Write-Host "Adding temporary SQL firewall rule $ruleName for $clientIp"
az sql server firewall-rule create `
    --resource-group $env:AZURE_RESOURCE_GROUP `
    --server $env:SQL_SERVER_NAME `
    --name $ruleName `
    --start-ip-address $clientIp `
    --end-ip-address $clientIp `
    --only-show-errors | Out-Null

try {
    $sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$env:APP_IDENTITY_NAME')
BEGIN
    CREATE USER [$env:APP_IDENTITY_NAME] FROM EXTERNAL PROVIDER;
END;
IF NOT EXISTS (SELECT 1 FROM sys.database_role_members rm JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id JOIN sys.database_principals m ON rm.member_principal_id = m.principal_id WHERE r.name = N'db_datareader' AND m.name = N'$env:APP_IDENTITY_NAME')
BEGIN
    ALTER ROLE db_datareader ADD MEMBER [$env:APP_IDENTITY_NAME];
END;
IF NOT EXISTS (SELECT 1 FROM sys.database_role_members rm JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id JOIN sys.database_principals m ON rm.member_principal_id = m.principal_id WHERE r.name = N'db_datawriter' AND m.name = N'$env:APP_IDENTITY_NAME')
BEGIN
    ALTER ROLE db_datawriter ADD MEMBER [$env:APP_IDENTITY_NAME];
END;
IF NOT EXISTS (SELECT 1 FROM sys.database_role_members rm JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id JOIN sys.database_principals m ON rm.member_principal_id = m.principal_id WHERE r.name = N'db_ddladmin' AND m.name = N'$env:APP_IDENTITY_NAME')
BEGIN
    ALTER ROLE db_ddladmin ADD MEMBER [$env:APP_IDENTITY_NAME];
END;
"@
    Write-Host "Creating/updating contained database user for $env:APP_IDENTITY_NAME on $env:SQL_SERVER_FQDN/$env:SQL_DATABASE_NAME"
    sqlcmd -S $env:SQL_SERVER_FQDN -d $env:SQL_DATABASE_NAME --authentication-method ActiveDirectoryDefault -Q $sql
}
finally {
    Write-Host "Removing temporary SQL firewall rule $ruleName"
    az sql server firewall-rule delete `
        --resource-group $env:AZURE_RESOURCE_GROUP `
        --server $env:SQL_SERVER_NAME `
        --name $ruleName `
        --only-show-errors | Out-Null
}
