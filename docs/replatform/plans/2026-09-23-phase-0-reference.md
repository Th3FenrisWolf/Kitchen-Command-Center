# Replatform Phase 0 — Reference Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Status:** done 2026-09-23 (`replatform` d9bcb09..da95d98; tag `xperience-final` pushed to origin; the branch
stays local until parity). **Resume point:** the Phase 1 plan in this folder. The capture tool grew beyond Task 3's listing: it waits
for network idle, validates each page's status and URL, hides dev overlays, shoots cook mode at viewport size by a
dispatched click, and takes `--only <names>` (signed-out selections skip sign-in). The reference set and the
differences Phase 2 should expect are in `docs/replatform/reference/xperience-final/` (see its `NOTES.md`).

**Goal:** Freeze a reference of the Xperience site before anything changes — a tag, the 216 UI-string values
exported to a committed file, and reference screenshots of every page type in both ramps.

**Architecture:** No application code changes. A tag marks the last Xperience commit and a `replatform` branch
starts from it. A one-off script exports the resource-string values from the local SQL Server database into
`src/KCC.Web/Features/Dictionary/ui-strings.json`, which Phase 1 imports into Umbraco's dictionary. A small
Playwright console tool, `tests/KCC.ReferenceCapture`, captures full-page screenshots; Phase 2 runs the same
tool against the Umbraco site to check parity.

**Tech Stack:** git, Docker (`mssql2022` container), `sqlcmd`, Python 3, .NET 10, Microsoft.Playwright 1.58.0.

**Spec:** `docs/replatform/specs/2026-09-21-replatform-off-xperience.md` — §15 (Phasing), §12 (seeding), §18
(Kentico reference risk).

## Global Constraints

- The tag is **`xperience-final`**, on `main`. All replatform work happens on branch **`replatform`**, created
  from that tag.
- The replatform's spec, plans and reference set are tracked in `docs/replatform/`; everything else under
  `.superpowers/` is gitignored scratch and never staged. Reference screenshots live in
  `docs/replatform/reference/xperience-final/`.
- Commit messages are Title Case imperative, matching the repo's history (e.g. `Add Reference Capture Tool`).
  No attribution lines.
- Pushing anything to GitHub (the tag, the branch) is outward-facing: ask the owner before each push.
- Never print, commit or write the database password or the E2E member credentials anywhere; read them from
  user-secrets and environment variables at run time.
- The capture tool pins **Microsoft.Playwright 1.58.0** — the version TUnit.Playwright 1.27.0 already uses, so it
  reuses the browsers installed under `~/Library/Caches/ms-playwright`.
- Test projects in this repo use `<Nullable>enable</Nullable>` and `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`;
  the new tool follows them.
- Paths below are relative to the repo root, `/Users/twinright/Repos/Kitchen-Command-Center`.

## File map

| File | Change |
|---|---|
| `src/KCC.Web/Features/Dictionary/ui-strings.json` | Create — the exported UI strings, a flat object sorted by key |
| `tests/KCC.ReferenceCapture/KCC.ReferenceCapture.csproj` | Create — console tool |
| `tests/KCC.ReferenceCapture/Program.cs` | Create — argument parsing and the capture loop |
| `tests/KCC.ReferenceCapture/Captures.cs` | Create — the page list, viewports and page actions |
| `Directory.Packages.props` | Modify — add `Microsoft.Playwright` 1.58.0 |
| `KitchenCommandCenter.sln` | Modify — add the tool under the `tests` folder |

---

### Task 1: Tag, branch and reference worktree

**Files:** none (git only).

- [ ] **Step 1: Confirm `main` is clean and current**

Run:

```bash
git switch main
git status --porcelain
git fetch origin
git status -sb | head -1
```

Expected: no output from `git status --porcelain`, and the branch line shows `## main...origin/main` with no
`ahead`/`behind`. If `main` is behind, `git pull --ff-only` first. If there are local changes, stop and ask the
owner.

- [ ] **Step 2: Create the annotated tag on `main`**

```bash
git tag -a xperience-final -m "Last Xperience by Kentico version before the Umbraco replatform"
git show --no-patch --format='%H %s' xperience-final
```

Expected: the tag resolves to the current `main` HEAD commit.

- [ ] **Step 3: Push the tag (ask the owner first)**

Ask: "Push tag `xperience-final` to origin?" On yes:

```bash
git push origin xperience-final
```

Expected: `* [new tag]         xperience-final -> xperience-final`.

- [ ] **Step 4: Create the `replatform` branch from the tag**

```bash
git switch -c replatform xperience-final
git branch --show-current
```

Expected: `replatform`.

- [ ] **Step 5: Create the reference worktree**

```bash
git worktree add ../Kitchen-Command-Center-xperience xperience-final
git worktree list
```

Expected: a second worktree at `../Kitchen-Command-Center-xperience` on the detached `xperience-final` commit. It
stays unused until a later phase needs the running Xperience site; to run it there, `yarn install` in that folder
and `dotnet watch` from its `src/KCC.Web` (it shares this checkout's user-secrets and the `mssql2022` database).
Only one of the two sites can hold `https://localhost:58671` at a time.

---

### Task 2: Export the UI strings

The English values of the 216 resource strings exist only in the local database's `Custom_ResourceString` table
(`ResourceStringValue` is excluded from the CI repository). This task copies every row into a committed file.

**Files:**
- Create: `src/KCC.Web/Features/Dictionary/ui-strings.json`

- [ ] **Step 1: Confirm the database container is running**

```bash
docker ps --format '{{.Names}} {{.Status}}' | grep mssql2022
```

Expected: `mssql2022 Up …`. If it isn't running: `docker start mssql2022`, wait ~20 seconds, re-run.

- [ ] **Step 2: Export the table to JSON**

The script reads the connection string from KCC.Web's user-secrets (never echoing it), runs the query inside the
container with `FOR JSON`, and writes a flat object sorted by key. SQL Server splits a long JSON result across
output lines and `sqlcmd` prints a column header above it, so the script joins the lines and slices from the first
`[` to the last `]`.

```bash
python3 - <<'PY'
import json, re, subprocess, sys
from pathlib import Path

secrets = subprocess.run(
    ["dotnet", "user-secrets", "list", "--project", "src/KCC.Web"],
    capture_output=True, text=True, check=True).stdout
match = re.search(r"^ConnectionStrings:CMSConnectionString = (.+)$", secrets, re.M)
if not match:
    sys.exit("CMSConnectionString not found in KCC.Web user-secrets")
parts = dict(
    (k.strip().lower(), v.strip())
    for k, v in (p.split("=", 1) for p in match.group(1).split(";") if "=" in p))
database = parts.get("initial catalog") or parts.get("database")
user = parts.get("user id") or parts.get("uid")
password = parts.get("password") or parts.get("pwd")

query = ("SET NOCOUNT ON; SELECT ResourceStringKey AS [key], ResourceStringValue AS [value] "
         "FROM Custom_ResourceString ORDER BY ResourceStringKey FOR JSON PATH, INCLUDE_NULL_VALUES")
out = subprocess.run(
    ["docker", "exec", "-e", f"SQLCMDPASSWORD={password}", "mssql2022",
     "/opt/mssql-tools18/bin/sqlcmd", "-S", "localhost", "-U", user, "-d", database,
     "-C", "-y", "0", "-Q", query],
    capture_output=True, text=True, check=True).stdout
joined = "".join(out.splitlines())
rows = json.loads(joined[joined.index("["):joined.rindex("]") + 1])

strings = {row["key"]: row.get("value") or "" for row in rows}
target = Path("src/KCC.Web/Features/Dictionary/ui-strings.json")
target.parent.mkdir(parents=True, exist_ok=True)
target.write_text(json.dumps(dict(sorted(strings.items())), indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

ci_keys = set()
for f in Path("src/KCC.Web/App_Data/CIRepository/@global/custom.resourcestring").glob("*.xml"):
    m = re.search(r"<ResourceStringKey>([^<]+)</ResourceStringKey>", f.read_text(encoding="utf-8-sig"))
    if m:
        ci_keys.add(m.group(1))
print(f"exported {len(strings)} rows; CI has {len(ci_keys)} keys")
print("in DB, not in CI:", sorted(set(strings) - ci_keys))
print("in CI, not in DB:", sorted(ci_keys - set(strings)))
print("empty values:", sorted(k for k, v in strings.items() if not v))
PY
```

Expected: `exported N rows; CI has 216 keys`, with N ≥ 216. Record the three printed lists in the task report.
Keys that exist only in the database (created through the on-page editor) are kept — the export takes every row.
Keys in CI but not in the database, or with empty values, are listed so Phase 1 knows which strings render as their
key today.

- [ ] **Step 3: Sanity-check the file**

```bash
python3 -c "import json;d=json.load(open('src/KCC.Web/Features/Dictionary/ui-strings.json'));print(len(d));print(d.get('Login.SignIn'));print(d.get('Api.UnexpectedError'))"
```

Expected: the row count again, then two real English strings (not `None`, not empty).

- [ ] **Step 4: Commit**

```bash
git add src/KCC.Web/Features/Dictionary/ui-strings.json
git commit -m "Export UI Strings for the Umbraco Dictionary"
```

---

### Task 3: The reference capture tool

A console tool that signs in when needed and saves full-page screenshots for a fixed list of pages, in both ramps
and two widths. The page list uses lowercase paths, which Xperience matches case-insensitively and which Umbraco
generates from the same node names, so Phase 2 can point the same tool at the new site. Recipe pages use the
seeded "Fluffy Buttermilk Pancakes" and its "Classic Stack" variant: it has instructions (cook mode opens) and
reviews (ratings show), and Phase 2's seeder recreates it identically.

**Files:**
- Create: `tests/KCC.ReferenceCapture/KCC.ReferenceCapture.csproj`
- Create: `tests/KCC.ReferenceCapture/Captures.cs`
- Create: `tests/KCC.ReferenceCapture/Program.cs`
- Modify: `Directory.Packages.props`
- Modify: `KitchenCommandCenter.sln`

**Interfaces:**
- Produces: `dotnet run --project tests/KCC.ReferenceCapture -- --out <dir> [--base-url <url>]`, writing
  `<dir>/<theme>-<viewport>/<capture>.png` for themes `light`, `dark`, viewports `desktop` (1440×900) and
  `mobile` (390×844), and the 12 capture names in `Captures.All`. Signed-in captures read
  `KCC_E2E_MEMBER_USERNAME` / `KCC_E2E_MEMBER_PASSWORD`.

- [ ] **Step 1: Pin Playwright centrally**

In `Directory.Packages.props`, add this line inside the `<ItemGroup>`, keeping the list alphabetical (after
`Microsoft.Extensions.Http.Polly`):

```xml
    <PackageVersion Include="Microsoft.Playwright" Version="1.58.0" />
```

- [ ] **Step 2: Create the project file**

`tests/KCC.ReferenceCapture/KCC.ReferenceCapture.csproj`:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
        <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
        <WarningsNotAsErrors>NU1608;NU1901;NU1902;NU1903</WarningsNotAsErrors>
        <PlaywrightPlatform>all</PlaywrightPlatform>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="Microsoft.Playwright" />
    </ItemGroup>
</Project>
```

- [ ] **Step 3: Write the capture definitions**

`tests/KCC.ReferenceCapture/Captures.cs`:

```csharp
using Microsoft.Playwright;

namespace KCC.ReferenceCapture;

public sealed record Viewport(string Name, int Width, int Height);

public sealed record Capture(string Name, string Path, bool SignedIn, Func<IPage, Task>? Prepare = null);

public static class Captures
{
    public const string RecipePath = "/recipes/fluffy-buttermilk-pancakes";
    public const string VariantPath = "/recipes/fluffy-buttermilk-pancakes/classic-stack";

    public static readonly string[] Themes = ["light", "dark"];

    public static readonly Viewport[] Viewports =
    [
        new("desktop", 1440, 900),
        new("mobile", 390, 844),
    ];

    public static readonly Capture[] All =
    [
        new("home", "/", SignedIn: false),
        new("recipes", "/recipes", SignedIn: false),
        new("recipe", RecipePath, SignedIn: false),
        new("variant", VariantPath, SignedIn: false),
        new("cook-mode", VariantPath, SignedIn: false, OpenCookModeAsync),
        new("login", "/account/login", SignedIn: false),
        new("registration-complete", "/account/registration-complete", SignedIn: false),
        new("not-found", "/this-page-does-not-exist", SignedIn: false),
        new("account", "/account", SignedIn: true),
        new("settings", "/account/settings", SignedIn: true),
        new("create-recipe", "/recipes/create-recipe", SignedIn: true),
        new("add-variant", RecipePath, SignedIn: true, OpenAddVariantAsync),
    ];

    private static async Task OpenCookModeAsync(IPage page)
    {
        // The desktop and mobile layouts each render their own open button; only one is visible.
        await page.Locator("[data-test^='cook-mode-open']:visible").First.ClickAsync();
        await page.Locator("[role='dialog']").WaitForAsync();
    }

    private static async Task OpenAddVariantAsync(IPage page)
    {
        // The add-variant URL carries the recipe's platform-specific key, so follow the recipe page's own link.
        await page.Locator("main a[href*='add-variant']").First.ClickAsync();
        await page.WaitForURLAsync(url => url.Contains("add-variant", StringComparison.OrdinalIgnoreCase));
    }
}
```

- [ ] **Step 4: Write the program**

`tests/KCC.ReferenceCapture/Program.cs`:

```csharp
using KCC.ReferenceCapture;
using Microsoft.Playwright;

var outDir = ArgValue(args, "--out") ?? throw new ArgumentException("--out <directory> is required");
var baseUrl = ArgValue(args, "--base-url") ?? "https://localhost:58671";

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

var written = 0;
foreach (var theme in Captures.Themes)
{
    foreach (var viewport in Captures.Viewports)
    {
        var folder = Path.Combine(outDir, $"{theme}-{viewport.Name}");
        Directory.CreateDirectory(folder);

        foreach (var signedIn in new[] { false, true })
        {
            await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                BaseURL = baseUrl,
                IgnoreHTTPSErrors = true,
                ViewportSize = new ViewportSize { Width = viewport.Width, Height = viewport.Height },
                ColorScheme = theme == "dark" ? ColorScheme.Dark : ColorScheme.Light,
            });

            // The layout's pre-paint script reads the stored ramp before the OS preference.
            await context.AddInitScriptAsync($"try {{ localStorage.setItem('kcc-theme', '{theme}'); }} catch (e) {{ }}");

            var page = await context.NewPageAsync();
            if (signedIn)
            {
                await SignInAsync(page);
            }

            foreach (var capture in Captures.All.Where(c => c.SignedIn == signedIn))
            {
                await OpenAsync(page, capture.Path);
                if (capture.Prepare is not null)
                {
                    await capture.Prepare(page);
                    await SettleAsync(page);
                }

                var file = Path.Combine(folder, $"{capture.Name}.png");
                await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = file,
                    FullPage = true,
                    Animations = ScreenshotAnimations.Disabled,
                });
                written++;
                Console.WriteLine(file);
            }
        }
    }
}

Console.WriteLine($"{written} screenshots written to {outDir}");
return 0;

static string? ArgValue(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static async Task OpenAsync(IPage page, string path)
{
    // A dev page keeps a Vite HMR socket open, so `load`/network-idle never settle; the markup is server-rendered.
    await page.GotoAsync(path, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
    await SettleAsync(page);
}

static async Task SettleAsync(IPage page)
{
    // Fonts swap in late, and in development Main.ts removes the inlined SSR styles once hydration has run.
    await page.WaitForFunctionAsync(
        "() => document.fonts.status === 'loaded' && !document.querySelector('style[data-ssr-styles]')",
        null,
        new PageWaitForFunctionOptions { Timeout = 20_000 });
}

static async Task SignInAsync(IPage page)
{
    var username = Environment.GetEnvironmentVariable("KCC_E2E_MEMBER_USERNAME");
    var password = Environment.GetEnvironmentVariable("KCC_E2E_MEMBER_PASSWORD");
    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
    {
        throw new InvalidOperationException("Set KCC_E2E_MEMBER_USERNAME and KCC_E2E_MEMBER_PASSWORD for the signed-in captures.");
    }

    await OpenAsync(page, "/account/login");
    await page.FillAsync("input[name='UserName']", username);
    await page.FillAsync("input[name='Password']", password);
    await page.ClickAsync("form button[type='submit']");
    await page.WaitForURLAsync(url => !url.Contains("/account/login", StringComparison.OrdinalIgnoreCase));
}
```

- [ ] **Step 5: Add the project to the solution and build**

```bash
dotnet sln KitchenCommandCenter.sln add --solution-folder tests tests/KCC.ReferenceCapture/KCC.ReferenceCapture.csproj
dotnet build tests/KCC.ReferenceCapture/KCC.ReferenceCapture.csproj
```

Expected: `Build succeeded` with 0 warnings and 0 errors. If `dotnet sln add` rejects `--solution-folder`, add
the project with `dotnet sln KitchenCommandCenter.sln add tests/KCC.ReferenceCapture/KCC.ReferenceCapture.csproj`
and move it under the `tests` folder by hand, matching how `KCC.E2ETests` is nested in the `.sln`.

- [ ] **Step 6: Confirm the whole solution still builds**

```bash
dotnet build KitchenCommandCenter.sln
```

Expected: `Build succeeded`, 0 warnings. The central `Microsoft.Playwright` pin equals TUnit.Playwright's own
dependency, so `KCC.E2ETests` restores the same version it did before.

- [ ] **Step 7: Commit**

```bash
git add Directory.Packages.props KitchenCommandCenter.sln tests/KCC.ReferenceCapture
git commit -m "Add Reference Capture Tool"
```

---

### Task 4: Capture the Xperience reference set

**Files:** none committed by this task. Output goes to `docs/replatform/reference/xperience-final/`, which is
committed with the plans.

- [ ] **Step 1: Start the Xperience site**

In a separate terminal:

```bash
cd src/KCC.Web
dotnet watch --non-interactive
```

Wait until `https://localhost:58671` serves the home page (`curl -sk -o /dev/null -w '%{http_code}\n' https://localhost:58671/` prints `200`).

- [ ] **Step 2: Make sure the seeded recipes and reviews are present**

```bash
curl -sk -X POST https://localhost:58671/api/dev/seed-recipes -o /dev/null -w '%{http_code}\n'
curl -sk -o /dev/null -w '%{http_code}\n' https://localhost:58671/recipes/fluffy-buttermilk-pancakes/classic-stack
```

Expected: `200` twice. The seeder is idempotent, so running it again is safe.

- [ ] **Step 3: Confirm the E2E member can sign in**

```bash
test -n "$KCC_E2E_MEMBER_USERNAME" && test -n "$KCC_E2E_MEMBER_PASSWORD" && echo creds-present
```

Expected: `creds-present`. If missing, they belong in `~/.zshenv` (see the README's "E2E seed member").

- [ ] **Step 4: Run the capture**

```bash
dotnet run --project tests/KCC.ReferenceCapture -- --out docs/replatform/reference/xperience-final
```

Expected: 48 file paths, then `48 screenshots written to docs/replatform/reference/xperience-final`.

- [ ] **Step 5: Spot-check the set**

```bash
find docs/replatform/reference/xperience-final -name '*.png' | wc -l
ls docs/replatform/reference/xperience-final
```

Expected: `48`, and four folders: `dark-desktop`, `dark-mobile`, `light-desktop`, `light-mobile`. Open
`light-desktop/recipe.png`, `dark-desktop/cook-mode.png` and `light-mobile/account.png` and confirm each shows
the expected page in the expected ramp (star ratings on the recipe, the cook-mode dialog, the signed-in account
page). A screenshot of the login page where `account.png` should be means sign-in failed — fix the member and
re-run.

- [ ] **Step 6: Stop the site**

Stop `dotnet watch` (Ctrl+C). Phase 0 is complete: set this file's **Status** line to `done` with the date, and
continue with the Phase 1 plan in this folder.
