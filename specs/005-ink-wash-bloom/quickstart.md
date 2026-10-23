# Quickstart: validate the bloom and the grain rule

Run from the repository root unless a step says otherwise. Every yarn command needs `FONTAWESOME_NPM_AUTH_TOKEN` in
the environment.

## 1. Build and test

```sh
yarn build:all
dotnet build KitchenCommandCenter.slnx -c Release
(cd src/KCC.Web && yarn test washes KccWashes KccSheet contrast motion grain paperRule kitClasses retiredTokens)
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
node tests/scripts/run.mjs   # all six suites; put a no-op `open` first on PATH when unattended
```

Expected: every suite green. `washes.test.ts` covers SC-003, SC-004, SC-005, the counts, the schedule and the
two-deep cap; `contrast.test.ts` covers SC-006; `grain.test.ts` covers FR-024; the E2E `BloomTests`, `GrainTests` and
`WashOverflowTests` cover SC-001, SC-002, SC-007, SC-008 and the field's clip in a browser.

## 2. Run the site

Start KCC.Web as usual (https://localhost:58671) and open it in the Browser pane.

## 3. A fresh visit is bare (SC-001, US1)

1. Clear the site's cookies, open `/`.
2. In the console: `document.getAnimations().filter(a => a.effect.target.matches('.kcc-wash')).map(a => getComputedStyle(a.effect.target).opacity)`.

Expected: every value `0` (or within a hair of it), and `document.cookie` holds `kcc-visit=1.…`.

## 4. Place yourself mid-visit (US1 scenarios 3 and 4)

Write the cookie with a start two minutes back, then reload:

```js
const [v, seed] = document.cookie.match(/kcc-visit=([^;]+)/)[1].split('.')
document.cookie = `kcc-visit=${v}.${seed}.${Math.floor(Date.now() / 1000) - 120}; max-age=1800; path=/; secure; samesite=lax`
location.reload()
```

Expected: `<html>` carries `--wash-elapsed: 120.0s` (±1s); `<head>` holds `#kcc-bloom-schedule`; washes whose
`getComputedStyle(w).getPropertyValue('--wash-at')` is under about 30s are fully bloomed,
later ones partly or not at all; no wash changes when hydration finishes (record a performance trace from reload and
look for no style change on `.kcc-wash` after the app mounts). Repeat with `- 300`: every wash fully bloomed.
Navigate to another page: the same seed, and its earned washes on first paint. Reload the same page: the same
arrangement (US1 scenario 6).

## 5. A new visit differs (US1 scenario 5, SC-003)

Delete `kcc-visit`, reload, fast-forward as in step 4 to 300s. Expected: a different arrangement from step 4.

## 6. Arrangement across sheets (US2)

On the library page at 300s: no two sheets alike; cards carry one or two washes, wide sheets four; a recipe card's
first wash is its tile's colour; a Home card with a backoffice wash wears it first. In the console,
`[...document.querySelectorAll('.kcc-washes')].map(f => f.children.length)` never exceeds 4. On Home, a card whose
backoffice wash is unset has no `.kcc-washes` at all (FR-010).

Sign in mid-visit (at about 60s) and return to the same page: every sheet that was there keeps its arrangement, and
the pad's washes keep their progress (A12, A25).

## 6a. Washes never change the page's size (A23)

At 300s on the library, a recipe page and a phone pad card (375px wide), compare `document.documentElement.scrollHeight`,
`scrollWidth` and each `.kcc-sheet--scroll`'s `scrollHeight` before and after injecting
`<style>.kcc-washes{display:none}</style>`. Expected: equal. Then check at 120s, mid-bloom: equal, and the scrollbar
does not move while washes scale up.

## 7. Reduced motion (US4)

Emulate `prefers-reduced-motion: reduce` (DevTools Rendering panel), repeat step 4 at 60s. Expected: arriving washes
change opacity only; `getComputedStyle(wash).scale` stays `1`.

## 8. Both ramps and the grain (US3, SC-007, FR-018)

Switch the ramp mid-bloom: washes keep their arrangement and progress. In each ramp, zoom on paper, a field, a
button, a label, tape, a well and a blank: no grain. `GrainTests` does the same by sampling a 40×16px patch of bare
paper between two rules (luminance standard deviation under 0.5 levels) against the same patch on the desk (over it). The desk between sheets shows the grain and crayon tooth as on
`main`. Washes and tiles keep their grain.

## 9. Scroll performance (SC-009)

On the library page, DevTools Performance with mobile emulation and 4× CPU throttle, two runs, each scrolling the page
for five seconds:

1. At 150s of visit time (step 4 with `- 150`), when about 21 washes are mid-bloom at once: the busiest moment.
2. At 300s, fully bloomed.

Expected in both: no long frames during the scroll, and no frame over 50ms. Record the dropped-frame count of each run
in the pull request. If either stutters, apply the levers in [research.md](research.md) R11 in order.

## 10. SSR cost per visitor (plan, Constitution Check V)

Every visitor now has their own seed, so a visitor's first view of a page always misses the SSR cache. Measure the
cost on the running site, and on the Pi when a deploy is available. Use the home page, `/recipes` and one variant
page:

1. **Cold**: request each page 10 times, each with a fresh visit, so every request renders:
   `for i in $(seq 10); do curl -sk -o /dev/null -w '%{time_total}\n' https://localhost:58671/recipes; done`
   (no cookie jar, so every request starts a visit).
2. **Warm**: request each page 10 times with one cookie jar (`-c jar -b jar`), so requests 2 to 10 hit the cache.
3. Read the sidecar's `renderTime` for each render from the KCC.Web log at Debug level (`VueSsrService`).

Expected: the cold median minus the warm median is the per-visitor render cost. Record it in the pull request
beside the CPU model, along with each page's median `renderTime`.
