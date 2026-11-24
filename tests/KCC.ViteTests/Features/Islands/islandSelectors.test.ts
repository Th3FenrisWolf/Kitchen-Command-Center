import { readFileSync } from 'node:fs'
import { join, relative } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { files } from '../../support/files'

const WEB = fileURLToPath(new URL('../../../../src/KCC.Web/', import.meta.url))
const FEATURES = join(WEB, 'Features')
const STYLES = join(FEATURES, 'Styles')
const TORN = join(STYLES, 'Torn')

interface Source {
  path: string
  css: string
}

const withoutComments = (css: string) => css.replace(/\/\*[\s\S]*?\*\//g, '')

const styleSheets = (): Source[] => [
  ...[...files(STYLES, /\.css$/)].map((path) => ({ path, css: withoutComments(readFileSync(path, 'utf8')) })),
  ...[...files(FEATURES, /\.vue$/)].flatMap((path) =>
    [...readFileSync(path, 'utf8').matchAll(/<style[^>]*>([\s\S]*?)<\/style>/g)].map(([, css]) => ({
      path,
      css: withoutComments(css!),
    })),
  ),
]

const selectorLists = (css: string) =>
  [...css.matchAll(/([^{};]+)\{/g)]
    .map(([, prelude]) => prelude!.trim().replace(/\s+/g, ' '))
    .filter((prelude) => !prelude.startsWith('@'))
    .map((prelude) => prelude.split(/,(?![^(]*\))/).map((selector) => selector.trim()))

// A combinator or a structural pseudo-class: the selectors that read an element's parent, siblings or position,
// all of which a `<kcc-island>` wrapper changes for an island root.
const RELATIONAL = /[>+~]|:(?:first|last|nth|only)-|:empty|:has\(/

// An island root sits at #app's top level (the header), as <main>'s child (a page view) or in the stacker block's
// grid. A selector that steps down from #app or <main> must also step through the wrapper.
const ISLAND_PARENT = /(#app|\.content-grid|(?<![\w-])main(?![\w-]))\s*>\s*/

const throughWrapper = (selector: string) => selector.replace(ISLAND_PARENT, '$1 > kcc-island > ')

const KIT =
  'both ends lie inside one kit structure (a slip, sheet, row, prose block or control); no island root is a child or sibling inside one'
const TEMPLATE = "both ends lie inside one component's own template"
const UTILITY = 'a utility no Razor element that holds an island root carries'
const REPEATED = 'repeated through kcc-island in the same rule'

const CHECKED: [selector: string | RegExp, how: string][] = [
  [':where(#app > *)', REPEATED],
  ['.content-grid > .thin', REPEATED],
  ['.content-grid > :not(.breakout, .full-width)', REPEATED],
  ['.content-grid > .breakout', REPEATED],
  ['.content-grid > .full-width', REPEATED],
  [':where(& > :not(:last-child))', UTILITY],
  [/^\.kcc-tear-[\w-]+ > \.kcc-(?:torn > \.kcc-sheet|fall > span|blank > span)$/, KIT],
  ['.kcc-blank > span', KIT],
  ['.kcc-fall > span', KIT],
  ['.kcc-prose > :not(p:empty) ~ *', KIT],
  ['.kcc-prose > p:empty', KIT],
  ['.kcc-prose ol > li::marker', KIT],
  ['.kcc-row > .kcc-btn--icon', KIT],
  ['.kcc-row > .kcc-btn--text', KIT],
  ['.kcc-row > .kcc-control', KIT],
  ['.kcc-row > :is(.kcc-kick, .kcc-lbl, .kcc-meta, .kcc-title, .kcc-heading)', KIT],
  ['.kcc-row > form > .kcc-btn--icon', KIT],
  ['.kcc-row > form > .kcc-btn--text', KIT],
  ['.kcc-row > form > .kcc-control', KIT],
  ['.kcc-sheet > :where(:not(.kcc-washes))', KIT],
  ['.kcc-sheet > .kcc-stat', KIT],
  ['.kcc-slip--fill > .kcc-torn', KIT],
  ['.kcc-slip--fill > .kcc-torn > .kcc-sheet', KIT],
  ['.kcc-slip--lift > :not(.kcc-blank)', KIT],
  ['.kcc-slip--lift:focus-within > :not(.kcc-blank)', KIT],
  ['.kcc-slip--lift:hover > :not(.kcc-blank)', KIT],
  [':root:focus-within .kcc-slip--lift > :not(.kcc-blank)', KIT],
  ['.kcc-slip:has(> .kcc-torn > .kcc-sheet--margin)', KIT],
  [':not(.kcc-slip) > .kcc-torn', KIT],
  ['.kcc-tilewrap > .kcc-tape', KIT],
  [":root[data-theme='dark'] .kcc-tilewrap > .kcc-tape", KIT],
  ['.kcc-check input:focus-visible + .kcc-box', KIT],
  ['.kcc-check li.kcc-done > :not(.kcc-q, .kcc-box)', KIT],
  ['.kcc-check li:has(input:disabled)', KIT],
  ['.kcc-seg button:first-of-type', KIT],
  ['.kcc-seg button:last-of-type', KIT],
  ['.kcc-steps li + li', KIT],
  ['.pad-top > .kcc-torn > .kcc-sheet', TEMPLATE],
  ['.pad-line > .kcc-num', TEMPLATE],
  ['.pad-line > i', TEMPLATE],
  ['.pad-line:hover > .pad-line-label', TEMPLATE],
]

const howOf = (selector: string) =>
  CHECKED.find(([checked]) => (typeof checked === 'string' ? checked === selector : checked.test(selector)))?.[1]

const audited = () =>
  styleSheets().flatMap(({ path, css }) =>
    selectorLists(css).flatMap((list) =>
      list
        .filter((selector) => RELATIONAL.test(selector))
        .map((selector) => ({ file: relative(WEB, path).replaceAll('\\', '/'), selector, list })),
    ),
  )

const layerBlocks = (css: string, layer: string) => {
  const blocks: string[] = []
  for (const match of css.matchAll(new RegExp(`@layer\\s+${layer}\\s*\\{`, 'g'))) {
    let depth = 1
    let at = match.index! + match[0].length
    const start = at
    while (depth > 0 && at < css.length) {
      if (css[at] === '{') depth++
      else if (css[at] === '}') depth--
      at++
    }
    blocks.push(css.slice(start, at - 1))
  }
  return blocks
}

describe('the island wrapper', () => {
  it('is display: contents in the kit base layer', () => {
    const base = [...files(TORN, /\.css$/)].flatMap((path) =>
      layerBlocks(withoutComments(readFileSync(path, 'utf8')), 'base'),
    )
    expect(base.some((block) => /(?:^|[\s}])kcc-island\s*\{\s*display:\s*contents;?\s*\}/.test(block))).toBe(true)
  })
})

describe('child, sibling and structural selectors', () => {
  it('are each on the checked list with how they still reach an island root', () => {
    const unchecked = audited()
      .filter(({ selector }) => !selector.includes('kcc-island') && !howOf(selector))
      .map(({ file, selector }) => `${file}: ${selector}`)
    expect(unchecked).toEqual([])
  })

  it('step through the wrapper wherever they step down from an element an island root sits in', () => {
    const missing = audited()
      .filter(({ selector }) => ISLAND_PARENT.test(selector) && !selector.includes('kcc-island'))
      .filter(({ selector, list }) => howOf(selector) !== REPEATED || !list.includes(throughWrapper(selector)))
      .map(({ file, selector }) => `${file}: ${selector} needs ${throughWrapper(selector)} beside it`)
    expect(missing).toEqual([])
  })

  it('name the wrapper only as the step-through of a checked selector', () => {
    const stray = audited()
      .filter(({ selector }) => selector.includes('kcc-island'))
      .filter(({ selector, list }) => !list.some((other) => other !== selector && throughWrapper(other) === selector))
      .map(({ file, selector }) => `${file}: ${selector}`)
    expect(stray).toEqual([])
  })

  it('list no selector the stylesheets no longer hold', () => {
    const found = audited().map(({ selector }) => selector)
    const stale = CHECKED.filter(([checked]) =>
      typeof checked === 'string' ? !found.includes(checked) : !found.some((selector) => checked.test(selector)),
    ).map(([checked]) => String(checked))
    expect(stale).toEqual([])
  })
})
