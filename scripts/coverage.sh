#!/usr/bin/env bash
# coverage.sh — Run all tests with Coverlet, generate HTML + text summary via ReportGenerator.
# Usage: bash scripts/coverage.sh [--open]   (--open opens HTML report in browser after generation)
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$REPO_ROOT"

RESULTS_DIR="$REPO_ROOT/TestResults"
REPORT_DIR="$REPO_ROOT/coverage-report"

echo "==> Cleaning previous results..."
rm -rf "$RESULTS_DIR" "$REPORT_DIR"

echo "==> Running tests with coverage collection..."
dotnet test \
  --collect:"XPlat Code Coverage" \
  --settings .runsettings \
  --results-directory "$RESULTS_DIR" \
  --logger "console;verbosity=minimal"

echo "==> Restoring dotnet tools..."
dotnet tool restore

echo "==> Generating HTML + text summary..."
dotnet reportgenerator \
  -reports:"$RESULTS_DIR/**/coverage.cobertura.xml" \
  -targetdir:"$REPORT_DIR" \
  -reporttypes:"Html;TextSummary"

echo ""
echo "==> Coverage summary:"
cat "$REPORT_DIR/Summary.txt"

if [[ "${1:-}" == "--open" ]]; then
  echo ""
  echo "==> Opening HTML report..."
  open "$REPORT_DIR/index.html" 2>/dev/null || xdg-open "$REPORT_DIR/index.html" 2>/dev/null || true
fi
