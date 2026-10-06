$ErrorActionPreference = 'Stop'

if (-not (Get-Command azd -ErrorAction SilentlyContinue)) {
    throw 'azd is required for preprovision hooks. Install Azure Developer CLI and retry.'
}

$deploymentMode = (& azd env get-value DEPLOYMENT_MODE 2>$null)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($deploymentMode)) {
    & azd env set DEPLOYMENT_MODE githubActions
    Write-Host 'Set DEPLOYMENT_MODE=githubActions'
}

$currentName = (& azd env get-value AZURE_PRINCIPAL_NAME 2>$null)
if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($currentName)) {
    Write-Host "AZURE_PRINCIPAL_NAME is already set to $currentName"
    exit 0
}

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw 'Azure CLI is required to discover the signed-in user. Set AZURE_PRINCIPAL_NAME manually with: azd env set AZURE_PRINCIPAL_NAME <user-or-group-name>'
}

$principalName = (& az ad signed-in-user show --query userPrincipalName -o tsv 2>$null)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($principalName)) {
    throw "Could not determine signed-in user. Run 'az login' or set AZURE_PRINCIPAL_NAME manually."
}

& azd env set AZURE_PRINCIPAL_NAME $principalName
Write-Host "Set AZURE_PRINCIPAL_NAME=$principalName"
