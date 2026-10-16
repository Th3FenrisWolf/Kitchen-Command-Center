import { readdirSync, readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import postcss, { type Rule } from 'postcss'
import { describe, expect, it } from 'vitest'

const repoRoot = new URL('../../../../', import.meta.url)
const read = (path: string) => readFileSync(fileURLToPath(new URL(path, repoRoot)), 'utf8')

const parse = (path: string) => {
  const rules: Rule[] = []
  postcss.parse(read(path)).walkRules((rule) => {
    rules.push(rule)
  })
  return rules
}

const kit = parse('src/KCC.Web/Features/Styles/Torn/Kit.css')
const main = parse('src/KCC.Web/Features/Styles/Main.css')

const ruleFor = (rules: Rule[], selector: string) => {
  const index = rules.findIndex((rule) => rule.selectors.includes(selector))
  return { index, rule: rules[index] }
}

const declarations = (...rules: (Rule | undefined)[]) => {
  const found: Record<string, string> = {}
  for (const rule of rules) {
    rule?.walkDecls((decl) => {
      found[decl.prop] = decl.value.replace(/\s+/g, ' ')
    })
  }
  return found
}

const everyRuleFor = (rules: Rule[], selector: string) => rules.filter((rule) => rule.selectors.includes(selector))

const inComponentsLayer = (rule: Rule | undefined) => {
  for (let node = rule?.parent; node; node = node.parent) {
    if (node.type === 'atrule' && 'name' in node && node.name === 'layer' && node.params === 'components') return true
  }
  return false
}

const PILE = ['.kcc-slip', '.kcc-slip--fill', '.kcc-blank', '.kcc-blank > span', '.kcc-slip--flip', '.kcc-slip--wide']

describe('the pile in the kit', () => {
  it('gives every slip its sheet height and the blank defaults', () => {
    expect(declarations(ruleFor(kit, '.kcc-slip').rule)).toMatchObject({
      height: 'fit-content',
      '--blank-flip': '1',
      '--blank-turn': '1.5deg',
      '--blank-scale': '1.015',
    })
  })

  it('keeps a filling slip at full height, declared after the slip', () => {
    const fill = ruleFor(kit, '.kcc-slip--fill')
    expect(declarations(fill.rule).height).toBe('100%')
    expect(fill.index).toBeGreaterThan(ruleFor(kit, '.kcc-slip').index)
  })

  it('lays the blank under the sheet, turned and scaled about its centre', () => {
    expect(declarations(...everyRuleFor(kit, '.kcc-blank'))).toMatchObject({
      position: 'absolute',
      inset: '0',
      'pointer-events': 'none',
      'z-index': '-1',
      '--tear': 'var(--blank-tear)',
      transform:
        'rotate(calc(var(--blank-turn) * var(--blank-dir) * var(--blank-flip) * var(--blank-region, 1))) scale(var(--blank-scale))',
    })
  })

  it('gives the blank the fibre and the fall in the same rule as the torn wrapper', () => {
    const shared = kit.find((rule) => rule.selectors.includes('.kcc-torn') && rule.selectors.includes('.kcc-blank'))
    expect(declarations(shared).filter).toBe(
      'drop-shadow(0 0 1px var(--color-fiber)) drop-shadow(0 7px 9px var(--color-fall))',
    )
  })

  it('draws the blank as torn paper with no ruling', () => {
    const paper = declarations(ruleFor(kit, '.kcc-blank > span').rule)
    expect(paper).toMatchObject({
      display: 'block',
      height: '100%',
      'background-color': 'var(--color-paper)',
      'clip-path': 'var(--tear)',
    })
    expect(paper).not.toHaveProperty('background-image')
  })

  it('turns a flipped blank the other way and a wide blank less', () => {
    const slip = ruleFor(kit, '.kcc-slip').index
    const flip = ruleFor(kit, '.kcc-slip--flip')
    const wide = ruleFor(kit, '.kcc-slip--wide')

    expect(declarations(flip.rule)['--blank-flip']).toBe('-1')
    expect(declarations(wide.rule)).toMatchObject({ '--blank-turn': '0.5deg', '--blank-scale': '1.005' })
    expect(flip.index).toBeGreaterThan(slip)
    expect(wide.index).toBeGreaterThan(slip)
  })

  it('lifts every child but the blank, keeping the focus ring fade', () => {
    expect(declarations(ruleFor(kit, '.kcc-slip--lift > :not(.kcc-blank)').rule).transition).toBe(
      'translate var(--default-transition-duration) cubic-bezier(0.4, 0, 0.2, 1), outline-color 0.2s, outline-offset 0.2s',
    )
  })

  const mediaOf = (rule: Rule | undefined) => {
    const media = rule?.parent
    return media?.type === 'atrule' && 'name' in media && `${media.name} ${media.params}`
  }

  it('lifts 4px on focus, only when motion is welcome', () => {
    const lifted = everyRuleFor(kit, '.kcc-slip--lift:focus-within > :not(.kcc-blank)')
    expect(lifted).toHaveLength(1)
    expect(declarations(lifted[0])).toEqual({ translate: '0 -4px' })
    expect(mediaOf(lifted[0])).toBe('media (prefers-reduced-motion: no-preference)')
  })

  it('lifts 4px on hover, only when motion is welcome and the pointer can hover, so a tap does not lift', () => {
    const lifted = everyRuleFor(kit, '.kcc-slip--lift:hover > :not(.kcc-blank)')
    expect(lifted).toHaveLength(1)
    expect(declarations(lifted[0])).toEqual({ translate: '0 -4px' })
    expect(mediaOf(lifted[0])).toBe('media (prefers-reduced-motion: no-preference) and (hover: hover)')
  })

  it('sits in the components layer', () => {
    for (const selector of [
      ...PILE,
      '.kcc-slip--lift > :not(.kcc-blank)',
      '.kcc-slip--lift:focus-within > :not(.kcc-blank)',
      '.kcc-slip--lift:hover > :not(.kcc-blank)',
    ]) {
      const rules = everyRuleFor(kit, selector)
      expect(rules.length, selector).toBeGreaterThan(0)
      for (const rule of rules) expect(inComponentsLayer(rule), selector).toBe(true)
    }
  })
})

describe('the page', () => {
  it('clips sideways, so a blank past a tall sheet never scrolls the page', () => {
    expect(declarations(ruleFor(main, '#app').rule)['overflow-x']).toBe('clip')
  })
})

const BLANK = '<span class="kcc-blank" aria-hidden="true"><span></span></span>'

const razorViews = readdirSync(fileURLToPath(new URL('src/KCC.Web/Features/', repoRoot)), {
  recursive: true,
  encoding: 'utf8',
})
  .filter((path) => path.endsWith('.cshtml'))
  .map((path) => `src/KCC.Web/Features/${path.replaceAll('\\', '/')}`)

const slipContents = (source: string) => {
  const contents: string[] = []
  for (const open of source.matchAll(/<(\w+)\b[^>]*\bclass="kcc-slip\b[^"]*"[^>]*>/g)) {
    const start = open.index + open[0].length
    const tag = new RegExp(`<(/?)${open[1]}\\b[^>]*>`, 'g')
    tag.lastIndex = start
    let depth = 1
    for (let match = tag.exec(source); match; match = tag.exec(source)) {
      depth += match[1] ? -1 : 1
      if (depth === 0) {
        contents.push(source.slice(start, match.index))
        break
      }
    }
  }
  return contents
}

describe('the Razor sheets', () => {
  it('finds the hand-written slips', () => {
    expect(razorViews.filter((path) => slipContents(read(path)).length > 0).length).toBeGreaterThanOrEqual(4)
  })

  it.each(razorViews)('ends every slip in %s with one blank', (path) => {
    for (const content of slipContents(read(path))) {
      expect(content.trimEnd().endsWith(BLANK), path).toBe(true)
      expect(content.split(BLANK).length - 1, path).toBe(1)
    }
  })
})

const featureFiles = readdirSync(fileURLToPath(new URL('src/KCC.Web/Features/', repoRoot)), {
  recursive: true,
  encoding: 'utf8',
}).map((path) => `src/KCC.Web/Features/${path.replaceAll('\\', '/')}`)

const styleSources = featureFiles.flatMap((path) => {
  if (path.endsWith('.css')) return [{ path, css: read(path) }]
  if (!path.endsWith('.vue')) return []
  return [...read(path).matchAll(/<style\b[^>]*>([\s\S]*?)<\/style>/g)].map(([, css]) => ({ path, css: css! }))
})

const tapeSelectors = (css: string) => {
  const found: string[] = []
  postcss.parse(css).walkRules((rule) => {
    found.push(...rule.selectors.filter((selector) => /\.kcc-tape\b/.test(selector)))
  })
  return found
}

const TAPE_SELECTORS = ['.kcc-tilewrap > .kcc-tape', ":root[data-theme='dark'] .kcc-tilewrap > .kcc-tape"]

const markupFiles = featureFiles.filter((path) => path.endsWith('.vue') || path.endsWith('.cshtml'))

const VOID = new Set(['area', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta', 'source', 'track', 'wbr'])

const classTokens = (attributes: string) => attributes.match(/(?:^|\s)class="([^"]*)"/)?.[1]?.split(/\s+/) ?? []

const markupOf = (path: string) => {
  const source = read(path)
  if (!path.endsWith('.vue')) return source
  return source.slice(source.indexOf('<template'), source.lastIndexOf('</template>'))
}

const tapeParents = (markup: string) => {
  const open: { name: string; classes: string[] }[] = []
  const parents: string[][] = []
  for (const [, closing, name, attributes, selfClosing] of markup.matchAll(
    /<(\/?)([A-Za-z][\w.-]*)((?:[^>"']|"[^"]*"|'[^']*')*?)(\/?)>/g,
  )) {
    if (closing) {
      const at = open.findLastIndex((element) => element.name === name)
      if (at >= 0) open.length = at
      continue
    }
    const classes = classTokens(attributes!)
    if (classes.includes('kcc-tape')) parents.push(open.at(-1)?.classes ?? [])
    if (!selfClosing && !VOID.has(name!.toLowerCase())) open.push({ name: name!, classes })
  }
  return parents
}

describe('the tape', () => {
  it('reads the component style blocks as well as the stylesheets', () => {
    expect(styleSources.some(({ path }) => path.endsWith('.vue'))).toBe(true)
    expect(styleSources.some(({ path }) => path.endsWith('Torn/Kit.css'))).toBe(true)
  })

  it('is styled only over a tile, never over a sheet, in both ramps', () => {
    const selectors = styleSources.flatMap(({ path, css }) => tapeSelectors(css).map((selector) => ({ path, selector })))
    for (const { path, selector } of selectors) expect(TAPE_SELECTORS, `${path}: ${selector}`).toContain(selector)
    expect(selectors.map(({ selector }) => selector)).toEqual(expect.arrayContaining(TAPE_SELECTORS))
  })

  it('keeps a tile in the flow of a sheet unmoved and unturned', () => {
    const flow = ruleFor(kit, '.kcc-tilewrap--flow')
    expect(declarations(flow.rule)).toMatchObject({ position: 'relative', left: 'auto', top: 'auto', transform: 'none' })
    expect(flow.index).toBeGreaterThan(ruleFor(kit, '.kcc-tilewrap').index)
    expect(inComponentsLayer(flow.rule)).toBe(true)
  })

  it('finds the tile strips', () => {
    expect(markupFiles.flatMap((path) => tapeParents(markupOf(path))).length).toBeGreaterThanOrEqual(3)
  })

  it.each(markupFiles)('lays every strip in %s over a tilewrap', (path) => {
    const markup = markupOf(path)
    const parents = tapeParents(markup)
    expect(parents.length, path).toBe((markup.match(/\bkcc-tape\b/g) ?? []).length)
    for (const classes of parents) expect(classes, path).toContain('kcc-tilewrap')
  })
})
