import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { configureApi, get, post } from '~/Utilities/Api'

const STRINGS = { unexpectedError: 'Something went wrong.', requestFailed: 'Request failed.' }

// Minimal stand-in for the parts of Response that Api.ts touches.
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
    global.fetch = vi.fn().mockResolvedValue(
      fakeResponse({ ok: true, body: JSON.stringify({ success: true, redirectUrl: '/home' }) }),
    )

    const result = await post<{ success: boolean; redirectUrl: string }>('/api/account/login', {})

    expect(result.success).toBe(true)
    if (result.success) expect(result.data.redirectUrl).toBe('/home')
  })

  it('treats success:false as a failure and joins the errors array', async () => {
    global.fetch = vi.fn().mockResolvedValue(
      fakeResponse({ ok: true, body: JSON.stringify({ success: false, errors: ['Bad.', 'Worse.'] }) }),
    )

    const result = await post('/api/profile', {})

    expect(result).toEqual({ success: false, data: null, errorMessage: 'Bad. Worse.' })
  })

  it('treats a bare 2xx body with no success field as success', async () => {
    global.fetch = vi.fn().mockResolvedValue(
      fakeResponse({ ok: true, body: JSON.stringify({ recipeId: 7 }) }),
    )

    const result = await post<{ recipeId: number }>('/api/recipes', {})

    expect(result.success).toBe(true)
    if (result.success) expect(result.data.recipeId).toBe(7)
  })

  it('extracts the message from a 4xx { error } body', async () => {
    global.fetch = vi.fn().mockResolvedValue(
      fakeResponse({ ok: false, status: 400, body: JSON.stringify({ error: 'Name is required.' }) }),
    )

    const result = await post('/api/recipes', {})

    expect(result).toEqual({ success: false, data: null, errorMessage: 'Name is required.' })
  })

  it('falls back to requestFailed when the error body is empty (401)', async () => {
    global.fetch = vi.fn().mockResolvedValue(fakeResponse({ ok: false, status: 401, body: '' }))

    const result = await post('/api/recipes', {})

    expect(result.success).toBe(false)
    expect(result.errorMessage).toBe(STRINGS.requestFailed)
  })

  it('falls back to unexpectedError when fetch rejects', async () => {
    global.fetch = vi.fn().mockRejectedValue(new Error('network down'))

    const result = await post('/api/recipes', {})

    expect(result.success).toBe(false)
    expect(result.errorMessage).toBe(STRINGS.unexpectedError)
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

  it('serializes get params via qs into the query string', async () => {
    const fetchMock = vi.fn().mockResolvedValue(fakeResponse({ ok: true, body: '[]' }))
    global.fetch = fetchMock

    await get('/api/search', { q: 'mac', page: 2 })

    const [url, init] = fetchMock.mock.calls[0]!
    expect(url).toBe('/api/search?q=mac&page=2')
    expect(init.method).toBe('GET')
  })
})
