---
description: Apply the remediation from a bug assessment, then run the definition of done before the fix is reported, and the review loop of speckit.review.run on the open pull request. The review fixes land as one commit.
strategy: wrap
---

{CORE_TEMPLATE}

## Review fixes as one commit

> This section changes gate 2 of the Gates above. Where the two conflict, this section wins.

- A pull request this session opens for the fix opens from a branch recomposed with the `recompose-branch` skill.
  `.claude/hooks/require-recomposed-branch.sh` refuses `gh pr create` until it is.
- Commit nothing until `speckit.review.run` has finished every round. Then commit every fix from every round as one
  commit, `fix(<bug slug>): address review feedback`, on the same branch, and push it to the same pull request. A
  review that fixed nothing adds no commit.
- Never recompose the branch again after its pull request opens.
