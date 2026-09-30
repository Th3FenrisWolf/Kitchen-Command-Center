# Xperience reference set — xperience-final

48 full-page PNGs of the Xperience site at tag `xperience-final` (d9bcb09), captured 2026-09-23 from the dev
site (`dotnet watch`, https://localhost:58671) after `POST /api/dev/seed-recipes`, with
`tests/KCC.ReferenceCapture` at da95d98 on branch `replatform`:

    dotnet run --project tests/KCC.ReferenceCapture -- --out <dir> [--base-url <url>] [--only <name>,…]

Layout: `<theme>-<viewport>/<capture>.png`; themes `light`, `dark`; viewports `desktop` 1440×900, `mobile` 390×844;
captures `home recipes recipe variant cook-mode login registration-complete not-found account settings
create-recipe add-variant`. `--only` (e.g. `recipe,variant,cook-mode`) skips sign-in when no signed-in capture is
selected, which is what Phase 2 needs before sign-in exists.

## How the tool shoots

- Waits for fonts, the dev SSR-style handoff, and network idle, so client-fetched sections (reviews, cook notes)
  are in the picture.
- Validates every navigation: HTTP 200 (404 for `not-found`) and the final URL path equal to the capture's path
  (case-insensitive, trailing slash ignored). A wrong page fails the run, naming the capture.
- Hides dev-only overlays while shooting (MiniProfiler's `.mp-results`, the Vue DevTools button).
- `cook-mode` is viewport-sized, not full-page: the dialog is a fixed full-screen overlay. It is opened by a
  dispatched click on `[data-test='cook-mode-open-desktop']`, because the only trigger is hidden below the lg
  breakpoint (1024px) — the mobile cook-mode shot is a state phones cannot reach on this site.

## Expected differences when comparing the Umbraco site

- Review cards show the author as "(deleted)" on both sites: neither seeder's reviews reference a real member. The
  Umbraco seeder's two real authors (spec §12) are set on some recipes and their variants, not on reviews.
- Review dates read the day the seeder ran, not "SEP 1, 2026".
- The recipe and variant rating (4.3 from a 4-star and a 4.5-star review) holds now that ratings count published
  variants only (spec §17): both reviews are on a published variant.
- Seeded variants have no nutrition, so the nutrition sheet shows its empty state.
- On mobile, the torn strip under the header's dashed rule differs at its bottom-left corner: a 3×4 px patch at
  x 18–20, y 114–117, by at most 34 levels in light and 26 in dark. It has been there since Phase 1, and its cause is
  not traced.
- `not-found` shows the hard-coded fallback text: the 404 page text was never serialized (spec §17). Umbraco
  serves the 404 node's baseline content.
- `registration-complete` text changes in Phase 4 (the account waits for approval, spec §8).
- The footer reads "© 2025".
- The browser console logs a hydration-mismatch warning (`href="/"` vs `"~/"`) from Kentico's `~/` URLs; the
  port removes them (spec §6.3).
- Three UI strings have empty values and render as their keys: `Theme.SwitchToDark` and `Theme.SwitchToLight`
  (the theme toggle's labels, so not visible) and `VariantDetail.SaturatedFat` (only shown when nutrition
  exists).
