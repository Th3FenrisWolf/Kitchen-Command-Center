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
  it('renders the nav/ol trail, as a list, with a home icon and every ancestor as a kcc-kick link', async () => {
    const html = await renderSsr(Breadcrumbs, { items })
    expect(html).toContain('<nav')
    expect(html).toContain('aria-label="Breadcrumb"')
    expect(html).toContain('<ol')
    expect(html).toContain('role="list"')
    expect(html).toContain('fa-duotone fa-house')

    const recipesTag = html.match(/<a[^>]*href="\/recipes"[^>]*>/)?.[0] ?? ''
    expect(recipesTag).toContain('kcc-kick')
    expect(recipesTag).toContain('kcc-link')
  })

  it('sizes the home crumb from its li and drops the underline from its icon-only link', async () => {
    const html = await renderSsr(Breadcrumbs, { items })

    const homeLi = html.match(/<li[^>]*>\s*<a[^>]*href="\/"[\s\S]*?<\/li>/)?.[0] ?? ''
    expect(homeLi).toContain('kcc-kick')

    const homeAnchor = html.match(/<a[^>]*href="\/"[^>]*>/)?.[0] ?? ''
    expect(homeAnchor).toContain('kcc-link')
    expect(homeAnchor).toContain('kcc-link--icon')
  })

  it('marks exactly one item as the current page, in kcc-kick text-ink, unlinked', async () => {
    const html = await renderSsr(Breadcrumbs, { items })
    expect((html.match(/aria-current="page"/g) ?? []).length).toBe(1)

    const currentLi = html.match(/<li[^>]*aria-current="page"[^>]*>[\s\S]*?<\/li>/)?.[0] ?? ''
    expect(currentLi).toContain('kcc-kick')
    expect(currentLi).toContain('text-ink')
    expect(currentLi).not.toContain('<a')
  })

  it('links exactly the home icon and the one linked ancestor, for the three-item fixture', async () => {
    const html = await renderSsr(Breadcrumbs, { items })
    expect((html.match(/<a /g) ?? []).length).toBe(2)
  })

  it('separates items with a middot kept off the accessibility tree', async () => {
    const html = await renderSsr(Breadcrumbs, { items })
    expect(html).toContain('·')
    expect((html.match(/aria-hidden="true">·/g) ?? []).length).toBe(items.length - 1)
  })

  it('treats an ancestor with an empty url as a plain, unlinked crumb — not the current page', async () => {
    // Only the true current page is unlinked in production (BreadcrumbService.cs sets Url to
    // string.Empty for it alone), but the component has no way to know that: it must treat any
    // empty-url ancestor as unlinked without mistaking it for the current page.
    const fourItems: Breadcrumb[] = [
      { linkText: 'Home', url: '/' },
      { linkText: 'Recipes', url: '' },
      { linkText: 'Pasta', url: '/recipes/pasta' },
      { linkText: 'Brown Butter Gnocchi', url: '' },
    ]
    const html = await renderSsr(Breadcrumbs, { items: fourItems })

    expect((html.match(/aria-current="page"/g) ?? []).length).toBe(1)

    const recipesLi = html.match(/<li[^>]*>Recipes<\/li>/)?.[0] ?? ''
    expect(recipesLi).toBe('<li class="kcc-kick">Recipes</li>')
    expect(recipesLi).not.toContain('<a')
    expect(recipesLi).not.toContain('aria-current')

    const currentLi = html.match(/<li[^>]*aria-current="page"[^>]*>[\s\S]*?<\/li>/)?.[0] ?? ''
    expect(currentLi).toContain('Brown Butter Gnocchi')
  })

  it('carries no retired weight or Softbound hook', async () => {
    const html = await renderSsr(Breadcrumbs, { items })
    expect(html).not.toContain('font-bold')
    expect(html).not.toContain('font-medium')
    expect(html).not.toContain('sk-')
  })
})
