# Bug Assessment: The phone pad cards vanish once they finish opening

- **Slug**: phone-pad-card-vanishes
- **Created**: 2026-11-12
- **Source**: pasted text, with an 18-second screen recording from the owner's iPhone
- **Verdict**: valid
- **Severity**: high

## Report (verbatim or summarized)

> My mobile menu is scuffed please fix it

The recording (`ScreenRecording_11-12-2026_14-37-31_1.mp4`, 1180×2556, 18 s) shows kitchen-command-center.com on an
iPhone, on a recipe page (Jambalaya, Andouille Sausage and Chicken), dark ramp, signed out. Read at 10 frames a
second:

| Time | What shows |
| --- | --- |
| 1.3 s | Tap on Menu. The button fades to the marker pill, and the Menu card slides down as a bare sheet. |
| 1.5–1.7 s | The card's rows (Meals, Diets, Quick picks, Sign in, Ask for an account) fade in. |
| 1.8 s | The whole card is gone, sheet, rows and shadow. The page shows where it was. The button stays green. |
| 1.8–2.9 s | Nothing changes: the button says open, no card shows. |
| 3.0–3.1 s | Tap on Menu again. The card reappears in place as a bare sheet, its rows faded out. |
| 3.2 s | The card and the pill are gone. |

The open repeats at 4.0 s and 5.0 s with the same result. Tapping the search glyph opens the find card with the
keyboard up, and that card vanishes the same way while the field keeps focus and the keyboard stays up.

## Symptom

On a phone in iOS WebKit, an open pad card (`#pad-card-menu`, `#pad-card-find`) shows only while its transform
transition runs. About 500 ms after the tap, when the 480 ms open transition ends, the card stops painting, though
the pad still holds it open, the menu shows the pill and `aria-expanded` is true. Closing it makes it paint again for
the 280 ms close transition. Expected: the card stays drawn for as long as it is open.

## Reproduction

1. On an iPhone, open any page of the site in Safari, or in any iOS browser, all of which run WebKit. Both ramps are
   expected to fail; the recording is dark.
2. Tap **Menu** in the header.
3. Wait half a second after the card lands. The card disappears and the button stays green.
4. Tap **Menu** again. The card flashes back as it slides away.
5. Tap the search glyph. The find card opens with the keyboard, then vanishes in the same way.

Not checked: desktop Safari's desktop cards (Recipes, My Kitchen, search). The owner has not tried them. Chromium
draws every card correctly. This container has no WebKit, so the fault was read from the recording, not reproduced
here.

## Suspected Code Paths

- `src/KCC.Web/Features/Components/Header/PadCard.vue:59-90` — `.pad-card` slides with
  `transition: transform 480ms` into `.is-open`. While the transition runs, WebKit composites the card on its own
  layer, and the recording shows it drawn. When the transition ends, the card loses its layer and paints into its
  nearest composited ancestor, and from that moment it is gone.
- `src/KCC.Web/Features/Components/Header/AppHeader.Component.vue:406-413` — `.pad-header` is `position: sticky`,
  which WebKit always composites, so the sticky header's layer is where a resting card paints. It also carries
  `overflow-x: clip`. The cards hang below the header's box: `.pad-cards` starts at `calc(100% - 14px)` of the pad.
  Content that paints into the header's layer outside the header's box is lost: the cards vanish and the bar above
  them stays.
- `src/KCC.Web/Features/Components/Header/AppHeader.Component.vue:447-456` — `.pad-cards` clips with
  `overflow: hidden`, so a card hides above its top edge while it rests. It holds every card, so it is the container
  whose contents are lost.
- `src/KCC.Web/Features/Styles/Main.css:37-41` — `#app` already clips sideways with `overflow-x: clip` (kit invariant
  14, spec 002 R10). The header is a full-width grid row of `#app` with no inline padding, so its own
  `overflow-x: clip` clips at the same two edges and adds nothing. Spec 002's research (R10) lists it as one of the
  two sideways clips that existed before `#app` took the job.
- `src/KCC.Web/Features/Components/Header/PadCard.vue:139-146` and `src/KCC.Web/Features/Styles/Torn/Kit.css`
  (`.kcc-sheet--scroll`) — the phone cards' sheet is a scroll container (`overflow-y: auto`) and carries the tear's
  `clip-path`. Only the phone cards have this. If desktop Safari turns out to draw its cards, this becomes the
  suspect.

## Root Cause Hypothesis

WebKit draws the card correctly while it has its own compositing layer and drops it when the card paints into the
sticky header's layer. The recording supports this directly: the card shows for exactly the length of each transform
transition and disappears within a frame of the 480 ms open transition ending. The most likely trigger is the header's
`overflow-x: clip`. On a composited layer with an overflow clip, WebKit sizes the layer's backing from its local clip
rect. For a clip on one axis, that rect is unbounded along the other, and the cards outside the header's box do not
get drawn. Confidence that the card fails whenever it paints into the header's layer: high. Confidence that
`overflow-x: clip` is the trigger: medium. The phone sheet's scroll container with a `clip-path` is the other
candidate.

## Proposed Remediation

**Preferred** (the owner's choice: both changes): make two CSS changes, with no script and no markup:

1. Remove `overflow-x: clip` from `.pad-header`. `#app` already clips sideways overflow at the same edges, so nothing
   on any page changes in Chromium. This removes the suspected trigger.
2. Keep a peeking or open card on its own layer: `will-change: transform` on `.pad-card.is-peek` and
   `.pad-card.is-open`. This holds the card in the state the recording shows working. A card at rest is hidden and
   has no layer, so the five cards cost nothing until one is shown. When the card closes, the class goes and the
   close transition keeps the layer until the card hides.

**Alternatives**:

- Drop the clip only. This is the smallest change and names one cause. If the hypothesis is wrong, the menu stays
  broken until another deploy round.
- Own layer only. This matches the recording directly but leaves the suspected trigger in place for anything else
  that paints into the header later.

**Files likely to change**:

- `src/KCC.Web/Features/Components/Header/AppHeader.Component.vue`
- `src/KCC.Web/Features/Components/Header/PadCard.vue`
- `tests/KCC.ViteTests/Features/Components/Header/AppHeader.test.ts` or `PadCards.test.ts`
- `docs/brand/kit.md`, if a sentence on the pad states where the sideways clip lives or how cards are layered

**Tests to add or update**:

- A Vitest style test, written first and failing: `.pad-header` declares no `overflow-x` or `overflow`, since `#app`
  holds the sideways clip (the existing `pile.test.ts` pins `#app`'s `overflow-x: clip`).
- A Vitest style test, written first and failing: `.pad-card.is-peek` and `.pad-card.is-open` declare
  `will-change: transform`, and the resting `.pad-card` rule does not.
- `washField.test.ts` already forbids `will-change` on `.kcc-wash`. It stays green because the new declaration is on
  `.pad-card`.
- Proof on WebKit is the owner's: open Menu and the search card on an iPhone, wait past the slide, and confirm the
  card stays drawn and its rows are tappable. `/speckit-bug-test` records the result.

## Risks & Considerations

- The fault can't be reproduced in this container, which has Chromium only. The CSS tests prove only that the change
  landed. Only the owner's iPhone proves the fix, so the bug stays open until that check.
- `will-change: transform` adds one composited layer while a card shows, about 300×470 CSS px on a phone. Spec 006
  declined `will-change` for the hover lift on budget grounds. That decision covered a different element and doesn't
  bear on this one.
- Without `overflow-x: clip` on the header, a sideways overhang from the pad (its page's turn and scale, the
  `.pad-cards` box 40px past each side) is clipped by `#app` instead. That is the same edge, so no page should
  scroll sideways. The 375px walk in spec 002 (A24) is the check.
- A composited card inside an `overflow: hidden` box still needs the box's clip, which WebKit applies through an
  ancestor clipping layer. That is the arrangement the recording shows working during the slide.

## Open Questions

- Does desktop Safari draw its cards? Not checked. The fix covers both, and the answer only narrows the recorded
  cause.
