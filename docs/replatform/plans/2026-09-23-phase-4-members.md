# Replatform Phase 4 — Members and Writes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Status:** not started. **Resume point:** "Before you start", then Task 1. **Requires Phase 3 done:** its Status
line reads `done (<date>)` and `node tests/scripts/run.mjs` is green on `replatform`.

**Goal:** Members sign up, wait for the owner's approval, sign in and out, and keep their profile. They write
reviews, cook notes and cooked marks, and submit recipes and variants for review. Every state-changing request is
checked for its anti-forgery token and rate-limited per client, and concurrent writes no longer stall on SQLite. The
member E2E flows and the SQLite concurrency test are green.

**Architecture:** Umbraco's member identity replaces Kentico's. A rewritten account API and profile API sit on
`IMemberSignInManager` and `IMemberManager`, and hijacked controllers serve the account, login, settings,
registration-complete and wizard pages. Approval is Umbraco's `IsApproved` flag, which its sign-in manager already
enforces (spec §19 row 5). The contribution store gains its remaining writes and two cascade handlers, all behind
its write lock. Submissions are saved, never published, through the content editing service. Two SQLite guards make
concurrent writes safe:
- a decorator that makes Umbraco's post-save relations update take a write lock before it reads;
- a member write lock around every member save KCC starts.

The spec's concurrency test proves them. Rate limits use ASP.NET Core's limiter, keyed on `CF-Connecting-IP`.

**Tech Stack:** Umbraco.Cms 17.x (the version Phase 1 pinned): its member identity (`MemberSignInManager`,
`MemberManager`) and its editing services. ASP.NET Core rate limiting and anti-forgery, from the shared framework.
EF Core 10 through Umbraco's scope. The Anthropic SDK, already referenced. TUnit 1.27 + Moq,
Microsoft.AspNetCore.Mvc.Testing, TUnit.Playwright, Vitest.

**Spec:** `docs/replatform/specs/2026-09-21-replatform-off-xperience.md`. Sections: §6.3 (sign-out is a POST route),
§6.4 (the one read of unpublished content), §6.5 (member security), §8 (members, submissions and security), §9.1
(cascades), §12 (the E2E member), §14 (the integration tests, the concurrency test and the E2E flows), §15 (the
Phase 4 row and gates), §17 (defects fixed), §18 (the SQLite risk) and §19 row 5 (approval is one flag).

## Before you start: reconcile with Phases 1–3 as built

This plan was written from Phase 2's and Phase 3's *plans*, while Phase 1 was running (it had reached its Task 3).
Check each item below. Where the code differs, adapt the step that depends on it and note the change in your task
report.

```bash
grep -n "Status:" docs/replatform/plans/2026-09-23-phase-3-search.md
sed -n '/Unported slices/,/<\/ItemGroup>/p' src/KCC.Web/KCC.Web.csproj tests/KCC.UnitTests/KCC.UnitTests.csproj \
  tests/KCC.E2ETests/KCC.E2ETests.csproj
grep -c "KCC.Admin.csproj" KitchenCommandCenter.sln
sed -n '/public interface IContributionWrites/,/^}/p;/public sealed class ContributionWrites/,/^{/p' src/KCC.Contributions/ContributionWrites.cs
grep -n "Task<.*Async(" src/KCC.Contributions/ContributionReads.cs
grep -n "AddNotificationAsyncHandler\|AddSingleton<IContributionWrites" src/KCC.Contributions/ContributionsComposer.cs
grep -n "class ReviewApiController\|class CookNoteApiController" -A 6 src/KCC.Web/Features/Api/ReviewApiController.cs \
  src/KCC.Web/Features/Api/CookNoteApiController.cs
grep -n "new ReviewApiController\|new CookNoteApiController" tests/KCC.UnitTests/Features/Api/*.cs
sed -n '/public interface IRecipeQueries/,/^}/p' src/KCC.Web/Features/Recipes/RecipeQueries.cs
grep -n "private static RecipeRecord RecipeFrom" src/KCC.Web/Features/Recipes/RecipeQueries.cs
grep -n "public static string FormatDisplayName\|AddSingleton<IAuthorNameProvider" -r src/KCC.Web/Features/Providers
grep -n "IContributionWrites contributionWrites)\|EnsureAuthorsAsync\|authors +" \
  src/KCC.Web/Features/DevTools/RecipeSeed/RecipeTestDataSeeder.cs
grep -n "private Dictionary<string, string> Settings\|SeedAsync\|RecipeSearch:RebuildDelay" tests/KCC.IntegrationTests/Config/UmbracoSite.cs
grep -n "public static\|TemplateKey\|private static async Task<Guid> PublishedAsync" tests/KCC.IntegrationTests/Config/TestContent.cs
grep -n "public static async Task<RenderedPage> GetAsync\|public string? Attribute\|public JsonElement Prop" \
  tests/KCC.IntegrationTests/Config/RenderedPage.cs
grep -n 'id="api-config"\|antiforgeryToken =' src/KCC.Web/Features/Pages/Shared/Layout.cshtml
grep -rln '/account/logout' src/KCC.Web/uSync/v17/Content
ls src/KCC.Web/Features/Models/Generated | grep -E \
  '^(AccountPage|LoginPage|AccountSettingsPage|RegistrationCompletePage|CreateRecipePage|AddVariantPage)\.generated\.cs$'
ls src/KCC.Web/Features/Dictionary/baseline-strings.json 2>/dev/null || ls src/KCC.Web/uSync/v17/Dictionary | head -3
grep -c "<Mandatory>true</Mandatory>" src/KCC.Web/uSync/v17/ContentTypes/recipe.config \
  src/KCC.Web/uSync/v17/ContentTypes/recipevariant.config
grep -n '\["DataProtection__KeysDirectory"\]' tests/KCC.E2ETests/Config/SiteProcess.cs
grep -n "AddScoped<IRecipeQueries" src/KCC.Web/Program.cs
grep -n "public static string ActionFor" src/KCC.Web/Features/Extensions/UrlHelperExtensions.cs
grep -rn "stripTilde\|['\"]~/[a-z]" src/KCC.Web/Features/Components/Header/MenuItem.vue src/KCC.Web/Features/Pages/Account \
  tests/KCC.ViteTests/Features/Pages/Account || echo "no tildes left"
ls src/KCC.Web/Views/Page.cshtml 2>/dev/null || echo "no template fallback"
echo "E2E member password length: ${#KCC_E2E_MEMBER_PASSWORD}"
```

Expected, and what to do if not:

1. Phase 3's Status is `done`. If not, stop: this phase builds on it.
2. The `Unported slices` groups hold exactly these lines, and this plan deletes all of them except the last group's
   `Features/HomePage/**`, which is Phase 6's, and the two `Admin/Home*Tests.cs` lines Task 9 leaves for Phase 5:
   - `KCC.Web.csproj`: `Features/Api/{AccountApiController,ProfileApiController,RecipeApiController,VariantCookedApiController}.cs`,
     `Features/Models/Api/**`, `Features/Models/Common/KCCApplicationUser.cs`,
     `Features/Pages/{Account,AddVariant,CreateRecipe}/**`, `Features/Providers/RecipeIconProvider.cs`, and one
     `Content Remove` line for each of `Features/Pages/{Account,AddVariant,CreateRecipe}/**/*.cshtml`.
   - `KCC.UnitTests.csproj`: `Admin/**`,
     `Features/Api/{CookNoteApiControllerTests,ProfileApiControllerTests,RecipeApiControllerTests,ReviewApiControllerTests,VariantCookedApiControllerTests}.cs`
     and `Features/Pages/Account/**`.
   - `KCC.E2ETests.csproj`: `Features/HomePage/**`, `Features/VariantCookNotes/**`, `Features/VariantCooked/**`,
     `Features/VariantReviews/**` and `Features/RecipeSearch/RecipeSearchLiveRatingTests.cs`.
3. `KCC.Admin.csproj` is not in the solution (the count is 0). Phase 1 took it out; Task 9 brings it back.
4. `IContributionWrites` declares only `Task UpsertReviewAsync(Guid variantKey, Guid memberKey, decimal rating, string text)`.
   `ContributionWrites` takes `(IEFCoreScopeProvider<ContributionsDbContext> scopes, IContributionStats stats, IEventAggregator eventAggregator)`,
   takes `scope.WriteLock(ContributionLocks.Contributions)` before its first read, and ends with `stats.Invalidate();`
   and `PublishAsync(new ReviewsChangedNotification())`. `IContributionReads` has `ReviewsAsync`, `MemberReviewAsync`,
   `NotesAsync` and `HasCookedAsync`, and `VariantStats` has `CookedCount`.
5. `ReviewApiController` takes `(IContributionStats, IContributionReads, IAuthorNameProvider, IMemberManager)` and
   `CookNoteApiController` takes `(IContributionReads, IAuthorNameProvider, IMemberManager)`, routes `api/variant` and
   `api`, GET only. `ReviewListTests` and `CookNoteListTests` build them with exactly those arguments.
6. `IRecipeQueries` declares `GetRecipePage`, `GetVariantPage`, `GetPublishedRecipes` and `GetCreateRecipeUrl`, and
   `RecipeQueries` has a `private static RecipeRecord RecipeFrom(Recipe recipe)`.
7. `AuthorNameProvider.FormatDisplayName(string firstName, string lastName, string userName)` is public and static,
   and `ProvidersComposer` registers `IAuthorNameProvider`.
8. `RecipeTestDataSeeder`'s constructor ends with `IContributionWrites contributionWrites)`, it has
   `EnsureAuthorsAsync`, and `SeedSummary.ToString()` ends `authors +{AuthorsCreated}.`.
9. `UmbracoSite` builds its settings in `Settings()` and seeds in `InitializeAsync`, and its `RebuildDelay` setting is
   `00:00:00.100`.
10. `TestContent` has `RecipeListing`, `RecipeAsync`, `VariantAsync`, `Author`, `Pick`, `CategoryAsync`,
    `TrashAsync`, `UnpublishAsync`, `AuthorAsync` and `RenameAuthor`, and a private
    `PublishedAsync(IServiceProvider services, string contentTypeAlias, string name, Guid parentKey, IEnumerable<PropertyValueModel> values)`.
11. `RenderedPage` has `GetAsync(HttpClient, string)`, `Attribute(string)` and `Prop(string)`.
12. `Layout.cshtml` writes `<script type="application/json" id="api-config">` with `antiforgeryToken =
    Antiforgery.GetAndStoreTokens(Context).RequestToken`. The Vue client already sends it as the
    `RequestVerificationToken` header.
13. The baseline's Site Settings content links the Account group's **Logout** to `/account/logout`. The header menu
    turns that link into the sign-out form (Task 6).
14. All six generated page models exist.
15. **The dictionary baseline.** If `src/KCC.Web/Features/Dictionary/baseline-strings.json` exists, Phase 1 took its
    dictionary fallback, and Task 6 edits the three strings there instead of in `uSync/v17/Dictionary`.
16. `recipe` has 2 mandatory properties (description, icon) and `recipeVariant` 7. A submission that leaves one out
    is still saved; see the Global Constraints.
17. `SiteProcess.SiteEnvironment()` has a `["DataProtection__KeysDirectory"]` entry; Task 10 adds lines after it.
18. `Program.cs` registers `IRecipeQueries` scoped; Tasks 3, 4 and 9 add registrations after it.
19. `UrlHelperExtensions.ActionFor` exists.
20. No tilde URL is left; the pattern skips `~/` alias imports, which start with a capital. Phase 1's Task 8
    removed `stripTilde` from `MenuItem.vue` and `AccountSettingsView.Component.vue`. The account specs' fixtures
    were `~/…` URLs, and Phase 1 had to change them to `/…` for its Vitest run to pass. Tasks 4 and 6 edit those
    files as they are now.
21. **The §19 template fallback.** If `src/KCC.Web/Views/Page.cshtml` exists, `TestContent.PublishedAsync` sets
    `TemplateKey`, and so must the draft helper Task 4 adds (`SavedAsync`) and the submission service (Task 9):
    set `TemplateKey` the same way `PublishedAsync` does.
22. **The E2E member's password** has at least 8 characters; the line prints only its length. Task 1's rules
    reject a shorter password, and the seeder then fails to create the member. If it is 0, the variables are not
    set (see Task 11, Step 3). If it is under 8, ask the owner for a new password, in `~/.zshenv` and in the
    repository's `KCC_E2E_MEMBER_PASSWORD` secret. Either way, ask the owner to confirm the secret's length: it
    cannot be read back, and a short one fails CI's E2E job.

Before any task starts the dev site, check that nothing else is listening on port 58671.

## What the scratch probe already proved

All of the following ran in a copy of Phase 3's scratch Umbraco 17.7 site on SQLite, using this plan's code blocks
verbatim. The run was green on every repeat: 132 unit tests (30 of them new) and 134 integration tests (56 new),
with warnings as errors and StyleCop on. The probe could not run three things:
- the E2E suites and the reference capture, which need the front end. Task 11's tests compiled against
  TUnit.Playwright 1.27 and Playwright 1.58 but did not run.
- the dictionary-string test in Task 6, because the probe has no uSync baseline.

The Vue changes ran in a copy of the front end with Phase 1's Task 8 applied: the touched specs' 51 Vitest tests,
and `vue-tsc`.

- **The SQLite hazard is real, and it is not only the one Phase 2 found.** With four EF Core review writers
  running, member saves, sign-in writes and content saves hung for over 100 seconds. Each was retrying a write on a
  stale snapshot, which Umbraco's retry policy repeats for about 9m40s. Two unlocked read-then-write patterns cause
  it:
  - Umbraco's `ContentRelationsUpdate` handles every content, media and member save. It runs after the save's
    transaction has committed, in a scope of its own, and reads the existing relations before it writes. Members
    have no references, so after every member save it reads and then issues a DELETE. That includes the
    lightweight save each sign-in and sign-out makes (`UpdateLoginPropertiesAsync`).
  - `MemberUserStore.UpdateAsync` reads the member and then saves it in one transaction. The sign-in manager calls
    it on success, on a failed attempt (the access-failed count) and on sign-out (the security stamp).
- **The two guards fix it.**
  - `WriteLockedRelationsUpdate` wraps Umbraco's handler and takes the write lock first; the handler's own scope
    joins it.
  - `IMemberWriteLock` takes the member tree's write lock before the store reads.

  With both, the race completes every time. It ran about 60,000 review writes in 20 seconds, alongside 400
  member saves, 200 sign-in, failure and sign-out writes, and backoffice member edits and content edits with
  publishes. Without them, the concurrency test fails with a `TimeoutException` at its 60-second round limit.
  The backoffice's own paths (`IMemberEditingService.UpdateAsync`, `IContentEditingService.UpdateAsync` and
  `IContentPublishingService.PublishAsync`) need only the decorator.
- **Spec §19 row 5 holds.** Umbraco's member user confirmation returns `IsApproved`, and `RequireConfirmedAccount` is
  on. An unapproved member's sign-in is `NotAllowed`. After approval through the member editing service, the
  Members section's own path, the same sign-in succeeds.
- **Umbraco's member defaults differ from the spec:** a 30-day lockout, 10-character passwords, and one session per
  member. With concurrent logins off, a sign-in rotates the security stamp, and stamps are re-checked every 30
  seconds, so a second device is signed out almost at once. Task 1's settings fix all three. The lockout test sees
  15 minutes, and an 8-character password is accepted.
- **The member cookie.**
  - `User.Identity.IsAuthenticated` is true for a signed-in member, on a plain `ControllerBase` and on pages, so
    Phase 1's header check works unchanged.
  - `IMemberManager.GetCurrentMemberAsync()` works in hijacked controllers and APIs.
  - The cookie has no `LoginPath`, and a non-XHR challenge redirects nowhere useful. So every page and endpoint
    checks for the member itself instead of using `[Authorize]`.
- **Anti-forgery.** Umbraco only calls `AddAntiforgery()`, so the header is `RequestVerificationToken` and the form
  field `__RequestVerificationToken`, which is what the client already sends.
  - A token handed to a signed-out page validates the sign-in POST.
  - Once the member is signed in, a token from a signed-in page validates their writes.
  - A missing token is answered with 400.
- **Rate limits.** ASP.NET Core's limiter runs from Umbraco's `PostRouting` pipeline filter, one partition per
  policy and client. The 429 carries a JSON `error`, which the client's API helper shows.
- **Drafts.**
  - `IContentEditingService.CreateAsync` saves a draft that misses a mandatory value and reports
    `PropertyValidationError`. Umbraco then refuses to publish it until the value is filled in.
  - The navigation service lists drafts, and the published cache does not.
  - A draft's member picker holds `umb://member/<key>`, and the picker records an `umbMember` relation.
- **Cascades.** A permanent delete publishes `ContentDeletedNotification` for every descendant. Deleting a recipe
  therefore reaches its variants' rows, and so does emptying the recycle bin. Async handlers run for both it and
  `MemberDeletedNotification`.
- **Search.**
  - A new five-star review on a recipe that sorts after Legendary Lasagna leaves the unfiltered spotlight alone,
    because ties go to the first hit.
  - The service returns a rated hit both in `Results` and as `Spotlight`, so tests read `Results`.
  - Authors' names are indexed as "started by". A test author named Ada Lovelace broke Phase 3's `lovelace`
    search.

## Global Constraints

Phase 1's, Phase 2's and Phase 3's constraints still apply:

- Work on branch **`replatform`**. The replatform's spec, phase plans and reference set are tracked in
  `docs/replatform/`: commit changes to them (a status line, a correction) with the work they describe. Everything
  else under `.superpowers/` stays gitignored: never stage anything there. Commit messages are Title Case imperative
  with no attribution lines. Ask the owner before any `git push`.
- **Umbraco.Cms 17.x, never 18.** Every `Umbraco.Cms*` package takes the same version as `Umbraco.Cms`.
- **The SQLite connection string never contains `Cache=Shared`.**
- **`ModelsMode`** is `SourceCodeManual` in Development and `Nothing` everywhere else. This phase changes no schema,
  so it generates no models and uSync exports no types.
- **Route hijacking.** A page controller is named `<Alias>Controller`, derives from `RenderController`, has no
  `[Route]`, and returns its view by explicit path. An async page hides the base action and adds its own:

  ```csharp
  [NonAction]
  public sealed override IActionResult Index() => throw new NotSupportedException();

  public async Task<IActionResult> Index(CancellationToken cancellationToken)
  ```

- **Only APIs that survive Umbraco 18.** Use `ControllerBase` for APIs, the async editing and publishing services,
  `IDocumentNavigationQueryService`, and the friendly extensions `Children<T>()` and `Url()`. Warnings are errors, so
  an obsolete member fails the build (CS0618).
- **Nullable reference types and StyleCop.**
  - Nullable is disabled in `KCC.Web`, `KCC.Contributions`, `KCC.Admin` and `KCC.UnitTests`. Never write `?` on a
    reference type there: it raises CS8632.
  - `KCC.IntegrationTests` and `KCC.E2ETests` have nullable enabled.
  - StyleCop runs on `src/KCC.Web`. It requires sorted usings, trailing commas in multi-line initializers, and
    static members before instance members. StyleCop 1.1.118 misreads a parenthesized pattern such as
    `is not (A or B)` (SA1008): write `!=` comparisons joined with `&&` instead.
- **Code comments** follow `~/.claude/CLAUDE.md`: explain *why* only, with no narration and no future promises.
- **Always build both front-end bundles together** with `yarn build:all`.
- **Every contributions write takes `scope.WriteLock(ContributionLocks.Contributions)` before its first read**, and
  every review write publishes `ReviewsChangedNotification` after its scope has completed and after
  `stats.Invalidate()`.
- **Tests that change content make their own nodes**, and assertions about the seed filter by category, diet or a
  seeded word; they never count, or take the spotlight of, the whole site.
- **Test commands:**
  - unit: `dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj`
  - integration: `dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`
  - E2E: `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj` (after `dotnet build` and `yarn build:all`)
  - one class: append `-- --treenode-filter "/*/*/<ClassName>/*"`
  - front end: `cd src/KCC.Web && yarn test`, then `yarn type-check`
  - everything: `node tests/scripts/run.mjs` (it opens its HTML report when it finishes)
- **The dev site** runs with `cd src/KCC.Web && dotnet run --launch-profile Local` on `https://localhost:58671`.

Phase 4's own constraints:

- **SQLite writes.** Anything that saves a member through Umbraco's member identity runs inside
  `IMemberWriteLock.RunAsync`. That covers the sign-in manager (sign-in, a failed attempt, sign-out, refreshing the
  cookie) and the member manager (sign-up, a password change), and so does a profile save through
  `IMemberService`. A multi-node content write opens one core scope and takes `Constants.Locks.ContentTree` before
  its first create. The relations decorator in `Features/Sqlite` is never removed: without it every member save can
  stall for minutes.
- **Anti-forgery.** Every KCC controller with a POST, PUT or DELETE action carries `[AutoValidateAntiforgeryToken]`
  at class level. Never add a global anti-forgery filter: the backoffice's Management API authenticates with bearer
  tokens and sends none.
- **Members are checked, not `[Authorize]`d.** An API answers `Unauthorized()` when
  `IMemberManager.GetCurrentMemberAsync()` is null. A member-only page sends the visitor to the login page with
  `SignInRedirect.To`.
- **Only local return URLs** (`Url.IsLocalUrl`); anything else goes to `/`.
- **Rate limits**, fixed windows, per client (`RateLimits.ClientKey`): the `CF-Connecting-IP` header, else the
  socket address.

  | Policy | Endpoints | Limit | Setting |
  |---|---|---|---|
  | `account` | sign-in, sign-up, password change | 10 a minute | `RateLimits:AccountPerMinute` |
  | `contributions` | review, cook-note and cooked writes | 30 a minute | `RateLimits:ContributionsPerMinute` |
  | `submissions` | new recipes and variants | 5 an hour | `RateLimits:SubmissionsPerHour` |

  The integration host keeps the real limits; each `MemberClient` sends its own `CF-Connecting-IP`. The E2E fixture
  raises all three to 1000, because every browser request comes from one address.
- **Submissions are drafts.** They are saved and never published. A submission missing a mandatory value is still
  saved: the create reports `PropertyValidationError`, which counts as saved, and the owner fills the value in
  before Umbraco will publish it.
- **Test data words.** Phase 3's suites search for Australian animals and for `lovelace`, `platypus`, `cupboard`,
  `ramen` and `zephyr`, and assert a single hit. Recipe names, variant names and member first and last names in
  this phase use other words: its recipes are named for other animals (axolotl, pangolin, narwhal, …) and its
  members for other scientists (Grace Hopper, Mary Somerville).
- **The E2E member's credentials** come from `KCC_E2E_MEMBER_USERNAME` and `KCC_E2E_MEMBER_PASSWORD` and are never
  committed. The integration host sets its own pair, `e2e-member` / `E2E-Member-Passw0rd`.
- **Seeded content the member E2E suites write to** is one variant, `/recipes/matcha-panna-cotta/green-tea-set`,
  which no read-only suite asserts on. Suites that sign in as the E2E member carry
  `[NotInParallel(MemberSession.Serial)]`.

## Not in Phase 4

- **Phase 5:** the Contributions dashboard. Until then the owner approves a member with the Members section's
  **Approved** toggle (spec §11), and moderates reviews and notes in the database. Its review edits and deletes
  publish `ReviewsChangedNotification` too.
- **Phase 7:** forwarded headers (trusted from the tunnel container only), HTTPS enforcement and
  `UmbracoApplicationUrl`, Cloudflare Access. They need the tunnel and the compose network. Until then the rate
  limiter trusts `CF-Connecting-IP` from any client, which only matters once a port is published, and Phase 7
  publishes none.
- **Out of scope, as the spec says (§8):** email of any kind, self-serve password reset, account deletion, editing a
  submission once sent, and changing the email address.

## File map

| Path | Change | Task |
|---|---|---|
| `src/KCC.Web/appsettings.json` | Modify (member security) | 1 |
| `tests/KCC.UnitTests/Features/Configuration/MemberSecuritySettingsTests.cs` | Create | 1 |
| `tests/KCC.IntegrationTests/Config/TestMembers.cs`, `Features/Members/MemberApprovalTests.cs` | Create | 1 |
| `src/KCC.Web/Features/Sqlite/{WriteLockedRelationsUpdate,SqliteComposer,MemberWriteLock}.cs` | Create | 2 |
| `tests/KCC.IntegrationTests/Features/Sqlite/{RelationsWriteLockTests,SqliteConcurrencyTests}.cs` | Create; the second grows in 7 | 2, 7 |
| `src/KCC.Web/Features/Security/{RateLimits,SecurityComposer}.cs` | Create | 3 |
| `src/KCC.Web/Features/Pages/Account/AccountPageQueries.cs` | Create | 3 |
| `src/KCC.Web/Features/Api/AccountApiController.cs`, `Features/Pages/Account/Logout/LogoutController.cs` | Rewrite | 3 |
| `src/KCC.Web/Features/Models/Common/KCCApplicationUser.cs` | Delete | 3 |
| `tests/KCC.IntegrationTests/Config/MemberClient.cs`, `Features/Api/AccountApiTests.cs` | Create | 3 |
| `src/KCC.Web/Features/Pages/Account/{AccountViewModel,AuthoredRecipeQueries,SignInRedirect}.cs` | Rewrite / create | 4 |
| `src/KCC.Web/Features/Pages/Account/**/*Controller.cs` → `*PageController.cs`, their views | Move and rewrite | 4 |
| `src/KCC.Web/Features/Pages/Account/AccountView.Component.vue` and its spec | Modify (keys) | 4 |
| `tests/KCC.IntegrationTests/Config/TestContent.cs` | Modify (drafts) | 4 |
| `tests/KCC.IntegrationTests/Features/Pages/AccountPagesTests.cs` | Create | 4 |
| `tests/KCC.UnitTests/Features/Pages/Account/AccountViewModelTests.cs` | Rewrite | 4 |
| `src/KCC.Web/Features/Api/ProfileApiController.cs` | Rewrite | 5 |
| `tests/KCC.IntegrationTests/Features/Api/ProfileApiTests.cs` | Create | 5 |
| `src/KCC.Web/Features/Components/Account/SignOutForm.vue`, `Features/Utilities/Api.ts` | Create / modify | 6 |
| `MenuItem.vue`, `AccountView.Component.vue`, `AccountSettingsView.Component.vue` and their specs | Modify | 6 |
| `src/KCC.Web/uSync/v17/Dictionary/*` (three strings) | Modify | 6 |
| `tests/KCC.IntegrationTests/Features/Baseline/AccountStringsTests.cs` | Create | 6 |
| `src/KCC.Contributions/ContributionWrites.cs` | Modify | 7, 8 |
| `src/KCC.Web/Features/Api/{ReviewApiController,CookNoteApiController,VariantCookedApiController,ContributionResponses}.cs` | Modify / rewrite | 7 |
| `src/KCC.Web/Features/Recipes/RecipeQueries.cs` | Modify | 7 |
| `tests/KCC.IntegrationTests/Features/Api/ContributionWriteApiTests.cs` | Create | 7 |
| `src/KCC.Contributions/{ContributionCascades,ContributionsComposer}.cs` | Create / modify | 8 |
| `tests/KCC.IntegrationTests/Features/Contributions/ContributionCascadeTests.cs` | Create | 8 |
| `src/KCC.Admin/KCC.Admin.csproj`, `KitchenCommandCenter.sln` | Rewrite / modify | 9 |
| `src/KCC.Web/Features/Providers/{RecipeIconProvider,ProvidersComposer}.cs` | Modify | 9 |
| `src/KCC.Web/Features/Submissions/{SubmissionValues,RecipeSubmissions}.cs` | Create | 9 |
| `src/KCC.Web/Features/Api/RecipeApiController.cs`, `Features/Pages/{CreateRecipe,AddVariant}/*` | Rewrite / move | 9 |
| `tests/KCC.UnitTests/Features/{Submissions/SubmissionValuesTests,Providers/RecipeIconProviderTests}.cs` | Create | 9 |
| `tests/KCC.IntegrationTests/Features/Api/SubmissionTests.cs` | Create | 9 |
| `src/KCC.Web/Features/DevTools/RecipeSeed/RecipeTestDataSeeder.cs` | Modify | 10 |
| `tests/KCC.IntegrationTests/Config/UmbracoSite.cs`, `Features/DevTools/RecipeSeederTests.cs` | Modify | 10 |
| `tests/KCC.E2ETests/Config/{SiteProcess,MemberSession,MemberTestVariant}.cs` | Modify / create | 10, 11 |
| `tests/KCC.E2ETests/Features/{VariantReviews,VariantCookNotes,VariantCooked,Members}/*`, `RecipeSearch/RecipeSearchLiveRatingTests.cs` | Rewrite / create | 11 |
| `tests/KCC.UnitTests/Features/Api/*ApiControllerTests.cs` (the five Kentico ones) | Delete | 5, 7, 9 |
| `README.md`, `CLAUDE.md`, memory | Modify | 12 |

These compile again unchanged: `Features/Models/Api/{CreateRecipeRequest,CreateVariantRequest}.cs`,
`Features/Pages/Account/Login/{LoginViewModel.cs,Index.cshtml}`, `Features/Pages/Account/Settings/AccountSettingsViewModel.cs`,
`Features/Pages/CreateRecipe/{CreateRecipeViewModel.cs,Index.cshtml}`, `Features/Pages/AddVariant/Index.cshtml`,
`KCC.Admin`'s `IRecipeIconService.cs`, `RecipeIcons.cs` and `FormComponents/RecipeUnits.cs`, and the unit test
`Admin/RecipeIconsTests`. The Vue wizards, `LoginView` and the variant page's review, note and cooked components are
unchanged too.

---

### Task 1: Prove that approval is the one flag, and set the member rules

Spec §19 row 5 is proven before anything is built on it: a member created unapproved cannot sign in, and approval
is that one flag. The same tests pin spec §6.5's member rules, which differ from Umbraco's defaults: five failures
lock a member out for 15 minutes, and a password needs 8 characters. They also keep today's behaviour of letting a
member stay signed in on several devices, which Umbraco's default would end.

**Files:**
- Create: `tests/KCC.IntegrationTests/Config/TestMembers.cs`,
  `tests/KCC.IntegrationTests/Features/Members/MemberApprovalTests.cs`,
  `tests/KCC.UnitTests/Features/Configuration/MemberSecuritySettingsTests.cs`
- Modify: `src/KCC.Web/appsettings.json`

**Interfaces:**
- Produces (namespace `KCC.IntegrationTests.Config`) `TestMembers`:
  - `const string Password`, which is `Quokka-Passw0rd`;
  - `UniqueUserName(string prefix) → string`;
  - `SignUpAsync(IServiceProvider, string userName, string password = Password) → Task<Guid>`, which creates an
    unapproved member the way the sign-up endpoint will;
  - `ApproveAsync(IServiceProvider, Guid memberKey) → Task`, through the member editing service, as the backoffice
    does;
  - `ApprovedAsync(IServiceProvider, string userName, string password = Password) → Task<Guid>`.
- Produces the settings `Umbraco:CMS:Security:{MemberAllowConcurrentLogins, MemberDefaultLockoutTimeInMinutes,
  MemberPassword:RequiredLength, MemberPassword:MaxFailedAccessAttemptsBeforeLockout}`.

- [ ] **Step 1: Write the proof**

Create `tests/KCC.IntegrationTests/Config/TestMembers.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Config;

// Members that can sign in, created the way the sign-up endpoint creates them.
public static class TestMembers
{
    public const string Password = "Quokka-Passw0rd";

    public static string UniqueUserName(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..(prefix.Length + 9)];

    public static async Task<Guid> SignUpAsync(IServiceProvider services, string userName, string password = Password)
    {
        using var scope = services.CreateScope();
        var members = scope.ServiceProvider.GetRequiredService<IMemberManager>();
        var user = MemberIdentityUser.CreateNew(userName, $"{userName}@example.test", Constants.Security.DefaultMemberTypeAlias, isApproved: false, userName);
        var created = await members.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException($"Signing up {userName} failed: {string.Join(" ", created.Errors.Select(error => error.Description))}");
        }

        return user.Key;
    }

    public static async Task<Guid> ApprovedAsync(IServiceProvider services, string userName, string password = Password)
    {
        var key = await SignUpAsync(services, userName, password);
        await ApproveAsync(services, key);
        return key;
    }

    // The backoffice's Approved toggle saves the member through the editing service, so approval does the same.
    public static async Task ApproveAsync(IServiceProvider services, Guid memberKey)
    {
        using var scope = services.CreateScope();
        var scoped = scope.ServiceProvider;
        var member = scoped.GetRequiredService<IMemberService>().GetById(memberKey) ?? throw new InvalidOperationException($"No member {memberKey}.");
        var superUser = await scoped.GetRequiredService<IUserService>().GetAsync(Constants.Security.SuperUserKey)
            ?? throw new InvalidOperationException("The super user is missing.");
        var updated = await scoped.GetRequiredService<IMemberEditingService>().UpdateAsync(
            memberKey,
            new MemberUpdateModel
            {
                Email = member.Email,
                Username = member.Username,
                IsApproved = true,
                Variants = [new VariantModel { Name = member.Name ?? member.Username }],
            },
            superUser);
        if (!updated.Success)
        {
            throw new InvalidOperationException($"Approving {member.Username} failed: {updated.Status.MemberEditingOperationStatus}.");
        }
    }
}
```

Create `tests/KCC.IntegrationTests/Features/Members/MemberApprovalTests.cs`:

```csharp
using KCC.IntegrationTests.Config;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Security;

namespace KCC.IntegrationTests.Features.Members;

// Spec §19: a member created unapproved cannot sign in, and approval is that one flag.
public class MemberApprovalTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task UnapprovedMember_IsNotAllowedToSignIn()
    {
        var userName = TestMembers.UniqueUserName("waiting");
        await TestMembers.SignUpAsync(Site.Services, userName);

        var result = await CheckPasswordAsync(userName, TestMembers.Password);

        _ = await Assert.That(result.IsNotAllowed).IsTrue();
        _ = await Assert.That(result.Succeeded).IsFalse();
    }

    [Test]
    public async Task ApprovedMember_SignsIn()
    {
        var userName = TestMembers.UniqueUserName("approved");
        var key = await TestMembers.SignUpAsync(Site.Services, userName);

        await TestMembers.ApproveAsync(Site.Services, key);

        _ = await Assert.That((await CheckPasswordAsync(userName, TestMembers.Password)).Succeeded).IsTrue();
    }

    [Test]
    public async Task FiveWrongPasswords_LockTheMemberOutForFifteenMinutes()
    {
        var userName = TestMembers.UniqueUserName("lockout");
        await TestMembers.ApprovedAsync(Site.Services, userName);

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            _ = await CheckPasswordAsync(userName, "not-the-password");
        }

        var result = await CheckPasswordAsync(userName, TestMembers.Password);

        _ = await Assert.That(result.IsLockedOut).IsTrue();
        using var scope = Site.Services.CreateScope();
        var member = await scope.ServiceProvider.GetRequiredService<IMemberManager>().FindByNameAsync(userName);
        _ = await Assert.That(member!.LockoutEnd!.Value).IsBetween(DateTimeOffset.UtcNow.AddMinutes(14), DateTimeOffset.UtcNow.AddMinutes(16));
    }

    [Test]
    [Arguments("Seven77", false)]
    [Arguments("Eight888", true)]
    public async Task Password_NeedsEightCharacters(string password, bool accepted)
    {
        using var scope = Site.Services.CreateScope();
        var userName = TestMembers.UniqueUserName("length");
        var user = MemberIdentityUser.CreateNew(userName, $"{userName}@example.test", Constants.Security.DefaultMemberTypeAlias, isApproved: false, userName);

        var created = await scope.ServiceProvider.GetRequiredService<IMemberManager>().CreateAsync(user, password);

        _ = await Assert.That(created.Succeeded).IsEqualTo(accepted);
    }

    private async Task<SignInResult> CheckPasswordAsync(string userName, string password)
    {
        using var scope = Site.Services.CreateScope();
        var member = await scope.ServiceProvider.GetRequiredService<IMemberManager>().FindByNameAsync(userName)
            ?? throw new InvalidOperationException($"No member {userName}.");
        return await scope.ServiceProvider.GetRequiredService<SignInManager<MemberIdentityUser>>()
            .CheckPasswordSignInAsync(member, password, lockoutOnFailure: true);
    }
}
```

`CheckPasswordSignInAsync` runs the sign-in manager's own checks, approval and lockout included, without issuing a
cookie, so these tests need no HTTP request.

- [ ] **Step 2: Run it**

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/MemberApprovalTests/*"
```

Expected: `UnapprovedMember_IsNotAllowedToSignIn` and `ApprovedMember_SignsIn` pass. Together they are spec §19 row
5. Two tests fail on Umbraco's defaults:
- `FiveWrongPasswords_LockTheMemberOutForFifteenMinutes`: the lockout ends 30 days away;
- `Password_NeedsEightCharacters("Eight888", True)`: the minimum is 10.

If either approval test fails, take §19's fallback. In Task 3, `Login` loads the member with `FindByNameAsync` and
answers `Login.NotAllowedError` while `IsApproved` is false, before it calls the sign-in manager. Record the fallback
in your report and in spec §19's row.

- [ ] **Step 3: Write the settings guard**

Create `tests/KCC.UnitTests/Features/Configuration/MemberSecuritySettingsTests.cs`:

```csharp
namespace KCC.UnitTests.Features.Configuration;

// Umbraco's own defaults are a 30-day lockout, 10-character passwords and one session per member.
public class MemberSecuritySettingsTests
{
    [Test]
    public async Task Lockout_IsFiveFailuresForFifteenMinutes()
    {
        var security = Security();

        _ = await Assert.That(security.GetProperty("MemberDefaultLockoutTimeInMinutes").GetInt32()).IsEqualTo(15);
        _ = await Assert.That(security.GetProperty("MemberPassword").GetProperty("MaxFailedAccessAttemptsBeforeLockout").GetInt32()).IsEqualTo(5);
    }

    [Test]
    public async Task Passwords_NeedEightCharacters()
    {
        _ = await Assert.That(Security().GetProperty("MemberPassword").GetProperty("RequiredLength").GetInt32()).IsEqualTo(8);
    }

    // With one session per member, signing in on a phone would sign the laptop out within 30 seconds.
    [Test]
    public async Task Members_CanStaySignedInOnSeveralDevices()
    {
        _ = await Assert.That(Security().GetProperty("MemberAllowConcurrentLogins").GetBoolean()).IsTrue();
    }

    private static System.Text.Json.JsonElement Security() =>
        WebAppSettings.Load().GetProperty("Umbraco").GetProperty("CMS").GetProperty("Security");
}
```

`WebAppSettings.Load()` is the helper Phase 1 added beside `ConfigFileWritesTests`. No integration test can watch
concurrent logins cheaply, since a second session only dies at its next stamp check, 30 seconds later. So this guard
is what keeps that setting.

```bash
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj -- --treenode-filter "/*/*/MemberSecuritySettingsTests/*"
```

Expected: 3 failures — `appsettings.json` has no `Security` section yet.

- [ ] **Step 4: Set the member rules**

In `src/KCC.Web/appsettings.json`, add this object inside `Umbraco` → `CMS`, after `ModelsBuilder`:

```json
      "Security": {
        "MemberAllowConcurrentLogins": true,
        "MemberDefaultLockoutTimeInMinutes": 15,
        "MemberPassword": {
          "RequiredLength": 8,
          "MaxFailedAccessAttemptsBeforeLockout": 5
        }
      }
```

Umbraco's own member defaults are a 30-day lockout, 10 characters, and one session per member.

```bash
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj -- --treenode-filter "/*/*/MemberSecuritySettingsTests/*"
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/MemberApprovalTests/*"
```

Expected: 3 and 5 passed.

- [ ] **Step 5: Commit**

```bash
git add src/KCC.Web/appsettings.json tests/KCC.UnitTests/Features/Configuration tests/KCC.IntegrationTests/Config/TestMembers.cs \
  tests/KCC.IntegrationTests/Features/Members
git commit -m "Prove Member Approval and Set the Member Rules"
```

---

### Task 2: Keep concurrent writes from stalling on SQLite

Two of Umbraco's write paths read before they write, without a lock (see "What the scratch probe already proved"):
- the relations update that follows every content, media and member save;
- the member store's update behind sign-in, a failed attempt and sign-out.

On SQLite a transaction that has read cannot become the writer once another connection has committed, so a review
landing between the read and the write leaves the save retrying for about ten minutes. This task adds both guards
and spec §14's SQLite concurrency test, which gates the phase. Task 7 extends the test with the writes it adds.

**Files:**
- Create: `src/KCC.Web/Features/Sqlite/{WriteLockedRelationsUpdate,SqliteComposer,MemberWriteLock}.cs`,
  `tests/KCC.IntegrationTests/Features/Sqlite/{RelationsWriteLockTests,SqliteConcurrencyTests}.cs`

**Interfaces:**
- Consumes: `TestMembers` (Task 1), `TestContent`, `IContributionWrites`, `IContributionReads`, `IContributionStats`,
  `IRecipeIndexRebuilder`, `IRecipeSearchService`.
- Produces (namespace `KCC.Web.Features.Sqlite`):
  - `IMemberWriteLock` with `Task RunAsync(Func<Task> write)` and `Task<T> RunAsync<T>(Func<Task<T>> write)`,
    registered scoped. It runs the write inside a core scope that holds `Constants.Locks.MemberTree`.
  - `WriteLockedRelationsUpdate<TNotification>`, and `SqliteComposer.RelationsHandlerTypeName`.

- [ ] **Step 1: Write the failing tests**

Create `tests/KCC.IntegrationTests/Features/Sqlite/RelationsWriteLockTests.cs`:

```csharp
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Sqlite;

public class RelationsWriteLockTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task EveryRelationsHandler_TakesTheWriteLock()
    {
        _ = await Assert.That(IsWrapped<ContentSavedNotification>()).IsTrue();
        _ = await Assert.That(IsWrapped<ContentPublishedNotification>()).IsTrue();
        _ = await Assert.That(IsWrapped<ContentUnpublishedNotification>()).IsTrue();
        _ = await Assert.That(IsWrapped<MediaSavedNotification>()).IsTrue();
        _ = await Assert.That(IsWrapped<MemberSavedNotification>()).IsTrue();
    }

    [Test]
    public async Task APickedCategory_IsStillRecordedAsARelation()
    {
        var category = await TestContent.CategoryAsync(Site.Services, "IT Pangolin Shelf");
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Pangolin", TestContent.Pick("category", category));

        using var scope = Site.Services.CreateScope();
        var content = scope.ServiceProvider.GetRequiredService<IContentService>();
        var relations = scope.ServiceProvider.GetRequiredService<IRelationService>().GetByParentId(content.GetById(recipe)!.Id) ?? [];

        _ = await Assert.That(relations.Select(relation => relation.ChildId)).Contains(content.GetById(category)!.Id);
    }

    private bool IsWrapped<TNotification>()
        where TNotification : INotification
    {
        using var scope = Site.Services.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<INotificationHandler<TNotification>>().ToList();
        return handlers.OfType<WriteLockedRelationsUpdate<TNotification>>().Count() == 1
            && handlers.All(handler => handler.GetType().FullName != SqliteComposer.RelationsHandlerTypeName);
    }
}
```

Create `tests/KCC.IntegrationTests/Features/Sqlite/SqliteConcurrencyTests.cs`:

```csharp
using System.Collections.Concurrent;
using KCC.Contributions;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Search;
using KCC.Web.Features.Sqlite;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace KCC.IntegrationTests.Features.Sqlite;

// Spec §14: parallel review writes, a content save and an index rebuild, repeated, with no lock errors and consistent
// counts. Members signing up, signing in and being approved race the same writes, because a member save is followed
// by Umbraco's relations update, which is the write that stalled on SQLite.
public class SqliteConcurrencyTests
{
    private const int Rounds = 5;
    private const int ReviewWriters = 4;
    private const int ReviewsPerWriter = 50;
    private const int MemberRepeats = 5;

    // Every write here takes well under a second. A transaction stuck on a stale snapshot retries for minutes, so a
    // round that outlives this limit is a lock failure.
    private static readonly TimeSpan RoundLimit = TimeSpan.FromSeconds(60);

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task ParallelWrites_FinishAndLeaveConsistentCounts()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Axolotl");
        var variant = await TestContent.VariantAsync(Site.Services, recipe, "Stress Test");
        var members = new List<string>();
        for (var index = 0; index < 3; index++)
        {
            var userName = TestMembers.UniqueUserName("race");
            await TestMembers.ApprovedAsync(Site.Services, userName);
            members.Add(userName);
        }

        var reviewers = new ConcurrentBag<Guid>();
        var rebuilder = Site.Services.GetRequiredService<IRecipeIndexRebuilder>();

        for (var round = 0; round < Rounds; round++)
        {
            var edit = round;
            var work = new List<Task>();
            work.AddRange(Enumerable.Range(0, ReviewWriters).Select(_ => Task.Run(() => WriteReviewsAsync(variant, reviewers))));
            work.AddRange(members.Select(userName => Task.Run(() => SignInRepeatedlyAsync(userName))));
            work.Add(Task.Run(() => SignUpAndApproveAsync()));
            work.Add(Task.Run(() => EditAndPublishAsync(recipe, $"Edited in round {edit}.")));
            work.Add(Task.Run(rebuilder.Signal));

            await Task.WhenAll(work).WaitAsync(RoundLimit);
            await rebuilder.WhenCurrentAsync(CancellationToken.None);

            var stored = (await Site.Services.GetRequiredService<IContributionReads>().ReviewsAsync(variant, 0, 1)).Total;
            var cached = (await Site.Services.GetRequiredService<IContributionStats>().GetAsync()).For(variant).ReviewCount;
            var indexed = IndexedReviewCount("axolotl");
            _ = await Assert.That(stored).IsEqualTo(reviewers.Count);
            _ = await Assert.That(cached).IsEqualTo(reviewers.Count);
            _ = await Assert.That(indexed).IsEqualTo(reviewers.Count);
        }
    }

    private async Task WriteReviewsAsync(Guid variant, ConcurrentBag<Guid> reviewers)
    {
        var writes = Site.Services.GetRequiredService<IContributionWrites>();
        for (var index = 0; index < ReviewsPerWriter; index++)
        {
            var reviewer = Guid.NewGuid();
            await writes.UpsertReviewAsync(variant, reviewer, 4.5m, "Held up under load.");
            reviewers.Add(reviewer);
        }
    }

    // The member saves a successful sign-in, a failed attempt and a sign-out make, in the lock the account
    // endpoints take.
    private async Task SignInRepeatedlyAsync(string userName)
    {
        for (var repeat = 0; repeat < MemberRepeats; repeat++)
        {
            using var scope = Site.Services.CreateScope();
            var members = scope.ServiceProvider.GetRequiredService<UserManager<MemberIdentityUser>>();
            await scope.ServiceProvider.GetRequiredService<IMemberWriteLock>().RunAsync(async () =>
            {
                var member = await members.FindByNameAsync(userName) ?? throw new InvalidOperationException($"No member {userName}.");
                member.LastLoginDate = DateTime.UtcNow;
                Succeeded(await members.UpdateAsync(member));
                Succeeded(await members.AccessFailedAsync(member));
                Succeeded(await members.ResetAccessFailedCountAsync(member));
                Succeeded(await members.UpdateSecurityStampAsync(member));
            });
        }
    }

    private async Task SignUpAndApproveAsync()
    {
        var userName = TestMembers.UniqueUserName("joiner");
        Guid key;
        using (var scope = Site.Services.CreateScope())
        {
            var members = scope.ServiceProvider.GetRequiredService<IMemberManager>();
            var member = MemberIdentityUser.CreateNew(userName, $"{userName}@example.test", Constants.Security.DefaultMemberTypeAlias, isApproved: false, userName);
            Succeeded(await scope.ServiceProvider.GetRequiredService<IMemberWriteLock>().RunAsync(() => members.CreateAsync(member, TestMembers.Password)));
            key = member.Key;
        }

        await TestMembers.ApproveAsync(Site.Services, key);
    }

    // The owner's Save and Publish in the backoffice goes through these two services, outside any lock of ours.
    private async Task EditAndPublishAsync(Guid recipe, string description)
    {
        using var scope = Site.Services.CreateScope();
        var updated = await scope.ServiceProvider.GetRequiredService<IContentEditingService>().UpdateAsync(
            recipe,
            new ContentUpdateModel
            {
                Variants = [new VariantModel { Name = "IT Axolotl" }],
                Properties =
                [
                    new PropertyValueModel { Alias = "description", Value = description },
                    new PropertyValueModel { Alias = "icon", Value = "fa-duotone fa-egg" },
                ],
            },
            Constants.Security.SuperUserKey);
        if (updated.Status != ContentEditingOperationStatus.Success)
        {
            throw new InvalidOperationException($"Editing the recipe failed: {updated.Status}.");
        }

        var published = await scope.ServiceProvider.GetRequiredService<IContentPublishingService>()
            .PublishAsync(recipe, [new CulturePublishScheduleModel { Culture = null }], Constants.Security.SuperUserKey);
        if (!published.Success)
        {
            throw new InvalidOperationException($"Publishing the recipe failed: {published.Status}.");
        }
    }

    private int IndexedReviewCount(string word) =>
        Site.Services.GetRequiredService<IRecipeSearchService>().Search(new RecipeSearchCriteria { Query = word }).Results.Single().ReviewCount;

    private static void Succeeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }
}
```

Each round runs, all at once:
- four writers adding fifty reviews each;
- three members making the saves a sign-in, a failed attempt and a sign-out make;
- a newcomer signing up and being approved;
- the owner editing and publishing the recipe;
- an index rebuild.

Then three counts must agree: the database's, the stats cache's and the index's.

```bash
dotnet build KitchenCommandCenter.sln
```

Expected: build errors — `KCC.Web.Features.Sqlite` does not exist.

- [ ] **Step 2: Write the guards**

Create `src/KCC.Web/Features/Sqlite/WriteLockedRelationsUpdate.cs`:

```csharp
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;

namespace KCC.Web.Features.Sqlite;

// Umbraco updates automatic relations in a scope of its own after a content, media or member save has committed. It
// reads the existing relations and then writes, and on SQLite a transaction that reads first cannot become the writer
// once another connection has committed: the write fails on a stale snapshot, and Umbraco's retry policy repeats it
// for about ten minutes. Taking the write lock first makes the update the writer before it reads, and the inner
// handler's scope joins this one.
public sealed class WriteLockedRelationsUpdate<TNotification>(
    INotificationHandler<TNotification> inner,
    ICoreScopeProvider scopeProvider,
    int lockId) : IDistributedCacheNotificationHandler<TNotification>
    where TNotification : INotification
{
    public void Handle(TNotification notification) => Handle([notification]);

    public void Handle(IEnumerable<TNotification> notifications)
    {
        using var scope = scopeProvider.CreateCoreScope();
        scope.WriteLock(lockId);
        inner.Handle(notifications);
        scope.Complete();
    }
}
```

Create `src/KCC.Web/Features/Sqlite/MemberWriteLock.cs`:

```csharp
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Scoping;

namespace KCC.Web.Features.Sqlite;

public interface IMemberWriteLock
{
    Task RunAsync(Func<Task> write);

    Task<T> RunAsync<T>(Func<Task<T>> write);
}

// Signing in, signing out and a failed attempt each save the member, and Umbraco's member store reads the member
// before it saves it, in one transaction. On SQLite that transaction cannot become the writer once another
// connection has committed, so it would retry for minutes behind a review write. Holding the member write lock from
// the start makes it the writer before it reads.
public class MemberWriteLock(ICoreScopeProvider scopeProvider) : IMemberWriteLock
{
    public Task RunAsync(Func<Task> write) => RunAsync(async () =>
    {
        await write();
        return true;
    });

    public async Task<T> RunAsync<T>(Func<Task<T>> write)
    {
        using var scope = scopeProvider.CreateCoreScope();
        scope.WriteLock(Constants.Locks.MemberTree);
        var result = await write();
        scope.Complete();
        return result;
    }
}
```

Create `src/KCC.Web/Features/Sqlite/SqliteComposer.cs`:

```csharp
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;

namespace KCC.Web.Features.Sqlite;

public class SqliteComposer : IComposer
{
    // Umbraco's handler is internal, so it can only be found by name.
    internal const string RelationsHandlerTypeName = "Umbraco.Cms.Infrastructure.Persistence.Relations.ContentRelationsUpdate";

    public void Compose(IUmbracoBuilder builder)
    {
        LockRelationsUpdate<ContentSavedNotification>(builder.Services, Constants.Locks.ContentTree);
        LockRelationsUpdate<ContentPublishedNotification>(builder.Services, Constants.Locks.ContentTree);
        LockRelationsUpdate<ContentUnpublishedNotification>(builder.Services, Constants.Locks.ContentTree);
        LockRelationsUpdate<MediaSavedNotification>(builder.Services, Constants.Locks.MediaTree);
        LockRelationsUpdate<MemberSavedNotification>(builder.Services, Constants.Locks.MemberTree);
        builder.Services.AddScoped<IMemberWriteLock, MemberWriteLock>();
    }

    private static void LockRelationsUpdate<TNotification>(IServiceCollection services, int lockId)
        where TNotification : INotification
    {
        // Without the lock the site hangs for minutes under concurrent writes, so a handler that has moved in an
        // Umbraco upgrade stops the boot rather than going unwrapped.
        var descriptor = services.SingleOrDefault(candidate =>
                candidate.ServiceType == typeof(INotificationHandler<TNotification>)
                && candidate.ImplementationType?.FullName == RelationsHandlerTypeName)
            ?? throw new InvalidOperationException($"Umbraco no longer registers {RelationsHandlerTypeName} for {typeof(TNotification).Name}.");

        var implementation = descriptor.ImplementationType;
        services[services.IndexOf(descriptor)] = ServiceDescriptor.Transient<INotificationHandler<TNotification>>(provider =>
            new WriteLockedRelationsUpdate<TNotification>(
                (INotificationHandler<TNotification>)ActivatorUtilities.CreateInstance(provider, implementation),
                provider.GetRequiredService<ICoreScopeProvider>(),
                lockId));
    }
}
```

The decorator implements `IDistributedCacheNotificationHandler<T>`, as Umbraco's handler does. A scope can carry a
publisher that reaches only handlers of that interface, as Umbraco Deploy's restores do, and the filter checks the
resolved instance. Replacing the descriptor in place keeps the handler's position among the others. The lock ids follow what each save itself takes: SQLite has one database-wide
write lock, so any id would do there, and matching them keeps the decorator sensible on SQL Server, the exit path.

- [ ] **Step 3: Run the tests**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/RelationsWriteLockTests/*"
for run in 1 2 3; do
  dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/SqliteConcurrencyTests/*"
done
```

Expected: 0 warnings; 2 passed; then the concurrency test passes three times, each round in a few seconds. It
repeats because a race that fails only sometimes is still a failure.

- [ ] **Step 4: Commit**

```bash
git add src/KCC.Web/Features/Sqlite tests/KCC.IntegrationTests/Features/Sqlite
git commit -m "Keep Concurrent Writes from Stalling on SQLite"
```

- [ ] **Step 5: Watch it fail without the guards**

This proves the test can see the hazard. Take both guards out: in `SqliteComposer.Compose`, comment out the five
`LockRelationsUpdate<…>(…)` calls, and in `MemberWriteLock.RunAsync<T>` delete the
`scope.WriteLock(Constants.Locks.MemberTree);` line. Then build and run the concurrency test once:

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/SqliteConcurrencyTests/*"
```

Expected: it fails with `TimeoutException` about a minute in, when a round outlives its 60-second limit. Put the
guards back and see it pass again:

```bash
git checkout -- src/KCC.Web/Features/Sqlite
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/SqliteConcurrencyTests/*"
git status --short src/KCC.Web/Features/Sqlite
```

Expected: 1 passed, and `git status` shows nothing.

---

### Task 3: Sign-up, sign-in and sign-out

The account API comes back on Umbraco's member identity. A new member is created unapproved and sent to the
registration-complete page. Sign-in is by username, with lockout on, and an unapproved or locked-out member is told
which. Sign-out becomes `POST /account/logout` (spec §6.3). The same task brings in the two protections every later
write endpoint uses:
- anti-forgery validation on each controller;
- per-client rate limits, from a pipeline filter that runs after routing.

**Files:**
- Create: `src/KCC.Web/Features/Security/{RateLimits,SecurityComposer}.cs`,
  `src/KCC.Web/Features/Pages/Account/AccountPageQueries.cs`, `tests/KCC.IntegrationTests/Config/MemberClient.cs`,
  `tests/KCC.IntegrationTests/Features/Api/AccountApiTests.cs`
- Rewrite: `src/KCC.Web/Features/Api/AccountApiController.cs`,
  `src/KCC.Web/Features/Pages/Account/Logout/LogoutController.cs`
- Delete: `src/KCC.Web/Features/Models/Common/KCCApplicationUser.cs`
- Modify: `src/KCC.Web/KCC.Web.csproj`, `src/KCC.Web/Program.cs`

**Interfaces:**
- Consumes: `IMemberWriteLock` (Task 2), `TestMembers` (Task 1), `IResourceStringProvider`, `RenderedPage`.
- Produces:
  - `KCC.Web.Features.Security.RateLimits`: the policy names `Account`, `Contributions` and `Submissions`, and
    `ClientKey(HttpContext) → string`. `RateLimitOptions` is bound from `RateLimits`, with `AccountPerMinute`,
    `ContributionsPerMinute` and `SubmissionsPerHour`.
  - `KCC.Web.Features.Pages.Account.IAccountPageQueries.GetUrls() → AccountUrls(string Account, string Login, string Settings, string RegistrationComplete)`,
    registered scoped.
  - `POST /api/account/login` with `LoginRequest(string UserName, string Password, bool RememberMe, string ReturnUrl)`,
    and `POST /api/account/register` with `RegisterRequest(string UserName, string Email, string Password)`. Both
    answer `AuthResponse(bool Success, string[] Errors, string RedirectUrl)`.
  - `POST /account/logout?returnUrl=…`, which answers 302 to the local return URL or `/`. The action is
    `LogoutController.Index(string returnUrl)`.
  - For the tests, `KCC.IntegrationTests.Config.MemberClient`, one visitor's browser:
    - `Http`, `PostAsync`, `PutAsync` and `DeleteAsync`, which send the anti-forgery header;
    - `SignInAsync(userName, password, returnUrl) → AuthResult`, `SignOutAsync()`, and `IsSignedInAsync(variantPath)`.

- [ ] **Step 1: Write the failing tests**

Create `tests/KCC.IntegrationTests/Config/MemberClient.cs`:

```csharp
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KCC.IntegrationTests.Config;

// One visitor's browser. It keeps its cookies, sends the anti-forgery token the layout hands out, and comes from its
// own address, so no test spends another test's rate limit.
public sealed partial class MemberClient : IDisposable
{
    // Any page rendered through the layout hands out a token, and this one renders signed in or out.
    private const string TokenPage = "/recipes/";

    private static int nextAddress;

    private readonly HttpClient client;
    private string token = string.Empty;

    public MemberClient(UmbracoSite site)
    {
        client = site.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("CF-Connecting-IP", $"2001:db8::{Interlocked.Increment(ref nextAddress):x}");
    }

    public HttpClient Http => client;

    public Task<HttpResponseMessage> PostAsync(string path, object? body = null) => SendAsync(HttpMethod.Post, path, body);

    public Task<HttpResponseMessage> PutAsync(string path, object? body = null) => SendAsync(HttpMethod.Put, path, body);

    public Task<HttpResponseMessage> DeleteAsync(string path) => SendAsync(HttpMethod.Delete, path, null);

    public async Task<AuthResult> SignInAsync(string userName, string password, string? returnUrl = null)
    {
        using var response = await PostAsync("/api/account/login", new { userName, password, rememberMe = false, returnUrl });
        var result = await response.Content.ReadFromJsonAsync<AuthResult>() ?? throw new InvalidOperationException("Sign-in answered no body.");

        // The token belongs to the visitor it was issued to, so a new identity needs a new one.
        token = string.Empty;
        return result;
    }

    public async Task<HttpResponseMessage> SignOutAsync()
    {
        await EnsureTokenAsync();
        using var form = new FormUrlEncodedContent([new("__RequestVerificationToken", token)]);
        var response = await client.PostAsync("/account/logout", form);
        token = string.Empty;
        return response;
    }

    public async Task<bool> IsSignedInAsync(string variantPath)
    {
        var page = await RenderedPage.GetAsync(client, variantPath);
        return page.Attribute(":is-authenticated") == "true";
    }

    public void Dispose() => client.Dispose();

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body)
    {
        await EnsureTokenAsync();
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("RequestVerificationToken", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }

    private async Task EnsureTokenAsync()
    {
        if (token.Length > 0)
        {
            return;
        }

        var html = await client.GetStringAsync(TokenPage);
        var config = ApiConfig().Match(html);
        if (!config.Success)
        {
            throw new InvalidOperationException($"{TokenPage} carries no api-config block.");
        }

        using var json = JsonDocument.Parse(config.Groups[1].Value);
        token = json.RootElement.GetProperty("antiforgeryToken").GetString() ?? string.Empty;
    }

    [GeneratedRegex("<script type=\"application/json\" id=\"api-config\">(.*?)</script>", RegexOptions.Singleline)]
    private static partial Regex ApiConfig();
}

public sealed record AuthResult(bool Success, string[]? Errors, string? RedirectUrl);
```

Create `tests/KCC.IntegrationTests/Features/Api/AccountApiTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Dictionary;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Api;

public class AccountApiTests
{
    // A seeded variant: the page tells a signed-in visitor apart from a signed-out one.
    private const string VariantPath = "/recipes/fluffy-buttermilk-pancakes/classic-stack/";

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task SignUp_CreatesAnUnapprovedMember_AndPointsAtRegistrationComplete()
    {
        using var visitor = new MemberClient(Site);
        var userName = TestMembers.UniqueUserName("joiner");

        var result = await SignUpAsync(visitor, userName);

        _ = await Assert.That(result.Success).IsTrue();
        _ = await Assert.That(result.RedirectUrl).IsEqualTo("/account/registration-complete/");
        var member = Site.Services.GetRequiredService<IMemberService>().GetByUsername(userName);
        _ = await Assert.That(member!.IsApproved).IsFalse();
        _ = await Assert.That(member.Email).IsEqualTo($"{userName}@example.test");
    }

    [Test]
    public async Task SignUp_ATakenUserName_Fails()
    {
        using var visitor = new MemberClient(Site);
        var userName = TestMembers.UniqueUserName("twice");
        _ = await SignUpAsync(visitor, userName);

        var again = await SignUpAsync(visitor, userName, $"other-{userName}@example.test");

        _ = await Assert.That(again.Success).IsFalse();
        _ = await Assert.That(again.Errors!.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task SignIn_Unapproved_SaysTheAccountIsWaitingForApproval()
    {
        using var visitor = new MemberClient(Site);
        var userName = TestMembers.UniqueUserName("waiting");
        _ = await SignUpAsync(visitor, userName);

        var result = await visitor.SignInAsync(userName, TestMembers.Password);

        _ = await Assert.That(result.Success).IsFalse();
        _ = await Assert.That(result.Errors!.Single()).IsEqualTo(String("Login.NotAllowedError"));
        _ = await Assert.That(await visitor.IsSignedInAsync(VariantPath)).IsFalse();
    }

    [Test]
    public async Task SignIn_Approved_SignsInAndReturnsToALocalUrl()
    {
        using var visitor = new MemberClient(Site);
        var userName = TestMembers.UniqueUserName("member");
        await TestMembers.ApprovedAsync(Site.Services, userName);

        var result = await visitor.SignInAsync(userName, TestMembers.Password, "/recipes/");

        _ = await Assert.That(result.Success).IsTrue();
        _ = await Assert.That(result.RedirectUrl).IsEqualTo("/recipes/");
        _ = await Assert.That(await visitor.IsSignedInAsync(VariantPath)).IsTrue();
    }

    [Test]
    public async Task SignIn_ToAForeignUrl_ReturnsHomeInstead()
    {
        using var visitor = new MemberClient(Site);
        var userName = TestMembers.UniqueUserName("member");
        await TestMembers.ApprovedAsync(Site.Services, userName);

        var result = await visitor.SignInAsync(userName, TestMembers.Password, "https://evil.example/recipes/");

        _ = await Assert.That(result.RedirectUrl).IsEqualTo("/");
    }

    [Test]
    public async Task SignIn_AfterFiveWrongPasswords_IsLockedOut()
    {
        using var visitor = new MemberClient(Site);
        var userName = TestMembers.UniqueUserName("guesser");
        await TestMembers.ApprovedAsync(Site.Services, userName);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            _ = await visitor.SignInAsync(userName, "not-the-password");
        }

        var result = await visitor.SignInAsync(userName, TestMembers.Password);

        _ = await Assert.That(result.Errors!.Single()).IsEqualTo(String("Login.LockedOutError"));
        _ = await Assert.That(await visitor.IsSignedInAsync(VariantPath)).IsFalse();
    }

    [Test]
    public async Task SignIn_WithAWrongPassword_SaysTheCredentialsAreWrong()
    {
        using var visitor = new MemberClient(Site);
        var userName = TestMembers.UniqueUserName("typo");
        await TestMembers.ApprovedAsync(Site.Services, userName);

        var result = await visitor.SignInAsync(userName, "not-the-password");

        _ = await Assert.That(result.Errors!.Single()).IsEqualTo(String("Login.InvalidCredentialsError"));
    }

    [Test]
    public async Task APost_WithoutTheAntiforgeryToken_IsRejected()
    {
        using var visitor = new MemberClient(Site);

        using var response = await visitor.Http.PostAsJsonAsync("/api/account/login", new { userName = "anyone", password = "anything" });

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task SignOut_OverGet_DoesNothing()
    {
        using var visitor = new MemberClient(Site);
        var userName = TestMembers.UniqueUserName("stays");
        await TestMembers.ApprovedAsync(Site.Services, userName);
        _ = await visitor.SignInAsync(userName, TestMembers.Password);

        using var response = await visitor.Http.GetAsync("/account/logout");

        _ = await Assert.That((int)response.StatusCode).IsGreaterThanOrEqualTo(400);
        _ = await Assert.That(await visitor.IsSignedInAsync(VariantPath)).IsTrue();
    }

    [Test]
    public async Task SignOut_OverPost_EndsTheSessionAndGoesHome()
    {
        using var visitor = new MemberClient(Site);
        var userName = TestMembers.UniqueUserName("leaver");
        await TestMembers.ApprovedAsync(Site.Services, userName);
        _ = await visitor.SignInAsync(userName, TestMembers.Password);

        using var response = await visitor.SignOutAsync();

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        _ = await Assert.That(response.Headers.Location!.OriginalString).IsEqualTo("/");
        _ = await Assert.That(await visitor.IsSignedInAsync(VariantPath)).IsFalse();
    }

    [Test]
    public async Task AccountEndpoints_AllowTenAMinutePerClient()
    {
        using var visitor = new MemberClient(Site);
        for (var attempt = 0; attempt < 10; attempt++)
        {
            using var allowed = await visitor.PostAsync("/api/account/login", new { userName = "nobody-at-all", password = "wrong-password" });
            _ = await Assert.That(allowed.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }

        using var limited = await visitor.PostAsync("/api/account/register", new { userName = "nobody-at-all", email = "nobody@example.test", password = "wrong-password" });
        using var neighbour = new MemberClient(Site);
        using var elsewhere = await neighbour.PostAsync("/api/account/login", new { userName = "nobody-at-all", password = "wrong-password" });

        _ = await Assert.That(limited.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
        _ = await Assert.That(elsewhere.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    private static async Task<AuthResult> SignUpAsync(MemberClient visitor, string userName, string? email = null)
    {
        using var response = await visitor.PostAsync("/api/account/register", new { userName, email = email ?? $"{userName}@example.test", password = TestMembers.Password });
        return await response.Content.ReadFromJsonAsync<AuthResult>() ?? throw new InvalidOperationException("Sign-up answered no body.");
    }

    private string String(string key)
    {
        using var scope = Site.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IResourceStringProvider>().GetOrDefault(key);
    }
}
```

The tests read expected messages through `IResourceStringProvider`, so they hold whatever the dictionary says.
Task 6 changes the wording.

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/AccountApiTests/*"
```

Expected: every test fails. `/api/account/*` answers 404, because the Kentico controller is still excluded.

- [ ] **Step 2: Write the rate limits**

Create `src/KCC.Web/Features/Security/RateLimits.cs`:

```csharp
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace KCC.Web.Features.Security;

public static class RateLimits
{
    public const string Account = "account";
    public const string Contributions = "contributions";
    public const string Submissions = "submissions";

    // Every request arrives through the Cloudflare tunnel, so every socket address is cloudflared's and the visitor's
    // own address is in this header. The tunnel being the only way in is what makes the header trustworthy. Without
    // it, in development and tests, the socket address stands in.
    public static string ClientKey(HttpContext context) =>
        context.Request.Headers["CF-Connecting-IP"].FirstOrDefault() is { Length: > 0 } address
            ? address
            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    internal static RateLimitPartition<string> PerClient(HttpContext context, Func<RateLimitOptions, int> permits, TimeSpan window)
    {
        var limits = context.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;
        return RateLimitPartition.GetFixedWindowLimiter(
            ClientKey(context),
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits(limits), Window = window, QueueLimit = 0 });
    }
}

public class RateLimitOptions
{
    public int AccountPerMinute { get; set; } = 10;

    public int ContributionsPerMinute { get; set; } = 30;

    public int SubmissionsPerHour { get; set; } = 5;
}
```

Create `src/KCC.Web/Features/Security/SecurityComposer.cs`:

```csharp
using Microsoft.AspNetCore.RateLimiting;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace KCC.Web.Features.Security;

public class SecurityComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<RateLimitOptions>(builder.Config.GetSection("RateLimits"));
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, cancellationToken) => new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(
                new { error = "Too many attempts. Wait a minute and try again." },
                cancellationToken));
            options.AddPolicy(RateLimits.Account, context => RateLimits.PerClient(context, limits => limits.AccountPerMinute, TimeSpan.FromMinutes(1)));
            options.AddPolicy(RateLimits.Contributions, context => RateLimits.PerClient(context, limits => limits.ContributionsPerMinute, TimeSpan.FromMinutes(1)));
            options.AddPolicy(RateLimits.Submissions, context => RateLimits.PerClient(context, limits => limits.SubmissionsPerHour, TimeSpan.FromHours(1)));
        });

        // After routing, so the limiter can read which policy the endpoint asks for.
        builder.Services.Configure<UmbracoPipelineOptions>(options => options.AddFilter(new UmbracoPipelineFilter("KCC rate limits")
        {
            PostRouting = app => app.UseRateLimiter(),
        }));
    }
}
```

A limiter placed before routing cannot see an endpoint's `[EnableRateLimiting]`, and Umbraco owns the pipeline, so
the limiter goes in through its `PostRouting` hook.

- [ ] **Step 3: Write the account pages query and the two controllers**

Create `src/KCC.Web/Features/Pages/Account/AccountPageQueries.cs`:

```csharp
using KCC.Web.Features.Models.Generated;
using Umbraco.Cms.Core;
using Umbraco.Extensions;

namespace KCC.Web.Features.Pages.Account;

public sealed record AccountUrls(string Account, string Login, string Settings, string RegistrationComplete);

public interface IAccountPageQueries
{
    AccountUrls GetUrls();
}

public class AccountPageQueries(IPublishedContentQuery contentQuery) : IAccountPageQueries
{
    public AccountUrls GetUrls()
    {
        var account = contentQuery.ContentAtRoot().OfType<HomePage>().FirstOrDefault()?.Children<AccountPage>().FirstOrDefault();
        return new AccountUrls(
            account?.Url(),
            account?.Children<LoginPage>().FirstOrDefault()?.Url(),
            account?.Children<AccountSettingsPage>().FirstOrDefault()?.Url(),
            account?.Children<RegistrationCompletePage>().FirstOrDefault()?.Url());
    }
}
```

Replace `src/KCC.Web/Features/Api/AccountApiController.cs` with:

```csharp
using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Pages.Account;
using KCC.Web.Features.Security;
using KCC.Web.Features.Sqlite;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.Common.Security;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api/account")]
[AutoValidateAntiforgeryToken]
[EnableRateLimiting(RateLimits.Account)]
public class AccountApiController(
    IMemberSignInManager signInManager,
    IMemberManager memberManager,
    IMemberWriteLock memberWriteLock,
    IAccountPageQueries accountPages,
    IResourceStringProvider resourceStrings) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.UserName) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new AuthResponse(false, ["Username and password are required."], null));
        }

        var result = await memberWriteLock.RunAsync(() =>
            signInManager.PasswordSignInAsync(request.UserName, request.Password, request.RememberMe, lockoutOnFailure: true));
        if (result.Succeeded)
        {
            return Ok(new AuthResponse(true, null, Url.IsLocalUrl(request.ReturnUrl) ? request.ReturnUrl : "/"));
        }

        var error = result switch
        {
            { IsLockedOut: true } => resourceStrings.GetOrDefault("Login.LockedOutError"),
            { IsNotAllowed: true } => resourceStrings.GetOrDefault("Login.NotAllowedError"),
            _ => resourceStrings.GetOrDefault("Login.InvalidCredentialsError"),
        };
        return Ok(new AuthResponse(false, [error], null));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.UserName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new AuthResponse(false, ["Username, email and password are required."], null));
        }

        // Every new member waits for the owner's approval before they can sign in.
        var userName = request.UserName.Trim();
        var member = MemberIdentityUser.CreateNew(userName, request.Email.Trim(), Constants.Security.DefaultMemberTypeAlias, isApproved: false, userName);
        var result = await memberWriteLock.RunAsync(() => memberManager.CreateAsync(member, request.Password));

        return result.Succeeded
            ? Ok(new AuthResponse(true, null, accountPages.GetUrls().RegistrationComplete))
            : Ok(new AuthResponse(false, [.. result.Errors.Select(error => error.Description)], null));
    }
}

public sealed record LoginRequest(string UserName, string Password, bool RememberMe, string ReturnUrl);

public sealed record RegisterRequest(string UserName, string Email, string Password);

public sealed record AuthResponse(bool Success, string[] Errors, string RedirectUrl);
```

Replace `src/KCC.Web/Features/Pages/Account/Logout/LogoutController.cs` with:

```csharp
using KCC.Web.Features.Sqlite;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Security;

namespace KCC.Web.Features.Pages.Account.Logout;

[Route("account/logout")]
[AutoValidateAntiforgeryToken]
public class LogoutController(IMemberSignInManager signInManager, IMemberWriteLock memberWriteLock) : Controller
{
    [HttpPost]
    public async Task<IActionResult> Index(string returnUrl)
    {
        await memberWriteLock.RunAsync(signInManager.SignOutAsync);
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }
}
```

A few things differ from the Kentico versions. There is no `RequiresTwoFactor` branch, because members have no
second factor. The sign-in and sign-up calls run inside `IMemberWriteLock`, and both endpoints validate the
anti-forgery token: login CSRF would otherwise sign a visitor into someone else's account. The `api/account/logout`
action is gone. Nothing called it, and signing out is now the form post.

In `src/KCC.Web/Program.cs`, add `using KCC.Web.Features.Pages.Account;` (sorted) and, after the `IRecipeQueries`
registration:

```csharp
builder.Services.AddScoped<IAccountPageQueries, AccountPageQueries>();
```

In the `Unported slices` group of `src/KCC.Web/KCC.Web.csproj`:
1. Delete `<Compile Remove="Features/Api/AccountApiController.cs" />` and
   `<Compile Remove="Features/Models/Common/KCCApplicationUser.cs" />`.
2. Replace `<Compile Remove="Features/Pages/Account/**" />` with the files Task 4 rewrites:

```xml
        <Compile Remove="Features/Pages/Account/AccountController.cs" />
        <Compile Remove="Features/Pages/Account/AccountViewModel.cs" />
        <Compile Remove="Features/Pages/Account/Login/**" />
        <Compile Remove="Features/Pages/Account/RegistrationComplete/**" />
        <Compile Remove="Features/Pages/Account/Settings/**" />
```

Then delete the Kentico identity user, which nothing uses any more:

```bash
git rm -q src/KCC.Web/Features/Models/Common/KCCApplicationUser.cs
```

- [ ] **Step 4: Run the tests**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/AccountApiTests/*"
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
```

Expected: 0 warnings; 11 passed; every unit test passes. The Kentico `ProfileApiController`, `RecipeApiController`
and `VariantCookedApiController` still reference `KCCApplicationUser`, and they stay excluded until Tasks 5, 9 and 7.

- [ ] **Step 5: Commit**

```bash
git add -A src/KCC.Web tests/KCC.IntegrationTests
git commit -m "Sign Members Up, In and Out on Umbraco"
```

---

### Task 4: The account pages

Four hijacked pages come back.
- **The account page** lists the member's recipes and variants, with "Pending review" on anything not yet
  published. This is spec §6.4's one read of saved, unpublished content: published nodes come from the published
  cache, and only drafts are loaded from the content service.
- **The login page** passes on only a local return URL, and sends a member who is already signed in straight there.
- **The settings page** shows the member's names and email.
- **The registration-complete page** renders its text from the dictionary and links to sign-in.

The account and settings pages send a signed-out visitor to sign in and back. Keys replace Kentico's page ids in
the account page's groups, on both sides.

**Files:**
- Move and rewrite: in `src/KCC.Web/Features/Pages/Account/`, `AccountController.cs` → `AccountPageController.cs`,
  `Login/LoginController.cs` → `Login/LoginPageController.cs`, `Settings/AccountSettingsController.cs` →
  `Settings/AccountSettingsPageController.cs`, `RegistrationComplete/RegistrationCompleteController.cs` →
  `RegistrationComplete/RegistrationCompletePageController.cs`
- Create: `src/KCC.Web/Features/Pages/Account/{AuthoredRecipeQueries,SignInRedirect}.cs`,
  `src/KCC.Web/Features/Pages/Account/RegistrationComplete/RegistrationCompleteViewModel.cs`,
  `tests/KCC.IntegrationTests/Features/Pages/AccountPagesTests.cs`
- Rewrite: `src/KCC.Web/Features/Pages/Account/AccountViewModel.cs`,
  `src/KCC.Web/Features/Pages/Account/{Index.cshtml,Settings/Index.cshtml,RegistrationComplete/Index.cshtml}`,
  `tests/KCC.UnitTests/Features/Pages/Account/AccountViewModelTests.cs`
- Modify: `tests/KCC.IntegrationTests/Config/TestContent.cs`,
  `src/KCC.Web/Features/Pages/Account/AccountView.Component.vue`,
  `tests/KCC.ViteTests/Features/Pages/Account/AccountView.test.ts`, `src/KCC.Web/KCC.Web.csproj`,
  `tests/KCC.UnitTests/KCC.UnitTests.csproj`, `src/KCC.Web/Program.cs`

**Interfaces:**
- Consumes: `IAccountPageQueries` and `LogoutController` (Task 3), `IMemberManager`, `IMemberService`,
  `PageMetadata.Apply`, `AuthorNameProvider.FormatDisplayName`, `UrlHelperExtensions.ActionFor`.
- Produces:
  - `AccountViewModel.BuildRecipeGroups(IReadOnlyCollection<AuthoredRecipeInput>, IReadOnlyCollection<AuthoredVariantInput>, IReadOnlySet<Guid>, IReadOnlySet<Guid>)`,
    where:
    - `AuthoredRecipeInput(Guid Key, string Name, string Icon, string Url, bool StartedByMe)`;
    - `AuthoredVariantInput(Guid Key, Guid ParentKey, string Name, string Icon, string Url)`;
    - the result's groups and variants carry `Key`.
  - `IAuthoredRecipeQueries.GetAuthoredBy(Guid memberKey) → AuthoredRecipes(Recipes, Variants, PublishedRecipeKeys, PublishedVariantKeys)`,
    registered scoped.
  - `SignInRedirect.To(string loginUrl, HttpRequest request) → IActionResult`, which answers
    `302 <login>?returnUrl=<escaped path and query>`, or 404 when there is no login page.
  - For the tests, `TestContent.DraftRecipeAsync(IServiceProvider, string name, Guid authorKey)` and
    `TestContent.DraftVariantAsync(IServiceProvider, Guid recipeKey, string name, Guid authorKey)`.

- [ ] **Step 1: Port the view model's tests**

In `tests/KCC.UnitTests/KCC.UnitTests.csproj`, delete `<Compile Remove="Features/Pages/Account/**" />`. Replace
`tests/KCC.UnitTests/Features/Pages/Account/AccountViewModelTests.cs` with:

```csharp
using KCC.Web.Features.Pages.Account;

namespace KCC.UnitTests.Features.Pages.Account;

public class AccountViewModelTests
{
    private static readonly Guid MacAndCheeseKey = Guid.NewGuid();
    private static readonly Guid TacosKey = Guid.NewGuid();
    private static readonly Guid ClassicKey = Guid.NewGuid();
    private static readonly Guid CarnitasKey = Guid.NewGuid();
    private static readonly Guid SpicyKey = Guid.NewGuid();

    private static readonly AccountViewModel.AuthoredRecipeInput MacAndCheese =
        new(Key: MacAndCheeseKey, Name: "Mac & Cheese", Icon: "fa-pot", Url: "/recipes/mac-and-cheese", StartedByMe: true);

    private static readonly AccountViewModel.AuthoredRecipeInput Tacos =
        new(Key: TacosKey, Name: "Tacos", Icon: "fa-taco", Url: "/recipes/tacos", StartedByMe: false);

    [Test]
    public async Task BuildRecipeGroups_GroupsVariantsUnderTheirRecipe()
    {
        var variants = new[]
        {
            new AccountViewModel.AuthoredVariantInput(Key: ClassicKey, ParentKey: MacAndCheeseKey, Name: "Classic", Icon: "fa-pot", Url: "/recipes/mac-and-cheese/classic"),
            new AccountViewModel.AuthoredVariantInput(Key: CarnitasKey, ParentKey: TacosKey, Name: "Carnitas", Icon: "fa-taco", Url: "/recipes/tacos/carnitas"),
        };

        var groups = AccountViewModel.BuildRecipeGroups(
            [MacAndCheese, Tacos],
            variants,
            publishedRecipeKeys: new HashSet<Guid> { MacAndCheeseKey, TacosKey },
            publishedVariantKeys: new HashSet<Guid> { ClassicKey, CarnitasKey });

        _ = await Assert.That(groups.Count()).IsEqualTo(2);
        _ = await Assert.That(groups.ElementAt(0).RecipeName).IsEqualTo("Mac & Cheese");
        _ = await Assert.That(groups.ElementAt(0).StartedByYou).IsTrue();
        _ = await Assert.That(groups.ElementAt(0).Variants.Count()).IsEqualTo(1);
        _ = await Assert.That(groups.ElementAt(0).Variants.ElementAt(0).Name).IsEqualTo("Classic");
        _ = await Assert.That(groups.ElementAt(1).StartedByYou).IsFalse();
    }

    [Test]
    public async Task BuildRecipeGroups_MarksUnpublishedItemsPendingWithoutUrls()
    {
        var variants = new[]
        {
            new AccountViewModel.AuthoredVariantInput(Key: ClassicKey, ParentKey: MacAndCheeseKey, Name: "Classic", Icon: "fa-pot", Url: "/recipes/mac-and-cheese/classic"),
        };

        var groups = AccountViewModel.BuildRecipeGroups([MacAndCheese], variants, new HashSet<Guid>(), new HashSet<Guid>());

        _ = await Assert.That(groups.ElementAt(0).IsPending).IsTrue();
        _ = await Assert.That(groups.ElementAt(0).RecipeUrl).IsNull();
        _ = await Assert.That(groups.ElementAt(0).Variants.ElementAt(0).IsPending).IsTrue();
        _ = await Assert.That(groups.ElementAt(0).Variants.ElementAt(0).Url).IsNull();
    }

    [Test]
    public async Task BuildRecipeGroups_PublishedItemsKeepUrlsAndAreNotPending()
    {
        var variants = new[]
        {
            new AccountViewModel.AuthoredVariantInput(Key: ClassicKey, ParentKey: MacAndCheeseKey, Name: "Classic", Icon: "fa-pot", Url: "/recipes/mac-and-cheese/classic"),
        };

        var groups = AccountViewModel.BuildRecipeGroups(
            [MacAndCheese],
            variants,
            publishedRecipeKeys: new HashSet<Guid> { MacAndCheeseKey },
            publishedVariantKeys: new HashSet<Guid> { ClassicKey });

        _ = await Assert.That(groups.ElementAt(0).IsPending).IsFalse();
        _ = await Assert.That(groups.ElementAt(0).RecipeUrl).IsEqualTo("/recipes/mac-and-cheese");
        _ = await Assert.That(groups.ElementAt(0).Variants.ElementAt(0).IsPending).IsFalse();
        _ = await Assert.That(groups.ElementAt(0).Variants.ElementAt(0).Url).IsEqualTo("/recipes/mac-and-cheese/classic");
    }

    [Test]
    public async Task BuildRecipeGroups_KeepsStartedRecipesWithNoVariantsOfMine()
    {
        var groups = AccountViewModel.BuildRecipeGroups([MacAndCheese], [], new HashSet<Guid> { MacAndCheeseKey }, new HashSet<Guid>());

        _ = await Assert.That(groups.Count()).IsEqualTo(1);
        _ = await Assert.That(groups.ElementAt(0).Variants.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task BuildRecipeGroups_DropsForeignRecipesWithNoVariantsAndSkipsOrphanVariants()
    {
        var orphan = new AccountViewModel.AuthoredVariantInput(Key: Guid.NewGuid(), ParentKey: Guid.NewGuid(), Name: "Orphan", Icon: "fa-x", Url: "/nowhere");

        var groups = AccountViewModel.BuildRecipeGroups([Tacos], [orphan], new HashSet<Guid> { TacosKey }, new HashSet<Guid> { orphan.Key });

        _ = await Assert.That(groups.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task BuildRecipeGroups_OrdersRecipesAndVariantsByName()
    {
        var variants = new[]
        {
            new AccountViewModel.AuthoredVariantInput(Key: SpicyKey, ParentKey: MacAndCheeseKey, Name: "Spicy", Icon: "fa-pot", Url: "/b"),
            new AccountViewModel.AuthoredVariantInput(Key: ClassicKey, ParentKey: MacAndCheeseKey, Name: "Classic", Icon: "fa-pot", Url: "/a"),
        };

        var groups = AccountViewModel.BuildRecipeGroups(
            [Tacos with { StartedByMe = true }, MacAndCheese],
            variants,
            publishedRecipeKeys: new HashSet<Guid> { MacAndCheeseKey, TacosKey },
            publishedVariantKeys: new HashSet<Guid> { ClassicKey, SpicyKey });

        _ = await Assert.That(groups.Select(group => group.RecipeName)).IsEquivalentTo(new[] { "Mac & Cheese", "Tacos" });
        _ = await Assert.That(groups.ElementAt(0).Variants.Select(variant => variant.Name)).IsEquivalentTo(new[] { "Classic", "Spicy" });
    }

    [Test]
    public async Task BuildRecipeGroups_ReturnsEmptyForNoInputs()
    {
        var groups = AccountViewModel.BuildRecipeGroups([], [], new HashSet<Guid>(), new HashSet<Guid>());

        _ = await Assert.That(groups.Count()).IsEqualTo(0);
    }

    [Test]
    [Arguments("Alex", "Carter", "AC")]
    [Arguments("alex", "carter", "AC")]
    [Arguments("Alex", "", "A")]
    [Arguments("  Alex  ", "  Carter  ", "AC")]
    public async Task ComputeInitials_UsesNames(string first, string last, string expected)
    {
        _ = await Assert.That(AccountViewModel.ComputeInitials(first, last, "ignored")).IsEqualTo(expected);
    }

    [Test]
    [Arguments("", "", "alex.carter@example.com", "AL")]
    [Arguments(null, null, "a", "A")]
    [Arguments("", "", "", "")]
    public async Task ComputeInitials_FallsBackWhenNamesEmpty(string first, string last, string fallback, string expected)
    {
        _ = await Assert.That(AccountViewModel.ComputeInitials(first, last, fallback)).IsEqualTo(expected);
    }

    [Test]
    public async Task FormatMemberSince_FormatsMonthAndYear()
    {
        _ = await Assert.That(AccountViewModel.FormatMemberSince(new DateTime(2024, 6, 15))).IsEqualTo("June 2024");
    }

    [Test]
    public async Task FormatMemberSince_ReturnsEmptyForNull()
    {
        _ = await Assert.That(AccountViewModel.FormatMemberSince(null)).IsEqualTo(string.Empty);
    }
}
```

```bash
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj -- --treenode-filter "/*/*/AccountViewModelTests/*"
```

Expected: build error — `AccountViewModel` does not compile yet (it is still excluded).

- [ ] **Step 2: Rewrite the view model**

Replace `src/KCC.Web/Features/Pages/Account/AccountViewModel.cs` with:

```csharp
using System.Globalization;
using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.Account;

public class AccountViewModel : BasePageViewModel
{
    public string DisplayName { get; set; }

    public string Initials { get; set; }

    public string MemberSince { get; set; }

    public string SettingsUrl { get; set; }

    public IEnumerable<RecipeGroupViewModel> RecipeGroups { get; set; } = [];

    public static string ComputeInitials(string firstName, string lastName, string fallback)
    {
        var first = (firstName ?? string.Empty).Trim();
        var last = (lastName ?? string.Empty).Trim();

        var initials = string.Concat(
            first.Length > 0 ? first[..1] : string.Empty,
            last.Length > 0 ? last[..1] : string.Empty);

        if (initials.Length is 0)
        {
            var source = (fallback ?? string.Empty).Trim();
            initials = source.Length >= 2 ? source[..2] : source;
        }

        return initials.ToUpperInvariant();
    }

    public static string FormatMemberSince(DateTime? created) =>
        created?.ToString("MMMM yyyy", CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>
    /// Groups the member's variants under their recipes. Items absent from the published-key
    /// sets are pending review: badged, no URL. Recipes the member started always show;
    /// foreign recipes only show while they contain the member's variants. Variants whose
    /// parent recipe is missing are skipped.
    /// </summary>
    /// <param name="recipes">Recipes the member started plus parents of their variants.</param>
    /// <param name="variants">Variants the member authored.</param>
    /// <param name="publishedRecipeKeys">Keys of recipes that are published.</param>
    /// <param name="publishedVariantKeys">Keys of variants that are published.</param>
    /// <returns>Ordered display groups for the profile's creations section.</returns>
    /// <remarks>Callers must supply unique <see cref="AuthoredRecipeInput.Key"/> values; duplicates throw.</remarks>
    public static IEnumerable<RecipeGroupViewModel> BuildRecipeGroups(
        IReadOnlyCollection<AuthoredRecipeInput> recipes,
        IReadOnlyCollection<AuthoredVariantInput> variants,
        IReadOnlySet<Guid> publishedRecipeKeys,
        IReadOnlySet<Guid> publishedVariantKeys)
    {
        var groups = recipes.ToDictionary(
            recipe => recipe.Key,
            recipe =>
            {
                var isPublished = publishedRecipeKeys.Contains(recipe.Key);
                return new RecipeGroupViewModel
                {
                    Key = recipe.Key,
                    RecipeName = recipe.Name,
                    RecipeIcon = recipe.Icon,
                    RecipeUrl = isPublished ? recipe.Url : null,
                    IsPending = !isPublished,
                    StartedByYou = recipe.StartedByMe,
                };
            });

        var variantsByParent = variants.ToLookup(variant => variant.ParentKey);

        var enrichedGroups = groups.Values.Select(group =>
        {
            group.Variants = variantsByParent[group.Key]
                .Select(variant =>
                {
                    var isPublished = publishedVariantKeys.Contains(variant.Key);
                    return new ProfileVariantViewModel
                    {
                        Key = variant.Key,
                        Name = variant.Name,
                        Icon = variant.Icon,
                        Url = isPublished ? variant.Url : null,
                        IsPending = !isPublished,
                    };
                })
                .OrderBy(variant => variant.Name, StringComparer.OrdinalIgnoreCase);

            return group;
        });

        return enrichedGroups
            .Where(group => group.StartedByYou || group.Variants.Any())
            .OrderBy(group => group.RecipeName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>A recipe relevant to the member's profile: started by them, or parent of one of their variants.</summary>
    /// <param name="Key">The recipe node's key.</param>
    /// <param name="Name">Recipe display name.</param>
    /// <param name="Icon">Recipe icon class.</param>
    /// <param name="Url">Relative URL of the live page; only surfaced for published recipes.</param>
    /// <param name="StartedByMe">Whether the profile's member authored the recipe.</param>
    public record AuthoredRecipeInput(Guid Key, string Name, string Icon, string Url, bool StartedByMe);

    /// <summary>A variant authored by the member.</summary>
    /// <param name="Key">The variant node's key.</param>
    /// <param name="ParentKey">The parent recipe node's key.</param>
    /// <param name="Name">Variant display name.</param>
    /// <param name="Icon">Variant icon class.</param>
    /// <param name="Url">Relative URL of the live page; only surfaced for published variants.</param>
    public record AuthoredVariantInput(Guid Key, Guid ParentKey, string Name, string Icon, string Url);
}

public class RecipeGroupViewModel
{
    public Guid Key { get; set; }

    public string RecipeName { get; set; }

    public string RecipeIcon { get; set; }

    public string RecipeUrl { get; set; }

    public bool IsPending { get; set; }

    public bool StartedByYou { get; set; }

    public IEnumerable<ProfileVariantViewModel> Variants { get; set; } = [];
}

public class ProfileVariantViewModel
{
    public Guid Key { get; set; }

    public string Name { get; set; }

    public string Icon { get; set; }

    public string Url { get; set; }

    public bool IsPending { get; set; }
}
```

The grouping is Kentico's, keyed by node key instead of page id, and the view model gains `SettingsUrl`, since the
settings page is content now. In `src/KCC.Web/KCC.Web.csproj`, delete
`<Compile Remove="Features/Pages/Account/AccountViewModel.cs" />`.

```bash
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj -- --treenode-filter "/*/*/AccountViewModelTests/*"
```

Expected: 16 passed.

- [ ] **Step 3: Write the failing page tests**

In `tests/KCC.IntegrationTests/Config/TestContent.cs`, add the two draft helpers and `SavedAsync`:

```diff
--- a/tests/KCC.IntegrationTests/Config/TestContent.cs
+++ b/tests/KCC.IntegrationTests/Config/TestContent.cs
@@ -199,12 +199,44 @@
         }
     }
 
+    // Saved and never published, as a member's submission is until the owner publishes it.
+    public static Task<Guid> DraftRecipeAsync(IServiceProvider services, string name, Guid authorKey) =>
+        SavedAsync(services, "recipe", name, RecipeListing(services), [new PropertyValueModel { Alias = "icon", Value = "fa-duotone fa-egg" }, Author(authorKey)]);
+
+    public static Task<Guid> DraftVariantAsync(IServiceProvider services, Guid recipeKey, string name, Guid authorKey) =>
+        SavedAsync(services, "recipeVariant", name, recipeKey, [new PropertyValueModel { Alias = "icon", Value = "fa-duotone fa-egg" }, Author(authorKey)]);
+
     private static Guid Folder(IServiceProvider services, string name)
     {
         services.GetRequiredService<IDocumentNavigationQueryService>().TryGetRootKeysOfType("contentFolder", out var folders);
         return services.GetRequiredService<IContentService>().GetByIds(folders).Single(folder => folder.Name == name).Key;
     }
 
+    private static async Task<Guid> SavedAsync(IServiceProvider services, string contentTypeAlias, string name, Guid parentKey, IEnumerable<PropertyValueModel> values)
+    {
+        using var scope = services.CreateScope();
+        var scoped = scope.ServiceProvider;
+        var key = Guid.NewGuid();
+        var created = await scoped.GetRequiredService<IContentEditingService>().CreateAsync(
+            new ContentCreateModel
+            {
+                Key = key,
+                ContentTypeKey = scoped.GetRequiredService<IContentTypeService>().Get(contentTypeAlias)!.Key,
+                ParentKey = parentKey,
+                Variants = [new VariantModel { Name = name }],
+                Properties = values,
+            },
+            Constants.Security.SuperUserKey);
+
+        // A draft may miss mandatory fields, as a submission can; the owner completes it before publishing.
+        if (created.Status is not (ContentEditingOperationStatus.Success or ContentEditingOperationStatus.PropertyValidationError))
+        {
+            throw new InvalidOperationException($"Saving {name} failed: {created.Status}.");
+        }
+
+        return key;
+    }
+
     private static async Task<Guid> PublishedAsync(IServiceProvider services, string contentTypeAlias, string name, Guid parentKey, IEnumerable<PropertyValueModel> values)
     {
         using var scope = services.CreateScope();
```

If Phase 1 took the §19 template fallback, give `SavedAsync` the same `TemplateKey` line as `PublishedAsync`.

Create `tests/KCC.IntegrationTests/Features/Pages/AccountPagesTests.cs`:

```csharp
using System.Net;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Dictionary;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Pages;

public class AccountPagesTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    [Arguments("/account/", "%2Faccount%2F")]
    [Arguments("/account/settings/", "%2Faccount%2Fsettings%2F")]
    public async Task SignedOut_MemberPages_SendTheVisitorToSignIn(string path, string returnUrl)
    {
        using var visitor = new MemberClient(Site);

        using var response = await visitor.Http.GetAsync(path);

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        _ = await Assert.That(response.Headers.Location!.OriginalString).IsEqualTo($"/account/login/?returnUrl={returnUrl}");
    }

    [Test]
    public async Task AccountPage_ListsTheMembersPublishedAndPendingWork()
    {
        var userName = TestMembers.UniqueUserName("cook");
        var member = await TestMembers.ApprovedAsync(Site.Services, userName);
        TestContent.RenameAuthor(Site.Services, member, "Grace", "Hopper");
        var otherAuthor = await TestContent.AuthorAsync(Site.Services, TestMembers.UniqueUserName("other"), "Mary", "Somerville");
        var foreign = await TestContent.RecipeAsync(Site.Services, "IT Okapi", TestContent.Author(otherAuthor));
        await TestContent.VariantAsync(Site.Services, foreign, "Mary's Way", TestContent.Author(otherAuthor));
        await TestContent.VariantAsync(Site.Services, foreign, "Grace's Way", TestContent.Author(member));
        await TestContent.DraftVariantAsync(Site.Services, foreign, "Grace's Draft", member);
        var pending = await TestContent.DraftRecipeAsync(Site.Services, "IT Tapir", member);
        await TestContent.DraftVariantAsync(Site.Services, pending, "First Try", member);
        using var visitor = new MemberClient(Site);
        _ = await visitor.SignInAsync(userName, TestMembers.Password);

        var page = await RenderedPage.GetAsync(visitor.Http, "/account/");

        _ = await Assert.That(page.Attribute("display-name")).IsEqualTo("Grace Hopper");
        _ = await Assert.That(page.Attribute("settings-url")).IsEqualTo("/account/settings/");
        _ = await Assert.That(page.Attribute("logout-url")).IsEqualTo("/account/logout");
        var groups = page.Prop("recipe-groups").EnumerateArray().ToList();
        _ = await Assert.That(string.Join(" | ", groups.Select(Describe))).IsEqualTo(
            "IT Okapi (published, not started): Grace's Draft (pending), Grace's Way (published) | IT Tapir (pending, started): First Try (pending)");
    }

    [Test]
    public async Task LoginPage_WhenSignedIn_ReturnsToALocalUrlOnly()
    {
        var userName = TestMembers.UniqueUserName("back");
        await TestMembers.ApprovedAsync(Site.Services, userName);
        using var visitor = new MemberClient(Site);
        _ = await visitor.SignInAsync(userName, TestMembers.Password);

        using var local = await visitor.Http.GetAsync("/account/login/?returnUrl=%2Frecipes%2F");
        using var foreign = await visitor.Http.GetAsync("/account/login/?returnUrl=https%3A%2F%2Fevil.example%2F");

        _ = await Assert.That(local.Headers.Location!.OriginalString).IsEqualTo("/recipes/");
        _ = await Assert.That(foreign.Headers.Location!.OriginalString).IsEqualTo("/");
    }

    [Test]
    public async Task LoginPage_PassesOnlyALocalReturnUrlToTheForm()
    {
        using var visitor = new MemberClient(Site);

        var local = await RenderedPage.GetAsync(visitor.Http, "/account/login/?returnUrl=%2Faccount%2F");
        var foreign = await RenderedPage.GetAsync(visitor.Http, "/account/login/?returnUrl=https%3A%2F%2Fevil.example%2F");

        _ = await Assert.That(local.Attribute("return-url")).IsEqualTo("/account/");
        // Razor drops an attribute whose value is null.
        _ = await Assert.That(foreign.Attribute("return-url")).IsNull();
    }

    [Test]
    public async Task SettingsPage_ShowsTheMembersNamesAndEmail()
    {
        var userName = TestMembers.UniqueUserName("names");
        var member = await TestMembers.ApprovedAsync(Site.Services, userName);
        TestContent.RenameAuthor(Site.Services, member, "Katherine", "Johnson");
        using var visitor = new MemberClient(Site);
        _ = await visitor.SignInAsync(userName, TestMembers.Password);

        var page = await RenderedPage.GetAsync(visitor.Http, "/account/settings/");

        _ = await Assert.That(page.Attribute("first-name")).IsEqualTo("Katherine");
        _ = await Assert.That(page.Attribute("last-name")).IsEqualTo("Johnson");
        _ = await Assert.That(page.Attribute("email")).IsEqualTo($"{userName}@example.test");
        _ = await Assert.That(page.Attribute("back-url")).IsEqualTo("/account/");
        _ = await Assert.That(page.Attribute("logout-url")).IsEqualTo("/account/logout");
    }

    [Test]
    public async Task RegistrationCompletePage_SaysTheAccountWaitsAndLinksToSignIn()
    {
        using var visitor = new MemberClient(Site);

        var page = await RenderedPage.GetAsync(visitor.Http, "/account/registration-complete/");

        _ = await Assert.That(page.Status).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(page.Body).Contains(WebUtility.HtmlEncode(String("RegistrationComplete.Body")));
        _ = await Assert.That(page.Body).Contains("href=\"/account/login/\"");
    }

    private static string Describe(System.Text.Json.JsonElement group)
    {
        var variants = group.GetProperty("variants").EnumerateArray()
            .Select(variant => $"{variant.GetProperty("name").GetString()} ({(variant.GetProperty("isPending").GetBoolean() ? "pending" : "published")})");
        var state = group.GetProperty("isPending").GetBoolean() ? "pending" : "published";
        var started = group.GetProperty("startedByYou").GetBoolean() ? "started" : "not started";
        return $"{group.GetProperty("recipeName").GetString()} ({state}, {started}): {string.Join(", ", variants)}";
    }

    private string String(string key)
    {
        using var scope = Site.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IResourceStringProvider>().GetOrDefault(key);
    }
}
```

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/AccountPagesTests/*"
```

Expected: every test fails. The pages answer 404, because their controllers are still excluded.

- [ ] **Step 4: Write the read of the member's work, and the redirect**

Create `src/KCC.Web/Features/Pages/Account/AuthoredRecipeQueries.cs`:

```csharp
using KCC.Web.Features.Models.Generated;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Extensions;

namespace KCC.Web.Features.Pages.Account;

public sealed record AuthoredRecipes(
    IReadOnlyList<AccountViewModel.AuthoredRecipeInput> Recipes,
    IReadOnlyList<AccountViewModel.AuthoredVariantInput> Variants,
    IReadOnlySet<Guid> PublishedRecipeKeys,
    IReadOnlySet<Guid> PublishedVariantKeys)
{
    public static AuthoredRecipes None { get; } = new([], [], new HashSet<Guid>(), new HashSet<Guid>());
}

public interface IAuthoredRecipeQueries
{
    AuthoredRecipes GetAuthoredBy(Guid memberKey);
}

// The account page lists a member's submissions before the owner publishes them, so this is the site's one read of
// saved, unpublished content. Published nodes come from the published cache; only drafts are loaded.
public class AuthoredRecipeQueries(
    IPublishedContentQuery contentQuery,
    IDocumentNavigationQueryService navigation,
    IContentService contentService) : IAuthoredRecipeQueries
{
    public AuthoredRecipes GetAuthoredBy(Guid memberKey)
    {
        var listing = contentQuery.ContentAtRoot().OfType<HomePage>().SelectMany(home => home.Children<RecipeListingPage>()).FirstOrDefault();
        if (listing is null || !navigation.TryGetChildrenKeysOfType(listing.Key, "recipe", out var recipeKeys))
        {
            return AuthoredRecipes.None;
        }

        var variantsByRecipe = recipeKeys.ToDictionary(
            recipeKey => recipeKey,
            recipeKey => navigation.TryGetChildrenKeysOfType(recipeKey, "recipeVariant", out var keys) ? keys.ToList() : []);
        var nodes = Describe(recipeKeys.Concat(variantsByRecipe.Values.SelectMany(keys => keys)).ToList());

        var variants = variantsByRecipe
            .SelectMany(pair => pair.Value.Select(variantKey => (RecipeKey: pair.Key, Node: nodes.GetValueOrDefault(variantKey))))
            .Where(variant => variant.Node?.AuthorKey == memberKey)
            .Select(variant => new AccountViewModel.AuthoredVariantInput(variant.Node.Key, variant.RecipeKey, variant.Node.Name, variant.Node.Icon, variant.Node.Url))
            .ToList();
        var parentKeys = variants.Select(variant => variant.ParentKey).ToHashSet();
        var recipes = recipeKeys
            .Select(recipeKey => nodes.GetValueOrDefault(recipeKey))
            .Where(recipe => recipe is not null && (recipe.AuthorKey == memberKey || parentKeys.Contains(recipe.Key)))
            .Select(recipe => new AccountViewModel.AuthoredRecipeInput(recipe.Key, recipe.Name, recipe.Icon, recipe.Url, recipe.AuthorKey == memberKey))
            .ToList();

        return new AuthoredRecipes(
            recipes,
            variants,
            recipes.Where(recipe => nodes[recipe.Key].IsPublished).Select(recipe => recipe.Key).ToHashSet(),
            variants.Where(variant => nodes[variant.Key].IsPublished).Select(variant => variant.Key).ToHashSet());
    }

    // A member picker stores its value as a member UDI, such as umb://member/0a1b…
    private static Guid? MemberKey(object value) =>
        value is string text && UdiParser.TryParse(text, out Udi udi) && udi is GuidUdi guidUdi ? guidUdi.Guid : null;

    private Dictionary<Guid, Node> Describe(IReadOnlyCollection<Guid> keys)
    {
        var nodes = new Dictionary<Guid, Node>();
        var drafts = new List<Guid>();
        foreach (var key in keys)
        {
            switch (contentQuery.Content(key))
            {
                case Recipe recipe:
                    nodes[key] = new Node(key, recipe.Name, recipe.Icon, recipe.Url(), recipe.Author?.Key, IsPublished: true);
                    break;
                case RecipeVariant variant:
                    nodes[key] = new Node(key, variant.Name, variant.Icon, variant.Url(), variant.Author?.Key, IsPublished: true);
                    break;
                default:
                    drafts.Add(key);
                    break;
            }
        }

        foreach (var draft in drafts.Count == 0 ? [] : contentService.GetByIds(drafts))
        {
            nodes[draft.Key] = new Node(draft.Key, draft.Name, draft.GetValue<string>("icon"), null, MemberKey(draft.GetValue("author")), IsPublished: false);
        }

        return nodes;
    }

    private sealed record Node(Guid Key, string Name, string Icon, string Url, Guid? AuthorKey, bool IsPublished);
}
```

Create `src/KCC.Web/Features/Pages/Account/SignInRedirect.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Pages.Account;

public static class SignInRedirect
{
    // Sends a signed-out visitor to the login page, which returns them here once they have signed in.
    public static IActionResult To(string loginUrl, HttpRequest request) =>
        loginUrl is null
            ? new NotFoundResult()
            : new RedirectResult($"{loginUrl}?returnUrl={Uri.EscapeDataString(request.Path + request.QueryString)}");
}
```

In `src/KCC.Web/Program.cs`, after the `IAccountPageQueries` registration:

```csharp
builder.Services.AddScoped<IAuthoredRecipeQueries, AuthoredRecipeQueries>();
```

- [ ] **Step 5: Move and rewrite the four controllers and their views**

```bash
cd src/KCC.Web/Features/Pages/Account
git mv AccountController.cs AccountPageController.cs
git mv Login/LoginController.cs Login/LoginPageController.cs
git mv Settings/AccountSettingsController.cs Settings/AccountSettingsPageController.cs
git mv RegistrationComplete/RegistrationCompleteController.cs RegistrationComplete/RegistrationCompletePageController.cs
cd ../../../../..
```

Replace `src/KCC.Web/Features/Pages/Account/AccountPageController.cs` with:

```csharp
using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using KCC.Web.Features.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.Account;

public class AccountPageController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    IMemberManager memberManager,
    IMemberService memberService,
    IAuthoredRecipeQueries authoredRecipes,
    IAccountPageQueries accountPages,
    IResourceStringProvider resourceStrings,
    PageMetadata pageMetadata)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    // Route hijacking calls the synchronous Index unless it is hidden like this; the overload below serves the page.
    [NonAction]
    public sealed override IActionResult Index() => throw new NotSupportedException();

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (CurrentPage is not AccountPage page)
        {
            return NotFound();
        }

        var urls = accountPages.GetUrls();
        if (await memberManager.GetCurrentMemberAsync() is not { } signedIn)
        {
            return SignInRedirect.To(urls.Login, Request);
        }

        var member = memberService.GetById(signedIn.Key);
        var firstName = member?.GetValue<string>("firstName");
        var lastName = member?.GetValue<string>("lastName");
        var authored = authoredRecipes.GetAuthoredBy(signedIn.Key);
        var viewModel = new AccountViewModel
        {
            DisplayName = AuthorNameProvider.FormatDisplayName(firstName, lastName, signedIn.UserName),
            Initials = AccountViewModel.ComputeInitials(firstName, lastName, signedIn.UserName),
            MemberSince = AccountViewModel.FormatMemberSince(member?.CreateDate),
            SettingsUrl = urls.Settings,
            RecipeGroups = AccountViewModel.BuildRecipeGroups(
                authored.Recipes,
                authored.Variants,
                authored.PublishedRecipeKeys,
                authored.PublishedVariantKeys).ToList(),
            ResourceStrings = GetStrings(),
        };
        pageMetadata.Apply(page, viewModel);

        return View("~/Features/Pages/Account/Index.cshtml", viewModel);
    }

    private Dictionary<string, string> GetStrings() => resourceStrings.GetManyOrDefault(
        "Account.MemberSince",
        "Account.AccountSettings",
        "Account.SignOut",
        "Account.MyRecipesAndVariants",
        "Account.Favorites",
        "Account.RecentActivity",
        "Account.ComingSoon",
        "Account.StartedByYou",
        "Account.PendingReview",
        "Account.NoCreationsYet",
        "Account.RecipesLabel",
        "Account.VariantsLabel");
}
```

Replace `src/KCC.Web/Features/Pages/Account/Index.cshtml` with:

```cshtml
@using KCC.Web.Features.Extensions
@using KCC.Web.Features.Helpers
@using KCC.Web.Features.Pages.Account
@using KCC.Web.Features.Pages.Account.Logout

@model AccountViewModel

<AccountView
  display-name="@Model.DisplayName"
  initials="@Model.Initials"
  member-since="@Model.MemberSince"
  settings-url="@Model.SettingsUrl"
  logout-url="@(Url.ActionFor<LogoutController>(c => c.Index(null)))"
  :recipe-groups="@Vue.Prop(Model.RecipeGroups)"
  :resource-strings="@Vue.Prop(Model.ResourceStrings)"
/>
```

Replace `src/KCC.Web/Features/Pages/Account/Login/LoginPageController.cs` with:

```csharp
using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.Account.Login;

public class LoginPageController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    IMemberManager memberManager,
    IResourceStringProvider resourceStrings,
    PageMetadata pageMetadata)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    // Route hijacking calls the synchronous Index unless it is hidden like this; the overload below serves the page.
    [NonAction]
    public sealed override IActionResult Index() => throw new NotSupportedException();

    public async Task<IActionResult> Index([FromQuery] string returnUrl, CancellationToken cancellationToken)
    {
        if (CurrentPage is not LoginPage page)
        {
            return NotFound();
        }

        var localReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        if (await memberManager.GetCurrentMemberAsync() is not null)
        {
            return Redirect(localReturnUrl ?? "/");
        }

        var viewModel = new LoginViewModel
        {
            ReturnUrl = localReturnUrl,
            ResourceStrings = GetStrings(),
        };
        pageMetadata.Apply(page, viewModel);

        return View("~/Features/Pages/Account/Login/Index.cshtml", viewModel);
    }

    private Dictionary<string, string> GetStrings() => resourceStrings.GetManyOrDefault(
        "Login.SignIn",
        "Login.SignUp",
        "Login.UsernamePlaceholder",
        "Login.EmailPlaceholder",
        "Login.PasswordPlaceholder",
        "Login.ConfirmPasswordPlaceholder",
        "Login.RememberMe",
        "Login.HaveAccount",
        "Login.HaveAccountDescription",
        "Login.NewHere",
        "Login.NewHereDescription");
}
```

`Login/LoginViewModel.cs` and `Login/Index.cshtml` are unchanged. Replace
`src/KCC.Web/Features/Pages/Account/Settings/AccountSettingsPageController.cs` with:

```csharp
using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.Account.Settings;

public class AccountSettingsPageController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    IMemberManager memberManager,
    IMemberService memberService,
    IAccountPageQueries accountPages,
    IResourceStringProvider resourceStrings,
    PageMetadata pageMetadata)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    // Route hijacking calls the synchronous Index unless it is hidden like this; the overload below serves the page.
    [NonAction]
    public sealed override IActionResult Index() => throw new NotSupportedException();

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (CurrentPage is not AccountSettingsPage page)
        {
            return NotFound();
        }

        var urls = accountPages.GetUrls();
        if (await memberManager.GetCurrentMemberAsync() is not { } signedIn)
        {
            return SignInRedirect.To(urls.Login, Request);
        }

        var member = memberService.GetById(signedIn.Key);
        var viewModel = new AccountSettingsViewModel
        {
            FirstName = member?.GetValue<string>("firstName"),
            LastName = member?.GetValue<string>("lastName"),
            Email = signedIn.Email,
            BackUrl = urls.Account,
            ResourceStrings = GetStrings(),
        };
        pageMetadata.Apply(page, viewModel);

        return View("~/Features/Pages/Account/Settings/Index.cshtml", viewModel);
    }

    private Dictionary<string, string> GetStrings() => resourceStrings.GetManyOrDefault(
        "Account.BackToProfile",
        "Account.AccountSettings",
        "Account.Profile",
        "Account.FirstName",
        "Account.LastName",
        "Account.Email",
        "Account.EmailComingSoon",
        "Account.EmailComingSoonNote",
        "Account.SaveChanges",
        "Account.ChangePassword",
        "Account.CurrentPassword",
        "Account.NewPassword",
        "Account.ConfirmNewPassword",
        "Account.UpdatePassword",
        "Account.SignOut",
        "Account.PasswordsDoNotMatch",
        "Account.ProfileSaved",
        "Account.PasswordUpdated",
        "Account.UnexpectedError");
}
```

In `src/KCC.Web/Features/Pages/Account/Settings/Index.cshtml`, the logout link needs `null` spelled out, because an
expression tree cannot leave out an optional argument:

```cshtml
@using KCC.Web.Features.Extensions
@using KCC.Web.Features.Helpers
@using KCC.Web.Features.Pages.Account.Logout
@using KCC.Web.Features.Pages.Account.Settings

@model AccountSettingsViewModel

<AccountSettingsView
  first-name="@Model.FirstName"
  last-name="@Model.LastName"
  email="@Model.Email"
  back-url="@Model.BackUrl"
  logout-url="@(Url.ActionFor<LogoutController>(c => c.Index(null)))"
  :resource-strings="@Vue.Prop(Model.ResourceStrings)"
/>
```

`Settings/AccountSettingsViewModel.cs` is unchanged. Create
`src/KCC.Web/Features/Pages/Account/RegistrationComplete/RegistrationCompleteViewModel.cs`:

```csharp
using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.Account.RegistrationComplete;

public class RegistrationCompleteViewModel : BasePageViewModel
{
    public string LoginUrl { get; set; }
}
```

Replace `src/KCC.Web/Features/Pages/Account/RegistrationComplete/RegistrationCompletePageController.cs` with:

```csharp
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.Account.RegistrationComplete;

public class RegistrationCompletePageController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    IAccountPageQueries accountPages,
    PageMetadata pageMetadata)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    public override IActionResult Index()
    {
        if (CurrentPage is not RegistrationCompletePage page)
        {
            return NotFound();
        }

        var viewModel = new RegistrationCompleteViewModel { LoginUrl = accountPages.GetUrls().Login };
        pageMetadata.Apply(page, viewModel);

        return View("~/Features/Pages/Account/RegistrationComplete/Index.cshtml", viewModel);
    }
}
```

Replace `src/KCC.Web/Features/Pages/Account/RegistrationComplete/Index.cshtml` with:

```cshtml
@using KCC.Web.Features.Dictionary
@using KCC.Web.Features.Pages.Account.RegistrationComplete

@inject IResourceStringProvider ResourceStrings

@model RegistrationCompleteViewModel

@* Razor cannot use the Sheet.vue primitive (not a *.Component.vue, so not globally registered); it
   hand-writes the same Structure from the global kit classes in Kit.css. *@
<div class="kcc-slip kcc-tear-2 mx-auto mt-12 w-full max-w-2xl">
  <div class="kcc-torn">
    <div class="kcc-sheet" style="--pad: 48px">
      <span class="kcc-wash" style="--c: var(--color-green); --x: 85%; --y: 90%; --w: 55%; --h: 50%" aria-hidden="true"></span>
      <p class="kcc-kick">Registered</p>
      <h1 class="kcc-h3 mt-6">@ResourceStrings.GetOrDefault("RegistrationComplete.Heading")</h1>
      <p class="kcc-body mt-6">@ResourceStrings.GetOrDefault("RegistrationComplete.Body")</p>
      <div class="mt-6 flex flex-wrap gap-4">
        <button-link class="kcc-btn--lg" href="@Model.LoginUrl">@ResourceStrings.GetOrDefault("RegistrationComplete.ContinueLink")</button-link>
      </div>
    </div>
  </div>
  <span class="kcc-label"><i class="fa-duotone fa-hourglass-half" aria-hidden="true"></i>Registered</span>
  <span class="kcc-tape" aria-hidden="true"></span>
</div>
```

The old view read its text through the resource-string HTML helper and linked to `Url.HomePage()`, and both went
with Kentico. Its envelope icon promised an email that is never sent. The hourglass says the account is waiting.

In the `Unported slices` group of `src/KCC.Web/KCC.Web.csproj`, delete the four remaining `Features/Pages/Account/…`
`Compile Remove` lines and `<Content Remove="Features/Pages/Account/**/*.cshtml" />`.

- [ ] **Step 6: Key the account page's groups on the client too**

Apply to `src/KCC.Web/Features/Pages/Account/AccountView.Component.vue`:

```diff
--- a/src/KCC.Web/Features/Pages/Account/AccountView.Component.vue
+++ b/src/KCC.Web/Features/Pages/Account/AccountView.Component.vue
@@ -13,7 +13,7 @@
   }
 
   interface ProfileVariant {
-    pageId: number
+    key: string
     name: string
     icon?: string
     url?: string
@@ -21,7 +21,7 @@
   }
 
   interface RecipeGroup {
-    pageId: number
+    key: string
     recipeName: string
     recipeIcon?: string
     recipeUrl?: string
@@ -106,7 +106,7 @@
       <ul v-else class="grid gap-9">
         <KccSheet
           v-for="(group, index) in recipeGroups"
-          :key="group.pageId"
+          :key="group.key"
           as="li"
           :tear="listTearFor(index + 1)"
           pad="16px"
@@ -127,7 +127,7 @@
           </div>
 
           <ul v-if="group.variants.length" class="mt-6 grid gap-2">
-            <li v-for="variant in group.variants" :key="variant.pageId" class="grid grid-cols-[1fr_auto] items-center gap-3">
+            <li v-for="variant in group.variants" :key="variant.key" class="grid grid-cols-[1fr_auto] items-center gap-3">
               <span class="flex flex-wrap items-center gap-2">
                 <i v-if="variant.icon" :class="variant.icon" aria-hidden="true"></i>
                 <component
```

And to `tests/KCC.ViteTests/Features/Pages/Account/AccountView.test.ts`:

```diff
--- a/tests/KCC.ViteTests/Features/Pages/Account/AccountView.test.ts
+++ b/tests/KCC.ViteTests/Features/Pages/Account/AccountView.test.ts
@@ -20,7 +20,7 @@
 
 const RECIPE_GROUPS = [
   {
-    pageId: 1,
+    key: '8d2b0c1e-4f6a-4d9b-9a31-2c7e5f0b6a11',
     recipeName: 'Mac & Cheese',
     recipeIcon: 'fa-duotone fa-pot-food',
     recipeUrl: '/recipes/mac-and-cheese',
@@ -28,22 +28,22 @@
     startedByYou: true,
     variants: [
       {
-        pageId: 10,
+        key: '5b7c9e2a-1d3f-4a6b-8c9d-0e1f2a3b4c10',
         name: 'Classic Stovetop',
         icon: 'fa-duotone fa-pot-food',
         url: '/recipes/mac/classic',
         isPending: false,
       },
-      { pageId: 11, name: 'Spicy Jalapeño', icon: 'fa-duotone fa-pepper-hot', isPending: true },
+      { key: '6c8d0f3b-2e4a-4b7c-9d0e-1f2a3b4c5d11', name: 'Spicy Jalapeño', icon: 'fa-duotone fa-pepper-hot', isPending: true },
     ],
   },
   {
-    pageId: 2,
+    key: '7d9e1a4c-3f5b-4c8d-8e1f-2a3b4c5d6e02',
     recipeName: 'Brown Butter Gnocchi',
     recipeIcon: 'fa-duotone fa-wheat',
     isPending: true,
     startedByYou: false,
-    variants: [{ pageId: 20, name: 'Sage Brown Butter', icon: 'fa-duotone fa-leaf', isPending: true }],
+    variants: [{ key: '9e0f2b5d-4a6c-4d9e-9f2a-3b4c5d6e7f20', name: 'Sage Brown Butter', icon: 'fa-duotone fa-leaf', isPending: true }],
   },
 ]
 
```

- [ ] **Step 7: Run everything this task touched**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/AccountPagesTests/*"
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
(cd src/KCC.Web && yarn test AccountView && yarn type-check)
```

Expected: 0 warnings; 7 passed; every unit test passes; the AccountView spec's 10 tests pass, and the type check
passes.

- [ ] **Step 8: Commit**

```bash
git add -A src/KCC.Web tests/KCC.IntegrationTests tests/KCC.UnitTests tests/KCC.ViteTests
git commit -m "Bring Back the Account Pages on Umbraco"
```

---

### Task 5: Profile and password

The settings page's two forms post to `api/profile` and `api/profile/password`, as they did on Kentico. A name
change saves the member through `IMemberService`. A password change goes through the member manager, and then
re-issues the cookie, because the change gives the member a new security stamp that would end this session at its
next check. Both run inside `IMemberWriteLock`. The password change counts against the `account` rate limit.

**Files:**
- Rewrite: `src/KCC.Web/Features/Api/ProfileApiController.cs`
- Create: `tests/KCC.IntegrationTests/Features/Api/ProfileApiTests.cs`
- Delete: `tests/KCC.UnitTests/Features/Api/ProfileApiControllerTests.cs`
- Modify: `src/KCC.Web/KCC.Web.csproj`, `tests/KCC.UnitTests/KCC.UnitTests.csproj`

**Interfaces:**
- Consumes: `MemberClient`, `TestMembers`, `IMemberWriteLock`, `IAuthorNameProvider.Resolve`.
- Produces `POST /api/profile` with `UpdateProfileRequest(string FirstName, string LastName)` and
  `POST /api/profile/password` with `ChangePasswordRequest(string CurrentPassword, string NewPassword)`. Both answer
  `ProfileResponse(bool Success, string[] Errors)`, or 401 when signed out.

- [ ] **Step 1: Write the failing tests**

Create `tests/KCC.IntegrationTests/Features/Api/ProfileApiTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Api;

public class ProfileApiTests
{
    private const string VariantPath = "/recipes/fluffy-buttermilk-pancakes/classic-stack/";

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task UpdateProfile_SavesTrimmedNames_AndRenamesTheAuthor()
    {
        using var member = await SignedInAsync("rename");

        var response = await ReadAsync(await member.Visitor.PostAsync("/api/profile", new { firstName = "  Grace ", lastName = " Hopper  " }));

        _ = await Assert.That(response.Success).IsTrue();
        _ = await Assert.That(await Site.Services.GetRequiredService<IAuthorNameProvider>().Resolve(member.Key)).IsEqualTo("Grace Hopper");
    }

    [Test]
    public async Task UpdateProfile_WithoutALastName_IsRefused()
    {
        using var member = await SignedInAsync("half");

        var response = await ReadAsync(await member.Visitor.PostAsync("/api/profile", new { firstName = "Grace", lastName = " " }));

        _ = await Assert.That(response.Success).IsFalse();
        _ = await Assert.That(response.Errors!.Single()).IsEqualTo(String("Account.NameRequiredError"));
    }

    [Test]
    public async Task UpdateProfile_SignedOut_IsUnauthorized()
    {
        using var visitor = new MemberClient(Site);

        using var response = await visitor.PostAsync("/api/profile", new { firstName = "Grace", lastName = "Hopper" });

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ChangePassword_KeepsTheMemberSignedIn_AndTheNewPasswordWorks()
    {
        using var member = await SignedInAsync("rotate");

        var response = await ReadAsync(await member.Visitor.PostAsync("/api/profile/password", new { currentPassword = TestMembers.Password, newPassword = "Brand-New-Passw0rd" }));

        _ = await Assert.That(response.Success).IsTrue();
        _ = await Assert.That(await member.Visitor.IsSignedInAsync(VariantPath)).IsTrue();
        using var elsewhere = new MemberClient(Site);
        _ = await Assert.That((await elsewhere.SignInAsync(member.UserName, TestMembers.Password)).Success).IsFalse();
        _ = await Assert.That((await elsewhere.SignInAsync(member.UserName, "Brand-New-Passw0rd")).Success).IsTrue();
    }

    [Test]
    public async Task ChangePassword_WithTheWrongCurrentPassword_Fails()
    {
        using var member = await SignedInAsync("forgot");

        var response = await ReadAsync(await member.Visitor.PostAsync("/api/profile/password", new { currentPassword = "not-my-password", newPassword = "Brand-New-Passw0rd" }));

        _ = await Assert.That(response.Success).IsFalse();
        _ = await Assert.That(response.Errors!.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task ChangePassword_ToOneTooShort_Fails()
    {
        using var member = await SignedInAsync("short");

        var response = await ReadAsync(await member.Visitor.PostAsync("/api/profile/password", new { currentPassword = TestMembers.Password, newPassword = "Short7!" }));

        _ = await Assert.That(response.Success).IsFalse();
    }

    private static async Task<ProfileResult> ReadAsync(HttpResponseMessage response)
    {
        using (response)
        {
            return await response.Content.ReadFromJsonAsync<ProfileResult>() ?? throw new InvalidOperationException("The profile API answered no body.");
        }
    }

    private async Task<SignedInMember> SignedInAsync(string prefix)
    {
        var userName = TestMembers.UniqueUserName(prefix);
        var key = await TestMembers.ApprovedAsync(Site.Services, userName);
        var visitor = new MemberClient(Site);
        _ = await visitor.SignInAsync(userName, TestMembers.Password);
        return new SignedInMember(visitor, key, userName);
    }

    private string String(string key)
    {
        using var scope = Site.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IResourceStringProvider>().GetOrDefault(key);
    }

    private sealed record ProfileResult(bool Success, string[]? Errors);

    private sealed record SignedInMember(MemberClient Visitor, Guid Key, string UserName) : IDisposable
    {
        public void Dispose() => Visitor.Dispose();
    }
}
```

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/ProfileApiTests/*"
```

Expected: every test fails, with 404s from `/api/profile`.

- [ ] **Step 2: Rewrite the controller**

Replace `src/KCC.Web/Features/Api/ProfileApiController.cs` with:

```csharp
using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Security;
using KCC.Web.Features.Sqlite;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api/profile")]
[AutoValidateAntiforgeryToken]
public class ProfileApiController(
    IMemberManager memberManager,
    IMemberService memberService,
    SignInManager<MemberIdentityUser> signInManager,
    IMemberWriteLock memberWriteLock,
    IResourceStringProvider resourceStrings) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (await memberManager.GetCurrentMemberAsync() is not { } signedIn)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request?.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return Ok(new ProfileResponse(false, [resourceStrings.GetOrDefault("Account.NameRequiredError")]));
        }

        await memberWriteLock.RunAsync(() =>
        {
            var member = memberService.GetById(signedIn.Key);
            member.SetValue("firstName", request.FirstName.Trim());
            member.SetValue("lastName", request.LastName.Trim());
            memberService.Save(member);
            return Task.CompletedTask;
        });

        return Ok(new ProfileResponse(true, null));
    }

    [HttpPost("password")]
    [EnableRateLimiting(RateLimits.Account)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (await memberManager.GetCurrentMemberAsync() is not { } signedIn)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request?.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Ok(new ProfileResponse(false, [resourceStrings.GetOrDefault("Account.PasswordRequiredError")]));
        }

        var result = await memberWriteLock.RunAsync(async () =>
        {
            var changed = await memberManager.ChangePasswordAsync(signedIn, request.CurrentPassword, request.NewPassword);
            if (changed.Succeeded)
            {
                // The change replaces the member's security stamp, which would end this session at its next check.
                await signInManager.RefreshSignInAsync(signedIn);
            }

            return changed;
        });

        return result.Succeeded
            ? Ok(new ProfileResponse(true, null))
            : Ok(new ProfileResponse(false, [.. result.Errors.Select(error => error.Description)]));
    }
}

public sealed record UpdateProfileRequest(string FirstName, string LastName);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record ProfileResponse(bool Success, string[] Errors);
```

In `src/KCC.Web/KCC.Web.csproj`, delete `<Compile Remove="Features/Api/ProfileApiController.cs" />`. The Kentico
unit tests mocked Kentico's identity classes and have nothing left to test; the integration tests replace them:

```bash
git rm -q tests/KCC.UnitTests/Features/Api/ProfileApiControllerTests.cs
```

In `tests/KCC.UnitTests/KCC.UnitTests.csproj`, delete `<Compile Remove="Features/Api/ProfileApiControllerTests.cs" />`.

- [ ] **Step 3: Run the tests**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/ProfileApiTests/*"
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
```

Expected: 0 warnings; 6 passed; every unit test passes.

- [ ] **Step 4: Commit**

```bash
git add -A src/KCC.Web tests/KCC.IntegrationTests tests/KCC.UnitTests
git commit -m "Port Profile and Password Changes to Umbraco"
```

---

### Task 6: Sign-out forms and the approval wording

Signing out is a POST now, so three places turn from links into a small form (spec §8):
- the header menu's **Logout** entry;
- the account page's **Sign out**;
- the settings page's **Sign out**.

The header's nav is content, and its Logout entry links to `/account/logout`, so the menu swaps any entry with
that URL for the form. The anti-forgery token is filled in as the form submits. The server render has no token to
give, and the one Layout hands the client is already in `Api.ts`. Three dictionary strings change as well:
- a new account waits for approval;
- an unapproved member is told so;
- sign-in is by username, not email.

**Files:**
- Create: `src/KCC.Web/Features/Components/Account/SignOutForm.vue`,
  `tests/KCC.ViteTests/Features/Components/Account/SignOutForm.test.ts`,
  `tests/KCC.ViteTests/Features/Components/Header/MenuItem.test.ts`,
  `tests/KCC.IntegrationTests/Features/Baseline/AccountStringsTests.cs`
- Modify: `src/KCC.Web/Features/Utilities/Api.ts`, `src/KCC.Web/Features/Components/Header/MenuItem.vue`,
  `src/KCC.Web/Features/Pages/Account/AccountView.Component.vue`,
  `src/KCC.Web/Features/Pages/Account/Settings/AccountSettingsView.Component.vue`,
  `tests/KCC.ViteTests/Features/Utilities/Api.test.ts`,
  `tests/KCC.ViteTests/Features/Pages/Account/AccountView.test.ts`,
  `tests/KCC.ViteTests/Features/Pages/Account/Settings/AccountSettingsView.test.ts`,
  three files under `src/KCC.Web/uSync/v17/Dictionary/`, or `Features/Dictionary/baseline-strings.json` (see
  "Before you start", item 15)

**Interfaces:**
- Consumes: `POST /account/logout` (Task 3).
- Produces:
  - `antiforgeryToken() → string` in `~/Utilities/Api`;
  - `SignOutForm` (`~/Components/Account/SignOutForm.vue`), which takes an `action` prop (default `/account/logout`)
    and a default slot for its submit button;
  - `SIGN_OUT_PATH` and `isSignOutUrl(url?: string) → boolean`, exported from the same file.

- [ ] **Step 1: Write the failing specs**

Create `tests/KCC.ViteTests/Features/Components/Account/SignOutForm.test.ts`:

```ts
import { describe, expect, it } from 'vitest'
import { h } from 'vue'
import SignOutForm, { isSignOutUrl } from '~/Components/Account/SignOutForm.vue'
import { renderSsr } from '../../../support/renderSsr'

const button = { default: () => h('button', { type: 'submit' }, 'Sign out') }

describe('SignOutForm', () => {
  it('posts to the sign-out path, with a token field the submit fills in', async () => {
    const html = await renderSsr(SignOutForm, {}, button)

    expect(html).toContain('<form method="post" action="/account/logout" class="contents">')
    expect(html).toContain('<input type="hidden" name="__RequestVerificationToken" value="">')
    expect(html).toContain('<button type="submit">Sign out</button>')
  })

  it('posts to the action it is given', async () => {
    const html = await renderSsr(SignOutForm, { action: '/account/logout?returnUrl=%2Frecipes' }, button)

    expect(html).toContain('action="/account/logout?returnUrl=%2Frecipes"')
  })

  it.each([
    ['/account/logout', true],
    ['/account/logout/', true],
    ['/Account/Logout', true],
    ['/account/', false],
    [undefined, false],
  ])('treats %s as the sign-out link: %s', (url, expected) => {
    expect(isSignOutUrl(url)).toBe(expected)
  })
})
```

Create `tests/KCC.ViteTests/Features/Components/Header/MenuItem.test.ts`:

```ts
import { describe, expect, it } from 'vitest'
import MenuItem from '~/Components/Header/MenuItem.vue'
import { renderSsr } from '../../../support/renderSsr'

const account = {
  displayText: 'Account',
  subLinks: [
    { displayText: 'Profile', url: '/account/', target: '' },
    { displayText: 'Logout', url: '/account/logout', target: '' },
  ],
}

describe('MenuItem', () => {
  it('signs out through a posted form, not a link', async () => {
    const html = await renderSsr(MenuItem, { item: account, menuId: 'Account' })

    expect(html).toMatch(/<form method="post" action="\/account\/logout"[^>]*>.*<button type="submit"[^>]*>\s*Logout\s*<\/button>/s)
    expect(html).not.toContain('href="/account/logout"')
  })

  it('keeps every other entry a link', async () => {
    const html = await renderSsr(MenuItem, { item: account, menuId: 'Account' })

    expect(html).toMatch(/<a[^>]*href="\/account\/"[^>]*>\s*Profile\s*<\/a>/)
  })

  it('turns a flat Logout entry into the same form', async () => {
    const html = await renderSsr(MenuItem, { item: { displayText: 'Logout', url: '/account/logout' }, menuId: 'Logout' })

    expect(html).toMatch(/^<form method="post" action="\/account\/logout"/)
  })
})
```

Apply to `tests/KCC.ViteTests/Features/Utilities/Api.test.ts`:

```diff
--- a/tests/KCC.ViteTests/Features/Utilities/Api.test.ts
+++ b/tests/KCC.ViteTests/Features/Utilities/Api.test.ts
@@ -1,5 +1,5 @@
 import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
-import { configureApi, del, get, post, put } from '~/Utilities/Api'
+import { antiforgeryToken, configureApi, del, get, post, put } from '~/Utilities/Api'
 
 const STRINGS = { unexpectedError: 'Something went wrong.', requestFailed: 'Request failed.' }
 
@@ -132,4 +132,12 @@
     expect(url).toBe('/api/search?q=mac&page=2')
     expect(init.method).toBe('GET')
   })
+
+  it('hands a native form the configured anti-forgery token, or nothing before one is configured', () => {
+    expect(antiforgeryToken()).toBe('tok')
+
+    configureApi({})
+
+    expect(antiforgeryToken()).toBe('')
+  })
 })
```

Apply to `tests/KCC.ViteTests/Features/Pages/Account/AccountView.test.ts`:

```diff
--- a/tests/KCC.ViteTests/Features/Pages/Account/AccountView.test.ts
+++ b/tests/KCC.ViteTests/Features/Pages/Account/AccountView.test.ts
@@ -102,7 +102,8 @@
     const html = await render()
 
     expect(tagWith(html, 'href="/account/settings"')).toContain('class="kcc-btn kcc-btn--ghost"')
-    expect(tagWith(html, 'href="/account/logout"')).toContain('class="kcc-btn kcc-btn--text"')
+    expect(tagWith(html, 'action="/account/logout"')).toContain('method="post"')
+    expect(html).toMatch(/action="\/account\/logout"[^>]*>.*?<button class="kcc-btn kcc-btn--text" type="submit">/s)
   })
 
   it('sets every recipe group on its own slip, never repeating a neighbour’s tear', async () => {
```

Apply to `tests/KCC.ViteTests/Features/Pages/Account/Settings/AccountSettingsView.test.ts`. The form-count test
now leaves the sign-out form out, because that form has a pill of its own:

```diff
--- a/tests/KCC.ViteTests/Features/Pages/Account/Settings/AccountSettingsView.test.ts
+++ b/tests/KCC.ViteTests/Features/Pages/Account/Settings/AccountSettingsView.test.ts
@@ -123,7 +123,8 @@
   })
 
   it('saves each form from a marker pill inside its own form', async () => {
-    const html = await render()
+    // The sign-out form has a pill of its own, checked in the next test.
+    const html = (await render()).replace(/<form[^>]*action="\/account\/logout"[\s\S]*?<\/form>/, '')
     const submits = html.match(/<button[^>]*type="submit"[^>]*>/g) ?? []
 
     expect(submits).toHaveLength(2)
@@ -137,7 +138,8 @@
     const html = await render()
 
     expect(tagWith(html, 'href="/account"')).toContain('class="kcc-btn kcc-btn--ghost"')
-    expect(tagWith(html, 'href="/account/logout"')).toContain('class="kcc-btn kcc-btn--ghost"')
+    expect(tagWith(html, 'action="/account/logout"')).toContain('method="post"')
+    expect(html).toMatch(/action="\/account\/logout"[^>]*>.*?<button class="kcc-btn kcc-btn--ghost" type="submit">/s)
     expect(html).not.toContain('text-danger-ink')
   })
 
```

```bash
cd src/KCC.Web && yarn test SignOutForm MenuItem Api AccountView AccountSettingsView; cd ../..
```

Expected: failures: `SignOutForm.vue` does not exist, `antiforgeryToken` is not exported, and the account pages
still link to `/account/logout`.

- [ ] **Step 2: Write the form and the token getter**

Create `src/KCC.Web/Features/Components/Account/SignOutForm.vue`:

```vue
<!-- #region SignOutForm Component Properties -->
<script lang="ts">
  /**
   * Signing out is a POST, so every "sign out" is this small form around its button.
   */
  export default {
    name: 'SignOutForm',
  }

  // The header's nav is content: its Logout entry is a link to this path, which the header swaps for the form.
  export const SIGN_OUT_PATH = '/account/logout'

  export const isSignOutUrl = (url?: string) => url?.replace(/\/+$/, '').toLowerCase() === SIGN_OUT_PATH

  export interface SignOutFormProps {
    action?: string
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  import { antiforgeryToken } from '~/Utilities/Api'
  const { action = SIGN_OUT_PATH } = defineProps<SignOutFormProps>()

  // The server render cannot know the token Layout.cshtml hands the client, so it is read as the form submits.
  const fillToken = (event: Event) => {
    const form = event.currentTarget as HTMLFormElement
    const field = form.elements.namedItem('__RequestVerificationToken') as HTMLInputElement
    field.value = antiforgeryToken()
  }
</script>

<template>
  <form method="post" :action="action" class="contents" @submit="fillToken">
    <input type="hidden" name="__RequestVerificationToken" value="" />
    <slot />
  </form>
</template>
```

`class="contents"` keeps the form out of the flex rows its button sits in. Apply to
`src/KCC.Web/Features/Utilities/Api.ts`:

```diff
--- a/src/KCC.Web/Features/Utilities/Api.ts
+++ b/src/KCC.Web/Features/Utilities/Api.ts
@@ -35,6 +35,11 @@
   }
 }
 
+// Sign-out posts a real form, which carries the token as a field rather than a header.
+export function antiforgeryToken(): string {
+  return config.antiforgeryToken ?? ''
+}
+
 export function get<T>(url: string, params?: Record<string, unknown>): Promise<ApiResult<T>> {
   const query = params ? `?${qs.stringify(params)}` : ''
   return request<T>(`${url}${query}`, { method: 'GET' })
```

- [ ] **Step 3: Use it in the three places**

Apply to `src/KCC.Web/Features/Components/Header/MenuItem.vue`:

```diff
--- a/src/KCC.Web/Features/Components/Header/MenuItem.vue
+++ b/src/KCC.Web/Features/Components/Header/MenuItem.vue
@@ -1,6 +1,7 @@
 <!-- #region MenuItem Component Properties -->
 <script lang="ts">
   import { computed, inject } from 'vue'
+  import SignOutForm, { isSignOutUrl } from '~/Components/Account/SignOutForm.vue'
   import { MENU_CONTROLLER_KEY } from '~/Components/Header/menuController'
 
   /**
@@ -54,8 +55,16 @@
   }
 </script>
 <template>
+  <SignOutForm v-if="isSignOutUrl(item.url)" :action="item.url">
+    <button
+      type="submit"
+      class="kcc-kick relative z-20 flex h-full w-full cursor-pointer items-center px-4 py-2 text-ink decoration-hair-strong underline-offset-[3px] hover:underline"
+    >
+      {{ item.displayText }}
+    </button>
+  </SignOutForm>
   <a
-    v-if="item.url"
+    v-else-if="item.url"
     :href="item.url"
     :target="item.target"
     class="kcc-kick relative z-20 flex h-full w-full cursor-pointer items-center px-4 py-2 text-ink decoration-hair-strong underline-offset-[3px] hover:underline"
@@ -92,7 +101,16 @@
               :key="subLink.displayText"
               class="basis-full rounded-md bg-paper-2 text-ink"
             >
+              <SignOutForm v-if="isSignOutUrl(subLink.url)" :action="subLink.url">
+                <button
+                  type="submit"
+                  class="block size-full cursor-pointer p-4 text-center decoration-hair-strong underline-offset-[3px] hover:underline"
+                >
+                  {{ subLink.displayText }}
+                </button>
+              </SignOutForm>
               <a
+                v-else
                 class="block size-full p-4 text-center decoration-hair-strong underline-offset-[3px] hover:underline"
                 :href="subLink.url"
                 :target="subLink.target"
```

Apply to `src/KCC.Web/Features/Pages/Account/AccountView.Component.vue`:

```diff
--- a/src/KCC.Web/Features/Pages/Account/AccountView.Component.vue
+++ b/src/KCC.Web/Features/Pages/Account/AccountView.Component.vue
@@ -49,6 +49,7 @@
 <!-- #endregion -->
 
 <script setup lang="ts">
+  import SignOutForm from '~/Components/Account/SignOutForm.vue'
   import Badge from '~/Components/Badge/Badge.vue'
   import Button from '~/Components/Button/Button.vue'
   import ComingSoonSection from '~/Components/ComingSoon/ComingSoonSection.vue'
@@ -90,9 +91,11 @@
         <Button as="a" :href="settingsUrl" variant="ghost">
           <ResourceString for="AccountSettings" />
         </Button>
-        <Button as="a" :href="logoutUrl" variant="text">
-          <ResourceString for="SignOut" />
-        </Button>
+        <SignOutForm :action="logoutUrl">
+          <Button type="submit" variant="text">
+            <ResourceString for="SignOut" />
+          </Button>
+        </SignOutForm>
       </div>
     </KccSheet>
 
```

Apply to `src/KCC.Web/Features/Pages/Account/Settings/AccountSettingsView.Component.vue`:

```diff
--- a/src/KCC.Web/Features/Pages/Account/Settings/AccountSettingsView.Component.vue
+++ b/src/KCC.Web/Features/Pages/Account/Settings/AccountSettingsView.Component.vue
@@ -35,6 +35,7 @@
 <!-- #endregion -->
 
 <script setup lang="ts">
+  import SignOutForm from '~/Components/Account/SignOutForm.vue'
   import Button from '~/Components/Button/Button.vue'
   import KccSheet from '~/Components/Sheet/KccSheet.vue'
   const props = defineProps<AccountSettingsViewProps>()
@@ -225,9 +226,11 @@
     <h2 class="sr-only">{{ rs('SignOut') }}</h2>
 
     <div class="flex justify-end">
-      <Button as="a" :href="logoutUrl" variant="ghost">
-        <ResourceString for="SignOut" />
-      </Button>
+      <SignOutForm :action="logoutUrl">
+        <Button type="submit" variant="ghost">
+          <ResourceString for="SignOut" />
+        </Button>
+      </SignOutForm>
     </div>
   </KccSheet>
 </template>
```

```bash
cd src/KCC.Web
yarn test SignOutForm MenuItem Api AccountView AccountSettingsView
yarn type-check
yarn prettier --check Features/Components/Account Features/Components/Header Features/Pages/Account Features/Utilities \
  ../../tests/KCC.ViteTests/Features/Components ../../tests/KCC.ViteTests/Features/Pages/Account ../../tests/KCC.ViteTests/Features/Utilities
cd ../..
```

Expected: the five specs pass: SignOutForm's 7, MenuItem's 3, and the account and Api specs with their new
checks. The type check passes. If Prettier flags a file, `yarn prettier --write` it and re-check.

- [ ] **Step 4: Write the failing string test**

Create `tests/KCC.IntegrationTests/Features/Baseline/AccountStringsTests.cs`:

```csharp
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Dictionary;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Baseline;

// A new account waits for the owner's approval, and members sign in by username.
public class AccountStringsTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    [Arguments("Login.NotAllowedError", "Your account is waiting for approval. You can sign in once the owner approves it.")]
    [Arguments("RegistrationComplete.Body", "Your account has been created and is waiting for approval. You can sign in once the owner approves it.")]
    [Arguments("Login.InvalidCredentialsError", "That username or password is incorrect.")]
    public async Task BaselineStrings_DescribeApprovalAndUsernames(string key, string expected)
    {
        using var scope = Site.Services.CreateScope();

        _ = await Assert.That(scope.ServiceProvider.GetRequiredService<IResourceStringProvider>().GetOrDefault(key)).IsEqualTo(expected);
    }
}
```

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/AccountStringsTests/*"
```

Expected: 3 failures, showing today's values ("…confirm your email address…", "…Check your email…",
"That email or password is incorrect.").

- [ ] **Step 5: Change the three strings in the baseline**

| Key | New value |
|---|---|
| `Login.NotAllowedError` | Your account is waiting for approval. You can sign in once the owner approves it. |
| `RegistrationComplete.Body` | Your account has been created and is waiting for approval. You can sign in once the owner approves it. |
| `Login.InvalidCredentialsError` | That username or password is incorrect. |

Find each key's uSync file, for example:

```bash
grep -rl 'Login.NotAllowedError' src/KCC.Web/uSync/v17/Dictionary
```

Replace the text of its `en-US` translation with the new value, keeping the escaping the file already uses (CDATA or
entities). If Phase 1 took its dictionary fallback, change the three values in
`src/KCC.Web/Features/Dictionary/baseline-strings.json` instead.

A fresh database imports the new values. An existing one keeps its old values, because the import only creates
missing keys (spec §12). In the dev backoffice, change them under Translation → Dictionary. The export on save
writes the same text back to the file.

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/AccountStringsTests/*"
```

Expected: 3 passed.

- [ ] **Step 6: Build both bundles and commit**

```bash
(cd src/KCC.Web && yarn build:all && yarn test)
git add -A src/KCC.Web tests/KCC.ViteTests tests/KCC.IntegrationTests
git commit -m "Sign Out Through a Posted Form and Say Accounts Wait for Approval"
```

Expected: both bundles build and every Vitest spec passes.

---

### Task 7: Review, cook-note and cooked writes

The variant page's review editor, cook-note box and "I cooked this" toggle get their write endpoints back. Their
routes and response shapes are Kentico's, so the Vue components do not change:
- `PUT` and `DELETE /api/variant/{guid}/review`;
- `POST /api/variant/{guid}/note` and `DELETE /api/note/{id}`;
- `POST` and `DELETE /api/variant/{guid}/cooked`.

Every write goes through `ContributionWrites`, whose new `WriteAsync` helper takes the contributions lock before
the first read. Review writes and review deletes publish `ReviewsChangedNotification`, and a cooked mark only
invalidates the stats, since the index does not use cooked counts. A write needs a signed-in member and a published
variant. It is anti-forgery-checked and limited to 30 a minute per client. A member has at most one review and one
cooked mark per variant: the upsert and the mark check first, inside the lock, and the unique indexes back them up.
The concurrency test grows to race all of it.

**Files:**
- Modify: `src/KCC.Contributions/ContributionWrites.cs`, `src/KCC.Web/Features/Recipes/RecipeQueries.cs`,
  `src/KCC.Web/Features/Api/ContributionResponses.cs`, `tests/KCC.UnitTests/Features/Api/{ReviewListTests,CookNoteListTests}.cs`,
  `tests/KCC.IntegrationTests/Features/Sqlite/SqliteConcurrencyTests.cs`, `src/KCC.Web/KCC.Web.csproj`,
  `tests/KCC.UnitTests/KCC.UnitTests.csproj`
- Rewrite: `src/KCC.Web/Features/Api/{ReviewApiController,CookNoteApiController,VariantCookedApiController}.cs`
- Create: `tests/KCC.IntegrationTests/Features/Api/ContributionWriteApiTests.cs`
- Delete: `tests/KCC.UnitTests/Features/Api/{ReviewApiControllerTests,CookNoteApiControllerTests,VariantCookedApiControllerTests}.cs`

**Interfaces:**
- Consumes: `MemberClient`, `TestMembers`, `RateLimits.Contributions`, `IMemberWriteLock` (the concurrency test).
- Produces:
  - `IContributionWrites`, with:
    - `DeleteReviewAsync(Guid variantKey, Guid memberKey) → Task<bool>`;
    - `AddNoteAsync(Guid variantKey, Guid memberKey, string text) → Task<int>`, the note's id;
    - `DeleteOwnNoteAsync(int noteId, Guid memberKey) → Task<bool>`, false when the note is missing or someone
      else's;
    - `MarkCookedAsync(Guid variantKey, Guid memberKey) → Task`, idempotent;
    - `UnmarkCookedAsync(Guid variantKey, Guid memberKey) → Task`.
  - `IRecipeQueries.IsPublishedVariant(Guid key) → bool`.
  - `ReviewRequest(decimal Rating, string Text)` and `CookedResponse(int CookedCount, bool HasCooked)`.
  - The endpoints above. A review delete with nothing to delete answers 404, and deleting someone else's note
    answers 403, as on Kentico.

- [ ] **Step 1: Write the failing tests**

Create `tests/KCC.IntegrationTests/Features/Api/ContributionWriteApiTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Api;

// Each test writes to a variant it made, so no count here depends on another test.
public class ContributionWriteApiTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Review_IsWrittenEditedAndDeleted_AsOneReviewPerMember()
    {
        var variant = await VariantAsync("IT Narwhal");
        using var member = await SignedInAsync();

        await ExpectOkAsync(await member.PutAsync($"/api/variant/{variant}/review", new { rating = 4.5, text = "Crisp edges." }));
        await ExpectOkAsync(await member.PutAsync($"/api/variant/{variant}/review", new { rating = 3, text = "Softer the next day." }));
        var reviews = await ReviewsAsync(member, variant);

        _ = await Assert.That(reviews.GetProperty("total").GetInt32()).IsEqualTo(1);
        _ = await Assert.That(reviews.GetProperty("myReview").GetProperty("rating").GetDecimal()).IsEqualTo(3m);
        _ = await Assert.That(reviews.GetProperty("myReview").GetProperty("text").GetString()).IsEqualTo("Softer the next day.");

        await ExpectOkAsync(await member.DeleteAsync($"/api/variant/{variant}/review"));
        using var again = await member.DeleteAsync($"/api/variant/{variant}/review");

        _ = await Assert.That((await ReviewsAsync(member, variant)).GetProperty("total").GetInt32()).IsEqualTo(0);
        _ = await Assert.That(again.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Review_ReachesTheSearchIndex()
    {
        var variant = await VariantAsync("IT Quetzal");
        using var member = await SignedInAsync();

        await ExpectOkAsync(await member.PutAsync($"/api/variant/{variant}/review", new { rating = 5, text = "Worth the wait." }));
        await Site.Services.GetRequiredService<IRecipeIndexRebuilder>().WhenCurrentAsync(CancellationToken.None);

        var hit = Site.Services.GetRequiredService<IRecipeSearchService>().Search(new RecipeSearchCriteria { Query = "quetzal" }).Results.Single();
        _ = await Assert.That(hit.ReviewCount).IsEqualTo(1);
        _ = await Assert.That(hit.AverageRating).IsEqualTo(5d);
    }

    [Test]
    [Arguments(0)]
    [Arguments(5.5)]
    [Arguments(3.25)]
    public async Task Review_WithARatingOffTheHalfStarScale_IsRefused(decimal rating)
    {
        var variant = await VariantAsync("IT Marmot");
        using var member = await SignedInAsync();

        using var response = await member.PutAsync($"/api/variant/{variant}/review", new { rating, text = "Hmm." });

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Writes_ToAnUnknownOrUnpublishedVariant_AreNotFound()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Ibex");
        var withdrawn = await TestContent.VariantAsync(Site.Services, recipe, "Withdrawn");
        await TestContent.UnpublishAsync(Site.Services, withdrawn);
        using var member = await SignedInAsync();

        using var unknown = await member.PutAsync($"/api/variant/{Guid.NewGuid()}/review", new { rating = 4, text = "Lost." });
        using var unpublished = await member.PostAsync($"/api/variant/{withdrawn}/cooked");
        using var note = await member.PostAsync($"/api/variant/{withdrawn}/note", "Too late.");

        _ = await Assert.That(unknown.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        _ = await Assert.That(unpublished.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        _ = await Assert.That(note.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Writes_SignedOut_AreUnauthorized()
    {
        var variant = await VariantAsync("IT Gibbon");
        using var visitor = new MemberClient(Site);

        using var review = await visitor.PutAsync($"/api/variant/{variant}/review", new { rating = 4, text = "Anonymous." });
        using var note = await visitor.PostAsync($"/api/variant/{variant}/note", "Anonymous.");
        using var cooked = await visitor.PostAsync($"/api/variant/{variant}/cooked");

        _ = await Assert.That(review.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        _ = await Assert.That(note.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        _ = await Assert.That(cooked.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CookNote_IsAddedAndOnlyItsAuthorCanDeleteIt()
    {
        var variant = await VariantAsync("IT Capybara");
        using var author = await SignedInAsync();
        using var neighbour = await SignedInAsync();

        using var added = await author.PostAsync($"/api/variant/{variant}/note", "  Rest the dough overnight.  ");
        var id = (await added.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var notes = await (await author.Http.GetAsync($"/api/variant/{variant}/notes")).Content.ReadFromJsonAsync<JsonElement>();
        using var blank = await author.PostAsync($"/api/variant/{variant}/note", "   ");
        using var stolen = await neighbour.DeleteAsync($"/api/note/{id}");

        _ = await Assert.That(notes.GetProperty("notes")[0].GetProperty("text").GetString()).IsEqualTo("Rest the dough overnight.");
        _ = await Assert.That(notes.GetProperty("notes")[0].GetProperty("isMine").GetBoolean()).IsTrue();
        _ = await Assert.That(blank.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        _ = await Assert.That(stolen.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

        await ExpectOkAsync(await author.DeleteAsync($"/api/note/{id}"));
        var after = await (await author.Http.GetAsync($"/api/variant/{variant}/notes")).Content.ReadFromJsonAsync<JsonElement>();
        _ = await Assert.That(after.GetProperty("total").GetInt32()).IsEqualTo(0);
    }

    [Test]
    public async Task Cooked_CountsEachMemberOnce()
    {
        var variant = await VariantAsync("IT Lemur");
        using var member = await SignedInAsync();
        using var neighbour = await SignedInAsync();

        var first = await CookedAsync(await member.PostAsync($"/api/variant/{variant}/cooked"));
        var twice = await CookedAsync(await member.PostAsync($"/api/variant/{variant}/cooked"));
        var second = await CookedAsync(await neighbour.PostAsync($"/api/variant/{variant}/cooked"));
        var undone = await CookedAsync(await member.DeleteAsync($"/api/variant/{variant}/cooked"));

        _ = await Assert.That(first).IsEqualTo((1, true));
        _ = await Assert.That(twice).IsEqualTo((1, true));
        _ = await Assert.That(second).IsEqualTo((2, true));
        _ = await Assert.That(undone).IsEqualTo((1, false));
    }

    [Test]
    public async Task ContributionWrites_AllowThirtyAMinutePerClient()
    {
        var variant = await VariantAsync("IT Ibis");
        using var member = await SignedInAsync();
        for (var write = 0; write < 30; write++)
        {
            using var allowed = await member.PostAsync($"/api/variant/{variant}/cooked");
            _ = await Assert.That(allowed.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }

        using var limited = await member.PutAsync($"/api/variant/{variant}/review", new { rating = 4, text = "One too many." });

        _ = await Assert.That(limited.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
    }

    private static async Task ExpectOkAsync(HttpResponseMessage response)
    {
        using (response)
        {
            _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }
    }

    private static async Task<JsonElement> ReviewsAsync(MemberClient member, Guid variant) =>
        await (await member.Http.GetAsync($"/api/variant/{variant}/reviews")).Content.ReadFromJsonAsync<JsonElement>();

    private static async Task<(int Count, bool HasCooked)> CookedAsync(HttpResponseMessage response)
    {
        using (response)
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return (body.GetProperty("cookedCount").GetInt32(), body.GetProperty("hasCooked").GetBoolean());
        }
    }

    private async Task<Guid> VariantAsync(string recipeName)
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, recipeName);
        return await TestContent.VariantAsync(Site.Services, recipe, "Classic");
    }

    private async Task<MemberClient> SignedInAsync()
    {
        var userName = TestMembers.UniqueUserName("writer");
        await TestMembers.ApprovedAsync(Site.Services, userName);
        var member = new MemberClient(Site);
        _ = await member.SignInAsync(userName, TestMembers.Password);
        return member;
    }
}
```

Replace `tests/KCC.IntegrationTests/Features/Sqlite/SqliteConcurrencyTests.cs` with this version, which also marks,
unmarks, notes and deletes, and checks every count:

```csharp
using System.Collections.Concurrent;
using KCC.Contributions;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Search;
using KCC.Web.Features.Sqlite;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace KCC.IntegrationTests.Features.Sqlite;

// Spec §14: parallel review writes, a content save and an index rebuild, repeated, with no lock errors and consistent
// counts. Cooked marks, cook notes and deletes race alongside, and so do members signing up, signing in and being
// approved, because a member save is followed by Umbraco's relations update, which is the write that stalled on SQLite.
public class SqliteConcurrencyTests
{
    private const int Rounds = 5;
    private const int Writers = 4;
    private const int WritesPerWriter = 50;
    private const int MemberRepeats = 5;

    // Every write here takes well under a second. A transaction stuck on a stale snapshot retries for minutes, so a
    // round that outlives this limit is a lock failure.
    private static readonly TimeSpan RoundLimit = TimeSpan.FromSeconds(60);

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task ParallelWrites_FinishAndLeaveConsistentCounts()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Axolotl");
        var variant = await TestContent.VariantAsync(Site.Services, recipe, "Stress Test");
        var members = new List<string>();
        for (var index = 0; index < 3; index++)
        {
            var userName = TestMembers.UniqueUserName("race");
            await TestMembers.ApprovedAsync(Site.Services, userName);
            members.Add(userName);
        }

        var reviewers = new ConcurrentDictionary<Guid, bool>();
        var cooks = new ConcurrentDictionary<Guid, bool>();
        var notes = new ConcurrentBag<int>();
        var rebuilder = Site.Services.GetRequiredService<IRecipeIndexRebuilder>();

        for (var round = 0; round < Rounds; round++)
        {
            var edit = round;
            var work = new List<Task>();
            work.AddRange(Enumerable.Range(0, Writers).Select(_ => Task.Run(() => WriteContributionsAsync(variant, reviewers, cooks, notes))));
            work.AddRange(members.Select(userName => Task.Run(() => SignInRepeatedlyAsync(userName))));
            work.Add(Task.Run(() => SignUpAndApproveAsync()));
            work.Add(Task.Run(() => EditAndPublishAsync(recipe, $"Edited in round {edit}.")));
            work.Add(Task.Run(rebuilder.Signal));

            await Task.WhenAll(work).WaitAsync(RoundLimit);
            await rebuilder.WhenCurrentAsync(CancellationToken.None);

            var reads = Site.Services.GetRequiredService<IContributionReads>();
            var stats = (await Site.Services.GetRequiredService<IContributionStats>().GetAsync()).For(variant);
            _ = await Assert.That((await reads.ReviewsAsync(variant, 0, 1)).Total).IsEqualTo(reviewers.Count);
            _ = await Assert.That(stats.ReviewCount).IsEqualTo(reviewers.Count);
            _ = await Assert.That(IndexedReviewCount("axolotl")).IsEqualTo(reviewers.Count);
            _ = await Assert.That(stats.CookedCount).IsEqualTo(cooks.Count);
            _ = await Assert.That((await reads.NotesAsync(variant, 0, 1)).Total).IsEqualTo(notes.Count);
        }
    }

    // Every fourth member takes their review and their cooked mark back, so deletes race the inserts too.
    private async Task WriteContributionsAsync(
        Guid variant,
        ConcurrentDictionary<Guid, bool> reviewers,
        ConcurrentDictionary<Guid, bool> cooks,
        ConcurrentBag<int> notes)
    {
        var writes = Site.Services.GetRequiredService<IContributionWrites>();
        for (var index = 0; index < WritesPerWriter; index++)
        {
            var member = Guid.NewGuid();
            await writes.UpsertReviewAsync(variant, member, 4.5m, "Held up under load.");
            reviewers[member] = true;
            await writes.MarkCookedAsync(variant, member);
            cooks[member] = true;
            notes.Add(await writes.AddNoteAsync(variant, member, "Noted under load."));
            if (index % 4 == 0)
            {
                _ = await writes.DeleteReviewAsync(variant, member);
                reviewers.TryRemove(member, out _);
                await writes.UnmarkCookedAsync(variant, member);
                cooks.TryRemove(member, out _);
            }
        }
    }

    // The member saves a successful sign-in, a failed attempt and a sign-out make, in the lock the account
    // endpoints take.
    private async Task SignInRepeatedlyAsync(string userName)
    {
        for (var repeat = 0; repeat < MemberRepeats; repeat++)
        {
            using var scope = Site.Services.CreateScope();
            var members = scope.ServiceProvider.GetRequiredService<UserManager<MemberIdentityUser>>();
            await scope.ServiceProvider.GetRequiredService<IMemberWriteLock>().RunAsync(async () =>
            {
                var member = await members.FindByNameAsync(userName) ?? throw new InvalidOperationException($"No member {userName}.");
                member.LastLoginDate = DateTime.UtcNow;
                Succeeded(await members.UpdateAsync(member));
                Succeeded(await members.AccessFailedAsync(member));
                Succeeded(await members.ResetAccessFailedCountAsync(member));
                Succeeded(await members.UpdateSecurityStampAsync(member));
            });
        }
    }

    private async Task SignUpAndApproveAsync()
    {
        var userName = TestMembers.UniqueUserName("joiner");
        Guid key;
        using (var scope = Site.Services.CreateScope())
        {
            var members = scope.ServiceProvider.GetRequiredService<IMemberManager>();
            var member = MemberIdentityUser.CreateNew(userName, $"{userName}@example.test", Constants.Security.DefaultMemberTypeAlias, isApproved: false, userName);
            Succeeded(await scope.ServiceProvider.GetRequiredService<IMemberWriteLock>().RunAsync(() => members.CreateAsync(member, TestMembers.Password)));
            key = member.Key;
        }

        await TestMembers.ApproveAsync(Site.Services, key);
    }

    // The owner's Save and Publish in the backoffice goes through these two services, outside any lock of ours.
    private async Task EditAndPublishAsync(Guid recipe, string description)
    {
        using var scope = Site.Services.CreateScope();
        var updated = await scope.ServiceProvider.GetRequiredService<IContentEditingService>().UpdateAsync(
            recipe,
            new ContentUpdateModel
            {
                Variants = [new VariantModel { Name = "IT Axolotl" }],
                Properties =
                [
                    new PropertyValueModel { Alias = "description", Value = description },
                    new PropertyValueModel { Alias = "icon", Value = "fa-duotone fa-egg" },
                ],
            },
            Constants.Security.SuperUserKey);
        if (updated.Status != ContentEditingOperationStatus.Success)
        {
            throw new InvalidOperationException($"Editing the recipe failed: {updated.Status}.");
        }

        var published = await scope.ServiceProvider.GetRequiredService<IContentPublishingService>()
            .PublishAsync(recipe, [new CulturePublishScheduleModel { Culture = null }], Constants.Security.SuperUserKey);
        if (!published.Success)
        {
            throw new InvalidOperationException($"Publishing the recipe failed: {published.Status}.");
        }
    }

    private int IndexedReviewCount(string word) =>
        Site.Services.GetRequiredService<IRecipeSearchService>().Search(new RecipeSearchCriteria { Query = word }).Results.Single().ReviewCount;

    private static void Succeeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }
}
```

```bash
dotnet build KitchenCommandCenter.sln
```

Expected: build errors — `IContributionWrites` has no `DeleteReviewAsync`, `MarkCookedAsync`, `UnmarkCookedAsync` or
`AddNoteAsync`.

- [ ] **Step 2: Grow the store**

Replace `src/KCC.Contributions/ContributionWrites.cs` with:

```csharp
using KCC.Contributions.Data;
using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace KCC.Contributions;

public interface IContributionWrites
{
    Task UpsertReviewAsync(Guid variantKey, Guid memberKey, decimal rating, string text);

    Task<bool> DeleteReviewAsync(Guid variantKey, Guid memberKey);

    Task<int> AddNoteAsync(Guid variantKey, Guid memberKey, string text);

    Task<bool> DeleteOwnNoteAsync(int noteId, Guid memberKey);

    Task MarkCookedAsync(Guid variantKey, Guid memberKey);

    Task UnmarkCookedAsync(Guid variantKey, Guid memberKey);
}

public sealed class ContributionWrites(
    IEFCoreScopeProvider<ContributionsDbContext> scopes,
    IContributionStats stats,
    IEventAggregator eventAggregator) : IContributionWrites
{
    public async Task UpsertReviewAsync(Guid variantKey, Guid memberKey, decimal rating, string text)
    {
        if (!RatingMath.IsValidRating(rating))
        {
            throw new ArgumentOutOfRangeException(nameof(rating), rating, "A rating runs from 0.5 to 5 in half-star steps.");
        }

        await WriteAsync(async db =>
        {
            var now = DateTime.UtcNow;
            var review = await db.Reviews.FirstOrDefaultAsync(r => r.VariantKey == variantKey && r.MemberKey == memberKey);
            if (review is null)
            {
                review = new Review { VariantKey = variantKey, MemberKey = memberKey, Created = now };
                db.Reviews.Add(review);
            }

            review.Rating = rating;
            review.Text = RatingMath.ClampText(text);
            review.Modified = now;
            return await db.SaveChangesAsync();
        });
        await ReviewsChangedAsync();
    }

    public async Task<bool> DeleteReviewAsync(Guid variantKey, Guid memberKey)
    {
        var deleted = await WriteAsync(db => db.Reviews
            .Where(review => review.VariantKey == variantKey && review.MemberKey == memberKey)
            .ExecuteDeleteAsync());
        if (deleted > 0)
        {
            await ReviewsChangedAsync();
        }

        return deleted > 0;
    }

    public Task<int> AddNoteAsync(Guid variantKey, Guid memberKey, string text)
    {
        var clamped = RatingMath.ClampText(text) ?? throw new ArgumentException("A cook note needs text.", nameof(text));
        return WriteAsync(async db =>
        {
            var now = DateTime.UtcNow;
            var note = new CookNote { VariantKey = variantKey, MemberKey = memberKey, Text = clamped, Created = now, Modified = now };
            db.CookNotes.Add(note);
            await db.SaveChangesAsync();
            return note.Id;
        });
    }

    public async Task<bool> DeleteOwnNoteAsync(int noteId, Guid memberKey) =>
        await WriteAsync(db => db.CookNotes.Where(note => note.Id == noteId && note.MemberKey == memberKey).ExecuteDeleteAsync()) > 0;

    public async Task MarkCookedAsync(Guid variantKey, Guid memberKey)
    {
        await WriteAsync(async db =>
        {
            if (await db.CookedMarks.AnyAsync(mark => mark.VariantKey == variantKey && mark.MemberKey == memberKey))
            {
                return 0;
            }

            db.CookedMarks.Add(new CookedMark { VariantKey = variantKey, MemberKey = memberKey, Created = DateTime.UtcNow });
            return await db.SaveChangesAsync();
        });
        stats.Invalidate();
    }

    public async Task UnmarkCookedAsync(Guid variantKey, Guid memberKey)
    {
        await WriteAsync(db => db.CookedMarks
            .Where(mark => mark.VariantKey == variantKey && mark.MemberKey == memberKey)
            .ExecuteDeleteAsync());
        stats.Invalidate();
    }

    private async Task<T> WriteAsync<T>(Func<ContributionsDbContext, Task<T>> write)
    {
        using var scope = scopes.CreateScope();

        // Taken before the first read, so this transaction is already the writer when it looks at existing rows:
        // SQLite cannot upgrade a reader whose snapshot another writer has moved past.
        scope.WriteLock(ContributionLocks.Contributions);
        var result = await scope.ExecuteWithContextAsync(write);
        scope.Complete();
        return result;
    }

    private async Task ReviewsChangedAsync()
    {
        stats.Invalidate();
        await eventAggregator.PublishAsync(new ReviewsChangedNotification());
    }
}
```

`UpsertReviewAsync` behaves as before. Its lock comment moves into `WriteAsync`, which every write now shares, and
the deletes use `ExecuteDeleteAsync`, which runs inside the same transaction.

- [ ] **Step 3: The variant check, the shapes and the controllers**

Apply to `src/KCC.Web/Features/Recipes/RecipeQueries.cs`:

```diff
--- a/src/KCC.Web/Features/Recipes/RecipeQueries.cs
+++ b/src/KCC.Web/Features/Recipes/RecipeQueries.cs
@@ -14,6 +14,8 @@
     IReadOnlyList<RecipePageData> GetPublishedRecipes();
 
     string GetCreateRecipeUrl(RecipeListingPage listing);
+
+    bool IsPublishedVariant(Guid key);
 }
 
 public class RecipeQueries(IPublishedContentQuery contentQuery) : IRecipeQueries
@@ -48,6 +50,8 @@
     public string GetCreateRecipeUrl(RecipeListingPage listing) =>
         listing.Children<CreateRecipePage>().FirstOrDefault()?.Url();
 
+    public bool IsPublishedVariant(Guid key) => contentQuery.Content(key) is RecipeVariant;
+
     private static RecipeRecord RecipeFrom(Recipe recipe) => new(
         recipe.Key,
         recipe.Name,
```

Apply to `src/KCC.Web/Features/Api/ContributionResponses.cs`:

```diff
--- a/src/KCC.Web/Features/Api/ContributionResponses.cs
+++ b/src/KCC.Web/Features/Api/ContributionResponses.cs
@@ -17,3 +17,7 @@
 public sealed record CookNotesResponse(int Total, int Page, int PageSize, IReadOnlyList<CookNoteItem> Notes);
 
 public sealed record CookNoteItem(int Id, string AuthorName, string Text, DateTime Created, bool IsMine);
+
+public sealed record ReviewRequest(decimal Rating, string Text);
+
+public sealed record CookedResponse(int CookedCount, bool HasCooked);
```

Replace `src/KCC.Web/Features/Api/ReviewApiController.cs` with:

```csharp
using KCC.Contributions;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Recipes;
using KCC.Web.Features.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Umbraco.Cms.Core.Security;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api/variant")]
[AutoValidateAntiforgeryToken]
public class ReviewApiController(
    IContributionStats contributionStats,
    IContributionReads contributionReads,
    IContributionWrites contributionWrites,
    IRecipeQueries recipes,
    IAuthorNameProvider authorNames,
    IMemberManager memberManager) : ControllerBase
{
    [HttpGet("{variantGuid:guid}/reviews")]
    public async Task<ActionResult<ReviewsResponse>> GetReviews(Guid variantGuid, int page = 0, int pageSize = 10)
    {
        var stats = (await contributionStats.GetAsync()).For(variantGuid);
        var memberKey = (await memberManager.GetCurrentMemberAsync())?.Key;
        var reviews = await contributionReads.ReviewsAsync(variantGuid, page, pageSize);
        var names = await authorNames.ResolveMany(reviews.Items.Select(review => review.MemberKey));
        var mine = memberKey is { } key ? await contributionReads.MemberReviewAsync(variantGuid, key) : null;

        return new ReviewsResponse(
            stats.Rating.Average,
            stats.Rating.Count,
            stats.Distribution,
            reviews.Total,
            Math.Max(0, page),
            Math.Clamp(pageSize, 1, ContributionReads.MaxPageSize),
            reviews.Items.Select(review => new ReviewItem(
                names.GetValueOrDefault(review.MemberKey) ?? AuthorNameProvider.DeletedMemberName,
                review.Rating,
                review.Text,
                review.Created,
                review.MemberKey == memberKey)).ToList(),
            mine is null ? null : new MyReview(mine.Rating, mine.Text));
    }

    [HttpPut("{variantGuid:guid}/review")]
    [EnableRateLimiting(RateLimits.Contributions)]
    public async Task<IActionResult> UpsertReview(Guid variantGuid, [FromBody] ReviewRequest request)
    {
        if (request is null || !RatingMath.IsValidRating(request.Rating))
        {
            return BadRequest(new { error = "Rating must be between 0.5 and 5 in half-star steps." });
        }

        if (await memberManager.GetCurrentMemberAsync() is not { } member)
        {
            return Unauthorized();
        }

        if (!recipes.IsPublishedVariant(variantGuid))
        {
            return NotFound(new { error = "Variant not found." });
        }

        await contributionWrites.UpsertReviewAsync(variantGuid, member.Key, request.Rating, request.Text);
        return Ok(new { success = true });
    }

    [HttpDelete("{variantGuid:guid}/review")]
    [EnableRateLimiting(RateLimits.Contributions)]
    public async Task<IActionResult> DeleteReview(Guid variantGuid)
    {
        if (await memberManager.GetCurrentMemberAsync() is not { } member)
        {
            return Unauthorized();
        }

        return await contributionWrites.DeleteReviewAsync(variantGuid, member.Key)
            ? Ok(new { success = true })
            : NotFound(new { error = "No review to delete." });
    }
}
```

Replace `src/KCC.Web/Features/Api/CookNoteApiController.cs` with:

```csharp
using KCC.Contributions;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Recipes;
using KCC.Web.Features.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Umbraco.Cms.Core.Security;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api")]
[AutoValidateAntiforgeryToken]
public class CookNoteApiController(
    IContributionReads contributionReads,
    IContributionWrites contributionWrites,
    IRecipeQueries recipes,
    IAuthorNameProvider authorNames,
    IMemberManager memberManager) : ControllerBase
{
    [HttpGet("variant/{variantGuid:guid}/notes")]
    public async Task<ActionResult<CookNotesResponse>> GetNotes(Guid variantGuid, int page = 0, int pageSize = 10)
    {
        var notes = await contributionReads.NotesAsync(variantGuid, page, pageSize);
        var names = await authorNames.ResolveMany(notes.Items.Select(note => note.MemberKey));
        var memberKey = (await memberManager.GetCurrentMemberAsync())?.Key;

        return new CookNotesResponse(
            notes.Total,
            Math.Max(0, page),
            Math.Clamp(pageSize, 1, ContributionReads.MaxPageSize),
            notes.Items.Select(note => new CookNoteItem(
                note.Id,
                names.GetValueOrDefault(note.MemberKey) ?? AuthorNameProvider.DeletedMemberName,
                note.Text,
                note.Created,
                note.MemberKey == memberKey)).ToList());
    }

    [HttpPost("variant/{variantGuid:guid}/note")]
    [EnableRateLimiting(RateLimits.Contributions)]
    public async Task<IActionResult> AddNote(Guid variantGuid, [FromBody] string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return BadRequest(new { error = "Note text is required." });
        }

        if (await memberManager.GetCurrentMemberAsync() is not { } member)
        {
            return Unauthorized();
        }

        if (!recipes.IsPublishedVariant(variantGuid))
        {
            return NotFound(new { error = "Variant not found." });
        }

        return Ok(new { id = await contributionWrites.AddNoteAsync(variantGuid, member.Key, text) });
    }

    [HttpDelete("note/{id:int}")]
    [EnableRateLimiting(RateLimits.Contributions)]
    public async Task<IActionResult> DeleteNote(int id)
    {
        if (await memberManager.GetCurrentMemberAsync() is not { } member)
        {
            return Unauthorized();
        }

        return await contributionWrites.DeleteOwnNoteAsync(id, member.Key)
            ? Ok(new { success = true })
            : StatusCode(StatusCodes.Status403Forbidden, new { error = "You can only delete your own note." });
    }
}
```

Replace `src/KCC.Web/Features/Api/VariantCookedApiController.cs` with:

```csharp
using KCC.Contributions;
using KCC.Web.Features.Recipes;
using KCC.Web.Features.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Umbraco.Cms.Core.Security;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api/variant")]
[AutoValidateAntiforgeryToken]
[EnableRateLimiting(RateLimits.Contributions)]
public class VariantCookedApiController(
    IContributionWrites contributionWrites,
    IContributionStats contributionStats,
    IRecipeQueries recipes,
    IMemberManager memberManager) : ControllerBase
{
    [HttpPost("{variantGuid:guid}/cooked")]
    public async Task<IActionResult> MarkCooked(Guid variantGuid)
    {
        if (await memberManager.GetCurrentMemberAsync() is not { } member)
        {
            return Unauthorized();
        }

        if (!recipes.IsPublishedVariant(variantGuid))
        {
            return NotFound(new { error = "Variant not found." });
        }

        await contributionWrites.MarkCookedAsync(variantGuid, member.Key);
        return Ok(new CookedResponse((await contributionStats.GetAsync()).For(variantGuid).CookedCount, HasCooked: true));
    }

    [HttpDelete("{variantGuid:guid}/cooked")]
    public async Task<IActionResult> UnmarkCooked(Guid variantGuid)
    {
        if (await memberManager.GetCurrentMemberAsync() is not { } member)
        {
            return Unauthorized();
        }

        await contributionWrites.UnmarkCookedAsync(variantGuid, member.Key);
        return Ok(new CookedResponse((await contributionStats.GetAsync()).For(variantGuid).CookedCount, HasCooked: false));
    }
}
```

`AutoValidateAntiforgeryToken` checks the POST, PUT and DELETE actions and leaves the GETs alone.

The two list tests build the controllers, so they pass the new dependencies. Apply to
`tests/KCC.UnitTests/Features/Api/ReviewListTests.cs` and `CookNoteListTests.cs`:

```diff
--- a/tests/KCC.UnitTests/Features/Api/ReviewListTests.cs
+++ b/tests/KCC.UnitTests/Features/Api/ReviewListTests.cs
@@ -2,6 +2,7 @@
 using KCC.Contributions.Data;
 using KCC.Web.Features.Api;
 using KCC.Web.Features.Providers;
+using KCC.Web.Features.Recipes;
 using Moq;
 using Umbraco.Cms.Core.Security;
 
@@ -95,6 +96,6 @@
         members.Setup(m => m.GetCurrentMemberAsync())
             .ReturnsAsync(memberKey is { } key ? new MemberIdentityUser { Key = key } : null);
 
-        return new ReviewApiController(stats.Object, reads.Object, authors.Object, members.Object);
+        return new ReviewApiController(stats.Object, reads.Object, Mock.Of<IContributionWrites>(), Mock.Of<IRecipeQueries>(), authors.Object, members.Object);
     }
 }
```

```diff
--- a/tests/KCC.UnitTests/Features/Api/CookNoteListTests.cs
+++ b/tests/KCC.UnitTests/Features/Api/CookNoteListTests.cs
@@ -2,6 +2,7 @@
 using KCC.Contributions.Data;
 using KCC.Web.Features.Api;
 using KCC.Web.Features.Providers;
+using KCC.Web.Features.Recipes;
 using Moq;
 using Umbraco.Cms.Core.Security;
 
@@ -58,6 +59,6 @@
         members.Setup(m => m.GetCurrentMemberAsync())
             .ReturnsAsync(memberKey is { } key ? new MemberIdentityUser { Key = key } : null);
 
-        return new CookNoteApiController(reads.Object, authors.Object, members.Object);
+        return new CookNoteApiController(reads.Object, Mock.Of<IContributionWrites>(), Mock.Of<IRecipeQueries>(), authors.Object, members.Object);
     }
 }
```

In `src/KCC.Web/KCC.Web.csproj`, delete `<Compile Remove="Features/Api/VariantCookedApiController.cs" />`. The
Kentico write tests mocked the old providers and are replaced by `ContributionWriteApiTests`:

```bash
git rm -q tests/KCC.UnitTests/Features/Api/ReviewApiControllerTests.cs tests/KCC.UnitTests/Features/Api/CookNoteApiControllerTests.cs \
  tests/KCC.UnitTests/Features/Api/VariantCookedApiControllerTests.cs
```

In `tests/KCC.UnitTests/KCC.UnitTests.csproj`, delete their three `Compile Remove` lines.

- [ ] **Step 4: Run the tests**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/ContributionWriteApiTests/*"
for run in 1 2 3; do
  dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/SqliteConcurrencyTests/*"
done
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected:
- 0 warnings, and every unit test passes, `ReviewListTests` and `CookNoteListTests` included;
- 10 passed;
- the concurrency test passes three times;
- the whole integration suite is green, Phase 3's search suites included, since review writes still reach the index.

- [ ] **Step 5: Commit**

```bash
git add -A src/KCC.Contributions src/KCC.Web tests/KCC.UnitTests tests/KCC.IntegrationTests
git commit -m "Write Reviews, Cook Notes and Cooked Marks Again"
```

---

### Task 8: Cascades

Spec §9.1: deleting a variant permanently, or the recipe above it, deletes its reviews, notes and cooked marks, and
deleting a member deletes theirs. A variant in the recycle bin keeps its rows, so restoring it restores its reviews;
while it is trashed, the published-only ratings already ignore them. Umbraco publishes `ContentDeletedNotification`
for every node a permanent delete removes, descendants included, and for everything an emptied recycle bin removes.
Only variants have rows, so the cascade deletes by every deleted key without asking what each node was.

**Files:**
- Create: `src/KCC.Contributions/ContributionCascades.cs`,
  `tests/KCC.IntegrationTests/Features/Contributions/ContributionCascadeTests.cs`
- Modify: `src/KCC.Contributions/ContributionWrites.cs`, `src/KCC.Contributions/ContributionsComposer.cs`

**Interfaces:**
- Consumes: `IContributionWrites` (Task 7), `TestContent`, `TestMembers`.
- Produces `IContributionWrites.DeleteForVariantsAsync(IReadOnlyCollection<Guid> variantKeys)` and
  `DeleteForMembersAsync(IReadOnlyCollection<Guid> memberKeys)`. Each publishes `ReviewsChangedNotification` when it
  deleted a review; otherwise it only invalidates the stats.

- [ ] **Step 1: Write the failing tests**

Create `tests/KCC.IntegrationTests/Features/Contributions/ContributionCascadeTests.cs`:

```csharp
using KCC.Contributions;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Contributions;

public class ContributionCascadeTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private IContributionWrites Writes => Site.Services.GetRequiredService<IContributionWrites>();

    [Test]
    public async Task DeletingAVariant_DeletesItsReviewsNotesAndCookedMarks()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Aardvark");
        var doomed = await TestContent.VariantAsync(Site.Services, recipe, "Doomed");
        var kept = await TestContent.VariantAsync(Site.Services, recipe, "Kept");
        await ContributeAsync(doomed, Guid.NewGuid());
        await ContributeAsync(kept, Guid.NewGuid());

        await DeleteAsync(doomed);

        _ = await Assert.That(await RowsAsync(doomed)).IsEqualTo((0, 0, 0));
        _ = await Assert.That(await RowsAsync(kept)).IsEqualTo((1, 1, 1));
        _ = await Assert.That((await SearchWhenCurrentAsync("aardvark")).ReviewCount).IsEqualTo(1);
    }

    [Test]
    public async Task DeletingARecipe_DeletesItsVariantsRows()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Jerboa");
        var first = await TestContent.VariantAsync(Site.Services, recipe, "First");
        var second = await TestContent.VariantAsync(Site.Services, recipe, "Second");
        await ContributeAsync(first, Guid.NewGuid());
        await ContributeAsync(second, Guid.NewGuid());

        await DeleteAsync(recipe);

        _ = await Assert.That(await RowsAsync(first)).IsEqualTo((0, 0, 0));
        _ = await Assert.That(await RowsAsync(second)).IsEqualTo((0, 0, 0));
    }

    [Test]
    public async Task TrashingAVariant_KeepsItsRows_UntilTheRecycleBinIsEmptied()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Chinchilla");
        var trashed = await TestContent.VariantAsync(Site.Services, recipe, "Trashed");
        await ContributeAsync(trashed, Guid.NewGuid());

        await TestContent.TrashAsync(Site.Services, trashed);
        var whileTrashed = await RowsAsync(trashed);
        using (var scope = Site.Services.CreateScope())
        {
            _ = await scope.ServiceProvider.GetRequiredService<IContentService>().EmptyRecycleBinAsync(Constants.Security.SuperUserKey);
        }

        _ = await Assert.That(whileTrashed).IsEqualTo((1, 1, 1));
        _ = await Assert.That(await RowsAsync(trashed)).IsEqualTo((0, 0, 0));
    }

    [Test]
    public async Task DeletingAMember_DeletesTheirRowsOnly()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Dormouse");
        var variant = await TestContent.VariantAsync(Site.Services, recipe, "Classic");
        var leaving = await TestMembers.ApprovedAsync(Site.Services, TestMembers.UniqueUserName("leaving"));
        var staying = await TestMembers.ApprovedAsync(Site.Services, TestMembers.UniqueUserName("staying"));
        await ContributeAsync(variant, leaving);
        await ContributeAsync(variant, staying);

        using (var scope = Site.Services.CreateScope())
        {
            var deleted = await scope.ServiceProvider.GetRequiredService<IMemberEditingService>().DeleteAsync(leaving, Constants.Security.SuperUserKey);
            _ = await Assert.That(deleted.Success).IsTrue();
        }

        _ = await Assert.That(await RowsAsync(variant)).IsEqualTo((1, 1, 1));
        _ = await Assert.That(await Site.Services.GetRequiredService<IContributionReads>().MemberReviewAsync(variant, leaving)).IsNull();
        _ = await Assert.That(await Site.Services.GetRequiredService<IContributionReads>().HasCookedAsync(variant, staying)).IsTrue();
    }

    private async Task ContributeAsync(Guid variant, Guid member)
    {
        await Writes.UpsertReviewAsync(variant, member, 4m, "Good.");
        _ = await Writes.AddNoteAsync(variant, member, "Noted.");
        await Writes.MarkCookedAsync(variant, member);
    }

    private async Task DeleteAsync(Guid key)
    {
        using var scope = Site.Services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IContentEditingService>().DeleteAsync(key, Constants.Security.SuperUserKey);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Deleting {key} failed: {result.Status}.");
        }
    }

    private async Task<(int Reviews, int Notes, int Cooked)> RowsAsync(Guid variant)
    {
        var reads = Site.Services.GetRequiredService<IContributionReads>();
        var stats = (await Site.Services.GetRequiredService<IContributionStats>().GetAsync()).For(variant);
        return ((await reads.ReviewsAsync(variant, 0, 1)).Total, (await reads.NotesAsync(variant, 0, 1)).Total, stats.CookedCount);
    }

    private async Task<RecipeSearchHit> SearchWhenCurrentAsync(string word)
    {
        await Site.Services.GetRequiredService<IRecipeIndexRebuilder>().WhenCurrentAsync(CancellationToken.None);
        return Site.Services.GetRequiredService<IRecipeSearchService>().Search(new RecipeSearchCriteria { Query = word }).Results.Single();
    }
}
```

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/ContributionCascadeTests/*"
```

Expected: all 4 fail, because the rows survive every delete. `TrashingAVariant_KeepsItsRows…` fails only on its
second assertion, after the bin is emptied.

- [ ] **Step 2: Write the cascade**

Apply to `src/KCC.Contributions/ContributionWrites.cs`:

```diff
--- a/src/KCC.Contributions/ContributionWrites.cs
+++ b/src/KCC.Contributions/ContributionWrites.cs
@@ -18,6 +18,10 @@
     Task MarkCookedAsync(Guid variantKey, Guid memberKey);
 
     Task UnmarkCookedAsync(Guid variantKey, Guid memberKey);
+
+    Task DeleteForVariantsAsync(IReadOnlyCollection<Guid> variantKeys);
+
+    Task DeleteForMembersAsync(IReadOnlyCollection<Guid> memberKeys);
 }
 
 public sealed class ContributionWrites(
@@ -102,6 +106,28 @@
         stats.Invalidate();
     }
 
+    public async Task DeleteForVariantsAsync(IReadOnlyCollection<Guid> variantKeys)
+    {
+        var reviews = await WriteAsync(async db =>
+        {
+            await db.CookNotes.Where(note => variantKeys.Contains(note.VariantKey)).ExecuteDeleteAsync();
+            await db.CookedMarks.Where(mark => variantKeys.Contains(mark.VariantKey)).ExecuteDeleteAsync();
+            return await db.Reviews.Where(review => variantKeys.Contains(review.VariantKey)).ExecuteDeleteAsync();
+        });
+        await DeletedAsync(reviews);
+    }
+
+    public async Task DeleteForMembersAsync(IReadOnlyCollection<Guid> memberKeys)
+    {
+        var reviews = await WriteAsync(async db =>
+        {
+            await db.CookNotes.Where(note => memberKeys.Contains(note.MemberKey)).ExecuteDeleteAsync();
+            await db.CookedMarks.Where(mark => memberKeys.Contains(mark.MemberKey)).ExecuteDeleteAsync();
+            return await db.Reviews.Where(review => memberKeys.Contains(review.MemberKey)).ExecuteDeleteAsync();
+        });
+        await DeletedAsync(reviews);
+    }
+
     private async Task<T> WriteAsync<T>(Func<ContributionsDbContext, Task<T>> write)
     {
         using var scope = scopes.CreateScope();
@@ -119,4 +145,16 @@
         stats.Invalidate();
         await eventAggregator.PublishAsync(new ReviewsChangedNotification());
     }
+
+    private async Task DeletedAsync(int reviews)
+    {
+        if (reviews > 0)
+        {
+            await ReviewsChangedAsync();
+        }
+        else
+        {
+            stats.Invalidate();
+        }
+    }
 }
```

Create `src/KCC.Contributions/ContributionCascades.cs`:

```csharp
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace KCC.Contributions;

// Only a permanent delete cascades: a variant in the recycle bin keeps its rows, so restoring it restores its
// reviews. Only variants have rows, so the keys of whatever else was deleted match nothing.
public class ContributionCascades(IContributionWrites contributionWrites) :
    INotificationAsyncHandler<ContentDeletedNotification>,
    INotificationAsyncHandler<MemberDeletedNotification>
{
    public Task HandleAsync(ContentDeletedNotification notification, CancellationToken cancellationToken) =>
        contributionWrites.DeleteForVariantsAsync(notification.DeletedEntities.Select(content => content.Key).ToList());

    public Task HandleAsync(MemberDeletedNotification notification, CancellationToken cancellationToken) =>
        contributionWrites.DeleteForMembersAsync(notification.DeletedEntities.Select(member => member.Key).ToList());
}
```

Apply to `src/KCC.Contributions/ContributionsComposer.cs`:

```diff
--- a/src/KCC.Contributions/ContributionsComposer.cs
+++ b/src/KCC.Contributions/ContributionsComposer.cs
@@ -23,6 +23,9 @@
             shareUmbracoConnection: true);
 
         builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunContributionsMigrations>();
+        builder
+            .AddNotificationAsyncHandler<ContentDeletedNotification, ContributionCascades>()
+            .AddNotificationAsyncHandler<MemberDeletedNotification, ContributionCascades>();
 
         builder.Services.AddSingleton<IContributionStats, ContributionStatsSource>();
         builder.Services.AddSingleton<IContributionReads, ContributionReads>();
```

The notifications arrive after the delete has committed, so the cascade runs in a transaction of its own, lock first.

- [ ] **Step 3: Run the tests**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/ContributionCascadeTests/*"
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: 0 warnings; 4 passed; the whole suite is green.

- [ ] **Step 4: Commit**

```bash
git add src/KCC.Contributions tests/KCC.IntegrationTests/Features/Contributions
git commit -m "Delete Contributions with Their Variant or Member"
```

---

### Task 9: Submissions

Create Recipe and Add Variant come back (spec §8). A recipe is saved under the recipe listing together with its
first variant, in one transaction, so a recipe is never left without it. A variant is saved under its recipe, which
must be a published recipe (spec §17). Both are saved, never published, with `author` set to the member and icons
from `IRecipeIconService`. The icon service lives in `KCC.Admin` with the icon list, as spec §6.2 has it. That
project rejoins the solution now as a plain class library, its Kentico files excluded until Phase 5 rewrites them.
Without an Anthropic key the icon provider now picks its fallback straight away instead of failing a request first.
Submissions count against the 5-an-hour limit.

**Files:**
- Rewrite: `src/KCC.Admin/KCC.Admin.csproj`, `src/KCC.Web/Features/Api/RecipeApiController.cs`
- Modify: `KitchenCommandCenter.sln`, `src/KCC.Web/KCC.Web.csproj`, `tests/KCC.UnitTests/KCC.UnitTests.csproj`,
  `src/KCC.Web/Features/Providers/{RecipeIconProvider,ProvidersComposer}.cs`,
  `src/KCC.Web/Features/Recipes/RecipeQueries.cs`, `src/KCC.Web/Program.cs`
- Create: `src/KCC.Web/Features/Submissions/{SubmissionValues,RecipeSubmissions}.cs`,
  `tests/KCC.UnitTests/Features/Submissions/SubmissionValuesTests.cs`,
  `tests/KCC.UnitTests/Features/Providers/RecipeIconProviderTests.cs`,
  `tests/KCC.IntegrationTests/Features/Api/SubmissionTests.cs`
- Move and rewrite: `src/KCC.Web/Features/Pages/CreateRecipe/CreateRecipeController.cs` →
  `CreateRecipePageController.cs`, `src/KCC.Web/Features/Pages/AddVariant/AddVariantController.cs` →
  `AddVariantPageController.cs`; rewrite `Features/Pages/AddVariant/AddVariantViewModel.cs`
- Delete: `tests/KCC.UnitTests/Features/Api/RecipeApiControllerTests.cs`

**Interfaces:**
- Consumes: `IAccountPageQueries`, `SignInRedirect` (Tasks 3–4), `MemberClient`, `TestMembers`, `TestContent`,
  `RateLimits.Submissions`, `CreateRecipeRequest` and `CreateVariantRequest` (unchanged).
- Produces:
  - `KCC.Web.Features.Submissions.IRecipeSubmissions`, registered scoped:
    - `SubmitRecipeAsync(CreateRecipeRequest, Guid authorKey, CancellationToken) → Task<Guid>`, the recipe's key;
    - `SubmitVariantAsync(Guid recipeKey, CreateVariantRequest, Guid authorKey, CancellationToken) → Task<Guid?>`,
      null unless the key is a published recipe's.
  - `SubmissionValues`: `Recipe`, `Variant`, `IngredientsJson`, `InstructionsJson` and `IngredientNames`.
  - `IRecipeQueries.FindPublishedRecipe(Guid key) → RecipeRecord`, which is null for anything but a published
    recipe.
  - `POST /api/recipes` → `{ recipeKey }`, and `POST /api/recipes/{recipeKey:guid}/variants` → `{ variantKey }`.
    The add-variant route takes the recipe's key where Kentico took its page id. `AddVariantView` already posts
    whatever `recipe-id` it is given, so it does not change.
  - `AddVariantViewModel.RecipeId`, now a `Guid`.

- [ ] **Step 1: Bring back KCC.Admin as a plain library**

Replace `src/KCC.Admin/KCC.Admin.csproj` with:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
        <WarningsNotAsErrors>NU1608;NU1901;NU1902;NU1903</WarningsNotAsErrors>
    </PropertyGroup>
    <ItemGroup Label="Unported slices">
        <Compile Remove="FormComponents/IngredientsEditor/**" />
        <Compile Remove="FormComponents/InstructionsEditor/**" />
        <Compile Remove="FormComponents/RecipeIconSelector/**" />
        <Compile Remove="Module.cs" />
        <Compile Remove="UIPages/**" />
    </ItemGroup>
</Project>
```

```bash
dotnet sln KitchenCommandCenter.sln add src/KCC.Admin/KCC.Admin.csproj
```

In `src/KCC.Web/KCC.Web.csproj`, add `<ProjectReference Include="..\KCC.Admin\KCC.Admin.csproj" />` beside the
`KCC.Contributions` reference. In `tests/KCC.UnitTests/KCC.UnitTests.csproj`, replace `<Compile Remove="Admin/**" />`
with the two tests of the Kentico admin home, which Phase 5 deletes with it:

```xml
        <Compile Remove="Admin/HomeApplicationsResolverTests.cs" />
        <Compile Remove="Admin/HomeStatsServiceTests.cs" />
```

The React client under `src/KCC.Admin/Client` stays in the yarn workspace and builds as before.

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj -- --treenode-filter "/*/*/RecipeIconsTests/*"
```

Expected: 0 warnings; the 5 `RecipeIconsTests` pass again.

- [ ] **Step 2: Write the failing tests**

Create `tests/KCC.UnitTests/Features/Submissions/SubmissionValuesTests.cs`:

```csharp
using KCC.Web.Features.Api;
using KCC.Web.Features.Submissions;

namespace KCC.UnitTests.Features.Submissions;

public class SubmissionValuesTests
{
    private static readonly Guid Author = Guid.NewGuid();

    [Test]
    public async Task Recipe_CarriesItsIconAuthorAndTrimmedDescription()
    {
        var values = SubmissionValues.Recipe(Request(" A weeknight staple. "), "fa-duotone fa-egg", Author).ToDictionary(value => value.Alias, value => value.Value);

        _ = await Assert.That(values["icon"]).IsEqualTo("fa-duotone fa-egg");
        _ = await Assert.That(values["author"]).IsEqualTo(Author.ToString());
        _ = await Assert.That(values["description"]).IsEqualTo("A weeknight staple.");
    }

    [Test]
    public async Task Recipe_LeavesOutABlankDescription()
    {
        var values = SubmissionValues.Recipe(Request("   "), "fa-duotone fa-egg", Author);

        _ = await Assert.That(values.Any(value => value.Alias == "description")).IsFalse();
    }

    [Test]
    public async Task Variant_LeavesOutTimesTheMemberDidNotGive()
    {
        var variant = Variant();
        variant.PrepTime = 10;
        variant.CookTime = null;
        variant.Servings = 0;

        var aliases = SubmissionValues.Variant(variant, "fa-duotone fa-egg", Author).Select(value => value.Alias).ToList();

        _ = await Assert.That(aliases).Contains("prepTime");
        _ = await Assert.That(aliases).DoesNotContain("cookTime");
        _ = await Assert.That(aliases).DoesNotContain("servings");
    }

    [Test]
    public async Task IngredientsJson_GivesAnEyeballedRowNoQuantityOrUnit()
    {
        var json = SubmissionValues.IngredientsJson(
        [
            new IngredientDto { Name = " Flour ", Quantity = 2, Unit = " Cups ", IsEyeballed = false },
            new IngredientDto { Name = "Salt", Quantity = 1, Unit = "Pinch", IsEyeballed = true },
            new IngredientDto { Name = " ", Quantity = 3, Unit = "Cups", IsEyeballed = false },
        ]);

        _ = await Assert.That(json).IsEqualTo("""[{"name":"Flour","quantity":2,"unit":"Cups","isEyeballed":false},{"name":"Salt","quantity":null,"unit":"","isEyeballed":true}]""");
    }

    [Test]
    public async Task InstructionsJson_NumbersTheStepsThatHaveText()
    {
        var json = SubmissionValues.InstructionsJson(
        [
            new InstructionDto { Step = 4, Text = " Whisk. " },
            new InstructionDto { Step = 5, Text = "  " },
            new InstructionDto { Step = 9, Text = "Bake." },
        ]);

        _ = await Assert.That(json).IsEqualTo("""[{"step":1,"text":"Whisk."},{"step":2,"text":"Bake."}]""");
    }

    private static CreateRecipeRequest Request(string description) => new()
    {
        RecipeName = "Shakshuka",
        RecipeDescription = description,
        FirstVariant = Variant(),
    };

    private static CreateVariantRequest Variant() => new()
    {
        VariantName = "Classic",
        VariantDescription = "Eggs in tomato.",
        Ingredients = [new IngredientDto { Name = "Eggs", Quantity = 4, Unit = "Whole" }],
        Instructions = [new InstructionDto { Step = 1, Text = "Simmer." }],
    };
}
```

Create `tests/KCC.UnitTests/Features/Providers/RecipeIconProviderTests.cs`:

```csharp
using Anthropic;
using Anthropic.Core;
using KCC.Admin;
using KCC.Web.Features.Models.Options;
using KCC.Web.Features.Providers;
using Microsoft.Extensions.Logging.Abstractions;

namespace KCC.UnitTests.Features.Providers;

public class RecipeIconProviderTests
{
    [Test]
    public async Task WithoutAnApiKey_PicksTheFallbackIcon()
    {
        var provider = new RecipeIconProvider(
            new AnthropicClient(new ClientOptions { ApiKey = string.Empty }),
            new AnthropicOptions { ApiKey = string.Empty },
            NullLogger<RecipeIconProvider>.Instance);

        var icon = await provider.PickAsync("Shakshuka", "Eggs in tomato.", ["Eggs"], CancellationToken.None);

        _ = await Assert.That(icon).IsEqualTo(RecipeIcons.Fallback("Shakshuka"));
    }
}
```

Create `tests/KCC.IntegrationTests/Features/Api/SubmissionTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KCC.Admin;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;

namespace KCC.IntegrationTests.Features.Api;

public class SubmissionTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task CreateRecipe_SavesADraftUnderTheListing_WithItsFirstVariant()
    {
        using var member = await SignedInAsync();

        var recipeKey = await CreateRecipeAsync(member.Visitor, "IT Wolverine Stew");

        using var scope = Site.Services.CreateScope();
        var content = scope.ServiceProvider.GetRequiredService<IContentService>();
        var recipe = content.GetById(recipeKey)!;
        scope.ServiceProvider.GetRequiredService<IDocumentNavigationQueryService>().TryGetChildrenKeys(recipeKey, out var children);
        var variant = content.GetById(children.Single())!;
        _ = await Assert.That(recipe.ParentId).IsEqualTo(content.GetById(TestContent.RecipeListing(Site.Services))!.Id);
        _ = await Assert.That(recipe.Published).IsFalse();
        _ = await Assert.That(variant.Published).IsFalse();
        _ = await Assert.That(variant.Name).IsEqualTo("Slow and Low");
        _ = await Assert.That(recipe.GetValue<string>("icon")).IsEqualTo(RecipeIcons.Fallback("IT Wolverine Stew"));
        _ = await Assert.That(recipe.GetValue<string>("author")).IsEqualTo(Udi.Create(Constants.UdiEntityType.Member, member.Key).ToString());
        _ = await Assert.That(variant.GetValue<string>("ingredients")).IsEqualTo("""[{"name":"Beans","quantity":2,"unit":"Cans","isEyeballed":false}]""");
    }

    [Test]
    public async Task CreateRecipe_StaysOutOfSearch_AndShowsAsPendingOnTheAccountPage()
    {
        using var member = await SignedInAsync();

        _ = await CreateRecipeAsync(member.Visitor, "IT Wolverine Chili");
        await Site.Services.GetRequiredService<IRecipeIndexRebuilder>().WhenCurrentAsync(CancellationToken.None);

        var search = Site.Services.GetRequiredService<IRecipeSearchService>().Search(new RecipeSearchCriteria { Query = "wolverine chili" });
        var groups = (await RenderedPage.GetAsync(member.Visitor.Http, "/account/")).Prop("recipe-groups").EnumerateArray().ToList();
        _ = await Assert.That(search.Results.Any(hit => hit.Name == "IT Wolverine Chili")).IsFalse();
        _ = await Assert.That(groups.Single().GetProperty("recipeName").GetString()).IsEqualTo("IT Wolverine Chili");
        _ = await Assert.That(groups.Single().GetProperty("isPending").GetBoolean()).IsTrue();
    }

    [Test]
    public async Task AddVariant_ToAPublishedRecipe_SavesADraftUnderIt()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Wolverine Pie");
        using var member = await SignedInAsync();

        using var response = await member.Visitor.PostAsync($"/api/recipes/{recipe}/variants", Variant("Deep Dish"));
        var variantKey = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("variantKey").GetGuid();

        using var scope = Site.Services.CreateScope();
        var variant = scope.ServiceProvider.GetRequiredService<IContentService>().GetById(variantKey)!;
        _ = await Assert.That(variant.ParentId).IsEqualTo(scope.ServiceProvider.GetRequiredService<IContentService>().GetById(recipe)!.Id);
        _ = await Assert.That(variant.Published).IsFalse();
    }

    [Test]
    public async Task AddVariant_ToAnythingButAPublishedRecipe_IsNotFound()
    {
        using var member = await SignedInAsync();
        var draft = await TestContent.DraftRecipeAsync(Site.Services, "IT Wolverine Draft", member.Key);
        var published = await TestContent.RecipeAsync(Site.Services, "IT Wolverine Tart");
        var variant = await TestContent.VariantAsync(Site.Services, published, "Classic");

        using var toDraft = await member.Visitor.PostAsync($"/api/recipes/{draft}/variants", Variant("Nope"));
        using var toVariant = await member.Visitor.PostAsync($"/api/recipes/{variant}/variants", Variant("Nope"));
        using var toNothing = await member.Visitor.PostAsync($"/api/recipes/{Guid.NewGuid()}/variants", Variant("Nope"));

        _ = await Assert.That(toDraft.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        _ = await Assert.That(toVariant.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        _ = await Assert.That(toNothing.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Submissions_SignedOut_AreUnauthorized()
    {
        using var visitor = new MemberClient(Site);

        using var response = await visitor.PostAsync("/api/recipes", Recipe("IT Wolverine Ghost"));

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Submissions_AllowFiveAnHourPerClient()
    {
        using var member = await SignedInAsync();
        for (var submission = 0; submission < 5; submission++)
        {
            _ = await CreateRecipeAsync(member.Visitor, $"IT Wolverine Batch {submission}");
        }

        using var limited = await member.Visitor.PostAsync("/api/recipes", Recipe("IT Wolverine Batch 5"));

        _ = await Assert.That(limited.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task WizardPages_SendASignedOutVisitorToSignIn()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Wolverine Cake");
        using var visitor = new MemberClient(Site);

        using var create = await visitor.Http.GetAsync("/recipes/create-recipe/");
        using var add = await visitor.Http.GetAsync($"/recipes/add-variant/?recipe={recipe}");

        _ = await Assert.That(create.Headers.Location!.OriginalString).IsEqualTo("/account/login/?returnUrl=%2Frecipes%2Fcreate-recipe%2F");
        _ = await Assert.That(add.Headers.Location!.OriginalString).IsEqualTo($"/account/login/?returnUrl=%2Frecipes%2Fadd-variant%2F%3Frecipe%3D{recipe}");
    }

    [Test]
    public async Task AddVariantPage_NamesThePublishedRecipe_AndRefusesAnyOther()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Wolverine Bread");
        using var member = await SignedInAsync();
        var draft = await TestContent.DraftRecipeAsync(Site.Services, "IT Wolverine Dough", member.Key);

        var page = await RenderedPage.GetAsync(member.Visitor.Http, $"/recipes/add-variant/?recipe={recipe}");
        var refused = await RenderedPage.GetAsync(member.Visitor.Http, $"/recipes/add-variant/?recipe={draft}");
        var missing = await RenderedPage.GetAsync(member.Visitor.Http, "/recipes/add-variant/");

        _ = await Assert.That(page.Attribute("recipe-id")).IsEqualTo(recipe.ToString());
        _ = await Assert.That(page.Attribute("recipe-name")).IsEqualTo("IT Wolverine Bread");
        _ = await Assert.That(page.Attribute("recipe-slug")).IsEqualTo("/recipes/it-wolverine-bread/");
        _ = await Assert.That(refused.Status).IsEqualTo(HttpStatusCode.NotFound);
        _ = await Assert.That(missing.Status).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateRecipePage_RendersTheWizardForAMember()
    {
        using var member = await SignedInAsync();

        var page = await RenderedPage.GetAsync(member.Visitor.Http, "/recipes/create-recipe/");

        _ = await Assert.That(page.Status).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(page.Body).Contains("<CreateRecipeView");
    }

    private static object Recipe(string name) => new
    {
        recipeName = name,
        recipeDescription = "Made for one test.",
        firstVariant = Variant("Slow and Low"),
    };

    private static object Variant(string name) => new
    {
        variantName = name,
        variantDescription = "Warming.",
        prepTime = 15,
        cookTime = 120,
        servings = 6,
        ingredients = new[] { new { name = "Beans", quantity = 2, unit = "Cans", isEyeballed = false } },
        instructions = new[] { new { step = 1, text = "Simmer for two hours." } },
    };

    private static async Task<Guid> CreateRecipeAsync(MemberClient visitor, string name)
    {
        using var response = await visitor.PostAsync("/api/recipes", Recipe(name));
        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("recipeKey").GetGuid();
    }

    private async Task<SignedInMember> SignedInAsync()
    {
        var userName = TestMembers.UniqueUserName("chef");
        var key = await TestMembers.ApprovedAsync(Site.Services, userName);
        var visitor = new MemberClient(Site);
        _ = await visitor.SignInAsync(userName, TestMembers.Password);
        return new SignedInMember(visitor, key);
    }

    private sealed record SignedInMember(MemberClient Visitor, Guid Key) : IDisposable
    {
        public void Dispose() => Visitor.Dispose();
    }
}
```

```bash
dotnet build KitchenCommandCenter.sln
```

Expected: build errors — `KCC.Web.Features.Submissions` does not exist and `RecipeIconProvider` is excluded.

- [ ] **Step 3: Bring back the icon provider**

In `src/KCC.Web/KCC.Web.csproj`, delete `<Compile Remove="Features/Providers/RecipeIconProvider.cs" />`. Apply to
`src/KCC.Web/Features/Providers/RecipeIconProvider.cs`:

```diff
--- a/src/KCC.Web/Features/Providers/RecipeIconProvider.cs
+++ b/src/KCC.Web/Features/Providers/RecipeIconProvider.cs
@@ -30,6 +30,13 @@
         IEnumerable<string> ingredients,
         CancellationToken cancellationToken)
     {
+        // Development, CI and the test hosts run without a key, and a request that cannot authenticate only delays the
+        // same fallback.
+        if (string.IsNullOrWhiteSpace(options.ApiKey))
+        {
+            return RecipeIcons.Fallback(name);
+        }
+
         try
         {
             string ingredientList = string.Join(", ", ingredients ?? Array.Empty<string>());
```

Apply to `src/KCC.Web/Features/Providers/ProvidersComposer.cs`, which takes over the registration Kentico's
`Program.cs` made:

```diff
--- a/src/KCC.Web/Features/Providers/ProvidersComposer.cs
+++ b/src/KCC.Web/Features/Providers/ProvidersComposer.cs
@@ -1,3 +1,7 @@
+using Anthropic;
+using Anthropic.Core;
+using KCC.Admin;
+using KCC.Web.Features.Models.Options;
 using Umbraco.Cms.Core.Composing;
 using Umbraco.Cms.Core.DependencyInjection;
 using Umbraco.Cms.Core.Notifications;
@@ -10,6 +14,11 @@
     public void Compose(IUmbracoBuilder builder)
     {
         builder.Services.AddSingleton<IAuthorNameProvider, AuthorNameProvider>();
+
+        var anthropic = builder.Config.GetSection(AnthropicOptions.SectionName).Get<AnthropicOptions>() ?? new();
+        builder.Services.AddSingleton(anthropic);
+        builder.Services.AddSingleton(new AnthropicClient(new ClientOptions { ApiKey = anthropic.ApiKey ?? string.Empty }));
+        builder.Services.AddSingleton<IRecipeIconService, RecipeIconProvider>();
         builder
             .AddNotificationHandler<MemberSavedNotification, AuthorNameCacheRefresher>()
             .AddNotificationHandler<MemberDeletedNotification, AuthorNameCacheRefresher>();
```

- [ ] **Step 4: Write the submission service**

Create `src/KCC.Web/Features/Submissions/SubmissionValues.cs`:

```csharp
using System.Text.Json;
using KCC.Web.Features.Api;
using KCC.Web.Features.Models.Common;
using Umbraco.Cms.Core.Models.ContentEditing;

namespace KCC.Web.Features.Submissions;

// Maps a member's wizard submission onto the recipe and variant properties. A value the member left out is left out
// here too: the draft then fails the type's mandatory check, which stops it being published until the owner fills it in.
public static class SubmissionValues
{
    public static List<PropertyValueModel> Recipe(CreateRecipeRequest request, string icon, Guid authorKey)
    {
        var values = new List<PropertyValueModel>
        {
            Value("icon", icon),
            Value("author", authorKey.ToString()),
        };
        AddText(values, "description", request.RecipeDescription);
        return values;
    }

    public static List<PropertyValueModel> Variant(CreateVariantRequest request, string icon, Guid authorKey)
    {
        var values = new List<PropertyValueModel>
        {
            Value("icon", icon),
            Value("author", authorKey.ToString()),
            Value("ingredients", IngredientsJson(request.Ingredients)),
            Value("instructions", InstructionsJson(request.Instructions)),
        };
        AddText(values, "description", request.VariantDescription);
        AddNumber(values, "prepTime", request.PrepTime);
        AddNumber(values, "cookTime", request.CookTime);
        AddNumber(values, "servings", request.Servings);
        return values;
    }

    // The stored shape the ingredients editor reads: an eyeballed row has no quantity and no unit.
    public static string IngredientsJson(IEnumerable<IngredientDto> ingredients) => JsonSerializer.Serialize(
        (ingredients ?? [])
            .Where(ingredient => !string.IsNullOrWhiteSpace(ingredient?.Name))
            .Select(ingredient => new
            {
                name = ingredient.Name.Trim(),
                quantity = ingredient.IsEyeballed ? null : ingredient.Quantity,
                unit = ingredient.IsEyeballed ? string.Empty : ingredient.Unit?.Trim() ?? string.Empty,
                isEyeballed = ingredient.IsEyeballed,
            }),
        JsonNaming.CamelCase);

    public static string InstructionsJson(IEnumerable<InstructionDto> instructions) => JsonSerializer.Serialize(
        (instructions ?? [])
            .Where(instruction => !string.IsNullOrWhiteSpace(instruction?.Text))
            .Select((instruction, index) => new { step = index + 1, text = instruction.Text.Trim() }),
        JsonNaming.CamelCase);

    public static IEnumerable<string> IngredientNames(CreateVariantRequest request) =>
        (request?.Ingredients ?? []).Select(ingredient => ingredient?.Name).Where(name => !string.IsNullOrWhiteSpace(name));

    private static PropertyValueModel Value(string alias, object value) => new() { Alias = alias, Value = value };

    private static void AddText(List<PropertyValueModel> values, string alias, string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            values.Add(Value(alias, text.Trim()));
        }
    }

    private static void AddNumber(List<PropertyValueModel> values, string alias, int? number)
    {
        if (number is > 0)
        {
            values.Add(Value(alias, number.Value));
        }
    }
}
```

Create `src/KCC.Web/Features/Submissions/RecipeSubmissions.cs`:

```csharp
using KCC.Admin;
using KCC.Web.Features.Api;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Recipes;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Extensions;

namespace KCC.Web.Features.Submissions;

public interface IRecipeSubmissions
{
    Task<Guid> SubmitRecipeAsync(CreateRecipeRequest request, Guid authorKey, CancellationToken cancellationToken);

    // Null when the key is not a published recipe's.
    Task<Guid?> SubmitVariantAsync(Guid recipeKey, CreateVariantRequest request, Guid authorKey, CancellationToken cancellationToken);
}

// A submission is saved and never published; the owner publishes it once it has been checked.
public class RecipeSubmissions(
    IContentEditingService contentEditingService,
    IContentTypeService contentTypeService,
    ICoreScopeProvider scopeProvider,
    IPublishedContentQuery contentQuery,
    IRecipeQueries recipes,
    IRecipeIconService recipeIconService) : IRecipeSubmissions
{
    public async Task<Guid> SubmitRecipeAsync(CreateRecipeRequest request, Guid authorKey, CancellationToken cancellationToken)
    {
        var listing = contentQuery.ContentAtRoot().OfType<HomePage>().SelectMany(home => home.Children<RecipeListingPage>()).FirstOrDefault()
            ?? throw new InvalidOperationException("The site has no recipe listing page.");
        var ingredients = SubmissionValues.IngredientNames(request.FirstVariant).ToList();

        // Picked before the write lock is taken: the icon service may call out to the Anthropic API.
        var recipeIcon = await recipeIconService.PickAsync(request.RecipeName, request.RecipeDescription, ingredients, cancellationToken);
        var variantIcon = await recipeIconService.PickAsync(request.FirstVariant.VariantName, request.FirstVariant.VariantDescription, ingredients, cancellationToken);

        // One transaction, so a recipe is never saved without its first variant.
        using var scope = scopeProvider.CreateCoreScope();
        scope.WriteLock(Constants.Locks.ContentTree);
        var recipeKey = await SaveAsync("recipe", request.RecipeName, listing.Key, SubmissionValues.Recipe(request, recipeIcon, authorKey));
        await SaveAsync("recipeVariant", request.FirstVariant.VariantName, recipeKey, SubmissionValues.Variant(request.FirstVariant, variantIcon, authorKey));
        scope.Complete();
        return recipeKey;
    }

    public async Task<Guid?> SubmitVariantAsync(Guid recipeKey, CreateVariantRequest request, Guid authorKey, CancellationToken cancellationToken)
    {
        if (recipes.FindPublishedRecipe(recipeKey) is null)
        {
            return null;
        }

        var icon = await recipeIconService.PickAsync(request.VariantName, request.VariantDescription, SubmissionValues.IngredientNames(request), cancellationToken);
        return await SaveAsync("recipeVariant", request.VariantName, recipeKey, SubmissionValues.Variant(request, icon, authorKey));
    }

    private async Task<Guid> SaveAsync(string contentTypeAlias, string name, Guid parentKey, IEnumerable<PropertyValueModel> values)
    {
        var key = Guid.NewGuid();
        var created = await contentEditingService.CreateAsync(
            new ContentCreateModel
            {
                Key = key,
                ContentTypeKey = contentTypeService.Get(contentTypeAlias)!.Key,
                ParentKey = parentKey,
                Variants = [new VariantModel { Name = name.Trim() }],
                Properties = values,
            },
            Constants.Security.SuperUserKey);

        // A draft missing a mandatory value is still saved, and reports PropertyValidationError; see SubmissionValues.
        if (created.Status != ContentEditingOperationStatus.Success && created.Status != ContentEditingOperationStatus.PropertyValidationError)
        {
            throw new InvalidOperationException($"Saving the submitted {contentTypeAlias} '{name}' failed: {created.Status}.");
        }

        return key;
    }
}
```

If Phase 1 took the §19 template fallback, `SaveAsync` sets `TemplateKey` too, the way `TestContent.PublishedAsync`
does ("Before you start", item 21). Apply to `src/KCC.Web/Features/Recipes/RecipeQueries.cs`:

```diff
--- a/src/KCC.Web/Features/Recipes/RecipeQueries.cs
+++ b/src/KCC.Web/Features/Recipes/RecipeQueries.cs
@@ -16,6 +16,8 @@
     string GetCreateRecipeUrl(RecipeListingPage listing);
 
     bool IsPublishedVariant(Guid key);
+
+    RecipeRecord FindPublishedRecipe(Guid key);
 }
 
 public class RecipeQueries(IPublishedContentQuery contentQuery) : IRecipeQueries
@@ -52,6 +54,8 @@
 
     public bool IsPublishedVariant(Guid key) => contentQuery.Content(key) is RecipeVariant;
 
+    public RecipeRecord FindPublishedRecipe(Guid key) => contentQuery.Content(key) is Recipe recipe ? RecipeFrom(recipe) : null;
+
     private static RecipeRecord RecipeFrom(Recipe recipe) => new(
         recipe.Key,
         recipe.Name,
```

In `src/KCC.Web/Program.cs`, add `using KCC.Web.Features.Submissions;` (sorted) and, after the
`IAuthoredRecipeQueries` registration:

```csharp
builder.Services.AddScoped<IRecipeSubmissions, RecipeSubmissions>();
```

- [ ] **Step 5: The recipe API and the two wizard pages**

Replace `src/KCC.Web/Features/Api/RecipeApiController.cs` with:

```csharp
using KCC.Web.Features.Security;
using KCC.Web.Features.Submissions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Umbraco.Cms.Core.Security;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api/recipes")]
[AutoValidateAntiforgeryToken]
[EnableRateLimiting(RateLimits.Submissions)]
public class RecipeApiController(IRecipeSubmissions submissions, IMemberManager memberManager) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateRecipe([FromBody] CreateRecipeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.RecipeName))
        {
            return BadRequest(new { error = "Recipe name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.FirstVariant?.VariantName))
        {
            return BadRequest(new { error = "First variant name is required." });
        }

        if (await memberManager.GetCurrentMemberAsync() is not { } member)
        {
            return Unauthorized();
        }

        return Ok(new { recipeKey = await submissions.SubmitRecipeAsync(request, member.Key, cancellationToken) });
    }

    [HttpPost("{recipeKey:guid}/variants")]
    public async Task<IActionResult> AddVariant(Guid recipeKey, [FromBody] CreateVariantRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.VariantName))
        {
            return BadRequest(new { error = "Variant name is required." });
        }

        if (await memberManager.GetCurrentMemberAsync() is not { } member)
        {
            return Unauthorized();
        }

        return await submissions.SubmitVariantAsync(recipeKey, request, member.Key, cancellationToken) is { } variantKey
            ? Ok(new { variantKey })
            : NotFound(new { error = "Recipe not found." });
    }
}
```

```bash
git mv src/KCC.Web/Features/Pages/CreateRecipe/CreateRecipeController.cs src/KCC.Web/Features/Pages/CreateRecipe/CreateRecipePageController.cs
git mv src/KCC.Web/Features/Pages/AddVariant/AddVariantController.cs src/KCC.Web/Features/Pages/AddVariant/AddVariantPageController.cs
git rm -q tests/KCC.UnitTests/Features/Api/RecipeApiControllerTests.cs
```

Replace `src/KCC.Web/Features/Pages/CreateRecipe/CreateRecipePageController.cs` with:

```csharp
using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Account;
using KCC.Web.Features.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.CreateRecipe;

public class CreateRecipePageController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    IMemberManager memberManager,
    IAccountPageQueries accountPages,
    IResourceStringProvider resourceStrings,
    PageMetadata pageMetadata)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    // Route hijacking calls the synchronous Index unless it is hidden like this; the overload below serves the page.
    [NonAction]
    public sealed override IActionResult Index() => throw new NotSupportedException();

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (CurrentPage is not CreateRecipePage page)
        {
            return NotFound();
        }

        if (await memberManager.GetCurrentMemberAsync() is null)
        {
            return SignInRedirect.To(accountPages.GetUrls().Login, Request);
        }

        var viewModel = new CreateRecipeViewModel { ResourceStrings = resourceStrings.GetManyOrDefault("CreateRecipe.CreateRecipe") };
        pageMetadata.Apply(page, viewModel);

        return View("~/Features/Pages/CreateRecipe/Index.cshtml", viewModel);
    }
}
```

Replace `src/KCC.Web/Features/Pages/AddVariant/AddVariantPageController.cs` with:

```csharp
using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Account;
using KCC.Web.Features.Pages.Shared;
using KCC.Web.Features.Recipes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.AddVariant;

public class AddVariantPageController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    IMemberManager memberManager,
    IAccountPageQueries accountPages,
    IRecipeQueries recipes,
    IResourceStringProvider resourceStrings,
    PageMetadata pageMetadata)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    // Route hijacking calls the synchronous Index unless it is hidden like this; the overload below serves the page.
    [NonAction]
    public sealed override IActionResult Index() => throw new NotSupportedException();

    public async Task<IActionResult> Index([FromQuery(Name = "recipe")] Guid? recipeKey, CancellationToken cancellationToken)
    {
        if (CurrentPage is not AddVariantPage page)
        {
            return NotFound();
        }

        if (await memberManager.GetCurrentMemberAsync() is null)
        {
            return SignInRedirect.To(accountPages.GetUrls().Login, Request);
        }

        if (recipeKey is not { } key || recipes.FindPublishedRecipe(key) is not { } recipe)
        {
            return NotFound();
        }

        var viewModel = new AddVariantViewModel
        {
            RecipeId = recipe.Key,
            RecipeName = recipe.Name,
            RecipeSlug = recipe.Url,
            ResourceStrings = GetStrings(),
        };
        pageMetadata.Apply(page, viewModel);

        return View("~/Features/Pages/AddVariant/Index.cshtml", viewModel);
    }

    private Dictionary<string, string> GetStrings() => resourceStrings.GetManyOrDefault(
        // Hero + shared navigation
        "AddVariant.AddVariantFor",
        "AddVariant.Cancel",
        "AddVariant.Next",
        "AddVariant.Back",
        // Step 1: variant info
        "AddVariant.VariantInfo",
        "AddVariant.VariantName",
        "AddVariant.Description",
        "AddVariant.DescriptionPlaceholder",
        "AddVariant.PrepTime",
        "AddVariant.CookTime",
        "AddVariant.Servings",
        // Step 2: ingredients
        "AddVariant.Ingredients",
        "AddVariant.IngredientName",
        "AddVariant.IngredientNamePlaceholder",
        "AddVariant.Eyeball",
        "AddVariant.Quantity",
        "AddVariant.QuantityPlaceholder",
        "AddVariant.Unit",
        "AddVariant.UnitPlaceholder",
        "AddVariant.Remove",
        "AddVariant.AddIngredient",
        // Step 3: instructions
        "AddVariant.Instructions",
        "AddVariant.DescribeThisStep",
        "AddVariant.AddStep",
        // Step 4: review & submit
        "AddVariant.ReviewAndSubmit",
        "AddVariant.Min",
        "AddVariant.Serves",
        "AddVariant.ToTaste",
        "AddVariant.Step",
        "AddVariant.Steps",
        "AddVariant.Submitting",
        "AddVariant.SubmitForReview",
        // Success + error states
        "AddVariant.VariantSubmitted",
        "AddVariant.VariantSubmittedMessage",
        "AddVariant.BackTo",
        "AddVariant.FailedToAddVariant",
        "AddVariant.UnexpectedError");
}
```

Replace `src/KCC.Web/Features/Pages/AddVariant/AddVariantViewModel.cs` with:

```csharp
using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.AddVariant;

public class AddVariantViewModel : BasePageViewModel
{
    public Guid RecipeId { get; set; }

    public string RecipeName { get; set; }

    public string RecipeSlug { get; set; }
}
```

Both views, `CreateRecipeViewModel.cs` and `Features/Models/Api/*` compile again unchanged. In the `Unported slices`
group of `src/KCC.Web/KCC.Web.csproj`, delete the remaining lines:
- `Features/Api/RecipeApiController.cs`, `Features/Models/Api/**`,
  `Features/Pages/AddVariant/**`, `Features/Pages/CreateRecipe/**`;
- the two `Content Remove` lines for their views.

The group is then empty, so delete the whole `ItemGroup`. In `tests/KCC.UnitTests/KCC.UnitTests.csproj`, delete
`<Compile Remove="Features/Api/RecipeApiControllerTests.cs" />`.

- [ ] **Step 6: Run the tests**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/SubmissionTests/*"
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected:
- 0 warnings;
- every unit test passes, including the 5 `SubmissionValuesTests` and `RecipeIconProviderTests`;
- 9 passed;
- the whole integration suite is green. The drafts the submission tests save stay out of search, so no search
  assertion moves.

- [ ] **Step 7: Commit**

```bash
git add -A KitchenCommandCenter.sln src/KCC.Admin/KCC.Admin.csproj src/KCC.Web tests/KCC.UnitTests tests/KCC.IntegrationTests
git commit -m "Save Member Submissions as Drafts for Review"
```

---

### Task 10: The approved E2E member

The E2E suites sign in as one member. Kentico's CI made that member with a SQL `UPDATE`; now the seeder creates it,
approved, from `KCC_E2E_MEMBER_USERNAME` and `KCC_E2E_MEMBER_PASSWORD` (spec §12). The fixtures run the seeder, so a
fresh site already has the member. Its first and last names are "E2E" and "Member". Without both variables the
seeder skips the member and says so. The E2E fixture also raises the rate limits, because every browser request
comes from one address.

**Files:**
- Modify: `src/KCC.Web/Features/DevTools/RecipeSeed/RecipeTestDataSeeder.cs`,
  `tests/KCC.IntegrationTests/Config/UmbracoSite.cs`, `tests/KCC.IntegrationTests/Features/DevTools/RecipeSeederTests.cs`,
  `tests/KCC.E2ETests/Config/SiteProcess.cs`

**Interfaces:**
- Consumes: `SeedKeys.Author`, `IMemberEditingService`, `IConfiguration`.
- Produces: `SeedSummary.E2EMembersCreated`, and the summary's new tail `, E2E member +{n}.`. The rest of the
  summary is unchanged, so Phase 3's gate text still matches.

- [ ] **Step 1: Write the failing test**

Apply to `tests/KCC.IntegrationTests/Features/DevTools/RecipeSeederTests.cs`:

```diff
--- a/tests/KCC.IntegrationTests/Features/DevTools/RecipeSeederTests.cs
+++ b/tests/KCC.IntegrationTests/Features/DevTools/RecipeSeederTests.cs
@@ -2,7 +2,9 @@
 using KCC.Contributions;
 using KCC.IntegrationTests.Config;
 using KCC.Web.Features.DevTools.RecipeSeed;
+using Microsoft.AspNetCore.Identity;
 using Microsoft.Extensions.DependencyInjection;
+using Umbraco.Cms.Core.Security;
 using Umbraco.Cms.Core.Services;
 using Umbraco.Cms.Core.Services.Navigation;
 
@@ -54,6 +56,19 @@
     }
 
     [Test]
+    public async Task Seeder_CreatesTheApprovedE2EMember_WhoCanSignIn()
+    {
+        using var scope = Site.Services.CreateScope();
+        var member = await scope.ServiceProvider.GetRequiredService<IMemberManager>().FindByNameAsync("e2e-member");
+
+        var signIn = await scope.ServiceProvider.GetRequiredService<SignInManager<MemberIdentityUser>>()
+            .CheckPasswordSignInAsync(member!, "E2E-Member-Passw0rd", lockoutOnFailure: false);
+
+        _ = await Assert.That(member!.IsApproved).IsTrue();
+        _ = await Assert.That(signIn.Succeeded).IsTrue();
+    }
+
+    [Test]
     public async Task Seeder_ReviewsEachRecipesFirstVariant()
     {
         var stats = await Site.Services.GetRequiredService<IContributionStats>().GetAsync();
```

In `tests/KCC.IntegrationTests/Config/UmbracoSite.cs`, add these entries to `Settings()`:

```csharp
        // The seeder creates the approved member the E2E suite signs in as, from these two settings.
        ["KCC_E2E_MEMBER_USERNAME"] = "e2e-member",
        ["KCC_E2E_MEMBER_PASSWORD"] = "E2E-Member-Passw0rd",
```

The keys are the environment variables' names: configuration reads the variables under those names, so the
settings and the real environment reach the seeder the same way.

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/RecipeSeederTests/*"
```

Expected: `Seeder_CreatesTheApprovedE2EMember_WhoCanSignIn` fails, because there is no member called `e2e-member`.

- [ ] **Step 2: Seed the member**

Apply to `src/KCC.Web/Features/DevTools/RecipeSeed/RecipeTestDataSeeder.cs`:

```diff
--- a/src/KCC.Web/Features/DevTools/RecipeSeed/RecipeTestDataSeeder.cs
+++ b/src/KCC.Web/Features/DevTools/RecipeSeed/RecipeTestDataSeeder.cs
@@ -21,7 +21,8 @@
     IMemberTypeService memberTypeService,
     IMemberEditingService memberEditingService,
     IUserService userService,
-    IContributionWrites contributionWrites)
+    IContributionWrites contributionWrites,
+    IConfiguration configuration)
 {
     private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
 
@@ -34,6 +35,7 @@
         var categories = KeysByName("recipeCategory");
         var tags = KeysByName("recipeTag");
         var authors = await EnsureAuthorsAsync(summary, log);
+        await EnsureE2EMemberAsync(summary, log);
         var today = DateTime.UtcNow.Date;
 
         foreach (var recipe in RecipeSeedData.Recipes)
@@ -189,10 +191,6 @@
 
     private async Task<Dictionary<string, Guid>> EnsureAuthorsAsync(SeedSummary summary, TextWriter log)
     {
-        var memberTypeKey = memberTypeService.Get(Constants.Security.DefaultMemberTypeAlias)?.Key
-            ?? throw new InvalidOperationException("The default member type is missing.");
-        var superUser = await userService.GetAsync(Constants.Security.SuperUserKey)
-            ?? throw new InvalidOperationException("The super user is missing.");
         var keys = new Dictionary<string, Guid>(StringComparer.Ordinal);
 
         foreach (var author in RecipeSeedData.Authors)
@@ -203,32 +201,13 @@
                 continue;
             }
 
-            var created = await memberEditingService.CreateAsync(
-                new MemberCreateModel
-                {
-                    Key = SeedKeys.Author(author.UserName),
-                    ContentTypeKey = memberTypeKey,
-                    Username = author.UserName,
-                    Email = author.Email,
-
-                    // Seeded authors never sign in, so their password is random.
-                    Password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)),
-                    IsApproved = true,
-                    Variants = [new VariantModel { Name = $"{author.FirstName} {author.LastName}" }],
-                    Properties =
-                    [
-                        new PropertyValueModel { Alias = "firstName", Value = author.FirstName },
-                        new PropertyValueModel { Alias = "lastName", Value = author.LastName },
-                    ],
-                },
-                superUser);
-            if (!created.Success)
-            {
-                throw new InvalidOperationException(
-                    $"Creating author {author.UserName} failed: {created.Status.MemberEditingOperationStatus}, {created.Status.ContentEditingOperationStatus}.");
-            }
-
-            keys[author.Key] = created.Result.Content.Key;
+            // Seeded authors never sign in, so their password is random.
+            keys[author.Key] = await CreateApprovedMemberAsync(
+                author.UserName,
+                author.Email,
+                Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)),
+                author.FirstName,
+                author.LastName);
             summary.AuthorsCreated++;
             log.WriteLine($"  author created: {author.FirstName} {author.LastName} ({author.UserName})");
         }
@@ -236,6 +215,60 @@
         return keys;
     }
 
+    // The E2E suite and the reference capture sign in as this member. Its credentials come from the environment, so
+    // none is committed; without them there is no member to create.
+    private async Task EnsureE2EMemberAsync(SeedSummary summary, TextWriter log)
+    {
+        var userName = configuration["KCC_E2E_MEMBER_USERNAME"];
+        var password = configuration["KCC_E2E_MEMBER_PASSWORD"];
+        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
+        {
+            log.WriteLine("  E2E member skipped: KCC_E2E_MEMBER_USERNAME and KCC_E2E_MEMBER_PASSWORD are not both set.");
+            return;
+        }
+
+        if (memberService.GetByUsername(userName) is not null)
+        {
+            return;
+        }
+
+        _ = await CreateApprovedMemberAsync(userName, $"{userName}@example.test", password, "E2E", "Member");
+        summary.E2EMembersCreated++;
+        log.WriteLine($"  E2E member created: {userName}");
+    }
+
+    private async Task<Guid> CreateApprovedMemberAsync(string userName, string email, string password, string firstName, string lastName)
+    {
+        var memberTypeKey = memberTypeService.Get(Constants.Security.DefaultMemberTypeAlias)?.Key
+            ?? throw new InvalidOperationException("The default member type is missing.");
+        var superUser = await userService.GetAsync(Constants.Security.SuperUserKey)
+            ?? throw new InvalidOperationException("The super user is missing.");
+        var created = await memberEditingService.CreateAsync(
+            new MemberCreateModel
+            {
+                Key = SeedKeys.Author(userName),
+                ContentTypeKey = memberTypeKey,
+                Username = userName,
+                Email = email,
+                Password = password,
+                IsApproved = true,
+                Variants = [new VariantModel { Name = $"{firstName} {lastName}" }],
+                Properties =
+                [
+                    new PropertyValueModel { Alias = "firstName", Value = firstName },
+                    new PropertyValueModel { Alias = "lastName", Value = lastName },
+                ],
+            },
+            superUser);
+        if (!created.Success)
+        {
+            throw new InvalidOperationException(
+                $"Creating member {userName} failed: {created.Status.MemberEditingOperationStatus}, {created.Status.ContentEditingOperationStatus}.");
+        }
+
+        return created.Result.Content.Key;
+    }
+
     private async Task CreatePublishedAsync(Guid key, string name, Guid contentTypeKey, Guid parentKey, DateTime createDate, IEnumerable<PropertyValueModel> values)
     {
         var created = await contentEditingService.CreateAsync(
@@ -286,7 +319,9 @@
 
     public int ReviewsWritten { get; set; }
 
+    public int E2EMembersCreated { get; set; }
+
     public override string ToString() =>
         $"Seed complete: recipes +{RecipesCreated} (skipped {RecipesSkipped}), variants +{VariantsCreated}, " +
-        $"reviews +{ReviewsWritten}, authors +{AuthorsCreated}.";
+        $"reviews +{ReviewsWritten}, authors +{AuthorsCreated}, E2E member +{E2EMembersCreated}.";
 }
```

The authors and the E2E member now share `CreateApprovedMemberAsync`. The seeder creates members through the member
editing service, the backoffice's path, which needs no member lock: nothing else writes while a fixture seeds.

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/RecipeSeederTests/*"
```

Expected: 0 warnings; 6 passed. `Seeder_SecondRun_SkipsEveryRecipe` still finds `recipes +0 (skipped 25)`.

- [ ] **Step 3: Give the E2E site room**

In `tests/KCC.E2ETests/Config/SiteProcess.cs`, add to `SiteEnvironment()`, after the
`["DataProtection__KeysDirectory"]` entry:

```csharp

            // Every browser request comes from this machine, so the whole suite shares one client's rate limits.
            ["RateLimits__AccountPerMinute"] = "1000",
            ["RateLimits__ContributionsPerMinute"] = "1000",
            ["RateLimits__SubmissionsPerHour"] = "1000",
```

The site process inherits the test runner's environment, `KCC_E2E_MEMBER_*` included, so nothing else needs
passing on. CI already sets both variables from its secrets.

```bash
dotnet build KitchenCommandCenter.sln
```

- [ ] **Step 4: Commit**

```bash
git add src/KCC.Web/Features/DevTools tests/KCC.IntegrationTests tests/KCC.E2ETests/Config/SiteProcess.cs
git commit -m "Seed the Approved E2E Member"
```

---

### Task 11: The member E2E suites

The review, cook-note, cooked and live-rating suites come back, and a new suite walks the member flows spec §14
names:
- a new sign-up waits for approval and cannot sign in yet;
- the approved member signs in and signs out through the header's form;
- a submitted recipe waits on the account page as pending.

The suites write to one seeded variant, Matcha Panna Cotta's Green Tea Set, so no read-only suite's assertion can
move. It has no reviews of its own, and its recipe sorts after Legendary Lasagna, so even a five-star review leaves
the listing's spotlight alone. The live-rating test watches the spotlight of a `matcha` search instead. Every suite
that signs in as the E2E member runs one at a time with the others (`[NotInParallel(MemberSession.Serial)]`), since
they share that member.

**Files:**
- Modify: `tests/KCC.E2ETests/KCC.E2ETests.csproj`
- Rewrite: `tests/KCC.E2ETests/Config/MemberSession.cs`,
  `tests/KCC.E2ETests/Features/VariantReviews/VariantReviewsTests.cs`,
  `tests/KCC.E2ETests/Features/VariantCookNotes/VariantCookNotesTests.cs`,
  `tests/KCC.E2ETests/Features/VariantCooked/VariantCookedTests.cs`,
  `tests/KCC.E2ETests/Features/RecipeSearch/RecipeSearchLiveRatingTests.cs`
- Create: `tests/KCC.E2ETests/Config/MemberTestVariant.cs`, `tests/KCC.E2ETests/Features/Members/MemberFlowTests.cs`

**Interfaces:**
- Consumes the UI hooks:
  - the variant page's `data-testid`s: `review-input`, `submit-review`, `reviews-list`, `delete-review`,
    `cook-note-input`, `add-cook-note`, `cook-notes-list` and `cooked-toggle`, and the stars' `data-value` and
    `data-star`/`data-state`;
  - Phase 3's `recipe-search-input`, `recipe-search-submit`, `recipe-card`, `recipe-spotlight` and
    `recipe-card-rating`;
  - the login form's `input[name]`s, and the create wizard's placeholders and Next / Submit for Review buttons;
  - the header's Account group, its Logout entry (Task 6) and its Login link.
- Consumes the Task 6 strings, whose texts contain "waiting for approval".

- [ ] **Step 1: Bring the suites back into the build**

In `tests/KCC.E2ETests/KCC.E2ETests.csproj`, delete these four `Compile Remove` lines:
- `Features/VariantCookNotes/**`;
- `Features/VariantCooked/**`;
- `Features/VariantReviews/**`;
- `Features/RecipeSearch/RecipeSearchLiveRatingTests.cs`.

`Features/HomePage/**` is Phase 6's.

- [ ] **Step 2: Write the suites**

Replace `tests/KCC.E2ETests/Config/MemberSession.cs` with:

```csharp
using Microsoft.Playwright;

namespace KCC.E2ETests.Config;

/// <summary>Signs the seeded E2E member in through the public /account/login form.</summary>
public static class MemberSession
{
    // Every suite that signs in as this member changes what the others see of it, so they run one at a time.
    public const string Serial = "e2e-member";

    // The fixture's seeder creates this approved member from the same two variables, so no login is committed to
    // source control. Set them in your environment (on macOS/zsh, ~/.zshenv, which `dotnet run` also inherits).
    public static string Username => GetRequired("KCC_E2E_MEMBER_USERNAME");

    public static string Password => GetRequired("KCC_E2E_MEMBER_PASSWORD");

    /// <summary>Signs the member in via the login form and waits for the post-login redirect.</summary>
    public static async Task SignInAsync(IPage page)
    {
        _ = await page.GotoAsync("/account/login");

        await page.FillAsync("input[name='UserName']", Username);
        await page.FillAsync("input[name='Password']", Password);

        // The submit control renders the "SignIn" dictionary string; clicking it by type keeps the helper
        // independent of the string's value.
        await page.ClickAsync("form button[type='submit']");

        // On success the client sets window.location.href; wait until we've left the login page.
        await page.WaitForURLAsync(url => !url.Contains("/account/login", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetRequired(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException(
                $"{name} is not set. The member E2E flows sign in as the member the fixture seeds from "
                    + "KCC_E2E_MEMBER_USERNAME and KCC_E2E_MEMBER_PASSWORD; set both (see \"E2E tests\" in the README).");
}
```

Create `tests/KCC.E2ETests/Config/MemberTestVariant.cs`:

```csharp
namespace KCC.E2ETests.Config;

// The seeded variant the member suites review, note and mark cooked. No read-only suite asserts on it, it has no
// reviews of its own, and its recipe sorts after Legendary Lasagna, so even a five-star review cannot take the
// listing's Top Rated spotlight.
public static class MemberTestVariant
{
    public const string RecipeName = "Matcha Panna Cotta";

    public const string Path = "/recipes/matcha-panna-cotta/green-tea-set";
}
```

Replace `tests/KCC.E2ETests/Features/VariantReviews/VariantReviewsTests.cs` with:

```csharp
using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.VariantReviews;

[NotInParallel(MemberSession.Serial)]
public class VariantReviewsTests : BasePageTests
{
    [Test]
    public async Task Anonymous_SeesLogInToReviewPrompt()
    {
        _ = await Page.GotoAsync(MemberTestVariant.Path);

        // For anonymous users the review editor and cook-note draft are not rendered
        // (both textareas are gated on isAuthenticated), so the page has no textareas.
        await Expect(Page.Locator("textarea")).ToHaveCountAsync(0);

        // The ratings/reviews section still renders its heading, which contains "review".
        await Expect(Page.GetByText("review", new() { Exact = false }).First).ToBeVisibleAsync();
    }

    [Test]
    public async Task LoggedInMember_CanSubmitEditAndDeleteAReview()
    {
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync(MemberTestVariant.Path);

        // The interactive StarRating is a slider whose per-star half/whole hit areas carry data-value (0.5 .. 5);
        // the review editor exposes data-testid hooks so the flow is independent of the dictionary's text.
        var reviewInput = Page.Locator("[data-testid='review-input']");
        var submit = Page.Locator("[data-testid='submit-review']");
        var reviewsList = Page.Locator("[data-testid='reviews-list']");

        await Page.Locator("[data-value='4']").First.ClickAsync();
        await reviewInput.FillAsync("E2E review - tasty");
        await submit.ClickAsync();
        await Expect(reviewsList.GetByText("E2E review - tasty")).ToBeVisibleAsync();

        // Resubmitting edits in place: the member still has one review.
        await reviewInput.FillAsync("E2E review - edited");
        await submit.ClickAsync();
        await Expect(reviewsList.GetByText("E2E review - edited")).ToBeVisibleAsync();

        await Page.Locator("[data-testid='delete-review']").ClickAsync();
        await Expect(reviewsList.GetByText("E2E review - edited")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task LoggedInMember_CanSubmitAHalfStarReview()
    {
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync(MemberTestVariant.Path);

        var submit = Page.Locator("[data-testid='submit-review']");
        var reviewsList = Page.Locator("[data-testid='reviews-list']");

        // Click the left half of the 4th star -> 3.5, add text, submit.
        await Page.Locator("[data-value='3.5']").First.ClickAsync();
        await Page.Locator("[data-testid='review-input']").FillAsync("E2E half-star review");
        await submit.ClickAsync();
        await Expect(reviewsList.GetByText("E2E half-star review")).ToBeVisibleAsync();

        // The stored 3.5 round-trips: the review's readonly stars show a half at position 4.
        await Expect(reviewsList.Locator("[data-star='4'][data-state='half']").First).ToBeVisibleAsync();

        await Page.Locator("[data-testid='delete-review']").ClickAsync();
        await Expect(reviewsList.GetByText("E2E half-star review")).ToHaveCountAsync(0);
    }
}
```

Replace `tests/KCC.E2ETests/Features/VariantCookNotes/VariantCookNotesTests.cs` with:

```csharp
using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.VariantCookNotes;

[NotInParallel(MemberSession.Serial)]
public class VariantCookNotesTests : BasePageTests
{
    [Test]
    public async Task LoggedInMember_CanAddAndDeleteACookNote()
    {
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync(MemberTestVariant.Path);
        var notes = Page.Locator("[data-testid='cook-notes-list']");
        var noteText = $"E2E note {Guid.NewGuid():N}";

        await Page.Locator("[data-testid='cook-note-input']").FillAsync(noteText);
        await Page.Locator("[data-testid='add-cook-note']").ClickAsync();
        await Expect(notes.GetByText(noteText)).ToBeVisibleAsync();

        // Only the member's own note carries a delete button.
        await notes.Locator("li").Filter(new() { HasText = noteText }).GetByRole(AriaRole.Button).ClickAsync();
        await Expect(Page.GetByText(noteText)).ToHaveCountAsync(0);
    }
}
```

Replace `tests/KCC.E2ETests/Features/VariantCooked/VariantCookedTests.cs` with:

```csharp
using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.VariantCooked;

[NotInParallel(MemberSession.Serial)]
public class VariantCookedTests : BasePageTests
{
    [Test]
    public async Task LoggedInMember_TogglingICookedThis_ChangesTheCount()
    {
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync(MemberTestVariant.Path);

        var toggle = Page.Locator("[data-testid='cooked-toggle']");
        await Expect(toggle).ToBeVisibleAsync();

        var before = await ReadCount(toggle);
        await toggle.ClickAsync();
        await Expect(toggle).Not.ToContainTextAsync($"({before})");
        _ = await Assert.That(await ReadCount(toggle)).IsEqualTo(before + 1);

        // Untoggle so the run is repeatable.
        await toggle.ClickAsync();
        await Expect(toggle).ToContainTextAsync($"({before})");
    }

    private static async Task<int> ReadCount(ILocator toggle)
    {
        var text = await toggle.InnerTextAsync();
        var digits = new string(text.Where(char.IsDigit).ToArray());
        return digits.Length == 0 ? 0 : int.Parse(digits);
    }
}
```

Replace `tests/KCC.E2ETests/Features/RecipeSearch/RecipeSearchLiveRatingTests.cs` with:

```csharp
using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.RecipeSearch;

// A review reaches the search index within seconds: the variant's recipe, unrated before, becomes the Top Rated
// spotlight for a search that finds it. Searching keeps the listing's own spotlight, which other suites assert,
// untouched.
[NotInParallel(MemberSession.Serial)]
public class RecipeSearchLiveRatingTests : BasePageTests
{
    private const string ReviewText = "E2E rating - live reindex";

    [Test]
    public async Task ReviewWrite_MakesTheRecipeTheSearchSpotlight()
    {
        await MemberSession.SignInAsync(Page);
        var spotlight = Page.Locator("[data-testid='recipe-spotlight']");

        await SearchForMatchaAsync();
        await Expect(Page.Locator($"[data-testid='recipe-card'][data-recipe-name='{MemberTestVariant.RecipeName}']")).ToHaveCountAsync(1);
        await Expect(spotlight).ToHaveCountAsync(0);

        _ = await Page.GotoAsync(MemberTestVariant.Path);
        await Page.Locator("[data-value='5']").First.ClickAsync();
        await Page.Locator("[data-testid='review-input']").FillAsync(ReviewText);
        await Page.Locator("[data-testid='submit-review']").ClickAsync();
        await Expect(Page.Locator("[data-testid='reviews-list']").GetByText(ReviewText)).ToBeVisibleAsync();

        try
        {
            // The index rebuilds about two seconds after the review.
            for (var attempt = 0; attempt < 20 && !await spotlight.IsVisibleAsync(); attempt++)
            {
                await Page.WaitForTimeoutAsync(1000);
                await SearchForMatchaAsync();
            }

            await Expect(spotlight).ToHaveAttributeAsync("data-recipe-name", MemberTestVariant.RecipeName);
            await Expect(spotlight.Locator("[data-testid='recipe-card-rating']")).ToBeVisibleAsync();
        }
        finally
        {
            _ = await Page.GotoAsync(MemberTestVariant.Path);
            await Page.Locator("[data-testid='delete-review']").ClickAsync();
            await Expect(Page.Locator("[data-testid='reviews-list']").GetByText(ReviewText)).ToHaveCountAsync(0);
        }
    }

    private async Task SearchForMatchaAsync()
    {
        _ = await Page.GotoAsync("/recipes");
        await Page.Locator("[data-testid='recipe-search-input']").FillAsync("matcha");
        await Page.RunAndWaitForResponseAsync(
            () => Page.Locator("[data-testid='recipe-search-submit']").ClickAsync(),
            response => response.Url.Contains("/api/recipes/search", StringComparison.Ordinal));
    }
}
```

Create `tests/KCC.E2ETests/Features/Members/MemberFlowTests.cs`:

```csharp
using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Members;

[NotInParallel(MemberSession.Serial)]
public class MemberFlowTests : BasePageTests
{
    [Test]
    public async Task SignUp_WaitsForTheOwnersApproval()
    {
        var userName = $"newcomer-{Guid.NewGuid():N}"[..18];
        _ = await Page.GotoAsync("/account/login");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign Up", Exact = true }).ClickAsync();
        await Page.FillAsync("input[name='UserName']", userName);
        await Page.FillAsync("input[name='Email']", $"{userName}@example.test");
        await Page.FillAsync("input[name='Password']", "Newcomer-Passw0rd");
        await Page.FillAsync("input[name='PasswordConfirmation']", "Newcomer-Passw0rd");
        await Page.ClickAsync("form button[type='submit']");

        await Page.WaitForURLAsync(url => url.Contains("/account/registration-complete", StringComparison.OrdinalIgnoreCase));
        await Expect(Page.Locator("main")).ToContainTextAsync("waiting for approval");

        _ = await Page.GotoAsync("/account/login");
        await Page.FillAsync("input[name='UserName']", userName);
        await Page.FillAsync("input[name='Password']", "Newcomer-Passw0rd");
        await Page.ClickAsync("form button[type='submit']");

        await Expect(Page.GetByRole(AriaRole.Alert)).ToContainTextAsync("waiting for approval");
        _ = await Assert.That(Page.Url).Contains("/account/login");
    }

    [Test]
    public async Task ApprovedMember_SignsIn_AndSignsOutFromTheHeader()
    {
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync("/");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Account", Exact = true }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Logout", Exact = true }).ClickAsync();

        await Page.WaitForURLAsync(url => new Uri(url).AbsolutePath == "/");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Login", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Account", Exact = true })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task SubmittedRecipe_WaitsOnTheAccountPageAsPending()
    {
        var recipeName = $"E2E Submission {Guid.NewGuid():N}"[..23];
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync("/recipes/create-recipe");
        var next = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });

        await Page.GetByPlaceholder("e.g., Mac & Cheese").FillAsync(recipeName);
        await Page.GetByPlaceholder("A short description of this dish").FillAsync("Submitted by the E2E suite.");
        await next.ClickAsync();
        await Page.GetByPlaceholder("e.g., Classic Stovetop").FillAsync("First Try");
        await next.ClickAsync();
        await Page.GetByPlaceholder("Ingredient name").First.FillAsync("Eggs");
        await next.ClickAsync();
        await Page.GetByPlaceholder("Describe this step").First.FillAsync("Scramble gently.");
        await next.ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Submit for Review" }).ClickAsync();
        await Expect(Page.GetByText("Recipe Submitted!")).ToBeVisibleAsync();

        _ = await Page.GotoAsync("/account");
        var group = Page.Locator("li").Filter(new() { HasText = recipeName });
        await Expect(group.GetByText("Pending review").First).ToBeVisibleAsync();
    }
}
```

The E2E site is a production build started by `SiteProcess`, so a plain `GotoAsync` settles, and the Vite
dev-server waits in the old helpers go. The suites no longer go through `Config/RecipeNavigation.cs`. Delete it if
nothing else uses it (`grep -rn RecipeNavigation tests/KCC.E2ETests`).

- [ ] **Step 3: Run the E2E suite**

The member flows need the two variables. The password must have at least 8 characters, or seeding fails.

```bash
export KCC_E2E_MEMBER_USERNAME=e2e-member KCC_E2E_MEMBER_PASSWORD='<at least 8 characters>'
dotnet build KitchenCommandCenter.sln
(cd src/KCC.Web && yarn build:all)
dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj
```

Skip the `export` if your shell already sets both variables, for example in `~/.zshenv`.

Expected: every E2E test passes: Phase 3's, plus these 9. If a locator misses, dump the section's markup with
`InnerHTMLAsync()` and adjust the locator, never the app. The pages' markup comes from unchanged components, apart
from Task 6's forms.

- [ ] **Step 4: Commit**

```bash
git add -A tests/KCC.E2ETests
git commit -m "Bring Back the Member E2E Flows"
```

---

### Task 12: Docs, memory and the Phase 4 gate

**Files:**
- Modify: `README.md`, `CLAUDE.md`, this plan's Status line
- Memory, outside the repo, in `~/.claude/projects/-Users-twinright-Repos-Kitchen-Command-Center/memory/`:
  - `e2e-tests-seed-member-and-db.md`
  - `umbraco-sqlite-relations-lock-hazard.md`
  - `MEMORY.md`

- [ ] **Step 1: Document members and the E2E member**

In `README.md`, replace the body of the **E2E tests** subsection with:

```markdown
The E2E suite starts its own copy of the site on a free port, with a fresh SQLite database and its own SSR process.
Run `dotnet build` and `yarn build:all` (in `src/KCC.Web`) first, and set `KCC_E2E_MEMBER_USERNAME` and
`KCC_E2E_MEMBER_PASSWORD` (a password of at least 8 characters; on macOS/zsh, in `~/.zshenv`). The site's seeder
creates that member, approved, before any test runs.
```

Under **Development Best Practices**, add this subsection after Phase 3's **Recipe search**:

```markdown
#### Members

Anyone can sign up, and the account waits until the owner approves it: Members → the member → **Approved** →
**Save**. Five failed sign-ins lock a member out for 15 minutes. The account, contribution and submission endpoints
check the anti-forgery token the layout hands out, and are rate-limited per client (`RateLimits:*` in
`appsettings.json`). A member's recipe or variant is saved as a draft under Recipes: open it, fill in anything it
lacks, then **Save and Publish**.
```

In `CLAUDE.md`, add this section after **Vue SFC `<style>` blocks**:

```markdown
## SQLite writes

The site runs on SQLite, where a transaction that has read cannot become the writer once another connection has
committed, and Umbraco then retries the write for about ten minutes. Two guards in `Features/Sqlite` prevent it:

- `SqliteComposer` wraps Umbraco's post-save relations update so that it takes the write lock before it reads. Never
  remove it. If an Umbraco upgrade moves the handler, the site refuses to boot rather than run unguarded.
- Code that saves a member through Umbraco's sign-in manager, member manager or `IMemberService` runs inside
  `IMemberWriteLock.RunAsync`.

Contribution writes go through `ContributionWrites`, which takes its lock before its first read. The concurrency test
in `tests/KCC.IntegrationTests/Features/Sqlite` proves all three; run it after touching any of them.
```

```bash
git add README.md CLAUDE.md
git commit -m "Document Members and SQLite Writes"
```

- [ ] **Step 2: Update the memory this phase invalidated**

Earlier phases may already have edited these files. Apply each change to the text that describes the same thing now.

In `e2e-tests-seed-member-and-db.md`, replace the paragraph about member seeding returning in Phase 4 with:

> Since replatform Phase 4 the fixture's seeder creates the approved E2E member from `KCC_E2E_MEMBER_USERNAME` and
> `KCC_E2E_MEMBER_PASSWORD`, which must have at least 8 characters. The member suites write to one seeded variant,
> Matcha Panna Cotta's Green Tea Set, and share `[NotInParallel(MemberSession.Serial)]`.

In `umbraco-sqlite-relations-lock-hazard.md`, append:

> **Mitigated in replatform Phase 4.** A second pattern turned up too: `MemberUserStore.UpdateAsync`, behind
> sign-in, a failed attempt and sign-out, reads the member and then saves it in one transaction. Two guards cover
> both patterns:
> - `WriteLockedRelationsUpdate`, which `SqliteComposer` wraps around `ContentRelationsUpdate`;
> - `IMemberWriteLock` around every member save KCC starts.
>
> `SqliteConcurrencyTests` proves them, and it fails with a `TimeoutException` when they are removed. The
> backoffice's editing services need only the decorator.

Update its `description` to say the hazard is mitigated, and its `MEMORY.md` line with it. In `MEMORY.md`, append to
the replatform line: `Phase 4 landed (<date>): members with approval, contribution writes, submissions, SQLite
guards.`

- [ ] **Step 3: The gate — the member flows in a fresh clone**

Stop any site on port 58671, then clone the branch fresh and start it with the E2E member's variables in the
environment:

```bash
GATE="$(mktemp -d)/kcc-phase-4-gate"
git clone --branch replatform --single-branch /Users/twinright/Repos/Kitchen-Command-Center "$GATE"
cd "$GATE" && yarn install --frozen-lockfile
cd src/KCC.Web && dotnet watch --non-interactive
```

Once it serves, run these from the main checkout:

```bash
curl -sk -X POST https://localhost:58671/api/dev/seed-recipes | head -c 300; echo
dotnet run --project tests/KCC.ReferenceCapture -- --only login,registration-complete,account,settings,create-recipe,add-variant \
  --out .superpowers/reference/umbraco-phase-4
ls .superpowers/reference/umbraco-phase-4/*/
```

Expected: the seed summary ends `E2E member +1.`. The capture signs in as the E2E member through the login form and
writes six PNGs in each of its four folders: both ramps, at desktop and mobile widths.

Compare each with the file of the same name under `docs/replatform/reference/xperience-final/`. The header and
footer were compared at Phase 1's gate. The bodies must match, except for:
- **registration-complete**: the body now says the account is waiting for approval, and the label's icon is an
  hourglass, not an envelope;
- **account**: the name is "E2E Member", and the creations list is the E2E member's (empty on a fresh site);
- **settings**: the names are "E2E" and "Member", and the email is `<username>@example.test`;
- **add-variant**: the wizard for Fluffy Buttermilk Pancakes, whose recipe page the capture follows.

The sign-out controls look as before: the same pills, now inside forms. Any other difference is a bug: fix it and
re-capture before closing the phase.

Then walk the member flow in the gate site with the owner. The public pages can be driven in the Browser pane; the
backoffice needs the owner's password, so the owner does those steps.
1. At `/account/login`, **Sign Up** a new member. The registration-complete page says the account is waiting for
   approval, and signing in with it says so too.
2. **Owner:** backoffice → Members → that member → **Approved** → **Save**. The member now signs in, the header shows
   the Account menu, and its **Logout** signs them out.
3. As that member, run the **Create Recipe** wizard. The account page shows the recipe as "Pending review", and
   `/recipes` does not list it.
4. **Owner:** Content → Recipes → the draft → fill in anything it lacks → **Save and Publish**. About two seconds
   later a search on `/recipes` finds it, and the member's account page shows it without the badge.
5. **Owner:** delete that recipe, then empty the recycle bin.
6. Signed in, in both ramps (the header's theme toggle switches them), open the header's Account menu and then
   `/recipes/matcha-panna-cotta/green-tea-set`. The Logout entry sits in the menu like its neighbours, and the
   variant page's review editor, cook-note box and cooked toggle look like the rest of the page. No capture
   covers these.

Stop the gate site and delete `$GATE`.

- [ ] **Step 4: Everything, once more**

From the main checkout:

```bash
dotnet build KitchenCommandCenter.sln
(cd src/KCC.Web && yarn build:all && yarn test && yarn type-check)
node tests/scripts/run.mjs
```

Expected:
- `Build succeeded` with 0 warnings.
- Both bundles build, and Vitest and the type check pass.
- The combined run is green: unit, integration (one test at a time, the SQLite concurrency test included) and E2E
  (Phase 3's count plus 9). It opens its HTML report when it finishes.

- [ ] **Step 5: Close the phase**

Set this file's **Status** line to `done (<date>)`, then commit it:

```bash
git add docs/replatform/plans/2026-09-23-phase-4-members.md
git commit -m "Close Replatform Phase 4"
```

Phase 5 (Backoffice) is planned next, in this folder, against the code as it then stands. Its dashboard's review and
note edits and deletes go through `ContributionWrites` and publish `ReviewsChangedNotification`. Its Approve action
saves the member through the member editing service, which needs only the relations decorator.
