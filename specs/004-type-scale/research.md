# Research: Type Scale

All decisions here rest on the approved look in `mockups/README.md`, the clarifications in `spec.md`, a font-size
inventory of `src/KCC.Web/Features` taken on 2026-10-19, and a Tailwind 4.3.2 spike against the repository's own
`@tailwindcss/node`.

## R1. Where the scale lives

- **Decision**: Tailwind 4 `--text-*` tokens in `@theme static` in `Styles/TailwindConfig.css`, in rem, each with its
  `--text-<role>--line-height`. The block opens with `--text-*: initial;`, which removes Tailwind's default size scale
  (`text-xs` … `text-9xl`).
- **Rationale**: one definition feeds the kit CSS (`var(--text-body)`), the component style blocks and the Razor views
  (`text-body`). The spike showed `text-body` emits both `font-size` and `line-height` from the token, and that
  `--text-*: initial` stops `text-5xl` from generating, so a default size cannot creep back in through a utility. `static`
  keeps every token alive for the kit CSS, as the other role tokens already do.
- **Alternatives**: kit-only custom properties in `Torn/Tokens.css` (no Tailwind utility, so templates would need
  arbitrary values); a `--spacing` override (rescales every width and padding utility, not only text).

## R2. The heading roles carry the phone step

- **Decision**: `kcc-display`, `kcc-section`, `kcc-heading` and `kcc-title` are kit classes in `Torn/Kit.css`, written
  mobile first. Below 768px `kcc-display` takes a phone-only `--text-display-phone` (36 / 48), and every other role
  keeps its one size, so no two heading roles share a size at any width. From 768px the display takes its own size. 768px is the breakpoint the header pad already switches at (`AppHeader.Component.vue`, `PadCard.vue`).
- **Rationale**: a Tailwind `text-*` utility has no breakpoint built in, and Razor and Vue both render these headings,
  so the rule belongs in `@layer components`. Bare `h1`–`h6` in `Typography.css` map to the same roles, so editor rich
  text still gets a sane size: `h1` display, `h2` section, `h3` heading, `h4`–`h6` title.
- **Alternatives**: `md:text-display` on every heading (repeats the breakpoint at about 30 sites and lets it drift).

## R3. Rule units for vertical spacing

- **Decision**: functional utilities in `TailwindConfig.css`, each `calc(var(--bl) * --value(number))`: `mt-rule-*`,
  `mb-rule-*`, `my-rule-*`, `pt-rule-*`, `pb-rule-*`, `py-rule-*`, `gap-y-rule-*`, `space-y-rule-*`, `h-rule-*`,
  `min-h-rule-*` and `top-rule-*`. Halves are written `mt-rule-0.5` and `gap-y-rule-1.5`.
- **Rationale**: the spike emitted `.mt-rule-1\.5 { margin-top: calc(var(--bl) * 1.5) }`, so one family covers whole
  and half rules and follows `--bl` if the pitch moves again. Tailwind's default spacing stays for horizontal space and
  for sizes unrelated to the rule.
- **Conversion map** (the fixed 24px spacing → rule units; on the 24px rule each unit is the same distance again): `mt-6` → `mt-rule-1`, `mt-12` → `mt-rule-2`, `gap-y-6` →
  `gap-y-rule-1`, `gap-y-9` → `gap-y-rule-1.5`, `space-y-6/9/12` → `space-y-rule-1/1.5/2`, `mb-6` → `mb-rule-1`,
  `py-6` → `py-rule-1`, `h-6` → `h-rule-1`, `gap-y-[72px]` and `space-y-[72px]` → `*-rule-3`, `gap-y-3` → `gap-y-rule-0.5`.
  A two-axis `gap-6` or `gap-9` on a grid or a wrapping row splits into `gap-x-6 gap-y-rule-1` (or `-1.5`); on a single
  row it stays, because it is horizontal space. `gap-3` and `gap-2` stay: they space items in a row.
- **Alternatives**: `--spacing-rule: var(--bl)` as a named spacing value (gives `mt-rule` only, no multiples); arbitrary
  `mt-[calc(var(--bl)*2)]` at every site (unreadable and unguardable).

## R4. Sheet padding stays

- **Decision**: `--pad` keeps its values (24px, 48px, 16px and the `clamp(24px, …, 48px)` forms).
- **Rationale**: the ruling's offset is computed from `--pad` (`Kit.css` ruling `background-position`), so the first
  text line and the ruling move together whatever the padding is. Padding sets the paper margin, a horizontal look the
  approved round kept. Leaving it avoids changing five test pins for no visible gain.

## R5. The ruling offset

- **Decision**: the ruling's offset is derived from the pitch, the body size and Hazelnut's `k` (0.38208), as a token:
  `--rule-drop: calc(var(--bl) / 2 + 0.38208 * var(--text-body))`, and the ruling sits at
  `calc(var(--pad) + var(--rule-drop) - var(--bl) + 1px)`, so the top of its 1px line sits on the baseline.
- **Rationale**: for today's 15 / 24 the formula gives a rule 17.7px into the line, matching the hand-tuned `- 5px`
  (24 − 5 − 1 = 18). For 18 / 28 it gave 20.9px, and for the approved 16 / 24 it gives 18.1px, so one formula follows every rescale.
  Spec 003 owns the general shim for other faces and sizes; this keeps body text on the rule until it lands.
- **Alternatives**: a new hand-tuned constant (breaks again at the next size change).

## R6. Controls keep their heights

- **Decision**: `kcc-btn`, `kcc-field` and `kcc-seg` stay 36px, `kcc-btn--lg` 48px, as FR-018 and the approved look say.
  Their label text becomes 14px Sono caps (`--text-caps`) and the field text 16px. The small pills, the badge, the
  marker label, the pad's count and its key hint, share one 24px line. The heights become tokens in `@theme static`:
  `--control-md` 2.25rem, `--control-lg` 3rem and `--pill-sm` 1.5rem.
- **Rationale**: a 36px control is one and a half 24px rules, and a 48px control two. Spec 003's shim covers blocks of another
  height. The approved round showed the controls at these heights in both ramps.

## R7. Icons

- **Decision**: an icon beside text inherits the text's size (`font-size: inherit`, the kit default). A standalone icon
  takes one of three named sizes, `--icon-sm` 1.5rem, `--icon-md` 2.25rem and `--icon-lg` 3rem, through the kit classes
  `kcc-icon-sm`, `kcc-icon-md` and `kcc-icon-lg`.
- **Mapping**: empty-state glyphs (`text-5xl`) → `kcc-icon-lg`; the add-variant glyph (`text-4xl`) and the large tile
  glyph (38px) → `kcc-icon-md`; the Cook Mode stopwatch (`text-2xl`), the tile glyph (22px) and the pad glyph (17px) →
  `kcc-icon-sm`. Label, button, field, stat, checklist and pad-line icons (9px to 15px today) inherit their text.
- **Rationale**: FR-008 and SC-002 count icon sizes apart from text sizes. Three sizes cover every standalone glyph in
  the inventory.

## R8. The guard test

- **Decision**: a new Vitest suite, `tests/KCC.ViteTests/Features/Styles/typeScale.test.ts`, reads every `.css`, `.vue`,
  `.cshtml` and `.ts` file under `src/KCC.Web/Features` except the generated `Tears.css`, and fails, naming the file,
  the line and the value, on:
  1. a `font-size` that is not `var(--text-*)`, `var(--icon-*)`, `inherit` or `1em`;
  2. a `font` shorthand whose size is not a `var(--text-*)`;
  3. a `line-height` in px or a bare number, other than `var(--bl)`, a `var(--text-*--line-height)`, a
     `var(--control-*)` or `var(--pill-sm)`, `1` or a `calc()` of `var(--bl)`;
  4. a Tailwind text-size utility that is not a scale token: `text-[<length>]`, any `leading-*`, or a default size name
     (`text-xs` … `text-9xl`);
  5. a vertical spacing utility on the old rule: `mt|mb|my|pt|pb|py|gap-y|space-y` with `-3`, `-6`, `-9`, `-12`, `-18`
     or an arbitrary px value.
  It also checks that `TailwindConfig.css` defines at most eight `--text-*` sizes besides `--text-readout` (SC-002).
- **Rationale**: SC-004 asks for a test that fails on a raw size and passes on the converted code. Rule 5 keeps the
  conversion from drifting back. The suite reads files the way `retiredTokens.test.ts` does.
- **Alternatives**: a stylelint rule (does not see Tailwind classes in templates or Razor).

## R9. Heading class mapping

- **Decision**: `kcc-h3` → `kcc-display` everywhere it titles a page or a hero, and `kcc-section` in Cook Mode's step
  text (`CookModeStep.vue`). `kcc-h4` → `kcc-heading` where it titles a whole sheet (wizard step titles in `StartStep`,
  `MatchStep` and `ReviewStep`, the account sheet heading in `AccountView`), and `kcc-title` where it titles a card, a
  list row, a tile, a pad card line or an empty state. The seven `kcc-secname` headings take `kcc-section`, and
  `.kcc-secname h2`'s size rule goes. `kcc-hand` keeps its name and takes the title size.
- **Rationale**: the owner chose role names in a clean cutover (Clarifications, 2026-10-19). The unit, integration and
  E2E tests that pin the old names change in the same commit (`PileTests.cs`, `HomePageTests.cs`,
  `HomeSectionsPageTests.cs`, about 20 Vitest files).

## R10. Spec 003

- **Finding**: spec 003 still describes its Recipe preset as 20px body on a 32px pitch. FR-014 makes 003 take 16px on a
  24px pitch from this scale. This feature does not edit 003. The owner re-clarifies 003 before it is planned.
