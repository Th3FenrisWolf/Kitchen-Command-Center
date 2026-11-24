# Contract: Islands, Later Parts and the Page Payload

How a server-rendered page becomes interactive after Phase 2. Every rule here holds in production; development
differs only where noted.

## Global components

- A file named `*.Component.vue` anywhere under `src/KCC.Web/Features/` is a global component, as today. Razor
  markup names it by its file name (`<RecipeSearchView ...>`), and the SSR sidecar resolves it from the registry in
  `Features/GlobalComponents.ts`.
- A global component is an **island** unless it declares `defineOptions({ serverOnly: true })`. An island's script is
  its own chunk, fetched only on a page that renders it. The one exception is what `AppHeader` imports statically
  (`KccWashes`, `SignOutForm`, `KccSheet`, `useRecentlyViewed` and `signOut`): the build writes them as one shared chunk,
  `HeaderImports`, instead of one chunk each, because over HTTP/1.1 every extra chunk request on the way to hydration
  adds a round trip when the browser cache is cold. This is the orchestrator's decision under the owner's no-behaviour-change
  clarify answer. `AppHeader` stays its own chunk. A server-only component is rendered by the sidecar and never
  hydrated, and its script is never fetched. `serverOnly` governs only where Razor places the component: imported
  into an island's own template (`KccWashes` in `AppHeader`), it hydrates with that island.
- Server-only today: `AppFooter`, `Card`, `UnderlineLink`, `KccWashes`, `Breadcrumbs`, `ResourceString`, `SmallHero`.
  Islands: `AppHeader`, `Stacker` and the eight page views.
- An island placed in Razor markup takes **props only**: attributes and `:prop="@Vue.Prop(...)"` bindings. The
  sidecar fails the render, naming the component, when an island receives slot content or a function. A server-only
  component may take slot content, and an island inside that content is hydrated on its own.
- Inside a `.vue` file, every component is imported. The client has no global registry, so a tag resolved at runtime
  renders nothing. `tests/KCC.ViteTests/Features/Islands/explicitImports.test.ts` fails on a component tag in a
  template that the file does not import.

## Server output

The sidecar's `/render` answers, beside `html` and `schedule`:

- `islands`: one entry per island in document order, `{ name, props, keys }`. `props` are the evaluated props as
  Razor passed them; `keys` are the wash keys the island claimed during the render, in order.
- `components`: the registry path (`Features/.../X.Component.vue`) of every global component the page rendered,
  islands and server-only alike.
- `pageWashes`: the number of page-scoped washes the render collected.

In the HTML each island's output sits in `<kcc-island data-island="N">`, `N` its index in `islands`. `kcc-island` is
`display: contents` in the kit's base layer.

`SsrHtmlContent` writes `#app` with the rendered HTML, then `<script id="server-content" type="application/json">`
holding `{ isPreview, bloom: { seed, page, pageWashes }, islands }`. With rendered HTML the payload carries no
`headerContent`, `bodyContent` or `footerContent`.

`Layout.cshtml`, from the build manifest, writes in `<head>`:

- a stylesheet link for the CSS of every component in `components` and its static imports, deduplicated, after
  `Main.css`;
- a `modulepreload` for the island entry first, then one for the chunk of every island in `islands` and its static imports;
- the island entry, `Features/Main.ts`.

## Fallback

When the sidecar returns no HTML (SSR off, a failure, a timeout or an open circuit), the page is today's: `#app`
empty, `#server-content` with the three region templates, and `Layout.cshtml` links the fallback build
(`wwwroot/fallback/`), which renders the whole page in the browser with the template compiler. The island entry is not
linked on that page.

The fallback build has its own manifest, `wwwroot/fallback/.vite/manifest.json`, which Vite.AspNetCore does not read.
In production a reader in `Features/Islands/` writes the fallback entry's script and a stylesheet link for every CSS
file of its closure from that manifest, so the page keeps its component styles. In development the page links
`Features/Fallback.ts` from the dev server, which builds nothing into `wwwroot/fallback/`.

The fallback honours `beforeHydrate` too (see Hydration). It registers each global that declares one in a wrapper that
renders the component only once its `beforeHydrate` has settled, with the props the region template gives it, and it
sets `kcc:hydrated` only after every wrapper has settled and rendered. A `beforeHydrate` that rejects is handled as in
`hydrate.ts`: a console warning, and the component mounts anyway.

## Hydration

`Main.ts` configures the API from `#api-config`, creates one bloom from `bloom` and provides it, with `isPreview`, to
every island. For each `<kcc-island>` it imports the island's chunk through the registry and hydrates it with
`createSSRApp(component, props)`. The bloom replays each island's `keys` so the client claims the qualified keys the
server did. All islands present on load hydrate in parallel; after the last one mounts, `Main.ts` sets
`performance.mark('kcc:hydrated')`, calls `bloom.markHydrated(pageWashes)`, starts the visit heartbeat and schedules
later parts. `kcc:hydrated` is the moment the page is usable.

An island component may declare `beforeHydrate(props)` in `defineOptions`. `hydrate.ts` awaits the promise it
returns, after the island's chunk and before `createSSRApp`, so a later part the server rendered first arrives before
Vue hydrates it, and is never hydrated as its pending sheet. The hook receives the island's props under their
declared names, whatever case Razor wrote them in. A rejected `beforeHydrate` logs a console warning and the island
is rendered afresh with `createApp` in place of its server HTML; the part then shows its error state (see Later
parts). Hydrating it would leave that part's server HTML inert, since Vue hydrates an async part only once its load
succeeds. Only the island's own component is asked for the hook, so the wizard's views declare it:
`CreateRecipeView.Component.vue` and `AddVariantView.Component.vue` use `beforeWizardHydrates` with photo import
off, when the server opens on Basics or Variant, and `prefetchOpeningStep` loads that step from the flow's own
opening step (D35).
`openingStep.test.ts` checks both views hydrate their opening step with no mismatch.

A chunk that fails to load leaves its island's server HTML in place: links and content work as they do with scripts
off.

## Later parts

A part a page does not show on load may load after `kcc:hydrated`. It is prefetched in idle time after the mark, so it
is in memory before a reader reaches it. If a reader gets there first, the control still answers within the budget:
the state change (a step, a dialog opening) happens at once, the part's sheet shows a pending state, and the part
fills in when it arrives. Nothing the reader did is lost. The pending state is the part's own `KccSheet` with its
tear, empty, with `aria-busy="true"`: no text and no dictionary key.

`laterPart(loader, pending)` in `Islands/laterParts.ts` makes a later part. A load that rejects, or that has not
settled after 15 seconds, is tried again after one second and then after two; after the third attempt the part shows
its error state in the same place as its pending state, so a reader who waits learns that it failed. The error state
drops `aria-busy` and `data-later-pending` and shows, inside the part's own sheet and tear:

- a danger well (`kcc-well kcc-well--danger kcc-kick`, `role="alert"`) with the island's `UnexpectedError` string,
  and
- a kit `Button` (`data-test="later-part-retry"`) with the island's `TryAgain` string and the rotate-right icon.

The wizard's steps, reading state, submitted sheet and photo strip use `RecipeWizard/PartFailed.vue` with
`RecipeWizard.UnexpectedError` and `RecipeWizard.TryAgain`. Cook mode's pending dialog shows the same pair inline with
`VariantDetail.UnexpectedError` and `VariantDetail.TryAgain`, and keeps its close button (`VariantDetail.Close`),
Escape and its contained focus while pending or failed. Retry (`useLaterPartRetry()`) starts a fresh load of the part,
since Vue keeps a failed load's rejection for good; the part fills in if that load arrives. `whenArrived` resolves
once the pending root leaves the holder, by arrival or failure, so the wizard's step focus follows either.

A guarded link pressed in the wizard before `LeaveConfirm` has arrived waits for it and then asks. If it fails, the
wizard navigates anyway, and the browser's own `beforeunload` prompt asks before the work is lost.

Later parts today:

- **Recipe wizard steps.** Every `Components/RecipeWizard/*Step.vue` except `StartStep`, the one step that stays
  static (D34). `BasicsStep` and `VariantStep` are later parts too, prefetched at idle ahead of the other steps, and
  the step a wizard opens on is resolved before the island hydrates (D35, `beforeHydrate` above).
  `wizardSteps.ts` finds them with one `import.meta.glob`, so a new `*Step.vue` file is a later part with no other
  change. `RecipeWizard.vue` takes each with `const IngredientsStep =
laterStep('IngredientsStep')`, never with a static import; `laterStep` throws on an unknown name, and
  `wizardSteps.test.ts` fails when any file imports a later step statically. A component a later step imports
  (`DetailsSummary.vue` in `ReviewStep.vue`) rides in that step's chunk.
- **The wizard's other parts.** `ReadingState`, `SubmittedSheet`, `PhotoStrip`, `LeaveConfirm` and `photoEncoding`.
- **Cook mode.** `Pages/VariantDetail/CookMode.vue` and what only it imports.

## Development

The dev server keeps the full Vue build, so the fallback works there too. The sidecar still inlines style-block CSS
as `<style data-ssr-styles>`, and `Main.ts` removes it after the last island hydrates. An island's own import then
brings its style block back; a server-only component is never imported on the client, so it takes no `<style>` block,
and `registry.test.ts` fails on one.
