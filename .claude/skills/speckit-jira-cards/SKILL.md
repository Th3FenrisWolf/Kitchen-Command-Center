---
name: speckit-jira-cards
description: Write client-facing Jira cards from the specs/jira-cards.md registry, link specs to cards that already exist, and move a card when its pull requests open. One Story per outcome, several specs per card, dry run by default.
compatibility: Requires spec-kit project structure with .specify/ directory
metadata:
  author: github-spec-kit
  source: jira:commands/speckit.jira.cards.md
---

# Jira cards

A **card** is an outcome a client, a project manager, a business analyst, or
QA can see — usually a page area or a capability, never a spec, a user story,
or a task. Several specs feed one card. `specs/jira-cards.md`, which the
project owns, is the registry: one row per card (card, epic, the specs that
feed it, Jira key) and the card text under its own heading. The project adds
a row and writes its text before the card exists; this command turns an
approved row into a Story and writes the key back into the row.

When the cards exist in Jira first, `speckit.jira.pull` fills the registry
instead, with every key already set.

Read `.specify/extensions/jira/templates/linking.md` first. It defines the
**Jira line**, a spec's one link to its cards. Read
`.specify/extensions/jira/templates/acli-gotchas.md` before scripting any
`acli` call below that this file does not spell out verbatim.

## Target

`$ARGUMENTS` resolves in this order:

1. `pr <spec-path>` — a spec's pull request just opened. Skip to
   [Track a pull request](#track-a-pull-request).
2. A path, a spec directory, a Jira key, or text matching one or more
   registry rows, optionally followed by `--create`. Touch only the matching
   rows.
3. Empty — every row with no Jira key yet, dry run.

`specs/jira-cards.md` must exist. When it does not, print the table header
and the `## Cards` heading from [Registry shape](#registry-shape) and stop —
the project adds rows and card text before this command has anything to
write.

## 1. Resolve rows and read config

Read `.specify/extensions/jira/jira-config.yml`. `project_key` is the
default; a row or the run may override it with `--project`. `test_notes_field_id`
is required before step 5 runs for real — dry run proceeds without it and
says so in its output.

For each target row, read its specs — for a keyed row, every spec whose Jira
line names its key; for a row with no key, every spec its `Specs` column
names — plus
`.specify/memory/constitution.md` when it exists for a settled epic layer or
a decision that gates this card. Every word that reaches Jira in step 3
traces to a spec; nothing is invented at the card.

**Done when:** every target row has its specs read and its card text located
under its `## Cards` heading.

## 2. Check auth and dedupe

```bash
acli jira auth status
```

Not authenticated → stop and say to create a Jira API token at
https://id.atlassian.com/manage-profile/security/api-tokens, then run
`acli jira auth login --site <site> --email <email> --token`, with the token on standard input.

A row that already carries a Jira key is a link, never a create. Add the
key to the Jira line of every spec its `Specs` column names that does not
list it yet. Point at `speckit.jira.pull --refresh <KEY>` for a drift check
against the live card. The row never gets recreated, and this step writes
nothing to Jira.

For a row with no key, read the Jira line of every spec it names first, and
report every key already on it: a spec may feed this card too, but one of
those keys may already be this card. Then run a label search before
drafting:

```bash
acli jira workitem search --jql "project = <KEY> AND labels = '<feature-label>'" --csv --fields "key,summary,labels"
```

The feature label is a spec's directory name, lowercased, non-alphanumerics
turned to hyphens (`003-checkout` stays `003-checkout`). Every card this
command creates carries the labels of every spec that feeds it, so build the
full label set for the row now.

## 3. Draft

Hold, in memory, one draft per target row: an Epic (only when needed, see
below), a Story, and the Story's Test Notes.

**Epic.** Reuse the project's own epic layer when it has one — an epic
already tracked in Jira, or in a document the project maintains for it. Only
a project with no epic layer at all gets an Epic created here, one per
distinct value in the registry's `Epic` column, shared by every row that
names it. An `Epic` value that is already a Jira key is an Epic that exists;
never create a second one for it.

**Story**, one per target row, under the [Card shape](#card-shape) rules.
Never a Sub-task, and never a second item for a spec, a user story, or a task
inside the row's specs — the row is the only unit that becomes a Story.

**Test Notes**: the row's own two to four happy-path steps, as an ordered
list. No branch cases; QA designs those independently.

**Done when:** every target row has a drafted Story under 800 characters, or
is flagged over budget for the registry to cut before this command runs
again.

## Card shape

```
Summary: <what moves>, e.g. "Gallery moves to the new site"

<One or two sentences: what moves, and that it looks and works the same.>

Known differences: <one sentence per deliberate change, drawn from the
specs' visible-change statements and pixel exceptions>. "none" is valid.
Open decision, <owner>: <the gated question, from a spec's gated marker>.
  (omit the line entirely when no decision is open)
```

Under 800 characters, the whole description. Test Notes live in the
project's Test Notes field, not the description.

**Layman rule.** A card names no file path, type or field name, task or
criterion ID, tool name, package name, or vendor term. A platform name or a
version number is a fact, not an implementation detail — keep it (".NET 10",
"the checkout API v2"). An infrastructure spec — one where nobody outside the
delivery team would see a change in what the product does — keeps the
team's own vocabulary instead; the stripped list above still applies to it.

## 4. Present for approval

Print the whole draft: every Epic and Story summary, full description text,
Test Notes, and each description's character count. State the project key
and the total item count.

**Stop here unless `--create` was passed on this run.** Creating Jira items
is client-visible. Approval covers this run only — it never carries to the
next.

## 5. Create

Create each Epic first:

```bash
acli jira workitem create --project <KEY> --type Epic --summary "<summary>" --description-file <file> --label "<label-1>,<label-2>"
```

Write its key into the `Epic` column of every row that names it the moment
it lands, before any Story is created. A resumed run then reads a key there
and creates no second Epic.

Create each Story from a JSON file, because only the JSON form carries a
custom field. `acli jira workitem create --generate-json` prints the
template. Fill `projectKey`, `type` (`Story`), `summary`, `labels`, the
description, and Test Notes under `additionalAttributes` as
`customfield_<test_notes_field_id>`. Put the Epic key in `parentIssueId`.

```bash
acli jira workitem create --from-json <file>
```

Record the Story key under step 6 the moment `create` returns it, before any
check below. Then confirm the Story with `acli jira workitem view <KEY>
--fields "parent,customfield_<test_notes_field_id>" --json`. A missing parent
is set with `acli jira workitem edit --key <KEY> --parent <EPIC-KEY> --yes`.
Missing Test Notes stop the run: the Story is client-visible and needs a
second pass to repair.

**Write the description and Test Notes as ADF, never plain text.** Plain text
wraps in one paragraph node and every line break collapses, so a multi-line
Known differences section reads as one run-on sentence. Build a `doc` node:
one `paragraph` per descriptive sentence, a `bulletList` of `listItem` →
`paragraph` for the Known differences once there is more than one, and an
`orderedList` built the same way for Test Notes.

Stop on the first failure and report what already exists. A partial run
resumes cleanly through the keys written in step 6; a silent one does not.

**Done when:** every drafted item exists in Jira with its parent and Test
Notes, or the run stopped and reported exactly where.

## 6. Record

Write the Jira key into the registry row **as each card lands**, not after
the run finishes — the acli-gotchas template explains why a deferred write
turns an interrupted run into a duplicate. Rename the row's card heading to
`### <KEY> <card title>` at the same time.

Then add the key to the Jira line of every spec the row names.

Report every created key as a markdown link.

## Track a pull request

For `pr <spec-path>`: read the spec's Jira line. No line → stop and name the
gap. Run the steps below once for each key on the line.

Find every spec carrying the key. Pull requests live in the repo the code
lives in: `code_repo.path` from `.specify/extensions/card/card-config.yml`
when set, this repo otherwise. Name it on every `gh` call, because `gh`
without `--repo` resolves the working directory's repo, which in split mode
holds no pull request. Read the name once, with a `cd` into the code repo:

```bash
gh repo view --json nameWithOwner --jq .nameWithOwner
```

Then list the pull requests once:

```bash
gh pr list --repo <owner/name> --state all --limit 1000 --json number,title,body,url,state,headRefName
```

A pull request belongs to a spec when its `headRefName` equals the spec's
directory name or starts with it followed by `-` or `/`, or its title names
that directory. A body that names the directory does not count: a pull
request for one spec often cites another. A branch
name need not equal the directory name. A pull request that only carries the
key in its title does not count for a spec, and a closed, unmerged one never
counts.

A spec is **landed** when it has a pull request that is `OPEN` or `MERGED`,
or when it is docs-only: `code_repo.path` is set, every task in its
`tasks.md` is marked `[X]`, and no task changes a file in the code repo. A
task path is written from the code repo's root, so `src/Foo.cs` is a code
change; only a path inside this repo is a docs change.

Move the card when every spec carrying the key is landed, and at least one of
those pull requests is `OPEN` or this run's spec is docs-only. Read the
card's available transitions, then apply the one that lands on
`board.in_review_status` from `.specify/extensions/card/card-config.yml`, or
`In Review` without that file. Otherwise, add a comment on the card linking
this pull request, or naming the docs-only spec — the card itself does not
move yet.

## Registry shape

```markdown
# Client-facing Jira cards

| Card | Epic | Specs | Jira |
|---|---|---|---|
| <card title> | <epic id, or its Jira key once it exists> | <spec directory names that feed it> | |

## Cards

### <card title>, or <KEY> <card title> once it has a key

<the card text, in the shape above>
```

The `Specs` column names the specs a row is drafted from. Once a row has a
key, each spec's Jira line is the link, and a card's specs are found by
searching for it.

## Guardrails

- This command writes `specs/jira-cards.md` (the `Epic` and `Jira` columns
  and the card heading), the Jira line in `spec.md`, and Jira itself. It
  never edits any other part of a spec, a plan, or a task file.
- Nothing reaches Jira that a target row's specs do not already say. A gap
  goes back to the registry, never invented at the card.
- A card this command did not create is never edited from here. Its Jira
  writes are limited to the pull-request comment and transition above.