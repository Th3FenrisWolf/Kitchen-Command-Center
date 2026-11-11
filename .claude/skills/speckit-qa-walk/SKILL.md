---
name: speckit-qa-walk
description: Build the QA wizard for a feature. One stage per acceptance scenario the QA audit did not pass, each opening the page to check and recording pass, fail, skip or blocked. For the manual pass a developer does by hand before hand-off.
compatibility: Requires spec-kit project structure with .specify/ directory
metadata:
  author: BizStream
  source: qa:commands/speckit.qa.walk.md
---

# QA walk

A **wizard** is a script that walks a developer through the manual QA pass for one feature, one
**stage** per check the QA audit did not pass. The `bizstream:qa-walk` skill of the bizstream
plugin builds it: it is the one home of the stage rules, the drop rule over audit results, and
the wizard library. This command points it at the feature.

## Target

`$ARGUMENTS` resolves in this order:

1. A feature directory, or a path inside one (for example `specs/003-checkout`).
2. Empty. Run the `check-prerequisites` script under `.specify/scripts/` with
   `--json --paths-only` (bash) or `-Json -PathsOnly` (PowerShell) and take
   `FEATURE_DIR` from its output.

`FEATURE_DIR/spec.md` must exist. When it does not, stop and name the path you looked at.

## 1. Find the core skill

Find `bizstream:qa-walk` the way `.specify/extensions/qa/commands/speckit.qa.audit.md` step 1
finds `bizstream:qa-audit`, with `qa-walk` in place of `qa-audit`. When it is missing, stop and
say: update the bizstream plugin with `/bizstream:update`, then run this command again.

## 2. The criteria file

When `FEATURE_DIR/qa-criteria.json` is missing, write it by step 2 of
`.specify/extensions/qa/commands/speckit.qa.audit.md`, without its card reads when `acli` is not
signed in (put that in `warnings`). Every scenario, edge case and Success Criterion is then a
check.

## 3. Walk

Run `bizstream:qa-walk` with `FEATURE_DIR` as its argument: with the Skill tool, invoke it;
without, read its file from step 1 and follow it. When `.specify/extensions/grill/templates/grilling.md`
exists, its questions are asked by those rules.

## 4. Report

Give the core skill's report, with `FEATURE_DIR` for its folder in the run lines.

## Guardrails

- This command writes `FEATURE_DIR/qa-criteria.json` when it is missing, and the core skill
  writes `FEATURE_DIR/qa-walk.sh` or `FEATURE_DIR/qa-walk.ps1`. Source code and the spec stay as
  they are.
- The developer runs the wizard. Never run it yourself.