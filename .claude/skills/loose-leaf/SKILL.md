---
name: loose-leaf
description: Use when styling, restyling, reviewing or building any Kitchen Command Center public-site surface - a sheet, card, button, form, widget, page or stylesheet - or when a task mentions the brand, the kit, kcc- classes, washes, tears, the desk, the ramps, or Loose Leaf. Quick reference plus pointers to the full identity and kit contract.
---

# Loose Leaf

The KCC public site is torn paper on a desk: careless sheets, exact drawing, the tear as the frame. The
full identity is `docs/brand/loose-leaf.md`; the engineering contract is `docs/brand/kit.md`. Read
the contract before writing markup or CSS. This file is the part you should be able to hold in your head.
Where a general design skill (impeccable, taste-skill, ui-ux-pro-max, frontend-design) disagrees, this skill and
the brand docs win: kickers, Sono caps labels, the stats row, the tilt and the tear stay.

## Three rules

1. **The paper is careless.** Sheets are torn on four sides, tilt ±1°, lie on a blank sheet turned 1.5° off them
   (0.5° when wide), and carry one off-centre wax wash. Sheets are never taped; tape holds only a pinned tile. No
   two neighbours share a tear, and neighbouring blanks turn opposite ways.
2. **The drawing is exact.** A 24px feint blue ruling governs everything. Type, controls and fields sit on it, numbers are Sono tabular, icons
   are duotone in `currentColor`, one weight of everything.
3. **The tear is the frame.** Nothing is stroked or shadowed except the sheet's fibre and fall. Only printed
   marks (pills, labels, checkboxes) have radii.

## Torn vs printed

| Torn: `kcc-sheet`, `kcc-blank`, `kcc-tile` | Printed: `kcc-btn`, `kcc-seg`, `kcc-badge`, `kcc-box`, `kcc-label`, `kcc-tape` | Written: `kcc-field` |
|---|---|---|
| clipped, fibre + fall, may tilt, no radius | clean, unclipped, never shadowed, pill / 6px / 3px | a line on the rule with an X before it; no box, no radius |

## The structure

```html
<div class="kcc-slip kcc-tear-3">            <!-- tilt -->
  <div class="kcc-torn">                     <!-- shadow filter: outside the clip -->
    <div class="kcc-sheet" style="--pad: 24px">
      <span class="kcc-wash" style="--c: var(--color-peach); --x: 88%; --y: 18%; --w: 38%; --h: 60%" aria-hidden="true"></span>
      …content (auto-lifted above the wash)…
    </div>
  </div>
  <span class="kcc-label">Label</span>       <!-- outside clip and shadow -->
  <span class="kcc-blank" aria-hidden="true"><span></span></span>   <!-- always the last child -->
</div>
```

Vue: `import KccSheet from '~/Components/Sheet/KccSheet.vue'` with `label`, `icon`, `labelRight`, `wash`,
`at`, `tear`, `piled` (default `true`; only `PadCard.vue` passes `false`), `wide` (any sheet wider than 700px;
`tear="hero"` is wide already), `flip` (turns the blank the other way where the page's reading order needs it),
`pad`, `crisp`, `margin` (the margin rule), `blank` (no ruling), `scroll`, `as`. Razor: write it by hand, blank included. Tear in a loop: `kcc-tear-${(i % 6) + 1}`; the
blanks then alternate by themselves.

## Tokens you will reach for

`bg-desk` `bg-desk-2` `bg-paper` `bg-paper-2` · `text-ink` `text-ink-soft` · `border-hair` `border-hair-strong`
· `bg-marker` + `text-marker-ink` (accent, labels, primary) · washes `peach yellow green teal sky lavender
pink red` as fills only. Light is the default ramp; dark overrides live in `Features/Styles/Torn/Tokens.css`.

## Type on the rule

Roles: `kcc-display` APCasual 40/48 (36/48 below 768px) · `kcc-section` 32/48 · `kcc-heading`
24/24 · `kcc-title` 20/24 · `kcc-body` Hazelnut 16/24 · `kcc-kick` Sono caps 14/24 · `kcc-num` Sono tabular ·
`kcc-hand` italic 20/24, one per sheet. Bare `h1`, `h2`, `h3` and `h4`-`h6` take display, section, heading and
title. Only the Cook Mode timer takes `text-readout` (64/72). Standalone icons take `kcc-icon-sm|md|lg`
(24/36/48); an icon beside text inherits.

Type sits on the 24px rule. Space blocks vertically in rule units: `mt-rule-1`, `gap-y-rule-1.5`,
`space-y-rule-2`, `mt-rule-0.5`; rows gap 1.5 rules, sections sit 3 apart. No `leading-*`, no `text-[…]`, no
Tailwind default sizes (`text-sm`, `text-xl`: they do not generate), and no `mt|mb|my|pt|pb|py|gap-y|space-y`
with `3`, `6`, `9`, `12` or `18`. `tests/KCC.ViteTests/Features/Styles/typeScale.test.ts` fails on each.

## The paper rule

Every line on a sheet is written on its ruling, by CSS alone (`docs/brand/kit.md` → Paper rule):

- **The ruling** hangs from the sheet's content box, 2px under the body baseline, so any padding moves text and
  rules together. `kcc-sheet--blank` (`KccSheet blank`) drops the rules and keeps the rhythm.
- **The shim** moves every text role onto the nearest rule with a `translate` computed from its line height and its
  face's constant (`--k-hazelnut`, `--k-casual`, `--k-sono`). Never write a pixel nudge: if a line is off, a block's
  height or spacing is not a whole or half number of rules.
- **The margin rule** (`KccSheet margin`, `kcc-sheet--margin`) goes on sheets read down the page: the recipe and
  variant reading sheets, every wizard step and wizard sheet, the account, login and settings sheets,
  `RegistrationComplete`, cook mode. Never on a dialog, a hero, a toolbar, a card, the header pad, a home block, the
  stacker or an empty state. It drops itself below 480px of sheet width.
- **`kcc-row`** holds a row of 36px controls and their labels, top-aligned, so each wrapped line fills two rules.
  Never centre a label beside a control with `items-center`.
- **Controls** on a sheet centre on the caps of their line; a ghost button, a segment, a field and a badge fill
  with paper. An action beside a display title sits one rule down. Icon buttons are 32px (`--control-sm`) at every
  size.
- **Fields are lines**: `InputField`, `TextAreaField`, `SelectField`, `NumberStepper` or `kcc-field` markup. The X in
  `::before` is the one leading mark, so no icon before an input. Focus is a 2px ink underline; `kcc-field--error` is
  a 2px ink underline, and the `kcc-well--danger` below carries the red.

## Before you finish

- [ ] Both ramps asserted in the unit test where markup differs; the visual check happens before merging.
- [ ] No `sk-*`, `v-ink`, `data-ink`, `shadow-*`, `rounded-lg`+, `font-bold/semibold/medium`, `fa-primary-*`,
      literal colours, retired tokens (`ink-on-wash`, `ink-line`, `link`, status inks, the six dropped washes).
- [ ] Filter on `.kcc-torn`, not on the clipped element; label and tape outside both, except a
      `.kcc-tilewrap--flow` and its tape in a sheet's content flow.
- [ ] Every sheet ends its slip with one `.kcc-blank`; no tape on a sheet, only over a tile; neighbouring blanks
      turn opposite ways down the page.
- [ ] Text never on a wash core; `text-marker-ink` only inside tiles, labels, marker buttons.
- [ ] Numbers in `kcc-num`; meta in `kcc-kick` / `kcc-meta`; headings in APCasual from a role class.
- [ ] Sizes from the roles and tokens, vertical spacing in rule units; `typeScale.test.ts` passes.
- [ ] Every line on the rule with no nudge: labels beside controls in a `kcc-row`, fields with no leading icon, the
      margin rule only on a reading sheet; `paperRule.test.ts` passes, and `RuleTests.cs` when the layout moved.
- [ ] Motion at the 300ms default, or up to 500ms for a bigger move; motion started from script checks
      `prefers-reduced-motion`.
- [ ] Every focusable shows a ring. Only buttons have a disabled look, so add one to the kit before disabling
      anything else.
- [ ] Structural hooks (`data-testid`, ids, roles) untouched.
- [ ] `ALLOWLIST` in `tests/KCC.ViteTests/Features/Styles/retiredTokens.test.ts` still empty.
- [ ] Ran the last step of `docs/brand/kit.md` → Building a surface (tests, type-check, format; `yarn build:all`
      before merging).

## Common mistakes

- Putting `filter: drop-shadow` on `.kcc-sheet` — the shadow is clipped away. Use `.kcc-torn`.
- Rounding a sheet or an image — paper has no radius.
- `text-ink` on a wash **fill** (a tile) — that is `text-marker-ink`. `text-ink` on a wash **pool** under a
  sheet margin is correct.
- A `.kcc-label` inside `.kcc-sheet` — it gets torn and shadowed. It is a sibling of `.kcc-torn`.
- A `kcc-tape` strip on a sheet — paper is piled, not taped. Tape sits only in a `kcc-tilewrap`.
- A hover lift from `hover:-translate-y-1` utilities — it lifts the blank too. Use `kcc-slip--lift`.
- Using a green `text-link` — links are ink with a hair-strong underline (`kcc-link`).
- Red text for an error — status is a `kcc-well--danger` with ink text; `kcc-field--error` draws an ink line.
- `items-center` on a label beside a button — the label lands off the rule. Use `kcc-row`.
- A `translate`, `top` or margin of a few px to land text on a line — the shim does that; fix the block's height.
- An icon before a field's input — the X is the field's one leading mark.
- Reaching for `font-bold` for emphasis — use a `kcc-kick`, a label, or marker.
- Hand-editing `Features/Styles/Torn/Tears.css` — it is generated; edit `Features/Torn/tears.ts` and run
  `yarn tears`.

## Agents

`brand-steward` reviews a diff or page against the brand (read-only). `kit-builder` converts one surface,
test-first. Use them from the Agent tool by name.
