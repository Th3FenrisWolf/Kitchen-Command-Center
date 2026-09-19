import { describe, expect, it } from 'vitest'
import '~/Utilities/StringExtensions'
import AccountView from '~/Pages/Account/AccountView.Component.vue'
import { renderSsr } from '../../../support/renderSsr'

const STRINGS = {
  'Account.AccountSettings': 'Account settings',
  'Account.ComingSoon': 'Coming soon',
  'Account.Favorites': 'Favorites',
  'Account.MemberSince': 'Member since',
  'Account.MyRecipesAndVariants': 'My recipes and variants',
  'Account.NoCreationsYet': 'Nothing on the pad yet.',
  'Account.PendingReview': 'Pending review',
  'Account.RecentActivity': 'Recent activity',
  'Account.RecipesLabel': 'Recipes',
  'Account.SignOut': 'Sign out',
  'Account.StartedByYou': 'Started by you',
  'Account.VariantsLabel': 'Variants',
}

const RECIPE_GROUPS = [
  {
    pageId: 1,
    recipeName: 'Mac & Cheese',
    recipeIcon: 'fa-duotone fa-pot-food',
    recipeUrl: '~/recipes/mac-and-cheese',
    isPending: false,
    startedByYou: true,
    variants: [
      {
        pageId: 10,
        name: 'Classic Stovetop',
        icon: 'fa-duotone fa-pot-food',
        url: '~/recipes/mac/classic',
        isPending: false,
      },
      { pageId: 11, name: 'Spicy Jalapeño', icon: 'fa-duotone fa-pepper-hot', isPending: true },
    ],
  },
  {
    pageId: 2,
    recipeName: 'Brown Butter Gnocchi',
    recipeIcon: 'fa-duotone fa-wheat',
    isPending: true,
    startedByYou: false,
    variants: [{ pageId: 20, name: 'Sage Brown Butter', icon: 'fa-duotone fa-leaf', isPending: true }],
  },
]

const render = (recipeGroups: unknown[] = RECIPE_GROUPS) =>
  renderSsr(AccountView, {
    displayName: 'Ada Lovelace',
    initials: 'AL',
    memberSince: 'March 2025',
    settingsUrl: '/account/settings',
    logoutUrl: '/account/logout',
    recipeGroups,
    resourceStrings: STRINGS,
  })

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

// Three blocks in source order: the profile sheet, the creations column, then the coming-soon pair. The
// creations column is the only one holding a section name, and its groups are list items, so the first
// </section> after that name closes it and everything past it is the coming-soon pair.
const creationsOf = (html: string) =>
  html.slice(html.indexOf('kcc-secname'), html.indexOf('</section>', html.indexOf('kcc-secname')))

const comingSoonOf = (html: string) => html.slice(html.indexOf('</section>', html.indexOf('kcc-secname')))

describe('AccountView', () => {
  it('pools one lavender wash in the profile sheet corner and names the member in display size', async () => {
    const html = await render()
    const profile = html.slice(0, html.indexOf('kcc-secname'))

    expect(profile).toContain('kcc-slip kcc-tear-1')
    expect(profile).toContain('--c:var(--color-lavender)')
    expect(profile).toContain('--x:88%')
    expect(profile).toContain('<h1 class="kcc-h3">Ada Lovelace</h1>')
    expect(profile.match(/kcc-wash/g)).toHaveLength(1)
  })

  it('kicks the join date in Sono', async () => {
    const html = await render()

    expect(html).toContain('Member since')
    expect(html).toContain('<span class="kcc-num">March 2025</span>')
    expect(tagWith(html, 'kcc-kick')).toMatch(/^<p /)
  })

  it('counts the member’s creations in a stats row', async () => {
    const html = await render()
    const stats = html.slice(html.indexOf('kcc-stats'), html.indexOf('kcc-secname'))

    expect(stats).toContain('Recipes')
    expect(stats).toContain('<p class="kcc-v">1</p>')
    expect(stats).toContain('Variants')
    expect(stats).toContain('<p class="kcc-v">3</p>')
  })

  it('offers settings as a ghost pill and signing out as text', async () => {
    const html = await render()

    expect(tagWith(html, 'href="/account/settings"')).toContain('class="kcc-btn kcc-btn--ghost"')
    expect(tagWith(html, 'href="/account/logout"')).toContain('class="kcc-btn kcc-btn--text"')
  })

  it('sets every recipe group on its own slip, never repeating a neighbour’s tear', async () => {
    const creations = creationsOf(await render())

    expect(creations).toMatch(/<h2[^>]*>My recipes and variants<\/h2>/)
    expect(creations).toContain('kcc-slip kcc-tear-2')
    expect(creations).toContain('kcc-slip kcc-tear-3')
    expect(creations).not.toContain('kcc-tear-1')
    expect(tagWith(creations, 'kcc-sheet')).toContain('--pad:16px')
    expect(creations).toContain('<h3 class="kcc-h4">')
    expect(creations).toContain('Mac &amp; Cheese')
  })

  it('badges what the member started and what is still waiting, in hairline pills', async () => {
    const creations = creationsOf(await render())

    expect(creations).toContain('<span class="kcc-badge">')
    expect(creations).toContain('Started by you')
    expect(creations.match(/Pending review/g)).toHaveLength(3)
    expect(creations).not.toMatch(/\bbg-(?:teal|yellow|maroon)\b/)
    expect(creations).not.toContain('text-ink-on-wash')
  })

  it('rows each variant name against its badge', async () => {
    const creations = creationsOf(await render())

    expect(creations).toContain('grid-cols-[1fr_auto]')
    expect(creations).toContain('Classic Stovetop')
    expect(creations).toContain('href="/recipes/mac/classic"')
  })

  it('says so plainly when the pad is empty', async () => {
    const creations = creationsOf(await render([]))

    expect(creations).toContain('Nothing on the pad yet.')
    expect(creations).toContain('kcc-body')
    expect(creations).not.toContain('kcc-slip')
  })

  it('stands the unbuilt sections in as labelled lavender sheets via ComingSoonSection, no skeleton blocks', async () => {
    const comingSoon = comingSoonOf(await render())

    expect(comingSoon).toContain('fa-duotone fa-hourglass-half')
    expect(comingSoon).toContain('Favorites')
    expect(comingSoon).toContain('Recent activity')
    expect(comingSoon).toContain('kcc-tear-3')
    expect(comingSoon).toContain('kcc-tear-5')
    expect(comingSoon.match(/kcc-wash/g)).toHaveLength(2)
    // AccountController doesn't request Shared.ComingSoon (only Account.ComingSoon, which the shared
    // label no longer reads), so it falls back to its own key here the same as it would in production.
    expect(comingSoon.match(/Shared\.ComingSoon/g)).toHaveLength(2)
  })

  it('leaves no Softbound remnant on the profile', async () => {
    const html = await render()

    expect(html).not.toMatch(/sk-[a-z]/)
    expect(html).not.toMatch(/\bv-ink\b/)
    expect(html).not.toMatch(/\brounded-(?:lg|xl|2xl|3xl|full)\b/)
    expect(html).not.toMatch(/\bfont-(?:bold|semibold|medium)\b/)
    expect(html).not.toMatch(/\bshadow-/)
    expect(html).not.toMatch(/\bbg-(?:desk-2|maroon|teal|yellow|peach)\b/)
    expect(html).not.toMatch(/\bborder\b/)
  })
})
