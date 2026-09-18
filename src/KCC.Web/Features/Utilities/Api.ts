import qs from 'qs'

export interface ApiStrings {
  /** Shown when the request never completes (network error, abort). */
  unexpectedError: string
  /** Shown when the server fails but provides no usable message (e.g. an empty 401 body). */
  requestFailed: string
}

export type ApiResult<T> =
  { success: true; data: T; errorMessage: null } | { success: false; data: null; errorMessage: string }

interface ApiConfig {
  antiforgeryToken?: string
  strings: ApiStrings
}

// English defaults, used until configureApi() runs (or when a seeded string is
// blank). Main.ts calls configureApi() once at mount with the localized values.
let config: ApiConfig = {
  strings: {
    unexpectedError: 'An unexpected error occurred. Please try again.',
    requestFailed: 'The request could not be completed.',
  },
}

export function configureApi(next: { antiforgeryToken?: string; strings?: Partial<ApiStrings> }): void {
  config = {
    antiforgeryToken: next.antiforgeryToken,
    strings: {
      // `||` so a blank seeded value falls back to the English default.
      unexpectedError: next.strings?.unexpectedError || config.strings.unexpectedError,
      requestFailed: next.strings?.requestFailed || config.strings.requestFailed,
    },
  }
}

export function get<T>(url: string, params?: Record<string, unknown>): Promise<ApiResult<T>> {
  const query = params ? `?${qs.stringify(params)}` : ''
  return request<T>(`${url}${query}`, { method: 'GET' })
}

export function post<T>(url: string, body?: unknown): Promise<ApiResult<T>> {
  return request<T>(url, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...(config.antiforgeryToken ? { RequestVerificationToken: config.antiforgeryToken } : {}),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
}

export function put<T>(url: string, body?: unknown): Promise<ApiResult<T>> {
  return request<T>(url, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      ...(config.antiforgeryToken ? { RequestVerificationToken: config.antiforgeryToken } : {}),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
}

export function del<T>(url: string): Promise<ApiResult<T>> {
  return request<T>(url, {
    method: 'DELETE',
    headers: {
      ...(config.antiforgeryToken ? { RequestVerificationToken: config.antiforgeryToken } : {}),
    },
  })
}

async function request<T>(url: string, init: RequestInit): Promise<ApiResult<T>> {
  let response: Response
  try {
    response = await fetch(url, init)
  } catch {
    return fail(config.strings.unexpectedError)
  }

  const body = await readBody(response)

  if (!response.ok || isEnvelopeFailure(body)) {
    return fail(messageFrom(body) ?? config.strings.requestFailed)
  }

  return { success: true, data: (body ?? null) as T, errorMessage: null }
}

// Reads the body as text then JSON-parses defensively: an empty or non-JSON
// body (e.g. a 401 Unauthorized with no content) yields undefined rather than
// throwing — the trap the old `await response.json()` calls fell into.
async function readBody(response: Response): Promise<unknown> {
  const text = await response.text().catch(() => '')
  if (!text) return undefined
  try {
    return JSON.parse(text)
  } catch {
    return undefined
  }
}

// A 2xx envelope can still mark business failure with success: false.
function isEnvelopeFailure(body: unknown): boolean {
  return isRecord(body) && body.success === false
}

// Pulls a message from { errors: string[] } or { error: string }; null if neither.
function messageFrom(body: unknown): string | null {
  if (!isRecord(body)) return null
  if (Array.isArray(body.errors)) {
    const joined = body.errors.filter((e): e is string => typeof e === 'string').join(' ')
    if (joined) return joined
  }
  if (typeof body.error === 'string' && body.error) return body.error
  return null
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function fail(errorMessage: string): { success: false; data: null; errorMessage: string } {
  return { success: false, data: null, errorMessage }
}
