#!/usr/bin/env bash
set -euo pipefail

TEST_ID="${1:-}"
if [[ -z "$TEST_ID" ]]; then
  echo "Usage: $0 <test-id>" >&2
  exit 2
fi

: "${LOAD_TEST_RESOURCE_NAME:?Set LOAD_TEST_RESOURCE_NAME}"
: "${AZURE_RESOURCE_GROUP:?Set AZURE_RESOURCE_GROUP}"

add_component() {
  local resource_id="$1"
  [[ -z "$resource_id" ]] && return 0
  az load test app-component add \
    --load-test-resource "$LOAD_TEST_RESOURCE_NAME" \
    --resource-group "$AZURE_RESOURCE_GROUP" \
    --test-id "$TEST_ID" \
    --resource-id "$resource_id" >/dev/null
}

add_metric() {
  local resource_id="$1" namespace="$2" metric="$3" aggregation="$4"
  [[ -z "$resource_id" ]] && return 0
  az load test server-metric add \
    --load-test-resource "$LOAD_TEST_RESOURCE_NAME" \
    --resource-group "$AZURE_RESOURCE_GROUP" \
    --test-id "$TEST_ID" \
    --resource-id "$resource_id" \
    --metric-namespace "$namespace" \
    --metric-name "$metric" \
    --aggregation "$aggregation" >/dev/null
}

add_component "${WEB_APP_ID:-}"
add_component "${SQL_DATABASE_ID:-}"
add_component "${APP_SERVICE_PLAN_ID:-}"
add_component "${APP_INSIGHTS_ID:-}"

add_metric "${WEB_APP_ID:-}" "Microsoft.Web/sites" "AverageResponseTime" "Average"
add_metric "${WEB_APP_ID:-}" "Microsoft.Web/sites" "Http5xx" "Total"
add_metric "${APP_SERVICE_PLAN_ID:-}" "Microsoft.Web/serverfarms" "CpuPercentage" "Average"
add_metric "${APP_SERVICE_PLAN_ID:-}" "Microsoft.Web/serverfarms" "MemoryPercentage" "Average"
add_metric "${SQL_DATABASE_ID:-}" "Microsoft.Sql/servers/databases" "cpu_percent" "Average"
add_metric "${SQL_DATABASE_ID:-}" "Microsoft.Sql/servers/databases" "dtu_consumption_percent" "Average"
add_metric "${APP_INSIGHTS_ID:-}" "microsoft.insights/components" "requests/duration" "Average"
add_metric "${APP_INSIGHTS_ID:-}" "microsoft.insights/components" "requests/count" "Total"

echo "Configured app components and server metrics for Azure Load Testing test '$TEST_ID'."
