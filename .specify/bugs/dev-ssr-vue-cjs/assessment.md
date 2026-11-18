# Bug Assessment: Dev-mode SSR inlines Vue's CommonJS build and every page falls back to client rendering

- **Slug**: dev-ssr-vue-cjs
- **Created**: 2026-11-18
- **Source**: pasted text (from the orchestrating session, seen while capturing screenshots for spec 011)
- **Verdict**: valid
- **Severity**: medium

## Report (verbatim or summarized)

> On `main`, running the site in development (`dotnet watch --non-interactive` from `src/KCC.Web`, which starts
> `pnpm dev:all`) makes every page's server render fail. The SSR sidecar logs:
>
> `[vite] (ssr) Error when evaluating SSR module .../src/KCC.Web/Features/Ssr/Server.Entry.ts: exports is not defined`
> `at eval (.../node_modules/.pnpm/@vue+runtime-dom@3.5.44/node_modules/@vue/runtime-dom/dist/runtime-dom.cjs.js:11:23)`
> `at ESModulesEvaluator.runInlinedModule (.../vite@8.3.7.../module-runner.js:807:163)`
>
> and every request falls back to client-side rendering (`SSR service returned InternalServerError ... falling back
> to client-side rendering`). A production build renders fine.

## Symptom

In development the SSR sidecar cannot evaluate `Server.Entry.ts`, so every page is served without server-rendered
HTML and paints only after the client bundle mounts. Expected: development renders on the server, as production does.

## Reproduction

1. Install the workspace with `pnpm install --frozen-lockfile` into a clean checkout, so `node_modules` has pnpm's
   strict layout and no hoisted `vue` or `@vue/*` at the repository root.
2. From `src/KCC.Web`, run `pnpm dev:ssr` (or the whole site with `dotnet watch`).
3. The sidecar logs `exports is not defined` from `runtime-dom.cjs.js`, and `POST /render` answers 500.

Reproduced on 2026-11-18 with a `git archive` export of `c1827268` installed fresh: a script calling
`vite.ssrLoadModule('Features/Ssr/Server.Entry.ts')` throws `ReferenceError: exports is not defined`.

A checkout whose root `node_modules` still carries flat `vue` and `@vue/*` folders from the npm era does not
reproduce: there `@vue/runtime-dom` resolves from the project root and is externalized. That is why the failure
showed only after the move to pnpm and only on some machines.

## Suspected Code Paths

- `src/KCC.Web/vite.config.ts` `resolve.dedupe` — lists `@vue/runtime-dom`, `@vue/runtime-core`,
  `@vue/compiler-dom` and `@vue/shared`, none of which `KCC.Web` depends on directly.
- `src/KCC.Web/vite.config.ts` `ssr.noExternal: ['vue']` with the `vue` alias to `vue/dist/vue.esm-bundler.js` —
  inlines Vue's bundler build into the SSR module runner, so its bare imports of `@vue/runtime-dom` and
  `@vue/compiler-dom` go through Vite's externalization check.
- Vite 8.3.7 `createIsConfiguredAsExternal` / `tryNodeResolve` (`node_modules/vite/dist/node/chunks/node.js`) — a
  deduped package is resolved from the project root only. When that fails, the import is not externalized.

## Root Cause Hypothesis

`resolve.dedupe` makes Vite resolve `@vue/runtime-dom` (and the other listed `@vue/*` packages) from
`src/KCC.Web` instead of from the importing `vue` package. Under pnpm's strict layout those transitive packages are not
reachable from `src/KCC.Web`, so the externalization check finds nothing and leaves the import inlined. The module
runner then resolves the package with the SSR conditions, where `@vue/runtime-dom`'s `exports` map picks
`node`/`development`, the CommonJS `runtime-dom.cjs.js`, and evaluates it as an ES module, where `exports` does not
exist. The production build bundles with Rolldown, which converts CommonJS, so it is unaffected. Confidence: high —
removing the four `@vue/*` names from `dedupe` makes the same clean export load and render.

## Proposed Remediation

**Preferred**: Reduce `resolve.dedupe` to `['vue']`. The `@vue/*` entries date from the npm layout, where every
package was hoisted to the root. Under pnpm they cannot resolve from the root at all, so they deduplicate nothing, and
the lockfile already holds one version of each. With them gone, `@vue/runtime-dom` and `@vue/compiler-dom` resolve
from `vue`'s own folder, are externalized, and Node loads their CommonJS builds natively, the same instance
`vue/server-renderer` loads in `Server.js`.

**Alternatives**:
- Add `@vue/runtime-dom`, `@vue/runtime-core`, `@vue/compiler-dom` and `@vue/shared` to `KCC.Web`'s dependencies so
  they resolve from the root. Keeps the dedupe list, but adds four direct dependencies to keep in step with `vue` and
  touches `package.json` and `pnpm-lock.yaml`, and the SSR image would install them.
- Drop `node` from the SSR resolve conditions so the inlined packages pick their `module` builds. Changes how every
  inlined dependency resolves, in the production SSR bundle too.

**Files likely to change**:
- `src/KCC.Web/vite.config.ts`
- `tests/KCC.ViteTests/Features/Ssr/` (new test)

**Tests to add or update**:
- A Vitest test that starts Vite's dev server the way `Server.js` does, loads `Features/Ssr/Server.Entry.ts` through
  `ssrLoadModule` and renders the app to a string. It fails with `exports is not defined` on a clean pnpm install
  before the fix, and passes after.
- A test that every name in `resolve.dedupe` resolves from `src/KCC.Web`, the condition dedupe needs to work at all.

## Risks & Considerations

- The client dev server and the client build also read `resolve.dedupe`. Under pnpm the dropped entries never
  resolved from the root there either, so their removal changes nothing a single installed Vue version relies on.
- The regression test only fails on a pnpm-strict `node_modules`. CI installs clean, so it guards there; a local
  checkout with stale hoisted `@vue/*` folders passes before and after.
- Booting Vite inside Vitest adds a few seconds to the suite.

## Open Questions

None.
