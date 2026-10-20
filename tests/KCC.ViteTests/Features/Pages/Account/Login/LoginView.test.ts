import { describe, expect, it } from 'vitest'
import LoginView from '~/Pages/Account/Login/LoginView.Component.vue'
import { renderSsr, tagWith, expectNoRetiredMarkup } from '../../../../support/ssr'
import { classesOf, enclosingTag } from '../../../../support/recipeWizard'

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

const render = (register = false) => renderSsr(LoginView, { returnUrl: '/account', register, resourceStrings: STRINGS })

const formOf = (html: string) => html.slice(html.indexOf('<form'), html.indexOf('</form>'))

const switcherOf = (html: string) => html.slice(html.indexOf('</form>'))

describe('LoginView', () => {
  it('rests on one blank, with the standard turn', async () => {
    const html = await render()

    expect((html.match(/kcc-blank/g) ?? []).length).toBe(1)
    expect(html).not.toContain('kcc-slip--wide')
  })

  it('sets the form on one crisp sheet at a readable measure, nothing fixed to the viewport', async () => {
    const html = await render()
    const slip = tagWith(html, 'kcc-slip')

    expect(slip).toContain('kcc-tear-4')
    expect(slip).toContain('mx-auto')
    expect(slip).toContain('w-full')
    expect(slip).toContain('max-w-md')
    expect(slip).toContain('--r:0')
    expect(html).toContain('<div class="kcc-torn"><div class="kcc-sheet kcc-sheet--margin"')
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
    expect(html).toMatch(/<div class="kcc-sheet kcc-sheet--margin"[^>]*>(?:<!--[^>]*-->)*<h2 class="sr-only">Sign in<\/h2>/)
  })

  it('keeps the selectors the sign-in helper types into', async () => {
    const form = formOf(await render())

    expect(tagWith(form, 'name="UserName"')).toMatch(/^<input /)
    expect(tagWith(form, 'name="Password"')).toMatch(/^<input /)

    const submit = tagWith(form, 'type="submit"')
    expect(submit).toMatch(/^<button /)
    expect(submit).toContain('class="kcc-btn w-full"')
  })

  it.each([false, true])('runs the submit button the full width of the form (sign-up: %s)', async (register) => {
    const submit = tagWith(formOf(await render(register)), 'type="submit"')

    expect(submit.match(/class="([^"]*)"/)?.[1]?.split(' ')).toContain('w-full')
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

  it.each([
    ['sign-in', false, ['UserName', 'Password']],
    ['sign-up', true, ['UserName', 'Email', 'Password', 'PasswordConfirmation']],
  ])(
    'starts each %s field on a line of a two-rule box and sets the submit button in a row of two, so the lines after them land on the rule',
    async (_mode, register, names) => {
      const form = formOf(await render(register as boolean))
      const boxes = (names as string[]).map((name) => enclosingTag(form, `name="${name}"`, 1))

      boxes.forEach((box) => expect(classesOf(box)).toEqual(['flex', 'min-h-rule-2', 'flex-col']))
      expect(classesOf(enclosingTag(form, 'type="submit"'))).toEqual(['kcc-row'])
    },
  )

  it('keeps the error well off the sheet until a sign-in fails', async () => {
    const html = await render()

    expect(html).not.toContain('kcc-well')
    expect(html).not.toContain('role="alert"')
  })

  it.each([false, true])('marks no field before the form is sent (sign-up: %s)', async (register) => {
    const form = formOf(await render(register))

    expect(form).not.toContain('kcc-field--error')
    expect(form).not.toContain('aria-invalid')
    expect(form).not.toContain('aria-describedby')
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

    expectNoRetiredMarkup(html)
    expect(html).not.toMatch(/\bbg-paper-2\b/)
  })

  it('opens on the sign-up form when the address asks for it', async () => {
    const html = await render(true)
    const form = formOf(html)

    expect(html).toContain('<h2 class="sr-only">Sign up</h2>')
    expect(form).toContain('name="Email"')
    expect(form).toContain('name="PasswordConfirmation"')
    expect(form).not.toContain('name="RememberMe"')
    expect(switcherOf(html)).toContain('Already have an account?')
  })
})
