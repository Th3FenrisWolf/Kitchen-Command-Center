import { describe, expect, it } from 'vitest'
import MenuItem from '~/Components/Header/MenuItem.vue'
import { renderSsr } from '../../../support/ssr'

const account = {
  displayText: 'Account',
  subLinks: [
    { displayText: 'Profile', url: '/account/', target: '' },
    { displayText: 'Logout', url: '/account/logout', target: '' },
  ],
}

describe('MenuItem', () => {
  it('signs out through a posted form, not a link', async () => {
    const html = await renderSsr(MenuItem, { item: account, menuId: 'Account' })

    expect(html).toMatch(
      /<form method="post" action="\/account\/logout"[^>]*>.*<button type="submit"[^>]*>\s*Logout\s*<\/button>/s,
    )
    expect(html).not.toContain('href="/account/logout"')
  })

  it('keeps every other entry a link', async () => {
    const html = await renderSsr(MenuItem, { item: account, menuId: 'Account' })

    expect(html).toMatch(/<a[^>]*href="\/account\/"[^>]*>\s*Profile\s*<\/a>/)
  })

  it('turns a flat Logout entry into the same form', async () => {
    const html = await renderSsr(MenuItem, { item: { displayText: 'Logout', url: '/account/logout' }, menuId: 'Logout' })

    expect(html).toMatch(/^<form method="post" action="\/account\/logout"/)
  })
})
