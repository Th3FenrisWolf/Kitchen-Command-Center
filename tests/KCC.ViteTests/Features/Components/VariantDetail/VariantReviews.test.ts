// @vitest-environment happy-dom
import { renderSsr, tagWith, expectNoRetiredMarkup } from '../../../support/ssr'
import { washFields } from '../../../support/washes'
import { createApp, nextTick } from 'vue'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { BLOOM_KEY, createBloom } from '~/Bloom/bloomContext'
import { planSheet } from '~/Bloom/washes'
import VariantReviews from '~/Components/VariantDetail/VariantReviews.vue'
import type { Review, ReviewsResponse } from '~/Types/Recipe'
import { get, put } from '~/Utilities/Api'

vi.mock('~/Utilities/Api', () => ({ get: vi.fn(), put: vi.fn(), del: vi.fn() }))

// The reviews themselves arrive from `/api/variant/{guid}/reviews` in `onMounted`, which never runs on the
// server: every render below is the pre-fetch section, driven by the server-rendered average and count.
// The written reviews' slips are covered by VariantReviewsTests in the e2e suite, and Load more by PileTests.
const render = (over: Record<string, unknown> = {}) =>
  renderSsr(VariantReviews, { variantGuid: '11111111-2222-3333-4444-555555555555', ...over })

describe('VariantReviews section', () => {
  it('names the section on the rule and counts the reviews beside it', async () => {
    const html = await render({ reviewCount: 12 })

    expect(html).toMatch(/<div class="kcc-secname"><h2 class="kcc-section">\s*RatingsReviews\s*<\/h2>/)
    expect(html).toMatch(/<p class="kcc-kick"><span class="kcc-num">12<\/span>\s*<span>Reviews<\/span><\/p>/)
  })

  it('sets the blocks a rule and a half apart down the section', async () => {
    expect(await render({ reviewCount: 12 })).toContain('space-y-rule-1.5')
  })
})

describe('VariantReviews summary', () => {
  it('is one plain slip — no label, no tape — on a tear of its own', async () => {
    // Signed out, so the summary is the only sheet on the section and carries the whole assertion.
    const html = await render({ reviewCount: 12, averageRating: 4.5 })

    expect(tagWith(html, 'kcc-slip')).toContain('kcc-slip kcc-slip--wide kcc-tear-5')
    expect(html).not.toContain('kcc-label')
    expect(html).not.toContain('kcc-tape')
    expect(washFields(html).map((field) => field.length)).toEqual([3])
  })

  it('rests on one blank, with the wide turn', async () => {
    const html = await render({ reviewCount: 12, averageRating: 4.5 })

    expect((html.match(/kcc-blank/g) ?? []).length).toBe(1)
    expect(tagWith(html, 'kcc-tear-5')).toContain('kcc-slip--wide')
  })

  it('prints the average as stars and a Sono number, and leaves the count to the section heading', async () => {
    const html = await render({ reviewCount: 12, averageRating: 4.5 })

    expect(html).toContain('<span class="kcc-num">4.5</span>')
    expect(html).toContain('aria-label="4.5 of 5 stars"')
    expect(html).not.toContain('font-casual text-[3.5rem]')
    expect(html.match(/<span class="kcc-num">12<\/span>/g)).toHaveLength(1)
  })

  it('draws the distribution as five plain bars on the rule, 5 star down to 1', async () => {
    const html = await render({ reviewCount: 12, averageRating: 4.5 })
    const rows = [...html.matchAll(/aria-label="(\d) of 5 stars: \d+"/g)].map((match) => match[1])

    expect(rows).toEqual(['5', '4', '3', '2', '1'])
    // The star is the kit's glyph, not a text codepoint set in Sono.
    expect(html).not.toContain('★')
    expect(
      (html.match(/<span class="kcc-num">\d<\/span><i class="fa-solid fa-star" aria-hidden="true"><\/i>/g) ?? []).length,
    ).toBe(5)
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
  it('is a crisp labelled sheet, torn apart from the summary and from the slip above it', async () => {
    // No review has loaded on the server, so the form takes the tear that clears a first sibling card (1).
    const html = await render({ isAuthenticated: true })
    const form = tagWith(html, 'kcc-tear-2')

    expect(form).toContain('class="kcc-slip kcc-slip--wide kcc-slip--flip kcc-tear-2"')
    expect(form).toContain('--r:0')
    expect(html).toContain('<span class="kcc-label"><i class="fa-duotone fa-comment-pen" aria-hidden="true"></i>')
    expect(html).toMatch(/kcc-label[^>]*>.*?YourReview/s)
  })

  it('rests on one blank, with the wide turn', async () => {
    const html = await render({ isAuthenticated: true })

    expect((html.match(/kcc-blank/g) ?? []).length).toBe(1)
    expect(tagWith(html, 'kcc-tear-2')).toContain('kcc-slip--wide')
  })

  it('gives the summary and the form a blank each when both show', async () => {
    const html = await render({ isAuthenticated: true, reviewCount: 12, averageRating: 4.5 })

    expect((html.match(/kcc-blank/g) ?? []).length).toBe(2)
    expect(tagWith(html, 'kcc-tear-5')).toContain('kcc-slip--wide')
    expect(tagWith(html, 'kcc-tear-2')).toContain('kcc-slip--wide')
  })

  it('gives the stars two rules and sets the buttons in a kit row, so the form stays on the rule', async () => {
    const html = await render({ isAuthenticated: true })

    expect(html).toMatch(/<div class="flex h-rule-2 items-center">(<!--[\s\S]*?-->)*<div[^>]*role="slider"/)
    expect(html).toContain('<label class="kcc-field kcc-field--area relative w-full"')
    expect(tagWith(html, 'data-testid="review-input"')).toContain('class="h-full resize-none"')
    expect(html).toMatch(
      /<label class="kcc-field kcc-field--area[^"]*" style="height:calc\(var\(--bl\) \* 4\);">(<!--[\s\S]*?-->)*<textarea /,
    )
    expect(html).not.toContain('kcc-field--noicon')
    expect(html).toMatch(/<div class="kcc-row justify-end gap-x-7">(<!--[^>]*-->)*<button[^>]*data-testid="submit-review"/)
  })

  it('takes the rating on the slider and the text in the kit field, hooks on the controls', async () => {
    const html = await render({ isAuthenticated: true })

    expect(html).toContain('role="slider"')
    expect(html).toContain('<label class="kcc-field kcc-field--area relative w-full"')
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

    expectNoRetiredMarkup(html)
    expect(html).not.toContain('bg-paper-2')
    expect(html).not.toContain('border-rule')
  })
})

describe('VariantReviews blanks, before any review loads', () => {
  const slipTag = (html: string, tear: number) => tagWith(html, `kcc-tear-${tear}"`)

  it('leaves the summary its own - turn', async () => {
    const html = await render({ reviewCount: 3, averageRating: 4.5 })

    expect(slipTag(html, 5)).toContain('class="kcc-slip kcc-slip--wide kcc-tear-5"')
  })

  it('turns the form + under the summary when there are reviews', async () => {
    const html = await render({ isAuthenticated: true, reviewCount: 3, averageRating: 4.5 })

    expect(slipTag(html, 2)).toContain('class="kcc-slip kcc-slip--wide kcc-tear-2"')
  })

  it('turns the form over, to -, against cook notes when there are none', async () => {
    const html = await render({ isAuthenticated: true })

    expect(slipTag(html, 2)).toContain('class="kcc-slip kcc-slip--wide kcc-slip--flip kcc-tear-2"')
  })

  it('emits no trailing sign while the server renders', async () => {
    const onTrailingSign = vi.fn()

    await render({ isAuthenticated: true, reviewCount: 3, onTrailingSign })
    await render({ reviewCount: 3, onTrailingSign })

    expect(onTrailingSign).not.toHaveBeenCalled()
  })
})

describe('VariantReviews review sheets, once the reviews load', () => {
  const SEED = 2671381617
  const PAGE = '/recipes/brown-butter-gnocchi/classic'
  const ada: Review = { authorName: 'Ada', rating: 4, text: 'Lovely', created: '2026-10-01T10:00:00Z', isMine: false }
  const bea: Review = { authorName: 'Bea', rating: 4, text: 'Rich', created: '2026-10-02T10:00:00Z', isMine: false }
  const mine: Review = { authorName: 'Cy', rating: 5, text: 'Mine', created: '2026-10-03T10:00:00Z', isMine: true }

  const response = (reviews: Review[]): { success: true; data: ReviewsResponse; errorMessage: null } => ({
    success: true,
    data: {
      average: 4.3,
      count: reviews.length,
      distribution: [0, 0, 0, 2, 1],
      total: reviews.length,
      page: 0,
      pageSize: 10,
      reviews,
      myReview: { rating: 5, text: 'Mine' },
    },
    errorMessage: null,
  })

  const settle = async () => {
    for (let i = 0; i < 4; i++) {
      await new Promise((resolve) => setTimeout(resolve))
      await nextTick()
    }
  }

  const mount = async (first: Review[]) => {
    vi.mocked(get).mockResolvedValueOnce(response(first))
    const bloom = createBloom(SEED, PAGE)
    bloom.markHydrated()
    const host = document.createElement('div')
    const app = createApp(VariantReviews, { variantGuid: '11111111-2222-3333-4444-555555555555', isAuthenticated: true })
    app.provide(BLOOM_KEY, bloom)
    app.mount(host)
    await settle()
    return { host, unmount: () => app.unmount() }
  }

  const reviewSheets = (host: HTMLElement) => [...host.querySelectorAll('[data-testid="reviews-list"] > li')]

  const washesOf = (sheet: Element) =>
    [...sheet.querySelectorAll('.kcc-wash')].map((wash) => ({
      id: wash.getAttribute('data-wash'),
      style: wash.getAttribute('style'),
    }))

  const sheetBy = (host: HTMLElement, author: string) =>
    reviewSheets(host).find((sheet) => sheet.textContent?.includes(author)) as Element

  afterEach(() => vi.mocked(get).mockReset())

  it('gives each review sheet one or two washes, as a card', async () => {
    const { host, unmount } = await mount([ada, bea])

    expect(reviewSheets(host)).toHaveLength(2)
    for (const sheet of reviewSheets(host)) expect([1, 2]).toContain(sheet.querySelectorAll('.kcc-wash').length)
    unmount()
  })

  it('keys each review sheet by its author and date, so two reviews with the same rating differ', async () => {
    const { host, unmount } = await mount([ada, bea])
    const idsOf = (review: Review) =>
      planSheet({ seed: SEED, key: `${PAGE}|review:${review.authorName}|${review.created}`, size: 'card' }).map(
        (wash) => wash.id,
      )

    expect(washesOf(sheetBy(host, 'Ada')).map((wash) => wash.id)).toEqual(idsOf(ada))
    expect(washesOf(sheetBy(host, 'Bea')).map((wash) => wash.id)).toEqual(idsOf(bea))
    expect(idsOf(ada)).not.toEqual(idsOf(bea))
    unmount()
  })

  it('keeps every review’s washes when a reload puts a new review first', async () => {
    const { host, unmount } = await mount([ada, bea])
    const before = { ada: washesOf(sheetBy(host, 'Ada')), bea: washesOf(sheetBy(host, 'Bea')) }
    vi.mocked(put).mockResolvedValueOnce({ success: true, data: null, errorMessage: null })
    vi.mocked(get).mockResolvedValueOnce(response([mine, ada, bea]))

    ;(host.querySelector('[data-testid="submit-review"]') as HTMLButtonElement).click()
    await settle()

    expect(reviewSheets(host)).toHaveLength(3)
    expect(reviewSheets(host)[0].textContent).toContain('Cy')
    expect(washesOf(sheetBy(host, 'Ada'))).toEqual(before.ada)
    expect(washesOf(sheetBy(host, 'Bea'))).toEqual(before.bea)
    unmount()
  })
})
