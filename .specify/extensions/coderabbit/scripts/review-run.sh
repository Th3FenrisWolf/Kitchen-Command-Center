#!/bin/bash
# Runs one CodeRabbit round under a machine-wide lock.
#
# Bash twin of review-run.ps1. Keep the two in step.
#
#   review-run.sh <repo> review --agent --base-commit <base> -c <files>...
#
# The tool keeps one cache under ~/.coderabbit/reviews for every review on the
# machine, and a stale cache entry fails a later round with "is outside
# repository". Clearing it while another agent reviews stops that review. So
# each round takes the lock, clears the cache, runs in <repo>, and releases the
# lock. One review at a time also keeps parallel agents under the rate limit.
#
# Output goes straight through, so redirect it to a file. The exit code is the
# tool's, or 5 when the lock stays busy for 30 minutes.

repo=$1; shift
lock="$HOME/.coderabbit/bizstream.lock"
mkdir -p "$HOME/.coderabbit"
waited=0
until mkdir "$lock" 2>/dev/null; do
  holder=$(cat "$lock/pid" 2>/dev/null)
  age=$(( $(date +%s) - $(stat -f %m "$lock" 2>/dev/null || stat -c %Y "$lock" 2>/dev/null || date +%s) ))
  # A holder that died, or a lock older than 30 minutes, is stale.
  if { [ -n "$holder" ] && ! kill -0 "$holder" 2>/dev/null; } || [ "$age" -gt 1800 ]; then
    rm -rf "$lock"; continue
  fi
  [ "$waited" -ge 1800 ] && { echo "the CodeRabbit lock stayed busy for 30 minutes: $lock" >&2; exit 5; }
  [ "$waited" -eq 0 ] && echo "waiting for another CodeRabbit review to finish" >&2
  sleep 10; waited=$((waited + 10))
done
echo $$ > "$lock/pid"
trap 'rm -rf "$lock"' EXIT

rm -rf "$HOME/.coderabbit/reviews"
cd "$repo" || exit 4
coderabbit "$@"
