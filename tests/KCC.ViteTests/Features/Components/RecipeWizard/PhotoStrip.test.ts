import { describe, expect, it } from 'vitest'
import PhotoStrip from '~/Components/RecipeWizard/PhotoStrip.vue'
import { buttonsWith, classesOf, renderInWizard, unresolvedKeys, wizardPhoto } from '../../../support/recipeWizard'
import { expectNoRetiredMarkup, tagWith } from '../../../support/ssr'

const PHOTOS = [1, 2, 3, 4].map(wizardPhoto)

const render = (props: Record<string, unknown> = {}) => renderInWizard(PhotoStrip, { photos: PHOTOS, ...props })

const tiles = (html: string) => buttonsWith(html, 'data-testid="photo-tile"')

const tileTag = (tile: string) => tagWith(tile, 'data-testid="photo-tile"')

const imageIn = (tile: string) => tagWith(tile, 'src=')

const everyClass = (html: string) => [...html.matchAll(/\sclass="([^"]*)"/g)].flatMap(([, names]) => names!.split(/\s+/))

describe('PhotoStrip', () => {
  it('lists the photos in the flow, named by the photos title', async () => {
    const html = await render()
    const list = tagWith(html, 'aria-label="«PhotosTitle»"')

    expect(list).toMatch(/^<ul /)
    expect(classesOf(list)).toEqual(expect.arrayContaining(['flex', 'flex-wrap']))
    expect(html.match(/<li\b/g)).toHaveLength(PHOTOS.length)
  })

  it('makes each photo a button, named by ViewPhoto, that holds the photo', async () => {
    const buttons = tiles(await render())

    expect(buttons).toHaveLength(PHOTOS.length)
    buttons.forEach((tile, index) => {
      expect(tileTag(tile)).toContain('type="button"')
      expect(tileTag(tile)).toContain('aria-label="«ViewPhoto»"')
      expect(imageIn(tile)).toMatch(/^<img /)
      expect(imageIn(tile)).toContain(`src="${PHOTOS[index]!.url}"`)
      expect(imageIn(tile)).toContain('alt=""')
      expect(classesOf(imageIn(tile))).toEqual(expect.arrayContaining(['block', 'size-20', 'object-cover']))
    })
  })

  it('tears each photo with a tile preset that neither neighbour shares', async () => {
    const buttons = tiles(await render())
    const presets = buttons.map((tile) => classesOf(tileTag(tile)).find((name) => /^kcc-tear-tile-[123]$/.test(name)))

    buttons.forEach((tile) => {
      expect(classesOf(tileTag(tile))).toContain('kcc-torn')
      expect(classesOf(imageIn(tile))).toContain('[clip-path:var(--tear)]')
    })
    expect(presets.every(Boolean)).toBe(true)
    presets.slice(1).forEach((preset, index) => expect(preset).not.toBe(presets[index]))
  })

  it('pins no wax tile', async () => {
    const names = everyClass(await render({ removable: true }))

    expect(names).not.toContain('kcc-tilewrap')
    expect(names).not.toContain('kcc-tile')
    expect(names).not.toContain('kcc-tile--lg')
  })

  it('puts a remove button, named by RemovePhoto, beside each photo on the photo step, never inside it', async () => {
    const html = await render({ removable: true })
    const removes = buttonsWith(html, 'data-testid="photo-remove"')

    expect(removes).toHaveLength(PHOTOS.length)
    removes.forEach((remove) => {
      const tag = tagWith(remove, 'data-testid="photo-remove"')
      expect(tag).toContain('type="button"')
      expect(tag).toContain('aria-label="«RemovePhoto»"')
      expect(classesOf(tag)).toEqual(expect.arrayContaining(['kcc-btn', 'kcc-btn--icon']))
    })
    tiles(html).forEach((tile) => expect(tile).not.toContain('photo-remove'))
  })

  it('offers no remove button in its view-only mode', async () => {
    const html = await render()

    expect(tiles(html)).toHaveLength(PHOTOS.length)
    expect(html).not.toContain('photo-remove')
  })

  it('prints no photo viewer on the server', async () => {
    const html = await render()

    expect(html).not.toContain('photo-viewer')
    expect(html).not.toContain('role="dialog"')
    expect(html).not.toContain('<!--teleport')
  })

  it('resolves every string under the RecipeWizard group', async () => {
    expect(unresolvedKeys(await render({ removable: true }))).toEqual([])
  })

  it('uses no retired markup', async () => {
    expectNoRetiredMarkup(await render({ removable: true }))
  })
})
