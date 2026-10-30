import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import postcss, { type AtRule, type Rule } from 'postcss'
import { describe, expect, it } from 'vitest'
import { createSSRApp, h } from 'vue'
import { renderToString } from '@vue/server-renderer'
import AppHeader from '~/Components/Header/AppHeader.Component.vue'
import { BLOOM_KEY, createBloom } from '~/Bloom/bloomContext'
import { planSheet } from '~/Bloom/washes'
import type { NavModel } from '~/Types/Nav'
import { member, visitor } from '../../../support/nav'
import { expectNoRetiredMarkup, openTag, renderSsr, tagWith } from '../../../support/ssr'
import { washFields } from '../../../support/washes'

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
      '<a class="kcc-btn pad-new" href="/recipes/create-recipe/" title="New recipe"><i class="fa-duotone fa-plus" aria-hidden="true"></i><span class="max-[820px]:sr-only">New recipe</span></a>',
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

  it('starts every item of the bar on the bar’s first line, so the text in it lies on the pad’s rules', () => {
    const rules = styleRules()

    expect(['flex-start', 'start']).toContain(declarationsOf(rules, '.pad-bar')['align-items'])
    expect(['flex-start', 'start']).toContain(declarationsOf(rules, '.pad-menus')['align-items'])
  })

  it('lets the kit place the menus and the glyph on the line they start on, centred on the menu label’s rule', async () => {
    const html = await render(member())
    const controls = [...html.matchAll(/<button[^>]*class="pad-(?:menu|glyph)[" ][^>]*>/g)].map(([tag]) => tag)
    const rules = styleRules()

    expect(controls.length).toBeGreaterThan(0)
    for (const tag of controls) expect(tag).toMatch(/class="[^"]*\bkcc-control\b/)
    for (const selector of ['.pad-menu', '.pad-glyph']) {
      expect(declarationsOf(rules, selector).translate).toBeUndefined()
    }
    for (const selector of ['.pad-menu', '.pad-glyph']) {
      expect(declarationsOf(rules, selector)['--centre-size']?.replace(/\s+/g, '')).toBe(
        'calc(var(--k-hazelnut)/var(--k-sono)*var(--text-body))',
      )
    }
    expect(declarationsOf(rules, '.pad-menu')['--h'], 'a menu takes kcc-control’s default height').toBeUndefined()
  })

  it('sizes the glyph as an icon button', () => {
    const glyph = declarationsOf(styleRules(), '.pad-glyph')

    expect(glyph['--h']).toBe('var(--control-sm)')
    expect(glyph.height).toBe('var(--control-sm)')
    expect(glyph.width).toBe('var(--control-sm)')
  })

  it.each(['.pad-menu', '.pad-glyph'])('leaves %s’s paper fill at rest to kcc-control', (selector) => {
    const rules = styleRules()
    expect(declarationsOf(rules, selector)['background-color']).toBeUndefined()
    expect(declarationsOf(rules, `${selector}:not(.kcc-pill)`)['background-color']).toBeUndefined()
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

  it('makes New recipe a 32px icon button below 820px, where it loses its label', () => {
    const narrow = styleRules().filter(
      (rule) => rule.parent?.type === 'atrule' && (rule.parent as AtRule).params === '(max-width: 819.98px)',
    )

    expect(declarationsOf(narrow, '.pad-new')).toEqual({
      '--h': 'var(--control-sm)',
      height: 'var(--control-sm)',
      'line-height': 'var(--control-sm)',
      padding: '0',
      width: 'var(--control-sm)',
    })
  })

  it('searches the library from a form that works before the page has its script', async () => {
    const html = await render(visitor())
    const field = tagWith(html, 'id="pad-search-input"')

    expect(field).toContain('name="query"')
    expect(field).toContain('placeholder="Search 25 recipes"')
    expect(field).toContain('aria-label="Search 25 recipes"')
    expect(field).toContain('aria-keyshortcuts="/"')
    expect(html).toContain('<kbd class="pad-key kcc-num" aria-hidden="true">/</kbd>')
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

const PAD_CARDS = ['pad-card-recipes', 'pad-card-kitchen', 'pad-card-search', 'pad-card-find', 'pad-card-menu']

const renderBloomed = async (nav: NavModel, seed: number, page: string) => {
  const bloom = createBloom(seed, page)
  const app = createSSRApp({ render: () => h(AppHeader, { logoAlt: 'Kitchen Command Center', nav }) })
  app.provide(BLOOM_KEY, bloom)
  return { html: await renderToString(app), washes: bloom.collected() }
}

const padTopOf = (html: string) => between(html, 'class="pad-top', 'id="pad-bar-desk"')

const cardOf = (html: string, id: string) => {
  const start = html.indexOf(`id="${id}"`)
  if (start === -1) return undefined
  const next = html.indexOf('id="pad-card-', start + id.length)
  return html.slice(start, next === -1 ? undefined : next)
}

const padSheetFields = (html: string) =>
  Object.fromEntries(
    [['@pad', padTopOf(html)] as const, ...PAD_CARDS.map((id) => [`@${id}`, cardOf(html, id)] as const)].flatMap(
      ([key, sheet]) => (sheet === undefined ? [] : [[key, washFields(sheet)[0]]]),
    ),
  )

describe("the pad's washes", () => {
  it('opens the top sheet with a page of washes keyed @pad, the same on every page', async () => {
    const { html } = await renderBloomed(visitor(), 7, '/recipes')
    const fields = washFields(padTopOf(html))

    expect(padTopOf(html)).toMatch(/<div class="kcc-sheet" style="--pad:\s?12px;?"><span class="kcc-washes"/)
    expect(fields).toHaveLength(1)
    expect(fields[0]!.map((wash) => wash.id)).toEqual(
      planSheet({ seed: 7, key: '@pad', size: 'page' }).map((wash) => wash.id),
    )
  })

  it("lays no wash on the pad page, which is the top sheet's blank", async () => {
    const { html } = await renderBloomed(member(), 7, '/recipes')

    expect(between(html, 'class="pad-page', 'id="pad-cards"')).not.toContain('kcc-washes')
  })

  it('names fixed beats for the pad and each card, fourteen in all and none shared', async () => {
    const slots = new Map<string, number | undefined>()
    for (let seed = 1; seed <= 40; seed++) {
      for (const [nav, page] of [
        [member(), '/a'],
        [visitor(), '/b'],
      ] as const) {
        const { washes } = await renderBloomed(nav, seed, page)
        for (const wash of washes.filter((candidate) => candidate.key.startsWith('@'))) {
          const at = `${wash.key}#${wash.index}`
          if (slots.has(at)) expect(wash.slot, at).toBe(slots.get(at))
          slots.set(at, wash.slot)
        }
      }
    }

    const byKey = new Map<string, number[]>()
    for (const [at, slot] of slots) {
      const key = at.slice(0, at.indexOf('#'))
      byKey.set(key, [...(byKey.get(key) ?? []), slot!])
    }
    const all = [...slots.values()]

    expect([...byKey.keys()].sort()).toEqual(['@pad', ...PAD_CARDS.map((id) => `@${id}`)].sort())
    expect(byKey.get('@pad')).toHaveLength(4)
    for (const id of PAD_CARDS) expect(byKey.get(`@${id}`), id).toHaveLength(2)
    expect(all.every((slot) => Number.isInteger(slot) && slot! >= 0 && slot! < 16)).toBe(true)
    expect(new Set(all).size).toBe(14)
  })

  it("renders every pad sheet's washes the same with and without a member, and on two pages", async () => {
    const seen = await Promise.all([
      renderBloomed(visitor(), 11, '/recipes'),
      renderBloomed(member(), 11, '/recipes'),
      renderBloomed(member(), 11, '/account'),
    ])
    const [asVisitor, asMember, elsewhere] = seen.map(({ html }) => padSheetFields(html))

    expect(Object.keys(asMember!)).toHaveLength(6)
    expect(elsewhere).toEqual(asMember)
    const { ['@pad-card-kitchen']: kitchen, ...shared } = asMember!
    expect(kitchen).toBeDefined()
    expect(asVisitor).toEqual(shared)
  })
})
