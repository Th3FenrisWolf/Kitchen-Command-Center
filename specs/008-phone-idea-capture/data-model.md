# Data Model: Phone Idea Capture

Nothing new is stored. The sync derives everything on each run from the repository, the owner's issues and the
board. These are the shapes `tools/project-board/derive.mjs` works with.

## Owner issue (read)

| Field | Source | Rule |
|---|---|---|
| `id` | `Issue.id` | Joins to a board item's `contentId` |
| `number` | `Issue.number` | The only part of the issue the sync prints |
| `state` | `Issue.state` | `OPEN` or `CLOSED` |
| `body` | `Issue.body` | Kept in memory for an off-board takeover's body merge (FR-012), never printed |
| `synced` | `body` contains `<!-- sync -->` | Separates the sync's own issues from the owner's ideas |

Read only for issues `createdBy` the board owner (`board.json` `owner`).

## Board item (read, extended)

Today's item gains `archived` (`ProjectV2Item.isArchived`), and the read includes archived items (research R1). An
issue's item also gains `labels`, `[{ id, name }]` from `Issue.labels`; a draft's or a pull request's is `[]` (plan
D25, research R8).

When several items carry one Key, `planChanges` matches the card to the first unarchived one in read order, else the
first archived one (plan D19), so an archived duplicate never stands in for the live card. A claim's rule 1 below
uses the same match.

## Owner idea (derived)

An owner issue that is not `synced` and whose board item, if any, has no `Key`.

| State | Item | Sync action |
|---|---|---|
| Open | none | **add**: add the issue to the board, set Status Idea, Kind Idea, Iteration Current |
| Open | archived | **restore**: unarchive, then fill blanks |
| Open | on the board, a blank among Status, Kind, Iteration | **fill**: set only the blank ones |
| Open | on the board, all three set | none |
| Closed | on the board | **archive** |
| Closed | none, or archived | none |

A claimed idea (below) takes none of these actions: the card it becomes owns it.

## Idea line (read from a spec or assessment)

`parseIdeaLine(markdown)` returns the issue number of the first line matching
`^(?:- )?\*\*Idea\*\*:\s*\[?#(\d+)`, or null. The grammar is in [contracts/idea-link.md](contracts/idea-link.md). A
spec or bug card carries it as `idea`, read from the snapshot the card already uses.

## Claim (derived)

`claimIdeas(cards, ideas, items)` matches each card that names an idea:

1. A card already matched by its Key or an alias keeps its item and claims nothing. If it names an idea and the
   matched item's `contentId` is not the named issue's `id`, a warning names both (plan D18). A card whose item is
   the idea it names, as after its own takeover, warns nothing. A named issue missing from the owner-issue read has
   no `id` to match, so a card naming one warns.
2. An idea that is not an owner idea (another author, `synced`, a Key'd item, no such issue) is claimed by nobody.
   Each card naming it gets its own card and a warning.
3. Among the remaining cards naming one idea, the one whose key sorts first (`localeCompare`) claims it. Each other one
   gets its own card and a warning naming the winner and the issue.

A claim gives `planChanges` an item for the card: the idea's board item, or, when the idea is not on the board, an
item built from the owner issue (below).

## Card takeover (an update op, extended)

| Idea item | Op |
|---|---|
| On the board | `update` as today (Key, title, synced block above the owner's text, fields, open or closed state, parent) |
| Archived | the same, with `unarchive: true`, applied before any other write |
| Not on the board | `update` with `adopt: true`: the issue is added to the board first, and `Iteration: Current` is set as on create |

The item an adopt op is planned against (plan D22):

| Field | Value | Effect in `planChanges` |
|---|---|---|
| `type` | `"Issue"` | Editable, so the title and body are written, and the state and parent are compared |
| `contentId` | the owner issue's `id` | The issue `addProjectV2ItemById` adds |
| `state` | the owner issue's `state` | `state` is written only when it differs from the card's: a closed idea reopens unless the card is Done |
| `body` | the owner issue's `body` | The synced block is merged above the owner's text |
| `fields` | empty | Every card field is written |
| `id`, `title`, `parentId` | none | The new item's id comes from the add; the title and the parent link are written |

Once written, the item carries the card's Key, so the issue is no longer an owner idea and later runs match it by Key.

## Key'd archived card

Matched by its Key so that no duplicate is created, and otherwise left untouched (plan D4).

## Building at pickup (derived)

`pickupStage(stage, item)` (plan D26) runs on the derived stage of every Key'd card matched to an unarchived item,
before `planChanges` compares fields. `building` is `BUILDING_LABEL`.

| Derived stage | Item is an `Issue` with `building` | Stage written | `unlabel` |
|---|---|---|---|
| Specified | yes | Building | none |
| Specified | no | Specified | none |
| Building, In review or Done | yes | the derived stage | the label's id |
| Building, In review or Done | no | the derived stage | none |
| Idea | a draft has no labels | Idea | none |

The update op carries `unlabel: <label id>`, which `sync.mjs` applies with `removeLabelsFromLabelable` after the op's
other writes, and `describeOp` prints as `unlabel building`. An owner idea, an archived Key'd card and a held card get
no `pickupStage` and no `unlabel`. The issue's open or closed state does not enter the rule.

## Pickup target (derived)

`pickupTarget(key, items)` answers `pickup.mjs` (plan D28), with the item `indexByKey` picks for the key (plan D19):

| Item | Answer |
|---|---|
| none | `{ key, refused: "no card on the board carries the Key <key>" }` |
| a draft | `{ key, refused: "<key>'s card is a draft, not an issue" }` |
| archived | `{ key, refused: "<key>'s card is archived" }` |
| an issue that is closed | `{ key, refused: "<key>'s issue is closed" }` |
| an open issue | `{ key, number, repository }`, `repository` being `<owner>/<repo>` from `board.json` |

The contract is [contracts/building-label.md](contracts/building-label.md).
