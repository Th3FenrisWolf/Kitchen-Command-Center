# Contract: the visit

How a visit travels from the server to the page. Tests and E2E fixtures may write the cookie directly to place a
reader anywhere in a visit.

## Cookie `kcc-visit`

| Attribute | Value |
|---|---|
| Value | `1.<seed>.<start>`: format version `1`, `seed` as 8 lower-case hex digits, `start` as Unix seconds (server clock) |
| `Max-Age` | `1800`, renewed by every page the server renders and by the client heartbeat |
| `Path` | `/` |
| `SameSite` | `Lax` |
| `Secure` | when the request is HTTPS |
| `HttpOnly` | no: the heartbeat rewrites it |

- The server writes it from `Layout.cshtml` through `VisitClock`, before the response starts. A response that has
  already started writes nothing and renders with the visit it read.
- Any value that does not parse, a version other than `1`, or a `start` in the future or more than a year back
  starts a new visit. Nothing throws.
- The heartbeat (`Features/Bloom/visitHeartbeat.ts`, started from `Main.ts`) copies the current value unchanged with
  a fresh `Max-Age`, every 60 seconds and on `pagehide`. It never creates a visit and never changes a value.
- The value identifies no one and joins nothing: no consent banner (A3).

Example, a reader five minutes in:

```text
Set-Cookie: kcc-visit=1.9f3a0c71.1792000000; Max-Age=1800; Path=/; SameSite=Lax; Secure
```

## Page markup

`Layout.cshtml` writes the elapsed time on the root, outside the Vue render:

```html
<html lang="en" data-theme="light" style="--wash-elapsed: 124.3s">
```

- Seconds since `start`, one decimal, clamped to `[0, 300]`.

`SsrHtmlContent` adds the bloom inputs to the `server-content` JSON the client hydrates from:

```json
{ "headerContent": "…", "bodyContent": "…", "footerContent": "…", "isPreview": false,
  "bloom": { "seed": 2671381617, "page": "/recipes/brown-butter-gnocchi" } }
```

- `seed` is the visit seed as an unsigned 32-bit integer.
- `page` is the request path, lower-case, without a trailing slash; `/` for the home page.

## SSR sidecar

- `POST /render` takes `bloom: { seed, page }` beside the content regions; `createApp` provides it.
- After `renderToString`, the sidecar runs `scheduleWashes` over the washes the render collected and returns
  `schedule`, a CSS string, beside `html` and `css`.
- `VueSsrService`'s cache key includes `seed` and `page`, and it caches `schedule` with the markup. Two visitors never
  share a cached render.
- `Layout.cshtml` calls the SSR in its opening code block (header, `RenderBody()` and footer captured first), writes
  `<style id="kcc-bloom-schedule">` in `<head>`, and writes the rendered app in `<body>` as today. When the sidecar
  falls back to client rendering there is no schedule, and each wash blooms at its `--wash-fallback-at`.
