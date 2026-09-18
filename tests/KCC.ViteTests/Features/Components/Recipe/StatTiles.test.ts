import { describe, expect, it } from 'vitest'
import StatTiles from '~/Components/Recipe/StatTiles.vue'
import type { StatTileSpec } from '~/Components/Recipe/StatTiles.vue'
import { renderSsr } from '../../../support/renderSsr'

const tiles: StatTileSpec[] = [
  { icon: 'fa-duotone fa-clock', value: 25, unit: 'min', label: 'Prep' },
  { icon: 'fa-duotone fa-users', value: 4, label: 'Servings' },
  { dotColor: 'green', value: 'Easy', label: 'Difficulty' },
]

describe('StatTiles', () => {
  it('renders the kcc-stats row on the stat-row hook', async () => {
    const html = await renderSsr(StatTiles, { tiles })
    expect(html).toContain('data-testid="variant-stats"')
    expect(html).toContain('class="kcc-stats"')
  })

  it('renders one child per stat, each with a kcc-lbl label and a kcc-v value', async () => {
    const html = await renderSsr(StatTiles, { tiles })
    expect((html.match(/class="kcc-lbl"/g) ?? []).length).toBe(tiles.length)
    expect((html.match(/class="kcc-v"/g) ?? []).length).toBe(tiles.length)
    expect(html).toContain('Prep')
    expect(html).toContain('Servings')
    expect(html).toContain('Difficulty')
  })

  it('puts the unit inside a small element beside the value', async () => {
    const html = await renderSsr(StatTiles, { tiles })
    expect(html).toContain('<small>min</small>')
  })

  it('keeps the difficulty-dot hook on a dot-coloured tile', async () => {
    const html = await renderSsr(StatTiles, { tiles })
    expect(html).toContain('data-testid="difficulty-dot"')
  })

  it('renders an em dash for a null value', async () => {
    const html = await renderSsr(StatTiles, { tiles: [{ label: 'Contributors', value: null }] })
    expect(html).toContain('—')
  })

  it('renders a coming-soon value as a kcc-badge', async () => {
    const html = await renderSsr(StatTiles, { tiles: [{ label: 'Cooked', value: 'Soon', comingSoon: true }] })
    expect(html).toContain('kcc-badge')
  })

  it('carries no Softbound hook', async () => {
    const html = await renderSsr(StatTiles, { tiles })
    expect(html).not.toContain('sk-')
  })
})
