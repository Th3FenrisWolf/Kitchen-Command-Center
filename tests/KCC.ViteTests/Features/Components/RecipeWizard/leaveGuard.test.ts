import { describe, expect, it } from 'vitest'
import { controlOfInertAncestor, guardedLink } from '~/Components/RecipeWizard/leaveGuard'

const PAGE = 'https://kitchen.test/recipes/create-recipe/'

function anchor(href: string, attributes: Record<string, string> = {}) {
  const url = new URL(href, PAGE)
  const all: Record<string, string> = { href, ...attributes }
  return {
    href: url.href,
    origin: url.origin,
    protocol: url.protocol,
    host: url.host,
    hostname: url.hostname,
    port: url.port,
    pathname: url.pathname,
    search: url.search,
    hash: url.hash,
    target: all.target ?? '',
    download: all.download ?? '',
    getAttribute: (name: string) => all[name] ?? null,
    hasAttribute: (name: string) => name in all,
  }
}

type Anchor = ReturnType<typeof anchor>

interface Click {
  button: number
  metaKey: boolean
  ctrlKey: boolean
  shiftKey: boolean
  altKey: boolean
  defaultPrevented: boolean
}

const click = (link: Anchor | null, over: Partial<Click> = {}) =>
  ({
    button: 0,
    metaKey: false,
    ctrlKey: false,
    shiftKey: false,
    altKey: false,
    defaultPrevented: false,
    target: { closest: () => link },
    ...over,
  }) as unknown as MouseEvent

describe('leaveGuard', () => {
  it.each([
    ['the header logo', '/'],
    ["the hero's Cancel", '/recipes/brown-butter-gnocchi/'],
    ['an absolute link on this origin', 'https://kitchen.test/account/'],
    ['a relative link', '../'],
    ['a link to this page, which reloads it', PAGE],
  ])('asks first before a plain primary click on a same-origin link: %s', (_link, href) => {
    const link = anchor(href)

    expect(guardedLink(click(link), PAGE)).toBe(link)
  })

  it.each(['metaKey', 'ctrlKey', 'shiftKey', 'altKey'] as const)('lets a click with %s pass', (key) => {
    expect(guardedLink(click(anchor('/'), { [key]: true }), PAGE)).toBeNull()
  })

  it.each([1, 2])('lets a click with mouse button %i pass', (button) => {
    expect(guardedLink(click(anchor('/'), { button }), PAGE)).toBeNull()
  })

  it('lets a click on a link with target="_blank" pass', () => {
    expect(guardedLink(click(anchor('/recipes/', { target: '_blank' })), PAGE)).toBeNull()
  })

  it.each(['', 'recipe.json'])('lets a click on a link with download=%j pass', (download) => {
    expect(guardedLink(click(anchor('/recipes/', { download })), PAGE)).toBeNull()
  })

  it.each(['https://example.com/recipes/', 'http://kitchen.test/recipes/', 'https://kitchen.test:8443/recipes/'])(
    'lets a click on a link to another origin pass: %s',
    (href) => {
      expect(guardedLink(click(anchor(href)), PAGE)).toBeNull()
    },
  )

  it.each(['#ingredients', `${PAGE}#ingredients`, '#'])(
    'lets a click on a link to a fragment of this page pass: %s',
    (href) => {
      expect(guardedLink(click(anchor(href)), PAGE)).toBeNull()
    },
  )

  it('lets a click that another handler cancelled pass', () => {
    expect(guardedLink(click(anchor('/'), { defaultPrevented: true }), PAGE)).toBeNull()
  })

  it('lets a click outside any link pass', () => {
    expect(guardedLink(click(null), PAGE)).toBeNull()
  })
})

interface Fake {
  id: string
  getAttribute(name: string): string | null
  hasAttribute(name: string): boolean
}

const fake = (attributes: Record<string, string>): Fake => ({
  id: attributes.id ?? '',
  getAttribute: (name) => attributes[name] ?? null,
  hasAttribute: (name) => name in attributes,
})

const attributeOf = (selector: string) => selector.match(/^\[([\w-]+)\]$/)?.[1] ?? `unsupported selector ${selector}`

function linkWithin(ancestors: Fake[], page: Fake[]) {
  return {
    closest: (selector: string) => ancestors.find((ancestor) => ancestor.hasAttribute(attributeOf(selector))) ?? null,
    ownerDocument: {
      querySelectorAll: (selector: string) => page.filter((element) => element.hasAttribute(attributeOf(selector))),
    },
  } as unknown as Element
}

describe('leaveGuard focus after Stay', () => {
  const RECIPES_MENU = fake({ id: 'pad-menu-recipes', 'aria-controls': 'pad-card-recipes' })
  const SEARCH_FIELD = fake({ id: 'pad-search-input', 'aria-controls': 'pad-card-search' })
  const LOGO = fake({ href: '/' })
  const HEADER = [LOGO, RECIPES_MENU, SEARCH_FIELD]

  const closed = (id: string) => fake({ id, inert: '' })

  it('returns the menu button of the closed header card that holds the link', () => {
    expect(controlOfInertAncestor(linkWithin([closed('pad-card-recipes')], HEADER))).toBe(RECIPES_MENU)
  })

  it('returns the search field for a link in the closed search card', () => {
    expect(controlOfInertAncestor(linkWithin([closed('pad-card-search')], HEADER))).toBe(SEARCH_FIELD)
  })

  it('returns the control of the closest closed region when closed regions nest', () => {
    const outer = closed('pad-card-search')

    expect(controlOfInertAncestor(linkWithin([closed('pad-card-recipes'), outer], HEADER))).toBe(RECIPES_MENU)
  })

  it('finds a control that names the card among other ids', () => {
    const both = fake({ 'aria-controls': 'pad-card-kitchen  pad-card-recipes' })

    expect(controlOfInertAncestor(linkWithin([closed('pad-card-recipes')], [LOGO, SEARCH_FIELD, both]))).toBe(both)
  })

  it('returns nothing for a link in no closed region', () => {
    expect(controlOfInertAncestor(linkWithin([fake({ id: 'pad-card-recipes' })], HEADER))).toBeNull()
  })

  it('returns nothing for a closed region that nothing controls', () => {
    expect(controlOfInertAncestor(linkWithin([closed('pad-card-kitchen')], HEADER))).toBeNull()
    expect(controlOfInertAncestor(linkWithin([fake({ inert: '' })], HEADER))).toBeNull()
  })
})
