---
name: speckit-jira-pull
description: Pull Jira cards that already exist into the specs/jira-cards.md registry so specs are written from them, refresh the registry when a card changes, or link an existing spec to a card. Never writes to Jira.
compatibility: Requires spec-kit project structure with .specify/ directory
metadata:
  author: github-spec-kit
  source: jira:commands/speckit.jira.pull.md
---

# Jira pull

Some projects start from cards a project manager or the client already made
in Jira. This command brings each card into `specs/jira-cards.md`, the
registry `speckit.jira.cards` owns, with its key already set. Specs are then
written from the registry row, and each spec links to its card through its
Jira line. The live card stays the source: nothing here writes to Jira.

Read `.specify/extensions/jira/templates/linking.md` first. It defines the
Jira line and the card criteria. Read
`.specify/extensions/jira/templates/acli-gotchas.md` before scripting any
`acli` call below that this file does not spell out verbatim.

## Target

`$ARGUMENTS` resolves in this order:

1. `--refresh [<KEY>...]` — compare keyed rows with their live cards. Skip to
   [Refresh](#refresh). No key → every keyed row.
2. `<KEY> --spec <spec-dir>` — link a spec that already exists. Skip to
   [Link a spec](#link-a-spec).
3. `<KEY> [<KEY>...]` or `--jql "<query>"` — pull those cards.

Check auth first:

```bash
acli jira auth status
```

Not authenticated → stop and say to create a Jira API token at
https://id.atlassian.com/manage-profile/security/api-tokens, then run
`acli jira auth login --site <site> --email <email> --token`, with the token on standard input.

## Pull

With `--jql`, resolve the keys first:

```bash
acli jira workitem search --jql "<query>" --csv --fields "key,summary"
```

Create `specs/jira-cards.md` in the registry shape `speckit.jira.cards`
defines when it does not exist: the table header `| Card | Epic | Specs | Jira |`
and a `## Cards` heading.

For each key: a registry row that already carries it is skipped and reported.
Otherwise read the card with its parent and, when config sets it, the
acceptance criteria field:

```bash
acli jira workitem view <KEY> --fields "summary,description,parent,customfield_<acceptance_criteria_field_id>" --json
```

Then write:

1. A registry row: the summary as `Card`, the parent Epic key as `Epic`, an
   empty `Specs`, and the key as `Jira`. Escape every `|` in the summary as
   `\|`.
2. A `### <KEY> <summary>` heading under `## Cards`, with the snapshot below
   it: the live description verbatim, then the card criteria as a numbered
   list under `Card criteria:`. Demote every heading inside the description
   to `####` or lower, so none of them reads as a card of its own.

Write each row as its card is read, not at the end of the run.

**Done when:** every key is a registry row or is reported as skipped. Report
every key pulled as a markdown link, then the next step:
`speckit.batch.run specify` writes one spec per pulled card and links it.

## Refresh

For each target row, read the live card the way Pull does and compare its description and
criteria with the row's snapshot. When they differ:

1. Write one marker into every spec whose Jira line names the key, at the
   section the change touches, or below the header block when none fits:
   `[NEEDS CLARIFICATION: card <KEY> changed — <what changed, and each
   criterion added, removed or reworded by id>]`. Write the markers before
   the snapshot, so an interrupted run leaves the change visible.
2. Replace the snapshot with the live text, in the pull shape above.
3. Report the key, the change, and every spec that got a marker.

The marker is an ordinary open decision. `speckit.clarify`, `speckit.batch.run
clarify` and `speckit.grill.run` pick it up, and `speckit.card.implement`
stops on it until it is answered.

**Done when:** every target row matches its live card, and every spec its
change touches carries a marker for it.

## Link a spec

For `<KEY> --spec <spec-dir>`: the key must be a registry row; pull it first
when it is not. Add the key to the Jira line in `<spec-dir>/spec.md`, or write
the line when the spec has none. Then check the spec's scenarios against the row's criteria and
report every criterion no scenario cites.

## Guardrails

- This command writes `specs/jira-cards.md`, the Jira line in `spec.md`, and
  the refresh markers above. It never edits any other part of a spec, and
  never writes to Jira.
- A pulled card is changed by its owner in Jira. When a spec disagrees with
  it, report the difference.