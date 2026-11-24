# Quickstart: Validating Performance Sweep, Phase 2

## Prerequisites

- `FONTAWESOME_NPM_AUTH_TOKEN`, `KCC_E2E_MEMBER_USERNAME` and `KCC_E2E_MEMBER_PASSWORD` set; Playwright's Chromium
  and WebKit installed.
- A worktree at `c1827268` for the "before" set (FR-001), built the same way.
- Both trees built with every bundle together. `pnpm build:all` builds the client, SSR and fallback bundles:

```bash
pnpm build:all && dotnet build KitchenCommandCenter.slnx -c Release --disable-build-servers
```

A timing run shares the machine with nothing else: every E2E run, timing run, capture and heavy build takes the
shared CPU lock (tasks.md "Conventions", plan.md D31).

## Scenarios

1. **Before (FR-001).** In the `c1827268` worktree: `pnpm perf:baseline`, then copy `tests/results/perf/results.json`
   to `specs/009-script-hydration-cost/before.json`; `pnpm perf:capture before`.
2. **Weight (US3, SC-003).** On the branch: `pnpm perf --check --before specs/009-script-hydration-cost/before.json`.
   Every case reads `js_first_load_gz_bytes` at or under 61,440 and `js_gz_bytes` at or under 117,266, and the report
   shows each metric before and after.
3. **Usable (FR-007).** In the same report, `hydrated_ms` is at or under 2,000 ms on the phone and 1,000 ms on
   desktop, and no later than `before.json`.
4. **Taps and frames (US1, US2, SC-001, SC-002).** `pnpm perf --check --page search-3,home,sign-in --profile phone
   --engine chromium` passes: every interaction within 100 ms, 95% of frames within 16.7 ms, no frame over 50 ms,
   `cls_scroll` 0. Repeat with `--profile desktop` in both engines. WebKit on the phone is no worse than `before.json`.
   If the owner kept the look under C2 (T056a), phone Chromium's interaction and frame numbers are no worse than
   `before.json` instead, and research.md records the gap. Search's reserved space appears only after `kcc:hydrated`,
   below the screen; a short first page with the footer on screen still reads no layout shift (C3, D19).
5. **Cause probes (FR-003).** With a site booted as `research.md` describes,
   `sh specs/009-script-hydration-cost/research/probes/ablate.sh / home base nowash reduced` prints the worst entry
   and long frames per variant. After the render work, `base` reads within 100 ms and no long frame.
6. **Look (US4, SC-006).** `pnpm perf:capture after`, `pnpm perf:diff before after`. Every flagged page goes to a
   `/speckit-preview-run` look round; none ships unapproved. `pnpm perf:scripts-off` passes on every page type.
7. **Behaviour (FR-012).** `node tests/scripts/run.mjs` passes, with the E2E suite's checks unchanged.
8. **Sidecar down.** With the sidecar stopped (or `VueSsr__Enabled=false`), every page still renders in the
   browser, keeps its component styles (the fallback's CSS is linked from its own manifest in production and comes
   from the dev server in development), and its controls work, as they do today.
9. **Later parts (FR-013).** With the phone profile, open Create Recipe and take every step: each Next answers within
   100 ms. With the network blocked after `kcc:hydrated`, Next still moves the progress bar at once, and the step
   fills in when the network returns.
10. **Guards (US5, SC-007).** Locally, against the guard classes CI runs (the owner's C1 answer; no branch is
   pushed): about 20 KB of gzipped script imported into `Main.ts` as an uncommitted scratch edit fails
   `DeterministicGuardTests` with page, profile, engine, ramp, budget and number; with the edit removed, three runs
   each of `DeterministicGuardTests` and `TimingGuardTests` on unchanged code pass three times (the owner's 2026-11-19
   decision, amending C1). Three runs of `pnpm perf --check` decide which timing metrics flip to target; locally they
   read against Phase 1's CI baselines, so baseline-mode metrics can fail on this machine. CI runs the same classes on
   the pull request.
11. **iPhone (FR-011, SC-005).** The owner, in Safari on an iPhone against this build: recipe search to three pages
   and back, a recipe and cook mode, the header's search and pad cards, at a fresh visit and two minutes in.

## Definition of done

The constitution's rows DoD D1 to D4, D6, D7, D9 and D10 apply. DoD D5 is N/A unless `tears.ts`, the schema, the
dictionary, the baseline content or a contributions entity changes, and none is planned. DoD D8 is N/A unless
`deploy/` changes. DoD D9 applies because T076 corrects the `Dockerfile`'s comment that names two bundles; no
instruction changes. The Dockerfile's frontend stage runs `pnpm build:all` and copies every `src/*/wwwroot`, so
`wwwroot/fallback/` reaches the image; T085 confirms it with `docker buildx bake --load` and `deploy/smoke-test.sh`.
