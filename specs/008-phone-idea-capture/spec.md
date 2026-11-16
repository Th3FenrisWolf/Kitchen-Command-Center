# Feature Specification: Phone Idea Capture

**Feature Branch**: `008-phone-idea-capture`

**Created**: 2026-11-16

**Status**: [On the project board](https://github.com/users/Th3FenrisWolf/projects/3?filterQuery=key%3A008-phone-idea-capture)

**Input**: User description: "i want to be able to add ideas that havent been specced in the github app on my pohne
when i think of them, but this process seems really bad" and "yes, every issue, start the specify"

## Background

The project board holds the owner's ideas next to every spec, phase and bug. The board sync writes every card that
carries a Key and never touches a card without one. An idea therefore has to be made by hand on the board, with its
Status, Kind and Iteration set by hand, and when it becomes a spec the owner types the spec's Key into it by hand.

The GitHub mobile app cannot do the first step well. It cannot make a draft card on the board. Its new-issue screen
can put an issue on the board, but it cannot set any of the board's fields. Issue #148, "Companion App", was made
that way. It reached the board with a blank Status, Kind and Iteration, and the board view, which groups cards by
Status, did not show it. The sync also runs only on pushes, pull requests and once each morning, so a new issue
would wait hours for it even if the sync took it in.

This feature makes an issue opened on the phone the whole of capturing an idea, and makes the spec or bug that grows
from it take over the same card.

## Clarifications

### Session 2026-11-16

- Q: Does every issue the owner opens count as an idea, or only issues with an `idea` label? → A: Every issue. No
  label, project or field is needed. An issue that turns out not to be an idea has its Kind changed by hand.
- Q: Issues from which accounts count? → A: The board owner's account, Th3FenrisWolf, only. Issues opened by
  twinright-bzs, the other collaborator, or by any visitor stay off the board unless added by hand.
- Q: What happens to the card of an idea whose issue is closed? → A: The sync archives it. Reopening the issue brings
  it back. The issue and its history stay in the repository.
- Q: What happens when the owner archives the card of a still-open idea by hand? → A: It comes back on the next sync.
  An open idea is on the board and a closed idea is archived. To drop an idea, close its issue.
- Q: Can a bug take over an idea too? → A: Yes. `/speckit-bug-assess #NNN` takes over an idea the same way
  `/speckit-specify #NNN` does.
- Q: Which issues can those two commands take over? → A: Only issues the owner opened. Naming anyone else's issue is
  refused with a message, and the command runs as usual without a link.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Capture an idea from the phone (Priority: P1)

The owner thinks of something while away from a computer. In the GitHub mobile app they open a new issue in this
repository, type a title and maybe a few lines, and tap Create. They pick no project, label or field. Within two
minutes the idea is on the project board in the Idea column, marked as an Idea, in the Current iteration.

**Why this priority**: This is the problem the owner hit. Without it an idea from the phone either never reaches the
board or reaches it invisible.

**Independent Test**: Open an issue from the owner's account with only a title, and see its card appear in the Idea
column with Kind Idea and Iteration Current, and with its title and text unchanged.

**Acceptance Scenarios**:

1. **Given** the owner opens an issue with only a title, **When** two minutes have passed, **Then** the issue is on
   the board with Status Idea, Kind Idea, Iteration Current and no Key, and its title and text are as the owner wrote
   them.
2. **Given** an open issue of the owner's is already on the board with blank fields, as #148 is, **When** the sync
   runs, **Then** its blank Status, Kind and Iteration are filled with Idea, Idea and Current.
3. **Given** the owner has moved an idea to Future and changed its Kind to Bug, **When** the sync runs again,
   **Then** both values stay as the owner set them.
4. **Given** twinright-bzs or a visitor opens an issue, **When** the issue is opened, **Then** no board update runs
   for it and the issue does not appear on the board.
5. **Given** the sync itself opens an issue for a card that has left Idea, **When** that issue is opened, **Then** no
   board update runs for it and the issue is never treated as an idea.

---

### User Story 2 - Drop an idea, and bring it back (Priority: P2)

The owner decides an idea is not worth doing and closes its issue, from the phone or anywhere else. Its card leaves
the board. If they change their mind and reopen the issue, the card comes back as it was.

**Why this priority**: Capturing ideas freely only works if dropping one is as easy as making one, so the Idea column
does not fill with dead cards.

**Independent Test**: Close an idea's issue and see its card archived; reopen it and see the card return with the
Status, Kind and Iteration it had.

**Acceptance Scenarios**:

1. **Given** an open idea on the board, **When** the owner closes its issue, **Then** within two minutes its card is
   archived, and the issue itself is unchanged.
2. **Given** an idea whose card was archived because its issue closed, **When** the owner reopens the issue, **Then**
   within two minutes the card is back on the board with the Status, Kind and Iteration it had.
3. **Given** an open idea whose card the owner archived by hand, **When** the sync runs, **Then** the card is back on
   the board.
4. **Given** a closed issue of the owner's that has never been on the board, **When** the sync runs, **Then** it
   stays off the board.

---

### User Story 3 - Turn an idea into a spec (Priority: P2)

The owner is ready to specify an idea. They run `/speckit-specify #148` instead of typing a description. The command
reads the idea's title and text as the feature description, and the spec records which idea it came from. Once the
spec is pushed, the idea's own card becomes the spec's card: it takes the spec's Key, title and stage, and the
owner's original words stay in the issue below the synced block. The board never shows the idea and the spec as two
cards.

**Why this priority**: Without it, every idea that becomes a spec leaves a stale idea card behind or needs its Key
typed by hand, which is the manual step this feature removes.

**Independent Test**: Run `/speckit-specify` on an open idea, push the spec, and see one card on the board for it,
carrying the spec's Key, title and stage, with the owner's original text still in the issue.

**Acceptance Scenarios**:

1. **Given** an open idea #148, **When** the owner runs `/speckit-specify #148`, **Then** the spec's input is the
   issue's title and text, and the spec carries a line naming and linking issue #148 as its idea.
2. **Given** a pushed spec that names #148 as its idea, **When** the sync runs, **Then** #148's card carries the
   spec's Key, the spec's card title and the spec's stage, the issue's text has the synced block above the owner's
   original words, and the board has exactly one card for the spec.
3. **Given** a spec that names an idea whose issue was closed and whose card was archived, **When** the sync runs,
   **Then** the card is back on the board and the issue's open or closed state follows the spec's stage.
4. **Given** the owner runs `/speckit-specify #200` and #200 was opened by another account or already carries a
   Key, **When** the command starts, **Then** it says why it cannot take #200 over, does not read #200's text, and
   carries on without a link, from any description the owner typed with it or by asking for one.

---

### User Story 4 - Turn an idea into a bug (Priority: P3)

Some ideas are really bug notes. The owner runs `/speckit-bug-assess #NNN` on one, and the bug's card takes the idea
over the same way a spec does.

**Why this priority**: Bug notes taken on the phone are less common than feature ideas, and changing a card's Kind by
hand covers them until this lands.

**Independent Test**: Run `/speckit-bug-assess` on an open idea, push the assessment, and see the idea's card become
the bug's card, with Kind Bug and the bug's Key, and no second card.

**Acceptance Scenarios**:

1. **Given** an open idea #150, **When** the owner runs `/speckit-bug-assess #150`, **Then** the assessment's input
   is the issue's title and text, and the assessment carries a line naming and linking issue #150 as its idea.
2. **Given** a pushed bug assessment that names #150, **When** the sync runs, **Then** #150's card carries the bug's
   Key, title, Kind Bug and stage, and the board has exactly one card for the bug.
3. **Given** the owner names another account's issue, or one that already carries a Key, **When** the command
   starts, **Then** it refuses the link as in User Story 3 and carries on without one.

---

### Edge Cases

- Two specs, two bugs, or a spec and a bug name the same idea: if the idea's card already carries one of their Keys,
  that one keeps it; otherwise the one whose Key sorts first takes it over. The other gets a card of its own as it
  would today, and the sync prints a warning naming both.
- A spec or bug names an issue that does not exist, was transferred away, already carries another Key, or was not
  opened by the owner: it gets a card of its own as it would today, and the sync prints a warning naming the issue.
- The sync and the owner use the same account, so every issue the sync opens has the owner as its author. Those
  issues carry the sync block in their text, and an issue with the sync block is never an idea. This also covers the
  duplicate issues an earlier sync failure left behind (#144 to #147).
- An issue on the board that carries a Key is the sync's, as today, whoever opened it.
- The owner sets a Key on an idea card by hand: the sync takes it over as it does today. The Idea line is a second
  way to link an idea, not the only one.
- The owner makes a draft card on the board in a browser: it stays untouched until it carries a Key, as today.
- The owner opens several issues in a row: each reaches the board. Queued board updates collapse into one run that
  covers them all. Issue events that start no board update, including those for the issues the sync itself opens or
  closes, leave a waiting board update in place.
- A board update fails or GitHub Actions is down: the next run of any kind catches up, because the sync works from
  the current state of the repository and the board, not from the event that started it.
- The owner edits an idea's title or text after it reaches the board: the sync does not notice or touch it.
- The owner clears one of an idea's fields: the next sync fills it again.
- The owner opens an issue from an issue template or with labels: it still counts as an idea, and its labels are left
  alone.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The sync MUST treat as an owner idea every issue in this repository that the board owner's account
  opened, whose text carries no sync block, and whose board card, if it has one, carries no Key. The board owner is
  the account the board's configuration names, Th3FenrisWolf. No label, project or field is needed.
- **FR-002**: The sync MUST put every open owner idea on the board, and MUST bring back the card of an open owner idea
  that is archived, however it came to be archived.
- **FR-003**: On an owner idea's card the sync MUST fill a blank Status with Idea, a blank Kind with Idea and a blank
  Iteration with Current, and MUST NOT change any of the three that already has a value.
- **FR-004**: While an owner idea carries no Key, the sync MUST NOT change its title, text, labels, open or closed
  state, or any board field other than the three blanks of FR-003.
- **FR-005**: The sync MUST archive the card of every closed owner idea that is on the board, and MUST NOT add a
  closed owner idea that is not on the board.
- **FR-006**: The sync MUST NOT add, fill, archive or restore the card of an issue opened by any other account,
  including the repository's other collaborators.
- **FR-007**: A board update MUST start when an issue is opened, reopened or closed, as well as on the events that
  start one today. An issue event MUST NOT start a board update when the issue was opened by another account or its
  text carries the sync block. An issue event that does not start a board update MUST NOT cancel, delay or replace a
  board update that is already waiting to run.
- **FR-008**: The board update MUST NOT place any issue's title or text in a command it runs. Anyone can open an issue
  in this public repository, and the update holds a token that can write the board and the repository's issues.
- **FR-009**: `/speckit-specify` MUST accept an issue reference, `#NNN`, in place of a description. It reads that
  issue's title and text as the feature description and writes an `**Idea**:` line into the spec that names and links
  the issue.
- **FR-010**: `/speckit-bug-assess` MUST accept `#NNN` the same way and write the same `**Idea**:` line into the bug's
  assessment.
- **FR-011**: Both commands MUST refuse to link an issue that the board owner did not open or that already carries a
  Key. They MUST say why, MUST NOT read the text of an issue another account opened, and MUST carry on without a link.
- **FR-012**: When a spec or bug assessment names an owner idea in its `**Idea**:` line, the sync MUST give that
  idea's card the spec's or bug's Key and from then on write it as that spec's or bug's card: its title, fields,
  synced block, open or closed state and parent link. It MUST NOT create a second card. The owner's original text MUST
  stay in the issue below the synced block.
- **FR-013**: When the idea a spec or bug names has an archived card, the sync MUST bring the card back as it takes it
  over.
- **FR-014**: The sync MUST give an idea to at most one card. An idea whose card already carries a Key keeps that
  Key. When several specs or bugs name an idea whose card carries no Key yet, the one whose Key sorts first takes it.
  Every other spec or bug that names it, and one that names an idea that cannot be taken over (FR-011's cases, or no
  such issue), gets a card created as it is today. In each case the sync MUST print a warning naming the cards and
  the issue.
- **FR-015**: Setting a Key on an idea card by hand MUST keep working as it does today, and draft cards MUST stay
  untouched until they carry a Key.
- **FR-016**: The sync's dry run MUST list every idea it would add, fill, archive, bring back or hand over, with its
  issue number, as it lists its other changes.
- **FR-017**: The constitution's Flow section and `CLAUDE.md`'s project board notes MUST state the new rules: the
  owner's issues are ideas the sync files and archives, and a spec or bug takes over the idea it names. The
  constitution changes through `/speckit-constitution`, run by the owner, as a MINOR amendment in the same pull
  request.

### Key Entities

- **Owner idea**: an issue the board owner opened that carries no sync block and whose card carries no Key. It is
  open or closed, and has at most one card on the board.
- **Board card**: an item on the project board, with Status, Kind, Iteration, Key, Feature and Phase fields, and
  archived or not. A card with a Key belongs to the sync. A card for an owner idea belongs to the owner, except for
  the blanks the sync fills and its archiving.
- **Idea line**: the `**Idea**: #NNN` line in a spec or a bug assessment, naming the owner idea it grew from. It is the
  only link the sync reads between a spec or bug and an idea.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An idea opened from the phone with nothing but a title reaches the Idea column within two minutes in at
  least 9 of 10 tries, with no step on the phone beyond creating the issue.
- **SC-002**: Issue #148 sits in the Idea column, with Kind Idea and Iteration Current, after the first board update
  that follows the merge.
- **SC-003**: Over the first two weeks of use, the sync changes none of the Status, Kind or Iteration values the owner
  set on an idea.
- **SC-004**: Every idea that becomes a spec or a bug through its command leaves exactly one card on the board, and
  the owner types no Key by hand.
- **SC-005**: Issues opened by other accounts start no board update and never appear on the board unless the owner
  adds them.
- **SC-006**: A closed idea leaves the board within two minutes, and a reopened one returns within two minutes with
  the fields it had.

## Assumptions

- The board owner, the account that opens issues on the phone, and the account whose token the sync uses are one
  account, Th3FenrisWolf. The sync block, not the author, is what separates the sync's issues from the owner's ideas.
- "Within two minutes" assumes GitHub Actions runs without a backlog. A run that is late or fails is made up by the
  next run of any kind.
- A new idea starts in the Current iteration, as every card the sync creates does, and the owner moves it to Future
  when it is not for now.
- The board's own built-in automations, such as setting Done when an item closes or reopens, are turned off or left
  to the sync. If one is on, a reopened idea comes back with the Status it set, because the sync fills only blank
  fields.
- `/speckit-specify` and `/speckit-bug-assess` come from Spec Kit extensions, which an extension update rewrites. The
  `#NNN` handling survives an update the same way the recompose steps do, by a project preset that is reapplied after
  every update.
- Out of scope: ideas from other accounts, labels or issue templates for ideas, adopting draft cards made on the
  board, notifications, moving ideas between iterations on their own, and any phone shortcut beyond the GitHub app's
  new-issue screen.
