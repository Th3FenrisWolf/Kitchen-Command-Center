# Contract: Configuration

Every setting has a default in its options class, so `appsettings.json` gains no section. Each one can be overridden
the usual way: user-secrets in development, environment variables (`Section__Key`) in the container.

## `RecipeImport` (new, `RecipeImportOptions`)

| Key | Default | Meaning |
|---|---|---|
| `Model` | `claude-opus-5-5` | The reader model. It must be one of the models in research R5's table: any other id has no price profile and turns the import off. The evaluation task replaces this default with its measured choice |
| `Effort` | `low` | Sent only to models that take an effort setting (research R5) |
| `MonthlyCeilingUsd` | `2.00` | Reads stop starting once the month's total reaches this |
| `MaxImageEdge` | `2000` | The longest photo edge, in pixels, that the browser sends. The model's own limit caps it. The evaluation task changes this default only to an edge that a run measured (research R16) |
| `ReadTimeoutSeconds` | `85` | How long one read may run. It is the read's only limit: a cancelled or dropped request does not stop a read (research R4). Keep it under Cloudflare's 100-second origin limit |
| `StandInReader` | `false` | Uses the canned reader. It has effect only in the Development and Testing environments |
| `StandInDelaySeconds` | `0` | How long the canned reader waits before it answers, for the manual check of a slow read (quickstart scenario 12). It has effect only with the canned reader |

`MaxTokens` (12,000) and the reservation constants are code, not configuration (research R5). So are the photo limits:
2.2 MB a photo in the browser, and 4 MB a photo and 22 MB together at the server (research R6).

## `RateLimits` (existing, `RateLimitOptions`)

| Key | Default | Meaning |
|---|---|---|
| `ImportsPerHour` | `10` | Reads per client per hour (new) |

## `Anthropic` (existing, `AnthropicOptions`)

`ApiKey` turns the photo import on: the Anthropic reader reports `IsConfigured` only when the key is set and
`RecipeImport:Model` has a price profile. Without the key, the wizards show no photo choice, as recipe icons fall back
today. `Model` stays the icon model and is unrelated to the reader.

The owner's dev site holds the key in KCC.Web's user-secrets, so it reads for real whenever the stand-in reader is
off, against its own ledger and its own ceiling, apart from the Pi's (spec A2). Behind both ledgers, one organization
spend limit of about US$20 a month in the Anthropic Console caps the account. The owner sets it and raises it before
each later evaluation ([quickstart.md](../quickstart.md#turning-it-on-at-the-pi)).

## The Pi

The owner added `ANTHROPIC_API_KEY` to `/srv/kcc/repo/deploy/.env` on 2026-10-05, before the client had a timeout.
This change adds the timeout (research R4).

`deploy/compose.yaml` passes one new variable to the app:

```yaml
RecipeImport__MonthlyCeilingUsd: ${KCC_IMPORT_CEILING_USD:-2}
```

To change the ceiling, set `KCC_IMPORT_CEILING_USD` in `/srv/kcc/repo/deploy/.env` and run
`sudo -u kcc -H kcc up -d --wait`.

## Tests

- No test host sets an API key: a key, even a fake one, would make every submission call Anthropic for its icon.
- The integration tests run on the one shared `UmbracoSite`, and boot no other site. It sets
  `RecipeImport:MonthlyCeilingUsd` to a non-default `1.00`, which proves that the option binds, pins
  `RecipeImport:StandInReader` to `false` and `RecipeImport:StandInDelaySeconds` to `0` whatever the environment holds,
  and wraps the composer's reader in a scriptable fake. The fake delegates until a test switches it on, so the import
  stays unconfigured for every other test (research R15).
- The E2E process sets `RecipeImport__StandInReader=true`, `RecipeImport__StandInDelaySeconds=0`,
  `RecipeImport__MonthlyCeilingUsd=2` and `RateLimits__ImportsPerHour=1000`, over whatever the environment holds.
- The manual scenarios pass their `RecipeImport__*` variables on the site's start command and never export them, so
  a later test run in the same shell does not inherit them ([quickstart.md](../quickstart.md)).
