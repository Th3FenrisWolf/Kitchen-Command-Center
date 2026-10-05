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

The loader merges both. A project entry overrides the shipped entry with the same id. Add a
project question to the project bank. Change a shipped question in the bizstream.ai repository.

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
- Prefer a wrong `review` to a wrong `pass`. Set the fallback to the higher tier.
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

Route a subagent with `subagent-tier`, and act on the outcome.

| Outcome | Action |
|---|---|
| `none` | Skip the row. It waits on other work. |
| `owner` | Stop and ask the owner. Never dispatch it. |
| `code` | Run it on the main thread with no subagent. |
| `sonnet`, `opus` | Pass it as `model` on the Agent call. |
| `review` | Treat it as `opus`. |

`subagent-tier` routes one `tasks.md` row. The floor sends `[WAITS]` to `none`, `[OWNER]`, a Q
number, a deploy or the administration interface to `owner`, and a create-directory row to `code`.
Jev asks whether the row needs a file outside the paths it names: `pass` routes to `sonnet`, every
other answer to `opus`.

Pass `model` on every Agent call. Sonnet is the lowest tier. Never route a subagent to Haiku.
Give two rows that share a file to one subagent, at the higher tier.

## Owner steering

The plugin runs `<jev>/scripts/steer.mjs` on five hook events. Each steer decides whether a
session needs its owner. A floor match, an unsure answer, an error and a missing key each keep
what the session does today.

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

The stop and ask steers default to `shadow`, and the prompt and session steers to `on`. A steer
that asks Jev is `off` when `JEV_API_KEY` is unset or the config sets `"jev": false`.

### Configure it

Settings merge in this order: the plugin defaults, `~/.claude/steer.json`, the project's
`.claude/steer.json`, then the environment. `mode` merges per event. Every other key replaces.

```json
{
  "mode": { "stop": "shadow", "ask": "shadow", "prompt": "on", "session": "on" },
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
- Turn a steer `on` only after its shadow log agrees with the owner.

`redact()` removes keys, tokens, connection strings and email addresses before any text reaches
Jev or the log.

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
