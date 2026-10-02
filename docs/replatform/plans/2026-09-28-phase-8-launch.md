# Replatform Phase 8 — Launch Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Status:** in progress: Tasks 1 and 2, Steps 1 to 6, done (2026-09-30) on branch `ready-for-public`, based on
`hosting` at `c4a84bf`. **Resume point:** Task 2, Step 7. The push, the pull request and the merge wait on the owner,
and on Phase 7's merge into `main`. Task 3 waits for Umbraco 17.8.0 on NuGet (scheduled for 2026-10-29), and branches
off `main` once this pull request is merged. Task 4, the owner's content, can start once "Before you start" passes,
and Task 6 waits for Tasks 2, 3 and 5. "Findings from Phase 8", at the end, lists where the work so far departs from
this plan. **Requires Phase 7 done** before Task 2, Step 7: its Status line reads `done (<date>)`, both of its pull
requests are merged into `main`, and the site runs on the Pi behind Cloudflare Access.

**Goal:** The site goes public on the owner's domain. It holds the owner's real content and runs Umbraco 17.8 or
later, and every clause of the spec's definition of done has been proven on the live site first. Then the
replatform's scaffolding is retired, and the README, CLAUDE.md and memory are rewritten for a site that is live. The
gate: public.

**Architecture:** Three pull requests to `main`, each of which the Pi deploys by itself:
1. **Ready for the public** (Tasks 1 and 2):
   - production's robots.txt lets crawlers in and names the sitemap;
   - the sitemap answers HEAD;
   - the antiforgery cookie is Secure over HTTPS.

   The smoke test checks all three through the local edge. It is the first change the Pi deploys on its own, days
   before launch, while the pre-launch Access application still hides the site.
2. **Umbraco 17.8** (Task 3), rehearsed first on a copy of production restored from the bucket.
3. **Close the replatform** (Tasks 7, 8 and 10):
   - CI stops listening for `replatform`;
   - the README, CLAUDE.md and runbook are rewritten for the live site, and the spec is corrected;
   - this plan records its findings.

Between them the owner authors the real content in the production backoffice (Task 4), then runs the definition of
done on the live site while Access still hides it (Task 5). Going public (Task 6) is one act: after a restore drill of
the real content, the owner deletes the `KCC pre-launch` Access application. `/umbraco` stays behind Access for good.

**Tech Stack:** Umbraco.Cms 17.8.x (from 17.7.0), uSync 17, ASP.NET Core's antiforgery options, SimpleMvcSitemap,
RobotsTxtCore; Docker Compose and Buildx bake; the Pi's `kcc-deploy` and `kcc-backup` timers; Cloudflare Access;
GitHub Actions and `gh`. Tests: TUnit 1.27 with `Microsoft.AspNetCore.Mvc.Testing`, Vitest, and `deploy/smoke-test.sh`.

**Spec:** `docs/replatform/specs/2026-09-21-replatform-off-xperience.md`. Sections:
- §1, the definition of done, which Task 5 proves;
- §6.1, Umbraco 17.8.0 or later before launch;
- §6.3, robots.txt;
- §8, Secure cookies and Access on the backoffice;
- §11, the editing experience;
- §12, content lives in production;
- §13.3 and §13.5, Access and the restore drill before launch;
- §15, the Phase 8 row: "Real content authored in production; `RobotsTxtDenyAll` off and the whole-site Access
  application deleted; README, CLAUDE.md and memory rewritten; the reference worktree and the old SQL Server
  container retired". Its gate is "Public".

Task 8 corrects §6.3, §8, §13.3 and §15 where this phase does something else, each change with its reason. One of them
is the owner's decision of 2026-09-28: the SQL Server container stays.

## Before you start: reconcile with Phases 1–7 as built

This plan was written on 2026-09-28, before Phases 4 to 7 ran. Its code ran against Phase 7's probe tree; see "What
planning checked". Phases 4 to 7 as built may differ from their plans in the files this phase edits. Check each point
below and adapt the step that depends on it, noting the change in your task report.

```bash
git checkout main && git pull --ff-only
grep -H "Status:" docs/replatform/plans/*-phase-7-*.md
git log --oneline -8
grep -n "RobotsTxtDenyAll" src/KCC.Web/appsettings.json src/KCC.Web/appsettings.Production.json src/KCC.Web/Features/Sitemap/RobotsTxtProvider.cs
grep -n "HttpGet\|HttpHead" src/KCC.Web/Features/Sitemap/SitemapController.cs
grep -n "public async Task" tests/KCC.UnitTests/Features/Configuration/ProductionSettingsTests.cs tests/KCC.IntegrationTests/Features/Pages/SitemapTests.cs tests/KCC.IntegrationTests/Features/Hosting/TunnelTests.cs
grep -rn "AntiforgeryOptions\|SecurePolicy" src/
grep -n "^using\|public void Compose\|AddRateLimiter" src/KCC.Web/Features/Security/SecurityComposer.cs
grep -n 'pass "' deploy/smoke-test.sh
grep -n 'Include="Umbraco\|Include="uSync' Directory.Packages.props
grep -n '"@umbraco-cms/backoffice"' src/KCC.Admin/Client/package.json src/KCC.Contributions/Client/package.json
gh pr list --repo Th3FenrisWolf/Kitchen-Command-Center --author app/dependabot
grep -n "branches:\|node-version" .github/workflows/build-and-test.yml
grep -n "^## \|^5\. \*\*Access" docs/hosting/runbook.md
grep -n "^#" README.md CLAUDE.md
git worktree list
docker ps -a --format '{{.Names}} {{.Status}}' | grep mssql2022
gh auth status && gh secret list --repo Th3FenrisWolf/Kitchen-Command-Center
ls ~/.claude/projects/-Users-twinright-Repos-Kitchen-Command-Center/memory/
echo "${FONTAWESOME_NPM_AUTH_TOKEN:+token set} ${KCC_E2E_MEMBER_USERNAME:+member set}"
docker version --format '{{.Server.Version}}' && lsof -nP -iTCP:8443 -sTCP:LISTEN
```

Expected, and what to do if not:

1. Phase 7's Status is `done`, and `main`'s log shows its two squash merges, "Host the Site on a Raspberry Pi Behind
   Cloudflare" and "Close Replatform Phase 7". If not, stop: this phase builds on the running site.
2. The owner confirms three things:
   - the site runs on the Pi, and `https://<KCC_HOST>` asks for an Access PIN;
   - Access has two applications, `KCC backoffice` (path `umbraco`) and `KCC pre-launch` (the whole host);
   - healthchecks.io's `kcc-backup` check is green.

   The owner also gives the agent the public hostname. It is not a secret, and Phase 7's findings record it. Every
   `<KCC_HOST>` below means it, without `https://`.
3. `appsettings.json` has `"RobotsTxtDenyAll": true`, `appsettings.Production.json` has no such line, and
   `RobotsTxtProvider` reads it with `GetValue("RobotsTxtDenyAll", true)`.
4. `SitemapController` has `[HttpGet("sitemap.xml")]` and no `[HttpHead]`.
5. `ProductionSettingsTests` has Phase 7's three tests. `SitemapTests` has `RobotsTxt_DeniesEverythingUntilLaunch`.
   `TunnelTests` has Phase 7's four test methods (six tests, with the arguments) and uses `UmbracoSite.PeerHeader` and
   `UmbracoSite.TunnelAddress`.
6. Nothing under `src/` configures `AntiforgeryOptions` or a `SecurePolicy`. `SecurityComposer` (Phase 4) has
   `Compose(IUmbracoBuilder builder)`, which registers the rate limiter; Task 2 adds one line at its top. If the file
   is missing, stop and ask the owner where Phase 4 put its security settings.
7. The smoke test has 11 `pass` calls (one sits inside a `case`), and the fourth is
   `pass "robots.txt, the sitemap and the backoffice load"`. Tasks 1 and 2 insert their checks after that line.
8. `Umbraco.Cms` and every other `Umbraco.Cms.*` entry share one 17.x version, 17.7.0 unless something moved it;
   `uSync` is 17.x; both clients' `@umbraco-cms/backoffice` match `Umbraco.Cms`. If Dependabot has opened a pull
   request that moves `Umbraco.*` or `uSync`, ask the owner to leave it unmerged until Task 3, which rehearses the
   upgrade on a copy of production first. If `main` already runs 17.8.0 or later, Task 3 says which steps remain.
9. The workflow runs for pull requests and pushes on `[main, replatform]`, and `setup-node` uses `node-version: "22"`,
   while the images run Node 24. Task 7 changes both.
10. The runbook has sections 1 to 11, and its section 4 has step 5, "**Access for the whole site, until launch.**".
11. README.md has the sections Task 8, Step 2 keeps as they stand (Members, Backoffice extensions, E2E tests,
    Deployment), and CLAUDE.md the ones Step 3 keeps (SQLite writes, Backoffice extensions, Vue SFC `<style>` blocks,
    Loose Leaf design language). If one is missing, its phase never added it: ask the owner before writing it from
    this plan's text alone.
12. `git worktree list` shows the main checkout and `../Kitchen-Command-Center-xperience`, detached at `d9bcb09`
    (tag `xperience-final`). Other worktrees are not this phase's business.
13. `mssql2022` exists, running or stopped. It stays (see Global Constraints).
14. `gh` is signed in as `Th3FenrisWolf`, and the Actions secrets are `FONTAWESOME_NPM_AUTH_TOKEN`,
    `KCC_E2E_MEMBER_USERNAME`, `KCC_E2E_MEMBER_PASSWORD`, `KENTICO_HASH_STRING_SALT` and `KENTICO_LICENSE_KEY`.
15. The memory folder holds the files Task 9's table names, plus any that Phases 4 to 7 added.
16. The Font Awesome token and the E2E member are set, Docker 28 or later runs, and nothing listens on 8443 (the
    smoke test's local edge).

`/Users/twinright/Repos/Kitchen-Command-Center/.superpowers/phase-8-planning/`, in the main checkout, holds the probe's
scripts, the files it ran (`task1/`, `task2/`), its logs and its captures. Read it if a step's outcome surprises you.
It is gitignored, so a worktree does not have it, and it is never committed.

## What planning checked

**The code, in Phase 7's probe tree:** a clone of `replatform-phase-3` at `6a1cbcb` with Phase 7's final files on top
(`.superpowers/phase-7-planning/final-tree/`), on Umbraco 17.7.0 and Docker Desktop on an arm64 Mac. Tasks 1 and 2 ran
there as written below, on 2026-09-28, except that Task 2's line sat in `Program.cs`: the tree has no Phase 4 yet.
- `Production_LetsSearchEnginesIn` failed with `KeyNotFoundException` (`total: 4`, `failed: 1`), then passed once
  `appsettings.Production.json` had `"RobotsTxtDenyAll": false`.
- `Sitemap_AnswersHead` failed with `Expected to be equal to OK`, because the sitemap answered HEAD with 404. It
  passed with `[HttpHead("sitemap.xml")]`. robots.txt and home already answered HEAD with 200.
- `HttpsPages_MarkTheAntiforgeryCookieSecure` failed with `Expected to be true`, then passed with the antiforgery
  cookie's `SecurePolicy` set to `SameAsRequest`. `PlainHttpPages_LeaveTheAntiforgeryCookieUnmarked` passed both
  times, and `TunnelTests` ended 8 of 8.
- With the images built by `docker buildx bake --load`, the smoke test failed at `not ok - robots.txt turns every
  crawler away` against an app image without the robots setting. With Tasks 1 and 2 in, it passed all 14 checks, in
  about 30 seconds. `shellcheck -s sh` had nothing to say.

**What production serves**, captured through the local edge on a fresh install:
- robots.txt, with `cache-control: max-age=3600`:

  ```text
  User-agent: *
  Disallow: /umbraco
  Disallow: /api
  Disallow: /account
  Disallow: /error

  Sitemap: https://localhost:8443/sitemap.xml
  ```

- sitemap.xml lists `https://localhost:8443/` and `https://localhost:8443/recipes/`. SimpleMvcSitemap turns Umbraco's
  relative URLs into absolute ones from the request's scheme and host, so the addresses are `https` only because the
  tunnel's forwarded headers say so.
- Home's `Set-Cookie`, before Task 2: `.AspNetCore.Antiforgery.…; path=/; samesite=strict; httponly`, with no
  `secure`. ASP.NET Core defaults the antiforgery cookie to `CookieSecurePolicy.None` on purpose, and Umbraco's
  `AddAntiforgery()` keeps that. Umbraco's own cookies, read from their options, are fine:
  - the backoffice's `UMB_UCONTEXT` is always Secure;
  - the member cookie, `.AspNetCore.Identity.Application`, follows the request's scheme, so behind the tunnel it is
    Secure.

**Umbraco 17.8, on 2026-09-28.** NuGet's newest 17.x was 17.7.0, with no 17.8 release candidate yet; uSync's was
17.4.2, and npm's `@umbraco-cms/backoffice` was 17.7.0 (its `latest` tag is 18.2.0, a major the constraints forbid).
17.8.0's SQLite change is umbraco/Umbraco-CMS#23960: it strips `Cache=Shared` from the connection string at startup,
and logs "Shared-cache mode was removed from the SQLite connection string" when it does. This site's connection string
has never had it, so the log line never appears and nothing changes. Launch waits for 17.8 anyway, as spec §6.1
decided.

**Cloudflare.** By default Cloudflare's edge caches a site's robots.txt, honouring its `max-age` (Cloudflare's "Default
cache behavior" page); it does not cache sitemap.xml or HTML. Task 1's robots.txt deploys days before launch, so any
copy the edge still holds when Task 6 goes public is the open one. Cloudflare's managed robots.txt, if the owner turned
it on when adding the domain, prepends Cloudflare's own rules for AI crawlers. So the checks from outside look for the
app's lines rather than for the absence of `Disallow: /`.

**The machine.** The reference worktree `../Kitchen-Command-Center-xperience` is clean: no changes, no ignored files,
16 MB. The tag `xperience-final` is on origin. `mssql2022` has no volume: its database files, KCC's Xperience database
and other projects', live in the container's 39 GB writable layer, so `docker rm` would destroy them. KCC.Web's
user-secrets id is the same in both versions (`85272aa4-…`), so the tag reads its `ConnectionStrings:CMSConnectionString`
from the same store as today's site.

## Global Constraints

Phases 1 to 7's constraints still apply:

- The replatform's spec, phase plans and reference set are tracked in `docs/replatform/`: commit changes to them (a
  status line, a correction) with the work they describe. Everything else under `.superpowers/` stays gitignored:
  never stage anything there. Commit messages are Title Case imperative with no attribution lines. Ask the owner before
  any `git push`.
- **Umbraco.Cms 17.x, never 18.** Every `Umbraco.Cms*` package takes the same version as `Umbraco.Cms`, and so does
  `@umbraco-cms/backoffice` in both Lit clients.
- **The SQLite connection string never contains `Cache=Shared`.**
- **`ModelsMode`** is `SourceCodeManual` in Development and `Nothing` everywhere else. Production refuses to boot
  otherwise.
- **Only APIs that survive Umbraco 18.** Warnings are errors, so an obsolete member fails the build.
- **Nullable reference types and StyleCop.**
  - Nullable is disabled in `KCC.Web`, `KCC.Contributions`, `KCC.Admin` and `KCC.UnitTests`. Never write `?` on a
    reference type there: it raises CS8632.
  - `KCC.IntegrationTests` and `KCC.E2ETests` have nullable enabled.
  - StyleCop runs on `src/KCC.Web`. It requires sorted usings, trailing commas in multi-line initializers, and static
    members before instance members.
- **Code comments** follow `~/.claude/CLAUDE.md`: explain *why* only, with no narration and no future promises. The
  same goes for comments in shell and YAML.
- **Never name a file or folder `icon`, or anything starting with `backup`.** `.gitignore`'s `Icon` line and its Visual
  Studio `Backup*/` line ignore them without a word, because `core.ignorecase` is on. Check a new path with
  `git check-ignore -v <path>`.
- **`compose.yaml` never publishes a host port,** and images are built only by `docker buildx bake` at the repository
  root, never on the Pi. Secrets never enter git.
- **Test commands:**
  - unit: `dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj`
  - integration: `dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`
  - E2E: `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj`
  - one class: append `-- --treenode-filter "/*/*/<ClassName>/*"`
  - front end: `cd src/KCC.Web && yarn test`, then `yarn type-check`
  - everything: `node tests/scripts/run.mjs` (it opens its HTML report when it finishes)
  - the images: `docker buildx bake --load`, then `deploy/smoke-test.sh`
- **Build from the repository root.** Run `dotnet build KitchenCommandCenter.sln` and the root `yarn build:all` before
  the integration and E2E suites: the integration host refuses to start without the Vite manifest.

Phase 8's own constraints:

- **`main` is production.** A merge that changes an image deploys to the Pi within five minutes of CI pushing it,
  after a snapshot of the database. The owner approves every push, pull request and merge. Each pull request comes from
  a short branch off `main`: `ready-for-public` (Tasks 1 and 2), `umbraco-17-8` (Task 3) and `launch-close` (Tasks 7,
  8 and 10). Every pull request on `main` is squash-merged.
- **Agents never act in the owner's accounts or on the live site.** Cloudflare, GitHub's settings, healthchecks.io, the
  Pi and the production backoffice are the owner's. The agent guides, and checks from outside with `curl`, `gh` and a
  browser. It never signs in to the live site or its backoffice, and never asks for, reads or pastes a secret.
- **Keep the Xperience version runnable** (the owner's decision, 2026-09-28). The `mssql2022` container stays, and so
  does the Kitchen Command Center database in it: the container also serves other projects. KCC.Web's Kentico
  user-secrets stay too, because the `xperience-final` tag reads them from the same store. Never remove, prune or reset
  any of them. Only the reference worktree goes, and `git worktree add` brings it back in one command.
- **Nothing is public until Task 6, Step 3.** Until then the `KCC pre-launch` Access application stays, and the
  acceptance run's test data never shows outside Access.
- **Launch waits for Umbraco 17.8.0 or later on the Pi** (spec §6.1).

## Not in Phase 8

- **Not built, on purpose:**
  - an uptime monitor for the public site (spec §13.6 names the container health checks, the backup ping and the log
    viewer, and §18 accepts no uptime promise);
  - email of any kind, self-serve password reset and account deletion (spec §8);
  - amd64 images and the VPS move, which the runbook's section 10 describes for when it is needed;
  - renaming `Features/Widgets`, whose Card and Stacker components serve Home's blocks: churn with no change in
    behaviour.
- **Later:** the move to Umbraco 21 LTS, due 2027-12-09, before 17's security fixes end on 2028-11-27 (spec §6.1).
- **Their own tasks, if still open:** the corner washes that never render (the `kcc-wash-min-radius-bug` memory), and
  the "15 min or more" time filter that drops longer recipes (spec §17).
- **Left in place:** `docs/replatform/`, the branches the phases ran on (Phase 6 kept `replatform` on origin), and the
  planning scratch in `.superpowers/`.

## File map

| Path | Change | Task |
|---|---|---|
| `tests/KCC.UnitTests/Features/Configuration/ProductionSettingsTests.cs` | Modify | 1 |
| `src/KCC.Web/appsettings.Production.json` | Modify | 1 |
| `tests/KCC.IntegrationTests/Features/Pages/SitemapTests.cs` | Modify | 1 |
| `src/KCC.Web/Features/Sitemap/SitemapController.cs` | Modify | 1 |
| `deploy/smoke-test.sh` | Modify | 1, 2 |
| `tests/KCC.IntegrationTests/Features/Hosting/TunnelTests.cs` | Modify | 2 |
| `src/KCC.Web/Features/Security/SecurityComposer.cs` | Modify | 2 |
| `Directory.Packages.props`, `src/KCC.Admin/Client/package.json`, `src/KCC.Contributions/Client/package.json`, `yarn.lock` | Modify | 3 |
| `.github/workflows/build-and-test.yml` | Modify | 7 |
| `README.md`, `CLAUDE.md` | Rewrite | 8 |
| `docs/hosting/runbook.md`, `docs/brand/kit.md`, the spec (§6.3, §8, §13.3, §15) | Modify | 8 |
| `.vscode/settings.json`, and comments that still speak of phases, launch or Xperience (Task 8's sweep) | Modify | 8 |
| Memory, outside the repository | Rewrite | 9 |
| This plan (its Status line, and "Findings from Phase 8"), the spec's Status line | Modify | 10 |

---

### Task 1: Let search engines in

Spec §6.3: "`RobotsTxtDenyAll` stays on until launch; when off, robots.txt disallows `/umbraco`, `/api`, `/account`
and `/error` and points at the sitemap." Production turns it off in `appsettings.Production.json`. Every other
environment keeps `appsettings.json`'s deny-all, because only the live site should ever be indexed: development, the
test sites, the smoke stack and the restore drills should not.

The change ships now, days before launch. Behind the `KCC pre-launch` Access application no crawler can read it, and it
is the first change the Pi deploys by itself.

Two more things matter to crawlers, and planning checked both:
- **`HEAD /sitemap.xml` answers 404,** because an attribute route answers only the verbs it names. robots.txt and the
  pages already answer HEAD.
- **The sitemap's addresses must be `https://<KCC_HOST>/…`.** SimpleMvcSitemap builds them from the request's scheme
  and host, which only the tunnel's forwarded headers make `https`. The smoke test checks this through the local edge,
  which forwards the way the tunnel does.

**Files:**
- Modify: `tests/KCC.UnitTests/Features/Configuration/ProductionSettingsTests.cs`
- Modify: `src/KCC.Web/appsettings.Production.json`
- Modify: `tests/KCC.IntegrationTests/Features/Pages/SitemapTests.cs`
- Modify: `src/KCC.Web/Features/Sitemap/SitemapController.cs`
- Modify: `deploy/smoke-test.sh`

**Interfaces:**
- Consumes: Phase 7's `WebAppSettings.Load(string fileName)` and `ProductionSettingsTests`; the smoke test's `pass` and
  `fail` helpers and its local edge on `https://localhost:8443`.
- Produces: `RobotsTxtDenyAll` false in Production; `SitemapController.Index` on GET and HEAD; two smoke checks, "robots.txt
  lets crawlers in and names the sitemap" and "the sitemap lists the site's pages at https addresses". Task 2 adds its
  check right after them.

- [ ] **Step 1: Branch**

```bash
git checkout main && git pull --ff-only && git checkout -b ready-for-public
```

- [ ] **Step 2: Write the failing unit test**

In `tests/KCC.UnitTests/Features/Configuration/ProductionSettingsTests.cs`, after `Production_NeverInstallsASiteByItself`
and before the `Cms()` helper, add:

```csharp
    // Every other environment keeps appsettings.json's deny-all robots.txt: only the live site is meant to be indexed.
    [Test]
    public async Task Production_LetsSearchEnginesIn()
    {
        var denyAll = WebAppSettings.Load("appsettings.Production.json").GetProperty("RobotsTxtDenyAll").GetBoolean();

        _ = await Assert.That(denyAll).IsFalse();
    }

```

- [ ] **Step 3: Run it to see it fail**

```bash
dotnet build tests/KCC.UnitTests/KCC.UnitTests.csproj
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj --no-build -- --treenode-filter "/*/*/ProductionSettingsTests/*"
```

Expected: `total: 4`, `failed: 1`. `Production_LetsSearchEnginesIn` fails with `KeyNotFoundException: The given key was
not present in the dictionary.`

- [ ] **Step 4: Write the failing integration test, and rename the robots test**

In `tests/KCC.IntegrationTests/Features/Pages/SitemapTests.cs`:

1. Add `using System.Net;` above `using KCC.IntegrationTests.Config;`.
2. Before `RobotsTxt_DeniesEverythingUntilLaunch`, add:

   ```csharp
       // An attribute route answers only the verbs it names, and link checkers and some crawlers ask with HEAD.
       [Test]
       public async Task Sitemap_AnswersHead()
       {
           using var client = Site.CreateClient();
           using var request = new HttpRequestMessage(HttpMethod.Head, "/sitemap.xml");
           using var response = await client.SendAsync(request);

           _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
       }

   ```

3. Rename `RobotsTxt_DeniesEverythingUntilLaunch` to `RobotsTxt_DeniesEverythingOutsideProduction`. The test runs in
   the Testing environment, which keeps the deny-all after launch as well; its body stays as it is.

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj --no-build -- --treenode-filter "/*/*/SitemapTests/*"
```

Expected: `total: 4`, `failed: 1`. `Sitemap_AnswersHead` fails with `Expected to be equal to OK`: the sitemap answered
HEAD with `NotFound`.

- [ ] **Step 5: Write the failing smoke checks**

In `deploy/smoke-test.sh`, directly after the line `pass "robots.txt, the sitemap and the backoffice load"`, insert:

```sh
robots=$(curl -fsSk https://localhost:8443/robots.txt | tr -d '\r')
if printf '%s\n' "$robots" | grep -qx 'Disallow: /' || ! printf '%s\n' "$robots" | grep -qx 'Disallow: /umbraco'; then
    fail "robots.txt turns every crawler away"
fi
printf '%s\n' "$robots" | grep -qx 'Sitemap: https://localhost:8443/sitemap.xml' || fail "robots.txt names the wrong sitemap"
pass "robots.txt lets crawlers in and names the sitemap"
locs=$(curl -fsSk https://localhost:8443/sitemap.xml | grep -oE '<loc>[^<]*</loc>' | sed -e 's#<loc>##' -e 's#</loc>##')
[ -n "$locs" ] || fail "the sitemap lists no pages"
for loc in $locs; do
    case "$loc" in
        https://localhost:8443/*) ;;
        *) fail "the sitemap lists $loc, which is not an https address on the site" ;;
    esac
done
pass "the sitemap lists the site's pages at https addresses"
```

The local edge sends the browser's `Host`, `localhost:8443`, on to the app, as the tunnel sends `<KCC_HOST>`. So the
sitemap line and the addresses carry the port. A fresh install's sitemap lists Home and the recipe listing.

```bash
docker buildx bake --load
deploy/smoke-test.sh
```

Expected: the first four `ok` lines, then `not ok - robots.txt turns every crawler away`, and exit 1. The smoke test
removes its containers and volumes either way.

- [ ] **Step 6: Turn the deny-all off in production, and answer HEAD**

In `src/KCC.Web/appsettings.Production.json`, add `"RobotsTxtDenyAll": false` as the last top-level property. As Phase 7
wrote the file, it becomes:

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
  },
  "RobotsTxtDenyAll": false
}
```

Keep anything else later phases added to the file.

In `src/KCC.Web/Features/Sitemap/SitemapController.cs`, add `[HttpHead("sitemap.xml")]` directly below
`[HttpGet("sitemap.xml")]`:

```csharp
    [HttpGet("sitemap.xml")]
    [HttpHead("sitemap.xml")]
    public IActionResult Index()
```

Kestrel sends no body in answer to HEAD, so the action needs no branch for it.

- [ ] **Step 7: Run everything this task touched to see it pass**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj --no-build
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj --no-build -- --treenode-filter "/*/*/SitemapTests/*"
docker buildx bake --load
deploy/smoke-test.sh
```

Expected:
- the build has 0 warnings;
- the whole unit suite is green, 1 more than before;
- `SitemapTests` is `total: 4`, `failed: 0`;
- the smoke test prints 13 `ok` lines, the fifth `ok - robots.txt lets crawlers in and names the sitemap` and the
  sixth `ok - the sitemap lists the site's pages at https addresses`, and exits 0.

- [ ] **Step 8: Commit**

```bash
git add tests/KCC.UnitTests/Features/Configuration/ProductionSettingsTests.cs src/KCC.Web/appsettings.Production.json \
  tests/KCC.IntegrationTests/Features/Pages/SitemapTests.cs src/KCC.Web/Features/Sitemap/SitemapController.cs \
  deploy/smoke-test.sh
git commit -m "Let Search Engines In on the Live Site"
```

---

### Task 2: Mark the antiforgery cookie Secure over HTTPS

Spec §8: "Forwarded headers are trusted from the tunnel container alone, so cookies are `Secure` and generated URLs
are `https`." Umbraco's own cookies meet that:
- the backoffice's `UMB_UCONTEXT` is always Secure;
- the member cookie follows the request's scheme, and behind the tunnel every request is HTTPS.

The antiforgery cookie, which every page sets, does not. ASP.NET Core defaults it to `CookieSecurePolicy.None` on
purpose, and Umbraco's `AddAntiforgery()` keeps that default. Through the local edge, over HTTPS, the probe got
`.AspNetCore.Antiforgery.…; path=/; samesite=strict; httponly`.

`SameAsRequest` marks it Secure whenever the request is HTTPS. It leaves it unmarked on the E2E site's plain HTTP,
where a browser would drop a Secure cookie and every form would fail.

**Files:**
- Modify: `tests/KCC.IntegrationTests/Features/Hosting/TunnelTests.cs`
- Modify: `deploy/smoke-test.sh`
- Modify: `src/KCC.Web/Features/Security/SecurityComposer.cs`

**Interfaces:**
- Consumes: `UmbracoSite.PeerHeader` and `UmbracoSite.TunnelAddress` (Phase 7), which make a test request arrive from
  the tunnel; Phase 4's `SecurityComposer`.
- Produces: `AntiforgeryOptions.Cookie.SecurePolicy` = `SameAsRequest`; the smoke check "every cookie home sets over
  HTTPS is Secure".

- [ ] **Step 1: Write the failing tests**

In `tests/KCC.IntegrationTests/Features/Hosting/TunnelTests.cs`:

1. Add `using Microsoft.Net.Http.Headers;` below `using KCC.IntegrationTests.Config;`.
2. After `PlainHttpResponses_CarryNoStrictTransportSecurity`, add:

   ```csharp
       // Umbraco's member cookie follows the request's scheme, and its backoffice cookie is always Secure. ASP.NET Core
       // leaves the antiforgery cookie unmarked unless told otherwise.
       [Test]
       public async Task HttpsPages_MarkTheAntiforgeryCookieSecure()
       {
           var cookie = await AntiforgeryCookieAsync(forwardedAsHttps: true);

           _ = await Assert.That(cookie.Secure).IsTrue();
       }

       // The E2E site runs over plain HTTP, where a browser would drop a Secure cookie and every form would fail.
       [Test]
       public async Task PlainHttpPages_LeaveTheAntiforgeryCookieUnmarked()
       {
           var cookie = await AntiforgeryCookieAsync(forwardedAsHttps: false);

           _ = await Assert.That(cookie.Secure).IsFalse();
       }

   ```

3. After the `AuthorizeAsync` helper, before the class's closing brace, add:

   ```csharp

       private async Task<SetCookieHeaderValue> AntiforgeryCookieAsync(bool forwardedAsHttps)
       {
           using var client = Site.CreateClient();
           using var request = new HttpRequestMessage(HttpMethod.Get, "/");
           if (forwardedAsHttps)
           {
               request.Headers.Add(UmbracoSite.PeerHeader, UmbracoSite.TunnelAddress);
               request.Headers.Add("X-Forwarded-Proto", "https");
           }

           using var response = await client.SendAsync(request);
           return SetCookieHeaderValue.ParseList(response.Headers.GetValues("Set-Cookie").ToList())
               .Single(cookie => cookie.Name.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal));
       }
   ```

Each test uses a fresh client, which holds no cookie yet, so every page render issues a new one. `SetCookieHeaderValue`
reads the attributes, so a token that happens to contain "secure" cannot pass for the flag.

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj --no-build -- --treenode-filter "/*/*/TunnelTests/*"
```

Expected: `total: 8`, `failed: 1`. `HttpsPages_MarkTheAntiforgeryCookieSecure` fails with `Expected to be true`.

- [ ] **Step 2: Write the failing smoke check**

In `deploy/smoke-test.sh`, directly after the line `pass "the sitemap lists the site's pages at https addresses"`,
insert:

```sh
cookies=$(curl -fsSk -o /dev/null -D - https://localhost:8443/ | tr -d '\r' | grep -i '^set-cookie:') ||
    fail "home sets no cookie"
if printf '%s\n' "$cookies" | grep -qiv '; secure'; then
    fail "home sets a cookie without Secure over HTTPS"
fi
pass "every cookie home sets over HTTPS is Secure"
```

ASP.NET Core writes the flag as `; secure`, so a line without it is a cookie without it.

```bash
docker buildx bake --load
deploy/smoke-test.sh
```

Expected: six `ok` lines, then `not ok - home sets a cookie without Secure over HTTPS`, and exit 1.

- [ ] **Step 3: Follow the request's scheme**

In `src/KCC.Web/Features/Security/SecurityComposer.cs`:

1. Add `using Microsoft.AspNetCore.Antiforgery;` above `using Microsoft.AspNetCore.RateLimiting;`.
2. Make these the first lines of `Compose`:

   ```csharp
           // ASP.NET Core never marks the antiforgery cookie Secure by default. Following the request's scheme, as
           // Umbraco's member cookie does, marks it over the tunnel's HTTPS and keeps it on the E2E site's plain HTTP.
           builder.Services.Configure<AntiforgeryOptions>(options => options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest);

   ```

`CookieSecurePolicy` comes from `Microsoft.AspNetCore.Http`, one of the web SDK's implicit usings, as `StatusCodes`
does in the same method. The probe put the line in `Program.cs`, which had no `SecurityComposer` yet. The options are
the same wherever they are configured.

- [ ] **Step 4: Run them to see them pass**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj --no-build -- --treenode-filter "/*/*/TunnelTests/*"
docker buildx bake --load
deploy/smoke-test.sh
```

Expected:
- 0 warnings;
- `TunnelTests` is `total: 8`, `failed: 0`;
- the smoke test prints 14 `ok` lines, the seventh `ok - every cookie home sets over HTTPS is Secure`, and exits 0.

- [ ] **Step 5: Commit**

```bash
git add tests/KCC.IntegrationTests/Features/Hosting/TunnelTests.cs deploy/smoke-test.sh \
  src/KCC.Web/Features/Security/SecurityComposer.cs
git commit -m "Mark the Antiforgery Cookie Secure over HTTPS"
```

- [ ] **Step 6: Everything, once more**

From the repository root:

```bash
dotnet build KitchenCommandCenter.sln
yarn build:all
(cd src/KCC.Web && yarn test && yarn type-check)
node tests/scripts/run.mjs
```

Expected: the build has 0 warnings and every bundle builds. The combined run is green: unit 1 more than before this
phase, integration 3 more, E2E unchanged; it opens its HTML report when it finishes. The E2E suite exercises every
form over plain HTTP, which is what `PlainHttpPages_LeaveTheAntiforgeryCookieUnmarked` protects.

- [ ] **Step 7: Push, and open the pull request (the owner approves each)**

Ask the owner, then:

```bash
git push -u origin ready-for-public
```

Write the body to `.superpowers/phase-8/pr-ready-for-public.md`:

```markdown
Readies the live site for the public, ahead of launch (replatform Phase 8, Tasks 1 and 2;
`docs/replatform/plans/2026-09-28-phase-8-launch.md`).

- Production's robots.txt lets crawlers in: it disallows `/umbraco`, `/api`, `/account` and `/error` and names the
  sitemap. Every other environment keeps the deny-all. Until launch the pre-launch Access application still hides the
  site, so no crawler can read it yet.
- The sitemap answers HEAD as well as GET.
- The antiforgery cookie is Secure over HTTPS, as Umbraco's member and backoffice cookies already are. It stays
  unmarked over the E2E site's plain HTTP.
- The smoke test checks all three through the local edge: robots.txt, the sitemap's https addresses, and every cookie
  home sets.

Test plan: the combined suite is green (unit <count>, integration <count>, E2E <count>, Vitest <count>); the smoke
test passes its 14 checks locally and in this pull request's `images` job.
```

Fill in the counts from Step 6. Then, with the owner's approval:

```bash
gh pr create --repo Th3FenrisWolf/Kitchen-Command-Center --base main --head ready-for-public \
  --title "Ready the Site for the Public" --body-file .superpowers/phase-8/pr-ready-for-public.md
```

Give the owner the pull request's link. Both jobs must go green (`gh pr checks`).

- [ ] **Step 8: The owner merges it, and the Pi deploys it by itself**

This is the first image the Pi deploys on its own (Phase 7, Task 11, Step 4). When CI is green, the owner merges. The
push to `main` runs the workflow again, and its `images` job pushes the three images:

```bash
git checkout main && git pull --ff-only
gh run list --repo Th3FenrisWolf/Kitchen-Command-Center --branch main --limit 1
```

Expected: the newest run succeeded, with both jobs green. Within five minutes of the push, `kcc-deploy` takes a
snapshot and recreates what changed. The owner checks on the Pi:

```bash
journalctl -u kcc-deploy --since "1 hour ago" --no-pager | grep -E "deployed|denied|error"
sudo -u kcc -H kcc ps
sudo -u kcc -H kcc run --rm --entrypoint ls backup /srv/kcc/snapshots/pre-deploy
```

Expected:
- one `deployed ghcr.io/th3fenriswolf/kcc-app:main sha256:…` line, and no `denied` or `error`;
- `app`, `ssr` and `cloudflared` healthy;
- a snapshot named for the minutes before the deploy.

Then, in a browser past Access, the owner opens `https://<KCC_HOST>/robots.txt` and `https://<KCC_HOST>/sitemap.xml`:
- robots.txt ends with the four `Disallow:` lines and `Sitemap: https://<KCC_HOST>/sitemap.xml`;
- every `<loc>` in the sitemap starts with `https://<KCC_HOST>/`.

If Cloudflare's managed robots.txt is on, its rules for AI crawlers come first; they are Cloudflare's, and Task 6
says what to do about them. Note the minutes from the merge to the `deployed` line, for the findings. If no `deployed`
line appears within ten minutes of the `images` job finishing, the runbook's section 11 covers the causes: a
`denied` pull means an expired token.

---

### Task 3: Upgrade Umbraco to 17.8

Spec §6.1: "Umbraco.Cms 17.x, minimum 17.8.0 (scheduled 2026-10-29) … launch waits for 17.8." For this site the fix
itself changes nothing: 17.8.0 strips `Cache=Shared` from a SQLite connection string at startup, and this site's has
never had it. The wait was decided anyway, and 17.8 is also the version the launch runs on.

The Pi migrates its database when the new image first starts, and a migrated database cannot go back to 17.7. So the
upgrade is rehearsed first on a copy of production, restored from the bucket on the owner's Mac. The pre-deploy
snapshot and the runbook's rollback remain the way back if the Pi still fails.

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `src/KCC.Admin/Client/package.json`, `src/KCC.Contributions/Client/package.json`, `yarn.lock`
- Modify, only if the build says so: code that uses an API 17.8 marks obsolete

**Interfaces:**
- Consumes: the images and the drill from Phase 7 (`deploy/compose.yaml`, `deploy/local.yaml`, runbook §9); the Pi's
  `kcc-deploy` timer; `UpgradeUnattended`, true in `appsettings.json`.
- Produces: every `Umbraco.Cms*` package and `@umbraco-cms/backoffice` on one 17.8.x version, deployed. Task 6 waits for
  it.

If `main` already runs 17.8.0 or later, because a Dependabot pull request was merged first, the Pi has migrated its
database without a rehearsal. Do Step 9's checks. Then, if either client's `@umbraco-cms/backoffice` still differs
from `Umbraco.Cms`, run Steps 3 to 8 for that change alone.

- [ ] **Step 1: Check that 17.8 is out**

```bash
dotnet package search Umbraco.Cms --exact-match
dotnet package search uSync --exact-match
npm view @umbraco-cms/backoffice versions --json | tail -n 8
```

Each prints the published versions. Choose:
- `Umbraco.Cms`: the newest 17.x that is 17.8.0 or later, never an 18.x and never a release candidate;
- `uSync`: the newest 17.x;
- `@umbraco-cms/backoffice`: the same version as `Umbraco.Cms`.

If NuGet lists no 17.8.0 yet, stop this task. Tasks 4 and 5 go on without it, and Task 6 waits for it. Record the
three versions in the task report.

- [ ] **Step 2: Read the release notes for what this site touches**

Open `https://github.com/umbraco/Umbraco-CMS/releases/tag/release-<version>` for each release after 17.7.0, up to the
one chosen, and the uSync release notes for its version. Look for anything that changes one of these:

| Area | Where this site depends on it |
|---|---|
| SQLite, distributed locking, cache instructions | Phase 4's write-lock guards in `Features/Sqlite` |
| Content relations after a save | `SqliteComposer`, which refuses to boot if the handler it wraps has moved |
| Member sign-in, lockout, approval | Phase 4's account API and `IMemberWriteLock` |
| Block List, Tiptap, UFM labels | Home's blocks (Phase 6) |
| Backoffice extension manifests, `umbHttpClient` | The two Lit clients (Phase 5) |
| Health endpoints, Production runtime mode | The compose health check on `/umbraco/api/health/ready`; `appsettings.Production.json` |
| ModelsBuilder, uSync import | The committed models; the startup import and the create-only dictionary |

Write down each item that applies, and the step where you handle it. A breaking change with no workaround that
survives Umbraco 18 is a reason to stop and ask the owner.

- [ ] **Step 3: Branch, and move every package together**

```bash
git checkout main && git pull --ff-only && git checkout -b umbraco-17-8
grep -n 'Include="Umbraco\|Include="uSync' Directory.Packages.props
```

In `Directory.Packages.props`, set the `Version` of every `PackageVersion` whose `Include` starts with `Umbraco.Cms` to
the version Step 1 chose, and `uSync`'s to its own. In both clients' `package.json`, set `@umbraco-cms/backoffice` to
the same version as `Umbraco.Cms`, keeping the file's range style. Then:

```bash
yarn install
git diff --stat
```

Expected: `Directory.Packages.props`, the two `package.json` files and `yarn.lock` changed. In `yarn.lock` only
`@umbraco-cms/backoffice` and the packages it brings move. The generated models do not change: Phase 1 turned off
ModelsBuilder's version stamp (`IncludeVersionNumberInGeneratedModels`).

- [ ] **Step 4: Build, and fix what 17.8 made obsolete**

```bash
dotnet build KitchenCommandCenter.sln
yarn build:all
```

Expected: 0 warnings, and every bundle builds. Warnings are errors, so a member 17.8 marks obsolete fails the build
with CS0618. Replace it with the member the warning or the release notes name. Among replacements, take the one that
survives Umbraco 18: plain `ControllerBase`, `ManagementApiControllerBase`, and the async content, member and
dictionary services (spec §6.1). Rebuild until the build is clean.

- [ ] **Step 5: Run everything, the images included**

```bash
node tests/scripts/run.mjs
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj --no-build -- --treenode-filter "/*/*/SqliteConcurrencyTests/*"
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj --no-build -- --treenode-filter "/*/*/RelationsWriteLockTests/*"
docker buildx bake --load
deploy/smoke-test.sh
```

Expected:
- the combined run is green, with every suite's count as before the bump;
- the two SQLite classes pass on their own. The combined run already includes them, but these are the tests an
  upgrade is most likely to break, so read their output;
- the smoke test prints its 14 `ok` lines. A fresh install through `first-boot.env` and a restore into fresh volumes
  both boot on 17.8.

If a class names a moved handler or a lock it can no longer take, go back to Step 2's notes.

- [ ] **Step 6: Commit**

```bash
git add Directory.Packages.props src/KCC.Admin/Client/package.json src/KCC.Contributions/Client/package.json yarn.lock
git commit -m "Upgrade Umbraco to 17.8"
```

If Step 4 changed code, stage those files too, and say in the commit body which API replaced which.

- [ ] **Step 7: Rehearse the upgrade on a copy of production (the owner)**

The owner runs the restore drill (runbook §9) with this branch's images in place of `main`'s. The latest nightly backup
migrates to 17.8 on the Mac, off the Pi:

1. Recreate `~/kcc-drill/drill.env` as runbook §9, step 2 describes, with the bucket's read-only token. Keep it for
   Task 6.
2. From this branch's checkout:

   ```bash
   docker buildx bake --load
   cd deploy
   export KCC_IMAGE_TAG=local
   drill() { docker compose -p kcc-drill --env-file ~/kcc-drill/drill.env -f compose.yaml -f local.yaml "$@"; }
   drill run --rm restore latest
   drill up -d --wait
   drill logs app | grep -iE "upgrad|migrat"
   ```

3. At `https://localhost:8443`, past the local certificate's warning, check:
   - Home, and a recipe with its image;
   - `/umbraco`, where the production administrator signs in and finds the content.
4. Clean up:

   ```bash
   drill --profile jobs down -v
   unset KCC_IMAGE_TAG
   cd ..
   ```

Expected: `up --wait` returns with every service healthy. The app is healthy only at RuntimeLevel Run, which it reaches
after any migration has run. The `grep` shows Umbraco's unattended upgrade if the release brought migrations; a minor
release may bring none. If the stack never turns healthy, `drill logs --tail 200 app` says why. Stop there: the Pi must
not get these images until the cause is fixed.

- [ ] **Step 8: Push, and open the pull request (the owner approves each)**

```bash
git push -u origin umbraco-17-8
```

Write `.superpowers/phase-8/pr-umbraco-17-8.md`:

```markdown
Moves Umbraco from 17.7.0 to <version>, with uSync <version> and `@umbraco-cms/backoffice` <version> (replatform
Phase 8, Task 3; spec §6.1). Launch waits for 17.8.

- <each release-note item from Step 2 that applied, and what it needed; or "Nothing in the release notes touches this
  site.">
- Rehearsed on a copy of production: the latest nightly backup, restored on the owner's Mac, migrated and served the
  site and the backoffice (<date>).

Test plan: the combined suite is green (unit <count>, integration <count>, E2E <count>, Vitest <count>); the SQLite
concurrency and relations tests pass; the smoke test passes its 14 checks locally and in this pull request's `images`
job.
```

Then, with the owner's approval:

```bash
gh pr create --repo Th3FenrisWolf/Kitchen-Command-Center --base main --head umbraco-17-8 \
  --title "Upgrade Umbraco to 17.8" --body-file .superpowers/phase-8/pr-umbraco-17-8.md
```

Both jobs must go green. If Dependabot has its own pull request for these packages, it closes it by itself once
`main` has the version.

- [ ] **Step 9: The owner merges it, and the Pi upgrades**

After the merge and the `images` job, within five minutes, `kcc-deploy` snapshots the database and recreates the app,
and the app migrates the database as it starts. The owner checks:

```bash
journalctl -u kcc-deploy --since "1 hour ago" --no-pager | grep -E "deployed|denied|error"
sudo -u kcc -H kcc ps
sudo -u kcc -H kcc exec app sh -c 'grep -o "\"Umbraco.Cms.Core/[0-9.]*\"" KCC.Web.deps.json | head -n 1'
```

Expected: a `deployed …` line; `app`, `ssr` and `cloudflared` healthy; `"Umbraco.Cms.Core/<version>"`. Past Access,
the site renders and `/umbraco` signs in.

If the app stays unhealthy, roll back as the runbook's section 8 says: pin the previous commit's SHA in `.env`, and put
back the snapshot this deploy took, because the database may already be migrated. Then find the cause on a branch,
with Step 7's rehearsal.

---

### Task 4: The real content, authored in production (the owner)

Spec §12: "Schema lives in git; content lives in the production database." Nothing moves content from a development
database to the live one, so the owner authors in the live backoffice at `https://<KCC_HOST>/umbraco`. Until Task 6,
what the owner publishes there shows only to people past the pre-launch Access application.

The agent cannot see behind Access. It answers questions, and writes down what surprised the owner, for the findings:
help text that was missing, an editor that confused, a field nobody needed.

Two things come in order: the owner's member account before the recipes that name it as their author, and the images
before the recipes that show them.

**Files:** none. The owner's notes go into Task 10's findings.

- [ ] **Step 1: The owner's member account**

In a browser past Access, open `https://<KCC_HOST>/account/login/`, switch to **Sign up**, and create an account: a
username, an email address and a password. The registration-complete page says the account is waiting for approval. Then, in the backoffice:
Content → **Contributions** → **Waiting** → **Approve**. Back on the site, sign in.

- [ ] **Step 2: Images, taxonomy, recipes and variants**

- **Images:** Media → create a folder, then upload the originals. The site serves resized copies, and Cloudflare
  takes uploads of up to 100 MB.
- **Categories and tags:** Content → **Recipe Categories** and **Recipe Tags** hold the baseline's 6 and 11. Rename,
  add or remove them to fit the recipes. Their names are the listing's filter labels, and publishing one rebuilds the
  search index within seconds. Never name one after a page under Home, such as `recipes` or `account`: taxonomy nodes
  also answer at the top of the site's URL space (Phase 1's findings).
- **Recipes:** Content → **Recipes** → **Create** → **Recipe**:
  - fill in the name, description, image, category and author (yourself);
  - choose the icon with **Suggest with AI**, which needs `ANTHROPIC_API_KEY` in the Pi's `.env`, or pick one from
    the grid;
  - then **Save and Publish**.
- **Variants:** right-click a recipe → **Create** → **Recipe Variant**, then fill in:
  - the ingredients and instructions;
  - the times, servings and nutrition;
  - the tags, images and author.

  **Save and Preview** shows the real page before **Save and Publish**.

To add the Anthropic key later: add `ANTHROPIC_API_KEY=` with its value to `/srv/kcc/repo/deploy/.env`, then run
`sudo -u kcc -H kcc up -d --wait`.

- [ ] **Step 3: Home, the navigation, and each page's metadata**

- **Home:** Content → **Home** → **Sections**. Phase 6's baseline holds placeholder text taken from the Xperience home.
  Replace it block by block, point the cards at real recipes, and set each section's background and width. **Save and
  Preview**, then **Save and Publish**.
- **Site Settings:** check every link in the main and utility navigation, and each item's **Show when** (Always,
  Signed in, Signed out).
- **Metadata:** on Home, Recipes and each recipe, the metadata tab holds the title, description and share image, and
  **Exclude from sitemap** for a page that should stay out of it.
- **Status Codes:** the 404 and 500 pages hold the heading and text a lost or unlucky visitor reads.

- [ ] **Step 4: The UI strings**

Translation → **Dictionary**. Phase 0 found four strings that are not real text yet:
- `Theme.SwitchToDark` and `Theme.SwitchToLight` are empty, so the theme toggle's label is read aloud as its key;
- `VariantDetail.SaturatedFat` is empty;
- `RecipeSearch.NoRecipesHint` reads "No Recipes Hint".

Fill in all four, and read through the rest for wording. A saved string is live at once, and no deploy ever overwrites
it.

- [ ] **Step 5: Clear out the test data, and check that the backup took the content**

Content → **Contributions** → **Waiting** lists every unapproved member and every draft. Delete whatever Phase 7's
checks or the owner's trials left behind.

The next morning, the healthchecks.io check is green: that night's archive in the bucket holds the content, and Task
6's drill restores it.

---

### Task 5: Prove the definition of done on the live site, behind Access

Spec §1: "Done means: the site is public on the owner's domain; members can sign up, be approved, sign in and
contribute; search, submissions and moderation work; the owner edits every page, recipe, nav item and UI string in the
backoffice; and a nightly backup has been restored successfully at least once."

This task proves every clause but the first on production, while Access still hides it:
- the live site behind the tunnel is where forwarded headers, Secure cookies and rate limits keyed on `CF-Connecting-IP`
  meet real browsers;
- Phases 4 and 5 proved the same flows on test sites only.

Task 6 proves the first clause, and repeats the backup clause with the real content.

The test data belongs to a throwaway member, `launch-check`, and ends up deleted. The owner's hands do it; the agent
reads out each check and records its result.

**Files:** none. The results go into Task 10's findings.

- [ ] **Step 1: A new member waits for approval**

In a private window, pass Access, open `https://<KCC_HOST>/account/login/`, switch to **Sign up**, and create the
account `launch-check`, with the email `launch-check@example.com` and a throwaway password. No email is ever sent.

Check:
- the registration-complete page says the account is waiting for approval;
- signing in now says "waiting for approval", not a generic error.

- [ ] **Step 2: Approval lets it in**

In the owner's own window: Content → **Contributions** → **Waiting** lists `launch-check`, with its email and
registration date → **Approve**. In the private window, sign in: the header shows the member's menu.

- [ ] **Step 3: Lockout**

Sign out: the menu's **Sign out** sends a form, since it is a POST. Then sign in with a wrong password five times, then
with the right one. The sixth attempt shows the lockout message.

To unlock the member, go to Members → `launch-check`, turn **Locked out** off and **Save**, or wait 15 minutes. Then
sign in again.

Sign-in, sign-up and password changes share a limit of 10 a minute per visitor. If "Too many attempts" appears, wait a
minute.

- [ ] **Step 4: Contributing**

As `launch-check`, on one of the owner's published variants:
- mark it cooked;
- write a review of 4.5 stars with a sentence;
- write a cook note.

After a reload, all three show on the page. Within a few seconds the recipe's rating on `/recipes/` includes the
review, because the index rebuilds about two seconds after a review is written.

- [ ] **Step 5: Submitting**

As `launch-check`:
- **Create Recipe:** a recipe named "Launch Check", with its first variant;
- **Add Variant**, on one of the owner's published recipes: a variant named "Launch Check Variant".

The account page lists both under pending review. Neither shows on `/recipes/` or in search.

- [ ] **Step 6: Moderating**

In the backoffice, Content → **Contributions**:
- **Reviews:** find `launch-check`'s review → **Edit**. Change it to 3 stars with new text → **Save**, and the variant
  page shows the change. Then **Delete** it; the dashboard asks first.
- **Cook notes:** **Delete** `launch-check`'s note.

- [ ] **Step 7: Publishing a submission**

**Waiting** lists "Launch Check" and "Launch Check Variant".
1. Open "Launch Check" and check its fields, the ingredients and instructions included.
2. **Save and Preview** renders the draft.
3. **Save and Publish**.

Within seconds, the recipe is on `/recipes/` and search finds it by name, with `launch-check` as its author.

- [ ] **Step 8: Editing everything**

- **A UI string:** Translation → **Dictionary** → **RecipeSearch**. Change a string the listing shows → **Save**.
  Reload `/recipes/` and it shows the new text. Change it back.
- **A nav item:** Site Settings → add a link to the main navigation → **Save and Publish**, and it shows in the
  header. Remove it.
- **A category:** rename one → **Save and Publish**, and within a few seconds the listing's filter shows the new name.
  Rename it back. This is also the check Phase 3 left with the owner.
- **A page:** on Home, change one block's text → **Save and Preview** → **Save and Publish**. Put it back, or keep it.
- **A recipe:** change a variant's servings → **Save and Publish**, and the page shows it.

- [ ] **Step 9: Clean up, in this order**

1. **The test recipe and variant.** Content → "Launch Check" → **Delete**, and the same for "Launch Check Variant". Then,
   in the **Recycle Bin**, delete each one for good. Deleting a recipe or variant permanently also deletes its
   reviews, notes and cooked marks (spec §9.1). Delete only these two in the Recycle Bin; emptying it would delete
   everything else in it too.
2. **The test member.** Members → `launch-check` → **Delete**. Deleting a member deletes their reviews, notes and
   cooked marks. The recipes go first, so no recipe is left naming a deleted author.
3. **The variant from Step 4** shows no trace of `launch-check`: no cooked mark, review or note, and its rating is what
   it was before.

- [ ] **Step 10: Record the results**

Write down each step's result. A failure is a bug on `main`: fix it on a branch, test first, through a pull request,
then run the failed step again. Task 6 needs all nine steps passed.

---

### Task 6: Go public

Spec §15's gate: "Public". The owner deletes the `KCC pre-launch` Access application, and from then on anyone reaches
the site. Before that, spec §13.5 asks for "a restore drill … a gate before launch": Phase 7 rehearsed one on an empty
site, and this one restores the real content.

**Requires:** Task 2's pull request deployed (robots.txt, the sitemap, the Secure cookie); Task 3's upgrade deployed
(spec §6.1); Task 5 passed; and the owner's go-ahead.

**Files:** none.

- [ ] **Step 1: The restore drill, with the real content (the owner)**

Run the runbook's section 9 on the owner's Mac, with `main`'s images and the latest nightly archive, which now holds
Task 4's content. Use `~/kcc-drill/drill.env` from Task 3, or recreate it. Check at `https://localhost:8443`:
- Home, a recipe and a variant, with their images;
- `/umbraco`, where the production administrator signs in, finds the content, and finds the owner's member under
  Members.

Clean up as section 9 says, then delete `~/kcc-drill`. Record the date and the archive's name.

- [ ] **Step 2: The go-ahead**

Ask the owner: "The drill passed and the content is ready. Delete the `KCC pre-launch` Access application now, and
make the site public?" Wait for a yes.

- [ ] **Step 3: Delete the pre-launch application (the owner)**

Cloudflare → Zero Trust → Access controls → **Applications** → `KCC pre-launch` → **Delete**. Leave `KCC backoffice`
exactly as it is.

- [ ] **Step 4: Check from outside, with no Access session**

```bash
H=<KCC_HOST>
curl -sS -o /dev/null -w "%{http_code}\n" "https://$H/"
curl -sS "https://$H/" | grep -c '<div id="app"><[^/]'
curl -sS -o /dev/null -w "%{http_code} %{redirect_url}\n" "https://$H/umbraco/"
curl -sS "https://$H/robots.txt" | tr -d '\r'
curl -sS "https://$H/sitemap.xml" | grep -oE '<loc>[^<]*</loc>' | head -n 5
curl -sS -I -o /dev/null -w "%{http_code}\n" "https://$H/sitemap.xml"
curl -sS "https://$H/api/recipes/search" | head -c 400; echo
curl -sS -o /dev/null -w "%{http_code} %{redirect_url}\n" "http://$H/"
curl -sS -I "https://$H/" | grep -iE "^(strict-transport-security|set-cookie):"
curl -sS -o /dev/null -w "%{http_code}\n" "https://$H/this-page-does-not-exist/"
```

Expected, line by line:
1. `200`, then `1`: Home answers without Access, rendered on the server.
2. `302` to `https://<team>.cloudflareaccess.com/cdn-cgi/access/login/…`: the backoffice still asks for Access.
3. robots.txt holds the app's lines: `Disallow:` for `/umbraco`, `/api`, `/account` and `/error`, and
   `Sitemap: https://<KCC_HOST>/sitemap.xml`.
   - If Cloudflare's managed robots.txt is on, its rules for AI crawlers come first (Step 5).
   - If it shows only `User-agent: *` and `Disallow: /`, Cloudflare's edge still holds a copy from before Task 1. Go
     to Caching → Configuration → **Purge Everything**, then check again.
4. The sitemap's addresses start with `https://<KCC_HOST>/`, and the recipes are among them. HEAD answers `200`.
5. The search API answers with JSON naming the published recipes.
6. `301` to `https://<KCC_HOST>/`, from Cloudflare's Always Use HTTPS.
7. `strict-transport-security` is present, and every `set-cookie` line carries `secure`.
8. `404`, for a page that does not exist.

- [ ] **Step 5: Cloudflare's rules for AI crawlers (the owner)**

If robots.txt begins with rules the app did not write, they are Cloudflare's managed robots.txt, offered when the
domain was added ("Instruct AI bot traffic with robots.txt"). They ask AI-training crawlers to stay away, and leave
search engines alone. Keeping or dropping them is the owner's choice, in the domain's AI crawler settings in
Cloudflare. The app's own lines follow either way.

- [ ] **Step 6: The owner's check, from a phone off the home Wi-Fi**

In both ramps (the header's toggle), with no Access prompt anywhere but `/umbraco`:
- Home;
- the listing, with a filter and a search;
- a recipe and a variant;
- signing in as the owner's member.

- [ ] **Step 7: The agent's check, in a browser**

In both ramps, the agent opens:
- `https://<KCC_HOST>/`;
- `/recipes/`, with a filter applied;
- a recipe;
- a variant;
- `/account/login/`.

On each page it reads the console: no errors, and no hydration warnings. It never signs in.

- [ ] **Step 8: Record the launch**

Record the date and time the application was deleted: it is the launch date the README, CLAUDE.md and the spec name.
Tell the owner: "Anyone can now sign up. Approve only the people you know, in **Waiting**. A member you never approve
can never sign in, and deleting one removes everything they wrote."

To take the site private again, recreate the application as the runbook's section 4 says (Task 8 writes it down).

---

### Task 7: Retire what only the Xperience version needed

Spec §15 names two things to retire at launch: the reference worktree and the old SQL Server container. The owner
decided on 2026-09-28 to keep the container, and the Xperience database in it. It also serves other projects, and
together with the tag it keeps the last Xperience version runnable. So this task retires:
- the reference worktree, which `git worktree add` recreates in one command;
- CI's trigger for `replatform`, merged in Phase 6;
- the two Kentico secrets CI stopped reading in Phase 1;
- CI's Node 22. The images build and run on Node 24, and so does development.

**Files:**
- Modify: `.github/workflows/build-and-test.yml`

- [ ] **Step 1: Branch**

```bash
git checkout main && git pull --ff-only && git checkout -b launch-close
```

Tasks 7, 8 and 10 commit here, and Task 10 opens the pull request.

- [ ] **Step 2: Remove the reference worktree**

```bash
git -C ../Kitchen-Command-Center-xperience status --short --ignored
```

Expected: no output. If it lists anything, stop and show the owner. `git worktree remove` refuses changed and untracked
files, but it would delete ignored ones, such as a local settings file, without a word.

```bash
git worktree remove ../Kitchen-Command-Center-xperience
git worktree prune
git worktree list
git ls-remote --tags origin xperience-final
```

Expected: the list no longer shows it, and origin answers with the tag and its commit, `d9bcb09…`. The README's History
section, written in Task 8, says how to bring the worktree back.

- [ ] **Step 3: Check that what runs the tag is still there**

Change nothing here:

```bash
docker ps -a --format '{{.Names}} {{.Status}}' | grep mssql2022
dotnet user-secrets list --project src/KCC.Web | cut -d= -f1 | grep -c "^ConnectionStrings:CMSConnectionString"
```

Expected: `mssql2022` with its status, then `1`. `cut` keeps only the names, so no value is printed.

- [ ] **Step 4: Delete the Kentico secrets (the owner)**

GitHub → the repository → Settings → Secrets and variables → **Actions**: delete `KENTICO_LICENSE_KEY` and
`KENTICO_HASH_STRING_SALT`. Nothing has read them since Phase 1 rebuilt CI without SQL Server, and a local run of the
tag reads its keys from user-secrets, not from these. Then:

```bash
gh secret list --repo Th3FenrisWolf/Kitchen-Command-Center
gh secret list --repo Th3FenrisWolf/Kitchen-Command-Center --app dependabot
```

Expected: both lists name `FONTAWESOME_NPM_AUTH_TOKEN`, `KCC_E2E_MEMBER_USERNAME` and `KCC_E2E_MEMBER_PASSWORD`, and
nothing else (runbook §6 made the Dependabot copies).

- [ ] **Step 5: CI builds `main` alone, on Node 24**

In `.github/workflows/build-and-test.yml`:
- under `on:`, in both `pull_request` and `push`, replace `branches: [main, replatform]` with `branches: [main]`;
- in the `build-and-test` job's **Set up Node** step, replace `node-version: "22"` with `node-version: "24"`.

```bash
docker run --rm -v "$PWD:/repo" --workdir /repo rhysd/actionlint:latest -no-color
```

Expected: no output. The pull request in Task 10 runs the workflow on Node 24 for the first time.

- [ ] **Step 6: Commit**

```bash
git add .github/workflows/build-and-test.yml
git commit -m "Build Only Main in CI, on Node 24"
```

---

### Task 8: Rewrite the docs for the live site

Spec §15: "Each phase corrects the README and CLAUDE.md lines it invalidates … Phase 8 does the full rewrite." Phases 1
to 7 corrected lines and added sections. What is left is the frame around them, which still describes something older:
- a starter kit's architecture section (`Widgets/Accordion`);
- front-end commands run from the wrong folder;
- a colour-ramp default that is wrong (it falls back to light, not dark);
- a test report counted at five suites instead of six;
- CLAUDE.md's replatform exception, written for work in progress;
- code comments that still speak of phases or of launch.

The new README is laid out for someone arriving at a live site's repository. The new CLAUDE.md gathers the invariants
that fail silently. Many of them were in memory until now, and Task 9 drops those memories.

**Files:**
- Rewrite: `README.md`, `CLAUDE.md`
- Modify: `docs/hosting/runbook.md`, `docs/brand/kit.md`, `docs/replatform/specs/2026-09-21-replatform-off-xperience.md`
- Modify: whatever Step 1's sweep finds, in `src/`, `tests/`, `.vscode/` and elsewhere

**Interfaces:**
- Consumes: `<KCC_HOST>` and the launch date (Task 6, Step 8).
- Produces: README sections Task 9's memory points at ("First run", "Content and schema", "Tests", "History").

- [ ] **Step 1: Sweep for what still speaks of the port**

```bash
git grep -n -I -i -E "kentico|xperience|page ?builder|phase [0-9]|until launch|before launch|pre-launch|replatform" \
  -- . ':!docs/replatform' ':!src/KCC.Web/uSync'
```

Fix each hit by these rules:
- **A comment that narrates the port or points at a phase,** such as "since Phase 4" or "until launch": delete it, or
  rewrite it to state the constraint that still holds, without the history.
- **A comment that explains a constraint by Xperience:** if the constraint still holds, reword it without Xperience.
  If it does not, delete the comment, then ask the owner whether the code it explained is still needed.
- **A name that speaks of a phase or of launch,** such as a test's: rename it.
- **In `.vscode/settings.json`:** remove the cSpell words `kentico` and `xperience`. Remove `bychannel`, `cmsctx`,
  `typeinfo` and `webpageitem` too, once `git grep -n -i -w -E "bychannel|cmsctx|typeinfo|webpageitem" -- src tests`
  finds nothing.
- **README.md, CLAUDE.md, the runbook and kit.md:** leave their hits to Steps 2 to 5, which rewrite them.

Known on 2026-09-28, besides those: `tests/KCC.ReferenceCapture/Program.cs` has "// Phase 2 has no sign-in until Phase
4, so an --only selection of public pages must not require credentials." Replace it with "// Capturing public pages
alone must need no sign-in credentials." Phases 4 to 7 may have left more.

Expected, once Steps 2 to 5 are done: the command lists only README.md's History section, CLAUDE.md's
`docs/replatform/` paragraph, and the runbook's line about taking the site private again.

- [ ] **Step 2: Rewrite README.md**

Replace the whole file with the text below. `<KCC_HOST>` is the hostname and `<launch date>` Task 6's date, as
`YYYY-MM-DD`.

Four sections keep their text as it stands on `main`, and only their heading level changes: **Members** (with one
paragraph added at its end), **Backoffice extensions**, **E2E tests** and **Deployment**. Phases 4, 5 and 7 wrote
them, and main's version holds whatever those phases learned. The text shown for them below is what their plans wrote.

One paragraph is conditional, and shown after the file's text.

````markdown
# Kitchen Command Center

Recipes, the variants people make of them, and what they thought of each: **[<KCC_HOST>](https://<KCC_HOST>)**.

An Umbraco 17 site on SQLite, with a Vue 3 front end rendered on the server, in the Loose Leaf identity. It runs on a
Raspberry Pi behind Cloudflare. Anyone can sign up; the owner approves every account, and members then review, note and
submit recipes.

## Contents

- [Local development](#local-development)
- [How the code is laid out](#how-the-code-is-laid-out)
- [Content and schema](#content-and-schema)
- [Members and contributions](#members-and-contributions)
- [Recipe search](#recipe-search)
- [Front end](#front-end)
- [Tests](#tests)
- [Deployment](#deployment)
- [History](#history)

---

## Local development

### What you need

- The .NET 10 SDK. `global.json` pins its feature band and the test runner.
- Node 24 and Yarn 1.22.
- A Font Awesome Pro token (below).
- Docker, only to build and run the production images.

### Font Awesome Pro

The site's icons are **Font Awesome Pro**, installed from Font Awesome's private npm registry. The committed root
`.npmrc` names the registry and reads the token from the `FONTAWESOME_NPM_AUTH_TOKEN` environment variable. Yarn reads
`.npmrc` on every command, so every `yarn` command fails while the variable is unset, `yarn test` included.

1. Get a token from your Font Awesome account (Account → Tokens).
2. Set `FONTAWESOME_NPM_AUTH_TOKEN` in your environment:
   - bash or zsh: `export FONTAWESOME_NPM_AUTH_TOKEN=<token>`. On macOS, put it in `~/.zshenv` rather than `~/.zshrc`,
     so non-interactive shells have it too.
   - PowerShell, for good (reopen the terminal afterwards): `setx FONTAWESOME_NPM_AUTH_TOKEN "<token>"`
   - PowerShell, for this session only: `$env:FONTAWESOME_NPM_AUTH_TOKEN = "<token>"`

The production images need the token too, as a build secret (see [Deployment](#deployment)). Never commit the token or
any Font Awesome file: `node_modules/`, `**/wwwroot/assets`, `**/wwwroot/webfonts` and the backoffice bundles in
`src/KCC.*/wwwroot/App_Plugins/` are gitignored, and the images live in private packages.

### First run

1. **Install the front-end dependencies** from the repository root. Every workspace installs into one `node_modules`
   there.

   ```bash
   yarn install
   ```

2. **Set the backoffice admin account** once per machine. Umbraco creates it on the first boot of a new database. The
   password needs at least 10 characters; keep it in your password manager.

   ```bash
   cd src/KCC.Web
   dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserName" "<name>"
   dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserEmail" "<email>"
   dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserPassword" "<password>"
   dotnet user-secrets set "Umbraco:CMS:Imaging:HMACSecretKey" "$(openssl rand -base64 64 | tr -d '\n')"
   ```

   The last line stores a random imaging key. Without one, Umbraco writes a generated key into the tracked
   `appsettings.json` whenever it installs a new database. For **Suggest with AI** in the recipe icon editor, also set
   `Anthropic:ApiKey`.

3. **Build the bundles** from the repository root. The backoffice's recipe editors and its Contributions dashboard load
   from this build, which `dotnet watch` does not redo.

   ```bash
   yarn build:all
   ```

4. **Run it** from `src/KCC.Web`:

   ```bash
   dotnet watch --non-interactive
   ```

   The first run creates `umbraco/Data/Umbraco.sqlite.db`, installs Umbraco, and imports the schema, UI strings and
   baseline pages from `uSync/v17/`. `dotnet watch` also starts Vite and the SSR service. With `--non-interactive`, an
   edit that needs a restart restarts the site instead of waiting at a prompt.

The site is at `https://localhost:58671`, and the backoffice at `/umbraco`.

- **To start over,** stop the site and delete `src/KCC.Web/umbraco/Data/Umbraco.sqlite.db*`, and
  `src/KCC.Web/wwwroot/media` for uploaded images.
- **Never build while `dotnet watch` runs.** The two builds collide, the watch exits, and Vite and the SSR service go
  with it. Stop it, build, and start it again.
- **Production data on your machine:** section 8 of the [runbook](docs/hosting/runbook.md) copies the live database and
  media down over Tailscale.

### Test data

With the site running, seed the test data: 25 recipes, 29 variants, two authors and their reviews.

```bash
curl -sk -X POST https://localhost:58671/api/dev/seed-recipes
```

The endpoint answers only in Development and in the Testing environment the test fixtures use. It skips recipes that
already exist, and the integration and E2E fixtures run it before their first test. While seeded recipes exist, the
baseline export refuses to run, so change the baseline from a fresh database.

---

## How the code is laid out

| Path | What it holds |
|---|---|
| `src/KCC.Web` | The Umbraco site: `Program.cs`, everything under `Features/`, the generated models and `uSync/v17/` |
| `src/KCC.Web/Features/Pages` | One folder per page type: its route-hijacking controller, view model, `Index.cshtml` and Vue view |
| `src/KCC.Web/Features/Components`, `Features/Widgets` | Shared Vue components; `Widgets` holds the ones Home's blocks render |
| `src/KCC.Web/Features/Api` | The public JSON endpoints under `/api` |
| `src/KCC.Web/Features/Recipes`, `Features/Search` | The recipe queries, and the in-memory recipe index |
| `src/KCC.Web/Features/Security`, `Features/Sqlite`, `Features/Hosting` | Rate limits and cookies; SQLite's write locks; the tunnel's forwarded headers |
| `src/KCC.Web/Features/Ssr` | The Node SSR service (`Server.js`) and the C# side that calls it |
| `src/KCC.Web/Features/Styles`, `Features/Torn` | Tailwind, the Loose Leaf kit, and the tear generator |
| `src/KCC.Web/Features/DevTools` | The test-data seeder and the baseline export, which answer in Development and Testing only |
| `src/KCC.Contributions` | Reviews, cook notes and cooked marks on EF Core, and the Contributions dashboard |
| `src/KCC.Admin` | The backoffice's recipe editors (ingredients, instructions, icon) and the icon API |
| `packages/admin-client-config` | The Vite and TypeScript base the two Lit backoffice clients share |
| `tests/` | The TUnit unit, integration and E2E suites, the Vitest suites, the combined report, and a screenshot tool |
| `deploy/`, `Dockerfile`, `docker-bake.hcl` | The production images, the compose stack, and the Pi's deploy and backup jobs |
| `docs/brand/` | The Loose Leaf identity and its engineering contract |
| `docs/hosting/runbook.md` | Setting up and running the Pi |

Each feature keeps its controller, view model, view and components together in one folder.

### Content modelling conventions

Use the same field names across document types and blocks:

- **Heading**: primary title text
- **SubHeading**: supporting title text
- **Body**: main content or description

Order a type's fields by purpose: what it does first (a card grid's cards), then its content (headings, text, media),
then its styling (backgrounds, widths).

---

## Content and schema

- Document types, data types and dictionary items are edited in the backoffice and exported by uSync on save in
  Development. Commit `src/KCC.Web/uSync/v17/`. Every environment imports that schema at every startup.
- Baseline content (the page tree with Home's sections, site settings, taxonomy and status pages) is not exported on
  save. Edit it in a fresh database, then run `curl -sk -X POST https://localhost:58671/api/dev/baseline/export` and
  commit the result. The endpoint refuses to run while seeded recipes exist.
- A fresh database boots into the baseline. An existing one keeps its own content, because content imports on first
  boot only: delete `src/KCC.Web/umbraco/Data/Umbraco.sqlite.db*` and restart to see the baseline's.
- UI strings are dictionary items, grouped by page (`RecipeSearch`, `VariantDetail`, `Shared`, …). The startup import
  creates a missing key but never changes one that exists, so a deploy never overwrites a string edited on the live
  site, and a key added in code arrives with the next deploy. Each page's controller sends its view a hand-picked list
  of keys, and a key missing from that list renders as its own name.
- ModelsBuilder runs in `SourceCodeManual` mode: after a schema change, use Settings → Models Builder → Generate models,
  and commit `Features/Models/Generated`.
- The live site's content lives in its database, and nothing copies content from a development database to it: author
  in the live backoffice.

---

## Members and contributions

### Members

Anyone can sign up, and the account waits until the owner approves it: Content → **Contributions** → **Waiting** →
**Approve** (the member's **Approved** toggle in the Members section does the same). Five failed sign-ins lock a member
out for 15 minutes. The account, contribution and submission endpoints check the anti-forgery token the layout hands
out, and are rate-limited per client (`RateLimits:*` in `appsettings.json`). A member's recipe or variant is saved as a
draft under Recipes, and **Waiting** lists it: open it, fill in anything it lacks, then **Save and publish**. The
**Reviews** and **Cook notes** tabs edit or delete what members have written.

No email is ever sent. The owner resets a forgotten password in the Members section, and unlocks a locked-out member
there too.

### Contributions

Reviews, cook notes and cooked marks are EF Core tables (`kccReview`, `kccCookNote`, `kccCookedMark`) in the same
SQLite file, migrated at startup. Every write runs inside Umbraco's scope and takes `ContributionLocks.Contributions`
before its first read, so it shares Umbraco's single SQLite writer. After changing an entity or
`ContributionsDbContext`, add a migration:

```bash
dotnet tool restore
dotnet dotnet-ef migrations add <Name> --project src/KCC.Contributions --startup-project src/KCC.Web \
  --context ContributionsDbContext --output-dir Data/Migrations
```

### Backoffice extensions

The recipe editors (ingredients, instructions and the icon, with **Suggest with AI**) and the Contributions dashboard
are Lit + TypeScript clients in `src/KCC.Admin/Client` and `src/KCC.Contributions/Client`, on the shared Vite base in
`packages/admin-client-config`. `yarn build:all` at the repository root builds them into their projects'
`wwwroot/App_Plugins/`, which the site serves to the backoffice; without that build the backoffice has no editors
for those fields and no dashboard. Rebuilding a client needs no .NET build: restart the site and reload the
backoffice. Run a client's tests with `yarn workspace @kcc/admin test` or `yarn workspace @kcc/contributions test`.

---

## Recipe search

The recipe index is a Lucene index held in memory and rebuilt whole: at startup, and two seconds
(`RecipeSearch:RebuildDelay`) after the last of any burst of content, member or review changes. Nothing needs
rebuilding by hand and nothing is written to disk. Each rebuild logs `Rebuilt the recipe index with N recipes`; a
failed one retries on its own after `RecipeSearch:RetryDelay` (30 seconds), backing off to at most ten minutes.
Code that writes reviews publishes `ReviewsChangedNotification`, and tests wait for a rebuild with
`IRecipeIndexRebuilder.WhenCurrentAsync`.

---

## Front end

### Commands

From the repository root:

```bash
yarn build:all    # both backoffice clients, then the site's client and SSR bundles, together
```

From `src/KCC.Web`:

```bash
yarn dev:all      # Vite and the SSR service, which dotnet watch also starts
yarn test         # Vitest
yarn type-check
yarn lint
yarn tears        # regenerate the tear presets from Features/Torn/tears.ts
```

Build the client and SSR bundles together, always with `yarn build:all`. Each scoped style's ID hashes its component's
path and content, so bundles built apart lose their scoped styles.

### Light and dark ramps

The site ships two colour ramps. A toggle in the header's utility nav writes `'light'` or `'dark'` to
`localStorage['kcc-theme']`, and an inline script in `Layout.cshtml` applies it to `<html data-theme>` before first
paint, so the wrong ramp never flashes.

With no stored choice, the site follows `prefers-color-scheme`, and falls back to light. To force a ramp while
testing, set the key by hand and reload:

```js
localStorage.setItem('kcc-theme', 'light') // or 'dark'
localStorage.removeItem('kcc-theme') // back to following the OS
```

**Check any visual change in both ramps.** The light ramp is the binding contrast constraint, and
`tests/KCC.ViteTests/Features/Styles/contrast.test.ts` asserts WCAG AA across every token pair in both.

### Server-side rendering

Razor renders each page's header, body and footer as Vue template text, and `VueSsrService` posts it to the SSR
service (`Features/Ssr/Server.js`, on port 3001 in development). The service renders it, and returns the HTML with the
hydration payload. If the SSR service is down, the page falls back to rendering in the browser.

### Loose Leaf

The public site's identity: torn-paper sheets on a lilac-grey desk, wax washes, one marker green. The brand is
[`docs/brand/loose-leaf.md`](docs/brand/loose-leaf.md), and [`docs/brand/kit.md`](docs/brand/kit.md) is the
engineering contract every style change follows.

---

## Tests

`node tests/scripts/run.mjs` runs every suite: three TUnit suites (unit, integration and E2E) and three Vitest suites
(the site and the two backoffice clients). It writes one tabbed HTML report to `tests/results/combined-report.html`
and opens it, and exits non-zero if any suite fails or does not run. Build first, from the repository root, with
`dotnet build KitchenCommandCenter.sln` and `yarn build:all`.

One suite at a time:

```bash
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj
cd src/KCC.Web && yarn test
```

For one class, append `-- --treenode-filter "/*/*/<ClassName>/*"` to a `dotnet run`. The TUnit suites run on
Microsoft.Testing.Platform, so `dotnet test` on one project finds no tests.

The integration suite boots the site in process, on a fresh SQLite database. Neither it nor the E2E suite needs a
database set up by hand.

### E2E tests

The E2E suite starts its own copy of the site on a free port, with a fresh SQLite database and its own SSR process.
Run `dotnet build` and `yarn build:all` (at the repository root) first, and set `KCC_E2E_MEMBER_USERNAME` and
`KCC_E2E_MEMBER_PASSWORD` (a password of at least 8 characters; on macOS/zsh, in `~/.zshenv`). The site's seeder
creates that member, approved, before any test runs.

---

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

---

## History

Kitchen Command Center ran on Xperience by Kentico until <launch date>. Xperience has no free or hobby licence, so the
site moved to Umbraco 17 on SQLite, keeping its Vue front end and the Loose Leaf identity. `docs/replatform/` holds
the spec, one plan per phase, and screenshots of the last Xperience version.

The tag `xperience-final` is that last version. To run it:

```bash
git worktree add ../Kitchen-Command-Center-xperience xperience-final
docker start mssql2022
cd ../Kitchen-Command-Center-xperience && yarn install
cd src/KCC.Web && dotnet watch
```

It needs what this repository does not hold: its SQL Server database, in the owner's `mssql2022` container, and its
Kentico keys, in KCC.Web's user-secrets, which both versions share. Only one of the two sites can hold
`https://localhost:58671` at a time.
````

If `git grep -n "CacheInstruction" src/` finds nothing, Phase 4 left the cache-instruction stall of Phase 1's
findings unfixed. Then end **First run**'s step 4 with this paragraph, indented as the step's text is:

```markdown
   Umbraco's log prints to the console. If cache instructions are still pending at Umbraco's first cache sync, about
   two minutes after boot (a new database's first-boot import, or an edit made before then), it logs
   `Cache instruction sync did not complete within 00:01:00`: a harmless Umbraco SQLite race that recovers on its own
   about 20 minutes later. A later edit can occasionally hit the same race.
```

In **Deployment**, add this paragraph at the end of main's text, after its bullets:

```markdown
`main` is production: merging a pull request that changes an image deploys it.
```

- [ ] **Step 3: Rewrite CLAUDE.md**

Replace the whole file with the text below. Four sections keep their text as it stands on `main`: **SQLite writes**
(Phase 4), **Backoffice extensions** (Phase 5), **Vue SFC `<style>` blocks** and **Loose Leaf design language**
(with Phase 6's and 7's edits). The text shown for them is what those plans left. Two changes are made to kept text:
- **Backoffice extensions** loses its bullet about naming a path `icon`, which **Naming traps** now holds, and its
  "Three things" becomes "Two things".
- **Tokens** gains the sentence about `--color-<name>`.

````markdown
# Kitchen Command Center

Project-level instructions for Claude Code. The site is Umbraco 17 LTS on SQLite, with a Vue 3 front end rendered on
the server in the Loose Leaf identity. It has been live at `https://<KCC_HOST>` since <launch date>, on a Raspberry
Pi behind Cloudflare. `README.md` is the developer guide, and `docs/hosting/runbook.md` runs the Pi.

## Superpowers: plans & specs

Save Superpowers **plans** and **specs** under `.superpowers/`, not under `docs/superpowers/`. This keeps all superpowers files together in the same location.

- **Plans** → `.superpowers/plans/YYYY-MM-DD-<slug>.md`
- **Specs / design docs** → `.superpowers/specs/YYYY-MM-DD-<slug>.md`

A plan and the spec it implements **must share the identical `<slug>` only** — same slug, *potentially* different date. When writing a plan, derive its filename from its spec, updating the date to be the new current date; don't coin a new slug. Example: spec `2026-05-20-recipes.md` ↔ plan `2026-05-21-recipes.md`.

This overrides the default locations and filename placeholders baked into the `writing-plans` and `brainstorming` skills (`docs/superpowers/plans/` and `docs/superpowers/specs/`), both of which explicitly defer to user preferences for file location.

**`.superpowers/` is gitignored** (see `.gitignore`). The plan, spec, and brainstorm files are local scratch only — do **not** stage or commit them while working through a feature, and don't be surprised when they don't appear in `git status`. Leave them out of every commit.

**`docs/replatform/` is a record.** The move off Xperience, finished on <launch date>, tracked its spec, phase plans
and reference screenshots there. New work never adds to it, and its plans describe the code as it was then.

## Building and testing

- **Build from the repository root:** `dotnet build KitchenCommandCenter.sln` and `yarn build:all`, before the
  integration and E2E suites, whose hosts refuse to start without the Vite manifest. Warnings are errors.
- **Tests:**
  - unit: `dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj`
  - integration: `dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`
  - E2E: `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj`
  - one class: append `-- --treenode-filter "/*/*/<ClassName>/*"`
  - the site's front end: `cd src/KCC.Web && yarn test`, then `yarn type-check`
  - a backoffice client: `yarn workspace @kcc/admin test` or `yarn workspace @kcc/contributions test`
  - everything: `node tests/scripts/run.mjs`
  - the images: `docker buildx bake --load`, then `deploy/smoke-test.sh`
- `dotnet test` on one project reports "Zero tests ran": the suites are TUnit on Microsoft.Testing.Platform.
- Vitest takes its `~` alias and its test folders from `src/KCC.Web/vite.config.ts`. A `vitest --root` of your own
  drops that config, and every `~/…` import fails to resolve.
- `node tests/scripts/run.mjs` calls `open` on its report when it finishes. For an unattended run, put a no-op `open`
  first on `PATH`.
- Every `yarn` command fails unless `FONTAWESOME_NPM_AUTH_TOKEN` is set, because the root `.npmrc` reads it. On macOS
  it belongs in `~/.zshenv`, since Claude Code's shells are not interactive and never read `~/.zshrc`.
- A `dotnet build` while `dotnet watch` runs crashes the watch, and Vite and the SSR service with it. Stop the watch
  first.
- A failing integration or E2E test never comes from a stale local database: each run boots a fresh SQLite site, and a
  site that fails to boot puts the tail of its log into the exception.

## Umbraco

- **Umbraco.Cms 17.x, never 18.** Every `Umbraco.Cms*` package takes `Umbraco.Cms`'s version, and so does
  `@umbraco-cms/backoffice` in both Lit clients. The next LTS, 21, is due 2027-12-09, and the move to it has to land
  before 17's security fixes end on 2028-11-27.
- **Only APIs that survive Umbraco 18:** plain `ControllerBase` for public APIs (never `UmbracoApiController`),
  `ManagementApiControllerBase` for backoffice endpoints, and the async content, member and dictionary services.
  Warnings are errors, so an obsolete member fails the build.
- **The SQLite connection string never contains `Cache=Shared`.** A unit test guards it.
- **ModelsBuilder** is `SourceCodeManual` in Development and `Nothing` everywhere else, and Production runtime mode
  refuses to boot otherwise. After a schema change, generate the models in the backoffice and commit
  `Features/Models/Generated`.
- **Schema is code; content is the live database's.** uSync imports schema from `src/KCC.Web/uSync/v17/` at every
  startup, dictionary items create-only (so a deploy never overwrites a string edited live), and baseline content on a
  new database's first boot only. Content never exports on save: `POST /api/dev/baseline/export` writes the baseline,
  and refuses while seeded recipes exist.
- **Deterministic keys** need RFC 9562's version and variant bits, or the backoffice rejects them.
  `DeterministicKey.For` sets them.
- **Umbraco writes into `appsettings.json`** unless told otherwise: a generated imaging key on each unattended install,
  and `Global:Id` minutes into a run. `Global:Id` is pinned, and the key comes from user-secrets, the test fixtures and
  the Pi's `.env`. `ConfigFileWritesTests` guards both.
- **Nullable and StyleCop.** Nullable is off in `KCC.Web`, `KCC.Contributions`, `KCC.Admin` and `KCC.UnitTests`, where
  `?` on a reference type raises CS8632, and on in the integration and E2E projects. StyleCop runs on `src/KCC.Web`:
  sorted usings, trailing commas in multi-line initializers, static members before instance members.
- **UI strings.** Each page's controller sends its view a hand-picked list of dictionary keys, and a key missing from
  that list renders as its own name, without an error. So a shared component resolves its strings in each page's
  wrapper, and its keys join each of those pages' lists.

## SQLite writes

The site runs on SQLite, where a transaction that has read cannot become the writer once another connection has
committed, and Umbraco then retries the write for about ten minutes. Two guards in `Features/Sqlite` prevent it:

- `SqliteComposer` wraps Umbraco's post-save relations update so that it takes the write lock before it reads. Never
  remove it. If an Umbraco upgrade moves the handler, the site refuses to boot rather than run unguarded.
- Code that saves a member through Umbraco's sign-in manager, member manager or `IMemberService` runs inside
  `IMemberWriteLock.RunAsync`.

Contribution writes go through `ContributionWrites`, which takes its lock before its first read. The concurrency test
in `tests/KCC.IntegrationTests/Features/Sqlite` proves all three; run it after touching any of them.

## Backoffice extensions

The backoffice's recipe editors and the Contributions dashboard are Lit clients in `src/KCC.Admin/Client` and
`src/KCC.Contributions/Client`, built by Vite from `packages/admin-client-config` into gitignored
`wwwroot/App_Plugins/` folders. Build them with the root `yarn build:all`. Two things fail without a word:

- A Management API call through `umbHttpClient` sends no token unless it passes
  `security: [{ scheme: 'bearer', type: 'http' }]`, and the endpoint answers 401.
- When a library `KCC.Web` already references gains its first controller, an incremental build keeps a stale
  `src/KCC.Web/obj/*/net10.0/KCC.Web.MvcApplicationPartsAssemblyInfo.cs` and every new route answers 404. Delete it.

## Vue SFC `<style>` blocks

Write component CSS in a `<style>` or `<style scoped>` block. A standalone CSS file is not required, and
adding one to work around missing styles is a symptom of a broken build, not a fix.

Style-block CSS never reaches the page as part of the SSR-rendered HTML, so each environment delivers it
separately:

- **Production** — the client build extracts it into chunk CSS assets (`GlobalComponents-*.css`). A
  `<link rel="stylesheet" vite-href="/Features/Main.ts">` in `Layout.cshtml` makes Vite.AspNetCore emit a
  `<link>` for every CSS file in the entry's import graph. It finds them in `wwwroot/.vite/manifest.json`, which
  `dotnet publish` skips like every dot-folder unless `KCC.Web.csproj` includes it, as it does: without it the
  published site links no CSS at all.
- **Development** — no such asset exists, so the SSR sidecar walks the Vite SSR module graph after
  rendering, compiles each style module through the client pipeline, and returns the CSS alongside the
  HTML. `SsrHtmlContent` inlines it as `<style data-ssr-styles>`, and `Main.ts` removes that tag once
  hydration has run so HMR-updated head styles win.

**Always build both bundles together — `yarn build:all`, never one side alone.** Production scope IDs hash
each component's path *and* its content, so a client bundle built against different component source than
the SSR bundle emits `data-v-` attributes the CSS has no selectors for, and every scoped component
silently loses its styles.

## Front end

- Tailwind scans only `.cshtml`, `.vue` and `.css` files (the `@source` lines in `Features/Styles/TailwindConfig.css`).
  A class name built in a `.ts` file is never emitted, and its element renders unstyled, unless an `@source inline(…)`
  line safelists it.
- `Features/Styles/Main.css` imports every Font Awesome style the markup uses. An icon whose style is not imported
  renders in the solid style instead, without an error; `mainCssIconStyles.test.ts` guards the list.

## Loose Leaf design language

The public site uses the Loose Leaf identity: torn-paper sheets on a lilac-grey desk, a 24px rule, eight
wax washes, marker green as the one strong fill, Sono for every number and label. **The brand lives in
`docs/brand/loose-leaf.md`; the engineering contract in `docs/brand/kit.md`.** Read the contract before
styling anything; use the `loose-leaf` skill for the quick reference, the `kit-builder` agent to convert
a surface and the `brand-steward` agent to review one. The invariants below are the ones that break the
build or the brand silently, so they stay here as well.

### Tokens

Role tokens live in `Features/Styles/TailwindConfig.css` under **`@theme static`** with the **light** values;
the dark ramp overrides them in `Features/Styles/Torn/Tokens.css` under `:root[data-theme='dark']`. The
`static` is load-bearing: Tailwind 4 prunes theme variables no utility references, and the kit CSS and the
dark ramp read `--color-*` directly. A colour is always `--color-<name>`: a bare name such as `--onyx` is undefined,
and a component `<style>` that uses one fails without a word. `Layout.cshtml` runs a pre-paint inline script that sets
`data-theme` from `localStorage['kcc-theme']`, else `prefers-color-scheme`, else **light**. **Check both ramps for any
visual change**; `tests/KCC.ViteTests/Features/Styles/contrast.test.ts` is the living contrast table and also
composites ink over paper plus each wash.

### The kit, and where CSS lives

`kcc-*` classes live in global `@layer components` CSS under `Features/Styles/Torn/`, **not** in component
`<style>` blocks: Razor-rendered views, home blocks and Vue components share them, and Razor cannot reach a scoped
block. Anything Razor also renders belongs in `@layer components`. A rule that must beat a Tailwind *utility*
sits **outside** `@layer` entirely (the ramp-swap rule at the end of `Features/Styles/Torn/Kit.css`), because
the `utilities` layer comes after `components`.

### The tear

Sheets are clipped with `clip-path: var(--tear)`; the presets in `Features/Styles/Torn/Tears.css` are
**generated** by `yarn tears` from `Features/Torn/tears.ts` and diffed by `tears.test.ts`. Never hand-edit
the CSS. No runtime JS draws anything; the SSR output is final. Two structural invariants:

- The `filter` (fibre + fall) sits on `.kcc-torn`, **outside** the clipped `.kcc-sheet`, or the shadow is
  clipped away with the paper.
- `.kcc-label`, `.kcc-tape` and `.kcc-tilewrap` are siblings of `.kcc-torn` inside `.kcc-slip`: outside the
  clip and outside the filter, so they are neither torn nor shadowed.

## Hosting

- The Pi runs `deploy/compose.yaml`, and `docs/hosting/runbook.md` covers everything on it. `main` is production: a
  merge that changes an image deploys within five minutes, after a snapshot of the database.
- `compose.yaml` never publishes a host port. The rate limiter keys on `CF-Connecting-IP`, and the app trusts
  forwarded headers, only because the tunnel is the only way in. `deploy/local.yaml` publishes `127.0.0.1:8443` for
  runs on a developer's machine, and never runs on the Pi.
- cloudflared's fixed address `172.30.9.10`, the `edge` subnet `172.30.9.0/24` and the app's `Hosting__TunnelAddress`
  change together.
- Images are built only by `docker buildx bake` at the repository root, never on the Pi. The Font Awesome token is a
  build secret, never a layer, and the GHCR packages stay private because the images carry Font Awesome Pro's files.
- A path a named volume mounts over must be an empty directory owned by UID 1654 in its image: an empty volume takes
  the content and the owner of the directory it is first mounted on.
- Database snapshots use SQLite's `.backup`, never `VACUUM INTO` or a copy of the live file: Umbraco sets WAL only when
  it creates a database, and its locking depends on it.
- Shell in `deploy/` is POSIX `sh` with `set -eu`, and passes `shellcheck -s sh`.
- Never act in the owner's accounts (Cloudflare, GitHub's settings, healthchecks.io) or on the Pi, and never sign in to
  the live site or its backoffice: guide the owner, and check from outside.

## Naming traps

With `core.ignorecase` on, `.gitignore`'s macOS `Icon` line ignores any path named `icon`, in any case, and its Visual
Studio `Backup*/` line any folder whose name starts with `backup`, without a word. Never name a path that way, and
check a new one with `git check-ignore -v <path>`. The backup image lives in `deploy/kcc-backup/` for that reason.
````

- [ ] **Step 4: Bring the runbook up to date**

In `docs/hosting/runbook.md`:

1. In **What runs where**, replace "Cloudflare Access asks for a one-time PIN before anyone reaches the site: until
   launch for the whole site, and always for `/umbraco`." with "Cloudflare Access asks for a one-time PIN before
   anyone reaches `/umbraco`."
2. In section 4, replace step 5, **Access for the whole site, until launch**, and its paragraph with:

   ```markdown
   5. **To take the site private again,** as it was until launch (<launch date>), add a second application the same
      way: name `KCC private`, public hostname `KCC_HOST` with no path, and a policy allowing your email and anyone
      else's who should see the site. The more specific application wins, so `/umbraco` stays yours alone. Delete the
      application to make the site public again.
   ```

3. In section 7, step 5, replace "open `https://KCC_HOST`, pass Access with your email and the PIN it sends, and the
   site appears;" with "open `https://KCC_HOST`, and the site appears;". Replace "open `/umbraco`, and sign in to
   Umbraco as the administrator from `first-boot.env`." with "open `/umbraco`, pass Access with your email and the PIN
   it sends, and sign in to Umbraco as the administrator from `first-boot.env`."
4. In section 8, **Everyday operations**:
   - add this bullet first:

     ```markdown
     - **New members.** Approve the people you know in the backoffice: Content → Contributions → **Waiting**. A member
       you never approve can never sign in. The README's Members section covers the rest.
     ```

   - in **Updates**, add at the end: "Before merging an Umbraco update, rehearse it on a copy of production: build
     the branch's images (`docker buildx bake --load` at the repository root), export `KCC_IMAGE_TAG=local`, and run
     the drill (section 9). The restored database migrates on your computer instead of on the Pi."
5. In section 9, replace "Once a quarter, and before launch," with "Once a quarter,".

Then check what is left:

```bash
grep -n -i "launch" docs/hosting/runbook.md
```

Expected: one line, in step 5's "as it was until launch".

- [ ] **Step 5: Two stale lines in the kit contract**

In `docs/brand/kit.md`, if these lines are still there:
- replace "props `label` or a `#label` slot for rich content (a `<ResourceString>` keeps its editor hooks that way)"
  with "props `label`, or a `#label` slot for rich content such as a `<ResourceString>`". The on-page string editor's
  hooks went in Phase 1.
- replace "Widget loops pick tears from their index" with "Loops pick tears from their index". Home's blocks replaced
  the widgets in Phase 6.

Leave the rest to the brand's owner.

- [ ] **Step 6: Correct the spec where this phase did something else**

In `docs/replatform/specs/2026-09-21-replatform-off-xperience.md`, make these replacements, and mention each to the
owner when the phase closes:

1. §6.3: replace "`RobotsTxtDenyAll` stays on until launch; when off," with "`RobotsTxtDenyAll` is off in production
   and on everywhere else; when off,". Then, after "points at the sitemap.", add " The sitemap answers HEAD as well
   as GET."
2. §8, the **Forwarded headers** bullet: after "so cookies are `Secure` and generated URLs are `https`." add " The
   antiforgery cookie, which ASP.NET Core never marks Secure by default, follows the request's scheme as the member
   cookie does."
3. §13.3, the **Cloudflare Access** bullet: replace Phase 7's sentence "Until launch a second application covers the
   whole host, for the owner and anyone testing; launch deletes it (the owner's decision, 2026-09-26)." with "Until
   launch (<launch date>) a second application covered the whole host, for the owner and anyone testing."
4. §15, the Phase 8 row: replace "the reference worktree and the old SQL Server container retired" with "the reference
   worktree retired; the SQL Server container, which also serves other projects, keeps the Xperience database, so
   `xperience-final` can still run (the owner's decision, 2026-09-28)".

Rewrap each changed paragraph to the file's 120 columns.

- [ ] **Step 7: Check every path the two files name**

```bash
for file in README.md CLAUDE.md; do
  grep -oE '`[^` ]*/[^` ]*`' "$file" | tr -d '`' | sed -e 's/[.,:;)]*$//' | sort -u | while read -r path; do
    case "$path" in http* | *'<'* | *'*'* | *'…'* | /* | ~*) continue ;; esac
    [ -e "$path" ] || [ -e "src/KCC.Web/$path" ] || echo "$file: $path"
  done
done
```

Expected, and nothing else:
- paths a run or a build creates: `umbraco/Data/Umbraco.sqlite.db`, `src/KCC.Web/wwwroot/media`,
  `tests/results/combined-report.html`, and `wwwroot/App_Plugins/` before a build;
- the `docs/superpowers/` paths, which CLAUDE.md names as the ones not to use;
- `.superpowers/`, when the check runs in a worktree.

Anything else means a path moved: correct the doc, not the code. Then check the
README's links: every `](docs/…)` target exists, and every `#anchor` in its Contents matches a heading.

- [ ] **Step 8: Commit**

```bash
git add README.md CLAUDE.md docs/hosting/runbook.md docs/brand/kit.md \
  docs/replatform/specs/2026-09-21-replatform-off-xperience.md
git add -u
git status --short
git commit -m "Rewrite the Docs for the Live Site"
```

`git add -u` takes the sweep's edits to tracked files. Check `git status --short` before committing: nothing under
`.superpowers/` and no new file belongs in it.

---

### Task 9: Rewrite the memory

Spec §15's Phase 8 row: "README, CLAUDE.md and memory rewritten". The memory lives outside the repository, in
`~/.claude/projects/-Users-twinright-Repos-Kitchen-Command-Center/memory/`, and `MEMORY.md` is loaded into every
session. `~/.claude/CLAUDE.md` says memory never holds what the repository already records. After Task 8, CLAUDE.md
and the README record most of what the dev-workflow memories said, and the Kentico-era notes describe a version that
now only runs from a tag.

Nothing is lost. The Kentico notes move to an archive folder outside the index, because they are general Xperience
knowledge the owner may want again. The facts CLAUDE.md now carries are deleted from memory only after Task 8 has
committed them.

Use the `anthropic-skills:consolidate-memory` skill for the pass, with the table below as its decisions.

**Files** (all outside the repository):
- Rewrite: `xperience-final-reference.md`, which planning wrote on 2026-09-28 to record the owner's decision
- Create: the folder `archive/xperience-final/`
- Move, delete or modify: the files the table names
- Rewrite: `MEMORY.md`

- [ ] **Step 1: Read every memory file**

```bash
M=~/.claude/projects/-Users-twinright-Repos-Kitchen-Command-Center/memory
ls "$M"
grep -l -i -E "phase [0-9]|replatform|kentico|xperience|until launch" "$M"/*.md
```

Read each file the table below keeps, and each file it does not name. Phases 4 to 7 may have added or changed some.

- [ ] **Step 2: Apply the table**

| File | What to do | Why |
|---|---|---|
| `admin-client-babel-ts-generics.md`, `admin-page-link-generator-prefix.md`, `kcc-admin-ui-login.md`, `kentico-admin-css-red-sentinel.md`, `xperience-admin-hidden-dashboard-category.md`, `kentico-admin-jsx-production-runtime.md`, `kentico-admin-client-link-and-cell-gotchas.md`, `kentico-migrate-class-field-type.md`, `ci-restore-guid-or-codename.md`, `ci-restore-dev-seeder-duplicate-page.md`, `resource-strings-populate-values.md` | Move to `archive/xperience-final/`, unchanged | They describe developing on Xperience, which only the tag does now |
| `self-hosting-arm64-sql-blocker.md` | Delete | Superseded: spec §5 and §18 record why SQL Server cannot run on the Pi |
| `replatform-off-xperience.md` | Delete | The replatform is done, and `docs/replatform/` and CLAUDE.md record it. Its one standing fact moves to `xperience-final-reference.md` |
| `tunit-run-single-suite.md`, `vitest-run-from-kcc-web.md`, `run-mjs-opens-report.md`, `e2e-tests-seed-member-and-db.md`, `kcc-web-dev-run-gotchas.md` | Delete | CLAUDE.md's **Building and testing** and the README's **First run** and **Tests** say it |
| `sfc-style-css-delivery-gap.md` | Delete | CLAUDE.md's **Vue SFC `<style>` blocks** |
| `umbraco-sqlite-relations-lock-hazard.md` | Delete | CLAUDE.md's **SQLite writes**, from Phase 4 |
| `umbraco-config-writes.md`, `resource-strings-curated-per-page.md` | Delete | CLAUDE.md's **Umbraco** |
| `gitignore-icon-rule.md` | Delete | CLAUDE.md's **Naming traps** |
| `tailwind-v4-color-token-gotcha.md`, `tailwind-source-scanning-excludes-ts.md`, `fontawesome-style-imports.md` | Delete | CLAUDE.md's **Tokens** and **Front end** |
| `umbraco-sqlite-cache-instruction-stall.md` | Keep while `git grep -n "CacheInstruction" src/` finds nothing; delete otherwise | The stall is live until Phase 4's fix exists |
| `kcc-wash-min-radius-bug.md` | Keep while the bug is open; delete once fixed | Its own task |
| `yarn-fontawesome-token.md` | Keep, and cut its 2026-07-01 workspace paragraph, which names the deleted `KCC.ResourceStrings`. Check its Prettier paragraph against the files | The `~/.zshenv` fix, the harmless `bash_completion` noise and the `node_modules/.bin` fallback are this machine's |
| `recipe-search-deterministic-order.md` | Keep, and drop its ci-restore cause | The name tie-break still matters to the E2E tests |
| `plans-one-file-per-phase.md` | Keep; its replatform sentence goes to the past tense | The owner's standing preference |
| `kcc-hosting.md` (Phase 7) | Keep. Add "Public since <launch date>: `KCC pre-launch` is gone, and `/umbraco` stays behind `KCC backoffice`." Drop what CLAUDE.md's **Hosting** now says | The Pi's traps, and where things run |
| `umbraco-backoffice-extensions.md` (Phase 5), `umbraco-block-list-authoring.md` (Phase 6) | Keep. Drop their phase-by-phase history and what CLAUDE.md now says (the bearer scheme, the stale application parts, the `Icon` rule) | Debugging knowledge the repository does not hold |
| `recipe-search-test-seeder.md`, `recipe-search-facet-universe.md`, `recipe-search-spotlight-hides-grid-card.md`, `shared-recipe-card-components.md`, `umbraco-cold-start-nav-race.md`, `dotnet10-backgroundservice-executeasync.md`, `autonomous-run-sleep-and-agent-resume.md`, `preview-pane-drops-hover.md`, `kcc-recipe-design-project.md`, `kcc-design-system-react-port.md`, `kcc-font-vertical-metrics.md`, `kcc-paper-system.md`, `kcc-ink-wash-patina.md`, `css-random-via-sibling-index.md`, `web-font-swap-measurement.md` | Keep. Reword any sentence that still speaks of a phase or of the port as the present | Facts neither the repository nor CLAUDE.md states |
| Any other file | Keep it if it records something neither the repository nor CLAUDE.md does; otherwise delete it | The same rule |

```bash
M=~/.claude/projects/-Users-twinright-Repos-Kitchen-Command-Center/memory
mkdir -p "$M/archive/xperience-final"
cd "$M" && mv admin-client-babel-ts-generics.md admin-page-link-generator-prefix.md kcc-admin-ui-login.md \
  kentico-admin-css-red-sentinel.md xperience-admin-hidden-dashboard-category.md kentico-admin-jsx-production-runtime.md \
  kentico-admin-client-link-and-cell-gotchas.md kentico-migrate-class-field-type.md ci-restore-guid-or-codename.md \
  ci-restore-dev-seeder-duplicate-page.md resource-strings-populate-values.md archive/xperience-final/
```

Delete the others one by one with `rm`, after reading each, only once Task 8's commit holds what the table says
replaced it.

- [ ] **Step 3: Rewrite `xperience-final-reference.md`**

Planning wrote it on 2026-09-28, before launch. Replace it with:

```markdown
---
name: xperience-final-reference
description: The last Xperience version (tag xperience-final) stays runnable by the owner's choice — never remove the mssql2022 container, the KCC database in it, or KCC.Web's Kentico user-secrets; the Kentico-era notes are archived in archive/xperience-final/
metadata:
  type: project
---

The site went public on Umbraco on <launch date>, and `docs/replatform/` is the replatform's record. On 2026-09-28
the owner chose to keep the last Xperience version runnable: the tag `xperience-final`, run as the README's History
section says.

Running it needs these, so they are never removed, pruned or reset:
- the `mssql2022` Docker container. It has no volume: its writable layer holds KCC's Xperience database, and other
  projects' databases too.
- KCC.Web's user-secrets (`85272aa4-641c-4fe4-b1e7-977f7decf9dc`), which both versions share: the Kentico keys, such
  as `ConnectionStrings:CMSConnectionString`, sit beside Umbraco's.

Its licence was a 30-day evaluation key, so running it again may first need a new one from Kentico.

The notes on developing with Xperience (the admin clients' Babel and JSX gotchas, CI restore, resource strings, the
admin UI) moved to `archive/xperience-final/` in this folder at launch, out of the index. Read them only when working
on the tag.

**Why:** the owner wants the Xperience implementation runnable in the future, and the container serves other work.

**How to apply:** never suggest removing `mssql2022`, its databases or those user-secrets as cleanup.
```

- [ ] **Step 4: Rewrite `MEMORY.md`**

One line per kept memory, a hook and not the content, the way `~/.claude/CLAUDE.md` asks. As the table leaves the
folder, it reads as below. Drop the two conditional lines if Step 2 deleted their files, and add a line for any other
file Step 2 kept.

```markdown
# Memory index

- [Font Awesome token for yarn](yarn-fontawesome-token.md) — every yarn command needs FONTAWESOME_NPM_AUTH_TOKEN; it lives in ~/.zshenv so non-interactive shells get it
- [One plan file per phase](plans-one-file-per-phase.md) — a multi-phase spec gets one self-contained plan per phase, written just in time
- [Xperience version stays runnable](xperience-final-reference.md) — keep mssql2022, its KCC database and the Kentico user-secrets; Kentico notes archived
- [KCC hosting on the Pi](kcc-hosting.md) — public since <launch date>; pull deploys, nightly R2 backups, docs/hosting/runbook.md; the container traps
- [Backoffice extensions](umbraco-backoffice-extensions.md) — how the Lit clients build and load, and the traps that fail silently
- [Block List authoring](umbraco-block-list-authoring.md) — authoring Umbraco Block Lists and content in code without the owner's dev database
- [Recipe test seeder](recipe-search-test-seeder.md) — what POST /api/dev/seed-recipes creates, and the search coverage the tests lean on
- [Recipe search facet universe](recipe-search-facet-universe.md) — zero-count facets are omitted; the client keeps the full option set from the first response
- [Recipe search deterministic order](recipe-search-deterministic-order.md) — relevance ties break by name, or E2E variant navigation flakes
- [Search spotlight hides the grid card](recipe-search-spotlight-hides-grid-card.md) — a single rated match moves to the spotlight; assert on [data-recipe-name]
- [Shared recipe card components](shared-recipe-card-components.md) — RecipeCard, RecipeCardRow and FeaturedRecipeCard over RecipeCardModel
- [Umbraco cold-start nav race](umbraco-cold-start-nav-race.md) — the first burst of requests after a boot can drop the header's Login link
- [Umbraco SQLite cache-instruction stall](umbraco-sqlite-cache-instruction-stall.md) — pending instructions at the first sync pause cache updates for ~20 minutes
- [Corner washes never render](kcc-wash-min-radius-bug.md) — min(40%, 68px) caps compute to no background in Chrome; its own task
- [.NET 10 BackgroundService start](dotnet10-backgroundservice-executeasync.md) — ExecuteAsync runs wholly on a background thread; start-up work belongs in StartAsync
- [Long autonomous runs on this Mac](autonomous-run-sleep-and-agent-resume.md) — lid-closed sleep stalls subagents; resume one with SendMessage
- [Browser pane input quirks](preview-pane-drops-hover.md) — the pane never applies :hover, and emulated clicks can miss
- [KCC design projects](kcc-recipe-design-project.md) — Loose Leaf is the shipped identity; the claude.ai/design projects are pre-history
- [React design-system port (removed)](kcc-design-system-react-port.md) — deliberately deleted 2026-08-21; don't rebuild it
- [Font vertical metrics](kcc-font-vertical-metrics.md) — Hazelnut and APCasual k constants; pin APCasual's ascent and descent
- [Paper and baseline grid](kcc-paper-system.md) — the pitch drives rule spacing; the shim is padding cancelled by a negative margin
- [Ink wash bloom](kcc-ink-wash-patina.md) — brand-colour washes bloom over minutes; accent tokens via nth-child
- [CSS random via sibling-index](css-random-via-sibling-index.md) — hash sibling-index() with sin() and mod(); register the seed with @property
- [Preload brand fonts](web-font-swap-measurement.md) — production preloads the woff2 files so fonts are ready at first paint; development doesn't
```

- [ ] **Step 5: Check the folder**

```bash
M=~/.claude/projects/-Users-twinright-Repos-Kitchen-Command-Center/memory
for f in "$M"/*.md; do n=$(basename "$f"); [ "$n" = MEMORY.md ] || grep -q "($n)" "$M/MEMORY.md" || echo "not indexed: $n"; done
grep -oE '\]\([^)]+\.md\)' "$M/MEMORY.md" | tr -d '])(' | while read -r n; do [ -f "$M/$n" ] || echo "index names a missing file: $n"; done
grep -ohE '\[\[[a-z0-9-]+\]\]' "$M"/*.md | sort -u
```

Expected: the first two print nothing. The third lists the `[[links]]` still in use. For each one whose file moved to
the archive or was deleted, reword the sentence around it in the file that links to it. A link to a memory not yet
written is allowed; a link to one just deleted is not.

---

### Task 10: Close the phase and the replatform

**Files:**
- Modify: this plan (its Status line, and a closing "Findings from Phase 8" section)
- Modify: `docs/replatform/specs/2026-09-21-replatform-off-xperience.md` (its Status line)

- [ ] **Step 1: Record the findings**

Append to this plan:

```markdown
## Findings from Phase 8

Found while launching the site (<dates>). The replatform is complete.

- **Launch.** The site went public on <launch date> at <time>, at `https://<KCC_HOST>`, when `KCC pre-launch` was
  deleted. `/umbraco` stays behind `KCC backoffice`.
- **The first deploy by pull.** Task 2's merge reached the Pi in <minutes>, snapshot first, with no hand on the Pi.
- **Umbraco <version>.** <what the release notes asked for>. The rehearsal on a copy of production (<date>): <result>.
  The Pi's upgrade: <result>.
- **The definition of done, on the live site** (<date>): <each Task 5 step and its result>.
- **The restore drill of the real content** (<date>) restored `<archive>`: <result>.
- **Authoring in production:** <what surprised the owner in Task 4>.
- **Cloudflare:** <whether the managed robots.txt was on, and what the owner chose>; <whether a purge was needed>.
- <each difference from this plan or the runbook, and how it was handled>
```

Set this file's **Status** line to `done (<date>)`.

- [ ] **Step 2: Mark the spec done**

In the spec's first paragraph, replace "**Status:** approved 2026-09-23, ready for planning." with "**Status:** done: the
site went public on <launch date>. Approved 2026-09-23." Keep the rest of the paragraph.

- [ ] **Step 3: Everything, once more**

From the repository root:

```bash
dotnet build KitchenCommandCenter.sln
yarn build:all
(cd src/KCC.Web && yarn test && yarn type-check)
node tests/scripts/run.mjs
docker buildx bake --load
deploy/smoke-test.sh
```

Expected: 0 warnings; every bundle builds; Vitest and the type check pass; the combined run is green; the smoke test
prints its 14 `ok` lines.

- [ ] **Step 4: Commit, push, and open the pull request (the owner approves each)**

```bash
git add docs/replatform/plans/2026-09-28-phase-8-launch.md docs/replatform/specs/2026-09-21-replatform-off-xperience.md
git commit -m "Close the Replatform"
```

Ask the owner, then `git push -u origin launch-close`. Write `.superpowers/phase-8/pr-launch-close.md`:

```markdown
Closes the replatform off Xperience (Phase 8, Tasks 7 to 10; `docs/replatform/plans/2026-09-28-phase-8-launch.md`).
The site has been public since <launch date>.

- CI builds `main` alone, and its build-and-test job runs on Node 24, as the images do.
- README.md and CLAUDE.md are rewritten for the live site. The runbook drops its pre-launch steps and says how to take
  the site private again. Comments that spoke of phases or of launch are gone.
- The spec records where the phase did something else: robots.txt open in production only, the Secure antiforgery
  cookie, and the SQL Server container kept so that `xperience-final` can still run. The phase plan records its
  findings.

Test plan: the combined suite is green (unit <count>, integration <count>, E2E <count>, Vitest <count>) on Node 24;
the smoke test passes its 14 checks locally and in this pull request's `images` job.
```

Then, with the owner's approval:

```bash
gh pr create --repo Th3FenrisWolf/Kitchen-Command-Center --base main --head launch-close \
  --title "Close the Replatform" --body-file .superpowers/phase-8/pr-launch-close.md
```

- [ ] **Step 5: The owner merges it**

When both jobs are green, the owner merges. If Task 8's sweep changed anything under `src/`, the images change and the
Pi deploys them: check `journalctl -u kcc-deploy` for the `deployed …` line, as in Task 2, Step 8. If only docs,
tests and the workflow changed, nothing deploys.

Then tell the owner that the replatform is complete, and list the four spec corrections from Task 8, Step 6.

---

## The gate

The Phase 8 row of spec §15 is "Public":
- **Public:** Task 6, Step 4. The site answers anonymously at `https://<KCC_HOST>`, rendered on the server, while
  `/umbraco` still asks for Access. robots.txt lets crawlers in and names the sitemap, whose addresses are `https`.
- **The rest of spec §1's definition of done:** Task 5, on the live site, before anyone else could see it.
- **The restore drill of the real content,** which spec §13.5 makes a gate before launch: Task 6, Step 1.
- **Launch on Umbraco 17.8 or later** (spec §6.1): Task 3.

Every gate also requires:
- `dotnet build` clean with warnings as errors;
- `yarn build:all`, with both bundles built together;
- Vitest green;
- a browser check of the touched pages in both ramps.

Task 2, Step 6 and Task 10, Step 3 cover the first three. Task 6, Steps 6 and 7 are the browser check: every page,
since the whole site is what launch touches.

## Findings from Phase 8

Found while building Tasks 1 and 2 (2026-09-30). Task 10 adds what the launch finds.

- **The branch.** Tasks 1 and 2 ran on `ready-for-public`, based on `hosting` at `c4a84bf`, because Phase 7 and the
  phases beneath it wait on the owner's merges into `main`; Task 1, Step 1's branch off `main` was skipped. The pull
  request opens after Phase 7's merge: if it lands as a squash, rebase first with
  `git rebase --onto main c4a84bf ready-for-public`.
- **"Before you start".** Checked on `hosting`: items 3 to 9 and 16 passed, apart from item 8's Dependabot check,
  which needs the pushed repository. Items 1 and 2 did not (Phase 7 is not merged, and the site does not run on the
  Pi yet), and items 10 to 15 belong to later tasks. After Tasks 1 and 2, items 3 to 7 describe the tree before
  them: now `appsettings.Production.json` has `"RobotsTxtDenyAll": false`, the sitemap has
  `[HttpHead("sitemap.xml")]`, `SecurityComposer` configures `AntiforgeryOptions`, `ProductionSettingsTests` has four
  tests, `SitemapTests` has `RobotsTxt_DeniesEverythingOutsideProduction`, `TunnelTests` has eight, and the smoke
  test has 14 `pass` calls.
- **Where the work departs from this plan's text.**
  - Task 1: the smoke test's fourth check also asks for `/sitemap.xml` with HEAD, through the local edge and
    Kestrel, so the sitemap's HEAD is proven on the real stack before the Pi deploys it. Without the HEAD route it
    fails with `not ok - the sitemap does not answer HEAD`. The smoke test still has 14 checks.
  - Task 1's introduction says the smoke stack and the restore drills keep the deny-all. They run the Production
    environment, so they serve the open robots.txt, as Step 5's own check requires; they listen on `127.0.0.1` only.
  - Task 2: the comments in `SecurityComposer` and `TunnelTests` give the reason the cookie is not always Secure.
    With `Always`, ASP.NET Core's antiforgery refuses to issue a token over plain HTTP (`CheckSSLConfig`), so every
    page on the E2E site would fail before a browser saw a cookie. The plan's reason, that a browser would drop the
    Secure cookie, never comes into play.
  - Task 8, Step 6's items 1 and 2, the spec's §6.3 and §8, are done here with the work they describe. Items 3 and 4
    wait for the launch date.
  - Task 3, Step 3: `@umbraco-cms/backoffice` is pinned once, at 17.7.0 in `packages/admin-client-config/package.json`,
    which both Lit clients build from; neither client's `package.json` names it. Move that one line.
  - Task 6, Step 4: its first check counted `<div id="app"><`, which also matches the empty client-side fallback
    `<div id="app"></div>`, as Phase 7's findings say. It now counts `<div id="app"><[^/]`, the smoke test's pattern.
- **Task 3.** On 2026-09-30 NuGet's newest `Umbraco.Cms` 17.x was 17.7.0, with no 17.8 release candidate, so Task 3
  has not started.
- **Counts.** 1333 passed, 0 failed: unit 233 (Phase 7's 232 plus 1), integration 263 (plus 3), E2E 41, web Vitest
  771 (769 and its 2 expected failures), admin Vitest 17, contributions Vitest 8. The smoke test's 14 checks pass.
