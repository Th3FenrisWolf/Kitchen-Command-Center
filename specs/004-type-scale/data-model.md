# Data Model: Type Scale

This feature stores no data. Its entities are design tokens and the roles that read them.

## Type roles

| Role | Token | Face | Size / line, from 768px | Size / line, below 768px | Kit class or utility |
|---|---|---|---|---|---|
| Display | `--text-display` | APCasual | 40 / 48 (2.5rem / 3rem) | 36 / 48 (`--text-display-phone`) | `kcc-display`; bare `h1` |
| Section heading | `--text-section` | APCasual | 32 / 48 (2rem / 3rem) | same | `kcc-section`; bare `h2` |
| Sheet heading, stat value | `--text-heading` | APCasual (stat value Sono) | 24 / 24 (1.5rem / 1.5rem) | same | `kcc-heading`; bare `h3`; `.kcc-stats .kcc-v` |
| Card title, hand note | `--text-title` | APCasual (hand note italic) | 20 / 24 (1.25rem / 1.5rem) | same | `kcc-title`; bare `h4`–`h6`; `kcc-hand` |
| Body, nav item, field, pad line | `--text-body` | Hazelnut | 16 / 24 (1rem / 1.5rem) | same | `body`, `kcc-body`, `.pad-menu`, `.pad-line`, `input`, `select`, `textarea` |
| Caps | `--text-caps` | Sono caps | 14 / 24 (0.875rem / 1.5rem) | same | `kcc-kick`, `kcc-lbl`, `kcc-meta`, `kcc-badge`, `kcc-btn`, `kcc-seg` buttons, `kcc-unit`, `kcc-label`, `kcc-stat`, `.kcc-n`, `.kcc-q`, stats `small`, `.pad-count`, `.pad-key` |
| Readout (counted apart) | `--text-readout` | Sono | 64 / 72 (4rem / 4.5rem) | same | `text-readout` on the Cook Mode timer |

Seven text sizes (14, 16, 20, 24, 32, 36 on phones, 40) plus the readout. Every role keeps its present letter spacing: Sono caps
`0.1em` (`0.14em` on buttons), the others `0`.

Validation: every size is a token (FR-010); no role under 14px, and only Sono under 16px (FR-002); no two heading
roles share a size at any width (FR-004); every line height is a whole number of 24px rules, or a control height
(FR-006).

## Rhythm tokens

| Token | Value | Read by |
|---|---|---|
| `--bl` | 1.5rem (24px) | the ruling, every line height, the rule utilities |
| `--rule-drop` | `calc(var(--bl) / 2 + 0.38208 * var(--text-body))` | the ruling's offset in `Kit.css` |
| `--control-md` | 2.25rem (36px) | `kcc-btn`, `kcc-seg` buttons, `kcc-field` |
| `--control-lg` | 3rem (48px) | `kcc-btn--lg`, the large icon button |
| `--pill-sm` | 1.5rem (24px) | `kcc-badge`, `kcc-label`, `.pad-count`, `.pad-key` |

## Icon sizes (counted apart)

| Token | Value | Class | For |
|---|---|---|---|
| `--icon-sm` | 1.5rem | `kcc-icon-sm` | the Cook Mode stopwatch, the tile glyph, the pad glyph |
| `--icon-md` | 2.25rem | `kcc-icon-md` | the add-variant glyph, the large tile glyph |
| `--icon-lg` | 3rem | `kcc-icon-lg` | empty-state glyphs |

An icon beside text has no size of its own: it inherits the text's.

## Rule utilities

`<prefix>-rule-<n>` = `calc(var(--bl) * n)`, `n` a whole or half number. Prefixes: `mt`, `mb`, `my`, `pt`, `pb`, `py`,
`gap-y`, `space-y`, `h`, `min-h`, `top`. The conversion from fixed spacing is in `research.md` R3.

## Retired

| Retired | Replaced by |
|---|---|
| `kcc-h3` | `kcc-display`, or `kcc-section` for Cook Mode's step text |
| `kcc-h4` | `kcc-heading` on a sheet's title, `kcc-title` on a card's, a row's or an empty state's |
| `.kcc-secname h2` size rule | `kcc-section` on the heading inside `kcc-secname` |
| Tailwind's default `text-xs` … `text-9xl` | removed by `--text-*: initial` |
| `text-[15px]`, `text-[22px]`, `text-[64px] leading-[72px]`, `leading-6`, `text-2xl/4xl/5xl` | `text-body`, `text-title`, `text-readout`, the rule utilities, the icon classes |

`kcc-h3` and `kcc-h4` join the retired list in `retiredTokens.test.ts`, so neither comes back.
