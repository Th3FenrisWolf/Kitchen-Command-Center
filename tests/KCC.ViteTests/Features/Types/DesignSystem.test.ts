import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { BACKGROUND_COLORS, TEXT_COLORS, WASHES, washOf } from '~/Types/DesignSystem'

const tailwindConfigCss = readFileSync(
  fileURLToPath(new URL('../../../../src/KCC.Web/Features/Styles/TailwindConfig.css', import.meta.url)),
  'utf8',
)

describe('TailwindConfig.css safelist', () => {
  // Tailwind scans .cshtml, .vue and .css only. The C# side (`TailwindStyleAttribute`, `SectionColors`, the
  // section padding ladder) builds class strings the scanner never sees, so a token this file does not
  // safelist is emitted only when it happens to appear in markup, and an unemitted class is a silent no-op
  // rather than a build error.
  const safelisted = [...tailwindConfigCss.matchAll(/@source inline\('([^']+)'\)/g)].flatMap(([, pattern]) =>
    expand(pattern),
  )

  it.each([...BACKGROUND_COLORS, ...TEXT_COLORS])('safelists %s', (token) => {
    expect(safelisted).toContain(token)
  })
})

describe('WASHES', () => {
  const RETIRED = ['rosewater', 'flamingo', 'mauve', 'maroon', 'sapphire', 'blue']

  it('lists the eight identity washes', () => {
    expect(WASHES).toEqual(['peach', 'yellow', 'green', 'teal', 'sky', 'lavender', 'pink', 'red'])
  })

  it.each(WASHES)('declares --color-%s in TailwindConfig.css', (wash) => {
    expect(tailwindConfigCss).toContain(`--color-${wash}:`)
  })

  it.each(WASHES)('keeps %s out of the retired Softbound list', (wash) => {
    expect(RETIRED).not.toContain(wash)
  })
})

describe('TEXT_COLORS', () => {
  // A wash is a fill. Ink on paper and desk, marker-ink inside a marker fill or a wash tile, and
  // nothing else: a coloured word is off-brand, and retiredTokens.test.ts fails any `text-<wash>`.
  it('offers ink and marker-ink only', () => {
    expect(TEXT_COLORS).toEqual(['text-ink', 'text-ink-soft', 'text-marker-ink'])
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

  // Until the migrated content is restored into the database, a stored card can still name a retired
  // hue. The sheet then renders bare paper rather than a `var(--color-rosewater)` that resolves to nothing.
  it('gives a value outside the eight washes no wash', () => {
    expect(washOf('bg-rosewater' as never)).toBeUndefined()
  })
})
