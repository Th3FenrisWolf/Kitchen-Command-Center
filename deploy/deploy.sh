#!/bin/sh
# Run every five minutes by kcc-deploy.timer. Takes in main's deploy files and images, snapshots the database, and
# restarts only what changed.
set -eu
cd "$(dirname "$0")"

changed=false
pulled=

# The Pi's sparse clone moves with every commit to main; only a change to this folder calls for a deploy.
# A ref records the deployed commit, so a run that fails after its pull leaves the change for the next run.
if [ -d ../.git ]; then
    git pull --ff-only --quiet
    pulled=$(git rev-parse HEAD)
    git diff --quiet refs/kcc/deployed "$pulled" -- . 2>/dev/null || changed=true
fi

# Only the GHCR images: cloudflared's tag is pinned in compose.yaml, and Docker Hub rate-limits anonymous pulls.
docker compose --progress quiet pull app ssr backup

containers=$(docker compose ps -q)
[ -n "$containers" ] || changed=true
for container in $containers; do
    tag=$(docker inspect -f '{{.Config.Image}}' "$container")
    [ "$(docker inspect -f '{{.Image}}' "$container")" = "$(docker image inspect -f '{{.Id}}' "$tag")" ] || changed=true
done

states=$(docker compose config --services |
    xargs docker compose ps -a --format '{{.Service}} {{.State}} {{.Health}}')
unhealthy=$(printf '%s\n' "$states" | awk '
    $2 != "running" || $3 == "unhealthy" { s = s sep $1 " " ($2 == "running" ? $3 : $2); sep = ", " }
    END { print s }')

if [ "$changed" != true ]; then
    # Compose recreates containers before it waits, so after a failed wait nothing looks changed: fail until healthy.
    [ -z "$unhealthy" ] || { echo "not healthy: $unhealthy" >&2; exit 1; }
    exit 0
fi

# Snapshot only while the app is running and not unhealthy: a retry would rotate out the first attempt's snapshot.
if [ -n "$(docker compose ps -q app)" ] && ! printf '%s\n' "$unhealthy" | grep -Eq '(^|, )app '; then
    docker compose run --rm backup snapshot
fi

docker compose up -d --remove-orphans --wait --wait-timeout 600
[ -z "$pulled" ] || git update-ref refs/kcc/deployed "$pulled"
docker image prune -f >/dev/null
echo "deployed $(docker inspect -f '{{.Config.Image}} {{.Image}}' "$(docker compose ps -q app)")"
