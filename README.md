# KitchenCommandCenter

An Umbraco 17 application with Vue 3 server-side rendering (SSR), on SQLite.

## Table of Contents

- [Local Development](#local-development)
- [Architecture](#architecture)
- [Deployment](#deployment)

---

## Local Development

### Quick Development Workflow

1. **Install frontend dependencies** from the repo root (needs the Font Awesome token, see below):

   ```bash
   yarn install
   ```

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

   Umbraco's log prints to the console. If cache instructions are still pending at Umbraco's first cache sync, about
   two minutes after boot (a new database's first-boot import, or an edit made before then), it logs
   `Cache instruction sync did not complete within 00:01:00`: a harmless Umbraco SQLite race that recovers on its own
   about 20 minutes later. A later edit can occasionally hit the same race.

The site is at `https://localhost:58671`; the backoffice is at `/umbraco`.

### Font Awesome Pro

This project renders icons with **Font Awesome Pro** (webfont / CSS), installed from Font Awesome's private npm registry. The registry is configured in committed `.npmrc` files (`src/KCC.Web/.npmrc` and `src/KCC.Admin/Client/.npmrc`); the auth token is **not** committed — it is read from the `FONTAWESOME_NPM_AUTH_TOKEN` environment variable.

Before running `yarn install` in **either** `src/KCC.Web` or `src/KCC.Admin/Client`:

1. Get a token from your Font Awesome account (Account → Tokens).
2. Set `FONTAWESOME_NPM_AUTH_TOKEN` in your environment:
   - PowerShell (persists; reopen the terminal afterward): `setx FONTAWESOME_NPM_AUTH_TOKEN "<token>"`
   - PowerShell (current session only): `$env:FONTAWESOME_NPM_AUTH_TOKEN = "<token>"`
   - bash / zsh: `export FONTAWESOME_NPM_AUTH_TOKEN=<token>`

The same variable must be set wherever the production frontend is built. Never commit the token or any Font Awesome font files — `node_modules/`, `**/wwwroot/assets`, `**/wwwroot/webfonts`, and `src/KCC.Admin/Client/dist/` are all git-ignored.

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

Use consistent field names across content types and widgets:

- **Heading**: Primary title text
- **SubHeading**: Supporting title text
- **Body**: Main content/description

#### Field Ordering

1. **Primary Function**: Core functionality (e.g., item selectors for listing widgets)
2. **Content Fields**: Headings, text, media
3. **Styling Options**: Colors, spacing, layout options

### Development Best Practices

#### Schema and baseline content

- Document types, data types and dictionary items are edited in the backoffice and exported by uSync on save in
  Development. Commit `src/KCC.Web/uSync/v17/`.
- Baseline content (the empty page tree, site settings, taxonomy, status pages) is not exported on save. Edit it in a
  fresh database, then run `curl -sk -X POST https://localhost:58671/api/dev/baseline/export` and commit the result.
  The endpoint refuses to run while seeded recipes exist.
- ModelsBuilder runs in `SourceCodeManual` mode: after a schema change, use Settings → Models Builder → Generate models,
  and commit `Features/Models/Generated`.

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

The site ships two colour ramps. A toggle sits in the header's utility nav; it writes `'light'` or
`'dark'` to `localStorage['kcc-theme']`, and an inline script in `Layout.cshtml` applies it to
`<html data-theme>` before first paint so there is no flash of the wrong ramp.

With no stored choice the site follows `prefers-color-scheme`, defaulting to dark. To force a ramp while
testing, set the key by hand and reload:

```js
localStorage.setItem('kcc-theme', 'light') // or 'dark'
localStorage.removeItem('kcc-theme') // back to following the OS
```

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

The E2E suite starts its own copy of the site on a free port, with a fresh SQLite database and its own SSR process, so
nothing needs setting up beyond building: run `dotnet build` and `yarn build:all` (in `src/KCC.Web`) first. The member
flows return in a later phase and will read `KCC_E2E_MEMBER_USERNAME` / `KCC_E2E_MEMBER_PASSWORD`.

### Vue SSR

The application uses Vue 3 server-side rendering. The SSR service:

- Runs on port 3001 in development
- Pre-renders Vue components on the server for better SEO and initial load
- Falls back gracefully to client-side rendering if SSR is unavailable

See [Vite Configuration Reference](https://vite.dev/config/) for build customization.
