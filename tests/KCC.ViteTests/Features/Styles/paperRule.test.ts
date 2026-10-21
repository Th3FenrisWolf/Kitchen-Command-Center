import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

const stylesDir = new URL('../../../../src/KCC.Web/Features/Styles/', import.meta.url)
const read = (path: string) => readFileSync(fileURLToPath(new URL(path, stylesDir)), 'utf8').replace(/\/\*[\s\S]*?\*\//g, '')

const theme = read('TailwindConfig.css')
const tokens = read('Torn/Tokens.css')
const typography = read('Typography.css')
const kit = read('Torn/Kit.css') + '\n' + read('Torn/Controls.css')

type Rule = { selectors: string[]; atRules: string[]; declarations: Record<string, string> }

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

const rulesOf = (css: string): Rule[] => {
  const rules: Rule[] = []
  const open: string[] = []
  let prelude = ''
  let quote = ''
  for (const char of css) {
    if (quote) {
      if (char === quote) quote = ''
      prelude += char
    } else if (char === '"' || char === "'") {
      quote = char
      prelude += char
    } else if (char === ';' && open.length === 0) {
      prelude = ''
    } else if (char === '{') {
      open.push(prelude.trim().replace(/\s+/g, ' '))
      prelude = ''
    } else if (char === '}') {
      const selector = open.pop()!
      const declarations = Object.fromEntries(
        splitTop(prelude, ';')
          .map((declaration) => declaration.split(/:(.*)/s).map((part) => part.trim().replace(/\s+/g, ' ')))
          .filter(([property, value]) => property && value && !property.includes(' ')),
      )
      if (Object.keys(declarations).length)
        rules.push({
          selectors: splitTop(selector, ',').map((part) => part.trim()),
          atRules: open.filter((at) => at.startsWith('@')),
          declarations,
        })
      prelude = ''
    } else {
      prelude += char
    }
  }
  return rules
}

const squash = (value: string | undefined) => value?.replace(/\s+/g, '')

const themeRules = rulesOf(theme)
const tokenRules = rulesOf(tokens)
const kitRules = rulesOf(kit + '\n' + typography)

const outsideQueries = (rule: Rule) => !rule.atRules.some((at) => /^@(media|container|supports)/.test(at))

const merged = (rules: Rule[]) => Object.assign({}, ...rules.map((rule) => rule.declarations)) as Record<string, string>

const exactly = (rules: Rule[], selector: string) =>
  merged(rules.filter(outsideQueries).filter((rule) => rule.selectors.includes(selector)))

const token = (rules: Rule[], selector: string, name: string) => exactly(rules, selector)[name]

const lightToken = (name: string) => token(themeRules, '@theme static', name)
const rootToken = (name: string) => token(tokenRules, ':root', name)
const darkToken = (name: string) => token(tokenRules, ":root[data-theme='dark']", name)

const compounds = (selector: string) => splitTop(selector.replace(/\s*([>+~])\s*/g, ' '), ' ').filter(Boolean)

const withoutPseudo = (compound: string) => {
  let out = ''
  let depth = 0
  let skipping = false
  for (let i = 0; i < compound.length; i++) {
    const char = compound[i]!
    if (depth === 0 && char === ':') skipping = true
    else if (depth === 0 && skipping && (char === '.' || char === '[')) skipping = false
    if (char === '(') depth++
    if (char === ')') {
      depth--
      continue
    }
    if (!skipping && depth === 0) out += char
  }
  return out
}

const keyOf = (compound: string) => {
  const bare = withoutPseudo(compound)
  return bare.match(/\.[\w-]+(?=[^.]*$)/)?.[0] ?? bare.match(/^[a-z][a-z0-9]*/)?.[0] ?? ''
}

const subjectsOf = (selector: string) => {
  const last = compounds(selector).at(-1) ?? ''
  const list = last.match(/^:(?:where|is)\((.*)\)$/)
  return list ? splitTop(list[1]!, ',').map((item) => item.trim()) : [last]
}

type Subject = { key: string; selector: string; pseudo: boolean; rule: Rule }

const subjects: Subject[] = kitRules.flatMap((rule) =>
  rule.selectors.flatMap((selector) =>
    subjectsOf(selector).map((subject) => ({
      key: keyOf(subject),
      selector,
      pseudo: withoutPseudo(subject) !== subject,
      rule,
    })),
  ),
)

const valuesFor = (key: string, property: string, filter: (subject: Subject) => boolean = () => true) =>
  subjects
    .filter((subject) => subject.key === key && filter(subject))
    .map((subject) => subject.rule.declarations[property])
    .filter((value): value is string => value !== undefined)

const plain = (subject: Subject) => !subject.pseudo && outsideQueries(subject.rule)

const mergedFor = (key: string) =>
  merged(subjects.filter((subject) => subject.key === key && plain(subject)).map((subject) => subject.rule))

const backgroundOf = (declarations: Record<string, string>) =>
  Object.entries(declarations)
    .filter(([property]) => property.startsWith('background'))
    .map(([, value]) => squash(value))
    .join(' ')

const lineHeightOf = (declarations: Record<string, string>) => {
  const order = Object.keys(declarations)
  const fromFont = declarations.font?.match(/\/\s*([^\s]+(?:\([^)]*\))?)/)?.[1]
  const explicit = declarations['line-height']
  if (explicit && fromFont) return order.indexOf('line-height') > order.indexOf('font') ? explicit : fromFont
  return explicit ?? fromFont
}

const paddingOf = (keys: string[]) => {
  const sides = { top: '0', right: '0', bottom: '0', left: '0' }
  const set = (names: (keyof typeof sides)[], values: string[]) =>
    names.forEach((name, i) => (sides[name] = values[Math.min(i, values.length - 1)]!.replace(/^0px$/, '0')))
  const rules = keys.flatMap((key) =>
    subjects.filter((subject) => subject.key === key && plain(subject)).map((subject) => subject.rule),
  )
  for (const rule of rules) {
    for (const [property, value] of Object.entries(rule.declarations)) {
      const values = splitTop(value, ' ').filter(Boolean)
      if (property === 'padding') {
        const [top, right = top, bottom = top, left = right] = values
        set(['top', 'right', 'bottom', 'left'], [top!, right!, bottom!, left!])
      } else if (property === 'padding-inline') set(['left', 'right'], values)
      else if (property === 'padding-block') set(['top', 'bottom'], values)
      else if (property === 'padding-inline-start' || property === 'padding-left') set(['left'], values)
      else if (property === 'padding-inline-end' || property === 'padding-right') set(['right'], values)
      else if (property === 'padding-block-start' || property === 'padding-top') set(['top'], values)
      else if (property === 'padding-block-end' || property === 'padding-bottom') set(['bottom'], values)
    }
  }
  return sides
}

const shimRoles = [
  'h1',
  'h2',
  'h3',
  'h4',
  'h5',
  'h6',
  '.kcc-display',
  '.kcc-section',
  '.kcc-heading',
  '.kcc-title',
  '.kcc-hand',
  '.kcc-kick',
  '.kcc-lbl',
  '.kcc-meta',
  '.kcc-unit',
  '.kcc-num',
  '.text-caps',
  '.text-title',
  '.text-heading',
  '.text-section',
  '.text-readout',
]

const shimRule = () =>
  kitRules.find(
    (rule) =>
      rule.declarations.translate !== undefined &&
      rule.selectors.some((selector) => /^\.kcc-sheet :where\(/.test(selector) && selector.includes('.kcc-display')),
  )

describe('R1: pinned metrics and the baseline constants', () => {
  const faces = rulesOf(typography).filter((rule) => rule.selectors.includes('@font-face'))

  it.each([
    ['Hazelnut', 4, '101.0742%', '24.6582%'],
    ['APCasual', 2, '75%', '25%'],
    ['Sono', 2, '91.2%', '28.8%'],
  ])('every %s face (%i) pins ascent %s, descent %s and no line gap', (family, count, ascent, descent) => {
    const ofFamily = faces.filter((face) => face.declarations['font-family'] === `'${family}'`)
    expect(ofFamily).toHaveLength(count)
    for (const face of ofFamily) {
      expect(face.declarations['ascent-override']).toBe(ascent)
      expect(face.declarations['descent-override']).toBe(descent)
      expect(face.declarations['line-gap-override']).toBe('0%')
    }
  })

  it.each([
    ['--k-hazelnut', '0.38208'],
    ['--k-casual', '0.25'],
    ['--k-sono', '0.312'],
  ])('%s is %s', (name, value) => {
    expect(rootToken(name)).toBe(value)
  })

  it('drops the rule to the body baseline through --k-hazelnut', () => {
    expect(squash(rootToken('--rule-drop'))).toBe('calc(var(--bl)/2+var(--k-hazelnut)*var(--text-body))')
  })
})

describe('R2: the ruling', () => {
  const sheet = () => exactly(kitRules, '.kcc-sheet')

  it('draws one 1px rule per --bl from the content box', () => {
    expect(squash(sheet()['background-image'])).toMatch(
      /^linear-gradient\((tobottom,)?var\(--color-rule\)1px,transparent(0|1px)\)$/,
    )
    expect(squash(sheet()['background-size'])).toBe('100%var(--bl)')
    expect(sheet()['background-origin']).toBe('content-box')
    expect([undefined, 'border-box']).toContain(sheet()['background-clip'])
  })

  it('places the ruling at --rule-drop plus --rule-offset, with no --pad term', () => {
    expect(squash(sheet()['background-position'])).toBe('0calc(var(--rule-drop)+var(--rule-offset))')
    expect(sheet()['background-position']).not.toContain('--pad')
  })

  it('sets --rule-offset to 2px', () => {
    expect(rootToken('--rule-offset')).toBe('2px')
  })

  it('kcc-sheet--blank draws no ruling', () => {
    expect(mergedFor('.kcc-sheet--blank')['background-image']).toBe('none')
  })
})

describe('R3: the shim', () => {
  it('shifts every text role on a sheet by a signed translate to the nearest rule', () => {
    const rule = shimRule()
    expect(rule, 'a .kcc-sheet :where(…) rule with a translate').toBeDefined()
    expect(squash(rule!.declarations.translate)).toBe(
      '0calc(mod(calc(var(--rule-drop)-var(--own)+var(--bl)/2),var(--bl))-var(--bl)/2)',
    )
    expect(squash(rule!.declarations['--own'])).toBe('calc(var(--lh)/2+var(--k)*1em)')
  })

  it('lists exactly the roles of research R3, and not body text', () => {
    const selector = shimRule()?.selectors.find((candidate) => candidate.startsWith('.kcc-sheet :where('))
    expect(selector, 'the shim selector').toBeDefined()
    expect(subjectsOf(selector!).sort()).toEqual([...shimRoles].sort())
  })

  it('wraps kcc-meta with no row gap, so its next line starts on the next rule', () => {
    const meta = mergedFor('.kcc-meta')
    const [rowGap] = meta.gap ? splitTop(meta.gap, ' ') : []
    expect(meta['row-gap'] ?? rowGap).toBe('0')
  })

  it.each(['.kcc-btn', '.kcc-seg', '.kcc-badge', '.kcc-control'])(
    'a role inside %s rides on the outer shift and takes none of its own',
    (outer) => {
      const cancels = kitRules
        .filter((rule) => rule.declarations.translate === 'none')
        .flatMap((rule) => rule.selectors)
        .filter((selector) => subjectsOf(selector).includes('.kcc-num'))
      const outers = cancels.flatMap((selector) => {
        const list = compounds(selector)
          .at(-2)
          ?.match(/^:(?:where|is)\((.*)\)$/)
        return list ? splitTop(list[1]!, ',').map((item) => item.trim()) : []
      })
      expect(outers).toContain(outer)
    },
  )

  it('makes Hazelnut the sheet’s inherited --k', () => {
    expect(exactly(kitRules, '.kcc-sheet')['--k']).toBe('var(--k-hazelnut)')
  })

  it.each([
    'h1',
    'h2',
    'h3',
    'h4',
    'h5',
    'h6',
    '.kcc-display',
    '.kcc-section',
    '.kcc-heading',
    '.kcc-title',
    '.kcc-hand',
    '.font-casual',
  ])('%s sets --k to --k-casual', (key) => {
    expect(valuesFor(key, '--k')).toContain('var(--k-casual)')
  })

  it.each(['.kcc-kick', '.kcc-lbl', '.kcc-meta', '.kcc-unit', '.kcc-num', '.font-mono'])(
    '%s sets --k to --k-sono',
    (key) => {
      expect(valuesFor(key, '--k')).toContain('var(--k-sono)')
    },
  )

  it.each([
    ['h1', /^var\(--text-display(-phone)?--line-height\)$/],
    ['.kcc-display', /^var\(--text-display(-phone)?--line-height\)$/],
    ['h2', /^var\(--text-section--line-height\)$/],
    ['.kcc-section', /^var\(--text-section--line-height\)$/],
    ['.text-section', /^var\(--text-section--line-height\)$/],
    ['h3', /^var\(--text-heading--line-height\)$/],
    ['.kcc-heading', /^var\(--text-heading--line-height\)$/],
    ['.text-heading', /^var\(--text-heading--line-height\)$/],
    ['h4', /^var\(--text-title--line-height\)$/],
    ['h5', /^var\(--text-title--line-height\)$/],
    ['h6', /^var\(--text-title--line-height\)$/],
    ['.kcc-title', /^var\(--text-title--line-height\)$/],
    ['.kcc-hand', /^var\(--text-title--line-height\)$/],
    ['.text-title', /^var\(--text-title--line-height\)$/],
    ['.text-caps', /^var\(--text-caps--line-height\)$/],
    ['.text-readout', /^var\(--text-readout--line-height\)$/],
    ['.kcc-kick', /^var\(--(bl|text-caps--line-height)\)$/],
    ['.kcc-lbl', /^var\(--(bl|text-caps--line-height)\)$/],
    ['.kcc-meta', /^var\(--(bl|text-caps--line-height)\)$/],
    ['.kcc-unit', /^var\(--[\w-]+\)$/],
    ['.kcc-num', /^var\(--[\w-]+\)$/],
  ])('%s gives the shim its line height as --lh', (key, value) => {
    const values = valuesFor(key, '--lh')
    expect(
      values.some((lh) => value.test(lh)),
      `${key} sets --lh to its role's line height`,
    ).toBe(true)
  })
})

describe('R4: the workbench colours in both ramps', () => {
  it('light: feint blue rule and margin red in @theme static', () => {
    expect(lightToken('--color-rule')).toBe('oklch(0.698 0.048 239.5 / 0.55)')
    expect(lightToken('--color-margin')).toBe('oklch(0.631 0.178 20 / 0.75)')
  })

  it('dark: feint blue rule and margin red under data-theme="dark"', () => {
    expect(darkToken('--color-rule')).toBe('oklch(0.74 0.05 240 / 0.24)')
    expect(darkToken('--color-margin')).toBe('oklch(0.66 0.15 22 / 0.58)')
  })
})

describe('R5: the margin rule', () => {
  const marginBefore = (inQuery: boolean) =>
    kitRules.filter(
      (rule) =>
        outsideQueries(rule) !== inQuery && rule.selectors.some((selector) => /\.kcc-sheet--margin::before$/.test(selector)),
    )
  const marginSheet = (inQuery: boolean) =>
    kitRules.filter(
      (rule) => outsideQueries(rule) !== inQuery && rule.selectors.some((selector) => /\.kcc-sheet--margin$/.test(selector)),
    )
  const narrow = (rule: Rule) =>
    rule.atRules.some((at) => /^@container\b.*(\(\s*width\s*<\s*480px\s*\)|max-width:\s*479(\.\d+)?px)/.test(at))

  it('draws the rule in ::before, two rules from the left edge, in --color-margin', () => {
    const rules = [...marginBefore(false), ...marginSheet(false)]
    expect(marginBefore(false).length, 'a .kcc-sheet--margin::before rule').toBeGreaterThan(0)
    expect(merged(marginBefore(false)).content).toBeDefined()
    const all = rules.flatMap((rule) => Object.values(rule.declarations).map(squash)).join(' ')
    expect(all).toContain('var(--color-margin)')
    expect(all).toContain('calc(var(--bl)*2)')
  })

  it('starts the copy one rule after the margin', () => {
    expect(squash(merged(marginSheet(false))['padding-inline-start'])).toBe('calc(var(--bl)*3)')
  })

  it('makes only a margin sheet’s slip an inline-size container', () => {
    const slip = kitRules.filter(
      (rule) =>
        outsideQueries(rule) &&
        rule.selectors.some((selector) => /^\.kcc-slip:has\(.*\.kcc-sheet--margin\)$/.test(selector)),
    )
    expect(merged(slip)['container-type']).toBe('inline-size')
    expect(exactly(kitRules, '.kcc-sheet')['container-type']).toBeUndefined()
    expect(exactly(kitRules, '.kcc-slip')['container-type']).toBeUndefined()
  })

  it('drops the line and the extra padding below 480px of sheet width', () => {
    const before = merged(marginBefore(true).filter(narrow))
    const removes = [
      before.display === 'none',
      before.content === 'none',
      before.opacity === '0',
      before.background === 'none',
      before['background-image'] === 'none',
      before.visibility === 'hidden',
    ]
    expect(removes, 'the narrow query hides ::before').toContain(true)
    const padding = merged(marginSheet(true).filter(narrow))['padding-inline-start']
    expect(padding, 'the narrow query resets padding-inline-start').toBeDefined()
    expect(squash(padding)).not.toBe('calc(var(--bl)*3)')
  })
})

describe('R6: controls on the ruling', () => {
  it('kcc-row is a wrapping flex row, top-aligned, 12px apart, half a rule between and below lines', () => {
    const row = mergedFor('.kcc-row')
    expect(row.display).toBe('flex')
    expect(row['flex-wrap']).toBe('wrap')
    expect(['flex-start', 'start']).toContain(row['align-items'])
    const [rowGap, columnGap] = row.gap ? splitTop(row.gap, ' ') : []
    expect(squash(row['row-gap'] ?? rowGap)).toBe('calc(var(--bl)/2)')
    expect(row['column-gap'] ?? columnGap).toBe('12px')
    expect(squash(row['padding-bottom'])).toBe('calc(var(--bl)/2)')
  })

  const controlShift =
    'calc(mod(calc(var(--rule-drop)-var(--k-sono)*var(--centre-size)-var(--h)/2+var(--bl)/2),var(--bl))-var(--bl)/2)'

  it.each(['.kcc-btn', '.kcc-seg', '.kcc-control'])(
    '%s centres on its own caps unless it is told the size of the line it sits beside',
    (key) => {
      expect(valuesFor(key, '--centre-size')).toEqual(['var(--text-caps)'])
    },
  )
  const inSheet = (subject: Subject) =>
    compounds(subject.selector)
      .slice(0, -1)
      .some((part) => part.includes('.kcc-sheet'))

  it('kcc-control is a control tall unless it is told its height', () => {
    expect(valuesFor('.kcc-control', '--h')).toEqual(['var(--control-md)'])
  })

  it.each(['.kcc-btn', '.kcc-seg', '.kcc-control'])('%s inside a sheet centres on the caps of its line', (key) => {
    const shifts = [
      ...valuesFor(key, 'translate', inSheet).map((value) => squash(value)),
      ...valuesFor(key, 'top', inSheet).map((value) => `0${squash(value)}`),
    ]
    expect(shifts).toContain(`0${controlShift}`)
  })

  it('shifts the header pad’s controls too, since its bar starts on a line', () => {
    const shifts = subjects.filter((subject) => ['.kcc-btn', '.kcc-seg'].includes(subject.key) && inSheet(subject))
    expect(shifts.length).toBeGreaterThan(0)
    for (const subject of shifts) expect(subject.selector).not.toContain('pad-bar')
  })

  it.each([
    ['.kcc-btn', 'var(--control-md)'],
    ['.kcc-seg', 'var(--control-md)'],
    ['.kcc-btn--lg', 'var(--control-lg)'],
    ['.kcc-btn--icon', 'var(--control-sm)'],
  ])('%s gives the shift its height as --h: %s', (key, height) => {
    expect(valuesFor(key, '--h')).toContain(height)
  })

  it.each(['.kcc-btn--ghost', '.kcc-seg', '.kcc-field', '.kcc-badge'])(
    '%s on a sheet fills with paper so no rule runs through it',
    (key) => {
      const fills = subjects
        .filter((subject) => subject.key === key && plain(subject) && inSheet(subject))
        .flatMap((subject) => [subject.rule.declarations['background-color'], subject.rule.declarations.background])
        .filter((value): value is string => value !== undefined)
      expect(fills.some((fill) => fill.includes('var(--color-paper)') && !fill.includes('color-mix'))).toBe(true)
    },
  )

  it('a kcc-control on a sheet fills with paper, except in its marker pill state', () => {
    const fills = subjects.filter(
      (subject) =>
        subject.key === '.kcc-control' &&
        inSheet(subject) &&
        outsideQueries(subject.rule) &&
        subject.rule.declarations['background-color'] === 'var(--color-paper)',
    )
    expect(fills.length, 'a .kcc-sheet :where(.kcc-control…) paper fill').toBeGreaterThan(0)
    for (const fill of fills) {
      const subject = subjectsOf(fill.selector).find((item) => keyOf(item) === '.kcc-control')!
      expect(subject).toBe('.kcc-control:not(.kcc-pill)')
    }
  })

  it('a ghost button on a sheet keeps its paper fill on hover', () => {
    const hovers = subjects
      .filter(
        (subject) =>
          subject.key === '.kcc-btn--ghost' &&
          subject.pseudo &&
          inSheet(subject) &&
          subjectsOf(subject.selector).some((s) => s.includes(':hover')),
      )
      .flatMap((subject) => [subject.rule.declarations['background-color'], subject.rule.declarations.background])
      .filter((value): value is string => value !== undefined)
    expect(hovers.length, 'a .kcc-sheet .kcc-btn--ghost:hover rule with a fill').toBeGreaterThan(0)
    for (const fill of hovers) {
      expect(['transparent', 'none']).not.toContain(fill)
      expect(fill).toContain('var(--color-paper)')
    }
  })
})

describe('R6: what controls share a line with', () => {
  const indexOf = (match: (rule: Rule) => boolean) => kitRules.findIndex(match)

  it('a ghost button’s sheet fill comes after its transparent base, which it ties on specificity', () => {
    const base = indexOf(
      (rule) => rule.selectors.includes('.kcc-btn--ghost') && rule.declarations.background === 'transparent',
    )
    const fill = indexOf(
      (rule) =>
        rule.selectors.some(
          (selector) => selector.startsWith('.kcc-sheet :where(') && selector.includes('.kcc-btn--ghost'),
        ) && rule.declarations['background-color'] === 'var(--color-paper)',
    )
    expect(base).toBeGreaterThan(-1)
    expect(fill).toBeGreaterThan(base)
  })

  it('a text button in a kcc-row takes a control’s height from its own line box, at any size', () => {
    expect(squash(exactly(kitRules, '.kcc-row > .kcc-btn--text')['margin-bottom'])).toBe('calc(var(--control-md)-var(--h))')
    expect(
      squash(exactly(kitRules, '.kcc-row > form > .kcc-btn--text')['margin-bottom']),
      'also inside a form that is laid out as its contents, such as the sign-out form',
    ).toBe('calc(var(--control-md)-var(--h))')
    expect(valuesFor('.kcc-btn--text', '--h')).toContain('var(--bl)')
  })

  it.each(['.kcc-kick', '.kcc-lbl', '.kcc-meta', '.kcc-title', '.kcc-heading'])(
    'a %s of any number of lines in a kcc-row ends half a rule down, so its row line fills whole rules as a control’s does',
    (key) => {
      const margins = kitRules
        .filter((rule) =>
          rule.selectors.some((selector) => /^\.kcc-row > :is\(/.test(selector) && subjectsOf(selector).includes(key)),
        )
        .map((rule) => squash(rule.declarations['margin-bottom']))
      expect(margins, 'an :is() rule, so it outweighs a role’s own margin: 0').toContain('calc(var(--bl)/2)')
    },
  )

  it('a kcc-control in a kcc-row takes a control’s height whatever its own', () => {
    expect(squash(exactly(kitRules, '.kcc-row > .kcc-control')['margin-bottom'])).toBe('calc(var(--control-md)-var(--h))')
    expect(squash(exactly(kitRules, '.kcc-row > form > .kcc-control')['margin-bottom'])).toBe(
      'calc(var(--control-md)-var(--h))',
    )
  })

  it('a badge is one rule tall, a 24px pill with no block margins, so a line of badges fills whole rules', () => {
    const badge = mergedFor('.kcc-badge')
    expect(badge.height).toBe('var(--pill-sm)')
    expect(badge['margin-block']).toBeUndefined()
    expect(badge.margin).toBeUndefined()
    expect(lightToken('--pill-sm')).toBe(rootToken('--bl'))
  })

  it('a checklist row that runs on keeps its box and its amount on its first line', () => {
    expect(exactly(kitRules, '.kcc-check li')['align-items']).toBe('start')
    expect(squash(exactly(kitRules, '.kcc-check .kcc-box')['margin-top'])).toBe('calc((var(--bl)-12px)/2)')
  })

  it('a badge on a sheet takes the shim on its Sono caps and its pill line, so its text sits on the rule', () => {
    const shifted = shimRule()!.selectors.some((selector) => subjectsOf(selector).includes('.kcc-badge'))
    expect(shifted, 'the shim rule lists .kcc-badge').toBe(true)
    const inSheet = (subject: Subject) => compounds(subject.selector).some((part) => part.includes('.kcc-sheet'))
    expect(valuesFor('.kcc-badge', '--k', inSheet)).toContain('var(--k-sono)')
    expect(valuesFor('.kcc-badge', '--lh', inSheet)).toContain('var(--pill-sm)')
  })
})

describe('R7: a field is a line to write on', () => {
  const field = () => mergedFor('.kcc-field')

  it('drops the pill: no radius, no ring, children aligned to the start, the control height kept', () => {
    expect([undefined, '0']).toContain(field()['border-radius'])
    expect([undefined, 'none']).toContain(field()['box-shadow'])
    expect(['start', 'flex-start']).toContain(field()['align-items'])
    expect(field().height).toBe('var(--control-md)')
  })

  it('draws a 1px hair-strong underline at --rule-drop plus --rule-offset', () => {
    const background = backgroundOf(field())
    expect(background).toContain('var(--color-hair-strong)')
    expect(background).toContain('calc(var(--rule-drop)+var(--rule-offset))')
    expect(background).toContain('1px')
  })

  it('draws the underline 2px in ink on focus, with no ring', () => {
    const focus = merged(
      kitRules.filter((rule) => rule.selectors.some((selector) => /^\.kcc-field:focus-within$/.test(selector))),
    )
    const background = backgroundOf(focus)
    expect(background).toContain('var(--color-ink)')
    expect(background).toContain('2px')
    expect([undefined, 'none']).toContain(focus['box-shadow'])
  })

  it.each(['input', 'select'])('a field’s %s sets its text on the rule', (element) => {
    const rules = kitRules.filter((rule) => rule.selectors.includes(`.kcc-field ${element}`))
    expect(lineHeightOf(merged(rules))).toBe('var(--bl)')
  })

  it.each(['input', 'select'])(
    'a field’s %s fills the field’s 36px hit area, its text kept on the first line by the padding below it',
    (element) => {
      const control = merged(kitRules.filter((rule) => rule.selectors.includes(`.kcc-field ${element}`)))
      expect(control.height).toBe('var(--control-md)')
      expect(squash(control['padding-bottom'])).toBe('calc(var(--control-md)-var(--bl))')
      expect(control.padding).toBeUndefined()
    },
  )

  it('writes the X before the line in ::before: APCasual title size, margin red, turned -6deg', () => {
    const x = merged(kitRules.filter((rule) => rule.selectors.some((selector) => /^\.kcc-field::before$/.test(selector))))
    expect(x.content, 'an X with empty alternative text, so it is never read out').toMatch(/^(['"])X\1 \/ (['"])\2$/)
    expect(x.color).toBe('var(--color-margin)')
    expect(x['font-family'] ?? x.font).toContain('var(--font-casual)')
    expect(x['font-size'] ?? x.font).toContain('var(--text-title)')
    expect(squash(x.rotate) ?? squash(x.transform)?.match(/rotate\(([^)]+)\)/)?.[1]).toBe('-6deg')
  })

  it('kcc-field--stepper is a control, not a line to write on: it draws no X and gives its number the whole width', () => {
    const before = merged(
      kitRules.filter((rule) => rule.selectors.some((selector) => /^\.kcc-field--stepper::before$/.test(selector))),
    )
    expect(before.content).toBe('none')
    expect(mergedFor('.kcc-field--stepper')['grid-template-columns']).toBe('1fr')
  })

  it('kcc-field--area has no box, and its textarea writes on the sheet’s rules', () => {
    const area = mergedFor('.kcc-field--area')
    expect([undefined, '0']).toContain(area['border-radius'])
    expect(paddingOf(['.kcc-field', '.kcc-field--area'])).toEqual({ top: '0', right: '0', bottom: '0', left: '0' })
    const textarea = merged(kitRules.filter((rule) => rule.selectors.includes('.kcc-field--area textarea')))
    expect(lineHeightOf(textarea)).toBe('var(--bl)')
    expect(['none', 'transparent']).toContain(textarea.background ?? textarea['background-color'])
  })

  it('kcc-field--area focuses with a 2px ink outline and no radius', () => {
    const focus = merged(
      kitRules.filter((rule) => rule.selectors.some((selector) => /\.kcc-field--area.*:focus/.test(selector))),
    )
    expect(squash(focus.outline)).toMatch(/2px.*var\(--color-ink\)/)
    expect([undefined, '0']).toContain(focus['border-radius'])
  })

  it('kcc-field--error draws the underline 2px in ink: red reads under 3:1 on paper, so the message below carries it', () => {
    const background = backgroundOf(mergedFor('.kcc-field--error'))
    expect(background).toContain('var(--color-ink)')
    expect(background).toContain('2px')
    expect(background).not.toContain('var(--color-red)')
    expect(merged(kitRules.filter((rule) => rule.selectors.includes('.kcc-field--error.kcc-field--area'))).outline).toBe(
      '2px solid var(--color-ink)',
    )
  })
})

describe('R8: icon buttons are 32px', () => {
  it('declares --control-sm as 2rem', () => {
    expect(lightToken('--control-sm')).toBe('2rem')
  })

  it('kcc-btn--icon is --control-sm square', () => {
    const icon = mergedFor('.kcc-btn--icon')
    expect(icon.width).toBe('var(--control-sm)')
    expect(icon.height).toBe('var(--control-sm)')
  })

  it('has no large icon button rule', () => {
    const large = kitRules.filter((rule) =>
      rule.selectors.some((selector) => /\.kcc-btn--lg\.kcc-btn--icon|\.kcc-btn--icon\.kcc-btn--lg/.test(selector)),
    )
    expect(large).toEqual([])
  })
})

describe('R9: the workbench grain', () => {
  it('lays a stitched four-octave noise tile over each sheet’s ground', () => {
    const grain = merged(kitRules.filter((rule) => rule.selectors.some((selector) => /^\.kcc-sheet::after$/.test(selector))))
    expect(grain.content).toBeDefined()
    expect(grain.position).toBe('absolute')
    expect(grain.inset).toBe('0')
    expect(grain['z-index']).toBe('0')
    expect(grain['pointer-events']).toBe('none')
    expect(grain.opacity).toBe('var(--sheet-grain-op)')
    expect(grain['mix-blend-mode']).toBe('var(--sheet-grain-blend)')
    const image = grain['background-image'] ?? grain.background ?? ''
    const tile = decodeURIComponent(image.match(/^var\((--[\w-]+)\)$/) ? (rootToken(image.slice(4, -1)) ?? '') : image)
    expect(tile).toMatch(/feTurbulence/)
    expect(tile).toMatch(/baseFrequency=['"]0?\.8['"]/)
    expect(tile).toMatch(/numOctaves=['"]4['"]/)
    expect(tile).toMatch(/stitchTiles=['"]stitch['"]/)
  })

  it('a sheet that scrolls itself carries the grain in its own scrolling background, under the rules, so it covers every line', () => {
    const scroll = mergedFor('.kcc-sheet--scroll')
    const layers = splitTop(scroll['background-image'] ?? '', ',').map((layer) => squash(layer))
    expect(layers).toHaveLength(3)
    expect(layers[0]).toBe('linear-gradient(tobottom,var(--color-rule)1px,transparent0)')
    expect(layers[1]).toContain('var(--sheet-grain-op)')
    expect(layers[2]).toBe('var(--sheet-grain)')
    expect(scroll['background-attachment']).toBe('local')
    expect(squash(scroll['background-blend-mode'])).toBe('normal,normal,var(--sheet-grain-blend)')
    expect(squash(scroll['background-position'])).toBe('0calc(var(--rule-drop)+var(--rule-offset)),00,00')
    expect(squash(scroll['background-origin'])).toBe('content-box,padding-box,padding-box')
    const after = merged(kitRules.filter((rule) => rule.selectors.includes('.kcc-sheet--scroll::after')))
    expect(after.content).toBe('none')
  })

  it('a sheet that scrolls itself turns off the grain overlay every sheet draws, later in the cascade', () => {
    const lastIndexOf = (selector: string) =>
      Math.max(
        ...kitRules.map((rule, index) =>
          outsideQueries(rule) && rule.declarations.content && rule.selectors.includes(selector) ? index : -1,
        ),
      )
    expect(lastIndexOf('.kcc-sheet--scroll::after')).toBeGreaterThan(lastIndexOf('.kcc-sheet::after'))
  })

  it('light: 0.06, multiplied', () => {
    expect(rootToken('--sheet-grain-op')).toBe('0.06')
    expect(rootToken('--sheet-grain-blend')).toBe('multiply')
  })

  it('dark: 0.08, soft-lit', () => {
    expect(darkToken('--sheet-grain-op')).toBe('0.08')
    expect(darkToken('--sheet-grain-blend')).toBe('soft-light')
  })
})
