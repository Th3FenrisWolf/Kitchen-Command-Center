# Research: Ink Wash Bloom and the Grain Rule

Phase 0 of [plan.md](plan.md). Each entry: decision, rationale, alternatives. Every decision here that the spec left
open is recorded in the spec as an assumption (A9 onward). Revised 2026-10-21 after the red team: R5, R6, R8, R10,
R11, R12 and R15 changed or are new.

## R1. Where a wash's numbers come from

**Decision**: pure TypeScript in `Features/Bloom/washes.ts`. `planSheet` derives a sheet's washes (count, size,
aspect, rotation, radii, position, colour pair, strength) from the visit seed and the sheet's key. It runs in the
Vue render, on the server for first paint and on the client for hydration, and `KccWashes` writes the result as
inline custom properties. `scheduleWashes` (R6) sets the arrival times once per page after the render. CSS only draws
and animates what it is given. (A9)

**Rationale**:

- Cross-browser. The prototype hashes `sibling-index()` in CSS, which ships in Chromium only; Firefox has none and
  Safari support was not verified. CSS `random()` is unsupported.
- Colour. Picking one of eight tokens by a computed number has no clean CSS form. In TypeScript the pick is an index
  into `WASHES`, written as `var(--color-<wash>)` (SC-005).
- Testable. SC-003 to SC-006, FR-011, FR-014 to FR-017 become Vitest assertions on pure functions.
- Deterministic across engines. The hash is integer-only (a 32-bit mixer on `Math.imul`), and every number is written
  with a fixed number of decimals, so Node and every browser write byte-identical `style` strings.

**Alternatives considered**: the prototype's CSS hash (rejected above); a CSS hash fed by a server-emitted index
(geometry untestable outside a browser; the colour still needs the seed).

## R2. Carrying the visit to the page

**Decision**: a `kcc-visit` cookie holds the seed and start time, with a sliding `Max-Age` of 30 minutes.
`Layout.cshtml` resolves the visit through `VisitClock` on every page it renders. The page carries it three ways:

- `--wash-elapsed` on `<html style>`, written by Razor outside the Vue render and the SSR cache, clamped to
  `[0, 300]` seconds.
- `bloom: { seed, page }` in the SSR payload and the `server-content` JSON, so `planSheet` runs with the same inputs
  on the server and on the client.
- The page's arrival schedule, a `<style id="kcc-bloom-schedule">` in `<head>` (R6). `Layout.cshtml` renders the SSR
  in its opening code block, before `<head>` is written, so the schedule can go there.

(A10, A11)

**Rationale**: A3 settles that the server carries the visit so first paint needs no script. The elapsed time changes
every request, so it stays out of anything Vue renders and out of the SSR cache key.

**Alternatives considered**: the seed in the Razor markup (each visitor adds a template string to the sidecar's
template compile cache, which has no bound); elapsed time in the render (a mismatch on every hydration).

## R3. "No page of the site open" for 30 minutes

**Decision**: the cookie is readable by script. `Main.ts` starts a heartbeat that rewrites it with the same value and
a fresh 30-minute `Max-Age` once a minute while a page is open, and on `pagehide`. The heartbeat never changes the
value. The start time in it is cosmetic: a reader can rewrite it from script, and the worst they get is their own
page at a different stage of bloom. The server clamps the elapsed time to `[0, 300]` seconds and treats a malformed
or out-of-range start as a new visit. Without script, each page request renews it. (A10)

**Rationale**: the server sees requests, not open pages; a 40-minute read would otherwise end the visit. Browsers
throttle background timers to about once a minute, inside the window.

**Alternatives considered**: an `HttpOnly` cookie with a last-request window (fails the long read); a keep-alive
endpoint (a request a minute per open tab on a Raspberry Pi).

**Known limits**: a sleeping device runs no timers. Two tabs opened in the same instant with no cookie can each start
a visit; the later `Set-Cookie` wins.

## R4. SSR cache

**Decision**: `VueSsrService` adds the seed and the page key to its cache key and render request, and caches the
schedule with the markup. (A11)

**Rationale**: one visitor's cached render must never serve another's arrangement. No output cache, response cache
or CDN HTML cache exists today (searched `src/KCC.Web` for `OutputCache`, `ResponseCache` and `Cache-Control`: only
Umbraco's schema file matches).

**Alternatives considered**: quantising seeds to keep hit rates (with 64 seeds, 20 visits collide about 95% of the
time, failing SC-003).

## R5. Sheet identity

**Decision**: every bloom-carrying sheet has an explicit, stable key. `washKey` is a required prop of `KccSheet` and
`KccWashes`: a fixed name per call site (`variant-instructions`), and for a list item the list's name plus the item's
own id (`recipe-card:<recipe key>`), never its index. A key that starts with `@` is global and keeps its arrangement
on every page (the pad: `@pad`, and each pad card `@` plus its `id`, such as `@pad-card-recipes`); any other key is qualified by the page key. If two sheets on one
page share a key, the second and later take `~2`, `~3` in render order and the development build warns. A Vitest scan
fails when a `KccSheet` or `KccWashes` in `Features/**` has no `wash-key`. (A12, A27)

**Rationale**: the red team showed render position is not stable. `useId()` draws from one counter shared by the
whole component tree, and conditional sheets come and go with state: the member-only kitchen card
(`AppHeader.Component.vue:310-311`, `v-if="nav.member"`) sits before the body, and `VariantReviews.vue` renders its
summary sheet only when `count > 0` and its form only when `isAuthenticated`. Signing in mid-visit would shift every
later key. An explicit key changes only when the sheet itself is a different sheet.

**Alternatives considered**: `useId()` plus the page path (rejected above); a key built from the component name and
props (not unique in lists; silent when two collide).

## R6. Arrival times

**Decision**: arrivals are set once per page, after the render, by `scheduleWashes(seed, washes)`, which depends only
on the set of washes on the page, never on render order. Page washes, ranked by a hash priority, take golden-ratio
slots, so the spread is even on every page (owner ruling, session "implement"). (A13, A25)

1. **The visit lattice.** Sixteen beats per visit, `g_j = (j + 0.5 + 0.3 (u_j - 0.5)) × 210s / 16`, `u_j` from the
   seed, in whole milliseconds. A global sheet (the pad and its cards) names its beats in markup (`:wash-slots`),
   fixed in `AppHeader.Component.vue` and `PadCard.vue`, so its washes arrive at the same visit times on every page,
   whether or not the reader is signed in.
2. **The arrival sequence.** Candidate `j` is `frac(c + j / φ) × 210s`, with `c = hash(seed, page)`. It is computed
   in 32-bit fixed point, `(c + j × 0x9E3779B9) mod 2^32`, and floored to the millisecond, so the server and the
   client agree to the digit and every start lies in `[0, 209.999]`. The page is the prefix of the page washes'
   qualified keys, which is the bloom context's `page`. **Skips**: with `n` page washes, the block radius is
   `r = 0.75 × 210s / B`, `B` the smallest power of two at least `2n + 32`, within [64, 512]. A candidate within `r` of any of the sixteen visit beats, present or not, is
   skipped. The blocked arcs cover at most 3/8 of the window and the odd step reaches every 32-bit position, so a
   start always follows; a run of more than six skips did not occur over 5,000 seeds and 2,000 candidates.
3. **Ranking.** Page washes, sorted by priority `h = hash(seed, key, k)` (ties by key, then index), take the
   unskipped candidates in order: rank 0 the first, rank 1 the second. Sorting makes the result a function of the
   set alone. A change to the page's set of sheets re-ranks it, so its washes re-time on the next load.
4. **The chosen colour first.** Within each sheet, the times it took are reassigned so wash 0, which wears the
   chosen colour, takes the earliest (FR-015). The set of times on the page does not change.
5. **Output.** `.kcc-wash[data-wash="<id>"] { --wash-at: <t>s }` per wash, in `<head>`. The id is
   `hash(key, k)` in hex, without the seed.
6. **No schedule.** A wash set up after hydration has no rule, and neither does any wash on a page whose SSR fell
   back to client rendering. Each wash also carries `--wash-fallback-at` inline, and the kit reads
   `var(--wash-at, var(--wash-fallback-at))`.
   - **Washes mounted after hydration** continue the page's sequence (owner rulings, sessions "analyze" and
     "implement"). Hydration collects the same washes as the server render, so the bloom context knows the page's
     `n` washes, advances the sequence past their `n` starts with the same skips, and offers the next unskipped
     candidates, one per late wash, in mount order. A late sheet's starts are assigned so that wash 0 takes the
     earliest (FR-015). A list that loads late (the review sheets that `VariantReviews.vue` fetches in `onMounted`,
     and its reload after a post) draws from the same sequence, so page and late washes together keep the bounds
     below with `N = n + late`.
   - **A server-rendered sheet with no rule** (an SSR fallback page) keeps one start per stratum: wash `k` of `n` at
     `(k + p) / n × 210s`, plus a jitter of at most `0.3 / n` of the window, with `p = frac(h_sheet / 2^32)`, clamped to
     `[0, 210)`. A clamp, not a wrap: a wrap would put a later wash before wash 0, which would break FR-015.
   (A34)

**Bounds** (three-distance theorem): the first `M` points `frac(c + j / φ)` cut the circle into gaps of at most three
lengths, the longest under `φ³ / √5 ≈ 1.894` of `1 / M` and the shortest over `1 / (√5 M)`. The window cuts the
circle's wrap gap in two, which only shortens it. A skipped candidate sits within `r` of a visit beat, so a quiet
stretch holds at most the two gaps around one blocked interval:

- **Longest quiet stretch** of `N` starts: at most `2 × 1.894 × 210 / N + 2r`. Measured worst over 1,000 seeds with
  the four-wash pad: 47.6s for 12 washes (bound 71.2s), 11.7s for 50 (17.1s), 2.8s for 200 (4.6s); about 2.6 to 2.8
  times the mean gap `210 / N`, against up to 13 times with the earlier hash lattice.
- **Closest pair**: at least `min(r, 210 / (2√5 N))`: a page or late start keeps `r` from every visit beat, and the
  `N` starts draw on fewer than `2N` candidates. On a 50-wash page that is 0.615s.
- **Same instant** (FR-016): starts are distinct at the millisecond while `210 / (2√5 N) > 0.001`, that is up to
  about 47,000 washes on a page; a test checks 2,000. No cap is set.

**Guarantees** (each a test in `washes.test.ts`, except the late-wash ones, which are in `bloomContext.test.ts`):
every start in `[0, 209.999]`; the page takes exactly the first `n` unskipped candidates; no page or late start within
`r` of a visit beat; the quiet-stretch and closest-pair bounds over 1,000 seeds for 2, 6, 12, 50 and 200 washes with
the pad; shuffled input gives identical CSS; global washes' times are identical across pages; wash 0 of every sheet
first; late washes continue the sequence and keep both bounds.

**Known limit**: a change to a page's set of sheets re-ranks its washes, so they take new times on the next load
(accepted by the owner, A13). Geometry and colour never move, and after 300 seconds every schedule shows the same
full page.

**Rationale**: FR-003 and FR-016 need coordination across the page: independent per-sheet times put pairs of
arrivals within a second of each other (about 35 pairs within 3s among 50 uniform starts). A render-order counter
coordinates but moves with every conditional sheet before it (R5). Ranking by a hash priority coordinates without
depending on order, and the golden-ratio slots spread the ranks evenly.

**Alternatives considered**: a page-wide golden-ratio counter in render order (the original design; unstable); a
hash lattice with probing (stable when a sheet changes, but quiet stretches up to 13 times the mean gap, about 42s on
a 50-wash page; replaced by the owner's ruling in session "implement"); one fixed lattice of 512 beats (stable, but a
small page's few washes land unevenly).

## R7. How many washes a sheet takes

**Decision**: from the props a sheet already carries. `tear="hero"` or `wide` is a page sheet: 4. `margin` is a
reading sheet: 3. Any other is a card: 1 or 2, by the hash. `margin` wins over `wide`: most reading sheets are both,
and `wide` only sets width. Small `wide` list items pass `bloom="card"` (A32). `bloom` (`card | reading | page | none`) overrides; Razor
and the hand-written Vue sheets pass it. `none` renders no field. A Home card or stacker card whose backoffice wash is
unset passes `none` and stays bare paper (owner ruling, FR-010). Every other top sheet blooms. (A14, A26)

**Rationale**: the schedule (R6) and the two-deep cap (R8) need the count on the server. The field is a size
container (R15), so a container query could count by real width; it would leave the server planning washes the CSS
then hides.

**Alternatives considered**: a container query on the field (rejected above); a bare share of cards (FR-010 permits
it; held back as a performance lever, R11).

## R8. Geometry, two deep at most, and pigment

**Decision**:

- **Even cover by strata.** A sheet's area is split into strata and each wash is centred at a hash-jittered point
  inside its own: one wash, the middle half of the sheet; two, opposite halves along a hashed diagonal; three or four,
  one quadrant each, a hashed rotation of which quadrant is which. (FR-012)
- **Size and shape.** Width 45% to 125% of the field's width (`cqw`); height from an aspect ratio of 0.68 to 1.32;
  rotation 0 to 360°; eight border radii from 32% to 68%.
- **Two deep at most (FR-017).** Washes pair up: {0, 2} and {1, 3}, each pair in opposite quadrants. If the two
  washes of each pair never touch, no point lies under more than two washes, one from each pair. The kit caps each
  wash of a pair in CSS against the real field size:

  ```css
  --wash-gap: hypot(calc(var(--wash-dx) * 100cqw), calc(var(--wash-dy) * 100cqh));
  width: max(0px, min(calc(var(--wash-d) * 100cqw), calc((var(--wash-gap) - 2 * var(--wash-reach)) / var(--wash-s))));
  ```

  `--wash-dx`, `--wash-dy` are the fractional offsets to the pair partner's centre; `--wash-s` is
  `sqrt(1 + 1 / aspect²)`, so `width × s / 2` is the radius of the circle round the rotated box; `--wash-reach` is how
  far the `#kcc-bloom` bleed carries pigment past the box. Two such circles, each at most half the centre distance
  less the bleed, never meet. The scale animation only shrinks. Sheets with one or two washes need no cap.
  `washes.test.ts` mirrors the cap in TypeScript and checks disjoint pairs for every seed and a range of sheet sizes
  from a 280×120 card to a 700×2400 reading sheet and a 1300×400 hero. On a short, wide hero the cap holds four
  washes to about a quarter of the width.
- **Pigment.** Two radial gradients per wash, the first ink and a second offset from it, each a brand token through
  `oklch(from var(--wash-ink) l c h / α)`. The dried edge lifts the first ink's alpha near the rim; it does not darken
  the colour (A15).
- **Colour pair.** A per-sheet shuffle of the eight washes; wash `k` takes slot `k` as its first ink and slot `k + 4`
  as its second; a chosen colour is moved to slot 0.
- **Strength.** Each wash peaks at 75% to 100% of the ramp's `--wash-strength` (R10).
- **Bleed and grain.** One SVG filter, `#kcc-bloom`, in `Layout.cshtml`: the wax displacement and grain plus a
  Gaussian bleed, replacing `#kcc-wax` and a CSS `blur()`. `--wash-reach` is pinned to twice its `stdDeviation` by a
  test. `#kcc-wax-flat` stays for tiles. (A22)

(A15, A22, A24)

**Rationale**: the server cannot know a sheet's height, so a server-side overlap test cannot hold on every sheet
shape; CSS can, once the field is a size container (R15). Pairing turns "at most two deep" into "two pairs that never
touch", which a per-wash cap enforces without any wash knowing more than its partner.

**Alternatives considered**: rejection sampling on the server (needs the sheet's aspect, unknown at render); a
non-uniform scale of a unit-square layout (exact, but stretches every wash on a tall sheet); the prototype's R2
positions (even, but no structure to pair).

## R9. Motion: the bloom against the kit's motion rules

**Decision**:

- `animation: kcc-bloom var(--wash-bloom) cubic-bezier(0.17, 0.72, 0.24, 1) both`, delay
  `calc(var(--wash-at, var(--wash-fallback-at)) - var(--wash-elapsed, 0s) - var(--wash-lag, 0s))`, keyframes on
  `opacity` and `scale` only. `--wash-bloom: 90s` in `Torn/Tokens.css`, the 210s window in `washes.ts`; a test holds
  them to 300s together.
- Reduced motion swaps in `kcc-bloom-fade` (opacity only), same duration and delay.
- `Main.css`'s reduced-motion rule and `Kit.css`'s ramp-switch freeze both exclude `.kcc-wash`: the freeze sets
  `animation: none`, which would restart every bloom, and the reduced-motion rule is `!important` in `@layer base`,
  which no later layer overrides.
- `motion.test.ts` names the bloom as the one non-loop animation past 500ms and asserts both exclusions.

(A17)

## R10. Strength against contrast

**Decision**: `--wash-strength` replaces `--wash-op`, one value per ramp. `contrast.test.ts` models the strongest
point of a two-wash overlap: each wash adds its first ink at the rim alpha and its second at its core alpha, times the
strength, saturated by `--wash-sat` (the Filter Effects `saturate()` matrix in sRGB), times a grain gain that stands
in for the `#kcc-bloom` grain composite as text reads it over a 3px average (1.26 multiply, 0.86 screen, calibrated
against headless Chromium renders of the real filter, reading at or below each), stacked source-over in the field, and
the field blended onto paper (multiply in light, screen in dark). For every ordered choice of the eight washes in both
layers and both inks, in both ramps, it asserts ink and ink-soft at 4.5:1 and hair-strong at 3:1, and that a tenth more
strength fails. The owner set the values at the D6 check: light .55 at saturation 2.5, dark .16 at saturation 3. (A19)

**The existing `it.fails`**: `contrast.test.ts` (lines 279-284) keeps a failing case on record: hair-strong over a
dark-ramp wash core measures 2.6–2.9:1 under today's single wash at `--wash-op` .26. The owner's ruling makes 3:1 a
requirement over the two-wash stack in both ramps, so that case becomes a plain `it` over the new model, with no
`fails` branch for either ramp. The old single-wash cases (ring at .38, core at 1, the dark core floor of 3:1) model
the retired gradient and go with it. The well cases, including the `softInWell` `it.fails`, are unchanged.

**Rationale**: FR-017 and SC-006. A blur only lowers a peak, so ignoring it is conservative; multiply and screen are
monotone in each layer, so the worst case lies among the colour choices the test enumerates.

## R11. Performance (SC-009)

**Decision**: animate `opacity` and `scale` only; no `will-change`; the bleed in the one SVG filter, rendered once per
wash; the field blends as one group. Measure twice on the library page with about 50 washes, under mobile emulation
and 4× CPU throttle: at full bloom (static paint), and at 150s of visit time, when about 21 washes are mid-bloom at
once, the busiest moment. Each run scrolls for five seconds and records the frames. If either stutters, the levers in
order: shrink the bleed, drop `scale` from the keyframes, then leave a share of cards bare (FR-010).

**Not verified**: frame timing on a real mid-range phone; the emulated runs stand in.

## R12. The grain rule

**Decision**:

- `.kcc-crayon` moves to `#app`'s first children beside `.kcc-grain`, both `z-index: 0`, under every sheet. The desk
  looks as it does (FR-022): in light both multiply, and in dark the crayon still follows the grain.
- `.kcc-sheet::after`, the `.kcc-sheet--scroll` grain layer, `--sheet-grain`, `--sheet-grain-op` and
  `--sheet-grain-blend` go and join `retiredTokens.test.ts`.
- `grain.test.ts` (static, Vitest) over every `.css`, `.vue` and `.cshtml` file in `Features/**`, inline `style`
  attributes included:
  1. **Sources.** A grain source is `--grain`, `--crayon` (each matched as a whole name, so `--grain-op` is not
     one), `url(#kcc-bloom)`, `url(#kcc-wax-flat)`, any
     `feTurbulence`, and any texture image: `url("data:image/svg`, `repeating-linear-gradient` or
     `repeating-radial-gradient`. The custom properties `--grain` and `--crayon` may be declared only in
     `Torn/Tokens.css`'s `:root` block, where their `feTurbulence` lives; the two filters only in `Layout.cshtml`'s
     defs. A source anywhere else fails.
  2. **Aliases.** The test builds the graph of every custom-property declaration and resolves `var()` chains, so
     `--tooth: var(--grain)` is itself a grain source wherever it is declared or read.
  3. **Uses.** A rule that reads a source or an alias must select only `.kcc-grain`, `.kcc-crayon` (with their
     `:root[data-theme='dark']` forms), `.kcc-wash` or `.kcc-tile`. The crayon's stripe gradient passes because it
     sits in `.kcc-crayon`; the same gradient on `.kcc-field` fails.
  3a. **The tape.** The woven stripe on `.kcc-tilewrap > .kcc-tape` (and its `:root[data-theme='dark']` form) is the
     tape's own material, not grain (owner ruling, session "analyze"). The scan exempts those two selectors by name,
     and only for their `repeating-linear-gradient`. A test proves the exemption is by name: the same gradient on any
     other selector, a new `.kcc-tape` rule included, still fails.
  4. **Placement.** `.kcc-grain` and `.kcc-crayon` are `App.vue`'s first two children and declare `z-index: 0`.
- `GrainTests` (E2E, both ramps, at 0s of visit time, so no wash): a screenshot of a 40×16px patch of bare paper
  between two rules, in a sheet's padding, has a luminance standard deviation under 0.5 levels; the same patch size on
  the desk is over it, proving the sample can see grain. A second check walks the stacking: no element with a grain
  background or filter paints above a `.kcc-sheet`.

(A18, A20)

**Rationale**: the original scan would have failed on `Tokens.css` lines 31-33, whose token values hold the
`feTurbulence`, and an exemption for them would have let an alias or a non-`feTurbulence` texture through. Declaring
where sources may live, resolving aliases and treating any texture image as a source closes both. The pixel sample
catches what no static scan sees.

## R13. Sheets mounted after first paint, and the back-forward cache

**Decision**: a `KccWashes` set up after hydration adds `--wash-lag`, the seconds since the page loaded, to its first
render. A page restored from the back-forward cache resumes where it was left. (A16)

## R14. Scroll sheets

**Decision**: on a `kcc-sheet--scroll` the field is absolutely placed in the scroll container, covers the first
scrollport, and scrolls with the content; it never extends the scroll range (R15). (A21)

## R15. The field never changes layout

**Decision**: `.kcc-washes` takes `overflow: clip` and `container-type: size`. An E2E check loads the library, a
recipe, a variant page and an open phone pad card at 300s of visit time, and asserts the document's and every scroll
container's `scrollWidth` and `scrollHeight` are equal with washes and with `.kcc-washes { display: none }` injected.
(A23)

**Rationale**: the red team was right. Washes up to 125% wide, rotated and centred anywhere, are absolutely placed
boxes that add to their scroll container's scrollable overflow, and `clip-path` on the sheet does not reduce it: the
document would grow, a phone pad card would scroll further, and the scroll range would change while washes scale up.
The prototype clipped its field (`overflow: hidden`, `reference/artifact-wash.css:61`). `clip` rather than `hidden`
makes no scroll container. The field is absolutely placed with `inset: 0`, so size containment on it changes nothing
about the sheet's own size.
