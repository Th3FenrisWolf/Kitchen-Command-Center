# Review loop

How a command runs a CodeRabbit round. The review loop runs on the branch of an open pull
request, never before it opens. The review commits nothing: the caller commits the fixes and
pushes them to the same pull request.

## Run a round

Run every round through the `review-run` script, never as a bare `coderabbit` call. It takes a
machine-wide lock, clears the tool's cache, runs the review in the code repo, and releases the
lock. The tool keeps one cache for the whole machine: a parallel agent that clears it stops a
running review, and a stale entry fails a round with `is outside repository`. The lock also
keeps parallel agents to one review at a time, under the rate limit.

Pass every path as an absolute path. A relative path breaks after a `cd` into the code repo.
Write the output to a file:

```bash
bash <project>/.specify/extensions/coderabbit/scripts/review-run.sh <code repo> review --agent --base-commit <base> --include-untracked -c <context files> > <log> 2>&1
```

```powershell
pwsh -NoProfile -File <project>/.specify/extensions/coderabbit/scripts/review-run.ps1 <code repo> review --agent --base-commit <base> --include-untracked -c <context files> *> <log>
```

The **base** is the commit `git -C <code repo> merge-base <ref> HEAD` prints, after a
`git -C <code repo> fetch origin`. `<ref>` is what
`git -C <code repo> symbolic-ref --short refs/remotes/origin/HEAD` prints, else `origin/main`.
When the code repo has no `origin` remote, skip the fetch and use `main` as `<ref>`. A branch
base also reviews the trunk's newer changes when the feature branch is behind it. Always add
`--include-untracked`: a fix is not committed yet, and it costs nothing when no file is untracked.

Add no `timeout` and no pipe. macOS has no `timeout`, and a pipe returns the exit code of its
last command, so either one turns a failed review into empty output. A round can take ten
minutes, longer when it waits for the lock: run it in the background, and read the whole log.

A round counts only when the script exits 0 and the log contains `review_completed`. Any other
result is a **failed round**. Report it as failed, with its error line, and never as clean. Exit
5 means the lock stayed busy for 30 minutes. A command runs one round and never retries a failed
one: it reports the failure, and the owner fixes the cause.
