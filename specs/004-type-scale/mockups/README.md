# Type scale: approved look

Approved by the owner on 2026-10-19 after three rounds of `/speckit-preview-run`. The first approval, 18 / 28, was
built and then rescaled to 16 / 24.

- `rescale.html` is the approved option alone. It reads `/kit.css` and `/assets/` from the `.mockups/` server, so open
  it through the `preview` configuration in `.claude/launch.json`.
- `rescale-light.jpg` and `rescale-dark.jpg` are full-page shots of it in each ramp.
- `scale.html`, `scale-light.jpg` and `scale-dark.jpg` are the first approval, 18 / 28, kept as the record the rescale
  replaced.

## Approved: Even steps on 16 / 24

Body is 16px Hazelnut on a 24px rule. Every heading is one even step above the one below it.

| Role | Face | Desktop size / line | Phone size / line |
|---|---|---|---|
| Display | APCasual | 40 / 48 | 36 / 48 |
| Section heading | APCasual | 32 / 48 | same |
| Sheet heading, stat value | APCasual | 24 / 24 | same |
| Card title, hand note | APCasual | 20 / 24 | same |
| Body, nav item, field, pad line | Hazelnut | 16 / 24 | same |
| Kicker, label, meta, badge, button, unit, quantity | Sono caps | 14 / 24 | same |
| Cook Mode timer (readout, counted apart) | Sono | 64 / 72 | same |

Cook Mode's step text takes the section heading step. Buttons and segments keep their 36px and 48px heights, one and a
half and two rules. Badges and the marker label keep a 24px line inside their pill. Standalone icons stay 24, 36 and
48px. Below 768px only the display heading drops, so the four heading roles stay distinct at every width.

It won because 18 / 28, built on the site, read as zoomed: the owner found it right at 90% zoom. 16 / 24 keeps the
same even staircase at that density.

## The options it beat

| Round | Option | Why not |
|---|---|---|
| 1 | Snug: 16 on a 24px rule, 20 / 24 / 32 / 40 | The owner chose the 18px body. Round 3 came back to these sizes. |
| 1 | Roomy: 20 on a 32px rule, 24 / 28 / 32 / 40 | The owner chose the 18px body. |
| 2 | Bold top: 22 / 28 / 40 / 56 | The owner chose even steps. No reason given. |
| 2 | Flat: 20 / 24 / 28 / 36 | The owner chose even steps. No reason given. |
| 3 | 18 / 28 as shipped: 22 / 28 / 36 / 44 | Built on the site, it read as zoomed. |

## Still open

- Spec 003 owns the baseline shim, and its Recipe preset still says 20px body on a 32px pitch. 003 takes 16px on 24px
  from this scale (FR-014) when it is clarified.
- The preview sets line heights only. Labels centred on a control wait for spec 003's shim.
