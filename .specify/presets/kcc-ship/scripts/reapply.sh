#!/usr/bin/env bash
set -euo pipefail

root="$(git rev-parse --show-toplevel)"
copy="$(mktemp -d)"
trap 'rm -rf "$copy"' EXIT

cp -R "$root/.specify/presets/kcc-ship" "$copy/"
rm -rf "$copy/kcc-ship/.composed"
cd "$root"
if jq -e '.presets["kcc-ship"]' .specify/presets/.registry >/dev/null; then specify preset remove kcc-ship; fi
specify preset add --dev "$copy/kcc-ship"
