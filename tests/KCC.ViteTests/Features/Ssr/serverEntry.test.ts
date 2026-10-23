import { describe, expect, it } from 'vitest'
import { useBloom } from '~/Bloom/bloomContext'
import { scheduleWashes as plannerSchedule } from '~/Bloom/washes'
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
})
