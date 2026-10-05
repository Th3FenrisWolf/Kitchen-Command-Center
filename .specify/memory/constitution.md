<!--
Sync Impact Report
==================
Version change: 1.0.0 -> 1.1.0 (MINOR: relaxes the generated-file rule for uSync files; everything that
complied with 1.0.0 still complies)

Modified principles:
- IV. Tests First, Builds Clean: the uSync files leave the list of generated files. A person or an agent MAY
  write one by hand when the repository holds uSync files of the same kind that show its format, and a test
  that boots a fresh site MUST prove that it imports.

Modified sections:
- Definition of done, row D5: checks the boot test for each hand-written uSync file, and applies to a change
  to the dictionary or the baseline content as well

Added sections: none
Removed sections: none

Dependent templates and commands read this file at runtime and are not edited here:
- .specify/templates/plan-template.md: its Constitution Check takes its gates from the principles
- .specify/templates/spec-template.md, .specify/templates/tasks-template.md: no change needed
- speckit-card-implement: reads Gates, Ordering rule and Definition of done
- speckit-grill-run and the grilling preset: read Settled decisions and Decision ownership
- speckit-coderabbit-review: passes this file to CodeRabbit as context

Deferred placeholders: none

Follow-ups outside this file, carried from 1.0.0:
- CLAUDE.md: describe the Spec Kit flow beside "Superpowers: plans & specs"
- .specify/extensions/card/card-config.yml: create it with review.command, or card implement skips
  the CodeRabbit loop that Review and merge requires

Resolved since 1.0.0: shellcheck is installed, so row D8 can run on a deploy change.
-->

# Kitchen Command Center Constitution

## Core Principles

### I. Content Lives in the Backoffice

The owner edits every page, recipe, nav item and UI string in the Umbraco backoffice. Code holds
structure. The backoffice holds content.

- A feature MUST NOT hard-code content that the owner edits: copy, labels, links, nav entries or
  UI strings. UI strings are Umbraco dictionary items.
- Structure lives in code. Document types, data types, blocks and dictionary keys travel as uSync
  files under `src/KCC.Web/uSync/v17/`, and the typed models as ModelsBuilder output in
  `src/KCC.Web/Features/Models/Generated`. A schema change commits both together.
- A feature that needs content to render ships its structure, plus baseline content in the uSync
  content files where a fresh site needs it.

Rationale: backoffice editing is a hard requirement of the replatform
(`docs/replatform/specs/2026-09-21-replatform-off-xperience.md`, §2). Content in code needs a
deploy for every change of wording.

### II. Loose Leaf Is the Only Look

The public site wears the Loose Leaf identity. `docs/brand/loose-leaf.md` holds the brand, and
`docs/brand/kit.md` holds the engineering contract.

- Every public-site surface MUST follow the kit: `kcc-*` classes and role tokens, sheets torn with
  the generated presets, and no runtime JavaScript that draws.
- Where a general design skill or plugin disagrees with the brand docs, the brand docs win.
- Every visual change MUST hold in both ramps. Light is the default ramp. Text meets WCAG AA in
  both, and `tests/KCC.ViteTests/Features/Styles/contrast.test.ts` proves it.
- The kit's enforcement tests, listed under Enforcement in `docs/brand/kit.md`, MUST stay green.
  The `ALLOWLIST` in `retiredTokens.test.ts` stays empty unless a commit message gives the reason
  for an entry.

Rationale: the identity fails silently. A missing token, a scoped style that Razor cannot reach or
an unchecked dark ramp raises no error. It only renders a wrong page.

### III. SQLite Has One Writer

On SQLite, a transaction that has read cannot become the writer once another connection has
committed, and Umbraco then retries the write for about ten minutes.

- The four write guards in `src/KCC.Web/Features/Sqlite` MUST stay: `WriteLockedRelationsUpdate`,
  `WriteLockedRelateOnTrash`, `WriteLockedCacheInstructionService` and `IMemberWriteLock`.
- A write MUST take its lock before its first read. Contribution writes go through
  `ContributionWrites`. Member saves run inside `IMemberWriteLock.RunAsync`. A write that creates
  several content nodes opens one core scope and takes `Constants.Locks.ContentTree` first.
- A lock MUST be taken with `scope.WriteLock(lockId)`, never with a `TimeSpan` overload. In
  Umbraco 17.7, `WriteLock(TimeSpan, int)` takes a read lock and `ReadLock(TimeSpan, int)` takes a
  write lock.
- A change to a write path, a guard or a lock MUST run the whole integration suite:
  `dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`.

Rationale: a missing guard shows as a ten-minute stall in production, not as a failed test on most
runs. "SQLite writes" in `CLAUDE.md` holds the detail and the paths that the guards do not reach.

### IV. Tests First, Builds Clean

- Every behavior change and every bug fix MUST start with a failing test that the change then
  makes pass. A refactor or a style-only change adds no test, and every suite stays green.
- All six suites MUST pass: the unit, integration and E2E suites in TUnit, and the web, admin and
  contributions suites in Vitest. `node tests/scripts/run.mjs` runs all six, as CI does.
- A change MUST keep every structural hook that a test locates by: a `data-testid`, a role, an id
  or a `kcc-*` class. A new test SHOULD locate by `data-testid` or role, because markup and
  styling change with the brand.
- The build MUST be clean. Warnings are errors (`Directory.Build.props`). `yarn build:all` at the
  repository root builds every bundle together, never one side alone: scoped-style IDs hash each
  component's path and content, so a client bundle built apart from its SSR bundle loses its
  styles.
- A generated file MUST come from its generator and be committed with its source: `Tears.css` from
  `yarn tears`, the ModelsBuilder models and the EF Core migrations. Nobody edits one by hand.
- A uSync file MAY be written by hand, by a person or an agent, when the repository already holds
  uSync files of the same kind that show its format. A test that boots a fresh site MUST prove that
  the file imports.

Rationale: the suites are the one check that SSR, the backoffice clients and the SQLite site agree.
CI runs the same six suites on every pull request. uSync writes schema and dictionary files only when
someone saves in a signed-in Development backoffice, and baseline content has had no exporter since
its export endpoint was retired, so a hand-written uSync file is allowed and a boot test checks it.

### V. Cheap to Run, Cheap to Upgrade

- The running cost MUST stay under about $10 a month, all in. The site runs on a Raspberry Pi
  behind Cloudflare, and the same compose file runs on an x86 VPS, the documented exit.
- A feature that adds a recurring cost or a paid product MUST name the cost in its spec.
- A new major version of .NET, Umbraco, uSync, Node or any package is deliberate work with its own
  spec. Dependabot proposes minor and patch updates only.
- A public API MUST derive from a plain `ControllerBase`, never from `UmbracoApiController`, which
  Umbraco 18 removes. A backoffice endpoint MUST derive from `ManagementApiControllerBase`. New
  code SHOULD use the async content, member and dictionary services, which survive Umbraco 18's
  breaking changes.
- The move to Umbraco 21 LTS falls between its release on 2027-12-09 and the end of security
  support for 17 on 2028-11-27.

Rationale: the project left Xperience by Kentico over a licence of about $11,880 a year. Section
6.1 and section 13 of the replatform spec hold the upgrade and hosting detail, and
`docs/hosting/runbook.md` holds the operations.

### VI. Members Earn Trust

- Anyone can sign up, and the owner approves every account before it can sign in. The site sends
  no email.
- Every public endpoint that writes MUST validate the anti-forgery token. Sign-in, sign-up,
  password changes, contribution writes and submissions MUST also carry a per-client rate limit
  from `src/KCC.Web/Features/Security/RateLimits.cs`.
- A development endpoint, such as the recipe seeder, MUST answer only in the Development and
  Testing environments.
- A member's recipe or variant arrives as an unpublished draft. Only the owner publishes.
- A secret MUST live in user-secrets, an environment variable or the Pi's `.env`, never in a
  tracked file. `ConfigFileWritesTests` guards `appsettings.json` against the keys that Umbraco
  writes on its own.
- The backoffice at `/umbraco` MUST stay behind Cloudflare Access.

Rationale: members are friends, but the site is public. Owner approval, the anti-forgery check and
the rate limits stand between the internet and the content.

### VII. Code Explains Itself

- A comment MUST NOT explain what code does. Names, extracted functions and simpler code do that.
  A comment is allowed only where logic is written unconventionally for a reason that needs
  noting, or where the code would confuse a reader on its own.
- No comment, doc comments included, restates a name, signature or type, references an issue
  tracker or another project, narrates an edit, addresses a reader, promises a future change or
  keeps commented-out code. This holds in every file type: C#, Vue, TypeScript, CSS, Razor, YAML,
  Dockerfile and shell.
- A rename is a clean cutover. The new name replaces the old one in every live file, path, agent
  and skill in one change, with no "formerly" note. Git history keeps the old name. Finished
  records, such as completed plans, stay as written.
- A change that makes a line in `README.md`, `CLAUDE.md` or `docs/` untrue MUST correct that line
  in the same change.

Rationale: code is the one description that cannot drift from the behavior. Comments and docs that
restate it drift.

## Platform and Settled Decisions

### Stack

| Layer | Choice |
|---|---|
| CMS | Umbraco 17 LTS. Launch needs 17.8.0 or later, which fixes the SQLite lock bug |
| Runtime | .NET 10 (`global.json`), warnings as errors, StyleCop analyzers |
| Data | One SQLite file: Umbraco, plus the EF Core contributions tables |
| Schema | uSync 17. ModelsBuilder in `SourceCodeManual` mode, output committed |
| Search | First-party in-memory Lucene.NET index, rebuilt whole on change |
| Front end | Vue 3 with server-side rendering, Vite, Tailwind CSS 4 |
| Backoffice extensions | Lit, TypeScript and UUI, built by Vite into `wwwroot/App_Plugins/` |
| Packages | Yarn 1 workspaces at the repository root. Font Awesome Pro from its private registry |
| Tests | TUnit and Moq, Vitest, Playwright |
| Hosting | Raspberry Pi 5 on SSD, Docker Compose, Cloudflare Tunnel, Cloudflare Access, Tailscale |
| Delivery | GitHub Actions builds arm64 images, pushes them to private GHCR, and the Pi pulls them |
| Backups | Nightly SQLite and media backup to Cloudflare R2, a dead-man ping and a restore drill |

### Settled decisions

These are decided. A spec MUST NOT reopen one, and a grill never asks one. Changing one is an
amendment.

- Multilingual is dropped. No route carries a language prefix.
- Page Builder is dropped. Home composes from a Block List.
- Members sign up openly, the owner approves each account, and the site sends no email.
- Reviews, cook notes and cooked marks are EF Core tables in the Umbraco SQLite file.
- Search stays faceted, with drill-sideways counts.
- Loose Leaf is the identity, and light is the default ramp.
- Umbraco Forms, Umbraco Workflow and uSync.Complete are not bought.
- Node stays on major version 24, because its images from 26 on drop Yarn 1, which the build uses.
- The tag `xperience-final` stays runnable. The `mssql2022` container, the KCC Xperience database
  in it and the Kentico user-secrets of `KCC.Web` are never removed.

### Decision ownership

- The owner holds every product, brand, scope, platform and release decision. No client and no
  other person holds a decision in this project, and members hold none.
- This project names no gated owner. An open decision is a plain NEEDS CLARIFICATION marker for
  the owner, never a GATED one.

## Development Workflow and Quality Gates

### Flow

- A feature runs `/speckit-specify`, `/speckit-clarify`, `/speckit-plan`, `/speckit-tasks`, then
  `/speckit-card-implement`, which ends at an open pull request. `/speckit-checklist` and
  `/speckit-analyze` are optional checks. A bug runs `/speckit-bug-assess`, `/speckit-bug-fix`,
  then `/speckit-bug-test`.
- Spec Kit artifacts live in `specs/NNN-slug/`, tracked, and are committed with the work they
  describe.
- `.superpowers/` stays gitignored local scratch. Work that started there finishes there.
  `docs/replatform/` holds the tracked replatform spec and phase plans, as `CLAUDE.md` describes.
- The project uses no issue tracker. A spec carries no Jira line, and the Jira commands and the
  board step of card implement do not apply. Pull requests are the record.

### Gates

Card implement confirms each gate before implementation runs. An unmet gate stops the run.

1. `FONTAWESOME_NPM_AUTH_TOKEN`, `KCC_E2E_MEMBER_USERNAME` and `KCC_E2E_MEMBER_PASSWORD` are set.
   Yarn, the image build and the E2E suite need them.
2. The tools that the applicable definition-of-done rows need are installed: the .NET SDK that
   `global.json` names, Node 24 with Yarn 1 and the Playwright browsers. A deploy change also
   needs `shellcheck` and a running Docker.
3. The plan's Constitution Check passes, or its Complexity Tracking table justifies each
   violation.
4. No NEEDS CLARIFICATION marker is open in the spec, the plan or the tasks.
5. The ordering rule holds.

### Ordering rule

A feature too big for one pull request splits into one numbered spec per phase.

- A phase is specified and planned only after the previous phase passes its definition of done,
  against that phase's code.
- `/speckit-specify` runs from the previous phase's branch, so the phase branches stack.
- Before a phase's implementation runs, every task in the previous phase's `tasks.md` is checked
  off, and the previous phase's pull request is open or merged.
- Card implement opens each pull request against `main`. The owner retargets the stack with
  `gh stack` and merges it in order.

### Definition of done

Card implement runs every row. A row whose condition does not hold reports N/A. A code change is a
change to any file outside `specs/`, `docs/` and Markdown files.

| Row | Check | Command or method | Applies when |
|---|---|---|---|
| D1 | The solution builds with no warning | `dotnet build KitchenCommandCenter.slnx -c Release` | A code change |
| D2 | Every bundle builds together | `yarn build:all` at the repository root | A code change |
| D3 | The web front end type-checks, lints and formats | In `src/KCC.Web`: `yarn type-check`, `yarn lint`, `yarn format`. Commit what lint and format change | A change to a `.vue`, `.ts`, `.js` or `.css` file under `src/KCC.Web` or `tests/KCC.ViteTests` |
| D4 | All six suites pass | `node tests/scripts/run.mjs` at the repository root, after D1 and D2. It opens its HTML report, so an unattended run puts a no-op `open` first on `PATH` | A code change |
| D5 | Generated files match their source | Each generated file came from its generator and is committed with its source: `Tears.css` from `yarn tears`, the ModelsBuilder models with the uSync files, an EF Core migration. A boot test proves each hand-written uSync file | A change to `tears.ts`, the schema, the dictionary, the baseline content or a contributions entity |
| D6 | Touched pages hold in both ramps | Open each touched page in the Browser pane, in the light and the dark ramp, against the running site | A public-site visual change |
| D7 | The brand holds | The `brand-steward` agent reports no violation | A public-site visual change |
| D8 | The deploy scripts pass lint | `shellcheck -s sh deploy/smoke-test.sh deploy/deploy.sh deploy/kcc deploy/kcc-backup/kcc-backup` | A change under `deploy/` |
| D9 | The images build and boot | `docker buildx bake --load`, then `deploy/smoke-test.sh` | A change to `Dockerfile`, `docker-bake.hcl` or `deploy/` |
| D10 | The docs still tell the truth | Every line in `README.md`, `CLAUDE.md` or `docs/` that the change makes untrue is corrected | Every change |

### Review and merge

- Every change gets the local CodeRabbit review loop (`/speckit-coderabbit-review`) over its whole
  diff before its pull request opens, with this constitution and `CLAUDE.md` as context, for at
  most three rounds.
- Automation never merges. The owner reviews each pull request and squash-merges it into `main`
  once CI (`.github/workflows/build-and-test.yml`) passes. A merge to `main` deploys to the Pi.
- Every commit is authored as `Th3FenrisWolf <34248224+Th3FenrisWolf@users.noreply.github.com>`.
  The identity comes from the global git config, which a profile switch can change mid-session, so
  each commit passes it:
  `git -c user.name=Th3FenrisWolf -c user.email=34248224+Th3FenrisWolf@users.noreply.github.com commit`.

## Governance

- This constitution governs every change to Kitchen Command Center. `CLAUDE.md` is the runtime
  guidance file and holds the operational detail. `docs/brand/loose-leaf.md`, `docs/brand/kit.md`,
  `README.md` and `docs/hosting/runbook.md` hold the rest. When one of them conflicts with this
  constitution, the constitution wins, and the change that finds the conflict corrects the doc.
- Only the owner amends this constitution. An amendment runs `/speckit-constitution`, which
  rewrites this file with a Sync Impact Report, and lands through a pull request like any change.
- Versioning follows semantic versioning. MAJOR removes or redefines a principle, a gate, a
  settled decision or a definition-of-done row. MINOR adds a principle, a section, a gate or a row,
  or materially expands guidance. PATCH changes wording without changing a rule.
- Compliance: every plan's Constitution Check reads these principles. Card implement enforces the
  gates, the ordering rule and the definition of done. The CodeRabbit loop reads this file as
  context. A plan that breaks a principle MUST justify the break in its Complexity Tracking table,
  and an unjustified break fails the Constitution Check.

**Version**: 1.1.0 | **Ratified**: 2026-10-05 | **Last Amended**: 2026-10-05
