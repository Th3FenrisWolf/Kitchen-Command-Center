import { existsSync, readFileSync } from 'node:fs'
import { createRequire } from 'node:module'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { subsetFontAwesome } from '~/Styles/FontAwesome/subsetFontAwesome'
import { files } from '../../support/files'

// Main.css must @import every FontAwesome style the app actually renders. FA7 loads one
// @font-face per style file; if a style used in markup (e.g. `fa-regular`) is never imported,
// those icons silently fall back to the only loaded classic face (solid-900) and render as
// their FILLED counterpart. That is the root cause of empty rating stars showing as full,
// and of the "not cooked" / unchecked toggles rendering identically to their solid state.
const WEB = fileURLToPath(new URL('../../../../src/KCC.Web/', import.meta.url))
const mainCss = readFileSync(join(WEB, 'Features/Styles/Main.css'), 'utf8')

const require = createRequire(import.meta.url)
const fontAwesomeCssDir = dirname(require.resolve('@fortawesome/fontawesome-pro/css/fontawesome.css'))
const STYLES = ['solid', 'regular', 'light', 'thin', 'duotone', 'brands', 'semibold']

const usedStyles = new Set<string>()
for (const path of files(join(WEB, 'Features'), /\.(vue|cshtml|ts|cs)$/)) {
  for (const match of readFileSync(path, 'utf8').matchAll(/(?<![\w-])fa-([a-z]+)(?![\w-])/g)) {
    if (STYLES.includes(match[1]!)) usedStyles.add(match[1]!)
  }
}

describe('Main.css FontAwesome imports', () => {
  it('finds the styles the sources render', () => {
    expect([...usedStyles]).toEqual(expect.arrayContaining(['solid', 'regular', 'duotone']))
  })

  it.each(['fontawesome', ...usedStyles])('imports the "%s" FontAwesome stylesheet', (style) => {
    expect(existsSync(join(fontAwesomeCssDir, `${style}.css`))).toBe(true)
    expect(mainCss).toContain(`@fortawesome/fontawesome-pro/css/${style}.css`)
  })

  it.each([...usedStyles])('keeps the "%s" style rule through the icon subset', (style) => {
    const css = readFileSync(join(fontAwesomeCssDir, `${style}.css`), 'utf8')
    expect(subsetFontAwesome(css)).toContain(`.fa-${style}`)
  })
})
