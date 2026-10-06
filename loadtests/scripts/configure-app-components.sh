#!/usr/bin/env bash
set -euo pipefail

TEST_ID="${1:-}"
if [[ -z "$TEST_ID" ]]; then
  echo "Usage: $0 <test-id>" >&2
  exit 2
fi

: "${LOAD_TEST_RESOURCE_NAME:?Set LOAD_TEST_RESOURCE_NAME}"
: "${AZURE_RESOURCE_GROUP:?Set AZURE_RESOURCE_GROUP}"
: "${AZURE_SUBSCRIPTION_ID:?Set AZURE_SUBSCRIPTION_ID}"
: "${AZURE_TENANT_ID:?Set AZURE_TENANT_ID}"

LOAD_TEST_ENDPOINT="${LOAD_TEST_ENDPOINT:-$(az resource show \
  --subscription "$AZURE_SUBSCRIPTION_ID" \
  --resource-group "$AZURE_RESOURCE_GROUP" \
  --name "$LOAD_TEST_RESOURCE_NAME" \
  --resource-type Microsoft.LoadTestService/loadTests \
  --query properties.dataPlaneURI -o tsv)}"

python3 - "$TEST_ID" "$LOAD_TEST_ENDPOINT" <<'PY'
import json
import os
import subprocess
import sys
import urllib.request

api_version = "2024-12-01-preview"
test_id, endpoint = sys.argv[1], sys.argv[2].rstrip("/")

def required(name):
    value = os.environ.get(name, "")
    if not value:
        raise SystemExit(f"Set {name}")
    return value

resource_specs = [
    (required("WEB_APP_ID"), os.environ.get("WEB_APP_NAME", "app-service"), "Microsoft.Web/sites", "app", [
        ("Requests", "Total"),
        ("Http5xx", "Total"),
        ("HttpResponseTime", "Average"),
        ("CpuTime", "Total"),
        ("MemoryWorkingSet", "Average"),
    ]),
    (required("APP_SERVICE_PLAN_ID"), os.environ.get("APP_SERVICE_PLAN_NAME", "app-service-plan"), "Microsoft.Web/serverfarms", None, [
        ("CpuPercentage", "Average"),
        ("MemoryPercentage", "Average"),
    ]),
    (required("SQL_DATABASE_ID"), os.environ.get("SQL_DATABASE_NAME", "database"), "Microsoft.Sql/servers/databases", None, [
        ("cpu_percent", "Average"),
        ("physical_data_read_percent", "Average"),
        ("sql_instance_cpu_percent", "Average"),
        ("connection_successful", "Total"),
        ("deadlock", "Total"),
    ]),
    (required("APP_INSIGHTS_ID"), os.environ.get("APP_INSIGHTS_NAME", "application-insights"), "microsoft.insights/components", "web", [
        ("requests/duration", "Average"),
        ("requests/count", "Count"),
        ("requests/failed", "Count"),
        ("dependencies/duration", "Average"),
    ]),
]

token = subprocess.check_output([
    "az", "account", "get-access-token",
    "--tenant", required("AZURE_TENANT_ID"),
    "--resource", "https://cnt-prod.loadtesting.azure.com",
    "--query", "accessToken", "-o", "tsv",
], text=True).strip()

components = {}
metrics = {}
for resource_id, name, resource_type, kind, metric_specs in resource_specs:
    components[resource_id] = {
        "resourceName": name,
        "resourceType": resource_type,
        "displayName": name,
    }
    if kind:
        components[resource_id]["kind"] = kind
    for metric_name, aggregation in metric_specs:
        metric_id = f"{resource_id}/providers/microsoft.insights/metricdefinitions/{metric_name}"
        metrics[metric_id] = {
            "resourceId": resource_id,
            "metricNamespace": resource_type,
            "name": metric_name,
            "aggregation": aggregation,
            "resourceType": resource_type,
        }

def patch(path, body):
    data = json.dumps(body).encode("utf-8")
    req = urllib.request.Request(
        f"{endpoint}{path}?api-version={api_version}",
        data=data,
        method="PATCH",
        headers={
            "Authorization": f"Bearer {token}",
            "Content-Type": "application/merge-patch+json",
            "Accept": "application/json",
        },
    )
    with urllib.request.urlopen(req, timeout=60) as response:
        return json.loads(response.read().decode("utf-8") or "{}")

patch(f"/tests/{test_id}", {"metricsReferenceIdentityType": "SystemAssigned"})
patch(f"/tests/{test_id}/app-components", {"components": components})
patch(f"/tests/{test_id}/server-metrics-config", {"metrics": metrics})
print(f"Configured app components and server metrics for Azure Load Testing test '{test_id}'.")
PY
