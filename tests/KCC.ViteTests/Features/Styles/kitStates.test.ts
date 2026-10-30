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

describe('the kit states', () => {
  it('gives a disabled checklist row the same look as a disabled button', () => {
    const [button] = declarationsFor((selector) => selector === '.kcc-btn:disabled')
    const [row] = declarationsFor((selector) => selector.startsWith('.kcc-check') && selector.includes(':disabled'))

    expect(button, 'no disabled rule for .kcc-btn').toBeDefined()
    expect(row, 'no disabled rule for a .kcc-check row').toBeDefined()
    expect(row!.opacity).toBe(button!.opacity)
    expect(row!.cursor).toBe(button!.cursor)
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
