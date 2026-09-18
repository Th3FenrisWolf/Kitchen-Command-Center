import { describe, expect, it } from 'vitest'
import '~/Utilities/StringExtensions'
import Breadcrumbs from '~/Components/Breadcrumbs/Breadcrumbs.Component.vue'
import type { Breadcrumb } from '~/Types/Recipe'
import { renderSsr } from '../../../support/renderSsr'

const items: Breadcrumb[] = [
  { linkText: 'Home', url: '/' },
  { linkText: 'Recipes', url: '/recipes' },
  { linkText: 'Brown Butter Gnocchi', url: '' },
]

describe('Breadcrumbs', () => {
  it('renders the nav/ol trail with a home icon and every ancestor as a kcc-kick link', async () => {
    const html = await renderSsr(Breadcrumbs, { items })
    expect(html).toContain('<nav')
    expect(html).toContain('aria-label="Breadcrumb"')
    expect(html).toContain('<ol')
    expect(html).toContain('fa-duotone fa-house')

    const recipesTag = html.match(/<a[^>]*href="\/recipes"[^>]*>/)?.[0] ?? ''
    expect(recipesTag).toContain('kcc-kick')
    expect(recipesTag).toContain('kcc-link')
  })

  it('marks the last item as the current page, in kcc-kick text-ink, unlinked', async () => {
    const html = await renderSsr(Breadcrumbs, { items })
    expect(html).toContain('aria-current="page"')

    const currentTag = html.match(/<li[^>]*aria-current="page"[^>]*>/)?.[0] ?? ''
    expect(currentTag).toContain('kcc-kick')
    expect(currentTag).toContain('text-ink')
    expect(html).not.toContain('<a href="">Brown Butter Gnocchi</a>')
  })

  it('separates items with a middot kept off the accessibility tree', async () => {
    const html = await renderSsr(Breadcrumbs, { items })
    expect(html).toContain('·')
    expect((html.match(/aria-hidden="true">·/g) ?? []).length).toBe(items.length - 1)
  })

  it('carries no retired weight or Softbound hook', async () => {
    const html = await renderSsr(Breadcrumbs, { items })
    expect(html).not.toContain('font-bold')
    expect(html).not.toContain('font-medium')
    expect(html).not.toContain('sk-')
  })
})
