---
description: Run the implementation planning workflow, grilling the user in dependency-ordered
  rounds on every decision the plan cannot settle from the spec.
scripts:
  sh: scripts/bash/setup-plan.sh --json
  ps: scripts/powershell/setup-plan.ps1 -Json
  py: scripts/python/setup_plan.py --json
---


## Before you begin

This command asks the user questions. The **Question protocol** at the end of this file replaces
how those questions get asked. Read this whole file before you act on any part of it.


## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Pre-Execution Checks

**Check for extension hooks (before planning)**:
- Check if `.specify/extensions.yml` exists in the project root.
- If it exists, read it and look for entries under the `hooks.before_plan` key
- If the YAML cannot be parsed or is invalid, do not skip silently: tell the user that `.specify/extensions.yml` could not be read (include the parser error) and that no hooks were checked, including any mandatory (`optional: false`) hooks registered there, then continue normally
- Filter out hooks where `enabled` is explicitly `false`. Treat hooks without an `enabled` field as enabled by default.
- For each remaining hook, do **not** attempt to interpret or evaluate hook `condition` expressions:
  - If the hook has no `condition` field, or it is null/empty, treat the hook as executable
  - If the hook defines a non-empty `condition`, skip the hook and leave condition evaluation to the HookExecutor implementation
- For each executable hook, output the following based on its `optional` flag:
  - **Optional hook** (`optional: true`):
    ```
    ## Extension Hooks

    **Optional Pre-Hook**: {extension}
    Command: `/{command}`
    Description: {description}

    Prompt: {prompt}
    To execute: `/{command}`
    ```
  - **Mandatory hook** (`optional: false`):
    ```
    ## Extension Hooks

    **Automatic Pre-Hook**: {extension}
    Executing: `/{command}`
    EXECUTE_COMMAND: {command}

    Wait for the result of the hook command before proceeding to the Outline.
    ```
    After emitting the block above you MUST actually invoke the hook and wait for it to finish before continuing. Run it the same way you would run the command yourself in this agent/session (the invocation may differ from the literal `{command}` id shown above, e.g. a skills-mode agent runs it as `/skill:speckit-...` or `$speckit-...`). Emitting the block alone does not run the hook.
- If no hooks are registered or `.specify/extensions.yml` does not exist, skip silently

## Outline

1. **Setup**: Run `{SCRIPT}` from repo root and parse JSON for FEATURE_SPEC, IMPL_PLAN, FEATURE_DIR, BRANCH. For single quotes in args like "I'm Groot", use escape syntax: e.g 'I'\''m Groot' (or double-quote if possible: "I'm Groot").

2. **Load context**: Read FEATURE_SPEC and `/memory/constitution.md`. Load IMPL_PLAN template (already copied).

3. **Execute plan workflow**: Follow the structure in IMPL_PLAN template to:
   - Fill Technical Context (mark unknowns as "NEEDS CLARIFICATION")
   - Fill Constitution Check section from constitution
   - Evaluate gates (ERROR if violations unjustified)
   - Phase 0: Generate research.md (resolve all NEEDS CLARIFICATION)
   - Phase 1: Generate data-model.md, contracts/, quickstart.md
   - Re-evaluate Constitution Check post-design

## Mandatory Post-Execution Hooks

**You MUST complete this section before reporting completion to the user.**

Check if `.specify/extensions.yml` exists in the project root.
- If it does not exist, or no hooks are registered under `hooks.after_plan`, skip to the Completion Report.
- If it exists, read it and look for entries under the `hooks.after_plan` key.
- If the YAML cannot be parsed or is invalid, do not skip silently: tell the user that `.specify/extensions.yml` could not be read (include the parser error) and that no hooks were checked, including any mandatory (`optional: false`) hooks registered there, then continue to the Completion Report.
- Filter out hooks where `enabled` is explicitly `false`. Treat hooks without an `enabled` field as enabled by default.
- For each remaining hook, do **not** attempt to interpret or evaluate hook `condition` expressions:
  - If the hook has no `condition` field, or it is null/empty, treat the hook as executable
  - If the hook defines a non-empty `condition`, skip the hook and leave condition evaluation to the HookExecutor implementation
- For each executable hook, output the following based on its `optional` flag:
  - **Mandatory hook** (`optional: false`) — **You MUST emit `EXECUTE_COMMAND:` for each mandatory hook**:
    ```
    ## Extension Hooks

    **Automatic Hook**: {extension}
    Executing: `/{command}`
    EXECUTE_COMMAND: {command}
    ```
    After emitting the block above you MUST actually invoke the hook and wait for it to finish before continuing. Run it the same way you would run the command yourself in this agent/session (the invocation may differ from the literal `{command}` id shown above, e.g. a skills-mode agent runs it as `/skill:speckit-...` or `$speckit-...`). Emitting the block alone does not run the hook.
  - **Optional hook** (`optional: true`):
    ```
    ## Extension Hooks

    **Optional Hook**: {extension}
    Command: `/{command}`
    Description: {description}

    Prompt: {prompt}
    To execute: `/{command}`
    ```

## Completion Report

Command ends after Phase 1 design. Report branch, IMPL_PLAN path, and generated artifacts.

## Phases

### Phase 0: Outline & Research

1. **Extract unknowns from Technical Context** above:
   - For each NEEDS CLARIFICATION → research task
   - For each dependency → best practices task
   - For each integration → patterns task

2. **Generate and dispatch research agents**:

   ```text
   For each unknown in Technical Context:
     Task: "Research {unknown} for {feature context}"
   For each technology choice:
     Task: "Find best practices for {tech} in {domain}"
   ```

3. **Consolidate findings** in `research.md` using format:
   - Decision: [what was chosen]
   - Rationale: [why chosen]
   - Alternatives considered: [what else evaluated]

**Output**: research.md with all NEEDS CLARIFICATION resolved

### Phase 1: Design & Contracts

**Prerequisites:** `research.md` complete

1. **Extract entities from feature spec** → `data-model.md`:
   - Entity name, fields, relationships
   - Validation rules from requirements
   - State transitions if applicable

2. **Define interface contracts** (if project has external interfaces) → `/contracts/`:
   - Identify what interfaces the project exposes to users or other systems
   - Document the contract format appropriate for the project type
   - Examples: public APIs for libraries, command schemas for CLI tools, endpoints for web services, grammars for parsers, UI contracts for applications
   - Skip if project is purely internal (build scripts, one-off tools, etc.)

3. **Create quickstart validation guide** → `quickstart.md`:
   - Document runnable validation scenarios that prove the feature works end-to-end
   - Include prerequisites, setup commands, test/run commands, and expected outcomes
   - Use links or references to contracts and data model details instead of duplicating them
   - Do not include full implementation code, model/service/controller bodies, migrations, or complete test suites
   - Keep this artifact as a validation/run guide; implementation details belong in `tasks.md` and the implementation phase

**Output**: data-model.md, /contracts/*, quickstart.md

## Key rules

- Use absolute paths for filesystem operations; use project-relative paths for references in documentation
- ERROR on gate failures or unresolved clarifications

## Done When

- [ ] Plan workflow executed and design artifacts generated
- [ ] Extension hooks dispatched or skipped according to the rules in Mandatory Post-Execution Hooks above
- [ ] Completion reported to user with branch, plan path, and generated artifacts


## Plan size

Four instructions in the command above are replaced. Disregard them:

- "Phase 0: Generate research.md". Write `research.md` only when an unknown or a technology choice
  needs research the plan cannot settle in a line. Otherwise record the decision in `plan.md`.
- "Phase 1: Generate data-model.md". Write `data-model.md` only when the feature adds or changes
  stored data, or an entity with real fields and rules.
- "Phase 1: Generate contracts/". Write `contracts/` only when the feature adds or changes an
  interface that another component or party calls, and the interface is more than `plan.md` can
  state in a few lines.
- "Phase 1: Generate quickstart.md". Write `quickstart.md` only when validation needs steps the
  spec's acceptance scenarios do not give.

Size the plan to the feature. Leave out a plan template section that does not apply; write no
"N/A" and no filler in its place.

The completion report lists each file written and each file skipped, with a short reason.

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
