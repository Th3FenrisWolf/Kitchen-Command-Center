# Code repo

The repo rules `speckit.implement` and `speckit.card.implement` share. Each reads this file
before its first command that touches code.

## Two repos, one command per `cd`

Some projects split specs from code: `FEATURE_DIR` and this file live in the
working directory, and `code_repo.path` names a sibling repo everything else
targets. Every command that touches code names its repo explicitly —
`git -C <code_repo.path> ...`, or a single `cd` immediately before the one
command that needs it. A `cd` binds the command right after it, never the one
after that. When `code_repo.path` is empty, every command already runs in the
right place: drop the `-C <code_repo.path>` from each `git` command below.

`code_repo.working_directory` names the subdirectory inside the code repo
where the build, test and format commands run. An empty value runs them at
the repo root.

The code repo's **trunk** is what follows the slash in the output of
`git -C <code_repo.path> symbolic-ref --short refs/remotes/origin/HEAD`. A
repo with no remote HEAD takes the output of
`gh repo view --json defaultBranchRef --jq .defaultBranchRef.name`, run with a
`cd` into the code repo. Its **GitHub repo** is the output of
`gh repo view --json nameWithOwner --jq .nameWithOwner`, run with a `cd` into
the code repo. Every `gh` call below takes `--repo <owner/name>`: without
it, `gh` resolves the repo of the working directory, which in split mode is
the specs repo, and finds nothing.

A card is **docs-only** when `code_repo.path` is set and no task in
`tasks.md` changes a file in the code repo. A task path is written from the
code repo's root, so `src/Foo.cs` is a code change although it does not start
with `code_repo.path`. Only a path inside this repo, such as `FEATURE_DIR`,
is a docs change. A docs-only card skips the branch, the
local review, and the push and pull request. Its definition-of-done rows that
run against code are reported `N/A`. Its board step still runs.
