import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useLoginForm } from '~/Pages/Account/Login/useLoginForm'

const answer = (ok: boolean, body: unknown) => ({
  ok,
  status: ok ? 200 : 400,
  text: () => Promise.resolve(JSON.stringify(body)),
})

function serve(response: ReturnType<typeof answer>) {
  const fetch = vi.fn().mockResolvedValue(response)
  vi.stubGlobal('fetch', fetch)
  return fetch
}

const refusal = (error: string, code?: string) =>
  answer(true, { success: false, errors: [error], redirectUrl: null, code: code ?? null })

function signIn() {
  const form = useLoginForm({})
  form.userName.value = 'cook'
  form.password.value = 'not-the-password'
  return form
}

function signUp(confirmation = 'Pa55word!') {
  const form = useLoginForm({ register: true })
  form.userName.value = 'cook'
  form.email.value = 'cook@example.test'
  form.password.value = 'Pa55word!'
  form.passwordConfirmation.value = confirmation
  return form
}

beforeEach(() => {
  vi.stubGlobal('window', { location: { href: '/account/login' } })
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('useLoginForm', () => {
  it('marks the name and the password when the credentials are wrong', async () => {
    serve(refusal('That username and password do not match.', 'invalid-credentials'))
    const form = signIn()

    await form.handleSubmit()

    expect(form.formError.value).toBe('That username and password do not match.')
    expect(form.fieldsInError.value).toEqual(['UserName', 'Password'])
  })

  it.each([
    ['an account waiting for approval', 'Your account is waiting for approval.'],
    ['a locked-out account', 'Too many attempts. Try again later.'],
  ])('marks no field for %s, only the alert says so', async (_case, message) => {
    serve(refusal(message))
    const form = signIn()

    await form.handleSubmit()

    expect(form.formError.value).toBe(message)
    expect(form.fieldsInError.value).toEqual([])
  })

  it('marks no field when the server cannot be reached', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('offline')))
    const form = signIn()

    await form.handleSubmit()

    expect(form.formError.value).not.toBeNull()
    expect(form.fieldsInError.value).toEqual([])
  })

  it('marks both password fields when they do not match, without sending anything', async () => {
    const fetch = serve(answer(true, { success: true }))
    const form = signUp('Pa55word?')

    await form.handleSubmit()

    expect(fetch).not.toHaveBeenCalled()
    expect(form.formError.value).toBe('Passwords do not match.')
    expect(form.fieldsInError.value).toEqual(['Password', 'PasswordConfirmation'])
  })

  it('marks no field for an error the sign-up server reports', async () => {
    serve(refusal("Username 'cook' is already taken."))
    const form = signUp()

    await form.handleSubmit()

    expect(form.formError.value).toBe("Username 'cook' is already taken.")
    expect(form.fieldsInError.value).toEqual([])
  })

  it('clears the marks when the form is sent again', async () => {
    serve(refusal('That username and password do not match.', 'invalid-credentials'))
    const form = signIn()
    await form.handleSubmit()

    serve(refusal('Your account is waiting for approval.'))
    await form.handleSubmit()

    expect(form.fieldsInError.value).toEqual([])
  })

  it('clears the error and the marks when the mode switches', async () => {
    serve(refusal('That username and password do not match.', 'invalid-credentials'))
    const form = signIn()
    await form.handleSubmit()

    form.switchMode()

    expect(form.formError.value).toBeNull()
    expect(form.fieldsInError.value).toEqual([])
  })

  it('goes where the server points once the sign-in succeeds', async () => {
    serve(answer(true, { success: true, redirectUrl: '/recipes/' }))
    const form = signIn()

    await form.handleSubmit()

    expect(window.location.href).toBe('/recipes/')
    expect(form.fieldsInError.value).toEqual([])
  })
})
