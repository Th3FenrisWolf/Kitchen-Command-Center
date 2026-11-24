# Kitchen Command Center

Project-level instructions for Claude Code.

## Spec Kit

New work runs Spec Kit. The constitution, `.specify/memory/constitution.md`, governs every change through its flow,
gates, ordering rule and definition of done. Only the Spec Kit commands read it, so this section repeats the rules
every session needs.

- **A feature** runs `/speckit-specify`, `/speckit-clarify`, `/speckit-plan`, `/speckit-tasks`, then
  `/speckit-card-implement`, which ships the branch and ends at an open pull request, and never merges.
  `/speckit-checklist` and `/speckit-analyze` are optional checks.
- **A preview** shows the owner a proposal before the plan or the tasks fix it. `/speckit-preview-run` serves look
  rounds, built from the site's own stylesheet, in the Browser pane after clarify, and architecture rounds as Mermaid
  after plan, for as many rounds as the owner wants. The approved round goes into the spec folder.
- **A bug** runs `/speckit-bug-assess`, `/speckit-bug-fix`, then `/speckit-bug-test`. Bug fix publishes
  through `ship-branch` too.
- **A pull request** opens only through the `ship-branch` skill with `--no-jira`. Development commits stay as they
  are until then. Card implement's close-out, and with it `/speckit-flow-run`, runs it in this session after the
  commit: two review rounds with a verifier, one CodeRabbit round and an acceptance check, then one recomposition
  with the `recompose-branch` skill, all before the push. A regression, an unfixed finding or an unmet criterion
  blocks the publish, and the owner approves the publish in the close-out question. The pull request then gets a
  watch. The branch is never recomposed again after its pull request opens: a later fix is one commit on top. The
  `kcc-ship` preset adds these steps, and they need `~/.claude/skills/ship-branch`. A branch outside Spec Kit
  ships the same way. `.claude/hooks/require-recomposed-branch.sh` refuses `gh pr create` until `backup/<branch without its
  prefix>` holds the same tree under a different history.
- **Review triage.** `/speckit-flow-run` and `/speckit-bug-fix` pass `--triage`, so ship-branch sizes the review to
  the diff: `full`, `light` (one round) or `minimal` (one reviewer, no CodeRabbit). Card implement run by hand stays
  `full`. A change to one of these risk paths for this triage keeps the review `full` at any size:
  `src/KCC.Web/Features/Sqlite/`, `src/KCC.Web/Features/RecipeImport/`, `src/KCC.Web/Features/Security/`, any code
  that takes `IMemberWriteLock` or `scope.WriteLock`, member sign-in and saves, `deploy/`, `Dockerfile`,
  `docker-bake.hcl`, `.github/workflows/`, `.claude/hooks/`, `Directory.Packages.props`, `global.json`, `package.json`
  and `pnpm-lock.yaml`. These paths are generated for this triage, and their lines are not counted:
  `src/KCC.Web/Features/Styles/Torn/Tears.css`, `src/KCC.Web/Features/Styles/Torn/Washes.css` and the baked wash
  PNGs.
- The artifacts live in `specs/NNN-slug/`, tracked, and are committed with the work they describe.
- **The project board** is the status view. `.github/workflows/project-board.yml` runs `tools/project-board/sync.mjs`
  from `main` on every push, pull request and morning, on the owner's issue events and on `building` label events, so a
  branch that changes the sync never writes the live board. It reads `specs/` and `.specify/bugs/` on every branch,
  with the pull requests, and sets each card's stage: Idea, Specified, Building (from the first ticked task, or a
  bug's fix on its branch, or the `building` label), In review, Done. It owns every card with a **Key**, rewriting
  its title, its fields and the `<!-- sync -->` block of its body, and never touches a draft card without one. It
  refuses to write when its read finds no spec and no bug. When two spec folders on any branches share a number, the
  run fails: it names the folders and their branches, and leaves each folder's card, and every card of a phased
  feature whose table on any branch names the number, as they are until all but one is renumbered. A card it creates
  starts in the Current iteration, and it fills a blank iteration on a card's issue with Current but never changes a
  set one, so a card moved to Future stays there.
- **Ideas.** Every issue the owner opens is an idea. The sync files it on the board with Status Idea, Kind Idea and
  Iteration Current, fills only blank fields, archives the card when the issue closes, and restores it when the issue
  reopens or when the card is archived by hand while the issue is open. It never touches anybody else's issue.
  `/speckit-specify #NNN` and `/speckit-bug-assess #NNN` read the idea through `tools/project-board/idea.mjs` and
  write its Idea line, and the sync then makes the idea's card the spec's or bug's. Setting a Key by hand on a card
  still works.
- A card the sync creates in Idea is a draft. Once a card leaves Idea, the sync turns it into an issue in this repository, closes the
  issue while the card is Done and reopens it if the card leaves Done, and makes a later phase's issue a sub-issue
  of phase 1's. A draft card with no Key stays the owner's: when an idea becomes a spec, set its Key
  to the spec's `NNN-slug` and the sync takes it over. Run it by hand with `node tools/project-board/sync.mjs
  --dry-run` after `git fetch`; `--setup` creates missing fields and options. `PROJECT_BOARD_TOKEN` is a classic
  token with the `project` and `public_repo` scopes.
- **The `building` label.** `/speckit-card-implement` and `/speckit-bug-fix` add it to the card's issue when they
  start, through `tools/project-board/pickup.mjs`, and push nothing. The sync shows a Specified card that carries it as
  Building, and removes it once a pushed branch or pull request carries the stage. The owner can add or remove it by
  hand.
- A spec's `**Status**:` line links to its board card and never names a stage. A phased feature carries a
  `| Phase | Spec | Covers |` table in each phase's spec, `later` for a phase not yet specified, and the board shows
  an Idea card of Kind Phase for each `later` row.

An extension update writes card implement, flow, bug fix, specify and bug assess back without the preset's steps, so run
`.specify/presets/kcc-ship/scripts/reapply.sh` after every `/bizstream:update`. It restores the idea reads, the pickup
step and the ship steps.

The Superpowers plugin is off in this repository: `.claude/settings.json` sets it to false, because its start-up hook
sends new work to its own brainstorming and plans. Spec Kit covers what it did: the constitution's test-first rule,
the definition of done, the bug flow, `ship-branch` through the `kcc-ship` preset, and `/speckit-preview-run`.

Several features can be in flight at once:

- `.specify/feature.json` names the active feature and is local to each checkout. Spec Kit reads the active feature
  from it or from `SPECIFY_FEATURE_DIRECTORY`, never from the branch name, so a manual branch switch leaves it on the
  old feature. Set `SPECIFY_FEATURE_DIRECTORY=specs/NNN-slug` for a command aimed at another one.
- `/speckit-specify` creates its `NNN-slug` branch from the current `HEAD` and switches to it. Start an unrelated
  feature from `main`, in its own worktree while another feature is in progress in this checkout. A later phase of
  the same feature starts from the previous phase's branch, as the ordering rule sets.
- `/speckit-specify` numbers a feature from every local and remote branch and the `specs/` folders.
  `/speckit-batch-run specify` numbers from the `specs/` folders on disk only, so a batch on a branch that lacks the
  newest spec reuses its number.
- `/speckit-card-implement` creates a missing feature branch from `origin/main`. A spec that a batch wrote on another
  branch reaches `main` before its card is built.
- A plan reads the code on its own branch. Run `/speckit-plan` and `/speckit-tasks` just before a feature is built,
  so the plan reads the code it changes.

## Past Superpowers work

`.superpowers/` keeps the plans, specs and brainstorm files of the work done before Spec Kit. It is gitignored local
scratch: never stage or commit it, and start nothing new there. A plan there or in `docs/replatform/plans/` that names
a Superpowers skill still runs: follow its steps by hand.

**Exception: the replatform off Xperience.** Its spec, phase plans and reference screenshots are tracked in
`docs/replatform/`, so they travel with the branch:

- **Spec** → `docs/replatform/specs/2026-09-21-replatform-off-xperience.md`
- **Phase plans** → `docs/replatform/plans/YYYY-MM-DD-phase-N-<name>.md`, one self-contained file per phase
- **Reference set** → `docs/replatform/reference/xperience-final/`: 48 PNGs of the last Xperience version, and a
  `NOTES.md` listing the differences to expect

Commit changes to them with the work they describe. Everything else in the replatform, such as SDD ledgers and
gate-capture output, stays in `.superpowers/`.

## Vue SFC `<style>` blocks

Write component CSS in a `<style>` or `<style scoped>` block. A standalone CSS file is not required, and
adding one to work around missing styles is a symptom of a broken build, not a fix.

Style-block CSS never reaches the page as part of the SSR-rendered HTML, so each environment delivers it
separately:

- **Production** — the client build extracts it into one CSS asset per component chunk. `IslandHeadLinks` reads
  `wwwroot/.vite/manifest.json` and `Layout.cshtml` writes in `<head>` a stylesheet link for the CSS closure of every
  component the render used, islands and server-only components alike, deduplicated after `Main.css`, and a
  `modulepreload` for every island's chunk closure. A `<link rel="stylesheet" vite-href="/Features/Main.ts">` adds the
  entry's own import graph. The manifest is a dot-folder file that `dotnet publish` skips unless `KCC.Web.csproj`
  includes it, as it does: without it the published site links no CSS at all.
- **Fallback** — when the sidecar returns no HTML (SSR off, a failure, a timeout or an open circuit), `#app` is empty,
  `#server-content` carries the three region templates, and `Layout.cshtml` links the fallback build in
  `wwwroot/fallback/` instead of `Main.ts`. That build keeps the template compiler and renders the whole page in the
  browser. Vite.AspNetCore reads only `wwwroot/.vite/manifest.json`, so `FallbackManifest` reads
  `wwwroot/fallback/.vite/manifest.json` and `FallbackLinks` writes the script and the stylesheet links from it. A
  missing or unreadable manifest is logged and not cached, and the page then loads without a script. In development
  the page links `Features/Fallback.ts` from the dev server.
- **Development** — no such asset exists, so the SSR sidecar walks the Vite SSR module graph after
  rendering, compiles each style module through the client pipeline, and returns the CSS alongside the
  HTML. `SsrHtmlContent` inlines it as `<style data-ssr-styles>`, and `Main.ts` removes that tag once
  every island has hydrated so HMR-updated head styles win.

**Always build all three bundles together — `pnpm build:all` (client, SSR and fallback), never one side alone.**
Production scope IDs hash each component's path *and* its content, so a bundle built against different component
source than another emits `data-v-` attributes the CSS has no selectors for, and every scoped component
silently loses its styles.

## Islands

The browser hydrates each interactive component on its own instead of the whole page.

- **Which components.** Every `*.Component.vue` under `src/KCC.Web/Features/` is a global component that Razor names by
  its file name, and an island unless it declares `defineOptions({ serverOnly: true })`. A server-only component is
  rendered by the sidecar and its script is never fetched: `AppFooter`, `Card`, `UnderlineLink`, `KccWashes`,
  `Breadcrumbs`, `ResourceString` and `SmallHero`. It takes no `<style>` block, because in development its CSS reaches
  the page only through `<style data-ssr-styles>`, which `Main.ts` removes; `registry.test.ts` fails on one.
- **Props only.** An island Razor places takes attributes and `:prop="@Vue.Prop(...)"` bindings; the sidecar fails the
  render, naming the component, on slot content or a function. A server-only component may take slot content.
- **Explicit imports.** Inside a `.vue` file every component is imported: the client has no global registry, and a
  tag resolved at runtime renders nothing. `explicitImports.test.ts` fails on a component tag the file does not import.
- **The flow.** The sidecar wraps each island in `<kcc-island data-island="N">` (`display: contents`) and
  `SsrHtmlContent` writes `#server-content` as `{ isPreview, bloom, islands }`, with no region HTML. `Main.ts` hydrates
  every `<kcc-island>` from its own chunk with `createSSRApp`, in parallel, then marks `kcc:hydrated`, which is the
  moment the page is usable. The client is runtime-only Vue. The eager glob over every component lives only in
  `Features/Ssr/ServerComponents.ts`, for the server entry and the fallback; importing it from `Main.ts` would pull every
  component into the entry chunk. A chunk that fails to load leaves its island's server HTML in place.
- **Later parts.** A part a page does not show on load is made with `laterPart(loader, pending)` in
  `Features/Islands/laterParts.ts` and prefetched in idle time after `kcc:hydrated`. A reader who gets there first sees
  the part's own empty sheet with `aria-busy="true"`, and the part fills in when it arrives. A load that rejects, or
  has not settled after 15 s, is retried after 1 s and then 2 s; after the third attempt the part's error state
  shows a danger well and a Retry button in the same sheet. Cook mode is a later part with its own pending dialog.
- **The wizard.** Every `Components/RecipeWizard/*Step.vue` except `StartStep` is a later part, found by one
  `import.meta.glob` in `wizardSteps.ts`. A new step is `<Name>Step.vue` plus
  `const <Name>Step = laterStep('<Name>Step')` in `RecipeWizard.vue`, never a static import; `wizardSteps.test.ts`
  fails on one. The step a wizard opens on is resolved before hydration through the island's
  `defineOptions({ beforeHydrate })`, so the server's step is never hydrated as its pending sheet.
- **Search.** After the island mounts, the browser holds the space the next page of results needs below the screen
  (`useReservedSpace`), so a page appended to the list moves nothing a reader can see. The server renders none of it,
  and the next page loads at once.
- **The tuck.** `useTuckOnScroll` toggles `.is-tucked` on `<main>` and writes `--pad-h` there only when it measures;
  a sticky neighbour adds `var(--pad-h)` to its top under `main:not(.is-tucked)`.

### Script and interaction budgets

`tests/KCC.E2ETests/Features/Performance/budgets.json` holds them; `README.md` explains the modes.

- **Script weight, a target.** `js_first_load_gz_bytes` is 61,440 B: the gzip of the same-origin scripts whose resource
  entry starts before `kcc:hydrated`, a `modulepreload`ed chunk included. `js_gz_bytes`, the visit total, is
  117,266 B, and `js_later_gz_bytes` is the remainder, report-only. `DeterministicGuardTests` reads both over the key
  subset plus create-recipe and add-variant, in both engines.
- **Usable time, a target and a baseline.** `hydrated_ms` is 2,000 ms on the phone and 1,000 ms on the desktop, scaled
  by the runner's calibration factor, and `alsoBaseline` holds it to its baseline as well.
- **Probes, Chromium only.** `islands_hydrated` may not rise above its baseline. `restyled_on_tuck` is at most 16 and
  `wash_raster_on_interaction` is 0 on bloomed visits; `restyled_on_hover` is report-only. WebKit has no trace and
  reports a gap.
- **The carried gap.** The phone Chromium profile's `interaction_ms_max`, `frames_within_16_7_pct`, `long_frames` and
  `frame_p95_ms` stay at baseline mode: SwiftShader rasterises in software past the CPU throttle, and the branch
  meets 100 ms on hardware raster. The evidence is in `specs/009-script-hydration-cost/mockups/README.md`.

## SQLite writes

The site runs on SQLite, where a transaction that has read cannot become the writer once another connection has
committed, and Umbraco then retries the write for about ten minutes. Five guards in `src/KCC.Web/Features/Sqlite`
cover the paths KCC writes through. Never remove one.

- `WriteLockedRelationsUpdate` wraps Umbraco's post-save relations update for content, media and member saves.
- `WriteLockedRelateOnTrash` wraps Umbraco's relate-on-trash handler.
- `WriteLockedCacheInstructionService` wraps the cache-instruction sync. It takes its write lock only when
  instructions are pending.
- `IMemberWriteLock.RunAsync` takes the member write lock up front. Code that saves a member through Umbraco's
  sign-in manager, member manager or `IMemberService` runs inside it.
- `WriteLockedUserService` and `WriteLockedBackOfficeUserStore` take the same member write lock before the first read
  of a backoffice user write. The first wraps every write method of `IUserService`. The second replaces Umbraco's
  `BackOfficeUserStore` and locks its `CreateAsync` and `UpdateAsync`, which sign-in and the token endpoint reach
  outside the user service.

`SqliteComposer` registers the four that wrap or replace Umbraco's own registrations, in place. If an Umbraco upgrade
moves one, the site refuses to boot rather than run unguarded.

`ImportSpendLedger` in `src/KCC.Web/Features/RecipeImport` writes Umbraco's key-value table through
`IKeyValueService`. `TryReserve` and `Settle` open one core scope and take `Constants.Locks.KeyValues` before their
first read.

The guards do not reach these paths inside Umbraco:

- `CacheInstructionsPruningJob` reads the newest instruction id and then deletes, every minute, outside
  `ICacheInstructionService`. Umbraco's other background jobs have not been surveyed.
- The backoffice's member password reset and unlock: `MemberEditingService.UpdateAsync` reaches
  `MemberUserStore.UpdateAsync`, which reads the member and then saves it, outside `IMemberWriteLock`.
- The backoffice user store's other writes, such as delete, external logins and tokens, and a user write called
  inside an outer scope that has already read, which takes the lock too late.
- `ObtainWriteLock` in Umbraco's EF Core SQLite lock (`SqliteEFCoreDistributedLockingMechanism`) does not await its
  own task, so a contributions write whose lock wait times out carries on unlocked.

Contribution writes go through `ContributionWrites`, which takes its lock before its first read. A write that creates
several content nodes opens one core scope and takes `Constants.Locks.ContentTree` before its first read, as
`RecipeSubmissions` does. Take a lock with `scope.WriteLock(lockId)`, never with a `TimeSpan` overload: in Umbraco 17.7,
`WriteLock(TimeSpan, int)` takes a read lock and `ReadLock(TimeSpan, int)` takes a write lock.

The tests are in `tests/KCC.IntegrationTests/Features/Sqlite`. Run the whole integration suite after touching any of
the above, with `dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj` (`dotnet test` reports
"Zero tests ran" in this repository):

- `SqliteConcurrencyTests` races review, cook-note and cooked writes, member saves, a content save and publish, and
  index rebuilds. It fails when the member write lock is removed.
- `CacheInstructionSyncTests` and `TrashRaceTests` reproduce their stalls deterministically and fail without their
  guards. `CacheInstructionSyncTests` also checks that an idle sync takes no write lock.
- `UserSaveRaceTests` reproduces the user save stall deterministically, for creating a user and for recording a
  sign-in, and fails without its guard. It also checks that the user service and the user store are wrapped.
- `RelationsWriteLockTests` checks that the relations and relate-on-trash handlers are wrapped. No test reproduces the
  relations update's own race deterministically. Without that guard, `SqliteConcurrencyTests` stalls in some full-suite
  runs but passes when run alone.

In the integration tests only, `CommitLog` records every commit on Umbraco's connections in a ring of the last 64. A
test that fails with SQLite error 5 prints the ring and the extended error code, naming the writer that stranded it.

## Backoffice extensions

The backoffice's recipe editors and the Contributions dashboard are Lit clients in `src/KCC.Admin/Client` and
`src/KCC.Contributions/Client`, built by Vite from `packages/admin-client-config` into gitignored
`wwwroot/App_Plugins/` folders. Build them with the root `pnpm build:all`. Three things fail silently or
misleadingly:

- A Management API call through `umbHttpClient` sends no token unless it passes
  `security: [{ scheme: 'bearer', type: 'http' }]`. The endpoint answers 401, and the backoffice reads that as an
  expired session: it shows its "session timed out" login prompt, with no error toast.
- When a library `KCC.Web` already references gains its first controller, an incremental build keeps a stale
  `src/KCC.Web/obj/*/net10.0/KCC.Web.MvcApplicationPartsAssemblyInfo.cs` and every new route answers 404. Delete
  `KCC.Web.MvcApplicationPartsAssemblyInfo.*` (the `.cs` and its `.cache`) and rebuild.
- Never name a file or folder `icon`: `.gitignore`'s macOS `Icon` rule ignores it.

## Loose Leaf design language

The public site uses the Loose Leaf identity: torn-paper sheets on a lilac-grey desk, a 24px rule, eight
ink washes that bloom onto bare sheets over a visit, marker green as the one strong fill, Sono for every number and label. **The brand lives in
`docs/brand/loose-leaf.md`; the engineering contract in `docs/brand/kit.md`.** Read the contract before
styling anything; use the `loose-leaf` skill for the quick reference, the `kit-builder` agent to convert
a surface and the `brand-steward` agent to review one. The invariants below are the ones that break the
build or the brand silently, so they stay here as well.

General design skills and plugins, such as impeccable, taste-skill, ui-ux-pro-max and frontend-design, do not set
the public site's look. Where one disagrees with the brand docs, the brand docs win. Some of them ban devices this
identity uses on purpose: the kicker above a heading (`kcc-kick`), Sono caps labels, the stats row, the tilt and the
tear. Those stay.

### Tokens

Role tokens live in `Features/Styles/TailwindConfig.css` under **`@theme static`** with the **light** values;
the dark ramp overrides them in `Features/Styles/Torn/Tokens.css` under `:root[data-theme='dark']`. The
`static` is load-bearing: Tailwind 4 prunes theme variables no utility references, and the kit CSS and the
dark ramp read `--color-*` directly. A signed-in member's saved Light or Dark is rendered by the server:
`Layout.cshtml` writes `data-theme` from the HttpOnly `kcc-ramp` cookie and leaves the pre-paint inline script
out. Otherwise that script sets `data-theme` from `prefers-color-scheme`, else **light**. **Check both ramps for
any visual change**; `tests/KCC.ViteTests/Features/Styles/contrast.test.ts` is the living contrast table and also
composites ink over paper plus a two-wash stack at its peak, which pins the dark `--wash-strength` and caps the light one.

### The kit, and where CSS lives

`kcc-*` classes live in global `@layer components` CSS under `Features/Styles/Torn/`, **not** in component
`<style>` blocks: Razor-rendered views, home blocks and Vue components share them, and Razor cannot reach a
scoped block. Anything Razor also renders belongs in `@layer components`. A rule that must beat a Tailwind
*utility* sits **outside** `@layer` entirely (the ramp-swap rule at the end of
`Features/Styles/Torn/Kit.css`), because the `utilities` layer comes after `components`.

### The tear

Sheets are clipped by the `clip-path` that `Tears.css` writes once per preset on grouped child selectors; the presets in `Features/Styles/Torn/Tears.css` are
**generated** by `pnpm tears` from `Features/Torn/tears.ts` and diffed by `tears.test.ts`. Never hand-edit
the CSS. No runtime JS draws anything; the SSR output is final. The structural invariants:

- The `filter` (fibre + fall) sits on `.kcc-fall`, the slip's child right before `.kcc-torn`, **outside** the
  clipped `.kcc-sheet`, or the shadow is clipped away with the paper. Its inner span carries the sheet's tear. A
  slip's `.kcc-torn` carries no filter: a filter around the sheet redraws on every frame of every wash blooming in it.
- `.kcc-label` and `.kcc-tilewrap` are siblings of `.kcc-torn` inside `.kcc-slip`: outside the clip and outside
  the fall, so they are neither torn nor shadowed. The `.kcc-tape` sits inside the tilewrap: tape holds only a
  tile, and a sheet is never taped. The one exception is a `.kcc-tilewrap--flow`, which sits in the sheet's content
  flow with its tape.
- Every sheet lies on a blank: `<span class="kcc-blank" aria-hidden="true"><span></span></span>`, the slip's
  **last** child, outside the top sheet's clip and fall, with its own filter on `.kcc-blank` and its clip on the
  inner span. The header pad is the exception: its `.pad-page` slip is the top sheet's blank, and a pad card holds
  none.
- A wash is never placed by hand: `KccSheet` and `KccWashes` render up to four per sheet from the visit's seed
  and the sheet's `wash-key`, two deep at most, and the kit CSS blooms them in 45 steps from the elapsed time
  `Layout.cshtml` writes as `--wash-elapsed`. A wash rests at its bloom's end and fills backwards only, so hydration
  keeps the first paint. A wash is chalk: its mask is a 128px chalk tile cut with one of eight lobed silhouettes (`Washes/wash-chalk.png`,
  `wash-blob-0.png` to `wash-blob-7.png`), baked by `pnpm washes` from `Features/Bloom/bakeWashes.ts` and named by
  `Features/Styles/Torn/Washes.css`, which is **generated** too. Never hand-edit them. The silhouette's padding is
  `--wash-span`, `--wash-gain` lifts the opacity to make up for the chalk's gaps, and `--wash-flip` mirrors each wash.
  A wash takes no filter, so neither does its chalk tile or silhouette: a filter on a blooming wash made WebKit redraw it on every step. The client build keeps the
  textures out of the stylesheet as cached files (`assetsInlineLimit` in `vite.config.ts`), and `Layout.cshtml` preloads them with
  `crossorigin` in production so a bloomed first paint has them. Every `KccSheet` and `KccWashes` needs a stable `wash-key` (a list item's own id, never
  its index); `keys.test.ts` scans for it.
- Grain and crayon lie on the desk, the washes and the tiles only; a wash's texture is its baked chalk tile, not a grain tile. The desk's are `#app`'s own background layers
  (`background-blend-mode`, each ramp's strength baked into the SVG in `Tokens.css`), not overlay elements. Paper, text, controls, labels, wells, blank sheets
  and tape carry none; `grain.test.ts` fails on any other use. The tape's weave is its own material.
