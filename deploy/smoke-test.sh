#!/bin/sh
# Boots the images the way the Pi runs them, with Caddy in the tunnel's place (local.yaml), then checks the site, the
# tunnel's trust boundary, and a backup restored into fresh volumes. CI runs it on every build of the images; locally,
# run it after `docker buildx bake --load` at the repository root.
set -eu
cd "$(dirname "$0")"
# Compose prefers the calling shell's variables to --env-file, so an exported key or host would reach the test stacks.
unset KCC_HOST KCC_IMAGING_HMAC_KEY KCC_TUNNEL_TOKEN ANTHROPIC_API_KEY KCC_FIRST_BOOT_ENV \
    KCC_BACKUP_BUCKET KCC_BACKUP_ENDPOINT KCC_BACKUP_ACCESS_KEY_ID KCC_BACKUP_SECRET_ACCESS_KEY KCC_BACKUP_PING_URL

export KCC_IMAGE_TAG="${KCC_IMAGE_TAG:-local}"
work=$(mktemp -d)
cat >"$work/drill.env" <<EOF
KCC_HOST=localhost
KCC_IMAGING_HMAC_KEY=$(head -c 64 /dev/urandom | base64 | tr -d '\n')
KCC_TUNNEL_TOKEN=unused
EOF
cp "$work/drill.env" "$work/smoke.env"
echo "KCC_FIRST_BOOT_ENV=$work/first-boot.env" >>"$work/smoke.env"
# A path that never exists, so a developer's deploy/first-boot.env cannot switch the install on in the drill.
echo "KCC_FIRST_BOOT_ENV=$work/no-first-boot.env" >>"$work/drill.env"
cat >"$work/first-boot.env" <<EOF
Umbraco__CMS__Unattended__InstallUnattended=true
Umbraco__CMS__Unattended__UnattendedUserName=Smoke Test
Umbraco__CMS__Unattended__UnattendedUserEmail=smoke@example.test
Umbraco__CMS__Unattended__UnattendedUserPassword=Smoke-Test-Passw0rd
EOF

smoke() { docker compose -p kcc-smoke --env-file "$work/smoke.env" -f compose.yaml -f local.yaml "$@"; }
drill() { docker compose -p kcc-smoke-drill --env-file "$work/drill.env" -f compose.yaml -f local.yaml "$@"; }
image() { printf '%s/kcc-%s:%s' "${KCC_REGISTRY:-ghcr.io/th3fenriswolf}" "$1" "$KCC_IMAGE_TAG"; }
pass() { echo "ok - $*"; }
fail() {
    echo "not ok - $*" >&2
    exit 1
}

cleanup() {
    status=$?
    if [ "$status" -ne 0 ]; then
        smoke logs --tail 60 app ssr 2>/dev/null || true
        drill logs --tail 60 app 2>/dev/null || true
    fi
    smoke --profile jobs down -v --remove-orphans >/dev/null 2>&1 || true
    drill --profile jobs down -v --remove-orphans >/dev/null 2>&1 || true
    rm -rf "$work"
    exit "$status"
}
# Under dash a signal ends the script without running its EXIT trap, so each signal becomes an ordinary exit.
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

smoke up -d --wait --wait-timeout 600
pass "app, ssr and the local edge are healthy"

home=$(curl -fsSk https://localhost:8443/)
case "$home" in
    *'<div id="app"><'[!/]*) pass "home is server-rendered" ;;
    *) fail "home fell back to client-side rendering" ;;
esac
assets=$(printf '%s' "$home" | grep -oE '(href|src)="/assets/[^"]+"' | cut -d'"' -f2 | sort -u)
[ -n "$assets" ] || fail "home links no /assets files: the Vite manifest is missing from the image"
for asset in $assets; do
    curl -fsSk -o /dev/null "https://localhost:8443$asset" || fail "$asset does not load"
done
pass "every asset home links loads"
for page in /robots.txt /sitemap.xml /umbraco; do
    curl -fsSk -o /dev/null "https://localhost:8443$page" || fail "$page does not load"
done
pass "robots.txt, the sitemap and the backoffice load"
for endpoint in /api/dev/seed-recipes /api/dev/baseline/export; do
    [ "$(curl -sk -o /dev/null -w '%{http_code}' -X POST "https://localhost:8443$endpoint")" = 404 ] ||
        fail "$endpoint answers in production"
done
pass "the development endpoints answer 404"

authorize=/umbraco/management/api/v1/security/back-office/authorize
curl -sk "https://localhost:8443$authorize" | grep -q ID2029 ||
    fail "the backoffice refused the HTTPS the edge forwarded"
pass "the backoffice takes the HTTPS the edge forwards"
docker run --rm --network kcc-smoke_edge --ip 172.30.9.20 --entrypoint curl "$(image app)" -s \
    -H 'Host: localhost' -H 'X-Forwarded-Proto: https' "http://app:8080$authorize" | grep -q ID2083 ||
    fail "the app trusted forwarded headers from outside the tunnel"
pass "forwarded headers from any other address are ignored"

if smoke exec -T ssr node -e "fetch('https://example.com').then(() => process.exit(0), () => process.exit(1))"; then
    fail "ssr reached the internet"
fi
pass "ssr has no route out"

smoke run --rm --entrypoint sh restore -c 'echo smoke >/srv/kcc/media/smoke-marker.txt'
smoke run --rm -e RCLONE_CONFIG_BUCKET_TYPE=local -e KCC_BACKUP_BUCKET=/srv/kcc/snapshots/bucket backup nightly
smoke run --rm backup snapshot
pass "the nightly backup and a pre-deploy snapshot ran"

# The drill uses the same fixed subnet for its edge network, so the first stack stops before it starts.
smoke down
drill run --rm --entrypoint true restore
docker run --rm -v kcc-smoke_snapshots:/from:ro -v kcc-smoke-drill_snapshots:/srv/kcc/snapshots \
    --entrypoint cp "$(image backup)" -R /from/bucket /srv/kcc/snapshots/bucket
drill run --rm -e RCLONE_CONFIG_BUCKET_TYPE=local -e KCC_BACKUP_BUCKET=/srv/kcc/snapshots/bucket restore latest
drill up -d --wait --wait-timeout 300
pass "the restored database boots with the install off"
[ "$(curl -fsSk https://localhost:8443/media/smoke-marker.txt)" = smoke ] || fail "the restored media is missing"
pass "the restored media is served"
