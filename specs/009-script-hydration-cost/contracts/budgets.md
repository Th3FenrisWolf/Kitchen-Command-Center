> This file supersedes `specs/006-performance-sweep/contracts/budgets.md` from Phase 2 on; 006's file stays as Phase 1's record.

# Contract: Budgets, Baselines and Results

## `tests/KCC.E2ETests/Features/Performance/budgets.json`

```json
{
  "budgets": [
    { "metric": "frames_within_16_7_pct", "scope": { "profile": "phone" }, "mode": "target", "limit": 95, "owner": "phase-1" },
    { "metric": "long_frames", "scope": {}, "mode": "target", "limit": 0, "owner": "phase-1" },
    { "metric": "interaction_ms_max", "scope": {}, "mode": "target", "limit": 100, "owner": "phase-1" },
    { "metric": "lcp_ms", "scope": { "profile": "phone" }, "mode": "baseline", "margin": { "ratio": 1.25, "floor": 150 }, "ciMode": "report-only", "owner": "phase-3" },
    { "metric": "hydrated_ms", "scope": { "profile": "phone" }, "mode": "target", "alsoBaseline": true, "limit": 2000, "margin": { "ratio": 1.25, "floor": 150 }, "ciMargin": { "ratio": 1.5, "floor": 150 }, "owner": "phase-2" },
    { "metric": "css_gz_bytes", "scope": {}, "mode": "baseline", "margin": { "pct": 2, "floorBytes": 1024 }, "owner": "phase-1" }
  ]
}
```

The entries above show the shape, not the final list. Rules:

- `mode: target` fails when the value is worse than `limit`.
- `mode: baseline` with `ratio` and `floor` fails when the value exceeds both `baseline x ratio` and
  `baseline + floor` (timing).
- A millisecond metric in `baseline` mode is judged against `baseline x runner` when the run and its baseline file
  both carry `calibrationMs`: `runner` is the run's calibration over the file's, to two places, and never below 1, so
  a slow runner relaxes the budget and a fast one never tightens it. Counts, bytes, scores and percentages are not
  scaled. A scaled failure ends `; runner x<factor>` inside its parentheses.
- `mode: baseline` with `pct` and `floorBytes` fails when the value exceeds `baseline + max(floorBytes, baseline x
  pct / 100)` (weight).
- Deterministic counts with no margin fail on any increase.
- A timing metric enters `target` mode only with a recorded three-of-three stability trial (the owner's 2026-11-19
  decision, amending C1).
- A millisecond metric in `target` mode is judged against `limit x runner`, with the same factor as in `baseline`
  mode, and a failure past a scaled limit ends `(runner x<factor>)`. Bytes and counts are not scaled.
- `alsoBaseline: true` on a `target` budget judges the metric against its `limit` and against its baseline with the
  budget's `margin` (`ciMargin` on CI). A failure names each side that failed, joined by `; `. `hydrated_ms` carries it,
  with a limit of 2000 ms on the phone and 1000 ms on the desktop.
- `notARegression: true` marks a metric whose rise the before-and-after report does not read as a regression:
  `js_later_gz_bytes` and `islands_hydrated`, which grow with the page's islands and later parts.
- When several entries match a case and metric, the last in the file wins, so a narrower entry placed after the general
  one overrides it.
- `js_first_load_gz_bytes` is `target` at 61440 and `js_gz_bytes` is `target` at 117266, with no margin; both are bytes
  and are never runner-scaled. `js_later_gz_bytes` is `report-only`.
- `islands_hydrated` is `baseline` with no margin, so it fails on any increase over `baseline.ci.json`.
- The trace probes exist on Chromium only. `restyled_on_tuck` is `target` at 16 on Chromium, and
  `wash_raster_on_interaction` is `target` at 0 on Chromium bloomed visits. `restyled_on_hover` is `report-only`.
  WebKit records each as a gap and is `report-only`.
- On the phone profile in Chromium, `interaction_ms_max`, `frames_within_16_7_pct`, `long_frames` and `frame_p95_ms`
  stay in `baseline` mode, in entries scoped `{ "profile": "phone", "engine": "chromium" }` that follow the general
  ones. The general ones are at `target`, and the scoped entries override them for that profile and engine.
- `cls` is `target` at 0.0001 (owner decision, 2026-11-18). The Font Awesome duotone `::before` glyph grows when the
  icon font arrives, about 0.00005 on account and 0.000055 on search-3; the entry is present at c1827268.
- `cls_scroll` on search-3 is `baseline` with no margin, so it fails on any increase over `baseline.json`
  (amended FR-008: a reader parked on the footer shifts no more than before). Every other page stays at the general
  `target` of 0.
- `ciMode`, when present, replaces `mode` in the CI guards (`DeterministicGuardTests` and `TimingGuardTests`, read
  against `baseline.ci.json`). `pnpm perf --check` ignores it and enforces `mode` against `baseline.json`. Every
  timing budget carries `ciMode: report-only` except `hydrated_ms` and `server_response_ms`, the only timing metrics
  steady across CI runners (research R4).
- `ciMargin`, when present, replaces `margin` in the CI guards the same way. `hydrated_ms` carries
  `{ "ratio": 1.5, "floor": 150 }` (research R4, Ubuntu 26.04 retake).

## Baseline files

`specs/006-performance-sweep/baseline.json` and `tests/KCC.E2ETests/Features/Performance/baseline.ci.json`:

```json
{
  "captured": "2026-10-27T00:00:00Z",
  "commit": "<sha>",
  "environment": { "chromium": "145.0.7632.6", "webkit": "26.0", "gpu": "swiftshader" },
  "calibrationMs": 0,
  "cases": {
    "search-3 · phone · chromium · light · bloomed": { "frame_p95_ms": 0, "layers": 0, "css_gz_bytes": 0 }
  }
}
```

## `results.json`

The baseline shape plus, per metric, `budget`, `baseline`, `delta` and `status` (`pass`, `fail`, `report-only`),
`runs` for timing metrics, and `runner` for a metric judged against a scaled baseline. At the top, beside the run's
`calibrationMs`, `baselineCalibrationMs` and the `runner` factor.
