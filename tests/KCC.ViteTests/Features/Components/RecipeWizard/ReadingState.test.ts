import { describe, expect, it } from 'vitest'
import ReadingState from '~/Components/RecipeWizard/ReadingState.vue'
import { buttonWith, classesOf, renderInWizard, unresolvedKeys } from '../../../support/recipeWizard'
import { expectNoRetiredMarkup, tagWith } from '../../../support/ssr'

const REQUEST = { flow: 'recipe', photos: [new Blob(['page'], { type: 'image/jpeg' })] }

const render = () => renderInWizard(ReadingState, { request: REQUEST, tear: 4 })

const region = (html: string) => html.slice(html.indexOf('data-testid="reading-state"'))

describe('ReadingState', () => {
  it('sits on a crisp sheet', async () => {
    const slip = tagWith(await render(), 'kcc-slip')

    expect(classesOf(slip)).toEqual(['kcc-slip', 'kcc-tear-4'])
    expect(slip).toContain('--r:0')
  })

  it('marks its region busy while the read runs', async () => {
    expect(tagWith(await render(), 'data-testid="reading-state"')).toContain('aria-busy="true"')
  })

  it('spins the loader beside the reading message, in the region', async () => {
    const message = region(await render()).match(/<h2 class="[^"]*\bkcc-kick\b[^"]*">[\s\S]*?<\/h2>/)?.[0] ?? ''

    expect(message).toContain('<i class="fa-solid fa-circle-notch fa-spin" aria-hidden="true"></i>')
    expect(message).toContain('«Reading»')
  })

  it('offers Cancel, in the region', async () => {
    const cancel = buttonWith(region(await render()), 'data-testid="reading-cancel"')

    expect(cancel).toContain('type="button"')
    expect(classesOf(cancel)).toEqual(expect.arrayContaining(['kcc-btn', 'kcc-btn--ghost']))
    expect(cancel).toContain('«ReadingCancel»')
  })

  it('resolves every string under the RecipeWizard group', async () => {
    expect(unresolvedKeys(await render())).toEqual([])
  })

  it('uses no retired markup', async () => {
    expectNoRetiredMarkup(await render())
  })
})
