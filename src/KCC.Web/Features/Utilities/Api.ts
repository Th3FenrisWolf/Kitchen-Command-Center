export interface ApiStrings {
  unexpectedError: string
  requestFailed: string
}

export interface ApiFailure {
  success: false
  data: null
  errorMessage: string
  status: number
  code: string | null
  body: Record<string, unknown> | null
}

export type ApiResult<T> = { success: true; data: T; errorMessage: null } | ApiFailure

export interface ApiAborted {
  success: false
  data: null
  aborted: true
}

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

// The browser writes the multipart content type itself, with the boundary the body needs.
export async function postForm<T>(url: string, form: FormData, signal: AbortSignal): Promise<ApiResult<T> | ApiAborted> {
  const result = await request<T>(url, { method: 'POST', headers: tokenHeader(), body: form, signal })
  return signal.aborted ? { success: false, data: null, aborted: true } : result
}

function send<T>(method: 'POST' | 'PUT' | 'DELETE', url: string, body?: unknown): Promise<ApiResult<T>> {
  return request<T>(url, {
    method,
    headers: {
      ...(method === 'DELETE' ? {} : { 'Content-Type': 'application/json' }),
      ...tokenHeader(),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
}

function tokenHeader(): Record<string, string> {
  return config.antiforgeryToken ? { RequestVerificationToken: config.antiforgeryToken } : {}
}

async function request<T>(url: string, init: RequestInit): Promise<ApiResult<T>> {
  let response: Response
  try {
    response = await fetch(url, init)
  } catch {
    return fail(0, null, config.strings.unexpectedError, null)
  }

  const body = await readBody(response)

  if (!response.ok || isEnvelopeFailure(body)) {
    return fail(
      response.status,
      codeFrom(body),
      messageFrom(body) ?? config.strings.requestFailed,
      isObject(body) ? body : null,
    )
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

function codeFrom(body: unknown): string | null {
  return isRecord(body) && typeof body.code === 'string' && body.code ? body.code : null
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

const isObject = (value: unknown): value is Record<string, unknown> => isRecord(value) && !Array.isArray(value)

function fail(status: number, code: string | null, errorMessage: string, body: Record<string, unknown> | null): ApiFailure {
  return { success: false, data: null, errorMessage, status, code, body }
}
