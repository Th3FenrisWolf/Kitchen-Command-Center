import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { antiforgeryToken, configureApi, del, get, post, postForm, put } from '~/Utilities/Api'

const STRINGS = { unexpectedError: 'Something went wrong.', requestFailed: 'Request failed.' }

function fakeResponse(opts: { ok: boolean; status?: number; body?: string }): Response {
  return {
    ok: opts.ok,
    status: opts.status ?? (opts.ok ? 200 : 400),
    text: () => Promise.resolve(opts.body ?? ''),
  } as unknown as Response
}

describe('Api', () => {
  beforeEach(() => {
    configureApi({ antiforgeryToken: 'tok', strings: STRINGS })
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('returns success with parsed data for a 2xx success envelope', async () => {
    global.fetch = vi
      .fn()
      .mockResolvedValue(fakeResponse({ ok: true, body: JSON.stringify({ success: true, redirectUrl: '/home' }) }))

    const result = await post<{ success: boolean; redirectUrl: string }>('/api/account/login', {})

    expect(result.success).toBe(true)
    if (result.success) expect(result.data.redirectUrl).toBe('/home')
  })

  it('treats success:false as a failure and joins the errors array', async () => {
    global.fetch = vi
      .fn()
      .mockResolvedValue(fakeResponse({ ok: true, body: JSON.stringify({ success: false, errors: ['Bad.', 'Worse.'] }) }))

    const result = await post('/api/profile', {})

    expect(result).toEqual({
      success: false,
      data: null,
      errorMessage: 'Bad. Worse.',
      status: 200,
      code: null,
      body: { success: false, errors: ['Bad.', 'Worse.'] },
    })
  })

  it('treats a bare 2xx body with no success field as success', async () => {
    global.fetch = vi.fn().mockResolvedValue(fakeResponse({ ok: true, body: JSON.stringify({ recipeId: 7 }) }))

    const result = await post<{ recipeId: number }>('/api/recipes', {})

    expect(result.success).toBe(true)
    if (result.success) expect(result.data.recipeId).toBe(7)
  })

  it('extracts the message from a 4xx { error } body', async () => {
    global.fetch = vi
      .fn()
      .mockResolvedValue(fakeResponse({ ok: false, status: 400, body: JSON.stringify({ error: 'Name is required.' }) }))

    const result = await post('/api/recipes', {})

    expect(result).toEqual({
      success: false,
      data: null,
      errorMessage: 'Name is required.',
      status: 400,
      code: null,
      body: { error: 'Name is required.' },
    })
  })

  it('carries the code a failure body names beside its status', async () => {
    global.fetch = vi.fn().mockResolvedValue(
      fakeResponse({
        ok: false,
        status: 409,
        body: JSON.stringify({ error: 'Resting.', code: 'ceilingReached', availableAgainOn: '2026-11-01' }),
      }),
    )

    const result = await post('/api/recipe-imports/read', {})

    expect(result).toMatchObject({
      success: false,
      data: null,
      errorMessage: 'Resting.',
      status: 409,
      code: 'ceilingReached',
    })
  })

  it('carries the parsed body of a failure when it is a JSON object', async () => {
    const refusal = { error: 'Resting.', code: 'ceilingReached', availableAgainOn: '2026-11-01' }
    global.fetch = vi.fn().mockResolvedValue(fakeResponse({ ok: false, status: 409, body: JSON.stringify(refusal) }))

    const result = await postForm('/api/recipe-imports/read', new FormData(), new AbortController().signal)

    expect(result).toMatchObject({ success: false, status: 409, body: refusal })
  })

  it.each([
    ['an empty body', ''],
    ['a page that is not JSON', '<html>Bad gateway</html>'],
    ['a JSON array', '["Bad."]'],
    ['a JSON string', '"Bad."'],
    ['JSON null', 'null'],
  ])('carries a null body for a failure with %s', async (_kind, body) => {
    global.fetch = vi.fn().mockResolvedValue(fakeResponse({ ok: false, status: 502, body }))

    const result = await post('/api/recipes', {})

    expect(result).toMatchObject({ success: false, status: 502, body: null })
  })

  it('carries a null body for a request that got no answer', async () => {
    global.fetch = vi.fn().mockRejectedValue(new TypeError('Failed to fetch'))

    expect(await post('/api/recipes', {})).toMatchObject({ success: false, status: 0, body: null })
  })

  it('carries the status of a failed get, put and delete', async () => {
    global.fetch = vi.fn().mockResolvedValue(fakeResponse({ ok: false, status: 503, body: '' }))

    const results = [await get('/api/x'), await put('/api/x', {}), await del('/api/x')]

    results.forEach((result) => expect(result).toMatchObject({ success: false, status: 503, code: null }))
  })

  it('falls back to requestFailed when the error body is empty (401)', async () => {
    global.fetch = vi.fn().mockResolvedValue(fakeResponse({ ok: false, status: 401, body: '' }))

    const result = await post('/api/recipes', {})

    expect(result.success).toBe(false)
    expect(result.errorMessage).toBe(STRINGS.requestFailed)
    expect(result).toMatchObject({ status: 401, code: null })
  })

  it('falls back to unexpectedError, with status 0, when fetch rejects', async () => {
    global.fetch = vi.fn().mockRejectedValue(new Error('network down'))

    const result = await post('/api/recipes', {})

    expect(result.success).toBe(false)
    expect(result.errorMessage).toBe(STRINGS.unexpectedError)
    expect(result).toMatchObject({ status: 0, code: null })
  })

  it('sends the RequestVerificationToken header and JSON body on post', async () => {
    const fetchMock = vi.fn().mockResolvedValue(fakeResponse({ ok: true, body: '{}' }))
    global.fetch = fetchMock

    await post('/api/x', { a: 1 })

    const [url, init] = fetchMock.mock.calls[0]!
    expect(url).toBe('/api/x')
    expect(init.method).toBe('POST')
    expect(init.headers['Content-Type']).toBe('application/json')
    expect(init.headers['RequestVerificationToken']).toBe('tok')
    expect(init.body).toBe(JSON.stringify({ a: 1 }))
  })

  it('omits the token header when no token is configured', async () => {
    configureApi({ strings: STRINGS })
    const fetchMock = vi.fn().mockResolvedValue(fakeResponse({ ok: true, body: '{}' }))
    global.fetch = fetchMock

    await post('/api/x', {})

    const [, init] = fetchMock.mock.calls[0]!
    expect(init.headers['RequestVerificationToken']).toBeUndefined()
  })

  it('sends the token header on put', async () => {
    const fetchMock = vi.fn().mockResolvedValue(fakeResponse({ ok: true, body: '{}' }))
    global.fetch = fetchMock
    await put('/api/variant/x/review', { rating: 5 })
    const [, init] = fetchMock.mock.calls[0]!
    expect(init.method).toBe('PUT')
    expect(init.headers['RequestVerificationToken']).toBe('tok')
  })

  it('sends the token header on delete', async () => {
    const fetchMock = vi.fn().mockResolvedValue(fakeResponse({ ok: true, body: '{}' }))
    global.fetch = fetchMock
    await del('/api/variant/x/review')
    const [, init] = fetchMock.mock.calls[0]!
    expect(init.method).toBe('DELETE')
    expect(init.headers['RequestVerificationToken']).toBe('tok')
  })

  it('serializes get params into the query string', async () => {
    const fetchMock = vi.fn().mockResolvedValue(fakeResponse({ ok: true, body: '[]' }))
    global.fetch = fetchMock

    await get('/api/search', { q: 'mac', page: 2 })

    const [url, init] = fetchMock.mock.calls[0]!
    expect(url).toBe('/api/search?q=mac&page=2')
    expect(init.method).toBe('GET')
  })

  it('hands a native form the configured anti-forgery token, or nothing before one is configured', () => {
    expect(antiforgeryToken()).toBe('tok')

    configureApi({})

    expect(antiforgeryToken()).toBe('')
  })
})

describe('postForm', () => {
  beforeEach(() => {
    configureApi({ antiforgeryToken: 'tok', strings: STRINGS })
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  const photos = () => {
    const form = new FormData()
    form.append('flow', 'recipe')
    return form
  }

  const fetchUntilAborted = (_url: string, init: RequestInit) =>
    new Promise<Response>((_resolve, reject) =>
      init.signal!.addEventListener('abort', () => reject(new DOMException('The operation was aborted.', 'AbortError'))),
    )

  it('posts the form as it is, with the token header and no JSON content type, under the caller signal', async () => {
    const fetchMock = vi.fn().mockResolvedValue(fakeResponse({ ok: true, body: '{}' }))
    global.fetch = fetchMock
    const form = photos()
    const controller = new AbortController()

    await postForm('/api/recipe-imports/read', form, controller.signal)

    const [url, init] = fetchMock.mock.calls[0]!
    expect(url).toBe('/api/recipe-imports/read')
    expect(init.method).toBe('POST')
    expect(init.headers['RequestVerificationToken']).toBe('tok')
    expect(init.headers['Content-Type']).toBeUndefined()
    expect(init.body).toBe(form)
    expect(init.signal).toBe(controller.signal)
  })

  it('returns the parsed answer', async () => {
    global.fetch = vi.fn().mockResolvedValue(fakeResponse({ ok: true, body: JSON.stringify({ outcome: 'noRecipe' }) }))

    const result = await postForm<{ outcome: string }>('/api/recipe-imports/read', photos(), new AbortController().signal)

    expect(result).toEqual({ success: true, data: { outcome: 'noRecipe' }, errorMessage: null })
  })

  it('carries the status and code of a refused form', async () => {
    global.fetch = vi
      .fn()
      .mockResolvedValue(
        fakeResponse({ ok: false, status: 400, body: JSON.stringify({ error: 'Bad photos.', code: 'invalidPhotos' }) }),
      )

    const result = await postForm('/api/recipe-imports/read', photos(), new AbortController().signal)

    expect(result).toEqual({
      success: false,
      data: null,
      errorMessage: 'Bad photos.',
      status: 400,
      code: 'invalidPhotos',
      body: { error: 'Bad photos.', code: 'invalidPhotos' },
    })
  })

  it('gives an aborted result, not a failure, when the caller aborts', async () => {
    global.fetch = vi.fn(fetchUntilAborted)
    const controller = new AbortController()

    const pending = postForm('/api/recipe-imports/read', photos(), controller.signal)
    controller.abort()

    expect(await pending).toEqual({ success: false, data: null, aborted: true })
  })

  it('gives the aborted result when the answer lands after the caller aborted', async () => {
    const controller = new AbortController()
    global.fetch = vi.fn(async () => {
      controller.abort()
      return fakeResponse({ ok: true, body: JSON.stringify({ outcome: 'recipe' }) })
    })

    const result = await postForm('/api/recipe-imports/read', photos(), controller.signal)

    expect(result).toEqual({ success: false, data: null, aborted: true })
  })

  it('reports a request that got no answer as a failure with status 0', async () => {
    global.fetch = vi.fn().mockRejectedValue(new TypeError('Failed to fetch'))

    const result = await postForm('/api/recipe-imports/read', photos(), new AbortController().signal)

    expect(result).toEqual({
      success: false,
      data: null,
      errorMessage: STRINGS.unexpectedError,
      status: 0,
      code: null,
      body: null,
    })
  })
})
