# Kitchen Command Center

Project-level instructions for Claude Code.

## Superpowers: plans & specs

Save Superpowers **plans** and **specs** under `.superpowers/`, not under `docs/superpowers/`. This keeps all superpowers files together in the same location.

- **Plans** → `.superpowers/plans/YYYY-MM-DD-<slug>.md`
- **Specs / design docs** → `.superpowers/specs/YYYY-MM-DD-<slug>.md`

A plan and the spec it implements **must share the identical `<slug>` only** — same slug, *potentially* different date. When writing a plan, derive its filename from its spec, updating the date to be the new current date; don't coin a new slug. Example: spec `2026-05-20-recipes.md` ↔ plan `2026-05-21-recipes.md`.

This overrides the default locations and filename placeholders baked into the `writing-plans` and `brainstorming` skills (`docs/superpowers/plans/` and `docs/superpowers/specs/`), both of which explicitly defer to user preferences for file location.

**`.superpowers/` is gitignored** (see `.gitignore`). The plan, spec, and brainstorm files are local scratch only — do **not** stage or commit them while working through a feature, and don't be surprised when they don't appear in `git status`. Leave them out of every commit.

## Vue SFC `<style>` blocks

Write component CSS in a `<style>` or `<style scoped>` block. A standalone CSS file is not required, and
adding one to work around missing styles is a symptom of a broken build, not a fix.

Style-block CSS never reaches the page as part of the SSR-rendered HTML, so each environment delivers it
separately:

- **Production** — the client build extracts it into chunk CSS assets (`GlobalComponents-*.css`). A
  `<link rel="stylesheet" vite-href="/Features/Main.ts">` in `Layout.cshtml` makes Vite.AspNetCore emit a
  `<link>` for every CSS file in the entry's import graph. Entries outside that graph need their own link —
  see `ResourceStringEditorTagHelper`.
- **Development** — no such asset exists, so the SSR sidecar walks the Vite SSR module graph after
  rendering, compiles each style module through the client pipeline, and returns the CSS alongside the
  HTML. `SsrHtmlContent` inlines it as `<style data-ssr-styles>`, and `Main.ts` removes that tag once
  hydration has run so HMR-updated head styles win.

**Always build both bundles together — `yarn build:all`, never one side alone.** Production scope IDs hash
each component's path *and* its content, so a client bundle built against different component source than
the SSR bundle emits `data-v-` attributes the CSS has no selectors for, and every scoped component
silently loses its styles.

## Softbound Sketch design language

The public site uses the Softbound Sketch language: a dark-first purple-slate **desk / paper / ink**
palette with 14 pale accent "washes", a `marker` green as the one strong fill, tightened radii, and **no
shadows** — elevation is a hand-drawn SVG outline plus a hatched shadow strip.

### Tokens

Role tokens live in `Styles/TailwindConfig.css` under **`@theme static`**. The `static` is load-bearing:
Tailwind 4 prunes theme variables no utility references, and the kit CSS and light ramp read `--color-*`
directly, so pruning would silently empty them.

- Grounds: `desk`, `desk-2`, `paper`, `paper-2`
- Inks: `ink`, `ink-soft`, `ink-line`, `ink-on-wash`
- Fills: `marker` / `marker-ink` — the *only* strong fill; use it for selected, checked and primary
- Status pigments: `danger-ink`, `success-ink`, `warning-ink`, `rating-ink`, `link` (text and glyphs);
  the washes themselves are for fills
- The 14 washes, `marker`, `marker-ink` and `ink-on-wash` are **identical in both ramps** — the brand constant

**Text on a wash is always `text-ink-on-wash`, never `text-ink`.** `ink` is near-white in the dark ramp, so
`text-ink` on a pale wash measures about 1.2:1. This bit three separate components during the restyle.

### Two ramps

Dark is the default. Light values live in `Styles/Sketch/Tokens.css` under `:root[data-theme='light']`.
`Layout.cshtml` runs a pre-paint inline script that sets `data-theme` from `localStorage['kcc-theme']`,
else `prefers-color-scheme`, else dark. **Check both ramps for any visual change** — the light ramp is the
binding contrast constraint. `tests/KCC.ViteTests/Features/Styles/contrast.test.ts` converts every token
pair to sRGB and asserts WCAG AA in both ramps; it is the living contrast table, not a static doc.

### The drawn kit, and where CSS lives

`sk-*` classes (`sk-sheet`, `sk-wash`, `sk-tab`, `sk-fold`, `sk-ruled`, `sk-hand`, `sk-btn`, …) live in
global `@layer components` CSS under `Styles/Sketch/`, **not** in component `<style>` blocks — Razor-rendered
widgets and Vue components share them, and Razor cannot reach a scoped block. That is the rule: anything
Razor also renders belongs in `@layer components`.

One exception worth knowing: a rule that must beat a Tailwind *utility* has to sit **outside** `@layer`
entirely, because the `utilities` layer comes after `components`. The ramp-swap rule at the end of
`Styles/Sketch/Kit.css` is deliberately unlayered for that reason.

### Ink API

Outlines are real SVG paths generated client-side. Vue uses the `v-ink` directive; Razor markup opts in
with `data-ink="sheet|card|tile|button"`.

Pick the kind by the **ground**, not the element: `card` (ink-line) on paper, `sheet` (ink-line + hatch) on
panels, `tile` (ink-on-wash) on washes. The wrong kind draws an *invisible* outline rather than a
wrong-coloured one.

Two invariants:

- **`Features/Ink/inkDom.ts` must never touch the DOM at module scope.** It is pulled into the SSR bundle
  via `vInk → GlobalComponents → Server.Entry`; a top-level `document`/`window` reference breaks SSR at
  import time, not call time.
- **Any vitest rendering a component that uses `v-ink` must go through
  `tests/KCC.ViteTests/support/renderSsr.ts`.** The directive is app-scoped, so a bare
  `renderToString(createSSRApp(C))` fails to resolve it.
