---
description: Build the QA wizard for a feature. One stage per acceptance scenario, each opening the page to check and recording pass or fail. For the manual pass a developer does by hand before hand-off.
---

# QA walk

A **wizard** is a script that walks a developer through the manual QA pass for
one feature, one **stage** per check. It comes in two forms, bash and
PowerShell, and they behave the same. Each stage opens the page or
location, says what to do, states what must be true, and records pass, fail or
skip. The results table it writes goes with the hand-off. Adapted from Matt
Pocock's `wizard` skill (github.com/mattpocock/skills), MIT.

The library in `.specify/extensions/qa/scripts/` already solves the UX:
progress, one stage on screen at a time, cross-platform URL opening, verdict
capture, the results file, and re-runs over named stages. Everything above its
`STAGES` marker is identical in every wizard. Your job is to collect the checks,
map each one's journey, and author the stages below the marker.

`qa-wizard.sh` is the bash form and `qa-wizard.ps1` is the PowerShell form.
Take the one the project already uses. When the project uses neither, take bash
on macOS and Linux, and PowerShell on Windows.

## Target

`$ARGUMENTS` resolves in this order:

1. A feature directory, or a path inside one (for example `specs/003-checkout`).
2. Empty. Run the `check-prerequisites` script under `.specify/scripts/` with
   `--json --paths-only` (bash) or `-Json -PathsOnly` (PowerShell) and take
   `FEATURE_DIR` from its output.

`FEATURE_DIR/spec.md` must exist. When it does not, stop and name the path you
looked at.

## 1. Collect the checks

Read `spec.md` in full. Each item below is one check, and its id is the one the
wizard keeps, so a result traces back to the spec line it came from:

- every Acceptance Scenario, as `US<story>-<n>` (the second scenario of User
  Story 1 is `US1-2`)
- every Edge Case, as `EC-<n>` in document order
- every Success Criterion a person can observe on a screen or in an output, as
  its own `SC-<nnn>`

A Success Criterion that needs instrumentation to judge, such as a load figure
or a rate over time, is listed in the report as not walkable, with the reason.

Read `FEATURE_DIR/tasks.md` and `FEATURE_DIR/plan.md` when they exist. A task
that names a file or a route tells you where the code for a scenario lives.

**Done when:** every scenario and edge case has an id, and every Success
Criterion is either a check or listed as not walkable with its reason.

## 2. Map each check's journey

For each check, work out the concrete path a developer follows: the URL or
location to open, the state it starts from (the Given), the actions (the When),
and what must be visibly true at the end (the Then).

Facts are your job. Read the routes, controllers, views, CLI entry points and
seed data. Find the local base URL in the project's own config:
`launchSettings.json`, `appsettings*.json`, `.env*`, `docker-compose*`, the
framework's dev-server config. Find test accounts in seed data or fixtures.

Decisions are the developer's. Put to them only what the repository cannot
settle: which base URL when several exist, which account to sign in with, and
any journey whose UI path you could not trace. Give your recommendation first.
When `.specify/extensions/grill/templates/grilling.md` exists, ask by its rules.
Where a UI path is unknown, say so; the developer confirms it before you write
it.

Group consecutive checks by page, so the developer stays on one screen for as
long as the checks allow. Then show the ordered stage list, with the location
and the check for each, and wait for the developer to confirm. They may add,
drop or reorder.

**Done when:** every check traces to a location and to instructions a stranger
could follow, the developer has confirmed the order, and no step describes a
control you have not seen in the code or had confirmed.

## 3. Author the wizard

Copy the library you chose to `FEATURE_DIR/qa-walk.sh` or
`FEATURE_DIR/qa-walk.ps1`, and make a bash copy executable. Below the marker,
write one stage per check in the confirmed order: `stage "<id>" "<title>"` in
bash, `Stage '<id>' '<title>'` in PowerShell. Set `TOTAL_STAGES` or
`$TotalStages` to the number of stages you wrote. Set the banner title to the
feature name.

Inside a stage, use the library in this shape:

- `say` or `Say` for the Given
- `open_url` or `Open-Url` before any instruction about that page
- `Step` for each When, one action per call
- `Check` for the Then, quoting the spec's own wording
- `Note` for context, `Warn` before a step that changes data, and `Confirm` to
  gate that step

Keep a stage to one check. The screen clears at each stage, so everything the
developer needs for that check sits inside it. The library above the marker
stays byte-identical to the source.

The spec's wording is the card author's. `Check` quotes the Then as written;
where the spec's phrasing cannot be checked as written, the stage says what to
look for in a `Note` and the report names the gap.

**Done when:** the file has one stage per check from step 1, in the confirmed
order, and nothing above the marker changed.

## 4. Verify and hand off

- Parse the file. Run `bash -n FEATURE_DIR/qa-walk.sh`, or for PowerShell
  `[System.Management.Automation.Language.Parser]::ParseFile`. The error count
  must be 0.
- Count the stage calls. It equals `TOTAL_STAGES` or `$TotalStages`.
- Every id from step 1 appears exactly once as a stage, or sits in the
  not-walkable list.
- Trace it statically. The developer runs it; it opens browsers and waits for a
  person at every stage.

Report: the wizard's path, the stage count, the criteria you listed as not
walkable and why, and the run line:

```bash
bash specs/<feature>/qa-walk.sh
```

```powershell
pwsh specs/<feature>/qa-walk.ps1
```

Say that `--only US1-2,EC-1` in bash, or `-Only` in PowerShell, re-walks named
stages and keeps the other rows. Say that `--no-browser` or `-NoBrowser` prints
each URL instead of opening it, and that results land in
`FEATURE_DIR/qa-walk-results.md`. Both files belong in the feature's commit.

## Guardrails

- This command writes `FEATURE_DIR/qa-walk.sh` or `FEATURE_DIR/qa-walk.ps1`,
  and nothing else. Source code and the spec stay as they are.
- The developer runs the wizard. Never run it yourself.
