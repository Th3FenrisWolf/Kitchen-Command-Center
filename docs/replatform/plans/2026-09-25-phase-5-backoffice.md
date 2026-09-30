# Replatform Phase 5 — Backoffice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Status:** done (2026-09-29), on branch `replatform-phase-5`, which is based on `replatform-phase-4`. **Resume
point:** the Phase 6 plan in this folder; read **Findings from Phase 5** at the end of this file first. **Requires
Phase 4 done** (it is, 2026-09-28).

**Goal:** The owner runs the site from the backoffice. Recipes and variants get their own editors for ingredients,
instructions and the icon, the icon one with **Suggest with AI**. A Contributions dashboard in the Content section
approves members, lists the drafts waiting to be published, and edits or deletes reviews and cook notes. The gate:
in the backoffice, approve a member, publish a submission, edit and delete a review, and suggest an icon.

**Architecture:** Two Lit + TypeScript clients build with Vite from one shared base, `packages/admin-client-config`.
Each writes into its project's `wwwroot/App_Plugins/<project>/`, which the project ships as Razor class library
static web assets. Each bundle is a hashed `bundle-*.js` that registers the client's extensions, and a generated
`umbraco-package.json` points at it.
- `KCC.Admin`'s three property editor UIs attach to the existing data types by their `EditorUIAlias` alone, so the
  schema and the stored values do not change. They read the icon list, the unit list and the AI suggestion from a
  Management API controller that wraps `IRecipeIconService`.
- `KCC.Contributions` gains the dashboard: one Management API controller for administrators, over the contributions
  store and a small member port that the site implements (`IDashboardMembers`), and a Lit dashboard with three tabs.

The React clients and the webpack/Babel toolchain are deleted.

**Tech Stack:** Umbraco.Cms 17.x (the version Phase 1 pinned), with `Umbraco.Cms.Api.Management` at the same version.
The npm package `@umbraco-cms/backoffice` at the same version supplies types only: the backoffice provides its
modules, Lit and UUI at runtime. Vite 8 (Rolldown), TypeScript 6.0, Vitest 4; TUnit 1.27, Moq,
Microsoft.AspNetCore.Mvc.Testing, TUnit.Playwright.

**Spec:** `docs/replatform/specs/2026-09-21-replatform-off-xperience.md`. Sections: §3 (backoffice extensions are Lit,
TypeScript and UUI), §6.1 (the upgrade policy: `ManagementApiControllerBase`), §6.2 (the solution layout and
`App_Plugins`), §9.1 (the rules every contributions write keeps), §10 (the backoffice), §11 (the editing experience),
§14 (Vitest and CI), §15 (the Phase 5 row and gates) and §16 items 6, 7 and 10 (what the dashboard gives up).

## Before you start: reconcile with Phases 1–4 as built

This plan was written on 2026-09-25 from Phase 3's and Phase 4's *plans*, while Phase 2 was running (it had reached
its Task 7, `07c3df1` on branch `replatform-phase-2`). Its code ran against that branch, with the pieces Phases 3 and
4 add copied in from their plans (see "What the scratch probe already proved"). Check each item below. Where the
code differs, adapt the step that depends on it and note the change in your task report.

```bash
grep -n "Status:" docs/replatform/plans/2026-09-23-phase-4-members.md
sed -n '/Unported slices/,/<\/ItemGroup>/p' src/KCC.Admin/KCC.Admin.csproj tests/KCC.UnitTests/KCC.UnitTests.csproj
grep -c "KCC.Admin.csproj" KitchenCommandCenter.sln src/KCC.Web/KCC.Web.csproj
grep -n 'Include="Umbraco.Cms' Directory.Packages.props
sed -n '/public interface IContributionWrites/,/^}/p' src/KCC.Contributions/ContributionWrites.cs
grep -n "private async Task<T> WriteAsync\|private async Task ReviewsChangedAsync\|private async Task DeletedAsync" \
  src/KCC.Contributions/ContributionWrites.cs
sed -n '/public interface IContributionReads/,/^}/p' src/KCC.Contributions/ContributionReads.cs
grep -n "MaxPageSize =\|record Paged\|private static async Task<Paged<T>> PageAsync" src/KCC.Contributions/ContributionReads.cs
grep -n "AddSingleton<IContributionWrites" src/KCC.Contributions/ContributionsComposer.cs
sed -n '/public interface IMemberWriteLock/,/^}/p' src/KCC.Web/Features/Sqlite/MemberWriteLock.cs
grep -n "Task<IReadOnlyDictionary<Guid, string>> ResolveMany" src/KCC.Web/Features/Providers/AuthorNameProvider.cs
sed -n '/public void Compose/,/^    }/p' src/KCC.Web/Features/Providers/ProvidersComposer.cs
grep -n "public static\|public const" tests/KCC.IntegrationTests/Config/TestMembers.cs tests/KCC.IntegrationTests/Config/TestContent.cs
grep -rn "Task WhenCurrentAsync(\|RecipeSearchResults Search(" src/KCC.Web/Features/Search
grep -n "AverageRating {\|ReviewCount {" src/KCC.Web/Features/Search/RecipeSearchResults.cs
grep -n "private Dictionary<string, string> Settings\|Anthropic" tests/KCC.IntegrationTests/Config/UmbracoSite.cs
grep -n "Unattended\|RateLimits__\|UseHttps\|Anthropic\|front-end bundles are missing" tests/KCC.E2ETests/Config/SiteProcess.cs
grep -n "public const string" tests/KCC.E2ETests/Config/MemberSession.cs tests/KCC.E2ETests/Config/MemberTestVariant.cs
grep -n "EditorUIAlias" src/KCC.Web/uSync/v17/DataTypes/KCCIngredients.config \
  src/KCC.Web/uSync/v17/DataTypes/KCCInstructions.config src/KCC.Web/uSync/v17/DataTypes/KCCRecipeIcon.config
grep -c "<Mandatory>true</Mandatory>" src/KCC.Web/uSync/v17/ContentTypes/recipe.config \
  src/KCC.Web/uSync/v17/ContentTypes/recipevariant.config
sed -n '/"resolutions"/,/^  }/p;/"scripts"/,/^  }/p' package.json
grep -n "admin-frontend\|contributions-frontend" tests/scripts/suites.mjs
grep -n "Client/dist/\|^Icon$" .gitignore
grep -c 'placeholder="e.g., Mac & Cheese"\|-prep-time`' src/KCC.Web/Features/Pages/CreateRecipe/CreateRecipeView.Component.vue
grep -c "data-testid=\"review-input\"\|data-testid=\"submit-review\"\|data-testid=\"reviews-list\"" \
  src/KCC.Web/Features/Components/VariantDetail/VariantReviews.vue
```

Expected, and what to do if not:

1. Phase 4's Status is `done`. If not, stop: this phase builds on it.
2. `KCC.Admin.csproj`'s `Unported slices` group holds exactly `FormComponents/IngredientsEditor/**`,
   `FormComponents/InstructionsEditor/**`, `FormComponents/RecipeIconSelector/**`, `Module.cs` and `UIPages/**`.
   `KCC.UnitTests.csproj`'s group holds `Admin/HomeApplicationsResolverTests.cs` and `Admin/HomeStatsServiceTests.cs`,
   plus any lines Phase 6 still owns for its Home slices. Task 1 deletes the `KCC.Admin` group and the two
   `Admin/Home*` lines, and nothing else.
3. Both counts are 1: `KCC.Admin` is in the solution and `KCC.Web` references it (Phase 4's Task 9).
4. `Directory.Packages.props` pins `Umbraco.Cms` and `Umbraco.Cms.Persistence.EFCore` at one version, and has no
   `Umbraco.Cms.Api.Management`. Task 1 adds it at that same version. Write that version wherever this plan says
   `17.7.0`, in `Directory.Packages.props` and in `packages/admin-client-config/package.json`'s
   `@umbraco-cms/backoffice`: if Phase 1 moved to 17.8 or later, both follow.
5. `IContributionWrites` declares Phase 4's eight members: `UpsertReviewAsync`, `DeleteReviewAsync`, `AddNoteAsync`,
   `DeleteOwnNoteAsync`, `MarkCookedAsync`, `UnmarkCookedAsync`, `DeleteForVariantsAsync` and `DeleteForMembersAsync`.
   `ContributionWrites` has the private helpers `WriteAsync<T>`, `ReviewsChangedAsync` and `DeletedAsync`. Task 6 adds
   four members that use `WriteAsync` and `ReviewsChangedAsync`.
6. `IContributionReads` has `ReviewsAsync`, `MemberReviewAsync`, `NotesAsync` and `HasCookedAsync`;
   `ContributionReads.MaxPageSize` is 50; `Paged<T>(IReadOnlyList<T> Items, int Total)` and the private static
   `PageAsync<T>` exist. Task 6 adds two reads beside them.
7. `ContributionsComposer` registers `IContributionWrites` as a singleton. Task 5 adds a scoped registration after it.
8. `IMemberWriteLock` (namespace `KCC.Web.Features.Sqlite`) has `Task RunAsync(Func<Task>)` and
   `Task<T> RunAsync<T>(Func<Task<T>>)`, and `SqliteComposer` registers it.
9. `IAuthorNameProvider.ResolveMany(IEnumerable<Guid>)` returns `Task<IReadOnlyDictionary<Guid, string>>` and leaves out
   keys that have no member. The dashboard shows those as "Deleted member".
10. `ProvidersComposer.Compose` registers `IAuthorNameProvider`, `AnthropicOptions`, `AnthropicClient` and
    `IRecipeIconService` (Phase 4's Task 9). Task 5 adds one registration after `IAuthorNameProvider`.
11. `TestMembers` has `Password`, `UniqueUserName`, `SignUpAsync`, `ApprovedAsync` and `ApproveAsync`. `TestContent`
    has `RecipeListing`, `RecipeAsync`, `VariantAsync`, `Author`, `AuthorAsync`, `DraftRecipeAsync` and
    `DraftVariantAsync`.
12. `IRecipeIndexRebuilder.WhenCurrentAsync(CancellationToken)` and `IRecipeSearchService.Search(RecipeSearchCriteria)`
    exist in `KCC.Web.Features.Search`, and a `RecipeSearchHit` has `double? AverageRating` and `int ReviewCount`.
    Task 6's search test uses them the way Phase 4's `ContributionCascadeTests` does.
13. `UmbracoSite` builds its settings in `Settings()` and sets nothing under `Anthropic`. Task 1 adds one entry.
14. `SiteProcess.SiteEnvironment()` sets the unattended admin to `admin@example.test` / `E2E-Passw0rd-2026` and has
    Phase 4's three `RateLimits__*` entries, and nothing sets `UseHttps` or `Anthropic`. Its `RequireBuildOutput()`
    says "The front-end bundles are missing". Task 8 edits both places.
15. `MemberSession.Serial` and `MemberTestVariant.Path` exist (Phase 4's Task 11).
16. The three data types' `EditorUIAlias` values are `Umb.PropertyEditorUi.TextArea` (ingredients, instructions) and
    `Umb.PropertyEditorUi.TextBox` (icon). `recipe` has 2 mandatory properties (description, icon) and `recipeVariant`
    7 (description, icon, prep time, cook time, servings, ingredients and instructions). Task 8 fills every one of
    them before it publishes.
17. The root `package.json` has the scripts `build:admin`, `build:contributions`, `build:web` and
    `build:all` (the three in that order), and `resolutions` for `react`, `react-dom`, `json-joy`, `react-router`,
    `react-router-dom`, `@types/node` and `uuid`. Task 2 keeps the scripts and trims the resolutions.
18. `suites.mjs` lists the `admin-frontend` and `contributions-frontend` Vitest suites, so the combined run already
    covers both clients.
19. `.gitignore` ignores `src/KCC.Admin/Client/dist/` and `src/KCC.Contributions/Client/dist/`, and has a bare `Icon`
    line. Read "A macOS gitignore trap" below before you name any file.
20. The wizard view has the placeholders and ids Task 8's suite fills (the count is at least 2), and `VariantReviews.vue`
    has the three test ids (3). Phase 4's E2E suites use the same locators.

Before any task starts the dev site, check that nothing else is listening on port 58671.

## What the scratch probe already proved

Everything below ran in a clone of `replatform-phase-2` at `07c3df1`: Phase 1 and Phase 2's Tasks 1–7 as built, on
Umbraco 17.7 and SQLite. The pieces Phases 3 and 4 add were copied in from their plans: `ReviewsChangedNotification`,
Phase 4's `ContributionWrites`, `IMemberWriteLock`, the `KCC.Admin` reference, the icon provider's registration and
key check, `TestContent`'s author and draft helpers, and `TestMembers.SignUpAsync`. This plan's code blocks ran
verbatim, except where noted. The run was green: 97 unit and 100 integration tests (18 of them new), with warnings as
errors and StyleCop on; 17 Vitest tests in the admin client and 5 in the contributions client; and a Playwright
walkthrough of every gate flow against the E2E site, with screenshots in both backoffice themes.

Two things did not run:
- **Task 6's search test.** The clone has no search index; its pattern is Phase 4's `ContributionCascadeTests`.
- **Task 8's suite.** The walkthrough set up its member, submission and review through probe-only endpoints, because
  Phase 4's sign-up, wizard and review flows do not exist on the Phase 2 branch. The suite drives those flows with the
  locators Phase 4's E2E suites use. It compiled against TUnit.Playwright 1.27, and its `SiteProcess` changes ran:
  the fixture checked both backoffice bundles and started, and a backoffice approval flow passed through it.

Task 2's package test ran in its final form (2 passed).

Changed since the probe ran:
- the icon editor's folder is `recipe-icon/`, not `icon/` (see the gitignore trap below);
- the instructions editor's add button says "Add step", the React editor's label;
- both test hosts blank the Anthropic key;
- `KCC.Contributions` gets the Management API package with its controller (Task 5), not with its static web assets
  (Task 2). A build without it was checked: the library still serves its `App_Plugins` files at `/`.

The findings:

- **A Management API controller in a KCC library.** A controller that derives from `ManagementApiControllerBase`,
  with `[ApiVersion("1.0")]`, `[VersionedApiBackOfficeRoute("kcc/…")]`, `[ApiExplorerSettings(GroupName = "KCC")]` and
  an `AuthorizationPolicies` policy, answers at `/umbraco/management/api/v1/kcc/…`. An editor's token passes
  `SectionAccessContent` and gets 403 from `RequireAdminAccess`; no token gets 401. The JSON is camelCase.
- **Backoffice tokens without a browser.** An API user (`UserKind.Api`) with client credentials gets a bearer token
  from `POST /umbraco/management/api/v1/security/back-office/token`. The credentials come from
  `IBackOfficeUserClientCredentialsManager.SaveAsync`, and the client id must start with `umbraco-back-office-`.
  - Over plain HTTP the endpoint answers 400 (OpenIddict ID2083), because `Umbraco:CMS:Global:UseHttps` is on by
    default. The integration client calls it at `https://localhost`, which the test server accepts without TLS.
  - The E2E site listens on plain HTTP, so it turns `UseHttps` off for the backoffice's sign-in.
- **A stale build file hides a new controller.** When a project that `KCC.Web` already references gains its first
  controller, an incremental build keeps the old
  `src/KCC.Web/obj/<configuration>/net10.0/KCC.Web.MvcApplicationPartsAssemblyInfo.cs`. The new library is then no
  MVC application part, and every route of the controller answers 404. Deleting the file fixes it, and so does a
  clean build; a fresh clone and CI never see it. Tasks 1 and 5 delete it.
- **Static web assets.** A Razor class library with `StaticWebAssetBasePath` `/` serves its
  `wwwroot/App_Plugins/<Name>/` at `/App_Plugins/<Name>/`. That works in Development, in the E2E site's Testing
  environment and in the integration host, because Umbraco calls `UseStaticWebAssets()` unless its runtime mode is
  Production.
  - The Management API's `manifest/manifest/private` endpoint lists both packages by name and id, each with its one
    `bundle` extension, whose `js` path answers 200.
  - A client rebuilt while the site is stopped needs no .NET build. At the next start the new hashed bundle answers
    200 and the old one 404.
- **The bundle.** Vite's library mode inlines every asset, even with `assetsInlineLimit: 0`: it put the 353 kB
  duotone font in twice, once from the CSS `url()`. A plain build that fixes the output instead:
  - `src/bundle.ts` is its only input, with `preserveEntrySignatures: 'exports-only'`, `@umbraco…` external and
    hashed file names;
  - the font comes out as its own file, and each element as a lazy chunk;
  - Umbraco cache-busts `App_Plugins` only by the package version, which these packages do not have. So the entry's
    file name carries the hash, and a small Vite plugin writes `umbraco-package.json` pointing at it.
- **Font Awesome in a shadow root.** Browsers ignore `@font-face` inside a shadow root, so the icon editor adds the
  duotone font to the document (`document.fonts.add`). Each element's styles get only the class rules: the CSS
  imported `?raw`, with its font faces stripped. Importing it `?inline` resolves the `url()`s and inlines the font
  again.
- **The editors.**
  - `UmbFormControlMixin` validators stop **Save and publish** on an empty mandatory list or an invalid row, and show
    the message under the field.
  - `UmbSorterController` reorders rows by their drag handle.
  - A `<datalist>` works only for a plain `<input>` in the editor's own shadow root, not for a `uui-input`.
  - The icon editor reads the unsaved name and description from `UMB_PROPERTY_DATASET_CONTEXT`.
- **The data types keep their schema.** Changing only the `EditorUIAlias` in the three uSync data-type files swaps
  the editor. The stored JSON and strings do not change, and neither do the generated models.
- **Umbraco's HTTP client needs a security scheme.** `umbHttpClient` sends the backoffice token only when a call
  passes `security: [{ scheme: 'bearer', type: 'http' }]`. Without it every call answers 401.
- **Waiting.**
  - `IMemberService.FilterAsync(new MemberFilter { IsApproved = false }, …)` finds the unapproved members. It can only
    order by username, name, email or member type, so the list is read whole and sorted by created date.
  - "Never published", as spec §10 puts it, cannot be known. Unpublishing clears the publish date, and the audit log
    and old versions are cleaned up over time. So Waiting lists the recipes and variants that are not published now,
    which is the account page's test for "Pending review".
- **Approval.** The dashboard's Approve sets `IsApproved` on the member through `IMemberService`, inside
  `IMemberWriteLock`: Phase 4's rule for any member save through that service. Phase 4 suggested the member editing
  service instead, but its update takes the member's whole editable state (email, username, name and properties), and
  Approve has no reason to send any of it back.
- **The backoffice in a browser.**
  - The sign-in form at `/umbraco` has `#username-input`, `#password-input` and a **Login** button.
  - The dashboard opens at `/umbraco/section/content/dashboard/contributions`, as the first tab of the Content
    section.
  - Three tests that signed in as the same backoffice user in parallel: one got in, and the other two never left the
    sign-in page (their 60-second wait for the section timed out). One at a time, every flow passed.
  - The console logs `[UmbAuthClient] Token request failed: 400 Bad Request` on every full load of the backoffice,
    in passing runs too. It is noise; do not chase it.
  - **Save and publish** sends `PUT /umbraco/management/api/v1/document/{id}/publish`, so a test can wait for that
    response instead of sleeping.
- **The dark theme.** Umbraco's "Dark (Experimental)" theme (`localStorage['umb-theme-alias'] = 'umb-dark-theme'`)
  shows the editors, the icon picker and the dashboard legibly, because they use UUI's tokens only.
- **A macOS gitignore trap.** `.gitignore`'s `Icon` line lost the two carriage returns that limit it to macOS
  folder-icon files, and with `core.ignorecase` it ignores any file or folder named `icon`. The probe's `src/icon/`
  folder was silently never staged. This plan puts the icon editor in `src/recipe-icon/`; never name a path `icon`.
- **Dead resolutions.** With the React clients and webpack gone, `yarn why` finds nothing that needs the root
  `resolutions` for `react`, `react-dom`, `react-router`, `react-router-dom`, `json-joy` or `uuid`; only `@types/node`
  is still used. `yarn.lock` loses about 6,500 lines.
- **Seeded reviews** belong to made-up reviewer keys, not members, so the dashboard shows their author as "Deleted
  member". Real reviews show the member's name.

## Global Constraints

Phase 1's to Phase 4's constraints still apply:

- Work on branch **`replatform`**. The replatform's spec, phase plans and reference set are tracked in
  `docs/replatform/`: commit changes to them (a status line, a correction) with the work they describe. Everything
  else under `.superpowers/` stays gitignored: never stage anything there. Commit messages are Title Case imperative
  with no attribution lines. Ask the owner before any `git push`.
- **Umbraco.Cms 17.x, never 18.** Every `Umbraco.Cms*` package takes the same version as `Umbraco.Cms`, and so does
  the npm package `@umbraco-cms/backoffice`.
- **The SQLite connection string never contains `Cache=Shared`.**
- **`ModelsMode`** is `SourceCodeManual` in Development and `Nothing` everywhere else. This phase changes no document
  or element type, so it generates no models; uSync exports only the three data types Tasks 3 and 4 edit.
- **Only APIs that survive Umbraco 18.** Public APIs derive from `ControllerBase`, backoffice APIs from
  `ManagementApiControllerBase` (spec §6.1). Warnings are errors, so an obsolete member fails the build (CS0618).
- **Nullable reference types and StyleCop.**
  - Nullable is disabled in `KCC.Web`, `KCC.Contributions`, `KCC.Admin` and `KCC.UnitTests`. Never write `?` on a
    reference type there: it raises CS8632.
  - `KCC.IntegrationTests` and `KCC.E2ETests` have nullable enabled.
  - StyleCop runs on `src/KCC.Web`. It requires sorted usings, trailing commas in multi-line initializers, and static
    members before instance members.
- **Code comments** follow `~/.claude/CLAUDE.md`: explain *why* only, with no narration and no future promises.
- **Every contributions write takes `scope.WriteLock(ContributionLocks.Contributions)` before its first read**, and
  every review write publishes `ReviewsChangedNotification` after its scope has completed and after
  `stats.Invalidate()`. The dashboard's edits and deletes go through `ContributionWrites.WriteAsync` and
  `ReviewsChangedAsync`, so they do.
- **SQLite writes.** Anything that saves a member through Umbraco's member identity or `IMemberService` runs inside
  `IMemberWriteLock.RunAsync`. The relations decorator in `Features/Sqlite` is never removed.
- **Anti-forgery.** KCC's public controllers with a POST, PUT or DELETE carry `[AutoValidateAntiforgeryToken]`. The
  Management API controllers do not, and there is no global filter: the backoffice authenticates with bearer tokens
  and sends no anti-forgery token.
- **Tests that change content make their own nodes**, and assertions about the seed filter by category, diet or a
  seeded word; they never count, or take the spotlight of, the whole site.
- **Test commands:**
  - unit: `dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj`
  - integration: `dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`
  - E2E: `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj` (after `dotnet build` and the root
    `yarn build:all`)
  - one class: append `-- --treenode-filter "/*/*/<ClassName>/*"`
  - front end: `cd src/KCC.Web && yarn test`, then `yarn type-check`
  - everything: `node tests/scripts/run.mjs` (it opens its HTML report when it finishes)
- **The dev site** runs with `cd src/KCC.Web && dotnet run --launch-profile Local` on `https://localhost:58671`.

Phase 5's own constraints:

- **Build from the repository root.** `yarn build:all` at the root builds the admin client, then the contributions
  client, then KCC.Web's client and SSR bundles together. Run it before the integration suite (Task 2's package test
  reads the backoffice bundles) and before the E2E suite. Each client's tests run with
  `yarn workspace @kcc/admin test` and `yarn workspace @kcc/contributions test`; each client's `build` type-checks
  first (`tsc && vite build`).
- **Build outputs stay out of git.** `src/KCC.Admin/wwwroot/App_Plugins/` and
  `src/KCC.Contributions/wwwroot/App_Plugins/` are gitignored and never committed.
- **Never name a file or folder `icon` or `Icon`.** `.gitignore`'s `Icon` line ignores it without a word (see the
  probe's findings).
- **Backoffice endpoints.** Each controller derives from `ManagementApiControllerBase` and carries
  `[ApiVersion("1.0")]`, `[VersionedApiBackOfficeRoute("kcc/<area>")]`, `[ApiExplorerSettings(GroupName = "KCC")]`
  and one `[Authorize(Policy = AuthorizationPolicies.…)]`: `SectionAccessContent` for the editors' endpoints,
  `RequireAdminAccess` for the dashboard's. Routes live under `/umbraco/management/api/v1/kcc/`.
- **Backoffice client calls** go through `tryExecute(host, umbHttpClient.<verb>({ url, …, security }))`, with
  `security = [{ scheme: 'bearer', type: 'http' }]`.
- **Lit elements** extend `UmbLitElement` (or `UmbModalBaseElement`), use UUI components and `--uui-*` tokens only,
  and are registered through their bundle's `manifests`. The Torn & Waxed kit is the public site's and never appears
  in the backoffice. A property editor UI implements `UmbPropertyEditorUiElement` and validates through
  `UmbFormControlMixin`.
- **Test data words.** This phase's recipes are named for minerals (Garnet, Obsidian, Jasper, Malachite, Topaz,
  Basalt), prefixed `IT ` in the integration suites, and its members for composers (Clara Schumann, Lili Boulanger).
  No earlier suite searches for either.
- **No five-star reviews in tests.** A recipe named `IT …` sorts before Legendary Lasagna, the seed's only 5.0, so a
  five-star review would take the listing's Top Rated spotlight, which Phase 3's suites assert. Every review this
  phase writes is 4.5 or lower.
- **The E2E suite** signs in to the backoffice as the fixture's unattended admin, one test at a time, and as the E2E
  member: every backoffice test carries `[NotInParallel(new[] { BackofficeSession.Serial, MemberSession.Serial })]`.
  It waits on Playwright's auto-waiting assertions or on a response (`WaitForResponseAsync`), never on
  `WaitForTimeoutAsync`. Its recipe names start with `Garnet`, so they sort after Avocado Toast Supreme, which Phase
  3's listing test expects first.
- **A throwaway backoffice for looking.** The dev site's admin password is the owner's own, so no agent types it.
  Tasks 3, 4 and 7 end with a look at the backoffice in a disposable site that is started the way the E2E fixture
  starts one, with the fixture's committed test admin (`admin@example.test` / `E2E-Passw0rd-2026`) and the
  integration host's member pair. Create `.superpowers/phase-5/throwaway-site.sh` once (it is gitignored, like
  everything under `.superpowers/`):

  ```zsh
  #!/bin/zsh
  set -e
  repo=$(git rev-parse --show-toplevel)
  run=$(mktemp -d "${TMPDIR:-/tmp}/kcc-backoffice-XXXXXX")
  mkdir -p "$run/tmp"
  export TMPDIR="$run/tmp" TMP="$run/tmp" TEMP="$run/tmp"
  export ASPNETCORE_ENVIRONMENT=Testing
  export ConnectionStrings__umbracoDbDSN="Data Source=$run/Umbraco.sqlite.db;Cache=Private;Foreign Keys=True;Pooling=True"
  export ConnectionStrings__umbracoDbDSN_ProviderName=Microsoft.Data.Sqlite
  export Umbraco__CMS__Unattended__InstallUnattended=true
  export Umbraco__CMS__Unattended__UnattendedUserName="E2E Admin"
  export Umbraco__CMS__Unattended__UnattendedUserEmail=admin@example.test
  export Umbraco__CMS__Unattended__UnattendedUserPassword=E2E-Passw0rd-2026
  export Umbraco__CMS__Global__UseHttps=false
  export Umbraco__CMS__Global__UmbracoMediaPhysicalRootPath="$run/media"
  export Umbraco__CMS__ModelsBuilder__ModelsMode=Nothing
  export Umbraco__CMS__Hosting__LocalTempStorageLocation=EnvironmentTemp
  export Umbraco__CMS__Hosting__SiteName=$(basename "$run")
  export Umbraco__CMS__Examine__LuceneDirectoryFactory=TempFileSystemDirectoryFactory
  export Umbraco__CMS__Logging__Directory="$run/logs"
  export Umbraco__CMS__WebRouting__UmbracoApplicationUrl=http://127.0.0.1:5890/
  export Umbraco__CMS__Imaging__HMACSecretKey=$(openssl rand -base64 64 | tr -d '\n')
  export DataProtection__KeysDirectory="$run/keys"
  export uSync__Settings__ExportOnSave=None
  export VueSsr__Enabled=false
  export Anthropic__ApiKey=
  export KCC_E2E_MEMBER_USERNAME=e2e-member KCC_E2E_MEMBER_PASSWORD=E2E-Member-Passw0rd
  echo "Throwaway site data: $run"
  cd "$repo/src/KCC.Web" && exec dotnet bin/Debug/net10.0/KCC.Web.dll --urls http://127.0.0.1:5890
  ```

  After `dotnet build` and the root `yarn build:all`, start it in the background, wait for it, and seed it:

  ```bash
  zsh .superpowers/phase-5/throwaway-site.sh > .superpowers/phase-5/site.log 2>&1 &
  for i in $(seq 1 90); do curl -s -m 5 http://127.0.0.1:5890/umbraco/management/api/v1/server/status | grep -q '"Run"' && break; sleep 2; done
  curl -s -X POST http://127.0.0.1:5890/api/dev/seed-recipes | head -c 300; echo
  ```

  Then open `http://127.0.0.1:5890/umbraco` in the Browser pane and sign in with the fixture's admin. The panel behind
  the avatar (top right) has a theme choice for the backoffice's dark theme; or run
  `localStorage.setItem('umb-theme-alias', 'umb-dark-theme')` in the console and reload. Stop the site when done
  (`pkill -f 'KCC.Web.dll --urls http://127.0.0.1:5890'`). If whoever runs a task has no Browser pane, the
  controller does this step between tasks.

## Not in Phase 5

- **Phase 6:** Home's Block List and its unported `HomePage` slices.
- **Phase 7:** publishing the backoffice bundles in the image. Its Dockerfile runs the root `yarn build:all` before
  `dotnet publish`, which copies both libraries' `wwwroot` into the app's; a missing bundle is a missing editor, not
  a failed build. Cloudflare Access in front of `/umbraco`, and `UseHttps` behind the tunnel, are Phase 7's too.
- **Phase 8:** the full README and CLAUDE.md rewrite. This phase corrects only the lines it invalidates.
- **Given up, as spec §16 says:** the custom admin home and its stats; the contributions overview rollups, the
  cooked list, the recipe and variant tabs with their rating breakdown, the filters and the custom permissions; the
  React admin clients.
- **Out of scope:** editing a member's details from the dashboard (the Members section does it), and a browser test of
  the dashboard's absence for editors (Task 5's API test covers the policy; the dashboard's `IsAdmin` condition is
  Umbraco's own).

## File map

| Path | Change | Task |
|---|---|---|
| `Directory.Packages.props` | Modify (`Umbraco.Cms.Api.Management`) | 1 |
| `src/KCC.Admin/KCC.Admin.csproj` | Rewrite (Razor class library) | 1 |
| `src/KCC.Admin/{Module.cs,UIPages/,FormComponents/*Editor/,FormComponents/RecipeIconSelector/}` | Delete | 1 |
| `src/KCC.Admin/FormComponents/RecipeUnits.cs` → `src/KCC.Admin/RecipeUnits.cs` | Move | 1 |
| `src/KCC.Admin/RecipeEditorController.cs` | Create | 1 |
| `tests/KCC.UnitTests/Admin/{HomeApplicationsResolverTests,HomeStatsServiceTests}.cs`, `KCC.UnitTests.csproj` | Delete / modify | 1 |
| `tests/KCC.IntegrationTests/Config/{BackofficeClient.cs,UmbracoSite.cs}` | Create / modify | 1 |
| `tests/KCC.IntegrationTests/Features/Backoffice/RecipeEditorApiTests.cs` | Create | 1 |
| `packages/admin-client-config/*` | Rewrite (Vite + TypeScript base) | 2 |
| `src/KCC.Admin/Client/*`, `src/KCC.Contributions/Client/*` | Rewrite (React → Lit scaffolding and helpers) | 2 |
| `src/KCC.Contributions/KCC.Contributions.csproj` | Modify (Razor class library) | 2 |
| `package.json`, `yarn.lock`, `.gitignore` | Modify | 2 |
| `tests/KCC.IntegrationTests/Features/Backoffice/BackofficePackageTests.cs` | Create | 2 |
| `src/KCC.Admin/Client/src/{api.ts,bundle.ts,json-array/*}` | Create / modify | 3 |
| `src/KCC.Web/uSync/v17/DataTypes/{KCCIngredients,KCCInstructions}.config` | Modify (`EditorUIAlias`) | 3 |
| `tests/KCC.IntegrationTests/Features/Backoffice/RecipeEditorDataTypeTests.cs` | Create; grows in 4 | 3, 4 |
| `src/KCC.Admin/Client/src/{api.ts,bundle.ts,recipe-icon/*}` | Create / modify | 4 |
| `src/KCC.Web/uSync/v17/DataTypes/KCCRecipeIcon.config`, `src/KCC.Admin/RecipeIcons.cs` (a comment) | Modify | 4 |
| `src/KCC.Contributions/Dashboard/{IDashboardMembers,DashboardModels,DashboardQueries,ContributionsDashboardController}.cs` | Create; grow in 6 | 5, 6 |
| `src/KCC.Contributions/ContributionsComposer.cs` | Modify | 5 |
| `src/KCC.Web/Features/Providers/{DashboardMembers,ProvidersComposer}.cs` | Create / modify | 5 |
| `tests/KCC.IntegrationTests/Features/Backoffice/ContributionsDashboardApiTests.cs` | Create; grows in 6 | 5, 6 |
| `src/KCC.Contributions/{ContributionWrites,ContributionReads}.cs` | Modify | 6 |
| `src/KCC.Contributions/Client/src/*` | Create / modify | 7 |
| `tests/KCC.E2ETests/Config/{SiteProcess,BackofficeSession}.cs` | Modify / create | 8 |
| `tests/KCC.E2ETests/Features/Backoffice/BackofficeTests.cs` | Create | 8 |
| `README.md`, `CLAUDE.md`, the spec (§10's Waiting line), memory | Modify | 9 |

---

### Task 1: The recipe editor's Management API

Everything else in this phase stands on one mechanism, so it is proven first: a controller in a KCC class library,
reached with a real backoffice token. The controller serves what the property editors need from the server: the
curated icon list, the unit suggestions, and an AI icon suggestion that wraps `IRecipeIconService` (spec §10). The
lists stay single-sourced in C#. `KCC.Admin` becomes a Razor class library, which Task 2 needs for its static web
assets, and loses the Kentico files Phase 4 left excluded.

**Files:**
- Modify: `Directory.Packages.props`, `tests/KCC.UnitTests/KCC.UnitTests.csproj`,
  `tests/KCC.IntegrationTests/Config/UmbracoSite.cs`
- Rewrite: `src/KCC.Admin/KCC.Admin.csproj`
- Move: `src/KCC.Admin/FormComponents/RecipeUnits.cs` → `src/KCC.Admin/RecipeUnits.cs`
- Create: `src/KCC.Admin/RecipeEditorController.cs`, `tests/KCC.IntegrationTests/Config/BackofficeClient.cs`,
  `tests/KCC.IntegrationTests/Features/Backoffice/RecipeEditorApiTests.cs`
- Delete: `src/KCC.Admin/Module.cs`, `src/KCC.Admin/UIPages/`, `src/KCC.Admin/FormComponents/IngredientsEditor/`,
  `src/KCC.Admin/FormComponents/InstructionsEditor/`, `src/KCC.Admin/FormComponents/RecipeIconSelector/`,
  `tests/KCC.UnitTests/Admin/HomeApplicationsResolverTests.cs`, `tests/KCC.UnitTests/Admin/HomeStatsServiceTests.cs`

**Interfaces:**
- Consumes: `IRecipeIconService.PickAsync(string name, string description, IEnumerable<string> ingredients,
  CancellationToken)`, which Phase 4's `ProvidersComposer` registers; `RecipeIcons.All`, `RecipeIcons.Fallback(string)`
  and `RecipeUnits.All`.
- Produces:
  - For any backoffice user with Content access: `GET /umbraco/management/api/v1/kcc/recipe-editor/icons` → `string[]`,
    `GET …/units` → `string[]`, and `POST …/icon-suggestion` with `{ name, description }` → `{ icon }`.
  - `KCC.Admin.RecipeUnits`, now in namespace `KCC.Admin`.
  - `KCC.IntegrationTests.Config.BackofficeClient` (`IDisposable`): `AdministratorAsync(UmbracoSite)` and
    `EditorAsync(UmbracoSite)` → `Task<BackofficeClient>`; `GetAsync(string)`, `PostAsync(string, object? body = null)`,
    `PutAsync(string, object)` and `DeleteAsync(string)` → `Task<HttpResponseMessage>`; `GetJsonAsync(string)` →
    `Task<JsonElement>`, which throws unless the response succeeded.

- [ ] **Step 1: Write the failing tests**

Create `tests/KCC.IntegrationTests/Config/BackofficeClient.cs`:

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Config;

// The backoffice sends its signed-in user's bearer token with every Management API request. An API user with client
// credentials gets the same kind of token without a browser, in whichever user group a test needs.
public sealed class BackofficeClient : IDisposable
{
    private const string Secret = "Integration-Client-Secret-2026";

    private readonly HttpClient client;

    private BackofficeClient(HttpClient client) => this.client = client;

    public static Task<BackofficeClient> AdministratorAsync(UmbracoSite site) => InGroupAsync(site, Constants.Security.AdminGroupKey);

    public static Task<BackofficeClient> EditorAsync(UmbracoSite site) => InGroupAsync(site, Constants.Security.EditorGroupKey);

    public Task<HttpResponseMessage> GetAsync(string path) => client.GetAsync(path);

    public Task<HttpResponseMessage> PostAsync(string path, object? body = null) => client.PostAsync(path, body is null ? null : JsonContent.Create(body));

    public Task<HttpResponseMessage> PutAsync(string path, object body) => client.PutAsJsonAsync(path, body);

    public Task<HttpResponseMessage> DeleteAsync(string path) => client.DeleteAsync(path);

    public async Task<JsonElement> GetJsonAsync(string path)
    {
        using var response = await client.GetAsync(path);
        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    public void Dispose() => client.Dispose();

    private static async Task<BackofficeClient> InGroupAsync(UmbracoSite site, Guid userGroupKey)
    {
        var id = Guid.NewGuid().ToString("N")[..12];
        var clientId = $"umbraco-back-office-it-{id}";
        using (var scope = site.Services.CreateScope())
        {
            var created = await scope.ServiceProvider.GetRequiredService<IUserService>().CreateAsync(
                Constants.Security.SuperUserKey,
                new UserCreateModel
                {
                    Email = $"api-{id}@example.test",
                    UserName = $"api-{id}@example.test",
                    Name = $"API {id}",
                    Kind = UserKind.Api,
                    UserGroupKeys = new HashSet<Guid> { userGroupKey },
                },
                approveUser: true);
            if (!created.Success)
            {
                throw new InvalidOperationException($"Creating an API user failed: {created.Status}.");
            }

            var saved = await scope.ServiceProvider.GetRequiredService<IBackOfficeUserClientCredentialsManager>()
                .SaveAsync(created.Result.CreatedUser!.Key, clientId, Secret);
            if (!saved.Success)
            {
                throw new InvalidOperationException($"Saving client credentials failed: {saved.Result}.");
            }
        }

        // Umbraco's token endpoint refuses plain HTTP while Umbraco:CMS:Global:UseHttps is on, as it is by default.
        var http = site.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var token = await http.PostAsync(
            "/umbraco/management/api/v1/security/back-office/token",
            new FormUrlEncodedContent([new("grant_type", "client_credentials"), new("client_id", clientId), new("client_secret", Secret)]));
        var body = await token.Content.ReadFromJsonAsync<JsonElement>();
        if (!token.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"The token endpoint answered {(int)token.StatusCode}: {body}");
        }

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("access_token").GetString());
        return new BackofficeClient(http);
    }
}
```

Create `tests/KCC.IntegrationTests/Features/Backoffice/RecipeEditorApiTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KCC.Admin;
using KCC.IntegrationTests.Config;

namespace KCC.IntegrationTests.Features.Backoffice;

public class RecipeEditorApiTests
{
    private const string Base = "/umbraco/management/api/v1/kcc/recipe-editor";

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Icons_AreTheCuratedList()
    {
        using var editor = await BackofficeClient.EditorAsync(Site);

        var icons = await editor.GetJsonAsync($"{Base}/icons");

        _ = await Assert.That(icons.EnumerateArray().Select(icon => icon.GetString()!).ToList()).IsEquivalentTo(RecipeIcons.All);
    }

    [Test]
    public async Task Units_AreTheCuratedList()
    {
        using var editor = await BackofficeClient.EditorAsync(Site);

        var units = await editor.GetJsonAsync($"{Base}/units");

        _ = await Assert.That(units.EnumerateArray().Select(unit => unit.GetString()!).ToList()).IsEquivalentTo(RecipeUnits.All);
    }

    [Test]
    public async Task IconSuggestion_WithoutAnAnthropicKey_IsTheNamesFallback()
    {
        using var editor = await BackofficeClient.EditorAsync(Site);

        using var response = await editor.PostAsync($"{Base}/icon-suggestion", new { name = "Garnet Tacos", description = "Crisp shells." });
        var suggestion = await response.Content.ReadFromJsonAsync<JsonElement>();

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(suggestion.GetProperty("icon").GetString()).IsEqualTo(RecipeIcons.Fallback("Garnet Tacos"));
    }

    [Test]
    public async Task Endpoints_WithoutABackofficeToken_AreUnauthorized()
    {
        using var anonymous = Site.CreateClient();

        using var icons = await anonymous.GetAsync($"{Base}/icons");
        using var suggestion = await anonymous.PostAsJsonAsync($"{Base}/icon-suggestion", new { name = "Obsidian Pie", description = string.Empty });

        _ = await Assert.That(icons.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        _ = await Assert.That(suggestion.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
```

The suggestion test relies on the host having no Anthropic key. Add this entry to the dictionary `UmbracoSite.Settings()`
returns, so a key in the machine's environment cannot reach the tests:

```csharp
        // The icon suggestion takes its fallback instead of calling Anthropic, whatever key the environment holds.
        ["Anthropic:ApiKey"] = string.Empty,
```

- [ ] **Step 2: Run the tests to watch them fail**

```bash
dotnet build tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: the build fails with `error CS0103: The name 'RecipeUnits' does not exist in the current context`, because
`RecipeUnits` is still in `KCC.Admin.FormComponents`.

- [ ] **Step 3: Turn KCC.Admin into a Razor class library, without its Kentico files**

In `Directory.Packages.props`, add after the `Umbraco.Cms` line, at the version `Umbraco.Cms` has:

```xml
    <PackageVersion Include="Umbraco.Cms.Api.Management" Version="17.7.0" />
```

Replace `src/KCC.Admin/KCC.Admin.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
        <WarningsNotAsErrors>NU1608;NU1901;NU1902;NU1903</WarningsNotAsErrors>
        <StaticWebAssetBasePath>/</StaticWebAssetBasePath>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="Umbraco.Cms.Api.Management" />
    </ItemGroup>
    <ItemGroup>
        <Content Remove="Client/**" />
    </ItemGroup>
</Project>
```

`StaticWebAssetBasePath` `/` serves the library's `wwwroot/App_Plugins/KCC.Admin/` at `/App_Plugins/KCC.Admin/`, where
Umbraco looks for backoffice packages; Task 2 builds the client there. `Content Remove` keeps the client's sources out
of the library's content, as Umbraco's `umbraco-extension` template does.

Delete the Kentico admin files, move the unit list up a level, and drop the two tests of the Kentico admin home that
Phase 4 left excluded:

```bash
git rm -r -q src/KCC.Admin/Module.cs src/KCC.Admin/UIPages src/KCC.Admin/FormComponents/IngredientsEditor \
  src/KCC.Admin/FormComponents/InstructionsEditor src/KCC.Admin/FormComponents/RecipeIconSelector \
  tests/KCC.UnitTests/Admin/HomeApplicationsResolverTests.cs tests/KCC.UnitTests/Admin/HomeStatsServiceTests.cs
git mv src/KCC.Admin/FormComponents/RecipeUnits.cs src/KCC.Admin/RecipeUnits.cs
sed -i '' 's/^namespace KCC.Admin.FormComponents;/namespace KCC.Admin;/' src/KCC.Admin/RecipeUnits.cs
ls src/KCC.Admin
```

Expected: `Client`, `IRecipeIconService.cs`, `KCC.Admin.csproj`, `RecipeIcons.cs` and `RecipeUnits.cs` (and `bin`/`obj`
if built). If `FormComponents` is still listed, it is an empty folder: `rmdir src/KCC.Admin/FormComponents`.

In `tests/KCC.UnitTests/KCC.UnitTests.csproj`, delete these two lines from the `Unported slices` group, and the group
itself if it is now empty:

```xml
        <Compile Remove="Admin/HomeApplicationsResolverTests.cs" />
        <Compile Remove="Admin/HomeStatsServiceTests.cs" />
```

- [ ] **Step 4: Write the controller**

Create `src/KCC.Admin/RecipeEditorController.cs`:

```csharp
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Web.Common.Authorization;

namespace KCC.Admin;

[ApiVersion("1.0")]
[VersionedApiBackOfficeRoute("kcc/recipe-editor")]
[ApiExplorerSettings(GroupName = "KCC")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessContent)]
public class RecipeEditorController(IRecipeIconService iconService) : ManagementApiControllerBase
{
    [HttpGet("icons")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public IActionResult Icons() => Ok(RecipeIcons.All);

    [HttpGet("units")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public IActionResult Units() => Ok(RecipeUnits.All);

    [HttpPost("icon-suggestion")]
    [ProducesResponseType<IconSuggestion>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SuggestIcon(IconSuggestionRequest request, CancellationToken cancellationToken)
    {
        var icon = await iconService.PickAsync(request.Name ?? string.Empty, request.Description ?? string.Empty, [], cancellationToken);
        return Ok(new IconSuggestion(icon));
    }
}

public sealed record IconSuggestionRequest(string Name, string Description);

public sealed record IconSuggestion(string Icon);
```

- [ ] **Step 5: Run the tests**

`KCC.Web` already referenced `KCC.Admin`, so an incremental build keeps the generated file that lists the MVC
application parts, and the new controller's routes would all answer 404. Delete the file first:

```bash
find src/KCC.Web/obj -name 'KCC.Web.MvcApplicationPartsAssemblyInfo.*' -delete
dotnet build KitchenCommandCenter.sln
grep -c 'ApplicationPartAttribute("KCC.Admin")' src/KCC.Web/obj/Debug/net10.0/KCC.Web.MvcApplicationPartsAssemblyInfo.cs
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/RecipeEditorApiTests/*"
```

Expected: `Build succeeded` with 0 warnings; the count is 1; 4 passed. If the routes answer 404 anyway, the
application-parts file is still stale: delete it again and rebuild.

- [ ] **Step 6: Run both suites**

```bash
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: every test passes. The unit count is unchanged, because the two deleted tests were excluded; `RecipeIconsTests`
still runs. The integration count is Phase 4's plus 4.

- [ ] **Step 7: Commit**

```bash
git add -A Directory.Packages.props src/KCC.Admin tests/KCC.UnitTests tests/KCC.IntegrationTests
git commit -m "Serve the Recipe Editor's Lists and Icon Suggestion from the Management API"
```

---

### Task 2: One Vite base for both backoffice clients

The two React clients and their webpack/Babel build go. Both clients become Lit + TypeScript projects on one shared
Vite base (spec §6.2, §10), and each builds into its project's `wwwroot/App_Plugins/<project>/`, which the project
ships as static web assets. Their bundles register nothing yet; Tasks 3, 4 and 7 add the extensions. The pure helpers
move over now with their Vitest tests (spec §14), so each client's suite, which the combined run already includes,
has something to run from the start. `moveItem` does not come across, because Umbraco's sorter reorders the rows
(Task 3), and neither do the star, page-append and router helpers of the React views spec §16 gives up. CI needs no
change: its frontend step already runs the root `yarn build:all`, and `tests/scripts/run.mjs` already runs both
clients' suites.

**Files:**
- Rewrite: `packages/admin-client-config/package.json`, `packages/admin-client-config/tsconfig.base.json`,
  `src/KCC.Admin/Client/package.json`, `src/KCC.Admin/Client/tsconfig.json`, `src/KCC.Admin/Client/vitest.config.ts`,
  `src/KCC.Contributions/Client/package.json`, `src/KCC.Contributions/Client/tsconfig.json`,
  `src/KCC.Contributions/Client/vitest.config.ts`
- Create: `packages/admin-client-config/vite.ts`, `src/KCC.Admin/Client/vite.config.ts`,
  `src/KCC.Admin/Client/src/bundle.ts`, `src/KCC.Admin/Client/src/json-array/{types,helpers,helpers.test}.ts`,
  `src/KCC.Contributions/Client/vite.config.ts`, `src/KCC.Contributions/Client/src/bundle.ts`,
  `src/KCC.Contributions/Client/src/{format,format.test}.ts`,
  `tests/KCC.IntegrationTests/Features/Backoffice/BackofficePackageTests.cs`
- Modify: `src/KCC.Contributions/KCC.Contributions.csproj`, `package.json`, `yarn.lock`, `.gitignore`
- Delete: `packages/admin-client-config/{babel.config.json,webpack.js}`, and in both `src/KCC.Admin/Client` and
  `src/KCC.Contributions/Client`: `babel.config.js`, `webpack.config.js` and the whole `src/` folder

**Interfaces:**
- Consumes: Task 1's `BackofficeClient`; `KCC.Admin` as a Razor class library.
- Produces:
  - `@kcc/admin-client-config/vite`: `backofficeClient({ packageName, title }: BackofficeClient) → UserConfig`, and
    `@kcc/admin-client-config/tsconfig.base.json`.
  - In each client, `src/bundle.ts` exporting `manifests: Array<UmbExtensionManifest>`, built to
    `src/<project>/wwwroot/App_Plugins/<project>/bundle-<hash>.js` with an `umbraco-package.json` beside it.
  - Admin client, `src/json-array/types.ts`: `Ingredient { name, quantity: number | null, unit, isEyeballed }` and
    `Instruction { step, text }`. `src/json-array/helpers.ts`: `ParseResult<T> { items, error }`, `parseItems<T>`,
    `serializeItems<T>` (undefined for an empty list), `reconcileIncomingValue<T>(value, lastEmitted)` (null when the
    value is the one the editor emitted), `normalizeIngredient`, `stampSteps`, `isIngredientValid`,
    `isInstructionValid`, `formatQuantity` and `formatIngredientSummary`.
  - Contributions client, `src/format.ts`: `ratingSteps` (0.5 to 5 in halves), `maxTextLength` (4000),
    `formatDateTime(iso)`, `formatRating(rating)` (`"4.5 ★"`) and `totalPagesFor(total, pageSize)`.

- [ ] **Step 1: Write the failing test**

Create `tests/KCC.IntegrationTests/Features/Backoffice/BackofficePackageTests.cs`:

```csharp
using System.Net;
using System.Text.Json;
using KCC.IntegrationTests.Config;

namespace KCC.IntegrationTests.Features.Backoffice;

public class BackofficePackageTests
{
    private const string Manifests = "/umbraco/management/api/v1/manifest/manifest/private";

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    [Arguments("KCC.Admin", "KCC Admin")]
    [Arguments("KCC.Contributions", "KCC Contributions")]
    public async Task Package_IsFoundByTheBackoffice_AndItsBundleIsServed(string id, string name)
    {
        using var admin = await BackofficeClient.AdministratorAsync(Site);
        using var anonymous = Site.CreateClient();

        var package = (await admin.GetJsonAsync(Manifests)).EnumerateArray()
            .SingleOrDefault(candidate => candidate.GetProperty("id").GetString() == id);
        if (package.ValueKind == JsonValueKind.Undefined)
        {
            // The bundles are build output, so a missing package usually means the clients were never built.
            throw new InvalidOperationException($"The backoffice has no {id} package. Run `yarn build:all` at the repository root first.");
        }

        var bundle = package.GetProperty("extensions").EnumerateArray().Single();
        var script = bundle.GetProperty("js").GetString()!;
        using var served = await anonymous.GetAsync(script);

        _ = await Assert.That(package.GetProperty("name").GetString()).IsEqualTo(name);
        _ = await Assert.That(bundle.GetProperty("type").GetString()).IsEqualTo("bundle");
        _ = await Assert.That(script).StartsWith($"/App_Plugins/{id}/bundle-");
        _ = await Assert.That(served.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
```

- [ ] **Step 2: Run it to watch it fail**

```bash
yarn build:all
dotnet build tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/BackofficePackageTests/*"
```

Expected: both fail with "The backoffice has no KCC.Admin package" (and KCC.Contributions). The React clients still
build into their `Client/dist/` folders, where Umbraco never looks.

- [ ] **Step 3: Delete the React clients and the webpack/Babel configuration**

```bash
git rm -r -q packages/admin-client-config/babel.config.json packages/admin-client-config/webpack.js \
  src/KCC.Admin/Client/babel.config.js src/KCC.Admin/Client/webpack.config.js src/KCC.Admin/Client/src \
  src/KCC.Contributions/Client/babel.config.js src/KCC.Contributions/Client/webpack.config.js src/KCC.Contributions/Client/src
rm -rf src/KCC.Admin/Client/dist src/KCC.Contributions/Client/dist
git ls-files packages/admin-client-config src/KCC.Admin/Client src/KCC.Contributions/Client
```

Expected: only `packages/admin-client-config/{package.json,tsconfig.base.json}` and, in each client,
`package.json`, `tsconfig.json` and `vitest.config.ts`. The next steps rewrite all of them.

- [ ] **Step 4: Write the shared Vite and TypeScript base**

Replace `packages/admin-client-config/package.json` with (the `@umbraco-cms/backoffice` version is `Umbraco.Cms`'s):

```json
{
  "name": "@kcc/admin-client-config",
  "version": "1.0.0",
  "private": true,
  "type": "module",
  "exports": {
    "./vite": "./vite.ts",
    "./tsconfig.base.json": "./tsconfig.base.json"
  },
  "dependencies": {
    "@umbraco-cms/backoffice": "17.7.0",
    "@umbraco-ui/uui": "^2.0.2",
    "lit": "^3.3.1",
    "rxjs": "^7.8.2",
    "typescript": "~6.0.3",
    "vite": "^8.1.2",
    "vitest": "^4.1.9"
  }
}
```

TypeScript, Vite and Vitest take the same ranges as `src/KCC.Web/package.json`, so the workspace resolves one copy of
each.

Replace `packages/admin-client-config/tsconfig.base.json` with:

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "bundler",
    "lib": ["ES2022", "DOM", "DOM.Iterable"],
    "experimentalDecorators": true,
    "useDefineForClassFields": false,
    "isolatedModules": true,
    "moduleDetection": "force",
    "noEmit": true,
    "skipLibCheck": true,
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true,
    "types": ["@umbraco-cms/backoffice/extension-types", "vite/client"]
  }
}
```

`experimentalDecorators` with `useDefineForClassFields` off is what Lit's `@property` and `@state` need. The
`extension-types` entry declares the global `UmbExtensionManifest` the bundles use.

Create `packages/admin-client-config/vite.ts`:

```ts
import { defineConfig, type Plugin, type UserConfig } from 'vite'

export interface BackofficeClient {
  packageName: string
  title: string
}

// Umbraco only cache-busts an App_Plugins entry by the package version, so the entry file name carries a content
// hash instead, and the manifest that points at it is written once the bundle's file names are known.
function umbracoPackage({ packageName, title }: BackofficeClient): Plugin {
  return {
    name: 'kcc-umbraco-package',
    generateBundle(_, bundle) {
      const entry = Object.values(bundle).find((file) => file.type === 'chunk' && file.isEntry)
      if (!entry) {
        this.error('The build produced no entry chunk.')
      }

      this.emitFile({
        type: 'asset',
        fileName: 'umbraco-package.json',
        source: JSON.stringify(
          {
            id: packageName,
            name: title,
            allowTelemetry: false,
            extensions: [
              {
                type: 'bundle',
                alias: `${packageName}.Bundle`,
                name: `${title} Bundle`,
                js: `/App_Plugins/${packageName}/${entry.fileName}`,
              },
            ],
          },
          null,
          2,
        ),
      })
    },
  }
}

// Not a library build: library mode inlines every asset, and the icon font alone would add 470 kB to a chunk.
export function backofficeClient(client: BackofficeClient): UserConfig {
  return defineConfig({
    base: `/App_Plugins/${client.packageName}/`,
    build: {
      outDir: `../wwwroot/App_Plugins/${client.packageName}`,
      emptyOutDir: true,
      sourcemap: true,
      rolldownOptions: {
        input: 'src/bundle.ts',
        preserveEntrySignatures: 'exports-only',
        external: [/^@umbraco/],
        output: {
          entryFileNames: 'bundle-[hash].js',
          chunkFileNames: '[name]-[hash].js',
          assetFileNames: '[name]-[hash][extname]',
        },
      },
    },
    plugins: [umbracoPackage(client)],
  })
}
```

`@umbraco…` stays external because the backoffice serves those modules through its import map; bundling them would
load a second copy of Lit and the context system.

- [ ] **Step 5: The admin client's scaffolding and helpers**

Replace `src/KCC.Admin/Client/package.json` with:

```json
{
  "name": "@kcc/admin",
  "version": "1.0.0",
  "description": "Backoffice property editors for recipes.",
  "private": true,
  "type": "module",
  "scripts": {
    "build": "tsc && vite build",
    "test": "vitest run"
  },
  "dependencies": {
    "@fortawesome/fontawesome-pro": "^7.3.0",
    "@kcc/admin-client-config": "*"
  }
}
```

Replace `src/KCC.Admin/Client/tsconfig.json` with:

```json
{
  "extends": "@kcc/admin-client-config/tsconfig.base.json",
  "include": ["src"]
}
```

Replace `src/KCC.Admin/Client/vitest.config.ts` with:

```ts
import { defineConfig } from 'vitest/config'

export default defineConfig({
  test: {
    environment: 'node',
    include: ['src/**/*.test.ts'],
  },
})
```

Create `src/KCC.Admin/Client/vite.config.ts`:

```ts
import { backofficeClient } from '@kcc/admin-client-config/vite'

export default backofficeClient({ packageName: 'KCC.Admin', title: 'KCC Admin' })
```

Create `src/KCC.Admin/Client/src/bundle.ts`:

```ts
export const manifests: Array<UmbExtensionManifest> = []
```

Create `src/KCC.Admin/Client/src/json-array/types.ts`:

```ts
export interface Ingredient {
  name: string
  quantity: number | null
  unit: string
  isEyeballed: boolean
}

export interface Instruction {
  step: number
  text: string
}
```

Create `src/KCC.Admin/Client/src/json-array/helpers.test.ts`:

```ts
import { describe, expect, it } from 'vitest'
import {
  formatIngredientSummary,
  formatQuantity,
  isIngredientValid,
  isInstructionValid,
  normalizeIngredient,
  parseItems,
  reconcileIncomingValue,
  serializeItems,
  stampSteps,
} from './helpers.js'
import type { Ingredient, Instruction } from './types.js'

describe('parseItems', () => {
  it('reads nothing from null, empty or blank', () => {
    expect(parseItems(null)).toEqual({ items: [], error: false })
    expect(parseItems('')).toEqual({ items: [], error: false })
    expect(parseItems('   ')).toEqual({ items: [], error: false })
  })

  it('reads a JSON array', () => {
    const result = parseItems<Ingredient>('[{"name":"Bacon","quantity":3,"unit":"Slices","isEyeballed":false}]')
    expect(result.error).toBe(false)
    expect(result.items[0].name).toBe('Bacon')
  })

  it('flags invalid JSON, and JSON that is not an array', () => {
    expect(parseItems('{not json')).toEqual({ items: [], error: true })
    expect(parseItems('{"a":1}')).toEqual({ items: [], error: true })
  })
})

describe('serializeItems', () => {
  it('turns an empty list into no value, so a mandatory property fails validation', () => {
    expect(serializeItems([])).toBeUndefined()
  })

  it('writes a list as compact JSON', () => {
    const items: Instruction[] = [{ step: 1, text: 'Boil water.' }]
    expect(serializeItems(items)).toBe('[{"step":1,"text":"Boil water."}]')
  })
})

describe('reconcileIncomingValue', () => {
  const json = '[{"step":1,"text":"Boil water."}]'

  it('keeps the rows when the value is the one the editor emitted', () => {
    expect(reconcileIncomingValue(json, json)).toBeNull()
    expect(reconcileIncomingValue(undefined, undefined)).toBeNull()
  })

  it('replaces the rows with any other value', () => {
    expect(reconcileIncomingValue<Instruction>(json, '[{"step":1,"text":"edited"}]')).toEqual({
      items: [{ step: 1, text: 'Boil water.' }],
      error: false,
    })
    expect(reconcileIncomingValue(undefined, json)).toEqual({ items: [], error: false })
    expect(reconcileIncomingValue('{not json', json)).toEqual({ items: [], error: true })
  })

  it('reads a value that arrives after the editor started', () => {
    expect(reconcileIncomingValue<Instruction>(json, undefined)?.items).toEqual([{ step: 1, text: 'Boil water.' }])
  })
})

describe('normalizeIngredient', () => {
  it('clears the quantity and unit of an eyeballed ingredient', () => {
    expect(normalizeIngredient({ name: 'Salt', quantity: 2, unit: 'Pinch', isEyeballed: true })).toEqual({
      name: 'Salt',
      quantity: null,
      unit: '',
      isEyeballed: true,
    })
  })

  it('leaves a measured ingredient alone', () => {
    const item: Ingredient = { name: 'Bacon', quantity: 3, unit: 'Slices', isEyeballed: false }
    expect(normalizeIngredient(item)).toEqual(item)
  })
})

describe('stampSteps', () => {
  it('numbers the steps by position', () => {
    expect(stampSteps([{ step: 9, text: 'b' }, { step: 4, text: 'a' }])).toEqual([
      { step: 1, text: 'b' },
      { step: 2, text: 'a' },
    ])
  })
})

describe('validation', () => {
  it('needs an ingredient name and a step text', () => {
    expect(isIngredientValid({ name: 'Bacon', quantity: 1, unit: '', isEyeballed: false })).toBe(true)
    expect(isIngredientValid({ name: '  ', quantity: 1, unit: '', isEyeballed: false })).toBe(false)
    expect(isInstructionValid({ step: 1, text: 'Stir.' })).toBe(true)
    expect(isInstructionValid({ step: 1, text: ' ' })).toBe(false)
  })
})

describe('formatQuantity', () => {
  it('writes whole numbers, common fractions as glyphs, and anything else as a decimal', () => {
    expect(formatQuantity(3)).toBe('3')
    expect(formatQuantity(0.5)).toBe('½')
    expect(formatQuantity(1.5)).toBe('1½')
    expect(formatQuantity(0.4)).toBe('0.4')
  })
})

describe('formatIngredientSummary', () => {
  it('writes a measured, an eyeballed and a unitless ingredient', () => {
    expect(formatIngredientSummary({ name: 'Bacon', quantity: 3, unit: 'Slices', isEyeballed: false })).toBe('3 Slices Bacon')
    expect(formatIngredientSummary({ name: 'Salt', quantity: null, unit: '', isEyeballed: true })).toBe('Salt — to taste')
    expect(formatIngredientSummary({ name: 'Eggs', quantity: 4, unit: '', isEyeballed: false })).toBe('4 Eggs')
  })
})
```

Run it to watch it fail:

```bash
yarn install
yarn workspace @kcc/admin test
```

Expected: `yarn install` rewrites `yarn.lock` for the new dependencies. The suite fails to load `./helpers.js`.

Create `src/KCC.Admin/Client/src/json-array/helpers.ts`:

```ts
import type { Ingredient, Instruction } from './types.js'

export interface ParseResult<T> {
  items: T[]
  error: boolean
}

export function parseItems<T>(value: string | null | undefined): ParseResult<T> {
  if (value == null || value.trim() === '') {
    return { items: [], error: false }
  }

  try {
    const parsed: unknown = JSON.parse(value)
    return Array.isArray(parsed) ? { items: parsed as T[], error: false } : { items: [], error: true }
  } catch {
    return { items: [], error: true }
  }
}

// An empty list is no value at all, so a mandatory property fails validation instead of saving "[]".
export function serializeItems<T>(items: T[]): string | undefined {
  return items.length === 0 ? undefined : JSON.stringify(items)
}

// The editor keeps its own rows so half-typed input survives the value it emits coming straight back in. Anything
// else that arrives (a late initial value, a restored version) replaces the rows.
export function reconcileIncomingValue<T>(
  value: string | null | undefined,
  lastEmitted: string | null | undefined,
): ParseResult<T> | null {
  return value === lastEmitted ? null : parseItems<T>(value)
}

export function normalizeIngredient(item: Ingredient): Ingredient {
  return item.isEyeballed ? { ...item, quantity: null, unit: '' } : item
}

export function stampSteps(items: Instruction[]): Instruction[] {
  return items.map((item, index) => ({ ...item, step: index + 1 }))
}

export function isIngredientValid(item: Ingredient): boolean {
  return item.name.trim().length > 0
}

export function isInstructionValid(item: Instruction): boolean {
  return item.text.trim().length > 0
}

const commonFractions: Record<string, string> = {
  '0.25': '¼',
  '0.33': '⅓',
  '0.5': '½',
  '0.67': '⅔',
  '0.75': '¾',
}

export function formatQuantity(quantity: number): string {
  if (Number.isInteger(quantity)) {
    return String(quantity)
  }

  const whole = Math.floor(quantity)
  const glyph = commonFractions[String(Math.round((quantity - whole) * 100) / 100)]
  if (!glyph) {
    return String(quantity)
  }

  return whole === 0 ? glyph : `${whole}${glyph}`
}

export function formatIngredientSummary(item: Ingredient): string {
  if (item.isEyeballed) {
    return `${item.name} — to taste`
  }

  const quantity = item.quantity == null ? '' : formatQuantity(item.quantity)
  return [quantity, item.unit, item.name]
    .map((part) => part.trim())
    .filter((part) => part.length > 0)
    .join(' ')
}
```

```bash
yarn workspace @kcc/admin test
```

Expected: 14 passed.

- [ ] **Step 6: The contributions client's scaffolding and helpers**

Replace `src/KCC.Contributions/Client/package.json` with:

```json
{
  "name": "@kcc/contributions",
  "version": "1.0.0",
  "description": "The Contributions dashboard in the backoffice.",
  "private": true,
  "type": "module",
  "scripts": {
    "build": "tsc && vite build",
    "test": "vitest run"
  },
  "dependencies": {
    "@kcc/admin-client-config": "*"
  }
}
```

Replace `src/KCC.Contributions/Client/tsconfig.json` with:

```json
{
  "extends": "@kcc/admin-client-config/tsconfig.base.json",
  "include": ["src"]
}
```

Replace `src/KCC.Contributions/Client/vitest.config.ts` with:

```ts
import { defineConfig } from 'vitest/config'

export default defineConfig({
  test: {
    environment: 'node',
    include: ['src/**/*.test.ts'],
  },
})
```

Create `src/KCC.Contributions/Client/vite.config.ts`:

```ts
import { backofficeClient } from '@kcc/admin-client-config/vite'

export default backofficeClient({ packageName: 'KCC.Contributions', title: 'KCC Contributions' })
```

Create `src/KCC.Contributions/Client/src/bundle.ts`:

```ts
export const manifests: Array<UmbExtensionManifest> = []
```

Create `src/KCC.Contributions/Client/src/format.test.ts`:

```ts
import { describe, expect, it } from 'vitest'
import { formatDateTime, formatRating, ratingSteps, totalPagesFor } from './format.js'

describe('formatDateTime', () => {
  it('writes nothing for a missing or unreadable date', () => {
    expect(formatDateTime(null)).toBe('')
    expect(formatDateTime('not-a-date')).toBe('')
  })

  it('reads a timestamp without an offset as UTC', () => {
    expect(formatDateTime('2026-09-25T15:30:00')).toBe(formatDateTime('2026-09-25T15:30:00Z'))
    expect(formatDateTime('2026-09-25T15:30:00Z')).not.toBe('')
  })
})

describe('formatRating', () => {
  it('writes one decimal and a star', () => {
    expect(formatRating(4.5)).toBe('4.5 ★')
    expect(formatRating(3)).toBe('3.0 ★')
  })
})

describe('ratingSteps', () => {
  it('runs from half a star to five in half steps', () => {
    expect(ratingSteps).toHaveLength(10)
    expect(ratingSteps[0]).toBe(0.5)
    expect(ratingSteps.at(-1)).toBe(5)
  })
})

describe('totalPagesFor', () => {
  it('always reports at least one page and rounds a part page up', () => {
    expect(totalPagesFor(0, 20)).toBe(1)
    expect(totalPagesFor(21, 20)).toBe(2)
    expect(totalPagesFor(40, 20)).toBe(2)
  })
})
```

```bash
yarn workspace @kcc/contributions test
```

Expected: the suite fails to load `./format.js`.

Create `src/KCC.Contributions/Client/src/format.ts`:

```ts
export const ratingSteps = [0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4, 4.5, 5]

export const maxTextLength = 4000

export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) {
    return ''
  }

  // The server writes UTC; a timestamp without an offset would otherwise be read as local time.
  const date = new Date(/Z$|[+-]\d\d:\d\d$/.test(iso) ? iso : `${iso}Z`)
  return Number.isNaN(date.getTime())
    ? ''
    : new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(date)
}

export function formatRating(rating: number): string {
  return `${rating.toFixed(1)} ★`
}

export function totalPagesFor(total: number, pageSize: number): number {
  return Math.max(1, Math.ceil(Math.max(0, total) / Math.max(1, pageSize)))
}
```

```bash
yarn workspace @kcc/contributions test
```

Expected: 5 passed.

- [ ] **Step 7: Ship both clients as static web assets**

Apply to `src/KCC.Contributions/KCC.Contributions.csproj` (the Management API package comes with the dashboard's
controller, in Task 5):

```diff
--- a/src/KCC.Contributions/KCC.Contributions.csproj
+++ b/src/KCC.Contributions/KCC.Contributions.csproj
@@ -1,16 +1,21 @@
-<Project Sdk="Microsoft.NET.Sdk">
+<Project Sdk="Microsoft.NET.Sdk.Razor">
 
     <PropertyGroup>
         <TargetFramework>net10.0</TargetFramework>
         <ImplicitUsings>enable</ImplicitUsings>
         <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
         <WarningsNotAsErrors>NU1608;NU1901;NU1902;NU1903</WarningsNotAsErrors>
+        <StaticWebAssetBasePath>/</StaticWebAssetBasePath>
     </PropertyGroup>
 
     <ItemGroup>
         <PackageReference Include="Umbraco.Cms.Persistence.EFCore" />
     </ItemGroup>
 
+    <ItemGroup>
+        <Content Remove="Client/**" />
+    </ItemGroup>
+
     <ItemGroup>
         <InternalsVisibleTo Include="KCC.UnitTests" />
         <InternalsVisibleTo Include="KCC.IntegrationTests" />
```

Apply to `.gitignore`, whose heading named the Kentico clients:

```diff
--- a/.gitignore
+++ b/.gitignore
@@ -501,5 +501,5 @@
-# Kentico admin client module build artifacts
-src/KCC.Admin/Client/dist/
+# Backoffice client build output
+src/KCC.Admin/wwwroot/App_Plugins/
 src/KCC.Admin/Client/node_modules/
-src/KCC.Contributions/Client/dist/
+src/KCC.Contributions/wwwroot/App_Plugins/
 src/KCC.Contributions/Client/node_modules/
```

In the root `package.json`, the React and webpack chains were the only reason for most of the `resolutions`. Leave
only the one still in use:

```json
  "resolutions": {
    "@types/node": "^24.13.2"
  },
```

```bash
yarn install
for package in react react-dom react-router react-router-dom json-joy uuid; do yarn why "$package" 2>&1 | grep -q "couldn't find a match" && echo "$package: gone" || echo "$package: STILL USED"; done
yarn build:all
ls src/KCC.Admin/wwwroot/App_Plugins/KCC.Admin src/KCC.Contributions/wwwroot/App_Plugins/KCC.Contributions
cat src/KCC.Admin/wwwroot/App_Plugins/KCC.Admin/umbraco-package.json
git status --short --ignored src/KCC.Admin/wwwroot src/KCC.Contributions/wwwroot
```

Expected:
- every package prints `gone`. If one is still used, keep its resolution and say which package needs it in the task
  report.
- `yarn build:all` builds the admin client, the contributions client and KCC.Web's two bundles, in that order.
- each folder holds `bundle-<hash>.js`, its `.map` and `umbraco-package.json`, whose one `bundle` extension points at
  `/App_Plugins/KCC.Admin/bundle-<hash>.js`.
- both `wwwroot` folders are listed as ignored (`!!`).

- [ ] **Step 8: Run the tests**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/BackofficePackageTests/*"
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
(cd src/KCC.Web && yarn test && yarn type-check)
```

Expected: 0 warnings; 2 passed, then the whole integration suite; KCC.Web's Vitest suite and type check are
unchanged.

- [ ] **Step 9: Commit**

```bash
git add -A package.json yarn.lock .gitignore packages/admin-client-config src/KCC.Admin/Client src/KCC.Contributions \
  tests/KCC.IntegrationTests
git commit -m "Build the Backoffice Clients with Vite and Lit"
```

---

### Task 3: The ingredients and instructions editors

The two lists get their editors (spec §10): a sortable list of rows over the JSON the site already stores. One
abstract element holds the list, its validation, the read-only summary and the "unreadable value" view. The two
editors differ only in their row fields, labels and the way they prepare the list for saving: ingredients clear the
quantity and unit of an eyeballed row, and steps are renumbered by position. The data types keep `Umbraco.TextArea`
as their schema, so the stored values, the generated models and the site's reading of them do not change; only the
`EditorUIAlias` moves to the new editors.

**Files:**
- Create: `src/KCC.Admin/Client/src/api.ts`,
  `src/KCC.Admin/Client/src/json-array/{json-array-editor,ingredients-editor,instructions-editor}.element.ts`,
  `tests/KCC.IntegrationTests/Features/Backoffice/RecipeEditorDataTypeTests.cs`
- Modify: `src/KCC.Admin/Client/src/bundle.ts`, `src/KCC.Web/uSync/v17/DataTypes/KCCIngredients.config`,
  `src/KCC.Web/uSync/v17/DataTypes/KCCInstructions.config`

**Interfaces:**
- Consumes: Task 1's `GET /umbraco/management/api/v1/kcc/recipe-editor/units`; Task 2's `json-array/types.ts` and
  `json-array/helpers.ts`.
- Produces:
  - `api.ts`: `getRecipeUnits(host: UmbControllerHost) → Promise<string[]>`, empty when the call fails, beside the
    module's `base` and `security` constants, which Task 4's two calls reuse.
  - `KccJsonArrayEditorElement<T>` (abstract), with `labels: JsonArrayLabels { add, empty, invalid, missing }`,
    `summaryTag: 'ul' | 'ol'`, `newItem()`, `isItemValid(item)`, `prepare(items)`, `renderSummary(item)` and
    `renderFields(item, index, change)`.
  - `kcc-ingredients-editor` and `kcc-instructions-editor`, registered as the property editor UIs
    `KCC.PropertyEditorUi.Ingredients` and `KCC.PropertyEditorUi.Instructions` (schema `Umbraco.TextArea`, group
    "Recipes").
  - Markup Task 8's suite relies on: a row is `.row` with a `.handle`; an ingredient's fields are `uui-input.name`,
    `uui-input.quantity`, `input.unit` and a `uui-checkbox` labelled "Eyeballed"; a step's field is a `uui-textarea`;
    the add buttons are labelled "Add ingredient" and "Add step".

- [ ] **Step 1: Write the failing test**

Create `tests/KCC.IntegrationTests/Features/Backoffice/RecipeEditorDataTypeTests.cs`:

```csharp
using KCC.IntegrationTests.Config;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Backoffice;

// The schema stays a text area or text box, so the stored JSON and the generated string properties do not change;
// only the backoffice editor does.
public class RecipeEditorDataTypeTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    [Arguments("KCC Ingredients", "Umbraco.TextArea", "KCC.PropertyEditorUi.Ingredients")]
    [Arguments("KCC Instructions", "Umbraco.TextArea", "KCC.PropertyEditorUi.Instructions")]
    public async Task RecipeDataType_UsesTheKccEditor(string name, string schema, string editorUi)
    {
        var dataType = (await Site.Services.GetRequiredService<IDataTypeService>().GetAllAsync()).Single(type => type.Name == name);

        _ = await Assert.That(dataType.EditorAlias).IsEqualTo(schema);
        _ = await Assert.That(dataType.EditorUiAlias).IsEqualTo(editorUi);
    }
}
```

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/RecipeEditorDataTypeTests/*"
```

Expected: both fail on the second assertion: the UI is still `Umb.PropertyEditorUi.TextArea`.

- [ ] **Step 2: The unit list call**

Create `src/KCC.Admin/Client/src/api.ts`:

```ts
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api'
import { umbHttpClient } from '@umbraco-cms/backoffice/http-client'
import { tryExecute } from '@umbraco-cms/backoffice/resources'

const base = '/umbraco/management/api/v1/kcc/recipe-editor'

// Without a security scheme the backoffice client sends no bearer token.
const security = [{ scheme: 'bearer', type: 'http' }] as const

export async function getRecipeUnits(host: UmbControllerHost): Promise<string[]> {
  const { data } = await tryExecute(host, umbHttpClient.get<{ 200: string[] }>({ url: `${base}/units`, security }))
  return data ?? []
}
```

`tryExecute` shows the backoffice's own error notification when a call fails, so the editors only handle the empty
result.

- [ ] **Step 3: The shared list editor**

Create `src/KCC.Admin/Client/src/json-array/json-array-editor.element.ts`:

```ts
import { css, html, nothing, repeat, property, state } from '@umbraco-cms/backoffice/external/lit'
import type { TemplateResult } from '@umbraco-cms/backoffice/external/lit'
import { UmbChangeEvent } from '@umbraco-cms/backoffice/event'
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element'
import type { UmbPropertyEditorUiElement } from '@umbraco-cms/backoffice/property-editor'
import { UmbSorterController } from '@umbraco-cms/backoffice/sorter'
import { UmbTextStyles } from '@umbraco-cms/backoffice/style'
import { UmbFormControlMixin } from '@umbraco-cms/backoffice/validation'
import { reconcileIncomingValue, serializeItems } from './helpers.js'

interface Row<T> {
  id: string
  item: T
}

export interface JsonArrayLabels {
  add: string
  empty: string
  invalid: string
  missing: string
}

let nextRowId = 0

const toRows = <T>(items: T[]): Array<Row<T>> => items.map((item) => ({ id: `row-${nextRowId++}`, item }))

export abstract class KccJsonArrayEditorElement<T>
  extends UmbFormControlMixin<string | undefined, typeof UmbLitElement, undefined>(UmbLitElement)
  implements UmbPropertyEditorUiElement
{
  @property({ type: Boolean, reflect: true })
  readonly = false

  @property({ type: Boolean })
  mandatory = false

  @state()
  private rows: Array<Row<T>> = []

  @state()
  private parseFailed = false

  #lastEmitted: string | undefined

  #sorter = new UmbSorterController<Row<T>>(this, {
    getUniqueOfElement: (element) => element.dataset.rowId,
    getUniqueOfModel: (row) => row.id,
    identifier: 'KCC.Sorter.JsonArray',
    itemSelector: '.row',
    containerSelector: '.rows',
    handleSelector: '.handle',
    onChange: ({ model }) => {
      this.rows = model
      this.#emit()
    },
  })

  protected abstract readonly labels: JsonArrayLabels

  protected abstract readonly summaryTag: 'ul' | 'ol'

  protected abstract newItem(): T

  protected abstract isItemValid(item: T): boolean

  protected abstract prepare(items: T[]): T[]

  protected abstract renderSummary(item: T): string

  protected abstract renderFields(item: T, index: number, change: (next: T) => void): TemplateResult

  constructor() {
    super()
    this.addValidator('valueMissing', () => this.labels.missing, () => this.mandatory && !this.parseFailed && this.rows.length === 0)
    this.addValidator('customError', () => this.labels.invalid, () => this.rows.some((row) => !this.isItemValid(row.item)))
  }

  override set value(value: string | undefined) {
    const reconciled = reconcileIncomingValue<T>(value, this.#lastEmitted)
    super.value = value
    if (reconciled) {
      this.#lastEmitted = value
      this.parseFailed = reconciled.error
      this.#setRows(toRows(reconciled.items))
    }
  }

  override get value(): string | undefined {
    return super.value
  }

  #setRows(rows: Array<Row<T>>) {
    this.rows = rows
    this.#sorter.setModel(rows)
  }

  #emit() {
    const value = serializeItems(this.prepare(this.rows.map((row) => row.item)))
    this.#lastEmitted = value
    this.value = value
    this.dispatchEvent(new UmbChangeEvent())
  }

  #change(id: string, item: T) {
    this.#setRows(this.rows.map((row) => (row.id === id ? { id, item } : row)))
    this.#emit()
  }

  #add() {
    this.#setRows([...this.rows, ...toRows([this.newItem()])])
    this.pristine = false
    this.#emit()
  }

  #remove(id: string) {
    this.#setRows(this.rows.filter((row) => row.id !== id))
    this.pristine = false
    this.#emit()
  }

  #startFresh() {
    this.parseFailed = false
    this.#setRows([])
    this.#emit()
  }

  override render() {
    if (this.parseFailed) {
      return this.#renderUnreadable()
    }

    return this.readonly ? this.#renderSummary() : this.#renderRows()
  }

  #renderUnreadable() {
    return html`
      <div class="unreadable">
        <p>This field holds data that is not a JSON list, so it cannot be edited here. The stored value is below.</p>
        <pre>${this.value}</pre>
        ${this.readonly
          ? nothing
          : html`<uui-button look="secondary" color="danger" label="Start fresh (clears this field)" @click=${this.#startFresh}></uui-button>`}
      </div>
    `
  }

  #renderSummary() {
    if (this.rows.length === 0) {
      return html`<p class="empty">${this.labels.empty}</p>`
    }

    const items = this.rows.map((row) => html`<li>${this.renderSummary(row.item)}</li>`)
    return this.summaryTag === 'ol' ? html`<ol>${items}</ol>` : html`<ul>${items}</ul>`
  }

  #renderRows() {
    return html`
      <div class="rows">
        ${repeat(
          this.rows,
          (row) => row.id,
          (row, index) => html`
            <div class="row ${this.isItemValid(row.item) ? '' : 'invalid'}" data-row-id=${row.id}>
              <uui-symbol-drag-handle class="handle" title="Drag to reorder"></uui-symbol-drag-handle>
              <div class="fields">${this.renderFields(row.item, index, (next) => this.#change(row.id, next))}</div>
              <uui-button compact label="Remove" look="outline" color="danger" @click=${() => this.#remove(row.id)}>
                <uui-icon name="icon-trash"></uui-icon>
              </uui-button>
            </div>
          `,
        )}
      </div>
      <uui-button look="placeholder" label=${this.labels.add} @click=${this.#add}></uui-button>
    `
  }

  static override readonly styles = [
    UmbTextStyles,
    css`
      :host {
        display: block;
      }

      .rows {
        display: grid;
        gap: var(--uui-size-space-3);
        margin-bottom: var(--uui-size-space-3);
      }

      .row {
        display: grid;
        grid-template-columns: auto 1fr auto;
        gap: var(--uui-size-space-3);
        align-items: start;
        padding: var(--uui-size-space-3);
        border: 1px solid var(--uui-color-border);
        border-radius: var(--uui-border-radius);
        background: var(--uui-color-surface);
      }

      .row.invalid {
        border-color: var(--uui-color-invalid);
      }

      .handle {
        cursor: grab;
        align-self: center;
      }

      .--umb-sorter-placeholder {
        visibility: hidden;
      }

      uui-button[look='placeholder'] {
        width: 100%;
      }

      .unreadable pre {
        white-space: pre-wrap;
        word-break: break-all;
        background: var(--uui-color-surface-alt);
        padding: var(--uui-size-space-3);
        border-radius: var(--uui-border-radius);
      }

      .empty {
        color: var(--uui-color-text-alt);
      }
    `,
  ]
}
```

What each part is for:
- `value` compares what arrives with what the editor last emitted (`reconcileIncomingValue`), so the rows, and a
  half-typed field in them, survive their own round trip through the workspace.
- `valueMissing` fires only for a mandatory property with no rows, and never while the stored value is unreadable:
  "Start fresh" decides that.
- Adding or removing a row marks the field as touched (`pristine = false`), so a new empty row shows its message at
  once instead of at the next save. An invalid row is outlined in red whatever the field's state.
- The read-only view is a plain list, so a user without update rights still sees the recipe.

- [ ] **Step 4: The two editors**

Create `src/KCC.Admin/Client/src/json-array/ingredients-editor.element.ts`:

```ts
import { css, customElement, html, state } from '@umbraco-cms/backoffice/external/lit'
import { getRecipeUnits } from '../api.js'
import { formatIngredientSummary, isIngredientValid, normalizeIngredient } from './helpers.js'
import { KccJsonArrayEditorElement } from './json-array-editor.element.js'
import type { Ingredient } from './types.js'

@customElement('kcc-ingredients-editor')
export class KccIngredientsEditorElement extends KccJsonArrayEditorElement<Ingredient> {
  protected override readonly labels = {
    add: 'Add ingredient',
    empty: 'No ingredients yet.',
    invalid: 'Every ingredient needs a name.',
    missing: 'Add at least one ingredient.',
  }

  protected override readonly summaryTag = 'ul'

  @state()
  private units: string[] = []

  override connectedCallback() {
    super.connectedCallback()
    void getRecipeUnits(this).then((units) => (this.units = units))
  }

  protected override newItem(): Ingredient {
    return { name: '', quantity: null, unit: '', isEyeballed: false }
  }

  protected override isItemValid(item: Ingredient): boolean {
    return isIngredientValid(item)
  }

  protected override prepare(items: Ingredient[]): Ingredient[] {
    return items.map(normalizeIngredient)
  }

  protected override renderSummary(item: Ingredient): string {
    return formatIngredientSummary(item)
  }

  // The unit is a plain input: a datalist must sit in the same shadow root as the input that lists it, and
  // uui-input keeps its input in a shadow root of its own.
  protected override renderFields(item: Ingredient, index: number, change: (next: Ingredient) => void) {
    return html`
      <div class="ingredient">
        <uui-input
          class="name"
          label="Ingredient ${index + 1} name"
          placeholder="Ingredient name"
          .value=${item.name}
          @input=${(event: Event) => change({ ...item, name: (event.target as HTMLInputElement).value })}></uui-input>
        <uui-input
          class="quantity"
          type="number"
          min="0"
          step="any"
          label="Ingredient ${index + 1} quantity"
          placeholder="Qty"
          ?disabled=${item.isEyeballed}
          .value=${item.quantity == null ? '' : String(item.quantity)}
          @input=${(event: Event) => {
            const raw = (event.target as HTMLInputElement).value
            change({ ...item, quantity: raw === '' ? null : Number(raw) })
          }}></uui-input>
        <input
          class="unit"
          list="units"
          aria-label="Ingredient ${index + 1} unit"
          placeholder="Unit"
          ?disabled=${item.isEyeballed}
          .value=${item.unit}
          @input=${(event: Event) => change({ ...item, unit: (event.target as HTMLInputElement).value })} />
        <uui-checkbox
          label="Eyeballed"
          ?checked=${item.isEyeballed}
          @change=${(event: Event) =>
            change(
              (event.target as HTMLInputElement).checked
                ? { ...item, isEyeballed: true, quantity: null, unit: '' }
                : { ...item, isEyeballed: false },
            )}></uui-checkbox>
      </div>
    `
  }

  override render() {
    return html`${super.render()}
      <datalist id="units">${this.units.map((unit) => html`<option value=${unit}></option>`)}</datalist>`
  }

  static override readonly styles = [
    ...KccJsonArrayEditorElement.styles,
    css`
      .ingredient {
        display: grid;
        grid-template-columns: minmax(10rem, 2fr) minmax(4rem, 0.6fr) minmax(6rem, 1fr) auto;
        gap: var(--uui-size-space-3);
        align-items: center;
      }

      .unit {
        box-sizing: border-box;
        height: var(--uui-size-11);
        padding: 0 var(--uui-size-space-3);
        border: 1px solid var(--uui-color-border);
        border-radius: var(--uui-border-radius);
        background: var(--uui-color-surface);
        color: var(--uui-color-text);
        font: inherit;
      }

      .unit:hover {
        border-color: var(--uui-color-border-emphasis);
      }

      .unit:focus {
        outline: calc(2px * var(--uui-show-focus-outline, 1)) solid var(--uui-color-focus);
      }

      .unit:disabled {
        background: var(--uui-color-disabled);
        color: var(--uui-color-disabled-contrast);
        border-color: var(--uui-color-disabled-standalone);
      }
    `,
  ]
}

export default KccIngredientsEditorElement

declare global {
  interface HTMLElementTagNameMap {
    'kcc-ingredients-editor': KccIngredientsEditorElement
  }
}
```

The unit input copies `uui-input`'s look from UUI's tokens, so it follows the backoffice theme like its neighbours.

Create `src/KCC.Admin/Client/src/json-array/instructions-editor.element.ts`:

```ts
import { css, customElement, html } from '@umbraco-cms/backoffice/external/lit'
import { isInstructionValid, stampSteps } from './helpers.js'
import { KccJsonArrayEditorElement } from './json-array-editor.element.js'
import type { Instruction } from './types.js'

@customElement('kcc-instructions-editor')
export class KccInstructionsEditorElement extends KccJsonArrayEditorElement<Instruction> {
  protected override readonly labels = {
    add: 'Add step',
    empty: 'No steps yet.',
    invalid: 'Every step needs a description.',
    missing: 'Add at least one step.',
  }

  protected override readonly summaryTag = 'ol'

  protected override newItem(): Instruction {
    return { step: 0, text: '' }
  }

  protected override isItemValid(item: Instruction): boolean {
    return isInstructionValid(item)
  }

  protected override prepare(items: Instruction[]): Instruction[] {
    return stampSteps(items)
  }

  protected override renderSummary(item: Instruction): string {
    return item.text
  }

  protected override renderFields(item: Instruction, index: number, change: (next: Instruction) => void) {
    return html`
      <div class="instruction">
        <span class="step">${index + 1}</span>
        <uui-textarea
          label="Step ${index + 1}"
          placeholder="Step description"
          auto-height
          .value=${item.text}
          @input=${(event: Event) => change({ ...item, text: (event.target as HTMLTextAreaElement).value })}></uui-textarea>
      </div>
    `
  }

  static override readonly styles = [
    ...KccJsonArrayEditorElement.styles,
    css`
      .instruction {
        display: grid;
        grid-template-columns: 2rem 1fr;
        gap: var(--uui-size-space-3);
        align-items: start;
      }

      .step {
        font-weight: 700;
        line-height: var(--uui-size-11);
        text-align: center;
      }
    `,
  ]
}

export default KccInstructionsEditorElement

declare global {
  interface HTMLElementTagNameMap {
    'kcc-instructions-editor': KccInstructionsEditorElement
  }
}
```

The labels are the React editors' own, word for word.

- [ ] **Step 5: Register them, and point the data types at them**

Replace `src/KCC.Admin/Client/src/bundle.ts` with:

```ts
export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'propertyEditorUi',
    alias: 'KCC.PropertyEditorUi.Ingredients',
    name: 'KCC Ingredients Property Editor UI',
    element: () => import('./json-array/ingredients-editor.element.js'),
    meta: {
      label: 'KCC Ingredients',
      icon: 'icon-bulleted-list',
      group: 'Recipes',
      propertyEditorSchemaAlias: 'Umbraco.TextArea',
      supportsReadOnly: true,
    },
  },
  {
    type: 'propertyEditorUi',
    alias: 'KCC.PropertyEditorUi.Instructions',
    name: 'KCC Instructions Property Editor UI',
    element: () => import('./json-array/instructions-editor.element.js'),
    meta: {
      label: 'KCC Instructions',
      icon: 'icon-ordered-list',
      group: 'Recipes',
      propertyEditorSchemaAlias: 'Umbraco.TextArea',
      supportsReadOnly: true,
    },
  },
]
```

Each element loads only when a workspace shows its property, from its own chunk.

The uSync files start with a byte-order mark and end without a newline; `sed` keeps both:

```bash
sed -i '' 's|<EditorUIAlias>Umb.PropertyEditorUi.TextArea</EditorUIAlias>|<EditorUIAlias>KCC.PropertyEditorUi.Ingredients</EditorUIAlias>|' \
  src/KCC.Web/uSync/v17/DataTypes/KCCIngredients.config
sed -i '' 's|<EditorUIAlias>Umb.PropertyEditorUi.TextArea</EditorUIAlias>|<EditorUIAlias>KCC.PropertyEditorUi.Instructions</EditorUIAlias>|' \
  src/KCC.Web/uSync/v17/DataTypes/KCCInstructions.config
git diff --stat src/KCC.Web/uSync
```

Expected: 2 files changed, 2 insertions, 2 deletions. uSync imports the schema at every startup, so the change reaches
every database, the dev one included.

- [ ] **Step 6: Build and run the tests**

```bash
yarn build:all
ls src/KCC.Admin/wwwroot/App_Plugins/KCC.Admin
yarn workspace @kcc/admin test
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/RecipeEditorDataTypeTests/*"
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/BackofficePackageTests/*"
```

Expected: the admin client type-checks and builds; its folder holds the bundle plus `api-`,
`json-array-editor.element-`, `ingredients-editor.element-` and `instructions-editor.element-` chunks; 14 Vitest tests
pass; 2 passed, then 2 passed.

- [ ] **Step 7: Look at it**

Start the throwaway backoffice (see the Global Constraints) and open Content → Home → Recipes → any seeded recipe →
one of its variants. In the **Method** group, in the light theme and then the dark one:
- The ingredients show as rows of name, quantity, unit and **Eyeballed**; the unit field offers the 15 units as you
  type.
- A row drags by its handle, and **Add ingredient** and the bin add and remove rows.
- Ticking **Eyeballed** empties and disables the quantity and unit.
- Clearing a name outlines its row in red, and **Save** refuses with "Every ingredient needs a name." under the field.
- The instructions show numbered steps. **Add step** adds one, and dragging a step renumbers the list.

Leave the throwaway data unsaved, and stop the site. The "unreadable value" view has no browser check, because nothing
in the site writes invalid JSON: it shows whenever `parseItems` flags an error, which the helper tests cover.

- [ ] **Step 8: Commit**

```bash
git add -A src/KCC.Admin/Client src/KCC.Web/uSync/v17/DataTypes tests/KCC.IntegrationTests
git commit -m "Edit Ingredients and Instructions in Custom Property Editors"
```

---

### Task 4: The recipe icon editor

The icon property gets its editor (spec §10). It shows the chosen icon and its name, and offers two ways to change it:
**Select icon** opens a sidebar with the searchable grid of the 153 curated icons, and **Suggest with AI** sends the
name and description, as they stand in the workspace, to Task 1's suggestion endpoint. The value stays a class string
such as `fa-duotone fa-cheese` in a text-box data type. The folder is `recipe-icon/`, never `icon/` (see the Global
Constraints).

**Files:**
- Create: `src/KCC.Admin/Client/src/recipe-icon/{icons,icons.test,font-awesome,icon-picker-modal.token}.ts`,
  `src/KCC.Admin/Client/src/recipe-icon/{icon-picker-modal,recipe-icon-editor}.element.ts`
- Modify: `src/KCC.Admin/Client/src/api.ts`, `src/KCC.Admin/Client/src/bundle.ts`,
  `src/KCC.Web/uSync/v17/DataTypes/KCCRecipeIcon.config`, `src/KCC.Admin/RecipeIcons.cs` (a stale comment),
  `tests/KCC.IntegrationTests/Features/Backoffice/RecipeEditorDataTypeTests.cs`

**Interfaces:**
- Consumes: Task 1's `…/recipe-editor/icons` and `…/recipe-editor/icon-suggestion`; Task 3's `api.ts` constants.
- Produces:
  - `api.ts`: `getRecipeIcons(host) → Promise<string[]>` and
    `suggestRecipeIcon(host, name, description) → Promise<string | undefined>`.
  - `recipe-icon/icons.ts`: `iconLabel(icon)` (`"fa-duotone fa-cheese-swiss"` → `"cheese-swiss"`) and
    `filterIcons(icons, search)`.
  - `recipe-icon/font-awesome.ts`: `fontAwesomeStyles` and `registerFontAwesome()`.
  - `KCC_ICON_PICKER_MODAL`, an `UmbModalToken<object, KccIconPickerModalValue { icon }>` for a medium sidebar,
    implemented by `kcc-icon-picker-modal` (modal `KCC.Modal.RecipeIconPicker`).
  - `kcc-recipe-icon-editor`, registered as the property editor UI `KCC.PropertyEditorUi.RecipeIcon` (schema
    `Umbraco.TextBox`, group "Recipes").
  - Markup Task 8's suite relies on: the editor's `.preview i` carries the icon's classes; its buttons are labelled
    "Select icon" and "Suggest with AI"; the picker's search is a `uui-input` labelled "Search icons", each icon is a
    `button[title='<classes>']`, and its footer buttons are "Cancel" and "Select icon".

- [ ] **Step 1: Write the failing tests**

In `tests/KCC.IntegrationTests/Features/Backoffice/RecipeEditorDataTypeTests.cs`, add a third row:

```diff
     [Arguments("KCC Ingredients", "Umbraco.TextArea", "KCC.PropertyEditorUi.Ingredients")]
     [Arguments("KCC Instructions", "Umbraco.TextArea", "KCC.PropertyEditorUi.Instructions")]
+    [Arguments("KCC Recipe Icon", "Umbraco.TextBox", "KCC.PropertyEditorUi.RecipeIcon")]
     public async Task RecipeDataType_UsesTheKccEditor(string name, string schema, string editorUi)
```

Create `src/KCC.Admin/Client/src/recipe-icon/icons.test.ts`:

```ts
import { describe, expect, it } from 'vitest'
import { filterIcons, iconLabel } from './icons.js'

const icons = ['fa-duotone fa-cheese', 'fa-duotone fa-cheese-swiss', 'fa-duotone fa-egg']

describe('iconLabel', () => {
  it('drops the style and prefix from a class string', () => {
    expect(iconLabel('fa-duotone fa-cheese-swiss')).toBe('cheese-swiss')
  })
})

describe('filterIcons', () => {
  it('keeps every icon for a blank search', () => {
    expect(filterIcons(icons, '  ')).toEqual(icons)
  })

  it('matches the label, ignoring case and the style prefix', () => {
    expect(filterIcons(icons, 'CHEESE')).toEqual(['fa-duotone fa-cheese', 'fa-duotone fa-cheese-swiss'])
    expect(filterIcons(icons, 'duotone')).toEqual([])
  })
})
```

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/RecipeEditorDataTypeTests/*"
yarn workspace @kcc/admin test
```

Expected: the new row fails on its UI (`Umb.PropertyEditorUi.TextBox`) while the other two pass, and the new Vitest
file fails to load `./icons.js`.

- [ ] **Step 2: The icon helpers**

Create `src/KCC.Admin/Client/src/recipe-icon/icons.ts`:

```ts
export function iconLabel(icon: string): string {
  return icon.replace(/^fa-[a-z]+\s+fa-/, '')
}

export function filterIcons(icons: string[], search: string): string[] {
  const term = search.trim().toLowerCase()
  return term === '' ? icons : icons.filter((icon) => iconLabel(icon).includes(term))
}
```

```bash
yarn workspace @kcc/admin test
```

Expected: 17 passed.

- [ ] **Step 3: The two calls**

Append to `src/KCC.Admin/Client/src/api.ts`:

```ts
export async function getRecipeIcons(host: UmbControllerHost): Promise<string[]> {
  const { data } = await tryExecute(host, umbHttpClient.get<{ 200: string[] }>({ url: `${base}/icons`, security }))
  return data ?? []
}

export async function suggestRecipeIcon(host: UmbControllerHost, name: string, description: string): Promise<string | undefined> {
  const { data } = await tryExecute(
    host,
    umbHttpClient.post<{ 200: { icon: string } }>({ url: `${base}/icon-suggestion`, body: { name, description }, security }),
  )
  return data?.icon
}
```

- [ ] **Step 4: Font Awesome inside the backoffice's shadow roots**

Create `src/KCC.Admin/Client/src/recipe-icon/font-awesome.ts`:

```ts
import { unsafeCSS } from '@umbraco-cms/backoffice/external/lit'
import fontAwesome from '@fortawesome/fontawesome-pro/css/fontawesome.min.css?raw'
import duotone from '@fortawesome/fontawesome-pro/css/duotone.min.css?raw'
import duotoneFont from '@fortawesome/fontawesome-pro/webfonts/fa-duotone-900.woff2?url'

// Browsers ignore a font face declared inside a shadow root, so the icon font is added to the document instead and
// only the class rules go into each element's styles.
const withoutFontFaces = (css: string) => css.replace(/@font-face\s*{[^}]*}/g, '')

export const fontAwesomeStyles = unsafeCSS(withoutFontFaces(fontAwesome) + withoutFontFaces(duotone))

let registered = false

export function registerFontAwesome() {
  if (registered) {
    return
  }

  registered = true
  document.fonts.add(
    new FontFace('Font Awesome 7 Duotone', `url(${duotoneFont}) format('woff2')`, { style: 'normal', weight: '900', display: 'block' }),
  )
}
```

All 153 curated icons are duotone ones (each starts `fa-duotone`), so the duotone style is the only one loaded. The
CSS comes in `?raw`: `?inline` would resolve its `url()`s and inline the font into the chunk.

- [ ] **Step 5: The icon picker**

Create `src/KCC.Admin/Client/src/recipe-icon/icon-picker-modal.token.ts`:

```ts
import { UmbModalToken } from '@umbraco-cms/backoffice/modal'

export interface KccIconPickerModalValue {
  icon: string
}

export const KCC_ICON_PICKER_MODAL = new UmbModalToken<object, KccIconPickerModalValue>('KCC.Modal.RecipeIconPicker', {
  modal: { type: 'sidebar', size: 'medium' },
})
```

Create `src/KCC.Admin/Client/src/recipe-icon/icon-picker-modal.element.ts`:

```ts
import { css, customElement, html, repeat, state } from '@umbraco-cms/backoffice/external/lit'
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal'
import { UmbTextStyles } from '@umbraco-cms/backoffice/style'
import { getRecipeIcons } from '../api.js'
import { fontAwesomeStyles, registerFontAwesome } from './font-awesome.js'
import type { KccIconPickerModalValue } from './icon-picker-modal.token.js'
import { filterIcons, iconLabel } from './icons.js'

@customElement('kcc-icon-picker-modal')
export class KccIconPickerModalElement extends UmbModalBaseElement<object, KccIconPickerModalValue> {
  @state()
  private icons: string[] = []

  @state()
  private search = ''

  override connectedCallback() {
    super.connectedCallback()
    registerFontAwesome()
    void getRecipeIcons(this).then((icons) => (this.icons = icons))
  }

  override render() {
    return html`
      <umb-body-layout headline="Select an icon">
        <uui-box>
          <uui-input
            type="search"
            label="Search icons"
            placeholder="Search icons"
            .value=${this.search}
            @input=${(event: Event) => (this.search = (event.target as HTMLInputElement).value)}></uui-input>
          <div class="grid">
            ${repeat(
              filterIcons(this.icons, this.search),
              (icon) => icon,
              (icon) => html`
                <button
                  type="button"
                  class=${icon === this.value?.icon ? 'icon selected' : 'icon'}
                  title=${icon}
                  aria-pressed=${icon === this.value?.icon ? 'true' : 'false'}
                  @click=${() => this.updateValue({ icon })}>
                  <i class=${icon}></i>
                  <span>${iconLabel(icon)}</span>
                </button>
              `,
            )}
          </div>
        </uui-box>
        <uui-button slot="actions" label="Cancel" @click=${() => this._rejectModal()}></uui-button>
        <uui-button
          slot="actions"
          look="primary"
          color="positive"
          label="Select icon"
          ?disabled=${!this.value?.icon}
          @click=${() => this._submitModal()}></uui-button>
      </umb-body-layout>
    `
  }

  static override readonly styles = [
    UmbTextStyles,
    fontAwesomeStyles,
    css`
      uui-input {
        width: 100%;
        margin-bottom: var(--uui-size-space-4);
      }

      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(6rem, 1fr));
        gap: var(--uui-size-space-3);
      }

      .icon {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: var(--uui-size-space-2);
        min-height: 6rem;
        padding: var(--uui-size-space-3);
        border: 1px solid var(--uui-color-border);
        border-radius: var(--uui-border-radius);
        background: var(--uui-color-surface);
        color: var(--uui-color-text);
        font: inherit;
        cursor: pointer;
      }

      .icon:hover {
        background: var(--uui-color-surface-emphasis);
      }

      .icon:focus-visible {
        outline: 2px solid var(--uui-color-focus);
      }

      .icon.selected {
        border-color: var(--uui-color-selected);
        box-shadow: inset 0 0 0 1px var(--uui-color-selected);
      }

      .icon i {
        font-size: 1.75rem;
      }

      .icon span {
        font-size: var(--uui-type-small-size);
        text-align: center;
        word-break: break-word;
      }
    `,
  ]
}

export default KccIconPickerModalElement

declare global {
  interface HTMLElementTagNameMap {
    'kcc-icon-picker-modal': KccIconPickerModalElement
  }
}
```

The picker opens on the current icon, already selected, and returns a value only through **Select icon**.

- [ ] **Step 6: The editor**

Create `src/KCC.Admin/Client/src/recipe-icon/recipe-icon-editor.element.ts`:

```ts
import { css, customElement, html, nothing, property, state } from '@umbraco-cms/backoffice/external/lit'
import { UmbChangeEvent } from '@umbraco-cms/backoffice/event'
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element'
import { umbOpenModal } from '@umbraco-cms/backoffice/modal'
import { UMB_PROPERTY_DATASET_CONTEXT } from '@umbraco-cms/backoffice/property'
import type { UmbPropertyEditorUiElement } from '@umbraco-cms/backoffice/property-editor'
import { UmbTextStyles } from '@umbraco-cms/backoffice/style'
import { UmbFormControlMixin } from '@umbraco-cms/backoffice/validation'
import { suggestRecipeIcon } from '../api.js'
import { fontAwesomeStyles, registerFontAwesome } from './font-awesome.js'
import { KCC_ICON_PICKER_MODAL } from './icon-picker-modal.token.js'
import { iconLabel } from './icons.js'

@customElement('kcc-recipe-icon-editor')
export class KccRecipeIconEditorElement
  extends UmbFormControlMixin<string | undefined, typeof UmbLitElement, undefined>(UmbLitElement)
  implements UmbPropertyEditorUiElement
{
  @property({ type: Boolean, reflect: true })
  readonly = false

  @property({ type: Boolean })
  mandatory = false

  @state()
  private suggesting = false

  #dataset?: typeof UMB_PROPERTY_DATASET_CONTEXT.TYPE

  constructor() {
    super()
    this.consumeContext(UMB_PROPERTY_DATASET_CONTEXT, (context) => (this.#dataset = context))
    this.addValidator('valueMissing', () => 'Choose an icon.', () => this.mandatory && !this.value)
  }

  override connectedCallback() {
    super.connectedCallback()
    registerFontAwesome()
  }

  async #choose() {
    const chosen = await umbOpenModal(this, KCC_ICON_PICKER_MODAL, { value: { icon: this.value ?? '' } }).catch(() => undefined)
    if (chosen?.icon) {
      this.#set(chosen.icon)
    }
  }

  // Sends the name and description as they are in the editor, saved or not.
  async #suggest() {
    this.suggesting = true
    try {
      const properties = (await this.#dataset?.getProperties()) ?? []
      const description = properties.find((property) => property.alias === 'description')?.value
      const icon = await suggestRecipeIcon(this, this.#dataset?.getName() ?? '', typeof description === 'string' ? description : '')
      if (icon) {
        this.#set(icon)
      }
    } finally {
      this.suggesting = false
    }
  }

  #set(icon: string) {
    this.value = icon
    this.pristine = false
    this.dispatchEvent(new UmbChangeEvent())
  }

  override render() {
    return html`
      <div class="field">
        ${this.value
          ? html`<span class="preview"><i class=${this.value}></i></span><span class="label">${iconLabel(this.value)}</span>`
          : html`<span class="empty">No icon chosen</span>`}
        ${this.readonly
          ? nothing
          : html`
              <uui-button look="secondary" label="Select icon" @click=${this.#choose}></uui-button>
              <uui-button
                look="secondary"
                label="Suggest with AI"
                .state=${this.suggesting ? 'waiting' : undefined}
                ?disabled=${this.suggesting}
                @click=${this.#suggest}></uui-button>
            `}
      </div>
    `
  }

  static override readonly styles = [
    UmbTextStyles,
    fontAwesomeStyles,
    css`
      .field {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: var(--uui-size-space-3);
      }

      .preview {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: var(--uui-size-12);
        height: var(--uui-size-12);
        font-size: 1.75rem;
        border: 1px solid var(--uui-color-border);
        border-radius: var(--uui-border-radius);
      }

      .empty {
        color: var(--uui-color-text-alt);
      }
    `,
  ]
}

export default KccRecipeIconEditorElement

declare global {
  interface HTMLElementTagNameMap {
    'kcc-recipe-icon-editor': KccRecipeIconEditorElement
  }
}
```

The picker rejects its promise when it is cancelled; the `catch` turns that into "nothing chosen". Recipes and
variants both call their text `description`, so the one editor serves both types.

- [ ] **Step 7: Register it, point the data type at it, and correct the icon list's comment**

In `src/KCC.Admin/Client/src/bundle.ts`, add these two entries to the end of the `manifests` array:

```ts
  {
    type: 'propertyEditorUi',
    alias: 'KCC.PropertyEditorUi.RecipeIcon',
    name: 'KCC Recipe Icon Property Editor UI',
    element: () => import('./recipe-icon/recipe-icon-editor.element.js'),
    meta: {
      label: 'KCC Recipe Icon',
      icon: 'icon-picture',
      group: 'Recipes',
      propertyEditorSchemaAlias: 'Umbraco.TextBox',
      supportsReadOnly: true,
    },
  },
  {
    type: 'modal',
    alias: 'KCC.Modal.RecipeIconPicker',
    name: 'KCC Recipe Icon Picker Modal',
    element: () => import('./recipe-icon/icon-picker-modal.element.js'),
  },
```

```bash
sed -i '' 's|<EditorUIAlias>Umb.PropertyEditorUi.TextBox</EditorUIAlias>|<EditorUIAlias>KCC.PropertyEditorUi.RecipeIcon</EditorUIAlias>|' \
  src/KCC.Web/uSync/v17/DataTypes/KCCRecipeIcon.config
git diff --stat src/KCC.Web/uSync
```

Expected: 1 file changed, 1 insertion, 1 deletion. The data type keeps its `maxChars` of 200.

`RecipeIcons`'s summary still names the Kentico form component this task replaces. Apply to
`src/KCC.Admin/RecipeIcons.cs`:

```diff
 /// The curated set of Font Awesome Pro duotone icons available for recipes, as full
 /// Font Awesome class strings (e.g. "fa-duotone fa-cheese"). This is the single
-/// source of truth shared by the AI enum, the deterministic fallback, and the admin
-/// icon-selector form component; the value is stored verbatim and rendered as-is.
+/// source of truth shared by the AI enum, the deterministic fallback, and the backoffice's
+/// recipe icon editor; the value is stored verbatim and rendered as-is.
 /// </summary>
```

- [ ] **Step 8: Build and run the tests**

```bash
yarn build:all
ls src/KCC.Admin/wwwroot/App_Plugins/KCC.Admin
git status --short --ignored src/KCC.Admin/Client/src
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/RecipeEditorDataTypeTests/*"
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj -- --treenode-filter "/*/*/RecipeIconsTests/*"
```

Expected:
- The admin folder now also holds `recipe-icon-editor.element-`, `icon-picker-modal.element-` and `icons-` chunks, and
  one `fa-duotone-900-<hash>.woff2`. No chunk is anywhere near the font's size; if one is, the CSS was imported
  `?inline`.
- `git status` lists the new `recipe-icon/` files as untracked (`??`), never ignored (`!!`).
- 3 passed; the icon list's unit tests pass.

- [ ] **Step 9: Look at it**

In the throwaway backoffice (see the Global Constraints), open a seeded recipe, then one of its variants. In both
themes:
- The **Icon** property shows the duotone icon and its name.
- **Select icon** opens the sidebar grid with the current icon selected. Typing `cheese` leaves three icons; picking
  one and **Select icon** changes the preview, while **Cancel** leaves it alone.
- **Suggest with AI** shows its busy state, then the fallback icon for the name: the throwaway site has no Anthropic
  key.

Leave it unsaved, and stop the site.

- [ ] **Step 10: Commit**

```bash
git add -A src/KCC.Admin src/KCC.Web/uSync/v17/DataTypes tests/KCC.IntegrationTests
git commit -m "Pick Recipe Icons from the Curated Grid or by AI Suggestion"
```

---

### Task 5: The Waiting list and member approval

The dashboard's default tab lists what is waiting for the owner (spec §10): members who have signed up but are not
approved yet, and recipes and variants that are saved but not published. **Approve** lets a member sign in. The
dashboard's backend lives in `KCC.Contributions` with the store it moderates, behind one controller for
administrators only. Member names and member saves belong to the site (the author-name cache and Phase 4's member
write lock are in `KCC.Web`), so the library declares `IDashboardMembers` and `KCC.Web` implements it.

"Never published", as spec §10 puts it, cannot be read back from Umbraco (see "What the scratch probe already
proved"), so Waiting lists the recipes and variants that are not published now. That is the account page's own test
for "Pending review". A recipe the owner unpublishes on purpose shows up here too, which is the right place to find
it again.

**Files:**
- Modify: `src/KCC.Contributions/KCC.Contributions.csproj`, `src/KCC.Contributions/ContributionsComposer.cs`,
  `src/KCC.Web/Features/Providers/ProvidersComposer.cs`
- Create: `src/KCC.Contributions/Dashboard/{IDashboardMembers,DashboardModels,DashboardQueries,ContributionsDashboardController}.cs`,
  `src/KCC.Web/Features/Providers/DashboardMembers.cs`,
  `tests/KCC.IntegrationTests/Features/Backoffice/ContributionsDashboardApiTests.cs`

**Interfaces:**
- Consumes: `IAuthorNameProvider.ResolveMany`, `IMemberWriteLock.RunAsync<T>`, `IMemberService`, `IContentService`,
  `IEntityService`, `IDocumentNavigationQueryService` and `IPublishStatusQueryService`; `TestMembers.SignUpAsync` and
  `UniqueUserName`, `TestContent.AuthorAsync`, `RecipeAsync`, `DraftRecipeAsync` and `DraftVariantAsync`; Task 1's
  `BackofficeClient`.
- Produces:
  - `KCC.Contributions.Dashboard.IDashboardMembers`: `NamesAsync(IEnumerable<Guid>) →
    Task<IReadOnlyDictionary<Guid, string>>` and `ApproveAsync(Guid memberKey) → Task<bool>`, false when there is no
    such member. `KCC.Web.Features.Providers.DashboardMembers` implements it, registered scoped.
  - `IDashboardQueries.WaitingAsync() → Task<WaitingModel>`, registered scoped. Task 6 adds two queries.
  - `WaitingModel(IReadOnlyList<WaitingMember> Members, IReadOnlyList<WaitingDraft> Drafts)`,
    `WaitingMember(Guid Key, string Name, string UserName, string Email, DateTime Registered)` and
    `WaitingDraft(Guid Key, string Kind, string Name, string RecipeName, string AuthorName, DateTime Created)`, where
    `Kind` is `DashboardQueries.RecipeKind` (`"recipe"`) or `VariantKind` (`"variant"`), `RecipeName` is set for
    variants, and both lists come newest first. Dates are UTC.
  - For administrators: `GET /umbraco/management/api/v1/kcc/contributions/waiting` → the model, and
    `POST …/members/{memberKey:guid}/approval` → 204, or 404 when there is no such member.

- [ ] **Step 1: Write the failing tests**

Create `tests/KCC.IntegrationTests/Features/Backoffice/ContributionsDashboardApiTests.cs`:

```csharp
using System.Net;
using System.Text.Json;
using KCC.IntegrationTests.Config;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Backoffice;

// Each test finds its own members, recipes and entries by key: other suites leave drafts, sign-ups and reviews
// behind, so nothing here counts the whole list.
public class ContributionsDashboardApiTests
{
    private const string Base = "/umbraco/management/api/v1/kcc/contributions";

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Dashboard_IsForAdministratorsOnly()
    {
        using var editor = await BackofficeClient.EditorAsync(Site);
        using var anonymous = Site.CreateClient();

        using var asEditor = await editor.GetAsync($"{Base}/waiting");
        using var asAnonymous = await anonymous.GetAsync($"{Base}/waiting");

        _ = await Assert.That(asEditor.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        _ = await Assert.That(asAnonymous.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Waiting_ListsAMemberUntilTheyAreApproved()
    {
        var userName = TestMembers.UniqueUserName("schumann");
        var key = await TestMembers.SignUpAsync(Site.Services, userName);
        using var admin = await BackofficeClient.AdministratorAsync(Site);

        var listed = WaitingMember(await admin.GetJsonAsync($"{Base}/waiting"), key);
        using var approval = await admin.PostAsync($"{Base}/members/{key}/approval");
        var afterwards = WaitingMember(await admin.GetJsonAsync($"{Base}/waiting"), key);

        _ = await Assert.That(listed).IsNotNull();
        _ = await Assert.That(listed!.Value.GetProperty("userName").GetString()).IsEqualTo(userName);
        _ = await Assert.That(listed.Value.GetProperty("email").GetString()).IsEqualTo($"{userName}@example.test");
        _ = await Assert.That(listed.Value.GetProperty("registered").GetDateTime()).IsGreaterThan(DateTime.UtcNow.AddMinutes(-5));
        _ = await Assert.That(approval.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        _ = await Assert.That(afterwards).IsNull();
        _ = await Assert.That(Site.Services.GetRequiredService<IMemberService>().GetById(key)!.IsApproved).IsTrue();
    }

    [Test]
    public async Task Approval_OfAnUnknownMember_IsNotFound()
    {
        using var admin = await BackofficeClient.AdministratorAsync(Site);

        using var approval = await admin.PostAsync($"{Base}/members/{Guid.NewGuid()}/approval");

        _ = await Assert.That(approval.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Waiting_ListsUnpublishedRecipesAndVariants_NewestFirst()
    {
        var author = await TestContent.AuthorAsync(Site.Services, TestMembers.UniqueUserName("boulanger"), "Lili", "Boulanger");
        var draftRecipe = await TestContent.DraftRecipeAsync(Site.Services, "IT Garnet Stew", author);
        var draftVariant = await TestContent.DraftVariantAsync(Site.Services, draftRecipe, "Slow-cooked", author);
        var publishedRecipe = await TestContent.RecipeAsync(Site.Services, "IT Obsidian Pie");
        var newVariant = await TestContent.DraftVariantAsync(Site.Services, publishedRecipe, "Smoky", author);
        using var admin = await BackofficeClient.AdministratorAsync(Site);

        var drafts = (await admin.GetJsonAsync($"{Base}/waiting")).GetProperty("drafts").EnumerateArray().ToList();
        var keys = drafts.Select(draft => draft.GetProperty("key").GetGuid()).ToList();
        var recipe = drafts.Single(draft => draft.GetProperty("key").GetGuid() == draftRecipe);
        var variant = drafts.Single(draft => draft.GetProperty("key").GetGuid() == newVariant);

        _ = await Assert.That(keys).DoesNotContain(publishedRecipe);
        _ = await Assert.That(keys.IndexOf(newVariant)).IsLessThan(keys.IndexOf(draftVariant));
        _ = await Assert.That(keys.IndexOf(draftVariant)).IsLessThan(keys.IndexOf(draftRecipe));
        _ = await Assert.That(recipe.GetProperty("kind").GetString()).IsEqualTo("recipe");
        _ = await Assert.That(recipe.GetProperty("name").GetString()).IsEqualTo("IT Garnet Stew");
        _ = await Assert.That(recipe.GetProperty("authorName").GetString()).IsEqualTo("Lili Boulanger");
        _ = await Assert.That(variant.GetProperty("kind").GetString()).IsEqualTo("variant");
        _ = await Assert.That(variant.GetProperty("recipeName").GetString()).IsEqualTo("IT Obsidian Pie");
    }

    private static JsonElement? WaitingMember(JsonElement waiting, Guid key)
    {
        foreach (var member in waiting.GetProperty("members").EnumerateArray())
        {
            if (member.GetProperty("key").GetGuid() == key)
            {
                return member;
            }
        }

        return null;
    }
}
```

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/ContributionsDashboardApiTests/*"
```

Expected: all 4 fail, because every route answers 404.

- [ ] **Step 2: The member port and the models**

In `src/KCC.Contributions/KCC.Contributions.csproj`, add the Management API package beside the EF Core one:

```diff
     <ItemGroup>
+        <PackageReference Include="Umbraco.Cms.Api.Management" />
         <PackageReference Include="Umbraco.Cms.Persistence.EFCore" />
     </ItemGroup>
```

Create `src/KCC.Contributions/Dashboard/IDashboardMembers.cs`:

```csharp
namespace KCC.Contributions.Dashboard;

// Display names and the member write lock belong to the site, which implements this for the dashboard.
public interface IDashboardMembers
{
    Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IEnumerable<Guid> memberKeys);

    Task<bool> ApproveAsync(Guid memberKey);
}
```

Create `src/KCC.Contributions/Dashboard/DashboardModels.cs`:

```csharp
namespace KCC.Contributions.Dashboard;

public sealed record WaitingModel(IReadOnlyList<WaitingMember> Members, IReadOnlyList<WaitingDraft> Drafts);

public sealed record WaitingMember(Guid Key, string Name, string UserName, string Email, DateTime Registered);

public sealed record WaitingDraft(Guid Key, string Kind, string Name, string RecipeName, string AuthorName, DateTime Created);
```

Create `src/KCC.Web/Features/Providers/DashboardMembers.cs`:

```csharp
using KCC.Contributions.Dashboard;
using KCC.Web.Features.Sqlite;
using Umbraco.Cms.Core.Services;

namespace KCC.Web.Features.Providers;

public class DashboardMembers(IAuthorNameProvider authorNames, IMemberService memberService, IMemberWriteLock memberWriteLock) : IDashboardMembers
{
    public Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IEnumerable<Guid> memberKeys) => authorNames.ResolveMany(memberKeys);

    // Inside the member write lock, as every member save is: a sign-in attempt saves the same member, and must not
    // overwrite the approval with the copy it read before.
    public Task<bool> ApproveAsync(Guid memberKey) => memberWriteLock.RunAsync(() =>
    {
        var member = memberService.GetById(memberKey);
        if (member is null)
        {
            return Task.FromResult(false);
        }

        if (!member.IsApproved)
        {
            member.IsApproved = true;
            memberService.Save(member);
        }

        return Task.FromResult(true);
    });
}
```

Approving flips one flag. Phase 4 suggested the member editing service, but its update takes the member's whole
editable state (email, username, name and properties), which Approve has no reason to send back; the save through
`IMemberService` inside the lock is the path Phase 4's own profile save takes. The save publishes
`MemberSavedNotification`, so the author-name cache and the search index hear about it as they do for any member
save.

In `src/KCC.Web/Features/Providers/ProvidersComposer.cs`, add `using KCC.Contributions.Dashboard;` (it sorts between
`KCC.Admin` and `KCC.Web.Features.Models.Options`), and register the port right after the author-name provider:

```diff
         builder.Services.AddSingleton<IAuthorNameProvider, AuthorNameProvider>();
+        builder.Services.AddScoped<IDashboardMembers, DashboardMembers>();
```

- [ ] **Step 3: The Waiting query**

Create `src/KCC.Contributions/Dashboard/DashboardQueries.cs`:

```csharp
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;

namespace KCC.Contributions.Dashboard;

public interface IDashboardQueries
{
    Task<WaitingModel> WaitingAsync();
}

// Recipes and variants waiting for the owner are saved but unpublished, so they are read from the content service;
// everything else comes from the navigation structure and the publish-status cache.
public sealed class DashboardQueries(
    IDashboardMembers members,
    IMemberService memberService,
    IContentService contentService,
    IEntityService entityService,
    IDocumentNavigationQueryService navigation,
    IPublishStatusQueryService publishStatus) : IDashboardQueries
{
    public const string RecipeKind = "recipe";

    public const string VariantKind = "variant";

    private const int MemberBatch = 100;

    public async Task<WaitingModel> WaitingAsync() => new(await UnapprovedMembersAsync(), await DraftsAsync());

    // Umbraco stores its dates in UTC, but SQLite hands them back unmarked.
    private static DateTime Utc(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();

    // A member picker saves its value as a member UDI, such as umb://member/0a1b…
    private static Guid? AuthorKey(IContent content) =>
        content.GetValue("author") is string text && UdiParser.TryParse(text, out Udi udi) && udi is GuidUdi member ? member.Guid : null;

    private async Task<IReadOnlyList<WaitingMember>> UnapprovedMembersAsync()
    {
        // Umbraco's member filter cannot order by date, so the whole list is read before it is sorted.
        var unapproved = new List<IMember>();
        var filter = new MemberFilter { IsApproved = false };
        while (true)
        {
            var batch = await memberService.FilterAsync(filter, skip: unapproved.Count, take: MemberBatch);
            unapproved.AddRange(batch.Items);
            if (!batch.Items.Any() || unapproved.Count >= batch.Total)
            {
                break;
            }
        }

        return unapproved
            .OrderByDescending(member => member.CreateDate)
            .Select(member => new WaitingMember(member.Key, member.Name, member.Username, member.Email, Utc(member.CreateDate)))
            .ToList();
    }

    private async Task<IReadOnlyList<WaitingDraft>> DraftsAsync()
    {
        var recipeKeys = RecipeKeys();
        var recipeOfVariant = new Dictionary<Guid, Guid>();
        foreach (var recipeKey in recipeKeys)
        {
            if (navigation.TryGetChildrenKeysOfType(recipeKey, "recipeVariant", out var variantKeys))
            {
                foreach (var variantKey in variantKeys)
                {
                    recipeOfVariant[variantKey] = recipeKey;
                }
            }
        }

        var unpublished = recipeKeys.Concat(recipeOfVariant.Keys)
            .Where(key => !publishStatus.IsDocumentPublishedInAnyCulture(key))
            .ToList();
        if (unpublished.Count == 0)
        {
            return [];
        }

        var drafts = contentService.GetByIds(unpublished).ToList();
        var recipeNames = Names(recipeOfVariant.Where(pair => unpublished.Contains(pair.Key)).Select(pair => pair.Value));
        var authors = await members.NamesAsync(drafts.Select(AuthorKey).OfType<Guid>());

        return drafts
            .OrderByDescending(draft => draft.CreateDate)
            .Select(draft =>
            {
                var isVariant = recipeOfVariant.TryGetValue(draft.Key, out var recipeKey);
                return new WaitingDraft(
                    draft.Key,
                    isVariant ? VariantKind : RecipeKind,
                    draft.Name,
                    isVariant ? recipeNames.GetValueOrDefault(recipeKey) : null,
                    AuthorKey(draft) is { } author ? authors.GetValueOrDefault(author) : null,
                    Utc(draft.CreateDate));
            })
            .ToList();
    }

    private List<Guid> RecipeKeys() =>
        navigation.TryGetRootKeys(out var roots)
            ? roots.SelectMany(root => navigation.TryGetDescendantsKeysOfType(root, "recipe", out var keys) ? keys : []).ToList()
            : [];

    private Dictionary<Guid, string> Names(IEnumerable<Guid> keys)
    {
        var distinct = keys.Distinct().ToArray();
        return distinct.Length == 0
            ? []
            : entityService.GetAll(UmbracoObjectTypes.Document, distinct).ToDictionary(entity => entity.Key, entity => entity.Name);
    }
}
```

The navigation service knows drafts and skips the recycle bin, so a trashed draft drops off the list. A variant under
a draft recipe is listed too, though Umbraco publishes it only once its recipe is published.

In `src/KCC.Contributions/ContributionsComposer.cs`, add `using KCC.Contributions.Dashboard;` above
`using KCC.Contributions.Data;`, and register the query right after the `IContributionWrites` registration:

```csharp
        builder.Services.AddScoped<IDashboardQueries, DashboardQueries>();
```

- [ ] **Step 4: The controller**

Create `src/KCC.Contributions/Dashboard/ContributionsDashboardController.cs`:

```csharp
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Web.Common.Authorization;

namespace KCC.Contributions.Dashboard;

[ApiVersion("1.0")]
[VersionedApiBackOfficeRoute("kcc/contributions")]
[ApiExplorerSettings(GroupName = "KCC")]
[Authorize(Policy = AuthorizationPolicies.RequireAdminAccess)]
public class ContributionsDashboardController(IDashboardQueries queries, IDashboardMembers members) : ManagementApiControllerBase
{
    [HttpGet("waiting")]
    [ProducesResponseType<WaitingModel>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Waiting() => Ok(await queries.WaitingAsync());

    [HttpPost("members/{memberKey:guid}/approval")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(Guid memberKey) =>
        await members.ApproveAsync(memberKey) ? NoContent() : NotFound();
}
```

- [ ] **Step 5: Run the tests**

`KCC.Contributions` gains its first controller, so the application-parts file is stale again (Task 1, Step 5):

```bash
find src/KCC.Web/obj -name 'KCC.Web.MvcApplicationPartsAssemblyInfo.*' -delete
dotnet build KitchenCommandCenter.sln
grep -c 'ApplicationPartAttribute("KCC.Contributions")' src/KCC.Web/obj/Debug/net10.0/KCC.Web.MvcApplicationPartsAssemblyInfo.cs
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/ContributionsDashboardApiTests/*"
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: 0 warnings, StyleCop included; the count is 1; 4 passed, then the whole suite. The drafts these tests save
stay out of search, and the one published recipe is uncategorised with a mineral name, so no search assertion moves.

- [ ] **Step 6: Commit**

```bash
git add -A src/KCC.Contributions src/KCC.Web/Features/Providers tests/KCC.IntegrationTests
git commit -m "List Waiting Members and Drafts, and Approve Members"
```

---

### Task 6: Moderating reviews and cook notes

The Reviews and Cook notes tabs (spec §10) list every entry newest first, a page at a time, with the recipe and variant
it is about, its member, its rating (for a review), its text and its date. **Edit** changes a review's rating, in half
steps, and its text, or a note's text, which must not be blank; **Delete** removes the entry. The writes go through
`ContributionWrites`, so they take the contributions lock before their first read, and a review edit or delete
invalidates the ratings and publishes `ReviewsChangedNotification` (spec §9.1, Phase 3's rule). Search therefore sees
the new rating about two seconds later.

**Files:**
- Modify: `src/KCC.Contributions/ContributionWrites.cs`, `src/KCC.Contributions/ContributionReads.cs`,
  `src/KCC.Contributions/Dashboard/DashboardModels.cs`,
  `tests/KCC.IntegrationTests/Features/Backoffice/ContributionsDashboardApiTests.cs`
- Rewrite: `src/KCC.Contributions/Dashboard/DashboardQueries.cs`,
  `src/KCC.Contributions/Dashboard/ContributionsDashboardController.cs`

**Interfaces:**
- Consumes: Phase 4's `ContributionWrites.WriteAsync<T>` and `ReviewsChangedAsync`; `RatingMath.IsValidRating` and
  `RatingMath.ClampText` (null for blank text; trims and cuts to 4,000); `ContributionReads.PageAsync<T>` and
  `MaxPageSize`; Task 5's `IDashboardMembers`, `DashboardQueries` and controller; `IRecipeIndexRebuilder` and
  `IRecipeSearchService` for the test.
- Produces:
  - `IContributionWrites.EditReviewAsync(int reviewId, decimal rating, string text) → Task<bool>`, which throws
    `ArgumentOutOfRangeException` off the half-star scale; `DeleteReviewByIdAsync(int reviewId) → Task<bool>`;
    `EditNoteAsync(int noteId, string text) → Task<bool>`, which throws `ArgumentException` for blank text; and
    `DeleteNoteByIdAsync(int noteId) → Task<bool>`. Each is false when there is no such entry.
  - `IContributionReads.LatestReviewsAsync(int page, int pageSize) → Task<Paged<Review>>` and
    `LatestNotesAsync(int page, int pageSize) → Task<Paged<CookNote>>`, newest first, then by id.
  - `IDashboardQueries.ReviewsAsync(int page, int pageSize)` and `NotesAsync(int page, int pageSize)` →
    `Task<EntryPage>`.
  - `EntryPage(int Total, int Page, int PageSize, IReadOnlyList<Entry> Items)`,
    `Entry(int Id, Guid VariantKey, string VariantName, string RecipeName, string MemberName, decimal? Rating, string Text, DateTime Created)`,
    `ReviewEdit(decimal Rating, string Text)` and `NoteEdit(string Text)`. `VariantName` is null once the variant is
    deleted, `RecipeName` while it is in the recycle bin, and `MemberName` for a key with no member.
  - For administrators, under `/umbraco/management/api/v1/kcc/contributions`: `GET reviews?page=0&pageSize=20` →
    `EntryPage` (the page is 0-based; the size is held to 1–50); `PUT reviews/{id:int}` with `{ rating, text }` → 204,
    400 off the half-star scale, or 404; `DELETE reviews/{id:int}` → 204 or 404; and the same three for `notes`, with
    `{ text }` and 400 for blank text.

- [ ] **Step 1: Write the failing tests**

Apply to `tests/KCC.IntegrationTests/Features/Backoffice/ContributionsDashboardApiTests.cs`. Add these usings, keeping
them sorted:

```csharp
using KCC.Contributions;
using KCC.Web.Features.Search;
```

Add these tests after `Waiting_ListsUnpublishedRecipesAndVariants_NewestFirst`:

```csharp
    [Test]
    public async Task Reviews_ComeNewestFirst_WithTheirVariantRecipeAndMember()
    {
        var variant = await ReviewedVariantAsync("IT Jasper Loaf", 4.5m, "Dense crumb.");
        using var admin = await BackofficeClient.AdministratorAsync(Site);

        var page = await admin.GetJsonAsync($"{Base}/reviews?page=0&pageSize=5");
        var newest = page.GetProperty("items")[0];

        _ = await Assert.That(page.GetProperty("pageSize").GetInt32()).IsEqualTo(5);
        _ = await Assert.That(newest.GetProperty("variantKey").GetGuid()).IsEqualTo(variant);
        _ = await Assert.That(newest.GetProperty("variantName").GetString()).IsEqualTo("Classic");
        _ = await Assert.That(newest.GetProperty("recipeName").GetString()).IsEqualTo("IT Jasper Loaf");
        _ = await Assert.That(newest.GetProperty("memberName").GetString()).IsEqualTo("Clara Schumann");
        _ = await Assert.That(newest.GetProperty("rating").GetDecimal()).IsEqualTo(4.5m);
        _ = await Assert.That(newest.GetProperty("text").GetString()).IsEqualTo("Dense crumb.");
    }

    [Test]
    public async Task Review_IsEditedThenDeleted()
    {
        var variant = await ReviewedVariantAsync("IT Malachite Tart", 2m, "Too sweet.");
        using var admin = await BackofficeClient.AdministratorAsync(Site);
        var id = await NewestIdAsync(admin, "reviews", variant);

        using var edited = await admin.PutAsync($"{Base}/reviews/{id}", new { rating = 3.5, text = "Sweet, but fair." });
        var afterEdit = (await admin.GetJsonAsync($"{Base}/reviews?page=0&pageSize=5")).GetProperty("items")[0];
        using var deleted = await admin.DeleteAsync($"{Base}/reviews/{id}");
        using var again = await admin.DeleteAsync($"{Base}/reviews/{id}");

        _ = await Assert.That(edited.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        _ = await Assert.That(afterEdit.GetProperty("rating").GetDecimal()).IsEqualTo(3.5m);
        _ = await Assert.That(afterEdit.GetProperty("text").GetString()).IsEqualTo("Sweet, but fair.");
        _ = await Assert.That(deleted.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        _ = await Assert.That(again.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    [Arguments(0)]
    [Arguments(5.5)]
    [Arguments(2.25)]
    public async Task ReviewEdit_OffTheHalfStarScale_IsRefused(decimal rating)
    {
        var variant = await ReviewedVariantAsync($"IT Topaz {rating}", 4m, "Fine.");
        using var admin = await BackofficeClient.AdministratorAsync(Site);
        var id = await NewestIdAsync(admin, "reviews", variant);

        using var edited = await admin.PutAsync($"{Base}/reviews/{id}", new { rating, text = "Changed." });

        _ = await Assert.That(edited.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Edits_ToAnUnknownEntry_AreNotFound()
    {
        using var admin = await BackofficeClient.AdministratorAsync(Site);

        using var review = await admin.PutAsync($"{Base}/reviews/{int.MaxValue}", new { rating = 4, text = "Gone." });
        using var note = await admin.PutAsync($"{Base}/notes/{int.MaxValue}", new { text = "Gone." });
        using var deletedNote = await admin.DeleteAsync($"{Base}/notes/{int.MaxValue}");

        _ = await Assert.That(review.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        _ = await Assert.That(note.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        _ = await Assert.That(deletedNote.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CookNote_IsListedEditedAndDeleted_AndNeedsText()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Basalt Bake");
        var variant = await TestContent.VariantAsync(Site.Services, recipe, "Classic");
        var member = await TestContent.AuthorAsync(Site.Services, TestMembers.UniqueUserName("schumann"), "Clara", "Schumann");
        await Site.Services.GetRequiredService<IContributionWrites>().AddNoteAsync(variant, member, "Needs more salt.");
        using var admin = await BackofficeClient.AdministratorAsync(Site);
        var id = await NewestIdAsync(admin, "notes", variant);

        var listed = (await admin.GetJsonAsync($"{Base}/notes?page=0&pageSize=5")).GetProperty("items")[0];
        using var blank = await admin.PutAsync($"{Base}/notes/{id}", new { text = "   " });
        using var edited = await admin.PutAsync($"{Base}/notes/{id}", new { text = "  Needs a pinch more salt.  " });
        var afterEdit = (await admin.GetJsonAsync($"{Base}/notes?page=0&pageSize=5")).GetProperty("items")[0];
        using var deleted = await admin.DeleteAsync($"{Base}/notes/{id}");
        var afterDelete = (await admin.GetJsonAsync($"{Base}/notes?page=0&pageSize=50")).GetProperty("items").EnumerateArray()
            .Select(note => note.GetProperty("id").GetInt32());

        _ = await Assert.That(listed.GetProperty("rating").ValueKind).IsEqualTo(JsonValueKind.Null);
        _ = await Assert.That(listed.GetProperty("recipeName").GetString()).IsEqualTo("IT Basalt Bake");
        _ = await Assert.That(blank.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        _ = await Assert.That(edited.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        _ = await Assert.That(afterEdit.GetProperty("text").GetString()).IsEqualTo("Needs a pinch more salt.");
        _ = await Assert.That(deleted.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        _ = await Assert.That(afterDelete).DoesNotContain(id);
    }

    [Test]
    public async Task ReviewModeration_ReachesTheSearchIndex()
    {
        var variant = await ReviewedVariantAsync("IT Quartz Cake", 4m, "Light.");
        using var admin = await BackofficeClient.AdministratorAsync(Site);
        var id = await NewestIdAsync(admin, "reviews", variant);
        var reviewed = await SearchWhenCurrentAsync("quartz");

        using var edited = await admin.PutAsync($"{Base}/reviews/{id}", new { rating = 2, text = "Heavy after all." });
        var afterEdit = await SearchWhenCurrentAsync("quartz");
        using var deleted = await admin.DeleteAsync($"{Base}/reviews/{id}");
        var afterDelete = await SearchWhenCurrentAsync("quartz");

        _ = await Assert.That(reviewed.AverageRating).IsEqualTo(4d);
        _ = await Assert.That(edited.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        _ = await Assert.That(afterEdit.AverageRating).IsEqualTo(2d);
        _ = await Assert.That(deleted.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        _ = await Assert.That(afterDelete.ReviewCount).IsEqualTo(0);
        _ = await Assert.That(afterDelete.AverageRating).IsNull();
    }
```

Add these helpers after `WaitingMember`:

```csharp
    private static async Task<int> NewestIdAsync(BackofficeClient admin, string kind, Guid variant)
    {
        var newest = (await admin.GetJsonAsync($"{Base}/{kind}?page=0&pageSize=1")).GetProperty("items")[0];
        if (newest.GetProperty("variantKey").GetGuid() != variant)
        {
            throw new InvalidOperationException($"The newest entry in {kind} is not this test's.");
        }

        return newest.GetProperty("id").GetInt32();
    }

    private async Task<Guid> ReviewedVariantAsync(string recipeName, decimal rating, string text)
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, recipeName);
        var variant = await TestContent.VariantAsync(Site.Services, recipe, "Classic");
        var member = await TestContent.AuthorAsync(Site.Services, TestMembers.UniqueUserName("schumann"), "Clara", "Schumann");
        await Site.Services.GetRequiredService<IContributionWrites>().UpsertReviewAsync(variant, member, rating, text);
        return variant;
    }

    private async Task<RecipeSearchHit> SearchWhenCurrentAsync(string word)
    {
        await Site.Services.GetRequiredService<IRecipeIndexRebuilder>().WhenCurrentAsync(CancellationToken.None);
        return Site.Services.GetRequiredService<IRecipeSearchService>().Search(new RecipeSearchCriteria { Query = word }).Results.Single();
    }
```

`NewestIdAsync` relies on the suite running one test at a time (`[assembly: NotInParallel]`): the newest entry is then
always the one the test just wrote. Every rating here is below five stars (see the Global Constraints).

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/ContributionsDashboardApiTests/*"
```

Expected: Task 5's 4 pass, and 7 of the 8 new ones fail because `reviews` and `notes` answer 404.
`Edits_ToAnUnknownEntry_AreNotFound` passes already, since a missing route is a 404 too; it pins the answer once the
routes exist.

- [ ] **Step 2: The store's moderation writes and reads**

Apply to `src/KCC.Contributions/ContributionWrites.cs`. Add to `IContributionWrites`, after `DeleteForMembersAsync`:

```csharp

    Task<bool> EditReviewAsync(int reviewId, decimal rating, string text);

    Task<bool> DeleteReviewByIdAsync(int reviewId);

    Task<bool> EditNoteAsync(int noteId, string text);

    Task<bool> DeleteNoteByIdAsync(int noteId);
```

Add to `ContributionWrites`, after `DeleteForMembersAsync` and before the private `WriteAsync`:

```csharp
    public async Task<bool> EditReviewAsync(int reviewId, decimal rating, string text)
    {
        if (!RatingMath.IsValidRating(rating))
        {
            throw new ArgumentOutOfRangeException(nameof(rating), rating, "A rating runs from 0.5 to 5 in half-star steps.");
        }

        var edited = await WriteAsync(async db =>
        {
            var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId);
            if (review is null)
            {
                return 0;
            }

            review.Rating = rating;
            review.Text = RatingMath.ClampText(text);
            review.Modified = DateTime.UtcNow;
            return await db.SaveChangesAsync();
        });
        if (edited > 0)
        {
            await ReviewsChangedAsync();
        }

        return edited > 0;
    }

    public async Task<bool> DeleteReviewByIdAsync(int reviewId)
    {
        var deleted = await WriteAsync(db => db.Reviews.Where(review => review.Id == reviewId).ExecuteDeleteAsync());
        if (deleted > 0)
        {
            await ReviewsChangedAsync();
        }

        return deleted > 0;
    }

    public async Task<bool> EditNoteAsync(int noteId, string text)
    {
        var clamped = RatingMath.ClampText(text) ?? throw new ArgumentException("A cook note needs text.", nameof(text));
        return await WriteAsync(async db =>
        {
            var note = await db.CookNotes.FirstOrDefaultAsync(n => n.Id == noteId);
            if (note is null)
            {
                return 0;
            }

            note.Text = clamped;
            note.Modified = DateTime.UtcNow;
            return await db.SaveChangesAsync();
        }) > 0;
    }

    public async Task<bool> DeleteNoteByIdAsync(int noteId) =>
        await WriteAsync(db => db.CookNotes.Where(note => note.Id == noteId).ExecuteDeleteAsync()) > 0;
```

A note changes no rating, so its writes leave the stats and the index alone, as Phase 4's own note writes do. A review
edit keeps the review's member, variant and created date: the owner moderates what was written, not who wrote it.

Apply to `src/KCC.Contributions/ContributionReads.cs`. Add to `IContributionReads`, after `HasCookedAsync`:

```csharp

    Task<Paged<Review>> LatestReviewsAsync(int page, int pageSize);

    Task<Paged<CookNote>> LatestNotesAsync(int page, int pageSize);
```

Add to `ContributionReads`, after `HasCookedAsync` and before the private `PageAsync`:

```csharp
    public Task<Paged<Review>> LatestReviewsAsync(int page, int pageSize) =>
        ReadAsync(db => PageAsync(
            db.Reviews.AsNoTracking().OrderByDescending(review => review.Created).ThenByDescending(review => review.Id),
            page,
            pageSize));

    public Task<Paged<CookNote>> LatestNotesAsync(int page, int pageSize) =>
        ReadAsync(db => PageAsync(
            db.CookNotes.AsNoTracking().OrderByDescending(note => note.Created).ThenByDescending(note => note.Id),
            page,
            pageSize));
```

- [ ] **Step 3: The pages and the edit shapes**

Append to `src/KCC.Contributions/Dashboard/DashboardModels.cs`:

```csharp

public sealed record EntryPage(int Total, int Page, int PageSize, IReadOnlyList<Entry> Items);

public sealed record Entry(int Id, Guid VariantKey, string VariantName, string RecipeName, string MemberName, decimal? Rating, string Text, DateTime Created);

public sealed record ReviewEdit(decimal Rating, string Text);

public sealed record NoteEdit(string Text);
```

Replace `src/KCC.Contributions/Dashboard/DashboardQueries.cs` with Task 5's file plus the two page queries. Only the
interface, the constructor's first parameter, the two public methods and the private `PageAsync` and `Row` are new:

```csharp
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;

namespace KCC.Contributions.Dashboard;

public interface IDashboardQueries
{
    Task<WaitingModel> WaitingAsync();

    Task<EntryPage> ReviewsAsync(int page, int pageSize);

    Task<EntryPage> NotesAsync(int page, int pageSize);
}

// Recipes and variants waiting for the owner are saved but unpublished, so they are read from the content service;
// everything else comes from the navigation structure and the publish-status cache.
public sealed class DashboardQueries(
    IContributionReads contributionReads,
    IDashboardMembers members,
    IMemberService memberService,
    IContentService contentService,
    IEntityService entityService,
    IDocumentNavigationQueryService navigation,
    IPublishStatusQueryService publishStatus) : IDashboardQueries
{
    public const string RecipeKind = "recipe";

    public const string VariantKind = "variant";

    private const int MemberBatch = 100;

    public async Task<WaitingModel> WaitingAsync() => new(await UnapprovedMembersAsync(), await DraftsAsync());

    public async Task<EntryPage> ReviewsAsync(int page, int pageSize) =>
        await PageAsync(
            await contributionReads.LatestReviewsAsync(page, pageSize),
            page,
            pageSize,
            review => new Row(review.Id, review.VariantKey, review.MemberKey, review.Rating, review.Text, review.Created));

    public async Task<EntryPage> NotesAsync(int page, int pageSize) =>
        await PageAsync(
            await contributionReads.LatestNotesAsync(page, pageSize),
            page,
            pageSize,
            note => new Row(note.Id, note.VariantKey, note.MemberKey, null, note.Text, note.Created));

    // Umbraco stores its dates in UTC, but SQLite hands them back unmarked.
    private static DateTime Utc(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();

    // A member picker saves its value as a member UDI, such as umb://member/0a1b…
    private static Guid? AuthorKey(IContent content) =>
        content.GetValue("author") is string text && UdiParser.TryParse(text, out Udi udi) && udi is GuidUdi member ? member.Guid : null;

    private async Task<IReadOnlyList<WaitingMember>> UnapprovedMembersAsync()
    {
        // Umbraco's member filter cannot order by date, so the whole list is read before it is sorted.
        var unapproved = new List<IMember>();
        var filter = new MemberFilter { IsApproved = false };
        while (true)
        {
            var batch = await memberService.FilterAsync(filter, skip: unapproved.Count, take: MemberBatch);
            unapproved.AddRange(batch.Items);
            if (!batch.Items.Any() || unapproved.Count >= batch.Total)
            {
                break;
            }
        }

        return unapproved
            .OrderByDescending(member => member.CreateDate)
            .Select(member => new WaitingMember(member.Key, member.Name, member.Username, member.Email, Utc(member.CreateDate)))
            .ToList();
    }

    private async Task<IReadOnlyList<WaitingDraft>> DraftsAsync()
    {
        var recipeKeys = RecipeKeys();
        var recipeOfVariant = new Dictionary<Guid, Guid>();
        foreach (var recipeKey in recipeKeys)
        {
            if (navigation.TryGetChildrenKeysOfType(recipeKey, "recipeVariant", out var variantKeys))
            {
                foreach (var variantKey in variantKeys)
                {
                    recipeOfVariant[variantKey] = recipeKey;
                }
            }
        }

        var unpublished = recipeKeys.Concat(recipeOfVariant.Keys)
            .Where(key => !publishStatus.IsDocumentPublishedInAnyCulture(key))
            .ToList();
        if (unpublished.Count == 0)
        {
            return [];
        }

        var drafts = contentService.GetByIds(unpublished).ToList();
        var recipeNames = Names(recipeOfVariant.Where(pair => unpublished.Contains(pair.Key)).Select(pair => pair.Value));
        var authors = await members.NamesAsync(drafts.Select(AuthorKey).OfType<Guid>());

        return drafts
            .OrderByDescending(draft => draft.CreateDate)
            .Select(draft =>
            {
                var isVariant = recipeOfVariant.TryGetValue(draft.Key, out var recipeKey);
                return new WaitingDraft(
                    draft.Key,
                    isVariant ? VariantKind : RecipeKind,
                    draft.Name,
                    isVariant ? recipeNames.GetValueOrDefault(recipeKey) : null,
                    AuthorKey(draft) is { } author ? authors.GetValueOrDefault(author) : null,
                    Utc(draft.CreateDate));
            })
            .ToList();
    }

    private List<Guid> RecipeKeys() =>
        navigation.TryGetRootKeys(out var roots)
            ? roots.SelectMany(root => navigation.TryGetDescendantsKeysOfType(root, "recipe", out var keys) ? keys : []).ToList()
            : [];

    private Dictionary<Guid, string> Names(IEnumerable<Guid> keys)
    {
        var distinct = keys.Distinct().ToArray();
        return distinct.Length == 0
            ? []
            : entityService.GetAll(UmbracoObjectTypes.Document, distinct).ToDictionary(entity => entity.Key, entity => entity.Name);
    }

    private async Task<EntryPage> PageAsync<T>(Paged<T> paged, int page, int pageSize, Func<T, Row> toRow)
    {
        var rows = paged.Items.Select(toRow).ToList();
        var variants = rows.Count == 0
            ? []
            : entityService.GetAll(UmbracoObjectTypes.Document, rows.Select(row => row.VariantKey).Distinct().ToArray()).ToDictionary(entity => entity.Key);
        var recipeIds = variants.Values.Where(variant => !variant.Trashed).Select(variant => variant.ParentId).Distinct().ToArray();
        var recipes = recipeIds.Length == 0
            ? []
            : entityService.GetAll(UmbracoObjectTypes.Document, recipeIds).ToDictionary(entity => entity.Id, entity => entity.Name);
        var names = await members.NamesAsync(rows.Select(row => row.MemberKey));

        return new EntryPage(
            paged.Total,
            Math.Max(0, page),
            Math.Clamp(pageSize, 1, ContributionReads.MaxPageSize),
            rows.Select(row =>
            {
                var variant = variants.GetValueOrDefault(row.VariantKey);
                return new Entry(
                    row.Id,
                    row.VariantKey,
                    variant?.Name,
                    variant is { Trashed: false } ? recipes.GetValueOrDefault(variant.ParentId) : null,
                    names.GetValueOrDefault(row.MemberKey),
                    row.Rating,
                    row.Text,
                    row.Created);
            }).ToList());
    }

    private sealed record Row(int Id, Guid VariantKey, Guid MemberKey, decimal? Rating, string Text, DateTime Created);
}
```

A page names its variants and recipes with two entity lookups and its members with one cached lookup, however many
entries it holds. The entity service reads saved content, so an entry on a draft or unpublished variant still shows
its names; only a trashed variant loses its recipe's name, because its parent is the recycle bin.

- [ ] **Step 4: The endpoints**

Replace `src/KCC.Contributions/Dashboard/ContributionsDashboardController.cs` with:

```csharp
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Common.Builders;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Web.Common.Authorization;

namespace KCC.Contributions.Dashboard;

[ApiVersion("1.0")]
[VersionedApiBackOfficeRoute("kcc/contributions")]
[ApiExplorerSettings(GroupName = "KCC")]
[Authorize(Policy = AuthorizationPolicies.RequireAdminAccess)]
public class ContributionsDashboardController(
    IDashboardQueries queries,
    IDashboardMembers members,
    IContributionWrites contributionWrites) : ManagementApiControllerBase
{
    [HttpGet("waiting")]
    [ProducesResponseType<WaitingModel>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Waiting() => Ok(await queries.WaitingAsync());

    [HttpPost("members/{memberKey:guid}/approval")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(Guid memberKey) =>
        await members.ApproveAsync(memberKey) ? NoContent() : NotFound();

    [HttpGet("reviews")]
    [ProducesResponseType<EntryPage>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Reviews(int page = 0, int pageSize = 20) => Ok(await queries.ReviewsAsync(page, pageSize));

    [HttpPut("reviews/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditReview(int id, ReviewEdit edit)
    {
        if (!RatingMath.IsValidRating(edit.Rating))
        {
            return Invalid("A rating runs from 0.5 to 5 in half-star steps.");
        }

        return await contributionWrites.EditReviewAsync(id, edit.Rating, edit.Text) ? NoContent() : NotFound();
    }

    [HttpDelete("reviews/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReview(int id) =>
        await contributionWrites.DeleteReviewByIdAsync(id) ? NoContent() : NotFound();

    [HttpGet("notes")]
    [ProducesResponseType<EntryPage>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Notes(int page = 0, int pageSize = 20) => Ok(await queries.NotesAsync(page, pageSize));

    [HttpPut("notes/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditNote(int id, NoteEdit edit)
    {
        if (string.IsNullOrWhiteSpace(edit.Text))
        {
            return Invalid("A cook note needs text.");
        }

        return await contributionWrites.EditNoteAsync(id, edit.Text) ? NoContent() : NotFound();
    }

    [HttpDelete("notes/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNote(int id) =>
        await contributionWrites.DeleteNoteByIdAsync(id) ? NoContent() : NotFound();

    private BadRequestObjectResult Invalid(string title) => BadRequest(new ProblemDetailsBuilder().WithTitle(title).Build());
}
```

The controller checks a rating and a note's text before it calls the store, so a bad edit is a 400 with a title the
dashboard shows, not an exception. The store keeps its own checks for any other caller.

- [ ] **Step 5: Run the tests**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/ContributionsDashboardApiTests/*"
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: 0 warnings; 12 passed; then the whole suite, the SQLite concurrency test included. The recipes these tests
publish are uncategorised and named for minerals, and every rating is below five stars, so no search assertion
elsewhere moves.

- [ ] **Step 6: Commit**

```bash
git add -A src/KCC.Contributions tests/KCC.IntegrationTests
git commit -m "Moderate Reviews and Cook Notes from the Dashboard API"
```

---

### Task 7: The Contributions dashboard

The dashboard itself (spec §10, §11): a **Contributions** tab in the Content section, first among Umbraco's own
dashboards and shown to administrators only, with three tabs of its own.
- **Waiting**, the default, has two boxes. Members waiting for approval (name, username, email, registered date) each
  have **Approve**. Recipes and variants waiting to be published link to their nodes, newest first, with the author
  and, for a variant, its recipe.
- **Reviews** and **Cook notes** list their entries a page at a time. **Edit** opens a rating choice (reviews only,
  in half steps) and the text in place; **Delete** asks first.

Each action confirms itself with a backoffice notification and reloads its list. A failed call shows Umbraco's own
error notification (`tryExecute`) and leaves the list as it was.

**Files:**
- Create: `src/KCC.Contributions/Client/src/api.ts`,
  `src/KCC.Contributions/Client/src/{contributions-dashboard,waiting-tab,entries-tab}.element.ts`
- Modify: `src/KCC.Contributions/Client/src/bundle.ts`

**Interfaces:**
- Consumes: Task 5's `waiting` and `members/{key}/approval` and Task 6's `reviews` and `notes` endpoints; Task 2's
  `format.ts`.
- Produces:
  - `api.ts`: the types `WaitingMember`, `WaitingDraft`, `Waiting`, `Entry`, `EntryPage`, `EntryKind`
    (`'reviews' | 'notes'`) and `EntryEdit`; `getWaiting(host)`, `approveMember(host, key) → Promise<boolean>`,
    `getEntries(host, kind, page, pageSize)`, `editEntry(host, kind, id, edit) → Promise<boolean>` and
    `deleteEntry(host, kind, id) → Promise<boolean>`.
  - The dashboard `KCC.Dashboard.Contributions` (element `kcc-contributions-dashboard`, path `contributions`, weight
    100), for the Content section and administrators only; the elements `kcc-waiting-tab` and
    `kcc-entries-tab kind="reviews|notes"`.
  - Markup Task 8's suite relies on:
    - the tabs are `uui-tab`s labelled **Waiting**, **Reviews** and **Cook notes**;
    - a waiting member is `uui-table-row[data-member='<username>']` with an **Approve** button;
    - a draft is `uui-table-row[data-draft='<name>']`, holding a link named after the draft and, for a variant, its
      recipe's name;
    - an entry is `kcc-entries-tab uui-box[data-entry='<id>']` with **Edit** and **Delete**; its editor has a
      `uui-select` for the rating, a `uui-textarea`, **Cancel** and **Save**;
    - a delete is confirmed with the **Delete** button of `umb-confirm-modal`.

- [ ] **Step 1: The calls**

Create `src/KCC.Contributions/Client/src/api.ts`:

```ts
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api'
import { umbHttpClient } from '@umbraco-cms/backoffice/http-client'
import { tryExecute } from '@umbraco-cms/backoffice/resources'

const base = '/umbraco/management/api/v1/kcc/contributions'

// Without a security scheme the backoffice client sends no bearer token.
const security = [{ scheme: 'bearer', type: 'http' }] as const

export interface WaitingMember {
  key: string
  name: string
  userName: string
  email: string
  registered: string
}

export interface WaitingDraft {
  key: string
  kind: 'recipe' | 'variant'
  name: string
  recipeName: string | null
  authorName: string | null
  created: string
}

export interface Waiting {
  members: WaitingMember[]
  drafts: WaitingDraft[]
}

export interface Entry {
  id: number
  variantKey: string
  variantName: string | null
  recipeName: string | null
  memberName: string | null
  rating: number | null
  text: string | null
  created: string
}

export interface EntryPage {
  total: number
  page: number
  pageSize: number
  items: Entry[]
}

export type EntryKind = 'reviews' | 'notes'

export interface EntryEdit {
  rating?: number
  text: string
}

export async function getWaiting(host: UmbControllerHost): Promise<Waiting | undefined> {
  const { data } = await tryExecute(host, umbHttpClient.get<{ 200: Waiting }>({ url: `${base}/waiting`, security }))
  return data
}

export async function approveMember(host: UmbControllerHost, key: string): Promise<boolean> {
  const { error } = await tryExecute(host, umbHttpClient.post({ url: `${base}/members/${key}/approval`, security }))
  return !error
}

export async function getEntries(host: UmbControllerHost, kind: EntryKind, page: number, pageSize: number): Promise<EntryPage | undefined> {
  const { data } = await tryExecute(
    host,
    umbHttpClient.get<{ 200: EntryPage }>({ url: `${base}/${kind}`, query: { page, pageSize }, security }),
  )
  return data
}

export async function editEntry(host: UmbControllerHost, kind: EntryKind, id: number, edit: EntryEdit): Promise<boolean> {
  const { error } = await tryExecute(host, umbHttpClient.put({ url: `${base}/${kind}/${id}`, body: edit, security }))
  return !error
}

export async function deleteEntry(host: UmbControllerHost, kind: EntryKind, id: number): Promise<boolean> {
  const { error } = await tryExecute(host, umbHttpClient.delete({ url: `${base}/${kind}/${id}`, security }))
  return !error
}
```

The dashboard's pages are 1-based, as `uui-pagination` counts; the API's are 0-based, so `entries-tab` subtracts one.

- [ ] **Step 2: The Waiting tab**

Create `src/KCC.Contributions/Client/src/waiting-tab.element.ts`:

```ts
import { css, customElement, html, nothing, state } from '@umbraco-cms/backoffice/external/lit'
import { UMB_EDIT_DOCUMENT_WORKSPACE_PATH_PATTERN } from '@umbraco-cms/backoffice/document'
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element'
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification'
import { UmbTextStyles } from '@umbraco-cms/backoffice/style'
import { approveMember, getWaiting, type Waiting, type WaitingMember } from './api.js'
import { formatDateTime } from './format.js'

@customElement('kcc-waiting-tab')
export class KccWaitingTabElement extends UmbLitElement {
  @state()
  private waiting?: Waiting

  @state()
  private approving?: string

  override connectedCallback() {
    super.connectedCallback()
    void this.#load()
  }

  async #load() {
    this.waiting = await getWaiting(this)
  }

  async #approve(member: WaitingMember) {
    this.approving = member.key
    try {
      if (await approveMember(this, member.key)) {
        const notifications = await this.getContext(UMB_NOTIFICATION_CONTEXT)
        notifications?.peek('positive', { data: { message: `${member.userName} can now sign in.` } })
        await this.#load()
      }
    } finally {
      this.approving = undefined
    }
  }

  override render() {
    if (!this.waiting) {
      return html`<uui-loader></uui-loader>`
    }

    return html`
      <uui-box headline="Members waiting for approval">
        ${this.waiting.members.length === 0 ? html`<p class="empty">No one is waiting.</p>` : this.#renderMembers()}
      </uui-box>
      <uui-box headline="Recipes and variants waiting to be published">
        ${this.waiting.drafts.length === 0 ? html`<p class="empty">Nothing is waiting.</p>` : this.#renderDrafts()}
      </uui-box>
    `
  }

  #renderMembers() {
    return html`
      <uui-table aria-label="Members waiting for approval">
        <uui-table-head>
          <uui-table-head-cell>Name</uui-table-head-cell>
          <uui-table-head-cell>Username</uui-table-head-cell>
          <uui-table-head-cell>Email</uui-table-head-cell>
          <uui-table-head-cell>Registered</uui-table-head-cell>
          <uui-table-head-cell></uui-table-head-cell>
        </uui-table-head>
        ${this.waiting!.members.map(
          (member) => html`
            <uui-table-row data-member=${member.userName}>
              <uui-table-cell>${member.name}</uui-table-cell>
              <uui-table-cell>${member.userName}</uui-table-cell>
              <uui-table-cell>${member.email}</uui-table-cell>
              <uui-table-cell>${formatDateTime(member.registered)}</uui-table-cell>
              <uui-table-cell>
                <uui-button
                  look="primary"
                  color="positive"
                  label="Approve"
                  .state=${this.approving === member.key ? 'waiting' : undefined}
                  ?disabled=${this.approving !== undefined}
                  @click=${() => this.#approve(member)}></uui-button>
              </uui-table-cell>
            </uui-table-row>
          `,
        )}
      </uui-table>
    `
  }

  #renderDrafts() {
    return html`
      <uui-table aria-label="Recipes and variants waiting to be published">
        <uui-table-head>
          <uui-table-head-cell>Name</uui-table-head-cell>
          <uui-table-head-cell>Type</uui-table-head-cell>
          <uui-table-head-cell>Submitted by</uui-table-head-cell>
          <uui-table-head-cell>Created</uui-table-head-cell>
        </uui-table-head>
        ${this.waiting!.drafts.map(
          (draft) => html`
            <uui-table-row data-draft=${draft.name}>
              <uui-table-cell>
                <a href=${UMB_EDIT_DOCUMENT_WORKSPACE_PATH_PATTERN.generateAbsolute({ unique: draft.key })}>${draft.name}</a>
                ${draft.kind === 'variant' && draft.recipeName ? html`<small>of ${draft.recipeName}</small>` : nothing}
              </uui-table-cell>
              <uui-table-cell>${draft.kind === 'recipe' ? 'Recipe' : 'Variant'}</uui-table-cell>
              <uui-table-cell>${draft.authorName ?? '—'}</uui-table-cell>
              <uui-table-cell>${formatDateTime(draft.created)}</uui-table-cell>
            </uui-table-row>
          `,
        )}
      </uui-table>
    `
  }

  static override readonly styles = [
    UmbTextStyles,
    css`
      :host {
        display: grid;
        gap: var(--uui-size-layout-1);
      }

      .empty {
        color: var(--uui-color-text-alt);
      }

      small {
        display: block;
        color: var(--uui-color-text-alt);
      }
    `,
  ]
}

export default KccWaitingTabElement

declare global {
  interface HTMLElementTagNameMap {
    'kcc-waiting-tab': KccWaitingTabElement
  }
}
```

One approval runs at a time, so a second click cannot race the list's reload.

- [ ] **Step 3: The Reviews and Cook notes tabs**

Create `src/KCC.Contributions/Client/src/entries-tab.element.ts`:

```ts
import { css, customElement, html, nothing, property, state } from '@umbraco-cms/backoffice/external/lit'
import { UMB_EDIT_DOCUMENT_WORKSPACE_PATH_PATTERN } from '@umbraco-cms/backoffice/document'
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element'
import { umbConfirmModal } from '@umbraco-cms/backoffice/modal'
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification'
import { UmbTextStyles } from '@umbraco-cms/backoffice/style'
import { deleteEntry, editEntry, getEntries, type Entry, type EntryKind, type EntryPage } from './api.js'
import { formatDateTime, formatRating, maxTextLength, ratingSteps, totalPagesFor } from './format.js'

const pageSize = 20

interface Draft {
  id: number
  rating: number
  text: string
}

@customElement('kcc-entries-tab')
export class KccEntriesTabElement extends UmbLitElement {
  @property()
  kind: EntryKind = 'reviews'

  @state()
  private entries?: EntryPage

  @state()
  private page = 1

  @state()
  private draft?: Draft

  get #isReviews() {
    return this.kind === 'reviews'
  }

  override connectedCallback() {
    super.connectedCallback()
    void this.#load()
  }

  async #load() {
    this.entries = await getEntries(this, this.kind, this.page - 1, pageSize)
  }

  async #notify(message: string) {
    const notifications = await this.getContext(UMB_NOTIFICATION_CONTEXT)
    notifications?.peek('positive', { data: { message } })
  }

  async #save() {
    const draft = this.draft
    if (!draft || (!this.#isReviews && draft.text.trim() === '')) {
      return
    }

    const saved = await editEntry(this, this.kind, draft.id, this.#isReviews ? { rating: draft.rating, text: draft.text } : { text: draft.text })
    if (saved) {
      this.draft = undefined
      await this.#notify(this.#isReviews ? 'Review saved.' : 'Cook note saved.')
      await this.#load()
    }
  }

  async #delete(entry: Entry) {
    const noun = this.#isReviews ? 'review' : 'cook note'
    const confirmed = await umbConfirmModal(this, {
      headline: `Delete ${noun}?`,
      content: `This permanently removes the ${noun} by ${entry.memberName ?? 'a deleted member'}.`,
      color: 'danger',
      confirmLabel: 'Delete',
    }).then(
      () => true,
      () => false,
    )
    if (confirmed && (await deleteEntry(this, this.kind, entry.id))) {
      await this.#notify(this.#isReviews ? 'Review deleted.' : 'Cook note deleted.')
      await this.#load()
    }
  }

  #goTo(page: number) {
    this.page = page
    void this.#load()
  }

  override render() {
    if (!this.entries) {
      return html`<uui-loader></uui-loader>`
    }

    if (this.entries.total === 0) {
      return html`<uui-box><p class="empty">${this.#isReviews ? 'No reviews yet.' : 'No cook notes yet.'}</p></uui-box>`
    }

    const pages = totalPagesFor(this.entries.total, this.entries.pageSize)
    return html`
      ${this.entries.items.map((entry) => this.#renderEntry(entry))}
      ${pages > 1
        ? html`<uui-pagination
            .current=${this.page}
            .total=${pages}
            @change=${(event: Event) => this.#goTo((event.target as HTMLElement & { current: number }).current)}></uui-pagination>`
        : nothing}
    `
  }

  #renderEntry(entry: Entry) {
    const editing = this.draft?.id === entry.id
    return html`
      <uui-box data-entry=${entry.id}>
        <div slot="headline" class="headline">
          <a href=${UMB_EDIT_DOCUMENT_WORKSPACE_PATH_PATTERN.generateAbsolute({ unique: entry.variantKey })}>
            ${entry.recipeName ? `${entry.recipeName} — ` : ''}${entry.variantName ?? 'Deleted variant'}
          </a>
        </div>
        <div slot="header-actions">
          ${editing
            ? nothing
            : html`
                <uui-button
                  label="Edit"
                  look="secondary"
                  @click=${() => (this.draft = { id: entry.id, rating: entry.rating ?? 5, text: entry.text ?? '' })}></uui-button>
                <uui-button label="Delete" look="secondary" color="danger" @click=${() => this.#delete(entry)}></uui-button>
              `}
        </div>
        <p class="meta">
          <strong>${entry.memberName ?? 'Deleted member'}</strong>
          ${this.#isReviews && entry.rating !== null ? html`<span>${formatRating(entry.rating)}</span>` : nothing}
          <span>${formatDateTime(entry.created)}</span>
        </p>
        ${editing ? this.#renderEditor() : html`<p class="text">${entry.text ?? ''}</p>`}
      </uui-box>
    `
  }

  #renderEditor() {
    const draft = this.draft!
    return html`
      <div class="editor">
        ${this.#isReviews
          ? html`
              <uui-select
                label="Rating"
                .options=${ratingSteps.map((step) => ({ name: formatRating(step), value: String(step), selected: step === draft.rating }))}
                @change=${(event: Event) => (this.draft = { ...draft, rating: Number((event.target as HTMLSelectElement).value) })}></uui-select>
            `
          : nothing}
        <uui-textarea
          label="Text"
          maxlength=${maxTextLength}
          auto-height
          .value=${draft.text}
          @input=${(event: Event) => (this.draft = { ...draft, text: (event.target as HTMLTextAreaElement).value })}></uui-textarea>
        <div class="actions">
          <uui-button label="Cancel" look="secondary" @click=${() => (this.draft = undefined)}></uui-button>
          <uui-button
            label="Save"
            look="primary"
            color="positive"
            ?disabled=${!this.#isReviews && draft.text.trim() === ''}
            @click=${this.#save}></uui-button>
        </div>
      </div>
    `
  }

  static override readonly styles = [
    UmbTextStyles,
    css`
      :host {
        display: grid;
        gap: var(--uui-size-space-4);
      }

      .meta {
        display: flex;
        flex-wrap: wrap;
        gap: var(--uui-size-space-4);
        margin: 0 0 var(--uui-size-space-3);
        color: var(--uui-color-text-alt);
      }

      .text {
        white-space: pre-wrap;
        margin: 0;
      }

      .editor {
        display: grid;
        gap: var(--uui-size-space-3);
      }

      .actions {
        display: flex;
        justify-content: flex-end;
        gap: var(--uui-size-space-3);
      }

      .empty {
        color: var(--uui-color-text-alt);
      }
    `,
  ]
}

export default KccEntriesTabElement

declare global {
  interface HTMLElementTagNameMap {
    'kcc-entries-tab': KccEntriesTabElement
  }
}
```

Each entry links to its variant's node. One entry is edited at a time, in place. A note's **Save** stays disabled
while its text is blank, matching the API's 400. `umbConfirmModal` rejects on **Cancel**, which the `then` turns into
`false`.

- [ ] **Step 4: The dashboard and its manifest**

Create `src/KCC.Contributions/Client/src/contributions-dashboard.element.ts`:

```ts
import { css, customElement, html, state } from '@umbraco-cms/backoffice/external/lit'
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element'
import { UmbTextStyles } from '@umbraco-cms/backoffice/style'
import './entries-tab.element.js'
import './waiting-tab.element.js'

type Tab = 'waiting' | 'reviews' | 'notes'

const tabs: Array<{ tab: Tab; label: string }> = [
  { tab: 'waiting', label: 'Waiting' },
  { tab: 'reviews', label: 'Reviews' },
  { tab: 'notes', label: 'Cook notes' },
]

@customElement('kcc-contributions-dashboard')
export class KccContributionsDashboardElement extends UmbLitElement {
  @state()
  private tab: Tab = 'waiting'

  override render() {
    return html`
      <uui-tab-group>
        ${tabs.map(
          ({ tab, label }) =>
            html`<uui-tab label=${label} ?active=${this.tab === tab} @click=${() => (this.tab = tab)}>${label}</uui-tab>`,
        )}
      </uui-tab-group>
      <div class="panel">${this.#renderTab()}</div>
    `
  }

  #renderTab() {
    switch (this.tab) {
      case 'reviews':
        return html`<kcc-entries-tab kind="reviews"></kcc-entries-tab>`
      case 'notes':
        return html`<kcc-entries-tab kind="notes"></kcc-entries-tab>`
      default:
        return html`<kcc-waiting-tab></kcc-waiting-tab>`
    }
  }

  static override readonly styles = [
    UmbTextStyles,
    css`
      :host {
        display: block;
        padding: var(--uui-size-layout-1);
      }

      .panel {
        margin-top: var(--uui-size-layout-1);
      }
    `,
  ]
}

export default KccContributionsDashboardElement

declare global {
  interface HTMLElementTagNameMap {
    'kcc-contributions-dashboard': KccContributionsDashboardElement
  }
}
```

Switching tabs renders the tab afresh, so each one reloads its list when it is opened.

Replace `src/KCC.Contributions/Client/src/bundle.ts` with:

```ts
export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'dashboard',
    alias: 'KCC.Dashboard.Contributions',
    name: 'KCC Contributions Dashboard',
    element: () => import('./contributions-dashboard.element.js'),
    // Above Umbraco's own content dashboards, so the Content section opens on this one.
    weight: 100,
    meta: {
      label: 'Contributions',
      pathname: 'contributions',
    },
    conditions: [
      { alias: 'Umb.Condition.SectionAlias', match: 'Umb.Section.Content' },
      { alias: 'Umb.Condition.CurrentUser.IsAdmin' },
    ],
  },
]
```

The `IsAdmin` condition only hides the tab from other users; the controller's `RequireAdminAccess` policy is what
keeps them out (Task 5's test).

- [ ] **Step 5: Build and test**

```bash
yarn build:all
ls src/KCC.Contributions/wwwroot/App_Plugins/KCC.Contributions
yarn workspace @kcc/contributions test
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/BackofficePackageTests/*"
```

Expected: the client type-checks and builds, with a `contributions-dashboard.element-` chunk beside the bundle, which
imports the two tabs itself; 5 Vitest tests pass; 2 passed.

- [ ] **Step 6: Look at it**

In the throwaway backoffice (see the Global Constraints), in both themes:
1. Content opens on **Contributions** → **Waiting**, with both boxes saying nothing is waiting.
2. In a second tab, at `http://127.0.0.1:5890/account/login`, sign up a member named for a composer. Back in the
   dashboard, reopen **Waiting**: the member is listed. **Approve** shows "… can now sign in." and the row goes.
   Signing in with that member now works.
3. Sign in there as `e2e-member` / `E2E-Member-Passw0rd` and run **Create Recipe** to the end. **Waiting** lists the
   recipe and its first variant ("of …"), each linking to its node.
4. **Reviews** lists the seeded reviews newest first, their authors "Deleted member" (see the probe's findings), 20 to
   a page; the pager shows once there are more. **Edit** a review to 2.5 stars with new text → **Save**: the
   notification, the new rating and text. **Delete** it: the confirmation, then the entry is gone.
5. **Cook notes** says "No cook notes yet." until the member writes one on a variant page; then its **Save** stays
   disabled for blank text.

Stop the site.

- [ ] **Step 7: Commit**

```bash
git add -A src/KCC.Contributions/Client
git commit -m "Add the Contributions Dashboard to the Content Section"
```

---

### Task 8: The gate's flows, end to end

The phase gate's four flows become one E2E suite (spec §15): approve a member, publish a submission, edit and delete a
review, and suggest an icon, which the submission flow does on its way. Each test sets its case up on the public site
the way a visitor or the E2E member would, through Phase 4's sign-up form, wizard and review editor, and finishes it
in the backoffice as the fixture's admin. Where it can, it then checks the public site for the result.

The E2E site listens on plain HTTP, so the fixture turns `UseHttps` off for the backoffice's sign-in, and it blanks
the Anthropic key so the icon suggestion is the deterministic fallback. The fixture also refuses to start without the
two backoffice bundles, whose absence would otherwise show up as a missing editor deep inside a test.

**Files:**
- Modify: `tests/KCC.E2ETests/Config/SiteProcess.cs`
- Create: `tests/KCC.E2ETests/Config/BackofficeSession.cs`, `tests/KCC.E2ETests/Features/Backoffice/BackofficeTests.cs`

**Interfaces:**
- Consumes: Phase 4's `MemberSession.SignInAsync(IPage)`, `MemberSession.Serial` and `MemberTestVariant.Path`, and the
  locators of its `MemberFlowTests` (sign-up, the Create Recipe wizard) and `VariantReviewsTests` (the review editor);
  the markup Tasks 3, 4 and 7 list.
- Produces:
  - `SiteProcess.AdminEmail` and `SiteProcess.AdminPassword`, the fixture's unattended admin.
  - `KCC.E2ETests.Config.BackofficeSession`: `Serial`, `DashboardPath`, `LoadTimeout` and `SignInAsync(IPage)`.

- [ ] **Step 1: Let the fixture's site serve the backoffice**

Apply to `tests/KCC.E2ETests/Config/SiteProcess.cs`. Put the admin's credentials in constants at the top of the class,
so the backoffice helper signs in with the same pair:

```diff
 public sealed class SiteProcess : IAsyncInitializer, IAsyncDisposable
 {
+    public const string AdminEmail = "admin@example.test";
+
+    public const string AdminPassword = "E2E-Passw0rd-2026";
+
     private static readonly string Configuration =
```

In `SiteEnvironment()`, use them:

```diff
-            ["Umbraco__CMS__Unattended__UnattendedUserEmail"] = "admin@example.test",
-            ["Umbraco__CMS__Unattended__UnattendedUserPassword"] = "E2E-Passw0rd-2026",
+            ["Umbraco__CMS__Unattended__UnattendedUserEmail"] = AdminEmail,
+            ["Umbraco__CMS__Unattended__UnattendedUserPassword"] = AdminPassword,
```

and add these entries after Phase 4's `["RateLimits__SubmissionsPerHour"]`:

```csharp

            // The site listens on plain HTTP, and the backoffice's sign-in refuses it while UseHttps is on.
            ["Umbraco__CMS__Global__UseHttps"] = "false",

            // The icon suggestion takes its fallback instead of calling Anthropic, whatever key the environment holds.
            ["Anthropic__ApiKey"] = string.Empty,
```

In `RequireBuildOutput()`, name the root build and check the two backoffice bundles:

```diff
         var web = RepoPaths.WebProject;
         if (!File.Exists(Path.Combine(web, "wwwroot", ".vite", "manifest.json")) || !File.Exists(Path.Combine(web, "wwwroot", "ssr", "Server.Entry.js")))
         {
-            throw new InvalidOperationException("The front-end bundles are missing. Run `yarn build:all` in src/KCC.Web first.");
+            throw new InvalidOperationException("The front-end bundles are missing. Run `yarn build:all` at the repository root first.");
+        }
+
+        foreach (var package in new[] { "KCC.Admin", "KCC.Contributions" })
+        {
+            if (!File.Exists(Path.Combine(RepoPaths.Root, "src", package, "wwwroot", "App_Plugins", package, "umbraco-package.json")))
+            {
+                throw new InvalidOperationException($"The {package} backoffice bundle is missing. Run `yarn build:all` at the repository root first.");
+            }
         }
     }
```

Create `tests/KCC.E2ETests/Config/BackofficeSession.cs`:

```csharp
using Microsoft.Playwright;

namespace KCC.E2ETests.Config;

/// <summary>Signs the fixture's unattended admin in to the backoffice.</summary>
public static class BackofficeSession
{
    // Parallel sign-ins as one backoffice user left all but one of them on the sign-in page, so the suites that sign
    // in as the admin run one at a time.
    public const string Serial = "backoffice-admin";

    public const string DashboardPath = "/umbraco/section/content/dashboard/contributions";

    // The backoffice downloads its whole client before it draws a section: longer than Playwright's five seconds.
    public const float LoadTimeout = 30_000;

    public static async Task SignInAsync(IPage page)
    {
        _ = await page.GotoAsync("/umbraco");
        await page.FillAsync("#username-input", SiteProcess.AdminEmail);
        await page.FillAsync("#password-input", SiteProcess.AdminPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Login", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(url => url.Contains("/umbraco/section/", StringComparison.Ordinal), new() { Timeout = 2 * LoadTimeout });
    }
}
```

```bash
dotnet build tests/KCC.E2ETests/KCC.E2ETests.csproj
```

Expected: 0 warnings.

- [ ] **Step 2: Write the suite**

Create `tests/KCC.E2ETests/Features/Backoffice/BackofficeTests.cs`:

```csharp
using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Backoffice;

[NotInParallel(new[] { BackofficeSession.Serial, MemberSession.Serial })]
public class BackofficeTests : BasePageTests
{
    private const string NewcomerPassword = "Newcomer-Passw0rd";

    [Test]
    public async Task ApprovingANewMember_LetsThemSignIn()
    {
        var userName = $"schumann-{Guid.NewGuid():N}"[..17];
        await SignUpAsync(userName);

        await BackofficeSession.SignInAsync(Page);
        _ = await Page.GotoAsync(BackofficeSession.DashboardPath);
        var row = Page.Locator($"kcc-waiting-tab uui-table-row[data-member='{userName}']");
        await Expect(row).ToBeVisibleAsync(new() { Timeout = BackofficeSession.LoadTimeout });
        await row.GetByRole(AriaRole.Button, new() { Name = "Approve" }).ClickAsync();
        await Expect(row).ToHaveCountAsync(0);

        await Context.ClearCookiesAsync();
        _ = await Page.GotoAsync("/account/login");
        await Page.FillAsync("input[name='UserName']", userName);
        await Page.FillAsync("input[name='Password']", NewcomerPassword);
        await Page.ClickAsync("form button[type='submit']");
        await Page.WaitForURLAsync(url => !url.Contains("/account/login", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task PublishingASubmission_PutsItOnTheSite()
    {
        var recipeName = $"Garnet Stew {Guid.NewGuid():N}"[..20];
        await MemberSession.SignInAsync(Page);
        await SubmitRecipeAsync(recipeName);
        await Context.ClearCookiesAsync();

        await BackofficeSession.SignInAsync(Page);
        _ = await Page.GotoAsync(BackofficeSession.DashboardPath);
        var recipeRow = Page.Locator($"kcc-waiting-tab uui-table-row[data-draft='{recipeName}']");
        await recipeRow.GetByRole(AriaRole.Link, new() { Name = recipeName }).ClickAsync(new() { Timeout = BackofficeSession.LoadTimeout });
        await ChooseThenSuggestAnIconAsync();
        await PublishAsync();

        _ = await Page.GotoAsync(BackofficeSession.DashboardPath);
        var variantRow = Page.Locator("kcc-waiting-tab uui-table-row[data-draft='First Try']").Filter(new() { HasText = recipeName });
        await variantRow.GetByRole(AriaRole.Link, new() { Name = "First Try" }).ClickAsync(new() { Timeout = BackofficeSession.LoadTimeout });
        var ingredients = Page.Locator("kcc-ingredients-editor");
        var steps = Page.Locator("kcc-instructions-editor");
        await Expect(ingredients.Locator("uui-input.name input").First).ToHaveValueAsync("Eggs", new() { Timeout = BackofficeSession.LoadTimeout });
        await Expect(steps.Locator("uui-textarea textarea").First).ToHaveValueAsync("Scramble gently.");
        await ingredients.GetByRole(AriaRole.Button, new() { Name = "Add ingredient" }).ClickAsync();
        await ingredients.Locator("uui-input.name input").Last.FillAsync("Chives");
        await ingredients.Locator("input.unit").Last.FillAsync("Pinch");
        await steps.GetByRole(AriaRole.Button, new() { Name = "Add step" }).ClickAsync();
        await steps.Locator("uui-textarea textarea").Last.FillAsync("Garnish.");
        await PublishAsync();

        _ = await Page.GotoAsync(BackofficeSession.DashboardPath);
        await Expect(Page.Locator("kcc-waiting-tab uui-box").First).ToBeVisibleAsync(new() { Timeout = BackofficeSession.LoadTimeout });
        await Expect(recipeRow).ToHaveCountAsync(0);
        await Expect(variantRow).ToHaveCountAsync(0);

        _ = await Page.GotoAsync($"/recipes/{recipeName.ToLowerInvariant().Replace(' ', '-')}/first-try");
        await Expect(Page.GetByText("Chives").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Garnish.").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task EditingAndDeletingAReview_ReachesTheVariantPage()
    {
        var written = $"Backoffice E2E review {Guid.NewGuid():N}";
        var edited = $"Edited by the owner {Guid.NewGuid():N}";
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync(MemberTestVariant.Path);
        await Page.Locator("[data-value='4']").First.ClickAsync();
        await Page.Locator("[data-testid='review-input']").FillAsync(written);
        await Page.Locator("[data-testid='submit-review']").ClickAsync();
        await Expect(Page.Locator("[data-testid='reviews-list']").GetByText(written)).ToBeVisibleAsync();
        await Context.ClearCookiesAsync();

        await BackofficeSession.SignInAsync(Page);
        _ = await Page.GotoAsync(BackofficeSession.DashboardPath);
        await Page.GetByRole(AriaRole.Tab, new() { Name = "Reviews" }).ClickAsync(new() { Timeout = BackofficeSession.LoadTimeout });
        var id = await Page.Locator("kcc-entries-tab uui-box").Filter(new() { HasText = written }).GetAttributeAsync("data-entry");
        var review = Page.Locator($"kcc-entries-tab uui-box[data-entry='{id}']");
        await review.GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();
        await review.Locator("uui-select select").SelectOptionAsync("2.5");
        await review.Locator("uui-textarea textarea").FillAsync(edited);
        await review.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await Expect(review).ToContainTextAsync(edited);
        await Expect(review).ToContainTextAsync("2.5 ★");

        _ = await Page.GotoAsync(MemberTestVariant.Path);
        await Expect(Page.Locator("[data-testid='reviews-list']").GetByText(edited)).ToBeVisibleAsync();

        _ = await Page.GotoAsync(BackofficeSession.DashboardPath);
        await Page.GetByRole(AriaRole.Tab, new() { Name = "Reviews" }).ClickAsync(new() { Timeout = BackofficeSession.LoadTimeout });
        await review.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await Page.Locator("umb-confirm-modal").GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await Expect(review).ToHaveCountAsync(0);
    }

    private async Task SignUpAsync(string userName)
    {
        _ = await Page.GotoAsync("/account/login");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign Up", Exact = true }).ClickAsync();
        await Page.FillAsync("input[name='UserName']", userName);
        await Page.FillAsync("input[name='Email']", $"{userName}@example.test");
        await Page.FillAsync("input[name='Password']", NewcomerPassword);
        await Page.FillAsync("input[name='PasswordConfirmation']", NewcomerPassword);
        await Page.ClickAsync("form button[type='submit']");
        await Page.WaitForURLAsync(url => url.Contains("/account/registration-complete", StringComparison.OrdinalIgnoreCase));
    }

    // The variant's prep time, cook time and servings are mandatory, so they are filled here, where a member would.
    private async Task SubmitRecipeAsync(string recipeName)
    {
        _ = await Page.GotoAsync("/recipes/create-recipe");
        var next = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });
        await Page.GetByPlaceholder("e.g., Mac & Cheese").FillAsync(recipeName);
        await Page.GetByPlaceholder("A short description of this dish").FillAsync("Submitted by the backoffice E2E suite.");
        await next.ClickAsync();
        await Page.GetByPlaceholder("e.g., Classic Stovetop").FillAsync("First Try");
        await Page.GetByPlaceholder("What makes this variant special?").FillAsync("The first way.");
        await Page.Locator("input[id$='-prep-time']").FillAsync("10");
        await Page.Locator("input[id$='-cook-time']").FillAsync("20");
        await Page.Locator("input[id$='-servings']").FillAsync("4");
        await next.ClickAsync();
        await Page.GetByPlaceholder("Ingredient name").First.FillAsync("Eggs");
        await next.ClickAsync();
        await Page.GetByPlaceholder("Describe this step").First.FillAsync("Scramble gently.");
        await next.ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Submit for Review" }).ClickAsync();
        await Expect(Page.GetByText("Recipe Submitted!")).ToBeVisibleAsync();
    }

    // The submission's icon is already the name's fallback, so a suggestion alone would change nothing visible: pick
    // another icon from the grid first, then let the suggestion replace it.
    private async Task ChooseThenSuggestAnIconAsync()
    {
        var editor = Page.Locator("kcc-recipe-icon-editor");
        var preview = editor.Locator(".preview i");
        await Expect(preview).ToBeVisibleAsync(new() { Timeout = BackofficeSession.LoadTimeout });
        var submitted = await preview.GetAttributeAsync("class");
        var other = submitted == "fa-duotone fa-cheese" ? "fa-duotone fa-egg" : "fa-duotone fa-cheese";

        await editor.GetByRole(AriaRole.Button, new() { Name = "Select icon" }).ClickAsync();
        var picker = Page.Locator("kcc-icon-picker-modal");
        await picker.GetByLabel("Search icons").FillAsync(other["fa-duotone fa-".Length..]);
        await picker.Locator($"button[title='{other}']").ClickAsync();
        await picker.GetByRole(AriaRole.Button, new() { Name = "Select icon" }).ClickAsync();
        await Expect(preview).ToHaveAttributeAsync("class", other);

        var suggestion = Page.WaitForResponseAsync(response => response.Url.Contains("/kcc/recipe-editor/icon-suggestion", StringComparison.Ordinal));
        await editor.GetByRole(AriaRole.Button, new() { Name = "Suggest with AI" }).ClickAsync();
        var suggested = (await (await suggestion).JsonAsync())!.Value.GetProperty("icon").GetString()!;
        await Expect(preview).ToHaveAttributeAsync("class", suggested);
        _ = await Assert.That(suggested).IsNotEqualTo(other);
    }

    private async Task PublishAsync()
    {
        var published = Page.WaitForResponseAsync(response =>
            response.Request.Method == "PUT"
                && response.Url.Contains("/umbraco/management/api/v1/document/", StringComparison.Ordinal)
                && response.Url.EndsWith("/publish", StringComparison.Ordinal));
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save and publish", Exact = true }).ClickAsync();
        _ = await Assert.That((await published).Ok).IsTrue();
    }
}
```

What the tests lean on:
- **Sign-out between roles** is `Context.ClearCookiesAsync()`: the member's session is not what these tests are about,
  and Phase 4's suites cover the sign-out form.
- **The reviewed variant** is Phase 4's `MemberTestVariant`, which no read-only suite asserts on. The review is four
  stars and the edit two and a half, below the spotlight's five (see the Global Constraints). The E2E member has one
  review per variant, so a review left behind by another suite is overwritten, not doubled.
- **The published recipe** is named `Garnet Stew <8 hex>`, with no category and no tags, so Phase 3's listing and
  facet assertions do not move.
- **Waits.** Every wait is an auto-waiting assertion or a response. `LoadTimeout` covers the backoffice's first draw
  after each navigation; everything after it uses Playwright's defaults.

- [ ] **Step 3: Run the suite**

```bash
dotnet build KitchenCommandCenter.sln
yarn build:all
dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj -- --treenode-filter "/*/*/BackofficeTests/*"
dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj
```

The member flows need `KCC_E2E_MEMBER_USERNAME` and `KCC_E2E_MEMBER_PASSWORD` in the environment, as for Phase 4.

Expected: 3 passed, then every E2E test: Phase 4's count plus 3. If a locator misses, dump the section's markup (for a
backoffice element, `await Page.Locator("kcc-waiting-tab").EvaluateAsync<string>("e => e.shadowRoot.innerHTML")`) and
adjust the locator, never the app. The backoffice's own locators here all ran in the probe; the public-site ones are
Phase 4's.

- [ ] **Step 4: Commit**

```bash
git add -A tests/KCC.E2ETests
git commit -m "Drive the Backoffice Gate Flows End to End"
```

---

### Task 9: Docs, memory and the Phase 5 gate

**Files:**
- Modify: `README.md`, `CLAUDE.md`, `docs/replatform/specs/2026-09-21-replatform-off-xperience.md` (one line of §10),
  this plan's Status line
- Memory, outside the repo, in `~/.claude/projects/-Users-twinright-Repos-Kitchen-Command-Center/memory/`:
  - `umbraco-backoffice-extensions.md` (new)
  - `replatform-off-xperience.md`
  - `MEMORY.md`

- [ ] **Step 1: Correct the docs this phase invalidated**

In `README.md`'s **Font Awesome Pro** section, the registry and the build output moved:
- Replace "The registry is configured in committed `.npmrc` files (`src/KCC.Web/.npmrc` and
  `src/KCC.Admin/Client/.npmrc`)" with "The registry is configured in the committed root `.npmrc`".
- Replace "Before running `yarn install` in **either** `src/KCC.Web` or `src/KCC.Admin/Client`:" with "Before running
  `yarn install` at the repository root, which installs every workspace:".
- In the last sentence, replace "and `src/KCC.Admin/Client/dist/` are all git-ignored" with "and the backoffice bundles
  in `src/KCC.*/wwwroot/App_Plugins/` are all git-ignored".

In its **E2E tests** subsection, replace "`yarn build:all` (in `src/KCC.Web`)" with "`yarn build:all` (at the
repository root)".

In the **Members** subsection Phase 4 added, replace the approval and draft sentences
("…approves it: Members → the member → **Approved** → **Save**." and "A member's recipe or variant is saved as a draft
under Recipes: …") with:

```markdown
Anyone can sign up, and the account waits until the owner approves it: Content → **Contributions** → **Waiting** →
**Approve** (the member's **Approved** toggle in the Members section does the same). A member's recipe or variant is
saved as a draft under Recipes, and **Waiting** lists it: open it, fill in anything it lacks, then **Save and
publish**. The **Reviews** and **Cook notes** tabs edit or delete what members have written.
```

Keep the subsection's sentences about lockout, anti-forgery and rate limits as they are. After the subsection, add:

```markdown
#### Backoffice extensions

The recipe editors (ingredients, instructions and the icon, with **Suggest with AI**) and the Contributions dashboard
are Lit + TypeScript clients in `src/KCC.Admin/Client` and `src/KCC.Contributions/Client`, on the shared Vite base in
`packages/admin-client-config`. `yarn build:all` at the repository root builds them into their projects'
`wwwroot/App_Plugins/`, which the site serves to the backoffice; without that build the backoffice has no editors
for those fields and no dashboard. Rebuilding a client needs no .NET build: restart the site and reload the
backoffice. Run a client's tests with `yarn workspace @kcc/admin test` or `yarn workspace @kcc/contributions test`.
```

In `CLAUDE.md`, add this section after **SQLite writes**:

```markdown
## Backoffice extensions

The backoffice's recipe editors and the Contributions dashboard are Lit clients in `src/KCC.Admin/Client` and
`src/KCC.Contributions/Client`, built by Vite from `packages/admin-client-config` into gitignored
`wwwroot/App_Plugins/` folders. Build them with the root `yarn build:all`. Three things fail without a word:

- A Management API call through `umbHttpClient` sends no token unless it passes
  `security: [{ scheme: 'bearer', type: 'http' }]`, and the endpoint answers 401.
- When a library `KCC.Web` already references gains its first controller, an incremental build keeps a stale
  `src/KCC.Web/obj/*/net10.0/KCC.Web.MvcApplicationPartsAssemblyInfo.cs` and every new route answers 404. Delete it.
- Never name a file or folder `icon`: `.gitignore`'s macOS `Icon` rule ignores it.
```

In the spec, §10's Waiting bullet says "never-published recipes and variants". Umbraco cannot tell (see "What the
scratch probe already proved"), so replace "never-published recipes and variants" with "recipes and variants that are
not published", which is what the dashboard lists and what the account page calls "Pending review". Mention the
change to the owner at the gate.

```bash
git add README.md CLAUDE.md docs/replatform/specs/2026-09-21-replatform-off-xperience.md
git commit -m "Document the Backoffice Extensions"
```

- [ ] **Step 2: Record what this phase learned**

Earlier phases may already have edited these files. Apply each change to the text that describes the same thing now.

Create `umbraco-backoffice-extensions.md`:

```markdown
---
name: umbraco-backoffice-extensions
description: KCC's Lit backoffice extensions (KCC.Admin editors, KCC.Contributions dashboard) — how they build and load, and the traps that fail silently (stale application parts, bearer scheme, HTTPS token endpoint, Vite lib mode, shadow-root fonts, the .gitignore Icon rule, parallel sign-ins of one backoffice user)
metadata:
  type: project
---

Since replatform Phase 5 (<date>) the backoffice extensions live in two Razor class libraries: `KCC.Admin` (the
ingredients, instructions and recipe-icon property editors, and the `kcc/recipe-editor` Management API) and
`KCC.Contributions` (the Contributions dashboard and `kcc/contributions`). Each Lit client builds with Vite from
`packages/admin-client-config` into its project's gitignored `wwwroot/App_Plugins/<project>/`; the root
`yarn build:all` builds both before KCC.Web's bundles.

The traps, each met while proving the phase:
- A library KCC.Web already references gains its first controller: an incremental build keeps a stale
  `src/KCC.Web/obj/*/net10.0/KCC.Web.MvcApplicationPartsAssemblyInfo.cs` and every new route answers 404.
- `umbHttpClient` sends the backoffice token only with `security: [{ scheme: 'bearer', type: 'http' }]`.
- The back-office token endpoint refuses plain HTTP while `Umbraco:CMS:Global:UseHttps` is on (the default):
  integration tests call it at `https://localhost`; the E2E site sets `UseHttps` to false.
- Vite library mode inlines every asset, the Font Awesome font twice, so the shared config makes a plain build with
  hashed names and writes `umbraco-package.json` itself.
- Browsers ignore `@font-face` in a shadow root: the icon editor adds the font to `document.fonts` and imports the
  CSS `?raw` with its font faces stripped.
- `.gitignore`'s `Icon` line ignores any path named `icon` ([[gitignore-icon-rule]]).
- Parallel sign-ins as one backoffice user leave all but one on the sign-in page, so backoffice E2E tests run one at
  a time. The console's `[UmbAuthClient] Token request failed: 400` on every full backoffice load is noise.

**Why:** each of these failed silently or misleadingly during the Phase 5 probe.

**How to apply:** check this list before changing a backoffice client or adding a Management API controller. See
[[replatform-off-xperience]].
```

In `MEMORY.md`, add a line for it after the replatform line:

```markdown
- [Umbraco backoffice extensions](umbraco-backoffice-extensions.md) — Lit clients in KCC.Admin/KCC.Contributions built into gitignored wwwroot/App_Plugins by the root `yarn build:all`; traps: stale MvcApplicationParts file 404s a new controller, umbHttpClient needs the bearer security scheme, token endpoint needs HTTPS, never name a path `icon`
```

and append to the replatform line: `Phase 5 landed (<date>): Lit backoffice editors and the Contributions
dashboard.` In `replatform-off-xperience.md`, replace the paragraph that says Phase 5 is planned with one line saying it
is done, with the date and the commit range.

- [ ] **Step 3: The gate — the owner's flows in a fresh clone**

Stop any site on port 58671, then clone the branch fresh and start it, with the E2E member's variables in the
environment:

```bash
GATE="$(mktemp -d)/kcc-phase-5-gate"
git clone --branch replatform --single-branch /Users/twinright/Repos/Kitchen-Command-Center "$GATE"
cd "$GATE" && yarn install --frozen-lockfile && yarn build:all
cd src/KCC.Web && dotnet watch --non-interactive
```

Once it serves, seed it from the main checkout:

```bash
curl -sk -X POST https://localhost:58671/api/dev/seed-recipes | head -c 300; echo
```

Expected: the summary ends `E2E member +1.`.

The backoffice is the owner's account (its password is in user-secrets), so **the owner** does the backoffice steps;
the public-site steps can be driven in the Browser pane. In the backoffice's light theme, then its dark one:
1. On the public site, **Sign Up** a new member. Backoffice → Content opens on **Contributions** → **Waiting**, which
   lists them. **Approve**: the member now signs in.
2. As that member, run **Create Recipe** with a name no seeded recipe uses. **Waiting** lists the recipe and its first
   variant. Open the recipe: **Suggest with AI** answers with an icon (from the model when user-secrets hold an
   Anthropic key, otherwise the name's fallback), and **Select icon** picks from the grid. **Save and publish**.
3. Open the variant from **Waiting**: its ingredients and instructions show as rows. Add an ingredient, drag a step,
   fill in anything the wizard left out, **Save and publish**. **Waiting** no longer lists either, and about two
   seconds later a search on `/recipes` finds the recipe.
4. As the member, review that variant. **Reviews** lists it first. **Edit** it to 2.5 stars with new text: the variant
   page shows the edit. **Delete** it: the page no longer lists it.
5. Delete the recipe, empty the recycle bin, and delete the member in the Members section.

On the public site, in both ramps, the new recipe's and variant's pages render like the seeded ones. Stop the gate
site and delete `$GATE`.

- [ ] **Step 4: Everything, once more**

From the main checkout:

```bash
dotnet build KitchenCommandCenter.sln
yarn build:all
yarn workspace @kcc/admin test && yarn workspace @kcc/contributions test
(cd src/KCC.Web && yarn test && yarn type-check)
node tests/scripts/run.mjs
```

Expected:
- `Build succeeded` with 0 warnings.
- The four bundles build, the admin client's 17 and the contributions client's 5 Vitest tests pass, and KCC.Web's
  Vitest suite and type check pass.
- The combined run is green: unit, integration (Phase 4's count plus 21) and E2E (Phase 4's count plus 3), and the
  Admin Frontend and Contributions Frontend suites in its report. It opens its HTML report when it finishes.

- [ ] **Step 5: Close the phase**

Set this file's **Status** line to `done (<date>)`, then commit it:

```bash
git add docs/replatform/plans/2026-09-25-phase-5-backoffice.md
git commit -m "Close Replatform Phase 5"
```

Phase 6 (Home) is planned next, in this folder, against the code as it then stands. Its Block List and blocks are core
Umbraco and need no client of their own. If a block needs a custom preview or editor, it belongs in `KCC.Admin`'s
client, on the base this phase built.

## Findings from Phase 5

Found during Phase 5 (2026-09-29). The Phase 6 to 8 plans predate them.

- **The branch.** Phase 5 ran on `replatform-phase-5`, based on `replatform-phase-4` and rebased by the owner onto
  `fe8ba92`, which adds the Phase 7 and 8 plans. None of Phases 2 to 5 is merged into `replatform` yet.
- **Build order.** The SDK registers a Razor class library's `wwwroot/**` content root only if the folder exists
  when `ResolveProjectStaticWebAssets` runs, and both libraries' `wwwroot` holds only untracked build output. So a
  `dotnet build` before the first root `yarn build:all` left the site without either backoffice package until the
  next .NET build, and the E2E backoffice tests timed out. Both library projects now create `wwwroot` first
  (`EnsureWebRoot`), so the order no longer matters, and "a client rebuilt while the site is stopped needs no .NET
  build" holds from a fresh clone too. The E2E fixture's bundle check still looks only for the files on disk.
- **Where the code departs from this plan's text.**
  - Task 1 has a fifth recipe-editor test: an API user in Umbraco's Translators group gets 403 from all three
    endpoints. The plan's four tests still pass with `SectionAccessContent` removed. `BackofficeClient` gained
    `TranslatorAsync`.
  - Task 3's sorter. The shared `identifier: 'KCC.Sorter.JsonArray'` let a row be dragged from the ingredients list
    into the steps list, where it broke both editors, so each list's sorter keeps its own default identifier. The
    `.rows` container is now always rendered, and the sorter is disabled while the editor is read-only or its value
    is unreadable: the plan's version bound the sorter once to a container that read-only and unreadable mounts never
    render, so every such mount threw and drag stayed dead after **Start fresh**.
  - Task 3's look. Plain **Save** keeps a draft that fails validation and shows the message; only **Save and
    publish** refuses. Umbraco 17.7 saves drafts regardless.
  - Task 7's dashboard. Deleting the last entry on the last page drops back a page (`clampPage` in `format.ts`, with
    3 Vitest cases). A failed load keeps the list on screen, and a failed first load says so. A failed action reloads
    the list, so stale rows drop. The delete confirmation passes its text as a Lit template: `umb-confirm-modal`
    renders string content as raw HTML, and the text holds the member's display name, which members type freely.
  - Task 8's `PublishAsync` waits for `PUT …/document/{id}/update-and-publish`. In 17.7, **Save and publish** on an
    existing document calls that endpoint, not `…/publish` as "What the scratch probe already proved" says. The review
    test also removes the E2E member's review when it fails, as Phase 4's tests on the shared variant do, and checks
    the variant page after the delete.
  - Task 9's CLAUDE.md remedy for the stale application-parts file deletes `KCC.Web.MvcApplicationPartsAssemblyInfo.*`,
    the `.cs` and its `.cache`. The SDK regenerates the `.cs` only when the `.cache` is missing or stale, so deleting
    the `.cs` alone leaves the site with no application parts at all.
  - Approval saves the member through `IMemberService` inside `IMemberWriteLock`, as Task 5 says. Phase 4's closing
    note (the member editing service) is superseded by CLAUDE.md's SQLite-writes rule: the editing service's update
    reaches `MemberUserStore.UpdateAsync`, which CLAUDE.md lists as unguarded.
- **Known limitations.**
  - Backspacing across the decimal point of an ingredient quantity rewrites it ("1.5", Backspace, "7" gives "71").
    UUI's own `uui-input type=number` does it without any binding. A text field would avoid it but silently store
    inputs like "1/2" as no quantity, so the number field stays.
  - The backoffice bundles ship with their source maps, and `App_Plugins` is public once Cloudflare Access covers only
    `/umbraco`. Phase 7 decides whether production builds keep them.
  - The Waiting list reads every unapproved member. Cap or page it before sign-up opens at launch.
- **Counts.** Unit 190, unchanged. Integration 220 (Phase 4's 198 plus 22), E2E 36 (plus 3), admin Vitest 17 and
  contributions Vitest 8.
- **Not taken up here, from Phase 4's findings.**
  - Un-approving or locking out a signed-in member still does not end their session. The dashboard only approves.
  - The Anthropic client still has no timeout, and the icon provider still swallows cancellation, so **Suggest with
    AI** inherits both.
- **The gate** ran from a fresh copy of `replatform-phase-5` (`59f49c2`), built `dotnet build` first and then the root
  `yarn build:all`, and started as the E2E fixture starts its site: the Testing environment, the fixture's admin, the
  SSR sidecar, and no Anthropic key.
  - A new member, `gate-lili`, signed up. Content opened on **Contributions** → **Waiting**, which listed them;
    **Approve** said "gate-lili can now sign in.", and they could.
  - As `gate-lili`, **Create Recipe** "Garnet Gate Chowder" with "Classic Pot". **Waiting** listed both, newest first.
    **Suggest with AI** sent the description as edited and not yet saved, and answered the name's fallback. **Select
    icon** picked `bowl-hot`. **Save and publish** showed one transient "does not have a URL" warning; the Info tab
    listed `/recipes/garnet-gate-chowder/` and the page answered 200.
  - The variant showed the wizard's ingredient and steps as rows. An added ingredient, a third step and a dragged step
    (renumbered) published, **Waiting** emptied, and search found the recipe with its icon, author and time.
  - `gate-lili` reviewed the variant at 4 stars. **Reviews** listed it first; **Edit** to 2.5 stars with new text
    reached the variant page and search; **Delete** asked first, then the page said "No reviews yet.".
  - Trashing the recipe (its read-only workspace hid the icon editor's buttons), emptying the recycle bin and deleting
    the member all worked. The new pages rendered like the seeded ones in both ramps, and the backoffice screens in
    both themes. The log held no stall or lock error.
  - The owner's own pass on a dev site, with the real Anthropic key and the owner's account, is still to do.
- **Spec change.** §10's Waiting line now reads "recipes and variants that are not published", which is what the
  dashboard lists and what the account page calls "Pending review".
