---
name: torn-and-waxed
description: Use when styling, restyling, reviewing or building any Kitchen Command Center public-site surface - a sheet, card, button, form, widget, page or stylesheet - or when a task mentions the brand, the kit, kcc- classes, washes, tears, the desk, the ramps, or Torn & Waxed. Quick reference plus pointers to the full identity and kit contract.
---

# Torn & Waxed

The KCC public site is torn paper on a desk: careless sheets, exact drawing, the tear as the frame. The
full identity is `docs/brand/torn-and-waxed.md`; the engineering contract is `docs/brand/kit.md`. Read
the contract before writing markup or CSS. This file is the part you should be able to hold in your head.

## Three rules

1. **The paper is careless.** Sheets are torn on four sides, tilt ±1°, may take one strip of tape, and carry
   one off-centre wax wash. No two neighbours share a tear.
2. **The drawing is exact.** A 24px rule governs everything. Type sits on it, numbers are Sono tabular, icons
   are duotone in `currentColor`, one weight of everything.
3. **The tear is the frame.** Nothing is stroked or shadowed except the sheet's fibre and fall. Only printed
   marks (pills, labels, checkboxes) have radii.

## Torn vs printed

| Torn: `kcc-sheet`, `kcc-tile` | Printed: `kcc-btn`, `kcc-seg`, `kcc-field`, `kcc-badge`, `kcc-box`, `kcc-label`, `kcc-tape` |
|---|---|
| clipped, fibre + fall, may tilt, no radius | clean, unclipped, never shadowed, pill / 6px / 3px |

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
  <span class="kcc-tape" aria-hidden="true"></span>
</div>
```

Vue: `import KccSheet from '~/Components/Sheet/KccSheet.vue'` with `label`, `icon`, `wash`, `at`, `tear`,
`tape`, `pad`, `crisp`, `as`. Razor: write it by hand. Tear in a loop: `kcc-tear-${(i % 6) + 1}`.

## Tokens you will reach for

`bg-desk` `bg-desk-2` `bg-paper` `bg-paper-2` · `text-ink` `text-ink-soft` · `border-hair` `border-hair-strong`
· `bg-marker` + `text-marker-ink` (accent, labels, primary) · washes `peach yellow green teal sky lavender
pink red` as fills only. Light is the default ramp; dark overrides live in `Styles/Torn/Tokens.css`.

## Type on the rule

`kcc-h3` 40/48 · `kcc-h4` 22/24 · `kcc-body` 15/24 · `kcc-kick` Sono caps 10.5/24 · `kcc-num` Sono tabular ·
`kcc-hand` italic 17/24, one per sheet. Spacing in 24px steps: `mt-6`, `gap-9` (rows), `gap-x-7` (columns).

## Before you finish

- [ ] Both ramps checked (light default, then `data-theme="dark"`).
- [ ] No `sk-*`, `v-ink`, `data-ink`, `shadow-*`, `rounded-lg`+, `font-bold/semibold/medium`, `fa-primary-*`,
      literal colours, retired tokens (`ink-on-wash`, `ink-line`, `link`, status inks, the six dropped washes).
- [ ] Filter on `.kcc-torn`, not on the clipped element; label and tape outside both.
- [ ] Text never on a wash core; `text-marker-ink` only inside tiles, labels, marker buttons.
- [ ] Numbers in `kcc-num`; meta in `kcc-kick` / `kcc-meta`; headings in APCasual.
- [ ] Structural hooks (`data-testid`, ids, roles) untouched.
- [ ] Path removed from `ALLOWLIST` in `tests/KCC.ViteTests/Features/Styles/retiredTokens.test.ts`.
- [ ] From `src/KCC.Web`: `yarn test <name> retiredTokens contrast`, `yarn type-check`, `yarn format`; at a
      phase gate `yarn build:all`.

## Common mistakes

- Putting `filter: drop-shadow` on `.kcc-sheet` — the shadow is clipped away. Use `.kcc-torn`.
- Rounding a sheet or an image — paper has no radius.
- `text-ink` on a wash **fill** (a tile) — that is `text-marker-ink`. `text-ink` on a wash **pool** under a
  sheet margin is correct.
- A `.kcc-label` inside `.kcc-sheet` — it gets torn and shadowed. It is a sibling of `.kcc-torn`.
- Using a green `text-link` — links are ink with a hair-strong underline (`kcc-link`).
- Reaching for `font-bold` for emphasis — use a `kcc-kick`, a label, or marker.
- Hand-editing `Styles/Torn/Tears.css` — it is generated; edit `Features/Torn/tears.ts` and run `yarn tears`.

## Agents

`brand-steward` reviews a diff or page against the brand (read-only). `kit-builder` converts one surface,
test-first. Use them from the Agent tool by name.
