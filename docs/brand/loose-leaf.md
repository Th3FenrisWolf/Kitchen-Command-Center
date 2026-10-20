# Loose Leaf

The visual identity of Kitchen Command Center's public site. This document is the brand: what it is, what it
forbids, and why. The engineering contract that implements it is [kit.md](kit.md). When the two disagree,
this one wins and `kit.md` gets fixed.

Source mockup: claude.ai/design project `2bf4f7b9-045b-4b9c-b923-a5e402273b6e`, file
`Torn Crayon Identity Soft.html` (the **Soft** variant; the hard variant is not built).

## The idea

**The perfect drawing, torn out and kept.** Every panel is a sheet ripped off a pad: fibre on the edge, wax
over the colour, a blank sheet under it, knocked a little askew. Everything *drawn on* the sheet is exact: one
ruled grid, one line weight, numbers that line up. The mess is the paper. The work is clean.

Three rules follow, and every other rule in this document is one of them applied.

1. **The paper is careless.** Torn on four sides, tilted up to a degree, piled two high, coloured in wax.
   Nothing about the sheet is precise, and no two sheets share an edge.
2. **The drawing is exact.** Inside the tear a 24px rule governs everything. Type sits on it. Icons share one
   style. Every number is set in a rounded mono, tabular, so columns align without tables.
3. **The tear is the frame.** Nothing is stroked. No borders, no radii on paper, no drawn shadows. Depth is a
   hair of fibre along the edge and a soft fall beneath the silhouette. *The edge is the border; nothing is
   drawn around anything.*

## What is torn, what is printed

| Torn (paper) | Printed (marks on the paper) |
|---|---|
| Sheets: every panel, card, hero, form, list, and the blank under each | Buttons, segmented controls, fields, badges, checkboxes |
| Accent tiles pinned to a sheet's corner | Labels, tape, stat numbers, meta lines, step numbers |

Torn things are clipped to a generated tear, carry the fibre and the fall, and may tilt. Printed things are
clean, unclipped, never shadowed, and take the only radii in the system: pills for controls, 6px for labels,
wells and textareas, 4px for small marks, 3px for checkboxes.

## Stock

Ink is purple-slate, never black; it belongs to the desk. Paper is warm and near-white. The desk is a pale
lilac-grey. Marker green is the one accent and the only strong fill: primary action, labels, selected state.

| Role | Light (default) | Dark |
|---|---|---|
| desk / desk-2 | `oklch(.884 .016 288)` / `oklch(.836 .018 288)` | `oklch(.132 .024 288)` / `oklch(.176 .028 288)` |
| paper / paper-2 | `oklch(.958 .009 92)` / `oklch(.972 .008 92)` | `oklch(.258 .030 288)` / `oklch(.222 .028 288)` |
| ink / ink-soft | `oklch(.232 .026 288)` / `oklch(.432 .024 288)`\* | `oklch(.945 .012 92)` / `oklch(.735 .018 288)` |
| hair / hair-strong | ink at 16% / 56%\* | chalk at 18% / 45% |
| rule | `oklch(.58 .045 288 / .22)` | `oklch(.88 .02 288 / .13)` |
| marker / marker-ink | `oklch(.876 .112 126)` / `oklch(.24 .03 288)` | same |
| fiber / fall | `oklch(1 .006 92)` / `oklch(.25 .03 288 / .28)` | `oklch(.435 .034 288)` / `oklch(.04 .02 288 / .7)` |
| tape | `oklch(.985 .012 92 / .6)` | `oklch(.9 .012 92 / .28)` |

\* Two values are pinned by `contrast.test.ts` rather than by the mockup: `ink-soft` (the mockup's `.452`
fails WCAG AA for small caps on desk-2) and light `hair-strong` (the mockup's 40% alpha composites to 2.4:1
against paper; a control boundary needs 3:1, so it ships at 56%, with headroom for the wax overlay). `hair`
keeps the mockup's lightness for dividers.

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

Washes are fills. Status is a wash behind ink text, never coloured text: a well of paper tinted with the wash
(red for danger, green for success, yellow for warning) carrying an ink message. See `kit.md` → Status.
Links are ink with a hair-strong underline; there is no link colour.

## Type

Everything sits on a 24px rule. Every line height is a multiple of the rule, a control's or a pill's height, or 1
on a glyph, number or unit inside a line. Sizes 14, 16, 20, 24, 32 and 40, with the display at 36 on phones, and at no width do two headings
share a size. Nothing under 14px, and nothing but Sono under 16px. One weight of everything: regular.

| Role | Face | Size / line, from 768px | Below 768px |
|---|---|---|---|
| Display | APCasual | 40 / 48 | 36 / 48 |
| Section heading | APCasual | 32 / 48 | same |
| Sheet heading, stat value | APCasual (stat value Sono) | 24 / 24 | same |
| Card title, hand note | APCasual (hand note italic) | 20 / 24 | same |
| Body, nav item, field | Hazelnut | 16 / 24 | same |
| Kick, label, meta, button, badge | Sono, caps, `.1em` (`.14em` on buttons and labels, `.12em` on badges and segments, `.08em` on meta) | 14 / 24 | same |
| Numbers | Sono, tabular | inherit | same |
| Readout (the Cook Mode timer only) | Sono | 64 / 72 | same |

Controls are 36px tall, and 48px when large. Small pills (badge, label, count) are 24px. A standalone icon is 24,
36 or 48px; an icon beside text takes the text's size.

Sono is a rounded mono (`MONO` axis at 1) that keeps the two hand fonts company. Hand notes are casual
italic, at most one per sheet, and never carry information the sheet needs. No bold, no semibold, no medium:
emphasis is a kick line, a label, or marker, never weight.

## Icons

Font Awesome. Duotone by default: ink over a faint second layer at 35%. Solid or regular where the simpler
glyph reads better. Always `currentColor`. Never coloured, never on a coloured ground of their own except a
tile's wash.

## Edge and handling

Handling is fixed at **Handled**: top sheets tilt within ±1°, and the blank under each turns 1.5° off it, or 0.5°
on a wide sheet. Pristine (square, no blank) and Well-thumbed (fingerprints, smears, corner strips, heavier wax)
exist in the mockup and are not built.

- **Tear**: fine and dense on all four sides, 2.4px amplitude on sheets, 1.8px on tiles, with the odd deeper
  fibre nick. Six sheet tears, two hero tears and three tile tears exist; neighbours never share one, and a
  sheet never lies on its own tear.
- **Cut corner**: one clean 16–20px chamfer through the tear at the top-right, bottom-right or bottom-left.
  Never top-left, where the label lives. At least one of the six standard tears is uncut (the mockup's "about
  one in four"). The hero takes a 28px cut, and the hero's blank a 24px one.
- **Label**: a flat marker pill overlapping the top-left edge. Sono caps. Never torn, never shadowed, no
  grain: the one thing on the sheet that was printed. It sits outside the tear and outside the shadow.
- **Pile**: paper is piled two high. Every sheet lies on a blank sheet torn like it, with the fibre and the
  fall but no ruling and nothing on it, turned 1.5° off the sheet above (0.5° under a wide sheet: a hero, the
  pad, anything wider than 700px). Down a page the blanks turn left and right in turn. The header pad is a top
  sheet on one page, and its menus slide out between the two.
- **Tape**: holds applied items only. A sheet is never taped. A tile pinned to a sheet takes one small
  translucent strip, slightly rotated.
- **Wax**: a light crayon tooth over desk and paper alike (multiply in light, soft-light in dark); washes and
  tiles carry a finer grain of their own.
- **Pencil ruling**: every sheet is ruled at 24px, faint, aligned to its padding. Always on.

## Layout rhythm

Sheet padding 24px (heroes 48px). Grid gaps 36px between rows (a rule and a half), 28px between columns.
Sections 72px apart (three rules), a section heading 36px above its content. Vertical margins between blocks are
whole or half rules.

## Motion

Paper moves like paper. A sheet you can open lifts off its blank when you reach for it and settles when you let go.
A drawer slides up inside its sheet, and a stacked sheet steps back as the next one covers it. The header is a
notepad. A menu is a card in the middle of the stack: it fans out from its far corner when you reach for it,
turning up to 2.4° (past a resting sheet's degree, and only while you reach), and slides out from between the pad's
two sheets when you open it. The pad tucks away as you read and returns when you scroll up. Most motion takes
300ms, and a bigger move that would feel rushed at 300ms takes up to 500ms. Only a loading spinner runs longer.
Reduced motion stills all of it.

## Applied patterns

- **Recipe slip**: a plain sheet on its blank; a small torn wax tile pinned over the top-left corner with its own
  tape; the hero stat (rating · reviews, or time) top-right in Sono; the title in APCasual 20; a Sono meta line;
  hairline badges. The tile's wash is the recipe's colour.
- **Library**: a green-washed sheet holding search, sort and view controls; below it a grid of slips cycling
  through the six tears.
- **Detail**: a hero slip with a 28px cut, the recipe's wash pooled in the bottom-right corner (the kick lines
  run along the left, and soft ink never sits on a core), a large pinned tile; then an
  "At a glance" stats sheet; then ingredients (checklist, teal wash) beside method (numbered steps, peach
  wash). One hand note at most.
- **Lists**: checklists on the rule: 12px box, quantity in Sono right-aligned, done rows struck through.

## Don't

- Don't stroke anything: no `border`, no outline on paper, no drawn frames. Inset hairlines on printed
  controls are the one exception.
- Don't shadow anything except the sheet's fall. No `shadow-*` utilities; no shadow on labels or tape. (Tiles
  are torn paper and carry the fall like any sheet.)
- Don't round paper. No radius on sheets, tiles or images.
- Don't put text on a wash core. Washes go under margins and corners.
- Don't colour icons, links or status text.
- Don't use bold.
- Don't tear a control. Only paper is torn.
- Don't let two neighbouring sheets share a tear.
- Don't give a sheet more than one wash, one label or one hand note.
- Don't tape a sheet, and don't leave one without its blank.

## Decisions and history

- **2026-09-17**: Loose Leaf (Soft) replaces Softbound Sketch, whose drawn outlines and hatched shadows are
  retired. Light is the default ramp. The handling and ruling dials from the mockup are not built. The class
  prefix is `kcc-`. Tears are pure-CSS polygon presets; a JS `path()` tear is the fallback if fidelity needs it.
- **2026-10-16**: Paper piles, not tape. Every sheet lies on a blank sheet, and tape holds only applied tiles. The
  header pad drops to a top sheet on one page.
- **2026-10-19**: The type scale moves from a 15px body to one scale of even steps, 16/24 on the same 24px rule. Headings are
  named by role: `kcc-display`, `kcc-section`, `kcc-heading` and `kcc-title`. Sono caps set at 14px, and vertical
  spacing counts in rule units.
