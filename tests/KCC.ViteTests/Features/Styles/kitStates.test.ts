import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import postcss from 'postcss'
import { describe, expect, it } from 'vitest'

const repoRoot = new URL('../../../../', import.meta.url)
const read = (path: string) => readFileSync(fileURLToPath(new URL(path, repoRoot)), 'utf8')

const KIT_FILES = ['Kit.css', 'Controls.css'] as const
const kit = postcss.parse(KIT_FILES.map((name) => read(`src/KCC.Web/Features/Styles/Torn/${name}`)).join('\n'))

const declarationsFor = (match: (selector: string) => boolean) => {
  const found: Record<string, string>[] = []
  kit.walkRules((rule) => {
    if (!rule.selectors.some(match)) return
    const declarations: Record<string, string> = {}
    rule.walkDecls((decl) => {
      declarations[decl.prop] = decl.value
    })
    found.push(declarations)
  })
  return found
}

const RANGE_THUMBS = ['::-webkit-slider-thumb', '::-moz-range-thumb'] as const

const inForcedColors = (rule: postcss.Rule) =>
  rule.parent?.type === 'atrule' && (rule.parent as postcss.AtRule).params === '(forced-colors: active)'

// An engine drops a whole selector list that names another engine's pseudo-element, so each thumb has rules of its own.
const thumbRules = (selector: string) => {
  const found: postcss.Rule[] = []
  kit.walkRules((rule) => {
    if (rule.selectors.includes(selector)) found.push(rule)
  })
  return found
}

const declarationsOf = (rule: postcss.Rule) => {
  const declarations: Record<string, string> = {}
  rule.walkDecls((decl) => {
    declarations[decl.prop] = decl.value.replace(/\s+/g, ' ')
  })
  return declarations
}

describe('the kit states', () => {
  it('gives a disabled checklist row the same look as a disabled button', () => {
    const [button] = declarationsFor((selector) => selector === '.kcc-btn:disabled')
    const [row] = declarationsFor((selector) => selector.startsWith('.kcc-check') && selector.includes(':disabled'))

    expect(button, 'no disabled rule for .kcc-btn').toBeDefined()
    expect(row, 'no disabled rule for a .kcc-check row').toBeDefined()
    expect(row!.opacity).toBe(button!.opacity)
    expect(row!.cursor).toBe(button!.cursor)
  })

  it.each(RANGE_THUMBS)('rings a focused %s in ink, 2px out beyond its paper ring', (thumb) => {
    const rules = thumbRules(`.kcc-range:focus-visible${thumb}`).filter((rule) => !inForcedColors(rule))

    expect(rules, `no focus rule for .kcc-range${thumb}`).toHaveLength(1)
    expect(rules[0]!.selectors).toHaveLength(1)
    expect(declarationsOf(rules[0]!)['box-shadow']).toBe('0 0 0 2px var(--color-paper-2), 0 0 0 4px var(--color-focus)')
  })

  it.each(RANGE_THUMBS)('fades the ring on %s as the focus ring fades, behind the same gate', (thumb) => {
    const fades = thumbRules(`:root:focus-within:not([data-theme-switching]) .kcc-range${thumb}`)

    expect(fades).toHaveLength(1)
    expect(fades[0]!.selectors).toHaveLength(1)
    expect((fades[0]!.parent as postcss.AtRule).params).toBe('(prefers-reduced-motion: no-preference)')
    expect(declarationsOf(fades[0]!).transition).toBe('box-shadow 0.2s')
  })

  it.each(RANGE_THUMBS)('outlines a focused %s in CanvasText under forced colours', (thumb) => {
    const rules = thumbRules(`.kcc-range:focus-visible${thumb}`).filter(inForcedColors)

    expect(rules, `no forced-colours focus rule for .kcc-range${thumb}`).toHaveLength(1)
    expect(rules[0]!.selectors).toHaveLength(1)
    expect(declarationsOf(rules[0]!)).toMatchObject({ outline: '2px solid CanvasText', 'outline-offset': '2px' })
  })

  // kit.md, "Every surface" rule 5: text lands on the rule by itself, with no pixel nudge.
  it('nudges no text off the rule by a pixel', () => {
    const nudges: string[] = []
    kit.walkDecls((decl) => {
      if (/^(transform|translate)$/.test(decl.prop) && /(^|[\s(])-?[1-3]px\b/.test(decl.value)) {
        nudges.push(`${(decl.parent as postcss.Rule).selector} { ${decl.prop}: ${decl.value} }`)
      }
    })

    expect(nudges).toEqual([])
  })
})
