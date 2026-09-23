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

## Torn & Waxed design language

The public site uses the Torn & Waxed identity: torn-paper sheets on a lilac-grey desk, a 24px rule, eight
wax washes, marker green as the one strong fill, Sono for every number and label. **The brand lives in
`docs/brand/torn-and-waxed.md`; the engineering contract in `docs/brand/kit.md`.** Read the contract before
styling anything; use the `torn-and-waxed` skill for the quick reference, the `kit-builder` agent to convert
a surface and the `brand-steward` agent to review one. The invariants below are the ones that break the
build or the brand silently, so they stay here as well.

### Tokens

Role tokens live in `Features/Styles/TailwindConfig.css` under **`@theme static`** with the **light** values;
the dark ramp overrides them in `Features/Styles/Torn/Tokens.css` under `:root[data-theme='dark']`. The
`static` is load-bearing: Tailwind 4 prunes theme variables no utility references, and the kit CSS and the
dark ramp read `--color-*` directly. `Layout.cshtml` runs a pre-paint inline script that sets `data-theme`
from `localStorage['kcc-theme']`, else `prefers-color-scheme`, else **light**. **Check both ramps for any
visual change**; `tests/KCC.ViteTests/Features/Styles/contrast.test.ts` is the living contrast table and also
composites ink over paper plus each wash.

### The kit, and where CSS lives

`kcc-*` classes live in global `@layer components` CSS under `Features/Styles/Torn/`, **not** in component
`<style>` blocks: Razor-rendered widgets and Vue components share them, and Razor cannot reach a scoped
block. Anything Razor also renders belongs in `@layer components`. A rule that must beat a Tailwind *utility*
sits **outside** `@layer` entirely (the ramp-swap rule at the end of `Features/Styles/Torn/Kit.css`), because
the `utilities` layer comes after `components`.

### The tear

Sheets are clipped with `clip-path: var(--tear)`; the presets in `Features/Styles/Torn/Tears.css` are
**generated** by `yarn tears` from `Features/Torn/tears.ts` and diffed by `tears.test.ts`. Never hand-edit
the CSS. No runtime JS draws anything; the SSR output is final. Two structural invariants:

- The `filter` (fibre + fall) sits on `.kcc-torn`, **outside** the clipped `.kcc-sheet`, or the shadow is
  clipped away with the paper.
- `.kcc-label`, `.kcc-tape` and `.kcc-tilewrap` are siblings of `.kcc-torn` inside `.kcc-slip`: outside the
  clip and outside the filter, so they are neither torn nor shadowed.
