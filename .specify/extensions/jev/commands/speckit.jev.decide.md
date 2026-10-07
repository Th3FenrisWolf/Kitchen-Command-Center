---
description: Route one decision through the Jev switch, or add and calibrate a Jev question for this project.
---

Read `.specify/extensions/jev/templates/jev.md` and follow it exactly. It holds
every Jev rule; this file only supplies the target. `<jev>` in it is
`.specify/extensions/jev`.

## Target

The target is `$ARGUMENTS`, resolved in this order:

1. A question id and a state, or a path to a state file. Run `decide.mjs` on
   it and report the JSON line.
2. A `tasks.md` path. Run `subagent-tier` over every open row with `--lines`
   and report one table: task id, outcome, and `via`.
3. `calibrate`. Run `calibrate.mjs --offline`, then `calibrate.mjs` when
   `JEV_API_KEY` is set, and report each question below the bar.
4. Anything else. Treat the text as a decision to add. Apply the switch test,
   draft the entry for `.claude/jev/questions.json`, and show it before you
   write it.

## Done when

- [ ] The outcome, the table, the calibration report, or the drafted entry is
  in the reply
- [ ] Every `owner` outcome is named as a stop for the user
- [ ] No question entered a bank without `criteria`, and a noul without `band`
  and `failWhen`
