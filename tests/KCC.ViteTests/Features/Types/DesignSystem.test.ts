import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { BACKGROUND_COLORS, TEXT_COLORS, WASHES, toBackgroundColor, toTextColor, washOf } from '~/Types/DesignSystem'

const tailwindConfigCss = readFileSync(
  fileURLToPath(new URL('../../../../src/KCC.Web/Features/Styles/TailwindConfig.css', import.meta.url)),
  'utf8',
)

const suffixOf = (token: string) => token.slice(token.indexOf('-') + 1)

describe('color axis mapping', () => {
  // The helpers map by index, so a token added to one axis but not the other would make them
  // return undefined behind a non-null assertion.
  it('lists the same tokens in the same order on both axes', () => {
    expect(TEXT_COLORS.map(suffixOf)).toEqual(BACKGROUND_COLORS.map(suffixOf))
  })

  it.each(TEXT_COLORS)('maps %s onto its background twin', (textColor) => {
    expect(toBackgroundColor(textColor)).toBe(`bg-${suffixOf(textColor)}`)
  })

  it.each(BACKGROUND_COLORS)('maps %s onto its text twin', (backgroundColor) => {
    expect(toTextColor(backgroundColor)).toBe(`text-${suffixOf(backgroundColor)}`)
  })

  it('round-trips a token back to itself', () => {
    expect(toTextColor(toBackgroundColor('text-paper'))).toBe('text-paper')
  })
})

describe('TailwindConfig.css safelist', () => {
  // Tailwind scans .cshtml, .vue and .css only, so a token this file does not safelist is emitted
  // only when it happens to appear in markup. Card builds drawer classes from the arrays above,
  // and an unemitted class is a silent no-op rather than a build error.
  const safelisted = [...tailwindConfigCss.matchAll(/@source inline\('([^']+)'\)/g)].flatMap(([, pattern]) =>
    expand(pattern),
  )

  it.each([...BACKGROUND_COLORS, ...TEXT_COLORS])('safelists %s', (token) => {
    expect(safelisted).toContain(token)
  })
})

describe('WASHES', () => {
  // TailwindConfig.css keeps the Softbound palette alive under a TRANSITIONAL marker until the
  // conversion finishes (see CLAUDE.md); a wash declared only after it would build today but vanish
  // once that block is deleted.
  const head = tailwindConfigCss.slice(0, tailwindConfigCss.indexOf('TRANSITIONAL'))
  const RETIRED = ['rosewater', 'flamingo', 'mauve', 'maroon', 'sapphire', 'blue']

  it('lists the eight identity washes', () => {
    expect(WASHES).toEqual(['peach', 'yellow', 'green', 'teal', 'sky', 'lavender', 'pink', 'red'])
  })

  it.each(WASHES)('declares --color-%s in TailwindConfig.css ahead of the retired Softbound tokens', (wash) => {
    expect(head).toContain(`--color-${wash}:`)
  })

  it.each(WASHES)('keeps %s out of the retired Softbound list', (wash) => {
    expect(RETIRED).not.toContain(wash)
  })
})

function expand(pattern: string): string[] {
  const group = pattern.match(/\{([^}]*)\}/)

  if (!group) {
    return [pattern]
  }

  const [placeholder, options] = group

  return options.split(',').flatMap((option) => expand(pattern.replace(placeholder, option)))
}

describe('washOf', () => {
  it('reads the wash name out of a wash background', () => {
    expect(washOf('bg-peach')).toBe('peach')
  })

  it('gives a ground no wash', () => {
    expect(washOf('bg-paper')).toBeUndefined()
  })

  // CMS content still carries Softbound hues until they are migrated; the sheet then renders bare paper
  // rather than a `var(--color-rosewater)` that resolves to nothing.
  it('gives a retired hue no wash', () => {
    expect(washOf('bg-rosewater')).toBeUndefined()
  })
})
