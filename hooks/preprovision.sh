#!/usr/bin/env sh
set -eu

if ! command -v azd >/dev/null 2>&1; then
  echo "azd is required for preprovision hooks. Install Azure Developer CLI and retry." >&2
  exit 1
fi

deployment_mode="$(azd env get-value DEPLOYMENT_MODE 2>/dev/null || true)"
if [ -z "$deployment_mode" ]; then
  azd env set DEPLOYMENT_MODE githubActions
  echo "Set DEPLOYMENT_MODE=githubActions"
fi

current_name="$(azd env get-value AZURE_PRINCIPAL_NAME 2>/dev/null || true)"
if [ -n "$current_name" ]; then
  echo "AZURE_PRINCIPAL_NAME is already set to $current_name"
  exit 0
fi

if ! command -v az >/dev/null 2>&1; then
  echo "Azure CLI is required to discover the signed-in user's UPN. Set AZURE_PRINCIPAL_NAME manually with: azd env set AZURE_PRINCIPAL_NAME <user-or-group-name>" >&2
  exit 1
fi

principal_name="$(az ad signed-in-user show --query userPrincipalName -o tsv 2>/dev/null || true)"
if [ -z "$principal_name" ]; then
  echo "Could not determine signed-in user. Run 'az login' or set AZURE_PRINCIPAL_NAME manually." >&2
  exit 1
fi

azd env set AZURE_PRINCIPAL_NAME "$principal_name"
echo "Set AZURE_PRINCIPAL_NAME=$principal_name"
