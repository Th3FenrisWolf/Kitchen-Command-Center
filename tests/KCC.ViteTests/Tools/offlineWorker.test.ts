import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { afterEach, describe, expect, it, vi } from 'vitest'
// @ts-expect-error plain JavaScript with no declarations
import { render } from '../../../tools/offline-page/build.mjs'

type Worker = { fetch(request: Request, env: Record<string, string>): Promise<Response> }

const { worker: source } = render() as { worker: string }
const { default: worker } = (await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`)) as {
  default: Worker
}

const pageRequest = (path = '/') =>
  new Request(`https://kitchen.example.com${path}`, { headers: { Accept: 'text/html,application/xhtml+xml' } })

function originAnswers(response: Response | Error) {
  vi.stubGlobal(
    'fetch',
    vi.fn(async () => {
      if (response instanceof Error) {
        throw response
      }
      return response
    }),
  )
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('offline worker', () => {
  it('passes the live site through unchanged', async () => {
    originAnswers(new Response('live', { status: 200 }))
    const response = await worker.fetch(pageRequest(), {})
    expect(response.status).toBe(200)
    expect(await response.text()).toBe('live')
  })

  it("passes the site's own errors through", async () => {
    originAnswers(new Response('missing', { status: 404 }))
    expect((await worker.fetch(pageRequest('/nope'), {})).status).toBe(404)
  })

  it('passes a redirect through', async () => {
    originAnswers(new Response(null, { status: 302, headers: { Location: '/login' } }))
    expect((await worker.fetch(pageRequest('/account'), {})).status).toBe(302)
  })

  it.each([502, 503, 504, 520, 521, 522, 523, 524, 525, 526, 530])(
    'serves the offline page when the origin answers %i',
    async (status) => {
      originAnswers(new Response('', { status }))
      const response = await worker.fetch(pageRequest(), {})
      expect(response.status).toBe(503)
      expect(response.headers.get('Cache-Control')).toBe('no-store')
      expect(await response.text()).toContain('Back soon')
    },
  )

  it('answers a HEAD request with no body', async () => {
    originAnswers(new Response('', { status: 530 }))
    const response = await worker.fetch(new Request('https://kitchen.example.com/', { method: 'HEAD' }), {})
    expect(response.status).toBe(503)
  })

  it.each([502, 530])(
    'turns a write away when the origin answers %i, before the request reached the app',
    async (status) => {
      originAnswers(new Response('', { status }))
      const write = new Request('https://kitchen.example.com/api/review', { method: 'POST', body: '{}' })
      expect((await worker.fetch(write, {})).status).toBe(503)
    },
  )

  it.each([503, 504, 524])(
    'passes a write through when the origin answers %i, as the app may have committed it',
    async (status) => {
      originAnswers(new Response('timed out', { status }))
      const write = new Request('https://kitchen.example.com/api/review', { method: 'POST', body: '{}' })
      const response = await worker.fetch(write, {})
      expect(response.status).toBe(status)
      expect(await response.text()).toBe('timed out')
    },
  )

  it('serves the offline page when the origin cannot be reached', async () => {
    originAnswers(new Error('connection refused'))
    expect(await (await worker.fetch(pageRequest(), {})).text()).toContain('Back soon')
  })

  it('answers a request for anything but a page in plain text', async () => {
    originAnswers(new Response('', { status: 530 }))
    const request = new Request('https://kitchen.example.com/media/a.jpg', { headers: { Accept: 'image/*' } })
    const response = await worker.fetch(request, {})
    expect(response.status).toBe(503)
    expect(response.headers.get('Content-Type')).toContain('text/plain')
  })

  it('serves the offline page without asking the origin while MAINTENANCE is true', async () => {
    originAnswers(new Response('live', { status: 200 }))
    const response = await worker.fetch(pageRequest(), { MAINTENANCE: 'true' })
    expect(response.status).toBe(503)
    expect(fetch).not.toHaveBeenCalled()
  })

  it('passes through while MAINTENANCE is false', async () => {
    originAnswers(new Response('live', { status: 200 }))
    expect((await worker.fetch(pageRequest(), { MAINTENANCE: 'false' })).status).toBe(200)
  })

  it.each(['nope.woff2', 'constructor', '__proto__', 'toString'])('answers 404 for the font %s', async (name) => {
    const response = await worker.fetch(new Request(`https://kitchen.example.com/__kcc-offline/${name}`), {})
    expect(response.status).toBe(404)
  })

  it('serves the brand fonts the page links', async () => {
    originAnswers(new Error('unreachable'))
    const html = await (await worker.fetch(pageRequest(), {})).text()
    const fonts = [...html.matchAll(/url\('(\/__kcc-offline\/[\w.-]+)'\)/g)].map(([, path]) => path!)
    expect(fonts).toHaveLength(3)

    for (const path of fonts) {
      const response = await worker.fetch(new Request(`https://kitchen.example.com${path}`), {})
      expect(response.headers.get('Content-Type')).toBe('font/woff2')
      expect(new Uint8Array(await response.arrayBuffer()).subarray(0, 4)).toEqual(new Uint8Array([0x77, 0x4f, 0x46, 0x32]))
    }
  })
})

// The page carries its own copy of the ramps, because the site's stylesheet is on the Pi.
describe('offline page colours', () => {
  const styles = new URL('../../../src/KCC.Web/Features/Styles/', import.meta.url)
  const read = (path: string) => readFileSync(fileURLToPath(new URL(path, styles)), 'utf8')
  const page = readFileSync(fileURLToPath(new URL('../../../tools/offline-page/offline.html', import.meta.url)), 'utf8')

  const tokens = (css: string) => new Map([...css.matchAll(/--color-([a-z0-9-]+):\s*([^;]+);/g)].map(([, n, v]) => [n!, v!]))
  const block = (css: string, opener: string) => {
    const at = css.indexOf(opener)
    if (at < 0) {
      throw new Error(`no ${opener} block`)
    }
    const start = css.indexOf('{', at) + 1
    let depth = 1
    let end = start
    while (depth > 0) {
      if (end >= css.length) {
        throw new Error(`the ${opener} block never closes`)
      }
      depth += css[end] === '{' ? 1 : css[end] === '}' ? -1 : 0
      end++
    }
    return css.slice(start, end - 1)
  }
  const light = tokens(read('TailwindConfig.css'))
  const dark = new Map([...light, ...tokens(block(read('Torn/Tokens.css'), "data-theme='dark'"))])

  it.each([
    ['light', block(page, ':root {'), light],
    ['dark', block(page, '@media (prefers-color-scheme: dark)'), dark],
  ])('matches the site %s ramp', (_, pageCss, site) => {
    const own = tokens(pageCss)
    expect(own.size).toBeGreaterThan(0)
    for (const [name, value] of own) {
      expect(value, `--color-${name}`).toBe(site.get(name))
    }
  })
})
