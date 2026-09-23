import { describe, expect, it } from 'vitest'
import StatTiles from '~/Components/Recipe/StatTiles.vue'
import type { StatTileSpec } from '~/Components/Recipe/StatTiles.vue'
import { renderSsr } from '../../../support/renderSsr'

const tiles: StatTileSpec[] = [
  { icon: 'fa-duotone fa-clock', value: 25, unit: 'min', label: 'Prep' },
  { icon: 'fa-duotone fa-users', value: 4, label: 'Servings' },
  { dotColor: 'green', value: 'Easy', label: 'Difficulty' },
]

const valueTag = (html: string) => html.match(/<p[^>]*data-testid="difficulty-dot"[^>]*>/)?.[0] ?? ''

describe('StatTiles', () => {
  it('renders the kcc-stats row on the stat-row hook', async () => {
    const html = await renderSsr(StatTiles, { tiles })
    expect(html).toContain('data-testid="variant-stats"')
    expect(html).toContain('class="kcc-stats"')
  })

  it('renders one child per stat, each with a kcc-lbl label and a kcc-v value', async () => {
    const html = await renderSsr(StatTiles, { tiles })
    expect((html.match(/class="kcc-lbl"/g) ?? []).length).toBe(tiles.length)
    expect((html.match(/class="[^"]*\bkcc-v\b[^"]*"/g) ?? []).length).toBe(tiles.length)
    expect(html).toContain('Prep')
    expect(html).toContain('Servings')
    expect(html).toContain('Difficulty')
  })

  it('puts the unit inside a small element beside the value', async () => {
    const html = await renderSsr(StatTiles, { tiles })
    expect(html).toContain('<small>min</small>')
  })

  it('prints a set difficulty as a status well in ink, on the difficulty-dot hook', async () => {
    const html = await renderSsr(StatTiles, { tiles })
    const value = valueTag(html)
    expect(value).toContain('kcc-v')
    expect(value).toContain('kcc-well')
    expect(value).toContain('kcc-well--success')
    expect(html).toContain('Easy')
    expect(html).not.toContain('fa-circle')
    expect(html).not.toMatch(/text-(?:green|yellow|red|success|warning|danger)/)
  })

  it('tints the well with the level: green success, yellow warning, red danger', async () => {
    const wellOf = async (dotColor: string) =>
      valueTag(await renderSsr(StatTiles, { tiles: [{ dotColor, value: dotColor, label: 'Difficulty' }] }))

    expect(await wellOf('green')).toContain('kcc-well--success')
    expect(await wellOf('yellow')).toContain('kcc-well--warning')
    expect(await wellOf('red')).toContain('kcc-well--danger')
  })

  it('leaves a plain stat unwelled, glyph and all', async () => {
    const html = await renderSsr(StatTiles, { tiles: [tiles[0]!] })
    expect(html).toContain('<p class="kcc-v">')
    expect(html).not.toContain('kcc-well')
    expect(html).toContain('fa-duotone fa-clock')
    expect(html).not.toContain('difficulty-dot')
  })

  it('renders an em dash for a null value', async () => {
    const html = await renderSsr(StatTiles, { tiles: [{ label: 'Contributors', value: null }] })
    expect(html).toContain('—')
  })

  it('carries no Softbound hook', async () => {
    const html = await renderSsr(StatTiles, { tiles })
    expect(html).not.toContain('sk-')
  })
})
