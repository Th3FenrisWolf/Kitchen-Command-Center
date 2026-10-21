import { describe, expect, it } from 'vitest'
import PhotoStep from '~/Components/RecipeWizard/PhotoStep.vue'
import {
  buttonHolding,
  buttonWith,
  buttonsWith,
  classesOf,
  enclosingTag,
  markerButtons,
  renderInWizard,
  unresolvedKeys,
  wizardPhoto,
} from '../../../support/recipeWizard'
import { expectNoRetiredMarkup, tagWith } from '../../../support/ssr'

const ENGLISH = {
  PhotoCount: '{0} of {1} photos',
  TooManyPhotos: 'An import holds up to {0} photos.',
  ImportUnavailable: 'Photo import is resting until {0}. You can still type the recipe in.',
}

const ONE = [wizardPhoto(1)]
const TEN = Array.from({ length: 10 }, (_, index) => wizardPhoto(index + 1))

const render = (props: Record<string, unknown> = {}) =>
  renderInWizard(PhotoStep, { photos: [], canGoBack: true, tear: 3, ...props }, ENGLISH)

const alerts = (html: string) => html.match(/<p[^>]*role="alert"[^>]*>[\s\S]*?<\/p>/g) ?? []

const WELL = ['kcc-well', 'kcc-kick']

describe('PhotoStep', () => {
  it('rests on one blank, with the wide turn', async () => {
    const html = await render()

    expect((html.match(/kcc-blank/g) ?? []).length).toBe(1)
    expect(tagWith(html, 'kcc-slip')).toContain('kcc-slip kcc-slip--wide kcc-tear-3')
  })

  it('sits on a crisp sheet, labelled and headed with the photos title', async () => {
    const html = await render()
    const slip = tagWith(html, 'kcc-slip')

    expect(classesOf(slip)).toEqual(['kcc-slip', 'kcc-slip--wide', 'kcc-tear-3'])
    expect(slip).toContain('--r:0')
    expect(html).toMatch(
      /<span class="kcc-label"><i class="fa-duotone fa-camera" aria-hidden="true"><\/i>[\s\S]*?«PhotosTitle»/,
    )
    expect(html).toMatch(/<h2 class="sr-only">\s*«PhotosTitle»\s*<\/h2>/)
  })

  it('shows the camera view, with its shutter named by TakePhoto', async () => {
    const html = await render()
    const view = tagWith(html, 'data-testid="camera-view"')
    const shutter = tagWith(html, 'data-testid="camera-shutter"')

    expect(view).toMatch(/^<video /)
    expect(view).toContain('playsinline')
    expect(view).toContain('muted')
    expect(shutter).toMatch(/^<button /)
    expect(shutter).toContain('type="button"')
    expect(shutter).toContain('aria-label="«TakePhoto»"')
    expect(classesOf(shutter)).toEqual(expect.arrayContaining(['kcc-btn', 'kcc-btn--ghost', 'kcc-btn--icon']))
  })

  it('marks the camera busy while it starts', async () => {
    const html = await render()

    expect(tagWith(html, 'aria-busy="true"')).toMatch(/^<div /)
    expect(html).toMatch(/aria-busy="true"[^>]*>\s*<video [^>]*data-testid="camera-view"/)
  })

  it('keeps Read recipe as the one marker button on the sheet, with the camera live or a read again pending', async () => {
    const html = await render({ photos: ONE, canRead: true, confirmingRead: true })
    const markers = markerButtons(html)

    expect(markers).toHaveLength(1)
    expect(markers[0]).toContain('data-testid="read-photos"')
  })

  it('adds photos from the device through a file input for images', async () => {
    const html = await render()
    const input = tagWith(html, 'data-testid="photo-input"')

    expect(input).toMatch(/^<input /)
    expect(input).toContain('type="file"')
    expect(input).toContain('accept="image/*"')
    expect(input).toContain('multiple')
    expect(classesOf(buttonHolding(html, '«AddPhotos»'))).toEqual(expect.arrayContaining(['kcc-btn', 'kcc-btn--ghost']))
  })

  it('tells the member where the photos go before a read', async () => {
    expect(await render()).toContain('«PrivacyNote»')
  })

  it('counts the photos against the limit of ten', async () => {
    expect(await render({ photos: [wizardPhoto(1), wizardPhoto(2)] })).toContain('2 of 10 photos')
  })

  it('shows each photo with a remove button beside it', async () => {
    const html = await render({ photos: [wizardPhoto(1), wizardPhoto(2)] })

    expect(buttonsWith(html, 'data-testid="photo-tile"')).toHaveLength(2)
    expect(buttonsWith(html, 'data-testid="photo-remove"')).toHaveLength(2)
  })

  it('holds Read recipe until the flow can start a read', async () => {
    const empty = tagWith(await render(), 'data-testid="read-photos"')
    const ready = buttonWith(await render({ photos: ONE, canRead: true }), 'data-testid="read-photos"')

    expect(empty).toMatch(/^<button /)
    expect(empty).toContain('disabled')
    expect(tagWith(ready, 'data-testid="read-photos"')).not.toContain('disabled')
    expect(ready).toContain('«ReadRecipe»')
  })

  it('holds Read recipe with photos present while the flow cannot start a read', async () => {
    expect(tagWith(await render({ photos: ONE, canRead: false }), 'data-testid="read-photos"')).toContain('disabled')
  })

  it('refuses an eleventh photo with a message that names the limit', async () => {
    const [alert = ''] = alerts(await render({ photos: TEN, photoErrors: ['TooManyPhotos'] }))

    expect(classesOf(alert)).toEqual(expect.arrayContaining([...WELL, 'kcc-well--danger']))
    expect(alert).toContain('An import holds up to 10 photos.')
  })

  it('refuses a file it cannot read with a message', async () => {
    const [alert = ''] = alerts(await render({ photoErrors: ['UnsupportedFile'] }))

    expect(classesOf(alert)).toEqual(expect.arrayContaining([...WELL, 'kcc-well--danger']))
    expect(alert).toContain('«UnsupportedFile»')
  })

  it('shows each refusal of one pick in a danger well of its own', async () => {
    const wells = alerts(await render({ photos: TEN, photoErrors: ['UnsupportedFile', 'TooManyPhotos'] }))

    expect(wells).toHaveLength(2)
    wells.forEach((well) => expect(classesOf(well)).toEqual(expect.arrayContaining([...WELL, 'kcc-well--danger'])))
    expect(wells[0]).toContain('«UnsupportedFile»')
    expect(wells[1]).toContain('An import holds up to 10 photos.')
  })

  it('says when a read found no recipe, and offers to type it in', async () => {
    const html = await render({ photos: ONE, noRecipeFound: true })
    const [alert = ''] = alerts(html)

    expect(classesOf(alert)).toEqual(expect.arrayContaining([...WELL, 'kcc-well--warning']))
    expect(alert).toContain('«NoRecipeFound»')
    expect(buttonHolding(html, '«TypeInstead»')).toContain('type="button"')
    expect(buttonWith(html, 'data-testid="read-photos"')).toContain('«ReadRecipe»')
  })

  it('shows a failed read, and offers to try again or type it in', async () => {
    const html = await render({ photos: ONE, readError: 'ReadFailed' })
    const error = tagWith(html, 'data-testid="read-error"')

    expect(error).toMatch(/^<p /)
    expect(error).toContain('role="alert"')
    expect(classesOf(error)).toEqual(expect.arrayContaining([...WELL, 'kcc-well--danger']))
    expect(html).toMatch(/data-testid="read-error"[^>]*>\s*«ReadFailed»\s*</)
    expect(buttonWith(html, 'data-testid="read-photos"')).toContain('«TryAgain»')
    expect(buttonHolding(html, '«TypeInstead»')).toContain('type="button"')
  })

  it('announces when photo import is available again after the ceiling, and offers to type it in', async () => {
    const html = await render({ photos: ONE, availableAgainOn: '2026-11-01' })
    const notice = tagWith(html, 'data-testid="import-unavailable"')
    const [alert = '', ...others] = alerts(html)

    expect(notice).toMatch(/^<p /)
    expect(classesOf(notice)).toEqual(['kcc-well', 'kcc-kick'])
    expect(html).toMatch(
      /data-testid="import-unavailable"[^>]*>\s*Photo import is resting until November 1, 2026\. You can still type the recipe in\.\s*</,
    )
    expect(alert).toContain('data-testid="import-unavailable"')
    expect(others).toEqual([])
    expect(buttonHolding(html, '«TypeInstead»')).toContain('type="button"')
  })

  it('wraps Back, Type it in and Read recipe two rules apart while the import rests', async () => {
    const html = await render({ photos: ONE, availableAgainOn: '2026-11-01' })

    expect(classesOf(enclosingTag(html, 'fa-arrow-left', 1))).toEqual(expect.arrayContaining(['kcc-row', 'justify-between']))
    expect(classesOf(enclosingTag(html, 'data-testid="read-photos"'))).toEqual(
      expect.arrayContaining(['flex-wrap', 'gap-x-3', 'gap-y-rule-0.5']),
    )
  })

  it('keeps the camera on the rule: a rule between its lines, and a preview a whole number of rules tall', async () => {
    const html = await render({ photos: ONE })
    const column = classesOf(enclosingTag(html, 'data-testid="camera-view"', 0))

    expect(column).toContain('gap-y-rule-1')
    expect(column).not.toContain('gap-3')
    expect(classesOf(tagWith(html, 'data-testid="camera-view"'))).toEqual(
      expect.arrayContaining(['h-[round(down,60vh,var(--bl))]', 'object-contain', 'bg-paper-2']),
    )
  })

  it('sets Add photos and the photo count in a kit row, so the count sits on the rule and the note under it too', async () => {
    const html = await render({ photos: ONE })
    const row = classesOf(enclosingTag(html, 'fa-images', 1))

    expect(row).toEqual(['kcc-row', 'justify-between'])
    expect(enclosingTag(html, 'class="kcc-kick" aria-live', 0)).toContain('kcc-row')
  })

  it('stops capturing while the import rests, and keeps the photos with their remove buttons', async () => {
    const html = await render({ photos: [wizardPhoto(1), wizardPhoto(2)], availableAgainOn: '2026-11-01' })

    expect(html).not.toContain('data-testid="camera-view"')
    expect(html).not.toContain('data-testid="camera-shutter"')
    expect(html).not.toContain('data-testid="photo-input"')
    expect(html).not.toContain('«AddPhotos»')
    expect(html).not.toContain('2 of 10 photos')
    expect(buttonsWith(html, 'data-testid="photo-tile"')).toHaveLength(2)
    expect(buttonsWith(html, 'data-testid="photo-remove"')).toHaveLength(2)
    expect(buttonHolding(html, '«Back»')).toContain('type="button"')
  })

  it('shows no message and no way to type it in while nothing has gone wrong', async () => {
    const html = await render({ photos: ONE })

    expect(alerts(html)).toEqual([])
    expect(html).not.toContain('import-unavailable')
    expect(html).not.toContain('«TypeInstead»')
  })

  it('goes back from a ghost button', async () => {
    expect(classesOf(buttonHolding(await render(), '«Back»'))).toEqual(expect.arrayContaining(['kcc-btn', 'kcc-btn--ghost']))
  })

  it('warns that reading again replaces the edits while a confirmation is pending, with Read again and Cancel', async () => {
    const html = await render({ photos: ONE, canRead: true, confirmingRead: true })
    const warning = tagWith(html, 'data-testid="read-again-warning"')

    expect(warning).toMatch(/^<p /)
    expect(warning).toContain('role="alert"')
    expect(classesOf(warning)).toEqual(expect.arrayContaining([...WELL, 'kcc-well--warning']))
    expect(html).toMatch(/data-testid="read-again-warning"[^>]*>\s*«ReadAgainWarning»\s*</)
    expect(buttonWith(html, 'data-testid="read-again-confirm"')).toContain('«ReadAgain»')
    expect(classesOf(tagWith(html, 'data-testid="read-again-confirm"'))).toEqual(
      expect.arrayContaining(['kcc-btn', 'kcc-btn--ghost']),
    )
    expect(buttonHolding(html, '«ReadingCancel»')).toContain('type="button"')
  })

  it('holds the read-again confirmation while the flow cannot start a read', async () => {
    const held = await render({ photos: [], canRead: false, confirmingRead: true })
    const ready = await render({ photos: ONE, canRead: true, confirmingRead: true })

    expect(tagWith(held, 'data-testid="read-again-confirm"')).toContain('disabled')
    expect(tagWith(ready, 'data-testid="read-again-confirm"')).not.toContain('disabled')
  })

  it('shows no read-again warning while no confirmation is pending', async () => {
    const html = await render({ photos: ONE, canRead: true })

    expect(html).not.toContain('read-again-warning')
    expect(html).not.toContain('read-again-confirm')
  })

  it('offers Back to review from a ghost button while the flow returns to the review', async () => {
    const html = await render({ photos: ONE, canRead: true, returnsToReview: true })
    const button = buttonWith(html, 'data-testid="back-to-review"')

    expect(classesOf(tagWith(button, 'data-testid="back-to-review"'))).toEqual(
      expect.arrayContaining(['kcc-btn', 'kcc-btn--ghost']),
    )
    expect(button).toContain('«BackToReview»')
    expect(buttonHolding(html, '«Back»')).toContain('type="button"')
  })

  it('keeps Back to review while the import rests after a 409', async () => {
    const html = await render({ photos: ONE, availableAgainOn: '2026-11-01', returnsToReview: true })

    expect(buttonWith(html, 'data-testid="back-to-review"')).toContain('«BackToReview»')
  })

  it('offers no Back to review while the flow does not return to the review', async () => {
    expect(await render({ photos: ONE, canRead: true })).not.toContain('back-to-review')
  })

  it('prints no photo viewer on the server', async () => {
    const html = await render({ photos: ONE })

    expect(html).not.toContain('photo-viewer')
    expect(html).not.toContain('role="dialog"')
    expect(html).not.toContain('<!--teleport')
  })

  it('resolves every string under the RecipeWizard group', async () => {
    expect(
      unresolvedKeys(
        await render({ photos: ONE, readError: 'ReadFailed', photoErrors: ['UnsupportedFile', 'TooManyPhotos'] }),
      ),
    ).toEqual([])
    expect(unresolvedKeys(await render({ photos: ONE, availableAgainOn: '2026-11-01' }))).toEqual([])
    expect(unresolvedKeys(await render({ photos: ONE, confirmingRead: true, returnsToReview: true }))).toEqual([])
  })

  it('uses no retired markup', async () => {
    expectNoRetiredMarkup(
      await render({ photos: ONE, readError: 'ReadFailed', photoErrors: ['UnsupportedFile', 'TooManyPhotos'] }),
    )
  })
})
