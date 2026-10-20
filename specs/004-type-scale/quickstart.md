# Quickstart: validate the type scale

## Prerequisites

- `FONTAWESOME_NPM_AUTH_TOKEN` is set (every yarn command needs it).
- The site runs from `src/KCC.Web` at `https://localhost:58671`, seeded with `POST /api/dev/seed-recipes`.

## 1. The guard and the token tests

```bash
cd src/KCC.Web && yarn test typeScale typography retiredTokens kitClasses contrast
```

Expected: all pass. Remove one conversion, for example put `text-[15px]` back in `TextAreaField.vue`, and
`typeScale.test.ts` fails naming `Components/Forms/TextAreaField.vue`, its line and `text-[15px]`.

## 2. The rendered sizes (SC-001, SC-003)

Open the recipe search, a recipe detail page, a variant detail page, Cook Mode, the create-recipe wizard and the account
page in the Browser pane at 1280px. Run in the console:

```js
[...document.querySelectorAll('main *, header *')]
  .filter((e) => e.checkVisibility() && [...e.childNodes].some((n) => n.nodeType === 3 && n.textContent.trim()))
  .map((e) => parseFloat(getComputedStyle(e).fontSize))
  .reduce((sizes, size) => sizes.add(size), new Set())
```

Expected: a subset of 14, 16, 20, 24, 32, 40, plus 64 on Cook Mode's timer. Nothing under 14, and only Sono under 16.
Body paragraphs are 16.

## 3. The phone step and sideways scroll (SC-005)

At 375px: the display title is 36px, a section heading 32px, a sheet heading 24px, and `document.documentElement.scrollWidth` equals
`innerWidth`. At 1024px: the header pad shows Recipes, My Kitchen, the search field and New Recipe on one bar. Wherever the search
field shows its full placeholder, the New recipe button shows its label (FR-019).

## 4. Text on the rule

On a recipe detail page, in both ramps, body lines sit on the pencil ruling of their sheet. A heading, a list and a
paragraph that follow each other each start on a whole rule, so the body after a heading is back on the line. Heading,
Sono caps and number baselines may sit a few px off the line until spec 003's baseline shim. Controls (36px and 48px)
may sit between rules.

## 5. Both ramps and the brand (D6, D7)

Switch each page above to the dark ramp and back. Run the `brand-steward` agent over the diff: no violation.

## 6. All suites (D4)

```bash
node tests/scripts/run.mjs
```
