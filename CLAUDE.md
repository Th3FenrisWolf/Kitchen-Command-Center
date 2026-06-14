# Kitchen Command Center

Project-level instructions for Claude Code.

## Superpowers: plans & specs

Save Superpowers **plans** and **specs** under `.superpowers/`, not under `docs/superpowers/`. This keeps all superpowers files together in the same location.

- **Plans** → `.superpowers/plans/YYYY-MM-DD-<slug>.md`
- **Specs / design docs** → `.superpowers/specs/YYYY-MM-DD-<slug>.md`

A plan and the spec it implements **must share the identical `<slug>` only** — same slug, *potentially* different date. When writing a plan, derive its filename from its spec, updating the date to be the new current date; don't coin a new slug. Example: spec `2026-05-20-recipes.md` ↔ plan `2026-05-21-recipes.md`.

This overrides the default locations and filename placeholders baked into the `writing-plans` and `brainstorming` skills (`docs/superpowers/plans/` and `docs/superpowers/specs/`), both of which explicitly defer to user preferences for file location.

**`.superpowers/` is gitignored** (see `.gitignore`). The plan, spec, and brainstorm files are local scratch only — do **not** stage or commit them while working through a feature, and don't be surprised when they don't appear in `git status`. Leave them out of every commit.
