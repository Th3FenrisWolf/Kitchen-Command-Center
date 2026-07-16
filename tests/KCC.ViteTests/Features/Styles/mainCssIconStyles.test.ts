import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

// Main.css must @import every FontAwesome style the app actually renders. FA7 loads one
// @font-face per style file; if a style used in markup (e.g. `fa-regular`) is never imported,
// those icons silently fall back to the only loaded classic face (solid-900) and render as
// their FILLED counterpart. That is the root cause of empty rating stars showing as full,
// and of the "not cooked" / unchecked toggles rendering identically to their solid state.
const mainCss = readFileSync(
  fileURLToPath(new URL('../../../../src/KCC.Web/Features/Styles/Main.css', import.meta.url)),
  'utf8',
)

describe('Main.css FontAwesome imports', () => {
  // Classic styles referenced in Features/**: `fa-solid`, `fa-regular`, `fa-duotone`.
  it.each(['fontawesome', 'solid', 'regular', 'duotone'])(
    'imports the "%s" FontAwesome stylesheet',
    (style) => {
      expect(mainCss).toContain(`@fortawesome/fontawesome-pro/css/${style}.css`)
    },
  )
})
