import { createSSRApp } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { describe, expect, it } from 'vitest'
import RatingSummary from '~/Components/StarRating/RatingSummary.vue'

// StarRating tags each star with data-star, so counting those attributes = stars rendered.
const countStars = (html: string) => (html.match(/data-star=/g) ?? []).length

const render = (props: Record<string, unknown>) => renderToString(createSSRApp(RatingSummary, props))

describe('RatingSummary', () => {
  it('renders one star per max (default 5) plus the one-decimal average', async () => {
    const html = await render({ value: 4.5 })
    expect(countStars(html)).toBe(5)
    expect(html).toContain('4.5')
  })

  it('passes max through to the star row', async () => {
    expect(countStars(await render({ value: 3, max: 10 }))).toBe(10)
  })

  it('emphasizes the number only when strong is set', async () => {
    expect(await render({ value: 4, strong: true })).toContain('font-bold')
    expect(await render({ value: 4 })).not.toContain('font-bold')
  })
})
