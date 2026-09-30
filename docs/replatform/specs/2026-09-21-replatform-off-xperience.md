# Replatform off Xperience by Kentico

**Status:** approved 2026-09-23, ready for planning. The 2026-09-21 spike was re-measured against `main` @
`d9bcb09` and its external claims re-verified on 2026-09-23; each design section below was reviewed and
approved by the owner in turn.

**Driver:** Xperience by Kentico has no community, hobby or free developer tier — only a 30-day evaluation key.
Kentico publishes no prices; third-party trackers put self-managed at about **$990/month (~$11,880/year)**. The
project began as an Xperience playground; the ideas in it now justify a real public site, and the licence does not.

## 1. Goal

Move Kitchen Command Center off Xperience by Kentico onto **Umbraco 17 LTS**, self-hosted on a Raspberry Pi behind
Cloudflare, for a total running cost under ~$10/month, keeping the Vue 3 SSR front end and the Torn & Waxed
identity intact.

Done means: the site is public on the owner's domain; members can sign up, be approved, sign in and contribute;
search, submissions and moderation work; the owner edits every page, recipe, nav item and UI string in the
backoffice; and a nightly backup has been restored successfully at least once.

## 2. Constraints

Stated by the owner on 2026-09-21 and settled on 2026-09-23.

| Constraint | Value |
|---|---|
| Audience | Personal project. The owner authors; friends read and contribute |
| Admin editing | **Required.** Content is edited in the backoffice, never in code |
| Page Builder | **Dropped.** Home composes from a Block List |
| Search | **Must stay** — faceted, with drill-sideways counts |
| Members | **Must stay.** Open sign-up; the owner approves every account; no email |
| Multilingual | **Dropped entirely** |
| Existing content | **Disposable.** Only the 216 UI-string values carry over; everything else is re-authored or seeded |
| Budget | Under ~$10/month, all in |
| Rewrite appetite | Keep the Vue / Torn & Waxed front end; the back end is rewritten |

Dropping multilingual removes the `{lang}` route prefix, `cms.contentlanguage`, the language-aware content
retrieval, the status-code re-execute language restore in `Program.cs`, the translation table and the language
fallback chain. It is the single largest free simplification available. English URLs never carried a prefix, so
no URL changes shape because of it.

## 3. Decisions

| Area | Decision | § |
|---|---|---|
| Platform | Umbraco 17 LTS, **17.8.0 or later** | 6 |
| Database | SQLite with guardrails; an x86 VPS with SQL Server Express is the documented exit | 6, 18 |
| Schema as code | uSync 17: schema imported at every startup; dictionary keys created at startup but never overwritten; a baseline of content imported on first boot only | 12 |
| Typed models | ModelsBuilder `SourceCodeManual`, committed | 6 |
| Page composition | Block List on Home with three blocks; in-context editing given up | 7 |
| UI strings | Umbraco dictionary items; `KCC.ResourceStrings` and the on-page editor deleted | 7 |
| Contributions data | EF Core in the same SQLite file | 9 |
| Recipe search | First-party in-memory Lucene.NET index, rebuilt whole on change | 9 |
| Membership | Open sign-up, owner approval (`IsApproved`), no email | 8 |
| Moderation | One Contributions dashboard — Waiting, Reviews, Cook notes — with edit and delete | 10 |
| Backoffice extensions | Lit + TypeScript + UUI | 10 |
| Hosting | Raspberry Pi 5 on SSD; the same compose file runs on a VPS | 13 |
| Exposure | Cloudflare Tunnel; Cloudflare Access on `/umbraco`; Tailscale for SSH | 13 |
| Images and deploy | Built on GitHub's arm64 runners, pushed to private GHCR, deployed by pull | 13 |
| Backups | Nightly SQLite `.backup` plus media to R2 (or B2), a dead-man ping, a restore drill | 13 |
| Process | Branch `replatform` from tag `xperience-final`, merged at parity; phases with gates | 15 |
| Not bought | Umbraco Forms, Umbraco Workflow, uSync.Complete | 16 |

## 4. Current state — the Xperience dependency surface

Measured on `main` @ `d9bcb09`, not recalled.

| Surface | Extent |
|---|---|
| Page content types | 13: `Recipe`, `RecipeVariant`, `HomePage`, `PageBuilderPage` (no pages), `RecipeListingPage`, `AccountPage`, `AccountSettingsPage`, `LoginPage`, `RegistrationCompletePage`, `CreateRecipePage`, `AddVariantPage`, `StatusCodePage`, `HeaderNavigation` |
| Reusable content types | 5: `NavLink`, `NavItem`, `CardItem`, `ImageItem`, `LinkItem` |
| Reusable field schemas | 2: `IMetadata`, `IListingMetadata` |
| Page Builder | 6 widgets (Button, Card, CardGrid, Image, SmallHero, Stacker) and 3 sections (Base, BentoBox, MultipleColumn). Only Home has Page Builder content: 4 Base sections and 1 MultipleColumn, holding a CardGrid, 4 Cards, a Stacker and 2 Kentico RichText widgets. BentoBox, Button, Image and SmallHero appear in no content |
| Content | 72 page items: 27 recipes (25 are dev-seeder output; Egg Skillet and Mac & Cheese were written by hand), 32 variants, 11 app and system pages, 2 folders. 27 reusable items (14 cards, 7 nav links, 2 nav items, 3 images, 1 link). 3 taxonomies: AuthStatus (2 tags), RecipeCategories (6), RecipeTags (11) |
| UI strings | 216 keys in the CI repository. The English values exist **only in the local database**. 191 keys are read by Vue/TS; 8 page controllers send curated key lists |
| Custom Info types | 5: `VariantReview`, `VariantCooked`, `VariantCookNote`, `ResourceString`, `ResourceStringTranslation`. Contribution rows are database-only, reference variant, recipe and member by GUID, and have no foreign keys or unique constraints |
| Custom admin | 3 form components (IngredientsEditor, InstructionsEditor, RecipeIconSelector with an AI suggestion); a custom admin home with stats plus a `/admin` redirect middleware; the resource-string app and its on-page editor |
| Contributions admin | `KCC.Contributions`: 2,769 lines of C# and 941 of TSX. Overview rollups, reviews / cook notes / cooked lists, edit pages, recipe and variant tabs, 2 custom permissions. There is no approval step: reviews and notes are public the moment they are written |
| Controllers | 13 page controllers, 7 API controllers, the dev seeder API |
| Search | `Kentico.Xperience.Lucene` 15.0.3; one document per recipe; drill-sideways facets. ~926 lines: ~320 pure Lucene.NET, ~380 Kentico-bound, ~230 neutral |
| Members | ASP.NET Identity over Kentico's member store; sign-in by username; custom first and last name; no roles, external logins, lockout, password reset or account deletion |
| Email | **None is sent anywhere.** Registration requires confirmation, so a new member stays disabled until enabled by hand in SQL |
| Media | `ImageItem` assets on local disk, served raw with no resizing. No member uploads. The AzureStorage package is a dead reference |
| Tests | 206 TUnit unit tests (Moq, Kentico interfaces mocked), 1 integration test against a real database, 27 Playwright E2E tests, ~534 Vitest tests. No Kentico test package is referenced |
| CI | One workflow: SQL Server container, `dbmanager` with the licence key, CI restore, the E2E member enabled by a SQL `UPDATE`, the seeder, every suite. No deploy step |

### What is already portable

The Vue SSR pipeline is first-party: `AddVueSsr` / `UseVueSsr`, `Server.Entry.ts`, `Server.js` (an Express
server), `CollectCss.js`, `VueSsrService.cs` and `Vite.AspNetCore`. Razor renders the header, body and footer as
**Vue template text**; `VueSsrService` posts it through a named `HttpClient` with a Polly circuit breaker to the
Node service, which compiles and renders it and falls back to client-side rendering on failure. Nothing in that
chain calls Kentico except the preview flag and `PreviewJsonUrlSyncMiddleware`, which exists only for Page
Builder preview.

Of 60 `.vue` files, **3** mention Kentico, all in comments. The front end's remaining ties are the injected
`isPreview`, 10 `.stripTilde()` calls for Kentico's `~/` URLs, `PageBuilderMount.ts`, and the preview-only edit
markers `<ResourceString>` adds for the on-page string editor.

The Torn & Waxed kit (`Features/Styles/Torn/*`, the tear generator, the token ramps, the contrast test) touches no
Kentico API and ports unchanged. `IRecipeIconService`, the Anthropic-backed icon picker, is framework-agnostic and
carries over untouched.

### What "approval" currently means

There is no content workflow. `RecipeApiController` creates recipe and variant pages with the page manager and
never publishes them, so a member submission lands as an **unpublished draft** — and, because it sets no parent,
at the **tree root**, outside `/Recipes` and outside the search index. Only the dev seeder publishes. The approval
gate is draft-then-publish; there is no workflow engine to replicate.

## 5. Options considered

| Platform | Verdict |
|---|---|
| **Umbraco 17 LTS** | **Chosen.** .NET 10 (matches `global.json`), MIT, free, near-1:1 feature mapping. Full support to 2027-11-27, security fixes to 2028-11-27 |
| Umbraco 18 | Rejected. A short-term release (end of life 2027-06-25) with nothing this site needs. The next LTS is 21, due 2027-12-09 |
| Orchard Core 3.0.1 | Runner-up. .NET 10, free, supports PostgreSQL, admin extends in plain Razor. Rejected for document-based (YesSql) content modelling, a thinner ecosystem and no block editor as polished |
| Piranha CMS | Not pursued. Lightweight but small; site members would be custom work |
| Headless (Strapi / Payload / Directus) | Rejected. Members, reviews and cook notes still need a .NET home, so the result is two runtimes and two datastores joined by an HTTP hop on a box kept under $10/month. Licensing is not the obstacle |
| No CMS — EF Core + bespoke admin | Rejected, the closest call. Most page types are routes with a few strings and the contribution data is already plain tables, but building the admin is the largest piece of work and Umbraco supplies it free. The hybrid survives inside the chosen design: Umbraco for pages, nav, recipes and the dictionary; EF Core for contributions |

| Database | Verdict |
|---|---|
| **SQLite** | **Chosen.** The only Umbraco-supported database that runs on the Pi; no server process; backups, dev resets and per-run test databases are file operations |
| SQL Server Express | The exit path. Umbraco's primary provider, but it has no ARM64 build (emulation fails) and wants 2 GB of RAM of its own |
| PostgreSQL | Rejected. Runs on ARM, but only through the community `Our.Umbraco.PostgreSql` provider, which Umbraco HQ does not support; every upgrade would wait on one maintainer |

| Recipe search | Verdict |
|---|---|
| **Own Lucene.NET index** | **Chosen.** The existing drill-sideways code runs on the Lucene.Net.Facet version Umbraco already ships |
| Examine 3.10 (in Umbraco 17) | No facets |
| Examine 4.0 | Facets, but no drill-sideways, and not in Umbraco 17 |
| Umbraco Search add-on | Too new: its Examine provider went stable on 2026-09-19 and its README still calls it a work in progress. It moves into core in Umbraco 19 |

## 6. Architecture

### 6.1 Stack

- **Umbraco.Cms 17.x, minimum 17.8.0** (scheduled 2026-10-29), which fixes the SQLite "database table is locked"
  bug caused by `Cache=Shared` together with WAL. Until it ships, development runs 17.7 with `Cache=Shared`
  removed from the connection string by hand; launch waits for 17.8.
- **.NET 10** (`global.json` unchanged); **SQLite** through `Microsoft.Data.Sqlite`; **uSync 17**; **ModelsBuilder**
  in `SourceCodeManual` mode writing to `Features/Models/Generated`, committed; Umbraco's EF Core integration
  (`Umbraco.Cms.Persistence.EFCore`) for contributions; **Lucene.Net.Facet** at whatever version Examine brings,
  never pinned separately.
- **Kept:** `Vite.AspNetCore`, `Microsoft.Extensions.Http.Polly`, `SimpleMvcSitemap`, `RobotsTxtCore`,
  `Anthropic`, `StyleCop.Analyzers`, TUnit, Moq. Umbraco's built-in profiler replaces the Kentico MiniProfiler
  package.
- **Removed:** every `Kentico.*` package, Lucene, Component Registry and the Management API preview included; the
  dead `AzureStorage`, `LigerShark.WebOptimizer.Core`, `Microsoft.Extensions.FileProviders.Embedded`,
  `Kentico.Xperience.Core.Tests` and NUnit entries in `Directory.Packages.props`; the Magick.NET override unless
  something else still needs it.
- **Upgrade policy.** Move to Umbraco 21 LTS after it ships (2027-12-09) and before 17's security support ends
  (2028-11-27). To keep that move cheap, use only APIs that survive Umbraco 18's breaking changes: plain
  `ControllerBase` for public APIs (never `UmbracoApiController`, which 18 removes), `ManagementApiControllerBase`
  for backoffice endpoints, and the async content, member and dictionary services.

### 6.2 Solution layout

| Project | Role after the port |
|---|---|
| `KCC.Web` | The Umbraco host: `Program.cs`, `Features/**` (page controllers, views, Vue, public APIs under `/api`, search, members, SSR, providers, the dev seeder), the generated models, the `uSync` folder |
| `KCC.Contributions` | EF Core context, entities and migrations for reviews, cook notes and cooked marks; read and write services; cascade handlers; the Contributions dashboard (Management API controller and Lit client) |
| `KCC.Admin` | The three property editors (Lit clients), the icon Management API endpoints, `IRecipeIconService`, the icon and unit lists |
| `packages/admin-client-config` | Rewritten from webpack/Babel into the shared Vite + TypeScript base for the two Lit clients |
| `KCC.ResourceStrings` | Deleted |

Each Lit client builds into its project's `wwwroot/App_Plugins/<project>/` with an `umbraco-package.json`
manifest and ships as static web assets.

**Deleted outright**, beyond the packages: `App_Data/CIRepository`, `CDRepository` and `LuceneSearch`; the Kentico
codegen models; `Features/Widgets`, `Features/Sections`, `EditableAreaHelpers`, the `PageBuilderMount*` tag
helpers, `PageBuilderMount.ts` and `PreviewJsonUrlSyncMiddleware`; `Components/Personalization`; `AdminHomePage`;
the Kentico content-retrieval extensions and constants.

### 6.3 Request path

1. Umbraco resolves the URL to a published node.
2. Route hijacking picks `<documentTypeAlias>Controller : RenderController` — today's page controllers, re-based.
   It maps the typed model to the **existing** view model and returns the **existing** `Index.cshtml`.
3. `Layout.cshtml` renders header, body and footer as Vue template text; `VueSsrService` posts it to the Node
   service; the response carries the HTML and the hydration payload. The circuit breaker and the client-side
   fallback are unchanged.

What changes on that path: `isPreview` comes from Umbraco's preview mode and joins the SSR cache key; `<html
lang>` is fixed to English; the Page Builder and resource-string-editor tags leave the layout; `.stripTilde()` and
its call sites go.

Document types carry no Umbraco templates (§19); each page controller returns its view by explicit path, because
the base `Index()` answers 404 when a node has no template. Endpoints outside the content tree are
attribute-routed: `/api/*`, `/error/{code}`, `/sitemap.xml`, `/robots.txt` and `POST /account/logout`. A 404 is
served by a last-chance content finder that returns the 404 status-code node; a 500 goes through
`UseExceptionHandler("/error")`, which renders the 500 node's text with today's hard-coded fallback. Status-code
re-execution is dropped: it would turn the backoffice's bodiless 401s into HTML error pages.
`RobotsTxtDenyAll` stays on until launch; when off, robots.txt disallows `/umbraco`, `/api`, `/account` and
`/error` and points at the sitemap.

### 6.4 Content access and caching

Controllers and view components depend on small first-party query services over Umbraco's published content cache
and navigation service — one per aggregate (recipes, site settings, pages). Only those services touch Umbraco's
read APIs, which keeps the unit tests mockable and confines the 17 → 21 API churn to a few classes. The account
page's "pending review" list is the one read of saved, unpublished content.

Umbraco's published cache replaces `IContentRetriever`'s cache and Kentico's cache dependencies; there is no custom
content cache. Contribution aggregates are cached in `IMemoryCache` and invalidated on write. Dictionary reads go
through Umbraco's cached culture dictionary. The 60-second SSR HTML cache stays. The 2-minute widget output cache
disappears with Page Builder.

### 6.5 Configuration and secrets

- **Connection string:** Umbraco's SQLite form **without `Cache=Shared`**. Umbraco sets WAL mode when it creates
  the file. `Microsoft.Data.Sqlite` retries a busy command until its default 30-second command timeout elapses, and
  Umbraco's own write-lock wait (`Umbraco:CMS:Global:DistributedLockingWriteLockDefaultTimeout`, 5 seconds by
  default) is raised to 30 seconds.
- **Unattended install:** a new database installs itself (`Umbraco:CMS:Unattended`), taking the admin account from
  secrets. The credentials matter only on first boot.
- **Member security:** sign-in locks out after 5 failures for **15 minutes**
  (`MaxFailedAccessAttemptsBeforeLockout`, `MemberDefaultLockoutTimeInMinutes`; Umbraco's default is 30 days). The
  minimum password length stays 8.
- **URLs:** `UmbracoApplicationUrl` is the public `https://` hostname, and HTTPS is enforced.
- **Data protection:** ASP.NET data-protection keys persist under `umbraco/Data/keys`, on the data volume.
- **Telemetry:** Umbraco's telemetry at its minimal level.
- **Secrets never enter git:** the unattended admin account, the imaging HMAC key, the Anthropic key, the tunnel token, the backup
  bucket credentials, the dead-man ping URL, the E2E member. Development uses user-secrets, CI uses Actions
  secrets, the Pi uses a mode-600 `.env`.

## 7. Content model

Aliases keep the Xperience class names in camelCase, so generated models never collide with feature namespaces
(a class `Account` would shadow `KCC.Web.Features.Pages.Account`).

| Document type | Replaces | Notes |
|---|---|---|
| `homePage` (root, `/`) | HomePage | `sections`: a Block List of Rich text, Card grid and Stacker blocks |
| `recipeListingPage` (`/recipes`) | RecipeListingPage | Collection view, last edited first |
| `recipe` | Recipe | Name, description, icon, image, **one** category (only the first was ever indexed), `author` (Member Picker) |
| `recipeVariant` | RecipeVariant | Name, description, icon, images, prep and cook time, servings, nutrition, ingredients and instructions (custom editors, same JSON), tags (multi-picker), `author` |
| `createRecipePage`, `addVariantPage` | CreateRecipePage, AddVariantPage | Children of `recipeListingPage` |
| `accountPage` → `loginPage`, `accountSettingsPage`, `registrationCompletePage` | AccountPage and its children | Routes plus metadata |
| `siteSettings` (root, no page) | HeaderNavigation | Main and utility nav as Block Lists of `navLink` and `navGroup` |
| `contentFolder` (root) → `recipeCategory`, `recipeTag`, `statusCodePage` | `cms.taxonomy`, StatusCodePage | Three root folders — Recipe Categories, Recipe Tags, Status Codes. Categories and tags are curated nodes whose names are the facet labels; a status-code page holds the code, a heading and a body |

- **Blocks.** `richTextBlock` (rich text), `cardGridBlock` (optional heading, column count, cards) and
  `stackerBlock` (heading, body, image, cards). Together they cover everything the current home page does — its
  four-column row is a card grid with a heading. All three share one settings element, `sectionSettings`, with
  today's section options: background (desk, desk two, paper, paper two), width (thin, container, breakout, full),
  and top, bottom and horizontal padding. Blocks render through Razor partials that emit the existing Vue
  components.
- **Element types.** `card` (CardItem's fields), `navLink` and `navGroup`. Each nav element has `showWhen` —
  Always, Signed in or Signed out — replacing the AuthStatus taxonomy; a group's setting governs its links, as
  today. Every link is a core Multi URL Picker (an internal page or a URL, plus target), replacing the Type / Page /
  Url / Target fields.
- **Compositions.** `metadata` replaces `IMetadata`, minus its publish date: `article:published_time` comes from
  the node's create date instead. `IListingMetadata` had no consumer but the dropped `PageBuilderPage`, so it goes.
  The sitemap honours `metadata`'s exclude flag and also skips the account, login, wizard and status-code types.
- **Media.** The core Image media type. Recipe, variant and metadata images are media pickers. Images are served
  through ImageSharp at the sizes the components use, with long cache headers. Members upload nothing.
- **The logo is a static brand asset** in the front end, in both ramps, like the fonts. A fresh database therefore
  depends on no media.
- **Dictionary.** The 216 items sit under one parent per page prefix (`RecipeSearch`, `VariantDetail`, `Shared`,
  …) with their keys unchanged, plus `Shared.Home`, which the breadcrumbs read but CI never had. The Vue contract
  is unchanged — curated `GetStrings()` lists, `provideResourceStrings`, `<ResourceString>`, and a missing key
  still renders as the key — except that `<ResourceString>` drops its preview-only edit markers. Only the
  provider behind it is rewritten.
- **One language**, English, and every type invariant.

## 8. Members, submissions and security

- **Member type.** Umbraco's default member type plus `firstName` and `lastName`.
- **Sign-up** takes username, email and password, as today. The member is created with `IsApproved = false`, and
  the registration-complete page says the account is waiting for approval. The owner signs up the same way to have
  a member account.
- **Sign-in** is by username, with lockout on. An unapproved member sees "waiting for approval" instead of today's
  generic error; a locked-out member sees the lockout message.
- **Profile and password change** port as they are. Email change stays "coming soon".
- **Out of scope, as today:** confirmation email, self-serve password reset, account deletion. The owner resets
  passwords in the Members section. Any of these later needs an email provider (Resend and Brevo have usable free
  tiers).
- **Authors.** Recipes and variants hold `author` as a Member Picker. Author names come from first and last name,
  falling back to username, and are cached. A recipe the owner writes can leave `author` empty or pick the owner's
  own member.
- **Submissions.** Create Recipe and Add Variant go through Umbraco's async content-editing service. A recipe is
  created **under `recipeListingPage`**; a variant under its recipe, which must be a published recipe. Both are saved
  and never published, with `author` set to the member and the icon chosen by `IRecipeIconService`. Members still
  cannot edit a submission once sent. The account page keeps its "pending review" list.
- **Anti-forgery** is validated on every cookie-authenticated POST, PUT and DELETE. The client already sends the
  header.
- **Rate limits** apply per client: 10 a minute for sign-in, sign-up and password change; 30 a minute for review,
  note and cooked writes; 5 an hour for submissions. The client key is `CF-Connecting-IP`: through the tunnel every socket address is `cloudflared`,
  so limiting by socket IP would throttle every visitor together. The header is trustworthy only because the
  tunnel is the sole ingress.
- **Forwarded headers** are trusted from the tunnel container alone, so cookies are `Secure` and generated URLs are
  `https`.
- **Redirects and sign-out.** `returnUrl` must be a local URL. Sign-out becomes a POST; the three places that link
  to it (the header menu, the account page, the settings page) submit a small form instead.
- **Backoffice.** Cloudflare Access protects `<host>/umbraco` with a one-time PIN, allowing only the owner's email;
  the Umbraco login is the second factor. Public APIs stay under `/api`, so Access needs no bypass rules unless an
  `/umbraco/surface` or `/umbraco/api` route ever becomes public.

## 9. Contributions and search

### 9.1 Contributions

The three tables move to EF Core in the same SQLite file, registered with `AddUmbracoDbContext`. Every write goes
through Umbraco's EF Core scope so it shares Umbraco's single-writer lock (§19). Migrations run at startup.

| Table | Columns | Constraint |
|---|---|---|
| Reviews | id, variant key, member key, rating (0.5–5 in half steps), text (≤ 4,000), created, modified | Unique member + variant |
| Cook notes | id, variant key, member key, text (required, ≤ 4,000), created, modified | — |
| Cooked | id, variant key, member key, created | Unique member + variant |

- **Keys.** Rows reference the variant and member keys only. Today's stored recipe GUID is dropped: the recipe is
  derived from the published tree when aggregating, so a moved, trashed or unpublished variant can never leave
  stale or orphaned ratings behind.
- **Cascades.** Permanently deleting a variant, or the recipe above it, deletes its rows; deleting a member deletes
  theirs. A trashed variant keeps its rows, so restoring it from the recycle bin restores its reviews, but it
  counts for nothing while unpublished.
- **Ratings** aggregate over published variants only, cached and invalidated on write.

### 9.2 Recipe search

- **The document is unchanged:** one per published recipe, with the same fields — name and its sort key, content,
  slug, icon, category and its facet, tags and the diet facet, started-by, fastest time, variant count, review
  count, average rating, published date — plus whichever of the Kentico package's base fields the result mapper
  reads. The API contract is unchanged, so the Vue search UI does not change.
- **The query side survives:** the ~320 lines of drill-sideways query, sort, facet, paging and spotlight logic,
  and `BuildDocument`.
- **One rule replaces the Kentico glue: any relevant change rebuilds the whole index.** Relevant means publishing,
  unpublishing, trashing or moving a recipe, variant, category or tag; saving or deleting a member (author names);
  and any review write. Signals are coalesced and the rebuild runs about 2 seconds after the last one; it also runs
  at startup. `RecipeReindexTargetResolver`, `RecipeReindexer` and `VariantReviewSearchModule` are deleted, and
  with them the "unpublish a variant, lose the recipe" bug.
- **The index lives in memory**, index and facet taxonomy together. Each rebuild builds a fresh pair off to the
  side and swaps it in whole, so no search ever sees a half-built index. There are 27 recipes today; a full
  rebuild takes milliseconds and stays well under a second at a few thousand.
- Umbraco's own Examine indexes (backoffice search) stay as Umbraco configures them, under `umbraco/Data/TEMP`,
  and are not backed up.

## 10. Backoffice

- **Technology.** Lit, TypeScript and Umbraco's UUI components — the documented path. Vue works but is
  undocumented and bundles its own runtime into every extension.
- **Property editors** (`KCC.Admin`):
  - **Ingredients** — rows of name, quantity (0 or more), unit (free text with the 15 suggestions) and
    "Eyeballed"; drag to reorder; at least one row, name required; invalid stored JSON shown raw with "Start
    fresh". Stored as `[{"name","quantity","unit","isEyeballed"}]`, eyeballed rows with a null quantity and an
    empty unit.
  - **Instructions** — numbered steps, drag to reorder, renumbered on save. Stored as `[{"step","text"}]`.
  - **Recipe Icon** — the searchable 153-icon grid and **Suggest with AI**, which sends the name and description
    to a Management API endpoint wrapping `IRecipeIconService`. Stored as a string such as `"fa-duotone fa-cheese"`.
  - The icon and unit lists stay single-sourced in C#; the client reads them from the same endpoints.
- **Contributions dashboard** (`KCC.Contributions`), in the Content section, administrators only, behind one
  Management API controller:
  - **Waiting** (the default tab): unapproved members (name, username, email, registered date) with **Approve**;
    never-published recipes and variants, newest first, each linking to its node.
  - **Reviews**: newest first, paged; recipe and variant, member, rating, text, date. **Edit** changes the rating
    (half steps) and the text (≤ 4,000); **Delete** asks first.
  - **Cook notes**: the same without a rating; text is required.
- **Everything else is core Umbraco:** the Members section, Collections, Save and Preview, the Dictionary.
- **Deleted:** the custom admin home, its stats and tiles, `AdminHomePageMiddleware` and `admin-home-redirect.ts`;
  the Contributions overview, listing and edit pages, filters, page tabs and permissions; every React client; the
  webpack/Babel configuration.

## 11. Editing experience

- **A page.** Content → right-click the parent → **Create** → pick the type → name it (the name becomes the URL
  segment) → fill the tabs, with metadata from the composition → on Home, add blocks and their settings →
  **Save**, **Save and Preview** (draft, rendered for real) or **Save and Publish**. The dropdown also offers
  **Schedule**, **Publish with descendants** and **Unpublish**.
- **A new member.** Contributions → **Waiting** → **Approve**. The Members section's Approved toggle does the same.
- **A submission.** Waiting → open the draft → check the fields, ingredients and instructions included → fix what
  needs fixing → **Save and Preview** → **Publish**. The recipe listing's Collection view is the fallback queue.
- **Moderation.** Contributions → **Reviews** or **Cook notes** → **Edit** or **Delete**.
- **UI strings.** Translation → **Dictionary** → the page's group → edit → **Save**. Live immediately.
- **Taxonomy and nav.** Content → Taxonomy, or Site Settings. Publishing a category or tag rebuilds the search
  index.

Given up: in-context Page Builder editing, and on-page UI string editing (§16).

## 12. Environments, schema and seeding

**Schema lives in git; content lives in the production database.**

| | Development | CI and E2E | Production |
|---|---|---|---|
| Database | `umbraco/Data/Umbraco.sqlite.db`, gitignored | A fresh temporary file per run | The `data` volume on the Pi |
| uSync export on save | On | Off | Off |
| ModelsBuilder mode | `SourceCodeManual` | `Nothing` | `Nothing` |
| Test-data seeder | Available | Run by the fixture | Absent |

- **Schema** — document, media, member and data types, and any templates — is imported by uSync at every startup
  in every environment.
- **First boot.** A new database installs unattended, then uSync imports a **baseline**: the empty site tree (home,
  recipes with the two wizards, account with its three children, site settings with the nav, the 6 categories and
  11 tags, the 404 and 500 pages) and the 216 dictionary items with their values. The baseline is authored once in
  the development backoffice — the strings imported from the Phase 0 export — and re-exported whenever it changes,
  as in Phase 6.
- **Dictionary values are content.** uSync's startup import creates dictionary keys that are missing but never
  changes an existing one (`CreateOnly`), so a deploy cannot overwrite a string edited on the live site, while keys
  added by later work still arrive (§19).
- **What exports itself.** In Development, uSync exports schema and dictionary items on save. Content is never
  exported on save, so the seeder's test recipes cannot leak into the baseline: a dev-only endpoint exports the
  baseline content on demand and refuses to run while any recipe exists.
- **Test data.** `POST /api/dev/seed-recipes`, Development and CI only, idempotent, rebuilt on Umbraco services: the
  25 recipes and 29 variants of today's seeder with their tags, two authors, the reviews, a backdated date for the
  "recent" sort, and the **approved** E2E member from `KCC_E2E_MEMBER_USERNAME` / `KCC_E2E_MEMBER_PASSWORD`, which
  retires the SQL `UPDATE`.
- **Dev loop.** `dotnet watch` as today. Resetting means deleting the database file (and the media folder) and
  restarting. There is no SQL Server container, `dbmanager`, CI restore, licence key or hash salt anywhere.
- **Production data down to dev** is one file and the media folder copied over Tailscale; the runbook has the
  commands.

## 13. Hosting, deploy and operations

### 13.1 The box

**Raspberry Pi 5**, 4 GB minimum (8 GB comfortable), booting from **USB SSD or NVMe — not microSD**, which SQLite
and index writes would wear out. A 64-bit OS and Docker Engine with the compose plugin. **Footprint:** .NET and
Umbraco ~300–500 MB, Node SSR ~150–250 MB, `cloudflared` ~30 MB; SQLite is a file, not a process. Under 1 GB in
total.

### 13.2 Compose

One file, identical on the Pi or a VPS. No host ports are published and there is no reverse proxy.

| Service | Details |
|---|---|
| `app` | The ASP.NET image, arm64, non-root, read-only root filesystem. Volume `data` → `umbraco/Data`, which also holds the logs (Serilog's file path points there), the data-protection keys and Umbraco's TEMP; volume `media` → `wwwroot/media`. Both owned by the container user. Health check on `/healthz` |
| `ssr` | Node with the built SSR bundle, non-root, read-only, reachable only from `app`. Health check on `/health` |
| `cloudflared` | Tunnel token from `.env`; one ingress rule, `<host>` → `http://app:8080` |
| `backup` | The nightly job (§13.5), with the data and media volumes mounted read-only |

Every service restarts unless stopped.

### 13.3 Network and exposure

- **Cloudflare Tunnel.** An outbound-only connection from the Pi to Cloudflare's edge: no open router ports, no
  port forwarding, the home IP never exposed, works behind CGNAT, no DDNS, no static IP, TLS terminated at the
  edge. Free. The Free plan caps a request body at 100 MB, plenty for backoffice image uploads.
- **Cloudflare DNS.** Tunnel needs the whole zone on Cloudflare; the CNAME setup is Business-only. The domain is
  registered at **Squarespace**; its nameservers move to Cloudflare in Phase 7, after every existing record (a
  Squarespace site, email) has been recreated in Cloudflare. Apex or subdomain is chosen then; nothing in the
  build depends on it.
- **Cloudflare Access** on `<host>/umbraco`: one-time PIN (added by hand, since new accounts default to Cloudflare
  logins), a policy allowing the owner's email, one seat of the free 50.
- **Tailscale** for SSH and private admin. Funnel is not used for the public site (HTTPS only, no custom domains on
  free, three funnels per tailnet).
- **Segmentation.** With no inbound path into the LAN, the threat model narrows to a compromised app pivoting off
  the Pi. In priority order: non-root containers with read-only root filesystems and only the volumes Umbraco must
  write; `ufw` denying inbound except SSH from the LAN or the tailnet; SSH keys only, password authentication off;
  `unattended-upgrades`; no credential reuse between the Pi and anything else. VLAN isolation is a nice-to-have if
  the router supports it, not the load-bearing control.

### 13.4 Build and deploy

- **Built in CI, never on the Pi.** Multi-stage Dockerfiles for `app` and `ssr`, with the repo root as the build
  context because of the yarn workspace layout. The client and SSR bundles are built **in the same stage**: scope
  IDs hash path and content, so a mismatch silently strips every scoped style. The Font Awesome Pro token is a
  BuildKit secret, never a layer.
- **Images** build on GitHub's `ubuntu-24.04-arm` runners, free because the repository is public, and are pushed to
  **private** GHCR packages tagged with the commit SHA and `main`. Private because the build contains Font Awesome
  Pro files. Old versions are pruned to stay inside GitHub's free private-package allowance; if transfer ever runs
  out, `docker save` / `docker load` over Tailscale is the fallback.
- **Deploy is pull-based.** A systemd timer on the Pi checks for a new `main` digest every 5 minutes with a
  read-only GHCR token. On a change it takes a database `.backup` snapshot, pulls, and restarts the stack; Umbraco,
  uSync and EF Core migrations run on start. CI never gets a path into the home network.
- **Rollback** pins `KCC_IMAGE_TAG` in `.env` to a previous SHA, restoring the pre-deploy snapshot if a migration
  went wrong.

### 13.5 Backups

- **Nightly:** SQLite online `.backup` (consistent under WAL; never a raw file copy) and the media folder,
  compressed, copied by `rclone` to a free S3-compatible bucket — **Cloudflare R2**, with Backblaze B2 as a
  drop-in. Retention: 14 daily, 8 weekly.
- The data-protection keys ride along inside `umbraco/Data`. Umbraco's Examine indexes and the in-memory recipe
  index are rebuilt, not backed up.
- A **healthchecks.io** ping on success; a missed night raises an alert.
- **A restore drill is a gate before launch:** the latest backup restored into a clean local instance, which boots
  and serves the site.

### 13.6 Monitoring and maintenance

Container health checks with restart policies; the backup ping; Umbraco's log viewer over the file logs on the data
volume. Dependabot for NuGet, npm, Docker and Actions. Umbraco 17 patch releases as they ship; the 21 LTS move as
§6.1 describes.

### 13.7 Fallback

The same compose file on an x86 VPS: restore the latest backup, `docker compose up`, change DNS. Moving off the Pi
is not a re-architecture, which is what makes starting on it low-regret. If SQLite itself turns out to be the
problem, SQL Server Express slots in there; it needs ~2 GB of RAM of its own.

## 14. Testing and CI

- **Vitest** (~534): unchanged, apart from tests for deleted code such as `PageBuilderMount`. The backoffice helper
  tests (JSON-array helpers, formatting) move to the Lit clients.
- **TUnit unit tests** (206): pure-logic tests stay as they are; tests that mock Kentico interfaces are re-pointed
  at the first-party query services.
- **Integration tests:** `WebApplicationFactory` over a temporary SQLite database with a real first boot, covering
  member approval and lockout, contribution uniqueness and cascades, and index rebuilds. One of them is the
  **SQLite concurrency test** — parallel review writes, a content save and an index rebuild, repeated, with no lock
  errors and consistent counts. It gates Phase 4.
- **E2E** (27 Playwright tests): the fixture starts the app on a fresh temporary SQLite file, waits for first boot,
  runs the seeder, then the tests. New flows: an unapproved member cannot sign in; an approved one can.
- **CI:** on every PR and push, build (`dotnet build`, `yarn build:all`, the two Lit clients), unit tests and
  Vitest, then E2E; on `main`, also build and push the arm64 images. The combined test report stays. No SQL Server
  container, `dbmanager`, licence key, hash salt or SQL step; the secrets left are the Font Awesome token and the
  E2E member.

## 15. Phasing, branching and gates

- **Reference first.** Tag `xperience-final` on `main`. Export the 216 string values. Capture reference screenshots
  of every page type in both ramps, so parity checks don't depend on the Kentico licence or its SQL Server
  container. A worktree at the tag stays available until launch.
- **Branch.** `replatform`, as `theme-overhaul` was, merged into `main` at parity after Phase 6. Hosting and launch
  then land on `main` as normal PRs. Periodic merges from `main` pick up anything fixed meanwhile.
- **Staying green mid-port.** Kentico and Umbraco cannot share a host, so Phase 1 removes Kentico outright. Code for
  features not yet ported is excluded from compilation along with its tests; each phase brings its slice back and
  deletes its exclusion, so every phase ends buildable and green.
- **Docs.** Each phase corrects the README and CLAUDE.md lines it invalidates — the SFC section's
  `ResourceStringEditorTagHelper` reference goes in Phase 1. Phase 8 does the full rewrite.

| Phase | Delivers | Gate |
|---|---|---|
| **0 Reference** | Tag, string export, reference screenshots | — |
| **1 Foundation** | Umbraco host on SQLite with the guardrails; unattended install; uSync, with the dictionary survival proof; ModelsBuilder; layout and SSR; dictionary and provider; site settings and nav; error pages; sitemap and robots; CI rebuilt without SQL | A fresh clone runs `dotnet watch` and the empty site renders through SSR in both ramps |
| **2 Recipes** | Recipe, variant, listing and taxonomy types; query services; detail controllers; breadcrumbs; image sizes; the contribution tables and their read side; the seeder with recipes, variants, tags, authors and reviews | Seeded recipe and variant pages match the reference screenshots |
| **3 Search** | In-memory index, rebuild triggers, API and listing page | Search E2E green |
| **4 Members and writes** | Sign-up, approval, sign-in, lockout, profile; the hardening; review, note and cooked write APIs with uniqueness and cascades; submissions; the approved E2E member | Member E2E flows and the SQLite concurrency test green |
| **5 Backoffice** | The three property editors; the Contributions dashboard | In the backoffice: approve a member, publish a submission, edit and delete a review, suggest an icon |
| **6 Home** | Block List, three blocks and section settings; home re-authored; baseline re-exported | Home in both ramps; a brand-steward review; then merge to `main` |
| **7 Hosting** | Dockerfiles and the image workflow; compose; the Pi runbook; nameservers to Cloudflare; tunnel; Access; backups; pull-based deploy | The site reachable behind Access; a restore rehearsed from the bucket |
| **8 Launch** | Real content authored in production; `RobotsTxtDenyAll` off; README, CLAUDE.md and memory rewritten; the reference worktree and the old SQL Server container retired | Public |

Every gate also requires `dotnet build` clean (warnings are errors), `yarn build:all` with both bundles built
together, Vitest green, and a browser check of the touched pages in both ramps.

## 16. What is lost

1. **In-context Page Builder editing.** The one real authoring regression. Block List composes in the property
   panel with per-block previews; *Save and Preview* renders the real page. One extra click and a context switch.
2. **On-page UI string editing.** Strings are edited in the Dictionary instead; the editor could return later as
   its own feature.
3. **Multilingual:** the translation table, the fallback chain, the `{lang}` routing.
4. **Activity tracking, contacts and the contact-group personalization condition** — one call site, and no content
   uses them.
5. **Component Registry + MCP** — dev-only tooling with no equivalent.
6. **The custom admin home** and its stats.
7. **Contributions admin breadth:** the overview rollups, the cooked list, the recipe and variant tabs with their
   rating breakdown, filters, custom permissions.
8. **Unused Page Builder pieces:** BentoBox, Button, Image and SmallHero, and the empty `PageBuilderPage` type. A
   generic content page can reuse the home Block List when one is wanted.
9. **The logo as editable content.**
10. **The React admin clients**, replaced by Lit.
11. **PostgreSQL and SQL Server on the Pi.**
12. **Paid add-ons, not bought:** Umbraco Forms (€100/year; no forms exist — the only `Kentico.Forms` reference is a
    Page Builder property attribute), Umbraco Workflow (€2,800/year; core draft/publish suffices), uSync.Complete
    (£1,150; it syncs members, which the seeder and real sign-ups cover).

Never existed, so not lost: email of any kind, password reset, account deletion, a content workflow.

## 17. Defects the port fixes

The code each one lives in is rewritten anyway.

| Today | After the port |
|---|---|
| Member-submitted recipes are created at the tree root, outside `/Recipes` and the search index | Created under `recipeListingPage` |
| Unpublishing or deleting a variant deletes its whole recipe from the search index | Whole-index rebuilds |
| Reviews of deleted variants or members still count toward ratings | Cascades, and ratings over published variants only |
| Nothing prevents duplicate reviews or cooked marks (check-then-insert) | Unique indexes |
| The anti-forgery header is sent but never validated | Validated on every state-changing endpoint |
| No rate limiting | Per-client limits |
| Sign-in redirects to any `returnUrl` | Local URLs only |
| Sign-out works over GET | POST only |
| Add Variant accepts any parent | The parent must be a published recipe |
| Variant cover images never render (a PascalCase read of camelCase JSON) | The image contract is rewritten with the media mapping |
| The sitemap lists account, login and wizard pages | Excluded by type |
| The SSR cache key ignores preview | Keyed on `isPreview` |
| The 404 and 500 text was never serialized, so the fallback text always shows | Status-code pages are baseline content |
| A new member can never sign in without a SQL `UPDATE` | Approved in the backoffice |

Out of scope, because it survives the port unchanged: the "15 min or more" time filter caps at 60 minutes and drops
longer recipes. It is its own task.

## 18. Risks

| Risk | Note |
|---|---|
| **SQLite in production** | Umbraco documents SQLite but never endorses it for a live site; one writer at a time; 17.7's `Cache=Shared` + WAL locking bug. Mitigated by 17.8.0 or later, WAL with a busy timeout, EF Core writes inside Umbraco's scope, and the Phase 4 concurrency test. Exit: an x86 VPS with SQL Server Express |
| **Support window** | 17's full support ends 2027-11-27 and its security fixes 2028-11-27; the 21 LTS move falls in that year. The first-party query services and avoiding the APIs 18 removes keep it small |
| **uSync behaviour** | The first-boot baseline and the dictionary exclusion are proven in Phase 1 before anything depends on them (§19) |
| **Backups** | The thing people skip and regret. Designed as a job with a dead-man ping and a restore-drill gate, not a note |
| **Data-protection keys** | Lost keys sign everyone out and break open forms. They live on the data volume and in the backup |
| **Scope-ID mismatch** | Building the client and SSR bundles separately silently strips every scoped style. One build, one Docker stage |
| **File permissions** | Umbraco must write the SQLite file, media, logs, keys and TEMP. Named volumes owned by the container user; the read-only root filesystem makes every writable path explicit. The classic first-run stumble |
| **Rate-limit key** | `CF-Connecting-IP` is trusted only because the tunnel is the sole ingress. Publishing a host port would make it spoofable |
| **Font Awesome Pro** | The built image contains Pro files: private registry only, the token a build secret |
| **Upload bandwidth** | Residential upload is the ceiling. Cloudflare edge-caches static assets and media; ImageSharp serves right-sized images with long cache headers |
| **Uptime** | Power cuts, ISP resets, reboots. Acceptable for this site; no SLA is promised |
| **ISP terms** | Many residential ISPs nominally prohibit servers. Tunnel traffic looks like outbound HTTPS and is rarely noticed, but it remains the ISP's call |
| **Cloudflare free terms** | Aimed at web content, not bulk video or file serving. A recipe site with images is fine |
| **Hosting market volatility** | Hetzner raised prices twice in 2026 and marked every shared-vCPU plan unavailable as of early September; the cheapest orderable is CPX12 at €11.99. Oracle halved its Always Free ARM tier from 4 OCPU / 24 GB to 2 OCPU / 12 GB on 2026-06-15 and terminated over-limit instances from 08-18. Do not build on a free tier without a migration path |
| **Kentico reference** | The reference instance may stop working when its licence lapses; the Phase 0 screenshots remove the dependency |

## 19. Behaviours the plan proves first

Decided designs whose Umbraco mechanics the first task of their phase verifies. Each has its fallback, so none is
an open question.

| Behaviour | Proven in | If it does not hold |
|---|---|---|
| Route hijacking serves a document type that has no template (Umbraco 17.7's source says it does, provided the controller returns its view by path) | Phase 1 | One shared empty template on every routable type |
| uSync imports the baseline content on first boot only, and its create-only dictionary import never changes an edited value | Phase 1 | The dictionary leaves uSync for a first-boot C# step reading a committed JSON file |
| EF Core writes share Umbraco's SQLite write lock through its scope | Phase 2 | NPoco repositories on Umbraco's own scope |
| The content cache refresher notification fires after the published cache is current | Phase 3 | Rebuild from the content service's published versions |
| A member created unapproved cannot sign in, and approval is that one flag | Phase 4 | The account API checks `IsApproved` itself before signing in |
