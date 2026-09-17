import { existsSync, readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

// Typography.css is the base of the 24px rule and the only place the brand fonts are declared. Pin the
// numbers the identity promises (docs/brand/torn-and-waxed.md → Type) and make sure every self-hosted font
// file it references is actually in the repo — a missing woff2 falls back silently to the next family.
const stylesDir = new URL('../../../../src/KCC.Web/Features/Styles/', import.meta.url)
const css = readFileSync(fileURLToPath(new URL('Typography.css', stylesDir)), 'utf8')

const rule = (selector: string) => {
  const match = css.match(
    new RegExp(`(?:^|\\n)\\s*${selector.replace(/[.*+?^${}()|[\\]\\\\]/g, '\\\\$&')}\\s*\\{([^}]*)\\}`),
  )
  if (!match) throw new Error(`no rule for "${selector}"`)
  return match[1]!.replace(/\\s+/g, ' ')
}

describe('Typography.css', () => {
  it('sets the body on the 24px rule at 15px, one weight', () => {
    const body = rule('body')
    expect(body).toContain('font-size: 15px')
    expect(body).toContain('line-height: var(--bl, 24px)')
    expect(body).toContain('font-weight: 400')
  })

  it('takes display over two rules and every heading level in APCasual', () => {
    expect(rule('h1')).toContain('font-size: 40px')
    expect(rule('h1')).toContain('line-height: 48px')
    expect(css).toMatch(/h1,\s*h2,\s*h3,\s*h4,\s*h5,\s*h6\s*\{[^}]*font-family: var\(--font-casual\)/)
    expect(css).toMatch(/h3,\s*h4,\s*h5,\s*h6\s*\{[^}]*font-size: 22px/)
  })

  it('keeps form controls at 16px so iOS never zooms into a field', () => {
    expect(css).toMatch(/input,\s*select,\s*textarea\s*\{[^}]*font-size: 16px/)
  })

  it('declares Sono for meta and numbers and ships every font file it references', () => {
    expect(css).toContain("font-family: 'Sono'")
    const files = [...css.matchAll(/url\('\.\/fonts\/([^']+)'\)/g)].map((m) => m[1]!)
    expect(files.length).toBeGreaterThanOrEqual(8)
    for (const file of files) {
      expect(existsSync(fileURLToPath(new URL(`fonts/${file}`, stylesDir))), `${file} is missing`).toBe(true)
    }
  })
})
