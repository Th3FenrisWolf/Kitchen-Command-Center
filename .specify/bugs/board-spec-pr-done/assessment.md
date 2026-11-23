# Bug Assessment: Board sync marks a spec Done when only its spec merges

- **Slug**: board-spec-pr-done
- **Created**: 2026-11-23
- **Source**: pasted text from the owner's session
- **Verdict**: valid
- **Severity**: medium

## Report (verbatim or summarized)

The project board moves a feature through its whole flow when only its spec is written. Specifying 011 and 012 and
merging their spec pull requests, [#162](https://github.com/Th3FenrisWolf/Kitchen-Command-Center/pull/162) from
`011-design-consistency` and [#169](https://github.com/Th3FenrisWolf/Kitchen-Command-Center/pull/169) from
`012-nav-structure`, moved both cards Specified, In review, Done and closed their issues, though neither feature is
built.

## Symptom

A spec card shows In review while a spec-only pull request is open, and Done once it merges, so its issue closes. It
should stay Specified until tasks are ticked, and reach Done only when the build merges.

## Reproduction

1. Run `/speckit-specify` for an idea. It creates branch `NNN-slug` with only `specs/NNN-slug/` files.
2. Open a pull request from `NNN-slug` to `main`. The sync shows the card as In review.
3. Merge it. The sync shows the card as Done and closes its issue.

## Suspected Code Paths

- `tools/project-board/derive.mjs:129-136` (`specCard`): the stage is Done for any merged pull request whose head is
  the spec's branch and In review for any open one, whatever the pull request contains.
- `tools/project-board/derive.mjs:86-93` (`pullFor`): prefers a merged pull request to an open one. `/speckit-card-implement`
  builds on the same `NNN-slug` branch, so once a spec pull request has merged, the build's open pull request is never
  chosen: the card stays Done and links the spec pull request.
- `tools/project-board/derive.mjs:122-125`: once a pull request has merged, the card reads `main` first, so ticked tasks
  on the branch of a later build are not seen.

## Root Cause Hypothesis

The sync assumes one pull request per feature branch, carrying the build. The Spec Kit flow now merges the spec first
from the same branch name, and the sync has no way to tell the two apart. Confidence: high.

## Proposed Remediation

**Preferred**: a ticked task is the evidence of a build, as the owner decided. In `specCard`:

- **Done**: a merged pull request from the branch, and `main`'s `tasks.md` has at least one ticked task.
- **In review**: an open pull request from the branch, and the branch's `tasks.md` has at least one ticked task.
- **Building**: at least one ticked task on the branch, or on `main` when the branch is gone. The `building` label
  still lifts a Specified card to Building through `pickupStage`, unchanged.
- **Specified**: everything else, including an open or merged spec-only pull request.

`pullFor` prefers an open pull request to a merged one, so the card links the build under review. The snapshot the card
reads for its body is the branch's while a pull request is open or none has merged, otherwise `main`'s.

On `main` today 001 to 008 all have ticked tasks, so they stay Done; 007, at 64 of 67, stays Done. 009 to 012 have
none: the next sync after the fix reaches `main` moves 011 and 012 back to Specified and reopens their issues.

**Alternatives**:

- Count a pull request as a build only when it changes files outside `specs/`. The sync would read every pull
  request's file list, and a feature that changes only docs would never reach Done. Rejected by the owner.

**Files likely to change**:

- `tools/project-board/derive.mjs`
- `tests/KCC.ViteTests/Tools/projectBoard.test.ts`

**Tests to add or update** (the `spec stage` describe):

- An open spec-only pull request leaves the card Specified, and still links the pull request.
- A merged spec-only pull request leaves the card Specified.
- After a merged spec pull request, ticked tasks on the branch make the card Building, and an open build pull request
  from the same branch makes it In review and links the open pull request.
- A merged pull request with ticked tasks on `main` is Done; the existing "stays Done when tasks were left open" case
  still passes.
- Update "is In review while its pull request is open, and Done once it merges" to carry ticked tasks.

## Risks & Considerations

- The sync runs from `main`, so the board changes only after the fix merges. Its first run reopens the issues of 011
  and 012, which is the intended correction.
- A build that merges with no task ticked would stay Specified. `/speckit-card-implement` ticks tasks as it goes, so
  this needs a build outside Spec Kit that also carries a `tasks.md` with none ticked.
- Bug cards (`bugCard`) are not affected: a bug's branch carries its fix.

## Open Questions

None.
