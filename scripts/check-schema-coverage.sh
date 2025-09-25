#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &> /dev/null && pwd)"
ROOT_DIR="$(cd -- "${SCRIPT_DIR}/.." && pwd)"

dotnet run --project "${ROOT_DIR}/tools/SchemaCoverage/SchemaCoverage.csproj" -- "$@"
