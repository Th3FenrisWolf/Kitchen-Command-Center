#!/usr/bin/env bash
set -euo pipefail

command="$(jq -r '.tool_input.command // empty')"
create_pattern='(^|[^[:alnum:]_.-])gh[[:space:]]+pr[[:space:]]+create([[:space:]]|$)'
[[ "$command" =~ $create_pattern ]] || exit 0
[[ "$command" == *KCC_ALLOW_UNRECOMPOSED=1* || "$command" == *--help* ]] && exit 0
git rev-parse --git-dir >/dev/null 2>&1 || exit 0

branch="$(git branch --show-current)"
head_pattern='(--head|-H)[=[:space:]]+([^[:space:]]+)'
[[ "$command" =~ $head_pattern ]] && branch="${BASH_REMATCH[2]}"
backup="backup/${branch#*/}"

if ! git rev-parse --verify --quiet "refs/heads/$backup" >/dev/null; then
  reason="$branch was never recomposed: $backup does not exist."
elif ! git diff --quiet "$backup" "$branch" --; then
  reason="$branch changed after its recomposition: its tree no longer matches $backup."
elif [[ "$(git rev-parse "$backup")" == "$(git rev-parse "$branch")" ]]; then
  reason="$branch still has the history that $backup saved."
else
  exit 0
fi

jq -n --arg reason "$reason Recompose the branch once with the recompose-branch skill before its pull request opens. Prefix the command with KCC_ALLOW_UNRECOMPOSED=1 only when the owner says so." \
  '{hookSpecificOutput: {hookEventName: "PreToolUse", permissionDecision: "deny", permissionDecisionReason: $reason}}'
