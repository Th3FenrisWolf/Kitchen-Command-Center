# Subagent model

Every command that dispatches a subagent reads this file. A subagent takes one of two routes.

| Dispatch | How the route is picked | What the Agent call passes |
|---|---|---|
| A whole job: a phase, a review, a lookup, a check run, a fix | the agent type descriptions | `subagent_type`, and no `model` |
| One `tasks.md` row inside `speckit.implement` | the row rules under One task row | `model` |

### A whole job

The bizstream plugin ships four agent types. A type sets the model, the effort and a hard turn
cap. A `model` or an `effort` on the Agent call overrides the value the type sets.

| `subagent_type` | Model, effort | `maxTurns` | Job |
|---|---|---|---|
| `bizstream:checker` | Haiku, low | 50 | runs prescribed checks and reports results |
| `bizstream:scout` | Sonnet, medium | 40 | finds facts, or applies decisions already taken |
| `bizstream:builder` | Sonnet, medium | 80 | changes code, CI or migration and keeps it working |
| `bizstream:architect` | Opus, high | 100 | spec, plan, tasks, review, red team, audit, root cause, decision research |

- A command that names the type for a role dispatches that type.
- Otherwise pick the type from the agent type descriptions.
- Start each brief with one sentence that names the job: what the agent decides, changes or
  checks.
- Each plugin type turns off the Skill tool and the session tools, such as Agent, Artifact and
  AskUserQuestion. The skill list alone measured about 12K tokens on every turn (2026-10-14). The agent reads a
  command's `SKILL.md` instead, and a brief names the path of any other skill it needs. A
  dispatch without the plugin, or a same-named project type, keeps the Skill tool.
- Before each dispatch, read the description of each skill in this session's skill list,
  including the skills of other plugins. When a description matches the job, such as a skill
  that says to read it before building a widget, name the skill's `SKILL.md` path in the brief.
  A project skill is at `.claude/skills/<skill>/SKILL.md`. A plugin skill `<plugin>:<skill>` is
  at `<installPath>/skills/<skill>/SKILL.md`. Get `installPath` from the `<plugin>@...` entry in
  `~/.claude/plugins/installed_plugins.json`; when the plugin has more than one entry, use the
  project entry for this project before the user entry.
- When the job holds a judgment, such as a recommendation or a verdict, use `architect`.
- Pass no `model` and no `effort` with a type, except in two cases. Pass `model: sonnet` on an
  architect that only finds review defects, a **reviewer**; the architect that confirms or
  verifies its findings passes no `model`. Pass `model: opus` on a builder in two cases:
  its job must find something outside the paths the brief names, or it is the second dispatch
  after `blocked` or `failed`.
- A phase that holds a row routed to `opus` splits. That row, with each row that
  shares a file with it, goes to its own builder with `model: opus`. The rest of the phase goes to
  a builder with no `model`. Dispatch them in row order. One `opus` row never moves a whole phase
  to Opus.
- An agent that reaches its cap returns a partial result. Resume it one time with `SendMessage`.
  A second cap counts as `blocked`.
- Without the plugin, pass the model of the type in the table as `model`.
- A project type of the same name in `.claude/agents/` wins. Pass its bare name.

A teammate is an Agent call that carries `name`, and only `speckit.implement` starts one, one per
lane of a `tasks.md` phase. It takes the highest `model` routed among its lane's rows. A teammate gets the lane's files as its write scope and no other file. A teammate has
no `maxTurns` cap: when it stalls, stop it, then run its open rows in subagent mode.

Give a builder one `tasks.md` phase or one pull request per dispatch, never a whole card. After
about 60 tool calls it commits the finished work, writes a state note where the project keeps its
card notes, and hands back. Dispatch a fresh builder that starts from the state note. A long run
in one agent reads its grown context again on every call.

### One task row

Route each `tasks.md` row yourself, in this order, and act on the outcome.

| Outcome | Action |
|---|---|
| `none` | Skip the row. It waits on other work. |
| `owner` | Stop and ask the owner. Never dispatch it. |
| `code` | Run it on the main thread with no subagent. |
| `sonnet`, `opus` | Pass it as `model` on the Agent call. |

1. A `[WAITS]` row is `none`.
2. An `[OWNER]` row, a row that names a Q number, a deploy, or the administration interface is `owner`.
3. A row that only creates a directory is `code`.
4. A row that changes, removes, renders, writes tests for, or wires into existing code outside the paths it names is `opus`.
5. Every other row is `sonnet`.

Pass `model` on every task-row Agent call. Sonnet is the lowest task-row tier. Haiku runs only
as the checker type. Never pass `haiku` as `model` on a task row.
Give two rows that share a file to one subagent, at the higher tier.
