---
description: Apply the remediation from a bug assessment, then run the definition of done before the fix is reported, and the review loop of speckit.review.run on the open pull request.
strategy: wrap
---

## Before you begin

This command fixes a bug, and a fix is code like any other. The **Gates** section at the end of
this file runs after the fix and before the report. Read this whole file before you act on any
part of it.

{CORE_TEMPLATE}

## Gates

Run these after the fix is applied and before the report the command above writes. A fix is not
done until both pass.

1. **Definition of done.** Run every row of the definition-of-done table in
   `.specify/memory/constitution.md`, in the working directory each row names. A failed row is a
   stop: report it, and do not report the bug as fixed. A project without the table runs the
   tests the command above runs.
2. **Review loop, on the open pull request.** The review runs after the pull request is open, never
   before it. When this session opens the pull request for the fix, open it first, then run this
   gate on its branch. When no pull request is open, report the review as not run yet and name
   that it runs once the pull request opens; do not hold the pull request for it. When
   `.specify/extensions/review/` does not exist, report the review as not run, and name
   `specify extension add review`. Otherwise invoke `speckit.review.run` as a command in this
   session, and wait for its report. When no skill of that name is registered, read its file
   `.specify/extensions/review/commands/speckit.review.run.md` and follow it. It reviews the diff
   and fixes what is real, and it commits nothing: commit its fixes on the same branch and push
   them to the same pull request. Name its checks: the rows of gate 1. A review that did not run
   is reported as not run, never as clean.

Add both results to the report: each definition-of-done row with its result, and the review
table of `speckit.review.run` with its final-round status, or why the review did not run.
