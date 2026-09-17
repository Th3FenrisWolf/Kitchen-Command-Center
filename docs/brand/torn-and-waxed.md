# Torn & Waxed

The visual identity of Kitchen Command Center's public site. This document is the brand: what it is, what it
forbids, and why. The engineering contract that implements it is [kit.md](kit.md). When the two disagree,
this one wins and `kit.md` gets fixed.

Source mockup: claude.ai/design project `2bf4f7b9-045b-4b9c-b923-a5e402273b6e`, file
`Torn Crayon Identity Soft.html` (the **Soft** variant; the hard variant is not built).

## The idea

**The perfect drawing, torn out and kept.** Every panel is a sheet ripped off a pad: fibre on the edge, wax
over the colour, a strip of tape where it was held down. Everything *drawn on* the sheet is exact: one ruled
grid, one line weight, numbers that line up. The mess is the paper. The work is clean.

Three rules follow, and every other rule in this document is one of them applied.

1. **The paper is careless.** Torn on four sides, tilted up to a degree, sometimes taped, coloured in wax.
   Nothing about the sheet is precise, and no two sheets share an edge.
2. **The drawing is exact.** Inside the tear a 24px rule governs everything. Type sits on it. Icons share one
   style. Every number is set in a rounded mono, tabular, so columns align without tables.
3. **The tear is the frame.** Nothing is stroked. No borders, no radii on paper, no drawn shadows. Depth is a
   hair of fibre along the edge and a soft fall beneath the silhouette. *The edge is the border; nothing is
   drawn around anything.*

## What is torn, what is printed

| Torn (paper) | Printed (marks on the paper) |
|---|---|
| Sheets: every panel, card, hero, form, list | Buttons, segmented controls, fields, badges, checkboxes |
| Accent tiles pinned to a sheet's corner | Labels, tape, stat numbers, meta lines, step numbers |

Torn things are clipped to a generated tear, carry the fibre and the fall, and may tilt. Printed things are
clean, unclipped, never shadowed, and take the only radii in the system: pills for controls, 6px for labels
and textareas, 3px for checkboxes.

## Stock

Ink is purple-slate, never black; it belongs to the desk. Paper is warm and near-white. The desk is a pale
lilac-grey. Marker green is the one accent and the only strong fill: primary action, labels, selected state.

| Role | Light (default) | Dark |
|---|---|---|
| desk / desk-2 | `oklch(.884 .016 288)` / `oklch(.836 .018 288)` | `oklch(.132 .024 288)` / `oklch(.176 .028 288)` |
| paper / paper-2 | `oklch(.958 .009 92)` / `oklch(.972 .008 92)` | `oklch(.258 .030 288)` / `oklch(.222 .028 288)` |
| ink / ink-soft | `oklch(.232 .026 288)` / `oklch(.432 .024 288)`\* | `oklch(.945 .012 92)` / `oklch(.735 .018 288)` |
| hair / hair-strong | ink at 16% / 74%\* | chalk at 18% / 45% |
| rule | `oklch(.58 .045 288 / .22)` | `oklch(.88 .02 288 / .13)` |
| marker / marker-ink | `oklch(.876 .112 126)` / `oklch(.24 .03 288)` | same |
| fiber / fall | `oklch(1 .006 92)` / `oklch(.25 .03 288 / .28)` | `oklch(.435 .034 288)` / `oklch(.04 .02 288 / .7)` |
| tape | `oklch(.985 .012 92 / .6)` | `oklch(.9 .012 92 / .28)` |

\* Two values are pinned by `contrast.test.ts` rather than by the mockup: `ink-soft` (the mockup's `.452`
fails WCAG AA for small caps on desk-2) and light `hair-strong` (the mockup's 40% alpha reads at 1.6:1
against paper; a control boundary needs 3:1). `hair` keeps the mockup's lightness for dividers.

Light leads. Dark is the same identity on a dark desk: chalk for ink, washes screened instead of multiplied,
the wax tooth soft-lit instead of multiplied. Every visual change is checked in both.

## Washes

Eight crayons. One wash per sheet, off-centre, bleeding past the tear, under the sheet's margin or a
corner. **Never under running text.** Identical in both ramps; the blend mode changes, the pigment does not.

| Token | Name | Value |
|---|---|---|
| peach | oat | `oklch(.886 .055 70)` |
| yellow | butter | `oklch(.906 .062 95)` |
| green | marker | `oklch(.876 .112 126)` |
| teal | mist | `oklch(.886 .045 175)` |
| sky | powder | `oklch(.890 .038 205)` |
| lavender | wisteria | `oklch(.874 .042 292)` |
| pink | dusty rose | `oklch(.876 .046 345)` |
| red | faded red | `oklch(.790 .085 25)` |

Washes are fills. Status is a wash behind ink text (red for danger, green for success, yellow for warning),
never coloured text. Links are ink with a hair-strong underline; there is no link colour.

## Type

Everything sits on a 24px rule. Display takes two rules. One weight of everything: regular.

| Role | Face | Size / line |
|---|---|---|
| Display | APCasual | 40 / 48, letter-spacing 0 |
| Heading | APCasual | 22 / 24 |
| Chrome mark, section name | APCasual | 24 / 24 |
| Body | Hazelnut | 15 / 24 |
| Kick, label, meta | Sono, caps, `.1em` | 10.5 / 24 |
| Numbers | Sono, tabular | inherit; stats 26 / 24 |
| Hand note | APCasual italic | 17 / 24 |

Sono is a rounded mono (`MONO` axis at 1) that keeps the two hand fonts company. Hand notes are casual
italic, at most one per sheet, and never carry information the sheet needs. No bold, no semibold, no medium:
emphasis is a kick line, a label, or marker, never weight.

## Icons

Font Awesome. Duotone by default: ink over a faint second layer at 35%. Solid or regular where the simpler
glyph reads better. Always `currentColor`. Never coloured, never on a coloured ground of their own except a
tile's wash.

## Edge and handling

Handling is fixed at **Handled**: sheets tilt within ±1° and may take one strip of tape. Pristine (square,
untaped) and Well-thumbed (fingerprints, smears, corner strips, heavier wax) exist in the mockup and are not
built.

- **Tear**: fine and dense on all four sides, 2.4px amplitude on sheets, 1.8px on tiles, with the odd deeper
  fibre nick. Six sheet tears, one hero tear and three tile tears exist; neighbours never share one.
- **Cut corner**: one clean 16–20px chamfer through the tear at the top-right, bottom-right or bottom-left.
  Never top-left, where the label lives. About one sheet in four is uncut. Heroes take a 28px cut.
- **Label**: a flat marker pill overlapping the top-left edge. Sono caps. Never torn, never shadowed, no
  grain: the one thing on the sheet that was printed. It sits outside the tear and outside the shadow.
- **Tape**: opt-in. One translucent strip, top-centre, slightly rotated. A pinned tile takes a smaller strip.
- **Wax**: a light crayon tooth over desk and paper alike (multiply in light, soft-light in dark); washes and
  tiles carry a finer grain of their own.
- **Pencil ruling**: every sheet is ruled at 24px, faint, aligned to its padding. Always on.

## Layout rhythm

Sheet padding 24px (heroes 48px). Grid gaps 36px between rows, 28px between columns. Sections 72px apart,
a section heading 36px above its content. Margins between blocks are multiples of 24px.

## Applied patterns

- **Recipe slip**: a plain sheet; a small torn wax tile pinned over the top-left corner with its own tape; the
  hero stat (rating · reviews, or time) top-right in Sono; the title in APCasual 22; a Sono meta line;
  hairline badges. The tile's wash is the recipe's colour.
- **Library**: a green-washed sheet holding search, sort and view controls; below it a grid of slips cycling
  through the six tears.
- **Detail**: a hero slip with a 28px cut, the recipe's wash under the left margin, a large pinned tile; then an
  "At a glance" stats sheet; then ingredients (checklist, teal wash) beside method (numbered steps, peach
  wash). One hand note at most.
- **Lists**: checklists on the rule: 12px box, quantity in Sono right-aligned, done rows struck through.

## Don't

- Don't stroke anything: no `border`, no outline on paper, no drawn frames. Inset hairlines on printed
  controls are the one exception.
- Don't shadow anything except the sheet's fall. No `shadow-*` utilities; no shadow on labels, tape or tiles.
- Don't round paper. No radius on sheets, tiles or images.
- Don't put text on a wash core. Washes go under margins and corners.
- Don't colour icons, links or status text.
- Don't use bold.
- Don't tear a control. Only paper is torn.
- Don't let two neighbouring sheets share a tear.
- Don't give a sheet more than one wash, one label or one hand note.

## Decisions and history

- **2026-09-17**: Torn & Waxed (Soft) replaces Softbound Sketch, whose drawn outlines and hatched shadows are
  retired. Light is the default ramp. The handling and ruling dials from the mockup are not built. The class
  prefix is `kcc-`. Tears are pure-CSS polygon presets; a JS `path()` tear is the fallback if fidelity needs it.
