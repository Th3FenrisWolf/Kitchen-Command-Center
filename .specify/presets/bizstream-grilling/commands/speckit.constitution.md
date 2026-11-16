---
description: Create or update the project constitution, grilling the user on each principle in dependency order.
strategy: wrap
---

## Before you begin

This command asks the user questions. The **Question protocol** at the end of this file replaces
how those questions get asked. Read this whole file before you act on any part of it.

{CORE_TEMPLATE}

## Question protocol

> This section is authoritative for every question this command puts to the user.
> Where anything above conflicts with it, this section wins.

Where the command above prescribes how many questions to ask, in what format, or when to stop,
this section replaces it. Everything else it asks for stands.

Interview the user until you reach a shared understanding. Adapted from Matt
Pocock's `grilling` skill (github.com/mattpocock/skills), MIT.

Map the open decisions as a **design tree**: every decision branches into the
decisions that hang off it. Work the tree in **rounds**. The **frontier** is
every decision whose prerequisites are already settled — the questions you can
ask *now* without guessing at answers you have not heard yet. Ask the whole
frontier in one round. Wait for the user's answers before the next round.

### Rules

- **Facts are your job, never the user's.** When a frontier question needs a
  fact from the environment (the repository, a linked document, an issue
  tracker, a database), dispatch a subagent or look it up yourself. Never ask
  the user for anything you can look up. A running lookup is an unsettled
  prerequisite: only its downstream questions wait; ask the rest of the
  frontier now.
- **Decisions are the user's.** Put each one to them with your recommended
  answer first, and wait.
- **A gate owned by someone else is never asked here.** When
  `.specify/memory/constitution.md` assigns a decision to a client or to a
  named human who is not in this session, that decision stays open. Record it
  as a row (see Checkpoint rows) or, in a target with no checkpoint files, as
  a GATED marker. Recommend nothing binding, and move on.
- **A GATED marker is never asked.** `[NEEDS CLARIFICATION: GATED — <owner>:
  <question>]` is the one form of a gate: a decision its owner holds outside
  this session. Never collect it into a round, never dedupe another question
  into it, and leave it verbatim. Only the owner's answer clears it.
- A question whose answer depends on another question still open in this round
  belongs to a later round.
- Done when the frontier is empty: every branch visited, nothing silently
  assumed. Do not act until the user confirms shared understanding.

### No person present

When `SPECKIT_HEADLESS` is `1` (`printenv SPECKIT_HEADLESS`), nobody can
answer. Ask nothing, and never call `AskUserQuestion`. Write each decision as
a row, or as a marker in a target with no checkpoint files, with its
recommended answer and its alternative. Draft on the recommended answer, and
end with `"stop":"owner"`.

### Checkpoint rows

When `.specify/extensions/flow/scripts/flow.mjs` exists and the target is a
feature directory or a file in one, the feature's open decisions live in its
checkpoint files, `FEATURE_DIR/.flow/checkpoints/`. Never write a
`[NEEDS CLARIFICATION]` marker there. Below, `flow` means
`node .specify/extensions/flow/scripts/flow.mjs`.

- **Read** the open rows with `flow checkpoints FEATURE_DIR`. They join the
  frontier.
- **Write** each new decision as a row before you ask it:
  `flow add FEATURE_DIR <checkpoint> '<row JSON>'`. The checkpoint is
  `product` for what the feature does for a user or a client, and `design`,
  in plan only, for the shape of the build: a new store, a new dependency or a
  new public contract.
  The row holds:
  - `id`: the question number, such as `Q3`.
  - `audience`: `po`, or the owner a gate names.
  - `question`, `recommended` and `alternative`, each one sentence a client
    could read.
  - `phase`: `clarify` when the answer changes `spec.md`, `plan` when it
    changes `plan.md`.
  - `affects`: the criterion ids that depend on the answer. Leave it empty
    when the whole checkpoint must wait.
- **Move** an old `[NEEDS CLARIFICATION]` marker: write it as a row, then
  delete the marker.
- **Answer** with `flow answer FEATURE_DIR <id> --answer "<text>" --by <who>
  --channel session`, where `<who>` is the output of
  `git config user.email`. Then write the answer into the artifact the row's
  `phase` names.

Offer at most two answers per question, your recommendation first, then these
two options:

- **Ask the client.** Run `flow route FEATURE_DIR <id> --audience client
  --affects <ids>`, with the criterion ids that depend on the answer. Ask no
  follow-up, go on to the next question, and keep drafting on the recommended
  answer. Only the tasks that name those ids wait for the client.
- **Client already answered.** Ask for the answer and for who gave it. Then run
  `flow answer FEATURE_DIR <id> --answer "<text>" --by <client> --relayed-by
  <who> --channel relayed`.

A row whose audience is `client`, or a gate's owner, waits for that owner.
Never put it in a round. When the person gives an answer for now on their own,
write it with `--interim`: the work goes on, and the row stays open until the
owner's answer replaces it.

#### Meeting notes

When the target names a notes file, such as meeting notes or an email, match
it against the open rows before the first round. For each row the notes
answer, show the row's question, the passage quoted word for word, and the
answer it gives. Ask with three options: Confirm, Edit, and Not an answer.
Write only a confirmed or edited answer: `flow answer FEATURE_DIR <id>
--answer "<text>" --by <who said it> --relayed-by <who> --channel notes
--source <notes path> --reply "<passage>"`. A row the notes do not answer stays
in the frontier.

### Asking a round

Prefer the agent's own question affordance — `AskUserQuestion` in Claude Code:
up to 4 questions per call, each with 2 to 4 options, your recommendation
listed first and marked `(Recommended)`. In a feature with checkpoint rows,
two of the options are the ones Checkpoint rows sets. A frontier larger than 4
spans consecutive calls in the same round. Dependency decides round
membership, not the 4-question cap.

Fall back to the markdown format below only when a question cannot be
expressed as options, because it is open-ended, needs a diagram, or needs long
context.

```
❓ **Q1** - **<question title>**: <question body, may be multiple paragraphs,
including choices>

➡️ <your recommended answer>
```

Number questions across the whole session (Q1, Q2, …), never restarting per
round, so answers can reference them unambiguously.

### Batch mode

When the input is a set of artifacts rather than one — several specs, or their
collected `[NEEDS CLARIFICATION]` markers:

1. Collect every marker and every open row across all artifacts, except GATED
   markers and rows whose audience is `client` or a gate's owner.
2. **Dedupe.** Many artifacts share one underlying decision. One question,
   answered once, applies to every artifact it touches.
3. Sort the survivors into the design tree and run rounds as above.
4. After each round, apply the answers to every affected artifact before you
   ask the next round.
5. Anything the user defers stays a marker or an open row for a later
   `/speckit.clarify`.
