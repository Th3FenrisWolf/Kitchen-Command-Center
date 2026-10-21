// Cloudflare stands in for the Pi when the tunnel cannot reach it: 530 is error 1033, the tunnel is down.
const ORIGIN_DOWN = new Set([502, 503, 504, 520, 521, 522, 523, 524, 525, 526, 530])
// A write that timed out (524) or lost its connection may already have committed, so a write sees the offline page
// only when the request never reached the app.
const ORIGIN_UNREACHED = new Set([502, 530])
const READS = new Set(['GET', 'HEAD'])
const FONT_PREFIX = '/__kcc-offline/'

const PAGE = /*PAGE*/ ''
const FONTS = /*FONTS*/ {}

export default {
  async fetch(request, env) {
    const url = new URL(request.url)
    if (url.pathname.startsWith(FONT_PREFIX)) {
      return font(url.pathname.slice(FONT_PREFIX.length))
    }

    if (env.MAINTENANCE === 'true') {
      return offline(request)
    }

    try {
      const response = await fetch(request)
      const down = READS.has(request.method) ? ORIGIN_DOWN : ORIGIN_UNREACHED
      return down.has(response.status) ? offline(request) : response
    } catch {
      return offline(request)
    }
  },
}

function offline(request) {
  const headers = { 'Cache-Control': 'no-store', 'Retry-After': '300' }
  const wantsPage = request.method === 'GET' && (request.headers.get('Accept') ?? '').includes('text/html')
  if (!wantsPage) {
    return new Response('Kitchen Command Center is offline. Try again soon.\n', {
      status: 503,
      headers: { ...headers, 'Content-Type': 'text/plain; charset=utf-8' },
    })
  }

  return new Response(PAGE, {
    status: 503,
    headers: { ...headers, 'Content-Type': 'text/html; charset=utf-8' },
  })
}

function font(name) {
  if (!Object.hasOwn(FONTS, name)) {
    return new Response('Not found\n', { status: 404 })
  }

  return new Response(
    Uint8Array.from(atob(FONTS[name]), (c) => c.charCodeAt(0)),
    {
      headers: {
        'Content-Type': 'font/woff2',
        'Cache-Control': 'public, max-age=604800',
      },
    },
  )
}
