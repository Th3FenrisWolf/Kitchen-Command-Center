#!/usr/bin/env bash
set -euo pipefail

usage="usage: preview-shot.sh <round URL> <output path without extension>"
url="${1:?$usage}"
out="${2:?$usage}"

driver="$(ls -d "$HOME"/.nuget/packages/microsoft.playwright/*/.playwright 2>/dev/null | sort -V | tail -1 || true)"
if [[ -z "$driver" ]]; then
  echo "No Playwright driver in the NuGet cache. Run dotnet restore at the repository root." >&2
  exit 1
fi
platform="$(uname -s | tr '[:upper:]' '[:lower:]')-$(uname -m | sed 's/x86_64/x64/')"
node="$driver/node/$platform/node"

separator='?'
[[ "$url" == *\?* ]] && separator='&'
mkdir -p "$(dirname "$out")"
for ramp in light dark; do
  "$node" "$driver/package/cli.js" screenshot --full-page --viewport-size=1280,900 --wait-for-timeout=800 \
    "$url${separator}ramp=$ramp" "$out-$ramp.jpg"
done
