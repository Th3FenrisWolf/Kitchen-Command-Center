import { describe, expect, it } from 'vitest'
import LoginView from '~/Pages/Account/Login/LoginView.Component.vue'
import { renderSsr } from '../../../../support/renderSsr'

const STRINGS = {
  'Login.SignIn': 'Sign in',
  'Login.SignUp': 'Sign up',
  'Login.UsernamePlaceholder': 'Username',
  'Login.EmailPlaceholder': 'Email',
  'Login.PasswordPlaceholder': 'Password',
  'Login.ConfirmPasswordPlaceholder': 'Confirm password',
  'Login.RememberMe': 'Remember me',
  'Login.NewHere': 'New here?',
  'Login.NewHereDescription': 'Start a pad of your own.',
  'Login.HaveAccount': 'Already have an account?',
  'Login.HaveAccountDescription': 'Pick up where you left off.',
}

// Sign-in is the server's state: `isSignIn` starts true, so the sign-up fields and the sign-up copy are
// client state this suite cannot reach.
const render = () => renderSsr(LoginView, { returnUrl: '/account', resourceStrings: STRINGS })

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

const formOf = (html: string) => html.slice(html.indexOf('<form'), html.indexOf('</form>'))

const switcherOf = (html: string) => html.slice(html.indexOf('</form>'))

describe('LoginView', () => {
  it('sets the form on one crisp sheet at a readable measure, nothing fixed to the viewport', async () => {
    const html = await render()
    const slip = tagWith(html, 'kcc-slip')

    expect(slip).toContain('kcc-tear-4')
    expect(slip).toContain('mx-auto')
    expect(slip).toContain('w-full')
    expect(slip).toContain('max-w-md')
    expect(slip).toContain('--r:0')
    expect(html).toContain('<div class="kcc-torn"><div class="kcc-sheet"')
    expect(html.match(/kcc-slip/g)).toHaveLength(1)
    expect(html).not.toContain('fixed')
  })

  it('pads the sheet up to 48px without a media query', async () => {
    const html = await render()

    expect(tagWith(html, 'kcc-sheet')).toContain('--pad:clamp(24px, 7.5vw, 48px)')
  })

  it('prints the mode on the label and keeps it in the outline', async () => {
    const html = await render()
    const label = html.slice(html.indexOf('<span class="kcc-label">'))

    expect(label).toContain('<i class="fa-duotone fa-key" aria-hidden="true">')
    expect(label).toContain('Sign in')
    expect(html).toMatch(/<div class="kcc-sheet"[^>]*>(?:<!--[^>]*-->)*<h2 class="sr-only">Sign in<\/h2>/)
  })

  it('keeps the selectors the sign-in helper types into', async () => {
    const form = formOf(await render())

    expect(tagWith(form, 'name="UserName"')).toMatch(/^<input /)
    expect(tagWith(form, 'name="Password"')).toMatch(/^<input /)

    const submit = tagWith(form, 'type="submit"')
    expect(submit).toMatch(/^<button /)
    expect(submit).toContain('class="kcc-btn"')
  })

  it('names each field by its placeholder rather than printing a label', async () => {
    const form = formOf(await render())

    expect(form).toContain('placeholder="Username"')
    expect(form).toContain('aria-label="Username"')
    expect(form).toContain('placeholder="Password"')
    expect(form).toContain('aria-label="Password"')
    expect(form).toContain('kcc-field')
    expect(form).not.toContain('kcc-lbl')
  })

  it('remembers the member with a box on the rule', async () => {
    const form = formOf(await render())
    const remember = tagWith(form, 'name="RememberMe"')

    expect(form).toContain('kcc-check')
    expect(remember).toMatch(/^<input /)
    expect(remember).toContain('type="checkbox"')
    expect(remember).toContain('class="sr-only"')
    expect(remember).toContain('value="true"')
    expect(form).toMatch(/<label for="[^"]+" class="kcc-box"><\/label>/)
    expect(form).toContain('Remember me')
  })

  it('keeps the error well off the sheet until a sign-in fails', async () => {
    const html = await render()

    expect(html).not.toContain('kcc-well')
    expect(html).not.toContain('role="alert"')
  })

  it('offers the other mode as a kick line with a text button', async () => {
    const switcher = switcherOf(await render())

    expect(switcher).toContain('kcc-kick')
    expect(switcher).toContain('New here?')
    expect(switcher).toContain('kcc-body text-ink-soft')
    expect(switcher).toContain('Start a pad of your own.')
    expect(switcher).toContain('class="kcc-btn kcc-btn--text')
    expect(switcher).toContain('Sign up')
  })

  it('leaves no Softbound remnant on the sheet', async () => {
    const html = await render()

    expect(html).not.toMatch(/sk-[a-z]/)
    expect(html).not.toMatch(/\bv-ink\b/)
    expect(html).not.toMatch(/\brounded-(?:lg|xl|2xl|3xl)\b/)
    expect(html).not.toMatch(/\bfont-(?:bold|semibold|medium)\b/)
    expect(html).not.toMatch(/\bshadow-/)
    expect(html).not.toMatch(/\btext-danger-ink\b/)
    expect(html).not.toMatch(/\bbg-paper-2\b/)
  })
})
