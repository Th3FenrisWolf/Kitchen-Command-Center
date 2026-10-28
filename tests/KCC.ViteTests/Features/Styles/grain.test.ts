import { readFileSync } from 'node:fs'
import { join, relative } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { files } from '../../support/files'

const WEB = fileURLToPath(new URL('../../../../src/KCC.Web/', import.meta.url))
const FEATURES = join(WEB, 'Features')

const TOKENS = 'Features/Styles/Torn/Tokens.css'
const LAYOUT = 'Features/Pages/Shared/Layout.cshtml'

type Source = { path: string; text: string }
type Rule = { selectors: string[]; declarations: [property: string, value: string][] }

const SOURCE_NAMES = new Set(['--grain', '--crayon', '--desk-stripe'])
const WHOLE_NAME = /(?<![\w-])--(?:grain|crayon)(?![\w-])/
const TEXTURE = {
  bloom: /url\(\s*#kcc-bloom\s*\)/i,
  waxFlat: /url\(\s*#kcc-wax-flat\s*\)/i,
  turbulence: /feTurbulence/i,
  dataSvg: /url\(\s*["']?data:image\/svg/i,
  png: /url\(\s*["']?(?:data:image\/png|[^)"']*\.png)/i,
  repeatingLinear: /repeating-linear-gradient/i,
  repeatingRadial: /repeating-radial-gradient/i,
}

const normalise = (selector: string) =>
  selector
    .replace(/\s+/g, ' ')
    .replace(/\s*>\s*/g, ' > ')
    .trim()

const DARK = ":root[data-theme='dark'] "
const withDark = (selector: string) => [selector, DARK + selector]
const DESK = new Set(withDark('#app'))
const SURFACES = new Set([...DESK, '.kcc-wash', '.kcc-tile'])
const RAMPS = new Set([':root', ":root[data-theme='dark']"])
const TAPE = new Set(withDark('.kcc-tilewrap > .kcc-tape'))

const splitTop = (text: string, separator: string) => {
  const parts: string[] = []
  let depth = 0
  let quote = ''
  let part = ''
  for (const char of text) {
    if (quote) {
      if (char === quote) quote = ''
    } else if (char === '"' || char === "'") quote = char
    else if (char === '(') depth++
    else if (char === ')') depth--
    else if (char === separator && depth === 0) {
      parts.push(part)
      part = ''
      continue
    }
    part += char
  }
  parts.push(part)
  return parts
}

const declarationsOf = (body: string) =>
  splitTop(body, ';').flatMap((declaration): [string, string][] => {
    const colon = declaration.indexOf(':')
    if (colon < 0) return []
    const property = declaration.slice(0, colon).trim()
    const value = declaration.slice(colon + 1).trim()
    return property && value && !/\s/.test(property) ? [[property, value]] : []
  })

const rulesOf = (css: string): Rule[] => {
  const rules: Rule[] = []
  const open: { selector: string; declarations: [string, string][] }[] = []
  let prelude = ''
  let quote = ''
  for (const char of css.replace(/\/\*[\s\S]*?\*\//g, '')) {
    if (quote) {
      if (char === quote) quote = ''
      prelude += char
    } else if (char === '"' || char === "'") {
      quote = char
      prelude += char
    } else if (char === '{') {
      const parts = splitTop(prelude, ';')
      const selector = parts.pop()!.trim()
      open.at(-1)?.declarations.push(...declarationsOf(parts.join(';')))
      open.push({ selector, declarations: [] })
      prelude = ''
    } else if (char === '}') {
      const rule = open.pop()
      rule?.declarations.push(...declarationsOf(prelude))
      if (rule?.declarations.length)
        rules.push({ selectors: splitTop(rule.selector, ',').map(normalise), declarations: rule.declarations })
      prelude = ''
    } else {
      prelude += char
    }
  }
  return rules
}

const STYLE_BLOCK = /<style\b[^>]*>([\s\S]*?)<\/style>/gi
const STYLE_ATTRIBUTE = /(?<![\w-])style=(?:"([^"]*)"|'([^']*)')/gi

const split = ({ path, text }: Source) => {
  if (path.endsWith('.css')) return { rules: rulesOf(text), markup: '' }
  const rules = [...text.matchAll(STYLE_BLOCK)].flatMap((block) => rulesOf(block[1]!))
  const inline = [...text.matchAll(STYLE_ATTRIBUTE)].flatMap((attribute) => {
    const declarations = declarationsOf(attribute[1] ?? attribute[2]!)
    return declarations.length ? [{ selectors: ['[style]'], declarations }] : []
  })
  return { rules: [...rules, ...inline], markup: text.replace(STYLE_BLOCK, '') }
}

const referencesOf = (value: string) => [...value.matchAll(/var\(\s*(--[\w-]+)/g)].map((match) => match[1]!)
const hasLiteralTexture = (value: string) => Object.values(TEXTURE).some((pattern) => pattern.test(value))

const aliasesOf = (rules: Rule[]) => {
  const aliases = new Set<string>()
  const declarations = rules.flatMap((rule) => rule.declarations).filter(([property]) => property.startsWith('--'))
  for (let grew = true; grew;) {
    grew = false
    for (const [property, value] of declarations) {
      if (SOURCE_NAMES.has(property) || aliases.has(property)) continue
      const reaches = referencesOf(value).some((name) => SOURCE_NAMES.has(name) || aliases.has(name))
      if (reaches || hasLiteralTexture(value)) {
        aliases.add(property)
        grew = true
      }
    }
  }
  return aliases
}

export const scan = (sources: Source[]): string[] => {
  const parsed = sources.map((source) => ({ path: source.path, ...split(source) }))
  const aliases = aliasesOf(parsed.flatMap((file) => file.rules))
  const reported: string[] = []
  const report = (path: string, where: string, reason: string) => reported.push(`${path}: ${where}: ${reason}`)

  for (const { path, rules, markup } of parsed) {
    for (const rule of rules) {
      const where = rule.selectors.join(', ')
      const onlyOn = (allowed: Set<string>) => rule.selectors.every((selector) => allowed.has(selector))
      for (const [property, value] of rule.declarations) {
        if (SOURCE_NAMES.has(property)) {
          if (path !== TOKENS || !RAMPS.has(where)) report(path, where, `declares ${property} outside a Tokens.css ramp`)
          continue
        }
        const readsSource = referencesOf(value).some((name) => SOURCE_NAMES.has(name) || aliases.has(name))
        if (readsSource && !onlyOn(SURFACES)) report(path, where, `${property} reads a grain source`)
        if (aliases.has(property) && !onlyOn(SURFACES)) report(path, where, `${property} is a grain alias`)
        if (TEXTURE.bloom.test(value)) report(path, where, `${property} reads #kcc-bloom`)
        if (TEXTURE.waxFlat.test(value) && !onlyOn(new Set(['.kcc-tile'])))
          report(path, where, `${property} reads #kcc-wax-flat`)
        if (
          (TEXTURE.turbulence.test(value) ||
            TEXTURE.dataSvg.test(value) ||
            TEXTURE.png.test(value) ||
            TEXTURE.repeatingRadial.test(value)) &&
          !onlyOn(SURFACES)
        )
          report(path, where, `${property} paints a texture image`)
        if (TEXTURE.repeatingLinear.test(value) && !onlyOn(SURFACES) && !onlyOn(TAPE))
          report(path, where, `${property} paints a texture image`)
      }
    }
    if (WHOLE_NAME.test(markup)) report(path, 'markup', 'names --grain or --crayon')
    if (TEXTURE.bloom.test(markup)) report(path, 'markup', 'uses url(#kcc-bloom)')
    if (TEXTURE.waxFlat.test(markup)) report(path, 'markup', 'uses url(#kcc-wax-flat) outside .kcc-tile')
    if (TEXTURE.turbulence.test(markup) && path !== LAYOUT) report(path, 'markup', 'defines a filter outside Layout.cshtml')
    if (
      TEXTURE.dataSvg.test(markup) ||
      TEXTURE.png.test(markup) ||
      TEXTURE.repeatingLinear.test(markup) ||
      TEXTURE.repeatingRadial.test(markup)
    )
      report(path, 'markup', 'paints a texture image')
  }
  return reported
}

const featureSources = (): Source[] =>
  [...files(FEATURES, /\.(css|vue|cshtml)$/)].map((file) => ({
    path: relative(WEB, file).replaceAll('\\', '/'),
    text: readFileSync(file, 'utf8'),
  }))

describe('grain lies only on the desk, the washes and the tiles', () => {
  it('no source is declared or used anywhere else in Features', () => {
    const sources = featureSources()
    const paths = sources.map((source) => source.path)
    expect(paths).toContain(TOKENS)
    expect(paths).toContain(LAYOUT)
    const tokens = rulesOf(sources.find((source) => source.path === TOKENS)!.text)
    const root = tokens.filter((rule) => rule.selectors.includes(':root')).flatMap((rule) => rule.declarations)
    expect(root.map(([property]) => property)).toEqual(expect.arrayContaining(['--grain', '--crayon', '--desk-stripe']))
    expect(scan(sources)).toEqual([])
  })

  it('the desk paints both textures as #app backgrounds, blended per layer, with no overlay and no isolation', () => {
    const app = readFileSync(join(FEATURES, 'App.vue'), 'utf8')
    expect(app).not.toMatch(/kcc-grain|kcc-crayon/)

    const rules = featureSources().flatMap((source) => split(source).rules)
    const valuesOf = (selector: string, property: string) =>
      rules
        .filter((rule) => rule.selectors.includes(selector))
        .flatMap((rule) => rule.declarations.filter(([name]) => name === property).map(([, value]) => value))
    const images = valuesOf('#app', 'background-image')
    expect(images.at(-1)).toMatch(/var\(--desk-stripe\).*var\(--crayon\).*var\(--grain\)/s)
    expect(images.at(-1)!.split('var(--').length - 1).toBe(3)
    for (const selector of withDark('#app')) {
      const blends = valuesOf(selector, 'background-blend-mode')
      expect(blends.at(-1), `${selector} background-blend-mode`).toBeTruthy()
      expect(blends.at(-1)!.split(',')).toHaveLength(3)
    }
    expect(valuesOf('#app', 'background-blend-mode').at(-1)).toMatch(/multiply/)
    expect(
      valuesOf('#app', 'background-blend-mode').at(-1)!.split(',')[0].trim(),
      'a white stripe multiplied draws nothing',
    ).toBe('normal')
    expect(valuesOf(":root[data-theme='dark'] #app", 'background-blend-mode').at(-1)).toMatch(/soft-light/)
    expect(valuesOf('#app', 'isolation')).toEqual([])
    expect(rules.flatMap((rule) => rule.selectors).filter((selector) => /kcc-(?:grain|crayon)/.test(selector))).toEqual([])
    const opacities = rules
      .flatMap((rule) => rule.declarations.map(([name]) => name))
      .filter((name) => /^--(?:grain|wax)-op$/.test(name))
    expect(opacities).toEqual([])
  })
})

describe('the scanner', () => {
  const tape =
    '.kcc-tilewrap > .kcc-tape { background-image: repeating-linear-gradient(90deg, #fff 0 1px, transparent 1px 3px); }'
  const darkTape = `:root[data-theme='dark'] ${tape}`
  const gradient = 'background-image: repeating-linear-gradient(90deg, #fff 0 1px, transparent 1px 3px)'
  const css = (text: string, path = 'Features/Styles/Torn/Kit.css'): Source[] => [{ path, text }]

  it('exempts the tape’s woven stripe and its dark form by name', () => {
    expect(scan(css(`${tape}\n${darkTape}`))).toEqual([])
  })

  it.each([
    ['.kcc-field', '.kcc-field'],
    ['a new .kcc-tape--wide rule', '.kcc-tilewrap > .kcc-tape--wide'],
    ['.kcc-sheet', '.kcc-sheet'],
    ['a bare .kcc-tape rule', '.kcc-tape'],
  ])('fails the same gradient on %s', (_, selector) => {
    expect(scan(css(`${selector} { ${gradient}; }`))).not.toEqual([])
  })

  it('exempts the tape for the linear gradient only', () => {
    expect(
      scan(
        css('.kcc-tilewrap > .kcc-tape { background-image: repeating-radial-gradient(#fff 0 1px, transparent 1px 3px); }'),
      ),
    ).not.toEqual([])
  })

  it('does not count a whole-name --grain-op as a source', () => {
    const text = ':root { --grain-op: 0.08; } .kcc-field { opacity: var(--grain-op); }'
    expect(scan(css(text, TOKENS))).toEqual([])
  })

  it('counts --grain and --crayon read by anything but the desk, a wash or a tile', () => {
    expect(scan(css('.kcc-field { background-image: var(--grain); }'))).not.toEqual([])
    expect(scan(css('.kcc-sheet::after { background-image: var(--crayon); }'))).not.toEqual([])
    expect(scan(css('#app { background-image: var(--grain); }'))).toEqual([])
    expect(scan(css(":root[data-theme='dark'] #app { background-image: var(--crayon); }"))).toEqual([])
    expect(scan(css('.kcc-grain { background-image: var(--grain); }'))).not.toEqual([])
  })

  it('declares --grain and --crayon only in the Tokens.css ramps', () => {
    const declared = ":root { --grain: url('x.png'); }"
    expect(scan(css(declared, TOKENS))).toEqual([])
    expect(scan(css(declared))).not.toEqual([])
    expect(scan(css(`:root[data-theme='dark'] { --crayon: url('x.png'); }`, TOKENS))).toEqual([])
    expect(scan(css(`:root[data-theme='dark'] { --crayon: url('x.png'); }`))).not.toEqual([])
    expect(scan(css('.kcc-grain { --grain: url("x.png"); }', TOKENS))).not.toEqual([])
  })

  it('resolves var() aliases, down a chain', () => {
    const text =
      ':root { --tooth: var(--grain); --paper-tooth: var(--tooth); } .kcc-field { background-image: var(--paper-tooth); }'
    const reported = scan(css(text))
    expect(reported.some((line) => line.includes('.kcc-field'))).toBe(true)
    expect(reported.some((line) => line.includes(':root'))).toBe(true)
  })

  it('treats any texture image or feTurbulence as a source', () => {
    expect(scan(css(`.kcc-field { background-image: url("data:image/svg+xml;utf8,<svg/>"); }`))).not.toEqual([])
    expect(scan(css('.kcc-field { background-image: repeating-radial-gradient(#fff, #000 2px); }'))).not.toEqual([])
    expect(scan(css('<svg><filter><feTurbulence/></filter></svg>', 'Features/Components/Thing.vue'))).not.toEqual([])
    expect(scan(css('<svg><filter><feTurbulence/></filter></svg>', LAYOUT))).toEqual([])
  })

  it('reads declarations before a nested rule as the enclosing rule’s own', () => {
    const nested = '.kcc-field { background-image: var(--grain); &:hover { opacity: 1; } }'
    expect(scan(css(nested))).not.toEqual([])
    const onDesk = '#app { background-image: var(--grain); &:hover { opacity: 1; } }'
    expect(scan(css(onDesk))).toEqual([])
  })

  it('matches texture names and style attributes whatever their case or quotes', () => {
    expect(scan(css('.kcc-field { filter: URL(#KCC-bloom); }'))).not.toEqual([])
    expect(scan(css('.kcc-field { background-image: Repeating-Radial-Gradient(#fff, #000 2px); }'))).not.toEqual([])
    const single = "<template><div style='background-image: var(--grain)'></div></template>"
    expect(scan(css(single, 'Features/Components/Thing.vue'))).not.toEqual([])
  })

  it('reads inline style attributes', () => {
    const vue = '<template><div style="background-image: var(--grain)"></div></template>'
    expect(scan(css(vue, 'Features/Components/Thing.vue'))).not.toEqual([])
    const bound = `<template><div :style="{ '--grain': 'none' }"></div></template>`
    expect(scan(css(bound, 'Features/Components/Thing.vue'))).not.toEqual([])
  })

  it('allows #kcc-wax-flat on the tile only and #kcc-bloom nowhere, the washes being baked', () => {
    expect(scan(css('.kcc-tile { filter: url(#kcc-wax-flat); }'))).toEqual([])
    expect(scan(css('.kcc-wash { filter: url(#kcc-bloom) saturate(1); }'))).not.toEqual([])
    expect(scan(css('.kcc-tile { filter: url(#kcc-bloom); }'))).not.toEqual([])
    expect(scan(css('.kcc-wash { filter: url(#kcc-wax-flat); }'))).not.toEqual([])
    expect(scan(css('.kcc-sheet { filter: url(#kcc-bloom); }'))).not.toEqual([])
    expect(scan(css('<g filter="url(#kcc-wax-flat)"></g>', 'Features/Pages/Thing.cshtml'))).not.toEqual([])
  })

  it.each([
    'url("./Washes/wash-grain.png")',
    "url('/assets/wash-grain-CRUC7kRo.png')",
    'url("data:image/png;base64,iVBORw0KGgo=")',
  ])('counts a baked PNG as a texture, allowed on a wash: %s', (png) => {
    expect(scan(css(`.kcc-wash { --wash-grain: ${png}; mask-image: var(--wash-grain); }`))).toEqual([])
    expect(scan(css(`.kcc-field { mask-image: ${png}; }`))).not.toEqual([])
    expect(scan(css(`.kcc-wash { --wash-grain: ${png}; } .kcc-field { mask-image: var(--wash-grain); }`))).not.toEqual([])
  })
})
