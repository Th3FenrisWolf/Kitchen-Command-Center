import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { configureApi, post, postForm, type ApiFailure } from '~/Utilities/Api'
import { readErrorKey, restingUntil, submitErrorKey } from '~/Components/RecipeWizard/wizardErrors'

const answer = (status: number, body: unknown = undefined) =>
  ({
    ok: status >= 200 && status < 300,
    status,
    text: () => Promise.resolve(body === undefined ? '' : JSON.stringify(body)),
  }) as unknown as Response

async function failedSubmission(respond: () => Promise<Response>): Promise<ApiFailure> {
  vi.stubGlobal('fetch', vi.fn(respond))
  const result = await post('/api/recipes', {})
  if (result.success) throw new Error('The submission was expected to fail.')
  return result
}

describe('submitErrorKey', () => {
  beforeEach(() => {
    configureApi({ antiforgeryToken: 'tok' })
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each([
    [400, { error: 'Recipe name is required.' }, 'SubmitFailed'],
    [401, undefined, 'SignInAgain'],
    [404, { error: 'Recipe not found.' }, 'RecipeNotFound'],
    [429, { error: 'Too many attempts. Try again later.' }, 'TooManyAttempts'],
  ])('maps a %i to %s', async (status, body, key) => {
    const failure = await failedSubmission(() => Promise.resolve(answer(status, body)))

    expect(submitErrorKey(failure)).toBe(key)
  })

  it.each([403, 409, 500, 502, 503])('maps any other status, such as %i, to UnexpectedError', async (status) => {
    const failure = await failedSubmission(() => Promise.resolve(answer(status)))

    expect(submitErrorKey(failure)).toBe('UnexpectedError')
  })

  it('maps a submission that got no answer to UnexpectedError', async () => {
    const failure = await failedSubmission(() => Promise.reject(new TypeError('Failed to fetch')))

    expect(failure.status).toBe(0)
    expect(submitErrorKey(failure)).toBe('UnexpectedError')
  })

  it('maps a success envelope that reports a failure to UnexpectedError', async () => {
    const failure = await failedSubmission(() => Promise.resolve(answer(200, { success: false, errors: ['No.'] })))

    expect(submitErrorKey(failure)).toBe('UnexpectedError')
  })
})

async function failedRead(respond: () => Promise<Response>): Promise<ApiFailure> {
  vi.stubGlobal('fetch', vi.fn(respond))
  const result = await postForm('/api/recipe-imports/read', new FormData(), new AbortController().signal)
  if (result.success || 'aborted' in result) throw new Error('The read was expected to fail.')
  return result
}

describe('readErrorKey', () => {
  beforeEach(() => {
    configureApi({ antiforgeryToken: 'tok' })
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each([
    [400, { error: 'The photos could not be used.', code: 'invalidPhotos' }, 'PhotosRejected'],
    [400, { error: 'The flow is not valid.', code: 'invalidRequest' }, 'ReadFailed'],
    [400, { error: 'The request is not valid.' }, 'ReadFailed'],
    [401, undefined, 'SignInAgain'],
    [404, { error: 'Recipe not found.', code: 'recipeNotFound' }, 'RecipeNotFound'],
    [429, { error: 'Too many attempts. Try again later.' }, 'TooManyAttempts'],
    [502, { error: 'The read failed.', code: 'readFailed' }, 'ReadFailed'],
    [503, { error: 'Photo import is off.', code: 'unavailable' }, 'ImportOff'],
    [504, { error: 'The read timed out.', code: 'readTimedOut' }, 'ReadTimedOut'],
  ])('maps a %i with %j to %s', async (status, body, key) => {
    const failure = await failedRead(() => Promise.resolve(answer(status, body)))

    expect(readErrorKey(failure)).toBe(key)
  })

  it('maps a 409 ceilingReached to ImportUnavailable, with the availableAgainOn of its body', async () => {
    const failure = await failedRead(() =>
      Promise.resolve(
        answer(409, { error: 'Photo import is resting.', code: 'ceilingReached', availableAgainOn: '2026-11-01' }),
      ),
    )

    expect(readErrorKey(failure)).toBe('ImportUnavailable')
    expect(restingUntil(failure)).toBe('2026-11-01')
  })

  it('maps a 409 with no code to ReadFailed', async () => {
    const failure = await failedRead(() => Promise.resolve(answer(409, { error: 'Conflict.' })))

    expect(readErrorKey(failure)).toBe('ReadFailed')
  })

  it.each([
    [409, { error: 'Conflict.', availableAgainOn: '2026-11-01' }],
    [409, { error: 'Photo import is resting.', code: 'ceilingReached' }],
    [503, { error: 'Photo import is off.', code: 'unavailable' }],
  ])('gives no availableAgainOn for a %i with %j', async (status, body) => {
    const failure = await failedRead(() => Promise.resolve(answer(status, body)))

    expect(restingUntil(failure)).toBeNull()
  })

  it('maps a read that got no answer to ReadFailed', async () => {
    const failure = await failedRead(() => Promise.reject(new TypeError('Failed to fetch')))

    expect(failure.status).toBe(0)
    expect(readErrorKey(failure)).toBe('ReadFailed')
  })

  it.each([
    [500, undefined],
    [502, undefined],
    [503, { error: 'Service unavailable.' }],
    [504, undefined],
    [403, undefined],
  ])('maps any other failure, such as a %i with %j, to ReadFailed', async (status, body) => {
    const failure = await failedRead(() => Promise.resolve(answer(status, body)))

    expect(readErrorKey(failure)).toBe('ReadFailed')
  })

  it('maps a 502 page from the proxy, which carries no code, to ReadFailed', async () => {
    const proxyPage = { ok: false, status: 502, text: () => Promise.resolve('<html>Bad gateway</html>') }

    const failure = await failedRead(() => Promise.resolve(proxyPage as unknown as Response))

    expect(failure).toMatchObject({ status: 502, code: null })
    expect(readErrorKey(failure)).toBe('ReadFailed')
  })
})
