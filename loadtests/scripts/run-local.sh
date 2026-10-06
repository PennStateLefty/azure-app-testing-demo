#!/usr/bin/env bash
set -euo pipefail

if ! command -v jmeter >/dev/null 2>&1; then
  echo "Apache JMeter is not installed. Install it with: brew install jmeter" >&2
  exit 127
fi

SCRIPT_PATH="${1:-loadtests/jmeter/smoke.jmx}"
SCRIPT_NAME="$(basename "$SCRIPT_PATH")"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
DATA_DIR="$ROOT_DIR/loadtests/jmeter/data"
RESULT_DIR="$ROOT_DIR/loadtests/results/${SCRIPT_NAME%.jmx}-$(date +%Y%m%d%H%M%S)"
mkdir -p "$RESULT_DIR"

if [[ ! -f "$ROOT_DIR/loadtests/jmeter/$SCRIPT_NAME" ]]; then
  echo "JMX not found under loadtests/jmeter: $SCRIPT_NAME" >&2
  exit 2
fi

(
  cd "$DATA_DIR"
  jmeter -n \
    -t "../$SCRIPT_NAME" \
    -Jhost="${JMETER_HOST:-localhost}" \
    -Jport="${JMETER_PORT:-5119}" \
    -Jprotocol="${JMETER_PROTOCOL:-http}" \
    -Jthreads="${JMETER_THREADS:-5}" \
    -Jrampup="${JMETER_RAMPUP:-10}" \
    -Jduration="${JMETER_DURATION:-60}" \
    -JthinkScale="${JMETER_THINK_SCALE:-1}" \
    -l "$RESULT_DIR/results.jtl" \
    -e -o "$RESULT_DIR/report"
)

echo "JMeter results: $RESULT_DIR/results.jtl"
echo "HTML report:     $RESULT_DIR/report/index.html"
