import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { BACKGROUND_COLORS, TEXT_COLORS, WASHES, washOf } from '~/Types/DesignSystem'

const tailwindConfigCss = readFileSync(
  fileURLToPath(new URL('../../../../src/KCC.Web/Features/Styles/TailwindConfig.css', import.meta.url)),
  'utf8',
)

describe('TailwindConfig.css safelist', () => {
  // Tailwind scans .cshtml, .vue and .css only, so a token this file does not safelist is emitted
  // only when it happens to appear in markup. Card builds its wash from the arrays above, and an
  // unemitted class is a silent no-op rather than a build error.
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
