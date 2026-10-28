# Quickstart: Validating Performance Sweep, Phase 1

## Prerequisites

- `FONTAWESOME_NPM_AUTH_TOKEN`, `KCC_E2E_MEMBER_USERNAME` and `KCC_E2E_MEMBER_PASSWORD` set.
- Playwright browsers installed, Chromium and WebKit.
- For the live run: `KCC_PERF_LIVE_URL`, `KCC_PERF_MEMBER_USERNAME`, `KCC_PERF_MEMBER_PASSWORD` and
  `KCC_PERF_BYPASS_TOKEN`, with the Cloudflare skip rule applied (`docs/hosting/runbook.md`).

```bash
yarn build:all && dotnet build KitchenCommandCenter.slnx -c Release --disable-build-servers
```

## Scenarios

1. **Report (US4, SC-007).** `yarn perf` writes `tests/results/perf/report.html` listing every case in
   [data-model.md](data-model.md) with each metric beside its budget and the baseline.
2. **Frames and interaction (US1, SC-001 to SC-003).** `yarn perf --check --page search-3 --profile phone` exits 0
   in both engines and both ramps at a bloomed visit: 95% of frames within 16.7 ms, no frame over 50 ms, every
   interaction within 100 ms.
3. **No regression (SC-004).** In the report, no load milestone, weight or server response is worse than
   `baseline.json`.
4. **Look (US2, SC-005).** `yarn perf:capture after`, then `yarn perf:diff before after`; the owner approves every
   page type in the `/speckit-preview-run` round. Every scripts-off capture matches its scripts-on capture (light
   when signed out).
5. **Guards (US3, SC-006).** On a throwaway branch, add `filter: blur(1px)` to `.kcc-sheet` and push: the E2E suite
   fails with page, profile, engine, ramp, budget and number. Revert: CI passes. On unchanged code the `perf` job
   passes ten runs out of ten.
6. **Live (FR-004).** `yarn perf:live` writes `tests/results/perf-live/report.html` signed out and as the measurement
   member, with server response as min, median and max.
7. **iPhone (FR-021).** In Safari on an iPhone, open the site fresh, scroll recipe search to the bottom and back,
   open a recipe; repeat two minutes into the visit. Record whether it keeps up.

## Definition of done

Rows D1 to D7 and D10 of the constitution apply; D8 and D9 do not, unless a task touches `deploy/` or the images.
