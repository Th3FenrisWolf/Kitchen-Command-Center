import { describe, expect, it } from 'vitest'
import StartStep from '~/Components/RecipeWizard/StartStep.vue'
import { buttonWith, classesOf, renderInWizard, unresolvedKeys } from '../../../support/recipeWizard'
import { expectNoRetiredMarkup, tagWith } from '../../../support/ssr'

const ENGLISH = { ImportUnavailable: 'Photo import is resting until {0}. You can still type the recipe in.' }

const render = (props: Record<string, unknown> = {}) => renderInWizard(StartStep, { tear: 2, ...props }, ENGLISH)

const RESTING = { availableAgainOn: '2026-11-01' }

const hintFor = (html: string, testId: string) => {
  const id = tagWith(html, `data-testid="${testId}"`).match(/aria-describedby="([^"]+)"/)?.[1]
  return id ? (html.match(new RegExp(`<p[^>]*\\sid="${id}"[^>]*>([^<]*)</p>`))?.[1]?.trim() ?? '') : ''
}

describe('StartStep', () => {
  it('rests on one blank, with the wide turn', async () => {
    const html = await render()

    expect((html.match(/kcc-blank/g) ?? []).length).toBe(1)
    expect(tagWith(html, 'kcc-slip')).toContain('kcc-slip kcc-slip--wide kcc-tear-2')
  })

  it('asks how to start, on a crisp sheet', async () => {
    const html = await render()
    const slip = tagWith(html, 'kcc-slip')

    expect(classesOf(slip)).toEqual(['kcc-slip', 'kcc-slip--wide', 'kcc-tear-2'])
    expect(slip).toContain('--r:0')
    expect(html).toMatch(/<h2 class="kcc-h4">\s*«StartTitle»\s*<\/h2>/)
  })

  it('starts from photos with a large marker button', async () => {
    const html = await render()
    const photos = tagWith(html, 'data-testid="wizard-start-photos"')

    expect(photos).toMatch(/^<button /)
    expect(photos).toContain('type="button"')
    expect(classesOf(photos)).toEqual(expect.arrayContaining(['kcc-btn', 'kcc-btn--lg']))
    expect(classesOf(photos)).not.toContain('kcc-btn--ghost')
    expect(buttonWith(html, 'data-testid="wizard-start-photos"')).toContain('«StartFromPhotos»')
  })

  it('starts typing with a large ghost button', async () => {
    const html = await render()
    const typing = tagWith(html, 'data-testid="wizard-start-typing"')

    expect(typing).toMatch(/^<button /)
    expect(typing).toContain('type="button"')
    expect(classesOf(typing)).toEqual(expect.arrayContaining(['kcc-btn', 'kcc-btn--ghost', 'kcc-btn--lg']))
    expect(buttonWith(html, 'data-testid="wizard-start-typing"')).toContain('«StartTyping»')
  })

  it('describes each choice with its hint', async () => {
    const html = await render()

    expect(hintFor(html, 'wizard-start-photos')).toBe('«StartFromPhotosHint»')
    expect(hintFor(html, 'wizard-start-typing')).toBe('«StartTypingHint»')
  })

  it('says when photo import is available again, in a neutral well, while it is unavailable', async () => {
    const html = await render(RESTING)
    const notice = tagWith(html, 'data-testid="import-unavailable"')

    expect(notice).toMatch(/^<p /)
    expect(classesOf(notice)).toEqual(['kcc-well', 'kcc-kick'])
    expect(html).toMatch(
      /data-testid="import-unavailable"[^>]*>\s*Photo import is resting until November 1, 2026\. You can still type the recipe in\.\s*</,
    )
  })

  it('turns the photo choice off while the import is unavailable, and keeps the typing choice', async () => {
    const html = await render(RESTING)

    expect(tagWith(html, 'data-testid="wizard-start-photos"')).toContain('disabled')
    expect(tagWith(html, 'data-testid="wizard-start-typing"')).not.toContain('disabled')
    expect(buttonWith(html, 'data-testid="wizard-start-typing"')).toContain('«StartTyping»')
  })

  it('describes the resting photo choice by its hint and by the unavailable notice', async () => {
    const html = await render(RESTING)
    const ids =
      tagWith(html, 'data-testid="wizard-start-photos"')
        .match(/aria-describedby="([^"]+)"/)?.[1]
        ?.split(' ') ?? []
    const [hintId = '', noticeId = ''] = ids

    expect(ids).toHaveLength(2)
    expect(html).toMatch(new RegExp(`<p[^>]*\\sid="${hintId}"[^>]*>\\s*«StartFromPhotosHint»\\s*</p>`))
    expect(tagWith(html, 'data-testid="import-unavailable"')).toContain(`id="${noticeId}"`)
  })

  it('shows no unavailable notice, and both choices, while the import is available', async () => {
    const html = await render()

    expect(html).not.toContain('import-unavailable')
    expect(tagWith(html, 'data-testid="wizard-start-photos"')).not.toContain('disabled')
    expect(tagWith(html, 'data-testid="wizard-start-typing"')).not.toContain('disabled')
  })

  it('resolves every string under the RecipeWizard group', async () => {
    expect(unresolvedKeys(await render(RESTING))).toEqual([])
  })

  it('uses no retired markup', async () => {
    expectNoRetiredMarkup(await render(RESTING))
  })
})
