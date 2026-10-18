# Look preview: the pile

Approved 2026-10-16: **Two steps**, round 3, option 2 (`#two-steps`).

| File | Holds |
|---|---|
| `pile.html` | the approved option alone, with the full specimen set and the ramp toggle |
| `pile-light.jpg`, `pile-dark.jpg` | the approved option in each ramp, at a 1280px viewport |

`pile.html` links the built site stylesheet and fonts through `/kit.css` and `/assets/`, which are in `.mockups/`. That
folder is not tracked, so the file renders only when it is served from `.mockups/` (the preview's server on port 4310),
after `.specify/extensions/preview/scripts/preview-setup.sh` has linked a current build there. The two JPEGs are the
record that needs no build.

## The approved pile

Each sheet rests on one blank sheet, as follows:

- **Placement:** the blank sheet sits centred under its top sheet, with no x or y offset and its transform origin at its
  centre.
- **Turn:** it turns off the top sheet's own tilt. The direction alternates between neighbours.

| Sheet | Turn off the top sheet | Scale |
|---|---|---|
| Cards, panels, forms, empty states | 1.5° | 1.015 |
| The pad's page, the hero (the "wide" modifier) | 0.5° | 1.005 |

- **Hover lift:** when a card lifts on hover or focus, only the top sheet moves. Its blank sheet stays turned and
  scaled where it was.
- **Crisp sheets:** a crisp top sheet stays square, and its blank sheet takes the 1.5° turn.
- **How production builds it:** plain CSS, with a custom property per surface. The pile sets the turn and scale by
  default, and a "wide" modifier on the pad's page and the hero sets the smaller pair. No container query and no typed
  arithmetic.

At a 1280px viewport, the ends of the blank sheet tip out past the top sheet by about:

| Sheet | Width | Tip-out |
|---|---|---|
| Card | 392px | 5px |
| Form | 502px | 7px |
| Panel, empty state | 602px | 8px |
| Pad's page | 1232px | 5px |
| Hero | 702px | 3px |

## Why it won

- It is plain CSS and works in every browser.
- It keeps the owner's 1.5° on every normal sheet.
- The wide sheets stay neat: the pad's page tips out about as far as a card does, where a 1.5° turn tipped it out 16px.

## Rejected options

| Round | Option | Pile | Why not |
|---|---|---|---|
| 1 | `#tight-ream` | 3px down, ±0.3° | The owner asked for no x or y translation, a bigger turn and a slight scale up. |
| 1 | `#pad-pages` | 5px down, ±0.42°, the pad's page today | Same reason. |
| 1 | `#loose-pile` | 5px across, 8px down, ±1° | Same reason. |
| 2 | `#turned-1` | 1°, scale 1.01, no offset | The owner picked 1.5°, but smaller on wide sheets. |
| 2 | `#turned-1-5` | 1.5°, scale 1.015, no offset | Same reason: at 1.5° the 1232px pad page tipped out about 16px at its ends. |
| 2 | `#turned-2` | 2°, scale 1.02, no offset | Same reason: at 2° the pad page tipped out about 21px. |
| 3 | `#capped-tip` | turn `min(1.5deg, asin(10.26px / 100cqi))`, a fixed 3px / 2px growth | Not picked. It puts a size container on every pile and needs typed division with a fallback, and panels (0.98°) and forms (1.17°) turn less than 1.5°. |

## Still open

- **The hero's blank sheet:** the kit has one hero tear preset, and FR-003 wants the blank sheet's edge to differ from
  its top sheet's. The hero's blank sheet needs a second hero tear, or it reuses `kcc-tear-3`, as the preview does. A
  standard tear is generated for sheets up to 700px wide, and a wide hero is wider than that.
- **Corner washes:** a wash radius written `min(60%, 68px)` computes to none in Chrome, so corner washes do not render.
  The preview uses a plain `68px`.
- **The empty state's wash:** in production, the empty state's yellow wash (`18% / 90%`, `60% × 50%`) sits under its
  hint and button. The preview moves it to the bottom-left corner.
