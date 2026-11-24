import { readFileSync } from 'node:fs'
import { relative } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { files } from '../../support/files'

const WEB = fileURLToPath(new URL('../../../../src/KCC.Web/', import.meta.url))

// The pad is sticky over every page, so anything else that sticks has to sit below it or slide under it. A class token
// in markup, or the declaration in CSS; prose that mentions stickiness is not a sticky element.
const STICKY = /(?<=["'\s])(?:[^\s"':]+:)*sticky(?=["'\s])|position:\s*sticky/
const STICKY_TOKEN = /^((?:[^\s:]+:)*)sticky$/
const SHOWN_PAD_TOP = /^\[main:not\(\.is-tucked\)_&\]:top-\[calc\([^\]\s]*var\(--pad-h,0px\)\)\]$/
const CLASS_STRING = /"([^"]*)"|'([^']*)'/g
const CSS_RULE = /([^{}]+)\{([^{}]*)\}/g
const THE_PAD = 'Features/Components/Header/AppHeader.Component.vue'

const CLASS_ATTRIBUTE = /(?<![\w-])(:|v-bind:)?class\s*=\s*(?:"([^"]*)"|'([^']*)')/g
const BOUND_STRING = /'([^']*)'|`([^`]*)`/g
const STICKY_TOKENS = /(?<=["'`\s])(?:[^\s"'`:]+:)*sticky(?=["'`\s])/g

function classLists(source: string): string[][] {
  return [...source.matchAll(CLASS_ATTRIBUTE)].map(([, bound, double, single]) => {
    const value = double ?? single ?? ''
    const text = bound
      ? [...value.matchAll(BOUND_STRING)].map(([, quoted, template]) => quoted ?? template).join(' ')
      : value
    return text.split(/\s+/).filter(Boolean)
  })
}

// Each sticky token needs the shown-pad top in the same element's classes, under the same variants or none. A sticky
// token outside a class attribute cannot be paired with its element, so it is reported too.
function unawareTokens(source: string): string[] {
  const inLists = classLists(source).flatMap((tokens) =>
    tokens.flatMap((token) => {
      const variants = STICKY_TOKEN.exec(token)?.[1]
      if (variants === undefined) return []
      const shown = tokens.some(
        (other) =>
          (other.startsWith(variants) && SHOWN_PAD_TOP.test(other.slice(variants.length))) || SHOWN_PAD_TOP.test(other),
      )
      return shown ? [] : [token]
    }),
  )
  const outside = [...source.replaceAll(CLASS_ATTRIBUTE, '').matchAll(STICKY_TOKENS)].map(
    ([token]) => `${token} outside a class`,
  )
  return [...inLists, ...outside]
}

// Each sticky CSS rule needs a rule for the same selector under a shown pad that offsets it by the pad's height.
function unawareRules(source: string): string[] {
  const rules = [...source.matchAll(CSS_RULE)].map(([, selector, body]) => ({ selector: selector!.trim(), body: body! }))
  return rules
    .filter(({ body }) => /position:\s*sticky/.test(body))
    .filter(
      ({ selector }) =>
        !rules.some(
          (rule) =>
            rule.selector.includes('main:not(.is-tucked)') &&
            rule.selector.includes(selector) &&
            /top:[^;]*var\(--pad-h/.test(rule.body),
        ),
    )
    .map(({ selector }) => selector)
}

const unaware = (path: string, source: string) =>
  path.endsWith('.css') ? unawareRules(source) : [...unawareTokens(source), ...unawareRules(styleBlocks(source))]

const styleBlocks = (source: string) =>
  [...source.matchAll(/<style[^>]*>([\s\S]*?)<\/style>/g)].map(([, css]) => css).join('\n')

const sticking = [...files(`${WEB}Features`, /\.(vue|cshtml|css)$/)]
  .map((file) => ({ path: relative(WEB, file).replaceAll('\\', '/'), source: readFileSync(file, 'utf8') }))
  .filter(({ path, source }) => path !== THE_PAD && STICKY.test(source))

describe('sticky neighbours', () => {
  it('finds the library filters and both halves of the home stacker', () => {
    expect(sticking.map(({ path }) => path).sort()).toEqual([
      'Features/Pages/Home/Blocks/StackerBlock.cshtml',
      'Features/Pages/RecipeSearch/RecipeSearchView.Component.vue',
      'Features/Widgets/Stacker/Stacker.Component.vue',
    ])
  })

  // The tuck toggles a class on <main> and never writes a property there, so a neighbour reads the pad's height
  // only through a selector that names it: a class change restyles just the elements its selectors match.
  it('sit below the pad only while it shows', () => {
    const found = sticking.flatMap(({ path, source }) => unaware(path, source).map((element) => `${path}: ${element}`))

    expect(found, 'add [main:not(.is-tucked)_&]:top-[calc(… var(--pad-h,0px))] to each sticky element').toEqual([])
  })

  it('judge each sticky element on its own, not by its file', () => {
    const aware = 'lg:sticky lg:[main:not(.is-tucked)_&]:top-[calc(2rem_+_var(--pad-h,0px))]'
    const source = [
      `<div class="${aware}"></div>`,
      '<div class="sticky top-0"></div>',
      `<div :class="['sticky', '[main:not(.is-tucked)_&]:top-[calc(var(--pad-h,0px))]']"></div>`,
      `<div :class="['sticky', open && 'block']"></div>`,
    ].join('')

    expect(unawareTokens(source)).toEqual(['sticky', 'sticky'])
    expect(unawareTokens(`<div class="${aware}"></div>`)).toEqual([])
    expect(
      unawareRules('.a { position: sticky; top: 0 } main:not(.is-tucked) .a { top: var(--pad-h) } .b { position: sticky }'),
    ).toEqual(['.b'])
  })

  it('read no property the tuck writes', () => {
    expect(sticking.filter(({ source }) => source.includes('--pad-offset')).map(({ path }) => path)).toEqual([])
  })
})
