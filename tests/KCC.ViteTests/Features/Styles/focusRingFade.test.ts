import { readFileSync } from 'node:fs'
import { join, relative } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { files } from '../../support/files'

const WEB = fileURLToPath(new URL('../../../../src/KCC.Web/', import.meta.url))
const FEATURES = join(WEB, 'Features')

const GATE = ':root:focus-within '
const OUTLINE_TRANSITION = /(?<![\w-])outline(?:-color|-offset|-width)?(?![\w-])|(?<![\w-])all(?![\w-])/
const TRANSITION = /(?<![\w-])transition(?:-property)?\s*:\s*([^;]+)/g

type Rule = { path: string; selectors: string[]; body: string }

const cssOf = (path: string) => {
  const text = readFileSync(path, 'utf8')
  if (!path.endsWith('.vue')) return text
  return [...text.matchAll(/<style[^>]*>([\s\S]*?)<\/style>/g)].map(([, css]) => css).join('\n')
}

const splitSelectors = (text: string) => {
  const parts = ['']
  let depth = 0
  for (const char of text) {
    if (char === '(') depth++
    else if (char === ')') depth--
    if (char === ',' && depth === 0) parts.push('')
    else parts[parts.length - 1] += char
  }
  return parts
}

const leafRules = (path: string): Rule[] =>
  [
    ...cssOf(path)
      .replace(/\/\*[\s\S]*?\*\//g, '')
      .matchAll(/([^{}]+)\{([^{}]*)\}/g),
  ].map(([, selector, body]) => ({
    path: relative(WEB, path),
    selectors: splitSelectors(selector!)
      .map((part) => part.replace(/\s+/g, ' ').trim())
      .filter(Boolean),
    body: body!,
  }))

const rules = [...files(FEATURES, /\.(css|vue)$/)].flatMap(leafRules)

const fadesTheRing = (rule: Rule) => [...rule.body.matchAll(TRANSITION)].some(([, value]) => OUTLINE_TRANSITION.test(value!))

// With scripts off nothing holds the parser on the head's stylesheets, so Chromium can style the body once
// before they apply. An outline transition then starts from the UA's currentColor outline and fades a ring in
// around every control on the first paint.
describe('the focus-ring fade', () => {
  it('runs only once something on the page has focus', () => {
    const ungated = rules
      .filter(fadesTheRing)
      .flatMap((rule) =>
        rule.selectors.filter((selector) => !selector.startsWith(GATE)).map((selector) => `${rule.path}: ${selector}`),
      )

    expect(ungated).toEqual([])
  })

  it('still fades every focusable the base layer rings', () => {
    const gated = rules.filter(fadesTheRing).flatMap((rule) => rule.selectors)

    expect(gated).toContain(`${GATE}:where(button, a, input, textarea, select, [tabindex]:not([tabindex='-1']))`)
    expect(gated).toContain(`${GATE}.kcc-slip--lift > :not(.kcc-blank)`)
  })
})
