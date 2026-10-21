# Contract: the paper rule for authors

The interface this feature exposes is what a Vue component, a Razor view or a kit stylesheet does to put a sheet's
text and controls on the rule. `docs/brand/kit.md` takes this contract over when the feature lands.

## An author MAY

- Pick a ruling: a `kcc-sheet` is ruled; add `kcc-sheet--blank` for no rules at the same rhythm.
- Add the margin rule to a sheet read down the page with `KccSheet`'s `margin` prop, or `kcc-sheet--margin` in Razor.
  The rule leaves on its own below 480px of sheet width.
- Set text in any role of spec 004 (a role class, a bare heading or a `text-<role>` utility) and get it on the rule.
- Put 36px controls and their labels in a `kcc-row`. Each wrapped line fills two rules.
- Use a field primitive (`InputField`, `TextAreaField`, `NumberStepper`) or the `kcc-field` markup: it is a line to
  write on, with the X.
- Read `--rule-drop`, `--rule-offset`, `--k-hazelnut`, `--k-casual`, `--k-sono`, `--color-rule`, `--color-margin` and
  `--control-sm` in a kit stylesheet or a style block.

## An author MUST NOT

- Write a pixel nudge to put text on the rule: the shim does it.
- Centre a label beside a control with `items-center`: use `kcc-row`.
- Put an icon before a field's input: the X is the field's one leading mark.
- Give a block a height that is not a whole or half number of rules, unless a half rule pairs with another.
- Put the margin rule on a hero, a toolbar, a card, the header, a home block or an empty state.
- Use `kcc-field--noicon`.

## Enforcement

| Rule | Test |
|---|---|
| Metrics, constants, ruling, shim, margin rule, `kcc-row`, fields and controls | `tests/KCC.ViteTests/Features/Styles/paperRule.test.ts` (new) |
| Real baselines on the rule in a browser | `tests/KCC.E2ETests/Features/Chrome/RuleTests.cs` (new) |
| Sizes and gaps stay on spec 004's scale | `tests/KCC.ViteTests/Features/Styles/typeScale.test.ts` (unchanged) |
| Every kit class is in kit.md's table | `tests/KCC.ViteTests/Features/Styles/kitClasses.test.ts` |
| `kcc-field--noicon` never returns | `tests/KCC.ViteTests/Features/Styles/retiredTokens.test.ts` |
| Text contrast in both ramps | `tests/KCC.ViteTests/Features/Styles/contrast.test.ts` |
