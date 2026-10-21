# Research: Paper Rule Scheme

All decisions here rest on the approved look in `mockups/README.md`, the clarifications in `spec.md`, the workbench's
`paper.css` (version `1789562980-d004`), a code survey taken on 2026-10-19, and baselines measured in the six preview
rounds.

## R1. Pinned metrics and the baseline constants

- **Decision**: every `@font-face` in `Styles/Typography.css` gets `ascent-override`, `descent-override` and
  `line-gap-override: 0%` from its hhea table. Hazelnut 101.0742% / 24.6582% (all four faces), APCasual 75% / 25%
  (both faces), Sono 91.2% / 28.8% (both unicode-range subsets). `Torn/Tokens.css` holds one constant per family:
  `--k-hazelnut: 0.38208`, `--k-casual: 0.25`, `--k-sono: 0.312`, and `--rule-drop` reads `--k-hazelnut` in place of
  its literal.
- **Rationale**: `k = (ascent − descent) / 2` places a baseline at `line-height / 2 + k · font-size` at any line height,
  but only when every platform reads the same ascent and descent. APCasual's OS/2 table (716 / 134) disagrees with its
  hhea (750 / 250), so Windows drifts it by about 0.04em without the override (SC-002). Sono's hhea is 1824 / −576 at
  2000 units; measured in Chrome, the pinned faces give k = 0.380, 0.250 and 0.310 at 100px, within rounding.
- **Alternatives**: measuring k per platform at runtime (needs script, which the kit forbids); trusting the OS/2 table
  (moves every APCasual heading on Windows).

## R2. The ruling

- **Decision**: `.kcc-sheet` draws `linear-gradient(to bottom, var(--color-rule) 1px, transparent 0)` at
  `background-size: 100% var(--bl)`, `background-origin: content-box`, `background-clip: border-box`, positioned at
  `0 calc(var(--rule-drop) + var(--rule-offset))`, with `--rule-offset: 2px` in `Tokens.css`. The `--pad` term leaves
  the ruling. `kcc-sheet--blank` sets `background-image: none`.
- **Rationale**: anchoring to the content box puts the ruling where the first line of text is, whatever the padding,
  so an asymmetric padding or a utility padding cannot move it (spec edge case). The top of each rule then lies 2px
  below the body baseline (FR-003), measured in round 1.
- **Alternatives**: keep the `--pad` term (a padding set by a utility, not `--pad`, moves the text but not the rule).

## R3. The shim moves text with `translate`, not padding

- **Decision**: one kit rule in `Torn/Kit.css` gives every text role on a sheet a signed shift:
  `translate: 0 calc(mod(calc(var(--rule-drop) - var(--own) + var(--bl) / 2), var(--bl)) - var(--bl) / 2)`, where
  `--own: calc(var(--lh) / 2 + var(--k) * 1em)`. The selector is `.kcc-sheet :where(h1, h2, h3, h4, h5, h6,
  .kcc-display, .kcc-section, .kcc-heading, .kcc-title, .kcc-hand, .kcc-kick, .kcc-lbl, .kcc-meta, .kcc-unit, .kcc-num,
  .text-caps, .text-title, .text-heading, .text-section, .text-readout)`. `--lh` comes from the role or the token
  utility (`--text-<role>--line-height`), and `--k` from the face: the APCasual roles and `font-casual` set
  `--k-casual`, the Sono roles, `kcc-num` and `font-mono` set `--k-sono`, and the sheet sets `--k-hazelnut` as the
  inherited default.
- **Rationale**: the workbench spends the shim as `padding-top` and takes it back as a negative `margin-bottom`. Here
  the rule utilities of spec 004 (`mb-rule-1`, `space-y-rule-*`) sit in the `utilities` layer and beat any margin the
  kit sets, so the negative margin would be lost and the sheet would drift. `translate` changes no layout, no margin
  collapses, and a signed shift in (−12px, 12px] reaches the nearest rule both ways, which the centred labels need
  (the Sono "4" in the stepper needed −0.1px, where the padding form gave +23.9px). Measured shifts: display +8.1,
  section +10.1, heading +0.1, title and hand +1.1, caps +1.7, readout −9.9. Body text needs none: the ruling is placed
  from its own baseline.
- **Alternatives**: `position: relative; top` (an absolutely positioned role, such as a corner stat, would pick up
  the offset); the padding form (above).
- **Inline text**: `translate` does not apply to a non-replaced inline box, so a `kcc-num` or a `text-caps` span inside
  a line of body text stays on that line's baseline. Only a block, a flex item or a grid item moves.
- **Constraint**: the shift assumes the element's line box starts on a line of the sheet. A label centred by
  `items-center` in a taller row does not, so rows that hold controls use `kcc-row` (R6).

## R4. The workbench look in both ramps

- **Decision**: `--color-rule` becomes feint blue, `oklch(0.698 0.048 239.5 / 0.55)` in light (the workbench's
  `hsl(205 28% 62% / 0.55)`) and `oklch(0.74 0.05 240 / 0.24)` in dark. A new `--color-margin`:
  `oklch(0.631 0.178 20 / 0.75)` in light (`hsl(356 70% 60% / 0.75)`) and `oklch(0.66 0.15 22 / 0.58)` in dark. Both are
  role tokens in `@theme static` with the dark values in `Tokens.css`.
- **Rationale**: the approved round, written in the oklch form every other token uses.
- **Contrast**: text is checked over paper, as today. A rule pixel lies 2px under the baseline, in the descender band,
  so it is not the text's ground. Measured for the record: ink-soft over a rule pixel is 4.60:1 in light and 4.16:1 in
  dark. The X is decorative and `aria-hidden`: 2.5:1 against paper in both ramps. `contrast.test.ts` gains a UI pair
  for the field underline (`hair-strong`, unchanged) and none for the rule or the X.
- **Grain**: excluded from the contrast table by owner ruling, 2026-10-20. The sheet grain is texture at 6% (light,
  `multiply`, which only darkens the paper under ink) and 8% (dark, `soft-light`), and the owner walked both ramps.
- **Windows**: the pinned metrics make SC-002 hold by construction. Accepted without a Windows measurement by owner
  ruling, 2026-10-20.

## R5. The margin rule

- **Decision**: a modifier `kcc-sheet--margin` draws the rule from `.kcc-sheet::before` at `calc(var(--bl) * 2)` from
  the sheet's left edge and sets `padding-inline-start: calc(var(--bl) * 3)`. The slip that holds a margin sheet is an
  inline-size container (`.kcc-slip:has(.kcc-sheet--margin)`), and `@container (width < 480px)` removes both the line
  and the extra padding. `KccSheet.vue` takes a `margin` prop, and Razor writes the class by hand.
- **Rationale**: a container query on the slip measures the sheet itself, so a form in a narrow column drops the rule
  as a phone does. Limiting `container-type` to margin slips leaves every shrink-to-fit sheet alone, because inline-size
  containment stops a box from sizing to its content.
- **Reach** (FR-011): the recipe page's At a glance sheet; the variant page's At a glance, ingredients, instructions, nutrition,
  cook notes and reviews sheets (the reviews summary and form; each single review in the list is a card and carries none, by owner ruling 2026-10-20); every wizard step and wizard sheet (`StartStep`, `BasicsStep`, `IngredientsStep`,
  `InstructionsStep`, `PhotoStep`, `ReviewStep`, `MatchStep`, `VariantStep`); the
  account, login and settings sheets and `RegistrationComplete`; cook mode. `ReadingState` and `SubmittedSheet` carry it
  like every other wizard sheet, their centred content shifting with the copy inset. Not `LeaveConfirm` or
  `PhotoViewer`, which are dialogs, and not heroes, toolbars, cards, the header pad, home blocks, the stacker or empty
  states.

## R6. Controls on the ruling

- **Decision**: inside a sheet, `kcc-btn` and `kcc-seg` shift by
  `calc(mod(calc(var(--rule-drop) - var(--k-sono) * var(--centre-size) - var(--h) / 2 + var(--bl) / 2), var(--bl)) - var(--bl) / 2)`
  with `--h` their height and `--centre-size` the size of the Sono line they centre on (`--text-caps` by default; cook
  mode's servings buttons take `--text-title`, the size of their count), so each centres on the caps of the line it
  starts on, and its own Sono text lands on the rule. A ghost button, a segmented control, a field and a badge paint `var(--color-paper)`, so no rule runs through them. A new
  kit class `kcc-row` (flex, `align-items: flex-start`, a 12px column gap, a half-rule row gap and a half-rule
  `padding-bottom`) holds a row of 36px controls and their labels, so every wrapped line fills two rules and the text
  below is back on the rule.
- **Rationale**: round 3 measured every label and every control's text on the rule. `items-center` rows put labels a
  few px off, which the shim cannot see (R3).
- **Sites** (FR-017): `RecipeResultsToolbar.vue:27` ("Sort"; the variant toolbar's "Sort" sits on the desk, not a
  sheet, so it keeps `items-center`), `VariantIngredients.vue:58`
  ("Makes"), `PhotoStep.vue:108` (the photo count), `IngredientsStep.vue:79` ("To taste"), `VariantGrid.vue:33-35`
  (the Add Variant tile).

- **Heading rows** (owner, 2026-10-19): an action beside a display title sits one rule down, so its text shares the
  title's baseline rule.
- **Header pad** (owner, 2026-10-19): the pad's sheets stay ruled, and the text in its bar and its card footers sits
  on their rules like any other sheet.
- **Badges**: 24px (`--pill-sm`), their caps on the rule.
- **Camera preview**: its rounded box fills with `paper-2`, because the kit keeps `marker-ink` for text.

## R7. Fields are written on the line

- **Decision**: `.kcc-field` drops its radius, its inset ring and its fill, aligns its children to the start, and
  draws its underline as a 1px `hair-strong` background line at `calc(var(--rule-drop) + var(--rule-offset))`, 2px
  `ink` on `:focus-within`. Its input and select take `line-height: var(--bl)`, so their text sits on the rule, and fill
  the field's hit area: `height: var(--control-md)`, `padding-bottom: calc(var(--control-md) - var(--bl))` and no other
  block padding, which keeps their one line on the rule and lets the whole 36px take a click. The X is
  `.kcc-field::before`: `content: 'X'`, APCasual at `--text-title`, `color: var(--color-margin)`, `rotate: -6deg`, and the
  title role's shim. The field keeps `height: var(--control-md)` as its hit area. `kcc-field--error` paints the
  underline 2px in `ink`, and `kcc-field--area` in error draws a 2px `ink` outline: red reads under 3:1 on paper, so
  the `kcc-well--danger` message below the field carries the red. `InputField`, `TextAreaField` and `SelectField`
  take an `error` prop, which a caller inside `Field` binds from the slot's `error`, and add the class and
  `aria-invalid` from it. A caller marks only the fields the message is about: `LoginView` marks the name and the
  password when the sign-in answers with the `invalid-credentials` code, and the two passwords when they do not
  match; an account waiting for approval or locked out, and a sign-up error from the server, show the well alone.
- **Textarea**: `kcc-field--area` loses its box and padding, its textarea is transparent with `line-height: var(--bl)`,
  and the X sits before its first line. Focus draws a 2px `ink` outline with no radius. `TextAreaField`'s drag handle
  snaps the height to whole rules.
- **Icons**: the leading `<i>` goes from `RecipeSearchHeader.vue:43`, `Header/SearchForm.vue:19`,
  `VariantToolbar.vue:41` and `MatchStep.vue:67`. `InputField` loses its `icon` prop (no caller passes it), and
  `kcc-field--noicon` retires: with the X as the one leading mark, every field has the same shape. The X is CSS
  content, decorative, and never read out. The field's accessible name stays its label.
- **Rationale**: one rule for every field (Clarifications, 2026-10-19), and Razor and Vue get it from the kit with no
  markup change beyond the icons.
- **Alternatives**: an `<span>` X in the markup (repeats in every view and every Razor form).

- **Error**: the error line is 2px `ink`, because `--color-red` is a wash at 1.77:1 on light paper; the
  `kcc-well--danger` message below the field carries the red.
- **Known limits**: a textarea scrolled by pixels shows its lines between the rules until it scrolls back to a whole
  line. Where `content: 'X' / ''` is unsupported (Safari before 17.4, Firefox before 128), the fallback X is read with
  the field's label.

## R8. Icon buttons are 32px

- **Decision**: a token `--control-sm: 2rem` in `@theme static`, and `.kcc-btn--icon` takes it as width and height,
  also when it carries `kcc-btn--lg` (the camera shutter, the photo viewer's close, cook mode and the timer), so the
  `--lg` icon rule retires.
- **Rationale**: the owner chose every icon button (Clarifications). 32px is above WCAG 2.5.8's 24px minimum target.
  `typeScale.test.ts` forbids a raw size, so the size is a token.

## R9. The workbench grain

- **Decision**: `.kcc-sheet::after` lays the workbench's noise tile (`baseFrequency 0.8`, 4 octaves, stitched) over the
  ground at `--sheet-grain-op: 0.06` with `multiply` in light and `0.08` with `soft-light` in dark, `z-index: 0`, under
  the lifted content. The page's `.kcc-grain` and `.kcc-crayon` stay.
- **Rationale**: FR-012 (adds to the wax). The sheet's children already sit at `z-index: 1`.

## R10. Tests

- **Decision**: a new `paperRule.test.ts` in `tests/KCC.ViteTests/Features/Styles/` reads the stylesheets and pins the
  overrides, the constants, the ruling, the shim rule and its role list, the margin rule and its container query, the
  `kcc-row` class, the field-as-line rules, the 32px icon token and the paper fills. It fails on today's code. The
  component tests that assert `kcc-field--noicon` or a field icon (`InputField.test.ts`, `TextAreaField.test.ts`,
  `VariantReviews.test.ts`, `VariantCookNotes.test.ts`) change first. A Playwright test,
  `KCC.E2ETests/Features/Chrome/RuleTests.cs`, measures real baselines on the recipe, variant, search and wizard
  pages at 375px and 1280px against the ruling (SC-001, SC-003, SC-008). `kitClasses.test.ts` sees `kcc-row`,
  `kcc-sheet--margin` and `kcc-sheet--blank` in kit.md's table.
- **Rationale**: CSS behaviour shows only in a browser, and the E2E suite already boots the site and locates sheets.

## R11. Docs

- **Decision**: `docs/brand/kit.md` gains a "Paper rule" section (metrics, the shim, the offset, the rulings, the
  margin rule's reach, `kcc-row`, controls and fields on the line) and updates Tokens, Classes, States and Vue
  primitives. `docs/brand/loose-leaf.md` rewrites "Pencil ruling", the stock colour rows, the field and control lines
  and adds a decisions-log entry. The `loose-leaf` skill and the `brand-steward` and `kit-builder` agents follow.
