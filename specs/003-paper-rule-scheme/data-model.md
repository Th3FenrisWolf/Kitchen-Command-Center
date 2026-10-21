# Data Model: Paper Rule Scheme

The feature stores no data. Its model is the set of tokens and kit classes the scheme adds, and how they relate.

## Tokens

| Token | Light | Dark | Home | Read by |
|---|---|---|---|---|
| `--k-hazelnut` | `0.38208` | same | `Torn/Tokens.css` | `--rule-drop`, the shim |
| `--k-casual` | `0.25` | same | `Torn/Tokens.css` | the shim, the field X |
| `--k-sono` | `0.312` | same | `Torn/Tokens.css` | the shim, the control shift |
| `--rule-drop` | `calc(var(--bl) / 2 + var(--k-hazelnut) * var(--text-body))` | same | `Torn/Tokens.css` | ruling, shim, field underline |
| `--rule-offset` | `2px` | same | `Torn/Tokens.css` | ruling, field underline |
| `--color-rule` | `oklch(0.698 0.048 239.5 / 0.55)` | `oklch(0.74 0.05 240 / 0.24)` | `TailwindConfig.css`, `Tokens.css` | ruling |
| `--color-margin` | `oklch(0.631 0.178 20 / 0.75)` | `oklch(0.66 0.15 22 / 0.58)` | `TailwindConfig.css`, `Tokens.css` | margin rule, field X |
| `--control-sm` | `2rem` (32px) | same | `TailwindConfig.css` | `kcc-btn--icon` |
| `--sheet-grain-op` | `0.06` | `0.08` | `Torn/Tokens.css` | `.kcc-sheet::after` |
| `--sheet-grain-blend` | `multiply` | `soft-light` | `Torn/Tokens.css` | `.kcc-sheet::after` |

Unchanged and read: `--bl` (1.5rem), every `--text-<role>` and `--text-<role>--line-height`, `--control-md`,
`--control-lg`.

## Font metrics

| Family | Faces | ascent-override | descent-override | line-gap-override | k |
|---|---|---|---|---|---|
| Hazelnut | 400, 700, italic, bold italic | 101.0742% | 24.6582% | 0% | 0.38208 |
| APCasual | regular, italic | 75% | 25% | 0% | 0.25 |
| Sono | latin, latin-ext | 91.2% | 28.8% | 0% | 0.312 |

## Kit classes

| Class | Kind | Relationship |
|---|---|---|
| `kcc-sheet` | changed | draws the ruling from its content box, the grain in `::after`, and sets `--k` to Hazelnut |
| `kcc-sheet--blank` | new modifier | no ruling; same rhythm |
| `kcc-sheet--margin` | new modifier | margin rule in `::before`, copy one rule after it; its slip becomes an inline-size container |
| `kcc-row` | new | a row of controls and labels; each line fills two rules |
| `kcc-field` | changed | a line to write on, with the X in `::before`; no pill |
| `kcc-field--area` | changed | no box; text on the sheet's rules |
| `kcc-field--error` | changed | 2px ink underline; a 2px ink outline on `--area`; the well below carries the red |
| `kcc-field--noicon` | retired | every field has the same shape |
| `kcc-btn`, `kcc-seg` | changed | centre on the caps of their line inside a sheet; ghost and segment fill with paper |
| `kcc-btn--icon` | changed | 32px |
| `kcc-badge` | changed | one rule tall, its caps on the rule inside a sheet; fills with paper |

## Text roles and their shift

Each role's shift is `mod(--rule-drop − (--lh / 2 + --k · font-size) + --bl / 2, --bl) − --bl / 2`.

| Role | Face, k | Size / line | Shift |
|---|---|---|---|
| body, `kcc-body` | Hazelnut, 0.38208 | 16 / 24 | 0 (not in the list) |
| `kcc-display`, `h1` | APCasual, 0.25 | 40 / 48, 36 / 48 below 768px | +8.1px, +9.1px |
| `kcc-section`, `h2` | APCasual, 0.25 | 32 / 48 | +10.1px |
| `kcc-heading`, `h3` | APCasual, 0.25 | 24 / 24 | +0.1px |
| `kcc-title`, `kcc-hand`, `h4`–`h6` | APCasual, 0.25 | 20 / 24 | +1.1px |
| caps roles, `text-caps` | Sono, 0.312 | 14 / 24 | +1.7px |
| `text-readout` | Sono, 0.312 | 64 / 72 | −9.9px |

## Sheets that carry the margin rule

See research R5. The list is the scope of `margin` on `KccSheet` and of `kcc-sheet--margin` in Razor.
