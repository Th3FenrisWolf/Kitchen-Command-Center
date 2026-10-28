# Contract: Budgets, Baselines and Results

## `tests/KCC.E2ETests/Features/Performance/budgets.json`

```json
{
  "budgets": [
    { "metric": "frames_within_16_7_pct", "scope": { "profile": "phone" }, "mode": "target", "limit": 95, "owner": "phase-1" },
    { "metric": "long_frames", "scope": {}, "mode": "target", "limit": 0, "owner": "phase-1" },
    { "metric": "interaction_ms_max", "scope": {}, "mode": "target", "limit": 100, "owner": "phase-1" },
    { "metric": "lcp_ms", "scope": { "profile": "phone" }, "mode": "baseline", "margin": { "ratio": 1.25, "floor": 150 }, "ciMode": "report-only", "owner": "phase-3" },
    { "metric": "css_gz_bytes", "scope": {}, "mode": "baseline", "margin": { "pct": 2, "floorBytes": 1024 }, "owner": "phase-1" }
  ]
}
```

The entries above show the shape, not the final list. Rules:

- `mode: target` fails when the value is worse than `limit`.
- `mode: baseline` with `ratio` and `floor` fails when the value exceeds both `baseline x ratio` and
  `baseline + floor` (timing).
- `mode: baseline` with `pct` and `floorBytes` fails when the value exceeds `baseline + max(floorBytes, baseline x
  pct / 100)` (weight).
- Deterministic counts with no margin fail on any increase.
- A timing metric enters `target` mode only with a recorded ten-of-ten stability trial.
- `ciMode`, when present, replaces `mode` in the CI guards (`DeterministicGuardTests` and `TimingGuardTests`, read
  against `baseline.ci.json`). `yarn perf --check` ignores it and enforces `mode` against `baseline.json`. Every
  timing budget carries `ciMode: report-only` except `hydrated_ms` and `server_response_ms`, the only timing metrics
  steady across CI runners (research R4).

## Baseline files

`specs/006-performance-sweep/baseline.json` and `tests/KCC.E2ETests/Features/Performance/baseline.ci.json`:

```json
{
  "captured": "2026-10-27T00:00:00Z",
  "commit": "<sha>",
  "environment": { "chromium": "145.0.7632.6", "webkit": "26.0", "gpu": "swiftshader" },
  "cases": {
    "search-3 · phone · chromium · light · bloomed": { "frame_p95_ms": 0, "layers": 0, "css_gz_bytes": 0 }
  }
}
```

## `results.json`

The baseline shape plus, per metric, `budget`, `baseline`, `delta` and `status` (`pass`, `fail`, `report-only`),
and `runs` for timing metrics.
