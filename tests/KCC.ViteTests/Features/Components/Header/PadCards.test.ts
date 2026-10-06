import { describe, expect, it } from 'vitest'
import KitchenCard from '~/Components/Header/KitchenCard.vue'
import MenuCard from '~/Components/Header/MenuCard.vue'
import PadCard from '~/Components/Header/PadCard.vue'
import RecipesCard from '~/Components/Header/RecipesCard.vue'
import SearchCard from '~/Components/Header/SearchCard.vue'
import { navLabels } from '~/Components/Header/navLabels'
import type { RecipeSearchHit } from '~/Types/Recipe'
import { LABELS, member, visitor } from '../../../support/nav'
import { renderSsr, tagWith } from '../../../support/ssr'

const t = navLabels(LABELS)

const hit = (name: string, slug: string): RecipeSearchHit => ({
  name,
  slug,
  category: 'Dinner',
  tags: ['Vegan', 'Spicy', 'Easy'],
  averageRating: null,
  reviewCount: 0,
  variantCount: 2,
  fastestTime: 35,
})

const search = (props: Record<string, unknown> = {}) =>
  renderSsr(SearchCard, {
    query: '',
    searched: '',
    results: [],
    total: 0,
    failed: false,
    recent: [],
    suggestions: ['Dinner', 'Breakfast'],
    library: '/recipes/',
    recipeTotal: 25,
    t,
    ...props,
  })

describe('PadCard', () => {
  it('keeps a card that is not open inert, so only an open card takes focus', async () => {
    const at = (state: string) => renderSsr(PadCard, { id: 'pad-card-x', state, tear: 4, tilt: -0.35 })

    expect(tagWith(await at('rest'), 'id="pad-card-x"')).toContain('inert')
    expect(tagWith(await at('peek'), 'id="pad-card-x"')).toContain('inert')
    expect(tagWith(await at('open'), 'id="pad-card-x"')).not.toContain('inert')
    expect(tagWith(await at('open'), 'id="pad-card-x"')).toContain('is-open')
  })

  it('is a torn sheet of the kit, tilted and placed by the pad', async () => {
    const html = await renderSsr(PadCard, {
      id: 'pad-card-x',
      state: 'open',
      tear: 6,
      tilt: 0.3,
      width: '440px',
      left: 120,
      wash: 'lavender',
      at: { x: '100%', y: '100%', w: '30%', h: '62%' },
    })
    const card = tagWith(html, 'id="pad-card-x"')

    expect(card).toContain('class="kcc-slip kcc-tear-6 pad-card is-open"')
    expect(card).toContain('style="--r:0.3;--pad-card-w:440px;left:120px;"')
    expect(html).toContain('<div class="kcc-torn"><div class="kcc-sheet"><span class="kcc-wash"')
  })
})

describe('RecipesCard', () => {
  const render = (nav = visitor()) =>
    renderSsr(RecipesCard, {
      recipes: nav.recipes,
      total: nav.recipeTotal,
      library: nav.urls.library,
      currentPage: nav.urls.currentPage,
      t,
    })

  it('lists meals with their glyphs and counts, and leaves a meal without a glyph its column', async () => {
    const html = await render()

    expect(html).toMatch(
      /<a class="pad-line" href="\/recipes\/\?category=Breakfast"><i class="fa-duotone fa-egg" aria-hidden="true"><\/i><span class="pad-line-label">Breakfast<\/span><span class="kcc-num">4<\/span><\/a>/,
    )
    expect(html).toMatch(
      /<a class="pad-line" href="\/recipes\/\?category=Dinner" aria-current="page"><span aria-hidden="true"><\/span>/,
    )
  })

  it('lists diets without glyphs, then the quick picks', async () => {
    const html = await render()

    expect(html).toContain('<span class="pad-line-label">Vegan</span><span class="kcc-num">12</span>')
    expect(html).toContain('href="/surprise-me"')
    expect(html.indexOf('Diets')).toBeLessThan(html.indexOf('Quick picks'))
  })

  it('leaves out a section with nothing in it', async () => {
    const nav = visitor()
    nav.recipes.diets = []

    expect(await render(nav)).not.toContain('Diets')
  })

  it('ends on the whole library, and the hand note only when the owner wrote one', async () => {
    const plain = await render()
    const noted = visitor()
    noted.recipes.note = 'Stuck? Dinner is a safe bet.'

    expect(plain).toContain('<a class="kcc-link" href="/recipes/">All 25 recipes</a>')
    expect(plain).not.toContain('kcc-hand')
    expect(await render(noted)).toContain('<p class="kcc-hand">Stuck? Dinner is a safe bet.</p>')
  })

  it('marks the current page however its address writes a space', async () => {
    const nav = visitor()
    nav.recipes.meals.push({ label: 'Main Dishes', url: '/recipes/?category=Main%20Dishes', count: 3 })
    nav.urls.currentPage = '/recipes/?category=Main+Dishes'

    expect(await render(nav)).toContain('<a class="pad-line" href="/recipes/?category=Main%20Dishes" aria-current="page">')
  })

  it('marks the current page with or without a trailing slash', async () => {
    const nav = visitor()
    nav.urls.currentPage = '/recipes?category=Dinner'

    expect(await render(nav)).toContain('<a class="pad-line" href="/recipes/?category=Dinner" aria-current="page">')
  })

  it('leaves a row whose address cannot be read unmarked', async () => {
    const nav = visitor()
    nav.recipes.meals.push({ label: 'Unreadable', url: 'http://', count: 1 })
    nav.urls.currentPage = 'http://'

    expect(await render(nav)).toContain('<a class="pad-line" href="http://">')
  })

  it("sets every number in a row's label in Sono", async () => {
    const nav = visitor()
    nav.recipes.quickPicks.unshift({
      label: 'Under 30 minutes',
      url: '/recipes/?timeMax=30',
      count: 9,
      icon: 'fa-duotone fa-stopwatch',
    })

    expect(await render(nav)).toContain('Under <span class="kcc-num">30</span> minutes')
  })
})

describe('KitchenCard', () => {
  const render = (nav = member()) => renderSsr(KitchenCard, { member: nav.member, urls: nav.urls, t })

  it('names the member, links to their kitchen, and counts it', async () => {
    const html = await render()

    expect(html).toContain('<a class="kcc-link" href="/account/" aria-current="page">Grace</a>')
    expect(html).toContain('Member since <span class="kcc-num">October 2026</span>')
    expect(html).toContain('<p class="kcc-lbl">Recipes</p><p class="kcc-v">3</p>')
    expect(html).toContain('<p class="kcc-lbl">Variants</p><p class="kcc-v">5</p>')
    expect(html).toContain('<span class="pad-line-label">Your recipes and variants</span><span class="kcc-num">8</span>')
    expect(html).toContain('<span class="pad-line-label">Waiting for review</span><span class="kcc-num">1</span>')
    expect(html).toContain('href="/account/settings/"')
  })

  it('leaves out Waiting for review when nothing waits', async () => {
    const nav = member()
    nav.member!.kitchen = { recipes: 3, variants: 5, waiting: 0 }

    expect(await render(nav)).not.toContain('Waiting for review')
  })

  it('shows the kitchen without counts when they could not be read', async () => {
    const nav = member()
    delete nav.member!.kitchen
    const html = await render(nav)

    expect(html).not.toContain('kcc-stats')
    expect(html).toContain('<span class="pad-line-label">Your recipes and variants</span><span class="kcc-num"></span>')
  })
})

describe('MenuCard', () => {
  const render = (nav = visitor()) => renderSsr(MenuCard, { recipes: nav.recipes, urls: nav.urls, member: nav.member, t })

  it('gives a phone the meals in two columns, the diets as chips, and the quick picks', async () => {
    const html = await render()

    expect(html).toContain('class="pad-lines pad-lines--glyphs pad-lines--two"')
    expect(html).toContain('<a class="kcc-badge pad-chip" href="/recipes/?diet=Vegan">Vegan</a>')
    expect(html).toContain('href="/recipes/?sort=rated"')
  })

  it('keeps the diet chips on the rule, however many rows they wrap to', async () => {
    expect(await render()).toContain('<div class="kcc-badges pad-chips"')
  })

  it('offers a visitor Sign in and Ask for an account', async () => {
    const html = await render()

    expect(html).toContain('href="/account/login/?returnUrl=%2Frecipes%2F%3Fcategory%3DDinner"')
    expect(html).toContain('<a class="kcc-link kcc-kick" href="/account/login/?mode=register">Ask for an account</a>')
    expect(html).not.toContain('kitchen')
  })

  it('gives a member their kitchen, New recipe and Sign out', async () => {
    const html = await render(member())

    expect(html).toContain('Grace’s kitchen')
    expect(html).toContain('<span class="pad-line-label">Grace</span>')
    expect(html).toContain('href="/recipes/create-recipe/"')
    expect(html).toContain('action="/account/logout?returnUrl=%2F"')
    expect(html).not.toContain('Sign in')
  })
})

describe('SearchCard', () => {
  it('offers the suggestions before you type', async () => {
    const html = await search()

    expect(html).toContain('Try')
    expect(html).toContain('<button type="button" class="kcc-badge pad-chip">Breakfast</button>')
    expect(html).not.toContain('Recently viewed')
  })

  it('lists what you viewed last, above the suggestions', async () => {
    const html = await search({ recent: [{ name: 'Weeknight Tacos', url: '/recipes/weeknight-tacos/' }] })

    expect(html.indexOf('Recently viewed')).toBeLessThan(html.indexOf('Try'))
    expect(html).toMatch(
      /href="\/recipes\/weeknight-tacos\/"><i class="fa-duotone fa-clock-rotate-left" aria-hidden="true"><\/i><span class="pad-line-label">Weeknight Tacos<\/span>/,
    )
  })

  it('lists the results with their meal, first two tags and quickest time, and links to the rest', async () => {
    const html = await search({
      query: 'chili',
      searched: 'chili',
      results: [hit('Big-Pot Chili', '/recipes/big-pot-chili/')],
      total: 7,
    })

    expect(html).toContain('<a class="pad-result" href="/recipes/big-pot-chili/">')
    expect(html).toContain('<span class="kcc-kick pad-result-meta">Dinner · Vegan · Spicy</span>')
    expect(html).toContain('<span class="kcc-kick kcc-num">35m</span>')
    expect(html).toContain('<a class="kcc-link" href="/recipes/?query=chili">All 7 results</a>')
    expect(html).toContain('aria-busy="false"')
  })

  it('keeps the suggestions while the first search is on its way, and says it is busy', async () => {
    const html = await search({ query: 'chi' })

    expect(html).toContain('<div aria-busy="true">')
    expect(html).toContain('<button type="button" class="kcc-badge pad-chip">Breakfast</button>')
  })

  it('keeps the last results while the next search is on its way', async () => {
    const html = await search({
      query: 'chilli',
      searched: 'chili',
      results: [hit('Big-Pot Chili', '/recipes/big-pot-chili/')],
      total: 1,
    })

    expect(html).toContain('<div aria-busy="true">')
    expect(html).toContain('<span class="kcc-h4 pad-result-name">Big-Pot Chili</span>')
  })

  it('links to no more results when all of them fit', async () => {
    const html = await search({
      query: 'chili',
      searched: 'chili',
      results: [hit('Big-Pot Chili', '/recipes/big-pot-chili/')],
      total: 1,
    })

    expect(html).not.toContain('All 1 results')
  })

  it('says when nothing matches, and offers the whole library', async () => {
    const html = await search({ query: 'zzz', searched: 'zzz' })

    expect(html).toContain('Nothing matches “zzz” yet.')
    expect(html).toContain('<a class="kcc-link" href="/recipes/">All 25 recipes</a>')
  })

  it('says when the search is unavailable, in a well', async () => {
    const html = await search({ query: 'chili', searched: 'chili', failed: true })

    expect(html).toContain('<p class="kcc-well kcc-well--danger kcc-kick" role="alert">Search is unavailable</p>')
  })
})
