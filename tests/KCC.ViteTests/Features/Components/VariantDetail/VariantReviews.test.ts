import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import VariantReviews from '~/Components/VariantDetail/VariantReviews.vue'

// The reviews themselves arrive from `/api/variant/{guid}/reviews` in `onMounted`, which never runs on the
// server: every render below is the pre-fetch section, driven by the server-rendered average and count.
// The written reviews — their slips, the author kick, the date and Load more — are covered by
// VariantReviewsTests in the e2e suite.
const render = (over: Record<string, unknown> = {}) =>
  renderSsr(VariantReviews, { variantGuid: '11111111-2222-3333-4444-555555555555', ...over })

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

describe('VariantReviews section', () => {
  it('names the section on the rule and counts the reviews beside it', async () => {
    const html = await render({ reviewCount: 12 })

    expect(html).toMatch(/<div class="kcc-secname"><h2>\s*RatingsReviews\s*<\/h2>/)
    expect(html).toMatch(/<p class="kcc-kick"><span class="kcc-num">12<\/span>\s*<span>Reviews<\/span><\/p>/)
  })

  it('sets the blocks 36px apart down the section', async () => {
    expect(await render({ reviewCount: 12 })).toContain('space-y-9')
  })
})

describe('VariantReviews summary', () => {
  it('is one plain slip — no label, no tape — on a tear of its own', async () => {
    // Signed out, so the summary is the only sheet on the section and carries the whole assertion.
    const html = await render({ reviewCount: 12, averageRating: 4.5 })

    expect(tagWith(html, 'kcc-slip')).toContain('kcc-slip kcc-tear-5')
    expect(html).not.toContain('kcc-label')
    expect(html).not.toContain('kcc-tape')
    expect(html).not.toContain('kcc-wash')
  })

  it('prints the average as stars and a Sono number, with the count under it', async () => {
    const html = await render({ reviewCount: 12, averageRating: 4.5 })

    expect(html).toContain('<span class="kcc-num">4.5</span>')
    expect(html).toContain('aria-label="4.5 of 5 stars"')
    expect(html).not.toContain('font-casual text-[3.5rem]')
  })

  it('draws the distribution as five plain bars on the rule, 5 star down to 1', async () => {
    const html = await render({ reviewCount: 12, averageRating: 4.5 })
    const rows = [...html.matchAll(/<p class="kcc-kick">(\d)★/g)].map((match) => match[1])

    expect(rows).toEqual(['5', '4', '3', '2', '1'])
    expect((html.match(/class="block h-1\.5 bg-peach"/g) ?? []).length).toBe(5)
    expect(html).not.toContain('bg-desk-2')
    expect(html).not.toContain('rounded-full')
  })

  it('keeps the summary off the page until there is something to summarize', async () => {
    const html = await render()

    expect(html).not.toContain('kcc-tear-5')
    expect(html).not.toContain('★')
  })
})

describe('VariantReviews list', () => {
  it('says the sheet is empty in body copy, with no dashed box and no great grey star', async () => {
    const html = await render()

    expect(html).toMatch(/class="kcc-body[^"]*">\s*NoReviewsYet\s*</)
    expect(html).not.toContain('reviews-list')
    expect(html).not.toContain('border-dashed')
    expect(html).not.toContain('fa-regular fa-star text-4xl')
  })
})

describe('VariantReviews form', () => {
  it('is a crisp labelled sheet, torn apart from the summary', async () => {
    const html = await render({ isAuthenticated: true })
    const form = tagWith(html, 'kcc-tear-3')

    expect(form).toContain('kcc-slip kcc-tear-3')
    expect(form).toContain('--r:0')
    expect(html).toContain('<span class="kcc-label"><i class="fa-duotone fa-comment-pen" aria-hidden="true"></i>')
    expect(html).toMatch(/kcc-label[^>]*>.*?YourReview/s)
  })

  it('takes the rating on the slider and the text in the kit field, hooks on the controls', async () => {
    const html = await render({ isAuthenticated: true })

    expect(html).toContain('role="slider"')
    expect(html).toMatch(/<label class="kcc-field kcc-field--area">/)
    expect(tagWith(html, 'data-testid="review-input"')).toMatch(/^<textarea /)
    expect(html).toContain('placeholder="WriteReview"')
  })

  it('submits from a marker pill that waits for half a star, and offers no delete until there is a review', async () => {
    const html = await render({ isAuthenticated: true })
    const submit = tagWith(html, 'data-testid="submit-review"')

    expect(submit).toMatch(/^<button /)
    expect(submit).toContain('kcc-btn')
    expect(submit).not.toContain('kcc-btn--')
    expect(submit).toContain('disabled')
    expect(html).not.toContain('delete-review')
  })

  it('asks a signed-out reader to log in, in body copy rather than a dashed box', async () => {
    const html = await render()

    expect(html).toMatch(/class="kcc-body[^"]*">\s*LogInToReview\s*</)
    expect(html).not.toContain('review-input')
    expect(html).not.toContain('submit-review')
  })

  it('carries no Softbound remnant, coloured status text, weight or paper radius', async () => {
    const html = await render({ isAuthenticated: true, reviewCount: 12, averageRating: 4.5 })

    expect(html).not.toMatch(/sk-[a-z]/)
    expect(html).not.toMatch(/text-(?:danger|warning|success|rating)/)
    expect(html).not.toContain('font-bold')
    expect(html).not.toMatch(/rounded-(?:lg|xl|2xl|3xl)/)
    expect(html).not.toContain('bg-paper-2')
    expect(html).not.toContain('border-rule')
  })
})
