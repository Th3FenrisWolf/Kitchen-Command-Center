#!/usr/bin/env bash
set -euo pipefail

folder="${1:?usage: preview-setup.sh <round folder>}"
root="$(git rev-parse --show-toplevel)"
templates="$root/.specify/extensions/preview/templates"
mockups="$root/.mockups"

newest_css() { ls -t "$1"/mainCss-*.css 2>/dev/null | head -1 || true; }

assets="$root/src/KCC.Web/wwwroot/assets"
if [[ -z "$(newest_css "$assets")" ]]; then
  main_checkout="$(git worktree list --porcelain | awk 'NR == 1 { print $2 }')"
  assets="$main_checkout/src/KCC.Web/wwwroot/assets"
fi
css="$(newest_css "$assets")"
web="${assets%/wwwroot/assets}"
if [[ -z "$css" ]]; then
  echo "No built kit in $web. Run yarn build:all at the root of that checkout." >&2
  exit 1
fi
checkout="${web%/src/KCC.Web}"
built="$(date -r "$css" +%s)"
committed="$(git -C "$checkout" log -1 --format=%ct -- src/KCC.Web/Features/Styles)"
edited="$(git -C "$checkout" status --porcelain -- src/KCC.Web/Features/Styles)"
if (( ${committed:-0} > built )) ||
  [[ -n "$edited" && -n "$(find "$web/Features/Styles" -type f -newer "$css" -print -quit)" ]]; then
  echo "The kit in $checkout is older than its styles. Run yarn build:all at the root of that checkout." >&2
  exit 1
fi

mkdir -p "$mockups/$folder"
ln -sfn "$assets" "$mockups/assets"
ln -sfn "assets/$(basename "$css")" "$mockups/kit.css"

{
  sed '/<!-- wax-defs -->/,$d' "$templates/round.html"
  sed -n '/<svg id="kcc-wax-defs"/,/<\/svg>/p' "$web/Features/Pages/Shared/Layout.cshtml"
  sed '1,/<!-- wax-defs -->/d' "$templates/round.html"
} > "$mockups/$folder/_round.html"
cp "$templates/architecture.html" "$mockups/$folder/_architecture.html"

echo "$mockups/$folder"
