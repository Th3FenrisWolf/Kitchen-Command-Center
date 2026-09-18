import { createSSRApp } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { describe, expect, it } from 'vitest'
import RangeSlider from '~/Components/Forms/RangeSlider.vue'

const render = (props: Record<string, unknown>) =>
  renderToString(createSSRApp(RangeSlider, { min: 0, max: 60, step: 5, modelMin: 10, modelMax: 30, ...props }))

describe('RangeSlider', () => {
  it('renders two range inputs', async () => {
    const html = await render({})
    expect((html.match(/type="range"/g) ?? []).length).toBe(2)
  })

  it('draws the track and the fill with the kit classes', async () => {
    const html = await render({})
    expect(html).toContain('kcc-range-track')
    expect(html).toContain('kcc-range-fill')
  })

  it('positions the fill from min% to max%', async () => {
    const html = await render({ modelMin: 15, modelMax: 30 })
    expect(html).toContain('left: 25%')
    expect(html).toContain('right: 50%')
  })
})
