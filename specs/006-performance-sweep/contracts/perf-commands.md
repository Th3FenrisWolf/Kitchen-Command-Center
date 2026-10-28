# Contract: Performance Commands

Root `package.json` scripts. Each needs a Release build and `yarn build:all` first.

| Command | Does | Writes | Exit code |
|---|---|---|---|
| `yarn perf` | Boots a fresh seeded site and runs the full local matrix | `tests/results/perf/results.json`, `report.html`, `report.md` | 0, even over budget |
| `yarn perf --check` | The same, enforcing budgets | as above | 1 when a budget fails |
| `yarn perf:live` | Runs the matrix against the live site, signed out and as the measurement member, ten runs for server response | `tests/results/perf-live/…` | 0 whatever the budgets (spec FR-018); non-zero when a live variable is missing |
| `yarn perf:capture <ref>` | PNG captures for every case, scripts on and off | `tests/results/perf/captures/<ref>/` | 0 |
| `yarn perf:scripts-off <ref>` | Compares each scripts-on capture with its scripts-off partner | heat maps in `tests/results/perf/captures/<ref>/heat/scripts-off/` | 1 when a pair differs in any pixel or has no partner |
| `yarn perf:diff <before> <after>` | Perceptual diffs and the look round page | heat maps beside the after captures; `.mockups/006-performance-sweep/look-<n>-<page>.html` | 0 |
| `yarn perf:report --before <file> [--after <file>]` | Renders the report again from a saved run, with the before run beside it; no site boots | `report.html` and `report.md` beside the after results | 1 when an after budget fails |
| `yarn perf:baseline` | Runs the matrix and rewrites `specs/006-performance-sweep/baseline.json` | the baseline | 0 |

## Before and after

`--before <results.json>` on `yarn perf`, `yarn perf --check`, `yarn perf:live` or `yarn perf:report` reads an earlier
run, for example `specs/006-performance-sweep/baseline.json`, and the report shows it beside the new one (spec US4
scenario 3). Each case found in both runs gets one table: Metric, Before, After, Change (signed, with a percentage),
and the after run's Budget and Status. A metric present on one side shows a dash on the other. Cases in one run only
are listed under "Only in before" and "Only in after", and the failures listed are the after run's. `yarn perf` clears
`results.json` before it runs, so it copies the before file first and a before file at that path survives.
`yarn perf:report` takes the after run from `tests/results/perf/results.json`, or from `--after <file>`, in which case
the report is written beside that file. Without `--before` the report is the single-run one, unchanged.

Filters, all optional: `--page`, `--profile`, `--engine`, `--ramp`, `--visit`.

## Live

`yarn perf:live` boots no site. It measures `KCC_PERF_LIVE_URL`'s origin and stops before any request, naming each
variable that is not set (never a value).

- **Cases**: the local matrix and its filters, with the measurement member on member pages and signed out elsewhere.
  `--check` is ignored and no case is compared with `baseline.json`: live numbers are report-only (spec FR-018).
- **Server response**: before the matrix, each selected page's path is loaded ten times in Chromium on the desktop
  profile, unthrottled, each in a fresh context. `results.json` holds them under `serverResponse.<page>`: the path,
  the member, `minMs`, `medianMs`, `maxMs`, `runsMs`, and per run `cfCacheStatus` (`none` when absent) and
  `contentEncoding` (`identity` when absent). `search-3` shares `search-1`'s path and is not loaded twice.
- **Reads only**: every live context aborts any request that is not a `GET` or `HEAD`, and records it as a gap. The
  one exception is the sign-in, which may `POST` to the site's own origin. The run signs in once and reuses that
  storage state in every engine.
- **Bypass header**: `X-KCC-Perf: <KCC_PERF_BYPASS_TOKEN>` goes on every request to the live origin from a live
  context, and on no other origin. Local, baseline and capture runs never send it.
- **Recipe pages**: `recipe`, `variant-cook` and `add-variant` load the seeded recipes unless
  `KCC_PERF_LIVE_RECIPE` and `KCC_PERF_LIVE_VARIANT` name paths the live site has. A recipe page that answers 404,
  or a member page that lands on the sign-in page, is recorded as a gap, not measured. `search-3` loads up to three
  pages of results and records a gap when the site has fewer.
- No variable's value reaches `results.json`, the reports or the console: error text has the origin and the token
  removed.

## Captures

`yarn perf:capture <ref>` runs `LookCaptureTests` with `KCC_PERF_MODE=capture` and `KCC_PERF_REF=<ref>` (letters,
digits, `.`, `_` and `-`; the run fails without one). It writes flat PNGs to `tests/results/perf/captures/<ref>/`;
`perf:diff` reads `KCC_PERF_CAPTURES` in place of `tests/results/perf/captures` when set.

- **Cases**: every page, both profiles, both ramps, at a bare visit in Chromium with the animation timeline frozen
  at zero, and at a bloomed visit in Chromium and WebKit; images on, full motion. Each at the fixed visit seed, after
  `kcc:hydrated`, settled results, the case's state, `document.fonts.ready`, full page. A signed-out case opens with
  no member state, whatever ran before it. Chromium renders a full-page capture as one surface and repeats the top
  of the page past 8192 device rows, so a Chromium page taller than 4096 device rows is captured in strips of at
  most 4096 device rows and stacked into one PNG.
- **Names**: the case identity with `__` for ` · ` and `-` for a space, page first:
  `<page>__<profile>__<engine>__<ramp>__<visit>.png`, for example `search-3__phone__chromium__dark__bloomed.png`.
- **Scripts-off pairs**: for each bloomed case except `search-3` and `add-variant`, signed-out pages in light only and
  member pages in both ramps, two viewport captures of the page as it loads with the same suffix scheme:
  `<case>__scripts-on.png` (settled) and `<case>__scripts-off.png`. A member's ramp is saved through the settings
  page before its cases, so the server renders it.
- **Scripts-off check**: `yarn perf:scripts-off <ref>` passes a pair only when both PNGs have the same size and every
  pixel is identical in every channel (spec FR-012). It writes a heat map for each failing pair and lists any capture
  without its partner. `variant-cook`'s pair is the variant page as it loads: cook mode opens by script, so neither
  side shows it.

## Variables

| Variable | Used by | Holds |
|---|---|---|
| `KCC_E2E_MEMBER_USERNAME`, `KCC_E2E_MEMBER_PASSWORD` | local runs | the E2E member the seeder creates |
| `KCC_PERF_LIVE_URL` | `perf:live` | the public site's origin |
| `KCC_PERF_MEMBER_USERNAME`, `KCC_PERF_MEMBER_PASSWORD` | `perf:live` | the owner's measurement member on production |
| `KCC_PERF_BYPASS_TOKEN` | `perf:live` | the value of the `X-KCC-Perf` header the Cloudflare skip rule matches |
| `KCC_PERF_LIVE_RECIPE`, `KCC_PERF_LIVE_VARIANT` | `perf:live`, optional | a recipe path and a variant path on the live site, such as `/recipes/<recipe>/` and `/recipes/<recipe>/<variant>` |

None is stored in a tracked file.

## CI

- The E2E suite runs the deterministic guards on the key subset in both engines (spec FR-015).
- The `perf` job runs the timing guards in Chromium on SwiftShader, alone on its runner, one warm-up and three runs
  per case, and appends `report.md` to the job summary. It fails only on `hydrated_ms` and `server_response_ms`
  against `baseline.ci.json`; the other timing metrics carry `ciMode: report-only` and are reported, never enforced.
  The job's timeout is 45 minutes. Before and after the cases it times a fixed script, DOM and layout workload in its
  own browser, and a runner slower than the baseline's scales the millisecond baselines (`budgets.md`). A failure
  reads:
  `search-3 · phone · chromium · dark · bloomed: hydrated_ms 2000 > baseline 1528.4 (x1.25, +150 ms)`, or on a
  slow runner `search-3 · phone · chromium · dark · bloomed: hydrated_ms 2400 > baseline 1528.4 (x1.25, +150 ms;
  runner x1.23)`.
