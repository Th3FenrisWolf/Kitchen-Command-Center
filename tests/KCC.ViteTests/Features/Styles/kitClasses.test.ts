import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

// docs/brand/kit.md is the contract; the Torn CSS is the implementation. Drift either way is a real bug: an
// undocumented class gets reinvented by the next component, and a documented class that no stylesheet
// defines gets written into markup where it silently does nothing.
const repoRoot = new URL('../../../../', import.meta.url)
const read = (path: string) => readFileSync(fileURLToPath(new URL(path, repoRoot)), 'utf8')

const CSS_FILES = ['Kit.css', 'Controls.css', 'Tears.css'] as const
const doc = read('docs/brand/kit.md')
const css = CSS_FILES.map((name) => read(`src/KCC.Web/Features/Styles/Torn/${name}`)).join('\n')

// A class name never ends on a hyphen: `kcc-tear-${index}` in prose has to read as the tear family, not as
// a class literally called `kcc-tear-`. The double hyphen is the modifier separator (`kcc-btn--ghost`).
const NAME = 'kcc-[a-z0-9]+(?:-{1,2}[a-z0-9]+)*'

// The docs write a class in a code span on its own (`kcc-slip`), as part of a selector (`li.kcc-done`,
// `.kcc-sheet > .kcc-stat`) or inside the markup examples (class="kcc-slip kcc-tear-3"). A `#` prefix is an
// SVG filter id (`#kcc-wax`), not a class, and `\w`/`-` before the name means it is part of a longer token.
const DOCUMENTED = new RegExp(`(?<![\\w#-])(${NAME})`, 'g')
// Modifiers hang off their base class as a suffix list: `kcc-btn` (+`--ghost`, `--ink`) documents two more.
const MODIFIER_LIST = new RegExp(`(${NAME})\`\\s*\\(\\+([^)]+)\\)`, 'g')
const MODIFIER = /--[a-z0-9-]+/g
const CSS_CLASS = new RegExp(`\\.(${NAME})`, 'g')

// Tears.css is generated, so its presets are documented as a family (`kcc-tear-1 … 6`, `kcc-tear-tile-1..3`)
// rather than one row each. The bare `kcc-tear` form is what the interpolated mentions collapse to.
const TEAR = /^kcc-tear(?:-(?:hero|tile-\d+|\d+))?$/
// Written in the docs as prose, not as a class that should exist: the `.kcc-sheet > .kcc-x` placeholder in
// Structure rule 3.
const NOT_A_CLASS = new Set(['kcc-x'])

const documented = new Set<string>()
for (const [, name] of doc.matchAll(DOCUMENTED)) documented.add(name!)
for (const [, base, list] of doc.matchAll(MODIFIER_LIST)) {
  for (const modifier of list!.match(MODIFIER) ?? []) documented.add(`${base}${modifier}`)
}

const defined = new Set<string>()
for (const [, name] of css.matchAll(CSS_CLASS)) defined.add(name!)

describe('the kit classes and kit.md agree', () => {
  // A regex that stopped matching would make both assertions below pass vacuously.
  it('reads classes from both sides', () => {
    expect(documented.size).toBeGreaterThan(40)
    expect(defined.size).toBeGreaterThan(40)
    expect(documented.has('kcc-btn--ghost'), 'the modifier suffix lists were not expanded').toBe(true)
  })

  it('documents nothing the CSS does not define', () => {
    const missing = [...documented].filter((name) => !defined.has(name) && !TEAR.test(name) && !NOT_A_CLASS.has(name))
    expect(missing.sort(), `documented in kit.md but defined in none of ${CSS_FILES.join(', ')}`).toEqual([])
  })

  it('defines nothing kit.md does not document', () => {
    const undocumented = [...defined].filter((name) => !documented.has(name) && !TEAR.test(name))
    expect(undocumented.sort(), 'defined in the kit CSS but missing from docs/brand/kit.md').toEqual([])
  })
})

// Nothing is shadowed except the sheet's fall, which is a filter on .kcc-torn. box-shadow survives in the kit
// only as a device that draws no shadow: the inset hairline and ring on controls, and the paper ring that
// keeps a range thumb off the marker fill. Anything else here is a drawn shadow on a printed thing.
const SHADOW_DEVICES = [
  /^inset 0 0 0 [12]px var\(--color-(?:hair-strong|ink)\)$/,
  /^0 0 0 2px var\(--color-paper-2\)$/,
  /^none$/,
]

describe('the kit draws no shadow but the fall', () => {
  it('uses box-shadow only for hairlines and rings', () => {
    const declarations = [...css.matchAll(/box-shadow:\s*([^;]+);/g)].map(([, value]) => value!.trim())
    expect(declarations.length).toBeGreaterThan(5)
    const drawn = declarations.filter((value) => !SHADOW_DEVICES.some((device) => device.test(value)))
    expect(drawn, 'a box-shadow in the Torn CSS that is not a hairline or a ring').toEqual([])
  })
})
