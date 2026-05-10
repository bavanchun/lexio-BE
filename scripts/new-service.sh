#!/usr/bin/env bash
# Usage: bash scripts/new-service.sh <ServiceName>
# Example: bash scripts/new-service.sh Identity
set -euo pipefail

NAME="${1:?Usage: new-service.sh <ServiceName>}"
OUTPUT="src/services/$NAME"

echo "Creating service: Lexio.$NAME"
dotnet new lexio-service -n "$NAME" -o "$OUTPUT"

echo "Adding projects to solution..."
# Use find for portability (no ** glob needed)
find "$OUTPUT" -name "*.csproj" | while read -r proj; do
  dotnet sln Lexio.slnx add "$proj"
done

echo "Done. Run: dotnet build $OUTPUT"
