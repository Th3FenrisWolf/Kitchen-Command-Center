# Look rounds

Nine rounds, served from `.mockups/007-crayon-wash-texture/` against the built kit stylesheet, with hand-written wash
fields on recipe cards, a four-wash reading sheet and two 360px phone cards. Every candidate was drawn with a live SVG
filter on the wash, as a mockup only; the plan decides how the approved look is built. Every round ran at the 006
strengths, .42 light and .18 dark, except round 8, which also shows the projected ones, and round 9, which shows the built washes at .42 and .125.

`wash.html` is the approved round (it needs the kit stylesheet the preview server links), and `wash-light.jpg` and
`wash-dark.jpg` are its captures.

## Approved: chalk strokes, edge-weighted, lobed and ragged, soft blur

Approved 2026-11-04, in both ramps.

- **Placement and size.** Each wash centres in the outer band of its quarter on one axis, chosen per wash: from 2%
  past the sheet's edge to 16% inside it. The other axis stays inside its half, from 10% to 45% (or 55% to 90%). A
  one-wash card takes a random quarter rather than the middle. Sizes run from 90% to 145% of the sheet's width (125%
  after round 8), where 006 ran them from 45% to 125%. Paired washes keep their cap against each other, so no point
  lies under more than two washes. Most washes are cut by an edge, so a whole circle is rare.
- **Outline.** A lobed, ragged blob: the slow wobble is the 005 bleed turbulence (`baseFrequency 0.012 0.016`, 3
  octaves, seed 5) at displacement scale 60 instead of 24, then a second displacement (`baseFrequency 0.08`, 2
  octaves, seed 3, scale 12). The stroke ends are ragged: a vertical-only displacement of scale 10 from turbulence at
  `baseFrequency 0.44 0.004`, 1 octave, seed 11, so each stroke stops at its own length.
- **Fading edge.** The outline is blurred by `stdDeviation 4`, and the first ink fades over a longer run: alpha .34 at
  0%, .32 at 62%, .2 at 80%, .07 at 92%, 0 at 100%. The 005 rim, where the alpha rose again near the edge, is gone.
- **Marks.** Short chalk strokes, the same in both ramps: stroke turbulence `baseFrequency 0.44 0.1`, 2 octaves,
  seed 11, cut to an alpha mask by `5R − 1.6`; paper tooth `baseFrequency 0.6`, 2 octaves, seed 21, cut by
  `3R − 0.5`. The strokes are the masked ink plus half the unmasked ink in the gaps (`k2 = 1`, `k3 = 0.5`). Each
  patch's strokes run along its own rotation.
- **Soft blur.** One pixel (`stdDeviation 1`) over the finished marks, so the dashes melt slightly into smudged chalk
  from arm's length and still read as marks up close.
- **Colour.** No colour speckle. The inks are saturated as they are today.

## Rounds, and why the other options lost

1. **Texture.** Now, Faithful 005, Dry edge, Pencil tooth, and Wax rub, which was added as a crayon option. Faithful
   005 rendered almost exactly like Now in Chromium and WebKit: 006's bake was faithful, and the soft round spot was
   already there in 005. Dry edge read as a flat disc. Wax rub's flecks read as blotchy. **Picked: Pencil tooth.**
2. **Placement and size**, on the pencil tooth. The owner named the small, whole circles as part of the mould. Current
   placement, Edge-weighted, Corner sweeps. Corner sweeps put more colour under text in the corners. **Picked:
   Edge-weighted.**
3. **Stroke weight.** Fine lead, Soft lead, Thick lead. Thick broke into rungs and lost fineness at phone width.
   **Picked: Soft lead.**
4. **Coverage.** Soft lead, Fuller, Fullest. The owner asked for less bare paper. Fullest left thin pale gaps that
   read as rain. **Picked: Fuller.**
5. **Outline.** Today's blob, Ragged stroke ends, Lobed and ragged, Coloured-in box. **Picked: Lobed and ragged**, with
   the edge faded more.
6. **Soft edge, and dark.** In dark, the fuller pencil's uncoloured gaps showed as thin dark strands on the lit fill,
   which read as fur. Same strokes, Blended strokes, Short chalk strokes, Pastel rub. **Picked: Short chalk strokes,
   in both ramps**, a little finer, with a blur on top to try.
7. **Blur on top**, on the finer chalk. None, half a pixel, one pixel, a pixel and a half. **Picked: Soft blur, one
   pixel.**
8. **Strengths and sizes** (A15, A16, A21), on the approved soft-blur chalk, served as `round-8-strengths.html` with
   captures `round-8-<ramp>-<engine>.jpg` (Chromium and WebKit, both ramps). Option 1 is the approved .42 light and
   .18 dark; Option 2 is the projected about .39 and .13, which the contrast table is expected to force (R9), so dark
   falls by about a quarter. Both show the same wash fields: the card fields and the four-wash reading sheet at the
   owner's .90 to 1.25 sizes, and two phone cards at d .90 (about 300 px). The second ink is centred within a quarter
   of the first ink's radii from its ellipse centre (R2). Below, an unpaired reading-sheet wash at d 1.25 of an 860 px
   sheet (about 1,075 px) and the same wash capped at 1,000 px, at the projected strengths. Each size previews what one
   480 px reference bake draws: the outline's turbulence frequencies are divided, and its displacement scales and blur
   multiplied, by the wash's size over 480 px (1.0 cards, 1.6 reading sheet, 2.24 large, 2.08 capped, 0.625 phone); the
   chalk marks stay unscaled. Why: the owner sees the lower strengths and the largest and smallest outlines before the
   bake fixes them. **Approved 2026-11-05: both.** The projected strengths read as chalk in both ramps, and the
   uncapped large wash's outline holds, so no px cap on a wash's width applies.
9. **The built washes**, served as `round-9-built.html` from `round-9-gen.mjs`. These are the site's own captures
   (`yarn perf:capture 007-chalk`) of home, recipe search, a recipe and sign-in: bloomed, in both ramps, both engines
   and both profiles. Each row puts the pre-006 capture (`97c51247`), the after-006 capture (`main`) and this build
   side by side, under the approved look and round 8. Full-resolution crops show the largest reading-sheet wash, the
   smallest phone-card wash, two washes overlapping and a wash centred past a card's edge, in both ramps. The harness
   captures a visit's start and its full bloom, not mid-bloom. **Approved 2026-11-05 (SC-001)**: the built washes
   read as chalk, in both ramps. This includes dark at .125 and the soft vertical bands on the wide home category
   cards. The captures are `round-9-home-<ramp>.jpg` and `round-9-search-phone-<ramp>.jpg`, with the crops in
   `round-9-crop-overlap-<ramp>.jpg` and `round-9-crop-edge-<ramp>.jpg`.

## Still open

Nothing. Both questions are answered.

- **How the look is built.** Baked, with no live filter (research R1): a 128px chalk tile and eight silhouettes from
  `yarn washes`, so a wash carries no filter and every 006 budget holds.
- **Strengths.** The contrast table set them (T041): light .42, which stays under its cap, and dark .125, the cap
  at which text holds WCAG AA over two washes at their peak.
