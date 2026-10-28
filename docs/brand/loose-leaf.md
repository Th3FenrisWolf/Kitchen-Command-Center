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
2. **The drawing is exact.** Inside the tear a 24px rule governs everything. Every line of type sits on it, and
   so does every control's text, with no nudge by hand. Icons share one style. Every number is set in a rounded mono, tabular, so columns align without tables.
3. **The tear is the frame.** Nothing is stroked. No borders, no radii on paper, no drawn shadows. Depth is a
   hair of fibre along the edge and a soft fall beneath the silhouette. *The edge is the border; nothing is
   drawn around anything.*

## What is torn, what is printed

| Torn (paper) | Printed (marks on the paper) |
|---|---|
| Sheets: every panel, card, hero, form, list, and the blank under each | Buttons, segmented controls, fields, badges, checkboxes |
| Accent tiles pinned to a sheet's corner | Labels, tape, stat numbers, meta lines, step numbers |

Torn things are clipped to a generated tear, carry the fibre and the fall, and may tilt. Printed things are
clean, unclipped, never shadowed, and take the only radii in the system: pills for buttons and segments, 6px for
labels and wells, 4px for small marks, 3px for checkboxes. A field has no radius: it is a line to write on.

## Stock

Ink is purple-slate, never black; it belongs to the desk. Paper is warm and near-white. The desk is a pale
lilac-grey. Marker green is the one accent and the only strong fill: primary action, labels, selected state.

| Role | Light (default) | Dark |
|---|---|---|
| desk / desk-2 | `oklch(.884 .016 288)` / `oklch(.836 .018 288)` | `oklch(.132 .024 288)` / `oklch(.176 .028 288)` |
| paper / paper-2 | `oklch(.958 .009 92)` / `oklch(.972 .008 92)` | `oklch(.258 .030 288)` / `oklch(.222 .028 288)` |
| ink / ink-soft | `oklch(.232 .026 288)` / `oklch(.432 .024 288)`\* | `oklch(.945 .012 92)` / `oklch(.76 .018 288)` |
| hair / hair-strong | ink at 16% / 56%\* | chalk at 18% / 45% |
| rule (feint blue ruling) | `oklch(.698 .048 239.5 / .55)` | `oklch(.74 .05 240 / .24)` |
| margin (red margin rule, field X) | `oklch(.631 .178 20 / .75)` | `oklch(.66 .15 22 / .58)` |
| marker / marker-ink | `oklch(.876 .112 126)` / `oklch(.24 .03 288)` | same |
| fiber / fall | `oklch(1 .006 92)` / `oklch(.25 .03 288 / .28)` | `oklch(.435 .034 288)` / `oklch(.04 .02 288 / .7)` |
| tape | `oklch(.985 .012 92 / .6)` | `oklch(.9 .012 92 / .28)` |

\* Two values are pinned by `contrast.test.ts` rather than by the mockup: `ink-soft` (the mockup's `.452`
fails WCAG AA for small caps on desk-2) and light `hair-strong` (the mockup's 40% alpha composites to 2.4:1
against paper; a control boundary needs 3:1, so it ships at 56%, with headroom for a wash beneath). `hair`
keeps the mockup's lightness for dividers.

Light leads. Dark is the same identity on a dark desk: chalk for ink, washes screened instead of multiplied,
the wax tooth soft-lit instead of multiplied. Every visual change is checked in both.

## Washes

Eight crayons. A sheet starts bare, and washes bloom onto it as the reader stays: one at a time, each at its own
moment, until about five minutes into the visit every sheet wears its share. A card carries one or two, a reading
sheet three, the largest sheets four, never more. The colour a reader has earned carries from page to page within
the visit; a visit that has been away half an hour starts bare again, in a different arrangement.

Each wash is one or two of the eight, as they are and never blended into a third, with its own size, shape and
place, so no two sheets look stamped from one mould and neighbours do not bloom in step. Where a sheet has a colour of
its own (a recipe's, or the one the owner picked for a Home card) its first wash wears it. A Home card left unset
stays bare paper.

Washes lie where they fall: under margins and corners, and under running text. They bleed past the margin, never past
the tear, and overlap two deep at most. Their strength is capped so ink and soft ink stay AA over the strongest overlap, and
control hairlines 3:1, in both ramps. Identical in both ramps; the blend mode changes, the pigment does not. Under
reduced motion a wash fades in where it will stay and does not grow.

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

Controls are 36px tall, and 48px when large. Icon buttons are 32px at every size. Small pills (badge, label,
count) are 24px. A standalone icon is 24, 36 or 48px; an icon beside text takes the text's size.

## On the rule

The ruling is feint blue, 1px, at 24px. Its top lies 2px under the body baseline, and it is measured from the
sheet's content box, so a sheet's padding cannot move it out from under the text. Body text sits on it by its own
metrics. Every other role sits on it by a shift in the kit: display, section and sheet headings, titles, hand notes,
kicks, labels, meta lines, units and numbers. No role is nudged by hand, and a line of badges is one rule tall, so
the text after it stays on the rule.

- **Margin rule**: a red line two rules in from the left edge, with the copy starting one rule after it. It runs
  down the sheets you read down: the recipe and variant main column, the wizard and account forms, the wizard's
  status sheets, and cook mode. Heroes, toolbars, cards, the header pad, home blocks, empty states and dialogs have
  none.
  It is gone below 480px, where the extra padding goes with it.
- **Controls** centre on the caps of the line they start on, so a button's text lands on the rule beside its label,
  and a row of controls and labels ends on a half rule so the text below is back on the rule. On a sheet a control
  or a badge is filled with paper, so no rule runs through it.
- **Fields** are written on the line. No pill, no box, no fill of its own: the text sits on the rule, the underline is that
  rule drawn in hair-strong (2px ink while it is being written in), and a hand-drawn red X in the margin colour
  starts the line, search fields included. A stepper is set with buttons, so it draws no X. The label stays above
  the line. An error draws the line 2px ink, because the red is a wash and reads under 3:1 on paper; the well below
  the field carries the red. A textarea has no box either: it writes on the sheet's own rules, and focus outlines
  it in 2px ink.

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
- **Grain**: on the desk, the washes and the tiles only. The desk keeps its grain and a light crayon tooth (multiply in
  light, soft-light in dark), painted under the paper; washes and tiles carry a finer grain of their own, which is
  what makes a wash read as pigment. Paper, text, inputs and every other control, labels, wells, blank sheets and
  tape are clean. The tape's weave is the tape's own material, not grain.
- **Ruling**: every sheet is ruled in feint blue at 24px, drawn from its content box so the text sits just above
  each line. A blank sheet carries none. Always on. The margin rule is its companion on reading sheets (see On the
  rule).

## Layout rhythm

Sheet padding 24px (heroes 48px). Grid gaps 36px between rows (a rule and a half), 28px between columns.
Sections 72px apart (three rules), a section heading 36px above its content. Vertical margins between blocks are
whole or half rules, and a row of controls ends so the text beneath it sits back on the rule.

## Motion

Paper moves like paper. A sheet you can open lifts off its blank when you reach for it and settles when you let go.
A drawer slides up inside its sheet, and a stacked sheet steps back as the next one covers it. The header is a
notepad. A menu is a card in the middle of the stack: it fans out from its far corner when you reach for it,
turning up to 2.4° (past a resting sheet's degree, and only while you reach), and slides out from between the pad's
two sheets when you open it. The pad tucks away as you read and returns when you scroll up. Most motion takes
300ms, and a bigger move that would feel rushed at 300ms takes up to 500ms. Only a loading spinner and the bloom run longer.
A wash blooms onto its sheet over a minute and a half, beginning at its own moment in the visit. Reduced motion stills
all of it but the washes, which fade in without growing.

## Applied patterns

- **Recipe slip**: a plain sheet on its blank; a small torn wax tile pinned over the top-left corner with its own
  tape; the hero stat (rating · reviews, or time) top-right in Sono; the title in APCasual 20; a Sono meta line;
  hairline badges. The tile's wash is the recipe's colour.
- **Library**: a sheet holding search, sort and view controls, whose first wash is green; below it a grid of slips cycling
  through the six tears.
- **Detail**: a hero slip with a 28px cut, four washes that bloom onto it with the recipe's colour first, a large
  pinned tile; then an "At a glance" stats sheet; then ingredients (checklist, teal first) beside method (numbered
  steps, peach first). One hand note at most.
- **Lists**: checklists on the rule: 12px box, quantity in Sono right-aligned, done rows struck through.

## Don't

- Don't stroke anything: no `border`, no outline on paper, no drawn frames. Inset hairlines on printed
  controls are the one exception.
- Don't shadow anything except the sheet's fall. No `shadow-*` utilities; no shadow on labels or tape. (Tiles
  are torn paper and carry the fall like any sheet.)
- Don't round paper. No radius on sheets, tiles or images.
- Don't colour icons, links or status text.
- Don't use bold.
- Don't tear a control. Only paper is torn.
- Don't let two neighbouring sheets share a tear.
- Don't give a sheet more than one label or one hand note.
- Don't put grain or crayon on paper, text, inputs, labels, wells, blank sheets or tape: only the desk, the washes and
  the tiles carry it.
- Don't place or colour a wash by hand: the bloom places them, in the eight colours as they are.
- Don't tape a sheet, and don't leave one without its blank.
- Don't nudge anything onto the rule by a pixel: no `translate-y-px`, `top-[3px]` or hand-tuned padding. The
  kit's shift puts every text role there, and a row of controls takes `kcc-row`. If a line is off the rule, fix the
  kit.
- Don't put a margin rule on a hero, a toolbar, a card or a dialog. It belongs to sheets you read down.

## Decisions and history

- **2026-09-17**: Loose Leaf (Soft) replaces Softbound Sketch, whose drawn outlines and hatched shadows are
  retired. Light is the default ramp. The handling and ruling dials from the mockup are not built. The class
  prefix is `kcc-`. Tears are pure-CSS polygon presets; a JS `path()` tear is the fallback if fidelity needs it.
- **2026-10-16**: Paper piles, not tape. Every sheet lies on a blank sheet, and tape holds only applied tiles. The
  header pad drops to a top sheet on one page.
- **2026-10-19**: The type scale moves from a 15px body to one scale of even steps, 16/24 on the same 24px rule. Headings are
  named by role: `kcc-display`, `kcc-section`, `kcc-heading` and `kcc-title`. Sono caps set at 14px, and vertical
  spacing counts in rule units.
- **2026-10-19**: Everything sits on the rule. The ruling turns feint blue, drawn from the content box with its top
  2px under the body baseline. A red margin rule runs two rules in on the sheets you read down, and each sheet
  adds a workbench grain to the wax. Every text role sits on the rule by the kit's shift, with no pixel nudges.
  Controls centre on the caps of their line and fill with paper on a sheet; icon buttons are 32px and badges 24px.
  Fields drop the pill for a line to write on, with a red hand-drawn X before it; an error is a 2px ink line with
  the red in the well.
- **2026-10-21**: The ink wash blooms. A sheet starts bare and gains up to four washes over the first five minutes of
  a visit, pseudo-randomly, two deep at most, under running text when they fall there, with strength capped so
  ink holds AA over them. "One wash per sheet", "never under running text" and author-placed washes are retired. Grain
  moves to the desk, the washes and the tiles only: the workbench grain each sheet carried is removed, and the crayon
  tooth lies under the paper with the desk's grain.
- **2026-10-27**: The performance sweep changes the look a little, owner-approved. Dark `--color-ink-soft` goes from
  L .735 to .76, which holds 4.5:1 over the stronger dark washes. `--wash-strength` goes from .55 to .42 in light and
  from .16 to .18 in dark. Sheet tears take 40 points a side and hero tears 80, half as many. Washes carry no filter:
  their bleed and grain are baked into PNG masks that `yarn washes` generates. The bloom runs in 45 steps and a wash
  rests at its bloom's end.
