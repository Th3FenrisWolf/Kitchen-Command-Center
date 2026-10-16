# Data Model: Paper Piles, Not Tape

This feature stores no data. Its model is the pile's parts, the custom properties that tie them together and the
preset data the tear generator writes. The markup is in [contracts/pile-markup.md](contracts/pile-markup.md).

## Parts

| Part | Element | Holds | Moves with |
|---|---|---|---|
| **Sheet** (spec entity) | `.kcc-slip` (`height: fit-content`, or `100%` with `--fill`) › `.kcc-torn` › `.kcc-sheet` | content, wash, label, tilewrap | its slip's tilt, lift, stickiness, step back |
| **Blank sheet** (spec entity) | `.kcc-blank` › `span` (`display: block`), last child of the slip, the slip's box | nothing; `aria-hidden` | the slip's tilt, stickiness and step back; **not** the lift |
| **Applied item** (spec entity) | `.kcc-tilewrap` › `.kcc-torn` › `.kcc-tile`, plus `.kcc-tape`; in a list row, `.kcc-tilewrap--flow` inside the sheet | an icon tile and its one strip | the lift |
| **Photo tile** | `PhotoStrip.vue`'s torn photos on the desk | a photo | nothing; a tile, not a sheet: no blank, no tape (A20) |
| **Pile** (spec entity) | a slip and its blank | | |
| **Header pad** | `.pad-top` slip, `.pad-cards`, one `.pad-page` (`kcc-slip kcc-slip--fill kcc-slip--wide kcc-tear-hero-2`, no `.kcc-blank` of its own) | the bar; the menu cards; a blank page | the pad's tuck |

Validation, from the requirements:

- A slip on the public site holds exactly one `.kcc-blank` (FR-001), except three: a pad card (`PadCard.vue`); the
  pad's top sheet, whose blank is the `.pad-page` sibling (FR-008); and the `.pad-page` itself, a `kcc-slip` that is
  the top sheet's blank and keeps `kcc-slip--fill` for the pad's height.
- A `.kcc-tilewrap` sits outside the clip and the filter, except a `.kcc-tilewrap--flow`, which sits in a sheet's
  content flow with its tape (the list row's tile).
- A `.kcc-tape` has a `.kcc-tilewrap` parent (FR-006, FR-007).
- A blank's tear differs from its slip's tear (FR-003).
- A blank's offset box equals its slip's `.kcc-torn` offset box (FR-004).
- In reading order (the visible sheets in `main`, in document order), each blank turns the other way from the one
  before (FR-003, research R12).
- No page scrolls sideways (research R10).

## Custom properties

| Property | Set on | Value | Read by |
|---|---|---|---|
| `--r` | a slip, by its tear class or inline (`crisp`) | the top's tilt in degrees; registered, non-inheriting | `.kcc-slip` transform |
| `--blank-tear` | a slip, by its generated tear class | `var(--tear-N)`, the blank's preset | `.kcc-blank` (`--tear`) |
| `--blank-dir` | a slip, by its generated tear class | `1` or `-1`: opposite the tilt for the six standard presets; `tear-hero` −1, `tear-hero-2` +1 | `.kcc-blank` transform |
| `--blank-flip` | `.kcc-slip` (`1`); `.kcc-slip--flip` (`-1`) | `1` or `-1`; not inherited past the slip that sets it | `.kcc-blank` transform |
| `--blank-region` | a container, by an arbitrary-property utility | `-1` over a run of sheets whose neighbour a breakpoint hides; unset elsewhere (read as `1`); inherited | `.kcc-blank` transform |
| `--blank-turn` | `.kcc-slip`; `.kcc-slip--wide` | `1.5deg`; `0.5deg` | `.kcc-blank` transform; `.pad-page` transform |
| `--blank-scale` | `.kcc-slip`; `.kcc-slip--wide` | `1.015`; `1.005` | `.kcc-blank` transform; `.pad-page` transform |
| `--tear` | a slip's tear class; `.kcc-blank` | the clip polygon | `.kcc-sheet`; `.kcc-blank > span` |

Wide sheets carry `kcc-slip--wide`: the pad's page, every hero-torn sheet, and every sheet wider than 700px at a
1280px viewport (the list is in research R5).

The blank's transform, about its centre:
`rotate(calc(var(--blank-turn) * var(--blank-dir) * var(--blank-flip) * var(--blank-region, 1))) scale(var(--blank-scale))`.
The pad's page, a sibling of the top sheet rather than a child:
`rotate(calc(-0.12deg + var(--blank-turn))) scale(var(--blank-scale))`, with `kcc-slip--wide` on the page.

## Generated preset data (`src/KCC.Web/Features/Torn/tears.ts`)

`TearPreset` gains `blank?: { tear: string; dir: 1 | -1 }`, set on every sheet preset and absent on tile presets.
`tearsCss()` writes it into the preset's class after `--r`:

```css
.kcc-tear-1 {
  --tear: var(--tear-1);
  --r: 0.3;
  --blank-tear: var(--tear-4);
  --blank-dir: -1;
}
```

| Preset | Points | Tilt | Blank tear | Direction |
|---|---|---|---|---|
| `tear-1` … `tear-6` | 80 | as today | the preset three along the cycle | opposite to the tilt's sign |
| `tear-hero` | 160 | −0.58 | `tear-hero-2` | −1 |
| `tear-hero-2` (new) | 160 | within ±1°, own seed and corner | `tear-hero` | +1 |

Rules `tears.test.ts` enforces: a blank tear is never the preset's own; it names a declared sheet preset; the six
standard directions are opposite their tilt and alternate around the cycle; the hero directions are as set; a tile
preset writes no blank.

## State

| State | Blank | Top sheet |
|---|---|---|
| At rest | turned and scaled | its tilt |
| Hover or focus within, `kcc-slip--lift`, motion allowed | still | children 4px up over 300ms |
| Hover or focus within, reduced motion | still | still |
| Stacker card stuck (`.stuck`) | scales to .95 with the slip | scales to .95 |
| Ramp switch (`data-theme-switching`) | switches with the sheet, no transition | same |
| Pad tucked | moves with the pad | moves with the pad |
