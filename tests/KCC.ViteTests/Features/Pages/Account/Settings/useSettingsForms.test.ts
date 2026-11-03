import { afterEach, describe, expect, it, vi } from 'vitest'
import { usePasswordForm, useProfileForm } from '~/Pages/Account/Settings/useSettingsForms'

const answer = (ok: boolean, body: unknown, status = ok ? 200 : 400) => ({
  ok,
  status,
  text: () => Promise.resolve(body === undefined ? '' : JSON.stringify(body)),
})

function serve(response: ReturnType<typeof answer>) {
  const fetch = vi.fn().mockResolvedValue(response)
  vi.stubGlobal('fetch', fetch)
  return fetch
}

const refusal = (error: string, code?: string) => answer(true, { success: false, errors: [error], code: code ?? null })

const rs = (key: string) => `«${key}»`

function profile(firstName: string, lastName: string) {
  const form = useProfileForm({ firstName: 'Ada', lastName: 'Lovelace' }, rs)
  form.firstName.value = firstName
  form.lastName.value = lastName
  return form
}

function password(current: string, next: string, confirmation = next) {
  const form = usePasswordForm(rs)
  form.currentPassword.value = current
  form.newPassword.value = next
  form.confirmPassword.value = confirmation
  return form
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('useProfileForm', () => {
  it.each([
    ['the first name', ' ', 'Lovelace', ['FirstName']],
    ['the last name', 'Ada', '', ['LastName']],
    ['both names', '', ' ', ['FirstName', 'LastName']],
  ])('marks only %s when the name is required and was sent blank', async (_case, first, last, fields) => {
    serve(refusal('Please enter your name.', 'name-required'))
    const form = profile(first, last)

    await form.save()

    expect(form.message.value).toEqual({ ok: false, text: 'Please enter your name.' })
    expect(form.fieldsInError.value).toEqual(fields)
  })

  it('marks no field when the refusal is about the account, only the alert says so', async () => {
    serve(answer(false, undefined, 401))
    const form = profile('', 'Lovelace')

    await form.save()

    expect(form.message.value?.ok).toBe(false)
    expect(form.fieldsInError.value).toEqual([])
  })

  it('clears the marks at the next save', async () => {
    serve(refusal('Please enter your name.', 'name-required'))
    const form = profile('', 'Lovelace')
    await form.save()

    serve(answer(true, { success: true, errors: null }))
    form.firstName.value = 'Ada'
    await form.save()

    expect(form.message.value).toEqual({ ok: true, text: '«ProfileSaved»' })
    expect(form.fieldsInError.value).toEqual([])
  })
})

describe('usePasswordForm', () => {
  it('marks the new password and its confirmation when they do not match, without asking the server', async () => {
    const fetch = serve(answer(true, { success: true, errors: null }))
    const form = password('Old-Passw0rd', 'New-Passw0rd', 'New-Passw0rd!')

    await form.change()

    expect(fetch).not.toHaveBeenCalled()
    expect(form.message.value).toEqual({ ok: false, text: '«PasswordsDoNotMatch»' })
    expect(form.fieldsInError.value).toEqual(['NewPassword', 'ConfirmPassword'])
  })

  it('marks the current password when it is wrong', async () => {
    serve(refusal('Incorrect password.', 'wrong-password'))
    const form = password('not-my-password', 'New-Passw0rd')

    await form.change()

    expect(form.message.value).toEqual({ ok: false, text: 'Incorrect password.' })
    expect(form.fieldsInError.value).toEqual(['CurrentPassword'])
  })

  it('marks only the new password when the policy refuses it', async () => {
    serve(refusal('Passwords must be at least 10 characters.', 'weak-password'))
    const form = password('Old-Passw0rd', 'Short7!')

    await form.change()

    expect(form.fieldsInError.value).toEqual(['NewPassword'])
  })

  it.each([
    ['the current password', ' ', 'New-Passw0rd', ['CurrentPassword']],
    ['the new password', 'Old-Passw0rd', ' ', ['NewPassword']],
  ])('marks only %s when a password is required and was sent blank', async (_case, current, next, fields) => {
    serve(refusal('Please enter your password.', 'password-required'))
    const form = password(current, next)

    await form.change()

    expect(form.fieldsInError.value).toEqual(fields)
  })

  it('marks no field when the refusal is about the account, only the alert says so', async () => {
    serve(answer(false, { error: 'Too many attempts. Try again later.' }, 429))
    const form = password('Old-Passw0rd', 'New-Passw0rd')

    await form.change()

    expect(form.message.value).toEqual({ ok: false, text: 'Too many attempts. Try again later.' })
    expect(form.fieldsInError.value).toEqual([])
  })

  it('clears every password and every mark once the change is saved', async () => {
    serve(refusal('Incorrect password.', 'wrong-password'))
    const form = password('not-my-password', 'New-Passw0rd')
    await form.change()

    serve(answer(true, { success: true, errors: null }))
    form.currentPassword.value = 'Old-Passw0rd'
    await form.change()

    expect(form.message.value).toEqual({ ok: true, text: '«PasswordUpdated»' })
    expect(form.fieldsInError.value).toEqual([])
    expect([form.currentPassword.value, form.newPassword.value, form.confirmPassword.value]).toEqual(['', '', ''])
  })
})
