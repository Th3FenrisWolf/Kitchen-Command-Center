# Replatform Phase 7 — Hosting Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Status:** in progress: Tasks 1 to 8, Step 4 done (2026-09-30) on branch `hosting`, based on `replatform-phase-6` at
`ec5c44a`. **Resume point:** Task 8, Step 5. The push, the pull request and the merge wait on the owner, and on Phase
6's merge into `main`; then Tasks 9 to 12. Task 9's DNS move can start at any time. "Findings from Phase 7", at the
end, lists where the code departs from this plan.

**Goal:** The site runs on the owner's Raspberry Pi as containers behind a Cloudflare Tunnel, private behind
Cloudflare Access until launch. The Pi deploys by pulling the images CI builds and pushes to private GHCR packages,
and backs itself up nightly to Cloudflare R2. The gate: the site reachable behind Access, and a restore rehearsed from
the bucket.

**Architecture:**
- **Images.** One `Dockerfile` at the repository root builds the `app` and `ssr` images from one frontend stage, so
  one `yarn build:all` makes both bundles. A small Alpine image in `deploy/kcc-backup/` holds SQLite, rclone and the
  `kcc-backup` script. `docker-bake.hcl` builds all three for `linux/arm64`.
- **The app in a container.** Forwarded headers are trusted from the tunnel's fixed address alone, because Umbraco's
  backoffice sign-in refuses plain HTTP. Responses over HTTPS carry HSTS. `appsettings.Production.json` turns on
  Umbraco's Production runtime mode, keeps the logs on the data volume, and never lets the site install itself. The
  Vite manifest is published with the app.
- **The stack.** `deploy/compose.yaml` runs `app`, `ssr` and `cloudflared` with read-only root file systems:
  - The `edge` network joins cloudflared, at a fixed address, to the app. The `render` network joins the app to ssr
    and has no route out.
  - Named volumes hold the data and the media. No host port is published.
  - `backup` and `restore` are one-off jobs in a `jobs` profile.
  - `deploy/local.yaml` puts Caddy in cloudflared's place on `https://localhost:8443`, for the smoke test and the
    restore drill.
- **On the Pi,** two systemd timers:
  - `kcc-deploy`, every five minutes, pulls `deploy/` from git and the images from GHCR. When anything changed, it
    snapshots the database and restarts what changed.
  - `kcc-backup`, nightly, streams a `.backup` copy of the database, the data-protection keys and the media to R2,
    then pings healthchecks.io.
- **CI.** A new `images` job builds the three images on an arm64 runner and runs `deploy/smoke-test.sh` against them:
  the site, the tunnel's trust boundary, and a backup restored into fresh volumes. On `main` it pushes them.
- **The runbook,** `docs/hosting/runbook.md`, takes the owner through Cloudflare (DNS, the tunnel, two Access
  applications), R2, healthchecks.io, the Pi, the first boot, deploys, rollbacks, restores and the drill.

**Tech Stack:** Docker Engine with Compose v2 and Buildx bake; `mcr.microsoft.com/dotnet/sdk` and `aspnet` 10.0
(Ubuntu 24.04), `node` 24 (Debian 12), `alpine` 3.24 with `sqlite` and `rclone`, `cloudflare/cloudflared`, Caddy
(local runs only); GitHub Actions on `ubuntu-24.04-arm` with `docker/bake-action` and
`actions/delete-package-versions`; GHCR; Cloudflare DNS, Tunnel, Access and R2; healthchecks.io; systemd;
Raspberry Pi OS Lite 64-bit (Debian 13); Tailscale. Tests: TUnit 1.27 with `Microsoft.AspNetCore.Mvc.Testing`,
Vitest, and the shell smoke test.

**Spec:** `docs/replatform/specs/2026-09-21-replatform-off-xperience.md`. Sections: §6.5 (configuration and secrets),
§8 (forwarded headers, the rate-limit key, Access on the backoffice), §12 (production data down to development), §13
(hosting, deploy and operations, all of it), §14 (CI builds and pushes the arm64 images), §15 (the Phase 7 row, and
hosting landing on `main` as normal pull requests) and §18 (the risks it mitigates: file permissions, the rate-limit
key, Font Awesome, upload bandwidth, backups). Task 8 corrects §13 where this plan builds something different, each
change with its reason.

## Before you start: reconcile with Phases 1–6 as built

This plan was written on 2026-09-26 and 2026-09-27, while Phase 3 was running (branch `replatform-phase-3` at
`6a1cbcb`). Its code ran against that commit; see "What the probe already proved". Phases 3 to 6 change several files
this plan edits (`Program.cs`, `KCC.Web.csproj`, `package.json`, `UmbracoSite.cs`, the CI workflow), and add the two
backoffice clients the images must carry. Check each point below and adapt the step that depends on it, noting the
change in your task report.

```bash
git checkout main && git pull --ff-only
grep -H "Status:" docs/replatform/plans/*-phase-6-*.md
git log --oneline -5
grep -n "BootUmbracoAsync\|IsDevelopment\|UseExceptionHandler\|UseForwardedHeaders\|UseHsts" src/KCC.Web/Program.cs
grep -n "Unported slices\|ProjectReference\|wwwroot" src/KCC.Web/KCC.Web.csproj
node -e "console.log(Object.keys(require('./src/KCC.Web/package.json').dependencies).join(' '))"
node -e "const p = require('./package.json'); console.log(p.workspaces.join(' ')); console.log(p.scripts['build:all'])"
grep -n "^import\|SSR_BUNDLE_PATH =\|SSR_PORT" src/KCC.Web/Features/Ssr/Server.js
grep -n "App_Plugins\|^\.env$" .gitignore
grep -n '"BaseUrl"\|RobotsTxtDenyAll\|umbracoDbDSN"\|"ModelsMode"\|"InstallUnattended"' src/KCC.Web/appsettings.json
ls src/KCC.Web/appsettings.*.json
grep -n 'Include="Umbraco.Cms"' Directory.Packages.props
grep -rn "CF-Connecting-IP" src/KCC.Web/Features/Security/
grep -n "DataProtection:KeysDirectory\|IStartupFilter, \|Hosting:" tests/KCC.IntegrationTests/Config/UmbracoSite.cs
grep -n "public static JsonElement Load" tests/KCC.UnitTests/Features/Configuration/WebAppSettings.cs
grep -n "^  [a-z-]*:$\|runs-on:\|branches:" .github/workflows/build-and-test.yml
ls .github/dependabot.yml deploy docs/hosting Dockerfile 2>&1 | grep -v "No such"
docker version --format '{{.Server.Version}}' && docker compose version --short && docker buildx version
echo "${FONTAWESOME_NPM_AUTH_TOKEN:+token set}"
lsof -nP -iTCP:8443 -iTCP:5001 -sTCP:LISTEN
```

Expected, and what to do if not:

1. Phase 6's Status is `done`, and `main`'s log shows the merge of `replatform`. If not, stop: this phase builds on
   `main` after the merge.
2. `Program.cs` calls `await app.BootUmbracoAsync();`, then has one `if (!app.Environment.IsDevelopment())` block with
   `app.UseExceptionHandler("/error");`. No forwarded headers or HSTS yet. Task 1 adds a line right after the boot and
   one inside that block, wherever the block sits.
3. `KCC.Web.csproj` has no `Unported slices` group, and references `KCC.Contributions` and `KCC.Admin` (Phases 4 and
   5). Both are Razor class libraries whose `wwwroot/App_Plugins` publishes with the app. If `KCC.Admin` is not
   referenced, stop and ask the owner: the backoffice editors would be missing from the image.
4. KCC.Web's `dependencies` are `@fortawesome/fontawesome-pro compression express qs tailwindcss vue`. Task 3 moves
   Font Awesome and Tailwind. Any other package added since stays only if `Server.js`, `CollectCss.js` or the SSR
   bundle imports it at run time (Task 3, Step 1 says how to tell). Otherwise move it with them.
5. The workspaces are `packages/* src/KCC.Admin/Client src/KCC.Contributions/Client src/KCC.Web`, and `build:all`
   builds the admin client, the contributions client, then the web bundles. The Dockerfile copies each workspace's
   `package.json` before `yarn install`. If the list differs, make Task 3's `COPY` lines match it.
6. `Server.js` imports `express`, `compression`, `http`, `crypto`, `vue/server-renderer`, `path`, `url` and
   `./CollectCss.js`, loads `../../wwwroot/ssr/Server.Entry.js`, and reads its port from `SSR_PORT`.
7. `.gitignore` ignores `src/KCC.Admin/wwwroot/App_Plugins/` and `src/KCC.Contributions/wwwroot/App_Plugins/`
   (Phase 5), and has a bare `.env` line.
8. `appsettings.json`: `VueSsr:BaseUrl` is `http://localhost:3001`, `RobotsTxtDenyAll` is true, the connection string
   has `Cache=Private`, `ModelsMode` is `Nothing` and `InstallUnattended` is true. Only
   `appsettings.Development.json` exists besides it.
9. `Umbraco.Cms` is 17.x. The probe ran 17.7.0, whose readiness probe `/umbraco/api/health/ready` the compose health
   check calls. On a later 17.x, Task 4's smoke test proves it still answers.
10. Phase 4's rate limiter keys on `CF-Connecting-IP`. This phase leaves it alone: the header is trustworthy because
    the tunnel is the only way in, and no step publishes a port.
11. `UmbracoSite` sets `DataProtection:KeysDirectory`, registers `ThrowingPathFilter` as an `IStartupFilter`, and has no
    `Hosting:` setting. `WebAppSettings.Load()` takes no argument.
12. The workflow has one job, `build-and-test`, on `ubuntu-latest`, for pull requests and pushes to `main` and
    `replatform`.
13. None of `.github/dependabot.yml`, `deploy/`, `docs/hosting/` or `Dockerfile` exists.
14. Docker 28 or later (the probe ran 29.7.2), Compose 2.24 or later (for `!reset` and optional `env_file`), and Buildx
    0.20 or later (for bake's `attest`).
15. The Font Awesome token is set; the image build needs it as a build secret.
16. Nothing listens on 8443 (the local edge) or 5001 (Task 6's throwaway registry).

`/Users/twinright/Repos/Kitchen-Command-Center/.superpowers/phase-7-planning/`, in the main checkout, holds the probe's
final files (`final-tree/`), its logs and its notes. Read it if a step's outcome surprises you. It is gitignored, so a
worktree does not have it, and it is never committed.

## What the probe already proved

Everything below ran in a clone of `replatform-phase-3` at `6a1cbcb` (Phases 1 and 2 as built, Phase 3 in progress),
on Umbraco 17.7.0 and SQLite, with Docker Desktop 29.7.2 on an arm64 Mac, so the images were native `linux/arm64`, as
on the Pi. Tasks 1 to 7 then ran in order in a clean clone of that commit, with this plan's code blocks as written;
see the end of this section for the results.

**The images**
- `mcr.microsoft.com/dotnet/aspnet:10.0` is Ubuntu 24.04. It has ICU, which Umbraco's cultures need, but no `curl`.
  Its non-root user is `app` (UID 1654), and the image does not switch to it: the Dockerfile says `USER $APP_UID`.
  `node:24-bookworm-slim` still ships Yarn 1.22; Node 26's images drop it, so Node stays on 24.
- `@fortawesome/fontawesome-pro` unpacks to 958 MB, and it sat in KCC.Web's runtime `dependencies`, with
  `tailwindcss`. Both are imported only from CSS, which Vite resolves at build time, so they move to
  `devDependencies`. `yarn.lock` does not change.
- At run time the SSR bundle imports `@vue/compiler-dom`, `@vue/runtime-dom`, `@vue/server-renderer`, `@vue/shared`
  and `qs`, and `Server.js` imports `express`, `compression` and `vue`. A manifest holding only the `dependencies`
  installs against the full `yarn.lock` with `--frozen-lockfile`, in 24 MB, with no token.
- Yarn 1 fetches `devDependencies` even for `yarn install --production`, so the SSR stage strips them from its copy of
  the manifest first. Yarn also reads `.npmrc` on every command and fails while `${FONTAWESOME_NPM_AUTH_TOKEN}` is
  unset, so only the install step sees `.npmrc`, through a bind mount.
- The build stage needs the root `.editorconfig`, which turns CS1591 off. Without it, warnings-as-errors fails the
  publish.
- **`dotnet publish` leaves out `wwwroot/.vite/manifest.json`,** because publish skips folders whose names start with
  a dot, and Vite.AspNetCore 2.4.0 has no publish target. Without the manifest the layout links no stylesheet or
  script. `<Content Include="wwwroot/.vite/**" />` in `KCC.Web.csproj` fixes it.
- The publish output is 237 MB, 156 MB of it Umbraco's backoffice. The uSync folder publishes by uSync's own targets.
  The images measured 719 MB and 377 MB unpacked, about 178 MB and 80 MB compressed. GHCR storage and bandwidth for
  container images are free.

**The app under a read-only root file system**
- At every start Umbraco creates `~/Views` and `~/Views/Partials` unless they exist, so the image creates them.
  Everything else it writes lands under `umbraco/Data`: the database with its `-wal` and `-shm`, `TEMP` (MainDom lock,
  uploads, Examine indexes), the data-protection keys, and the logs once `Logging:Directory` points there. `/tmp` holds
  only .NET's diagnostic pipes, on a tmpfs.
- **A fatal boot error hangs the container instead of ending it.** As PID 1, the crashed .NET process ignores its own
  `SIGABRT`, so it stayed "Up (unhealthy)" for 92 seconds and counting, and neither the restart policy nor
  `compose up --wait` noticed. With `init: true` the same failure exited in 2 seconds with code 134.
- **An empty unattended-install setting stops the boot.** Compose passes `${KCC_ADMIN_EMAIL:-}` as an empty string,
  and Umbraco rejects an empty `UnattendedUserEmail` as not an email address. That is exactly what deleting the
  administrator from `.env` after the first boot would have done. So production turns the install off in
  `appsettings.Production.json`, and the first boot turns it on from an optional `first-boot.env`, deleted afterwards.
  With the install off, a restored database boots normally.
- A fresh first boot reached Run in about 15 seconds and logged `uSync First boot complete 289 changes`. Two warnings
  are expected: five "Configured database is reporting as not being available" while the install creates the file,
  and "The culture specified  was not found" during the dictionary import. Idle, the app used about 208 MiB and ssr
  about 38 MiB.
- Umbraco 17.7 has health endpoints: `/umbraco/api/health/live`, and `/umbraco/api/health/ready`, which answers 200
  only at RuntimeLevel Run. The compose health check uses the second, so the app needs no endpoint of its own. Umbraco
  17.7 has no keep-alive job to turn off.
- Production runtime mode checks four things at boot: an application URL, ModelsBuilder `Nothing`, a Release build,
  and `UseHttps`, which defaults to true since 17.0. It also caches package manifests for 30 days, and skips
  `UseStaticWebAssets()`, so the backoffice clients must be published, as `dotnet publish` does.

**The tunnel's trust boundary**
- With `UseHttps` on, OpenIddict answers a plain-HTTP request to the backoffice's authorize endpoint with ID2083 ("This
  server only accepts HTTPS requests"). ID2029, the missing `client_id`, is its next check, so ID2029 means the request
  was taken as HTTPS. Through a TLS-terminating proxy at `172.30.9.10`, the endpoint answered ID2029. The same
  `X-Forwarded-Proto: https` from `172.30.9.20`, or from the render network, got ID2083.
- In .NET 10 `ForwardedHeadersOptions.KnownNetworks` is obsolete (ASPDEPR005); `KnownIPNetworks` replaces it. With both
  lists empty, the middleware trusts every sender, so the code clears the loopback defaults and adds the tunnel alone.
  The integration tests send the header from 127.0.0.1 and ::1 to prove it.
- `AllowedHosts` answered `Host: app:8080` with 400. ssr on the internal network could not resolve `example.com`.

**Backups**
- `.backup` and `VACUUM INTO` both copy a live WAL database from a read-only mount with integrity `ok`. But `VACUUM
  INTO` writes a rollback-journal file, and Umbraco sets WAL only when it creates a database, while its SQLite locking
  relies on WAL. `.backup` keeps the WAL flag, so snapshots use it.
- With the app stopped there are no `-wal` or `-shm` files, and a read-only mount cannot open the database ("unable to
  open database file"). A read-write mount as UID 1654 can, and the files keep their owner. So the backup job mounts
  the data volume read-write as 1654.
- **An empty named volume takes the content and the owner of the image path it is mounted on.** The backup image's
  Alpine `/media` (`cdrom`, `floppy`, `usb`, owned by root) filled the empty media volume and left it owned by root, so
  the app could no longer save an upload. The backup image mounts its volumes at `/srv/kcc/data`, `/srv/kcc/media` and
  `/srv/kcc/snapshots`, empty directories owned by 1654. `nocopy` would be wrong here: a brand-new volume would then
  stay owned by root, and a restore into it would fail.
- rclone's local backend errors when listing a prefix that does not exist yet, where S3 returns an empty list. So
  pruning deletes from the bucket root with an `--include` filter. `RCLONE_CONFIG=/dev/null` silences the "config file
  not found" notices, since the remote comes from environment variables.
- A nightly backup, then a restore into brand-new volumes under another project name, then a boot with the install
  off, came up on the restored database. The media marker was served, and uSync ran its startup import, not a first
  boot.

**Deploying by pull**
- `docker compose config --images app` also prints the images of the services `app` depends on. So `deploy.sh`
  compares each running container's image with the image its own tag now names.
- Against a throwaway registry on `localhost:5001`, a second run changed nothing, in 2 seconds with no output
  (`--progress quiet`). A new `kcc-app:main` made the next run take a pre-deploy snapshot and recreate only the app, in
  13 seconds; ssr and the edge kept running.
- Pulling images from Docker Hub every five minutes stalled one probe run for 11 minutes, and could meet Docker Hub's
  anonymous pull limits. So `deploy.sh` pulls only the three GHCR images, and cloudflared's pinned tag changes with
  `compose.yaml`.
- cloudflared 2026.9.3 runs as UID 65532 on distroless (no shell), with `ENTRYPOINT ["cloudflared",
  "--no-autoupdate"]`. `cloudflared tunnel --metrics 127.0.0.1:2000 ready` exits 1 until the tunnel is connected, so
  it serves as the health check.

**The clean-clone run** (Tasks 1 to 7 applied to `6a1cbcb`, on 2026-09-27):
- unit 160 (3 new), integration 134 (6 new), E2E 19, and Vitest 755 (2 new) beside the suite's two expected failures;
- the type check, and Prettier once it had its own line break in the new test;
- `dotnet build` with 0 warnings, and `yarn install --frozen-lockfile` leaving `yarn.lock` unchanged;
- `docker buildx bake --load` building the three images, and the smoke test passing its ten checks in 26 seconds;
- the deploy test, whose second run took 1 second and printed nothing, and whose third snapshotted the database and
  recreated the app alone;
- actionlint and shellcheck with nothing to say, and `systemd-analyze verify` clean in `debian:trixie`.

Changed after that run, and checked again:
- `compose.yaml` gained its comments and a quoted health check: `docker compose config` validates it;
- the smoke test's `curl` comes from the app image, so Task 4 needs no backup image, and it checks that the seeder
  and the baseline export answer 404 in production: both versions passed again, with 8 and 11 checks;
- `deploy.sh` deploys only for changes in `deploy/`: the deploy test passed again, and a sparse clone showed the git
  check;
- the runbook's rollback and production-data commands ran against a local stack: the downloaded database passed its
  integrity check, and the app came back healthy after a rollback;
- teardown names `--profile jobs`: `docker compose down -v` skips services in an inactive profile, so the jobs'
  `snapshots` volume and the default network outlived every run, and a later local run could have restored a stale
  archive. With the flag, a run leaves nothing behind.

One thing the probe got wrong at first: rebuilding an unchanged image with a warm cache gives the same digest, creation
time included. So a merge that changes no image deploys nothing on the Pi.

## Global Constraints

Phases 1 to 6's constraints still apply:

- The replatform's spec, phase plans and reference set are tracked in `docs/replatform/`: commit changes to them (a
  status line, a correction) with the work they describe. Everything else under `.superpowers/` stays gitignored:
  never stage anything there. Commit messages are Title Case imperative with no attribution lines. Ask the owner before
  any `git push`.
- **Umbraco.Cms 17.x, never 18.** Every `Umbraco.Cms*` package takes the same version as `Umbraco.Cms`.
- **The SQLite connection string never contains `Cache=Shared`.**
- **`ModelsMode`** is `SourceCodeManual` in Development and `Nothing` everywhere else. Production runtime mode refuses
  to boot otherwise.
- **Only APIs that survive Umbraco 18.** Warnings are errors, so an obsolete member fails the build (CS0618, and
  ASPDEPR005 for `ForwardedHeadersOptions.KnownNetworks`).
- **Nullable reference types and StyleCop.**
  - Nullable is disabled in `KCC.Web`, `KCC.Contributions`, `KCC.Admin` and `KCC.UnitTests`. Never write `?` on a
    reference type there: it raises CS8632.
  - `KCC.IntegrationTests` and `KCC.E2ETests` have nullable enabled.
  - StyleCop runs on `src/KCC.Web`. It requires sorted usings, trailing commas in multi-line initializers, and static
    members before instance members.
- **Code comments** follow `~/.claude/CLAUDE.md`: explain *why* only, with no narration and no future promises. The
  same goes for comments in shell, YAML, Dockerfiles and systemd units.
- **Never name a file or folder `icon`, or anything starting with `backup`.** `.gitignore`'s `Icon` line and its
  Visual Studio `Backup*/` line ignore them without a word, because `core.ignorecase` is on. The backup image lives in
  `deploy/kcc-backup/` for that reason. Check a new path with `git check-ignore -v <path>`.
- **Test commands:**
  - unit: `dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj`
  - integration: `dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`
  - E2E: `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj`
  - one class: append `-- --treenode-filter "/*/*/<ClassName>/*"`
  - front end: `cd src/KCC.Web && yarn test`, then `yarn type-check`
  - everything: `node tests/scripts/run.mjs` (it opens its HTML report when it finishes)
- **Build from the repository root.** Run `dotnet build KitchenCommandCenter.sln` and the root `yarn build:all` before
  the integration and E2E suites: the integration host refuses to start without the Vite manifest.

Phase 7's own constraints:

- **Work on branch `hosting`, from `main`.** It reaches `main` through one pull request (Task 8). The owner approves
  the push, the pull request and the merge. The phase closes in a second, small pull request (Task 12).
- **Agents never act in the owner's accounts.** Cloudflare, GitHub's settings, healthchecks.io and the Pi are the
  owner's (Tasks 9 and 10). The agent guides, and checks from outside with `curl` and `gh`. It never asks for, reads
  or pastes a secret: tokens and passwords go straight from a dashboard into the Pi's `.env` or `first-boot.env`.
- **Secrets never enter git** (spec §6.5). The root `.gitignore` ignores `deploy/.env` (its bare `.env` line) and
  `deploy/first-boot.env` (a line Task 4 adds). The `*.example` files hold names and comments only.
- **Images are built only by `docker buildx bake` at the repository root, and never on the Pi.** The client and SSR
  bundles come from one `yarn build:all` in one stage. The Font Awesome token is a build secret, never a layer or a
  build argument. The GHCR packages stay private, because the images carry Font Awesome Pro's files.
- **`compose.yaml` never publishes a host port.** The rate limiter's key (`CF-Connecting-IP`) and the forwarded
  headers are trustworthy only while the tunnel is the only way in (spec §8, §18). `local.yaml` publishes
  `127.0.0.1:8443` for runs on a developer's machine, and never runs on the Pi.
- **Fixed addresses.** The `edge` network is `172.30.9.0/24`, cloudflared is `172.30.9.10`, and the app's
  `Hosting__TunnelAddress` names it. Change the three together.
- **Container users.** The app runs as UID 1654 (`app`), and so do the backup jobs; ssr runs as `node` (1000), and
  cloudflared as 65532. A path that a named volume mounts over must be an empty directory owned by 1654 in its image,
  because an empty volume takes the content and the owner of the directory it is first mounted on.
- **Pinned versions.** Base images, cloudflared and Caddy take exact tags. Dependabot proposes updates (Task 7),
  holding .NET on 10, Node on 24 (its images from 26 on drop Yarn 1) and Umbraco on 17.
- **Shell is POSIX `sh`** (BusyBox ash in the backup image, dash on the Pi and in the SDK image), with `set -eu`, and
  passes `shellcheck -s sh`.
- **Snapshots use SQLite's `.backup`,** never `VACUUM INTO` or a copy of the live file: Umbraco sets WAL only when it
  creates a database, and its locking depends on WAL.

## Not in Phase 7

- **Phase 8:** real content authored in production; going public, which deletes the whole-site Access application and
  turns `RobotsTxtDenyAll` off; the Umbraco 17.8.0 bump before launch (spec §6.1); the full README, CLAUDE.md and
  memory rewrite; retiring the reference worktree and the old SQL Server container.
- **Not built, on purpose:**
  - amd64 images. The Dockerfile builds them as it stands: the VPS fallback (spec §13.7) adds `linux/amd64` to the
    bake file's platforms, as the runbook says.
  - long cache headers on the hashed `/assets` files. Cloudflare caches them by extension already, and ImageSharp's
    media responses already carry a year.
  - an uptime monitor for the site itself, restarts of containers that are unhealthy but still running (Docker restarts
    only those that exit), and client-side encryption of the backups.
  - a CI trigger cleanup: the workflow still lists `replatform`, which Phase 8's rewrite can drop.

## File map

| Path | Change | Task |
|---|---|---|
| `src/KCC.Web/Features/Hosting/TunnelForwardedHeaders.cs` | Create | 1 |
| `src/KCC.Web/Program.cs` | Modify | 1 |
| `tests/KCC.IntegrationTests/Config/UmbracoSite.cs` | Modify | 1 |
| `tests/KCC.IntegrationTests/Features/Hosting/TunnelTests.cs` | Create | 1 |
| `src/KCC.Web/appsettings.Production.json` | Create | 2 |
| `src/KCC.Web/KCC.Web.csproj` | Modify | 2 |
| `tests/KCC.UnitTests/Features/Configuration/{WebAppSettings,ProductionSettingsTests}.cs` | Modify / create | 2 |
| `CLAUDE.md` | Modify | 2 |
| `Dockerfile`, `.dockerignore`, `docker-bake.hcl` | Create; Task 5 adds the backup target | 3 |
| `src/KCC.Web/package.json` | Modify | 3 |
| `tests/KCC.ViteTests/Features/Ssr/ssrRuntimeDependencies.test.ts` | Create | 3 |
| `deploy/compose.yaml`, `deploy/local.yaml`, `deploy/local/Caddyfile` | Create; Task 5 adds the jobs to compose | 4 |
| `deploy/.env.example`, `deploy/first-boot.env.example`, `deploy/kcc`, `deploy/smoke-test.sh` | Create; Task 5 extends the env example and the smoke test | 4 |
| `.gitignore` | Modify | 4 |
| `deploy/kcc-backup/{Dockerfile,kcc-backup}` | Create | 5 |
| `deploy/deploy.sh`, `deploy/systemd/kcc-{deploy,backup}.{service,timer}` | Create | 6 |
| `.github/workflows/build-and-test.yml` | Modify | 7 |
| `.github/dependabot.yml` | Create | 7 |
| `docs/hosting/runbook.md` | Create | 8 |
| `README.md`, the spec (§6.5, §13, §15) | Modify | 8 |
| This plan's Status line and "Findings from Phase 7", `docs/hosting/runbook.md` corrections, memory | Modify | 12 |

---

### Task 1: Trust forwarded headers from the tunnel alone

Cloudflare's edge terminates TLS, so every request reaches the app as plain HTTP from the tunnel container. With
Umbraco's `UseHttps` on, as Production runtime mode requires, the backoffice's sign-in refuses plain HTTP, and cookies
lose `Secure`. The forwarded headers fix that, but only from the tunnel: from anyone else they would be a way to lie
about the scheme and the client's address. Spec §8: "Forwarded headers are trusted from the tunnel container alone".

**Files:**
- Create: `src/KCC.Web/Features/Hosting/TunnelForwardedHeaders.cs`
- Modify: `src/KCC.Web/Program.cs`
- Modify: `tests/KCC.IntegrationTests/Config/UmbracoSite.cs`
- Create: `tests/KCC.IntegrationTests/Features/Hosting/TunnelTests.cs`

**Interfaces:**
- Produces: `KCC.Web.Features.Hosting.TunnelForwardedHeaders` with `public const string AddressSetting =
  "Hosting:TunnelAddress"`, `public static ForwardedHeadersOptions For(IPAddress tunnel)` and `public static
  WebApplication UseTunnelForwardedHeaders(this WebApplication app)`. Task 4's `compose.yaml` sets
  `Hosting__TunnelAddress: 172.30.9.10`.
- Produces: `UmbracoSite.TunnelAddress` (`"192.0.2.10"`) and `UmbracoSite.PeerHeader`
  (`"X-Integration-Tests-Peer"`) for tests that need a request to arrive from a given address.

- [ ] **Step 1: Give the test site a tunnel and a way to choose where a request comes from**

A test server leaves `HttpContext.Connection.RemoteIpAddress` unset, so a startup filter, ahead of the app's own
middleware, sets it from a header. The tunnel's address is from the documentation range, so it can never be a real
peer.

In `tests/KCC.IntegrationTests/Config/UmbracoSite.cs`:

1. Add `using System.Net;` directly above `using System.Security.Cryptography;`.
2. After the `ThrowingPath` constant, add:

   ```csharp

       // A documentation address stands in for the tunnel container, and the header chooses the address a request
       // arrives from, which a test server otherwise leaves unset.
       public const string TunnelAddress = "192.0.2.10";
       public const string PeerHeader = "X-Integration-Tests-Peer";
   ```

3. In `ConfigureWebHost`, after the line that registers `ThrowingPathFilter`, add:

   ```csharp
           builder.ConfigureServices(services => services.AddTransient<IStartupFilter, PeerFilter>());
   ```

4. In `Settings()`, after the `["DataProtection:KeysDirectory"]` entry, add:

   ```csharp
           ["Hosting:TunnelAddress"] = TunnelAddress,
   ```

5. After the `ThrowingPathFilter` class, before the file's closing brace, add:

   ```csharp

       // Ahead of the app's own middleware, so the forwarded-headers middleware sees the chosen address.
       private sealed class PeerFilter : IStartupFilter
       {
           public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
           {
               app.Use(async (context, nextMiddleware) =>
               {
                   if (context.Request.Headers.TryGetValue(PeerHeader, out var peer))
                   {
                       context.Connection.RemoteIpAddress = IPAddress.Parse(peer.ToString());
                   }

                   await nextMiddleware(context);
               });
               next(app);
           };
       }
   ```

- [ ] **Step 2: Write the failing tests**

OpenIddict answers a plain-HTTP request to the backoffice's authorize endpoint with ID2083, and its next check, for
the missing `client_id`, with ID2029. So ID2029 proves the request was taken as HTTPS, through the same pipeline the
backoffice sign-in uses. The loopback addresses prove the defaults were cleared: ASP.NET Core trusts `127.0.0.1/8` and
`::1` unless told otherwise.

Create `tests/KCC.IntegrationTests/Features/Hosting/TunnelTests.cs`:

```csharp
using KCC.IntegrationTests.Config;

namespace KCC.IntegrationTests.Features.Hosting;

public class TunnelTests
{
    private const string Authorize = "/umbraco/management/api/v1/security/back-office/authorize";

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    // The backoffice's sign-in refuses plain HTTP (OpenIddict ID2083) while UseHttps is on, as production requires.
    // ID2029, the missing client_id, is the next check it makes, so it took the request as HTTPS.
    [Test]
    public async Task BackofficeSignIn_ForwardedAsHttpsByTheTunnel_IsTakenAsHttps()
    {
        var body = await AuthorizeAsync(UmbracoSite.TunnelAddress);

        _ = await Assert.That(body).Contains("ID2029");
    }

    [Test]
    [Arguments("192.0.2.99")]
    [Arguments("127.0.0.1")]
    [Arguments("::1")]
    public async Task BackofficeSignIn_ForwardedAsHttpsByAnyoneElse_IsRefused(string peer)
    {
        var body = await AuthorizeAsync(peer);

        _ = await Assert.That(body).Contains("ID2083");
    }

    [Test]
    public async Task HttpsResponses_CarryStrictTransportSecurity()
    {
        using var client = Site.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/robots.txt");
        request.Headers.Host = "kcc.example.test";
        request.Headers.Add(UmbracoSite.PeerHeader, UmbracoSite.TunnelAddress);
        request.Headers.Add("X-Forwarded-Proto", "https");
        using var response = await client.SendAsync(request);

        _ = await Assert.That(response.Headers.Contains("Strict-Transport-Security")).IsTrue();
    }

    [Test]
    public async Task PlainHttpResponses_CarryNoStrictTransportSecurity()
    {
        using var client = Site.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/robots.txt");
        request.Headers.Host = "kcc.example.test";
        using var response = await client.SendAsync(request);

        _ = await Assert.That(response.Headers.Contains("Strict-Transport-Security")).IsFalse();
    }

    private async Task<string> AuthorizeAsync(string peer)
    {
        using var client = Site.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, Authorize);
        request.Headers.Add(UmbracoSite.PeerHeader, peer);
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Add("X-Forwarded-For", "203.0.113.7");
        using var response = await client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }
}
```

The HSTS tests use a host other than `localhost`, which ASP.NET Core's HSTS middleware skips.

- [ ] **Step 3: Run them to see them fail**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj --no-build -- --treenode-filter "/*/*/TunnelTests/*"
```

Expected: `total: 6`, `failed: 2`. `BackofficeSignIn_ForwardedAsHttpsByTheTunnel_IsTakenAsHttps` finds ID2083 in the
body, and `HttpsResponses_CarryStrictTransportSecurity` finds no header. The other four pass.

- [ ] **Step 4: Trust the tunnel alone**

Create `src/KCC.Web/Features/Hosting/TunnelForwardedHeaders.cs`:

```csharp
using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace KCC.Web.Features.Hosting;

public static class TunnelForwardedHeaders
{
    public const string AddressSetting = "Hosting:TunnelAddress";

    public static ForwardedHeadersOptions For(IPAddress tunnel)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        };

        // The middleware trusts any sender once both lists are empty, so the tunnel replaces the loopback defaults
        // rather than being added to them.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        options.KnownProxies.Add(tunnel);
        return options;
    }

    // Cloudflare's edge terminates TLS, so every request reaches the app as plain HTTP from the tunnel container.
    // Without the forwarded scheme Umbraco's backoffice sign-in refuses the request, and cookies lose Secure.
    public static WebApplication UseTunnelForwardedHeaders(this WebApplication app)
    {
        var address = app.Configuration[AddressSetting];
        if (!string.IsNullOrWhiteSpace(address))
        {
            app.UseForwardedHeaders(For(IPAddress.Parse(address)));
        }

        return app;
    }
}
```

`ForwardLimit` stays at its default of 1, so the client address is the rightmost `X-Forwarded-For` entry: the one
Cloudflare appends, never one a client wrote. Where `Hosting:TunnelAddress` is unset (development, E2E), no middleware
runs and the headers mean nothing, as before.

In `src/KCC.Web/Program.cs`:

1. Add `using KCC.Web.Features.Hosting;` in sorted order (after `KCC.Web.Features.Dictionary`).
2. Directly after `await app.BootUmbracoAsync();`, add a blank line and `app.UseTunnelForwardedHeaders();`, so it runs
   before every other middleware.
3. Inside the `if (!app.Environment.IsDevelopment())` block, after `app.UseExceptionHandler("/error");`, add
   `app.UseHsts();`.

As in the probe, the start of the pipeline then reads:

```csharp
await app.BootUmbracoAsync();

app.UseTunnelForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
```

HSTS answers only HTTPS requests, and Cloudflare's "Always Use HTTPS" (Task 9) redirects plain HTTP at the edge. The
app never redirects to HTTPS itself: behind the tunnel every request it sees is HTTP, and Cloudflare warns that
redirecting at both ends can loop.

- [ ] **Step 5: Run them to see them pass, then the whole integration suite**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj --no-build -- --treenode-filter "/*/*/TunnelTests/*"
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj --no-build
```

Expected: the build has 0 warnings; `TunnelTests` is `total: 6`, `failed: 0`; the whole suite passes, 6 more than
before. The probe's filtered run took about a minute, most of it the site's first boot.

- [ ] **Step 6: Commit**

```bash
git add src/KCC.Web/Features/Hosting/TunnelForwardedHeaders.cs src/KCC.Web/Program.cs \
  tests/KCC.IntegrationTests/Config/UmbracoSite.cs tests/KCC.IntegrationTests/Features/Hosting/TunnelTests.cs
git commit -m "Trust Forwarded Headers from the Tunnel Alone"
```

---

### Task 2: Production settings and the published manifest

The container runs in the Production environment, which reads `appsettings.Production.json`. Three settings belong
there rather than in `compose.yaml`: they hold no secret and no hostname, and unit tests can read them.
- **Umbraco's Production runtime mode** (spec §6.1, Phase 1's findings). At boot it checks the application URL,
  ModelsBuilder `Nothing`, a Release build and `UseHttps`.
- **Logs on the data volume** (spec §13.2). The root file system is read-only, and `umbraco/Data` is the volume.
- **The install switched off.** With it on, a lost database would be replaced by an empty site without a word. The
  first boot switches it on for itself, from `deploy/first-boot.env` (Task 4).

The published app also needs the Vite manifest, which `dotnet publish` leaves out.

**Files:**
- Create: `src/KCC.Web/appsettings.Production.json`
- Modify: `src/KCC.Web/KCC.Web.csproj`
- Modify: `tests/KCC.UnitTests/Features/Configuration/WebAppSettings.cs`
- Create: `tests/KCC.UnitTests/Features/Configuration/ProductionSettingsTests.cs`
- Modify: `CLAUDE.md`

**Interfaces:**
- Produces: `WebAppSettings.Load(string fileName = "appsettings.json")`, which reads any settings file of KCC.Web's.
- Produces: `Umbraco:CMS:Unattended:InstallUnattended` false in Production. Task 4's `first-boot.env` sets
  `Umbraco__CMS__Unattended__InstallUnattended=true` for the first boot only.

- [ ] **Step 1: Let the settings helper read any file**

In `tests/KCC.UnitTests/Features/Configuration/WebAppSettings.cs`, change the signature to
`public static JsonElement Load(string fileName = "appsettings.json")`, and the path line to:

```csharp
        var path = Path.Combine(directory!.FullName, "src", "KCC.Web", fileName);
```

- [ ] **Step 2: Write the failing tests**

Create `tests/KCC.UnitTests/Features/Configuration/ProductionSettingsTests.cs`:

```csharp
using System.Text.Json;

namespace KCC.UnitTests.Features.Configuration;

public class ProductionSettingsTests
{
    [Test]
    public async Task Production_RunsInUmbracosProductionMode()
    {
        var mode = Cms().GetProperty("Runtime").GetProperty("Mode").GetString();

        _ = await Assert.That(mode).IsEqualTo("Production");
    }

    // The container's root file system is read-only, and umbraco/Data is the volume that survives a deploy.
    [Test]
    public async Task Production_LogsToTheDataVolume()
    {
        var directory = Cms().GetProperty("Logging").GetProperty("Directory").GetString();

        _ = await Assert.That(directory).IsEqualTo("~/umbraco/Data/Logs");
    }

    // Otherwise a lost database is silently replaced by an empty site. The first boot turns the install on for
    // itself, in deploy/first-boot.env.
    [Test]
    public async Task Production_NeverInstallsASiteByItself()
    {
        var install = Cms().GetProperty("Unattended").GetProperty("InstallUnattended").GetBoolean();

        _ = await Assert.That(install).IsFalse();
    }

    private static JsonElement Cms() =>
        WebAppSettings.Load("appsettings.Production.json").GetProperty("Umbraco").GetProperty("CMS");
}
```

- [ ] **Step 3: Run them to see them fail**

```bash
dotnet build tests/KCC.UnitTests/KCC.UnitTests.csproj
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj --no-build -- --treenode-filter "/*/*/ProductionSettingsTests/*"
```

Expected: `total: 3`, `failed: 3`, each with a `FileNotFoundException` for `appsettings.Production.json`.

- [ ] **Step 4: Write the settings**

Create `src/KCC.Web/appsettings.Production.json`:

```json
{
  "Umbraco": {
    "CMS": {
      "Runtime": {
        "Mode": "Production"
      },
      "Unattended": {
        "InstallUnattended": false
      },
      "Logging": {
        "Directory": "~/umbraco/Data/Logs"
      }
    }
  }
}
```

`UseHttps` needs no line: it is true by default since Umbraco 17.0, and Production mode refuses to boot without it.
`UpgradeUnattended` stays true from `appsettings.json`, so a deploy of a newer Umbraco migrates the database at start.

- [ ] **Step 5: Run them to see them pass**

Run Step 3's commands again, then the whole unit suite:

```bash
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj --no-build -- --treenode-filter "/*/*/ProductionSettingsTests/*"
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj --no-build
```

Expected: `total: 3`, `failed: 0`, then the whole suite green, 3 more than before.

- [ ] **Step 6: Show that publish leaves the Vite manifest out**

After a `yarn build:all` at the root, the manifest is in `src/KCC.Web/wwwroot/.vite/`. Publish, and look for it:

```bash
yarn build:all
PUBLISH="$(mktemp -d)/publish"
dotnet publish src/KCC.Web/KCC.Web.csproj -c Release -o "$PUBLISH"
ls "$PUBLISH/wwwroot/.vite/manifest.json"
```

Expected: `No such file or directory`. `dotnet publish` skips every folder whose name starts with a dot, and
Vite.AspNetCore 2.4.0 ships no publish target. A published site would link no stylesheet or script at all.

- [ ] **Step 7: Publish it**

In `src/KCC.Web/KCC.Web.csproj`, after the `<ItemGroup>` of `PackageReference`s, add:

```xml
    <ItemGroup>
        <!-- Publish skips folders whose names start with a dot, and Vite.AspNetCore reads the manifest from there. -->
        <Content Include="wwwroot/.vite/**" />
    </ItemGroup>
```

Run Step 6's `dotnet publish` and `ls` again, into a fresh folder. Expected: the `ls` prints the manifest's path.
`manifest.json.br` and `.gz` beside it are publish's precompressed copies, which do no harm. Task 4's smoke test then
proves the published site links assets that load.

- [ ] **Step 8: Record the manifest in CLAUDE.md**

The SFC section's **Production** bullet explains how style-block CSS reaches the page in production, and this is
what it silently depends on. In `CLAUDE.md`, replace the bullet:

```markdown
- **Production** — the client build extracts it into chunk CSS assets (`GlobalComponents-*.css`). A
  `<link rel="stylesheet" vite-href="/Features/Main.ts">` in `Layout.cshtml` makes Vite.AspNetCore emit a
  `<link>` for every CSS file in the entry's import graph.
```

with:

```markdown
- **Production** — the client build extracts it into chunk CSS assets (`GlobalComponents-*.css`). A
  `<link rel="stylesheet" vite-href="/Features/Main.ts">` in `Layout.cshtml` makes Vite.AspNetCore emit a
  `<link>` for every CSS file in the entry's import graph. It finds them in `wwwroot/.vite/manifest.json`, which
  `dotnet publish` skips like every dot-folder unless `KCC.Web.csproj` includes it, as it does: without it the
  published site links no CSS at all.
```

- [ ] **Step 9: Commit**

```bash
git add src/KCC.Web/appsettings.Production.json src/KCC.Web/KCC.Web.csproj CLAUDE.md \
  tests/KCC.UnitTests/Features/Configuration/WebAppSettings.cs \
  tests/KCC.UnitTests/Features/Configuration/ProductionSettingsTests.cs
git commit -m "Run in Production Mode and Publish the Vite Manifest"
```

---

### Task 3: The site images

One Dockerfile at the repository root builds both site images, from a build context that is the repository root
because of the yarn workspace layout (spec §13.4). The stages:
- **frontend** installs the workspace and runs the root `yarn build:all` once: the two backoffice clients, then
  KCC.Web's client and SSR bundles.
- **ssr-deps** installs only the SSR service's runtime dependencies.
- **build** restores and publishes KCC.Web with every project's built `wwwroot` in place, so publish copies the
  backoffice clients' `App_Plugins` into the app's.
- **app** and **ssr** are the two images.

The three build stages run on the build machine's own platform (`--platform=$BUILDPLATFORM`): JavaScript bundles and a
framework-dependent publish are the same on any platform, and `-a $TARGETARCH` picks the runtime's native files.

**Files:**
- Modify: `src/KCC.Web/package.json`
- Create: `tests/KCC.ViteTests/Features/Ssr/ssrRuntimeDependencies.test.ts`
- Create: `Dockerfile`, `.dockerignore`, `docker-bake.hcl`

**Interfaces:**
- Produces: images `${KCC_REGISTRY}/kcc-app:${KCC_IMAGE_TAG}` and `${KCC_REGISTRY}/kcc-ssr:${KCC_IMAGE_TAG}`, with
  `KCC_REGISTRY` defaulting to `ghcr.io/th3fenriswolf` and `KCC_IMAGE_TAG` to `local`. The app image listens on 8080 as
  UID 1654, with content root `/app`, so its volumes mount at `/app/umbraco/Data` and `/app/wwwroot/media`. The ssr image
  listens on 3001 as `node`.
- Produces: the bake targets `app` and `ssr`, and the hidden `_image` and `_site`. Task 5 adds `backup`.

- [ ] **Step 1: Write the failing test for the SSR service's dependencies**

The ssr image installs KCC.Web's `dependencies` and nothing else. So everything the server imports at run time must
be among them, and nothing only the build needs may be. To check a package by hand, look for it in the imports of
`Features/Ssr/Server.js` and `CollectCss.js`, and in the built bundle's imports:
`grep -oE 'from"[^".][^"]*"' src/KCC.Web/wwwroot/ssr/Server.Entry.js | sort -u`. The probe found `@vue/*` and `qs`
there, which `vue` and `qs` supply.

Create `tests/KCC.ViteTests/Features/Ssr/ssrRuntimeDependencies.test.ts`:

```ts
import { readFileSync } from 'node:fs'
import { builtinModules } from 'node:module'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

// The SSR image installs package.json's dependencies and nothing else (the Dockerfile's ssr-deps stage). What the
// server imports at run time must be among them, and what only the build needs must not: Font Awesome Pro alone
// unpacks to almost a gigabyte.
const WEB = fileURLToPath(new URL('../../../../src/KCC.Web/', import.meta.url))
const dependencies = Object.keys(JSON.parse(readFileSync(join(WEB, 'package.json'), 'utf8')).dependencies)

const packageOf = (specifier: string) =>
  specifier
    .split('/')
    .slice(0, specifier.startsWith('@') ? 2 : 1)
    .join('/')

const serverImports = ['Features/Ssr/Server.js', 'Features/Ssr/CollectCss.js'].flatMap((file) =>
  [...readFileSync(join(WEB, file), 'utf8').matchAll(/^import\b.*?\bfrom\s+'([^'.][^']*)'/gm)].map(([, specifier]) =>
    packageOf(specifier),
  ),
)

describe('the SSR service image', () => {
  it('installs every package the server imports', () => {
    const packages = serverImports.filter((name) => !builtinModules.includes(name) && !name.startsWith('node:'))

    expect(packages.filter((name) => !dependencies.includes(name))).toEqual([])
  })

  it('leaves packages only the build needs to the build', () => {
    expect(dependencies).not.toContain('@fortawesome/fontawesome-pro')
    expect(dependencies).not.toContain('tailwindcss')
  })
})
```

The dynamic `import('vite')` in `Server.js` runs in development only, and the pattern skips it on purpose.

- [ ] **Step 2: Run it to see it fail**

```bash
cd src/KCC.Web && yarn test ssrRuntimeDependencies && cd ../..
```

Expected: 1 failed, 1 passed. `leaves packages only the build needs to the build` fails: "expected [ Array(6) ] to not
include '@fortawesome/fontawesome-pro'".

- [ ] **Step 3: Move the build-only packages**

Both are imported only from CSS (`Features/Styles/Main.css` and `TailwindConfig.css`), which Vite resolves at build
time. In `src/KCC.Web/package.json`, take `"@fortawesome/fontawesome-pro": "^7.3.0"` and `"tailwindcss": "^4.3.2"` out
of `dependencies`, and add them to `devDependencies` in alphabetical order. The first goes before
`"@tailwindcss/postcss"`, the second between `"prettier-plugin-tailwindcss"` and `"typescript"`. As built on
`replatform-phase-3`, `dependencies` becomes:

```json
  "dependencies": {
    "compression": "^1.8.1",
    "express": "^5.2.1",
    "qs": "^6.15.3",
    "vue": "^3.5.39"
  },
```

Keep the versions the file has, if later phases moved them. `yarn.lock` records no dependency type, so it must not
change:

```bash
yarn install --frozen-lockfile
git diff --exit-code yarn.lock
cd src/KCC.Web && yarn test && yarn type-check && cd ../..
```

Expected: the install succeeds, `git diff` prints nothing and exits 0, and the whole Vitest suite passes, 2 more than
before, with the type check.

- [ ] **Step 4: Write the Dockerfile**

Create `Dockerfile` at the repository root:

```dockerfile
# syntax=docker/dockerfile:1

# The client and SSR bundles come out of one `yarn build:all` in the frontend stage, and both site images copy from
# it: scope IDs hash each component's path and content, so bundles built apart would strip every scoped style.

FROM --platform=$BUILDPLATFORM node:24.21.0-bookworm-slim AS frontend
WORKDIR /repo
COPY package.json yarn.lock ./
COPY packages/ packages/
COPY src/KCC.Web/package.json src/KCC.Web/
COPY src/KCC.Admin/Client/package.json src/KCC.Admin/Client/
COPY src/KCC.Contributions/Client/package.json src/KCC.Contributions/Client/
# Yarn reads .npmrc on every command and fails while its token variable is unset, so only the install sees it.
RUN --mount=type=bind,source=.npmrc,target=.npmrc \
    --mount=type=secret,id=fontawesome,env=FONTAWESOME_NPM_AUTH_TOKEN \
    --mount=type=cache,target=/usr/local/share/.cache/yarn \
    yarn install --frozen-lockfile --network-timeout 600000
COPY src/ src/
RUN yarn build:all \
    && for wwwroot in src/*/wwwroot; do mkdir -p "/out/$wwwroot" && cp -R "$wwwroot/." "/out/$wwwroot/"; done

FROM --platform=$BUILDPLATFORM node:24.21.0-bookworm-slim AS ssr-deps
WORKDIR /deps
COPY src/KCC.Web/package.json yarn.lock ./
# Yarn fetches devDependencies even for a production install, and Font Awesome's registry wants a token, so the SSR
# service's install reads a manifest with its runtime dependencies only.
RUN --mount=type=cache,target=/usr/local/share/.cache/yarn \
    node -e "const fs = require('fs'); const p = JSON.parse(fs.readFileSync('package.json')); delete p.devDependencies; fs.writeFileSync('package.json', JSON.stringify(p))" \
    && yarn install --production --frozen-lockfile --ignore-scripts --network-timeout 600000

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
ARG TARGETARCH
WORKDIR /repo
COPY .editorconfig global.json nuget.config Directory.Packages.props KCCStandardRules.ruleset ./
COPY src/KCC.Web/KCC.Web.csproj src/KCC.Web/
COPY src/KCC.Contributions/KCC.Contributions.csproj src/KCC.Contributions/
COPY src/KCC.Admin/KCC.Admin.csproj src/KCC.Admin/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore src/KCC.Web/KCC.Web.csproj -a $TARGETARCH
COPY src/ src/
COPY --from=frontend /out/ ./
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish src/KCC.Web/KCC.Web.csproj -c Release -a $TARGETARCH --no-restore -o /app \
    && rm -rf /app/wwwroot/ssr

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12 AS app
# The health check in deploy/compose.yaml needs an HTTP client, which the runtime image lacks.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app ./
# Named volumes mount over umbraco/Data and wwwroot/media and take their owner, so the app can write its database,
# keys, logs and media. Umbraco creates Views/Partials at every start unless it exists, which a read-only root refuses.
RUN mkdir -p umbraco/Data wwwroot/media Views/Partials && chown $APP_UID:$APP_UID umbraco/Data wwwroot/media
USER $APP_UID
ENTRYPOINT ["dotnet", "KCC.Web.dll"]

FROM node:24.21.0-bookworm-slim AS ssr
ENV NODE_ENV=production SSR_PORT=3001
WORKDIR /srv/ssr
COPY --from=ssr-deps /deps/node_modules/ node_modules/
COPY --from=frontend /repo/src/KCC.Web/package.json ./
COPY --from=frontend /repo/src/KCC.Web/Features/Ssr/Server.js /repo/src/KCC.Web/Features/Ssr/CollectCss.js Features/Ssr/
COPY --from=frontend /repo/src/KCC.Web/wwwroot/ssr/ wwwroot/ssr/
USER node
CMD ["node", "Features/Ssr/Server.js"]
```

Notes for a reviewer:
- The `.editorconfig` in the build stage turns CS1591 off; without it, warnings as errors fail the publish.
- `wwwroot/ssr` is the SSR bundle, which only the ssr image runs, so the app image drops it.
- The ssr image keeps `Server.js`'s relative layout: it loads `../../wwwroot/ssr/Server.Entry.js` from
  `Features/Ssr/`, and `package.json`'s `"type": "module"` makes Node read both files as ES modules.
- If Step 1 of "Before you start" listed other workspaces, give each its `package.json` `COPY` line in the frontend
  stage. If `KCC.Web` references another project, give its `.csproj` a `COPY` line before the restore.

- [ ] **Step 5: Write the build context's exclusions**

The context is the repository root, so `.dockerignore` keeps out what the build makes itself, the local database and
media, and the checkouts under `.claude/worktrees`. Create `.dockerignore`:

```text
**/node_modules
**/bin
**/obj
**/.DS_Store
**/*.user
.git
.github
.claude
.superpowers
.vscode
.idea
deploy
docs
tests
**/.env
src/*/Client/dist
src/*/wwwroot/App_Plugins
src/KCC.Web/wwwroot/.vite
src/KCC.Web/wwwroot/assets
src/KCC.Web/wwwroot/ssr
src/KCC.Web/wwwroot/media
src/KCC.Web/umbraco
src/KCC.Web/Features/Models/Generated/ood.flag
```

- [ ] **Step 6: Write the bake file**

Create `docker-bake.hcl` at the repository root:

```hcl
variable "KCC_REGISTRY" {
  default = "ghcr.io/th3fenriswolf"
}

variable "KCC_IMAGE_TAG" {
  default = "local"
}

group "default" {
  targets = ["app", "ssr"]
}

target "_image" {
  platforms = ["linux/arm64"]
  labels = {
    "org.opencontainers.image.source" = "https://github.com/Th3FenrisWolf/Kitchen-Command-Center"
  }
  attest = ["type=provenance,disabled=true", "type=sbom,disabled=true"]
}

target "_site" {
  inherits = ["_image"]
  context  = "."
  secret   = ["id=fontawesome,env=FONTAWESOME_NPM_AUTH_TOKEN"]
}

target "app" {
  inherits = ["_site"]
  target   = "app"
  tags     = ["${KCC_REGISTRY}/kcc-app:${KCC_IMAGE_TAG}"]
}

target "ssr" {
  inherits = ["_site"]
  target   = "ssr"
  tags     = ["${KCC_REGISTRY}/kcc-ssr:${KCC_IMAGE_TAG}"]
}
```

- `KCC_REGISTRY` and `KCC_IMAGE_TAG` are the variables `compose.yaml` reads too, so one environment drives the build
  and the stack.
- The `org.opencontainers.image.source` label links each GHCR package to the repository, which lets the workflow's
  token push to it and prune it.
- Bake adds provenance by default, in `max` mode for a public repository. `attest` turns it and the SBOM off, so each
  tag is one image manifest and the package lists no `unknown/unknown` platforms.

- [ ] **Step 7: Build the images**

```bash
docker buildx bake --load
docker images --format '{{.Repository}}:{{.Tag}} {{.Size}}' | grep ':local'
```

Expected: both targets build, the first time in several minutes. The listing shows
`ghcr.io/th3fenriswolf/kcc-app:local` at about 720 MB and `kcc-ssr:local` at about 380 MB (the probe's sizes). An
`Error: Failed to replace env in config: ${FONTAWESOME_NPM_AUTH_TOKEN}` means the token is not in this shell.

- [ ] **Step 8: Look inside them**

```bash
docker run --rm --entrypoint sh ghcr.io/th3fenriswolf/kcc-app:local -c \
  'id -u; ls wwwroot/.vite/manifest.json appsettings.Production.json uSync/v17/usync.config; ls -d Views/Partials; ls wwwroot/App_Plugins; ls wwwroot/ssr'
docker run --rm --entrypoint sh ghcr.io/th3fenriswolf/kcc-ssr:local -c \
  'id -u; ls node_modules | wc -l; ls wwwroot/ssr; ls node_modules/@fortawesome'
```

Expected:
- From the app: `1654`; the three files; `Views/Partials`; an `App_Plugins` listing that includes `KCC.Admin` and
  `KCC.Contributions` beside Umbraco's and uSync's; and `No such file or directory` for `wwwroot/ssr`.
- From ssr: `1000`; about 81 packages; `Server.Entry.js`; and `No such file or directory` for `@fortawesome`.

Neither image runs usefully on its own yet: the app needs its settings and volumes, which Task 4's stack supplies.

- [ ] **Step 9: Commit**

```bash
git add Dockerfile .dockerignore docker-bake.hcl src/KCC.Web/package.json \
  tests/KCC.ViteTests/Features/Ssr/ssrRuntimeDependencies.test.ts
git commit -m "Build the Site Images"
```

---

### Task 4: The stack, and its smoke test

`deploy/compose.yaml` is the one compose file for the Pi or a VPS (spec §13.2). It runs:
- `app` and `ssr` with read-only root file systems, `init: true`, health checks, and restart unless stopped;
- `cloudflared` at a fixed address on the `edge` network;
- the data and media volumes.

ssr sits on the `render` network, which has no route out. No port is published.

`deploy/local.yaml` swaps cloudflared for Caddy on `https://localhost:8443`, forwarding the way the tunnel does, so the
same file runs on a developer's machine: the smoke test here, the restore drill in Task 11.

The first boot's administrator comes from `first-boot.env`, which is deleted afterwards. That is how production turns
the install off without ever passing an empty setting: compose would pass an unset variable as an empty string, and
Umbraco refuses to boot on an empty `UnattendedUserEmail`.

**Files:**
- Create: `deploy/smoke-test.sh`, `deploy/compose.yaml`, `deploy/local.yaml`, `deploy/local/Caddyfile`
- Create: `deploy/.env.example`, `deploy/first-boot.env.example`, `deploy/kcc`
- Modify: `.gitignore`

**Interfaces:**
- Consumes: the images from Task 3; `Hosting:TunnelAddress` from Task 1; `InstallUnattended` false in Production from
  Task 2.
- Produces: the compose project `kcc`, with services `app`, `ssr` and `cloudflared`, volumes `data` and `media`, and
  networks `edge` (`172.30.9.0/24`) and `render` (internal). It reads `.env`: `KCC_HOST`, `KCC_IMAGING_HMAC_KEY`,
  `KCC_TUNNEL_TOKEN`, `ANTHROPIC_API_KEY`, `KCC_REGISTRY`, `KCC_IMAGE_TAG` and `KCC_FIRST_BOOT_ENV`. Task 5 adds the
  `backup` and `restore` jobs and the `snapshots` volume; Task 6's `deploy.sh` drives the stack.
- Produces: `deploy/smoke-test.sh`, run from anywhere, which reads `KCC_IMAGE_TAG` (default `local`) and
  `KCC_REGISTRY`, and prints one `ok - …` line per check. Task 5 extends it; Task 7's CI job runs it.

- [ ] **Step 1: Write the smoke test**

The checks, in the order a failure would matter:
- The stack comes up healthy.
- Home is server-rendered: `SsrHtmlContent` leaves `<div id="app">` empty when rendering falls back to the client.
- Every asset home links loads, which proves the manifest was published.
- The seeder and the baseline export answer 404: they exist only in Development and Testing (spec §12).
- The trust boundary holds: through the edge, the backoffice's authorize endpoint answers ID2029, taken as HTTPS; from
  another address on the edge network it answers ID2083. The second check borrows the app image for its `curl`.
- ssr has no route out.

Create `deploy/smoke-test.sh`, executable:

```sh
#!/bin/sh
# Boots the images the way the Pi runs them, with Caddy in the tunnel's place (local.yaml), then checks the site and the
# tunnel's trust boundary. CI runs it on every build of the images; locally, run it after `docker buildx bake --load`
# at the repository root.
set -eu
cd "$(dirname "$0")"

export KCC_IMAGE_TAG="${KCC_IMAGE_TAG:-local}"
work=$(mktemp -d)
cat >"$work/smoke.env" <<EOF
KCC_HOST=localhost
KCC_IMAGING_HMAC_KEY=$(head -c 64 /dev/urandom | base64 | tr -d '\n')
KCC_TUNNEL_TOKEN=unused
KCC_FIRST_BOOT_ENV=$work/first-boot.env
EOF
cat >"$work/first-boot.env" <<EOF
Umbraco__CMS__Unattended__InstallUnattended=true
Umbraco__CMS__Unattended__UnattendedUserName=Smoke Test
Umbraco__CMS__Unattended__UnattendedUserEmail=smoke@example.test
Umbraco__CMS__Unattended__UnattendedUserPassword=Smoke-Test-Passw0rd
EOF

smoke() { docker compose -p kcc-smoke --env-file "$work/smoke.env" -f compose.yaml -f local.yaml "$@"; }
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
    fi
    smoke down -v --remove-orphans >/dev/null 2>&1 || true
    rm -rf "$work"
    exit "$status"
}
trap cleanup EXIT

smoke up -d --wait --wait-timeout 600
pass "app, ssr and the local edge are healthy"

home=$(curl -fsSk https://localhost:8443/)
case "$home" in
    *'<div id="app"><'*) pass "home is server-rendered" ;;
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
```

```bash
chmod +x deploy/smoke-test.sh
```

- [ ] **Step 2: Run it to see it fail**

```bash
deploy/smoke-test.sh
```

Expected: it stops at once with `open …/deploy/compose.yaml: no such file or directory`, and exits non-zero.

- [ ] **Step 3: Write the compose file**

Create `deploy/compose.yaml`:

```yaml
# No service publishes a port: the tunnel is the only way in, which is what makes CF-Connecting-IP and the forwarded
# headers trustworthy. deploy/local.yaml, for runs on a developer's machine, publishes one on 127.0.0.1.
name: kcc

x-logging: &logging
  driver: local

services:
  app:
    image: ${KCC_REGISTRY:-ghcr.io/th3fenriswolf}/kcc-app:${KCC_IMAGE_TAG:-main}
    restart: unless-stopped
    # As PID 1, a crashed .NET process ignores its own SIGABRT, so without an init process the container hangs
    # instead of exiting and restarting.
    init: true
    read_only: true
    tmpfs:
      - /tmp
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      AllowedHosts: ${KCC_HOST:?set KCC_HOST in .env};localhost
      Umbraco__CMS__WebRouting__UmbracoApplicationUrl: https://${KCC_HOST}/
      Umbraco__CMS__Imaging__HMACSecretKey: ${KCC_IMAGING_HMAC_KEY:?set KCC_IMAGING_HMAC_KEY in .env}
      Anthropic__ApiKey: ${ANTHROPIC_API_KEY:-}
      # cloudflared's address below: forwarded headers are trusted from it alone.
      Hosting__TunnelAddress: 172.30.9.10
      VueSsr__BaseUrl: http://ssr:3001
    # The first boot's install settings (first-boot.env.example); absent afterwards, so a lost database stops at the
    # installer instead of becoming an empty site.
    env_file:
      - path: ${KCC_FIRST_BOOT_ENV:-first-boot.env}
        required: false
    volumes:
      - data:/app/umbraco/Data
      - media:/app/wwwroot/media
    networks:
      - edge
      - render
    healthcheck:
      test: ["CMD", "curl", "-fsS", "-o", "/dev/null", "http://localhost:8080/umbraco/api/health/ready"]
      interval: 30s
      timeout: 5s
      retries: 3
      start_period: 5m
      start_interval: 5s
    depends_on:
      ssr:
        condition: service_healthy
    logging: *logging

  ssr:
    image: ${KCC_REGISTRY:-ghcr.io/th3fenriswolf}/kcc-ssr:${KCC_IMAGE_TAG:-main}
    restart: unless-stopped
    init: true
    read_only: true
    networks:
      - render
    healthcheck:
      test:
        - CMD
        - node
        - -e
        - "fetch('http://127.0.0.1:3001/health').then((r) => process.exit(r.ok ? 0 : 1), () => process.exit(1))"
      interval: 30s
      timeout: 5s
      retries: 3
      start_period: 1m
      start_interval: 2s
    logging: *logging

  cloudflared:
    image: cloudflare/cloudflared:2026.9.3
    restart: unless-stopped
    read_only: true
    command: tunnel run
    environment:
      TUNNEL_TOKEN: ${KCC_TUNNEL_TOKEN:?set KCC_TUNNEL_TOKEN in .env}
      TUNNEL_METRICS: 127.0.0.1:2000
    networks:
      edge:
        ipv4_address: 172.30.9.10
    healthcheck:
      test: ["CMD", "cloudflared", "tunnel", "--metrics", "127.0.0.1:2000", "ready"]
      interval: 30s
      timeout: 5s
      retries: 3
      start_period: 1m
      start_interval: 5s
    depends_on:
      app:
        condition: service_healthy
    logging: *logging

volumes:
  data:
  media:

networks:
  edge:
    ipam:
      config:
        - subnet: 172.30.9.0/24
  render:
    internal: true
```

Notes for a reviewer:
- The app's health check calls Umbraco's own readiness probe, which answers 200 only at RuntimeLevel Run. So a site
  stuck at the installer, or failed to boot, shows as unhealthy.
- `start_period` covers a first boot on the Pi; `start_interval` checks every 5 seconds meanwhile, so a fast boot is
  seen at once.
- `AllowedHosts` includes `localhost` for the health check's own requests.
- `cloudflared` keeps the image's `cloudflared --no-autoupdate` entrypoint; `command: tunnel run` reads
  `TUNNEL_TOKEN`. `tunnel --metrics 127.0.0.1:2000 ready` exits 1 until the tunnel is connected.
- The `local` log driver rotates by default (5 files of 20 MB per container), where the default `json-file` driver
  never rotates and would fill the SSD.

- [ ] **Step 4: Write the local edge**

Create `deploy/local.yaml`:

```yaml
# Runs the stack on this machine without Cloudflare: Caddy takes the tunnel's address and terminates TLS on
# https://localhost:8443, forwarding the way the tunnel does. Used by smoke-test.sh and the restore drill.
services:
  app:
    environment:
      Umbraco__CMS__WebRouting__UmbracoApplicationUrl: https://localhost:8443/
  cloudflared:
    image: caddy:2.11.4
    command: caddy run --config /etc/caddy/Caddyfile
    environment: !reset {}
    healthcheck: !reset {}
    read_only: false
    volumes:
      - ./local/Caddyfile:/etc/caddy/Caddyfile:ro
    ports:
      - 127.0.0.1:8443:443
```

Create `deploy/local/Caddyfile`:

```text
{
	local_certs
	auto_https disable_redirects
}

https://localhost {
	tls internal
	reverse_proxy app:8080 {
		header_up CF-Connecting-IP {remote_host}
	}
}
```

Caddy answers at cloudflared's address, so the app trusts it as it would the tunnel. It sets `X-Forwarded-Proto` and
`X-Forwarded-For` itself, and `CF-Connecting-IP` from the line above. Its certificate comes from its own local
authority, so browsers warn once.

- [ ] **Step 5: Write the settings examples, the wrapper, and the ignore line**

Create `deploy/.env.example`:

```sh
# Copy to .env beside this file, readable by kcc alone, and fill in every value (docs/hosting/runbook.md says where
# each one comes from):
#   sudo -u kcc cp .env.example .env && sudo chmod 600 .env && sudo -u kcc nano .env
# Never commit .env: it holds the site's secrets.

# The public hostname without https://: the apex (example.com) or a subdomain (kitchen.example.com).
KCC_HOST=

# Signs image URLs, so nobody can ask for arbitrary resizes. Generate one: openssl rand -base64 64 | tr -d '\n'
KCC_IMAGING_HMAC_KEY=

# Cloudflare: Networking > Tunnels > kcc: the token in the tunnel's docker command.
KCC_TUNNEL_TOKEN=

# Optional: the recipe icon editor's Suggest with AI.
ANTHROPIC_API_KEY=

# To roll back, pin a commit's images by its full SHA instead of following main.
# KCC_IMAGE_TAG=
```

Create `deploy/first-boot.env.example`:

```sh
# First boot only (docs/hosting/runbook.md, section 7): copy to first-boot.env beside this file, readable by kcc
# alone, fill in the administrator, start the stack, and delete first-boot.env once the site is up. Without it,
# production never installs Umbraco, so a lost database stops at the installer instead of becoming an empty site.
Umbraco__CMS__Unattended__InstallUnattended=true
Umbraco__CMS__Unattended__UnattendedUserName=
Umbraco__CMS__Unattended__UnattendedUserEmail=
Umbraco__CMS__Unattended__UnattendedUserPassword=
```

Create `deploy/kcc`, executable, which the runbook links as `/usr/local/bin/kcc`:

```sh
#!/bin/sh
# docker compose for this stack from any directory: kcc ps, kcc logs -f app, kcc run --rm backup snapshot.
cd "$(dirname "$(readlink -f "$0")")" && exec docker compose "$@"
```

```bash
chmod +x deploy/kcc
```

In `.gitignore`, under `# dotenv files`, add `deploy/first-boot.env` after the `.env` line. The bare `.env` line
already ignores `deploy/.env`, but no rule matches `first-boot.env`. Check both:

```bash
touch deploy/.env deploy/first-boot.env
git check-ignore -v deploy/.env deploy/first-boot.env
rm deploy/.env deploy/first-boot.env
```

Expected: two lines, `.gitignore:7:.env` and `.gitignore:8:deploy/first-boot.env` (line numbers as on
`replatform-phase-3`).

- [ ] **Step 6: Check the compose file on its own**

```bash
cd deploy
KCC_HOST=example.com KCC_IMAGING_HMAC_KEY=x KCC_TUNNEL_TOKEN=x docker compose config --quiet
KCC_HOST=example.com KCC_IMAGING_HMAC_KEY=x docker compose config --quiet
cd ..
```

Expected: the first prints nothing. The second fails with `required variable KCC_TUNNEL_TOKEN is missing a value: set
KCC_TUNNEL_TOKEN in .env`.

- [ ] **Step 7: Run the smoke test to see it pass**

```bash
deploy/smoke-test.sh
```

Expected: eight `ok` lines, from `ok - app, ssr and the local edge are healthy` to `ok - ssr has no route out`, and
exit 0. In the probe the whole run took under 30 seconds. On a failure, it prints the last 60 log lines of app and ssr. It
always removes its containers and volumes.

- [ ] **Step 8: Commit**

```bash
git add deploy/smoke-test.sh deploy/compose.yaml deploy/local.yaml deploy/local/Caddyfile deploy/.env.example \
  deploy/first-boot.env.example deploy/kcc .gitignore
git commit -m "Run the Site in Compose Behind the Tunnel"
```

---

### Task 5: Backups, and restores into fresh volumes

Spec §13.5. Every night a one-off `backup` job does four things:
- copies the database with SQLite's online `.backup`, and checks the copy's integrity;
- adds the data-protection keys and the media, and streams the archive to the bucket with rclone;
- keeps 14 daily and 8 weekly archives, the weekly ones copied from Sunday's daily;
- pings healthchecks.io at the start, and again on success or failure.

The same image takes the pre-deploy snapshot Task 6 needs, restores an archive from the bucket, and rolls the database
back to a snapshot. Its volumes mount under `/srv/kcc`, on empty directories owned by the app's UID. An empty volume
takes the content and the owner of the directory it is first mounted on: the probe's first image, mounted on Alpine's
own `/media`, left the media volume full of `cdrom`, `floppy` and `usb`, and owned by root.

The folder is `deploy/kcc-backup/`: `.gitignore`'s Visual Studio rule `Backup*/`, with `core.ignorecase` on, would
silently ignore `deploy/backup/`.

**Files:**
- Modify: `deploy/smoke-test.sh`
- Create: `deploy/kcc-backup/Dockerfile`, `deploy/kcc-backup/kcc-backup`
- Modify: `docker-bake.hcl`, `deploy/compose.yaml`, `deploy/.env.example`

**Interfaces:**
- Consumes: the `data` and `media` volumes and the `edge` subnet from Task 4.
- Produces: image `${KCC_REGISTRY}/kcc-backup:${KCC_IMAGE_TAG}` (bake target `backup`, in the default group). Its
  entrypoint is `kcc-backup`, whose commands are `nightly` (the default), `snapshot`, `restore <daily/…|weekly/…|latest>`
  and `rollback <file>`. It reads `KCC_BACKUP_BUCKET`, `KCC_BACKUP_PING_URL` and an rclone remote named `bucket`, from
  `RCLONE_CONFIG_BUCKET_*`.
- Produces: the compose jobs `backup` (data read-write, media read-only) and `restore` (both read-write, entrypoint
  `kcc-backup restore`) in profile `jobs`, and the volume `snapshots`. Pre-deploy snapshots go to
  `/srv/kcc/snapshots/pre-deploy/Umbraco-<UTC time>.sqlite.db`, newest five kept. Task 6 calls
  `docker compose run --rm backup snapshot`; the runbook calls the rest.

- [ ] **Step 1: Extend the smoke test with a round trip**

After the site checks, the test now:
1. puts a marker file in the media volume;
2. runs the nightly backup into a bucket on rclone's local backend, then a snapshot;
3. restores the archive into a second project's brand-new volumes;
4. boots it with the install off, which reaches Run only on the restored database;
5. fetches the marker through it.

Replace `deploy/smoke-test.sh` with:

```sh
#!/bin/sh
# Boots the images the way the Pi runs them, with Caddy in the tunnel's place (local.yaml), then checks the site, the
# tunnel's trust boundary, and a backup restored into fresh volumes. CI runs it on every build of the images; locally,
# run it after `docker buildx bake --load` at the repository root.
set -eu
cd "$(dirname "$0")"

export KCC_IMAGE_TAG="${KCC_IMAGE_TAG:-local}"
work=$(mktemp -d)
cat >"$work/drill.env" <<EOF
KCC_HOST=localhost
KCC_IMAGING_HMAC_KEY=$(head -c 64 /dev/urandom | base64 | tr -d '\n')
KCC_TUNNEL_TOKEN=unused
EOF
cp "$work/drill.env" "$work/smoke.env"
echo "KCC_FIRST_BOOT_ENV=$work/first-boot.env" >>"$work/smoke.env"
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
trap cleanup EXIT

smoke up -d --wait --wait-timeout 600
pass "app, ssr and the local edge are healthy"

home=$(curl -fsSk https://localhost:8443/)
case "$home" in
    *'<div id="app"><'*) pass "home is server-rendered" ;;
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
```

- [ ] **Step 2: Run it to see it fail**

```bash
deploy/smoke-test.sh
```

Expected: the eight site checks pass, then it stops with `no such service: restore`.

- [ ] **Step 3: Write the backup image**

Create `deploy/kcc-backup/Dockerfile`:

```dockerfile
FROM alpine:3.24.2
# The volumes mount under /srv/kcc. An empty volume takes the content and owner of the directory it is mounted on, so
# these are empty and belong to the app's user (1654), who owns the files in them.
RUN apk add --no-cache sqlite rclone curl tar gzip \
    && addgroup -g 1654 kcc && adduser -D -u 1654 -G kcc kcc \
    && mkdir -p /srv/kcc/data /srv/kcc/media /srv/kcc/snapshots && chown -R kcc:kcc /srv/kcc
# The bucket is configured through RCLONE_CONFIG_BUCKET_* variables, so rclone needs no config file.
ENV RCLONE_CONFIG=/dev/null
COPY --chmod=755 kcc-backup /usr/local/bin/kcc-backup
USER kcc
ENTRYPOINT ["kcc-backup"]
CMD ["nightly"]
```

Create `deploy/kcc-backup/kcc-backup`, executable:

```sh
#!/bin/sh
# kcc-backup nightly | snapshot | restore <archive|latest> | rollback <snapshot>
set -eu

data=/srv/kcc/data
media=/srv/kcc/media
snapshots=/srv/kcc/snapshots
database="$data/Umbraco.sqlite.db"

log() {
    printf '%s %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$*"
}

fail() {
    log "$*"
    exit 1
}

bucket() {
    printf 'bucket:%s' "${KCC_BACKUP_BUCKET:?KCC_BACKUP_BUCKET is not set}"
}

# The online backup API copies a consistent image of a WAL database and keeps the WAL flag, which Umbraco sets only
# when it creates a database and its locking relies on. VACUUM INTO would write a rollback-journal file instead.
copy_database() {
    sqlite3 "$database" ".backup '$1'"
    [ "$(sqlite3 "$1" 'PRAGMA integrity_check;')" = ok ] || fail "the copy of the database failed its integrity check"
}

ping() {
    [ -n "${KCC_BACKUP_PING_URL:-}" ] || return 0
    curl -fsS -m 10 --retry 5 -o /dev/null "$KCC_BACKUP_PING_URL$1" || log "the ping to healthchecks.io failed"
}

nightly() {
    work=$(mktemp -d "$snapshots/nightly.XXXXXX")
    trap 'rm -rf "$work"; ping /fail' EXIT
    ping /start
    mkdir "$work/data"
    copy_database "$work/data/Umbraco.sqlite.db"
    cp -R "$data/keys" "$work/data/keys"
    name="kcc-$(date -u +%Y-%m-%d).tar.gz"
    tar -czf - -C "$work" data -C /srv/kcc media | rclone rcat "$(bucket)/daily/$name"
    if [ "$(date -u +%u)" = 7 ]; then
        rclone copyto "$(bucket)/daily/$name" "$(bucket)/weekly/$name"
    fi
    rclone delete "$(bucket)" --min-age 14d --include "/daily/**"
    rclone delete "$(bucket)" --min-age 56d --include "/weekly/**"
    rm -rf "$work"
    trap - EXIT
    log "uploaded daily/$name"
    ping ""
}

snapshot() {
    mkdir -p "$snapshots/pre-deploy"
    file="$snapshots/pre-deploy/Umbraco-$(date -u +%Y%m%dT%H%M%SZ).sqlite.db"
    copy_database "$file"
    find "$snapshots/pre-deploy" -name '*.sqlite.db' | sort -r | tail -n +6 | xargs -r rm -f
    log "saved $file"
}

replace_database() {
    rm -f "$database" "$database-wal" "$database-shm"
    cp "$1" "$database"
    rm -rf "$data/TEMP"
}

restore() {
    archive=${1:?name an archive such as daily/kcc-2026-10-01.tar.gz, or latest}
    if [ "$archive" = latest ]; then
        latest=$(rclone lsf "$(bucket)/daily" | sort | tail -n 1)
        [ -n "$latest" ] || fail "the bucket holds no daily backup"
        archive="daily/$latest"
    fi
    work=$(mktemp -d "$snapshots/restore.XXXXXX")
    trap 'rm -rf "$work"' EXIT
    rclone cat "$(bucket)/$archive" | tar -xzf - -C "$work"
    [ "$(sqlite3 "$work/data/Umbraco.sqlite.db" 'PRAGMA integrity_check;')" = ok ] || fail "$archive holds a damaged database"
    replace_database "$work/data/Umbraco.sqlite.db"
    rm -rf "$data/keys"
    cp -R "$work/data/keys" "$data/keys"
    find "$media" -mindepth 1 -delete
    cp -R "$work/media/." "$media/"
    log "restored $archive"
}

rollback() {
    file="$snapshots/pre-deploy/${1:?name a snapshot from: ls /srv/kcc/snapshots/pre-deploy}"
    [ -f "$file" ] || fail "$file does not exist"
    replace_database "$file"
    log "restored $file"
}

command=${1:-nightly}
[ $# -gt 0 ] && shift
case "$command" in
    nightly) nightly ;;
    snapshot) snapshot ;;
    restore) restore "$@" ;;
    rollback) rollback "$@" ;;
    *) fail "unknown command: $command" ;;
esac
```

```bash
chmod +x deploy/kcc-backup/kcc-backup
git check-ignore -v deploy/kcc-backup/kcc-backup || echo "not ignored"
```

Expected: `not ignored`.

Notes for a reviewer:
- `nightly` streams the archive through `rclone rcat`, so the media never needs a second copy on the Pi's disk. Its
  working copy of the database sits on the snapshots volume, not in `/tmp`, which is memory.
- The prune deletes from the bucket's root with `--include`: listing a prefix that does not exist yet is an error on
  rclone's local backend, where S3 returns nothing.
- `restore` deletes `TEMP`: its Examine indexes and caches belong to the old database, and Umbraco rebuilds them at the
  next start.
- The snapshot names carry a UTC timestamp, so sorting by name sorts by age.

- [ ] **Step 4: Build it with the others**

In `docker-bake.hcl`, set the default group to `targets = ["app", "ssr", "backup"]`, and add at the end:

```hcl
target "backup" {
  inherits = ["_image"]
  context  = "deploy/kcc-backup"
  tags     = ["${KCC_REGISTRY}/kcc-backup:${KCC_IMAGE_TAG}"]
}
```

It inherits `_image` and not `_site`: it needs neither the repository as its context nor the Font Awesome token.

```bash
docker buildx bake --load
docker run --rm --entrypoint sh ghcr.io/th3fenriswolf/kcc-backup:local -c \
  'id -u; sqlite3 --version; rclone version | head -n 1; ls -ln /srv/kcc'
```

Expected: the three images build, the site images from cache. Then `1654`; SQLite `3.53.4` and `rclone v1.74.1-DEV`
(Alpine 3.24.2's, in the probe); and `data`, `media` and `snapshots`, each owned by `1654 1654`.

- [ ] **Step 5: Add the jobs to the stack**

In `deploy/compose.yaml`, insert after the `cloudflared` service, before the top-level `volumes:`:

```yaml
  backup:
    image: ${KCC_REGISTRY:-ghcr.io/th3fenriswolf}/kcc-backup:${KCC_IMAGE_TAG:-main}
    profiles: [jobs]
    read_only: true
    tmpfs:
      - /tmp
    environment: &backup-environment
      RCLONE_CONFIG_BUCKET_TYPE: s3
      RCLONE_CONFIG_BUCKET_PROVIDER: Cloudflare
      RCLONE_CONFIG_BUCKET_ENDPOINT: ${KCC_BACKUP_ENDPOINT:-}
      RCLONE_CONFIG_BUCKET_ACCESS_KEY_ID: ${KCC_BACKUP_ACCESS_KEY_ID:-}
      RCLONE_CONFIG_BUCKET_SECRET_ACCESS_KEY: ${KCC_BACKUP_SECRET_ACCESS_KEY:-}
      RCLONE_CONFIG_BUCKET_ACL: private
      RCLONE_CONFIG_BUCKET_NO_CHECK_BUCKET: "true"
      KCC_BACKUP_BUCKET: ${KCC_BACKUP_BUCKET:-}
      KCC_BACKUP_PING_URL: ${KCC_BACKUP_PING_URL:-}
    # Data is read-write because SQLite cannot open a WAL database read-only once the app has closed it.
    volumes:
      - data:/srv/kcc/data
      - media:/srv/kcc/media:ro
      - snapshots:/srv/kcc/snapshots
    logging: *logging

  restore:
    image: ${KCC_REGISTRY:-ghcr.io/th3fenriswolf}/kcc-backup:${KCC_IMAGE_TAG:-main}
    profiles: [jobs]
    entrypoint: ["kcc-backup", "restore"]
    read_only: true
    tmpfs:
      - /tmp
    environment: *backup-environment
    volumes:
      - data:/srv/kcc/data
      - media:/srv/kcc/media
      - snapshots:/srv/kcc/snapshots
    logging: *logging
```

and add `snapshots:` under `volumes:`, after `media:`.

In `deploy/.env.example`, insert before the `# To roll back` comment:

```sh
# The backup bucket. For Cloudflare R2 the endpoint is https://<account id>.r2.cloudflarestorage.com.
KCC_BACKUP_BUCKET=kcc-backups
KCC_BACKUP_ENDPOINT=
KCC_BACKUP_ACCESS_KEY_ID=
KCC_BACKUP_SECRET_ACCESS_KEY=

# healthchecks.io: the kcc-backup check's ping URL.
KCC_BACKUP_PING_URL=
```

Leave a blank line after it. The bucket's variables all default to empty, so `docker compose up` needs none of them;
only the jobs read them.

- [ ] **Step 6: Run the smoke test to see it pass**

```bash
deploy/smoke-test.sh
```

Expected: eleven `ok` lines, ending with `ok - the restored database boots with the install off` and
`ok - the restored media is served`, and exit 0. In the probe it took 26 seconds.

- [ ] **Step 7: Commit**

```bash
git add deploy/smoke-test.sh deploy/kcc-backup docker-bake.hcl deploy/compose.yaml deploy/.env.example
git commit -m "Back Up to a Bucket and Restore into Fresh Volumes"
```

---

### Task 6: Deploy by pull

Spec §13.4: "Deploy is pull-based. A systemd timer on the Pi checks for a new `main` digest every 5 minutes with a
read-only GHCR token. On a change it takes a database `.backup` snapshot, pulls, and restarts the stack … CI never gets
a path into the home network."

On the Pi, `/srv/kcc/repo` is a sparse clone of the public repository with only `deploy/` checked out (runbook §7).
Each run of `deploy/deploy.sh`:
- pulls `deploy/` from git, so a change to `compose.yaml` or a script arrives the way an image does;
- pulls the three GHCR images;
- compares each running container's image with the image its tag now names;
- when anything changed, snapshots the database and runs `docker compose up -d --wait`.

`flock` keeps a deploy, a backup and a restore from ever overlapping.

**Files:**
- Create: `deploy/deploy.sh`
- Create: `deploy/systemd/kcc-deploy.service`, `kcc-deploy.timer`, `kcc-backup.service`, `kcc-backup.timer`
- Create, gitignored: `.superpowers/phase-7/deploy-test.sh`

**Interfaces:**
- Consumes: the compose project from Tasks 4 and 5 (`docker compose run --rm backup snapshot`, `… backup nightly`),
  the `kcc` wrapper, and the images' `main` tag, which Task 7 pushes.
- Produces: units that run as the user `kcc` from `/srv/kcc/repo/deploy`, with the lock file `/srv/kcc/kcc.lock`.
  The runbook installs them into `/etc/systemd/system`.

- [ ] **Step 1: Write the deploy test**

It runs `deploy.sh` three times against a throwaway registry on `localhost:5001`, standing in for GHCR, from a copy of
`deploy/` that is not a git checkout, so the git step is skipped:
1. with nothing running, it must bring the stack up;
2. with nothing changed, it must print nothing;
3. after a new `kcc-app:main` appears, it must snapshot the database and recreate the app alone.

Create `.superpowers/phase-7/deploy-test.sh`. `.superpowers/` is gitignored, and the script is never committed:

```sh
#!/bin/sh
# Task 6's check of deploy.sh against a throwaway registry on localhost:5001, run from a checkout's root after
# `KCC_IMAGE_TAG=<tag> docker buildx bake --load`. Usage: deploy-test.sh <tag of the images just built>
set -eu
built=${1:?the tag of the images to push}
unset KCC_IMAGE_TAG KCC_REGISTRY

docker rm -f kcc-test-registry >/dev/null 2>&1 || true
docker run -d --name kcc-test-registry -p 127.0.0.1:5001:5000 registry:2 >/dev/null
sleep 2
for n in app ssr backup; do
    docker tag "ghcr.io/th3fenriswolf/kcc-$n:$built" "localhost:5001/kcc-$n:main"
    docker push -q "localhost:5001/kcc-$n:main" >/dev/null
done

t=$(mktemp -d)
cp -R deploy/. "$t/"
cat >"$t/.env" <<EOF
KCC_HOST=localhost
KCC_IMAGING_HMAC_KEY=$(head -c 64 /dev/urandom | base64 | tr -d '\n')
KCC_TUNNEL_TOKEN=unused
KCC_REGISTRY=localhost:5001
COMPOSE_FILE=compose.yaml:local.yaml
COMPOSE_PROJECT_NAME=kcc-deploytest
EOF
sed -e 's/UserName=$/UserName=Deploy Test/' -e 's/UserEmail=$/UserEmail=deploy@example.test/' \
    -e 's/UserPassword=$/UserPassword=Deploy-Test-Passw0rd/' deploy/first-boot.env.example >"$t/first-boot.env"
cd "$t"
trap 'docker compose --profile jobs down -v >/dev/null 2>&1; docker rm -f kcc-test-registry >/dev/null 2>&1; rm -rf "$t"' EXIT

echo "== run 1: nothing running"
sh deploy.sh
rm first-boot.env

echo "== run 2: nothing changed"
start=$(date +%s)
out=$(sh deploy.sh)
[ -z "$out" ] || { echo "run 2 printed: $out"; exit 1; }
echo "run 2 took $(($(date +%s) - start))s and printed nothing"

ssr_before=$(docker compose ps -q ssr)
app_before=$(docker compose ps -q app)
printf 'FROM localhost:5001/kcc-app:main\nLABEL deploy-test.change=1\n' | docker build -q -t localhost:5001/kcc-app:main - >/dev/null
docker push -q localhost:5001/kcc-app:main >/dev/null
docker tag "ghcr.io/th3fenriswolf/kcc-app:$built" localhost:5001/kcc-app:main

echo "== run 3: a new app image"
sh deploy.sh
[ "$(docker compose ps -q ssr)" = "$ssr_before" ] || { echo "ssr was recreated"; exit 1; }
[ "$(docker compose ps -q app)" != "$app_before" ] || { echo "app was not recreated"; exit 1; }
docker compose run --rm --entrypoint ls backup /srv/kcc/snapshots/pre-deploy
curl -fsSk -o /dev/null https://localhost:8443/
echo "app recreated, ssr kept, snapshot taken, site answers"
```

- [ ] **Step 2: Run it to see it fail**

```bash
docker buildx bake --load
sh .superpowers/phase-7/deploy-test.sh local
```

Expected: `== run 1: nothing running`, then `deploy.sh: No such file or directory`, and a non-zero exit. The trap
removes the registry and the copy either way.

- [ ] **Step 3: Write the deploy script**

Create `deploy/deploy.sh`, executable:

```sh
#!/bin/sh
# Run every five minutes by kcc-deploy.timer. Takes in main's deploy files and images, snapshots the database, and
# restarts only what changed.
set -eu
cd "$(dirname "$0")"

changed=false

# The Pi's sparse clone moves with every commit to main; only a change to this folder calls for a deploy.
if [ -d ../.git ]; then
    before=$(git rev-parse HEAD)
    git pull --ff-only --quiet
    git diff --quiet "$before" HEAD -- . || changed=true
fi

# Only the GHCR images: cloudflared's tag is pinned in compose.yaml, and Docker Hub rate-limits anonymous pulls.
docker compose --progress quiet pull app ssr backup

containers=$(docker compose ps -q)
[ -n "$containers" ] || changed=true
for container in $containers; do
    tag=$(docker inspect -f '{{.Config.Image}}' "$container")
    [ "$(docker inspect -f '{{.Image}}' "$container")" = "$(docker image inspect -f '{{.Id}}' "$tag")" ] || changed=true
done

[ "$changed" = true ] || exit 0

if [ -n "$(docker compose ps -q app)" ]; then
    docker compose run --rm backup snapshot
fi

docker compose up -d --remove-orphans --wait --wait-timeout 600
docker image prune -f >/dev/null
echo "deployed $(docker inspect -f '{{.Config.Image}} {{.Image}}' "$(docker compose ps -q app)")"
```

```bash
chmod +x deploy/deploy.sh
```

Notes for a reviewer:
- The Pi's sparse clone moves with every commit to `main`, so `git diff --quiet "$before" HEAD -- .` limits a deploy
  to changes in `deploy/`. The probe checked it in a sparse clone: a commit to `docs/` changed nothing, and a commit to
  `deploy/` did.
- `docker compose config --images app` would also print the images of the services `app` depends on. So the script
  reads each running container's own tag (`.Config.Image`) and compares the container's image with the one that tag
  names now.
- An edit to `.env` alone, such as pinning `KCC_IMAGE_TAG` for a rollback, changes no image the script watches. The
  runbook's rollback runs `kcc up -d` itself.
- A container stopped on purpose stays stopped until something changes. The runbook pauses the timer for maintenance,
  so a deploy cannot start the app in the middle of a restore.

- [ ] **Step 4: Run the deploy test to see it pass**

```bash
sh .superpowers/phase-7/deploy-test.sh local
```

Expected, in the probe:

```text
== run 1: nothing running
deployed localhost:5001/kcc-app:main sha256:…
== run 2: nothing changed
run 2 took 1s and printed nothing
== run 3: a new app image
deployed localhost:5001/kcc-app:main sha256:…
Umbraco-<UTC time>.sqlite.db
app recreated, ssr kept, snapshot taken, site answers
```

Compose's own progress lines appear between them.

- [ ] **Step 5: Write the timers**

Create `deploy/systemd/kcc-deploy.service`:

```ini
[Unit]
Description=Deploy Kitchen Command Center from main
Wants=network-online.target
After=network-online.target docker.service

[Service]
Type=oneshot
User=kcc
ExecStart=/usr/bin/flock /srv/kcc/kcc.lock /srv/kcc/repo/deploy/deploy.sh
TimeoutStartSec=20min
```

Create `deploy/systemd/kcc-deploy.timer`:

```ini
[Unit]
Description=Check main for a new Kitchen Command Center build every five minutes

[Timer]
OnBootSec=2min
OnUnitActiveSec=5min

[Install]
WantedBy=timers.target
```

Create `deploy/systemd/kcc-backup.service`:

```ini
[Unit]
Description=Back up Kitchen Command Center to the bucket
Wants=network-online.target
After=network-online.target docker.service

[Service]
Type=oneshot
User=kcc
ExecStart=/usr/bin/flock /srv/kcc/kcc.lock /srv/kcc/repo/deploy/kcc run --rm backup nightly
TimeoutStartSec=2h
```

Create `deploy/systemd/kcc-backup.timer`:

```ini
[Unit]
Description=Back up Kitchen Command Center nightly

[Timer]
OnCalendar=*-*-* 03:00:00
RandomizedDelaySec=15min
Persistent=true

[Install]
WantedBy=timers.target
```

`Persistent=true` runs a backup missed while the Pi was off at the next boot. The random delay spreads a bucket
provider's load, and healthchecks.io's three-hour grace (runbook §5) covers it. The backup waits for any deploy holding
the lock, and the other way round.

- [ ] **Step 6: Check the units and the shell**

```bash
docker run --rm -v "$PWD/deploy/systemd:/units:ro" debian:trixie sh -c '
  apt-get update -qq >/dev/null && apt-get install -y -qq systemd util-linux >/dev/null 2>&1
  useradd --system kcc && mkdir -p /srv/kcc/repo/deploy
  printf "#!/bin/sh\n" >/srv/kcc/repo/deploy/deploy.sh && cp /srv/kcc/repo/deploy/deploy.sh /srv/kcc/repo/deploy/kcc
  chmod +x /srv/kcc/repo/deploy/* && cp /units/* /etc/systemd/system/ && cd /etc/systemd/system
  systemd-analyze verify kcc-deploy.service kcc-deploy.timer kcc-backup.service kcc-backup.timer && echo verified'
docker run --rm -v "$PWD:/repo" --workdir /repo --entrypoint shellcheck rhysd/actionlint:latest -s sh \
  deploy/smoke-test.sh deploy/deploy.sh deploy/kcc deploy/kcc-backup/kcc-backup
```

Expected: `verified` (the stubs stand in for the scripts the units run), and no output from shellcheck.

- [ ] **Step 7: Commit**

```bash
git add deploy/deploy.sh deploy/systemd
git commit -m "Deploy by Pull from GHCR"
```

---

### Task 7: Build, test and push the images in CI; keep dependencies current

Spec §13.4 and §14: images build on GitHub's `ubuntu-24.04-arm` runners, which are free for a public repository, and
go to private GHCR packages tagged with the commit and `main`. A new `images` job in the existing workflow:
- runs after `build-and-test`, so an image is never pushed from a commit whose tests failed: the Pi deploys `main`
  within five minutes;
- builds the three images and runs the smoke test on every pull request and push;
- pushes and prunes only on a push to `main`.

It pushes the very images the smoke test ran. GHCR stores and serves container images for free; pruning keeps ten
versions per package, enough to roll back.

Spec §13.6 adds Dependabot for NuGet, npm, Docker and Actions. New majors are held back where they are deliberate
work: .NET on 10, Node on 24 (whose successor's images drop Yarn 1), and Umbraco on 17.

**Files:**
- Modify: `.github/workflows/build-and-test.yml`
- Create: `.github/dependabot.yml`

**Interfaces:**
- Consumes: `docker-bake.hcl` (Tasks 3 and 5) and `deploy/smoke-test.sh` (Task 5); the repository secret
  `FONTAWESOME_NPM_AUTH_TOKEN`.
- Produces: `ghcr.io/th3fenriswolf/kcc-app`, `kcc-ssr` and `kcc-backup`, each tagged with the commit's full SHA and
  `main`. The Pi's `deploy.sh` pulls `main` unless `.env` pins `KCC_IMAGE_TAG` to a SHA.

- [ ] **Step 1: Add the images job**

Append to `.github/workflows/build-and-test.yml`, after the `build-and-test` job (the same indentation, under `jobs:`),
with a blank line before it:

```yaml
  images:
    needs: build-and-test
    runs-on: ubuntu-24.04-arm
    timeout-minutes: 40
    permissions:
      contents: read
      packages: write

    env:
      FONTAWESOME_NPM_AUTH_TOKEN: ${{ secrets.FONTAWESOME_NPM_AUTH_TOKEN }}
      KCC_IMAGE_TAG: ${{ github.sha }}
      PUBLISH: ${{ github.event_name == 'push' && github.ref == 'refs/heads/main' }}

    steps:
      - name: Checkout
        uses: actions/checkout@v7

      - name: Set up Buildx
        uses: docker/setup-buildx-action@v4

      # One bake builds the shared frontend stage once for both site images. Each target keeps its own cache
      # scope, because targets sharing one would overwrite each other's.
      - name: Build the images
        uses: docker/bake-action@v7
        with:
          source: .
          load: true
          set: |
            app.cache-from=type=gha,scope=app
            app.cache-to=type=gha,scope=app,mode=max
            ssr.cache-from=type=gha,scope=ssr
            ssr.cache-to=type=gha,scope=ssr,mode=max
            backup.cache-from=type=gha,scope=backup
            backup.cache-to=type=gha,scope=backup,mode=max

      - name: Smoke-test the images
        run: deploy/smoke-test.sh

      - name: Log in to the GitHub Container Registry
        if: env.PUBLISH == 'true'
        uses: docker/login-action@v4
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      # The images pushed are the ones the smoke test just ran, tagged with the commit and as main, which the Pi
      # deploys.
      - name: Push the images
        if: env.PUBLISH == 'true'
        run: |
          for name in app ssr backup; do
            image="ghcr.io/th3fenriswolf/kcc-$name"
            docker tag "$image:$KCC_IMAGE_TAG" "$image:main"
            docker push "$image:$KCC_IMAGE_TAG"
            docker push "$image:main"
          done

      - name: Prune old app images
        if: env.PUBLISH == 'true'
        uses: actions/delete-package-versions@v5
        with:
          package-name: kcc-app
          package-type: container
          min-versions-to-keep: 10

      - name: Prune old SSR images
        if: env.PUBLISH == 'true'
        uses: actions/delete-package-versions@v5
        with:
          package-name: kcc-ssr
          package-type: container
          min-versions-to-keep: 10

      - name: Prune old backup images
        if: env.PUBLISH == 'true'
        uses: actions/delete-package-versions@v5
        with:
          package-name: kcc-backup
          package-type: container
          min-versions-to-keep: 10
```

Notes for a reviewer:
- `source: .` builds from the checkout, so `.dockerignore` applies as it does locally. Since `docker/bake-action` v6
  the default is a Git context.
- `load: true` puts the images into the runner's Docker, where the smoke test runs them and `docker push` sends them.
- `PUBLISH` is the string `'true'` or `'false'`.
- The workflow's `concurrency` group cancels a run for `main` when a newer push arrives, and the newer run pushes
  instead. A tag moves only when a push completes.

- [ ] **Step 2: Lint it**

```bash
docker run --rm -v "$PWD:/repo" --workdir /repo rhysd/actionlint:latest -no-color
```

Expected: no output, exit 0. actionlint also runs shellcheck on each `run:` script.

- [ ] **Step 3: Add Dependabot**

Create `.github/dependabot.yml`:

```yaml
version: 2

registries:
  fontawesome:
    type: npm-registry
    url: https://npm.fontawesome.com
    token: ${{secrets.FONTAWESOME_NPM_AUTH_TOKEN}}
    scope: "@fortawesome"

updates:
  # A new major is deliberate work: .NET and Microsoft's packages stay on 10, Umbraco on its 17 LTS until the move to
  # 21 (spec §6.1), and uSync's majors follow Umbraco's.
  - package-ecosystem: nuget
    directory: /
    schedule:
      interval: weekly
    ignore:
      - dependency-name: "*"
        update-types: ["version-update:semver-major"]
    groups:
      umbraco:
        patterns: ["Umbraco.*", "uSync*"]
      nuget:
        patterns: ["*"]

  - package-ecosystem: npm
    directory: /
    registries:
      - fontawesome
    schedule:
      interval: weekly
    ignore:
      - dependency-name: "*"
        update-types: ["version-update:semver-major"]
    groups:
      npm:
        patterns: ["*"]

  # Node stays on 24: its images from 26 on drop Yarn 1, which the build uses.
  - package-ecosystem: docker
    directories:
      - /
      - /deploy/kcc-backup
    schedule:
      interval: weekly
    ignore:
      - dependency-name: "dotnet/*"
        update-types: ["version-update:semver-major"]
      - dependency-name: "node"
        update-types: ["version-update:semver-major"]

  - package-ecosystem: docker-compose
    directory: /deploy
    schedule:
      interval: weekly

  - package-ecosystem: github-actions
    directory: /
    schedule:
      interval: weekly
```

Notes for a reviewer:
- A dependency belongs to the first group whose pattern it matches, so Umbraco and uSync update together and apart from
  the rest.
- The `docker` entry covers both Dockerfiles, and `docker-compose` covers cloudflared's and Caddy's tags.
  cloudflared's versions are dates, so a new year is a new major: it is deliberately not held back, because Cloudflare
  supports a cloudflared release for a year.
- Workflows that Dependabot's pull requests trigger read Dependabot's secrets, not the repository's Actions secrets.
  The owner adds a copy of `FONTAWESOME_NPM_AUTH_TOKEN` there too (runbook §6), for the registry entry and for CI.

```bash
python3 -c "import yaml; yaml.safe_load(open('.github/dependabot.yml')); print('parses')"
```

Expected: `parses`. GitHub validates the file itself when it reaches `main`, and shows errors under the repository's
Insights → Dependency graph → Dependabot.

- [ ] **Step 4: Commit**

```bash
git add .github/workflows/build-and-test.yml
git commit -m "Build, Smoke-Test and Push the Images in CI"
git add .github/dependabot.yml
git commit -m "Keep Dependencies Current with Dependabot"
```

---

### Task 8: The runbook, the docs, and the pull request

The runbook is what the owner follows in Tasks 9 to 11, and what keeps the site running afterwards. Spec §13.3 names
"the Pi runbook" as a deliverable, and §12 asks it to carry the commands for production data down to development.
Every procedure in it that can run on a laptop ran in the probe: the rollback, the download of production data, and
the restore drill's mechanism, which the smoke test repeats.

**Files:**
- Create: `docs/hosting/runbook.md`
- Modify: `README.md`
- Modify: `docs/replatform/specs/2026-09-21-replatform-off-xperience.md`
- Create, gitignored: `.superpowers/phase-7/pr-body.md`

- [ ] **Step 1: Write the runbook**

Create `docs/hosting/runbook.md`:

````markdown
# Hosting runbook

Kitchen Command Center runs on a Raspberry Pi 5 behind a Cloudflare Tunnel. This runbook sets it up from nothing and
keeps it running. The design and its reasons are in `docs/replatform/specs/2026-09-21-replatform-off-xperience.md`,
§13.

## What runs where

- **The Pi** runs Docker Compose from `/srv/kcc/repo/deploy`, a sparse clone of this repository. Three services run
  all the time:
  - `app`, Umbraco and the site, on port 8080 inside Docker's networks only;
  - `ssr`, the Node service that renders Vue on the server, reachable from `app` alone, with no route out;
  - `cloudflared`, the tunnel, which connects out to Cloudflare, so no port on the router or the Pi is open.
- **Two timers** run jobs as the user `kcc`:
  - `kcc-deploy`, every five minutes, pulls `deploy/` from git and the images from GHCR. If anything changed, it takes
    a snapshot of the database, then restarts what changed.
  - `kcc-backup`, at 03:00, sends the database, its keys and the media to the backup bucket, then pings
    healthchecks.io.
- **GitHub Actions** builds the images on every push to `main`, tests them, and pushes them to private packages on
  GHCR.
- **Cloudflare** holds the domain's DNS. Cloudflare Access asks for a one-time PIN before anyone reaches the site:
  until launch for the whole site, and always for `/umbraco`. R2 holds the backups.

Commands below that start with `sudo -u kcc -H kcc` run Docker Compose for the stack, as `kcc`, from any folder.

## 1. What you need

- **Accounts:**
  - Cloudflare, on the Free plan. Zero Trust and R2 ask for a payment method but charge nothing within their free
    tiers.
  - healthchecks.io, on the free plan.
  - Tailscale, on the free plan.
  - GitHub, with admin rights on this repository.
- **Hardware:**
  - a Raspberry Pi 5: 4 GB is enough, 8 GB comfortable;
  - the official 27 W USB-C power supply, since a USB SSD needs the full 5 A;
  - the active cooler and a case;
  - an M.2 HAT+ with an NVMe SSD, or a USB 3 SSD. Not a microSD card: the database and the indexes would wear it out.
  - an Ethernet cable, and an adapter to flash the SSD from your computer.
- **Choose the hostname.** Use the apex (`example.com`) if the domain serves nothing else. If the Squarespace site stays
  on the apex, use a subdomain such as `kitchen.example.com`. It is `KCC_HOST` from here on.

## 2. Move the domain's DNS to Cloudflare

Start this first: the switch can take up to two days, and nothing else waits for it until section 4.

1. In Cloudflare, **Add a domain**, enter the domain, and pick the **Free** plan. Cloudflare scans for common records.
2. Compare its list with Squarespace's DNS page (Domains → the domain → DNS), and add whatever the scan missed:
   - every email record: `MX`, the SPF `TXT` on `@`, the DKIM records (`TXT` or `CNAME`, under custom names the scan
     misses), and the DMARC `TXT` on `_dmarc`;
   - if the Squarespace site stays, its records, set to **DNS only** (grey cloud): four `A` records on `@`
     (`198.185.159.144`, `198.185.159.145`, `198.49.23.144`, `198.49.23.145`), the `CNAME` `www` →
     `ext-cust.squarespace.com`, and the site's own verification `CNAME` → `verify.squarespace.com`.
3. At Squarespace: Domains → the domain → DNS → **Domain Nameservers** → **Use Custom Nameservers**. Squarespace asks
   to turn DNSSEC off; continue. Enter the two nameservers Cloudflare gave you, and save.
4. Wait for Cloudflare's email that the domain is active. Check that mail still arrives and the Squarespace site, if
   kept, still loads.
5. In Cloudflare, SSL/TLS → **Edge Certificates**: turn **Always Use HTTPS** on, and set **Minimum TLS Version** to 1.2.
   Leave HSTS off here: the app sends it.
6. Optional: DNS → Settings → enable **DNSSEC**, then add the DS record Cloudflare shows at Squarespace (DNS →
   DNSSEC).

## 3. Prepare the Pi

1. **Flash the SSD** from your computer with Raspberry Pi Imager, through the adapter. Choose Raspberry Pi 5, then
   Raspberry Pi OS **Lite (64-bit)**. In its customisation:
   - set the hostname `kcc`, your username and your time zone;
   - allow SSH with **public-key authentication only**, and paste your public key;
   - leave Wi-Fi off.
2. **Fit the SSD** and boot without a microSD card. A current Pi 5 boots from NVMe or USB by itself. If yours does not,
   boot once from a microSD with Raspberry Pi OS, run `sudo rpi-eeprom-update -a`, then choose `sudo raspi-config` →
   Advanced Options → Boot Order → **NVMe/USB Boot**.
3. **Update and keep updating:**

   ```bash
   ssh <you>@kcc.local
   sudo apt update && sudo apt full-upgrade -y
   sudo apt install -y unattended-upgrades git ufw
   sudo dpkg-reconfigure -plow unattended-upgrades
   sudo reboot
   ```

4. **Close SSH to passwords.** Create `/etc/ssh/sshd_config.d/10-kcc.conf` with `sudo nano`:

   ```text
   PasswordAuthentication no
   KbdInteractiveAuthentication no
   PermitRootLogin no
   ```

   Run `sudo systemctl reload ssh`, then open a second SSH session to check the key still works before you close the
   first.
5. **Tailscale,** for SSH from anywhere:

   ```bash
   curl -fsSL https://tailscale.com/install.sh | sh
   sudo tailscale up
   ```

   Open the link it prints, and approve the Pi in your tailnet. From then on `ssh <you>@kcc` works over the tailnet.
6. **The firewall.** Allow SSH from the tailnet and your LAN only, and replace `192.168.1.0/24` with your LAN's range:

   ```bash
   sudo ufw default deny incoming
   sudo ufw default allow outgoing
   sudo ufw allow in on tailscale0
   sudo ufw allow from 192.168.1.0/24 to any port 22 proto tcp
   sudo ufw enable
   ```

   Ports that Docker publishes bypass these rules. The stack publishes none, and must never publish one: the site
   trusts the tunnel's headers only because the tunnel is the only way in.
7. **Docker Engine,** from Docker's Debian repository (Docker's page for Raspberry Pi OS covers 32-bit only):

   ```bash
   sudo install -m 0755 -d /etc/apt/keyrings
   sudo curl -fsSL https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc
   sudo chmod a+r /etc/apt/keyrings/docker.asc
   printf 'Types: deb\nURIs: https://download.docker.com/linux/debian\nSuites: %s\nComponents: stable\nSigned-By: /etc/apt/keyrings/docker.asc\n' \
     "$(. /etc/os-release && echo "$VERSION_CODENAME")" | sudo tee /etc/apt/sources.list.d/docker.sources
   sudo apt update
   sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
   docker compose version
   ```

   Compose must be 2.24 or later.
8. **The stack's user and folder:**

   ```bash
   sudo useradd --system --create-home --home-dir /srv/kcc --shell /usr/sbin/nologin --groups docker kcc
   sudo -u kcc git clone --filter=blob:none --sparse https://github.com/Th3FenrisWolf/Kitchen-Command-Center.git /srv/kcc/repo
   sudo -u kcc git -C /srv/kcc/repo sparse-checkout set deploy
   sudo ln -s /srv/kcc/repo/deploy/kcc /usr/local/bin/kcc
   ```

## 4. The tunnel and Access

Once the domain is active in Cloudflare:

1. Open **Zero Trust**. Pick a team name, and the **Free** plan (it asks for a payment method and charges nothing).
2. **One-time PIN login.** Zero Trust → Integrations → Identity providers → **Add new** → **One-time PIN**. In older
   dashboards: Settings → Authentication.
3. **The tunnel.** Networking → Tunnels → **Create Tunnel**:
   - name it `kcc`, and choose **Docker**;
   - copy the token, the long string after `--token` in the command shown (do not run the command);
   - open the tunnel → Routes → **Add route** → **Published application**: the hostname is `KCC_HOST`, and the service
     URL is `http://app:8080`. Older dashboards call this a public hostname, with service type HTTP and URL `app:8080`.
4. **Access for the backoffice, for good.** Zero Trust → Access controls → Applications → **Add an application** →
   **Self-hosted**:
   - name `KCC backoffice`; public hostname `KCC_HOST`, path `umbraco`;
   - identity provider One-time PIN; session duration 24 hours;
   - one policy, `Owner`: action **Allow**, include **Emails**, with your email address.
5. **Access for the whole site, until launch.** The same again:
   - name `KCC pre-launch`; public hostname `KCC_HOST`, no path;
   - policy `Testers`: **Allow**, **Emails**: yours, and anyone else's who should see the site before launch.

   The more specific application wins, so `/umbraco` stays yours alone. Launch (replatform Phase 8) deletes this
   application.

## 5. The backup bucket, and the check that it ran

1. **R2.** R2 Object Storage: subscribe (free tier, $0), then **Create bucket** named `kcc-backups`, with location
   Automatic.
2. **Its token.** R2 → Account Details → API Tokens → **Manage** → **Create Account API token**:
   - permission **Object Read & Write**, applied to `kcc-backups` only; no expiry;
   - note the **Access Key ID**, the **Secret Access Key** (shown once) and the S3 endpoint,
     `https://<account id>.r2.cloudflarestorage.com`.
3. **A second token for drills,** the same with **Object Read** only. Keep it for section 9.
4. **healthchecks.io.** Add a check named `kcc-backup`:
   - schedule **Cron** `0 3 * * *`, with the Pi's time zone;
   - grace time **3 hours**;
   - copy its ping URL, `https://hc-ping.com/<uuid>`. It emails you when a night passes without a backup.

Backblaze B2 works too, with no card and 10 GB free: set `RCLONE_CONFIG_BUCKET_PROVIDER` to `Other` in
`compose.yaml`'s backup environment, and use B2's S3 endpoint and an application key.

## 6. GitHub

1. **A token for the Pi to pull images.** Settings → Developer settings → Personal access tokens → **Tokens (classic)**
   → Generate new token:
   - scope `read:packages` only, and an expiry of a year, with a reminder to rotate it (section 8);
   - GHCR accepts only classic tokens.
2. **The packages stay private.** After the first push to `main` (the images job), open your profile's Packages. For
   each of `kcc-app`, `kcc-ssr` and `kcc-backup`, Package settings should say **Private**, and list this repository
   with the Admin role under "Manage Actions access". The images job needs that role to prune old versions.
3. **Dependabot's secrets.** Workflows that Dependabot's pull requests trigger read Dependabot's secrets, not the
   repository's. Under Settings → Secrets and variables → **Dependabot**, add copies of `FONTAWESOME_NPM_AUTH_TOKEN`,
   `KCC_E2E_MEMBER_USERNAME` and `KCC_E2E_MEMBER_PASSWORD` with the same values as the Actions secrets.

## 7. Install the stack and boot it

1. **The settings.** Fill in every value:

   ```bash
   cd /srv/kcc/repo/deploy
   sudo -u kcc cp .env.example .env && sudo chmod 600 .env
   sudo -u kcc nano .env
   ```

   - `KCC_HOST` is the hostname from section 1.
   - For `KCC_IMAGING_HMAC_KEY`, run `openssl rand -base64 64 | tr -d '\n'` and paste the output.
   - `KCC_TUNNEL_TOKEN` is from section 4, and the bucket's four values and the ping URL from section 5.
   - `ANTHROPIC_API_KEY` is optional.
2. **The first boot's administrator:**

   ```bash
   sudo -u kcc cp first-boot.env.example first-boot.env && sudo chmod 600 first-boot.env
   sudo -u kcc nano first-boot.env
   ```

   Set your name, your email and a password of at least 10 characters. Keep them in your password manager.
3. **Sign in to GHCR** as `kcc`, pasting the classic token when Docker asks for the password:

   ```bash
   sudo -u kcc -H docker login ghcr.io -u <your GitHub username>
   ```

   Docker stores it in `/srv/kcc/.docker/config.json`, readable by `kcc` alone.
4. **Boot:**

   ```bash
   sudo -u kcc -H kcc up -d --wait
   sudo -u kcc -H kcc logs app | grep "uSync First boot complete"
   sudo -u kcc -H kcc ps
   ```

   The first pull takes a few minutes. The `grep` must print a line: uSync imports the baseline on the first boot only,
   and never tries again if that boot failed (section 11). `ps` shows `app`, `ssr` and `cloudflared` as healthy.
5. **Look at it** from a phone off your Wi-Fi:
   - open `https://KCC_HOST`, pass Access with your email and the PIN it sends, and the site appears;
   - open `/umbraco`, and sign in to Umbraco as the administrator from `first-boot.env`.
6. **Turn the installer off** for good:

   ```bash
   sudo rm /srv/kcc/repo/deploy/first-boot.env
   sudo -u kcc -H kcc up -d --wait
   ```

   Without `first-boot.env` the site never installs Umbraco. So a lost database stops at Umbraco's installer, behind
   Access, and the health check fails, instead of the site quietly becoming an empty one.
7. **The timers:**

   ```bash
   sudo cp /srv/kcc/repo/deploy/systemd/kcc-* /etc/systemd/system/
   sudo systemctl daemon-reload
   sudo systemctl enable --now kcc-deploy.timer kcc-backup.timer
   systemctl list-timers 'kcc-*'
   ```

   The units are copies. If a later commit changes them, copy them again and reload.
8. **The first backup, now:**

   ```bash
   sudo systemctl start kcc-backup.service
   journalctl -u kcc-backup -n 20 --no-pager
   ```

   The journal ends with `uploaded daily/kcc-<date>.tar.gz`, the bucket holds the file, and the healthchecks.io check
   turns green.

## 8. Everyday operations

- **Deploys.** Merge to `main`. CI pushes the images, and within five minutes the Pi deploys whatever changed,
  snapshotting the database first, and the app restarts for about a minute. A merge that changes no image, such as one
  to `docs/` alone, deploys nothing: the build reuses its cache, down to each image's digest. To watch, run
  `journalctl -u kcc-deploy -f`. To deploy now: `sudo systemctl start kcc-deploy.service`.
- **Status and logs.** `sudo -u kcc -H kcc ps`, and `sudo -u kcc -H kcc logs --tail 100 app`. The backoffice's
  Settings → Log Viewer reads Umbraco's own logs, kept for 31 days on the data volume.
- **Pause for maintenance**, so no deploy or backup starts in the middle:

  ```bash
  sudo systemctl stop kcc-deploy.timer kcc-backup.timer
  sudo systemctl start kcc-deploy.timer kcc-backup.timer
  ```

  The first line pauses; the second, afterwards, resumes.
- **Roll back a bad deploy.**
  1. Pause the timers.
  2. Find the last good commit's full SHA in the repository's history. Each image carries its commit's SHA as a tag.
  3. Add `KCC_IMAGE_TAG=<sha>` to `.env`.
  4. If the bad version changed the database, as an Umbraco upgrade does, put back the snapshot the deploy took
     first:

     ```bash
     sudo -u kcc -H kcc stop app
     sudo -u kcc -H kcc run --rm --entrypoint ls backup /srv/kcc/snapshots/pre-deploy
     sudo -u kcc -H kcc run --rm backup rollback Umbraco-<time>.sqlite.db
     ```

  5. Run `sudo -u kcc -H kcc up -d --wait`, then resume the timers.
  6. Once `main` has the fix, take the line out of `.env`, then run `sudo -u kcc -H kcc pull app ssr backup` and
     `sudo -u kcc -H kcc up -d --wait`. The deploy timer never reacts to `.env` alone.
- **Restore from the bucket,** when the database is lost or damaged:

  ```bash
  sudo systemctl stop kcc-deploy.timer kcc-backup.timer
  sudo -u kcc -H kcc stop app
  sudo -u kcc -H kcc run --rm restore latest
  sudo -u kcc -H kcc up -d --wait
  sudo systemctl start kcc-deploy.timer kcc-backup.timer
  ```

  Name an archive instead of `latest` to go further back, such as `daily/kcc-2026-10-01.tar.gz` or
  `weekly/kcc-2026-09-27.tar.gz`. To list them:
  `sudo -u kcc -H kcc run --rm --entrypoint sh backup -c 'rclone lsf -R "bucket:$KCC_BACKUP_BUCKET"'`.
- **Production data down to your computer** (spec §12), over Tailscale.
  1. On the Pi:

     ```bash
     sudo -u kcc -H kcc run --rm backup snapshot
     sudo -u kcc -H kcc run --rm -T --entrypoint sh backup -c \
       'cat "$(find /srv/kcc/snapshots/pre-deploy -name "*.sqlite.db" | sort | tail -n 1)"' >/tmp/Umbraco.sqlite.db
     sudo -u kcc -H kcc run --rm -T --entrypoint tar backup -czf - -C /srv/kcc media >/tmp/kcc-media.tar.gz
     ```

  2. On your computer, with the development site stopped, from the repository's root:

     ```bash
     scp '<you>@kcc:/tmp/Umbraco.sqlite.db' '<you>@kcc:/tmp/kcc-media.tar.gz' .
     rm -f src/KCC.Web/umbraco/Data/Umbraco.sqlite.db*
     mv Umbraco.sqlite.db src/KCC.Web/umbraco/Data/
     rm -rf src/KCC.Web/wwwroot/media && tar -xzf kcc-media.tar.gz -C src/KCC.Web/wwwroot && rm kcc-media.tar.gz
     ```

  3. Delete the copies on the Pi: `sudo rm /tmp/Umbraco.sqlite.db /tmp/kcc-media.tar.gz`. They hold members' data.

  The development site then signs you in as the production administrator.
- **Updates.** Dependabot opens pull requests weekly: merging one deploys it. Umbraco's 17.x patches arrive that way.
  The OS updates itself; reboot now and then for a new kernel (`sudo reboot`), and the stack comes back on its own.
- **Rotate the GHCR token** before it expires: make a new classic token (section 6), then run
  `sudo -u kcc -H docker login ghcr.io -u <your GitHub username>` again.

## 9. The restore drill

Once a quarter, and before launch, restore the latest backup from the bucket into a throwaway stack on your computer
(Docker Desktop on an Apple silicon Mac runs the same arm64 images).

1. Sign in to GHCR with the classic token: `docker login ghcr.io -u <your GitHub username>`.
2. Outside the repository, create `~/kcc-drill/drill.env`:

   ```sh
   KCC_HOST=localhost
   KCC_IMAGING_HMAC_KEY=<output of: openssl rand -base64 64 | tr -d '\n'>
   KCC_TUNNEL_TOKEN=unused
   KCC_BACKUP_BUCKET=kcc-backups
   KCC_BACKUP_ENDPOINT=https://<account id>.r2.cloudflarestorage.com
   KCC_BACKUP_ACCESS_KEY_ID=<the read-only token's id>
   KCC_BACKUP_SECRET_ACCESS_KEY=<the read-only token's secret>
   ```

3. From `deploy/` in a checkout of `main`:

   ```bash
   docker compose -p kcc-drill --env-file ~/kcc-drill/drill.env -f compose.yaml -f local.yaml run --rm restore latest
   docker compose -p kcc-drill --env-file ~/kcc-drill/drill.env -f compose.yaml -f local.yaml up -d --wait
   ```

4. Open `https://localhost:8443` and accept the local certificate. Check:
   - the home page and a recipe, with its image;
   - `/umbraco`, where you sign in as the production administrator and find your content.
5. Clean up:

   ```bash
   docker compose -p kcc-drill --env-file ~/kcc-drill/drill.env -f compose.yaml -f local.yaml --profile jobs down -v
   rm -rf ~/kcc-drill
   ```

Note the date and the archive's name. A drill that fails is the most useful thing this runbook can tell you.

## 10. Moving to a VPS

The same compose file runs on any Linux host with Docker (spec §13.7).
- An x86 host needs amd64 images. Add `"linux/amd64"` to `platforms` in `docker-bake.hcl`. The images job then has to
  build on QEMU and push both platforms in one step, because a multi-platform image cannot be loaded into the runner's
  Docker for the smoke test. Plan that change when it is needed.
- Set the host up as in section 3 from step 7, then restore the latest backup into its new volumes before the first
  `up`: skip `first-boot.env`, run `kcc run --rm restore latest`, then `kcc up -d --wait`.
- Stop the Pi's tunnel first (`sudo -u kcc -H kcc stop cloudflared`), so the two never serve different databases. The
  new host uses the same tunnel token, and nothing in DNS changes: the tunnel is the route.

## 11. Troubleshooting

- **`app` unhealthy, or restarting.** Read `sudo -u kcc -H kcc logs --tail 200 app`.
  - If the first boot never logged `uSync First boot complete`, its import failed and will not run again. Run
    `sudo -u kcc -H kcc down`, then `docker volume rm kcc_data`, fix the cause, and boot again with `first-boot.env`.
  - If Umbraco is at its installer, the database is missing: restore it (section 8).
- **Cloudflare error 1033 or 502.** The tunnel is not connected, or the app is not healthy. Check
  `sudo -u kcc -H kcc ps`, and `sudo -u kcc -H kcc logs --tail 50 cloudflared`.
- **The backoffice says "This server only accepts HTTPS requests",** or its sign-in loops. The app is not applying the
  tunnel's headers. cloudflared must be at `172.30.9.10`, and the app's `Hosting__TunnelAddress` in `compose.yaml` must
  say the same. To check the first:
  `docker inspect kcc-cloudflared-1 -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}'`.
- **The backoffice shows network errors after a day.** The Access session has expired, and the backoffice's API calls
  meet Cloudflare's sign-in instead of Umbraco. Reload the page and pass Access again.
- **healthchecks.io says a backup is late,** or failed: `journalctl -u kcc-backup -n 50 --no-pager`.
- **`Cache instruction sync did not complete`** in the log soon after a first boot is harmless on one server (Phase 1's
  findings), and clears itself.
- **Pulls fail with `denied`.** The GHCR token has expired or lacks `read:packages`: rotate it (section 8).
- **The disk fills up.** `docker system df` shows what is using it. Container logs rotate by themselves, and each
  deploy prunes unused images.
````

- [ ] **Step 2: Point the README at it**

The README's table of contents already links a **Deployment** section, which no longer exists. In `README.md`, add
this section before `## Frontend Development`:

```markdown
## Deployment

The site runs on a Raspberry Pi behind a Cloudflare Tunnel, as the containers in `deploy/compose.yaml`. On every push
to `main`, CI builds the images, smoke-tests them and pushes them to private GHCR packages, and within five minutes
the Pi pulls and deploys them. A nightly job backs the site up to Cloudflare R2.

- **Build the images:** `docker buildx bake --load` at the repository root. It needs the Font Awesome token, as `yarn`
  does.
- **Run them the way the Pi does:** `deploy/smoke-test.sh`. It boots them with Caddy standing in for the tunnel on
  `https://localhost:8443`, checks the site and a restored backup, and removes everything afterwards.
- **Everything else,** from Cloudflare and the Pi to deploys, rollbacks, restores and the restore drill, is in
  [`docs/hosting/runbook.md`](docs/hosting/runbook.md).
```

- [ ] **Step 3: Correct the spec where this phase built something else**

Make these replacements in `docs/replatform/specs/2026-09-21-replatform-off-xperience.md`, and mention each to the
owner at the gate:

1. §6.5, the **Unattended install** bullet. Replace "taking the admin account from secrets. The credentials matter
   only on first boot." with "taking the admin account from secrets. In production only the first boot installs:
   `deploy/first-boot.env` turns the install on with the admin account, and is deleted afterwards, so a lost database
   stops at the installer instead of becoming an empty site."
2. §13.2, the `app` row. Replace "Both owned by the container user. Health check on `/healthz`" with "Both owned by the
   container user. `init: true`, so a crash exits and restarts instead of hanging. Health check on Umbraco's readiness
   probe, `/umbraco/api/health/ready`".
3. §13.2, the `backup` row. Replace the whole row with:

   ```markdown
   | `backup`, `restore` | One-off jobs in the `jobs` profile, as the app's user: the nightly backup (§13.5), started by a systemd timer; the pre-deploy snapshot; restores. The media volume is mounted read-only for a backup, the data volume read-write, because SQLite cannot open a WAL database read-only once the app has closed it |
   ```

4. §13.2. Replace "Every service restarts unless stopped." with "Every long-running service restarts unless stopped."
5. §13.3, the **Cloudflare Access** bullet. After "one seat of the free 50." add " Until launch a second application
   covers the whole host, for the owner and anyone testing; launch deletes it (the owner's decision, 2026-09-26)."
6. §13.4, the first bullet. Replace "Multi-stage Dockerfiles for `app` and `ssr`, with the repo root as the build
   context because of the yarn workspace layout." with "One multi-stage Dockerfile for `app` and `ssr`, with the repo
   root as the build context because of the yarn workspace layout, and a small Alpine image for the backup jobs."
7. §13.4, the second bullet. Replace "Old versions are pruned to stay inside GitHub's free private-package allowance;
   if transfer ever runs out, `docker save` / `docker load` over Tailscale is the fallback." with "Old versions are
   pruned to the last ten. GitHub does not bill container images against the private-package allowance; if that
   changes, `docker save` / `docker load` over Tailscale is the fallback."
8. §15, the Phase 8 row. Replace "`RobotsTxtDenyAll` off;" with "`RobotsTxtDenyAll` off and the whole-site Access
   application deleted;".

Rewrap the changed paragraphs to the file's 120-column width.

```bash
git add docs/hosting/runbook.md README.md docs/replatform/specs/2026-09-21-replatform-off-xperience.md
git commit -m "Write the Hosting Runbook and Correct the Spec to Match"
```

- [ ] **Step 4: Everything, once more**

From the repository root:

```bash
dotnet build KitchenCommandCenter.sln
yarn build:all
(cd src/KCC.Web && yarn test && yarn type-check)
node tests/scripts/run.mjs
docker buildx bake --load
deploy/smoke-test.sh
```

Expected:
- `Build succeeded` with 0 warnings; every bundle builds; Vitest and the type check pass.
- The combined run is green: unit 3 more than before this phase, integration 6 more, E2E unchanged. It opens its HTML
  report when it finishes.
- The three images build, and the smoke test prints eleven `ok` lines.

- [ ] **Step 5: Push the branch (the owner approves first)**

CI runs on pull requests and on pushes to `main`, not on a push to `hosting`, so the pull request is where it first
runs. Ask the owner, then:

```bash
git push -u origin hosting
```

- [ ] **Step 6: Open the pull request (the owner approves first)**

Write the body to `.superpowers/phase-7/pr-body.md`:

```markdown
Hosts Kitchen Command Center on a Raspberry Pi 5 behind a Cloudflare Tunnel: replatform Phase 7
(`docs/replatform/plans/2026-09-26-phase-7-hosting.md`; spec §13).

- The app trusts forwarded headers from the tunnel's address alone, sends HSTS, and runs in Umbraco's Production
  mode, with its logs on the data volume and the install off after the first boot. The Vite manifest now publishes.
- One Dockerfile builds the app and SSR images from one front-end build, and a small image carries the backup jobs.
- `deploy/compose.yaml` runs them read-only, with cloudflared as the only way in. `deploy/smoke-test.sh` checks the
  site, the trust boundary and a backup restored into fresh volumes.
- The Pi deploys by pull every five minutes (`deploy/deploy.sh`), and backs up nightly to R2. The steps are in
  `docs/hosting/runbook.md`.
- A new `images` job builds and smoke-tests the images on an arm64 runner, and pushes them to private GHCR packages on
  `main`. Dependabot keeps NuGet, npm, the base images and the actions current.

Test plan: the combined suite is green (unit <count>, integration <count>, E2E <count>, Vitest <count>); the images
build and the smoke test passes locally and in this pull request's `images` job.
```

Fill in the counts from Step 4. Then, with the owner's approval:

```bash
gh pr create --repo Th3FenrisWolf/Kitchen-Command-Center --base main --head hosting \
  --title "Host the Site on a Raspberry Pi Behind Cloudflare" --body-file .superpowers/phase-7/pr-body.md
```

Give the owner the pull request's link. Both jobs must go green (`gh pr checks`). The `images` job's first run takes
longest, with no cache yet. A failure there that passes locally usually has one of three causes:
- the `FONTAWESOME_NPM_AUTH_TOKEN` secret is missing or expired, and `yarn install` answers 401;
- a path whose case differs from the file's, which Linux does not forgive;
- the arm64 runner queueing, which only needs patience.

- [ ] **Step 7: The owner merges it**

When CI is green, the owner merges, as a squash like every pull request on `main`. The push to `main` runs the
workflow again, and this time the `images` job pushes:

```bash
git checkout main && git pull --ff-only
gh run list --repo Th3FenrisWolf/Kitchen-Command-Center --branch main --limit 1
```

Expected: the newest run succeeded, both jobs green. The owner then checks that the three packages exist and are
private, as runbook §6 describes.

---

### Task 9: Cloudflare, the bucket and GitHub (the owner)

The owner does this task, following the runbook; no agent signs in anywhere. The agent answers questions, checks from
outside, and notes each place where the runbook and reality differ, for Task 12. Section 2, the DNS move, can start
at any time, even before Task 1, because the nameservers can take two days to switch. The rest needs the domain
active in Cloudflare.

**Files:** none. Differences from the runbook go into a list for Task 12.

- [ ] **Step 1: DNS (runbook §2)**

When the owner has switched the nameservers, check from any machine:

```bash
dig +short NS <the domain>
dig +short MX <the domain>
```

Expected: two nameservers ending in `ns.cloudflare.com`, and the same mail servers as before the move. If the
Squarespace site stays, the owner checks that it still loads.

- [ ] **Step 2: The tunnel and Access (runbook §4)**

Once the owner has made the tunnel's route and both Access applications, check that Access guards the whole host
before the Pi exists:

```bash
for path in / /umbraco/ /api/recipes/search; do curl -sS -o /dev/null -w "%{http_code} %{redirect_url}\n" "https://<KCC_HOST>$path"; done
```

Expected: `302` for each, redirecting to `https://<team>.cloudflareaccess.com/cdn-cgi/access/login/…`. Behind Access,
until the Pi connects, Cloudflare answers error 1033: the tunnel has no connector yet.

- [ ] **Step 3: The bucket and healthchecks.io (runbook §5), and GitHub (runbook §6)**

The owner confirms:
- the bucket `kcc-backups` exists;
- the read-and-write token and the read-only token are in the password manager;
- the `kcc-backup` check exists with its schedule;
- the classic `read:packages` token exists;
- after Task 8's merge, the three GHCR packages are private, with this repository as Admin;
- the Dependabot secrets exist.

The agent cannot see private packages or secrets, so the owner's word is the check.

---

### Task 10: The Pi and the first boot (the owner)

The owner follows runbook §3 (the Pi) and §7 (the stack). The agent guides, and checks from outside what it can.

**Files:** none. Differences from the runbook go into Task 12's list.

- [ ] **Step 1: The Pi (runbook §3)**

The owner confirms:
- `ssh <you>@kcc` works over the tailnet, and a password login is refused;
- `sudo ufw status` shows the three allow rules;
- `docker compose version` is 2.24 or later;
- `/srv/kcc/repo/deploy` holds this repository's `deploy/` folder.

- [ ] **Step 2: The first boot (runbook §7, steps 1 to 6)**

The owner confirms each of these:
- The first boot's log has `uSync First boot complete`.
- `sudo -u kcc -H kcc ps` shows `app`, `ssr` and `cloudflared` healthy.
- From a phone off the home network, Access asks for the PIN, then the site renders.
- `/umbraco` signs in as the first-boot administrator.
- `first-boot.env` is deleted, and the stack is healthy again after `up -d --wait`.

- [ ] **Step 3: The timers and the first backup (runbook §7, steps 7 and 8)**

The owner confirms:
- `systemctl list-timers 'kcc-*'` lists both timers;
- `journalctl -u kcc-backup` ends with `uploaded daily/kcc-<date>.tar.gz`;
- the bucket holds that file;
- the healthchecks.io check is green.

---

### Task 11: The gate: the site reachable behind Access, and a restore rehearsed from the bucket

This task is spec §15's gate for the phase: "The site reachable behind Access; a restore rehearsed from the bucket".

**Files:** none. The results go into Task 12's findings.

- [ ] **Step 1: The site behind Access, in both ramps**

On a phone off the home network, past Access, in light and then dark (the header's toggle switches them), the owner
checks:
1. Home renders, styled, with the header and footer; the page source shows server-rendered markup inside
   `<div id="app">`.
2. `/recipes/` lists what production holds, which may be nothing yet, and its filters open.
3. `/account/login` renders; `/this-page-does-not-exist` shows the torn 404 sheet.
4. `/umbraco` asks for no second PIN within the Access session, and Umbraco's sign-in works. This proves the forwarded
   HTTPS: without it the sign-in fails with "This server only accepts HTTPS requests".

The agent checks that nothing reaches the site without Access:

```bash
curl -sS -o /dev/null -w "%{http_code} %{redirect_url}\n" "https://<KCC_HOST>/"
curl -sS -o /dev/null -w "%{http_code} %{redirect_url}\n" "https://<KCC_HOST>/umbraco/"
```

Expected: `302` to the Access login, both.

- [ ] **Step 2: A backup the timer ran**

The morning after Task 10, the owner confirms:
- `journalctl -u kcc-backup --since yesterday` shows a run started by the timer, ending with `uploaded`;
- the healthchecks.io check had its ping on schedule.

- [ ] **Step 3: The restore drill (runbook §9)**

On the owner's Mac, with the read-only token, the owner restores `latest` into a throwaway `kcc-drill` stack. It must:
- boot healthy;
- show the production home at `https://localhost:8443`, and a recipe with its image, if production has one;
- sign the owner in at `/umbraco` as the production administrator.

Then the owner removes the stack. Record the date and the archive's name.

- [ ] **Step 4: The deploy timer runs clean**

Every five minutes `deploy.sh` pulls `deploy/` from git and the three images from GHCR with the Pi's token, then finds
nothing to do. The owner confirms:
- `systemctl list-timers 'kcc-deploy*'` shows a run within the last five minutes;
- `journalctl -u kcc-deploy --since "1 hour ago" --no-pager` shows runs ending `Deactivated successfully`, with no
  `denied` or `error`.

The path where something changed (snapshot, then `up`) ran in Task 6's deploy test. On the Pi, the first time is the
next merge that changes an image, such as a Dependabot update or Phase 8's first commit: its journal then shows
`deployed …`.

- [ ] **Step 5: Every gate's checks**

Task 8, Step 4 covered the build with warnings as errors, `yarn build:all`, Vitest and the suites; Step 1 above is the
browser check in both ramps. If anything changed on `main` since Task 8, run Task 8, Step 4 again.

---

### Task 12: Close the phase

**Files:**
- Modify: this plan (its Status line, and a closing "Findings from Phase 7" section)
- Modify: `docs/hosting/runbook.md`, with the differences Tasks 9 to 11 found
- Memory, outside the repository, in `~/.claude/projects/-Users-twinright-Repos-Kitchen-Command-Center/memory/`:
  `kcc-hosting.md` (new), `replatform-off-xperience.md`, `gitignore-icon-rule.md`, `MEMORY.md`

- [ ] **Step 1: Branch, and correct the runbook**

```bash
git checkout main && git pull --ff-only && git checkout -b hosting-close
```

Apply every difference Tasks 9 to 11 found, such as a renamed dashboard path or a missing command, to
`docs/hosting/runbook.md`.

- [ ] **Step 2: Record the findings**

Append to this plan:

```markdown
## Findings from Phase 7

Found while running the site on the Pi (<dates>).

- The hostname is <KCC_HOST>. The tunnel is `kcc`; Access has `KCC backoffice` (`/umbraco`, permanent) and
  `KCC pre-launch` (the whole host, which Phase 8 deletes).
- The first boot on the Pi took <time>. At idle the app used <MiB>, and ssr <MiB> (`docker stats --no-stream`).
- The restore drill (<date>) restored `<archive>` into a fresh stack, which booted and served the site.
- <each difference from the runbook, and how it was fixed>
```

Set this file's **Status** line to `done (<date>)`.

- [ ] **Step 3: Memory**

Create `kcc-hosting.md`:

```markdown
---
name: kcc-hosting
description: KCC runs on a Raspberry Pi 5 behind a Cloudflare Tunnel since replatform Phase 7 — deploy by pull (kcc-deploy timer), nightly R2 backups, docs/hosting/runbook.md; the container traps the probe found (unpublished Vite manifest, read-only root and Views, PID 1 hang, empty unattended settings, volume ownership, VACUUM INTO vs .backup)
metadata:
  type: project
---

Since replatform Phase 7 (<date>) the site runs from `deploy/compose.yaml` on a Pi 5 at `/srv/kcc/repo/deploy`
(sparse clone, user `kcc`, wrapper `sudo -u kcc -H kcc …`). CI's `images` job builds `kcc-app`, `kcc-ssr` and
`kcc-backup` on an arm64 runner, smoke-tests them (`deploy/smoke-test.sh`) and pushes them to private GHCR on `main`;
`kcc-deploy.timer` deploys within five minutes. Backups go to R2 nightly; the runbook is `docs/hosting/runbook.md`.
Until launch Access guards the whole host; `/umbraco` always.

Traps met while building it:
- `dotnet publish` drops `wwwroot/.vite/manifest.json` (a dot-folder); `KCC.Web.csproj` includes it.
- Umbraco creates `~/Views/Partials` at every start: the image makes it, or a read-only root refuses.
- As PID 1 a crashed .NET process ignores its own SIGABRT and the container hangs "Up (unhealthy)": `init: true`.
- Compose passes an unset `${X:-}` as an empty string, and Umbraco refuses an empty unattended email. Production
  keeps the install off; `first-boot.env` turns it on once.
- An empty named volume takes the content and owner of the image path it mounts on (Alpine's `/media` made the media
  volume root-owned); the backup image mounts under `/srv/kcc`.
- `VACUUM INTO` writes a rollback-journal database; Umbraco sets WAL only at creation. Snapshots use `.backup`.
- Forwarded headers trust `172.30.9.10` alone; ID2029 against ID2083 from the backoffice's authorize endpoint proves
  which side of the trust boundary a request came from.

**Why:** each cost a probe run while Phase 7 was planned.

**How to apply:** read the runbook before touching the Pi, and this list before changing the Dockerfile, the compose
file or the backup script. See [[replatform-off-xperience]] and [[gitignore-icon-rule]].
```

Add its line to `MEMORY.md`, after the replatform line:

```markdown
- [KCC hosting on the Pi](kcc-hosting.md) — Pi 5 behind a Cloudflare Tunnel since Phase 7: pull deploys, nightly R2 backups, docs/hosting/runbook.md; container traps (unpublished Vite manifest, Views on a read-only root, PID 1 hang, empty unattended settings, volume ownership, .backup not VACUUM INTO)
```

In `gitignore-icon-rule.md`, add that `.gitignore`'s Visual Studio `Backup*/` line also silently ignores any path
component starting with `backup`, which is why the backup image lives in `deploy/kcc-backup/`. Update its
`MEMORY.md` line to name both rules. In `replatform-off-xperience.md`, add one line saying Phase 7 is done, with the
date and the pull requests, and append `Phase 7 done (<date>): on the Pi behind Access.` to its `MEMORY.md` line.

- [ ] **Step 4: The closing pull request (the owner approves each step)**

```bash
git add docs/replatform/plans/2026-09-26-phase-7-hosting.md docs/hosting/runbook.md
git commit -m "Close Replatform Phase 7"
git push -u origin hosting-close
gh pr create --repo Th3FenrisWolf/Kitchen-Command-Center --base main --head hosting-close \
  --title "Close Replatform Phase 7" --body "Records Phase 7's findings and the runbook's corrections."
```

Ask the owner before the push and before the pull request. When CI is green, the owner merges.

Phase 8 (Launch) is planned next, against the site as it now runs. Its first code change is the first image the
Pi deploys by itself; its plan checks the `deployed …` line in `journalctl -u kcc-deploy`.

---

## The gate

The Phase 7 row of spec §15:
- **The site reachable behind Access:** Task 11, Step 1, with Task 10 before it.
- **A restore rehearsed from the bucket:** Task 11, Step 3, from a backup the timer made (Step 2).

Every gate also requires:
- `dotnet build` clean with warnings as errors;
- `yarn build:all`, with both bundles built together;
- Vitest green;
- a browser check of the touched pages in both ramps.

Task 8, Step 4 and Task 11, Step 1 cover those.

## Findings from Phase 7

Found while building (2026-09-29 to 2026-09-30), Tasks 1 to 8, Step 4. Task 12 adds what the Pi run finds.

- **The branch.** Phase 7 ran on `hosting`, based on `replatform-phase-6` at `ec5c44a`, because Phase 6's merge into
  `main` (its Task 8) waits on the owner. The pull request opens after that merge: if Phase 6 lands as a squash, rebase
  first with `git rebase --onto main ec5c44a hosting`.
- **"Before you start".** Every check passed except the first (the merge). Check 11's grep also finds Umbraco's own
  `Umbraco:CMS:Hosting:*` settings in `UmbracoSite`, which are unrelated to `Hosting:TunnelAddress`.
- **Where the code departs from this plan's text.**
  - Task 3: the ssr-deps stage's Yarn cache mount has its own id, `yarn-ssr`. The two install stages run in parallel,
    Yarn 1 does not lock its cache, and a shared cold cache failed 3 of 3 concurrent trials; a fresh CI runner starts
    cold. The `wwwroot` copy loop fails on any failed copy.
  - Tasks 4 and 5: the smoke test checks `*'<div id="app"><'[!/]*`. The plan's pattern also matches the empty
    client-side fallback `<div id="app"></div>`, so it could never fail. The Phase 8 plan's live check (its line 1128,
    `grep -c '<div id="app"><'`) has the same hole.
  - Task 5: the smoke test traps INT and TERM, because dash skips the EXIT trap on a signal and left both stacks behind.
    It unsets the variables compose interpolates, except `KCC_REGISTRY` and `KCC_IMAGE_TAG`, because compose prefers the
    shell's to `--env-file`. The drill's `KCC_FIRST_BOOT_ENV` names a file that never exists.
  - Task 5: `kcc-backup` refuses a database that is missing, empty, damaged or without Umbraco's schema, for every copy
    and every restored or rolled-back database. `sqlite3` had created an empty database in the live volume, and an empty
    database passes `integrity_check`, so a lost database became a "good" archive. `nightly` fails, pings `/fail` and
    deletes the uploaded object when tar fails: the pipe had hidden tar's status, and an unreadable file uploaded an
    incomplete archive with a success ping. `nightly` and `restore` stop before any rclone call when `KCC_BACKUP_BUCKET`
    is unset or empty, because the `:?` inside `$(...)` ended only the subshell.
  - Task 6: `deploy.sh` keeps the last deployed commit in `refs/kcc/deployed`, because a `deploy/` change was lost when
    the run that pulled it failed. With nothing to deploy, a run fails with `not healthy: …` while a service is not
    running or is unhealthy, because a failed `up --wait` was otherwise forgotten. The pre-deploy snapshot is taken only
    while the app is running and not unhealthy, because retries of a failing `deploy/` change rotated the pre-change
    snapshot out of the five kept. Accepted: a slow crash loop, which reads running or starting at each check, still
    snapshots on those retries; the nightly backup is the fallback.
  - Task 6: the deploy test runs `deploy.sh` behind a `docker` wrapper that skips `image prune`, which on a developer's
    machine removes every dangling image, not only this project's.
  - Task 7: the images job pushes every commit tag before it moves a `main` tag, never cancels a run on `main`, and
    moves `main` only while `main` still names the run's commit. Otherwise a cancel mid-push could pair app and ssr
    from different builds, and re-running an old run could roll production back. `build-and-test` shellchecks the deploy
    scripts, because `deploy.sh` updates itself on the Pi before CI has run anything.
  - Task 7: Dependabot does not read `deploy/local.yaml`, so Caddy's tag is bumped by hand. Renaming the file into
    Dependabot's compose pattern would make it parse the `!reset` tags, and Caddy never runs on the Pi.
  - Task 8: the runbook describes all of the above. It also runs `chmod 755 /srv/kcc` after `useradd`, because Debian 13
    creates home folders with mode 700. It adds `sudo -u kcc -H` to three commands in section 11, because only `kcc` is
    in the docker group. It stops the services along with the timers, and a move to a VPS retires the Pi's timers and
    takes a last backup first. The spec takes the plan's eight corrections, one to §13.4's deploy bullet, and one to
    §13.7's DNS phrase.
  - The final review corrected the stale premise of `RateLimits`' comment, a Dependabot comment that promised the
    future, and the runbook's "harmless" cache-instruction bullet (wrong since Phase 4's guard).
- **Decisions this run made that earlier phases left open.**
  - Phase 5's backoffice source maps stay in the images. The repository is public, and until launch Access covers the
    whole site.
  - Phase 4's Anthropic client timeout is not fixed here. Leave `ANTHROPIC_API_KEY` empty on the Pi until it is.
- **Known limitations.**
  - The backup jobs run BusyBox sh as PID 1, with no init and no signal traps. A stopped or timed-out job leaves its
    work folder and sends no `/fail`; healthchecks.io's grace still alerts. Stopping `kcc-backup.service` ends the
    compose client but not a job container already running, so a restore started meanwhile can overlap a backup, which
    then fails safe.
  - The deploy timer never sees a change to `.env`, and an exact revert of a failed `deploy/` change does not redeploy.
    The runbook covers both with `kcc up -d --wait`.
  - CI never runs cloudflared, because Caddy stands in for it. Nothing watches the site or the deploys: a failed deploy
    shows only in `systemctl --failed` and the journal.
  - The data-protection keys sit unencrypted on the data volume and in every backup; the bucket is private.
  - Production logs "Features/Main.ts doesn't have CSS chunks" on every render (filed in Phase 6), and "The culture
    specified  was not found" 62 times during the first boot's import.
  - A tar status of 1, from a media file changed while it was read, fails that night's backup. A missing bucket sends
    no ping at all.
- **For Task 12's runbook pass.** Section 11's first-boot bullet resumes the timers even when they were never
  installed; its troubleshooting bullet does not name `rollback`'s refusal messages; rollback step 6 and the
  production-data snapshot run outside `/srv/kcc/kcc.lock`; and `WriteLockedCacheInstructionService`'s comment puts the
  stall at about ten minutes where the runbook says about 20.
- **Recommendations from the final review, for the owner.** Protect `main` with required checks, because the Pi takes
  `deploy/` from git whatever CI says. Consider a second healthchecks.io check that `deploy.sh` pings. Pin the images
  job's actions by SHA.
- **Counts.** 1329 passed, 0 failed: unit 232 (Phase 6's 229 plus 3), integration 260 (plus 6), E2E 41, web Vitest 769
  (plus 2) with its 2 expected failures, admin Vitest 17, contributions Vitest 8. The smoke test's 11 checks pass in
  about 27 seconds, and the deploy test's three runs pass.
