---
description: Show the owner how a feature will look or how its parts fit together, in rounds, before the plan or the tasks fix it. Use when the owner wants to see a proposal, compare directions, or iterate on look and feel or architecture, for example "mock it up", "show me", "preview the look", "try a few layouts" or "diagram the architecture".
---

# Preview

Put a proposal on screen, take the owner's reaction, and answer it with the next round. A round is one new
file, so iterate freely. Nothing here touches production code: the rounds live in `.mockups/`, which git
ignores, and only the approved round reaches the spec.

`$ARGUMENTS` picks the mode:

- `look`: the screens, layout and feel of the spec's public-site surfaces.
- `architecture`: how the parts fit: components, data flow, requests and storage.
- Anything else names the focus of the round, for example `the empty state`.
- With no mode named: `look` while the feature has no `plan.md`, `architecture` once it has one.

## Setup

1. Resolve the feature: `.specify/scripts/bash/check-prerequisites.sh --json --paths-only`. Read `spec.md`
   with its Clarifications, and `plan.md`, `research.md` and `contracts/` where they exist. With no active
   feature, name the round folder after the topic, and at approval tell the owner which round to cite in
   `/speckit-specify`.
2. Run `.specify/extensions/preview/scripts/preview-setup.sh <folder>`, where the folder is the feature
   directory's name. It links the site's built stylesheet, fonts and logos into `.mockups/`, writes the round
   templates into `.mockups/<folder>/`, and prints that path. It stops when no build exists or a stylesheet
   source is newer than the build. Then run `yarn build:all` in the checkout it names, and run it again.
3. Open the Browser pane with `preview_start` and the `preview` configuration in `.claude/launch.json`. It
   serves `.mockups/` at `http://localhost:4310/`.

## A look round

Read `docs/brand/kit.md` before the first look round. Constitution principle II holds in every round.

1. Copy `.mockups/<folder>/_round.html` to `.mockups/<folder>/round-<n>-<topic>.html`. Never write over an
   earlier round, so the owner can go back to it.
2. Set the title, and write two or three options. Each option is a `<section class="pv-option">` with an `id`
   and a `<header>` that names its direction, its axis, when it wins and what it costs. The bar holds one jump
   link per option.
   - The options differ on a named axis: layout, density, hierarchy or interaction. Name each one for its
     direction, never Option A, B or C. Two options that differ only in colour or copy are one option.
   - Build with the kit: `kcc-*` classes, role tokens through `var(--color-*)`, the tear presets and the
     washes. The build emits only the Tailwind utilities that the site's source already uses, so a utility
     the site does not use has no rule. Put the round's own layout in its `<style>` block, with a `pv-` prefix.
   - Use real content that fits the spec: recipe names, categories, labels and counts.
3. Check the round before the owner sees it. Navigate the pane to it, read the console for errors, and look at
   it in both ramps.
4. Tell the owner what is on screen in two lines at most, give the URL, and stop. The reaction is the input to
   the next round.
5. The next round answers the reaction. It diverges around the direction the owner picked, or fixes what the
   owner named, in a new file with the next number.

## An architecture round

1. Copy `.mockups/<folder>/_architecture.html` to `.mockups/<folder>/arch-<n>-<topic>.html`.
2. Write one `<pre class="mermaid">` block per diagram: a flowchart for components and data flow, a sequence
   diagram for a request, an entity diagram for storage. Use the files, classes and endpoints the plan names.
3. Show it and stop, as for a look round.

## Approval

When the owner approves a round:

- **Look.** Save the approved option alone as `.mockups/<folder>/approved-<topic>.html`. Run
  `.specify/extensions/preview/scripts/preview-shot.sh http://localhost:4310/<folder>/approved-<topic>.html
  FEATURE_DIR/mockups/<topic>`, which writes `<topic>-light.jpg` and `<topic>-dark.jpg`, then copy the file to
  `FEATURE_DIR/mockups/<topic>.html`. Write `FEATURE_DIR/mockups/README.md`: the approved option and why it
  won, each other option with the reason the owner gave against it, and anything still open. Then add one line
  under today's `### Session YYYY-MM-DD` in the `## Clarifications` section of `spec.md`, creating either when
  it is missing: `- Q: Which <topic> direction? → A: <option>; see mockups/README.md`. The plan reads the
  Clarifications, so it builds the approved look.
- **Architecture.** Put the approved diagrams into `plan.md` under `## Architecture`, after
  `## Technical Context`, as `mermaid` code fences. Correct any plan text that the diagrams contradict.

Leave the commit to the next Spec Kit command's commit hook. Stop the server with `preview_stop` when the
owner is done.

## Guardrails

- Never edit a file under `src/`. A preview shows a proposal, and card implement builds it.
- Never stage `.mockups/`. It is scratch, and it links the build output.
- A round shows options. The owner picks one.
