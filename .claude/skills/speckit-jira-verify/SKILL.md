---
name: speckit-jira-verify
description: Verify an implementation against the spec's own Success Criteria and acceptance scenarios, plus any criteria a card made in Jira carries, and post the verdict as a comment on the card.
compatibility: Requires spec-kit project structure with .specify/ directory
metadata:
  author: github-spec-kit
  source: jira:commands/speckit.jira.verify.md
---

# Jira verify

`spec.md` owns the acceptance criteria. A card made in Jira may also carry
criteria of its own; the spec covers each one with a scenario that cites it.
This command checks the implementation against the spec and those
citations, and leaves the verdict where the card's readers can see it.

Read `.specify/extensions/jira/templates/linking.md` first. It defines the
Jira line and the card criteria.

## Target

`$ARGUMENTS` resolves in this order:

1. A feature directory, or a path inside one (for example `specs/003-checkout`),
   optionally followed by `--no-post`.
2. Empty, or `--no-post` alone. Run the `check-prerequisites` script under
   `.specify/scripts/` with `--json --paths-only` (bash) or `-Json -PathsOnly` (PowerShell) and take
   `FEATURE_DIR` from its output.

`FEATURE_DIR/spec.md` must exist. When it does not, stop and name the path
you looked at.

## 1. Find the card

Read the Jira line in `FEATURE_DIR/spec.md`. Each key on it is a card this
command posts to. No Jira line → stop and name the gap; this command never
guesses a key.

## 2. Collect the criteria

Read `FEATURE_DIR/spec.md` in full. Each item below is one thing to verify:

- every Acceptance Scenario, as `US<story>-<n>`
- every Success Criterion
- every card criterion, as `C<n>`, or `<KEY> C<n>` when the line names more
  than one card, read from each live card with
  `acli jira workitem view <KEY> --fields "description,customfield_<acceptance_criteria_field_id>" --json`
  (description alone when config sets no field). It is verified through the scenarios that
  cite it, and is Not Met when no scenario cites it.

A Success Criterion that needs instrumentation to judge, such as a load
figure or a rate over time, is listed as not verifiable this way, with the
reason, and does not count toward the verdict's denominator.

A spec whose every item lands in that list leaves nothing to verify. Report
that as the finding it is, post no verdict line, and name what instrumentation
the spec would need — an empty set is not a passing verification.

**Done when:** every scenario and every observable Success Criterion has an
id.

## 3. Verify each item

For each item:

1. Run `git diff` against the feature branch's base to see what changed, in
   the repo the code lives in: `code_repo.path` from
   `.specify/extensions/card/card-config.yml` when it is set, this repo
   otherwise. A diff of a specs-only repo shows no code change.
2. `Grep` and `Read` the codebase for evidence the behavior exists.
3. Run the project's build or test command when the item implies functional
   behavior a test already covers.

## 4. Report the table

```
| Item | Status | Evidence |
|---|---|---|
| US1-2 | Met | file:line — <what proves it> |
| SC-004 | Partial | <what's done, what's missing> |
| SC-007 | Not Met | <what's missing, the file it likely belongs in> |
```

For every Partial or Not Met row, name the file it belongs in and what
changes would close the gap.

## 5. Post the verdict

With `--no-post`, stop after step 4: the table is the whole output, and
nothing reaches the card.

Otherwise post the table and its remediation notes as a comment on each card
the Jira line names. A card's comment leaves out the criteria of the other
cards:

```bash
acli jira workitem comment create --key <KEY> --body-file <file>
```

The comment's **last line** is machine-parseable, exactly. It counts only the
items in that card's comment:

- `AC_VERDICT: ALL_MET` — every item verifiable this way is Met.
- `AC_VERDICT: X_OF_Y_MET` — otherwise, X met out of Y verifiable items (for
  example `AC_VERDICT: 3_OF_5_MET`).

`ALL_MET` needs at least one verifiable item behind it. With none, the run
stops at step 2 and posts nothing.

**Done when:** the comment exists on each card and its last line matches one
of the two forms above exactly.

## Guardrails

- This command posts at most one comment per card. It never transitions a card,
  edits the spec, or edits the card's description.
- Re-run after a fix to re-verify; each run posts a new comment rather than
  editing the last one, so the history of verdicts stays on the card.