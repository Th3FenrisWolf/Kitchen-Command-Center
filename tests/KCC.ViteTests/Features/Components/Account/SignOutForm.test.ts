import { describe, expect, it } from 'vitest'
import { h } from 'vue'
import SignOutForm from '~/Components/Account/SignOutForm.vue'
import { isSignOutUrl } from '~/Components/Account/signOut'
import { renderSsr } from '../../../support/renderSsr'

const button = { default: () => h('button', { type: 'submit' }, 'Sign out') }

describe('SignOutForm', () => {
  it('posts to the sign-out path, with a token field the submit fills in', async () => {
    const html = await renderSsr(SignOutForm, {}, button)

    expect(html).toContain('<form method="post" action="/account/logout" class="contents">')
    expect(html).toContain('<input type="hidden" name="__RequestVerificationToken" value="">')
    expect(html).toContain('<button type="submit">Sign out</button>')
  })

  it('posts to the action it is given', async () => {
    const html = await renderSsr(SignOutForm, { action: '/account/logout?returnUrl=%2Frecipes' }, button)

    expect(html).toContain('action="/account/logout?returnUrl=%2Frecipes"')
  })

  it.each([
    ['/account/logout', true],
    ['/account/logout/', true],
    ['/Account/Logout', true],
    ['/account/', false],
    [undefined, false],
  ])('treats %s as the sign-out link: %s', (url, expected) => {
    expect(isSignOutUrl(url)).toBe(expected)
  })
})
