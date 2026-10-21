# Jev

Jev takes a state and a typed question. It returns a typed answer with a probability. It
generates no text. This file holds every Jev rule.

`<jev>` below is `.specify/extensions/jev` in a Spec Kit project, or
`${CLAUDE_PLUGIN_ROOT}/extensions/jev` from the bizstream-ai plugin.

## The switch

`decide` is a switch statement whose default case asks Jev.

1. Each `floor` case is a regex over the state. The first match returns its `then` outcome, and
   Jev is never called.
2. The residue gets one Jev question. A noul maps `pass` and `fail` through `then`. A choice
   returns its option.
3. A value inside the `band`, a split sample, a confidence below `minConfidence`, and a missing
   `JEV_API_KEY` each return the `fallback`. With no `fallback`, the outcome is `review`.

Run it from the project root:

```
echo "<state>" | node "<jev>/scripts/decide.mjs" <question-id>
node "<jev>/scripts/decide.mjs" <question-id> <state-file>
grep -E '^\s*- \[ \] T[0-9]+' tasks.md | node "<jev>/scripts/decide.mjs" <question-id> - --lines
```

`--lines` asks once for each non-empty line and prints one JSON line for each, with the line as
`state`. Otherwise it prints one JSON line: `outcome`, `via` (`floor`, `jev` or `fallback`), `reason`, and the
`verdict` and `confidence` when Jev answered. Act on `outcome`. Treat `review` as a stop that a
person or Opus reads.

From code, import it:

```js
import { decide } from "<jev>/scripts/decide.mjs";
import { findQuestion } from "<jev>/scripts/core.mjs";
const { outcome } = await decide(state, findQuestion("subagent-tier"));
```

## What Jev decides

| Kind of decision | Owner |
|---|---|
| An atomic judgment a well-informed person makes quickly from supplied state | Jev |
| Inference chained across steps, where code holds the loop and the state | Jev inside a loop |
| Inference that needs a reasoner to hold the whole problem at once | Opus |
| A question with no factual answer, such as a ledger decision | the project owner |
| A lookup: what a package, a browser or a repository contains | code |
| Any text output | Opus |

Never route an owner decision, an irreversible action or a trust boundary through Jev. Write a
`floor` case that catches each one.

## The switch test

A question enters a bank only when both statements hold.

1. The state holds everything the answer needs. Jev cannot read the repository.
2. The question asks for one determination and no lookup.

Add a missing fact to the state, or move the question to code.

## Banks

| Bank | Path | Cases |
|---|---|---|
| Shipped | `<jev>/scripts/questions.json` | `<jev>/scripts/calibration.jsonl` |
| Project | `.claude/jev/questions.json` | `.claude/jev/calibration.jsonl` |

The loader merges both. A project entry overrides the shipped entry with the same id. The steers
are the exception: `steer.mjs` reads the shipped bank only. Add a project question to the project
bank. Change a shipped question in the bizstream.ai repository.

| Field | Meaning |
|---|---|
| `id` | Stable name. A verdict record cites it. |
| `scope` | The artifact the question runs against, such as `comment`, `prose`, `task-row`. |
| `primitive` | `noul`, `choice` or `score`. |
| `text` | The question. One determination only. |
| `criteria` | The rubric. A noul takes `{ "true", "false" }`. A choice takes a map of option to rubric. A score takes 2 to 10 ordered levels. |
| `band` | `[low, high]` for a noul. A probability inside it, edges included, returns `review`. |
| `failWhen` | `"true"` or `"false"`. The truth value that fails. |
| `samples` | How many times to ask. Three for a blocking question. |
| `floor` | `[{ "match", "then", "flags" }]`. Code cases that run before Jev. `flags` defaults to `i`. |
| `then` | A noul only. `{ "pass": outcome, "fail": outcome }`. |
| `fallback` | The outcome for every unsure answer. Defaults to `review`. |
| `minConfidence` | A choice or score answer below it takes the fallback. |
| `blocking` | `true` lets the question stop work. The calibration bar applies. |
| `rule` | The rule the question enforces. |
| `hash` | Stamped. It covers `primitive`, `text`, `criteria`, `band` and `failWhen`. |

`floor`, `then`, `fallback`, `minConfidence`, `blocking` and `rule` never reach the model, so an
edit to them keeps the recorded verdicts.

## Author a question

- Write one determination. Decompose anything larger and combine the answers in code.
- Write `criteria` on every question, with a rubric for each outcome.
- Set `failWhen` and `band` on every noul.
- Send only the state the question needs.
- Give a blocking question `samples: 3` and a `fallback` that is the safe side.
- Prefer a wrong `review` to a wrong `pass`. Set the fallback to the higher tier, except where a
  question's own section sets it.
- Never ask Jev to produce text.

## Calibrate before a question blocks

A case is one JSONL line: `{"question": id, "expected": verdict, "state": "..."}`. A blocking
question needs 20 cases and zero confident wrong answers.

```
node "<jev>/scripts/calibrate.mjs" --offline              # banks valid, case counts
node "<jev>/scripts/calibrate.mjs"                        # agreement, needs JEV_API_KEY
node "<jev>/scripts/calibrate.mjs" --stamp <bank path>    # rewrite hashes after an edit
```

**Warning**: read the missed case before you change the question. A miss has three causes.

| Cause | Fix |
|---|---|
| The label is wrong | Correct the case. |
| The question text is broader than its rule | Narrow the text. Criteria cannot rescue a question that asks the wrong thing. |
| The state lacks a fact | Add the fact to the state, or move the question to code. |

## Subagent model

A subagent takes one of two routes.

| Dispatch | How the route is picked | What the Agent call passes |
|---|---|---|
| A whole job: a phase, a review, a lookup, a check run, a fix | the agent type descriptions | `subagent_type`, and no `model` |
| One `tasks.md` row inside `speckit.implement` | `subagent-tier` | `model` |

### A whole job

The bizstream-ai plugin ships four agent types. A type sets the model, the effort and a hard turn
cap. A `model` or an `effort` on the Agent call overrides the value the type sets.

| `subagent_type` | Model, effort | `maxTurns` | Job |
|---|---|---|---|
| `bizstream-ai:checker` | Haiku, low | 50 | runs prescribed checks and reports results |
| `bizstream-ai:scout` | Sonnet, medium | 40 | finds facts, or applies decisions already taken |
| `bizstream-ai:builder` | Sonnet, medium | 120 | changes code, CI or migration and keeps it working |
| `bizstream-ai:architect` | Opus, high | 100 | spec, plan, tasks, review, red team, audit, root cause, decision research |

- A command that names the type for a role dispatches that type.
- Otherwise pick the type from the agent type descriptions. Ask Jev nothing before a dispatch.
- Start each brief with one sentence that names the job: what the agent decides, changes or
  checks.
- Each plugin type turns off the Skill tool and the session tools, such as Agent, Artifact and
  AskUserQuestion. The skill list alone measured about 12K tokens on every turn (2026-10-14). The agent reads a
  command's `SKILL.md` instead, and a brief names the path of any other skill it needs. A
  dispatch without the plugin, or a same-named project type, keeps the Skill tool.
- When the job holds a judgment, such as a recommendation or a verdict, use `architect`.
- Pass no `model` and no `effort` with a type, except in two cases. Pass `model: sonnet` on an
  architect that only finds review defects, a **reviewer**; the architect that confirms or
  verifies its findings passes no `model`. Pass `model: opus` on a builder in two cases:
  its job must find something outside the paths the brief names, or it is the second dispatch
  after `blocked` or `failed`.
- A phase that holds a row `subagent-tier` routed to `opus` splits. That row, with each row that
  shares a file with it, goes to its own builder with `model: opus`. The rest of the phase goes to
  a builder with no `model`. Dispatch them in row order. One `opus` row never moves a whole phase
  to Opus.
- An agent that reaches its cap returns a partial result. Resume it one time with `SendMessage`.
  A second cap counts as `blocked`.
- Without the plugin, pass the model of the type in the table as `model`.
- A project type of the same name in `.claude/agents/` wins. Pass its bare name.

A teammate is an Agent call that carries `name`, and only `speckit.implement` starts one, one per
lane of a `tasks.md` phase. It takes the highest `model` that `subagent-tier` routed among its
lane's rows. A teammate gets the lane's files as its write scope and no other file. A teammate has
no `maxTurns` cap: when it stalls, stop it, then run its open rows in subagent mode.

`agent-tier` is an audit, never a dispatch step. It re-classifies past dispatches offline and flags
each one whose type differs from the type used. Its state is the description line, then the first
600 characters of the brief. Run `node "<jev>/scripts/decide.mjs" agent-tier <state-file>` for each
past dispatch. Read each flag before you change an agent description: the dispatch may be right and
the outcome wrong.

Give a builder one `tasks.md` phase or one pull request per dispatch, never a whole card. After
about 100 tool calls it commits the finished work, writes a state note where the project keeps its
card notes, and hands back. Dispatch a fresh builder that starts from the state note. A long run
in one agent reads its grown context again on every call.

### One task row

Route a `tasks.md` row with `subagent-tier`, and act on the outcome.

| Outcome | Action |
|---|---|
| `none` | Skip the row. It waits on other work. |
| `owner` | Stop and ask the owner. Never dispatch it. |
| `code` | Run it on the main thread with no subagent. |
| `sonnet`, `opus` | Pass it as `model` on the Agent call. |
| `review` | Treat it as `sonnet`. |

`subagent-tier` routes one `tasks.md` row. The floor sends `[WAITS]` to `none`, `[OWNER]`, a Q
number, a deploy or the administration interface to `owner`, and a create-directory row to `code`.
Jev asks whether the row changes, removes, renders, writes tests for, or wires into existing code
outside the paths it names: `fail` routes to `opus`, `pass` and every unsure answer to `sonnet`.

Pass `model` on every task-row Agent call. Sonnet is the lowest task-row tier. Haiku runs only
as the checker type. Never pass `haiku` as `model` on a task row.
Give two rows that share a file to one subagent, at the higher tier.

## Owner steering

The plugin runs `<jev>/scripts/steer.mjs` on five hook events. Each steer decides whether a
session needs its owner. A floor match, an unsure answer, an error and a missing key each keep
what the session does today. The steer questions come from the shipped bank only, so a project
bank cannot change when a session needs its owner.

| Event | Subcommand | Question | Outcome in `on` mode |
|---|---|---|---|
| `Stop` | `stop` | `steer-stopped-short` | `continue` blocks the stop, and the agent carries on. |
| `PreToolUse`, `AskUserQuestion` | `ask` | `steer-owner-takes-default` | `answer` on every question fills `answers` with the recommended labels, and the dialog never shows. |
| `PostToolUse`, `AskUserQuestion` | `ask-result` | none | Records what the owner chose. |
| `UserPromptSubmit` | `prompt` | `owner-intent`, `owner-routine`, `owner-fault` | A correction adds context that tells the agent to write the rule. |
| `SessionStart` | `session` | none | Adds `templates/steer-session.md` and lists every failed probe. |

### Modes

- `off` runs nothing.
- `shadow` logs each decision and changes nothing.
- `on` acts on the outcome.

The stop, ask and prompt steers default to `shadow`, and the session steer to `on`. The prompt steer
stays in `shadow` until `owner-intent`, `owner-routine` and `owner-fault` each have 20 cases. A steer
that asks Jev is `off` when `JEV_API_KEY` is unset or the config sets `"jev": false`.

The prompt steer reads only what the owner typed. It skips task notifications, agent and teammate
messages, subagent hand-backs and local-command echoes, with no Jev call and no log row.

### Configure it

Settings merge in this order: the plugin defaults, `~/.claude/steer.json`, the project's
`.claude/steer.json`, then the environment. `mode` merges per event. Every other key replaces.

```json
{
  "mode": { "stop": "shadow", "ask": "shadow", "prompt": "shadow", "session": "on" },
  "jev": true,
  "floors": ["\\bGATED\\b"],
  "neverHeaders": ["Scope"],
  "probes": [{ "name": "Azure", "run": "az account show", "fix": "az login" }]
}
```

- `floors` are regexes, and a match keeps the owner in the loop on the stop and ask steers.
- `neverHeaders` sends an `AskUserQuestion` with that header to the owner.
- `probes` replaces the default probe list, which is `gh auth status`. Only `~/.claude/steer.json`
  sets it. A project file cannot, because a probe is a shell command.
- `BIZSTREAM_STEER=off|shadow|on` sets every steer for one session.
  `BIZSTREAM_STEER_<EVENT>` sets one steer, for example `BIZSTREAM_STEER_ASK=on`.

### The log and the cases

Every steer appends one line to `~/.claude/steer/log.jsonl`. The log stays on the machine. It
holds redacted prompt text, and it never enters this repository.

- `node <jev>/scripts/steer.mjs report [days]` prints the routine share, the intents, the
  corrections by fault, and how each shadow steer compares with what the owner did.
- `node <jev>/scripts/steer.mjs export-cases [days]` prints calibration cases: each answered
  question, and each shadow stop that the owner's next prompt labels.
- Read every exported case before it leaves the machine. Add the cases you keep to
  `<jev>/scripts/calibration.jsonl` in a pull request to `BizStream/bizstream.ai`.
- Relabel each `steer-stopped-short` case from the question's criteria before you keep it. Its export
  label comes from the owner's next prompt and is often wrong.
- Turn a steer `on` only after its shadow log agrees with the owner.

`redact()` removes keys, tokens, connection strings and email addresses before any text reaches
Jev or the log.

## History reader

`<jev>/scripts/history.mjs` reads every session transcript in `~/.claude/projects` and the steer
log. It classifies each human turn through the three `history-*` questions, writes a baseline
report, and lists decision points for new questions. Every file it writes lives in
`~/.claude/steer/history/`. No transcript text enters a repository, except as a scrubbed row the
owner has read.

Each subcommand prints one JSON line. The target is the command argument, resolved in this order:

1. `report`. Run step 4 only, on the files a past run left.
2. `candidates`. Run steps 5 to 9 only.
3. Anything else, or nothing. Run every step.

Every step that calls Jev (`classify` in step 2, `check` in step 4) needs the owner's confirmed
cost first, also when the target is `report`. `scan` and `classify` take `--only <dir>[,<dir>]`,
an allow-list of project directory names from `scan`; pass the same list to both.

1. Run `node "<jev>/scripts/history.mjs" scan`. Show the owner every directory in `projects`
   with its sessions and turns, the totals and `costEstimateUsd` as "at least" that amount
   (`costNote` says why it is a lower bound). Ask the owner to confirm the scope and the cost,
   or to name the directories to keep, and run `scan --only <dirs>` again for a narrowed list.
   Wait for the confirmation before step 2.
2. Run `classify`. On exit 1, report the error and stop. A session with an error stays out of the
   index, so the next run reads it again.
3. Run `sample`. Put each item to the owner with the question affordance, with up to three
   questions per item:
   - `history-turn-class`: `lookup`, `decision`, `misread`, `late-steer` or `other`.
   - `history-task-start`: `pass` if the turn starts new work, else `fail`.
   - `history-task-autonomous`, only when the item has `autonomous`: `pass` if the task could
     reach its outcome with no further human turn, else `fail`.

   Show Jev's answer as the first option. Run `answer <id> <class> <taskStart> [autonomous]` for
   each item.
4. Confirm the cost of `check` with the owner. Run `check`, then `report`. Show the owner the report path, `provisional` and `gate`. Below 80%
   agreement, name the classifier whose rubric must change. Agreement counts only `random` items.
5. Run `points`. Group the points into candidate questions, as "Author a question" says. Mark a
   candidate `generic` when its points come from two or more `repo` keys that do not start with
   `unresolved:`, and its text names nothing from one repository. Mark every other candidate
   `specific`, bound for that repository's `.claude/jev/questions.json`.
6. Record each candidate: pipe `{scope, repo, question, rows, points}` into
   `candidate set <id> proposed`.
7. Before a row goes to the shipped bank, rewrite it in neutral terms and write the rows to a
   scratch JSONL file. This applies to the rows of a generic candidate and to `cases.jsonl`. Run
   `scrub <file>`. Show the owner every passed row and the count of rejected rows. Drop each row the
   owner drops. A specific candidate skips this step.
8. Put each candidate to the owner. Run `candidate set <id> approved` or
   `candidate set <id> rejected`. `points` never lists the points of a rejected candidate again.
9. For each approved candidate, write the question and its rows to the target bank. Run
   `calibrate.mjs` from the target repository root. A candidate passes with 20 or more cases and no
   miss, blocking or not. Run `candidate set <id> calibrated` on a pass, else
   `candidate set <id> failed <reason>`. On a pass, stamp the bank with `--stamp`. A generic
   candidate reaches `<jev>/scripts/` only in a pull request to `BizStream/bizstream.ai`.

**Warning**: never paste `points`, `sample` or `scrub` output into a repository, an issue or a
pull request. That output holds redacted transcript text. Only the rows the owner read in step 7
may leave the machine.

Done when:

- [ ] The owner confirmed the scope and the cost before any request reached Jev
- [ ] The report path, `provisional` and `gate` are in the reply
- [ ] Every row bound for the shipped bank passed `scrub` and the owner read it
- [ ] No candidate joined a bank without owner approval and a calibration pass

The report stays provisional until three conditions hold: owner agreement is 80% or more, at
least 30 `random` items are answered, and `check` passes all three `history-*` questions.
`report` puts the Option B gate in the report. The gate opens when misread turns are 10% or more
of decided turns, or when the median misread touches more than one card.

## Bump the model

**Warning**: a model bump invalidates every recorded verdict. Follow these steps in order.

1. Read the jaggedness page for the new version.
2. Change `MODEL` in `<jev>/scripts/core.mjs` and `model` in each bank. Pin the full version. Never pin
   `jev-latest`.
3. Run `calibrate.mjs` and compare the agreement rate for each question.
4. Rewrite or drop every question that lost accuracy.
5. Run `calibrate.mjs --stamp` on each bank.
6. Commit the pin, the banks and the new rates together.

`core.mjs` raises when the served model differs from the pin. It retries a 429 and a 529 up to
five times, and raises a 401 and a 422 at once.
