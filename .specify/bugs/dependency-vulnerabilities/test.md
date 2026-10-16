# Bug Verification: NuGet and npm packages carry open vulnerability alerts

- **Slug**: dependency-vulnerabilities
- **Tested**: 2026-10-15
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: partial

## Summary

On commit 53bff2b, 26 of the 32 open alerts resolve to a patched version. The other 6 are the leftovers that
`fix.md` records, and no test regressed. The result is partial because GitHub has not re-scanned the branch. The
alerts close only after the merge, and the new images have not run on the Pi.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) | Read the open alerts with `gh api .../dependabot/alerts?state=open`, then compare each alert's first patched version with the versions that `yarn.lock` and every `obj/project.assets.json` resolve on the branch | pass | 26 resolve to a patched version: 3 `MessagePack`, 23 npm. 6 stay open: 5 `SixLabors.ImageSharp` and `braces`, as `fix.md` records. |
| NuGet audit | `dotnet list KitchenCommandCenter.slnx package --vulnerable --include-transitive` | pass | Only `SixLabors.ImageSharp` 3.1.12 is listed. `KCC.Admin`, `KCC.Contributions` and `KCC.E2ETests` have no vulnerable packages. |
| Shipped runtime | `docker run --entrypoint dotnet kcc-app:vuln-fix --list-runtimes`, on the arm64 image built from the fix's tree | pass | `Microsoft.AspNetCore.App 10.0.13`, `Microsoft.NETCore.App 10.0.13` |
| New / updated tests | `dotnet run --project tests/KCC.IntegrationTests/... -- --treenode-filter "/*/*/TiffUploadTests/*"`; the same for `PhotoValidationTests` in `KCC.UnitTests` | pass | 8 of 8, and 20 of 20 |
| Regression suite | `node tests/scripts/run.mjs` | pass | 2,664 passed, 0 failed, 0 skipped across 6 suites |
| Type-check | `vue-tsc --build` in `src/KCC.Web` | pass | exit 0 |
| Lint | `eslint .` in `src/KCC.Web`, without the script's `--fix` | pass | 0 errors, 4 `no-unused-vars` warnings in files the fix did not touch |
| GitHub alert list after the change | Dependabot reads `main` only | not-run | Nothing is pushed or merged |
| Images on the Pi | Run the `app` and `ssr` images on the Pi | skipped | This needs the production host |

## Output Excerpts

```text
119	npm	shell-quote	critical	fix 1.11.0	have 1.12.0	CLOSED
117	npm	proxy-addr	critical	fix 2.0.8	have 2.0.8	CLOSED
114	npm	@vue/server-renderer	high	fix 3.5.42	have 3.5.44	CLOSED
113	npm	braces	high	fix none	have 3.0.3	OPEN (no fix)
124	nuget	SixLabors.ImageSharp	high	fix 4.1.2	have 3.1.12	OPEN
```

```text
> SixLabors.ImageSharp      3.1.12     Moderate   https://github.com/advisories/GHSA-gwg2-r3hj-4w44
2664 passed · 0 failed · 0 skipped across 6 suites
```

## Residual Risks

- The production runtime changes to 10.0.13. The images have run on this Mac only.
- New advisories can land before the merge. Read the alert list again on the day of the merge.
- ImageSharp stays vulnerable to a crafted image of a format that is not TIFF, such as one with an ICC CLUT profile
  (GHSA-gwg2-r3hj-4w44). Signed imaging requests and backoffice-only media uploads reduce the exposure. They do not remove it.

## Recommendation

Hold the bug open until the merge. Open the pull request, and run the `app` and `ssr` images on the Pi before you
deploy. After the merge, close the bug if the open-alert list holds only the 6 alerts that `fix.md` records.
