#!/usr/bin/env bash
set -euo pipefail

# Optional App Service Build Service custom deployment script for repositories where
# Kudu/Oryx does not yet recognize net10.0. This file is intentionally not wired by
# default. To use it, change the root .deployment to include:
#   [config]
#   command = deploy/kudu-deploy.sh

PROJECT_PATH="${PROJECT:-src/LifeCore.Web/LifeCore.Web.csproj}"
PUBLISH_DIR="${DEPLOYMENT_TARGET:-$PWD/publish}"

echo "Publishing $PROJECT_PATH to $PUBLISH_DIR"
dotnet restore "$PROJECT_PATH"
dotnet publish "$PROJECT_PATH" -c Release -o "$PUBLISH_DIR" --no-restore
