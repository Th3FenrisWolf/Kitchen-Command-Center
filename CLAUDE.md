# Kitchen Command Center

Project-level instructions for Claude Code.

## Spec Kit

New work runs Spec Kit. The constitution, `.specify/memory/constitution.md`, governs every change through its flow,
gates, ordering rule and definition of done. Only the Spec Kit commands read it, so this section repeats the rules
every session needs.

- **A feature** runs `/speckit-specify`, `/speckit-clarify`, `/speckit-plan`, `/speckit-tasks`, then
  `/speckit-card-implement`, which recomposes the branch and ends at an open pull request, and never merges.
  `/speckit-checklist` and `/speckit-analyze` are optional checks.
- **A preview** shows the owner a proposal before the plan or the tasks fix it. `/speckit-preview-run` serves look
  rounds, built from the site's own stylesheet, in the Browser pane after clarify, and architecture rounds as Mermaid
  after plan, for as many rounds as the owner wants. The approved round goes into the spec folder.
- **A bug** runs `/speckit-bug-assess`, `/speckit-bug-fix`, then `/speckit-bug-test`.
- **A pull request** opens only from a recomposed branch. Development commits stay as they are until then. Card
  implement's close-out, and with it `/speckit-flow-run`, recomposes the branch once with the `recompose-branch` skill
  after the review loop and before the push, and the owner approves the new history in the close-out question. The
  `kcc-recompose` preset adds that step. A branch outside Spec Kit is recomposed the same way before its pull request
  opens. `.claude/hooks/require-recomposed-branch.sh` refuses `gh pr create` until `backup/<branch without its
  prefix>` holds the same tree under a different history.
- The artifacts live in `specs/NNN-slug/`, tracked, and are committed with the work they describe.

An extension update writes card implement and flow back without the preset's step, so run
`.specify/presets/kcc-recompose/scripts/reapply.sh` after every `/bizstream-ai:update`.

The Superpowers plugin is off in this repository: `.claude/settings.json` sets it to false, because its start-up hook
sends new work to its own brainstorming and plans. Spec Kit covers what it did: the constitution's test-first rule,
the definition of done, the bug flow, the CodeRabbit loop and `/speckit-preview-run`.

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

## SQLite writes

The site runs on SQLite, where a transaction that has read cannot become the writer once another connection has
committed, and Umbraco then retries the write for about ten minutes. Four guards in `src/KCC.Web/Features/Sqlite`
cover the paths KCC writes through. Never remove one.

- `WriteLockedRelationsUpdate` wraps Umbraco's post-save relations update for content, media and member saves.
- `WriteLockedRelateOnTrash` wraps Umbraco's relate-on-trash handler.
- `WriteLockedCacheInstructionService` wraps the cache-instruction sync. It takes its write lock only when
  instructions are pending.
- `IMemberWriteLock.RunAsync` takes the member write lock up front. Code that saves a member through Umbraco's
  sign-in manager, member manager or `IMemberService` runs inside it.

`SqliteComposer` registers the three that wrap Umbraco's own registrations by replacing them in place. If an Umbraco
upgrade moves one, the site refuses to boot rather than run unguarded.

The guards do not reach these paths inside Umbraco:

- `CacheInstructionsPruningJob` reads the newest instruction id and then deletes, every minute, outside
  `ICacheInstructionService`. Umbraco's other background jobs have not been surveyed.
- The backoffice's member password reset and unlock: `MemberEditingService.UpdateAsync` reaches
  `MemberUserStore.UpdateAsync`, which reads the member and then saves it, outside `IMemberWriteLock`.
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
- `RelationsWriteLockTests` checks that the relations and relate-on-trash handlers are wrapped. No test reproduces the
  relations update's own race deterministically. Without that guard, `SqliteConcurrencyTests` stalls in some full-suite
  runs but passes when run alone.

## Backoffice extensions

The backoffice's recipe editors and the Contributions dashboard are Lit clients in `src/KCC.Admin/Client` and
`src/KCC.Contributions/Client`, built by Vite from `packages/admin-client-config` into gitignored
`wwwroot/App_Plugins/` folders. Build them with the root `yarn build:all`. Three things fail silently or
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
wax washes, marker green as the one strong fill, Sono for every number and label. **The brand lives in
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
composites ink over paper plus each wash.

### The kit, and where CSS lives

`kcc-*` classes live in global `@layer components` CSS under `Features/Styles/Torn/`, **not** in component
`<style>` blocks: Razor-rendered views, home blocks and Vue components share them, and Razor cannot reach a
scoped block. Anything Razor also renders belongs in `@layer components`. A rule that must beat a Tailwind
*utility* sits **outside** `@layer` entirely (the ramp-swap rule at the end of
`Features/Styles/Torn/Kit.css`), because the `utilities` layer comes after `components`.

### The tear

Sheets are clipped with `clip-path: var(--tear)`; the presets in `Features/Styles/Torn/Tears.css` are
**generated** by `yarn tears` from `Features/Torn/tears.ts` and diffed by `tears.test.ts`. Never hand-edit
the CSS. No runtime JS draws anything; the SSR output is final. Two structural invariants:

- The `filter` (fibre + fall) sits on `.kcc-torn`, **outside** the clipped `.kcc-sheet`, or the shadow is
  clipped away with the paper.
- `.kcc-label`, `.kcc-tape` and `.kcc-tilewrap` are siblings of `.kcc-torn` inside `.kcc-slip`: outside the
  clip and outside the filter, so they are neither torn nor shadowed.
