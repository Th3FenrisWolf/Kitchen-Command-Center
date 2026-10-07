#!/usr/bin/env bash
set -euo pipefail

root="$(git rev-parse --show-toplevel)"
copy="$(mktemp -d)"
trap 'rm -rf "$copy"' EXIT

cp -R "$root/.specify/presets/kcc-recompose" "$copy/"
rm -rf "$copy/kcc-recompose/.composed"
cd "$root"
specify preset remove kcc-recompose
specify preset add --dev "$copy/kcc-recompose"
