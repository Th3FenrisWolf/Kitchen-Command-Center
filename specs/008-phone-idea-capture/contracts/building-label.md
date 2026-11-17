# Contract: Building label

Three parties share it: card implement and bug fix add the label, `pickup.mjs` tells them which issue to add it to,
and the sync reads it and takes it off. The owner may add or take it off by hand.

## The label

- Name: `building`, exactly, in this repository. `derive.mjs` exports it as `BUILDING_LABEL`, and the workflow's
  literal is tested equal to it.
- The owner made it once, on 2026-11-16 (plan D32). Nothing else makes or deletes it.
- It means something only on the issue of a card that carries a Key. On an owner idea, or any other issue, the sync
  leaves it alone (FR-004, FR-019).

## What the sync does with it

| Card's derived stage | Label on its issue | The sync |
|---|---|---|
| Specified | yes | writes Status Building |
| Specified | no | writes Status Specified, as before this feature |
| Building, In review or Done | yes | writes the derived stage and takes the label off |
| Building, In review or Done | no | writes the derived stage, as before this feature |

The dry run prints a removal as `unlabel building` in the card's update line. An archived Key'd card is left untouched
(plan D4).

## The workflow

An `issues` event of type `labeled` or `unlabeled` starts a board update only when `github.event.label.name` is
`building` and the issue's author is the repository owner. Every other label event takes a concurrency group of its
own and skips its job, so it never replaces a waiting update (FR-021, plan D30).

## `node tools/project-board/pickup.mjs <key>`

Finds the issue of one card for `/speckit-card-implement` and `/speckit-bug-fix`. `<key>` is `FEATURE_DIR`'s folder
name for card implement and `bug/<BUG_SLUG>` for bug fix, and must match
`^(?:\d{3}-[a-z0-9]+(?:-[a-z0-9]+)*|bug/[a-z0-9]+(?:-[a-z0-9]+)*)$`; anything else exits non-zero with
`<key> is not a card key` on stderr, and a missing argument with `no card key given`.

It prints one JSON object on stdout and exits 0:

```json
{ "key": "009-script-hydration-cost", "number": 152, "repository": "Th3FenrisWolf/Kitchen-Command-Center" }
```

or a refusal:

```json
{ "key": "011-example", "refused": "no card on the board carries the Key 011-example" }
```

| Refusal | When |
|---|---|
| `no card on the board carries the Key <key>` | No board item carries the Key, as for a spec never pushed |
| `<key>'s card is a draft, not an issue` | The item is a draft |
| `<key>'s card is not an issue` | The item is neither an issue nor a draft, such as a pull request |
| `<key>'s card is archived` | The only item with the Key is archived |
| `<key>'s issue is closed` | The card's issue is closed |

It exits non-zero, with a message on stderr, when `gh` is signed in as an account other than the board owner
(`ideaViewerError`'s message), or a read fails. It reads no issue title or body, and writes nothing.

## The commands' step

Card implement, between Phase 1 and Phase 2, runs `pickup.mjs <FEATURE_DIR's folder name>`. Bug fix, between its
Execution steps 1 and 2 (the plan confirmed, the remediation not yet applied), runs `pickup.mjs bug/<BUG_SLUG>`. With
a `number`, either runs:

```sh
gh issue edit <number> --repo <repository> --add-label building
```

Only the number and the repository from the JSON, and the label, go into the command. A refusal, a non-zero exit or a
failed edit is reported in one line, and the command carries on. Neither pushes a branch for the label.
