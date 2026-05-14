/**
 * Injected into admin SPA HTML responses by AdminHomePageMiddleware.
 * Forces client-side navigation targeting /admin (the admin chrome's home
 * button uses pushState, which the server never sees) over to our custom
 * Home application instead of Kentico's default dashboard.
 */

var TARGET = '/admin/home'

;(function () {
  document.addEventListener('click', clickEvent, true)
  wrapHistoryMethod('pushState')
  wrapHistoryMethod('replaceState')
})()

function clickEvent(event) {
  var target = event.target
  if (!target || typeof target.closest !== 'function') return

  var anchor = target.closest('a')
  if (anchor && shouldRedirect(anchor.getAttribute('href'))) {
    event.preventDefault()
    event.stopImmediatePropagation()
    window.location.href = TARGET
  }
}

function wrapHistoryMethod(name) {
  var original = history[name]
  history[name] = function (state, title, url) {
    if (shouldRedirect(url)) {
      window.location.href = TARGET
      return
    }
    return original.call(history, state, title, url)
  }
}

function shouldRedirect(url) {
  if (!url) return false
  try {
    var parsed = new URL(url, window.location.origin)
    return parsed.pathname === '/admin' || parsed.pathname === '/admin/'
  } catch {
    return false
  }
}
