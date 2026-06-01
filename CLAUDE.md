# Kitchen Command Center

Project-level instructions for Claude Code.

## Superpowers: plans & specs location

Save Superpowers **plans** and **specs** under `.superpowers/`, not under `docs/superpowers/`. This keeps them alongside the brainstorm scratch files in `.superpowers/brainstorm/`.

- **Plans** → `.superpowers/plans/YYYY-MM-DD-<slug>.md`
- **Specs / design docs** → `.superpowers/specs/YYYY-MM-DD-<slug>-design.md`

A plan and the spec it implements **must share the identical `YYYY-MM-DD-<slug>` stem** — same date, same slug — with the spec appending `-design`. When writing a plan, derive its filename from its spec by dropping the `-design` suffix; don't use the plan's creation date or coin a new slug. Example: spec `2026-05-20-recipes-design.md` ↔ plan `2026-05-20-recipes.md`.

This overrides the default locations and filename placeholders baked into the `writing-plans` and `brainstorming` skills (`docs/superpowers/plans/` and `docs/superpowers/specs/`), both of which explicitly defer to user preferences for file location.
