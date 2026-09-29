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
- On mobile, the torn strip under the header's dashed rule differs at both bottom corners: a 3×4 px patch at
  x 18–20, y 114–117, by at most 34 levels in light and 26 in dark, and a 3×7 px patch at x 371–373, y 111–117, by
  at most 17 in light and 12 in dark. Both have been there since Phase 1, and their cause is not traced. On signed-in
  mobile pages the header wraps onto a second row and the strip sits 48 px lower, where the two patches differ by at
  most 11 and 7 levels in light and 10 and 6 in dark.
- `recipes` holds 25 recipes, not 27: the reference also held two hand-made recipes, Egg Skillet and Mac & Cheese, so
  the cards after Crispy Roasted Chickpeas each move up one place, and the mobile page is 92 px taller because
  full-height cards fill the two short cards' slots. Its spotlight reads 5.0 · 3, not 5.0 · 1: the seed gives
  Legendary Lasagna three 5-star reviews.
- `not-found` shows the hard-coded fallback text: the 404 page text was never serialized (spec §17). Umbraco
  serves the 404 node's baseline content.
- `registration-complete` says the account is waiting for the owner's approval (spec §8), and its label's icon is an
  hourglass, not an envelope. The longer text takes one more line, two on desktop and three on mobile, so the sheet
  is one 24 px rule taller and its button sits one rule lower.
- `account` has the E2E member's name, "E2E Member", as its heading, where the reference's nameless member left the
  heading empty. The name adds two rules (48 px) to the member sheet and moves everything below it down 48 px, so the
  mobile page is 925 px tall, not 877. "Member since" reads the month the seeder created the E2E member, not July
  2026. The creations list is empty on both.
- `settings` fills First name and Last name with "E2E" and "Member", which the reference left empty, and the email
  reads `<username>@example.test`, not `<username>@kcc.test`.
- `account`'s Sign out link and `settings`' Sign out pill are buttons in small forms since Phase 4 (spec §8), and look
  as they did.
- The footer reads "© 2025".
- The browser console logs a hydration-mismatch warning (`href="/"` vs `"~/"`) from Kentico's `~/` URLs; the
  port removes them (spec §6.3). The Umbraco home page, empty until Phase 6, logs a different one whether or not a
  member is signed in: "Hydration children mismatch", with "Server rendered element contains fewer child nodes than
  client vdom".
- Three UI strings have empty values and render as their keys: `Theme.SwitchToDark` and `Theme.SwitchToLight`
  (the theme toggle's labels, so not visible) and `VariantDetail.SaturatedFat` (only shown when nutrition
  exists).
