# Bug Fix: Dev-mode SSR inlines Vue's CommonJS build and every page falls back to client rendering

- **Slug**: dev-ssr-vue-cjs
- **Fixed**: 2026-11-18
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

`resolve.dedupe` now names `vue` alone. The four `@vue/*` names made Vite resolve Vue's own dependencies from
`src/KCC.Web`, where pnpm does not link them, so the dev SSR runner never externalized them and evaluated
`runtime-dom.cjs.js` as an ES module.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/KCC.Web/vite.config.ts` | modified | `dedupe: ['vue']`, with a comment on why the `@vue/*` names cannot be deduped |
| `tests/KCC.ViteTests/Features/Ssr/devServerRender.test.ts` | added tests | Boots Vite's dev server as `Server.js` does, renders the SSR entry, and checks the render uses one Vue runtime |

## Tests Added or Updated

- `tests/KCC.ViteTests/Features/Ssr/devServerRender.test.ts` › the dev SSR server › loads the SSR entry and renders
  the app as Server.js does — fails with `ReferenceError: exports is not defined` at `runtime-dom.cjs.js:11:23` on a
  clean pnpm install before the fix, and passes after.
- `tests/KCC.ViteTests/Features/Ssr/devServerRender.test.ts` › the dev SSR server › renders with the one Vue runtime
  vue/server-renderer loads — after a render, exactly one `runtime-core` CommonJS build is in the module cache. On a
  checkout with stale flat `@vue/*` folders the old dedupe loaded two copies and rendered without an error; this test
  fails there before the fix too, so the regression shows on both layouts.

## Local Verification

The worktree's root `node_modules` still holds flat `vue` and `@vue/*` folders from before the move to pnpm, which
hide the bug, so the red and green runs used a `git archive` export of `c1827268` installed with
`pnpm install --frozen-lockfile`, the layout CI installs.

- `pnpm exec vitest run Ssr/devServerRender` on the clean export → red before the fix (`exports is not defined`),
  green after.
- `SSR_PORT=3101 pnpm dev:all` on the clean export after the fix → `POST /render` answers 200, again after touching
  `Features/App.vue` (module reload), and the client dev server serves `Features/Main.ts`.
- The whole site in development on the clean export (`dotnet run`, `ASPNETCORE_VITE=true`, a `sqlite3 .backup` copy of
  the main checkout's database, ports 58771 and 3102) → `/` and `/recipes` answer 200 with server-rendered markup; the
  log shows `SSR render completed` three times and no `falling back to client-side rendering`.
- `vite build --mode ssr` and `vite build` on the clean export with the old and the new config → the SSR bundle is
  byte-identical (same SHA-1) and the client chunk names, which carry content hashes, are the same. Production output
  does not change.

### Definition of done

| Row | Result |
|---|---|
| D1 `dotnet build KitchenCommandCenter.slnx -c Release` | pass: 0 warnings, 0 errors |
| D2 `pnpm build:all` | pass: client and SSR bundles built |
| D3 `pnpm type-check`, `pnpm lint`, `pnpm format` in `src/KCC.Web` | pass: type-check clean, lint 0 errors (6 warnings in files this fix does not touch), format changed nothing |
| D4 `node tests/scripts/run.mjs` | pass: 4445 passed, 0 failed, 2 skipped across 6 suites |
| D5 Generated files | N/A: no generator source changed |
| D6 Pages in both ramps | N/A: no visual change |
| D7 Brand | N/A: no visual change |
| D8 Deploy lint | N/A: nothing under `deploy/` |
| D9 Images | N/A: no `Dockerfile`, `docker-bake.hcl` or `deploy/` change |
| D10 Docs | pass: no line in `README.md`, `CLAUDE.md` or `docs/` describes the dedupe list |

## Deviations from Assessment

- The assessment also proposed a test that every `resolve.dedupe` name resolves from `src/KCC.Web`. It was dropped:
  the `vitest` bin shim puts `node_modules/.pnpm/node_modules` on `NODE_PATH`, so Node's resolver inside Vitest finds
  `@vue/runtime-dom` where Vite's resolver does not, and the test passed on the broken config. The dev-server render
  test is the guard.

## Follow-ups

- Board pickup refused: no card carried `bug/dev-ssr-vue-cjs` before the branch was pushed.
