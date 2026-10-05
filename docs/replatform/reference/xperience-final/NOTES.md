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
- The header is the pad and differs from the reference by design: a torn notepad whose menus are cards in its stack,
  with no dashed rule and no ramp toggle, Sign in for Login and My kitchen for Account. On a phone it keeps one row,
  with search and Menu.
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
- `VariantDetail.SaturatedFat` has an empty value and renders as its key, which shows only when nutrition exists.

## Home, from Phase 6

The home is re-authored in Umbraco blocks: a rich text, two card grids, a stacker and a rich text. Expected
differences:

- The welcome heading and the placeholder paragraphs sit on torn sheets, `max-w-2xl` wide, instead of on the bare
  desk. The heading is display size, 40 / 48; Xperience's content set an inline 60px span. The placeholder fills two
  sheets of five paragraphs.
- The stacker's heading is a section name above its text sheet and cards, not a display heading beside them.
- Sections sit 72px apart. Xperience's padding settings left 48px between them.
- Desk sections have no fill, so the desk grain runs through them. Xperience painted them `bg-desk`, which hid the
  grain: flat rectangles in light, near-black ones in dark. The paper band stays.
- Card subheadings are body text. Xperience set them in the heading face.
- A grid card's heading and subheading sit on the rule. Xperience's rested 4px under it until the card was hovered.
- Every sheet and card takes the next tear of one cycle down the page. Xperience restarted at 1 in each grid, and
  its "How About Something Sweeter?" cards hashed theirs from each heading.
- Every card's "View Recipes" goes to `/recipes/`. Xperience's went to Home.
- The stacker has no image. Xperience's test image did not render either.
- The grid cards still show no wash, as in Xperience: the kit's capped corner washes render nothing until their
  own fix lands.
