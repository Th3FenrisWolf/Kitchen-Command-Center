import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import postcss, { type AtRule, type Rule } from 'postcss'
import { describe, expect, it } from 'vitest'
import AppHeader from '~/Components/Header/AppHeader.Component.vue'
import type { NavModel } from '~/Types/Nav'
import { member, visitor } from '../../../support/nav'
import { expectNoRetiredMarkup, openTag, renderSsr, tagWith } from '../../../support/ssr'

const render = (nav: NavModel) => renderSsr(AppHeader, { logoAlt: 'Kitchen Command Center', nav })

const between = (html: string, from: string, to: string) =>
  html.slice(html.indexOf(from), html.indexOf(to, html.indexOf(from)))

const styleRules = () => {
  const source = readFileSync(
    fileURLToPath(new URL('../../../../../src/KCC.Web/Features/Components/Header/AppHeader.Component.vue', import.meta.url)),
    'utf8',
  )
  const rules: Rule[] = []
  postcss
    .parse(source.slice(source.indexOf('<style>') + '<style>'.length, source.lastIndexOf('</style>')))
    .walkRules((rule) => {
      rules.push(rule)
    })
  return rules
}

const declarationsOf = (rules: Rule[], selector: string) => {
  const found: Record<string, string> = {}
  for (const rule of rules.filter((candidate) => candidate.selectors.includes(selector))) {
    rule.walkDecls((decl) => {
      found[decl.prop] = decl.value.replace(/\s+/g, ' ')
    })
  }
  return found
}

describe('AppHeader', () => {
  it('gives a visitor Recipes, search and Sign in back to this page, and nothing of a member', async () => {
    const html = await render(visitor())
    const desk = between(html, 'id="pad-bar-desk"', 'id="pad-bar-phone"')

    expect(tagWith(desk, 'id="pad-menu-recipes"')).toContain('aria-controls="pad-card-recipes"')
    expect(desk).toContain('<form class="pad-search" role="search" action="/recipes/" method="get">')
    expect(desk).toMatch(
      /<a class="kcc-btn kcc-btn--ghost" href="\/account\/login\/\?returnUrl=%2Frecipes%2F%3Fcategory%3DDinner"><i class="fa-duotone fa-right-to-bracket" aria-hidden="true"><\/i>Sign in<\/a>/,
    )
    expect(html).not.toContain('pad-menu-kitchen')
    expect(html).not.toContain('pad-card-kitchen')
    expect(html).not.toContain('New recipe')
    expect(html).not.toContain('/account/logout')
  })

  it('gives a member My kitchen with what waits for review, and New recipe instead of Sign in', async () => {
    const html = await render(member())
    const kitchen = between(html, 'id="pad-menu-kitchen"', '</button>')

    expect(kitchen).toContain('<span class="pad-count kcc-num">1<span class="sr-only"> Waiting for review</span></span>')
    expect(between(html, 'id="pad-bar-desk"', 'id="pad-bar-phone"')).toContain(
      '<a class="kcc-btn max-[820px]:w-(--control-md) max-[820px]:px-0" href="/recipes/create-recipe/" title="New recipe"><i class="fa-duotone fa-plus" aria-hidden="true"></i><span class="max-[820px]:sr-only">New recipe</span></a>',
    )
    expect(html).not.toContain('Sign in')
    expect(html).not.toContain('Ask for an account')
  })

  it('shows no count when nothing waits, or when the kitchen could not be counted', async () => {
    const quiet = member()
    quiet.member!.kitchen = { recipes: 3, variants: 5, waiting: 0 }
    const uncounted = member()
    delete uncounted.member!.kitchen

    expect(await render(quiet)).not.toContain('pad-count')
    const html = await render(uncounted)
    expect(html).not.toContain('pad-count')
    expect(html).not.toContain('kcc-stats')
  })

  it('renders no ramp toggle: only the logo comes in a version per ramp', async () => {
    const html = await render(member())

    expect(html).not.toContain('Switch to')
    expect(html.match(/data-ramp="light"/g)).toHaveLength(2)
    expect(html.match(/data-ramp="dark"/g)).toHaveLength(2)
    expect([...html.matchAll(/<[a-z]+[^>]*data-ramp=/g)].every(([tag]) => tag.startsWith('<img'))).toBe(true)
  })

  it('puts the same alt text on the light and the dark logo', async () => {
    const html = await render(visitor())

    expect(html).toMatch(/<img data-ramp="light"[^>]*alt="Kitchen Command Center"/)
    expect(html).toMatch(/<img data-ramp="dark"[^>]*alt="Kitchen Command Center"/)
    expect(html).toContain('<a class="pad-logo" href="/">')
  })

  it('marks the section you are in on its menu, and nothing on other pages', async () => {
    const atHome = visitor()
    delete atHome.currentSection

    expect(tagWith(await render(visitor()), 'id="pad-menu-recipes"')).toContain('aria-current="true"')
    const kitchen = await render(member())
    expect(tagWith(kitchen, 'id="pad-menu-kitchen"')).toContain('aria-current="true"')
    expect(tagWith(kitchen, 'id="pad-menu-recipes"')).not.toContain('aria-current')
    expect(await render(atHome)).not.toContain('aria-current="true"')
  })

  it('marks the card link for the page you are on', async () => {
    const html = await render(visitor())

    expect(
      openTag(between(html, 'id="pad-card-recipes"', 'id="pad-card-search"'), 'href="/recipes/?category=Dinner"'),
    ).toContain('aria-current="page"')
    expect(openTag(html, 'href="/recipes/?category=Breakfast"')).not.toContain('aria-current')
  })

  it('renders every card closed, out of reach, and labelled by what opens it', async () => {
    const html = await render(member())
    const cards = [...html.matchAll(/<div[^>]*id="pad-card-[a-z]+"[^>]*>/g)].map(([tag]) => tag)

    expect(cards).toHaveLength(5)
    expect(cards.every((card) => card.includes('inert') && card.includes('is-rest') && card.includes('role="region"'))).toBe(
      true,
    )
    expect(tagWith(html, 'id="pad-card-recipes"')).toContain('aria-labelledby="pad-menu-recipes"')
    expect(tagWith(html, 'id="pad-card-kitchen"')).toContain('aria-labelledby="pad-menu-kitchen"')
    expect(tagWith(html, 'id="pad-card-search"')).toContain('aria-label="Search 25 recipes"')
    expect(tagWith(html, 'id="pad-card-find"')).toContain('aria-labelledby="pad-menu-find"')
    expect(tagWith(html, 'id="pad-card-menu"')).toContain('aria-labelledby="pad-menu-menu"')
    expect([...html.matchAll(/aria-expanded="([a-z]+)"/g)].map(([, expanded]) => expanded)).toEqual([
      'false',
      'false',
      'false',
      'false',
    ])
  })

  it('tears every sheet in a stack differently, on a desktop and on a phone', async () => {
    const html = await render(member())
    const sheets = [...html.matchAll(/<div[^>]*\bkcc-slip\b[^>]*>/g)].map(([tag]) => tag)
    const stack = (cards: string) =>
      sheets
        .filter((sheet) => !sheet.includes('id="pad-card-') || sheet.includes(cards))
        .map((sheet) => sheet.match(/\bkcc-tear-[\w-]+/)?.[0])

    for (const [cards, count] of [
      ['pad-card--desk', 5],
      ['pad-card--phone', 4],
    ] as const) {
      const tears = stack(cards)
      expect(tears).toHaveLength(count)
      expect(new Set(tears).size, tears.join(' ')).toBe(count)
    }
  })

  it('lays one centred page under the top sheet, with no blank in either', async () => {
    const html = await render(member())
    const pages = [...html.matchAll(/<div[^>]*\bpad-page\b[^>]*>/g)].map(([tag]) => tag)

    expect(pages).toEqual([
      '<div class="pad-page kcc-slip kcc-slip--fill kcc-slip--wide kcc-tear-hero-2" aria-hidden="true">',
    ])
    expect(between(html, 'class="pad-top', 'id="pad-cards"')).not.toContain('kcc-blank')
  })

  it('stacks the top sheet over the cards and the cards over the page, which turns about its centre', () => {
    const rules = styleRules()

    expect(declarationsOf(rules, '.pad-top')['z-index']).toBe('5')
    expect(declarationsOf(rules, '.pad-cards')['z-index']).toBe('4')
    const page = declarationsOf(rules, '.pad-page')
    expect(page['z-index']).toBe('3')
    expect(page.transform).toBe('rotate(calc(-0.12deg + var(--blank-turn))) scale(var(--blank-scale))')
    expect(page.translate).toBeUndefined()
    expect(rules.some((rule) => /\.pad-page--\d/.test(rule.selector))).toBe(false)
  })

  it('sets the menu items in Hazelnut at the body size, in mixed case', async () => {
    const html = await render(member())
    const menus = [...html.matchAll(/<button[^>]*class="pad-menu[" ][^>]*>/g)].map(([tag]) => tag)

    expect(menus).toHaveLength(3)
    expect(menus.some((tag) => tag.includes('kcc-kick'))).toBe(false)
    const menu = declarationsOf(styleRules(), '.pad-menu')
    expect(menu['font-family']).toBe('var(--font-hazelnut)')
    expect(menu['font-size']).toBe('var(--text-body)')
    expect(menu['min-height']).toBe('var(--control-md)')
    expect(menu['text-transform']).toBeUndefined()
    expect(menu['letter-spacing']).toBeUndefined()
  })

  it('lets the chevron inherit the item size, and sets the count and the glyph on the scale', () => {
    const rules = styleRules()

    expect(declarationsOf(rules, '.pad-menu .fa-chevron-down')['font-size']).toBeUndefined()
    const count = declarationsOf(rules, '.pad-count')
    expect(count['font-size']).toBe('var(--text-caps)')
    expect(count['line-height']).toBe('var(--pill-sm)')
    expect(count.height).toBe('var(--pill-sm)')
    expect(count['min-width']).toBe('var(--pill-sm)')
    expect(declarationsOf(rules, '.pad-glyph')['font-size']).toBe('var(--icon-sm)')
  })

  it('sizes the glyph buttons as controls', () => {
    const glyph = declarationsOf(styleRules(), '.pad-glyph')

    expect(glyph.height).toBe('var(--control-md)')
    expect(glyph.width).toBe('var(--control-md)')
  })

  it('starts the phone search results on a rule, under a field row two rules tall', async () => {
    const card = between(await render(visitor()), 'id="pad-card-find"', 'id="pad-card-menu"')

    expect(card).toMatch(/<div class="min-h-rule-2" data-row[^>]*><form class="pad-search"/)
    expect(card).not.toContain('mt-rule-0.5')
  })

  it('keeps the search field wide enough for a four-digit count in its placeholder, leaving New recipe its label from 820px', () => {
    const rules = styleRules()
    const desk = rules.filter((rule) => rule.parent?.type === 'root')
    const tablet = rules.filter(
      (rule) => rule.parent?.type === 'atrule' && (rule.parent as AtRule).params.includes('max-width: 1023.98px'),
    )

    expect(declarationsOf(desk, '.pad-bar--desk .pad-search-field').width).toBe('250px')
    expect(declarationsOf(desk, '.pad-bar--desk .pad-search-field:focus-within').width).toBe('330px')
    expect(declarationsOf(tablet, '.pad-bar--desk .pad-search-field').width).toBe('200px')
    expect(declarationsOf(tablet, '.pad-bar--desk .pad-search-field:focus-within').width).toBe('240px')
  })

  it('searches the library from a form that works before the page has its script', async () => {
    const html = await render(visitor())
    const field = tagWith(html, 'id="pad-search-input"')

    expect(field).toContain('name="query"')
    expect(field).toContain('placeholder="Search 25 recipes"')
    expect(field).toContain('aria-label="Search 25 recipes"')
    expect(field).toContain('aria-keyshortcuts="/"')
    expect(html).toContain('<kbd class="pad-key" aria-hidden="true">/</kbd>')
  })

  it('leaves search out when there is no library to search', async () => {
    const nav = visitor()
    delete nav.urls.library

    const html = await render(nav)

    expect(html).not.toContain('role="search"')
    expect(html).not.toContain('pad-menu-find')
    expect(html).not.toContain('pad-card-search')
  })

  it('signs a member out through a posted form, from the kitchen card and from the phone menu', async () => {
    const html = await render(member())

    expect(html.match(/<form method="post" action="\/account\/logout\?returnUrl=%2F" class="contents">/g)).toHaveLength(2)
    expect(html).toMatch(/<button type="submit" class="kcc-btn kcc-btn--text">Sign out<\/button>/)
    expect(html).not.toContain('href="/account/logout')
  })

  it('keeps Recently viewed out of the server render', async () => {
    const html = await render(visitor())

    expect(html).not.toContain('Recently viewed')
    expect(html).toContain('<button type="button" class="kcc-badge pad-chip">Dinner</button>')
  })

  it('offers a phone a search glyph and a Menu with the same count', async () => {
    const html = await render(member())
    const phone = between(html, 'id="pad-bar-phone"', 'class="pad-page')

    expect(tagWith(phone, 'id="pad-menu-find"')).toContain('aria-label="Search 25 recipes"')
    expect(between(phone, 'id="pad-menu-menu"', '</button>')).toContain('Menu')
    expect(between(phone, 'id="pad-menu-menu"', '</button>')).toContain('pad-count')
  })

  it('leaves no retired class behind', async () => {
    expectNoRetiredMarkup(await render(member()))
  })
})
