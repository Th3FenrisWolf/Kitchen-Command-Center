# Bug Verification: Dev-mode SSR inlines Vue's CommonJS build and every page falls back to client rendering

- **Slug**: dev-ssr-vue-cjs
- **Tested**: 2026-11-18
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

On a fresh `pnpm install --frozen-lockfile` of the fix branch, with no hoisted `vue` or `@vue/*` at the repository root,
the dev SSR sidecar loads `Server.Entry.ts` and renders. The same layout failed with `exports is not defined` before the
fix. No regression in any suite.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) | `git archive HEAD` into a new folder, `pnpm install --frozen-lockfile`, then `SSR_PORT=3103 node Features/Ssr/Server.js` and `POST /render` | pass | `SSR module loaded via Vite dev server`, render 200, no error |
| Reproduction, whole site | `dotnet run` in development on a clean export, as `fix.md` records | pass | `/` and `/recipes` render on the server, no `falling back to client-side rendering` |
| New test | `pnpm exec vitest run Ssr/` on the fresh export | pass | both `devServerRender.test.ts` tests pass; before the fix they fail with `exports is not defined` on a clean install and with two runtimes on a flat one |
| Regression suite | `node tests/scripts/run.mjs` | pass | 4445 passed, 0 failed, 2 skipped across 6 suites |
| Build | `dotnet build KitchenCommandCenter.slnx -c Release`, `pnpm build:all` | pass | 0 warnings; SSR bundle byte-identical to the old config's |
| Lint / type-check | `pnpm type-check`, `pnpm lint`, `pnpm format` | pass | 0 errors |

## Output Excerpts

```
SSR module loaded via Vite dev server
[189cac1b] SSR render completed in 11ms
Tests  2 passed (2)
4445 passed · 0 failed · 2 skipped across 6 suites
```

## Residual Risks

- A checkout whose root `node_modules` still holds flat `vue` or `@vue/*` folders from the npm era never showed the
  `exports is not defined` failure. There the old dedupe loaded a second `runtime-core` instead, and the single-runtime
  test fails on it, so the regression shows on both layouts.

## Recommendation

Close the bug: verified end to end on the layout that reproduced it.
