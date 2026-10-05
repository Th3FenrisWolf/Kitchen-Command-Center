# KitchenCommandCenter

An Umbraco 17 application with Vue 3 server-side rendering (SSR), on SQLite.

## Table of Contents

- [Local Development](#local-development)
- [Architecture](#architecture)
- [Deployment](#deployment)

---

## Local Development

### Quick Development Workflow

1. **Install and build the frontend** from the repo root (needs the Font Awesome token, see below):

   ```bash
   yarn install
   yarn build:all
   ```

   The build includes the backoffice bundles; without them the backoffice has no editors for ingredients,
   instructions and the icon, and no dashboard (see [Backoffice extensions](#backoffice-extensions)).

2. **Set the backoffice admin account** once per machine. Umbraco creates it on the first boot of a new database.
   The password needs at least 10 characters; keep it in 1Password.

   ```bash
   cd src/KCC.Web
   dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserName" "<name>"
   dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserEmail" "<email>"
   dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserPassword" "<password>"
   dotnet user-secrets set "Umbraco:CMS:Imaging:HMACSecretKey" "$(openssl rand -base64 64 | tr -d '\n')"
   ```

   The last line stores a random imaging key; without one, Umbraco writes a generated key into the tracked
   `appsettings.json` whenever it installs a new database.

3. **Run it** from `src/KCC.Web`:

   ```bash
   dotnet watch --non-interactive
   ```

   The first run creates `umbraco/Data/Umbraco.sqlite.db`, installs Umbraco and imports the schema, UI strings and
   baseline pages from `uSync/v17/`. `dotnet watch` also starts Vite and the SSR service. To start over, stop the site
   and delete `umbraco/Data/Umbraco.sqlite.db*`.

   Umbraco's log prints to the console.

The site is at `https://localhost:58671`; the backoffice is at `/umbraco`.

### Font Awesome Pro

This project renders icons with **Font Awesome Pro** (webfont / CSS), installed from Font Awesome's private npm registry. The registry is configured in the committed root `.npmrc`; the auth token is **not** committed — it is read from the `FONTAWESOME_NPM_AUTH_TOKEN` environment variable.

Before running `yarn install` at the repository root, which installs every workspace:

1. Get a token from your Font Awesome account (Account → Tokens).
2. Set `FONTAWESOME_NPM_AUTH_TOKEN` in your environment:
   - PowerShell (persists; reopen the terminal afterward): `setx FONTAWESOME_NPM_AUTH_TOKEN "<token>"`
   - PowerShell (current session only): `$env:FONTAWESOME_NPM_AUTH_TOKEN = "<token>"`
   - bash / zsh: `export FONTAWESOME_NPM_AUTH_TOKEN=<token>`

The same variable must be set wherever the production frontend is built. Never commit the token or any Font Awesome font files — `node_modules/`, `**/wwwroot/assets`, `**/wwwroot/webfonts`, and the backoffice bundles in `src/KCC.*/wwwroot/App_Plugins/` are all git-ignored.

---

## Architecture

This starter follows established patterns and best practices for maintainable, scalable applications.

### Vertical Slice Architecture

Features are organized by vertical slices in `src/KCC.Web/Features/`:

```
Features/
├── Widgets/
│   └── Accordion/
│       ├── AccordionViewComponent.cs
│       ├── AccordionViewModel.cs
│       ├── AccordionProperties.cs
│       └── Accordion.cshtml
└── Pages/
    └── HomePage/
        ├── HomePageController.cs
        ├── HomePageViewModel.cs
        └── HomePage.cshtml
```

Each feature contains all related files (controllers, view models, views) in one location.

### Content Modeling Standards

#### Field Naming Conventions

Use consistent field names across content types and blocks:

- **Heading**: Primary title text
- **SubHeading**: Supporting title text
- **Body**: Main content/description

#### Field Ordering

1. **Primary Function**: Core functionality (e.g., a card grid's cards)
2. **Content Fields**: Headings, text, media
3. **Styling Options**: Colors, spacing, layout options

### Development Best Practices

#### Schema and baseline content

- Document types, data types and dictionary items are edited in the backoffice and exported by uSync on save in
  Development. Commit `src/KCC.Web/uSync/v17/`.
- Baseline content (the page tree with Home's sections, site settings, taxonomy, status pages) is not exported on save.
  Edit it in a fresh database, then run `curl -sk -X POST https://localhost:58671/api/dev/baseline/export` and commit
  the result. The endpoint refuses to run while seeded recipes exist.
- A fresh database boots into the baseline home. An existing one keeps its own Home, because content imports on first
  boot only: delete `src/KCC.Web/umbraco/Data/Umbraco.sqlite.db*` and restart to see the baseline's.
- ModelsBuilder runs in `SourceCodeManual` mode: after a schema change, use Settings → Models Builder → Generate models,
  and commit `Features/Models/Generated`.

#### Test recipes

With the site running, seed the test data: 25 recipes, 29 variants, two authors and their reviews.

```bash
curl -sk -X POST https://localhost:58671/api/dev/seed-recipes
```

The endpoint answers only in Development and in the Testing environment the test fixtures use. It skips recipes that
already exist, and it is what the integration and E2E fixtures run before their first test. While seeded recipes
exist, the baseline export refuses to run, so change the baseline from a fresh database.

#### Contributions

Reviews, cook notes and cooked marks are EF Core tables (`kccReview`, `kccCookNote`, `kccCookedMark`) in the same
SQLite file, migrated at startup. Every write runs inside Umbraco's scope and takes `ContributionLocks.Contributions`
before its first read, so it shares Umbraco's single SQLite writer. After changing an entity or
`ContributionsDbContext`, add a migration:

```bash
dotnet tool restore
dotnet dotnet-ef migrations add <Name> --project src/KCC.Contributions --startup-project src/KCC.Web \
  --context ContributionsDbContext --output-dir Data/Migrations
```

#### Recipe search

The recipe index is a Lucene index held in memory and rebuilt whole: at startup, and two seconds
(`RecipeSearch:RebuildDelay`) after the last of any burst of content, member or review changes. A burst that never
goes quiet — a steady stream of sign-ins, say — still rebuilds within `RecipeSearch:MaxRebuildWait` (10 seconds) of
its first change. Nothing needs rebuilding by hand and nothing is written to disk. Each rebuild logs `Rebuilt the
recipe index with N recipes`; a failed one retries on its own after `RecipeSearch:RetryDelay` (30 seconds), backing
off to at most ten minutes.
Code that writes reviews publishes `ReviewsChangedNotification`, and tests wait for a rebuild with
`IRecipeIndexRebuilder.WhenCurrentAsync`.

#### Members

Anyone can sign up, and the account waits until the owner approves it: Content → **Contributions** → **Waiting** →
**Approve** (the member's **Approved** toggle in the Members section, then **Save**, does the same). A member's recipe
or variant is saved as a draft under Recipes, and **Waiting** lists it: open it, fill in anything it lacks, then **Save
and publish**. The **Reviews** and **Cook notes** tabs edit or delete what members have written.

A new recipe arrives with its first variant as a second draft; publish the recipe first, then that variant.

Five failed sign-ins lock a member out for 15 minutes (`Umbraco:CMS:Security` in `appsettings.json`). The account,
contribution and submission endpoints check the anti-forgery token the layout hands out. Rate limits apply per client,
keyed on the `CF-Connecting-IP` header, else the socket address:

- sign-in, sign-up and password changes: 10 a minute (`RateLimits:AccountPerMinute`)
- review, cook-note and cooked writes: 30 a minute (`RateLimits:ContributionsPerMinute`)
- recipe and variant submissions: 5 an hour (`RateLimits:SubmissionsPerHour`)

These are the defaults in `RateLimitOptions` (`src/KCC.Web/Features/Security/RateLimits.cs`); a `RateLimits` section in
the configuration overrides them.

#### Backoffice extensions

The recipe editors (ingredients, instructions and the icon, with **Suggest with AI**) and the Contributions dashboard
are Lit + TypeScript clients in `src/KCC.Admin/Client` and `src/KCC.Contributions/Client`, on the shared Vite base in
`packages/admin-client-config`. `yarn build:all` at the repository root builds them into their projects'
`wwwroot/App_Plugins/`, which the site serves to the backoffice; without that build the backoffice has no editors
for those fields and no dashboard. Rebuilding a client needs no .NET build: restart the site and reload the
backoffice. Run a client's tests with `yarn workspace @kcc/admin test` or `yarn workspace @kcc/contributions test`.

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

## Frontend Development

### IDE Setup

[VSCode](https://code.visualstudio.com/) + [Volar](https://marketplace.visualstudio.com/items?itemName=Vue.volar) (disable Vetur if installed).

### Frontend Commands

Run from `src/KCC.Web`:

```bash
# Install dependencies
yarn

# Development (Vite + SSR + CSS watching)
yarn dev:all

# Type checking
yarn type-check

# Linting
yarn lint

# Build for production (client + SSR bundles)
yarn build:all
```

### Light and dark ramps

The site ships two colour ramps. A member chooses Device, Light or Dark on Account Settings, and the choice is saved
on the member. A saved Light or Dark is mirrored in the HttpOnly `kcc-ramp` cookie, and `Layout.cshtml` renders it into
`<html data-theme>`. Everyone else, members on Device included, follows `prefers-color-scheme`: an inline script in
`Layout.cshtml` applies it before the first paint, so there is no flash of the wrong ramp, and the markup's light default
holds without JavaScript.

To check a ramp while testing, switch the operating system's appearance, or emulate `prefers-color-scheme` in the
browser's developer tools (Rendering → Emulate CSS media feature) and reload. That works signed out or as a member on
Device; a member with a saved Light or Dark changes Appearance instead.

**Check any visual change in both ramps.** The light ramp is the binding contrast constraint, and
`tests/KCC.ViteTests/Features/Styles/contrast.test.ts` asserts WCAG AA across every token pair in both.

### Combined Test Report

Run every test suite (3 .NET/TUnit + 2 vitest) and produce one self-contained,
tabbed HTML report at `tests/results/combined-report.html` (auto-opened):

```bash
node tests/scripts/run.mjs
```

The command exits non-zero if any suite has failures or fails to run, so it is
CI-friendly. The individual per-suite reports are still produced alongside it.

#### E2E tests

The E2E suite starts its own copy of the site on a free port, with a fresh SQLite database and its own SSR process.
Run `dotnet build` and `yarn build:all` (at the repository root) first, and set `KCC_E2E_MEMBER_USERNAME` and
`KCC_E2E_MEMBER_PASSWORD` (a password of at least 8 characters; on macOS/zsh, in `~/.zshenv`). The site's seeder
creates that member, approved, before any test runs.

### Vue SSR

The application uses Vue 3 server-side rendering. The SSR service:

- Runs on port 3001 in development
- Pre-renders Vue components on the server for better SEO and initial load
- Falls back gracefully to client-side rendering if SSR is unavailable

See [Vite Configuration Reference](https://vite.dev/config/) for build customization.
