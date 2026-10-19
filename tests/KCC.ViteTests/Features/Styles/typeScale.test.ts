import { readFileSync } from 'node:fs'
import { relative } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { files } from '../../support/files'

const WEB = fileURLToPath(new URL('../../../../src/KCC.Web/', import.meta.url))
const GENERATED = 'Features/Styles/Torn/Tears.css'
const THEME = 'Features/Styles/TailwindConfig.css'

const sources = [...files(`${WEB}Features`, /\.(css|vue|cshtml|ts)$/)]
  .map((file) => ({ path: relative(WEB, file).replaceAll('\\', '/'), source: readFileSync(file, 'utf8') }))
  .filter(({ path }) => path !== GENERATED)

const theme = sources.find(({ path }) => path === THEME)!.source
const sizesNamed = (prefix: string) =>
  [...theme.matchAll(new RegExp(`(?<![\\w-])--${prefix}-([\\w-]+):`, 'g'))]
    .map((match) => match[1]!)
    .filter((name) => !name.includes('--'))

const TEXT_SIZES = sizesNamed('text')
const ICON_SIZES = sizesNamed('icon')

const declared = (property: string) => new RegExp(`(?<![\\w-])${property}:\\s*([^;}\\]"'\`\\n]+)`, 'g')
const valueOf = (match: RegExpMatchArray) => match[1]!.replace(/!important/, '').trim()

const TEXT_SIZE_TOKEN = new RegExp(`^var\\(--text-(?:${TEXT_SIZES.join('|')})\\)$`)
const ICON_SIZE_TOKEN = new RegExp(`^var\\(--icon-(?:${ICON_SIZES.join('|')})\\)$`)
const isFontSizeToken = (value: string) =>
  TEXT_SIZE_TOKEN.test(value) || ICON_SIZE_TOKEN.test(value) || value === 'inherit' || value === '1em'
const NUMBER = String.raw`(?:\d+(?:\.\d+)?|\.\d+)`
const LINE_HEIGHT_TOKEN = new RegExp(
  String.raw`^(?:var\(--bl\)|var\(--text-[\w-]+--line-height\)|var\(--control-[\w-]+\)|var\(--pill-sm\)|1|` +
    String.raw`calc\(\s*(?:var\(--bl\)\s*\*\s*${NUMBER}|${NUMBER}\s*\*\s*var\(--bl\))\s*\))$`,
)
const LENGTH = /(?<![\w-])[\d.]*\d[a-z%]/i
const CSS_WIDE_KEYWORD = /^(?:inherit|initial|unset|revert|revert-layer)$/

const wordsOf = (value: string) => value.match(/(?:[^\s()/]+|\((?:[^()]|\([^()]*\))*\))+|\//g) ?? []

const shorthandOffScale = (value: string) => {
  const words = wordsOf(value)
  const slash = words.indexOf('/')
  if (slash > 0) return !TEXT_SIZE_TOKEN.test(words[slash - 1]!) || !LINE_HEIGHT_TOKEN.test(words[slash + 1] ?? '')
  return !CSS_WIDE_KEYWORD.test(value)
}

const classStrings = (line: string) => line.match(/"[^"]*"|'[^']*'|`[^`]*`|@apply[^;]*/g) ?? []
const STACKS_VERTICALLY = /(?<![\w-])(?:flex-col(?:-reverse)?|(?:inline-)?grid)(?![\w-])/
const OFF_RULE_GAP = /(?<![\w-])gap-(?:6|9|12|18|\[[^\]]+\])(?![\w.-])/g

type Rule = (line: string) => string[]

const rules: Record<string, Rule> = {
  'font-size reads a text or icon token': (line) =>
    [...line.matchAll(declared('font-size'))].map(valueOf).filter((value) => !isFontSizeToken(value)),

  'a font shorthand sizes with a text token': (line) =>
    [...line.matchAll(declared('font'))].map(valueOf).filter(shorthandOffScale),

  'line-height is the rule, a token or 1': (line) =>
    [...line.matchAll(declared('line-height'))]
      .map(valueOf)
      .filter(
        (value) =>
          !LINE_HEIGHT_TOKEN.test(value) && (LENGTH.test(value) || /^[\d.]+$/.test(value) || value.includes('calc(')),
      ),

  'no Tailwind text size outside the scale': (line) =>
    [
      ...line.matchAll(
        new RegExp(
          String.raw`(?<![\w-])(?:text-\[(?:length:)?(?:[\d.]+[a-z%]*|(?:clamp|min|max|calc)\([^\]]*\))\]|text-\[length:[^\]]+\]|text-\(length:[^)]+\)|text-(?:${TEXT_SIZES.join('|')})\/[\w.[\]()-]+|leading-[\w.[\]()/-]+|text-(?:xs|sm|base|lg|xl|[2-9]xl)(?![\w-]))`,
          'g',
        ),
      ),
    ].map((match) => match[0]),

  'vertical spacing is in rule units': (line) => [
    ...[
      ...line.matchAll(/(?<![\w-])-?(?:mt|mb|my|pt|pb|py|gap-y|space-y)-(?:3|6|9|12|18|\[-?[\d.]+[a-z%]+\])(?![\w.-])/g),
    ].map((match) => match[0]),
    ...classStrings(line)
      .filter((classes) => STACKS_VERTICALLY.test(classes))
      .flatMap((classes) => [...classes.matchAll(OFF_RULE_GAP)].map((match) => match[0])),
  ],
}

const proofs: Record<string, { caught: string[]; allowed: string[] }> = {
  'font-size reads a text or icon token': {
    caught: [
      'font-size: 11px;',
      'font-size: var(--text-body--line-height);',
      'font-size: var(--text-huge);',
      'font-size: var(--icon-xl);',
    ],
    allowed: [
      'font-size: var(--text-body);',
      'font-size: var(--text-readout);',
      'font-size: var(--icon-md);',
      'font-size: inherit;',
      'font-size: 1em;',
      'class="[font-size:var(--text-body)] mt-4"',
    ],
  },
  'a font shorthand sizes with a text token': {
    caught: [
      'font: 400 11px/var(--text-caps--line-height) var(--font-sono);',
      'font: 400 var(--text-body)/1.5 var(--font-sono);',
      'font: 400 var(--text-body)/24px var(--font-sono);',
      'font: 400 11px var(--font-sono);',
      'font: 400 var(--text-huge)/var(--bl) var(--font-sono);',
      'font: 400 var(--text-huge) var(--font-sono);',
      'font: 400 11pt var(--font-sono);',
      'font: 400 var(--text-body) var(--font-sono);',
      'font: 400 var(--text-body)/calc(var(--bl) - 3px) var(--font-sono);',
    ],
    allowed: [
      'font: inherit;',
      'font: 400 var(--text-body)/var(--bl) var(--font-sono);',
      'font: 400 var(--text-body) / var(--text-body--line-height) var(--font-sono);',
      'font: 600 var(--text-caps)/var(--control-md) var(--font-sono);',
      'font: 600 var(--text-caps)/var(--pill-sm) var(--font-sono);',
      'font: 400 var(--text-body)/calc(var(--bl) * 2) var(--font-sono);',
      'font: 400 var(--text-body)/calc(1.5 * var(--bl)) var(--font-sono);',
      '/* sets the `font:` shorthand */',
    ],
  },
  'line-height is the rule, a token or 1': {
    caught: [
      'line-height: 24px;',
      'line-height: 1.5;',
      'line-height: 1.5rem;',
      'line-height: 2em;',
      'line-height: 150%;',
      'line-height: 3vh;',
      'line-height: 2ch;',
      'line-height: 18pt;',
      'line-height: 2lh;',
      'line-height: 3dvh;',
      'line-height: 1.2ex;',
      'line-height: calc(var(--bl) - 3px);',
      'line-height: calc(var(--bl) + var(--x));',
    ],
    allowed: [
      'line-height: 1;',
      'line-height: var(--bl);',
      'line-height: var(--text-body--line-height);',
      'line-height: var(--control-md);',
      'line-height: var(--pill-sm);',
      'line-height: calc(var(--bl) * 2);',
      'line-height: calc(var(--bl) * 1.5);',
      'line-height: calc(2 * var(--bl));',
      '--text-body--line-height: 1.75rem;',
    ],
  },
  'no Tailwind text size outside the scale': {
    caught: [
      'class="text-[22px]"',
      'class="text-[1.5rem]"',
      'class="text-body/7"',
      'class="text-title/[24px]"',
      'class="text-(length:--x)"',
      'class="text-[length:var(--x)]"',
      'class="leading-6"',
      'class="text-sm"',
    ],
    allowed: ['class="text-body"', 'class="text-ink/70"', 'class="text-[var(--x)]"'],
  },
  'vertical spacing is in rule units': {
    caught: [
      'class="mt-6"',
      'class="mt-[24px]"',
      'class="mt-[1.5rem]"',
      'class="mt-[2em]"',
      'class="gap-y-[72px]"',
      'class="mt-[-24px]"',
      'class="flex flex-col gap-6"',
      'class="grid justify-items-center gap-6"',
      'class="grid gap-9"',
      'class="grid gap-[24px]"',
      ":class=\"['flex flex-col', 'gap-12']\"",
    ],
    allowed: [
      'class="mt-4"',
      'class="mt-1.5"',
      'class="space-y-24"',
      'class="mt-(--x)"',
      'class="flex gap-6"',
      'class="flex flex-wrap items-center gap-9"',
      'class="flex flex-col gap-x-6"',
      'class="grid gap-x-6 gap-y-rule-1"',
      'class="flex flex-col" data-gap="gap-6"',
    ],
  },
}

const hitsOf = (rule: Rule) =>
  sources.flatMap(({ path, source }) =>
    source.split('\n').flatMap((line, i) => rule(line).map((value) => `${path}:${i + 1} ${value}`)),
  )

describe('the type scale', () => {
  it.each(Object.entries(rules))('%s', (_, rule) => {
    expect(hitsOf(rule)).toEqual([])
  })

  it('declares at most eight text sizes besides the readout', () => {
    expect(TEXT_SIZES).toEqual(expect.arrayContaining(['body', 'readout']))
    expect(TEXT_SIZES.filter((name) => name !== 'readout').length).toBeLessThanOrEqual(8)
  })
})

describe('the type scale guard', () => {
  const cases = (pick: 'caught' | 'allowed') =>
    Object.entries(proofs).flatMap(([name, proof]) => proof[pick].map((line) => [name, line] as const))

  it.each(cases('caught'))('%s catches %s', (name, line) => {
    expect(rules[name]!(line)).not.toEqual([])
  })

  it.each(cases('allowed'))('%s allows %s', (name, line) => {
    expect(rules[name]!(line)).toEqual([])
  })
})
