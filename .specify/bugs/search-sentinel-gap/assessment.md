# Bug Assessment: Recipe search infinite scroll stalls while its sentinel stays in view

- **Slug**: search-sentinel-gap
- **Created**: 2026-11-03
- **Source**: pasted text (P-28, from the performance sweep's review)
- **Verdict**: valid
- **Severity**: medium

## Report (verbatim or summarized)

> P-28: on recipe search's infinite scroll, when the load-more sentinel is still in view after a page of results
> loads, no further page loads. A tall viewport, or a short page of results, leaves the list stuck.

## Symptom

On `/recipes`, once a page of results finishes loading with the load-more sentinel still inside the observer's
margin, no further page loads until the reader scrolls the sentinel out of range and back. Expected: pages keep
loading while the sentinel stays in view and more results remain.

## Reproduction

1. Seed more than two pages of recipes (more than 24; the page size is 12).
2. Open `/recipes` in a viewport tall enough that the sentinel, with its 400px root margin, is in view after the
   first two pages render (a tall desktop window, or a zoomed-out one).
3. The first notification loads page 1. After it renders, the sentinel is still in view, and nothing else loads:
   the list stops at 24 cards with the sentinel showing and `hasMore()` still true.

The performance harness already works around this: `LoadThreePagesAsync` in `tests/KCC.E2ETests/Config/Perf/Cases.cs`
scrolls away from the bottom and back on every attempt, and its comment names the cause.

## Suspected Code Paths

- `src/KCC.Web/Features/Components/RecipeSearch/useInfiniteScroll.ts` — the `IntersectionObserver` calls `onHit`
  only from its callback, which fires on observe and then only when the sentinel's intersection state changes.
  The sentinel element is observed once and kept across loads.
- `src/KCC.Web/Features/Pages/RecipeSearch/RecipeSearchView.Component.vue:103,186` — calls
  `useInfiniteScroll(loadMore)` and renders the sentinel behind `v-if="hasMore()"`. While more results remain the
  same element stays mounted, so the watch in the composable never re-observes it.
- `src/KCC.Web/Features/Pages/RecipeSearch/useRecipeSearch.ts:54` — `loadMore` ignores a hit while a page is in
  flight, so a notification that lands during a load is dropped too.

## Root Cause Hypothesis

An `IntersectionObserver` reports transitions, not state. The sentinel intersects when page N is requested and still
intersects after page N renders, so there is no transition and no callback; `loadMore` is never called again. High
confidence: the code and the harness's own workaround agree.

## Proposed Remediation

**Preferred**: re-arm the observer after each page lands. `useInfiniteScroll` takes a second argument, a watch source
that changes when results are replaced or appended (the view passes `results`; `useRecipeSearch` assigns a new array
on every successful fetch). On a change, after the DOM has updated (`flush: 'post'`), it unobserves and re-observes
the current sentinel. `observe()` always queues an initial notification computed at the next rendering update, so
the browser reports the sentinel's real position after the new cards lay out: still in view loads the next page,
pushed out loads nothing.

**Alternatives**:
- Re-arm when `loading` turns false. Also catches a failed fetch, so a persistent server error would retry in a
  tight loop at fetch speed. Re-arming on new results leaves a failure where it is today (a scroll retries).
- Track the last `isIntersecting` and call `onHit` straight after a load. That state is stale until the browser lays
  out the new cards, so it fetches one page too many whenever the new cards push the sentinel out.

**Files likely to change**:
- `src/KCC.Web/Features/Components/RecipeSearch/useInfiniteScroll.ts`
- `src/KCC.Web/Features/Pages/RecipeSearch/RecipeSearchView.Component.vue`
- `tests/KCC.ViteTests/Features/Pages/RecipeSearch/` (new client-mount test)
- `tests/KCC.E2ETests/Config/Perf/Cases.cs` — its comment states that a sentinel still in view never loads again,
  which the fix makes untrue; correct the comment, keep the harness's scroll pattern so measurements stay comparable.

**Tests to add or update**:
- A Vitest test (happy-dom) that mounts `RecipeSearchView` client-side with a fake `IntersectionObserver` whose
  sentinel is always in view (it notifies only on `observe()`, as a real observer does when nothing moves), a stubbed
  `fetch`, and more than two pages of results. It expects the page to keep fetching until `hasMore()` is false. It
  fails before the fix: one fetch, then nothing.
- A test that a failed page fetch does not re-arm the observer (no retry loop).

## Risks & Considerations

- Re-observing on a filter change: the results array is replaced, so the sentinel is re-checked then too. That is
  wanted: a filter change that leaves the sentinel in view also loaded nothing more before.
- A page that loads only a handful of cards fetches pages back to back until the sentinel leaves view or results run
  out. Each fetch waits on the previous one (`loading` guard), and each re-arm waits on a render, so this is bounded.
- No server, schema or SSR change; the observer exists only on the client.

## Open Questions

None.
