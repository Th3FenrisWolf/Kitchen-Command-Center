# Data Model: Performance Sweep, Phase 1

No database change. These are the harness's records, held in JSON files.

## Case

One measured page profile (spec Key Entities).

| Field | Values |
|---|---|
| `page` | `home`, `search-1`, `search-3`, `recipe`, `variant-cook`, `create-recipe`, `add-variant`, `account`, `sign-in`, `settings`, `error`, `status-404` |
| `profile` | `phone`, `desktop` |
| `engine` | `chromium`, `webkit` |
| `ramp` | `light`, `dark` |
| `visit` | `bare` (start now), `mid` (120 s, timing only), `bloomed` (start 400 s back, clamped to 300 s) |
| `images` | `on`, `off` |
| `motion` | `full`, `reduced` |
| `member` | `none`, `e2e`, `live` |

Identity: the tuple, written as `page · profile · engine · ramp · visit` plus `images`, `motion` and `member` when
they differ from `on`, `full` and the page's default. Member pages always run with a member; signed-out pages never
do.

Validation: `webkit` cases record no throttled metric and are labelled unthrottled; a signed-out `dark` case is
skipped by the scripts-off comparison (spec FR-012).

## Metric

| Field | Meaning |
|---|---|
| `name` | e.g. `lcp_ms`, `hydrated_ms`, `cls`, `interaction_ms_max`, `frame_p95_ms`, `frames_within_16_7_pct`, `long_frames`, `idle_gpu_pct`, `layers`, `layer_area_mpx`, `filtered_elements`, `animated_filtered`, `filtered_area_px2`, `css_gz_bytes`, `js_gz_bytes`, `server_response_ms`, `font_bytes`, `image_bytes` |
| `kind` | `deterministic` or `timing` |
| `unit` | `ms`, `pct`, `count`, `bytes`, `px2`, `mpx` |
| `value` | median of the runs for timing, the single value for deterministic |
| `runs` | each run's value, timing only |

## Budget

| Field | Meaning |
|---|---|
| `metric` | a metric name |
| `scope` | every case, or a filter on page, profile or engine |
| `mode` | `target` or `baseline` |
| `limit` | the target value, when `mode` is `target` |
| `margin` | for `baseline`: a ratio and a floor (timing), or a percentage and a byte floor (weight) |
| `owner` | `phase-1`, `phase-2` or `phase-3`: the phase that brings the metric within target |

State: a budget starts in `baseline` mode; the phase that brings it within target flips it to `target` in the same
change and records the stability trial (spec FR-016, SC-006).

## Baseline

A map from case identity to metric values, captured once before any fix. Two files: `baseline.json` in the spec
folder for the local matrix, `baseline.ci.json` beside the guards for CI values. Rewritten only by
`yarn perf:baseline`.

## Report

`results.json`: the run's environment (engine versions, GPU mode, machine, commit) and every case's metrics, each
beside its budget, baseline, delta and status (`pass`, `fail`, `report-only`). Rendered to `report.html` and
`report.md`.

## Capture

A PNG per case for the look check, named by case identity, under `tests/results/perf/captures/<ref>/`, plus a
`scripts-off` capture per case where the comparison applies. A diff pairs two refs and writes a heat map per case.
