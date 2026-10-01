export interface ApiStrings {
  unexpectedError: string
  requestFailed: string
}

export type ApiResult<T> =
  { success: true; data: T; errorMessage: null } | { success: false; data: null; errorMessage: string }

interface ApiConfig {
  antiforgeryToken?: string
  strings: ApiStrings
}

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

// Sign-out posts a real form, which carries the token as a field rather than a header.
export function antiforgeryToken(): string {
  return config.antiforgeryToken ?? ''
}

export function get<T>(url: string, params?: Record<string, string | number>): Promise<ApiResult<T>> {
  const query = params ? `?${new URLSearchParams(Object.entries(params).map(([key, value]) => [key, String(value)]))}` : ''
  return request<T>(`${url}${query}`, { method: 'GET' })
}

export function post<T>(url: string, body?: unknown): Promise<ApiResult<T>> {
  return send<T>('POST', url, body)
}

export function put<T>(url: string, body?: unknown): Promise<ApiResult<T>> {
  return send<T>('PUT', url, body)
}

export function del<T>(url: string): Promise<ApiResult<T>> {
  return send<T>('DELETE', url)
}

function send<T>(method: 'POST' | 'PUT' | 'DELETE', url: string, body?: unknown): Promise<ApiResult<T>> {
  return request<T>(url, {
    method,
    headers: {
      ...(method === 'DELETE' ? {} : { 'Content-Type': 'application/json' }),
      ...(config.antiforgeryToken ? { RequestVerificationToken: config.antiforgeryToken } : {}),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
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

// An empty or non-JSON body, such as a 401 with no content, reads as undefined instead of throwing.
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
