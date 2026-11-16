#!/usr/bin/env bash
set -euo pipefail

trunk="origin/${1:-main}"
branch="$(git branch --show-current)"

closest=""
fewest=""
while read -r ref; do
  case "$ref" in
    "$branch" | "origin/$branch" | origin | origin/HEAD | backup/*) continue ;;
  esac
  git merge-base --is-ancestor "$ref" HEAD || continue
  git merge-base --is-ancestor "$ref" "$trunk" && continue
  count="$(git rev-list --count "$ref..HEAD")"
  if [[ -z "$fewest" || "$count" -lt "$fewest" ]]; then
    closest="$ref"
    fewest="$count"
  fi
done < <(git for-each-ref --format='%(refname:short)' refs/heads refs/remotes/origin)

if [[ -n "$closest" ]]; then
  base="$(git rev-parse "$closest")"
  echo "base: $closest $base"
else
  base="$(git merge-base HEAD "$trunk")"
  echo "base: $trunk $base"
fi
echo "collapse:"
git log --format='  %h %s' "$base..HEAD"
