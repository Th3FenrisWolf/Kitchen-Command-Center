import { renderToString } from 'vue/server-renderer'
import { describe, expect, it } from 'vitest'
import { useBloom } from '~/Bloom/bloomContext'
import { isGlobalKey, scheduleWashes as plannerSchedule } from '~/Bloom/washes'
import { createApp, scheduleWashes } from '~/Ssr/Server.Entry'

const payload = {
  headerContent: '',
  bodyContent: '',
  footerContent: '',
  isPreview: false,
  bloom: { seed: 2671381617, page: '/recipes/brown-butter-gnocchi' },
}

describe('the SSR entry', () => {
  it('provides a collecting bloom for the visit and the page', () => {
    const { app, bloom } = createApp(payload)

    expect(app.runWithContext(() => useBloom())).toBe(bloom)
    expect(bloom.seed).toBe(2671381617)
    expect(bloom.page).toBe('/recipes/brown-butter-gnocchi')

    bloom.collect({ id: '0a1b2c3d', key: '/recipes/brown-butter-gnocchi|hero', index: 0 })
    expect(bloom.collected()).toHaveLength(1)
  })

  it('exports the planner schedule from the same module as createApp', () => {
    expect(scheduleWashes).toBe(plannerSchedule)
  })

  it('wraps the islands Razor placed and returns them, the components rendered and the page washes', async () => {
    const { app, bloom, output } = createApp({
      ...payload,
      bodyContent: '<KccWashes wash-key="body" size="reading" /><Stacker :cards="[]" />',
      footerContent: '<AppFooter />',
    })

    const html = await renderToString(app)
    const { islands, components, pageWashes } = output()

    expect(html.match(/<kcc-island\b/g)).toHaveLength(1)
    expect(html).toContain('<kcc-island data-island="0">')
    expect(islands).toEqual([{ name: 'Stacker', props: { cards: [] }, keys: [] }])
    expect(components).toEqual([
      'Features/Components/Sheet/KccWashes.Component.vue',
      'Features/Widgets/Stacker/Stacker.Component.vue',
      'Features/Components/Footer/AppFooter.Component.vue',
    ])
    expect(pageWashes).toBe(bloom.collected().filter((wash) => !isGlobalKey(wash.key)).length)
    expect(pageWashes).toBeGreaterThan(0)
  })
})
