import { describe, expect, it } from 'vitest'
import SegmentedControl from '~/Components/Recipe/SegmentedControl.vue'
import { renderSsr, expectNoRetiredMarkup, openTag } from '../../../support/ssr'

const textOptions = [
  { value: 'grid', label: 'Grid', testId: 'view-grid' },
  { value: 'list', label: 'List', testId: 'view-list' },
]

const iconOptions = [
  { value: 'grid', icon: 'fa-solid fa-table-cells-large', ariaLabel: 'Grid', testId: 'view-grid' },
  { value: 'list', icon: 'fa-solid fa-list', ariaLabel: 'List', testId: 'view-list' },
]

describe('SegmentedControl', () => {
  it('renders a kcc-seg radiogroup with one radio button per option', async () => {
    const html = await renderSsr(SegmentedControl, { options: textOptions, modelValue: 'grid', ariaLabel: 'View' })
    expect(html).toContain('role="radiogroup"')
    expect(html).toContain('class="kcc-seg"')
    expect((html.match(/role="radio"/g) ?? []).length).toBe(2)
  })

  it('checks only the option matching the model value', async () => {
    const html = await renderSsr(SegmentedControl, { options: textOptions, modelValue: 'list' })
    const gridButton = html.match(/<button[^>]*data-testid="view-grid"[^>]*>/)?.[0] ?? ''
    const listButton = html.match(/<button[^>]*data-testid="view-list"[^>]*>/)?.[0] ?? ''
    expect(gridButton).toContain('aria-checked="false"')
    expect(listButton).toContain('aria-checked="true"')
  })

  it('sizes icon-variant segments as w-9 px-0 squares', async () => {
    const html = await renderSsr(SegmentedControl, { options: iconOptions, modelValue: 'grid', variant: 'icon' })
    const gridButton = html.match(/<button[^>]*data-testid="view-grid"[^>]*>/)?.[0] ?? ''
    expect(gridButton).toContain('w-9')
    expect(gridButton).toContain('px-0')
  })

  it('renders an aria-hidden thumb first in the track', async () => {
    const html = await renderSsr(SegmentedControl, { options: textOptions, modelValue: 'grid' })
    const track = openTag(html, 'role="radiogroup"')
    const thumb = openTag(html, 'kcc-seg-thumb')
    expect(thumb).toContain('aria-hidden="true"')
    expect(html.indexOf(thumb)).toBe(html.indexOf(track) + track.length)
    expectNoRetiredMarkup(html)
  })

  it('leaves the fill on the checked button until the client places the thumb', async () => {
    const html = await renderSsr(SegmentedControl, { options: textOptions, modelValue: 'grid' })
    expect(openTag(html, 'role="radiogroup"')).toContain('class="kcc-seg"')
    expect(openTag(html, 'kcc-seg-thumb')).not.toContain('kcc-seg-thumb--glide')
  })
})
