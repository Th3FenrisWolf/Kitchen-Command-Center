import { createSSRApp } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { describe, expect, it } from 'vitest'
import StarRating from '~/Components/StarRating/StarRating.vue'

const render = (props: Record<string, unknown>) => renderToString(createSSRApp(StarRating, props))

describe('StarRating', () => {
  it('interactive mode is a half-step slider with two hit areas per star', async () => {
    const html = await render({ modelValue: 3.5, max: 5 })
    expect(html).toContain('role="slider"')
    expect(html).toContain('aria-valuemin="0.5"')
    expect(html).toContain('aria-valuenow="3.5"')
    expect(html).toContain('aria-valuemax="5"')
    // two hit areas per star = 10 data-value targets
    expect((html.match(/data-value=/g) ?? []).length).toBe(10)
    expect(html).toContain('data-value="3.5"')
    expect(html).toContain('data-value="4"')
    // a 3.5 rating shows exactly one half star
    expect((html.match(/data-state="half"/g) ?? []).length).toBe(1)
  })

  it('readonly mode is a static image with no hit areas', async () => {
    const html = await render({ modelValue: 3.5, readonly: true })
    expect(html).toContain('role="img"')
    expect(html).not.toContain('data-value=')
    expect((html.match(/data-state="half"/g) ?? []).length).toBe(1)
  })

  it('readonly mode announces the real half-star value in aria-label', async () => {
    const html = await render({ modelValue: 3.5, readonly: true })
    expect(html).toContain('aria-label="3.5 of 5 stars"')
    expect(html).not.toContain('aria-label="4 of 5 stars"')
  })
})
