import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import VariantCookNotes from '~/Components/VariantDetail/VariantCookNotes.vue'

// The notes themselves arrive from `/api/variant/{guid}/notes` in `onMounted`, which never runs on the
// server: every render below is the empty, pre-fetch sheet. The populated list — the author kick, the note
// body, the delete button and Load more — is covered by VariantCookNotesTests in the e2e suite.
const render = (over: Record<string, unknown> = {}) =>
  renderSsr(VariantCookNotes, { variantGuid: '11111111-2222-3333-4444-555555555555', ...over })

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

describe('VariantCookNotes sheet', () => {
  it('is a section sheet on the fourth tear, labelled with the pen nib', async () => {
    const html = await render()

    expect(tagWith(html, 'kcc-slip')).toMatch(/^<section class="kcc-slip kcc-tear-4"/)
    expect(html).toContain('<span class="kcc-label"><i class="fa-duotone fa-pen-nib" aria-hidden="true"></i>')
    expect(html).toMatch(/kcc-label[^>]*>.*?CookNotes/s)
    expect(html).not.toContain('kcc-wash')
    expect(html).not.toMatch(/sk-[a-z]/)
  })

  it('keeps the section heading for assistive tech while the label carries the printed name', async () => {
    const html = await render()

    expect(html).toMatch(/<h2 class="sr-only">\s*CookNotes\s*<\/h2>/)
    expect((html.match(/kcc-label/g) ?? []).length).toBe(1)
  })

  it('gives a signed-in cook the kit textarea, with the e2e hook on the textarea itself', async () => {
    const html = await render({ isAuthenticated: true })

    expect(html).toMatch(/<label class="kcc-field kcc-field--area[^"]*">/)
    expect(tagWith(html, 'data-testid="cook-note-input"')).toMatch(/^<textarea /)
    expect(html).toContain('placeholder="CookNotePlaceholder"')
  })

  it('posts the note from a marker pill that waits for something to say', async () => {
    const add = tagWith(await render({ isAuthenticated: true }), 'data-testid="add-cook-note"')

    expect(add).toMatch(/^<button /)
    expect(add).toContain('kcc-btn')
    expect(add).not.toContain('kcc-btn--')
    expect(add).toContain('disabled')
  })

  it('shows no compose box to a reader who is not signed in', async () => {
    const html = await render()

    expect(html).not.toContain('cook-note-input')
    expect(html).not.toContain('add-cook-note')
    expect(html).toContain('kcc-label')
  })

  it('says the sheet is empty in body copy, and prints no list until there is one', async () => {
    const html = await render()

    expect(html).toMatch(/class="kcc-body[^"]*">\s*NoCookNotesYet\s*</)
    expect(html).not.toContain('cook-notes-list')
    expect(html).not.toContain('border-dashed')
  })

  it('carries no Softbound hook, coloured status text, weight or paper radius', async () => {
    const html = await render({ isAuthenticated: true })

    expect(html).not.toContain('sk-')
    expect(html).not.toMatch(/text-(?:danger|warning|success)/)
    expect(html).not.toContain('font-bold')
    expect(html).not.toMatch(/rounded-(?:lg|xl|2xl|3xl)/)
  })
})
