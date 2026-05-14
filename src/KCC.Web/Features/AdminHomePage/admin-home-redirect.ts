/**
 * Injected into admin SPA HTML responses by AdminHomePageMiddleware.
 * Forces client-side navigation targeting /admin (the admin chrome's home
 * button uses pushState, which the server never sees) over to our custom
 * Home application instead of Kentico's default dashboard.
 */

const TARGET = '/admin/home'

;(function () {
  document.addEventListener('click', clickEvent, true)
  wrapHistoryMethod('pushState')
  wrapHistoryMethod('replaceState')
})()

function clickEvent(event: MouseEvent) {
  const target = event.target
  if (!(target instanceof Element)) return

  const anchor = target.closest('a')
  if (anchor && shouldRedirect(anchor.getAttribute('href'))) {
    event.preventDefault()
    event.stopImmediatePropagation()
    window.location.href = TARGET
  }
}

function wrapHistoryMethod(name: 'pushState' | 'replaceState') {
  const original = history[name]
  history[name] = (data, _, url) => {
    if (shouldRedirect(url)) {
      window.location.href = TARGET
      return
    }
    original.call(history, data, _, url)
  }
}

function shouldRedirect(url: string | URL | null | undefined) {
  if (!url) return false
  try {
    const parsed = new URL(url, window.location.origin)
    return parsed.pathname === '/admin' || parsed.pathname === '/admin/'
  } catch {
    return false
  }
}
