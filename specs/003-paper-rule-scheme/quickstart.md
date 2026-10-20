# Quickstart: Paper Rule Scheme

How to prove the feature works. Every yarn command needs `FONTAWESOME_NPM_AUTH_TOKEN` in the environment.

## Prerequisites

- The branch `003-paper-rule-scheme` with the work on it, and spec 004 on `main` (it is).
- `yarn build:all` at the repository root, after every change to a stylesheet or a component.

## 1. Style guards

```bash
cd src/KCC.Web && yarn test
```

Expected: `paperRule.test.ts`, `typeScale.test.ts`, `kitClasses.test.ts`, `retiredTokens.test.ts`,
`contrast.test.ts` and the component tests pass.

## 2. Baselines in a browser

```bash
dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj -- --treenode-filter "/*/*/RuleTests/*"
```

Expected: on the recipe page, a variant page, recipe search and the create recipe wizard, at 375px and 1280px wide,
every text baseline on a ruled sheet lies 2px above the top of its rule, within 0.75px (SC-001, SC-003), and each
FR-017 label and the text inside its control does too (SC-008).

## 3. Look in both ramps

Run the site (`https://localhost:58671`) and open, in light and in dark:

- a recipe page and a variant page: margin rule on the reading sheets, none on the hero or the variant cards;
- recipe search: no margin rule, fields as lines with the X, "Sort" on the rule beside its segments;
- the create recipe wizard and the account settings: margin rule, every field a line with the X, the textarea on the
  sheet's rules;
- cook mode: margin rule, 32px icon buttons, the readout on the rule;
- the header pad: its search a line with the X, the pad still fits its bar at 1024px.

Compare with `mockups/workbench-look-light.jpg` and `mockups/workbench-look-dark.jpg`.

## 4. Phone width

At 375px: no page scrolls sideways (SC-004), a form sheet narrower than 480px shows no margin rule, and a wrapped
control row keeps the text below it on the rule.

## 5. Whole suite

```bash
node tests/scripts/run.mjs
```

Expected: all six suites green.
