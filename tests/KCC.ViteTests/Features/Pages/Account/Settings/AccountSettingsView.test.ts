import { describe, expect, it } from 'vitest'
import '~/Utilities/StringExtensions'
import AccountSettingsView from '~/Pages/Account/Settings/AccountSettingsView.Component.vue'
import { renderSsr } from '../../../../support/renderSsr'

const STRINGS = {
  'Account.AccountSettings': 'Account settings',
  'Account.BackToProfile': 'Back to profile',
  'Account.ChangePassword': 'Change password',
  'Account.ConfirmNewPassword': 'Confirm new password',
  'Account.CurrentPassword': 'Current password',
  'Account.Email': 'Email',
  'Account.EmailComingSoon': 'Coming soon',
  'Account.EmailComingSoonNote': 'Changing your email is on the way.',
  'Account.FirstName': 'First name',
  'Account.LastName': 'Last name',
  'Account.NewPassword': 'New password',
  'Account.Profile': 'Profile',
  'Account.SaveChanges': 'Save changes',
  'Account.SignOut': 'Sign out',
  'Account.UpdatePassword': 'Update password',
}

const render = () =>
  renderSsr(AccountSettingsView, {
    firstName: 'Ada',
    lastName: 'Lovelace',
    email: 'ada@example.com',
    backUrl: '~/account',
    logoutUrl: '/account/logout',
    resourceStrings: STRINGS,
  })

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

// Field prints its label through a slot, so SSR wraps the resource string in Vue's fragment markers.
const controlIdFor = (html: string, label: string) =>
  html.match(new RegExp(`<label for="([^"]+)" class="kcc-lbl"><!--\\[--><span>${label}`))?.[1] ?? ''

// The hero is a sheet of its own; these assertions count and describe the form's three sheets, so they read
// the markup from the grid below it down.
const sheetsOf = (html: string) => html.slice(html.indexOf('<div class="mt-6 grid'))

describe('AccountSettingsView', () => {
  it('sets each group on its own crisp sheet, tears that never repeat', async () => {
    const sheets = sheetsOf(await render())

    expect(sheets.match(/kcc-slip/g)).toHaveLength(3)
    expect(sheets).toContain('kcc-tear-2')
    expect(sheets).toContain('kcc-tear-5')
    expect(sheets).toContain('kcc-tear-6')
    expect(sheets.match(/--r:0/g)).toHaveLength(3)
    expect(sheets.match(/--pad:clamp\(24px, 7\.5vw, 48px\)/g)).toHaveLength(3)
  })

  it('prints each group name on a label and keeps it in the outline', async () => {
    const sheets = sheetsOf(await render())

    expect(sheets.match(/kcc-label/g)).toHaveLength(3)
    expect(sheets).toContain('<h2 class="sr-only">Profile</h2>')
    expect(sheets).toContain('<h2 class="sr-only">Change password</h2>')
    expect(sheets).toContain('<h2 class="sr-only">Sign out</h2>')
  })

  it('binds every printed label to its control and keeps the form attributes', async () => {
    const html = await render()
    const firstNameId = controlIdFor(html, 'First name')
    const lastNameId = controlIdFor(html, 'Last name')
    const currentPasswordId = controlIdFor(html, 'Current password')

    expect(firstNameId).toBeTruthy()
    expect(firstNameId).not.toEqual(lastNameId)

    const firstName = tagWith(html, `id="${firstNameId}"`)
    expect(firstName).toMatch(/^<input /)
    expect(firstName).toContain('name="FirstName"')
    expect(firstName).toContain('autocomplete="given-name"')
    expect(firstName).toContain('type="text"')

    expect(tagWith(html, `id="${lastNameId}"`)).toContain('autocomplete="family-name"')

    const currentPassword = tagWith(html, `id="${currentPasswordId}"`)
    expect(currentPassword).toContain('type="password"')
    expect(currentPassword).toContain('autocomplete="current-password"')
    expect(currentPassword).toContain('required')
    expect(html).toContain('kcc-field')
  })

  it('keeps the resource string itself in each label, editor hooks and all', async () => {
    const html = await render()

    expect(html).toMatch(/<label for="[^"]+" class="kcc-lbl"><!--\[--><span>First name<\/span><!--\]-->/)
    expect(html).toMatch(
      /<label for="[^"]+" class="kcc-lbl"><!--\[--><span>Current password<\/span><!--\]--><span aria-hidden="true"> \*/,
    )
  })

  it('reads the email out with the coming-soon note as its hint, each half its own resource string', async () => {
    const html = await render()
    const emailId = controlIdFor(html, 'Email')
    const email = tagWith(html, `id="${emailId}"`)

    expect(email).toContain('readonly')
    expect(email).toContain('type="email"')
    expect(email).toContain(`aria-describedby="${emailId}-hint"`)
    expect(html).toContain(`<p id="${emailId}-hint" class="kcc-kick text-ink">`)
    // Two <ResourceString>s, not one interpolated string: each keeps its own in-context editor hook.
    expect(html).toContain('<span>Coming soon</span> · <span>Changing your email is on the way.</span>')
  })

  it('keeps both status wells off the page until a form answers', async () => {
    const html = await render()

    expect(html).not.toContain('kcc-well')
    expect(html).not.toContain('role="status"')
    expect(html).not.toContain('role="alert"')
  })

  it('saves each form from a marker pill inside its own form', async () => {
    const html = await render()
    const submits = html.match(/<button[^>]*type="submit"[^>]*>/g) ?? []

    expect(submits).toHaveLength(2)
    submits.forEach((submit) => expect(submit).toContain('class="kcc-btn"'))
    expect(html.match(/<form/g)).toHaveLength(2)
    expect(html).toContain('Save changes')
    expect(html).toContain('Update password')
  })

  it('walks back to the profile and out of the account on ghost pills, never in red', async () => {
    const html = await render()

    expect(tagWith(html, 'href="/account"')).toContain('class="kcc-btn kcc-btn--ghost"')
    expect(tagWith(html, 'href="/account/logout"')).toContain('class="kcc-btn kcc-btn--ghost"')
    expect(html).not.toContain('text-danger-ink')
  })

  it('leaves no Softbound remnant on the page', async () => {
    const sheets = await render()

    expect(sheets).not.toMatch(/sk-[a-z]/)
    expect(sheets).not.toMatch(/\bv-ink\b/)
    expect(sheets).not.toMatch(/\brounded-(?:lg|xl|2xl|3xl)\b/)
    expect(sheets).not.toMatch(/\bfont-(?:bold|semibold|medium)\b/)
    expect(sheets).not.toMatch(/\bshadow-/)
    expect(sheets).not.toMatch(/\btext-(?:danger|success)-ink\b/)
  })
})
