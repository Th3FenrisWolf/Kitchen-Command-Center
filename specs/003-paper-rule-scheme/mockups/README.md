# Workbench look: approved preview

Six look rounds on 2026-10-19 settled FR-010, FR-011, FR-012 and the place of controls on the ruling.
`workbench-look.html` is the approved round. `workbench-look-light.jpg` and `workbench-look-dark.jpg` show it in
both ramps. The round loads the site's built `kit.css` from the preview server, so open it through
`/speckit-preview-run`.

## Approved

**Reading sheets, written on the line, margin red.**

- **Ruling**: feint blue 1px rules, `hsl(205 28% 62% / 0.55)` in light and `oklch(0.74 0.05 240 / 0.24)` in dark. The
  top of each rule lies 2px below the body baseline: `background-position: 0 calc(var(--rule-drop) + 2px)`, with
  `background-origin: content-box`.
- **Margin rule**: red, `hsl(356 70% 60% / 0.75)` in light and `oklch(0.66 0.15 22 / 0.58)` in dark. Two rules in
  from the sheet's left edge (48px), with the copy one rule after it (72px). It appears only on the sheets you read
  down: the recipe detail, forms and cook mode. Heroes, the search toolbar, recipe cards and other narrow sheets have
  none.
- **Grain**: the workbench grain at 6% (multiply) in light and 8% (soft-light) in dark, per sheet. It adds to the
  crayon wax.
- **Metrics**: Hazelnut `k = 0.38208`, APCasual `k = 0.25`, Sono `k = 0.312` (hhea 1824 / −576 at 2000 upm:
  `ascent-override: 91.2%`, `descent-override: 28.8%`).
- **Shim**: each text role sets `padding-top: mod(var(--rule-drop) − (line-height / 2 + k · 1em), var(--bl))` and
  the same value back as a negative `margin-bottom`. 105 baselines measured at 425px and 1280px all sat on the
  rule within 0.75px.
- **Controls**: every pill and circle is filled with paper, so no rule runs through it. A segment control or a
  button centres on the caps of the Sono line it shares (`top: --rule-drop − 0.31 · caps − 18px`), so its own text
  sits on the rule beside its label. Icon circles in a row are 32px, centred the same way. A row of 36px controls
  wraps with a half-rule gap and ends with a half rule, so the text below is back on the rule.
- **Fields are written on the line**: no pill. The field's text sits on the rule, its underline is that rule drawn
  in `hair-strong` (2px `ink` on focus), and a hand-drawn APCasual "X" in the margin red starts every field,
  search included. The X replaces the field's leading icon. The label stays above the line.
- **Centred labels** ("Sort", "Makes", "To taste", the photo count, "Add Variant") sit on the rule, as FR-017 rules.

## Not chosen

| Round | Option | Why not |
| --- | --- | --- |
| 1 | Every wide sheet | Margin rule on every sheet ≥ 480px, grain replacing the wax, blueprint dark: the owner chose reading sheets. |
| 1 | The recipe only | Margin rule on the recipe and cook mode only: the owner chose reading sheets. |
| 2 | Controls lifted 3.9px, 36px circles | Fields still looked off centre, and the circles too big. |
| 3 | Text on the rule (pill fields) | The owner preferred fields written on the line. |
| 3 | Centred in the band | Moved row labels off the rule. |
| 4 | Caption under the line | The owner kept the label above. |
| 5 | Red pen | A full-strength X: the owner chose the margin rule's red, then asked for both reds slightly redder. |

## Still open

- The X's contrast: it is decorative (`aria-hidden`) and the field keeps its accessible name, so the contrast table
  does not cover it. The plan confirms the field's underline and placeholder pass.
- Where the kit's field icon goes on fields that have one today, other than search: the approved round drops it
  for the X everywhere.
