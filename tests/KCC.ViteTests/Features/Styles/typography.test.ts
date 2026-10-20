import { existsSync, readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

const stylesDir = new URL('../../../../src/KCC.Web/Features/Styles/', import.meta.url)
const read = (path: string) => readFileSync(fileURLToPath(new URL(path, stylesDir)), 'utf8').replace(/\/\*[\s\S]*?\*\//g, '')

const theme = read('TailwindConfig.css')
const tokens = read('Torn/Tokens.css')
const typography = read('Typography.css')
const kit = read('Torn/Kit.css')

type Rule = { selectors: string[]; atRules: string[]; declarations: Record<string, string> }

const rulesOf = (css: string): Rule[] => {
  const rules: Rule[] = []
  const open: string[] = []
  let prelude = ''
  for (const char of css) {
    if (char === '{') {
      open.push(prelude.trim())
      prelude = ''
    } else if (char === '}') {
      const selector = open.pop()!
      if (prelude.includes(':') && !selector.startsWith('@')) {
        const declarations = Object.fromEntries(
          prelude
            .split(';')
            .map((declaration) => declaration.split(/:(.*)/s).map((part) => part.trim().replace(/\s+/g, ' ')))
            .filter(([property, value]) => property && value),
        )
        rules.push({
          selectors: selector.split(',').map((part) => part.trim()),
          atRules: open.filter((at) => at.startsWith('@')).map((at) => at.replace(/\s+/g, ' ')),
          declarations,
        })
      }
      prelude = ''
    } else {
      prelude += char
    }
  }
  return rules
}

const fontSizesOf = (css: string, selector: string, media?: string) =>
  rulesOf(css)
    .filter((rule) => rule.selectors.includes(selector))
    .filter((rule) =>
      media ? rule.atRules.includes(`@media ${media}`) : !rule.atRules.some((at) => at.startsWith('@media')),
    )
    .map((rule) => rule.declarations['font-size'])
    .filter(Boolean)

const declaration = (css: string, property: string) =>
  css.match(new RegExp(`(?<![\\w-])${property.replace(/[*]/g, '\\*')}:\\s*([^;]+);`))?.[1]?.trim()

describe('the type scale tokens', () => {
  it('clear Tailwind’s default text sizes before declaring their own', () => {
    const block = theme.match(/@theme static\s*\{([\s\S]*?)\n\}/)![1]!
    expect(block.trim().startsWith('--text-*: initial;')).toBe(true)
  })

  it.each([
    ['display', '2.5rem', '3rem'],
    ['display-phone', '2.25rem', '3rem'],
    ['section', '2rem', '3rem'],
    ['heading', '1.5rem', '1.5rem'],
    ['title', '1.25rem', '1.5rem'],
    ['body', '1rem', '1.5rem'],
    ['caps', '0.875rem', '1.5rem'],
    ['readout', '4rem', '4.5rem'],
  ])('--text-%s is %s on %s', (role, size, lineHeight) => {
    expect(declaration(theme, `--text-${role}`)).toBe(size)
    expect(declaration(theme, `--text-${role}--line-height`)).toBe(lineHeight)
  })

  it.each([
    ['--control-md', '2.25rem'],
    ['--control-lg', '3rem'],
    ['--pill-sm', '1.5rem'],
    ['--icon-sm', '1.5rem'],
    ['--icon-md', '2.25rem'],
    ['--icon-lg', '3rem'],
  ])('%s is %s', (token, value) => {
    expect(declaration(theme, token)).toBe(value)
  })

  it('rules every sheet at 24px and drops the rule to the body text’s baseline', () => {
    expect(declaration(tokens, '--bl')).toBe('1.5rem')
    expect(declaration(tokens, '--rule-drop')).toBe('calc(var(--bl) / 2 + var(--k-hazelnut) * var(--text-body))')
  })
})

describe('the type roles', () => {
  it.each(['body', 'input', 'select', 'textarea'])('%s reads the body size', (selector) => {
    expect(fontSizesOf(typography, selector)).toEqual(['var(--text-body)'])
  })

  it.each([
    ['h1', 'display'],
    ['h2', 'section'],
    ['h3', 'heading'],
    ['h4', 'title'],
    ['h5', 'title'],
    ['h6', 'title'],
  ])('a bare %s takes the %s role', (selector, role) => {
    const sizes = rulesOf(typography)
      .filter((rule) => rule.selectors.includes(selector))
      .map((rule) => rule.declarations['font-size'])
    expect(sizes).toContain(`var(--text-${role})`)
  })

  it.each([
    ['.kcc-display', kit],
    ['h1', typography],
  ])('%s takes the phone display size below 768px and the display size from 768px', (selector, css) => {
    expect(fontSizesOf(css, selector)).toEqual(['var(--text-display-phone)'])
    expect(fontSizesOf(css, selector, '(min-width: 768px)')).toEqual(['var(--text-display)'])
  })

  it.each([
    ['.kcc-section', kit],
    ['h2', typography],
  ])('%s keeps the section size at every width', (selector, css) => {
    expect(fontSizesOf(css, selector)).toEqual(['var(--text-section)'])
    expect(fontSizesOf(css, selector, '(min-width: 768px)')).toEqual([])
  })
})

describe('the brand fonts', () => {
  it('declares Sono for meta and numbers and ships every font file it references', () => {
    expect(typography).toContain("font-family: 'Sono'")
    const files = [...typography.matchAll(/url\('\.\/fonts\/([^']+)'\)/g)].map((m) => m[1]!)
    expect(files.length).toBeGreaterThanOrEqual(8)
    for (const file of files) {
      expect(existsSync(fileURLToPath(new URL(`fonts/${file}`, stylesDir))), `${file} is missing`).toBe(true)
    }
  })
})
