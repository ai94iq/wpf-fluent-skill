#!/usr/bin/env bash
# Keyboard menu for the skill: scaffold, icons, project tools, guides and checks.
# Example: scripts/tui.sh
set -euo pipefail

repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "[tui] .NET 10 SDK not found. Install it from https://dotnet.microsoft.com/download/dotnet/10.0" >&2
    exit 1
fi

sdk="$(dotnet --version)"
if [[ "$sdk" != 10.* ]]; then
    echo "[tui] .NET 10 SDK required, found $sdk." >&2
    exit 1
fi

if [ "$#" -gt 0 ]; then
    exec dotnet run "$repo/scripts/tui.cs" -- "$repo" "$@"
fi
exec dotnet run "$repo/scripts/tui.cs" -- "$repo"
