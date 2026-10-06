import { readFileSync } from 'node:fs'
import { relative } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { files } from '../../support/files'

const WEB = fileURLToPath(new URL('../../../../src/KCC.Web/', import.meta.url))

// The pad is sticky over every page, so anything else that sticks has to sit below it or slide under it. A class token
// in markup, or the declaration in CSS; prose that mentions stickiness is not a sticky element.
const STICKY = /(?<=["'\s])(?:[^\s"':]+:)*sticky(?=["'\s])|position:\s*sticky/
const THE_PAD = 'Features/Components/Header/AppHeader.Component.vue'

const sticking = [...files(`${WEB}Features`, /\.(vue|cshtml|css)$/)]
  .map((file) => ({ path: relative(WEB, file).replaceAll('\\', '/'), source: readFileSync(file, 'utf8') }))
  .filter(({ path, source }) => path !== THE_PAD && STICKY.test(source))

describe('sticky neighbours', () => {
  it('finds the library filters and both halves of the home stacker', () => {
    expect(sticking.map(({ path }) => path).sort()).toEqual([
      'Features/Pages/Home/Blocks/StackerBlock.cshtml',
      'Features/Pages/RecipeSearch/RecipeSearchView.Component.vue',
      'Features/Widgets/Stacker/Stacker.Component.vue',
    ])
  })

  it('sit below the pad while it shows', () => {
    const unaware = sticking.filter(({ source }) => !source.includes('var(--pad-offset')).map(({ path }) => path)

    expect(unaware, 'add var(--pad-offset, 0px) to the top of each sticky element').toEqual([])
  })
})
